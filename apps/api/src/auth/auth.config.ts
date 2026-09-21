import bcrypt from 'bcryptjs';
import { DEFAULT_BCRYPT_COST, DEFAULT_DUMMY_HASH } from './auth.constants.js';

export interface RawAuthConfig {
  readonly allowedOrigins: readonly string[] | null;
  readonly idleTtlSeconds: number;
  readonly absoluteTtlSeconds: number;
  readonly loginFloorMs: number;
  readonly rateLimitMaxAttempts: number;
  readonly rateLimitIpMaxAttempts: number;
  readonly rateLimitWindowSeconds: number;
  readonly auditHmacKey: string;
  readonly bcryptCost: number;
  readonly dummyHash: string;
  readonly csrfMaxAgeSeconds: number;
}

export class AuthConfigError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'AuthConfigError';
  }
}

const BCRYPT_HASH_REGEX = /^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$/;

function isValidBcryptHash(value: string): boolean {
  return BCRYPT_HASH_REGEX.test(value);
}

function parseOptionalInt(
  env: Record<string, string | undefined>,
  key: string,
  defaultValue: number,
): number {
  const raw = env[key];
  if (raw === undefined || raw.trim() === '') {
    return defaultValue;
  }
  const trimmed = raw.trim();
  if (!/^-?\d+$/.test(trimmed)) {
    throw new AuthConfigError(`Environment variable "${key}" must be an integer.`);
  }
  const value = Number(trimmed);
  if (!Number.isSafeInteger(value)) {
    throw new AuthConfigError(`Environment variable "${key}" is out of safe integer range.`);
  }
  return value;
}

function parseAllowedOrigins(env: Record<string, string | undefined>): readonly string[] | null {
  const raw = env['AUTH_ALLOWED_ORIGINS'];
  if (raw === undefined || raw.trim() === '') {
    return null;
  }
  const origins = raw
    .split(',')
    .map((o) => o.trim())
    .filter((o) => o !== '');
  return origins.length === 0 ? null : Object.freeze(origins);
}

function parseBcryptCost(env: Record<string, string | undefined>, isTestSeam: boolean): number {
  const raw = env['AUTH_BCRYPT_COST'];
  if (raw === undefined || raw.trim() === '') {
    return DEFAULT_BCRYPT_COST;
  }
  const trimmed = raw.trim();
  if (!/^\d+$/.test(trimmed)) {
    throw new AuthConfigError('AUTH_BCRYPT_COST must be a positive integer.');
  }
  const value = Number(trimmed);
  if (!Number.isSafeInteger(value)) {
    throw new AuthConfigError('AUTH_BCRYPT_COST is out of safe integer range.');
  }
  if (isTestSeam) {
    if (value < 4 || value > 15) {
      throw new AuthConfigError('AUTH_BCRYPT_COST must be between 4 and 15 in test seam mode.');
    }
  } else {
    if (value < 10 || value > 15) {
      throw new AuthConfigError('AUTH_BCRYPT_COST must be between 10 and 15 in production.');
    }
  }
  return value;
}

function resolveDummyHash(env: Record<string, string | undefined>, cost: number): string {
  const raw = env['AUTH_BCRYPT_DUMMY_HASH']?.trim();
  if (raw !== undefined && raw !== '') {
    if (!isValidBcryptHash(raw)) {
      throw new AuthConfigError('AUTH_BCRYPT_DUMMY_HASH must be a valid bcrypt hash.');
    }
    if (bcrypt.getRounds(raw) !== cost) {
      throw new AuthConfigError('AUTH_BCRYPT_DUMMY_HASH cost must match AUTH_BCRYPT_COST.');
    }
    return raw;
  }
  if (cost === DEFAULT_BCRYPT_COST) {
    return DEFAULT_DUMMY_HASH;
  }
  return bcrypt.hashSync('dummy', cost);
}

export class AuthConfig implements RawAuthConfig {
  readonly allowedOrigins: readonly string[] | null;
  readonly idleTtlSeconds: number;
  readonly absoluteTtlSeconds: number;
  readonly loginFloorMs: number;
  readonly rateLimitMaxAttempts: number;
  readonly rateLimitIpMaxAttempts: number;
  readonly rateLimitWindowSeconds: number;
  readonly auditHmacKey: string;
  readonly bcryptCost: number;
  readonly dummyHash: string;
  readonly csrfMaxAgeSeconds: number;

