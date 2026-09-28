import { expect, test } from '../support/fixtures.js';
import { controlDb, readUser } from '../support/index.js';
import {
  TINY_PNG_BYTES,
  TINY_PNG_MIME,
  TINY_PNG_NAME,
  fillByLabel,
  loginAndOpen,
} from '../support/ui-checks.js';

/** Top-level page title is h1 "Mi Perfil"; the panel inside uses an h6 "Mi perfil". */
const PROFILE_HEADING = { level: 1, name: 'Mi Perfil', exact: true } as const;

async function openProfile(
  page: import('@playwright/test').Page,
  username: string,
  password: string,
): Promise<void> {
  await loginAndOpen(page, { loginIdentifier: username, password }, '/Profile');
  await expect(page.getByRole('heading', PROFILE_HEADING)).toBeVisible();
}

function profileInitialsImg(page: import('@playwright/test').Page, fullName: string) {
  return page.getByRole('img', { name: `Iniciales de ${fullName}` });
}

function profilePhotoImg(page: import('@playwright/test').Page, fullName: string) {
  return page.getByRole('img', { name: `Foto de ${fullName}` });
}

function topbarUserButton(
  page: import('@playwright/test').Page,
  displayName: string,
) {
  return page.getByRole('button', { name: displayName });
}

