import { expect, test } from '../support/fixtures.js';
import { apiCall, revokeSessions, seedUser } from '../support/index.js';
import { RAW_LEAK_REGEX, loginAndOpen } from '../support/ui-checks.js';

/** Stable per-test token for unique values (parallel-safe). */
function uniqueToken(): string {
  return `${Date.now().toString(36)}${Math.floor(Math.random() * 1e6).toString(36)}`;
}

/** Digits-only phone that satisfies `phoneNumber` (7..15 digits, `^[+]?[0-9 ().-]+$`). */
function digitsPhone(): string {
  return `7${String(Date.now() % 1_000_000_00).padStart(8, '0')}`;
}

/** Uppercase alphanum identity card that satisfies `^[0-9A-Z-]{1,10}$`. */
function digitsIdentityCard(): string {
  return `${(Date.now() % 1_000_000_000).toString().padStart(8, '0')}`.slice(0, 10);
}

test.describe('C6 — Estados de error con respuestas reales del servidor', () => {
  test('400 — crear persona con actorCode no-cadena devuelve mensaje a nivel de campo y ningún JSON crudo', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({
      label: 'c6bad400',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: admin.username, password: admin.password }, '/');

    const probe = await apiCall(page, 'POST', '/people', {
      type: 'internal',
      name: `Persona 400 ${uniqueToken()}`,
      actorCode: 12345,
      email: null,
      phoneNumber: null,
      isEntity: false,
      address: null,
      category: 1,
    });

    expect(probe.status, 'POST /people with non-string actorCode').toBe(400);
    const serialized = JSON.stringify(probe.body);
    expect(serialized).not.toMatch(RAW_LEAK_REGEX);

    const fields = (
      probe.body as { error?: { fieldErrors?: Record<string, readonly string[]> } } | null
    )?.error?.fieldErrors;
    expect(fields?.['actorCode']?.length ?? 0, 'fieldErrors.actorCode must be present').toBeGreaterThan(
      0,
    );

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /POST \/api\/v1\/people 400/,
    ]);
  });

  test('401 — sesiones revocadas: la próxima acción devuelve al login con un mensaje y sin shell previo', async ({
    page,
    health,
  }) => {
    const user = await seedUser({
      label: 'c6sess',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: user.username, password: user.password }, '/Users/Index');
    await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();

    await revokeSessions(user.id);

    // Trigger an in-app action so the SPA issues the next /api/v1/users
    // request with the now-revoked session cookie; apiCall() bypasses the SPA
    // and would not observe the session expiry path.
    const searchBox = page.getByLabel('Buscar usuario');
    await searchBox.fill('cualquier');
    await searchBox.press('Enter');

    await expect(page.getByRole('heading', { name: 'Ingreso al Sistema' })).toBeVisible();
    await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toHaveCount(0);
    await expect(page.getByRole('alert')).toHaveText(
      /Su sesi\u00f3n expir\u00f3 o fue revocada\. Inicie sesi\u00f3n nuevamente\./,
    );

    const shellStillThere = await page.evaluate(() =>
      Boolean(document.querySelector('header.topbar')),
    );
    expect(shellStillThere, 'shell must be removed after session failure').toBe(false);

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /GET \/api\/v1\/users 401/,
    ]);
  });

  test('404 — site-a admin sobre un usuario exclusivo de site-b: GET /users/:id devuelve 404 indistinguible', async ({
    page,
    health,
  }) => {
    const siteAAdmin = await seedUser({
      label: 'c6adm404',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    const siteBOnly = await seedUser({
      label: 'c6usr404',
      memberships: [{ site: 'b', role: 'Administrador' }],
    });
    await loginAndOpen(
      page,
      { loginIdentifier: siteAAdmin.username, password: siteAAdmin.password },
      '/',
    );

    const probe = await apiCall(page, 'GET', `/users/${siteBOnly.id}`);
    expect(probe.status, 'cross-site user lookup must be 404 by design').toBe(404);
    const body = JSON.stringify(probe.body);
    expect(body).not.toMatch(RAW_LEAK_REGEX);

    await page.goto(`/Users/Details/${siteBOnly.id}`);
    await expect(page.getByRole('alert')).toHaveText(
      'El registro solicitado no existe o no est\u00e1 disponible.',
    );
    await expect(
      page.getByRole('button', { name: 'Volver al Listado' }),
    ).toBeVisible();

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /GET \/api\/v1\/users\/[^ ]+ 404/,
    ]);
  });

  test('403 — Supervisor en sede a: /Users/Index se redirige al Dashboard y GET /users devuelve ACCESS_DENIED (RBAC)', async ({
    page,
    health,
  }) => {
    const supervisor = await seedUser({
      label: 'c6sup403',
      memberships: [{ site: 'a', role: 'Supervisor' }],
    });
    await loginAndOpen(
      page,
      { loginIdentifier: supervisor.username, password: supervisor.password },
      '/',
    );

    await page.goto('/Users/Index');
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Dashboard de Gestión' })).toBeVisible();

    const probe = await apiCall(page, 'GET', '/users');
    expect(probe.status, 'GET /users from a site Supervisor must be 403').toBe(403);
    const body = JSON.stringify(probe.body);
    // packages/contracts/src/api.ts: RBAC denial is ACCESS_DENIED (SITE_ACCESS_DENIED = no eligible site).
    expect(body).toContain('"code":"ACCESS_DENIED"');
    expect(body).not.toMatch(RAW_LEAK_REGEX);

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /GET \/api\/v1\/users 403/,
    ]);
  });

  test('409 — username duplicado en POST /users devuelve CONFLICT y ningún rastro de SQL/pg', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({
      label: 'c6adm409',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    const existing = await seedUser({ label: 'c6existing' });
    await loginAndOpen(page, { loginIdentifier: admin.username, password: admin.password }, '/');

    const probe = await apiCall(page, 'POST', '/users', {
      username: existing.username,
      email: `dupe-${uniqueToken()}@f7.example.invalid`,
      firstName: 'Duplicado',
      lastName: 'Usuario',
      identityCard: digitsIdentityCard(),
      phoneNumber: digitsPhone(),
      password: existing.password,
      role: 'Administrador',
    });
    expect(probe.status, 'POST /users with duplicate username must be 409').toBe(409);
    const body = JSON.stringify(probe.body);
    expect(body).not.toMatch(RAW_LEAK_REGEX);

    const code = (
      probe.body as { error?: { code?: string } } | null
    )?.error?.code;
    expect(code, 'response must carry the CONFLICT error code').toBe('CONFLICT');

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /POST \/api\/v1\/users 409/,
    ]);
  });
});

