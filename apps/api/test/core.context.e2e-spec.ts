import 'reflect-metadata';
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import type { ApiFailure, ApiSuccess, SiteRequestContext } from '@lu/contracts';
import { SESSION_COOKIE } from '../src/auth/auth.constants.js';
import { createMembership, createSite, createUser, FakeAuthRepository } from './auth.fakes.js';
import {
  createAuthenticatedSession,
  createAuthTestApp,
  expectSecurityHeaders,
  TRUSTED_ORIGIN,
  UNTRUSTED_ORIGIN,
} from './auth.test-helpers.js';

describe('Core site request context (e2e)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    app = await createAuthTestApp({ repository });
  });

  afterEach(async () => {
    await app.close();
  });

  function seedActiveContext(role: 'Administrador' | 'Supervisor' = 'Administrador') {
    const user = createUser();
    const site = createSite();
    repository.users.set(user.id, user);
    repository.memberships.set(user.id, [createMembership(site, { role })]);
    const auth = createAuthenticatedSession(repository, user, site);
    return { user, site, auth };
  }

  it('returns a correlation-bound context resolved only from the active session site', async () => {
    const { user, site, auth } = seedActiveContext('Supervisor');

    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/context',
      headers: {
        origin: TRUSTED_ORIGIN,
        cookie: `${SESSION_COOKIE}=${auth.rawToken}`,
        'x-site-id': 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
        'x-tenant-connection': 'Host=attacker.invalid',
      },
    });

    expect(response.statusCode).toBe(200);
    expectSecurityHeaders(response);
    expect(response.headers['access-control-allow-origin']).toBe(TRUSTED_ORIGIN);
    expect(response.headers['x-correlation-id']).toMatch(/^[0-9a-f-]{36}$/);
    const body = JSON.parse(response.payload) as ApiSuccess<SiteRequestContext>;
    expect(body).toEqual({
      success: true,
      data: {
        correlationId: response.headers['x-correlation-id'],
        userId: user.id,
        siteId: site.id,
        siteName: site.name,
        siteRole: 'Supervisor',
        globalRole: null,
      },
    });
    expect(JSON.stringify(body)).not.toContain('attacker.invalid');
    expect(JSON.stringify(body)).not.toMatch(/connection|dsn|host|writer/i);
  });

  it('rejects a request without a session and includes the server correlation id', async () => {
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/context',
      headers: { origin: TRUSTED_ORIGIN },
    });

    expect(response.statusCode).toBe(401);
    const body = JSON.parse(response.payload) as ApiFailure;
    expect(body.success).toBe(false);
    expect(body.error.code).toBe('INVALID_CREDENTIALS');
    expect(body.error.correlationId).toBe(response.headers['x-correlation-id']);
  });

  it('rejects a global SuperAdmin session until an eligible active site is selected', async () => {
    const user = createUser({ isSuperAdmin: true });
    repository.users.set(user.id, user);
    const auth = createAuthenticatedSession(repository, user);

    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/context',
      headers: {
        origin: TRUSTED_ORIGIN,
        cookie: `${SESSION_COOKIE}=${auth.rawToken}`,
      },
    });

    expect(response.statusCode).toBe(403);
    const body = JSON.parse(response.payload) as ApiFailure;
    expect(body.error.code).toBe('SITE_ACCESS_DENIED');
    expect(response.headers['access-control-allow-origin']).toBe(TRUSTED_ORIGIN);
  });

  it('rejects an untrusted browser origin before exposing context', async () => {
    const { auth } = seedActiveContext();
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/context',
      headers: {
        origin: UNTRUSTED_ORIGIN,
        cookie: `${SESSION_COOKIE}=${auth.rawToken}`,
      },
    });
    expect(response.statusCode).toBe(403);
    const body = JSON.parse(response.payload) as ApiFailure;
    expect(body.error.code).toBe('ORIGIN_DENIED');
  });

  it('preserves Administrator as a site role independently from global role', async () => {
    const { user, auth } = seedActiveContext('Administrador');
    repository.users.set(user.id, { ...user, isSuperAdmin: true });

    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/context',
      headers: {
        origin: TRUSTED_ORIGIN,
        cookie: `${SESSION_COOKIE}=${auth.rawToken}`,
      },
    });

    expect(response.statusCode).toBe(200);
    const body = JSON.parse(response.payload) as ApiSuccess<SiteRequestContext>;
    expect(body.data.siteRole).toBe('Administrador');
    expect(body.data.globalRole).toBe('SuperAdmin');
  });
});
