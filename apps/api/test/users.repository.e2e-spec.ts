import { readFileSync } from 'node:fs';
import { FakeUsersPgPool, makeMembershipRow, makeUserRow } from './users.fakes.js';
import { MembershipRepository } from '../src/users/membership.repository.js';
import { UserRepository } from '../src/users/user.repository.js';
import {
  activeRoleOf,
  initialsOf,
  mapMemberships,
  type MembershipSnapshotRow,
} from '../src/users/user.projection.js';

const SITE_A = '11111111-1111-1111-1111-111111111111';
const SITE_B = '22222222-2222-2222-2222-222222222222';
const USER_A = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';

describe('UserRepository + MembershipRepository SQL shape', () => {
  it('listGlobal builds parameterized SQL with placeholders and never writes full_name / row_version', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);

    pool.queueResult([{ total_count: 1 }]);
    pool.queueResult([
      makeUserRow({
        id: USER_A,
        email: 'a@example.com',
        firstName: 'Alice',
        lastName: 'Smith',
        identityCard: 'A1234567-8',
        phoneNumber: '+1 (555) 010-2030',
        accountStatus: 'active',
        isSuperAdmin: false,
      }),
    ]);

    await repo.listGlobal({ currentPage: 1, statusFilter: 'active', searchTerm: 'al' });

    const countSql = pool.queries[0]?.sql ?? '';
    expect(countSql).toContain('count(*)::int');
    expect(countSql).not.toMatch(/row_version/);
    expect(countSql).not.toMatch(/full_name\s*=/);

    const listSql = pool.queries[1]?.sql ?? '';
    expect(listSql).not.toMatch(/full_name\s*=/);
    expect(listSql).not.toMatch(/row_version\s*=/);
    expect(listSql).not.toMatch(/created_at\s*=/);
    expect(listSql).toContain('ORDER BY full_name ASC, id ASC');
    expect(listSql).toMatch(/LIMIT \$3 OFFSET \$4/);
    expect(pool.queries[0]?.params).toEqual(['active', '%al%']);
    expect(pool.queries[1]?.params).toEqual(['active', '%al%', 20, 0]);
  });

  it('findGlobalForUpdate uses SELECT ... FOR UPDATE and does not select full_name for writes', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([
      makeUserRow({
        id: USER_A,
        email: 'a@example.com',
        firstName: 'Alice',
        lastName: 'Smith',
        identityCard: 'A1234567-8',
        phoneNumber: '+1 (555) 010-2030',
        accountStatus: 'active',
        isSuperAdmin: false,
      }),
    ]);

    await repo.findGlobalForUpdate(pool, USER_A);
    const sql = pool.queries[0]?.sql ?? '';
    expect(sql).toContain('FOR UPDATE');
    expect(sql).toContain('SELECT');
    expect(sql).toContain('lu_user');
    expect(sql).not.toContain('full_name =');
  });

  it('updateGlobalFields writes only mutated columns and never full_name / row_version', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([]);
    await repo.updateGlobalFields(pool, {
      id: USER_A,
      email: 'a@example.com',
      firstName: 'Alice',
      modifiedByUserId: '00000000-0000-4000-8000-000000000001',
    });
    const sql = pool.queries[0]?.sql ?? '';
    expect(sql).toContain('UPDATE public.lu_user');
    expect(sql).not.toMatch(/full_name\s*=/);
    expect(sql).not.toMatch(/row_version\s*=/);
    expect(sql).not.toMatch(/security_version\s*=/);
    expect(sql).not.toMatch(/is_super_admin\s*=/);
    expect(pool.queries[0]?.params).toEqual([
      '00000000-0000-4000-8000-000000000001',
      'a@example.com',
      'Alice',
      USER_A,
    ]);
  });

  it('insertUser never references full_name/row_version/claims and inserts canonical reconciliation_state', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([
      makeUserRow({
        id: USER_A,
        email: 'a@example.com',
        firstName: 'Alice',
        lastName: 'Smith',
        identityCard: 'A1234567-8',
        phoneNumber: '+1 (555) 010-2030',
        accountStatus: 'active',
        isSuperAdmin: false,
      }),
    ]);
    await repo.insertUser(pool, {
      id: USER_A,
      email: 'a@example.com',
      username: 'alice',
      firstName: 'Alice',
      lastName: 'Smith',
      secondLastName: null,
      identityCard: 'A1234567-8',
      phoneNumber: '+1 (555) 010-2030',
      passwordHash: '$2b$12$abc',
      mustChangePassword: true,
      createdByUserId: '00000000-0000-4000-8000-000000000001',
    });
    const sql = pool.queries[0]?.sql ?? '';
    expect(sql).toContain('INSERT INTO public.lu_user');
    expect(sql).not.toContain('lu_login_identifier');
    expect(sql).not.toMatch(/full_name\s*=/);
    expect(sql).not.toMatch(/row_version\s*=/);
    // Only columns granted to lu_auth_runtime (control-plane 0006) may appear;
    // reconciliation_state keeps its 'canonical' default.
    const grant = readFileSync(
      new URL('../migrations/control-plane/0006_mig001_users_identity.sql', import.meta.url),
      'utf8',
    ).match(/GRANT INSERT \(([^)]+)\) ON TABLE public\.lu_user TO lu_auth_runtime/);
    const granted = new Set(grant![1]!.split(',').map((c) => c.trim()));
    const columns = sql
      .match(/INSERT INTO public\.lu_user \(([^)]+)\)/)![1]!
      .split(',')
      .map((c) => c.trim());
    expect(columns.filter((c) => !granted.has(c))).toEqual([]);
    expect(sql).not.toContain('reconciliation_state');
    expect(sql).toMatch(/'active', 'active', false,/);
  });

  it('membership revoke + restore flow does not update row_version manually', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new MembershipRepository(pool);
    pool.queueResult([]);
    pool.queueResult([]);
    await repo.revoke(pool, {
      userId: USER_A,
      siteId: SITE_A,
      modifiedByUserId: '00000000-0000-4000-8000-000000000002',
    });
    await repo.restore(pool, {
      userId: USER_A,
      siteId: SITE_A,
      role: 'Administrador',
      validFrom: null,
      validUntil: null,
      modifiedByUserId: '00000000-0000-4000-8000-000000000002',
    });
    const revokeSql = pool.queries[0]?.sql ?? '';
    expect(revokeSql).toContain("status = 'revoked'");
    expect(revokeSql).not.toContain('row_version');
    const restoreSql = pool.queries[1]?.sql ?? '';
    expect(restoreSql).toContain("status = 'active'");
    expect(restoreSql).not.toContain('row_version');
  });

  it('listMembershipsForUser joins lu_site and never writes', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([
      makeMembershipRow({ siteId: SITE_A, role: 'Administrador', status: 'active' }, 'Sede A'),
    ]);
    await repo.listMembershipsForUser(USER_A);
    const sql = pool.queries[0]?.sql ?? '';
    expect(sql).toContain('JOIN public.lu_site s ON s.id = m.site_id');
    expect(sql).not.toContain('INSERT');
    expect(sql).not.toContain('UPDATE');
  });

  it('countActiveSuperAdminsForUpdate locks the qualifying rows (no aggregate with FOR UPDATE)', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([{ id: USER_A }, { id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' }]);
    const count = await repo.countActiveSuperAdminsForUpdate(pool);
    expect(count).toBe(2);
    const sql = pool.queries[0]?.sql ?? '';
    expect(sql).toContain('FOR UPDATE');
    expect(sql.toLowerCase()).not.toContain('count(');
    expect(sql).toContain('is_super_admin = true');
    expect(sql).toContain("account_status = 'active'");
  });

  it('countNonRevokedMembershipsForUpdate separates revoked vs non-revoked rows', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([
      { site_id: SITE_A, status: 'active' },
      { site_id: SITE_B, status: 'revoked' },
      { site_id: '33333333-3333-4333-8333-333333333333', status: 'active' },
    ]);
    const result = await repo.countNonRevokedMembershipsForUpdate(pool, USER_A);
    expect(result.nonRevokedCount).toBe(2);
    expect(result.inSite[SITE_A]).toBe(1);
    expect(result.inSite['33333333-3333-4333-8333-333333333333']).toBe(1);
  });

  it('does not write to lu_login_identifier or full_name/row_version anywhere', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([]);
    pool.queueResult([]);
    pool.queueResult([]);
    await repo.softDeleteAccount(pool, {
      id: USER_A,
      modifiedByUserId: '00000000-0000-4000-8000-000000000099',
    });
    await repo.setAccountStatus(pool, {
      id: USER_A,
      accountStatus: 'inactive',
      modifiedByUserId: '00000000-0000-4000-8000-000000000099',
    });
    const sql = pool.queries.map((q) => q.sql).join('\n');
    expect(sql).not.toMatch(/lu_login_identifier/);
    expect(sql).not.toMatch(/full_name\s*=/);
    expect(sql).not.toMatch(/row_version\s*=/);
  });
});

