import assert from 'node:assert/strict';
import test from 'node:test';
import {
  AUTH_ROUTES,
  CORE_ROUTES,
  DASHBOARD_ROUTES,
  SiteRole,
  SiteState,
  type ActiveSiteSession,
  type SiteId,
  type SiteRequestContext,
} from '@lu/contracts';
import { ApiClient, ApiClientError } from './index.js';

const SITE_ID = '22222222-2222-4222-8222-222222222222' as SiteId;
const SESSION: ActiveSiteSession = {
  userId: '11111111-1111-4111-8111-111111111111',
  displayName: 'Administradora QA',
  email: 'admin@univalle.edu',
  activeSiteId: SITE_ID,
  activeSiteName: 'Sede QA',
  globalRole: null,
  memberships: [
    {
      siteId: SITE_ID,
      siteName: 'Sede QA',
      role: SiteRole.Administrador,
      state: SiteState.Active,
    },
  ],
};

test('rejects insecure or credential-bearing API origins', () => {
  assert.throws(() => new ApiClient({ baseUrl: 'http://api.example.test' }), /HTTPS/);
  assert.throws(
    () => new ApiClient({ baseUrl: 'https://user:password@api.example.test' }),
    /must not contain credentials/,
  );
  assert.doesNotThrow(() => new ApiClient({ baseUrl: 'http://localhost:3000' }));
});

test('login obtains CSRF first and sends only the typed credential payload', async () => {
  const calls: Array<{ url: string; init: RequestInit }> = [];
  const fetcher: typeof fetch = async (input, init = {}) => {
    const url = String(input);
    calls.push({ url, init });
    if (url.endsWith(AUTH_ROUTES.csrf)) {
      return new Response(JSON.stringify({ csrfToken: 'csrf-token' }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      });
    }
    return new Response(JSON.stringify({ success: true, data: SESSION }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
  };
  const client = new ApiClient({ baseUrl: 'https://api.example.test', fetch: fetcher });

  await assert.doesNotReject(() =>
    client.login({ email: 'user@example.test', password: 'secret-value' }),
  );

  assert.equal(calls.length, 2);
  assert.equal(calls[0]?.url, `https://api.example.test${AUTH_ROUTES.csrf}`);
  assert.equal(calls[0]?.init.credentials, 'include');
  assert.equal(calls[1]?.url, `https://api.example.test${AUTH_ROUTES.login}`);
  assert.equal(new Headers(calls[1]?.init.headers).get('X-CSRF-Token'), 'csrf-token');
  assert.deepEqual(JSON.parse(String(calls[1]?.init.body)), {
    email: 'user@example.test',
    password: 'secret-value',
  });
});

test('site context uses the fixed shared route and maps typed success', async () => {
  const context: SiteRequestContext = {
    correlationId: '33333333-3333-4333-8333-333333333333',
    userId: SESSION.userId,
    siteId: SITE_ID,
    siteName: 'Sede QA',
    siteRole: SiteRole.Administrador,
    globalRole: null,
  };
  let calledUrl = '';
  const client = new ApiClient({
    fetch: async (input) => {
      calledUrl = String(input);
      return new Response(JSON.stringify({ success: true, data: context }), { status: 200 });
    },
  });

  await assert.doesNotReject(async () => {
    assert.deepEqual(await client.siteContext(), context);
  });
  assert.equal(calledUrl, CORE_ROUTES.siteContext);
});

test('maps a typed API failure without exposing arbitrary response text', async () => {
  const client = new ApiClient({
    fetch: async () =>
      new Response(
        JSON.stringify({
          success: false,
          error: { code: 'SITE_ACCESS_DENIED', message: 'Site access denied.' },
        }),
        { status: 403 },
      ),
  });

  await assert.rejects(client.session(), (error: unknown) => {
    assert.ok(error instanceof ApiClientError);
    assert.equal(error.status, 403);
    assert.equal(error.failure.error.code, 'SITE_ACCESS_DENIED');
    return true;
  });
});

test('builds dashboard filters and protects notification commands with CSRF', async () => {
  const calls: Array<{ url: string; init: RequestInit }> = [];
  const fetcher: typeof fetch = async (input, init = {}) => {
    const url = String(input);
    calls.push({ url, init });
    if (url.endsWith(AUTH_ROUTES.csrf)) {
      return new Response(JSON.stringify({ csrfToken: 'csrf-token' }), { status: 200 });
    }
    if (url.includes(DASHBOARD_ROUTES.summary)) {
      return new Response(
        JSON.stringify({
          success: true,
          data: {
            activeManagement: null,
            metrics: {
              totalAssets: 0,
              completedAssets: 0,
              overdueAssets: 0,
              globalProgress: 0,
              pendingAssets: 0,
              goodAssets: 0,
              l6: 0,
              l7: 0,
              l8: 0,
              departures: 0,
              disbursements: 0,
            },
            equipmentTypes: [],
            groups: [],
            laboratories: [],
            overduePlans: [],
            plans: [],
            page: 1,
            pageSize: 5,
          },
        }),
        { status: 200 },
      );
    }
    return new Response(JSON.stringify({ success: true, data: null }), { status: 200 });
  };
  const client = new ApiClient({ fetch: fetcher });
  await client.dashboard({ managementId: 4, currentPage: 2, inventoryNumber: 'INV-9' });
  await client.markAllNotificationsRead();

  assert.match(calls[0]?.url ?? '', /managementId=4/);
  assert.match(calls[0]?.url ?? '', /currentPage=2/);
  assert.match(calls[0]?.url ?? '', /inventoryNumber=INV-9/);
  assert.equal(calls.at(-1)?.url, DASHBOARD_ROUTES.markAllNotificationsRead);
  assert.equal(new Headers(calls.at(-1)?.init.headers).get('X-CSRF-Token'), 'csrf-token');
});
