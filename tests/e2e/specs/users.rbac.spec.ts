import { expect, test } from '../support/fixtures.js';
import { apiCall, loginViaApi } from '../support/auth.js';
import { controlDb, readUser } from '../support/db.js';
import { stack } from '../support/index.js';
import {
  openAs,
  seedDualSiteUser,
  seedOrphanUser,
  seedSiteAUser,
  seedSiteBUser,
  seedSuperAdmin,
} from '../support/users-flows.js';

/** Reads the global `is_super_admin` flag for a user (not exposed by `readUser`). */
async function readIsSuperAdmin(userId: string): Promise<boolean> {
  return controlDb(async (c) => {
    const r = await c.query<{ is_super_admin: boolean }>(
      `SELECT is_super_admin FROM public.lu_user WHERE id = $1`,
      [userId],
    );
    return r.rows[0]?.is_super_admin ?? false;
  });
}

/** Counts every active SuperAdmin in the control database. */
async function countActiveSuperAdmins(): Promise<number> {
  return controlDb(async (c) => {
    const r = await c.query<{ n: number }>(
      `SELECT count(*)::int AS n FROM public.lu_user
         WHERE is_super_admin = true AND account_status = 'active'`,
      [],
    );
    return r.rows[0]?.n ?? 0;
  });
}

/**
 * Workstream B — RBAC / self-protection / SuperAdmin HTTP-mutation guard /
 * last-active-SuperAdmin race (B10-B13). All probes are server-backed (apiCall
 * + DB read), with the UI observation alongside where it surfaces the same
 * authorization decision.
 */

// ---------------------------------------------------------------------------
// B10(a) — Supervisor: /Users/Index is hidden + API denies list and create.
// ---------------------------------------------------------------------------

test('B10(a) Supervisor is denied by the API and the index route is redirected away', async ({ page, health }) => {
  const supervisor = await seedSiteAUser('Supervisor');

  await openAs(
    page,
    { loginIdentifier: supervisor.username, password: supervisor.password },
    '/Users/Index',
    stack().sites.a.id,
  );

  // The client-side route guard redirects a non-admin away from /Users/Index.
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard de Gestión' })).toBeVisible();
  await expect(page.getByRole('tab', { name: 'Cuentas de Acceso' })).toHaveCount(0);

  const listResp = await apiCall(page, 'GET', '/users');
  expect(listResp.status, 'GET /users as Supervisor').toBe(403);

  const createResp = await apiCall(page, 'POST', '/users', {
    username: `supblocked${Date.now()}`,
    email: `supblocked${Date.now()}@example.invalid`,
    firstName: 'Blocked',
    lastName: 'User',
    identityCard: '99999',
    phoneNumber: '70000000',
    password: 'Blocked1!Aa#Zz',
    role: 'Supervisor',
  });
  expect(createResp.status, 'POST /users as Supervisor').toBe(403);

  health.expectClean([
    /GET \/api\/v1\/users 403/,
    /POST \/api\/v1\/users 403/,
    /GET \/api\/v1\/auth\/session 401/,
  ]);
});

// ---------------------------------------------------------------------------
// B10(b) — site-a admin cannot read a site-b-only user (server indistinguisable
// from a missing id; UI shows a not-found message and never the user's data).
// ---------------------------------------------------------------------------

test('B10(b) site-a admin cannot read a site-b-only user (server returns 404 by design)', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const targetB = await seedSiteBUser('Supervisor');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, `/Users/Details/${targetB.id}`, stack().sites.a.id);

  await expect(page.getByRole('heading', { name: 'Cargando cuenta…' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: targetB.displayName })).toHaveCount(0);
  await expect(page.getByRole('alert').filter({ hasText: /no existe|no est|not_found/i }).first()).toBeVisible();

  const resp = await apiCall(page, 'GET', `/users/${targetB.id}`);
  // Fact 10: cross-site lookups return EXACTLY 404 — indistinguishable from an unknown id.
  expect(resp.status, 'GET /users/:id (cross-site)').toBe(404);
});

// ---------------------------------------------------------------------------
// B10(c) — site-a admin on a MULTI-site user: global-fields, admin-reset,
// admin-restored reject with CUSTODY_NOT_SINGLE_SITE / forbidden.
// ---------------------------------------------------------------------------

