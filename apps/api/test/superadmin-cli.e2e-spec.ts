import {
  applySuperAdminRole,
  parseSuperAdminArgs,
  type CliClient,
  type ParsedSuperAdminArgs,
} from '../src/users/superadmin-cli.js';

const ACTOR_ID = '00000000-0000-4000-8000-000000000099';
const TARGET_ID = '00000000-0000-4000-8000-000000000088';

function parsed(overrides: Partial<ParsedSuperAdminArgs> = {}): ParsedSuperAdminArgs {
  return {
    command: 'grant',
    userId: TARGET_ID,
    actorId: ACTOR_ID,
    reason: 'rotation',
    correlationId: '00000000-0000-4000-8000-000000000aaa',
    ...overrides,
  };
}

describe('parseSuperAdminArgs', () => {
  it('parses --grant with all required flags', () => {
    const args = parseSuperAdminArgs([
      '--grant',
      '--user',
      TARGET_ID,
      '--actor',
      ACTOR_ID,
      '--reason',
      'audited rotation',
    ]);
    expect(args.command).toBe('grant');
    expect(args.userId).toBe(TARGET_ID);
    expect(args.actorId).toBe(ACTOR_ID);
    expect(args.reason).toBe('audited rotation');
  });

  it.each(['short', `x${'a'.repeat(31)}\n`])('rejects an invalid reason: %s', (reason) => {
    expect(() =>
      parseSuperAdminArgs([
        '--grant',
        '--user',
        TARGET_ID,
        '--actor',
        ACTOR_ID,
        '--reason',
        reason,
      ]),
    ).toThrow(/reason/);
  });

  it('rejects missing command', () => {
    expect(() =>
      parseSuperAdminArgs(['--user', TARGET_ID, '--actor', ACTOR_ID, '--reason', 'r']),
    ).toThrow(/--grant|--revoke/);
  });

  it('rejects non-UUID user id', () => {
    expect(() =>
      parseSuperAdminArgs(['--grant', '--user', 'abc', '--actor', ACTOR_ID, '--reason', 'r']),
    ).toThrow(/--user/);
  });
});

/**
 * SQL-aware stand-in for the pg client: models lu_user / lu_session rows so
 * the CLI exercises SuperAdminRoleService, PgSessionInvalidator and
 * PgIdentityAuditWriter end to end.
 */
class ModelClient implements CliClient {
  readonly statements: string[] = [];
  readonly audits: unknown[][] = [];
  connected = false;
  ended = false;
  readonly users = new Map<
    string,
    { is_super_admin: boolean; account_status: string; security_version: number }
  >();
  readonly sessionsRevoked: string[] = [];

  async connect(): Promise<void> {
    this.connected = true;
  }
  async end(): Promise<void> {
    this.ended = true;
  }
  async query<T = Record<string, unknown>>(sql: string, params: unknown[] = []) {
    const text = sql.trim().replace(/\s+/g, ' ');
    this.statements.push(text);
    const rows = (value: unknown[]): { rows: T[]; rowCount: number } => ({
      rows: value as T[],
      rowCount: value.length,
    });
    if (/^(BEGIN|COMMIT|ROLLBACK|SET LOCAL|SELECT pg_advisory_xact_lock)/.test(text))
      return rows([]);
    if (
      text.startsWith('SELECT id, is_super_admin, account_status FROM public.lu_user WHERE id = $1')
    ) {
      const user = this.users.get(params[0] as string);
      return rows(user === undefined ? [] : [{ id: params[0], ...user }]);
    }
    if (text.startsWith('SELECT id FROM public.lu_user WHERE is_super_admin = true')) {
      return rows(
        [...this.users.entries()]
          .filter(([, u]) => u.is_super_admin && u.account_status === 'active')
          .map(([id]) => ({ id })),
      );
    }
    if (text.startsWith('UPDATE public.lu_user SET is_super_admin = $2')) {
      this.users.get(params[0] as string)!.is_super_admin = params[1] as boolean;
      return rows([{}]);
    }
    if (text.startsWith('UPDATE public.lu_user SET security_version = security_version + 1')) {
      const user = this.users.get(params[0] as string)!;
      user.security_version += 1;
      return rows([{ security_version: String(user.security_version) }]);
    }
    if (text.startsWith('UPDATE public.lu_session SET revoked_at')) {
      this.sessionsRevoked.push(params[1] as string);
      return rows([]);
    }
    if (text.startsWith('INSERT INTO public.lu_identity_audit_event')) {
      this.audits.push(params);
      return rows([]);
    }
    throw new Error('ModelClient: unexpected SQL ' + text.slice(0, 80));
  }
}

function modelWith(
  entries: Record<string, { is_super_admin: boolean; account_status?: string }>,
): ModelClient {
  const client = new ModelClient();
  for (const [id, u] of Object.entries(entries)) {
    client.users.set(id, {
      is_super_admin: u.is_super_admin,
      account_status: u.account_status ?? 'active',
      security_version: 1,
    });
  }
  return client;
}

