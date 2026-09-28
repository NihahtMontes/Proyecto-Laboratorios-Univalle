import { expect, test } from '../support/fixtures.js';
import { loginViaApi, submitLogin } from '../support/auth.js';
import {
  controlDb,
  findUserIdByUsername,
  readMemberships,
  readUser,
} from '../support/db.js';
import { syntheticPassword, stack } from '../support/index.js';
import {
  freshToken,
  openAs,
  randomTag,
  seedPageOfUsers,
  seedSiteAUser,
  seedSiteBUser,
  seedSuperAdmin,
  seedSuperAdminWithMemberships,
  STATUS_LABEL,
} from '../support/users-flows.js';

/**
 * Workstream B — happy-path / state-machine coverage for users (B1-B9).
 * Every scenario owns its seeded actors so the suite is parallel-safe.
 */

/**
 * Locates the Users-tab search button. `getByRole('button', { name: 'Buscar' })`
 * substring-matches the navbar "Buscar en el menú" toggle, so we anchor on the
 * accessible form by `role="search"` and pick the submit button inside it.
 */
function searchSubmit(page: import('@playwright/test').Page) {
  return page.getByRole('search').getByRole('button', { name: 'Buscar', exact: true });
}

/** Header card containing the user's displayName + role/status badges. */
function userHeader(page: import('@playwright/test').Page, displayName: string) {
  return page.getByRole('heading', { name: displayName, level: 2 }).locator('..');
}

// ---------------------------------------------------------------------------
// B1 — site-a Administrador lists/searches/filters/paginates the user index.
// ---------------------------------------------------------------------------

test('B1 site-a admin lists users: scoped search, status filter, pagination, health clean', async ({ page, health }) => {
  const admin = await seedSiteAUser('Administrador');
  const inA = await seedSiteAUser('Supervisor');
  const inactiveA = await seedSiteAUser('Supervisor', { accountStatus: 'inactive' });
  const inB = await seedSiteBUser('Administrador');

  // 25 page-fillers in site-a + 1 admin (initial page is 20, page 2 keeps the rest).
  const pageToken = randomTag('pg');
  const filler = await seedPageOfUsers(pageToken, 25);

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, '/Users/Index');

  // The "Cuentas de Acceso" tab heading is visible by default.
  await expect(page.getByRole('tab', { name: 'Cuentas de Acceso' })).toHaveAttribute(
    'aria-selected',
    'true',
  );

  const searchbox = page.getByRole('searchbox', { name: 'Buscar usuario' });

  // Scoped search: site-a admin searches for site-b-only user → no rows.
  await searchbox.fill(inB.username);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  await expect(
    page.getByText('No se encontraron cuentas bajo los criterios definidos.'),
  ).toBeVisible();

  // Reset filters and search for a site-a user.
  await searchbox.fill(inA.username);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  await expect(page.getByRole('row').filter({ hasText: inA.username })).toBeVisible();

  // Status filter: the inactive user is hidden by the default list and shows up
  // only under the inactive filter; the active filter hides it again.
  await searchbox.fill(inactiveA.username);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET'),
    searchSubmit(page).click(),
  ]);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('statusFilter') && res.request().method() === 'GET'),
    page.getByRole('combobox', { name: 'Filtrar por estado' }).selectOption('inactive'),
  ]);
  await expect(page.getByRole('row').filter({ hasText: inactiveA.username })).toBeVisible();
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('statusFilter') && res.request().method() === 'GET'),
    page.getByRole('combobox', { name: 'Filtrar por estado' }).selectOption('active'),
  ]);
  await expect(page.getByRole('row').filter({ hasText: inactiveA.username })).toHaveCount(0);

  // Pagination: 25 + 1 admin + 1 supervisor (inA) = 27 rows on the active filter.
  await page.getByRole('button', { name: 'Limpiar filtros' }).click();
  await searchbox.fill(pageToken);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  const pager = page.getByRole('navigation', { name: 'Paginación Usuarios' });
  await expect(pager).toBeVisible();
  await expect(pager.getByRole('button', { name: '2' })).toBeVisible();
  await expect(page.getByRole('row').filter({ hasText: pageToken })).toHaveCount(20);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes('currentPage=2') && res.request().method() === 'GET'),
    pager.getByRole('button', { name: '2' }).click(),
  ]);

  // Only the 25 fillers match the token: page 1 shows 20, page 2 the remaining 5
  // (server order is not asserted, only the page split).
  await expect(page.getByRole('row').filter({ hasText: pageToken })).toHaveCount(filler.length - 20);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// B2 — switch to the "Directorio de Personal" tab; People content is in C.