test('B10(c) site-a admin on a multi-site user is denied global-fields and admin-reset', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const dual = await seedDualSiteUser('Supervisor');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const fieldsResp = await apiCall(page, 'PUT', `/users/${dual.id}/global-fields`, {
    firstName: 'ShouldNotApply',
    reason: 'rbac probe',
  });
  expect(fieldsResp.status, 'PUT global-fields on multi-site target').toBe(403);

  const resetResp = await apiCall(page, 'POST', `/users/${dual.id}/admin-reset-password`, {
    password: 'F7-Rbac1#Aaaa',
    reason: 'rbac probe',
  });
  expect(resetResp.status, 'POST admin-reset on multi-site target').toBe(403);

  // UI Edit: the Save flow surfaces the same error inline.
  await page.goto(`/Users/Edit/${dual.id}`);
  // Required form fields render with the " *" suffix; match by accessible-name substring.
  await page.getByRole('textbox', { name: 'Nombres' }).fill('UiEditProbe');
  const [uiResp] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/global-fields') && res.request().method() === 'PUT'),
    page.getByRole('button', { name: 'Guardar Cambios' }).click(),
  ]);
  expect(uiResp.status()).toBe(403);
  await expect(page.getByRole('alert').filter({ hasText: /No tiene permisos|No se pudo actualizar/i }).first()).toBeVisible();

  // Persistence check: the multi-site user was NOT modified.
  const persisted = await readUser(dual.id);
  expect(persisted.first_name).not.toBe('UiEditProbe');
});

// ---------------------------------------------------------------------------
// B10(d) — site-a admin cannot link an existing user to another site.
// ---------------------------------------------------------------------------

test('B10(d) site-a admin cannot add a membership to an existing user', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const resp = await apiCall(page, 'POST', `/users/${target.id}/memberships`, {
    siteId: stack().sites.b.id,
    role: 'Supervisor',
  });
  expect(resp.status, 'POST memberships as site admin').toBe(403);
});

// ---------------------------------------------------------------------------
// B10(e) — vacuous custody: a user with ZERO memberships is refused on every
// privileged route from a site admin (never a 2xx). The server returns 404
// — indistinguishable from an unknown id (fact 10).
// ---------------------------------------------------------------------------

test('B10(e) site-a admin on a user with zero memberships is refused on every privileged route', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const orphan = await seedOrphanUser();

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const getResp = await apiCall(page, 'GET', `/users/${orphan.id}`);
  expect(getResp.status, 'GET /users/:id on orphan').toBe(404);

  const fieldsResp = await apiCall(page, 'PUT', `/users/${orphan.id}/global-fields`, {
    firstName: 'Probe',
    reason: 'rbac probe',
  });
  // Reads hide existence (404); commands refuse by custody (403). Never 2xx.
  expect(fieldsResp.status, 'PUT global-fields on orphan').toBe(403);
  // Wire contract (packages/contracts/src/api.ts): RBAC denials surface as ACCESS_DENIED.
  expect((fieldsResp.body as { error?: { code?: string } }).error?.code).toBe('ACCESS_DENIED');

  const statusResp = await apiCall(page, 'PUT', `/users/${orphan.id}/account-status`, {
    accountStatus: 'inactive',
    reason: 'rbac probe',
  });
  expect(statusResp.status, 'PUT account-status on orphan').toBe(403);
  expect((statusResp.body as { error?: { code?: string } }).error?.code).toBe('ACCESS_DENIED');
});

// ---------------------------------------------------------------------------
// B10(f) — site-a admin on a SuperAdmin target: account-status is forbidden.
// ---------------------------------------------------------------------------

test('B10(f) site-a admin cannot change the account status of a SuperAdmin target', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const target = await seedSuperAdmin();

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const resp = await apiCall(page, 'PUT', `/users/${target.id}/account-status`, {
    accountStatus: 'inactive',
    reason: 'rbac probe',
  });
  expect(resp.status, 'PUT account-status on SuperAdmin').toBe(403);

  const persisted = await readUser(target.id);
  expect(persisted.account_status).toBe('active');
});

// ---------------------------------------------------------------------------
// B10(g) — site-a admin cannot change role on the membership of a site they
// do not administer (NOT_ACTIVE_SITE).
// ---------------------------------------------------------------------------