  private validated = false;

  constructor(
    env: Record<string, string | undefined> = process.env,
    options?: { readonly testSeam?: boolean },
  ) {
    const isTestSeam = options?.testSeam === true;
    this.allowedOrigins = parseAllowedOrigins(env);
    this.idleTtlSeconds = parseOptionalInt(env, 'AUTH_SESSION_IDLE_TTL_SECONDS', 30 * 60);
    this.absoluteTtlSeconds = parseOptionalInt(
      env,
      'AUTH_SESSION_ABSOLUTE_TTL_SECONDS',
      12 * 60 * 60,
    );
    this.loginFloorMs = parseOptionalInt(env, 'AUTH_LOGIN_FLOOR_MS', 200);
    this.rateLimitMaxAttempts = parseOptionalInt(env, 'AUTH_RATE_LIMIT_MAX_ATTEMPTS', 10);
    this.rateLimitIpMaxAttempts = parseOptionalInt(env, 'AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS', 50);
    this.rateLimitWindowSeconds = parseOptionalInt(env, 'AUTH_RATE_LIMIT_WINDOW_SECONDS', 60);
    this.auditHmacKey = env['AUTH_AUDIT_HMAC_KEY']?.trim() ?? '';
    this.bcryptCost = parseBcryptCost(env, isTestSeam);
    this.dummyHash = resolveDummyHash(env, this.bcryptCost);
    this.csrfMaxAgeSeconds = parseOptionalInt(env, 'AUTH_CSRF_MAX_AGE_SECONDS', 600);
  }

  validate(): void {
    if (this.validated) {
      return;
    }
    if (this.idleTtlSeconds <= 0) {
      throw new AuthConfigError('AUTH_SESSION_IDLE_TTL_SECONDS must be > 0.');
    }
    if (this.absoluteTtlSeconds <= 0) {
      throw new AuthConfigError('AUTH_SESSION_ABSOLUTE_TTL_SECONDS must be > 0.');
    }
    if (this.idleTtlSeconds > this.absoluteTtlSeconds) {
      throw new AuthConfigError(
        'AUTH_SESSION_IDLE_TTL_SECONDS must be <= AUTH_SESSION_ABSOLUTE_TTL_SECONDS.',
      );
    }
    if (this.loginFloorMs < 0) {
      throw new AuthConfigError('AUTH_LOGIN_FLOOR_MS must be >= 0.');
    }
    if (this.rateLimitMaxAttempts <= 0) {
      throw new AuthConfigError('AUTH_RATE_LIMIT_MAX_ATTEMPTS must be > 0.');
    }
    if (this.rateLimitIpMaxAttempts < this.rateLimitMaxAttempts) {
      throw new AuthConfigError(
        'AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS must be >= AUTH_RATE_LIMIT_MAX_ATTEMPTS.',
      );
    }
    if (this.rateLimitWindowSeconds <= 0) {
      throw new AuthConfigError('AUTH_RATE_LIMIT_WINDOW_SECONDS must be > 0.');
    }
    if (this.auditHmacKey.length < 32) {
      throw new AuthConfigError('AUTH_AUDIT_HMAC_KEY must contain at least 32 characters.');
    }
    if (this.bcryptCost < 4 || this.bcryptCost > 15) {
      throw new AuthConfigError('AUTH_BCRYPT_COST must be between 4 and 15.');
    }
    if (!isValidBcryptHash(this.dummyHash)) {
      throw new AuthConfigError('AUTH_BCRYPT_DUMMY_HASH must be a valid bcrypt hash.');
    }
    if (bcrypt.getRounds(this.dummyHash) !== this.bcryptCost) {
      throw new AuthConfigError('AUTH_BCRYPT_DUMMY_HASH cost must match AUTH_BCRYPT_COST.');
    }
    if (this.csrfMaxAgeSeconds <= 0) {
      throw new AuthConfigError('AUTH_CSRF_MAX_AGE_SECONDS must be > 0.');
    }
    this.validated = true;
  }
}
