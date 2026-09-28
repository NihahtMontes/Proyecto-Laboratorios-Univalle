/**
 * Workstream A — auth, session, password, site-selection and legacy migration.
 *
 * Owns the entire UX of an auth flow against the live NestJS API and Vite
 * React app. Every scenario seeds its own actors; nothing here depends on a
 * sibling test's data or run order.
 *
 * Spanish strings and selector choices were lifted from:
 *   - apps/web/src/App.tsx (login, site picker, forced change, workspace shell)
 *   - apps/web/src/UsersPanel.tsx (PasswordChangeForm on /Profile)
 *   - apps/web/src/apiErrors.ts (rate-limit, validation messages)
 *   - apps/api/src/auth/auth.controller.ts (REST contract)
 *   - apps/api/src/auth/auth.exceptions.ts (error codes)
 *   - apps/api/src/core/core.guard.ts (workspace guard for /users)
 */
import { expect, test } from '../support/fixtures.js';
import {
  apiCall,
  loginViaUi,
  openAuthenticated,
  submitLogin,
} from '../support/auth.js';
import {
  liveSessionCount,
  seedUser,
} from '../support/db.js';
import { stack } from '../support/index.js';
import {
  eligibleSiteNames,
  expectOnLogin,
  expectSitePickerHeading,
  expectSuperAdminWelcome,
  expectWorkspaceHeading,
  logoutFromTopBar,
  readActiveSession,
} from '../support/auth-flows.js';

// ---------------------------------------------------------------------------
// A1: login by USERNAME — single-site Administrador -> dashboard.
// ---------------------------------------------------------------------------

