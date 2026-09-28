import type { AuthErrorCode } from './auth.constants.js';

export class AuthException extends Error {
  constructor(
    public readonly statusCode: number,
    public readonly code: AuthErrorCode,
    message: string,
  ) {
    super(message);
    this.name = 'AuthException';
  }
}

export class AuthValidationException extends AuthException {
  constructor(
    message: string,
    public readonly fieldErrors?: Readonly<Record<string, readonly string[]>>,
  ) {
    super(400, 'VALIDATION_ERROR', message);
  }
}

export class AuthUnauthorizedException extends AuthException {
  constructor() {
    super(401, 'INVALID_CREDENTIALS', 'Invalid credentials.');
  }
}

export class AuthForbiddenException extends AuthException {
  constructor(code: 'ORIGIN_DENIED' | 'CSRF_INVALID' | 'SITE_ACCESS_DENIED') {
    super(
      403,
      code,
      code === 'ORIGIN_DENIED'
        ? 'Origin not allowed.'
        : code === 'CSRF_INVALID'
          ? 'Invalid or missing CSRF token.'
          : 'Site access denied.',
    );
  }
}

export class AuthRateLimitException extends AuthException {
  constructor(public readonly retryAfterSeconds: number) {
    super(429, 'RATE_LIMITED', 'Too many login attempts. Please try again later.');
  }
}

export class AuthServiceUnavailableException extends AuthException {
  constructor() {
    super(503, 'SERVICE_UNAVAILABLE', 'Authentication service is temporarily unavailable.');
  }
}

/** Authenticated caller is not permitted to perform the operation (403). */
export class AuthAccessDeniedException extends AuthException {
  constructor(message = 'Operation not permitted.') {
    super(403, 'ACCESS_DENIED', message);
  }
}

export class AuthNotFoundException extends AuthException {
  constructor(message = 'Resource not found.') {
    super(404, 'NOT_FOUND', message);
  }
}

/** Uniqueness/state conflict that reveals no other account data (409). */
export class AuthConflictException extends AuthException {
  constructor(message = 'A conflicting record already exists.') {
    super(409, 'CONFLICT', message);
  }
}
