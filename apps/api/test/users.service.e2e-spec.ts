import { Test, type TestingModule } from '@nestjs/testing';
import { AUTH_PG_POOL, SELF_SESSION_RENEWER } from '../src/auth/auth.constants.js';
import {
  IDENTITY_AUDIT_WRITER,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  SESSION_INVALIDATOR,
} from '../src/identity/identity.contracts.js';
import {
  UserAuthorizationException,
  UserConflictException,
  UserService,
  UserValidationException,
} from '../src/users/user.service.js';
import { MembershipRepository } from '../src/users/membership.repository.js';
import { UserRepository } from '../src/users/user.repository.js';
import {
  FakeIdentityAuditWriter,
  FakePasswordHasher,
  FakePasswordPolicy,
  FakeSelfSessionRenewer,
  FakeSessionInvalidator,
  FakeUsersPgPool,
  buildCommandContext,
  makeMembershipRow,
  makeUserRow,
} from './users.fakes.js';

const SITE_A = '11111111-1111-4111-8111-111111111111';
const SITE_B = '22222222-2222-4222-8222-222222222222';
const ACTOR_ID = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const TARGET_ID = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
const ACTOR_SITE = SITE_A;
const OTHER_SITE = SITE_B;

async function makeService(): Promise<{
  moduleRef: TestingModule;
  service: UserService;
  pool: FakeUsersPgPool;
  invalidator: FakeSessionInvalidator;
  audit: FakeIdentityAuditWriter;
  renewer: FakeSelfSessionRenewer;
  hasher: FakePasswordHasher;
}> {
  const pool = new FakeUsersPgPool();
  const invalidator = new FakeSessionInvalidator();
  const audit = new FakeIdentityAuditWriter();
  const renewer = new FakeSelfSessionRenewer();
  const hasher = new FakePasswordHasher();
  const moduleRef = await Test.createTestingModule({
    providers: [
      UserService,
      UserRepository,
      MembershipRepository,
      { provide: AUTH_PG_POOL, useValue: pool },
      { provide: PASSWORD_POLICY, useValue: new FakePasswordPolicy(true) },
      { provide: PASSWORD_HASHER, useValue: hasher },
      { provide: SESSION_INVALIDATOR, useValue: invalidator },
      { provide: IDENTITY_AUDIT_WRITER, useValue: audit },
      { provide: SELF_SESSION_RENEWER, useValue: renewer },
    ],
  }).compile();
  return {
    moduleRef,
    service: moduleRef.get(UserService),
    pool,
    invalidator,
    audit,
    renewer,
    hasher,
  };
}

