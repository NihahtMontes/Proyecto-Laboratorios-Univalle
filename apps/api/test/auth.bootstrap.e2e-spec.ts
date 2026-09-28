import bcrypt from 'bcryptjs';
import {
  AuthBootstrapError,
  bootstrapInitialAdmin,
  createDefaultPasswordPolicy,
  type BootstrapClient,
  type InitialAdminBootstrapInput,
} from '../src/auth/auth-bootstrap.js';

type QueryLog = { readonly sql: string; readonly params: readonly unknown[] };

class BootstrapFakeClient implements BootstrapClient {
  readonly queries: QueryLog[] = [];

  constructor(private readonly state: Record<string, unknown>) {}

  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    this.queries.push({ sql, params: params ?? [] });
    if (sql.includes('current_database()')) {
      return Promise.resolve({ rows: [this.state as T], rowCount: 1 });
    }
    return Promise.resolve({ rows: [], rowCount: 1 });
  }
}

const PASSWORD = 'Bootstrap-Only#Password7';

function canonicalInput(
  overrides?: Partial<InitialAdminBootstrapInput>,
): InitialAdminBootstrapInput {
  return {
    expectedDatabase: 'GastroExample',
    siteCode: 'local.01',
    siteName: 'Sede Local',
    adminEmail: 'ADMIN@EXAMPLE.INVALID',
    adminFirstName: 'Initial',
    adminLastName: 'Administrator',
    adminIdentityCard: 'A1234567',
    adminPhoneNumber: '+15555550100',
    adminUsername: 'initialadmin',
    adminPassword: PASSWORD,
    bcryptCost: 10,
    auditHmacKey: 'bootstrap-test-hmac-key-at-least-32-characters',
    now: new Date('2026-09-19T12:00:00.000Z'),
    ...overrides,
  };
}

function emptyState(overrides?: Record<string, unknown>): Record<string, unknown> {
  return {
    database_name: 'GastroExample',
    site_count: 0,
    user_count: 0,
    superadmin_count: 0,
    site_id: null,
    site_name: null,
    site_code: null,
    site_status: null,
    user_id: null,
    user_full_name: null,
    user_status: null,
    is_super_admin: null,
    membership_role: null,
    membership_status: null,
    username: null,
    email: null,
    ...overrides,
  };
}

describe('initial administrator bootstrap', () => {
  it('creates site, SuperAdmin and identity audit rows in one transaction', async () => {
    const client = new BootstrapFakeClient(emptyState());

    const result = await bootstrapInitialAdmin(client, canonicalInput());

    expect(result.created).toBe(true);
    expect(result.siteId).toMatch(/^[0-9a-f-]{36}$/);
    expect(result.userId).toMatch(/^[0-9a-f-]{36}$/);
    expect(client.queries[0]?.sql).toBe('BEGIN ISOLATION LEVEL SERIALIZABLE');
    expect(client.queries.at(-1)?.sql).toBe('COMMIT');

    const siteInsert = client.queries.find((q) => q.sql.includes('INSERT INTO public.lu_site '));
    const userInsert = client.queries.find((q) => q.sql.includes('INSERT INTO public.lu_user'));
    const membershipInsert = client.queries.find((q) =>
      q.sql.includes('INSERT INTO public.lu_site_membership'),
    );
    const userAudit = client.queries.find((q) => q.sql.includes("'user_created'"));
    const superAdminAudit = client.queries.find((q) => q.sql.includes("'superadmin_granted'"));
    const legacyAudit = client.queries.find((q) => q.sql.includes('lu_security_event'));
    expect(siteInsert?.params[1]).toBe('LOCAL.01');
    expect(userInsert?.params[1]).toBe('admin@example.invalid');
    expect(userInsert?.params[2]).toBe('initialadmin');
    expect(userInsert?.params[3]).toBe('Initial');
    expect(userInsert?.params[4]).toBe('Administrator');
    expect(await bcrypt.compare(PASSWORD, String(userInsert?.params[7]))).toBe(true);
    expect(userInsert?.sql).toContain("'bcrypt'");
    expect(userInsert?.sql).toContain("'active'");
    expect(userInsert?.sql).toContain('is_super_admin');
    expect(userInsert?.sql).toContain("'canonical'");
    expect(membershipInsert?.params.slice(0, 2)).toEqual([result.userId, result.siteId]);
    expect(userAudit).toBeDefined();
    expect(superAdminAudit).toBeDefined();
    expect(legacyAudit).toBeDefined();
    expect(JSON.stringify(client.queries)).not.toContain(PASSWORD);
  });

  it('is idempotent only when the existing singleton state exactly matches', async () => {
    const client = new BootstrapFakeClient(
      emptyState({
        site_count: 1,
        user_count: 1,
        superadmin_count: 1,
        site_id: '22222222-2222-2222-2222-222222222222',
        site_name: 'Sede Local',
        site_code: 'LOCAL.01',
        site_status: 'active',
        user_id: '11111111-1111-1111-1111-111111111111',
        user_full_name: 'Initial Administrator',
        user_status: 'active',
        is_super_admin: true,
        membership_role: 'Administrador',
        membership_status: 'active',
        username: 'initialadmin',
        email: 'admin@example.invalid',
      }),
    );

    await expect(bootstrapInitialAdmin(client, canonicalInput())).resolves.toEqual({
      created: false,
      siteId: '22222222-2222-2222-2222-222222222222',
      userId: '11111111-1111-1111-1111-111111111111',
    });
    expect(client.queries.some((q) => q.sql.includes('INSERT INTO'))).toBe(false);
    expect(client.queries.at(-1)?.sql).toBe('COMMIT');
  });

  it('refuses partial or foreign identity state and rolls back without inserts', async () => {
    const client = new BootstrapFakeClient(emptyState({ site_count: 1 }));

    await expect(bootstrapInitialAdmin(client, canonicalInput())).rejects.toBeInstanceOf(
      AuthBootstrapError,
    );

    expect(client.queries.some((q) => q.sql.includes('INSERT INTO'))).toBe(false);
    expect(client.queries.at(-1)?.sql).toBe('ROLLBACK');
  });

  it('fails closed when the connected database differs from explicit confirmation', async () => {
    const client = new BootstrapFakeClient(emptyState({ database_name: 'wrong_database' }));

    await expect(bootstrapInitialAdmin(client, canonicalInput())).rejects.toThrow(
      'Bootstrap database confirmation does not match',
    );
    expect(client.queries.at(-1)?.sql).toBe('ROLLBACK');
  });

  it.each([
    { adminPassword: 'short' },
    { adminEmail: 'not-an-email' },
    { siteCode: '../bad' },
    { bcryptCost: 4 },
    { auditHmacKey: 'too-short' },
    { adminIdentityCard: 'lower-case-x' },
    { adminPhoneNumber: '' },
    { adminUsername: '' },
  ] satisfies Array<Partial<InitialAdminBootstrapInput>>)(
    'validates dangerous input before opening a transaction: %j',
    async (override) => {
      const client = new BootstrapFakeClient(emptyState());
      await expect(bootstrapInitialAdmin(client, canonicalInput(override))).rejects.toBeInstanceOf(
        AuthBootstrapError,
      );
      expect(client.queries).toHaveLength(0);
    },
  );
});

describe('default password policy shim', () => {
  it('reports every F1 §7 violation correctly', () => {
    const policy = createDefaultPasswordPolicy();
    expect(policy.validate('short')).toEqual(
      expect.arrayContaining(['too_short', 'missing_uppercase', 'missing_digit', 'missing_symbol']),
    );
    expect(policy.validate('GoodPassword1!').length).toBe(0);
  });
});
