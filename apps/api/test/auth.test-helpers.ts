import { FastifyAdapter, NestFastifyApplication } from '@nestjs/platform-fastify';
import { Test } from '@nestjs/testing';
import { AppModule } from '../src/app.module.js';
import { AUTH_CONFIG, AUTH_PG_POOL } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { hashSessionToken } from '../src/auth/auth.crypto.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import type { Session, Site, User } from '../src/auth/auth.types.js';
import { createSession, FakeAuthRepository, FakePgPool } from './auth.fakes.js';

export interface TestOverrides {
  config?: Partial<ConstructorParameters<typeof AuthConfig>[0]>;
  repository?: FakeAuthRepository;
  pool?: FakePgPool;
}

export async function createAuthTestApp(
  overrides: TestOverrides = {},
): Promise<NestFastifyApplication> {
  const env: Record<string, string | undefined> = {
    AUTH_ALLOWED_ORIGINS: 'http://localhost:3000',
    AUTH_SESSION_IDLE_TTL_SECONDS: '1800',
    AUTH_SESSION_ABSOLUTE_TTL_SECONDS: '43200',
    AUTH_LOGIN_FLOOR_MS: '0',
    AUTH_RATE_LIMIT_MAX_ATTEMPTS: '10',
    AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '50',
    AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
    AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
    AUTH_CSRF_MAX_AGE_SECONDS: '600',
    ...overrides.config,
  };

  const moduleRef = await Test.createTestingModule({
    imports: [AppModule],
  })
    .overrideProvider(AUTH_CONFIG)
    .useValue(new AuthConfig(env))
    .overrideProvider(AuthRepository)
    .useValue(overrides.repository ?? new FakeAuthRepository())
    .overrideProvider(AUTH_PG_POOL)
    .useValue(overrides.pool ?? new FakePgPool())
    .compile();

  const app = moduleRef.createNestApplication<NestFastifyApplication>(new FastifyAdapter(), {
    logger: false,
  });
  app.setGlobalPrefix('api/v1');
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
  return app;
}

export const TRUSTED_ORIGIN = 'http://localhost:3000';
export const UNTRUSTED_ORIGIN = 'http://evil.example';

export function parseCookies(header: string | string[] | undefined): Record<string, string> {
  const result: Record<string, string> = {};
  if (header === undefined) return result;
  const raw = Array.isArray(header) ? header.join('; ') : header;
  for (const segment of raw.split(';')) {
    const separatorIndex = segment.indexOf('=');
    if (separatorIndex < 0) continue;
    const name = segment.slice(0, separatorIndex).trim();
    const value = segment.slice(separatorIndex + 1).trim();
    if (name === '') continue;
    try {
      result[decodeURIComponent(name)] = decodeURIComponent(value);
    } catch {
      // ignore malformed
    }
  }
  return result;
}

export function expectSecurityHeaders(response: { headers: Record<string, unknown> }): void {
  const headers = response.headers;
  expect(headers['cache-control']).toBe('no-store');
  expect(headers['x-content-type-options']).toBe('nosniff');
  expect(headers['referrer-policy']).toBe('no-referrer');
  expect(headers['x-frame-options']).toBe('DENY');
}

export function expectCookieFlags(setCookie: string): void {
  expect(setCookie).toContain('HttpOnly');
  expect(setCookie).toContain('Secure');
  expect(setCookie).toContain('SameSite=Lax');
  expect(setCookie).toContain('Path=/');
  expect(setCookie).not.toMatch(/Domain=/i);
}

export function createAuthenticatedSession(
  repository: FakeAuthRepository,
  user: User,
  site?: Site,
): { rawToken: string; tokenHash: string; session: Session } {
  const rawToken = `raw-token-${user.id}`;
  const tokenHash = hashSessionToken(rawToken);
  const session = createSession(user, {
    tokenHash,
    activeSiteId: site?.id ?? null,
  });
  repository.sessions.set(tokenHash, session);
  return { rawToken, tokenHash, session };
}
