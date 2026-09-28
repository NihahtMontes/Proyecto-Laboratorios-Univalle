import { UserAccessPolicy } from '../src/users/user.policy.js';
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus,
  RequestIdentity,
} from '../src/identity/identity.contracts.js';

const SITE_A = '11111111-1111-1111-1111-111111111111';
const SITE_B = '22222222-2222-2222-2222-222222222222';
const ACTOR_ID = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
const SUBJECT_ID = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';

function identity(overrides: Partial<RequestIdentity> = {}): RequestIdentity {
  return {
    correlationId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    sessionId: 'ssssssss-ssss-4sss-8sss-ssssssssssss',
    userId: ACTOR_ID,
    isSuperAdmin: false,
    activeSiteId: null,
    activeSiteRole: null,
    ...overrides,
  };
}

function nonRevoked(
  siteId: string,
  role: MembershipRole = 'Administrador',
): {
  siteId: string;
  role: MembershipRole;
  status: MembershipStatus;
} {
  return { siteId, role, status: 'active' };
}

function expectDenied(
  decision: { allowed: true } | { allowed: false; reason: string },
  reason: string,
): void {
  expect(decision.allowed).toBe(false);
  if (decision.allowed === false) {
    expect(decision.reason).toBe(reason);
  }
}

describe('UserAccessPolicy', () => {
  const policy = new UserAccessPolicy();

  describe('canListUsers', () => {
    it('forbids Supervisor', () => {
      const decision = policy.canListUsers(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Supervisor' }),
      );
      expectDenied(decision, 'SITE_ACCESS_DENIED');
    });

    it('allows SuperAdmin regardless of site', () => {
      expect(policy.canListUsers(identity({ isSuperAdmin: true })).allowed).toBe(true);
      expect(
        policy.canListUsers(
          identity({ isSuperAdmin: true, activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        ).allowed,
      ).toBe(true);
    });

    it('allows Administrator with an active site', () => {
      expect(
        policy.canListUsers(identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }))
          .allowed,
      ).toBe(true);
    });
  });

  describe('canViewUser', () => {
    it('forbids Supervisor', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Supervisor' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [nonRevoked(SITE_A)],
      );
      expect(decision.allowed).toBe(false);
    });

    it('allows SuperAdmin to view any account', () => {
      const decision = policy.canViewUser(
        identity({ isSuperAdmin: true }),
        { id: SUBJECT_ID, accountStatus: 'deleted', isSuperAdmin: false },
        [],
      );
      expect(decision.allowed).toBe(true);
    });

    it('allows site Administrator to view a subject in their active site', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [nonRevoked(SITE_A)],
      );
      expect(decision.allowed).toBe(true);
    });

    it('forbids a site Administrator when the only rows (even revoked) belong to other sites', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'deleted', isSuperAdmin: false },
        [{ ...nonRevoked(SITE_B), status: 'revoked' }],
      );
      expect(decision.allowed).toBe(false);
    });

    it('forbids site Administrator from viewing a subject with no memberships (vacuous custody)', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [],
      );
      expect(decision.allowed).toBe(false);
    });

    it('forbids site Administrator from viewing a subject outside their site', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [nonRevoked(SITE_B)],
      );
      expect(decision.allowed).toBe(false);
    });

    // F8: F1 §11 site history — a revoked membership of the active site stays
    // visible in Details so the site Administrador can restore it.
    it('allows site Administrator to view a subject whose active-site membership is revoked', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [{ ...nonRevoked(SITE_A), status: 'revoked' }, nonRevoked(SITE_B)],
      );
      expect(decision.allowed).toBe(true);
    });

    it('allows site Administrator to view an account deleted with its active-site membership revoked', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'deleted', isSuperAdmin: false },
        [{ ...nonRevoked(SITE_A), status: 'revoked' }],
      );
      expect(decision.allowed).toBe(true);
    });

    it('forbids site Administrator when a revoked membership belongs to another site only', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [{ ...nonRevoked(SITE_B), status: 'revoked' }, nonRevoked(SITE_B)],
      );
      expectDenied(decision, 'SITE_ACCESS_DENIED');
    });

    it('forbids a membership-less deleted subject to a site Administrator (no vacuous custody)', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'deleted', isSuperAdmin: false },
        [],
      );
      expectDenied(decision, 'SITE_ACCESS_DENIED');
    });

    it('forbids an Administrador without an active site even for a revoked row', () => {
      const decision = policy.canViewUser(
        identity({ activeSiteId: null, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
        [{ ...nonRevoked(SITE_A), status: 'revoked' }],
      );
      expectDenied(decision, 'SITE_ACCESS_DENIED');
    });

    it('keeps revoked-row visibility from widening custody for global-field edits', () => {
      const actor = identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' });
      const subject = {
        id: SUBJECT_ID,
        accountStatus: 'active' as AccountStatus,
        isSuperAdmin: false,
      };
      expect(
        policy.canViewUser(actor, subject, [{ ...nonRevoked(SITE_A), status: 'revoked' }]).allowed,
      ).toBe(true);
      expectDenied(
        policy.canUpdateGlobalFields(actor, {
          subject,
          nonRevokedMemberships: [],
          now: new Date(),
        }),
        'CUSTODY_NOT_SINGLE_SITE',
      );
    });

    it('lets the site Administrador restore only the active-site membership', () => {
      const actor = identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' });
      const subject = { id: SUBJECT_ID, isSuperAdmin: false };
      expect(policy.canManageMembership(actor, subject, SITE_A, 'restore').allowed).toBe(true);
      expectDenied(
        policy.canManageMembership(actor, subject, SITE_B, 'restore'),
        'NOT_ACTIVE_SITE',
      );
      expectDenied(
        policy.canManageMembership(
          actor,
          { id: SUBJECT_ID, isSuperAdmin: true },
          SITE_A,
          'restore',
        ),
        'SUPERADMIN_PROTECTED',
      );
    });
  });

  describe('canCreateUser', () => {
    it('forbids SuperAdmin creation through HTTP (CLI owns the global role)', () => {
      const decision = policy.canCreateUser(identity({ isSuperAdmin: true }), {
        isSuperAdminRequested: true,
      });
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SUPERADMIN_FORBIDDEN_OVER_HTTP');
    });

    it('allows site Administrator to create a non-SuperAdmin in its site', () => {
      const decision = policy.canCreateUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { isSuperAdminRequested: false },
      );
      expect(decision.allowed).toBe(true);
    });

    it('forbids Supervisor', () => {
      const decision = policy.canCreateUser(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Supervisor' }),
        { isSuperAdminRequested: false },
      );
      expect(decision.allowed).toBe(false);
    });
  });

  describe('canUpdateGlobalFields + custody', () => {
    it('forbids site Admin from editing a multi-site subject', () => {
      const decision = policy.canUpdateGlobalFields(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        {
          subject: { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
          nonRevokedMemberships: [nonRevoked(SITE_A), nonRevoked(SITE_B)],
          now: new Date(),
        },
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'CUSTODY_NOT_SINGLE_SITE');
    });

    it('allows site Admin to edit a single-site subject', () => {
      const decision = policy.canUpdateGlobalFields(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        {
          subject: { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: false },
          nonRevokedMemberships: [nonRevoked(SITE_A)],
          now: new Date(),
        },
      );
      expect(decision.allowed).toBe(true);
    });

    it('forbids site Admin from editing a SuperAdmin subject', () => {
      const decision = policy.canUpdateGlobalFields(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        {
          subject: { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: true },
          nonRevokedMemberships: [nonRevoked(SITE_A)],
          now: new Date(),
        },
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SUPERADMIN_PROTECTED');
    });

    it('allows SuperAdmin to edit anyone', () => {
      const decision = policy.canUpdateGlobalFields(identity({ isSuperAdmin: true }), {
        subject: { id: SUBJECT_ID, accountStatus: 'active', isSuperAdmin: true },
        nonRevokedMemberships: [nonRevoked(SITE_A)],
        now: new Date(),
      });
      expect(decision.allowed).toBe(true);
    });
  });

  describe('self-protection', () => {
    it('forbids self inactivate (setAccountStatus)', () => {
      const decision = policy.canSetAccountStatus(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: ACTOR_ID, isSuperAdmin: false },
        {
          subject: { id: ACTOR_ID, accountStatus: 'active', isSuperAdmin: false },
          nonRevokedMemberships: [nonRevoked(SITE_A)],
          now: new Date(),
        },
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SELF_PROTECTION');
    });

    it('forbids self delete (canDeleteAccount)', () => {
      const decision = policy.canDeleteAccount(identity({ isSuperAdmin: true }), { id: ACTOR_ID });
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SELF_PROTECTION');
    });

    it('forbids Supervisor from changing own active-site role', () => {
      const decision = policy.canManageMembership(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Supervisor' }),
        { id: ACTOR_ID, isSuperAdmin: false },
        SITE_A,
        'changeRole',
      );
      expect(decision.allowed).toBe(false);
    });
  });

  describe('last-active-SuperAdmin', () => {
    it('refuses a revoke that would leave zero active SuperAdmins', () => {
      const decision = policy.canReduceActiveSuperAdminCount(identity({ isSuperAdmin: true }), {
        remainingActiveSuperAdmins: 0,
      });
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'LAST_SUPERADMIN');
    });

    it('allows a revoke that would leave at least one', () => {
      const decision = policy.canReduceActiveSuperAdminCount(identity({ isSuperAdmin: true }), {
        remainingActiveSuperAdmins: 1,
      });
      expect(decision.allowed).toBe(true);
    });

    it('refuses a non-SuperAdmin actor attempting any SuperAdmin count reduction', () => {
      const decision = policy.canReduceActiveSuperAdminCount(
        identity({ isSuperAdmin: false, activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { remainingActiveSuperAdmins: 1 },
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SUPERADMIN_REQUIRED');
    });
  });

  describe('canManageMembership', () => {
    it('forbids site Admin from touching a SuperAdmin target', () => {
      const decision = policy.canManageMembership(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, isSuperAdmin: true },
        SITE_A,
        'changeRole',
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SUPERADMIN_PROTECTED');
    });

    it('forbids site Admin from touching a site they do not administer', () => {
      const decision = policy.canManageMembership(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, isSuperAdmin: false },
        SITE_B,
        'changeRole',
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'NOT_ACTIVE_SITE');
    });

    it('forbids self revoke of own active site', () => {
      const decision = policy.canManageMembership(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: ACTOR_ID, isSuperAdmin: false },
        SITE_A,
        'revoke',
      );
      expect(decision.allowed).toBe(false);
      expectDenied(decision, 'SELF_PROTECTION');
    });

    it('allows SuperAdmin to revoke any site membership', () => {
      const decision = policy.canManageMembership(
        identity({ isSuperAdmin: true }),
        { id: SUBJECT_ID, isSuperAdmin: true },
        SITE_A,
        'revoke',
      );
      expect(decision.allowed).toBe(true);
    });

    it('forbids adding a new membership to a target outside actor site', () => {
      const decision = policy.canManageMembership(
        identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' }),
        { id: SUBJECT_ID, isSuperAdmin: false },
        SITE_B,
        'add',
      );
      expect(decision.allowed).toBe(false);
    });
  });

  describe('profile subset', () => {
    it('rejects identityCard from /profile writes', () => {
      const decision = policy.canEditOwnProfileSubset('identityCard');
      expect(decision.allowed).toBe(false);
    });
    it('allows email/names/phone from /profile writes', () => {
      expect(policy.canEditOwnProfileSubset('email').allowed).toBe(true);
      expect(policy.canEditOwnProfileSubset('names').allowed).toBe(true);
      expect(policy.canEditOwnProfileSubset('phone').allowed).toBe(true);
    });
  });
});
describe('UserAccessPolicy F3 review regressions', () => {
  const policy = new UserAccessPolicy();
  const siteAdmin = identity({ activeSiteId: SITE_A, activeSiteRole: 'Administrador' });
  const probe = (memberships: ReturnType<typeof nonRevoked>[], isSuperAdmin = false) => ({
    subject: { id: SUBJECT_ID, accountStatus: 'active' as AccountStatus, isSuperAdmin },
    nonRevokedMemberships: memberships,
    now: new Date(),
  });

  it.each([
    ['global fields', (p: ReturnType<typeof probe>) => policy.canUpdateGlobalFields(siteAdmin, p)],
    ['identity card', (p: ReturnType<typeof probe>) => policy.canUpdateIdentityCard(siteAdmin, p)],
    [
      'admin password reset',
      (p: ReturnType<typeof probe>) => policy.canAdminResetPassword(siteAdmin, p),
    ],
    [
      'account status',
      (p: ReturnType<typeof probe>) =>
        policy.canSetAccountStatus(siteAdmin, { id: SUBJECT_ID, isSuperAdmin: false }, p),
    ],
    [
      'inactive restore',
      (p: ReturnType<typeof probe>) =>
        policy.canRestoreAccount(siteAdmin, { id: SUBJECT_ID, isSuperAdmin: false }, p, 'inactive'),
    ],
  ])(
    'custody is never granted for a target without non-revoked memberships: %s',
    (_label, decide) => {
      expectDenied(decide(probe([])), 'CUSTODY_NOT_SINGLE_SITE');
      expectDenied(
        decide(probe([nonRevoked(SITE_A), nonRevoked(SITE_B)])),
        'CUSTODY_NOT_SINGLE_SITE',
      );
      expect(decide(probe([nonRevoked(SITE_A)])).allowed).toBe(true);
    },
  );

  it('never lets anyone change their own identity card or admin-reset their own password', () => {
    const self = {
      ...probe([nonRevoked(SITE_A)]),
      subject: { id: ACTOR_ID, accountStatus: 'active' as AccountStatus, isSuperAdmin: true },
    };
    const superAdmin = identity({ isSuperAdmin: true });
    expectDenied(policy.canUpdateIdentityCard(superAdmin, self), 'SELF_PROTECTION');
    expectDenied(policy.canAdminResetPassword(superAdmin, self), 'SELF_PROTECTION');
  });

  it('only a SuperAdmin restores a deleted account directly', () => {
    const p = probe([nonRevoked(SITE_A)]);
    expectDenied(
      policy.canRestoreAccount(siteAdmin, { id: SUBJECT_ID, isSuperAdmin: false }, p, 'deleted'),
      'SUPERADMIN_REQUIRED',
    );
    expect(
      policy.canRestoreAccount(
        identity({ isSuperAdmin: true }),
        { id: SUBJECT_ID, isSuperAdmin: false },
        p,
        'deleted',
      ).allowed,
    ).toBe(true);
  });

  it('only a SuperAdmin links an existing user to a site', () => {
    expectDenied(
      policy.canManageMembership(siteAdmin, { id: SUBJECT_ID, isSuperAdmin: false }, SITE_A, 'add'),
      'LINK_REQUIRES_SUPERADMIN',
    );
    expect(
      policy.canManageMembership(
        identity({ isSuperAdmin: true }),
        { id: SUBJECT_ID, isSuperAdmin: false },
        SITE_B,
        'add',
      ).allowed,
    ).toBe(true);
  });
});
