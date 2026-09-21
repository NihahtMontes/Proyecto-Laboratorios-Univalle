import { GlobalRole, SiteRole, type SiteId, type SiteRequestContext } from '@lu/contracts';
import { WriterLabel } from '../src/governance/writer-label.js';
import {
  PostgresTenantRouteCatalog,
  TenantRouter,
  TenantRoutingError,
  type TenantRouteCatalog,
  type TenantRouteRecord,
} from '../src/core/tenant-routing.js';
import type { IPgPool } from '../src/auth/auth.pg-pool.js';

const SITE_ID = '22222222-2222-4222-8222-222222222222' as SiteId;
const OTHER_SITE_ID = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' as SiteId;

const CONTEXT: SiteRequestContext = {
  correlationId: '33333333-3333-4333-8333-333333333333',
  userId: '11111111-1111-4111-8111-111111111111',
  siteId: SITE_ID,
  siteName: 'Sede QA',
  siteRole: SiteRole.Administrador,
  globalRole: GlobalRole.SuperAdmin,
};

class FakeCatalog implements TenantRouteCatalog {
  requestedSiteIds: SiteId[] = [];

  constructor(private readonly route: TenantRouteRecord | null) {}

  findBySiteId(siteId: SiteId): Promise<TenantRouteRecord | null> {
    this.requestedSiteIds.push(siteId);
    return Promise.resolve(this.route);
  }
}

describe('TenantRouter server-side isolation', () => {
  it('resolves only the authenticated context site and returns an internal secret reference', async () => {
    const catalog = new FakeCatalog({
      siteId: SITE_ID,
      state: 'active',
      runtimeSecretReference: 'tenant/sede-qa/runtime',
      writerLabel: WriterLabel.LEGACY_SQLSERVER,
    });
    const router = new TenantRouter(catalog);

    await expect(router.resolve(CONTEXT)).resolves.toEqual({
      siteId: SITE_ID,
      runtimeSecretReference: 'tenant/sede-qa/runtime',
      writerLabel: WriterLabel.LEGACY_SQLSERVER,
    });
    expect(catalog.requestedSiteIds).toEqual([SITE_ID]);
  });

  it.each([
    null,
    {
      siteId: OTHER_SITE_ID,
      state: 'active',
      runtimeSecretReference: 'tenant/other/runtime',
      writerLabel: WriterLabel.NEST_POSTGRES,
    },
    {
      siteId: SITE_ID,
      state: 'degraded',
      runtimeSecretReference: 'tenant/sede-qa/runtime',
      writerLabel: WriterLabel.NEST_POSTGRES,
    },
    {
      siteId: SITE_ID,
      state: 'active',
      runtimeSecretReference: 'Host=attacker.invalid;Password=secret',
      writerLabel: WriterLabel.NEST_POSTGRES,
    },
    {
      siteId: SITE_ID,
      state: 'active',
      runtimeSecretReference: '../tenant-secret',
      writerLabel: WriterLabel.NEST_POSTGRES,
    },
  ] satisfies Array<TenantRouteRecord | null>)(
    'fails closed for absent, mismatched, unhealthy or unsafe route %#',
    async (route) => {
      const router = new TenantRouter(new FakeCatalog(route));
      await expect(router.resolve(CONTEXT)).rejects.toBeInstanceOf(TenantRoutingError);
    },
  );
});

describe('PostgresTenantRouteCatalog', () => {
  it('maps only the server-side route row and preserves the writer label', async () => {
    const pool: IPgPool = {
      query: async <T = Record<string, unknown>>() => ({
        rows: [
          {
            site_id: SITE_ID,
            runtime_secret_reference: 'tenant/sede-qa/runtime',
            writer_label: WriterLabel.NEST_POSTGRES,
            state: 'active',
          },
        ] as T[],
        rowCount: 1,
      }),
      transaction: async () => {
        throw new Error('not used');
      },
    };

    await expect(new PostgresTenantRouteCatalog(pool).findBySiteId(SITE_ID)).resolves.toEqual({
      siteId: SITE_ID,
      runtimeSecretReference: 'tenant/sede-qa/runtime',
      writerLabel: WriterLabel.NEST_POSTGRES,
      state: 'active',
    });
  });

  it('fails closed for malformed control-plane route rows', async () => {
    const pool: IPgPool = {
      query: async <T = Record<string, unknown>>() => ({
        rows: [
          {
            site_id: SITE_ID,
            runtime_secret_reference: 'tenant/sede-qa/runtime',
            writer_label: 'UNTRUSTED',
            state: 'active',
          },
        ] as T[],
        rowCount: 1,
      }),
      transaction: async () => {
        throw new Error('not used');
      },
    };

    await expect(new PostgresTenantRouteCatalog(pool).findBySiteId(SITE_ID)).resolves.toBeNull();
  });
});