// ---------------------------------------------------------------------------

test('B2 site-a admin switches to Directorio de Personal tab and URL gains ?tab=personas', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, '/Users/Index');

  const personasTab = page.getByRole('tab', { name: 'Directorio de Personal' });
  await personasTab.click();
  await expect(page).toHaveURL(/\/Users\/Index\?tab=personas/);
  await expect(personasTab).toHaveAttribute('aria-selected', 'true');

  // The Personas panel is rendered (workstream C owns its own content asserts).
  await expect(page.getByRole('region', { name: 'Directorio de personas' })).toBeVisible();
});

// ---------------------------------------------------------------------------
// B3 — site-a user details persist across reload.
// ---------------------------------------------------------------------------

test('B3 site-a user details show identity, role, status, memberships and persist on reload', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, `/Users/Details/${target.id}`);

  await expect(page.getByRole('heading', { name: target.displayName })).toBeVisible();
  await expect(page.getByText(`@${target.username}`).first()).toBeVisible();
  await expect(page.getByText('Ficha de usuario')).toBeVisible();
  await expect(page.getByRole('row', { name: /Supervisor/i }).first()).toBeVisible();
  // Scope the status badge to the user header card (it also appears in the sidebar).
  await expect(userHeader(page, target.displayName).getByText(STATUS_LABEL.active)).toBeVisible();

  await page.reload();
  await expect(page.getByRole('heading', { name: target.displayName })).toBeVisible();
  const memberships = await readMemberships(target.id);
  expect(memberships).toEqual([
    expect.objectContaining({ role: 'Supervisor', status: 'active' }),
  ]);
});

// ---------------------------------------------------------------------------
// B4 — create a single-site user through the UI form.
// ---------------------------------------------------------------------------

test('B4 site-a admin creates a Supervisor through the UI form and that user can log in', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const token = freshToken('b4');
  const newPassword = syntheticPassword('B4');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, '/Users/Index');

  await page.getByRole('button', { name: 'Vincular Usuario' }).click();
  await expect(page).toHaveURL(/\/Users\/Create/);

  // Required form fields render with the " *" suffix; match by accessible-name substring.
  await page.getByRole('textbox', { name: 'Nombres' }).fill('B4');
  await page.getByRole('textbox', { name: 'Primer Apellido' }).fill('Create');
  await page.getByRole('textbox', { name: 'Cédula de Identidad' }).fill('11111');
  await page.getByRole('textbox', { name: 'Correo Institucional' }).fill(`b4-${token}@example.invalid`);
  await page.getByRole('textbox', { name: 'Teléfono de Contacto' }).fill('70000001');
  await page.getByRole('textbox', { name: 'Nombre de Usuario (Login)' }).fill(`b4${token}`);
  await page.getByRole('combobox', { name: 'Rol de Aplicación' }).selectOption('Supervisor');

  const passwordInput = page.getByLabel('Contraseña de Acceso');
  await passwordInput.fill(newPassword);

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => res.url().endsWith('/api/v1/users') && res.request().method() === 'POST'),
    page.getByRole('button', { name: 'Crear Cuenta' }).click(),
  ]);
  expect(createResponse.status(), 'POST /users').toBe(201);

  await expect(page).toHaveURL(/\/Users\/Index/);
  await expect(page.getByText('creada exitosamente')).toBeVisible();

  const newId = await findUserIdByUsername(`b4${token}`);
  expect(newId).not.toBeNull();
  const memberships = await readMemberships(newId!);
  expect(memberships).toEqual([
    expect.objectContaining({ role: 'Supervisor', status: 'active' }),
  ]);
  const persisted = await readUser(newId!);
  expect(persisted.account_status).toBe('active');
  expect(persisted.password_scheme).toBe('bcrypt');

  // The new user can log in immediately; the seed set must_change_password=true so we go via API.
  const fresh = await page.context().newPage();
  const loginResult = await loginViaApi(fresh, { loginIdentifier: `b4${token}`, password: newPassword }, stack().sites.a.id);
  expect(loginResult.status).toBe(200);
  await fresh.close();
});

