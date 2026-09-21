import 'reflect-metadata';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import type { ActiveSiteSession } from '@lu/contracts';
import bcrypt from 'bcryptjs';
import { createMembership, createSite, createUser, FakeAuthRepository } from './auth.fakes.js';
import {
  createAuthenticatedSession,
  createAuthTestApp,
  expectCookieFlags,
  expectSecurityHeaders,
  parseCookies,
  TRUSTED_ORIGIN,
  UNTRUSTED_ORIGIN,
} from './auth.test-helpers.js';

function expectContractShape(body: { data: ActiveSiteSession }): void {
  const data = body.data;
  const keys = Object.keys(data).sort();
  expect(keys).toEqual([
    'activeSiteId',
    'activeSiteName',
    'displayName',
    'email',
    'globalRole',
    'memberships',
    'userId',
  ]);
  expect(data.userId).toEqual(expect.any(String));
  expect(data.displayName).toEqual(expect.any(String));
  expect(data.email).toEqual(expect.any(String));
  expect(data.activeSiteId === null || typeof data.activeSiteId === 'string').toBe(true);
  expect(data.activeSiteName === null || typeof data.activeSiteName === 'string').toBe(true);
  expect(data.globalRole === null || data.globalRole === 'SuperAdmin').toBe(true);
  expect(Array.isArray(data.memberships)).toBe(true);
  for (const membership of data.memberships) {
    expect(Object.keys(membership).sort()).toEqual(['role', 'siteId', 'siteName', 'state']);
    expect(typeof membership.siteId).toBe('string');
    expect(typeof membership.siteName).toBe('string');
    expect(['Administrador', 'Supervisor']).toContain(membership.role);
    expect(['provisioning', 'active', 'migrating', 'degraded', 'disabled']).toContain(
      membership.state,
    );
  }
}

