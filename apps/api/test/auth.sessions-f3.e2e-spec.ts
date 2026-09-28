/**
 * MIG-001 F3 — session purposes, site selection and guards (F1 §1, §12, §17)
 * over HTTP with the real AppModule wiring.
 */
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import type { ExecutionContext } from '@nestjs/common';
import { CSRF_COOKIE, SESSION_COOKIE } from '../src/auth/auth.constants.js';
import { hashSessionToken } from '../src/auth/auth.crypto.js';
import { AuthForbiddenException, AuthUnauthorizedException } from '../src/auth/auth.exceptions.js';
import type { FastifyRequest } from '../src/auth/auth.fastify.js';
import type { IdentityUser } from '../src/auth/auth.types.js';
import { GlobalSessionGuard, SiteContextGuard } from '../src/core/core.guard.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import {
  bcryptHash,
  createAuthService,
  createKernel,
  createTestApp,
  extractCookie,
  TRUSTED_ORIGIN,
  type AuthKernel,
} from './auth.harness.js';

const PASSWORD = 'Correct-Horse-9!';
const NEW_PASSWORD = 'Brand-New-Secret-7?';
const SITE_A = createSite({
  id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  code: 'A',
  name: 'Sede A',
});
const SITE_B = createSite({
  id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
  code: 'B',
  name: 'Sede B',
});