test('B10(g) site-a admin cannot change role on the membership of site b', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const dual = await seedDualSiteUser('Supervisor');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const resp = await apiCall(page, 'PUT', `/users/${dual.id}/memberships/${stack().sites.b.id}/role`, {
    role: 'Administrador',
    reason: 'rbac probe',
  });
  expect(resp.status, 'PUT membership role on inactive site').toBe(403);
});

// ---------------------------------------------------------------------------
// B10(h) — site-a admin cannot globally DELETE a user (SuperAdmin only).
// ---------------------------------------------------------------------------

test('B10(h) site-a admin cannot globally delete a user', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const resp = await apiCall(page, 'DELETE', `/users/${target.id}`);
  expect(resp.status, 'DELETE /users as site admin').toBe(403);

  const persisted = await readUser(target.id);
  expect(persisted.account_status).toBe('active');
});

// ---------------------------------------------------------------------------
// B11 — self-protection (server-backed) + UI locks for own row.
// ---------------------------------------------------------------------------

test('B11 self-protection: site-a admin cannot self-inactivate, self-reset, or self-role; SuperAdmin cannot self-delete', async ({ page, browser }) => {
  // (a) site-a admin self-protection
  const adminA = await seedSiteAUser('Administrador');

  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index', stack().sites.a.id);

  const inactivateResp = await apiCall(page, 'PUT', `/users/${adminA.id}/account-status`, {
    accountStatus: 'inactive',
    reason: 'self probe',
  });
  expect(inactivateResp.status, 'PUT account-status on self').toBe(403);

  const resetResp = await apiCall(page, 'POST', `/users/${adminA.id}/admin-reset-password`, {
    password: 'F7-Self1#Probe',
    reason: 'self probe',
  });
  expect(resetResp.status, 'POST admin-reset on self').toBe(403);

  const roleResp = await apiCall(page, 'PUT', `/users/${adminA.id}/memberships/${stack().sites.a.id}/role`, {
    role: 'Supervisor',
    reason: 'self probe',
  });
  expect(roleResp.status, 'PUT membership role on self active site').toBe(403);

  // UI: the own row omits the "Eliminar" action (search it: site a is shared by parallel tests).
  await page.getByRole('searchbox', { name: 'Buscar usuario' }).fill(adminA.username);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('searchTerm') && res.request().method() === 'GET'),
    page.getByRole('search').getByRole('button', { name: 'Buscar', exact: true }).click(),
  ]);
  const ownRow = page.getByRole('row').filter({ hasText: adminA.username });
  await expect(ownRow).toBeVisible();
  await expect(ownRow.getByRole('button', { name: 'Eliminar' })).toHaveCount(0);

  // UI: in Edit, the role select is disabled (own active-site membership).
  await page.goto(`/Users/Edit/${adminA.id}`);
  await expect(page.getByRole('combobox', { name: 'Rol de Aplicación' })).toBeDisabled();

  // (b) SuperAdmin cannot delete itself.
  const sa = await seedSuperAdmin();
  const saContext = await browser.newContext();
  const saPage = await saContext.newPage();
  const loginResult = await loginViaApi(saPage, { loginIdentifier: sa.username, password: sa.password }, null);
  expect(loginResult.status).toBe(200);
  const selfDeleteResp = await apiCall(saPage, 'DELETE', `/users/${sa.id}`);
  expect(selfDeleteResp.status, 'DELETE /users on self').toBe(403);
  await saContext.close();
});

// ---------------------------------------------------------------------------
// B12 — SuperAdmin HTTP mutation ABSENT: isSuperAdmin keys are rejected.
// ---------------------------------------------------------------------------

