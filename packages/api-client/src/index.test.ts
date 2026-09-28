import assert from 'node:assert/strict';
import test from 'node:test';
import {
  AUTH_ROUTES,
  CORE_ROUTES,
  DASHBOARD_ROUTES,
  PASSWORD_POLICY,
  PERSON_ROUTES,
  PROFILE_PHOTO,
  SiteRole,
  SiteState,
  USER_ROUTES,
  passwordPolicyViolations,
  type ActiveSiteSession,
  type AuthSessionEnvelope,
  type EligibleSite,
  type ManagedUserPage,
  type ManagedUserRecord,
  type PersonPage,
  type SessionPurpose,
  type SiteId,
  type SiteRequestContext,
} from '@lu/contracts';
import { ApiClient, ApiClientError, apiErrorKind } from './index.js';

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

  await assert.rejects(client.currentSession(), (error: unknown) => {
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

// ---------------------------------------------------------------------------
// MIG-001 F4: auth session, users, people, photos and error model.
// ---------------------------------------------------------------------------

const API = 'https://api.example.test';
const USER_ID = '44444444-4444-4444-8444-444444444444';
const SECOND_SITE = '55555555-5555-4555-8555-555555555555' as SiteId;

interface Call {
  readonly url: string;
  readonly init: RequestInit;
}

function json(body: unknown, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json', ...headers },
  });
}

/** Fake fetch: CSRF always succeeds; every other call is answered by `reply`. */
function harness(reply: (url: string, init: RequestInit) => Response) {
  const calls: Call[] = [];
  const fetcher: typeof fetch = async (input, init = {}) => {
    const url = String(input);
    calls.push({ url, init });
    if (url.endsWith(AUTH_ROUTES.csrf)) return json({ csrfToken: 'csrf-token' });
    return reply(url, init);
  };
  const client = new ApiClient({ baseUrl: API, fetch: fetcher });
  const last = (): Call => calls.at(-1)!;
  const commands = (): Call[] => calls.filter((call) => !call.url.endsWith(AUTH_ROUTES.csrf));
  return { client, calls, last, commands };
}

function envelope(
  purpose: SessionPurpose,
  eligibleSites: readonly EligibleSite[] = [],
  mustChangePassword = false,
): AuthSessionEnvelope {
  const restricted = purpose !== 'normal';
  return {
    success: true,
    data: restricted
      ? { ...SESSION, activeSiteId: null, activeSiteName: null, memberships: [] }
      : SESSION,
    meta: { purpose, mustChangePassword },
    eligibleSites,
  };
}

const ELIGIBLE: readonly EligibleSite[] = [
  { siteId: SITE_ID, siteName: 'Sede QA', role: 'Administrador' },
  { siteId: SECOND_SITE, siteName: 'Sede Norte', role: 'Supervisor' },
];

function bodyOf(call: Call): unknown {
  return JSON.parse(String(call.init.body));
}

function header(call: Call, name: string): string | null {
  return new Headers(call.init.headers).get(name);
}

async function rejection(promise: Promise<unknown>): Promise<ApiClientError> {
  try {
    await promise;
  } catch (error) {
    assert.ok(error instanceof ApiClientError);
    return error;
  }
  return assert.fail('expected the call to reject');
}

function failure(status: number, code: string, message: string, extra: object = {}): Response {
  return json({ success: false, error: { code, message, ...extra } }, status);
}

test('signIn sends loginIdentifier for a username after obtaining CSRF with credentials', async () => {
  const { client, calls, last } = harness(() => json(envelope('normal', [ELIGIBLE[0]!])));

  const result = await client.signIn({
    loginIdentifier: 'admin.qa',
    password: 'secret-value',
    rememberMe: true,
  });

  assert.equal(calls[0]?.url, `${API}${AUTH_ROUTES.csrf}`);
  assert.equal(calls[0]?.init.credentials, 'include');
  assert.equal(last().url, `${API}${AUTH_ROUTES.login}`);
  assert.equal(last().init.method, 'POST');
  assert.equal(last().init.credentials, 'include');
  assert.equal(header(last(), 'X-CSRF-Token'), 'csrf-token');
  assert.deepEqual(bodyOf(last()), {
    loginIdentifier: 'admin.qa',
    password: 'secret-value',
    rememberMe: true,
  });
  assert.equal(result.purpose, 'normal');
  assert.equal(result.mustChangePassword, false);
  assert.deepEqual(result.session, SESSION);
  assert.deepEqual(result.eligibleSites, [ELIGIBLE[0]]);
});

