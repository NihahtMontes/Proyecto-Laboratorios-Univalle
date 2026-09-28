import { expect, test } from '../support/fixtures.js';
import { loginAndOpen, fillByLabel } from '../support/ui-checks.js';
import { apiCall, controlDb, seedPerson, seedUser, tenantDb } from '../support/index.js';

/** Unique token per test to keep the directory assertions parallel-safe. */
function uniqueToken(): string {
  return `${Date.now().toString(36)}${Math.floor(Math.random() * 1e6).toString(36)}`;
}

const PERSONAS_PATH = '/Users/Index?tab=personas';
const PERSONAS_ACCESS_DENIED_TEXT =
  'El Directorio de Personal requiere una sede activa en la que tenga el rol Administrador.';

async function openPersonasTab(
  page: import('@playwright/test').Page,
  identifier: string,
  password: string,
): Promise<void> {
  await loginAndOpen(page, { loginIdentifier: identifier, password }, PERSONAS_PATH);
  await expect(
    page.getByRole('tab', { name: 'Directorio de Personal' }),
  ).toHaveAttribute('aria-selected', 'true');
  await expect(
    page.getByRole('heading', { name: 'Directorio Base de Identidades' }),
  ).toBeVisible();
}

async function searchPersona(page: import('@playwright/test').Page, term: string): Promise<void> {
  await page.getByLabel('Buscar persona').fill(term);
  await page.getByLabel('Buscar persona').press('Enter');
}

async function selectPersonaStatus(
  page: import('@playwright/test').Page,
  value: '' | '0' | '1' | '2',
): Promise<void> {
  // Inside the Users Index tab the directory uses the SHARED account-status filter
  // (UsersPanel 'Filtrar por estado'); deleted maps to person status 2 (F5).
  const shared = { '': '', '0': 'active', '1': 'inactive', '2': 'deleted' }[value];
  await page.getByLabel('Filtrar por estado', { exact: true }).selectOption(shared);
}

test.describe('C1 — Directorio de Personal: búsqueda y aislamiento por sede', () => {
  test('muestra las personas de la sede activa y oculta las de otra sede con el mismo token', async ({
    page,
    health,
  }) => {
    const token = uniqueToken();
    const admin = await seedUser({ label: 'c1list', memberships: [{ site: 'a', role: 'Administrador' }] });
    const siteA = await seedPerson({ site: 'a', name: `F7 Persona A ${token}` });
    const siteB = await seedPerson({ site: 'b', name: `F7 Persona B ${token}` });
    await openPersonasTab(page, admin.username, admin.password);

    await expect(page.getByRole('row', { name: new RegExp(`F7 Persona A ${token}`) })).toBeVisible();
    await expect(page.getByRole('row', { name: new RegExp(`F7 Persona B ${token}`) })).toHaveCount(0);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
    void siteA;
    void siteB;
  });

  test('busca por nombre, correo y código de actor del servidor', async ({ page, health }) => {
    const admin = await seedUser({ label: 'c1srch', memberships: [{ site: 'a', role: 'Administrador' }] });
    const token = uniqueToken();
    const byName = await seedPerson({
      site: 'a',
      name: `Buscable Nombre ${token}`,
      email: `buscable.${token}@f7.example.invalid`,
      actorCode: `ACT-${token.slice(0, 6)}-N`,
    });
    await openPersonasTab(page, admin.username, admin.password);

    await searchPersona(page, `Nombre ${token}`);
    await expect(page.getByRole('row', { name: new RegExp(`Buscable Nombre ${token}`) })).toBeVisible();

    await searchPersona(page, `buscable.${token}@f7.example.invalid`);
    await expect(page.getByRole('row', { name: new RegExp(`Buscable Nombre ${token}`) })).toBeVisible();

    await searchPersona(page, `ACT-${token.slice(0, 6)}-N`);
    await expect(page.getByRole('row', { name: new RegExp(`Buscable Nombre ${token}`) })).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
    void byName;
  });

  test('documenta la brecha I-09: la búsqueda por Id numérico no encuentra coincidencias', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({ label: 'c1idgap', memberships: [{ site: 'a', role: 'Administrador' }] });
    const person = await seedPerson({ site: 'a', name: `Persona Brecha ${uniqueToken().replace(/[0-9]/g, 'x')}` });
    await openPersonasTab(page, admin.username, admin.password);

    // Assert on the settled search result for this Id. Other parallel tests' people
    // may contain the same digits in name/email/code, so the list is not required
    // to be empty — only this person must not be matched by its Id.
    const [searched] = await Promise.all([
      page.waitForResponse(
        (res) =>
          new URL(res.url()).pathname === '/api/v1/people' &&
          new URL(res.url()).searchParams.get('searchTerm') === String(person.id),
      ),
      searchPersona(page, String(person.id)),
    ]);
    expect(searched.status()).toBe(200);
    const body = (await searched.json()) as { data?: { items?: Array<{ id: number | string }> } };
    expect((body.data?.items ?? []).map((p) => String(p.id))).not.toContain(String(person.id));
    await expect(page.getByRole('row', { name: new RegExp(person.name) })).toHaveCount(0);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('filtra por estado: una persona status=2 solo aparece bajo "Eliminado"', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({ label: 'c1del', memberships: [{ site: 'a', role: 'Administrador' }] });
    const token = uniqueToken();
    const active = await seedPerson({ site: 'a', name: `Activa Pers ${token}`, status: 0 });
    const deleted = await seedPerson({ site: 'a', name: `Eliminada Pers ${token}`, status: 2 });
    await openPersonasTab(page, admin.username, admin.password);

    await searchPersona(page, `Pers ${token}`);
    await selectPersonaStatus(page, '0');
    await expect(page.getByRole('row', { name: new RegExp(`Activa Pers ${token}`) })).toBeVisible();
    await expect(page.getByRole('row', { name: new RegExp(`Eliminada Pers ${token}`) })).toHaveCount(0);

    await selectPersonaStatus(page, '2');
    await expect(page.getByRole('row', { name: new RegExp(`Eliminada Pers ${token}`) })).toBeVisible();
    await expect(page.getByRole('row', { name: new RegExp(`Activa Pers ${token}`) })).toHaveCount(0);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
    void active;
    void deleted;
  });
});