// ---------------------------------------------------------------------------
// B5 — duplicate username/email triggers an inline 409.
// ---------------------------------------------------------------------------

test('B5 creating a user with a duplicate username returns a 409 visible inline and does not create a row', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const existing = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, '/Users/Create');

  const token = freshToken('b5');
  // Rows matching this attempt: the new email must stay absent and the duplicated
  // username must keep exactly its original row (parallel tests share the DB).
  const countAttempt = () =>
    controlDb<number>(async (c) => {
      const r = await c.query<{ n: number }>(
        `SELECT count(*)::int AS n FROM public.lu_user WHERE email = lower($1) OR username = lower($2)`,
        [`b5-${token}@example.invalid`, existing.username],
      );
      return r.rows[0]?.n ?? 0;
    });
  const before = await countAttempt();
  expect(before).toBe(1);
  await page.getByRole('textbox', { name: 'Nombres' }).fill('B5');
  await page.getByRole('textbox', { name: 'Primer Apellido' }).fill('Dup');
  await page.getByRole('textbox', { name: 'Cédula de Identidad' }).fill('22222');
  await page.getByRole('textbox', { name: 'Correo Institucional' }).fill(`b5-${token}@example.invalid`);
  await page.getByRole('textbox', { name: 'Teléfono de Contacto' }).fill('70000002');
  await page.getByRole('textbox', { name: 'Nombre de Usuario (Login)' }).fill(existing.username);
  await page.getByRole('combobox', { name: 'Rol de Aplicación' }).selectOption('Supervisor');
  await page.getByLabel('Contraseña de Acceso').fill(syntheticPassword('B5'));

  const [resp] = await Promise.all([
    page.waitForResponse((res) => res.url().endsWith('/api/v1/users') && res.request().method() === 'POST'),
    page.getByRole('button', { name: 'Crear Cuenta' }).click(),
  ]);
  expect(resp.status(), 'POST /users (duplicate username)').toBe(409);
  await expect(page.getByRole('alert').filter({ hasText: /Conflicto|existe|ya est/i }).first()).toBeVisible();

  expect(await countAttempt()).toBe(before);
});

// ---------------------------------------------------------------------------
// B6 — edit a single-site user's names/phone; reload shows the new values.
// ---------------------------------------------------------------------------

test('B6 site-a admin edits a single-site user names/phone and the change persists after reload', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, `/Users/Edit/${target.id}`);

  const firstName = page.getByRole('textbox', { name: 'Nombres' });
  const lastName = page.getByRole('textbox', { name: 'A. Paterno' });
  const phone = page.getByRole('textbox', { name: 'Teléfono de Contacto' });

  await firstName.fill('B6Edited');
  await lastName.fill('B6EditedLast');
  await phone.fill('+591 76000000');

  const [resp] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/global-fields') && res.request().method() === 'PUT'),
    page.getByRole('button', { name: 'Guardar Cambios' }).click(),
  ]);
  expect(resp.status(), 'PUT /users/.../global-fields').toBe(200);

  await expect(page).toHaveURL(/\/Users\/Index|\/Users\/Details/);
  await expect(page.getByText('actualizados correctamente')).toBeVisible();

  await page.goto(`/Users/Details/${target.id}`);
  await expect(page.getByRole('heading', { name: /B6Edited B6EditedLast/ })).toBeVisible();

  const persisted = await readUser(target.id);
  expect(persisted.first_name).toBe('B6Edited');
  expect(persisted.last_name).toBe('B6EditedLast');
  expect(persisted.phone_number).toBe('+591 76000000');
});