test('signIn accepts an email as loginIdentifier and never sends the legacy email key', async () => {
  const { client, last } = harness(() => json(envelope('normal')));
  await client.signIn({ loginIdentifier: 'admin@univalle.edu', password: 'secret-value' });
  assert.deepEqual(bodyOf(last()), {
    loginIdentifier: 'admin@univalle.edu',
    password: 'secret-value',
  });
});

test('site_selection login is explicit and lists the eligible sites', async () => {
  const { client } = harness(() => json(envelope('site_selection', ELIGIBLE)));
  const result = await client.signIn({ loginIdentifier: 'admin.qa', password: 'secret-value' });
  assert.equal(result.purpose, 'site_selection');
  assert.equal(result.session.activeSiteId, null);
  assert.deepEqual(result.session.memberships, []);
  assert.deepEqual(
    result.eligibleSites.map((site) => site.siteId),
    [SITE_ID, SECOND_SITE],
  );
});

test('password_change login is explicit and exposes no credential internals', async () => {
  const { client } = harness(() => json(envelope('password_change', [], true)));
  const result = await client.signIn({ loginIdentifier: 'admin.qa', password: 'secret-value' });
  assert.equal(result.purpose, 'password_change');
  assert.equal(result.mustChangePassword, true);
  assert.deepEqual(result.eligibleSites, []);
  const serialized = JSON.stringify(result);
  for (const secret of ['securityVersion', 'passwordScheme', 'passwordHash', 'token']) {
    assert.equal(serialized.includes(secret), false, secret);
  }
});

test('currentSession reads GET /auth/session without CSRF and keeps the purpose', async () => {
  const { client, calls, last } = harness(() => json(envelope('site_selection', ELIGIBLE)));
  const result = await client.currentSession();
  assert.equal(calls.length, 1);
  assert.equal(last().url, `${API}${AUTH_ROUTES.session}`);
  assert.equal(last().init.method, 'GET');
  assert.equal(last().init.credentials, 'include');
  assert.equal(header(last(), 'X-CSRF-Token'), null);
  assert.equal(result.purpose, 'site_selection');
});

test('selectActiveSite sends only activeSiteId with CSRF and returns the normal session', async () => {
  const { client, last } = harness(() => json(envelope('normal', ELIGIBLE)));
  const result = await client.selectActiveSite({ activeSiteId: SECOND_SITE });
  assert.equal(last().url, `${API}${AUTH_ROUTES.activeSite}`);
  assert.equal(last().init.method, 'PUT');
  assert.equal(header(last(), 'X-CSRF-Token'), 'csrf-token');
  assert.deepEqual(bodyOf(last()), { activeSiteId: SECOND_SITE });
  assert.equal(result.purpose, 'normal');
});

test('changePassword posts exactly currentPassword/newPassword and returns the new session', async () => {
  const { client, last } = harness(() => json(envelope('normal')));
  const result = await client.changePassword({
    currentPassword: 'Old-password-1',
    newPassword: 'New-password-12',
  });
  assert.equal(last().url, `${API}${AUTH_ROUTES.password}`);
  assert.equal(last().init.method, 'POST');
  assert.deepEqual(bodyOf(last()), {
    currentPassword: 'Old-password-1',
    newPassword: 'New-password-12',
  });
  assert.equal(result.purpose, 'normal');
});

test('changePassword surfaces policy violations as typed validation field errors', async () => {
  const { client } = harness(() =>
    failure(400, 'VALIDATION_ERROR', 'New password violates the password policy.', {
      fieldErrors: { newPassword: ['too_short', 'missing_symbol'] },
    }),
  );
  const error = await rejection(
    client.changePassword({ currentPassword: 'Old-password-1', newPassword: 'short' }),
  );
  assert.equal(error.status, 400);
  assert.equal(error.kind, 'validation');
  assert.equal(error.code, 'VALIDATION_ERROR');
  assert.deepEqual(error.fieldErrors.newPassword, ['too_short', 'missing_symbol']);
});