describe('user projection (F1 §8, §11)', () => {
  const now = new Date('2026-09-25T12:00:00Z');
  const row = (overrides: Partial<MembershipSnapshotRow> = {}): MembershipSnapshotRow => ({
    site_id: SITE_A,
    site_name: 'Sede A',
    role: 'Administrador',
    membership_status: 'active',
    valid_from: null,
    valid_until: null,
    position: null,
    department: null,
    hire_date: null,
    ...overrides,
  });

  it.each([
    ['active account, active eligible membership', 'active', {}, 'Activo'],
    ['inactive account', 'inactive', {}, 'Inactivo'],
    ['suspended membership', 'active', { membership_status: 'suspended' }, 'Inactivo'],
    ['deleted account', 'deleted', {}, 'Eliminado'],
    ['revoked membership', 'active', { membership_status: 'revoked' }, 'Eliminado'],
    [
      'revoked membership of an inactive account',
      'inactive',
      { membership_status: 'revoked' },
      'Eliminado',
    ],
    [
      'validity not started',
      'active',
      { valid_from: new Date('2026-10-01T00:00:00Z') },
      'Inactivo',
    ],
    ['validity ended (exclusive end)', 'active', { valid_until: now }, 'Inactivo'],
  ] as const)('%s => %s', (_label, account, overrides, expected) => {
    const [projected] = mapMemberships(
      [row(overrides as Partial<MembershipSnapshotRow>)],
      now,
      account,
    );
    expect(projected?.effectiveStatus).toBe(expected);
  });

  it('derives initials from the first grapheme of first and last name', () => {
    expect(initialsOf('ángel', 'Ñuñez')).toBe('ÁÑ');
    expect(initialsOf('  ana ', ' pérez')).toBe('AP');
  });

  it('shows the active-site role, ignoring revoked memberships', () => {
    const actor = { activeSiteId: SITE_A } as Parameters<typeof activeRoleOf>[1];
    expect(activeRoleOf([row({ role: 'Supervisor' })], actor)).toBe('Supervisor');
    expect(activeRoleOf([row({ membership_status: 'revoked' })], actor)).toBeNull();
    expect(activeRoleOf([row({ site_id: SITE_B })], actor)).toBeNull();
  });
});

