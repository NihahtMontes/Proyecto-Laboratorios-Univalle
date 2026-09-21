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
export const DEFAULT_LOGIN_FLOOR_MS = 200;
export const DEFAULT_RATE_LIMIT_MAX_ATTEMPTS = 10;
export const DEFAULT_RATE_LIMIT_WINDOW_SECONDS = 60;
export const DEFAULT_CSRF_MAX_AGE_SECONDS = 600;

export const AuthErrorCode = {
  VALIDATION_ERROR: 'VALIDATION_ERROR',
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  ORIGIN_DENIED: 'ORIGIN_DENIED',
  CSRF_INVALID: 'CSRF_INVALID',
  SITE_ACCESS_DENIED: 'SITE_ACCESS_DENIED',
  RATE_LIMITED: 'RATE_LIMITED',
  SERVICE_UNAVAILABLE: 'SERVICE_UNAVAILABLE',
} as const;

export type AuthErrorCode = (typeof AuthErrorCode)[keyof typeof AuthErrorCode];