test.describe('C7 — Navegación: rutas canónicas y fallback de la app', () => {
  test('el grupo "Usuarios" del sidebar se expande y el enlace "Usuarios" abre /Users/Index', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({
      label: 'c7sb',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: admin.username, password: admin.password }, '/');

    const sidebarNav = page.getByRole('navigation', { name: 'Navegación principal' });
    const groupToggle = sidebarNav.getByRole('button', { name: 'Usuarios' });
    await expect(groupToggle).toBeVisible();
    await groupToggle.click();

    const link = sidebarNav.getByRole('link', { name: 'Usuarios' });
    await expect(link).toBeVisible();
    await link.click();
    await expect(page).toHaveURL(/\/Users\/Index(?:\?.*)?$/);
    await expect(page.getByRole('heading', { level: 3, name: 'Control de Usuarios' })).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('ruta desconocida: la app sigue cargada con la cabecera y el breadcrumb por defecto', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({
      label: 'c7unk',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: admin.username, password: admin.password }, '/');

    await page.goto('/No/Existe');
    await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: 'Dashboard de Gestión' })).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('/Users/Details sin id redirige a /Profile', async ({ page, health }) => {
    const user = await seedUser({
      label: 'c7prof',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: user.username, password: user.password }, '/Users/Details');

    await expect(page).toHaveURL(/\/Profile$/);
    await expect(
      page.getByRole('heading', { level: 1, name: 'Mi Perfil', exact: true }),
    ).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('/Users/Details?id=<id> redirige al detalle administrativo', async ({ page, health }) => {
    const admin = await seedUser({
      label: 'c7admdet',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    const target = await seedUser({
      label: 'c7tgt',
      memberships: [{ site: 'a', role: 'Administrador' }],
    });
    await loginAndOpen(page, { loginIdentifier: admin.username, password: admin.password }, '/');

    await page.goto(`/Users/Details?id=${target.id}`);
    await expect(page).toHaveURL(new RegExp(`/Users/Details/${target.id}`));
    await expect(
      page.getByRole('heading', { level: 1, name: 'Detalle de Usuario' }),
    ).toBeVisible();
    await expect(page.getByText('Ficha de usuario')).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });
});

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
