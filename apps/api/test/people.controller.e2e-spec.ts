/**
 * MIG-001 F3 — People tab boundary (F1 §15, §17) behind the real guards:
 * tenant People CRUD requires an eligible active-site Administrador; a global
 * SuperAdmin alone does not bypass tenant membership, and the explicit deleted
 * filter reaches the repository unchanged.
 */
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import type { TestingModuleBuilder } from '@nestjs/testing';
import { AuthService } from '../src/auth/auth.service.js';
import { TenantPgPoolRegistry } from '../src/core/tenant-pg-pool.js';
import { TenantRouter } from '../src/core/tenant-routing.js';
import { PersonRepository } from '../src/people/person.repository.js';
import type { IdentityUser } from '../src/auth/auth.types.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import { bcryptHash, createKernel, createTestApp, TRUSTED_ORIGIN } from './auth.harness.js';

const PASSWORD = 'Correct-Horse-9!';
const SITE = createSite({ id: '11111111-1111-4111-8111-111111111111', code: 'A', name: 'Sede A' });

describe('PersonController (F3 access boundary)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  const listCalls: { siteId: string; statusFilter: unknown }[] = [];

  beforeEach(async () => {
    listCalls.length = 0;
    repository = new FakeAuthRepository();
    const configure = (builder: TestingModuleBuilder) =>
      builder
        .overrideProvider(TenantRouter)
        .useValue({ resolve: async () => ({ siteId: SITE.id }) })
        .overrideProvider(TenantPgPoolRegistry)
        .useValue({ get: () => ({}), onApplicationShutdown: async () => undefined })
        .overrideProvider(PersonRepository)
        .useValue({
          list: async (_pool: unknown, siteId: string, query: { statusFilter?: unknown }) => {
            listCalls.push({ siteId, statusFilter: query.statusFilter });
            return { items: [], totalCount: 0, pageIndex: 1, totalPages: 0, pageSize: 10 };
          },
        });
    app = await createTestApp(repository, createKernel(repository), undefined, configure);
  });

  afterEach(async () => {
    await app.close();
  });

  async function login(
    overrides: Partial<IdentityUser>,
    role: 'Administrador' | 'Supervisor' | null,
  ): Promise<string> {
    const identity = createIdentity({
      username: 'ana',
      email: 'ana@x.test',
      passwordHash: bcryptHash(PASSWORD),
      ...overrides,
    });
    repository.addIdentity(
      identity,
      role === null ? [] : [createMembership(SITE, { userId: identity.id, role })],
    );
    return (
      await app.get(AuthService).login({ loginIdentifier: 'ana', password: PASSWORD }, '127.0.0.1')
    ).token;
  }

  function list(token?: string, query = '') {
    return app.inject({
      method: 'GET',
      url: '/api/v1/people' + query,
      headers: {
        origin: TRUSTED_ORIGIN,
        ...(token === undefined ? {} : { cookie: `__Host-lu_session=${token}` }),
      },
    });
  }

  it('requires a session', async () => {
    expect((await list()).statusCode).toBe(401);
  });

  it('denies a SuperAdmin without an eligible active site', async () => {
    const token = await login({ isSuperAdmin: true }, null);
    expect((await list(token)).statusCode).toBe(403);
    expect(listCalls).toHaveLength(0);
  });

  it('denies a Supervisor', async () => {
    expect((await list(await login({}, 'Supervisor'))).statusCode).toBe(403);
    expect(listCalls).toHaveLength(0);
  });

  it('denies a SuperAdmin whose active-site membership is Supervisor (no global bypass)', async () => {
    expect((await list(await login({ isSuperAdmin: true }, 'Supervisor'))).statusCode).toBe(403);
    expect(listCalls).toHaveLength(0);
  });

  it('lets an active-site Administrador list and forwards the explicit deleted filter', async () => {
    const token = await login({}, 'Administrador');
    expect((await list(token)).statusCode).toBe(200);
    expect((await list(token, '?statusFilter=2')).statusCode).toBe(200);
    expect(listCalls).toEqual([
      { siteId: SITE.id, statusFilter: undefined },
      { siteId: SITE.id, statusFilter: 2 },
    ]);
  });

  it('rejects a restricted password_change session', async () => {
    const token = await login({ mustChangePassword: true }, 'Administrador');
    expect((await list(token)).statusCode).toBe(403);
  });
});