describe('applySuperAdminRole (CLI over SuperAdminRoleService)', () => {
  it('grant flips the flag, rotates the subject security_version, revokes sessions and audits with actor and reason', async () => {
    const client = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: false },
    });
    const result = await applySuperAdminRole(parsed({ command: 'grant' }), client);
    expect(result).toEqual({ status: 'granted', targetUserId: TARGET_ID, actorUserId: ACTOR_ID });
    expect(client.users.get(TARGET_ID)).toMatchObject({
      is_super_admin: true,
      security_version: 2,
    });
    expect(client.sessionsRevoked).toEqual([TARGET_ID]);
    expect(client.statements[0]).toBe('BEGIN');
    expect(client.statements[1]).toBe('SET LOCAL ROLE lu_auth_runtime');
    expect(client.statements.at(-1)).toBe('COMMIT');
    const audit = client.audits[0]!;
    expect(audit).toContain('superadmin_granted');
    expect(audit).toContain(TARGET_ID);
    expect(audit).toContain(ACTOR_ID);
    expect(audit).toContain('rotation');
    expect(client.ended).toBe(true);
  });

  it('revoke refuses self-revoke and rolls back', async () => {
    const client = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: true },
    });
    await expect(
      applySuperAdminRole(parsed({ command: 'revoke', userId: ACTOR_ID }), client),
    ).rejects.toThrow(/own role/);
    expect(client.statements.at(-1)).toBe('ROLLBACK');
    expect(client.users.get(ACTOR_ID)?.is_super_admin).toBe(true);
  });

  it('revoke re-counts under the advisory lock and refuses to leave zero active SuperAdmins', async () => {
    // Defense in depth: simulate a concurrent demotion so the locked recount sees one row.
    const client = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: true },
    });
    const base = client.query.bind(client);
    client.query = (async (sql: string, params?: unknown[]) => {
      if (sql.includes('SELECT id FROM public.lu_user') && sql.includes('is_super_admin = true')) {
        client.statements.push(sql.trim().replace(/\s+/g, ' '));
        return { rows: [{ id: TARGET_ID }], rowCount: 1 };
      }
      return base(sql, params);
    }) as ModelClient['query'];
    await expect(applySuperAdminRole(parsed({ command: 'revoke' }), client)).rejects.toThrow(
      /last active SuperAdmin/,
    );
    expect(client.users.get(TARGET_ID)?.is_super_admin).toBe(true);
    const lock = client.statements.findIndex((s) => s.startsWith('SELECT pg_advisory_xact_lock'));
    const count = client.statements.findIndex((s) => s.startsWith('SELECT id FROM public.lu_user'));
    expect(lock).toBeGreaterThan(-1);
    expect(count).toBeGreaterThan(lock);
    expect(client.statements.some((s) => s.includes('count(') && s.includes('FOR UPDATE'))).toBe(
      false,
    );
  });

  it('revoke succeeds when another active SuperAdmin remains', async () => {
    const client = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: true },
    });
    const result = await applySuperAdminRole(parsed({ command: 'revoke' }), client);
    expect(result.status).toBe('revoked');
    expect(client.users.get(TARGET_ID)).toMatchObject({
      is_super_admin: false,
      security_version: 2,
    });
    expect(client.audits[0]).toContain('superadmin_revoked');
  });

  it.each([
    ['not a SuperAdmin', { is_super_admin: false }],
    ['an inactive SuperAdmin', { is_super_admin: true, account_status: 'inactive' }],
  ])('rejects an actor that is %s', async (_label, actor) => {
    const client = modelWith({ [ACTOR_ID]: actor, [TARGET_ID]: { is_super_admin: false } });
    await expect(applySuperAdminRole(parsed(), client)).rejects.toThrow(/active SuperAdmin/);
    expect(client.users.get(TARGET_ID)?.is_super_admin).toBe(false);
  });

  it('rejects a missing target and a deleted target', async () => {
    const missing = modelWith({ [ACTOR_ID]: { is_super_admin: true } });
    await expect(applySuperAdminRole(parsed(), missing)).rejects.toThrow(/Target was not found/);
    const deleted = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: false, account_status: 'deleted' },
    });
    await expect(applySuperAdminRole(parsed(), deleted)).rejects.toThrow(/deleted account/);
  });

  it('never touches credentials and always closes the client', async () => {
    const client = modelWith({
      [ACTOR_ID]: { is_super_admin: true },
      [TARGET_ID]: { is_super_admin: false },
    });
    await applySuperAdminRole(parsed(), client);
    expect(client.statements.some((s) => /password/i.test(s))).toBe(false);
    const failing = modelWith({});
    await expect(applySuperAdminRole(parsed(), failing)).rejects.toThrow();
    expect(failing.ended).toBe(true);
  });
});
