import { expect, test } from '../support/fixtures.js';
import { seedUser } from '../support/index.js';
import {
  assertNoPageHorizontalOverflow,
  assertTableScrollsInsideContainer,
  openResponsiveSidebar,
} from '../support/ui-checks.js';

interface SurfaceCase {
  readonly label: string;
  readonly path: string;
  readonly headingName: string;
  readonly headingLevel?: 1 | 2 | 3 | 4 | 5 | 6;
  readonly expectsWideTable?: boolean;
  readonly primaryField?: { readonly label: string; readonly fillWith: string };
}

const SURFACES: readonly SurfaceCase[] = [
  {
    label: 'login',
    path: '/',
    headingName: 'Ingreso al Sistema',
    headingLevel: 2,
    primaryField: { label: 'Usuario o Correo', fillWith: 'responsive-login' },
  },
  {
    label: 'dashboard-shell',
    path: '/',
    headingName: 'Dashboard de Gestión',
    headingLevel: 1,
  },
  {
    label: 'users-index',
    path: '/Users/Index',
    headingName: 'Control de Usuarios',
    headingLevel: 3,
    expectsWideTable: true,
    primaryField: { label: 'Buscar usuario', fillWith: 'responsive-search' },
  },
  {
    label: 'personas-tab',
    path: '/Users/Index?tab=personas',
    headingName: 'Directorio Base de Identidades',
    headingLevel: 3,
    expectsWideTable: true,
    primaryField: { label: 'Buscar persona', fillWith: 'responsive-search' },
  },
  {
    label: 'create-user',
    path: '/Users/Create',
    headingName: 'Alta de Cuenta de Usuario',
    headingLevel: 4,
    primaryField: { label: 'Nombres', fillWith: 'ResponsiveName' },
  },
  {
    label: 'profile',
    path: '/Profile',
    headingName: 'Mi Perfil',
    headingLevel: 1,
    primaryField: { label: 'Nombres', fillWith: 'ResponsiveName' },
  },
];

/**
 * Users details requires a target user id; the admin viewing their own record
 * would render the same UI surface, so we seed a sibling and target it.
 */
async function detailsSurface(label: string): Promise<SurfaceCase> {
  const target = await seedUser({
    label: `${label}-tgt`,
    memberships: [{ site: 'a', role: 'Administrador' }],
  });
  return {
    label: 'users-details',
    path: `/Users/Details/${target.id}`,
    headingName: 'Detalle de Usuario',
    headingLevel: 1,
    expectsWideTable: true,
  };
}

const VIEWPORTS: ReadonlyArray<{ readonly name: string; readonly width: number; readonly height: number }> = [
  { name: 'mobile-portrait-490x900', width: 490, height: 900 },
  { name: 'tablet-portrait-768x1024', width: 768, height: 1024 },
  { name: 'desktop-1440-baseline', width: 1440, height: 900 },
];

for (const viewport of VIEWPORTS) {
  test.describe(`C8 — Responsive smoke en ${viewport.name}`, () => {
    test('login no se desborda horizontalmente y los campos son operables', async ({
      page,
      health,
    }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await page.goto('/');
      await expect(page.getByRole('heading', { name: 'Ingreso al Sistema' })).toBeVisible();
      await assertNoPageHorizontalOverflow(page);

      const identifier = page.getByLabel('Usuario o Correo');
      const password = page.getByLabel('Contraseña', { exact: true });
      await identifier.fill(`responsive-${viewport.name}`);
      await password.fill('responSive#1Aa2');
      await expect(identifier).toHaveValue(`responsive-${viewport.name}`);
      await expect(password).toHaveValue('responSive#1Aa2');

      health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
    });

    for (const surface of SURFACES.slice(1)) {
      test(`${surface.label}: sin desbordamiento y elementos primarios visibles`, async ({
        page,
        health,
      }) => {
        await page.setViewportSize({ width: viewport.width, height: viewport.height });
        const admin = await seedUser({
          label: `c8-${viewport.name}-${surface.label}`,
          memberships: [{ site: 'a', role: 'Administrador' }],
        });
        await loginAuth(page, admin.username, admin.password, surface.path);
        await expect(
          page.getByRole('heading', { level: surface.headingLevel ?? 1, name: surface.headingName }),
        ).toBeVisible();
        await assertNoPageHorizontalOverflow(page);
        if (surface.primaryField !== undefined) {
          const field = page.getByLabel(surface.primaryField.label);
          await expect(field).toBeVisible();
          await field.fill(surface.primaryField.fillWith);
          await expect(field).toHaveValue(surface.primaryField.fillWith);
        }
        if (surface.expectsWideTable === true) {
          await assertTableScrollsInsideContainer(page);
        }
        await ensureNavigationVisible(page, viewport.width);

        health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
      });
    }

    test('users-details (sibling): la ficha administrativa cabe sin desbordar', async ({
      page,
      health,
    }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const surface = await detailsSurface(`c8-${viewport.name}-det`);
      const admin = await seedUser({
        label: `c8-${viewport.name}-det-admin`,
        memberships: [{ site: 'a', role: 'Administrador' }],
      });
      await loginAuth(page, admin.username, admin.password, surface.path);
      await expect(
        page.getByRole('heading', { level: surface.headingLevel ?? 1, name: surface.headingName }),
      ).toBeVisible();
      await assertNoPageHorizontalOverflow(page);
      await assertTableScrollsInsideContainer(page);
      await ensureNavigationVisible(page, viewport.width);

      health.expectClean([/GET \/api\/v1\/auth\/session 401/]);
    });
  });
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

async function loginAuth(
  page: import('@playwright/test').Page,
  username: string,
  password: string,
  path: string,
): Promise<void> {
  const { loginAndOpen } = await import('../support/ui-checks.js');
  await loginAndOpen(page, { loginIdentifier: username, password }, path);
}

/**
 * At >= md (≥ 768px) the sidebar is visible; below that the responsive
 * toggle ("Abrir menú") is the only way to surface the navigation. The helper
 * opens the toggle on small viewports and confirms a known sidebar entry is
 * reachable so the navigation is exercised end-to-end.
 */
async function ensureNavigationVisible(
  page: import('@playwright/test').Page,
  width: number,
): Promise<void> {
  const topbarNav = page.getByRole('navigation', { name: 'Cabecera principal' });
  await expect(topbarNav).toBeVisible();

  const sidebarNav = page.getByRole('navigation', { name: 'Navegación principal' });
  if (width < 768) {
    await openResponsiveSidebar(page);
    const shellHasOpenSidebar = await page.evaluate(() =>
      document.getElementById('main-wrapper')?.classList.contains('show-sidebar') ?? false,
    );
    expect(shellHasOpenSidebar, 'show-sidebar class must be present after the toggle').toBe(true);
  }

  // The "Usuarios" entry inside the sidebar is a collapsible group button;
  // expand it so the underlying <a> link becomes visible.
  const groupToggle = sidebarNav.getByRole('button', { name: 'Usuarios' });
  await expect(groupToggle).toBeVisible();
  const expanded = await groupToggle.getAttribute('aria-expanded');
  if (expanded !== 'true') {
    await groupToggle.click();
  }
  const sidebarLink = sidebarNav.getByRole('link', { name: 'Usuarios' });
  await expect(sidebarLink).toBeVisible();
}
