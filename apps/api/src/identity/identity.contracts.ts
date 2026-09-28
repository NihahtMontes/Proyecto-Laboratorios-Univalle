/**
 * MIG-001 F3 identity kernel contracts.
 *
 * Stable seam between the auth flows (apps/api/src/auth), the user administration
 * slice (apps/api/src/users) and the identity kernel implementations
 * (apps/api/src/identity). Consumers depend on these interfaces through the DI
 * tokens below, never on concrete classes (DIP). Binding semantics come from
 * docs/migration/users/01-IDENTITY-CONTRACT.md; section numbers are cited inline.
 */
import type { IPgClient } from '../auth/auth.pg-pool.js';

// ---------------------------------------------------------------------------
// Canonical enumerations (F1 §1, §3; F2 CHECK constraints in control-plane 0006)
// ---------------------------------------------------------------------------

export type AccountStatus = 'active' | 'inactive' | 'deleted';
export type MembershipStatus = 'active' | 'suspended' | 'revoked';
export type MembershipRole = 'Administrador' | 'Supervisor';
export type PasswordScheme =
  'legacy_identity_v2' | 'legacy_identity_v3' | 'bcrypt' | 'reset_required';
export type SessionPurpose = 'normal' | 'password_change' | 'site_selection';

/** Schemes that carry a verifiable stored hash. `reset_required` never verifies. */
export type VerifiablePasswordScheme = Exclude<PasswordScheme, 'reset_required'>;

// ---------------------------------------------------------------------------
// Password verification — Strategy (F1 §7)
// ---------------------------------------------------------------------------

export interface PasswordVerificationResult {
  /** True only when the plaintext proves the stored hash. */
  readonly verified: boolean;
  /**
   * True when a verified credential must be re-hashed to current-cost bcrypt
   * before a session is issued (legacy Identity V2/V3, or bcrypt at an allowed
   * non-current cost). Always false when `verified` is false.
   */
  readonly needsRehash: boolean;
}

/**
 * One strategy per stored scheme. Implementations must:
 * - treat the plaintext as opaque bytes (UTF-8, no trim/normalization);
 * - never throw for malformed/unsupported stored hashes: return
 *   `{ verified: false, needsRehash: false }` (fail closed);
 * - compare derived material in constant time;
 * - never log plaintext, hashes, salts or derived keys.
 */
export interface PasswordVerifier {
  readonly scheme: VerifiablePasswordScheme;
  verify(plaintext: string, storedHash: string): Promise<PasswordVerificationResult>;
}

/** Multi-provider token: `PasswordVerifier[]`, one entry per verifiable scheme. */
export const PASSWORD_VERIFIERS = Symbol('PASSWORD_VERIFIERS');

/**
 * Dispatches to the strategy registered for a scheme (OCP: adding/removing the
 * temporary legacy strategies only changes provider registration). For a
 * missing strategy, `reset_required`, or a null hash it runs a dummy
 * verification of comparable cost and returns `verified: false`.
 */
export interface PasswordVerificationService {
  verify(
    scheme: PasswordScheme,
    storedHash: string | null,
    plaintext: string,
  ): Promise<PasswordVerificationResult>;
  /** Equalizes timing when no user/credential exists. Always resolves. */
  dummyVerify(plaintext: string): Promise<void>;
}
export const PASSWORD_VERIFICATION_SERVICE = Symbol('PASSWORD_VERIFICATION_SERVICE');

/** Produces current-cost bcrypt hashes; the only writer of new credentials. */
export interface PasswordHasher {
  readonly currentCost: number;
  hash(plaintext: string): Promise<string>;
}
export const PASSWORD_HASHER = Symbol('PASSWORD_HASHER');

// ---------------------------------------------------------------------------
// Password policy (F1 §7 "New and changed passwords")
// ---------------------------------------------------------------------------

export type PasswordPolicyViolation =
  | 'too_short' // < 12 Unicode code points
  | 'too_long' // > 72 UTF-8 bytes
  | 'missing_uppercase'
  | 'missing_lowercase'
  | 'missing_digit'
  | 'missing_symbol'
  | 'insufficient_distinct'; // < 4 distinct code points

/** Applies to create, admin reset/provisioning and self-change. Never to login. */
export interface PasswordPolicy {
  validate(plaintext: string): readonly PasswordPolicyViolation[];
}
export const PASSWORD_POLICY = Symbol('PASSWORD_POLICY');

/** Transport bounds (F1 §7): login accepts up to 512 UTF-8 bytes; bcrypt uses 72. */
export const LOGIN_PASSWORD_MAX_BYTES = 512;
export const BCRYPT_PASSWORD_MAX_BYTES = 72;

// ---------------------------------------------------------------------------
// Session invalidation matrix (F1 §12)
// ---------------------------------------------------------------------------