test('auth responses without a valid purpose envelope are invalid responses', async () => {
  const bare = harness(() => json({ success: true, data: SESSION }));
  const error = await rejection(bare.client.currentSession());
  assert.equal(error.code, 'INVALID_API_RESPONSE');
  assert.equal(error.kind, 'invalid_response');

  const unknownPurpose = harness(() =>
    json({ ...envelope('normal'), meta: { purpose: 'elevated', mustChangePassword: false } }),
  );
  assert.equal((await rejection(unknownPurpose.client.currentSession())).kind, 'invalid_response');
});

test('invalid credentials map to a 401 authentication error with the generic message', async () => {
  const { client } = harness(() => failure(401, 'INVALID_CREDENTIALS', 'Invalid credentials.'));
  const error = await rejection(
    client.signIn({ loginIdentifier: 'admin.qa', password: 'wrong-password' }),
  );
  assert.equal(error.status, 401);
  assert.equal(error.kind, 'authentication');
  assert.equal(error.message, 'Invalid credentials.');
});

test('rate limiting exposes Retry-After seconds', async () => {
  const { client } = harness(() =>
    json(
      { success: false, error: { code: 'RATE_LIMITED', message: 'Too many login attempts.' } },
      429,
      { 'Retry-After': '30' },
    ),
  );
  const error = await rejection(client.signIn({ loginIdentifier: 'x', password: 'y' }));
  assert.equal(error.kind, 'rate_limited');
  assert.equal(error.retryAfterSeconds, 30);
});

test('logout clears the cached CSRF token so the next command fetches a fresh one', async () => {
  const { client, calls } = harness(() => new Response(null, { status: 204 }));
  await client.logout();
  await client.logout();
  assert.equal(calls.filter((call) => call.url.endsWith(AUTH_ROUTES.csrf)).length, 2);
});

const MANAGED_USER: ManagedUserRecord = {
  id: USER_ID,
  username: 'maria.perez',
  email: 'maria@univalle.edu',
  firstName: 'Maria',
  lastName: 'Perez',
  secondLastName: null,
  fullName: 'Maria Perez',
  initials: 'MP',
  identityCard: '1234567',
  phoneNumber: '70000000',
  accountStatus: 'active',
  isSuperAdmin: false,
  displayRole: 'Supervisor',
  profilePicture: { etag: '"abc"' },
  mustChangePassword: false,
  memberships: [
    {
      siteId: SITE_ID,
      siteName: 'Sede QA',
      role: 'Supervisor',
      status: 'active',
      effectiveStatus: 'Activo',
      validFrom: null,
      validUntil: null,
      position: 'Tecnica',
      department: null,
      hireDate: '2026-01-15T00:00:00.000Z',
    },
  ],
  createdAt: '2026-09-01T00:00:00.000Z',
  updatedAt: '2026-09-02T00:00:00.000Z',
  createdBy: { userId: SESSION.userId, fullName: 'Administradora QA' },
  modifiedBy: null,
};

test('listUsers builds the F3 query and returns global identity with site memberships', async () => {
  const page: ManagedUserPage = {
    items: [MANAGED_USER],
    totalCount: 1,
    pageIndex: 1,
    totalPages: 1,
    pageSize: 10,
  };
  const { client, last } = harness(() => json({ success: true, data: page }));
  const result = await client.listUsers({
    currentPage: 2,
    statusFilter: 'deleted',
    searchTerm: 'maria',
    siteScope: 'all',
  });
  const url = new URL(last().url);
  assert.equal(url.pathname, USER_ROUTES.users);
  assert.equal(url.searchParams.get('currentPage'), '2');
  assert.equal(url.searchParams.get('statusFilter'), 'deleted');
  assert.equal(url.searchParams.get('searchTerm'), 'maria');
  assert.equal(url.searchParams.get('siteScope'), 'all');
  const [user] = result.items;
  assert.equal(user?.accountStatus, 'active');
  assert.equal(user?.isSuperAdmin, false);
  assert.equal(user?.memberships[0]?.role, 'Supervisor');
  assert.equal(user?.memberships[0]?.effectiveStatus, 'Activo');
});

