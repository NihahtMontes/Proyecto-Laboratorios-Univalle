import 'reflect-metadata';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import { FastifyAdapter } from '@nestjs/platform-fastify';
import { Test } from '@nestjs/testing';
import { AppModule } from '../src/app.module.js';
import {
  AUTH_CONFIG,
  AUTH_PG_POOL,
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  PASSWORD_VERIFICATION_SERVICE,
  SELF_SESSION_RENEWER,
  SESSION_INVALIDATOR,
} from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { AuthService } from '../src/auth/auth.service.js';
import {
  PasswordScheme,
  type IdentityAuditWriter,
  type LegacyPasswordWindow,
  type PasswordHasher,
  type PasswordPolicy,
  type PasswordVerificationResult,
  type SelfSessionRenewer,
  type SessionInvalidator,
} from '../src/identity/identity.contracts.js';
import { FakeAuthRepository, FakePgPool } from './auth.fakes.js';

const TRUSTED_ORIGIN = 'http://localhost:3000';
const UNTRUSTED_ORIGIN = 'http://evil.example';

function makeVerifier(knownHashes: Map<string, string>): {
  verify(
    scheme: PasswordScheme,
    hash: string | null,
    plaintext: string,
  ): Promise<PasswordVerificationResult>;
  dummyVerify(p: string): Promise<void>;
} {
  return {
    async verify(scheme, hash, plaintext) {
      if (scheme === 'reset_required' || hash === null) {
        return { verified: false, needsRehash: false };
      }
      try {
        const ok = await import('bcryptjs').then((m) => m.compare(plaintext, hash));
        return { verified: ok, needsRehash: false };
      } catch {
        return { verified: false, needsRehash: false };
      }
    },
    async dummyVerify(p) {
      const bcrypt = await import('bcryptjs');
      await bcrypt
        .compare(
          p,
          knownHashes.get('dummy') ??
            '$2b$10$cwX8Zvuf1RsO.CYGKnnT5OiRQ/sGS6ptomphoUa2I1ReqXiiqGJ6i',
        )
        .catch(() => undefined);
    },
  };
}

async function bootstrapApp(
  repository: FakeAuthRepository,
  verifier: ReturnType<typeof makeVerifier>,
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
  };
  const moduleRef = await Test.createTestingModule({
    imports: [AppModule],
  })
    .overrideProvider(AUTH_CONFIG)
    .useValue(new AuthConfig(env))
    .overrideProvider(AuthRepository)
    .useValue(repository)
    .overrideProvider(AUTH_PG_POOL)
    .useValue(new FakePgPool())
    .overrideProvider(PASSWORD_VERIFICATION_SERVICE)
    .useValue(verifier)
    .overrideProvider(PASSWORD_HASHER)
    .useValue({
      currentCost: 10,
      hash: async (p: string) => (await import('bcryptjs')).hash(p, 10),
    })
    .overrideProvider(PASSWORD_POLICY)
    .useValue({ validate: (): readonly string[] => [] })
    .overrideProvider(LEGACY_PASSWORD_WINDOW)
    .useValue({ isOpen: async () => true })
    .overrideProvider(IDENTITY_AUDIT_WRITER)
    .useValue({ append: async () => undefined })
    .overrideProvider(SESSION_INVALIDATOR)
    .useValue({
      rotateAndRevokeAll: async (_c: unknown, _userId: string): Promise<string> => {
        void _c;
        void _userId;
        return '1';
      },
    })
    .overrideProvider(SELF_SESSION_RENEWER)
    .useValue({ renew: async () => undefined })
    .compile();

  const app = moduleRef.createNestApplication<NestFastifyApplication>(new FastifyAdapter(), {
    logger: false,
  });
  app.setGlobalPrefix('api/v1');
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
  return app;
}

const _unused: PasswordPolicy = { validate: () => [] };
const _h: PasswordHasher = { currentCost: 10, hash: async () => '' };
const _l: LegacyPasswordWindow = { isOpen: async () => true };
const _a: IdentityAuditWriter = { append: async () => undefined };
const _s: SessionInvalidator = { rotateAndRevokeAll: async () => '1' };
const _r: SelfSessionRenewer = { renew: async () => undefined };
void _unused;
void _h;
void _l;
void _a;
void _s;
void _r;

void AuthService;

describe('AuthController (e2e)', () => {
  let app: NestFastifyApplication | null = null;
  let repository: FakeAuthRepository;
  const closeApp = async (): Promise<void> => {
    if (app !== null) {
      await app.close();
      app = null;
    }
  };

  function csrfHeaders(token: string): Record<string, string> {
    return {
      'x-csrf-token': token,
      cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
    };
  }

  async function getCsrf(): Promise<string> {
    const response = await app!.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
    const body = JSON.parse(response.payload) as { csrfToken: string };
    return body.csrfToken;
  }

  afterEach(async () => {
    await closeApp();
  });

  it('returns 204 for trusted-origin preflight', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const response = await app.inject({
      method: 'OPTIONS',
      url: '/api/v1/auth/login',
      headers: {
        origin: TRUSTED_ORIGIN,
        'access-control-request-method': 'POST',
        'access-control-request-headers': 'Content-Type, X-CSRF-Token',
      },
    });
    expect(response.statusCode).toBe(204);
  });

  it('rejects untrusted-origin preflight with 403', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const response = await app.inject({
      method: 'OPTIONS',
      url: '/api/v1/auth/login',
      headers: { origin: UNTRUSTED_ORIGIN, 'access-control-request-method': 'POST' },
    });
    expect(response.statusCode).toBe(403);
  });

  it('returns csrf cookie and csrf token via __Host-lu_csrf', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/auth/csrf',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(200);
    const setCookie = response.headers['set-cookie'];
    expect(setCookie).toBeDefined();
    const cookieValue = Array.isArray(setCookie) ? setCookie[0] : setCookie;
    expect(cookieValue).toMatch(/__Host-lu_csrf=/);
    expect(cookieValue).toContain('HttpOnly');
    expect(cookieValue).toContain('Secure');
    expect(cookieValue).toContain('SameSite=Lax');
    expect(cookieValue).toContain('Path=/');
  });

  it('returns uniform 401 for an unknown identifier', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const csrf = await getCsrf();
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { loginIdentifier: 'unknown@example.com', password: 'whatever' },
    });
    expect(response.statusCode).toBe(401);
  });

  it('returns 400 when the deprecated email alias is supplied (F8 removed the alias)', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const csrf = await getCsrf();
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: {
        loginIdentifier: 'user@example.com',
        email: 'user@example.com',
        password: 'whatever',
      },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 when only the deprecated email alias is supplied without loginIdentifier', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const csrf = await getCsrf();
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { email: 'user@example.com', password: 'whatever' },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 when privileged keys are supplied', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const csrf = await getCsrf();
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: {
        loginIdentifier: 'user@example.com',
        password: 'whatever',
        isSuperAdmin: true,
        globalRole: 'SuperAdmin',
      },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 when the transport-bound password exceeds 512 UTF-8 bytes', async () => {
    repository = new FakeAuthRepository();
    app = await bootstrapApp(repository, makeVerifier(new Map()));
    const csrf = await getCsrf();
    const longPassword = 'a'.repeat(513);
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { loginIdentifier: 'u@example.com', password: longPassword },
    });
    expect(response.statusCode).toBe(400);
  });
});
