/**
 * Session issuance planning: pure logic for the F1 §17 initial-site decision
 * and the per-purpose expiry computation. Stateless so login, password change,
 * site selection and self-session renewal share one decision tree.
 */
import { SITE_SELECTION_ABSOLUTE_TTL_SECONDS } from '../auth.constants.js';
import type { IdentityMembership, IdentityUser, AuthSessionRecord } from '../auth.types.js';
import type { RequestIdentity, SessionPurpose } from '../../identity/identity.contracts.js';

export interface SessionIssuancePlan {
  readonly purpose: SessionPurpose;
  readonly activeSiteId: string | null;
  readonly idleExpiresAt: Date;
  readonly absoluteExpiresAt: Date;
}

export interface SessionTtls {
  readonly idleTtlSeconds: number;
  readonly absoluteTtlSeconds: number;
}

export interface SessionIssuanceInput extends SessionTtls {
  readonly identity: IdentityUser;
  readonly memberships: ReadonlyArray<IdentityMembership>;
  readonly requestedActiveSiteId: string | null;
  readonly now: Date;
  /**
   * True when a verified credential cannot be stored as bcrypt (a legacy
   * password longer than 72 UTF-8 bytes, F1 §7). Forces a password_change
   * session even though legacy states carry must_change_password=false.
   */
  readonly forcePasswordChange?: boolean;
}

function normalPlan(
  activeSiteId: string | null,
  ttls: SessionTtls,
  now: Date,
): SessionIssuancePlan {
  const absoluteExpiresAt = new Date(now.getTime() + ttls.absoluteTtlSeconds * 1000);
  const idle = new Date(now.getTime() + ttls.idleTtlSeconds * 1000);
  return {
    purpose: 'normal',
    activeSiteId,
    idleExpiresAt: idle < absoluteExpiresAt ? idle : absoluteExpiresAt,
    absoluteExpiresAt,
  };
}

/**
 * F1 §7/§17 decision tree:
 *  - must_change_password, or a verified legacy password over 72 bytes: a
 *    `password_change` session with no active site;
 *  - zero eligible memberships: denied (null) unless SuperAdmin, who gets a
 *    normal null-site session for allowlisted global routes;
 *  - a requested site is honored only when eligible, otherwise denied;
 *  - one eligible membership: selected automatically;
 *  - several and none requested: a `site_selection` session with a 15-minute
 *    absolute expiry and no idle extension (idle expiry equals absolute).
 */
export function resolveSessionIssuance(input: SessionIssuanceInput): SessionIssuancePlan | null {
  const { identity, memberships, requestedActiveSiteId, now } = input;
  if (identity.passwordScheme === 'reset_required') {
    return null;
  }
  if (identity.mustChangePassword || input.forcePasswordChange === true) {
    return { ...normalPlan(null, input, now), purpose: 'password_change' };
  }
  if (memberships.length === 0) {
    return identity.isSuperAdmin ? normalPlan(null, input, now) : null;
  }
  if (requestedActiveSiteId !== null) {
    return memberships.some((m) => m.siteId === requestedActiveSiteId)
      ? normalPlan(requestedActiveSiteId, input, now)
      : null;
  }
  if (memberships.length === 1) {
    return normalPlan(memberships[0]!.siteId, input, now);
  }
  const absoluteExpiresAt = new Date(now.getTime() + SITE_SELECTION_ABSOLUTE_TTL_SECONDS * 1000);
  return {
    purpose: 'site_selection',
    activeSiteId: null,
    idleExpiresAt: absoluteExpiresAt,
    absoluteExpiresAt,
  };
}

/**
 * Session issued after a committed self password change: from a
 * `password_change` session the initial-site decision runs again; from a
 * `normal` session the current active site is kept while still eligible (a
 * SuperAdmin null-site session stays global).
 */
export function resolvePostPasswordChangePlan(
  args: SessionTtls & {
    readonly identity: IdentityUser;
    readonly memberships: ReadonlyArray<IdentityMembership>;
    readonly previousSession: Pick<AuthSessionRecord, 'purpose' | 'activeSiteId'>;
    readonly now: Date;
  },
): SessionIssuancePlan | null {
  const { identity, memberships, previousSession, now } = args;
  if (previousSession.purpose === 'normal') {
    if (previousSession.activeSiteId === null && identity.isSuperAdmin) {
      return normalPlan(null, args, now);
    }
    if (
      previousSession.activeSiteId !== null &&
      memberships.some((m) => m.siteId === previousSession.activeSiteId)
    ) {
      return normalPlan(previousSession.activeSiteId, args, now);
    }
  }
  return resolveSessionIssuance({ ...args, requestedActiveSiteId: null });
}

/** RequestIdentity for a validated normal session (F3 identity seam). */
export function buildRequestIdentity(args: {
  readonly correlationId: string;
  readonly sessionId: string;
  readonly identity: Pick<IdentityUser, 'id' | 'isSuperAdmin'>;
  readonly activeSiteId: string | null;
  readonly memberships: ReadonlyArray<Pick<IdentityMembership, 'siteId' | 'role'>>;
}): RequestIdentity {
  const role =
    args.activeSiteId === null
      ? null
      : (args.memberships.find((m) => m.siteId === args.activeSiteId)?.role ?? null);
  return {
    correlationId: args.correlationId,
    sessionId: args.sessionId,
    userId: args.identity.id,
    isSuperAdmin: args.identity.isSuperAdmin,
    activeSiteId: args.activeSiteId,
    activeSiteRole: role,
  };
}
