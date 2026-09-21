import { createHash, createHmac, randomBytes, timingSafeEqual } from 'node:crypto';

export function generateSessionToken(): string {
  return randomBytes(32).toString('base64url');
}

export function hashSessionToken(token: string): string {
  return createHash('sha256').update(token).digest('hex').toLowerCase();
}

export function hashAuthIdentifier(secret: string, purpose: string, value: string): string {
  return createHmac('sha256', secret)
    .update(`lu-auth:${purpose}:`, 'utf8')
    .update(value, 'utf8')
    .digest('hex')
    .toLowerCase();
}

export function generateCsrfToken(): string {
  return randomBytes(32).toString('base64url');
}

export function constantTimeStringEquals(a: string, b: string): boolean {
  const aBuf = Buffer.from(a, 'utf8');
  const bBuf = Buffer.from(b, 'utf8');
  if (aBuf.length !== bBuf.length) {
    return false;
  }
  return timingSafeEqual(aBuf, bBuf);
}

export interface CookieOptions {
  readonly path?: string;
  readonly httpOnly?: boolean;
  readonly secure?: boolean;
  readonly sameSite?: 'Strict' | 'Lax' | 'None';
  readonly maxAge?: number;
}

export function serializeCookie(name: string, value: string, options: CookieOptions): string {
  const parts: string[] = [`${encodeURIComponent(name)}=${encodeURIComponent(value)}`];
  if (options.path !== undefined) {
    parts.push(`Path=${options.path}`);
  }
  if (options.httpOnly) {
    parts.push('HttpOnly');
  }
  if (options.secure) {
    parts.push('Secure');
  }
  if (options.sameSite !== undefined) {
    parts.push(`SameSite=${options.sameSite}`);
  }
  if (options.maxAge !== undefined) {
    parts.push(`Max-Age=${options.maxAge}`);
  }
  return parts.join('; ');
}

export function clearCookie(name: string, options: Omit<CookieOptions, 'maxAge'>): string {
  return serializeCookie(name, '', {
    ...options,
    maxAge: 0,
  });
}