test('A1 login by username as a site-a administrator opens the workspace and the session cookie is HttpOnly', async ({
  page,
  health,
  context,
}) => {
  const admin = await seedUser({
    label: 'a1user',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: admin.username, password: admin.password });

  await expectWorkspaceHeading(page);
  await expect(page.getByRole('button', { name: admin.displayName })).toBeVisible();
  await expect(page.getByRole('button', { name: stack().sites.a.name })).toBeVisible();

  // Session cookie declared HttpOnly + Lax, scoped at "/", never readable from JS.
  const cookies = await context.cookies();
  const sessionCookie = cookies.find((entry) => entry.name.includes('lu_session'));
  expect(sessionCookie, 'session cookie present').toBeTruthy();
  expect(sessionCookie!.httpOnly).toBe(true);
  expect(['Lax', 'Strict']).toContain(sessionCookie!.sameSite);
  expect(sessionCookie!.path).toBe('/');

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A2: login by EMAIL — same kind of user, identifier typed as the email.
// ---------------------------------------------------------------------------

test('A2 login by email routes to the dashboard the same way as a username', async ({
  page,
  health,
}) => {
  const admin = await seedUser({
    label: 'a2mail',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: admin.email, password: admin.password });
  await expectWorkspaceHeading(page);
  await expect(page.getByRole('button', { name: admin.displayName })).toBeVisible();

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A3: wrong password — generic alert, no movement, /auth/session stays 401.
// ---------------------------------------------------------------------------

test('A3 wrong password renders the generic rejection and keeps the session API unauthorised', async ({
  page,
  health,
}) => {
  const admin = await seedUser({
    label: 'a3wrong',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');
  await submitLogin(page, { loginIdentifier: admin.username, password: 'TOTALLY-WRONG-pw#1' });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);

  const session = await readActiveSession(page);
  expect(session.status).toBe(401);
  // Two 4xx responses: the bootstrap GET /auth/session (anonymous) and the
  // failed POST /auth/login with the wrong password. Both are 401 INVALID_CREDENTIALS.
  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A5: active non-SuperAdmin account with NO memberships -> rejected. The
// server maps `no_eligible_site` to the generic 401 INVALID_CREDENTIALS
// (`auth.service.ts: authenticate -> fail(...)`) so the UI surfaces the same
// "Usuario o contraseña incorrectos." string as every other credential
// failure — that's deliberate account enumeration protection; we additionally
// probe `/auth/session` to confirm the server-side rejection (401) sticks.
// ---------------------------------------------------------------------------

test('A5 an active account without memberships cannot reach the workspace', async ({
  page,
  health,
}) => {
  const orphan = await seedUser({ label: 'a5orphan', memberships: [] });

  await page.goto('/');
  await submitLogin(page, { loginIdentifier: orphan.username, password: orphan.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);

  // The bootstrap probe (GET /auth/session anonymous) and the failed
  // credential attempt (POST /auth/login) both produce 401s we expect.
  const session = await readActiveSession(page);
  expect(session.status).toBe(401);
  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A6: session bootstrap — after a successful login, a reload keeps the shell.
// ---------------------------------------------------------------------------

test('A6 a reload on the workspace preserves the authenticated shell', async ({
  page,
  health,
}) => {
  const admin = await seedUser({
    label: 'a6reload',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  await openAuthenticated(page, { loginIdentifier: admin.username, password: admin.password });

  await expectWorkspaceHeading(page);
  await page.reload();
  await expectWorkspaceHeading(page);
  // Login form must not reappear.
  await expect(page.getByRole('button', { name: 'INICIAR SESIÓN' })).toHaveCount(0);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A7: logout from the user menu — returns to login, page reload on login is
// stable and the server-side session is gone.
// ---------------------------------------------------------------------------

test('A7 logout from the user menu clears the server-side session and survives a reload', async ({
  page,
  health,
}) => {
  const admin = await seedUser({
    label: 'a7logout',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  await openAuthenticated(page, { loginIdentifier: admin.username, password: admin.password });
  await expectWorkspaceHeading(page);

  await logoutFromTopBar(page, admin.displayName);
  await page.reload();
  await expectOnLogin(page);

  const session = await readActiveSession(page);
  expect(session.status).toBe(401);

  expect
    .poll(() => liveSessionCount(admin.id), { message: 'liveSessionCount after logout' })
    .toBe(0);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A8a: expired session — login, expire sessions server-side, click something
// that calls the API -> the shell disappears and the session-expired message
// is rendered (inline alert; no global toast).
// ---------------------------------------------------------------------------

test('A8a an expired server-side session bounces the UI back to the login view with the expirato message', async ({
  page,
  health,
}) => {
  const { expireSessions } = await import('../support/db.js');
  const admin = await seedUser({
    label: 'a8exp',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  await openAuthenticated(page, { loginIdentifier: admin.username, password: admin.password });
  await expectWorkspaceHeading(page);

  await expireSessions(admin.id);

  // Trigger the SessionGuard from INSIDE the app (fact 6): a raw apiCall
  // fetch is outside the SPA's session-guarded proxy, so the SPA would never
  // notice the 401 and would keep showing the dashboard. Clicking the
  // sidebar "Usuarios" link navigates within the SPA to UsersPanel, whose
  // mount-time listUsers() hits the now-expired /users endpoint, and the
  // SessionGuard resets identity with the canonical "Su sesión expiró…"
  // inline alert.
  const mainNav = page.getByRole('navigation', { name: 'Navegación principal' });
  await mainNav.getByRole('button', { name: 'Usuarios' }).click();
  await mainNav.getByRole('link', { name: 'Usuarios' }).click();

  await expect(page.getByRole('alert')).toHaveText(
    'Su sesión expiró o fue revocada. Inicie sesión nuevamente.',
  );
  await expectOnLogin(page);

  // The bootstrap probe before login and after the guard reset is an expected 401.
  health.expectClean([/GET \/api\/v1\/users 401/, /GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A8b: revoked session — same UI bounce after server-side revoke.
// ---------------------------------------------------------------------------

test('A8b a server-side revoked session also returns the user to the login view', async ({
  page,
  health,
}) => {
  const { revokeSessions } = await import('../support/db.js');
  const admin = await seedUser({
    label: 'a8rev',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  await openAuthenticated(page, { loginIdentifier: admin.username, password: admin.password });
  await expectWorkspaceHeading(page);

  await revokeSessions(admin.id);
  // Same in-app trigger as A8a — a raw apiCall fetch bypasses the SessionGuard.
  const mainNav = page.getByRole('navigation', { name: 'Navegación principal' });
  await mainNav.getByRole('button', { name: 'Usuarios' }).click();
  await mainNav.getByRole('link', { name: 'Usuarios' }).click();

  await expect(page.getByRole('alert')).toHaveText(
    'Su sesión expiró o fue revocada. Inicie sesión nuevamente.',
  );
  await expectOnLogin(page);

  // The bootstrap probe before login and after the guard reset is an expected 401.
  // After revokeSessions the workspace boot requests that were still in flight or
  // fire afterwards correctly answer 401 (expected server-side revoked-session behavior).
  health.expectClean([
    /GET \/api\/v1\/users 401/,
    /GET \/api\/v1\/auth\/session 401/,
    /GET \/api\/v1\/notifications 401/,
    /GET \/api\/v1\/dashboard 401/,
    /GET \/api\/v1\/profile 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A9: multi-site user — site-selection listing, choice, switch via top-bar,
// reload preserves the switched site, and the session API confirms it.
// ---------------------------------------------------------------------------

test('A9 a multi-site user can pick a site, switch later via the top bar, and reload keeps the new site', async ({
  page,
  health,
}) => {
  const multi = await seedUser({
    label: 'a9multi',
    memberships: [
      { site: 'a', role: 'Administrador' },
      { site: 'b', role: 'Supervisor' },
    ],
  });

  await page.goto('/');
  // Multi-site users land on the site picker (no workspace shell), so
  // loginViaUi would time out waiting for the navigation that is not
  // rendered. submitLogin + expectSitePickerHeading is the right sequence.
  await submitLogin(page, { loginIdentifier: multi.username, password: multi.password });
  await expectSitePickerHeading(page);

  // Both eligible site names render inside the picker cards (App.tsx SiteChoices).
  for (const name of eligibleSiteNames(stack())) {
    await expect(page.getByRole('button', { name: new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) })).toBeVisible();
  }
  const presentNames = await page
    .getByRole('button')
    .filter({ has: page.getByText(/Sede/) })
    .allTextContents();
  // Two cards and only two cards (no extra copy button on this view).
  const renderedNames = presentNames.filter((value) => /Sede/.test(value));
  expect(renderedNames.length).toBe(eligibleSiteNames(stack()).length);

  // Choose site A.
  await page
    .getByRole('button', { name: new RegExp(stack().sites.a.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) })
    .click();
  await expectWorkspaceHeading(page);
  await expect(
    page.getByRole('button', { name: stack().sites.a.name }),
  ).toBeVisible();

  // Switch via the top-bar site dropdown to site B.
  await page.getByRole('button', { name: stack().sites.a.name }).click();
  await page
    .getByRole('button', { name: new RegExp(stack().sites.b.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) })
    .click();
  await expect(page.getByRole('button', { name: stack().sites.b.name })).toBeVisible();

  // Server check: the session API reports purpose=normal and active site B.
  const switched = await readActiveSession(page);
  expect(switched.status).toBe(200);
  expect(switched.body?.meta?.purpose).toBe('normal');
  const activeSiteBody = switched.body as unknown as {
    readonly data: { readonly activeSiteId: string | null };
  } | null;
  expect(activeSiteBody?.data?.activeSiteId).toBe(stack().sites.b.id);

  await page.reload();
  await expectWorkspaceHeading(page);
  await expect(page.getByRole('button', { name: stack().sites.b.name })).toBeVisible();

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A10: restricted site-selection session cannot reach workspace routes — the
// page still shows the site picker, and /users is refused by the server.
// ---------------------------------------------------------------------------

test('A10 a site-selection session cannot escape to workspace routes: the picker is shown and /users is refused', async ({
  page,
  health,
}) => {
  const { loginViaApi } = await import('../support/auth.js');
  const multi = await seedUser({
    label: 'a10sel',
    memberships: [
      { site: 'a', role: 'Administrador' },
      { site: 'b', role: 'Supervisor' },
    ],
  });

  // Open the app once so we have a base origin, then login via the API and
  // request NO active site — we land in the restricted site-selection flow.
  await page.goto('/');
  const result = await loginViaApi(page, {
    loginIdentifier: multi.username,
    password: multi.password,
  });
  expect(result.status).toBe(200);
  expect(result.purpose).toBe('site_selection');

  // After loginViaApi the already-rendered SPA does not know about the new
  // cookie (fact 1). page.goto('/Users/Index') forces the SPA to bootstrap
  // again — the SessionGuard then routes a site_selection purpose to the
  // picker, which is the real proof that a restricted session cannot reach
  // the workspace.
  await page.goto('/Users/Index');
  await expectSitePickerHeading(page);
  await expect(page.getByRole('button', { name: 'INICIAR SESIÓN' })).toHaveCount(0);

  // Server-side enforcement probe: /users refuses a site_selection purpose.
  // core.guard.ts -> AuthForbiddenException(SITE_ACCESS_DENIED) on a restricted
  // purpose at any business route. That is a 403.
  const probe = await apiCall(page, 'GET', '/users');
  expect(probe.status).toBe(403);

  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /GET \/api\/v1\/users 403/,
  ]);
});

// ---------------------------------------------------------------------------
// A11a: SuperAdmin with NO memberships -> global workspace with the welcome
// heading; tenant-only nav items are absent.
// ---------------------------------------------------------------------------

test('A11a a SuperAdmin with no memberships sees the global welcome and the workspace, with no tenant items', async ({
  page,
  health,
}) => {
  const root = await seedUser({ label: 'a11root', superAdmin: true });
  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: root.username, password: root.password });
  await expectWorkspaceHeading(page);
  await expectSuperAdminWelcome(page, root.firstName);

  const nav = page.getByRole('navigation', { name: 'Navegación principal' });
  await expect(nav).toBeVisible();
  await expect(nav).toContainText('Usuarios');
  // Tenant-only admin items must not appear for a global SuperAdmin with no
  // memberships (App.tsx isLinkAllowed: requires hasSite for non-global links).
  await expect(nav).not.toContainText('Equipo');
  await expect(nav).not.toContainText('Gestiones');

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A11b: SuperAdmin WITH memberships -> site-selection (purpose site_selection,
// not the global welcome).
// ---------------------------------------------------------------------------

test('A11b a SuperAdmin who also has site memberships must go through the site picker first', async ({
  page,
  health,
}) => {
  const root = await seedUser({
    label: 'a11rootm',
    superAdmin: true,
    memberships: [
      { site: 'a', role: 'Administrador' },
      { site: 'b', role: 'Supervisor' },
    ],
  });
  await page.goto('/');
  // SuperAdmin + memberships -> restricted site_selection purpose -> picker
  // (no workspace shell). loginViaUi would time out waiting for the
  // navigation that is not rendered. submitLogin + expectSitePickerHeading
  // is the right sequence.
  await submitLogin(page, { loginIdentifier: root.username, password: root.password });
  await expectSitePickerHeading(page);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A18: F8 hardening — the deprecated `{ email, password }` login body alias
// (carried over from the F4/F5 contract) is removed. POST /auth/login with
// that shape must answer 400 with the canonical "Unknown keys are not
// accepted" validation message. The canonical `{ loginIdentifier, password }`
// shape (already covered by A1/A2) keeps working — both the username and
// the email identifier forms.
// ---------------------------------------------------------------------------

test('A18 the deprecated {email, password} login body alias is removed: 400 + canonical bodies keep working', async ({
  page,
  health,
}) => {
  const admin = await seedUser({
    label: 'a18alias',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');

  // 1) Deprecated body: { email, password } -> 400.
  const deprecated = await apiCall(page, 'POST', '/auth/login', {
    email: admin.email,
    password: admin.password,
  });
  expect(deprecated.status, 'deprecated {email,password} login body').toBe(400);
  const errorCode = (deprecated.body as { error?: { code?: string } } | null)?.error?.code;
  expect(errorCode, 'wire-level error code').toBe('VALIDATION_ERROR');
  expect(JSON.stringify(deprecated.body)).toContain(
    'Unknown keys are not accepted',
  );

  // 2) Canonical body still works with the username identifier (A1 re-coverage).
  const byUsername = await apiCall(page, 'POST', '/auth/login', {
    loginIdentifier: admin.username,
    password: admin.password,
  });
  expect(byUsername.status, 'loginIdentifier with username').toBe(200);

  // 3) Canonical body still works with the email identifier (A2 re-coverage).
  // Open a fresh context so the first successful login does not reuse its
  // session cookie for this probe.
  const fresh = await page.context().browser()!.newContext();
  const freshPage = await fresh.newPage();
  await freshPage.goto('/');
  const byEmail = await apiCall(freshPage, 'POST', '/auth/login', {
    loginIdentifier: admin.email,
    password: admin.password,
  });
  expect(byEmail.status, 'loginIdentifier with email').toBe(200);
  await fresh.close();

  health.expectClean([
    /POST \/api\/v1\/auth\/login 400/,
    /GET \/api\/v1\/auth\/session 401/,
  ]);
});