/** Every change that F1 §12 says rotates `security_version`. */
export type SecurityRotationReason =
  | 'email_change'
  | 'username_repair'
  | 'self_password_change'
  | 'admin_password_reset'
  | 'credential_rehash'
  | 'global_role_change'
  | 'site_role_change'
  | 'membership_change'
  | 'account_status_change'
  | 'access_revoked'
  | 'access_restored'
  | 'legacy_retirement';

/**
 * Must run inside the caller's control-plane transaction, after the subject row
 * has been locked (`SELECT ... FOR UPDATE`). Increments `lu_user.security_version`
 * exactly once and revokes every non-revoked `lu_session` of the subject.
 * Returns the new version as a decimal string (bigint-safe). Callers that must
 * "replace current" issue a new session only after commit.
 * Cosmetic changes (names, phone, CI, photo, work profile) must NOT call this.
 */
export interface SessionInvalidator {
  rotateAndRevokeAll(
    client: IPgClient,
    userId: string,
    reason: SecurityRotationReason,
  ): Promise<string>;
}
export const SESSION_INVALIDATOR = Symbol('SESSION_INVALIDATOR');

// ---------------------------------------------------------------------------
// Append-only identity audit (F1 §13; table lu_identity_audit_event)
// ---------------------------------------------------------------------------

export type IdentityAuditAction =
  | 'user_created'
  | 'email_changed'
  | 'profile_updated'
  | 'identity_card_changed'
  | 'password_changed'
  | 'password_reset'
  | 'credential_rehashed'
  | 'legacy_password_rejected'
  | 'account_status_changed'
  | 'account_deleted'
  | 'account_restored'
  | 'membership_created'
  | 'membership_role_changed'
  | 'membership_status_changed'
  | 'membership_validity_changed'
  | 'membership_revoked'
  | 'membership_restored'
  | 'superadmin_granted'
  | 'superadmin_revoked'
  | 'sessions_revoked'
  | 'active_site_changed';

export interface IdentityAuditEvent {
  readonly action: IdentityAuditAction;
  readonly subjectUserId: string | null;
  /** Null only for labeled system/bootstrap/import actions (F1 §13). */
  readonly actorUserId: string | null;
  readonly siteId?: string | null;
  readonly correlationId?: string | null;
  readonly reason?: string | null;
  readonly beforeStatus?: AccountStatus | null;
  readonly afterStatus?: AccountStatus | null;
  /** Non-secret scalars only. Never passwords, hashes, tokens or file bytes. */
  readonly metadata?: Readonly<Record<string, string | number | boolean | null>>;
}

export interface IdentityAuditWriter {
  append(client: IPgClient, event: IdentityAuditEvent): Promise<void>;
}
export const IDENTITY_AUDIT_WRITER = Symbol('IDENTITY_AUDIT_WRITER');

// ---------------------------------------------------------------------------
// Legacy password compatibility window (F1 §7; table lu_identity_migration_state)
// ---------------------------------------------------------------------------

export interface LegacyPasswordWindow {
  /**
   * False once a recorded `legacy_password_deadline` has been reached
   * (`now >= deadline`): login fails closed at the boundary even before the
   * retirement batch runs (F1 §7, 01-DATA-MAPPING). When no deadline is
   * recorded (missing row or null cutover) no deadline-based refusal applies
   * and legacy verification follows the frozen rehash contract; the deadline
   * is never inferred from deployment time. The cutover runbook records
   * `cutover_at`/deadline before opening traffic (01-DATA-MAPPING, D021).
   */
  isOpen(client: IPgClient, now: Date): Promise<boolean>;
}
export const LEGACY_PASSWORD_WINDOW = Symbol('LEGACY_PASSWORD_WINDOW');

// ---------------------------------------------------------------------------
// Authenticated request identity (set by guards in apps/api/src/core/core.guard.ts)
// ---------------------------------------------------------------------------

/**
 * Populated on `request.identity` by `GlobalSessionGuard` (normal session,
 * active site optional; for /profile and global Users routes) and by
 * `SiteContextGuard` (normal session with an eligible active site). Restricted
 * `password_change` / `site_selection` sessions never produce a RequestIdentity.
 */
export interface RequestIdentity {
  readonly correlationId: string;
  readonly sessionId: string;
  readonly userId: string;
  readonly isSuperAdmin: boolean;
  readonly activeSiteId: string | null;
  /** Role of the active-site membership; null when no active site. */
  readonly activeSiteRole: MembershipRole | null;
}

/**
 * Issues a replacement normal session after a committed self-sensitive change
 * (self email change, self password change) and writes the session + CSRF
 * cookies on the reply. Preserves the previous active site only if still
 * eligible. Implemented by the auth slice; consumed by the users slice.
 */
export interface SelfSessionRenewer {
  renew(
    reply: { header(name: string, value: string | number): unknown },
    identity: RequestIdentity,
  ): Promise<void>;
}
export const SELF_SESSION_RENEWER = Symbol('SELF_SESSION_RENEWER');
