/**
 * Shared UI assertions and fixtures for the People / Profile / Errors /
 * Navigation / Responsive specs. No product code is modified; every helper
 * here derives its selectors from the existing web/src/ sources.
 */
import { expect, type Locator, type Page } from '@playwright/test';
import { loginViaApi } from './auth.js';

/**
 * 67-byte 1x1 transparent PNG (RGBA). The validator in
 * apps/api/src/users/profile-photo/profile-photo.validator.ts requires the
 * declared MIME, the file extension and the magic signature to agree; the
 * `89 50 4E 47 0D 0A 1A 0A` prefix below is the canonical PNG signature so
 * `detectProfilePhotoFormat` returns 'png'.
 */
export const TINY_PNG_BYTES: Readonly<Uint8Array> = Uint8Array.from([
  0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
  0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1f, 0x15, 0xc4,
  0x89, 0x00, 0x00, 0x00, 0x0b, 0x49, 0x44, 0x41,
  0x54, 0x78, 0x9c, 0x62, 0x00, 0x01, 0x00, 0x00,
  0x05, 0x00, 0x01, 0x0d, 0x0a, 0x2d, 0xb4, 0x00,
  0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44, 0xae,
  0x42, 0x60, 0x82,
]);

export const TINY_PNG_NAME = 'f7-tiny.png';
export const TINY_PNG_MIME = 'image/png';

/** Visible across the suite: typed characters and `pressSequentially` for forms. */
export async function fillByLabel(page: Page, label: string, value: string): Promise<void> {
  await page.getByLabel(label).fill(value);
}

/**
 * Opens the responsive sidebar via the "Abrir menú" toggle on small viewports.
 * The button is hidden via `d-md-none`; on wide screens it has no effect on the
 * already-visible sidebar, so this helper is safe to call unconditionally.
 */
export async function openResponsiveSidebar(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Abrir menú' }).click();
}

/** Asserts the document scrolling element does not overflow the viewport. */
export async function assertNoPageHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => {
    const element = document.scrollingElement ?? document.documentElement;
    return {
      scrollWidth: element.scrollWidth,
      clientWidth: element.clientWidth,
    };
  });
  expect(
    overflow.scrollWidth,
    `page overflows horizontally: scrollWidth=${overflow.scrollWidth} clientWidth=${overflow.clientWidth}`,
  ).toBeLessThanOrEqual(overflow.clientWidth + 1);
}

/**
 * The People table sits inside a `div.table-responsive` whose own scrollWidth
 * may exceed the page; the page itself must not scroll horizontally. Tables
 * that overflow should scroll inside their own container, not the document.
 */
export async function assertTableScrollsInsideContainer(page: Page): Promise<void> {
  const tables = page.locator('.table-responsive table');
  const count = await tables.count();
  expect(count, 'expected at least one table on the page').toBeGreaterThan(0);
  const container = page.locator('.table-responsive').first();
  const sizes = await container.evaluate((element) => {
    const table = element.querySelector('table');
    if (table === null) return null;
    return {
      containerClientWidth: element.clientWidth,
      tableScrollWidth: table.scrollWidth,
    };
  });
  expect(sizes, 'table container must contain a table').not.toBeNull();
  if (sizes !== null && sizes.tableScrollWidth > sizes.containerClientWidth) {
    expect(
      sizes.tableScrollWidth,
      'wide table should scroll inside its .table-responsive container',
    ).toBeGreaterThan(sizes.containerClientWidth);
  }
  await assertNoPageHorizontalOverflow(page);
}

/** Logs in through the API (no UI round-trip) and waits for the topbar shell. */
export async function loginAndOpen(
  page: Page,
  credentials: { readonly loginIdentifier: string; readonly password: string },
  path = '/',
  activeSiteId: string | null = null,
): Promise<void> {
  const result = await loginViaApi(page, credentials, activeSiteId);
  expect(result.status, 'login status').toBe(200);
  await page.goto(path);
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
}

/** Typed `getByRole('button', { name: displayName })` helper for the user dropdown. */
export function topbarUserButton(page: Page, displayName: string): Locator {
  return page.getByRole('button', { name: displayName });
}

/**
 * Waits for the API call that follows the form submit and asserts the status.
 * The path uses the `/api/v1` prefix as the suite does, and the request method
 * is matched exactly (e.g. POST /api/v1/people).
 */
export async function waitForApiResponse(
  page: Page,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  path: RegExp,
): Promise<{ status: number; body: unknown }> {
  const response = await page.waitForResponse((candidate) => {
    if (candidate.request().method() !== method) return false;
    const url = new URL(candidate.url());
    return path.test(url.pathname);
  });
  const body: unknown = await response.json().catch(() => null);
  return { status: response.status(), body };
}

/**
 * Field-level error rendering for People / Users 400 responses is implemented
 * in apps/web/src/apiErrors.ts: it joins `fieldErrors` as "field: messages".
 * The inline alert must not contain raw exception bodies or stack traces.
 */
export const RAW_LEAK_REGEX = /\bat \w+ \(|SQLSTATE|\bpg_|lu_[a-z_]+|Error: |ECONN|duplicate key/i;