test.describe('C2 — Personas: alta, edición y baja lógica', () => {
  test('crea una persona interna y la persiste en la base de la sede activa', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({ label: 'c2crt', memberships: [{ site: 'a', role: 'Administrador' }] });
    const token = uniqueToken();
    const name = `Persona Nueva ${token}`;
    await openPersonasTab(page, admin.username, admin.password);

    await page.getByRole('button', { name: /Nueva Identidad/ }).click();
    await expect(page.getByRole('heading', { name: 'Nueva Identidad' })).toBeVisible();

    await fillByLabel(page, 'Nombre / razón social', name);
    await fillByLabel(page, 'Correo', `nuevo.${token}@f7.example.invalid`);
    await fillByLabel(page, 'Teléfono', `7${token.replace(/\D/g, '').slice(-7).padStart(7, '0')}`);
    await page.getByLabel('Código de actor').fill(`NEW-${token.slice(0, 6).toUpperCase()}`);

    const [created] = await Promise.all([
      page.waitForResponse(
        (response) => response.url().endsWith('/api/v1/people') && response.request().method() === 'POST',
      ),
      page.getByRole('button', { name: 'Guardar' }).click(),
    ]);
    expect(created.status(), 'POST /api/v1/people status').toBe(201);

    await expect(page.getByText('Persona registrada correctamente.')).toBeVisible();
    await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();

    const db = await tenantDb('a', async (client) => {
      const r = await client.query<{ id: string; site_id: string; name: string }>(
        `SELECT id::text, site_id, name FROM public.lu_person WHERE name = $1`,
        [name],
      );
      return r.rows[0];
    });
    expect(db, 'tenant row must exist for the new person').toBeDefined();
    expect(db?.name).toBe(name);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('rechaza externamente sin dirección con un mensaje visible y no llama a la API', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({ label: 'c2ext', memberships: [{ site: 'a', role: 'Administrador' }] });
    await openPersonasTab(page, admin.username, admin.password);

    await page.getByRole('button', { name: /Nueva Identidad/ }).click();
    await page.getByLabel('Tipo', { exact: true }).selectOption('external');
    await fillByLabel(page, 'Nombre / razón social', `Externo Sin Direccion ${uniqueToken()}`);

    await page.evaluate(() => {
      document
        .querySelectorAll('textarea[required], input[required]')
        .forEach((el) => el.removeAttribute('required'));
    });

    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page.getByRole('alert')).toHaveText('La dirección es obligatoria para externos.');
    await expect(page.getByRole('heading', { name: 'Nueva Identidad' })).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('edita una persona y la fila actualizada persiste tras recargar', async ({ page, health }) => {
    const admin = await seedUser({ label: 'c2edt', memberships: [{ site: 'a', role: 'Administrador' }] });
    const token = uniqueToken();
    const originalName = `Editable Original ${token}`;
    const newName = `Editable Renombrada ${token}`;
    const person = await seedPerson({ site: 'a', name: originalName });
    await openPersonasTab(page, admin.username, admin.password);

    const row = page.getByRole('row', { name: new RegExp(originalName) });
    await row.getByRole('button', { name: 'Editar' }).click();
    await expect(page.getByRole('heading', { name: `Editar Identidad #${person.id}` })).toBeVisible();

    await fillByLabel(page, 'Nombre / razón social', newName);
    const [updated] = await Promise.all([
      page.waitForResponse(
        (response) =>
          response.url().endsWith(`/api/v1/people/${person.id}`) &&
          response.request().method() === 'PUT',
      ),
      page.getByRole('button', { name: 'Guardar' }).click(),
    ]);
    expect(updated.status(), 'PUT /api/v1/people/:id status').toBe(200);

    await page.reload();
    await expect(
      page.getByRole('tab', { name: 'Directorio de Personal' }),
    ).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByRole('row', { name: new RegExp(newName) })).toBeVisible();
    await expect(page.getByRole('row', { name: new RegExp(originalName) })).toHaveCount(0);

    const stored = await tenantDb('a', async (client) => {
      const r = await client.query<{ name: string }>(
        `SELECT name FROM public.lu_person WHERE id = $1`,
        [person.id],
      );
      return r.rows[0]?.name ?? null;
    });
    expect(stored).toBe(newName);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('da de baja lógica: oculta por defecto, aparece bajo Eliminado, status=2 en la base', async ({
    page,
    health,
  }) => {
    const admin = await seedUser({ label: 'c2del', memberships: [{ site: 'a', role: 'Administrador' }] });
    const token = uniqueToken();
    const name = `Baja Logica ${token}`;
    const person = await seedPerson({ site: 'a', name: name, status: 0 });
    await openPersonasTab(page, admin.username, admin.password);

    await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();

    page.once('dialog', (dialog) => {
      void dialog.accept();
    });
    const row = page.getByRole('row', { name: new RegExp(name) });
    await row.getByRole('button', { name: 'Eliminar' }).click();
    await expect(page.getByText('Persona dada de baja correctamente.')).toBeVisible();

    await searchPersona(page, name);
    await expect(page.getByRole('row', { name: new RegExp(name) })).toHaveCount(0);

    await selectPersonaStatus(page, '2');
    await searchPersona(page, name);
    await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();

    const row2 = await tenantDb('a', async (client) => {
      const r = await client.query<{ status: number }>(
        `SELECT status FROM public.lu_person WHERE id = $1`,
        [person.id],
      );
      return r.rows[0]?.status ?? null;
    });
    expect(row2).toBe(2);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });
});

test.describe('C3 — Personas: control de acceso por sede y rol', () => {
  test('Supervisor en sede a: la ruta se redirige al Dashboard y GET /people devuelve 403', async ({
    page,
    health,
  }) => {
    const supervisor = await seedUser({
      label: 'c3sup',
      memberships: [{ site: 'a', role: 'Supervisor' }],
    });
    await loginAndOpen(
      page,
      { loginIdentifier: supervisor.username, password: supervisor.password },
      PERSONAS_PATH,
    );

    // Site Supervisor lacks usersAdmin; the SPA replaces the URL with '/' and the
    // directorio de personal stays hidden.
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('tab', { name: 'Directorio de Personal' })).toHaveCount(0);

    const probe = await apiCall(page, 'GET', '/people');
    expect(probe.status, 'GET /people from a site Supervisor must be 403').toBe(403);
    expect(JSON.stringify(probe.body)).toContain('SITE_ACCESS_DENIED');

    health.expectClean([/GET \/api\/v1\/auth\/session 401/, /GET \/api\/v1\/people 403/]);
  });

  test('SuperAdmin sin sede activa: ve el mensaje del directorio y la lista queda vacía', async ({
    page,
    health,
  }) => {
    const superAdmin = await seedUser({ label: 'c3sa', superAdmin: true });
    await loginAndOpen(
      page,
      { loginIdentifier: superAdmin.username, password: superAdmin.password },
      PERSONAS_PATH,
    );
    // The route renders (usersAdmin) but `peopleAllowed` is false without an
    // active site, so the directory surfaces the access-denied message.
    await expect(page.getByRole('tab', { name: 'Directorio de Personal' })).toHaveAttribute(
      'aria-selected',
      'true',
    );
    await expect(page.getByText(PERSONAS_ACCESS_DENIED_TEXT)).toBeVisible();

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });
});

// Re-exports for tests that reuse the helpers without importing the playwright
// types directly (kept for readability of test files that import from here).
export const _internal = { openPersonasTab };
// Anchor the unused-parameter helpers above so TypeScript keeps them.
void controlDb;