// ---------------------------------------------------------------------------
// B7 — inactivate (window.confirm) and reactivate a user from Details.
// ---------------------------------------------------------------------------

test('B7 site-a admin inactivates and reactivates a user; login is rejected while inactive', async ({ page, browser }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, `/Users/Details/${target.id}`);

  page.once('dialog', (d) => {
    void d.accept();
  });

  const [inactivateResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/account-status`) &&
        res.request().method() === 'PUT',
    ),
    page.getByRole('button', { name: 'Inactivar cuenta' }).click(),
  ]);
  expect(inactivateResp.status(), 'PUT account-status inactive').toBe(200);

  // The status badge in the user header card carries the new label.
  await expect(userHeader(page, target.displayName).getByText(STATUS_LABEL.inactive)).toBeVisible();
  let persisted = await readUser(target.id);
  expect(persisted.account_status).toBe('inactive');

  // The inactive user cannot log in.
  const blockedContext = await page.context().browser()!.newContext();
  const blocked = await blockedContext.newPage();
  const loginBlocked = await loginViaApi(blocked, { loginIdentifier: target.username, password: target.password }, stack().sites.a.id);
  expect(loginBlocked.status, 'inactive login').toBe(401);
  await blockedContext.close();

  // Reactivate via the "Activar cuenta" button.
  const [activateResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/account-status`) &&
        res.request().method() === 'PUT',
    ),
    page.getByRole('button', { name: 'Activar cuenta' }).click(),
  ]);
  expect(activateResp.status(), 'PUT account-status active').toBe(200);

  await expect(userHeader(page, target.displayName).getByText(STATUS_LABEL.active)).toBeVisible();
  persisted = await readUser(target.id);
  expect(persisted.account_status).toBe('active');

  // The reactivated user can log in (use a fresh browser context to avoid session reuse).
  const ok = await browser.newContext();
  const okPage = await ok.newPage();
  const loginOk = await loginViaApi(okPage, { loginIdentifier: target.username, password: target.password }, stack().sites.a.id);
  expect(loginOk.status, 'reactivated login').toBe(200);
  await ok.close();
});

// ---------------------------------------------------------------------------
// B8 — membership operations in site a: role change, suspend/reactivate, revoke/restore.
// ---------------------------------------------------------------------------

