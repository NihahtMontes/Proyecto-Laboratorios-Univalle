import 'reflect-metadata';
import { ExecutionContext, Injectable, type CanActivate } from '@nestjs/common';
import { FastifyAdapter, type NestFastifyApplication } from '@nestjs/platform-fastify';
import { Test } from '@nestjs/testing';
import { AUTH_CONFIG, AUTH_PG_POOL, SELF_SESSION_RENEWER } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthSecurityGuard } from '../src/auth/auth.guard.js';
import {
  IDENTITY_AUDIT_WRITER,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  SESSION_INVALIDATOR,
  type RequestIdentity,
} from '../src/identity/identity.contracts.js';
import { CoreExceptionFilter } from '../src/core/core.filter.js';
import { GlobalSessionGuard } from '../src/core/core.guard.js';
import { ProfileController, UserController } from '../src/users/user.controller.js';
import { UserService } from '../src/users/user.service.js';
import { AuthService } from '../src/auth/auth.service.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import { bcryptHash, createKernel, createTestApp, type AuthKernel } from './auth.harness.js';
import { MembershipRepository } from '../src/users/membership.repository.js';
import { UserRepository } from '../src/users/user.repository.js';
import { SuperAdminRoleService } from '../src/users/superadmin-role.service.js';
import {
  FakeIdentityAuditWriter,
  FakePasswordHasher,
  FakePasswordPolicy,
  FakeSelfSessionRenewer,
  FakeSessionInvalidator,
  FakeUsersPgPool,
} from './users.fakes.js';

const TRUSTED_ORIGIN = 'http://localhost:3000';

@Injectable()
class FakeGlobalSessionGuard implements CanActivate {
  private static identity: RequestIdentity | null = null;
  static setIdentity(identity: RequestIdentity | null): void {
    FakeGlobalSessionGuard.identity = identity;
  }
  canActivate(context: ExecutionContext): true {
    const request = context.switchToHttp().getRequest<{ identity?: RequestIdentity }>();
    if (FakeGlobalSessionGuard.identity !== null) {
      request.identity = FakeGlobalSessionGuard.identity;
    }
    return true;
  }
}

@Injectable()
class PassThroughGuard implements CanActivate {
  canActivate(): true {
    return true;
  }
}

async function createApp(pool: FakeUsersPgPool): Promise<NestFastifyApplication> {
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
    controllers: [UserController, ProfileController],
    providers: [
      UserService,
      UserRepository,
      MembershipRepository,
      SuperAdminRoleService,
      { provide: AUTH_CONFIG, useValue: new AuthConfig(env) },
      { provide: AUTH_PG_POOL, useValue: pool },
      { provide: PASSWORD_POLICY, useValue: new FakePasswordPolicy(true) },
      { provide: PASSWORD_HASHER, useValue: new FakePasswordHasher() },
      { provide: SESSION_INVALIDATOR, useValue: new FakeSessionInvalidator() },
      { provide: IDENTITY_AUDIT_WRITER, useValue: new FakeIdentityAuditWriter() },
      { provide: SELF_SESSION_RENEWER, useValue: new FakeSelfSessionRenewer() },
      AuthSecurityGuard,
      CoreExceptionFilter,
    ],
  })
    .overrideGuard(GlobalSessionGuard)
    .useClass(FakeGlobalSessionGuard)
    .overrideGuard(AuthSecurityGuard)
    .useClass(PassThroughGuard)
    .compile();
  const app = moduleRef.createNestApplication<NestFastifyApplication>(new FastifyAdapter(), {
    logger: false,
  });
  app.setGlobalPrefix('api/v1');
  app.useGlobalFilters(new CoreExceptionFilter());
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
  return app;
}

function csrfHeaders(token: string): Record<string, string> {
  return {
    'x-csrf-token': token,
    cookie: `__Host-lu_csrf=${encodeURIComponent(token)}`,
  };
}

async function getCsrf(_app: NestFastifyApplication): Promise<string> {
  // CSRF is owned by the auth slice and is not wired here. The auth-slice
  // test harness (auth.test-helpers.ts) sets up the CSRF cookie; this slice
  // installs a PassThroughGuard that bypasses the AuthSecurityGuard's CSRF
  // check, so the tests below can use any opaque token as a placeholder.
  void _app;
  return 'csrf-token';
}

