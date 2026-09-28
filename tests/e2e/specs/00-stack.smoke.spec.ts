import { loginViaUi } from '../support/auth.js';
import { seedUser, stack } from '../support/index.js';
import { expect, test } from '../support/fixtures.js';

test('real stack: a site administrator logs in through the browser and reaches the workspace', async ({ page, health }) => {
  const admin = await seedUser({ label: 'smoke', memberships: [{ site: 'a', role: 'Administrador' }] });
  await page.goto('/');
  await loginViaUi(page, { loginIdentifier: admin.username, password: admin.password });
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard de Gestión' })).toBeVisible();
  await expect(page.getByRole('button', { name: admin.displayName })).toBeVisible();
  await expect(page.getByRole('button', { name: stack().sites.a.name })).toBeVisible();
  health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
});
