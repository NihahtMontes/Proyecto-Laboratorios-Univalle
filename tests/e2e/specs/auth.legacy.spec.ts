/**
 * Workstream A — legacy ASP.NET Identity migration scenarios. Runs SERIAL
 * because the deadline pre-conditions at the end of the file mutate the
 * singleton `public.lu_identity_migration_state` row whose CHECK makes the
 * deadline immutable once recorded (see
 * apps/api/src/database/schema-manifest-f2.ts:luIdentityMigrationStateFinalTable).
 *
 * Only this spec touches the legacy window; nothing else in the F7 suite uses
 * legacy users.
 */
import { expect, test } from '../support/fixtures.js';
import {
  apiCall,
  loginViaUi,
  openAuthenticated,
  submitLogin,
} from '../support/auth.js';
import { controlDb, readUser, seedUser, syntheticPassword } from '../support/db.js';
// loginViaUi waits for the workspace shell to render; the password-change
// view does NOT render that navigation. Use submitLogin (no shell wait) for
// restricted-view scenarios and assert the dedicated heading instead.
import {
  expectOnLogin,
  expectPasswordChangeHeading,
  expectWorkspaceHeading,
} from '../support/auth-flows.js';

test.describe.configure({ mode: 'serial' });

// ---------------------------------------------------------------------------
// A17 (a): ASP.NET Identity V2 — login through the UI rehashed to bcrypt on
// first touch and stays authenticable with the same plaintext afterwards.
// ---------------------------------------------------------------------------

test('A17a ASP.NET Identity V2 rehashes on first login and a second login with the same password succeeds', async ({
  page,
  health,
}) => {
  const legacy = await seedUser({
    label: 'a17v2',
    passwordKind: 'legacy_v2',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  const before = await readUser(legacy.id);
  expect(before.password_scheme).toBe('legacy_identity_v2');
  expect(before.password_migrated_at).toBeNull();

  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: legacy.username, password: legacy.password });
  await expectWorkspaceHeading(page);

  const after = await readUser(legacy.id);
  expect(after.password_scheme).toBe('bcrypt');
  expect(after.bcrypt_prefix?.startsWith('$2')).toBe(true);
  expect(after.password_migrated_at).not.toBeNull();
  expect(after.must_change_password).toBe(false);

  // Second login with the same plaintext password still works (now stored as bcrypt).
  await logoutForReuse(page, legacy.displayName);
  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: legacy.username, password: legacy.password });
  await expectWorkspaceHeading(page);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A17 (b): ASP.NET Identity V3 — same contract.
// ---------------------------------------------------------------------------

test('A17b ASP.NET Identity V3 rehashes on first login and a second login with the same password succeeds', async ({
  page,
  health,
}) => {
  const legacy = await seedUser({
    label: 'a17v3',
    passwordKind: 'legacy_v3',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  const before = await readUser(legacy.id);
  expect(before.password_scheme).toBe('legacy_identity_v3');

  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: legacy.username, password: legacy.password });
  await expectWorkspaceHeading(page);

  const after = await readUser(legacy.id);
  expect(after.password_scheme).toBe('bcrypt');
  expect(after.bcrypt_prefix?.startsWith('$2')).toBe(true);
  expect(after.password_migrated_at).not.toBeNull();
  expect(after.must_change_password).toBe(false);

  await logoutForReuse(page, legacy.displayName);
  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: legacy.username, password: legacy.password });
  await expectWorkspaceHeading(page);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A17 (c): legacy_v3 with an 80-byte ASCII password (>72 bytes) -> forced
// password-change view with the policy-text variant (mustChangePassword=false
// in the response), the DB row stays legacy_identity_v3, and a valid change
// reaches the workspace.
// ---------------------------------------------------------------------------