const SUPERADMIN_IDENTITY: RequestIdentity = {
  correlationId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
  sessionId: 'ssssssss-ssss-4sss-8sss-ssssssssssss',
  userId: '00000000-0000-4000-8000-000000000001',
  isSuperAdmin: true,
  activeSiteId: null,
  activeSiteRole: null,
};

const ADMIN_IDENTITY: RequestIdentity = {
  correlationId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
  sessionId: 'ssssssss-ssss-4sss-8sss-ssssssssssss',
  userId: '00000000-0000-4000-8000-000000000002',
  isSuperAdmin: false,
  activeSiteId: '11111111-1111-4111-8111-111111111111',
  activeSiteRole: 'Administrador',
};

describe('UserController (HTTP wiring)', () => {
  let app: NestFastifyApplication;
  let pool: FakeUsersPgPool;

  beforeEach(async () => {
    pool = new FakeUsersPgPool();
    app = await createApp(pool);
  });

  afterEach(() => {
    FakeGlobalSessionGuard.setIdentity(null);
  });

  afterEach(async () => {
    await app.close();
  });

  it('rejects unknown keys in updateGlobalFields (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/users/00000000-0000-4000-8000-000000000099/global-fields',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { fullName: 'Hacker', firstName: 'Real' },
    });
    expect(response.statusCode).toBe(400);
  });

  it('rejects isSuperAdmin over HTTP in create payload (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/users',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: {
        isSuperAdmin: true,
        username: 'admin',
        email: 'admin@example.com',
        firstName: 'A',
        lastName: 'B',
        identityCard: 'A1',
        phoneNumber: '+1 5550102030',
        password: 'Password!Aa1xyz',
        role: 'Administrador',
      },
    });
    expect(response.statusCode).toBe(400);
  });

  it('rejects passwordHash over HTTP (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/users',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: {
        passwordHash: '$2b$12$deadbeef',
        username: 'admin',
        email: 'admin@example.com',
        firstName: 'A',
        lastName: 'B',
        identityCard: 'A1',
        phoneNumber: '+1 5550102030',
        password: 'Password!Aa1xyz',
        role: 'Administrador',
      },
    });
    expect(response.statusCode).toBe(400);
  });

  it('rejects securityVersion/rowVersion in update payload (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/users/00000000-0000-4000-8000-000000000099/global-fields',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { securityVersion: '99', rowVersion: '5', email: 'new@example.com' },
    });
    expect(response.statusCode).toBe(400);
  });

  it('rejects an invalid uuid in :id (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    await getCsrf(app);
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/users/not-a-uuid',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(400);
  });

  it('rejects an invalid uuid in membership :siteId (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/users/00000000-0000-4000-8000-000000000099/memberships/not-a-uuid/role',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { role: 'Administrador' },
    });
    expect(response.statusCode).toBe(400);
  });

  it('GET /profile returns the self-service subset', async () => {
    FakeGlobalSessionGuard.setIdentity(ADMIN_IDENTITY);
    pool.queueResult([
      {
        id: ADMIN_IDENTITY.userId,
        email: 'admin@example.com',
        first_name: 'Site',
        last_name: 'Admin',
        second_last_name: null,
        full_name: 'Site Admin',
        identity_card: 'A1',
        phone_number: '+1 5550102030',
        account_status: 'active',
        status: 'active',
        is_super_admin: false,
        password_scheme: 'bcrypt',
        must_change_password: false,
        security_version: '1',
        created_at: new Date(),
        updated_at: new Date(),
      },
    ]);
    pool.queueResult([]);
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/profile',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(200);
    const body = JSON.parse(response.payload) as { data: { id: string; email: string } };
    expect(body.data.email).toBe('admin@example.com');
  });

  it('PUT /profile forbids identityCard (400)', async () => {
    FakeGlobalSessionGuard.setIdentity(ADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/profile',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { identityCard: 'A1' },
    });
    console.log('profile body:', response.payload);
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 (not 500) for an invalid role enum', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/users',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: {
        username: 'admin',
        email: 'admin@example.com',
        firstName: 'A',
        lastName: 'B',
        identityCard: 'A1',
        phoneNumber: '+1 5550102030',
        password: 'Password!Aa1xyz',
        role: 'SuperAdmin',
      },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 (not 500) for membership status outside {active,suspended}', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const csrf = await getCsrf(app);
    const response = await app.inject({
      method: 'PUT',
      url: '/api/v1/users/00000000-0000-4000-8000-000000000099/memberships/11111111-1111-4111-8111-111111111111/status',
      headers: { ...csrfHeaders(csrf), origin: TRUSTED_ORIGIN },
      payload: { status: 'deleted' },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 (not 500) for invalid currentPage', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/users?currentPage=0',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 400 for invalid statusFilter', async () => {
    FakeGlobalSessionGuard.setIdentity(SUPERADMIN_IDENTITY);
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/users?statusFilter=pending',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(400);
  });

  it('returns 401 when the actor identity is missing', async () => {
    FakeGlobalSessionGuard.setIdentity(null);
    const response = await app.inject({
      method: 'GET',
      url: '/api/v1/users',
      headers: { origin: TRUSTED_ORIGIN },
    });
    expect(response.statusCode).toBe(401);
  });
});
/**
 * Real AppModule wiring: ApiCorrelationGuard -> AuthSecurityGuard ->
 * GlobalSessionGuard protect /users and /profile (no guard overrides).
 */