describe('Auth sessions and guards (F3, HTTP)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  let csrf: string;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    app = await createTestApp(repository, kernel);
    const response = await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
    csrf = (JSON.parse(response.payload) as { csrfToken: string }).csrfToken;
  });

  afterEach(async () => {
    await app.close();
  });

  function seed(overrides: Partial<IdentityUser> = {}, sites = [SITE_A]): IdentityUser {
    const identity = createIdentity({
      username: 'ana.perez',
      email: 'ana.perez@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
      ...overrides,
    });
    return repository.addIdentity(
      identity,
      sites.map((site) => createMembership(site, { userId: identity.id })),
    );
  }

  function headers(session?: string): Record<string, string> {
    const cookies = [`${CSRF_COOKIE}=${encodeURIComponent(csrf)}`];
    if (session !== undefined) cookies.push(`${SESSION_COOKIE}=${session}`);
    return { origin: TRUSTED_ORIGIN, 'x-csrf-token': csrf, cookie: cookies.join('; ') };
  }

  interface LoginResponseBody {
    meta: { purpose: string; mustChangePassword?: boolean };
    data: { activeSiteId: string | null };
    eligibleSites?: readonly unknown[];
  }

  async function loginHttp(
    extra: Record<string, unknown> = {},
  ): Promise<{ token: string; body: LoginResponseBody }> {
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: headers(),
      payload: { loginIdentifier: 'ana.perez', password: PASSWORD, ...extra },
    });
    expect(response.statusCode).toBe(200);
    const token = extractCookie(response.headers['set-cookie'], SESSION_COOKIE);
    expect(token).toBeDefined();
    return { token: token!, body: JSON.parse(response.payload) };
  }

  async function get(url: string, session: string) {
    return app.inject({ method: 'GET', url, headers: headers(session) });
  }

  it('a normal session resolves the tenant context from its active site only', async () => {
    const user = seed();
    const { token, body } = await loginHttp();
    expect(body.meta).toEqual({ purpose: 'normal', mustChangePassword: false });
    const session = await get('/api/v1/auth/session', token);
    expect(JSON.parse(session.payload).data.activeSiteId).toBe(SITE_A.id);
    const context = await get('/api/v1/context', token);
    expect(context.statusCode).toBe(200);
    expect(JSON.parse(context.payload).data).toMatchObject({
      userId: user.id,
      siteId: SITE_A.id,
      siteRole: 'Administrador',
    });
  });

  it('login and session responses never expose the raw token beyond the cookie', async () => {
    seed();
    const { token, body } = await loginHttp();
    expect(JSON.stringify(body)).not.toContain(token);
    const session = await get('/api/v1/auth/session', token);
    expect(session.payload).not.toContain(token);
  });

  describe('site_selection sessions', () => {
    it('are restricted, do not idle-extend and are replaced on a CSRF-protected choice', async () => {
      const user = seed({}, [SITE_A, SITE_B]);
      const { token, body } = await loginHttp();
      expect(body.meta.purpose).toBe('site_selection');
      expect(body.data.activeSiteId).toBeNull();
      expect(body.eligibleSites).toHaveLength(2);

      const stored = repository.sessions.get(hashSessionToken(token))!;
      expect((await get('/api/v1/context', token)).statusCode).toBe(403);
      const inspected = await get('/api/v1/auth/session', token);
      expect(JSON.parse(inspected.payload).meta.purpose).toBe('site_selection');
      expect(repository.sessions.get(hashSessionToken(token))!.idleExpiresAt).toEqual(
        stored.idleExpiresAt,
      );

      const versionBefore = repository.getIdentity(user.id).securityVersion;
      const chosen = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: headers(token),
        payload: { activeSiteId: SITE_B.id },
      });
      expect(chosen.statusCode).toBe(200);
      const replacement = extractCookie(chosen.headers['set-cookie'], SESSION_COOKIE);
      expect(replacement).toBeDefined();
      expect(replacement).not.toBe(token);
      expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).not.toBeNull();
      expect(repository.getIdentity(user.id).securityVersion).toBe(versionBefore);
      expect(kernel.audit.actions()).toContain('active_site_changed');

      const context = await get('/api/v1/context', replacement!);
      expect(context.statusCode).toBe(200);
      expect(JSON.parse(context.payload).data.siteId).toBe(SITE_B.id);
      expect((await get('/api/v1/auth/session', token)).statusCode).toBe(401);
    });

    it('cannot select an ineligible site and cannot change password', async () => {
      seed({}, [SITE_A, SITE_B]);
      const { token } = await loginHttp();
      const bad = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: headers(token),
        payload: { activeSiteId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc' },
      });
      expect(bad.statusCode).toBe(403);
      expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).toBeNull();
      const change = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/password',
        headers: headers(token),
        payload: { currentPassword: PASSWORD, newPassword: NEW_PASSWORD },
      });
      expect(change.statusCode).toBe(403);
    });
  });

  describe('normal-session site change', () => {
    it('updates only the current session and never rotates security_version', async () => {
      const user = seed({}, [SITE_A, SITE_B]);
      const { token } = await loginHttp({ activeSiteId: SITE_A.id });
      const other = await loginHttp({ activeSiteId: SITE_A.id });
      const response = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: headers(token),
        payload: { activeSiteId: SITE_B.id },
      });
      expect(response.statusCode).toBe(200);
      expect(extractCookie(response.headers['set-cookie'], SESSION_COOKIE)).toBeUndefined();
      expect(repository.getIdentity(user.id).securityVersion).toBe(user.securityVersion);
      expect(repository.sessions.get(hashSessionToken(token))!.activeSiteId).toBe(SITE_B.id);
      expect(repository.sessions.get(hashSessionToken(other.token))!.activeSiteId).toBe(SITE_A.id);
    });
  });

  describe('password_change sessions', () => {
    it('are limited to session, CSRF, password change and logout', async () => {
      seed({ mustChangePassword: true });
      const { token, body } = await loginHttp();
      expect(body.meta).toEqual({ purpose: 'password_change', mustChangePassword: true });
      expect((await get('/api/v1/auth/session', token)).statusCode).toBe(200);
      expect((await get('/api/v1/context', token)).statusCode).toBe(403);
      const select = await app.inject({
        method: 'PUT',
        url: '/api/v1/auth/session/active-site',
        headers: headers(token),
        payload: { activeSiteId: SITE_A.id },
      });
      expect(select.statusCode).toBe(403);

      const change = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/password',
        headers: headers(token),
        payload: { currentPassword: PASSWORD, newPassword: NEW_PASSWORD },
      });
      expect(change.statusCode).toBe(200);
      expect(JSON.parse(change.payload).meta).toEqual({
        purpose: 'normal',
        mustChangePassword: false,
      });
      const fresh = extractCookie(change.headers['set-cookie'], SESSION_COOKIE)!;
      expect((await get('/api/v1/context', fresh)).statusCode).toBe(200);
      expect((await get('/api/v1/auth/session', token)).statusCode).toBe(401);
    });
  });

  it('rejects a session whose security_version was rotated or whose account became inactive', async () => {
    const user = seed();
    const first = await loginHttp();
    repository.rotateAndRevokeAll(user.id, 'account_status_change');
    expect((await get('/api/v1/auth/session', first.token)).statusCode).toBe(401);

    const second = await loginHttp();
    repository.identities.set(user.id, {
      ...repository.getIdentity(user.id),
      accountStatus: 'inactive',
    });
    expect((await get('/api/v1/auth/session', second.token)).statusCode).toBe(401);
  });

  it('rejects the tenant context once the active membership is suspended', async () => {
    const user = seed();
    const { token } = await loginHttp();
    repository.memberships.set(user.id, [
      createMembership(SITE_A, { userId: user.id, status: 'suspended' }),
    ]);
    expect((await get('/api/v1/context', token)).statusCode).not.toBe(200);
  });

  describe('guards', () => {
    function contextFor(token: string | undefined): {
      ctx: ExecutionContext;
      request: FastifyRequest;
    } {
      const request = {
        method: 'GET',
        headers: token === undefined ? {} : { cookie: `${SESSION_COOKIE}=${token}` },
        correlationId: '0f0f0f0f-0f0f-4f0f-8f0f-0f0f0f0f0f0f',
      } as FastifyRequest;
      const ctx = {
        switchToHttp: () => ({ getRequest: () => request, getResponse: () => ({}) }),
      } as unknown as ExecutionContext;
      return { ctx, request };
    }

    it('GlobalSessionGuard admits a SuperAdmin null-site session with the session UUID, never the token', async () => {
      const service = createAuthService(repository, kernel);
      const user = seed({ isSuperAdmin: true }, []);
      const { token } = await service.login(
        { loginIdentifier: 'ana.perez', password: PASSWORD },
        '127.0.0.1',
      );
      const { ctx, request } = contextFor(token);
      await expect(new GlobalSessionGuard(service).canActivate(ctx)).resolves.toBe(true);
      const stored = repository.sessions.get(hashSessionToken(token))!;
      expect(request.identity).toEqual({
        correlationId: '0f0f0f0f-0f0f-4f0f-8f0f-0f0f0f0f0f0f',
        sessionId: stored.id,
        userId: user.id,
        isSuperAdmin: true,
        activeSiteId: null,
        activeSiteRole: null,
      });
      expect(JSON.stringify(request.identity)).not.toContain(token);
      await expect(
        new SiteContextGuard(service).canActivate(contextFor(token).ctx),
      ).rejects.toBeInstanceOf(AuthForbiddenException);
    });

    it('GlobalSessionGuard rejects restricted purposes and non-SuperAdmins without a site', async () => {
      const service = createAuthService(repository, kernel);
      seed({ mustChangePassword: true });
      const restricted = await service.login(
        { loginIdentifier: 'ana.perez', password: PASSWORD },
        '127.0.0.1',
      );
      await expect(
        new GlobalSessionGuard(service).canActivate(contextFor(restricted.token).ctx),
      ).rejects.toBeInstanceOf(AuthForbiddenException);
      await expect(
        new GlobalSessionGuard(service).canActivate(contextFor(undefined).ctx),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    });
  });
});