describe('UserRepository.listForSite (site Administrator projection)', () => {
  it('scopes rows, totals and paging to the active site in SQL', async () => {
    const pool = new FakeUsersPgPool();
    const repo = new UserRepository(pool);
    pool.queueResult([{ total_count: 0 }]);
    pool.queueResult([]);
    await repo.listForSite(SITE_A, { currentPage: 2, searchTerm: 'ana' });
    const [count, page] = pool.queries;
    expect(count?.sql).toContain('m.site_id = $1');
    expect(page?.sql).toContain('m.site_id = $1');
    expect(count?.params[0]).toBe(SITE_A);
    expect(page?.params).toEqual([SITE_A, '%ana%', 20, 20]);
    expect(page?.sql).toContain("u.account_status <> 'deleted' AND m.status <> 'revoked'");
  });

  it.each([
    ['active', "u.account_status = 'active' AND m.status = 'active'"],
    ['deleted', "(u.account_status = 'deleted' OR m.status = 'revoked')"],
  ] as const)(
    'maps the explicit %s filter to the effective-status predicate',
    async (filter, predicate) => {
      const pool = new FakeUsersPgPool();
      const repo = new UserRepository(pool);
      pool.queueResult([{ total_count: 0 }]);
      pool.queueResult([]);
      await repo.listForSite(SITE_A, { statusFilter: filter });
      expect(pool.queries[1]?.sql).toContain(predicate);
    },
  );
});
