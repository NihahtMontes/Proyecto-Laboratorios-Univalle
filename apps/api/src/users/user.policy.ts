/**
 * Pure authorization policy for user administration (MIG-001 F3).
 *
 * Implements sections 10 and 17 of docs/migration/users/01-IDENTITY-CONTRACT.md
 * on top of the canonical RequestIdentity seam. This class NEVER queries SQL.
 * Every check is data-only and unit-testable.
 *
 * Custodial checks (single-site custodian) are pure logic that the service
 * re-verifies transactionally: the policy returns the *intent* and the service
 * re-locks + counts non-revoked memberships to confirm.
 */
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus,
  RequestIdentity,
} from '../identity/identity.contracts.js';

export interface PolicySubjectSummary {
  readonly id: string;
  readonly accountStatus: AccountStatus;
  readonly isSuperAdmin: boolean;
}

export interface PolicyMembershipSummary {
  readonly siteId: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
}

export type PolicyDecision =
  { readonly allowed: true } | { readonly allowed: false; readonly reason: string };

const ALLOW: PolicyDecision = { allowed: true };

function deny(reason: string): PolicyDecision {
  return { allowed: false, reason };
}

/**
 * Maps the effective ASP display status per section 11. Kept here so the policy
 * can answer authorization questions that depend on the effective state (e.g.
 * which memberships are eligible) without touching the database.
 */
export function effectiveMembershipStatus(
  subject: PolicySubjectSummary,
  membership: PolicyMembershipSummary,
  now: Date,
  validFrom: Date | null,
  validUntil: Date | null,
): EffectiveMembershipStatus {
  if (membership.status === 'revoked') return 'Eliminado';
  if (subject.accountStatus === 'deleted') return 'Eliminado';
  if (subject.accountStatus === 'inactive') return 'Inactivo';
  if (membership.status === 'suspended') return 'Inactivo';
  if (validFrom !== null && validFrom > now) return 'Inactivo';
  if (validUntil !== null && validUntil <= now) return 'Inactivo';
  return 'Activo';
}

export type EffectiveMembershipStatus = 'Activo' | 'Inactivo' | 'Eliminado';

function isEligibleMembership(
  membership: PolicyMembershipSummary,
  validFrom: Date | null,
  validUntil: Date | null,
  now: Date,
): boolean {
  if (membership.status !== 'active') return false;
  if (validFrom !== null && validFrom > now) return false;
  if (validUntil !== null && validUntil <= now) return false;
  return true;
}

/**
 * The service calls this AFTER it has locked the target user and counted the
 * non-revoked memberships inside a transaction. The policy turns the live
 * counts into an authorization decision.
 */
export interface CustodyProbe {
  readonly subject: PolicySubjectSummary;
  readonly nonRevokedMemberships: readonly PolicyMembershipSummary[];
  readonly now: Date;
}

export class UserAccessPolicy {
  /**
   * Section 17 default list scope:
   *  - SuperAdmin without active site sees global users.
   *  - site Administrator sees its active-site membership projection only.
   *  - SuperAdmin WITH active site can still query the global view.
   *  - Supervisor is forbidden from administering users.
   */
  canListUsers(actor: RequestIdentity): PolicyDecision {
    if (actor.activeSiteRole === 'Supervisor') {
      return deny('SITE_ACCESS_DENIED');
    }
    return ALLOW;
  }

  /**
   * Section 17 DETAIL: the target must belong to the actor's active site.
   * Section 11: the active-site history (revoked memberships, including an
   * account deleted by that site's revoke cascade) is visible in Details so the
   * Administrator can restore it. Only a membership row IN the active site
   * grants visibility: rows of other sites never do, and a membership-less
   * subject is never visible (no vacuous custody). The service projects only
   * the active-site membership; every command keeps its own authorization.
   * SuperAdmin may operate globally without an active site.
   */
  canViewUser(
    actor: RequestIdentity,
    _subject: PolicySubjectSummary,
    subjectMemberships: readonly PolicyMembershipSummary[],
  ): PolicyDecision {
    if (actor.isSuperAdmin) return ALLOW;
    if (actor.activeSiteRole !== 'Administrador') return deny('SITE_ACCESS_DENIED');
    if (actor.activeSiteId === null) return deny('SITE_ACCESS_DENIED');
    const inActorSite = subjectMemberships.some((m) => m.siteId === actor.activeSiteId);
    if (!inActorSite) return deny('SITE_ACCESS_DENIED');
    return ALLOW;
  }