test('userDetails and createManagedUser use the canonical routes and payloads', async () => {
  const { client, commands } = harness(() => json({ success: true, data: MANAGED_USER }, 201));
  assert.deepEqual(await client.userDetails(USER_ID), MANAGED_USER);
  await client.createManagedUser({
    username: 'maria.perez',
    email: 'maria@univalle.edu',
    firstName: 'Maria',
    lastName: 'Perez',
    identityCard: '1234567',
    phoneNumber: '70000000',
    password: 'Valid-password-1',
    role: 'Supervisor',
  });
  const [details, create] = commands();
  assert.equal(details?.url, `${API}${USER_ROUTES.user(USER_ID)}`);
  assert.equal(create?.url, `${API}${USER_ROUTES.users}`);
  assert.equal(create?.init.method, 'POST');
  assert.equal((bodyOf(create!) as Record<string, unknown>).role, 'Supervisor');
});

test('account and membership commands hit the split F3 routes and accept 204', async () => {
  const { client, commands } = harness((url) =>
    url.endsWith('/memberships')
      ? json({ success: true, data: null }, 201)
      : new Response(null, { status: 204 }),
  );
  await client.adminResetPassword(USER_ID, { password: 'Valid-password-1' });
  await client.restoreAccount(USER_ID);
  await client.deleteAccount(USER_ID);
  await client.addMembership(USER_ID, { siteId: SECOND_SITE, role: 'Administrador' });
  await client.revokeMembership(USER_ID, SITE_ID);
  await client.restoreMembership(USER_ID, SITE_ID, { restoreAccount: true });

  const sent = commands();
  assert.deepEqual(
    sent.map((call) => [call.init.method, call.url.slice(API.length)]),
    [
      ['POST', USER_ROUTES.adminResetPassword(USER_ID)],
      ['POST', USER_ROUTES.restore(USER_ID)],
      ['DELETE', USER_ROUTES.user(USER_ID)],
      ['POST', USER_ROUTES.memberships(USER_ID)],
      ['DELETE', USER_ROUTES.membership(USER_ID, SITE_ID)],
      ['POST', USER_ROUTES.membershipRestore(USER_ID, SITE_ID)],
    ],
  );
  for (const call of sent) assert.equal(header(call, 'X-CSRF-Token'), 'csrf-token');
  // Restore requires a JSON object body even when empty; deletes carry none.
  assert.deepEqual(bodyOf(sent[1]!), {});
  assert.equal(sent[2]?.init.body, undefined);
});

test('status, role, validity, work-profile and global-field updates use PUT routes', async () => {
  const { client, commands } = harness((url) =>
    json({ success: true, data: url.endsWith('/global-fields') ? MANAGED_USER : null }),
  );
  await client.setAccountStatus(USER_ID, { accountStatus: 'inactive', reason: 'baja' });
  await client.changeMembershipRole(USER_ID, SITE_ID, { role: 'Administrador' });
  await client.changeMembershipStatus(USER_ID, SITE_ID, { status: 'suspended' });
  await client.changeMembershipValidity(USER_ID, SITE_ID, {
    validFrom: '2026-01-01',
    validUntil: null,
  });
  await client.updateMembershipWorkProfile(USER_ID, SITE_ID, { position: 'Jefa' });
  const updated = await client.updateUserGlobalFields(USER_ID, { phoneNumber: '71111111' });

  const sent = commands();
  assert.deepEqual(
    sent.map((call) => call.url.slice(API.length)),
    [
      USER_ROUTES.accountStatus(USER_ID),
      USER_ROUTES.membershipRole(USER_ID, SITE_ID),
      USER_ROUTES.membershipStatus(USER_ID, SITE_ID),
      USER_ROUTES.membershipValidity(USER_ID, SITE_ID),
      USER_ROUTES.membershipWorkProfile(USER_ID, SITE_ID),
      USER_ROUTES.globalFields(USER_ID),
    ],
  );
  assert.ok(sent.every((call) => call.init.method === 'PUT'));
  assert.deepEqual(bodyOf(sent[0]!), { accountStatus: 'inactive', reason: 'baja' });
  assert.equal(updated.id, USER_ID);
});

