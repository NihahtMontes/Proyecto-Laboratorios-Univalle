import { expect, type APIResponse, type BrowserContext, type Page } from '@playwright/test';
import { BASE_URL } from './env.js';

export interface Credentials {
  readonly loginIdentifier: string;
  readonly password: string;
}

/** Fills and submits the real login form; does not assert the outcome. */
export async function submitLogin(page: Page, creds: Credentials): Promise<void> {
  if (!page.url().startsWith(BASE_URL)) await page.goto('/');
  await expect(page.getByRole('button', { name: 'INICIAR SESIÓN' })).toBeVisible();
  await page.getByLabel('Usuario o Correo').fill(creds.loginIdentifier);
  await page.getByLabel('Contraseña', { exact: true }).fill(creds.password);
  await page.getByRole('button', { name: 'INICIAR SESIÓN' }).click();
}

/** Logs in through the UI and waits until the authenticated shell is rendered. */
export async function loginViaUi(page: Page, creds: Credentials): Promise<void> {
  await submitLogin(page, creds);
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
}

/**
 * Logs in through the real HTTP API from inside the page (same origin, same
 * cookie jar as the browser), exactly as the React client does: CSRF token first,
 * then POST /auth/login. Faster than the form for tests whose subject is not login.
 */
export async function loginViaApi(
  page: Page,
  creds: Credentials,
  activeSiteId: string | null = null,
): Promise<{ status: number; purpose: string | null }> {
  if (!page.url().startsWith(BASE_URL)) await page.goto('/');
  return page.evaluate(
    async ({ creds, activeSiteId }) => {
      const csrf = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin' });
      const { csrfToken } = (await csrf.json()) as { csrfToken: string };
      const body: Record<string, unknown> = { loginIdentifier: creds.loginIdentifier, password: creds.password };
      if (activeSiteId !== null) body['activeSiteId'] = activeSiteId;
      const res = await fetch('/api/v1/auth/login', {
        method: 'POST',
        credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-Token': csrfToken },
        body: JSON.stringify(body),
      });
      const purpose = await res
        .json()
        .then((json: { meta?: { purpose?: string } }) => json.meta?.purpose ?? null)
        .catch(() => null);
      return { status: res.status, purpose };
    },
    { creds, activeSiteId },
  );
}

/** Logs in via API and opens `path` with the authenticated shell visible. */
export async function openAuthenticated(page: Page, creds: Credentials, path = '/', activeSiteId: string | null = null): Promise<void> {
  const result = await loginViaApi(page, creds, activeSiteId);
  expect(result.status, 'login status').toBe(200);
  await page.goto(path);
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
}

export interface ApiCallResult {
  readonly status: number;
  readonly body: unknown;
}

/**
 * Sends a raw API request from the page with the session cookie and a fresh CSRF
 * token. Used for server-enforcement probes (the UI hides the action; the server
 * must still refuse it).
 */
export async function apiCall(
  page: Page,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  path: string,
  body?: unknown,
): Promise<ApiCallResult> {
  return page.evaluate(
    async ({ method, path, body }) => {
      const csrf = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin' });
      const { csrfToken } = (await csrf.json()) as { csrfToken: string };
      const headers: Record<string, string> = { 'X-CSRF-Token': csrfToken };
      if (body !== undefined) headers['Content-Type'] = 'application/json';
      const res = await fetch(`/api/v1${path}`, {
        method,
        credentials: 'same-origin',
        headers,
        body: body === undefined ? undefined : JSON.stringify(body),
      });
      const text = await res.text();
      let parsed: unknown = text;
      try {
        parsed = JSON.parse(text);
      } catch {
        /* keep text */
      }
      return { status: res.status, body: parsed };
    },
    { method, path, body },
  );
}

export async function cookieNames(context: BrowserContext): Promise<string[]> {
  return (await context.cookies()).map((c) => c.name);
}

export function isJson(res: APIResponse): boolean {
  return (res.headers()['content-type'] ?? '').includes('application/json');
}