describe('UserService', () => {
  describe('createSite (site Administrator)', () => {
    it('creates the identity and an active-site membership in one transaction', async () => {
      const { service, pool, audit, hasher } = await makeService();
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'new@example.com',
          firstName: 'New',
          lastName: 'User',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      // Membership insert for the active site.
      pool.queueResult([
        {
          user_id: TARGET_ID,
          site_id: ACTOR_SITE,
          role: 'Administrador',
          status: 'active',
          valid_from: null,
          valid_until: null,
          position: null,
          department: null,
          hire_date: null,
        },
      ]);
      // listMembershipsForUpdate inside the transaction.
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      const ctx = buildCommandContext({
        userId: ACTOR_ID,
        activeSiteId: ACTOR_SITE,
        activeSiteRole: 'Administrador',
      });

      const record = await service.createSite(ctx, {
        username: 'new-user',
        email: 'NEW@example.com',
        firstName: 'New',
        lastName: 'User',
        identityCard: 'a1234567-8',
        phoneNumber: '+1 (555) 010-2030',
        password: 'Password!Aa1xyz',
        role: 'Administrador',
      });

      expect(record.id).toBe(TARGET_ID);
      expect(hasher.currentCost).toBe(12);
      const userInsert = pool.queries.find((q) => q.sql.startsWith('INSERT INTO public.lu_user'));
      expect(userInsert).toBeDefined();
      // reconciliation_state is not granted to lu_auth_runtime; its default is 'canonical'.
      expect(userInsert?.sql).not.toContain('reconciliation_state');
      expect(userInsert?.sql).toMatch(/'active', 'active', false,/);
      expect(audit.events.map((e) => e.action)).toContain('user_created');
    });

    it('SuperAdmin without a site can create a non-SuperAdmin globally', async () => {
      // The isSuperAdmin flag is a CLI-only flip; the user-creation service
      // refuses to set it. The HTTP controller parser drops the unknown key
      // before the service is called (covered by users.controller.e2e-spec.ts).
      const { service, pool } = await makeService();
      const ctx = buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true });
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'admin@example.com',
          firstName: 'A',
          lastName: 'B',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      const record = await service.createGlobal(ctx, {
        username: 'admin',
        email: 'admin@example.com',
        firstName: 'A',
        lastName: 'B',
        identityCard: 'A1234567-8',
        phoneNumber: '+1 (555) 010-2030',
        password: 'Password!Aa1xyz',
        memberships: [
          { siteId: ACTOR_SITE, role: 'Administrador', validFrom: null, validUntil: null },
        ],
      });
      expect(record.id).toBe(TARGET_ID);
      expect(record.isSuperAdmin).toBe(false);
      const insert = pool.queries.find((q) => q.sql.startsWith('INSERT INTO public.lu_user'));
      expect(insert?.sql).toMatch(/'active', 'active', false,/); // is_super_admin is never client-controlled
    });
  });

  describe('updateGlobalFields (section 12 rotation)', () => {
    it('rotates security version only when email changes', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      // First call: findGlobalForUpdate (target)
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'old@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      // Cosmetic change (names only): no rotation, profile_updated event.
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'old@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await service.updateGlobalFields(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        { firstName: 'Alicia' },
      );

      expect(invalidator.rotations).toHaveLength(0);
      expect(audit.events.at(-1)?.action).toBe('profile_updated');
    });

    it('rotates on email change and writes email_changed audit', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'old@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      // findGlobalForUpdate + listMembershipsForUpdate after update.
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'new@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await service.updateGlobalFields(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        { email: 'NEW@example.com', reason: 'verification' },
      );

      expect(invalidator.rotations).toHaveLength(1);
      expect(invalidator.rotations[0]?.reason).toBe('email_change');
      expect(audit.events.find((e) => e.action === 'email_changed')).toBeDefined();
    });

    it('mixed cosmetic + sensitive: the sensitive rule wins (rotation happens)', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'old@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'new@example.com',
          firstName: 'Alice',
          lastName: 'Smith',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await service.updateGlobalFields(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        { firstName: 'Alicia', email: 'NEW@example.com' },
      );
      expect(invalidator.rotations).toHaveLength(1);
      expect(audit.events.find((e) => e.action === 'email_changed')).toBeDefined();
    });
  });

  describe('adminResetPassword', () => {
    it('rotates security_version and writes password_reset audit', async () => {
      const { service, pool, invalidator, audit, hasher } = await makeService();
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 't@example.com',
          firstName: 'T',
          lastName: 'U',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
          passwordScheme: 'reset_required',
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await service.adminResetPassword(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        { password: 'NewPassword!Aa1xyz', reason: 'forgot' },
      );
      expect(hasher.currentCost).toBe(12);
      expect(invalidator.rotations[0]?.reason).toBe('admin_password_reset');
      expect(audit.events.at(-1)?.action).toBe('password_reset');
      expect(audit.events.at(-1)?.reason).toBe('forgot');
    });
  });

  describe('setAccountStatus', () => {
    it('refuses self-protection', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([
        makeUserRow({
          id: ACTOR_ID,
          email: 'a@example.com',
          firstName: 'A',
          lastName: 'A',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await expect(
        service.setAccountStatus(
          buildCommandContext({
            userId: ACTOR_ID,
            activeSiteId: ACTOR_SITE,
            activeSiteRole: 'Administrador',
          }),
          ACTOR_ID,
          { accountStatus: 'inactive' },
        ),
      ).rejects.toThrow(/SELF_PROTECTION/);
    });

    it('refuses last-SuperAdmin inactivation (advisory lock + recount)', async () => {
      const { service, pool } = await makeService();
      // 1) advisory lock
      pool.queueResult([]);
      // 2) find target FOR UPDATE
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'sa@example.com',
          firstName: 'A',
          lastName: 'A',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: true,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      // 3) count of active SuperAdmins (FOR UPDATE)
      pool.queueResult([{ c: 1 }]);

      await expect(
        service.setAccountStatus(
          buildCommandContext({
            userId: ACTOR_ID,
            isSuperAdmin: true,
          }),
          TARGET_ID,
          { accountStatus: 'inactive' },
        ),
      ).rejects.toThrow(/LAST_SUPERADMIN/);
    });
  });

  describe('globalDelete', () => {
    it('marks account deleted and revokes every membership', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 't@example.com',
          firstName: 'T',
          lastName: 'U',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
        makeMembershipRow({ siteId: OTHER_SITE, role: 'Supervisor', status: 'active' }, 'Sede B'),
      ]);

      await service.globalDelete(
        buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true }),
        TARGET_ID,
        { reason: 'audit' },
      );

      const revokeCalls = pool.queries.filter((q) => q.sql.includes("status = 'revoked'"));
      expect(revokeCalls.length).toBeGreaterThanOrEqual(2);
      expect(invalidator.rotations[0]?.reason).toBe('access_revoked');
      expect(audit.events.find((e) => e.action === 'account_deleted')).toBeDefined();
    });

    it('refuses last-SuperAdmin delete', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 'sa@example.com',
          firstName: 'S',
          lastName: 'A',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: true,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      pool.queueResult([{ c: 1 }]); // count

      await expect(
        service.globalDelete(
          buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true }),
          TARGET_ID,
          { reason: 'audit' },
        ),
      ).rejects.toThrow(/LAST_SUPERADMIN/);
    });
  });

  describe('revokeActiveSite (F1 §11 cascade)', () => {
    it('does not cascade when other non-revoked memberships remain', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 't@example.com',
          firstName: 'T',
          lastName: 'U',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      pool.queueResult([
        { site_id: ACTOR_SITE, status: 'revoked' },
        { site_id: OTHER_SITE, status: 'active' },
      ]);

      await service.revokeActiveSite(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        ACTOR_SITE,
        { reason: 'leaving' },
      );
      expect(invalidator.rotations[0]?.reason).toBe('access_revoked');
      expect(audit.events.some((e) => e.action === 'account_deleted')).toBe(false);
    });

    it('cascades to deleted when membership-less and not SuperAdmin', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 't@example.com',
          firstName: 'T',
          lastName: 'U',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);
      pool.queueResult([{ site_id: ACTOR_SITE, status: 'revoked' }]);

      await service.revokeActiveSite(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        ACTOR_SITE,
        { reason: 'leaving' },
      );
      const softDelete = pool.queries.find((q) => q.sql.includes("account_status = 'deleted'"));
      expect(softDelete).toBeDefined();
      expect(audit.events.some((e) => e.action === 'account_deleted')).toBe(true);
      expect(invalidator.rotations[0]?.reason).toBe('access_revoked');
    });
  });

  describe('updateWorkProfile', () => {
    it('does not rotate security_version', async () => {
      const { service, pool, invalidator } = await makeService();
      pool.queueResult([
        makeUserRow({
          id: TARGET_ID,
          email: 't@example.com',
          firstName: 'T',
          lastName: 'U',
          identityCard: 'A1234567-8',
          phoneNumber: '+1 (555) 010-2030',
          accountStatus: 'active',
          isSuperAdmin: false,
        }),
      ]);
      pool.queueResult([
        makeMembershipRow(
          { siteId: ACTOR_SITE, role: 'Administrador', status: 'active' },
          'Sede A',
        ),
      ]);

      await service.updateWorkProfile(
        buildCommandContext({
          userId: ACTOR_ID,
          activeSiteId: ACTOR_SITE,
          activeSiteRole: 'Administrador',
        }),
        TARGET_ID,
        ACTOR_SITE,
        { position: 'Técnico', department: 'Lab', hireDate: '2026-09-01' },
      );
      expect(invalidator.rotations).toHaveLength(0);
    });
  });

  describe('validation (400 instead of DB 500)', () => {
    it('rejects invalid email', async () => {
      const { service } = await makeService();
      await expect(
        service.createGlobal(buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true }), {
          username: 'a',
          email: 'not-an-email',
          firstName: 'A',
          lastName: 'B',
          identityCard: 'A1',
          phoneNumber: '+1 5550102030',
          password: 'Password!Aa1xyz',
          memberships: [
            { siteId: ACTOR_SITE, role: 'Administrador', validFrom: null, validUntil: null },
          ],
        }),
      ).rejects.toBeInstanceOf(UserValidationException);
    });

    it('rejects too-short password against the policy', async () => {
      const { pool } = await makeService();
      pool.queueResult([{ count: 0 }]); // unused
      const strict = Test.createTestingModule({
        providers: [
          UserService,
          UserRepository,
          MembershipRepository,
          { provide: AUTH_PG_POOL, useValue: new FakeUsersPgPool() },
          { provide: PASSWORD_POLICY, useValue: new FakePasswordPolicy(false) },
          { provide: PASSWORD_HASHER, useValue: new FakePasswordHasher() },
          { provide: SESSION_INVALIDATOR, useValue: new FakeSessionInvalidator() },
          { provide: IDENTITY_AUDIT_WRITER, useValue: new FakeIdentityAuditWriter() },
          { provide: SELF_SESSION_RENEWER, useValue: new FakeSelfSessionRenewer() },
        ],
      });
      const strictRef = await strict.compile();
      const strictService = strictRef.get(UserService);
      await expect(
        strictService.createGlobal(buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true }), {
          username: 'a',
          email: 'a@example.com',
          firstName: 'A',
          lastName: 'B',
          identityCard: 'A1',
          phoneNumber: '+1 5550102030',
          password: 'short',
          memberships: [
            {
              siteId: ACTOR_SITE,
              role: 'Administrador',
              validFrom: null,
              validUntil: null,
            },
          ],
        }),
      ).rejects.toBeInstanceOf(UserValidationException);
      void pool;
    });
  });
});
describe('UserService F3 review regressions', () => {
  const targetRow = (overrides: Partial<Parameters<typeof makeUserRow>[0]> = {}) =>
    makeUserRow({
      id: TARGET_ID,
      email: 't@example.com',
      firstName: 'T',
      lastName: 'U',
      identityCard: 'A1234567',
      phoneNumber: '+1 (555) 010-2030',
      accountStatus: 'active',
      isSuperAdmin: false,
      ...overrides,
    });
  const siteAdmin = () =>
    buildCommandContext({
      userId: ACTOR_ID,
      activeSiteId: ACTOR_SITE,
      activeSiteRole: 'Administrador',
    });
  const superAdmin = () => buildCommandContext({ userId: ACTOR_ID, isSuperAdmin: true });
  const storedMembership = (status: string, extra: Record<string, unknown> = {}) => ({
    user_id: TARGET_ID,
    site_id: ACTOR_SITE,
    role: 'Supervisor',
    status,
    valid_from: null,
    valid_until: null,
    position: 'Analyst',
    department: null,
    hire_date: null,
    ...extra,
  });

  describe('custody is never vacuous (F1 §10)', () => {
    it('denies a site Administrator global edits on a user without non-revoked memberships', async () => {
      const { service, pool, invalidator } = await makeService();
      pool.queueResult([targetRow()]);
      pool.queueResult([
        makeMembershipRow({ siteId: OTHER_SITE, role: 'Supervisor', status: 'revoked' }, 'Sede B'),
      ]);
      await expect(
        service.updateGlobalFields(siteAdmin(), TARGET_ID, { phoneNumber: '+1 555 010 2030' }),
      ).rejects.toBeInstanceOf(UserAuthorizationException);
      expect(pool.queries.some((q) => q.sql.startsWith('UPDATE public.lu_user'))).toBe(false);
      expect(invalidator.rotations).toHaveLength(0);
    });

    it('denies a site Administrator a password reset of a membership-less account', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([targetRow()]);
      pool.queueResult([]);
      await expect(
        service.adminResetPassword(siteAdmin(), TARGET_ID, { password: 'Valid-Passw0rd!' }),
      ).rejects.toBeInstanceOf(UserAuthorizationException);
    });

    it('denies a site Administrator restoring a globally deleted account through restoreAccount', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow({ accountStatus: 'deleted' })]);
      pool.queueResult([]);
      await expect(service.restoreAccount(siteAdmin(), TARGET_ID, {})).rejects.toBeInstanceOf(
        UserAuthorizationException,
      );
    });
  });

  it('a site Administrator cannot link an existing user to its site (F1 §17)', async () => {
    const { service, pool } = await makeService();
    pool.queueResult([targetRow()]);
    await expect(
      service.addMembership(siteAdmin(), TARGET_ID, {
        siteId: ACTOR_SITE,
        role: 'Supervisor',
        validFrom: null,
        validUntil: null,
      }),
    ).rejects.toMatchObject({ code: 'LINK_REQUIRES_SUPERADMIN' });
    expect(pool.queries.some((q) => q.sql.includes('lu_site_membership'))).toBe(false);
  });

  it('authorizes before looking up a membership in another site (no cross-site probing)', async () => {
    const { service, pool } = await makeService();
    pool.queueResult([targetRow()]);
    await expect(
      service.changeRole(siteAdmin(), TARGET_ID, OTHER_SITE, { role: 'Administrador' }),
    ).rejects.toBeInstanceOf(UserAuthorizationException);
    expect(pool.queries.some((q) => q.sql.includes('lu_site_membership'))).toBe(false);
  });

  it('refuses to change a revoked membership (restore first)', async () => {
    const { service, pool, invalidator } = await makeService();
    pool.queueResult([targetRow()]);
    pool.queueResult([storedMembership('revoked')]);
    await expect(
      service.changeStatus(siteAdmin(), TARGET_ID, ACTOR_SITE, { status: 'active' }),
    ).rejects.toBeInstanceOf(UserConflictException);
    expect(invalidator.rotations).toHaveLength(0);
  });

  describe('restoreMembership (F1 §11)', () => {
    it('only restores a revoked membership', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow()]);
      pool.queueResult([storedMembership('suspended')]);
      await expect(
        service.restoreMembership(siteAdmin(), TARGET_ID, ACTOR_SITE, {}),
      ).rejects.toBeInstanceOf(UserConflictException);
    });

    it('never silently extends expired validity', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow()]);
      pool.queueResult([
        storedMembership('revoked', { valid_until: new Date('2020-01-01T00:00:00Z') }),
      ]);
      await expect(
        service.restoreMembership(siteAdmin(), TARGET_ID, ACTOR_SITE, {}),
      ).rejects.toBeInstanceOf(UserValidationException);
    });

    it('keeps the stored role, rotates once and audits when new bounds are supplied', async () => {
      const { service, pool, invalidator, audit } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow()]);
      pool.queueResult([
        storedMembership('revoked', { valid_until: new Date('2020-01-01T00:00:00Z') }),
      ]);
      await service.restoreMembership(siteAdmin(), TARGET_ID, ACTOR_SITE, {
        validUntil: '2999-01-01T00:00:00.000Z',
      });
      const update = pool.queries.find((q) => q.sql.startsWith('UPDATE public.lu_site_membership'));
      expect(update?.params).toContain('Supervisor');
      expect(invalidator.rotations.map((r) => r.reason)).toEqual(['access_restored']);
      expect(audit.events.map((e) => e.action)).toEqual(['membership_restored']);
    });

    it("lets a site Administrator reverse its own site's revoke cascade atomically", async () => {
      const { service, pool, audit } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow({ accountStatus: 'deleted' })]);
      pool.queueResult([storedMembership('revoked')]);
      pool.queueResult([{ site_id: ACTOR_SITE, reason: 'site_revoke_cascade' }]);
      pool.queueResult([{ site_id: ACTOR_SITE, status: 'revoked' }]);
      await service.restoreMembership(siteAdmin(), TARGET_ID, ACTOR_SITE, { restoreAccount: true });
      expect(pool.queries.some((q) => q.sql.startsWith('UPDATE public.lu_user'))).toBe(true);
      expect(audit.events.map((e) => e.action)).toEqual([
        'membership_restored',
        'account_restored',
      ]);
    });

    it('refuses a site Administrator when the account was deleted globally', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow({ accountStatus: 'deleted' })]);
      pool.queueResult([storedMembership('revoked')]);
      pool.queueResult([{ site_id: null, reason: 'global delete' }]);
      pool.queueResult([]);
      await expect(
        service.restoreMembership(siteAdmin(), TARGET_ID, ACTOR_SITE, { restoreAccount: true }),
      ).rejects.toMatchObject({ code: 'SUPERADMIN_REQUIRED' });
    });

    it('requires restoring a deleted account together with the membership', async () => {
      const { service, pool } = await makeService();
      pool.queueResult([]); // advisory lock
      pool.queueResult([targetRow({ accountStatus: 'deleted' })]);
      pool.queueResult([storedMembership('revoked')]);
      await expect(
        service.restoreMembership(superAdmin(), TARGET_ID, ACTOR_SITE, {}),
      ).rejects.toBeInstanceOf(UserConflictException);
    });
  });

  it('renews the self session only after the email-change transaction has committed', async () => {
    const { service, pool, renewer } = await makeService();
    const ctx = {
      ...buildCommandContext({ userId: TARGET_ID, isSuperAdmin: true }),
      reply: { header: () => undefined } as never,
    };
    let inTransaction = false;
    let renewedInsideTransaction: boolean | null = null;
    const originalTransaction = pool.transaction.bind(pool);
    pool.transaction = (async (fn: Parameters<typeof pool.transaction>[0]) => {
      inTransaction = true;
      try {
        return await originalTransaction(fn);
      } finally {
        inTransaction = false;
      }
    }) as typeof pool.transaction;
    const originalRenew = renewer.renew.bind(renewer);
    renewer.renew = (reply, identity) => {
      renewedInsideTransaction = inTransaction;
      return originalRenew(reply, identity);
    };
    pool.queueResult([targetRow({ id: TARGET_ID, email: 'old@example.com' })]);
    pool.queueResult([targetRow({ id: TARGET_ID, email: 'new@example.com' })]);
    pool.queueResult([]);
    await service.updateProfile(ctx, { email: 'new@example.com' });
    expect(renewer.renewCalls).toBe(1);
    expect(renewedInsideTransaction).toBe(false);
  });

  it('lists only the active-site projection for a site Administrator', async () => {
    const { service, pool } = await makeService();
    pool.queueResult([{ total_count: 0 }]);
    pool.queueResult([]);
    const page = await service.list(siteAdmin(), { statusFilter: 'deleted' } as never);
    expect(page.totalCount).toBe(0);
    expect(pool.queries.every((q) => q.params[0] === ACTOR_SITE)).toBe(true);
    expect(pool.queries[1]?.sql).toContain('m.site_id = $1');
  });
});