test('B8 site-a admin changes membership role, suspends/reactives, and revokes/restores on site a', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, `/Users/Details/${target.id}`);

  // Change role: Supervisor -> Administrador.
  page.once('dialog', (d) => {
    void d.accept();
  });
  const [changeRoleResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}/role`) &&
        res.request().method() === 'PUT',
    ),
    page.getByRole('button', { name: 'Cambiar a Administrador' }).click(),
  ]);
  expect(changeRoleResp.status(), 'change role').toBe(200);
  expect((await readMemberships(target.id))[0]?.role).toBe('Administrador');

  // Suspend the membership.
  page.once('dialog', (d) => {
    void d.accept();
  });
  const [suspendResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}/status`) &&
        res.request().method() === 'PUT',
    ),
    page.getByRole('button', { name: 'Suspender' }).click(),
  ]);
  expect(suspendResp.status(), 'suspend membership').toBe(200);
  expect((await readMemberships(target.id))[0]?.status).toBe('suspended');

  // Reactivate.
  const [reactivateResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}/status`) &&
        res.request().method() === 'PUT',
    ),
    page.getByRole('button', { name: 'Reactivar' }).click(),
  ]);
  expect(reactivateResp.status(), 'reactivate membership').toBe(200);
  expect((await readMemberships(target.id))[0]?.status).toBe('active');

  // Revoke the membership from the Delete page. "Revocar Acceso" is rendered as a
  // button (not a link) inside the Details sidebar "Acciones" card.
  await page.getByRole('button', { name: 'Revocar Acceso' }).click();
  await expect(page).toHaveURL(new RegExp(`/Users/Delete/${target.id}$`));
  const [revokeResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}`) &&
        res.request().method() === 'DELETE',
    ),
    page.getByRole('button', { name: new RegExp(`Sí, Confirmar Baja en ${stack().sites.a.name}`) }).click(),
  ]);
  expect(revokeResp.status(), 'revoke membership').toBe(204);
  // Account soft-deleted by the site-revoke cascade (F1 §11) since this was the only membership.
  expect((await readUser(target.id)).account_status).toBe('deleted');

  // The target cannot log in to site a any longer.
  const blocked = await page.context().newPage();
  const loginBlocked = await loginViaApi(blocked, { loginIdentifier: target.username, password: target.password }, stack().sites.a.id);
  expect(loginBlocked.status, 'revoked login').toBe(401);
  await blocked.close();

  // The user no longer appears in the default list.
  await openAs(page, { loginIdentifier: admin.username, password: admin.password }, '/Users/Index');
  const searchbox = page.getByRole('searchbox', { name: 'Buscar usuario' });
  await searchbox.fill(target.username);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  await expect(
    page.getByText('No se encontraron cuentas bajo los criterios definidos.'),
  ).toBeVisible();

  // F8 (F1 §11 history): the site Administrator opens the Details from the
  // Eliminado history filter; the membership is revoked but the UI keeps the
  // revoke/restore action reachable, with the global actions hidden.
  await page.getByRole('button', { name: 'Limpiar filtros' }).click();
  await searchbox.fill(target.username);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  await page.getByRole('combobox', { name: 'Filtrar por estado' }).selectOption('deleted');
  await page.getByRole('row').filter({ hasText: target.username }).getByRole('button', { name: 'Detalles' }).click();

  await expect(page.getByRole('heading', { name: target.displayName, level: 2 })).toBeVisible();
  // Global commands are hidden for a site-admin viewing F1 §11 history.
  await expect(page.getByRole('button', { name: 'Modificar Perfil' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Inactivar cuenta' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Revocar Acceso' })).toHaveCount(0);
  await expect(page.getByLabel('Restablecer contraseña')).toHaveCount(0);
  // The membership row exposes the F1 §11 restore action only.
  await expect(page.getByRole('button', { name: 'Restaurar', exact: true })).toBeVisible();
  const [restoreResp] = await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}/restore`) &&
        res.request().method() === 'POST',
    ),
    page.getByRole('button', { name: 'Restaurar', exact: true }).click(),
  ]);
  expect(restoreResp.status(), 'restore membership').toBe(204);

  expect((await readUser(target.id)).account_status).toBe('active');

  // And login works again.
  const ok = await page.context().browser()!.newContext();
  const okPage = await ok.newPage();
  const loginOk = await loginViaApi(okPage, { loginIdentifier: target.username, password: target.password }, stack().sites.a.id);
  expect(loginOk.status, 'restored login').toBe(200);
  await ok.close();
});

// ---------------------------------------------------------------------------
// B8(SuperAdmin) — SuperAdmin restores a revoked membership from Details.
// A site-a admin first revokes the only membership through the Delete page
// (so the site-revoke cascade soft-deletes the account), then a SuperAdmin
// opens Details from the Eliminado filter and clicks Restaurar. The
// membership row exposes the same Restaurar button for a SuperAdmin; the
// cascade-deleted account is restored together with the membership
// (restoreAccount=true) and login works again.
// ---------------------------------------------------------------------------

test('B8(SuperAdmin) SuperAdmin restores a revoked membership from the Eliminado filter and login works again', async ({
  page,
  browser,
}) => {
  const superAdmin = await seedSuperAdmin();
  const target = await seedSiteAUser('Supervisor');

  // Revoke the membership from the Delete page using a site-a admin so the
  // site-revoke cascade deletes the account too (precondition for F1 §11).
  const adminA = await seedSiteAUser('Administrador');
  await openAs(page, { loginIdentifier: adminA.username, password: adminA.password }, '/Users/Index');
  await page.getByRole('searchbox', { name: 'Buscar usuario' }).fill(target.username);
  await Promise.all([
    page.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(page).click(),
  ]);
  await page
    .getByRole('row')
    .filter({ hasText: target.username })
    .getByRole('button', { name: 'Detalles' })
    .click();
  await expect(page).toHaveURL(new RegExp(`/Users/Details/${target.id}$`));
  await page.getByRole('button', { name: 'Revocar Acceso' }).click();
  await expect(page).toHaveURL(new RegExp(`/Users/Delete/${target.id}$`));
  await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}`) &&
        res.request().method() === 'DELETE',
    ),
    page.getByRole('button', { name: new RegExp(`Sí, Confirmar Baja en ${stack().sites.a.name}`) }).click(),
  ]);
  expect((await readUser(target.id)).account_status).toBe('deleted');

  // SuperAdmin opens Details from the Eliminado filter.
  const saContext = await browser.newContext();
  const saPage = await saContext.newPage();
  await openAs(
    saPage,
    { loginIdentifier: superAdmin.username, password: superAdmin.password },
    '/Users/Index',
    null,
  );
  await saPage.getByRole('searchbox', { name: 'Buscar usuario' }).fill(target.username);
  await Promise.all([
    saPage.waitForResponse(
      (res) => res.url().includes('/api/v1/users') && res.request().method() === 'GET',
    ),
    searchSubmit(saPage).click(),
  ]);
  await saPage.getByRole('combobox', { name: 'Filtrar por estado' }).selectOption('deleted');
  await saPage
    .getByRole('row')
    .filter({ hasText: target.username })
    .getByRole('button', { name: 'Detalles' })
    .click();
  await expect(saPage.getByRole('heading', { name: target.displayName, level: 2 })).toBeVisible();
  const [restoreResp] = await Promise.all([
    saPage.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${target.id}/memberships/${stack().sites.a.id}/restore`) &&
        res.request().method() === 'POST',
    ),
    saPage.getByRole('button', { name: 'Restaurar', exact: true }).click(),
  ]);
  expect(restoreResp.status(), 'SuperAdmin restore').toBe(204);
  await saContext.close();

  expect((await readUser(target.id)).account_status).toBe('active');

  // Login works again with the original password.
  const ok = await browser.newContext();
  const okPage = await ok.newPage();
  const loginOk = await loginViaApi(
    okPage,
    { loginIdentifier: target.username, password: target.password },
    stack().sites.a.id,
  );
  expect(loginOk.status, 'restored login').toBe(200);
  await ok.close();
});

// ---------------------------------------------------------------------------
// B9 — F8 hardening: a SuperAdmin WITHOUT eligible sites cannot create a user
// from /Users/Create. The form renders the canonical "no eligible sites"
// alert, the Crear Cuenta submit is disabled, and no POST /api/v1/users is
// issued. The role/site selects keep the existing placeholders.
// ---------------------------------------------------------------------------

test('B9 SuperAdmin without eligible sites is blocked in Create (disabled submit, no POST /users)', async ({
  page,
}) => {
  const sa = await seedSuperAdmin();

  const result = await loginViaApi(page, { loginIdentifier: sa.username, password: sa.password }, null);
  expect(result.status, 'SuperAdmin login').toBe(200);
  await page.goto('/');
  await expect(page.getByRole('heading', { level: 2, name: /Bienvenido/ })).toBeVisible();

  // Capture every POST /api/v1/users issued during the page lifetime so we can
  // assert the disabled submit never reaches the server.
  const postUsers: string[] = [];
  page.on('request', (req) => {
    if (req.url().endsWith('/api/v1/users') && req.method() === 'POST') {
      postUsers.push(req.url());
    }
  });

  await page.goto('/Users/Create');

  // The "no eligible sites" alert is the canonical copy from UsersPanel.tsx.
  // The icon is a sibling so the textContent of the test-id element starts with
  // a leading space; use a regex anchored on the salient sentence and tolerate
  // any whitespace between the two parts.
  await expect(page.getByTestId('superadmin-no-sites-notice')).toContainText(
    /Para vincular un usuario se requiere al menos una membres\u00eda de sede\./,
  );
  await expect(page.getByTestId('superadmin-no-sites-notice')).toContainText(
    /Su cuenta SuperAdmin no tiene sedes elegibles;[\s\u00a0]+solicite o asigne una membres\u00eda de sede antes de crear cuentas\./,
  );

  // The "Sede de la membresía" select shows the placeholder when eligibleSites is empty.
  const siteSelect = page.getByRole('combobox', { name: 'Sede de la membresía' });
  await expect(siteSelect).toBeVisible();
  await expect(siteSelect.locator('option').first()).toHaveText('Sin membresía de sede');

  // The role select offered to a SuperAdmin never includes SuperAdmin.
  const roleSelect = page.getByRole('combobox', { name: 'Rol de Aplicación' });
  const options = await roleSelect.locator('option').allTextContents();
  expect(options).toEqual(['Supervisor', 'Administrador']);

  // Filling the form keeps the submit disabled; no POST /users should reach the wire.
  const token = freshToken('b9nosite');
  await page.getByRole('textbox', { name: 'Nombres' }).fill('B9NoSite');
  await page.getByRole('textbox', { name: 'Primer Apellido' }).fill('Sa');
  await page.getByRole('textbox', { name: 'Cédula de Identidad' }).fill('33399');
  await page.getByRole('textbox', { name: 'Correo Institucional' }).fill(`b9nosite-${token}@example.invalid`);
  await page.getByRole('textbox', { name: 'Teléfono de Contacto' }).fill('70000999');
  await page.getByRole('textbox', { name: 'Nombre de Usuario (Login)' }).fill(`b9nosite${token}`);
  await page.getByLabel('Contraseña de Acceso').fill(syntheticPassword('B9NoSite'));

  const submit = page.getByRole('button', { name: 'Crear Cuenta' });
  await expect(submit).toBeDisabled();
  // A forced click on a disabled button still does not dispatch the POST.
  await submit.click({ force: true }).catch(() => undefined);
  await expect(page.getByTestId('superadmin-no-sites-notice')).toBeVisible();
  expect(postUsers, 'no POST /api/v1/users should have been issued').toEqual([]);
});

// ---------------------------------------------------------------------------
// B9 — positive path: SuperAdmin WITH memberships lands on the site picker,
// picks site a, creates a user for site a, then adds a site-b membership from
// the new user's Details (SuperAdmin-only add), and globally deletes another
// account via the Delete page. The role select never offers SuperAdmin.
// ---------------------------------------------------------------------------

test('B9 SuperAdmin with memberships creates a global user, adds a site-b membership, and global-deletes another account', async ({ page }) => {
  const sa = await seedSuperAdminWithMemberships(['a', 'b'], 'Administrador');
  const other = await seedSiteAUser('Supervisor');

  // Log in via UI; the SuperAdmin lands on the site picker.
  await page.goto('/');
  await submitLogin(page, { loginIdentifier: sa.username, password: sa.password });
  await expect(page.getByRole('heading', { name: 'Selecciona una sede', level: 1 })).toBeVisible();
  // Pick site a via the picker.
  await page
    .getByRole('button')
    .filter({ hasText: stack().sites.a.name })
    .first()
    .click();

  // Workspace shell rendered after the site choice.
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();

  await page.goto('/Users/Index');
  await page.getByRole('button', { name: 'Vincular Usuario' }).click();
  await expect(page).toHaveURL(/\/Users\/Create/);

  // The site select is populated with the SuperAdmin's eligible sites.
  const siteSelect = page.getByRole('combobox', { name: 'Sede de la membresía' });
  const siteOptions = await siteSelect.locator('option').allTextContents();
  expect(siteOptions).toEqual(expect.arrayContaining([stack().sites.a.name, stack().sites.b.name]));

  // The role select offered to a SuperAdmin never includes SuperAdmin.
  const roleSelect = page.getByRole('combobox', { name: 'Rol de Aplicación' });
  const roleOptions = await roleSelect.locator('option').allTextContents();
  expect(roleOptions).toEqual(['Supervisor', 'Administrador']);

  // Bind site a for the new user.
  await siteSelect.selectOption(stack().sites.a.id);

  const token = freshToken('b9');
  await page.getByRole('textbox', { name: 'Nombres' }).fill('B9');
  await page.getByRole('textbox', { name: 'Primer Apellido' }).fill('Super');
  await page.getByRole('textbox', { name: 'Cédula de Identidad' }).fill('33333');
  await page.getByRole('textbox', { name: 'Correo Institucional' }).fill(`b9-${token}@example.invalid`);
  await page.getByRole('textbox', { name: 'Teléfono de Contacto' }).fill('70000003');
  await page.getByRole('textbox', { name: 'Nombre de Usuario (Login)' }).fill(`b9${token}`);
  await roleSelect.selectOption('Supervisor');
  await page.getByLabel('Contraseña de Acceso').fill(syntheticPassword('B9'));

  const [createResp] = await Promise.all([
    page.waitForResponse((res) => res.url().endsWith('/api/v1/users') && res.request().method() === 'POST'),
    page.getByRole('button', { name: 'Crear Cuenta' }).click(),
  ]);
  expect(createResp.status(), 'POST /users as SuperAdmin with memberships').toBe(201);

  const newId = await findUserIdByUsername(`b9${token}`);
  expect(newId).not.toBeNull();

  // Open the new user's Details and add a site-b membership through the
  // SuperAdmin-only "Agregar membresía" form.
  await page.goto(`/Users/Details/${newId}`);
  await expect(page.getByRole('heading', { name: 'B9 Super' })).toBeVisible();

  const agregarForm = page.locator('form').filter({ has: page.getByRole('button', { name: 'Agregar membresía' }) });
  await agregarForm.getByRole('combobox', { name: 'Sede a vincular' }).selectOption(stack().sites.b.id);
  await agregarForm.getByRole('combobox', { name: 'Rol de la nueva membresía' }).selectOption('Administrador');
  await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes(`/api/v1/users/${newId}/memberships`) && res.request().method() === 'POST',
    ),
    agregarForm.getByRole('button', { name: 'Agregar membresía' }).click(),
  ]);

  // Persisted membership projection includes both sites.
  const memberships = await readMemberships(newId!);
  expect(memberships).toEqual(
    expect.arrayContaining([
      expect.objectContaining({ role: 'Supervisor', status: 'active' }),
      expect.objectContaining({ role: 'Administrador', status: 'active' }),
    ]),
  );
  expect(memberships).toHaveLength(2);

  // The role select in the Create form never offers SuperAdmin (already
  // asserted above; reiterating for the assertion grouping).

  // Global delete the other seeded account from the Delete page.
  await page.goto(`/Users/Delete/${other.id}`);
  await expect(page.getByRole('heading', { name: '¿Revocar Acceso al Sistema?' })).toBeVisible();
  const [deleteResp] = await Promise.all([
    page.waitForResponse(
      (res) => res.url().endsWith(`/api/v1/users/${other.id}`) && res.request().method() === 'DELETE',
    ),
    page.getByRole('button', { name: 'Eliminar cuenta global (revoca todas las sedes)' }).click(),
  ]);
  expect(deleteResp.status(), 'DELETE /users/:id').toBe(204);
  expect((await readUser(other.id)).account_status).toBe('deleted');
});