test.describe('C4 — Perfil: identidad, edición y correo propio', () => {
  test('muestra los datos reales y deja la tarjeta de Auditoría ausente (J-05)', async ({
    page,
    health,
  }) => {
    const user = await seedUserForProfile('c4show');
    await openProfile(page, user.username, user.password);

    await expect(page.getByLabel('Nombres')).toHaveValue(user.firstName);
    await expect(page.getByLabel('Apellido paterno')).toHaveValue(user.lastName);
    await expect(page.getByLabel('Correo institucional')).toHaveValue(user.email);
    await expect(page.getByLabel('Usuario / login')).toHaveValue(user.username);
    await expect(page.getByLabel('C.I.')).toHaveValue(user.identityCard);

    await expect(page.getByLabel('C.I.')).toHaveAttribute('readonly', '');
    await expect(page.getByLabel('Usuario / login')).toHaveAttribute('readonly', '');

    await expect(page.getByRole('heading', { name: 'Auditoria' })).toHaveCount(0);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('edita nombres y teléfono; recarga preserva el cambio en la base y actualiza la cabecera', async ({
    page,
    health,
  }) => {
    const user = await seedUserForProfile('c4edt');
    await openProfile(page, user.username, user.password);

    const newFirst = 'EditadoNombre';
    const newLast = 'EditadoApellido';
    const newPhone = `7${user.id.replace(/\D/g, '').slice(0, 7).padEnd(7, '0')}`;
    await fillByLabel(page, 'Nombres', newFirst);
    await fillByLabel(page, 'Apellido paterno', newLast);
    await fillByLabel(page, 'Telefono', newPhone);

    const [updated] = await Promise.all([
      page.waitForResponse(
        (response) =>
          response.url().endsWith('/api/v1/profile') && response.request().method() === 'PUT',
      ),
      page.getByRole('button', { name: 'Guardar perfil' }).click(),
    ]);
    expect(updated.status(), 'PUT /api/v1/profile status').toBe(200);

    await expect(page.getByText('Perfil actualizado correctamente.')).toBeVisible();

    const refreshedButton = topbarUserButton(page, `${newFirst} ${newLast}`);
    await expect(refreshedButton).toBeVisible();

    await page.reload();
    await expect(page.getByRole('heading', PROFILE_HEADING)).toBeVisible();
    await expect(page.getByLabel('Nombres')).toHaveValue(newFirst);
    await expect(page.getByLabel('Apellido paterno')).toHaveValue(newLast);
    await expect(page.getByLabel('Telefono')).toHaveValue(newPhone);

    const stored = await readUser(user.id);
    expect(stored.first_name).toBe(newFirst);
    expect(stored.last_name).toBe(newLast);
    expect(stored.phone_number).toBe(newPhone);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });

  test('cambia el correo propio: la sesión sigue válida y el nuevo correo permite iniciar sesión', async ({
    page,
    health,
    context,
  }) => {
    const user = await seedUserForProfile('c4mail');
    await openProfile(page, user.username, user.password);

    const newEmail = `renombrado.${user.id.slice(0, 8)}@f7.example.invalid`;
    await fillByLabel(page, 'Correo institucional', newEmail);

    await page.getByRole('button', { name: 'Guardar perfil' }).click();
    await expect(page.getByText('Perfil actualizado correctamente.')).toBeVisible();

    const sessionProbe = await page.evaluate(async () => {
      const res = await fetch('/api/v1/auth/session', { credentials: 'same-origin' });
      return { status: res.status };
    });
    expect(sessionProbe.status, 'session after self email change').toBe(200);

    await page.goto('/Profile');
    await expect(page.getByRole('heading', PROFILE_HEADING)).toBeVisible();
    await expect(page.getByLabel('Correo institucional')).toHaveValue(newEmail);

    await context.clearCookies();
    await page.goto('/');
    await page.getByLabel('Usuario o Correo').fill(newEmail);
    await page.getByLabel('Contraseña', { exact: true }).fill(user.password);
    await page.getByRole('button', { name: 'INICIAR SESIÓN' }).click();
    await expect(
      page.getByRole('navigation', { name: 'Cabecera principal' }),
    ).toBeVisible();

    const dbRow = await readUser(user.id);
    expect(dbRow.email).toBe(newEmail);

    health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
  });
});

test.describe('C5 — Foto de perfil: subir, persistir y quitar', () => {
  test('sube un PNG mínimo, aparece en perfil y cabecera; quitar restaura las iniciales', async ({
    page,
    health,
    context,
  }) => {
    const user = await seedUserForProfile('c5photo');
    const fullName = `${user.firstName} ${user.lastName}`;
    await openProfile(page, user.username, user.password);

    const initialsImg = profileInitialsImg(page, fullName);
    await expect(initialsImg).toBeVisible();
    await expect(profilePhotoImg(page, fullName)).toHaveCount(0);
    const userButton = topbarUserButton(page, fullName).first();
    await expect(userButton.locator('img')).toHaveCount(0);
    await expect(userButton).toContainText(initialsOf(user.firstName, user.lastName));

    await uploadProfilePhoto(page, TINY_PNG_BYTES, TINY_PNG_NAME, TINY_PNG_MIME);

    await expect(page.getByText('Foto de perfil actualizada.')).toBeVisible();

    await expect(initialsImg).toHaveCount(0);
    const photoImg = profilePhotoImg(page, fullName);
    await expect(photoImg).toBeVisible();

    const reloadedUserButton = topbarUserButton(page, fullName).first();
    await expect(reloadedUserButton.locator('img')).toHaveCount(1);

    await context.clearCookies();
    await openProfile(page, user.username, user.password);
    await expect(profilePhotoImg(page, fullName)).toHaveCount(1);
    await expect(topbarUserButton(page, fullName).first().locator('img')).toHaveCount(1);

    page.once('dialog', (dialog) => {
      void dialog.accept();
    });
    await Promise.all([
      page.waitForResponse(
        (response) =>
          response.url().endsWith('/api/v1/profile/photo') &&
          response.request().method() === 'DELETE',
      ),
      page.getByRole('button', { name: 'Quitar foto' }).click(),
    ]);
    await expect(page.getByText('Foto de perfil eliminada.')).toBeVisible();

    await page.reload();
    await expect(profilePhotoImg(page, fullName)).toHaveCount(0);
    await expect(profileInitialsImg(page, fullName)).toBeVisible();

    health.expectClean([
      /GET \/api\/v1\/auth\/session 401/,
      /GET \/api\/v1\/profile\/photo 200/,
    ]);
  });
});

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

interface SeededProfileActor {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly identityCard: string;
}

async function seedUserForProfile(label: string): Promise<SeededProfileActor> {
  const seeded = await (await import('../support/index.js')).seedUser({
    label,
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  return {
    id: seeded.id,
    username: seeded.username,
    email: seeded.email,
    password: seeded.password,
    firstName: seeded.firstName,
    lastName: seeded.lastName,
    identityCard: seeded.identityCard,
  };
}

function initialsOf(firstName: string, lastName: string): string {
  const tokens = `${firstName} ${lastName}`.trim().split(/\s+/);
  return (
    tokens
      .slice(0, 2)
      .map((token) => token.charAt(0).toUpperCase())
      .join('') || 'US'
  );
}

async function uploadProfilePhoto(
  page: import('@playwright/test').Page,
  bytes: Readonly<Uint8Array>,
  fileName: string,
  mime: string,
): Promise<void> {
  const fileInput = page.locator('input[aria-label="Seleccionar foto de perfil"]');
  await fileInput.setInputFiles({
    name: fileName,
    mimeType: mime,
    buffer: Buffer.from(bytes),
  });
  await page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/profile/photo') && response.request().method() === 'PUT',
  );
}

void controlDb;
