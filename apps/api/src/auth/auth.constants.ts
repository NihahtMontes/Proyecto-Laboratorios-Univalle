export const AUTH_CONFIG = Symbol('AUTH_CONFIG');
export const AUTH_PG_POOL = Symbol('AUTH_PG_POOL');

export const SESSION_COOKIE = '__Host-lu_session' as const;
export const CSRF_COOKIE = '__Host-lu_csrf' as const;
export const CSRF_HEADER = 'x-csrf-token' as const;

export const SKIP_CSRF_KEY = 'auth:skipCsrf' as const;

export const DEFAULT_BCRYPT_COST = 12;

export const DEFAULT_DUMMY_HASH = '$2b$12$cwX8Zvuf1RsO.CYGKnnT5OiRQ/sGS6ptomphoUa2I1ReqXiiqGJ6i';
export const DEFAULT_IDLE_TTL_SECONDS = 30 * 60;
export const DEFAULT_ABSOLUTE_TTL_SECONDS = 12 * 60 * 60;
/**
 * Site-selection sessions never idle-extend and have a 15-minute absolute cap
 * (F1 section 17 "Active-site session rules"). The session-handler tests rely
 * on the exact value.
 */
export const SITE_SELECTION_ABSOLUTE_TTL_SECONDS = 15 * 60;
export const DEFAULT_LOGIN_FLOOR_MS = 200;
export const DEFAULT_RATE_LIMIT_MAX_ATTEMPTS = 10;
export const DEFAULT_RATE_LIMIT_WINDOW_SECONDS = 60;
export const DEFAULT_CSRF_MAX_AGE_SECONDS = 600;

/**
 * F1 section 7: bcrypt accepts only up to 72 UTF-8 bytes; the transport-level
 * login bound is 512 UTF-8 bytes so a legacy Identity payload may still reach
 * the verifier. Concurrently implemented kernel constants are re-exported
 * through identity.contracts.ts (`LOGIN_PASSWORD_MAX_BYTES`,
 * `BCRYPT_PASSWORD_MAX_BYTES`).
 */
export {
  LOGIN_PASSWORD_MAX_BYTES,
  BCRYPT_PASSWORD_MAX_BYTES,
  SELF_SESSION_RENEWER,
  PASSWORD_VERIFICATION_SERVICE,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  LEGACY_PASSWORD_WINDOW,
  SESSION_INVALIDATOR,
  IDENTITY_AUDIT_WRITER,
} from '../identity/identity.contracts.js';

export const AuthErrorCode = {
  VALIDATION_ERROR: 'VALIDATION_ERROR',
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  ORIGIN_DENIED: 'ORIGIN_DENIED',
  CSRF_INVALID: 'CSRF_INVALID',
  SITE_ACCESS_DENIED: 'SITE_ACCESS_DENIED',
  RATE_LIMITED: 'RATE_LIMITED',
  SERVICE_UNAVAILABLE: 'SERVICE_UNAVAILABLE',
  ACCESS_DENIED: 'ACCESS_DENIED',
  NOT_FOUND: 'NOT_FOUND',
  CONFLICT: 'CONFLICT',
} as const;

export type AuthErrorCode = (typeof AuthErrorCode)[keyof typeof AuthErrorCode];
