/**
 * Error codes the API intentionally emits. `code` stays an open string so a new
 * server code never breaks decoding; clients switch on HTTP status first.
 *
 * - 400 `VALIDATION_ERROR` (with optional `fieldErrors`)
 * - 401 `INVALID_CREDENTIALS` (bad login, missing/expired/revoked session)
 * - 403 `ACCESS_DENIED` (RBAC), `SITE_ACCESS_DENIED` (no eligible active site or
 *   restricted session purpose), `CSRF_INVALID`, `ORIGIN_DENIED`
 * - 404 `NOT_FOUND`, 409 `CONFLICT`, 429 `RATE_LIMITED` (`Retry-After` header)
 * - 503 `SERVICE_UNAVAILABLE`; other framework rejections use `REQUEST_REJECTED`
 * - `INVALID_API_RESPONSE` is produced client-side for malformed responses.
 */
export const API_ERROR_CODES = {
  validation: 'VALIDATION_ERROR',
  invalidCredentials: 'INVALID_CREDENTIALS',
  accessDenied: 'ACCESS_DENIED',
  siteAccessDenied: 'SITE_ACCESS_DENIED',
  csrfInvalid: 'CSRF_INVALID',
  originDenied: 'ORIGIN_DENIED',
  notFound: 'NOT_FOUND',
  conflict: 'CONFLICT',
  rateLimited: 'RATE_LIMITED',
  serviceUnavailable: 'SERVICE_UNAVAILABLE',
  requestRejected: 'REQUEST_REJECTED',
  invalidApiResponse: 'INVALID_API_RESPONSE',
} as const;

export type KnownApiErrorCode = (typeof API_ERROR_CODES)[keyof typeof API_ERROR_CODES];

export interface ApiError {
  readonly code: string;
  readonly message: string;
  readonly correlationId?: string;
  /**
   * Field -> messages. Password policy failures list violation identifiers
   * (`newPassword` on `POST /auth/password`) or readable rules (`password` on
   * user creation/admin reset); profile photos list a violation under `photo`.
   */
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>;
}

export interface ApiSuccess<T> {
  readonly success: true;
  readonly data: T;
}

export interface ApiFailure {
  readonly success: false;
  readonly error: ApiError;
}

export type ApiResult<T> = ApiSuccess<T> | ApiFailure;