  /**
   * Section 9 + section 17: site Admin may never create a SuperAdmin. The CLI
   * owns global SuperAdmin lifecycle; HTTP create must reject the payload.
   */
  canCreateUser(
    actor: RequestIdentity,
    input: { readonly isSuperAdminRequested: boolean },
  ): PolicyDecision {
    if (input.isSuperAdminRequested) return deny('SUPERADMIN_FORBIDDEN_OVER_HTTP');
    if (actor.activeSiteRole === 'Supervisor') return deny('SITE_ACCESS_DENIED');
    if (actor.isSuperAdmin) return ALLOW;
    if (actor.activeSiteRole === 'Administrador' && actor.activeSiteId !== null) {
      return ALLOW;
    }
    return deny('SITE_ACCESS_DENIED');
  }

  /**
   * Global fields update (email, names, phone, identity card, photo).
   * Section 10: SuperAdmin yes; site Admin only as single-site custodian
   * (target's only non-revoked membership is the actor's active site) and the
   * target must not be SuperAdmin.
   */
  canUpdateGlobalFields(actor: RequestIdentity, custody: CustodyProbe): PolicyDecision {
    if (custody.subject.isSuperAdmin) {
      if (!actor.isSuperAdmin) return deny('SUPERADMIN_PROTECTED');
      return ALLOW;
    }
    if (actor.isSuperAdmin) return ALLOW;
    if (actor.activeSiteRole !== 'Administrador') return deny('SITE_ACCESS_DENIED');
    if (actor.activeSiteId === null) return deny('SITE_ACCESS_DENIED');
    if (!this.isSingleSiteCustodian(actor, custody)) return deny('CUSTODY_NOT_SINGLE_SITE');
    return ALLOW;
  }

  /**
   * Identity card is the same authorization rule as global fields (section 10).
   * Privileged only.
   */
  canUpdateIdentityCard(actor: RequestIdentity, custody: CustodyProbe): PolicyDecision {
    // F1 §9/§10: the identity card is never self-editable, whatever the role.
    if (actor.userId === custody.subject.id) return deny('SELF_PROTECTION');
    return this.canUpdateGlobalFields(actor, custody);
  }

  /**
   * Admin password reset (section 10): SuperAdmin anywhere; site Admin only as
   * single-site custodian, target not SuperAdmin.
   */
  canAdminResetPassword(actor: RequestIdentity, custody: CustodyProbe): PolicyDecision {
    // Self uses the verified self-change flow (POST /auth/password), never a reset.
    if (actor.userId === custody.subject.id) return deny('SELF_PROTECTION');
    return this.canUpdateGlobalFields(actor, custody);
  }

  /**
   * Account status change (active/inactive; never 'deleted' here — see
   * `canDeleteAccount`). Self-protection rule: no self inactivate.
   */
  canSetAccountStatus(
    actor: RequestIdentity,
    subject: Pick<PolicySubjectSummary, 'id' | 'isSuperAdmin'>,
    custody: CustodyProbe,
  ): PolicyDecision {
    if (actor.userId === subject.id) return deny('SELF_PROTECTION');
    if (custody.subject.isSuperAdmin) {
      if (!actor.isSuperAdmin) return deny('SUPERADMIN_PROTECTED');
      return ALLOW;
    }
    if (actor.isSuperAdmin) return ALLOW;
    if (actor.activeSiteRole !== 'Administrador') return deny('SITE_ACCESS_DENIED');
    if (actor.activeSiteId === null) return deny('SITE_ACCESS_DENIED');
    if (!this.isSingleSiteCustodian(actor, custody)) return deny('CUSTODY_NOT_SINGLE_SITE');
    return ALLOW;
  }

  /**
   * RestoreAccount: same matrix as setAccountStatus + only 'inactive' or
   * 'deleted' sources. Self-restoration forbidden.
   */
  canRestoreAccount(
    actor: RequestIdentity,
    subject: Pick<PolicySubjectSummary, 'id' | 'isSuperAdmin'>,
    custody: CustodyProbe,
    fromStatus: AccountStatus,
  ): PolicyDecision {
    if (fromStatus === 'active') return deny('NOT_RESTORABLE');
    if (actor.userId === subject.id) return deny('SELF_PROTECTION');
    if (custody.subject.isSuperAdmin) {
      if (!actor.isSuperAdmin) return deny('SUPERADMIN_PROTECTED');
      return ALLOW;
    }
    if (actor.isSuperAdmin) return ALLOW;
    // F1 §11: a deleted account is restored by a site Administrator only by
    // reversing its own site-revoke cascade (restoreMembership), never here.
    if (fromStatus === 'deleted') return deny('SUPERADMIN_REQUIRED');
    if (actor.activeSiteRole !== 'Administrador') return deny('SITE_ACCESS_DENIED');
    if (actor.activeSiteId === null) return deny('SITE_ACCESS_DENIED');
    if (!this.isSingleSiteCustodian(actor, custody)) return deny('CUSTODY_NOT_SINGLE_SITE');
    return ALLOW;
  }

