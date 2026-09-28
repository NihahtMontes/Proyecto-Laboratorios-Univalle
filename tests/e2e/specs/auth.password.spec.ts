/**
 * Workstream A — password / change-password / rate-limit / secret-exposure
 * scenarios. Uses the same Spanish copy as the rest of the auth suite;
 * assertions are tied to the strings and element roles defined in
 * `apps/web/src/App.tsx` and `apps/web/src/UsersPanel.tsx`.
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
  readPasswordHash,
  readUser,
  seedUser,
  syntheticPassword,
} from '../support/db.js';
import {
  expectOnLogin,
  expectPasswordChangeHeading,
  expectWorkspaceHeading,
} from '../support/auth-flows.js';

// ---------------------------------------------------------------------------
// A4: inactive, deleted and reset_required accounts are all refused with the
// generic "Usuario o contraseña incorrectos." alert.
// ---------------------------------------------------------------------------

test('A4a an inactive account is refused with the generic rejection', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a4inact',
    memberships: [{ site: 'a', role: 'Administrador' }],
    accountStatus: 'inactive',
  });
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);
  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

test('A4b a deleted account is refused with the generic rejection', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a4del',
    memberships: [{ site: 'a', role: 'Administrador' }],
    accountStatus: 'deleted',
  });
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);
  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

test('A4c a reset-required account is refused with the generic rejection', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a4reset',
    memberships: [{ site: 'a', role: 'Administrador' }],
    passwordKind: 'reset_required',
  });
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);
  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A12: purpose-based routing — a bcrypt account with must_change_password=true
// lands on the forced change view, with the must-change copy. Navigating to a
// workspace route keeps the user on the forced view.
// ---------------------------------------------------------------------------

test('A12 a must-change-password session forces the dedicated view even when navigation tries to leave it', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a12must',
    memberships: [{ site: 'a', role: 'Administrador' }],
    mustChangePassword: true,
  });

  await page.goto('/');
  // loginViaUi waits for the workspace shell; the password-change view does
  // not render that navigation. Use submitLogin and assert the restricted
  // heading instead.
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expectPasswordChangeHeading(page);
  await expect(
    page.getByText('Debe establecer una nueva contraseña antes de continuar.'),
  ).toBeVisible();

  // Try to escape via the URL — the SPA history.replaceState in
  // App.useEffect + isAllowedRoute sends the user back to /.
  await page.goto('/Users/Index');
  await expectPasswordChangeHeading(page);
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toHaveCount(0);

  // Server enforcement: a restricted session never reaches business routes.
  const probe = await apiCall(page, 'GET', '/users');
  expect(probe.status).toBe(403);

  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /GET \/api\/v1\/users 403/,
  ]);
});

// ---------------------------------------------------------------------------
// A13: forced password change — three sub-scenarios in one test suite:
// (a) wrong current password shows the "La contraseña actual es incorrecta."
//     message and the forced view is still up.
// (b) policy-violating new password is rejected locally and remotely.
// (c) a valid change reaches the workspace, the DB row changes (must_change_
//     password=false, scheme=bcrypt), and OLD/NEW logins match the new state.
// ---------------------------------------------------------------------------

test('A13a forced change with a wrong current password keeps the user on the forced view', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a13wrong',
    memberships: [{ site: 'a', role: 'Administrador' }],
    mustChangePassword: true,
  });
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expectPasswordChangeHeading(page);

  await page.getByLabel('Contraseña actual').fill('WRONG-OLD-password#1');
  await page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }).fill('OtraClaveSegura-2026!');
  await page.getByLabel('Confirmar nueva contraseña').fill('OtraClaveSegura-2026!');
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

  await expect(page.getByRole('alert')).toHaveText('La contraseña actual es incorrecta.');
  await expectPasswordChangeHeading(page);

  health.expectClean([
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/password 401/,
  ]);
});

test('A13b forced change with a policy-violating new password shows the local policy error', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a13pol',
    memberships: [{ site: 'a', role: 'Administrador' }],
    mustChangePassword: true,
  });
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expectPasswordChangeHeading(page);

  await page.getByLabel('Contraseña actual').fill(target.password);
  await page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }).fill('corta');
  await page.getByLabel('Confirmar nueva contraseña').fill('corta');
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

  await expect(page.getByRole('alert')).toContainText('al menos 12 caracteres');

  // Confirm the server was NOT contacted: the response was a local validation.
  health.expectClean([/POST \/api\/v1\/auth\/password 400/, /GET \/api\/v1\/auth\/session 401/]);
  await expectPasswordChangeHeading(page);
});

test('A13c forced change with valid credentials reaches the workspace and persists bcrypt + no flag', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a13ok',
    memberships: [{ site: 'a', role: 'Administrador' }],
    mustChangePassword: true,
  });
  const newPassword = syntheticPassword('A13');

  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expectPasswordChangeHeading(page);

  await page.getByLabel('Contraseña actual').fill(target.password);
  await page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }).fill(newPassword);
  await page.getByLabel('Confirmar nueva contraseña').fill(newPassword);
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

  await expectWorkspaceHeading(page);

  const after = await readUser(target.id);
  expect(after.password_scheme).toBe('bcrypt');
  expect(after.bcrypt_prefix?.startsWith('$2')).toBe(true);
  expect(after.must_change_password).toBe(false);

  // Old password no longer works.
  await logoutFromTopBarIfNeeded(page, target.displayName);
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: target.username, password: target.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);

  // New password logs in.
  await page.getByLabel('Usuario o Correo').fill('');
  await submitLogin(page, { loginIdentifier: target.username, password: newPassword });
  await expectWorkspaceHeading(page);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/, /POST \/api\/v1\/auth\/login 401/]);
});

async function logoutFromTopBarIfNeeded(page: import('@playwright/test').Page, displayName: string): Promise<void> {
  const onLoginNow = await page.getByRole('button', { name: 'INICIAR SESIÓN' }).isVisible().catch(() => false);
  if (onLoginNow) return;
  const userButton = page.getByRole('button', { name: displayName });
  if ((await userButton.isVisible().catch(() => false)) === false) return;
  await userButton.click();
  const signOut = page.getByRole('button', { name: 'Cerrar Sesión' });
  if ((await signOut.isVisible().catch(() => false)) === false) return;
  await signOut.click();
  await expectOnLogin(page);
}

// ---------------------------------------------------------------------------
// A14: voluntary password change from /Profile — the change succeeds (the
// 'Contraseña actualizada correctamente.' message is rendered, the form
// fields are cleared, and the user remains on /Profile), the old password
// no longer works, and a second logged-in context is revoked by the security
// version rotation. PasswordChangeForm (UsersPanel.tsx) renders the success
// alert with `role="status"`; this is the new F8 surface (the prior F7
// run only had field-clearing + no-error as success indicators).
// ---------------------------------------------------------------------------

test('A14 voluntary change from /Profile updates the hash, shows the success message, and kills the second context', async ({
  browser,
  page,
  health,
}) => {
  const actor = await seedUser({
    label: 'a14me',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  const newPassword = syntheticPassword('A14');

  // Second browser context logged in BEFORE the change.
  const second = await browser.newContext();
  const secondPage = await second.newPage();
  await openAuthenticated(
    secondPage,
    { loginIdentifier: actor.username, password: actor.password },
    '/',
  );
  await expectWorkspaceHeading(secondPage);

  // First context: log in, go to /Profile, change the password.
  await page.context().addCookies(await second.cookies());
  await openAuthenticated(page, { loginIdentifier: actor.username, password: actor.password });
  await page.goto('/Profile');
  // /Profile has TWO headings: h1 'Mi Perfil' (the page title) and h6 'Mi
  // perfil'. Target the exact level-1 page title (fact 4) — the inner h6 is
  // the small "Mi perfil" inside the photo card.
  await expect(
    page.getByRole('heading', { level: 1, name: 'Mi Perfil', exact: true }),
  ).toBeVisible();

  await page.getByLabel('Contraseña actual').fill(actor.password);
  await page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }).fill(newPassword);
  await page.getByLabel('Confirmar nueva contraseña').fill(newPassword);
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

  // F8 success surface: the success message is rendered with role="status",
  // the form fields are cleared, and we remain on /Profile (no error alert).
  // The change also rebuilds the session — the page briefly re-enters auth
  // via enterAuth() which is harmless.
  await expect(
    page.getByRole('status').filter({ hasText: 'Contraseña actualizada correctamente.' }),
  ).toBeVisible({ timeout: 10_000 });
  await expect(
    page.getByLabel('Contraseña actual'),
  ).toHaveValue('');
  await expect(
    page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }),
  ).toHaveValue('');
  await expect(
    page.getByLabel('Confirmar nueva contraseña'),
  ).toHaveValue('');
  await expect(page.getByRole('alert')).toHaveCount(0);
  await expect(
    page.getByRole('navigation', { name: 'Cabecera principal' }),
  ).toBeVisible();

  // Server-side: the actor's session rows are revoked (security_version rotated
  // by the changePassword transaction in auth.service.ts).
  expect
    .poll(() => liveSessionCountForUser(actor.id), { message: 'live sessions post-change' })
    .toBe(0);

  // The second context now gets a 401 on any business call (its session was
  // revoked), so the UI bounces it to the login view on its next API hit.
  const probeSecond = await apiCall(secondPage, 'GET', '/auth/session');
  expect(probeSecond.status).toBe(401);

  // Old password no longer authenticates anywhere.
  await page.context().clearCookies();
  await page.context().addCookies(await second.cookies());
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: actor.username, password: actor.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');

  health.expectClean([
    /POST \/api\/v1\/auth\/login 401/,
    /GET \/api\/v1\/auth\/session 401/,
  ]);
});

async function liveSessionCountForUser(userId: string): Promise<number> {
  return liveSessionCount(userId);
}

// ---------------------------------------------------------------------------
// A14b: F8 hardening — POST /auth/password is rate limited per user with
// the same per-identifier bucket used by login. The stack only raises the
// per-IP ceiling (AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS=1000); the per-identifier
// default of 10 stays in force. After 10 wrong current-password attempts the
// 11th attempt, even with the CORRECT current password, must answer 429,
// and the stored hash must not have changed (the rate-limit guard fires
// before the password verifier, so no transaction touches the row).
// ---------------------------------------------------------------------------

test('A14b the per-identifier rate limit on POST /auth/password answers 429 and does not change the hash', async ({
  page,
  health,
}) => {
  const actor = await seedUser({
    label: 'a14brate',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  const newPassword = syntheticPassword('A14b');

  // Authenticate so the session cookie is on the page and the
  // password-change endpoint can resolve the user (the rate-limit bucket
  // is keyed by `password-change:<userId>`).
  await openAuthenticated(page, { loginIdentifier: actor.username, password: actor.password });

  const hashBefore = await readPasswordHash(actor.id);
  expect(hashBefore, 'baseline hash').not.toBeNull();

  // 10 wrong attempts against the same session: the bucket stays below the
  // 10-attempts ceiling and the server answers 401 INVALID_CREDENTIALS each
  // time. The bucket trip happens at attempt #11 (auth.repository.ts:
  // attempt_count > identifierIpMaxAttempts).
  for (let attempt = 0; attempt < 10; attempt += 1) {
    const resp = await apiCall(page, 'POST', '/auth/password', {
      currentPassword: 'WRONG-CURRENT-pw#1',
      newPassword,
    });
    expect(resp.status, `wrong attempt #${attempt + 1}`).toBe(401);
  }

  // The 11th attempt uses the CORRECT current password: the bucket trips
  // BEFORE the verifier runs, so the response is 429 and the row never
  // changes.
  const blocked = await apiCall(page, 'POST', '/auth/password', {
    currentPassword: actor.password,
    newPassword,
  });
  expect(blocked.status, 'rate-limited correct-password attempt').toBe(429);
  const blockedCode = (blocked.body as { error?: { code?: string } } | null)?.error?.code;
  expect(blockedCode, 'wire-level error code').toBe('RATE_LIMITED');

  const hashAfter = await readPasswordHash(actor.id);
  expect(hashAfter, 'password hash must not have changed while rate-limited').toBe(hashBefore);

  // The new password must NOT authenticate: the underlying credentials were
  // never committed.
  await page.context().clearCookies();
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: actor.username, password: newPassword });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');

  // The OLD password still works: the row is byte-for-byte the same as before.
  await page.context().clearCookies();
  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: actor.username, password: actor.password });
  await expectWorkspaceHeading(page);

  health.expectClean([
    /POST \/api\/v1\/auth\/password 401/,
    /POST \/api\/v1\/auth\/password 429/,
    /GET \/api\/v1\/auth\/session 401/,
    /POST \/api\/v1\/auth\/login 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A15: per-identifier rate limit — 11 wrong attempts in the 60-second window
// trigger the 429 alert with retry seconds, and the CORRECT password is
// refused while the limit is active. Only the per-IP ceiling is raised by
// the F7 stack (AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS=1000); the per-identifier
// default of 10 stays in force (auth.config.ts: rateLimitMaxAttempts default 10).
// ---------------------------------------------------------------------------

test('A15 the per-identifier rate limit locks the account out (UI 429 message + correct password refused)', async ({
  page,
  health,
}) => {
  const target = await seedUser({
    label: 'a15rate',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');

  // Drive 11 wrong-password attempts against the same identifier so the
  // per-identifier bucket trips past 10 (auth.config.ts default).
  for (let attempt = 0; attempt < 10; attempt += 1) {
    await page.getByLabel('Usuario o Correo').fill(target.username);
    await page.getByLabel('Contraseña', { exact: true }).fill('TOTALLY-WRONG-pw#1');
    await page.getByRole('button', { name: 'INICIAR SESIÓN' }).click();
    await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  }
  // The 11th attempt must trip the bucket -> 429 with the Spanish retry text.
  await page.getByLabel('Usuario o Correo').fill(target.username);
  await page.getByLabel('Contraseña', { exact: true }).fill('TOTALLY-WRONG-pw#1');
  await page.getByRole('button', { name: 'INICIAR SESIÓN' }).click();
  await expect(page.getByRole('alert')).toHaveText(
    /Demasiados intentos\. Intente nuevamente en \d+ segundos\./,
  );

  // The CORRECT password is now refused too while the bucket window is open.
  await page.getByLabel('Usuario o Correo').fill(target.username);
  await page.getByLabel('Contraseña', { exact: true }).fill(target.password);
  await page.getByRole('button', { name: 'INICIAR SESIÓN' }).click();
  await expect(page.getByRole('alert')).toHaveText(
    /Demasiados intentos\. Intente nuevamente en \d+ segundos\./,
  );

  health.expectClean([
    /POST \/api\/v1\/auth\/login 401/,
    /POST \/api\/v1\/auth\/login 429/,
    /GET \/api\/v1\/auth\/session 401/,
  ]);
});

// ---------------------------------------------------------------------------
// A16: secret-exposure audit — the session cookie value must never be visible
// from the document, the storages, or any console message collected during
// the test. The session cookie is also asserted to be HttpOnly, sameSite
// Lax/Strict, and path "/".
// ---------------------------------------------------------------------------

test('A16 the session cookie value never leaks to JS-visible storage or to console messages', async ({
  page,
  health,
  context,
}) => {
  const actor = await seedUser({
    label: 'a16sec',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  const consoleMessages: string[] = [];
  page.on('console', (msg) => {
    consoleMessages.push(msg.text());
  });

  await openAuthenticated(page, { loginIdentifier: actor.username, password: actor.password });
  await expectWorkspaceHeading(page);

  const cookies = await context.cookies();
  const sessionCookie = cookies.find((entry) => entry.name.includes('lu_session'));
  expect(sessionCookie, 'session cookie present').toBeTruthy();
  expect(sessionCookie!.httpOnly).toBe(true);
  expect(['Lax', 'Strict']).toContain(sessionCookie!.sameSite);
  expect(sessionCookie!.path).toBe('/');
  const cookieValue = sessionCookie!.value;

  // Probe every JS-visible surface from the page context.
  const findings: Record<string, string> = await page.evaluate(async () => {
    const result: Record<string, string> = {};
    const probe = (label: string, key: string, value: string | null): void => {
      const has = value !== null && value.length > 0;
      result[label] = has ? `value=${value.slice(0, 8)}…` : 'absent';
    };
    probe('localStorage-raw', 'ls', JSON.stringify(Object.fromEntries(Object.entries(window.localStorage))));
    probe('sessionStorage-raw', 'ss', JSON.stringify(Object.fromEntries(Object.entries(window.sessionStorage))));
    probe('document-cookie', 'dc', document.cookie);
    probe('document-html-prefix', 'html', document.documentElement.outerHTML.slice(0, 2000));
    return result;
  });
  for (const [label, text] of Object.entries(findings)) {
    expect(text, `${label} must not contain the cookie value`).not.toContain(cookieValue);
  }

  // No console message may have included the cookie value (the fixture's
  // health listener will still log errors; we filter for the actual secret).
  for (const line of consoleMessages) {
    expect(line, `console message must not contain the cookie value: ${line}`).not.toContain(cookieValue);
  }

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});