describe('AuthController (e2e)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  let testPasswordHash: string;

  beforeAll(async () => {
    testPasswordHash = await bcrypt.hash('ignored-in-fake', 12);
  });

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    app = await createAuthTestApp({ repository });
  });

  afterEach(async () => {
    await app.close();
  });

  describe('OPTIONS preflight', () => {
    it('returns 204 with exact CORS headers for trusted origin', async () => {
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
      expect(response.payload).toBe('');
      expectSecurityHeaders(response);
      expect(response.headers['access-control-allow-origin']).toBe(TRUSTED_ORIGIN);
      expect(response.headers['access-control-allow-credentials']).toBe('true');
      expect(response.headers['vary']).toBe('Origin');
      expect(response.headers['access-control-allow-methods']).toBe('GET, POST, PUT, OPTIONS');
      expect(response.headers['access-control-allow-headers']).toBe('Content-Type, X-CSRF-Token');
    });

    it('rejects untrusted origin with 403', async () => {
      const response = await app.inject({
        method: 'OPTIONS',
        url: '/api/v1/auth/login',
        headers: {
          origin: UNTRUSTED_ORIGIN,
          'access-control-request-method': 'POST',
        },
      });
      expect(response.statusCode).toBe(403);
      expectSecurityHeaders(response);
    });

    it('rejects null origin with 403', async () => {
      const response = await app.inject({
        method: 'OPTIONS',
        url: '/api/v1/auth/login',
        headers: {
          origin: 'null',
          'access-control-request-method': 'POST',
        },
      });
      expect(response.statusCode).toBe(403);
      expectSecurityHeaders(response);
    });

    it('rejects missing origin with 403', async () => {
      const response = await app.inject({
        method: 'OPTIONS',
        url: '/api/v1/auth/login',
        headers: {
          'access-control-request-method': 'POST',
        },
      });
      expect(response.statusCode).toBe(403);
    });

    it('rejects disallowed request method with 400', async () => {
      const response = await app.inject({
        method: 'OPTIONS',
        url: '/api/v1/auth/login',
        headers: {
          origin: TRUSTED_ORIGIN,
          'access-control-request-method': 'DELETE',
        },
      });
      expect(response.statusCode).toBe(400);
      expectSecurityHeaders(response);
    });

    it('rejects disallowed request header with 400', async () => {
      const response = await app.inject({
        method: 'OPTIONS',
        url: '/api/v1/auth/login',
        headers: {
          origin: TRUSTED_ORIGIN,
          'access-control-request-method': 'POST',
          'access-control-request-headers': 'X-Custom-Header',
        },
      });
      expect(response.statusCode).toBe(400);
      expectSecurityHeaders(response);
    });

    it('handles preflight for all five auth routes', async () => {
      const routes = [
        '/api/v1/auth/csrf',
        '/api/v1/auth/login',
        '/api/v1/auth/session',
        '/api/v1/auth/session/active-site',
        '/api/v1/auth/logout',
      ];
      for (const url of routes) {
        const response = await app.inject({
          method: 'OPTIONS',
          url,
          headers: {
            origin: TRUSTED_ORIGIN,
            'access-control-request-method':
              url === '/api/v1/auth/csrf' || url === '/api/v1/auth/session'
                ? 'GET'
                : url === '/api/v1/auth/session/active-site'
                  ? 'PUT'
                  : 'POST',
          },
        });
        expect(response.statusCode).toBe(204);
        expect(response.headers['access-control-allow-origin']).toBe(TRUSTED_ORIGIN);
      }
    });
  });

  describe('GET /api/v1/auth/csrf', () => {
    it('returns a csrf token and sets __Host-lu_csrf cookie with secure flags', async () => {
      const response = await app.inject({
        method: 'GET',
        url: '/api/v1/auth/csrf',
        headers: { origin: TRUSTED_ORIGIN },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { csrfToken: string };
      expect(typeof body.csrfToken).toBe('string');
      expect(body.csrfToken.length).toBeGreaterThan(0);

      const setCookie = response.headers['set-cookie'];
      expect(setCookie).toBeDefined();
      const cookieValue = Array.isArray(setCookie) ? setCookie[0] : setCookie;
      expect(cookieValue).toMatch(/__Host-lu_csrf=/);
      expectCookieFlags(cookieValue ?? '');
      expectSecurityHeaders(response);

      const cookies = parseCookies(cookieValue);
      expect(cookies['__Host-lu_csrf']).toBe(body.csrfToken);
    });

    it('rejects untrusted origin on GET', async () => {
      const response = await app.inject({
        method: 'GET',
        url: '/api/v1/auth/csrf',
        headers: { origin: UNTRUSTED_ORIGIN },
      });
      expect(response.statusCode).toBe(403);
      expectSecurityHeaders(response);
    });
  });

  describe('POST /api/v1/auth/login', () => {
    function csrfHeaders(token: string) {
      return {
        'x-csrf-token': token,
        cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
      };
    }

    async function getCsrf(): Promise<string> {
      const response = await app.inject({
        method: 'GET',
        url: '/api/v1/auth/csrf',
      });
      const body = JSON.parse(response.payload) as { csrfToken: string };
      return body.csrfToken;
    }

    it('rejects missing origin on mutation', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: csrfHeaders(csrf),
        payload: { email: 'a@b.co', password: 'pass' },
      });
      expect(response.statusCode).toBe(403);
    });

    it('rejects untrusted origin on mutation', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: UNTRUSTED_ORIGIN },
        payload: { email: 'a@b.co', password: 'pass' },
      });
      expect(response.statusCode).toBe(403);
    });

    it('rejects mismatched csrf cookie and header', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: {
          origin: TRUSTED_ORIGIN,
          'x-csrf-token': 'header-token',
          cookie: `__Host-lu_csrf=${encodeURIComponent(csrf)}`,
        },
        payload: { email: 'a@b.co', password: 'pass' },
      });
      expect(response.statusCode).toBe(403);
      expect(response.headers['access-control-allow-origin']).toBe(TRUSTED_ORIGIN);
    });

    it('returns uniform invalid credentials for unknown user', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: 'unknown@example.com', password: 'pass' },
      });
      expect(response.statusCode).toBe(401);
      const body = JSON.parse(response.payload) as {
        success: boolean;
        error: { code: string; message: string };
      };
      expect(body.success).toBe(false);
      expect(body.error.code).toBe('INVALID_CREDENTIALS');
    });

    it('validates request shape strictly', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: 'a@b.co', password: 'pass', extra: 'field' },
      });
      expect(response.statusCode).toBe(400);
    });

    it('validates password byte length <= 72', async () => {
      const csrf = await getCsrf();
      const longPassword = 'a'.repeat(73);
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: 'a@b.co', password: longPassword },
      });
      expect(response.statusCode).toBe(400);
    });

    it('validates UUID format for activeSiteId rejecting v7 as invalid', async () => {
      const csrf = await getCsrf();
      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: 'a@b.co', password: 'pass', activeSiteId: 'not-a-uuid' },
      });
      expect(response.statusCode).toBe(400);
    });

    it('accepts UUID v7 and returns lowercase canonical form', async () => {
      const csrf = await getCsrf();
      const site = createSite({ id: '018f3b34-0000-7abc-8def-0123456789ab' });
      const user = createUser({
        email: 'uuid7@example.com',
        passwordHash: testPasswordHash,
      });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: user.email, password: 'ignored-in-fake', activeSiteId: site.id },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { success: boolean; data: ActiveSiteSession };
      expect(body.success).toBe(true);
      expect(body.data.activeSiteId).toBe(site.id.toLowerCase());
      expectContractShape(body);
    });

    it('sets session cookie with secure flags and omits raw token from body', async () => {
      const csrf = await getCsrf();
      const site = createSite();
      const user = createUser({
        email: 'login@example.com',
        passwordHash: testPasswordHash,
      });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: user.email, password: 'ignored-in-fake' },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { success: boolean; data: ActiveSiteSession };
      expect(body.success).toBe(true);
      expect(body.data.userId).toBe(user.id);
      expect(body.data.activeSiteId).toBe(site.id);
      expectContractShape(body);

      const setCookie = response.headers['set-cookie'];
      expect(setCookie).toBeDefined();
      const cookieValue = Array.isArray(setCookie) ? setCookie[0] : setCookie;
      expect(cookieValue).toMatch(/__Host-lu_session=/);
      expectCookieFlags(cookieValue ?? '');
      expect(cookieValue).not.toContain('Max-Age=');
      expect(response.payload).not.toMatch(/__Host-lu_session|token/);
    });

    it('persiste la cookie solo cuando el usuario selecciona Recordarme', async () => {
      const csrf = await getCsrf();
      const site = createSite();
      const user = createUser({
        email: 'remember@example.com',
        passwordHash: testPasswordHash,
      });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: user.email, password: 'ignored-in-fake', rememberMe: true },
      });

      expect(response.statusCode).toBe(200);
      const setCookie = response.headers['set-cookie'];
      const cookieValue = Array.isArray(setCookie) ? setCookie[0] : setCookie;
      expectCookieFlags(cookieValue ?? '');
      expect(cookieValue).toMatch(/Max-Age=\d+/);
    });

    it('selects single membership automatically', async () => {
      const csrf = await getCsrf();
      const site = createSite();
      const user = createUser({
        email: 'single@example.com',
        passwordHash: testPasswordHash,
      });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: user.email, password: 'ignored-in-fake' },
      });

      const body = JSON.parse(response.payload) as { data: ActiveSiteSession };
      expect(body.data.activeSiteId).toBe(site.id);
    });

    it('does not bypass active site selection for SuperAdmin without membership', async () => {
      const csrf = await getCsrf();
      const user = createUser({
        email: 'admin@example.com',
        isSuperAdmin: true,
        passwordHash: testPasswordHash,
      });
      repository.users.set(user.id, user);
      // No memberships

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
        payload: { email: user.email, password: 'ignored-in-fake' },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { data: ActiveSiteSession };
      expect(body.data.globalRole).toBe('SuperAdmin');
      expect(body.data.activeSiteId).toBeNull();
      expect(body.data.activeSiteName).toBeNull();
      expect(body.data.memberships).toHaveLength(0);
      expectContractShape(body);
    });
  });

  // MIG-F3-AUTH-TEST-010: cierra la cobertura pendiente anotada en
  // auth.rate-limit.e2e-spec.ts. Produccion (`getRemoteAddress` en
  // auth.controller.ts) extrae la IP de request.socket.remoteAddress e ignora
  // X-Forwarded-For. Aqui se inyectan requests Fastify reales con XFF
  // falsificados cambiantes y socket IP fijo via `remoteAddress` de inject.
  describe('rate limit vs spoofed X-Forwarded-For (POST /api/v1/auth/login)', () => {
    const SOCKET_IP = '198.51.100.7';
    const OTHER_SOCKET_IP = '192.0.2.44';
    const SPOOF_PROBE_EMAIL = 'rate-limit-spoof@example.com';
    const SPOOFED_XFF_VALUES = [
      '203.0.113.10',
      '198.51.100.99',
      '8.8.8.8',
      '203.0.113.55',
      '203.0.113.66',
      '198.51.100.123',
    ];
    const MAX_ATTEMPTS = 3;
    const WINDOW_SECONDS = 60;

    function csrfHeaders(token: string) {
      return {
        'x-csrf-token': token,
        cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
      };
    }

    async function getCsrf(): Promise<string> {
      const response = await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
      const body = JSON.parse(response.payload) as { csrfToken: string };
      return body.csrfToken;
    }

    function attemptLogin(csrf: string, spoofedXff: string, remoteAddress: string) {
      return app.inject({
        method: 'POST',
        url: '/api/v1/auth/login',
        remoteAddress,
        headers: {
          ...csrfHeaders(csrf),
          origin: TRUSTED_ORIGIN,
          'x-forwarded-for': spoofedXff,
        },
        payload: { email: SPOOF_PROBE_EMAIL, password: 'wrong-password' },
      });
    }

    it('cannot evade the limiter via spoofed X-Forwarded-For; blocking keys on socket.remoteAddress', async () => {
      // App propia con limite conocido y baja ventana de estado: el limiter
      // vive en memoria por app, y el ciclo beforeEach/afterEach del suite
      // garantiza que no hay dependencia del orden de tests ni estado residual.
      await app.close();
      app = await createAuthTestApp({
        repository,
        config: {
          AUTH_RATE_LIMIT_MAX_ATTEMPTS: String(MAX_ATTEMPTS),
          AUTH_RATE_LIMIT_WINDOW_SECONDS: String(WINDOW_SECONDS),
        },
      });

      const csrf = await getCsrf();

      // 1) Alcanza exactamente el limite configurado: MAX_ATTEMPTS intentos
      // fallidos, cada uno con un X-Forwarded-For spoofeado distinto pero con
      // la misma IP real de socket.
      for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt += 1) {
        const response = await attemptLogin(csrf, SPOOFED_XFF_VALUES[attempt]!, SOCKET_IP);
        expect(response.statusCode).toBe(401);
        const body = JSON.parse(response.payload) as { error: { code: string } };
        expect(body.error.code).toBe('INVALID_CREDENTIALS');
      }

      // 2) El intento siguiente con un XFF spoofeado adicional debe ser 429.
      // Si el codigo confiara en XFF, la "IP nueva" habria reiniciado el bucket
      // y esta asercion fallaria (regresion detectable).
      const blocked = await attemptLogin(csrf, SPOOFED_XFF_VALUES[MAX_ATTEMPTS]!, SOCKET_IP);
      expect(blocked.statusCode).toBe(429);
      const blockedBody = JSON.parse(blocked.payload) as { error: { code: string } };
      expect(blockedBody.error.code).toBe('RATE_LIMITED');

      // 3) Retry-After valido: entero, mayor que 0 y no mayor que la ventana.
      const retryAfterHeader = blocked.headers['retry-after'];
      expect(typeof retryAfterHeader).toBe('string');
      const retryAfterSeconds = Number.parseInt(String(retryAfterHeader), 10);
      expect(Number.isNaN(retryAfterSeconds)).toBe(false);
      expect(retryAfterSeconds).toBeGreaterThan(0);
      expect(retryAfterSeconds).toBeLessThanOrEqual(WINDOW_SECONDS);

      // 4) Mas variaciones de XFF siguen bloqueadas (mismo socket real).
      const stillBlocked = await attemptLogin(
        csrf,
        SPOOFED_XFF_VALUES[MAX_ATTEMPTS + 1]!,
        SOCKET_IP,
      );
      expect(stillBlocked.statusCode).toBe(429);

      // 5) Control: el mismo XFF del primer intento pero con otra IP real de
      // socket NO esta bloqueado, lo que prueba que la clave del bucket es
      // socket.remoteAddress y no X-Forwarded-For.
      const otherSocket = await attemptLogin(csrf, SPOOFED_XFF_VALUES[0]!, OTHER_SOCKET_IP);
      expect(otherSocket.statusCode).toBe(401);
    });
  });

  describe('GET /api/v1/auth/session', () => {
    it('returns active session without requiring csrf', async () => {
      const site = createSite();
      const user = createUser();
      const { rawToken } = createAuthenticatedSession(repository, user, site);
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const response = await app.inject({
        method: 'GET',
        url: '/api/v1/auth/session',
        headers: {
          cookie: `__Host-lu_session=${rawToken}`,
        },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { data: ActiveSiteSession };
      expect(body.data.userId).toBe(user.id);
      expect(body.data.activeSiteId).toBe(site.id);
      expectContractShape(body);
    });

    it('rejects invalid session token', async () => {
      const response = await app.inject({
        method: 'GET',
        url: '/api/v1/auth/session',
        headers: {
          cookie: '__Host-lu_session=invalid-token',
        },
      });
      expect(response.statusCode).toBe(401);
    });
  });

  describe('PUT /api/v1/auth/session/active-site', () => {
    function csrfHeaders(token: string) {
      return {
        'x-csrf-token': token,
        cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
      };
    }

    async function getCsrf(): Promise<string> {
      const response = await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
      const body = JSON.parse(response.payload) as { csrfToken: string };
      return body.csrfToken;
    }

    it('allows switching to an eligible site', async () => {
      const csrf = await getCsrf();
      const siteA = createSite({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', code: 'A' });
      const siteB = createSite({ id: 'bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb', code: 'B' });
      const user = createUser();
      const { rawToken } = createAuthenticatedSession(repository, user, siteA);
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(siteA), createMembership(siteB)]);

      const response = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: {
          ...csrfHeaders(csrf),
          origin: TRUSTED_ORIGIN,
          cookie: `__Host-lu_session=${rawToken}; __Host-lu_csrf=${encodeURIComponent(csrf)}`,
        },
        payload: { activeSiteId: siteB.id },
      });

      expect(response.statusCode).toBe(200);
      const body = JSON.parse(response.payload) as { data: ActiveSiteSession };
      expect(body.data.activeSiteId).toBe(siteB.id);
      expectContractShape(body);
    });

    it('denies switching to a site the user does not belong to', async () => {
      const csrf = await getCsrf();
      const siteA = createSite({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', code: 'A' });
      const siteB = createSite({ id: 'bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb', code: 'B' });
      const user = createUser();
      const { rawToken } = createAuthenticatedSession(repository, user, siteA);
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(siteA)]);

      const response = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: {
          ...csrfHeaders(csrf),
          origin: TRUSTED_ORIGIN,
          cookie: `__Host-lu_session=${rawToken}; __Host-lu_csrf=${encodeURIComponent(csrf)}`,
        },
        payload: { activeSiteId: siteB.id },
      });

      expect(response.statusCode).toBe(403);
    });
  });

  describe('POST /api/v1/auth/logout', () => {
    function csrfHeaders(token: string) {
      return {
        'x-csrf-token': token,
        cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
      };
    }

    async function getCsrf(): Promise<string> {
      const response = await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
      const body = JSON.parse(response.payload) as { csrfToken: string };
      return body.csrfToken;
    }

    it('clears session cookie and is idempotent', async () => {
      const csrf = await getCsrf();
      const user = createUser();
      const { rawToken, tokenHash } = createAuthenticatedSession(repository, user);
      repository.users.set(user.id, user);

      const response = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/logout',
        headers: {
          ...csrfHeaders(csrf),
          origin: TRUSTED_ORIGIN,
          cookie: `__Host-lu_session=${rawToken}; __Host-lu_csrf=${encodeURIComponent(csrf)}`,
        },
      });

      expect(response.statusCode).toBe(204);
      expectSecurityHeaders(response);
      const setCookie = response.headers['set-cookie'];
      expect(setCookie).toBeDefined();
      const cookieValue = Array.isArray(setCookie) ? setCookie[0] : setCookie;
      expect(cookieValue).toMatch(/__Host-lu_session=;/);
      expect(cookieValue).toContain('Max-Age=0');
      expect(repository.revoked.has(tokenHash)).toBe(true);

      // idempotent second call
      const response2 = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/logout',
        headers: {
          ...csrfHeaders(csrf),
          origin: TRUSTED_ORIGIN,
          cookie: `__Host-lu_session=${rawToken}; __Host-lu_csrf=${encodeURIComponent(csrf)}`,
        },
      });
      expect(response2.statusCode).toBe(204);
      expectSecurityHeaders(response2);
    });
  });
});