  /**
   * Global delete: SuperAdmin only. Never self-delete, never delete a target
   * who is the last active SuperAdmin (enforced separately by the service
   * using a serialized advisory lock).
   */
  canDeleteAccount(
    actor: RequestIdentity,
    subject: Pick<PolicySubjectSummary, 'id'>,
  ): PolicyDecision {
    if (!actor.isSuperAdmin) return deny('SUPERADMIN_REQUIRED');
    if (actor.userId === subject.id) return deny('SELF_PROTECTION');
    return ALLOW;
  }

  /**
   * Site membership commands — section 17 + section 9 self-protection.
   *   - addMembership, changeRole, changeStatus, changeValidity, updateWorkProfile,
   *     revokeActiveSite, restoreMembership.
   *   - Supervisor: forbidden.
   *   - Site Admin: active-site membership only; never on a SuperAdmin target.
   *   - SuperAdmin: any explicit site membership; never on own active site
   *     (self-protection covers changeRole/changeStatus/revoke for the active
   *     site; SuperAdmin may still add/change validity of non-active sites).
   */
  canManageMembership(
    actor: RequestIdentity,
    subject: Pick<PolicySubjectSummary, 'id' | 'isSuperAdmin'>,
    siteId: string,
    kind:
      | 'add'
      | 'changeRole'
      | 'changeStatus'
      | 'changeValidity'
      | 'updateWorkProfile'
      | 'revoke'
      | 'restore',
  ): PolicyDecision {
    if (actor.activeSiteRole === 'Supervisor') return deny('SITE_ACCESS_DENIED');
    if (subject.isSuperAdmin && !actor.isSuperAdmin) return deny('SUPERADMIN_PROTECTED');
    // F1 §17 LINK EXISTING USER: a site Administrator has no discovery beyond a
    // conflict and must escalate; only a SuperAdmin attaches memberships to an
    // existing identity (site Administrators create memberships via create).
    if (kind === 'add' && !actor.isSuperAdmin) return deny('LINK_REQUIRES_SUPERADMIN');

    const isActiveSite = actor.activeSiteId === siteId;
    const selfMembershipProtection =
      actor.userId === subject.id &&
      isActiveSite &&
      (kind === 'changeRole' || kind === 'changeStatus' || kind === 'revoke');

    if (selfMembershipProtection) return deny('SELF_PROTECTION');

    if (actor.isSuperAdmin) return ALLOW;

    if (actor.activeSiteRole !== 'Administrador') return deny('SITE_ACCESS_DENIED');
    if (!isActiveSite) return deny('NOT_ACTIVE_SITE');
    return ALLOW;
  }

  /**
   * Site Admin profile subset (section 16): /profile requires no active site.
   * A SuperAdmin may use it without an active site.
   */
  canAccessProfile(actor: RequestIdentity): PolicyDecision {
    void actor;
    return ALLOW;
  }

  /**
   * Self profile edit (section 9): the permitted subset is email, names, phone,
   * photo, password. Identity card, role, statuses, audit fields are not
   * permitted via /profile.
   */
  canEditOwnProfileSubset(field: 'email' | 'names' | 'phone' | 'identityCard'): PolicyDecision {
    if (field === 'identityCard') return deny('FIELD_NOT_PROFILE_WRITABLE');
    return ALLOW;
  }

  /**
   * Last-active-SuperAdmin invariant (section 8). The service calls this AFTER
   * it has locked the SuperAdmin rows and recounted; the policy only confirms
   * the would-be state.
   */
  canReduceActiveSuperAdminCount(
    actor: RequestIdentity,
    probe: { readonly remainingActiveSuperAdmins: number },
  ): PolicyDecision {
    if (!actor.isSuperAdmin) return deny('SUPERADMIN_REQUIRED');
    if (probe.remainingActiveSuperAdmins <= 0) return deny('LAST_SUPERADMIN');
    return ALLOW;
  }

  /**
   * Single-site custodian helper used by the service. It classifies the
   * subject's live non-revoked memberships against the actor's active site.
   * Returns true iff every non-revoked membership is in the actor's active site
   * AND there is at least one such membership.
   */
  isSingleSiteCustodian(actor: RequestIdentity, probe: CustodyProbe): boolean {
    if (actor.activeSiteId === null) return false;
    const nonRevoked = probe.nonRevokedMemberships;
    if (nonRevoked.length === 0) return false;
    return nonRevoked.every((m) => m.siteId === actor.activeSiteId);
  }

  /**
   * Eligible (for active-site session) membership check used by the service.
   * Mirrors auth.repository.findEligibleMemberships semantics so the policy
   * can answer "is this membership in scope?" without DB access.
   */
  static isMembershipEligible(
    membership: PolicyMembershipSummary,
    validFrom: Date | null,
    validUntil: Date | null,
    now: Date,
  ): boolean {
    return isEligibleMembership(membership, validFrom, validUntil, now);
  }
}