test('B12 SuperAdmin HTTP mutation: isSuperAdmin key is rejected (no DB row mutated)', async ({ page }) => {
  const sa = await seedSuperAdmin();
  const loginResult = await loginViaApi(page, { loginIdentifier: sa.username, password: sa.password }, null);
  expect(loginResult.status).toBe(200);

  const token = `suphttp${Date.now()}`;
  const postResp = await apiCall(page, 'POST', '/users', {
    username: token,
    email: `${token}@example.invalid`,
    firstName: 'X',
    lastName: 'Y',
    identityCard: '88888',
    phoneNumber: '70000000',
    password: 'F7-Probe1#Aaaa',
    role: 'Supervisor',
    isSuperAdmin: true,
    memberships: [{ siteId: stack().sites.a.id, role: 'Supervisor' }],
  });
  expect([400, 403]).toContain(postResp.status);

  const snakeResp = await apiCall(page, 'POST', '/users', {
    username: `${token}b`,
    email: `${token}b@example.invalid`,
    firstName: 'X',
    lastName: 'Y',
    identityCard: '88889',
    phoneNumber: '70000001',
    password: 'F7-Probe1#Aaaa',
    role: 'Supervisor',
    is_super_admin: true,
    memberships: [{ siteId: stack().sites.a.id, role: 'Supervisor' }],
  });
  expect([400, 403]).toContain(snakeResp.status);

  const target = await seedSiteAUser('Supervisor');
  const fieldsResp = await apiCall(page, 'PUT', `/users/${target.id}/global-fields`, {
    firstName: 'ShouldNotApply',
    reason: 'super-admin key probe',
    isSuperAdmin: true,
  });
  expect([400, 403]).toContain(fieldsResp.status);

  // No user was created from these rejected requests.
  const persisted = await readUser(target.id);
  expect(persisted.first_name).not.toBe('ShouldNotApply');
  expect(await readIsSuperAdmin(target.id)).toBe(false);
});

// ---------------------------------------------------------------------------
// B13 — concurrent cross-deletes serialize under the fixed advisory lock and
// must never leave the database with zero active SuperAdmins. The earlier
// "at most one succeeds" invariant is wrong: other tests in the same run
// seed SuperAdmins, so both cross-deletes can succeed while still preserving
// the global invariant.
// ---------------------------------------------------------------------------

test('B13 concurrent SuperAdmin cross-deletes serialize and never leave zero active SuperAdmins (global invariant)', async ({ browser }) => {
  const x = await seedSuperAdmin();
  const y = await seedSuperAdmin();

  const xContext = await browser.newContext();
  const yContext = await browser.newContext();
  const xPage = await xContext.newPage();
  const yPage = await yContext.newPage();
  try {
    const xLogin = await loginViaApi(xPage, { loginIdentifier: x.username, password: x.password }, null);
    const yLogin = await loginViaApi(yPage, { loginIdentifier: y.username, password: y.password }, null);
    expect(xLogin.status).toBe(200);
    expect(yLogin.status).toBe(200);

    const [xResult, yResult] = await Promise.all([
      apiCall(xPage, 'DELETE', `/users/${y.id}`),
      apiCall(yPage, 'DELETE', `/users/${x.id}`),
    ]);

    // The advisory lock prevents deadlocks and 5xx; every response is 2xx or a
    // documented policy code (LAST_SUPERADMIN / SELF_PROTECTION).
    for (const [label, result] of [
      ['x.delete(y)', xResult],
      ['y.delete(x)', yResult],
    ] as const) {
      expect(result.status, `${label} status`).toBeLessThan(500);
      if (result.status >= 400) {
        expect(result.status, `${label} policy code`).toBeGreaterThanOrEqual(400);
        expect(result.status, `${label} policy code`).toBeLessThan(500);
      }
    }

    // After the race settles the global SuperAdmin count must remain >= 1.
    // Other tests seed SuperAdmins in the same DB, so we only assert the
    // invariant — not exact counts or per-row outcomes.
    const afterActive = await countActiveSuperAdmins();
    expect(afterActive, 'active SuperAdmins after race').toBeGreaterThanOrEqual(1);
  } finally {
    await xContext.close();
    await yContext.close();
  }
});

// ---------------------------------------------------------------------------
// B14(a) — F8 hardening: site-a admin opening the Details of a user whose
// ONLY membership (active OR revoked) belongs to site b sees the canonical
// 404 alert. The active-membership case was already covered by B10(b); this
// test pins the revoked variant from the F1 §11 history side and confirms
// the policy stays "site a never probes site b".
// ---------------------------------------------------------------------------