describe('Users routes behind the real guards (F1 §17)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  const password = 'Correct-Horse-9!';
  const siteA = createSite({
    id: '11111111-1111-4111-8111-111111111111',
    code: 'A',
    name: 'Sede A',
  });

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    app = await createTestApp(repository, kernel);
  });

  afterEach(async () => {
    await app.close();
  });

  async function loginAs(
    overrides: Parameters<typeof createIdentity>[0],
    sites = [siteA],
    role: 'Administrador' | 'Supervisor' = 'Administrador',
  ): Promise<string> {
    const identity = createIdentity({
      username: 'actor.user',
      email: 'actor@univalle.test',
      passwordHash: bcryptHash(password),
      ...overrides,
    });
    repository.addIdentity(
      identity,
      sites.map((site) => createMembership(site, { userId: identity.id, role })),
    );
    return (
      await app.get(AuthService).login({ loginIdentifier: 'actor.user', password }, '127.0.0.1')
    ).token;
  }

  function get(url: string, token?: string) {
    return app.inject({
      method: 'GET',
      url,
      headers: {
        origin: TRUSTED_ORIGIN,
        ...(token === undefined ? {} : { cookie: `__Host-lu_session=${token}` }),
      },
    });
  }

  it('requires a session', async () => {
    expect((await get('/api/v1/users')).statusCode).toBe(401);
    expect((await get('/api/v1/profile')).statusCode).toBe(401);
  });

  it('lets a SuperAdmin without an active site reach the global users view', async () => {
    const token = await loginAs({ isSuperAdmin: true }, []);
    const response = await get('/api/v1/users', token);
    expect(response.statusCode).toBe(200);
    expect(JSON.parse(response.payload)).toMatchObject({ success: true, data: { items: [] } });
  });

  it('forbids a Supervisor from administering users', async () => {
    const token = await loginAs({}, [siteA], 'Supervisor');
    expect((await get('/api/v1/users', token)).statusCode).toBe(403);
  });

  it('rejects restricted password_change sessions on users and profile routes', async () => {
    const token = await loginAs({ mustChangePassword: true });
    expect((await get('/api/v1/users', token)).statusCode).toBe(403);
    expect((await get('/api/v1/profile', token)).statusCode).toBe(403);
  });

  it('enforces CSRF on mutating users routes before any session work', async () => {
    const token = await loginAs({ isSuperAdmin: true }, []);
    const response = await app.inject({
      method: 'POST',
      url: '/api/v1/users',
      headers: { origin: TRUSTED_ORIGIN, cookie: `__Host-lu_session=${token}` },
      payload: {},
    });
    expect(response.statusCode).toBe(403);
    expect(JSON.parse(response.payload).error.code).toBe('CSRF_INVALID');
  });
});
