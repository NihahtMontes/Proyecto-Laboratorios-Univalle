import type { ActiveSiteSession, SiteId } from './site.js';
import type { ApiSuccess } from './api.js';
import { API_PREFIX } from './healthz.js';

export const AUTH_ROUTES = {
  csrf: `${API_PREFIX}/auth/csrf`,
  login: `${API_PREFIX}/auth/login`,
  session: `${API_PREFIX}/auth/session`,
  activeSite: `${API_PREFIX}/auth/session/active-site`,
  password: `${API_PREFIX}/auth/password`,
  logout: `${API_PREFIX}/auth/logout`,
} as const;

export interface CsrfResponse {
  readonly csrfToken: string;
}

/**
 * `POST /auth/login` body (F1-D004). `loginIdentifier` is a username OR an
 * email; the server normalizes it (NFKC, lowercase, trim) and resolves it with
 * one exact claim lookup. Unknown keys are rejected with 400.
 */
export interface LoginRequest {
  readonly loginIdentifier: string;
  /** Transport bound: at most {@link LOGIN_PASSWORD_MAX_BYTES} UTF-8 bytes. */
  readonly password: string;
  readonly rememberMe?: boolean;
  readonly activeSiteId?: SiteId | null;
}

/** Login transport bound; the new-password policy does not apply to login. */
export const LOGIN_PASSWORD_MAX_BYTES = 512;

/**
 * F1 session purposes. Route on `purpose`, not on `mustChangePassword`:
 * - `normal`: full session; business routes are available.
 * - `password_change`: only session inspection, `POST /auth/password` and
 *   logout. Issued for `must_change_password` accounts and for verified legacy
 *   passwords longer than 72 bytes (which cannot be rehashed).
 * - `site_selection`: several eligible sites; only session inspection,
 *   `PUT /auth/session/active-site` and logout. 15-minute absolute expiry with
 *   no idle extension.
 */
export type SessionPurpose = 'normal' | 'password_change' | 'site_selection';

export const SITE_SELECTION_SESSION_TTL_SECONDS = 15 * 60;

/** Site the user may activate. Listed only for `site_selection` and `normal`. */
export interface EligibleSite {
  readonly siteId: SiteId;
  readonly siteName: string;
  readonly role: 'Administrador' | 'Supervisor';
}

export interface AuthSessionMeta {
  readonly purpose: SessionPurpose;
  readonly mustChangePassword: boolean;
}

/**
 * Wire body shared by login, session, active-site and password change.
 * Restricted purposes carry `data.activeSiteId === null` and empty
 * `data.memberships`; `eligibleSites` is empty for `password_change`.
 * The session token only travels in the HttpOnly cookie, never in the body.
 */
export interface AuthSessionEnvelope extends ApiSuccess<ActiveSiteSession> {
  readonly meta: AuthSessionMeta;
  readonly eligibleSites: readonly EligibleSite[];
}

/** Decoded auth session state (mirrors the F3 backend `AuthSessionResponse`). */
export interface AuthSessionResponse {
  readonly session: ActiveSiteSession;
  readonly purpose: SessionPurpose;
  readonly mustChangePassword: boolean;
  readonly eligibleSites: readonly EligibleSite[];
}

export interface SetActiveSiteRequest {
  readonly activeSiteId: SiteId;
}

/**
 * `POST /auth/password` body. Success revokes every session of the account and
 * issues a replacement session cookie; the response is an auth session.
 */
export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
}

// ---------------------------------------------------------------------------
// Password policy (F1 §7 / F1-D006) shared by every new-password entry point:
// self change, user creation and admin reset. Mirrors the backend
// Utf8PasswordPolicy; the server remains authoritative.
// ---------------------------------------------------------------------------

export const PASSWORD_POLICY = {
  minCodePoints: 12,
  maxUtf8Bytes: 72,
  minDistinctCodePoints: 4,
} as const;

export type PasswordPolicyViolation =
  | 'too_short'
  | 'too_long'
  | 'missing_uppercase'
  | 'missing_lowercase'
  | 'missing_digit'
  | 'missing_symbol'
  | 'insufficient_distinct';

function utf8ByteLength(value: string): number {
  let bytes = 0;
  for (const char of value) {
    const codePoint = char.codePointAt(0) ?? 0;
    bytes += codePoint < 0x80 ? 1 : codePoint < 0x800 ? 2 : codePoint < 0x10000 ? 3 : 4;
  }
  return bytes;
}

/**
 * Returns every policy violation (empty when valid). No trim, normalization or
 * case folding: the policy is code-point faithful like the backend.
 */
export function passwordPolicyViolations(password: string): readonly PasswordPolicyViolation[] {
  const violations: PasswordPolicyViolation[] = [];
  const codePoints = Array.from(password);
  if (utf8ByteLength(password) > PASSWORD_POLICY.maxUtf8Bytes) violations.push('too_long');
  if (codePoints.length < PASSWORD_POLICY.minCodePoints) violations.push('too_short');
  if (new Set(codePoints).size < PASSWORD_POLICY.minDistinctCodePoints) {
    violations.push('insufficient_distinct');
  }
  if (!codePoints.some((char) => /\p{Lu}/u.test(char))) violations.push('missing_uppercase');
  if (!codePoints.some((char) => /\p{Ll}/u.test(char))) violations.push('missing_lowercase');
  if (!codePoints.some((char) => /\p{Nd}/u.test(char))) violations.push('missing_digit');
  if (!codePoints.some((char) => !/\p{L}/u.test(char) && !/\p{N}/u.test(char))) {
    violations.push('missing_symbol');
  }
  return violations;
}