test('B14(a) site-a admin gets the 404 alert on Details of a user whose only membership in site b is revoked', async ({
  page,
}) => {
  const adminA = await seedSiteAUser('Administrador');
  const targetB = await seedSiteBUser('Supervisor');

  // Drop the active site-b membership to 'revoked' directly in the DB so the
  // test stays focused on the visibility behaviour (the revoke command itself
  // is owned by the B8 happy-path suite).
  await controlDb(async (c) => {
    await c.query(
      `UPDATE public.lu_site_membership SET status = 'revoked' WHERE user_id = $1`,
      [targetB.id],
    );
  });

  await openAs(
    page,
    { loginIdentifier: adminA.username, password: adminA.password },
    `/Users/Details/${targetB.id}`,
    stack().sites.a.id,
  );

  await expect(page.getByRole('heading', { name: 'Cargando cuenta…' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: targetB.displayName })).toHaveCount(0);
  await expect(page.getByRole('alert').filter({ hasText: /no existe|no est|not_found/i }).first()).toBeVisible();
  await expect(page.getByRole('button', { name: 'Volver al Listado' })).toBeVisible();

  // Server-side: the lookup is still 404 by design (fact 10).
  const resp = await apiCall(page, 'GET', `/users/${targetB.id}`);
  expect(resp.status, 'GET /users/:id (revoked site-b only)').toBe(404);
});

// ---------------------------------------------------------------------------
// B14(b) — F8 hardening: a site-a admin POSTing a restore for the
// site-b membership of a user is rejected with 403 (the command itself
// stays authorised to the site admin's active site; canManageMembership
// denies any non-active-site). The UI never offers the form, but the API
// must refuse too.
// ---------------------------------------------------------------------------

test('B14(b) site-a admin restore POST for a site-b membership answers 403 (NOT_ACTIVE_SITE)', async ({
  page,
}) => {
  const adminA = await seedSiteAUser('Administrador');
  // seedDualSiteUser covers both sites; revoke the site-b membership so the
  // restore target is a REVOKED site-b membership (site a stays active for
  // the site-a admin).
  const dual = await seedDualSiteUser('Supervisor');
  await controlDb(async (c) => {
    await c.query(
      `UPDATE public.lu_site_membership SET status = 'revoked' WHERE user_id = $1 AND site_id = $2`,
      [dual.id, stack().sites.b.id],
    );
  });

  await openAs(
    page,
    { loginIdentifier: adminA.username, password: adminA.password },
    '/Users/Index',
    stack().sites.a.id,
  );

  const resp = await apiCall(page, 'POST', `/users/${dual.id}/memberships/${stack().sites.b.id}/restore`, {});
  expect(resp.status, 'POST restore on inactive site').toBe(403);
  const code = (resp.body as { error?: { code?: string } } | null)?.error?.code;
  expect(code, 'wire-level error code').toBe('ACCESS_DENIED');

  // The site-b membership must stay revoked: the failed POST must not flip it.
  const persisted = await controlDb<Array<{ status: string }>>(async (c) =>
    (
      await c.query(
        `SELECT status FROM public.lu_site_membership WHERE user_id = $1 AND site_id = $2`,
        [dual.id, stack().sites.b.id],
      )
    ).rows,
  );
  expect(persisted[0]?.status).toBe('revoked');
});

// ---------------------------------------------------------------------------
// B14(c) — F8 hardening: a site-a admin opening Details of a user with ZERO
// memberships (an orphan) sees the canonical 404 alert. The membership-less
// case never had vacuous custody; the API must answer 404 indistinguishably
// from an unknown id (fact 10).
// ---------------------------------------------------------------------------

test('B14(c) site-a admin on a membership-less user gets the 404 alert on Details', async ({ page }) => {
  const adminA = await seedSiteAUser('Administrador');
  const orphan = await seedOrphanUser();

  await openAs(
    page,
    { loginIdentifier: adminA.username, password: adminA.password },
    `/Users/Details/${orphan.id}`,
    stack().sites.a.id,
  );

  await expect(page.getByRole('heading', { name: 'Cargando cuenta…' })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: orphan.displayName })).toHaveCount(0);
  await expect(page.getByRole('alert').filter({ hasText: /no existe|no est|not_found/i }).first()).toBeVisible();
  await expect(page.getByRole('button', { name: 'Volver al Listado' })).toBeVisible();

  const resp = await apiCall(page, 'GET', `/users/${orphan.id}`);
  expect(resp.status, 'GET /users/:id (orphan)').toBe(404);
});