test('users errors keep status, kind, code and message for 403, 404 and 409', async () => {
  const cases = [
    [403, 'ACCESS_DENIED', 'At least one active SuperAdmin must remain.', 'authorization'],
    [404, 'NOT_FOUND', 'User not found.', 'not_found'],
    [409, 'CONFLICT', 'A conflicting record already exists.', 'conflict'],
  ] as const;
  for (const [status, code, message, kind] of cases) {
    const { client } = harness(() => failure(status, code, message));
    const error = await rejection(client.deleteAccount(USER_ID));
    assert.equal(error.status, status);
    assert.equal(error.kind, kind);
    assert.equal(error.code, code);
    assert.equal(error.message, message);
  }
});

test('a restricted or site-less session on a business route is an authorization error', async () => {
  const { client } = harness(() => failure(403, 'SITE_ACCESS_DENIED', 'Site access denied.'));
  const error = await rejection(client.listUsers());
  assert.equal(error.kind, 'authorization');
  assert.equal(error.code, 'SITE_ACCESS_DENIED');
});

test('non-JSON or unexpected success bodies become INVALID_API_RESPONSE, never raw text', async () => {
  const html = harness(() => new Response('<html>stack trace</html>', { status: 500 }));
  const error = await rejection(html.client.userDetails(USER_ID));
  assert.equal(error.code, 'INVALID_API_RESPONSE');
  assert.equal(error.message.includes('stack'), false);

  const noEnvelope = harness(() => json({ items: [] }));
  assert.equal((await rejection(noEnvelope.client.listUsers())).kind, 'invalid_response');

  const commandWithoutEnvelope = harness(() => json({ ok: true }));
  const commandError = await rejection(
    commandWithoutEnvelope.client.setAccountStatus(USER_ID, { accountStatus: 'active' }),
  );
  assert.equal(commandError.kind, 'invalid_response');
});

test('profile photo upload sends raw bytes with MIME, encoded file name and CSRF', async () => {
  const { client, last } = harness(() => json({ success: true, data: { etag: '"new"' } }));
  const bytes = new Uint8Array([0xff, 0xd8, 0xff, 0xe0]);
  const ref = await client.uploadUserPhoto(USER_ID, {
    data: bytes,
    fileName: 'foto perfil.jpg',
    contentType: 'image/jpeg',
  });
  assert.deepEqual(ref, { etag: '"new"' });
  assert.equal(last().url, `${API}${USER_ROUTES.userPhoto(USER_ID)}`);
  assert.equal(last().init.method, 'PUT');
  assert.equal(header(last(), 'Content-Type'), 'image/jpeg');
  assert.equal(header(last(), PROFILE_PHOTO.fileNameHeader), 'foto%20perfil.jpg');
  assert.equal(header(last(), 'X-CSRF-Token'), 'csrf-token');
  assert.deepEqual(new Uint8Array(last().init.body as Uint8Array), bytes);
});

test('profile photo read returns bytes and delete accepts 204', async () => {
  const { client, calls } = harness((_url, init) =>
    init.method === 'DELETE'
      ? new Response(null, { status: 204 })
      : new Response(new Uint8Array([1, 2, 3]), {
          status: 200,
          headers: { 'content-type': 'image/png', etag: '"abc"' },
        }),
  );
  const blob = await client.ownPhoto();
  assert.equal(blob.size, 3);
  assert.equal(calls[0]?.url, `${API}${USER_ROUTES.profilePhoto}`);
  await client.deleteOwnPhoto();
  assert.equal(calls.at(-1)?.url, `${API}${USER_ROUTES.profilePhoto}`);
  assert.equal(calls.at(-1)?.init.method, 'DELETE');
});

test('profile photo validation failures expose the photo violation', async () => {
  const { client } = harness(() =>
    failure(400, 'VALIDATION_ERROR', 'Profile picture rejected.', {
      fieldErrors: { photo: ['signature_mismatch'] },
    }),
  );
  const error = await rejection(
    client.uploadOwnPhoto({
      data: new Uint8Array([1]),
      fileName: 'a.png',
      contentType: 'image/png',
    }),
  );
  assert.deepEqual(error.fieldErrors.photo, ['signature_mismatch']);
  assert.equal(PROFILE_PHOTO.maxBytes, 5 * 1024 * 1024);
  assert.deepEqual([...PROFILE_PHOTO.mimeTypes], ['image/jpeg', 'image/png', 'image/webp']);
});

