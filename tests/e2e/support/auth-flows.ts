/**
 * Shared helpers for the F7 auth / session / password / site-selection / legacy
 * suite. These tests own the entire UX of an auth flow: navigating the real
 * React app, typing into the real form, asserting visible Spanish strings, and
 * verifying backend effects with the disposable DB fixtures.
 *
 * Only the UI selectors derived from `apps/web/src/App.tsx` and
 * `apps/web/src/UsersPanel.tsx` are used here; no nth-child, generated classes
 * or fixed sleeps.
 */
import { expect, type Page } from '@playwright/test';
import {
  apiCall,
  loginViaUi,
  openAuthenticated,
} from './auth.js';
import { readUser } from './db.js';
import type { StackFixtures } from './env.js';

export { loginViaUi, openAuthenticated, apiCall, readUser };
export type { Credentials } from './auth.js';
export type { SeededUser } from './db.js';

export interface DesktopSurface {
  readonly heading: () => ReturnType<Page['getByRole']>;
  readonly navigation: () => ReturnType<Page['getByRole']>;
  readonly loginForm: () => ReturnType<Page['getByRole']>;
  readonly activeSiteButton: () => ReturnType<Page['getByRole']>;
}

export function desktopSurface(page: Page): DesktopSurface {
  return {
    heading: () => page.getByRole('heading', { level: 1, name: 'Dashboard de Gestión' }),
    navigation: () => page.getByRole('navigation', { name: 'Cabecera principal' }),
    loginForm: () => page.getByRole('button', { name: 'INICIAR SESIÓN' }),
    activeSiteButton: () => page.getByRole('button', { name: /Sede/ }),
  };
}

export async function expectOnLogin(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { name: 'Ingreso al Sistema' })).toBeVisible();
  await expect(page.getByLabel('Usuario o Correo')).toBeVisible();
  await expect(page.getByLabel('Contraseña', { exact: true })).toBeVisible();
}

export async function expectWorkspaceHeading(page: Page): Promise<void> {
  await expect(desktopSurface(page).navigation()).toBeVisible();
  await expect(desktopSurface(page).heading()).toBeVisible();
}

export async function expectSitePickerHeading(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { level: 1, name: 'Selecciona una sede' })).toBeVisible();
}

export async function expectPasswordChangeHeading(page: Page): Promise<void> {
  await expect(
    page.getByRole('heading', { level: 1, name: 'Cambio de contraseña requerido' }),
  ).toBeVisible();
}

export async function expectSuperAdminWelcome(page: Page, firstName: string): Promise<void> {
  await expect(page.getByRole('heading', { name: `Bienvenido, ${firstName}` })).toBeVisible();
}

export async function readActiveSession(
  page: Page,
): Promise<{ status: number; body: { meta?: { purpose?: string } } | null }> {
  const result = await apiCall(page, 'GET', '/auth/session');
  const body =
    typeof result.body === 'object' && result.body !== null
      ? (result.body as { meta?: { purpose?: string } })
      : null;
  return { status: result.status, body };
}

/** Reveals the user menu (display-name button) and clicks "Cerrar Sesión". */
export async function logoutFromTopBar(
  page: Page,
  displayName: string,
): Promise<{ signedOut: boolean }> {
  const userButton = page.getByRole('button', { name: displayName });
  await userButton.click();
  const signOut = page.getByRole('button', { name: 'Cerrar Sesión' });
  await expect(signOut).toBeVisible();
  await signOut.click();
  await expectOnLogin(page);
  return { signedOut: true };
}

export async function readLoginIdentifierLabel(page: Page, identifier: string): Promise<void> {
  const identifierField = page.getByLabel('Usuario o Correo');
  await identifierField.fill(identifier);
  await expect(identifierField).toHaveValue(identifier);
}

export function eligibleSiteNames(stack: StackFixtures): string[] {
  return [stack.sites.a.name, stack.sites.b.name];
}