test('A17c a legacy v3 password longer than 72 bytes lands on the forced change (mustChangePassword=false text) and rehashes after a valid change', async ({
  page,
  health,
}) => {
  // 80 ASCII bytes — strictly longer than bcrypt's 72-byte ceiling.
  const longPlain = 'Legacy-V3-Pass-' + 'A'.repeat(60); // 14 + 60 = 74, pad a bit more
  const plain = (longPlain + 'Zz9!').slice(0, 80);
  // Persist with the exact 80-byte plaintext.
  const legacy = await seedUser({
    label: 'a17v3long',
    password: plain,
    passwordKind: 'legacy_v3',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  const initial = await readUser(legacy.id);
  expect(initial.password_scheme).toBe('legacy_identity_v3');

  await page.goto('/');
  // The session lands on the forced change view; loginViaUi would time out
  // waiting for the workspace navigation which is not rendered here.
  await submitLogin(page, { loginIdentifier: legacy.username, password: plain });
  await expectPasswordChangeHeading(page);
  await expect(page.getByText(/no cumple la política vigente/)).toBeVisible();

  // Still legacy after the partial step: the forced change has not yet committed.
  const during = await readUser(legacy.id);
  expect(during.password_scheme).toBe('legacy_identity_v3');

  const newPassword = syntheticPassword('A17long');
  await page.getByLabel('Contraseña actual').fill(plain);
  await page.getByRole('textbox', { name: 'Nueva contraseña *', exact: true }).fill(newPassword);
  await page.getByLabel('Confirmar nueva contraseña').fill(newPassword);
  await page.getByRole('button', { name: 'Cambiar contraseña' }).click();
  await expectWorkspaceHeading(page);

  const after = await readUser(legacy.id);
  expect(after.password_scheme).toBe('bcrypt');
  expect(after.bcrypt_prefix?.startsWith('$2')).toBe(true);
  expect(after.must_change_password).toBe(false);

  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});

// ---------------------------------------------------------------------------
// A17 (d): wrong password on a legacy account -> generic rejection, scheme
// unchanged.
// ---------------------------------------------------------------------------

test('A17d a wrong password on a legacy account rejects with the generic message and leaves the scheme unchanged', async ({
  page,
  health,
}) => {
  const legacy = await seedUser({
    label: 'a17v3wrong',
    passwordKind: 'legacy_v3',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');
  await submitLogin(page, { loginIdentifier: legacy.username, password: 'TOTALLY-WRONG-pw#1' });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);

  const after = await readUser(legacy.id);
  expect(after.password_scheme).toBe('legacy_identity_v3');

  health.expectClean([/GET \/api\/v1\/auth\/session 401/, /POST \/api\/v1\/auth\/login 401/]);
});

// ---------------------------------------------------------------------------
// A17 (e): the LAST test in this file — record the cutover singleton with a
// 90-day deadline already in the past, then a fresh legacy_v3 user with the
// CORRECT password must be refused. The state is immutable once recorded, so
// this is the final test of the serial file (no other spec uses legacy).
// ---------------------------------------------------------------------------

test('A17e after the legacy password deadline, a fresh legacy_v3 login is refused (generic alert, scheme unchanged)', async ({
  page,
  health,
}) => {
  // The state CHECK requires deadline = cutover_at + INTERVAL '90 days'.
  // Per the task spec we set cutover=now-91d and deadline=now-1d so the
  // recorded window has clearly elapsed.
  await controlDb(async (c) => {
    await c.query(`DELETE FROM public.lu_identity_migration_state WHERE id = 1`);
    await c.query(
      `INSERT INTO public.lu_identity_migration_state
         (id, writer_label, cutover_at, legacy_password_deadline)
       SELECT 1, 'NEST_POSTGRES', t - interval '91 days', t - interval '1 day'
         FROM (SELECT date_trunc('second', now()) AS t) s`,
    );
  });

  const legacy = await seedUser({
    label: 'a17overrun',
    passwordKind: 'legacy_v3',
    memberships: [{ site: 'a', role: 'Administrador' }],
  });

  await page.goto('/');
  await submitLogin(page, { loginIdentifier: legacy.username, password: legacy.password });
  await expect(page.getByRole('alert')).toHaveText('Usuario o contraseña incorrectos.');
  await expectOnLogin(page);

  const after = await readUser(legacy.id);
  expect(after.password_scheme).toBe('legacy_identity_v3');

  // No session was issued: the bootstrap probe stays unauthenticated.
  const probe = await apiCall(page, 'GET', '/auth/session');
  expect(probe.status).toBe(401);

  // The refused login (deadline reached) is an intentional 401.
  health.expectClean([/GET \/api\/v1\/auth\/session 401/, /POST \/api\/v1\/auth\/login 401/]);
});

// ---------------------------------------------------------------------------
// Local helpers
// ---------------------------------------------------------------------------

/**
 * Reveals the user menu and clicks "Cerrar Sesión". Reusable across the four
 * re-login sub-scenarios above.
 */
async function logoutForReuse(
  page: import('@playwright/test').Page,
  displayName: string,
): Promise<void> {
  const userButton = page.getByRole('button', { name: displayName });
  await userButton.click();
  const signOut = page.getByRole('button', { name: 'Cerrar Sesión' });
  await expect(signOut).toBeVisible();
  await signOut.click();
  await expectOnLogin(page);
}

// Silence unused-import lint marks for symbols used by neighbouring specs.
void openAuthenticated;