test('own profile uses /profile and sends only the given self-editable fields', async () => {
  const { client, last } = harness(() => json({ success: true, data: MANAGED_USER }));
  await client.updateOwnProfile({ phoneNumber: '72222222' });
  assert.equal(last().url, `${API}${USER_ROUTES.profile}`);
  assert.equal(last().init.method, 'PUT');
  assert.deepEqual(bodyOf(last()), { phoneNumber: '72222222' });
  await client.ownProfile();
  assert.equal(last().init.method, 'GET');
});

test('people list keeps deleted filtering explicit and person delete accepts data null', async () => {
  const page: PersonPage = { items: [], totalCount: 0, pageIndex: 1, totalPages: 0, pageSize: 10 };
  const { client, commands } = harness((_url, init) =>
    json({ success: true, data: init.method === 'DELETE' ? null : page }),
  );
  await client.people({ statusFilter: 2, searchTerm: 'ana' });
  await client.people();
  await client.deletePerson(7);
  const [filtered, unfiltered, removed] = commands();
  const url = new URL(filtered!.url);
  assert.equal(url.pathname, PERSON_ROUTES.people);
  assert.equal(url.searchParams.get('statusFilter'), '2');
  assert.equal(new URL(unfiltered!.url).search, '');
  assert.equal(removed?.url, `${API}${PERSON_ROUTES.person(7)}`);
  assert.equal(removed?.init.method, 'DELETE');
});

test('apiErrorKind classifies every status F3 emits', () => {
  assert.equal(apiErrorKind(400), 'validation');
  assert.equal(apiErrorKind(401), 'authentication');
  assert.equal(apiErrorKind(403), 'authorization');
  assert.equal(apiErrorKind(404), 'not_found');
  assert.equal(apiErrorKind(409), 'conflict');
  assert.equal(apiErrorKind(413), 'payload_too_large');
  assert.equal(apiErrorKind(415), 'unsupported_media_type');
  assert.equal(apiErrorKind(429), 'rate_limited');
  assert.equal(apiErrorKind(503), 'unavailable');
  assert.equal(apiErrorKind(500), 'unexpected');
  assert.equal(apiErrorKind(200, 'INVALID_API_RESPONSE'), 'invalid_response');
});

test('shared password policy mirrors the F1 rules', () => {
  assert.deepEqual(passwordPolicyViolations('Valid-password-1'), []);
  assert.deepEqual(passwordPolicyViolations('Aa1!Aa1!Aa1'), ['too_short']);
  assert.ok(passwordPolicyViolations('aaaaaaaaaaaa').includes('insufficient_distinct'));
  assert.deepEqual(passwordPolicyViolations('lowercase-only-1'), ['missing_uppercase']);
  assert.deepEqual(passwordPolicyViolations('UPPERCASE-ONLY-1'), ['missing_lowercase']);
  assert.deepEqual(passwordPolicyViolations('No-digits-here!'), ['missing_digit']);
  assert.deepEqual(passwordPolicyViolations('NoSymbolsHere12'), ['missing_symbol']);
  // Code points, not UTF-16 units: 4 + 8 astral symbols = 12 code points.
  assert.deepEqual(passwordPolicyViolations('Aa1!' + '\u{1F600}'.repeat(8)), []);
  // 72 UTF-8 byte bound: 4 + 17 * 4 = 72 bytes is allowed, 76 is not.
  assert.ok(!passwordPolicyViolations('Aa1!' + '\u{1F600}'.repeat(17)).includes('too_long'));
  assert.ok(passwordPolicyViolations('Aa1!' + '\u{1F600}'.repeat(18)).includes('too_long'));
  // No trim: surrounding spaces are counted as characters and symbols.
  assert.deepEqual(passwordPolicyViolations(' Abcdefghij1 '), []);
  assert.equal(PASSWORD_POLICY.maxUtf8Bytes, 72);
});
