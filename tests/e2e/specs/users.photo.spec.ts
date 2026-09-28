import { expect, test } from '../support/fixtures.js';
import type { Cookie, Page } from '@playwright/test';
import { request as httpRequest } from 'node:http';
import { submitLogin } from '../support/auth.js';
import { BASE_URL } from '../support/env.js';
import { readProfilePictureKey, seedDualSiteUser, seedSiteAUser } from '../support/users-flows.js';

/**
 * Workstream B — profile picture flows (B14). Tiny images are generated at
 * runtime from base64 constants; no binary fixtures are committed.
 */

// ---------------------------------------------------------------------------
// Server raw fetch helpers — bypass the client's photoFileProblem UX gate
// without going through the client's fetch wrapper.
//
//   - Small bodies (type-mismatch probes) go through the browser fetch via
//     page.evaluate: the React client is never invoked, but the connection
//     handles small bodies cleanly so we observe the server status.
//   - Oversized bodies (>5 MiB) must be sent from the Node test process
//     because the browser fetch surfaces a "Failed to fetch" network reset
//     when the server closes the connection mid-upload.
// ---------------------------------------------------------------------------

async function rawPhotoPutViaBrowser(
  page: Page,
  userId: string,
  bytes: Buffer,
  contentType: string,
  fileName: string,
): Promise<number> {
  return page.evaluate(
    async ({ userId, base64, contentType, fileName }) => {
      const csrf = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin' });
      const { csrfToken } = (await csrf.json()) as { csrfToken: string };
      const decoded = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
      const res = await fetch(`/api/v1/users/${userId}/photo`, {
        method: 'PUT',
        credentials: 'same-origin',
        headers: {
          'Content-Type': contentType,
          'X-CSRF-Token': csrfToken,
          'X-Photo-Filename': encodeURIComponent(fileName),
        },
        body: decoded,
      });
      return res.status;
    },
    { userId, base64: bytes.toString('base64'), contentType, fileName },
  );
}

async function cookieHeader(page: Page): Promise<string> {
  const cookies: Cookie[] = await page.context().cookies();
  return cookies.map((c) => `${c.name}=${c.value}`).join('; ');
}

async function csrfFromPage(page: Page): Promise<string> {
  return page.evaluate(async () => {
    const csrf = await fetch('/api/v1/auth/csrf', { credentials: 'same-origin' });
    const { csrfToken } = (await csrf.json()) as { csrfToken: string };
    return csrfToken;
  });
}

/**
 * Raw PUT of the given image bytes from the Node test process. Returns the
 * status. Network resets (the 5 MiB+1 body truncates with a Fastify 413 then
 * a connection close) are rethrown with a clear message — the caller must
 * catch and translate them.
 */
async function rawPhotoPutViaNode(
  page: Page,
  userId: string,
  bytes: Buffer,
  contentType: string,
): Promise<number> {
  // The CSRF fetch rotates the CSRF cookie: read the cookies only afterwards.
  const csrfToken = await csrfFromPage(page);
  const cookieHeaderValue = await cookieHeader(page);
  const url = new URL(`${process.env['E2E_API_URL'] ?? 'http://127.0.0.1:3000'}/api/v1/users/${userId}/photo`);
  // The server must reject on the declared Content-Length alone (Fastify bodyLimit),
  // so the body is never streamed: streaming it only races the 413 against a reset.
  // If the server waited for body bytes instead, the timeout fails the probe.
  return new Promise<number>((resolve, reject) => {
    const timer = setTimeout(() => {
      req.destroy();
      reject(new Error(`rawPhotoPut: no status within 15 s for a declared ${bytes.length}-byte body`));
    }, 15_000);
    const req = httpRequest(
      {
        method: 'PUT',
        hostname: url.hostname,
        port: url.port,
        path: url.pathname,
        headers: {
          'Content-Type': contentType,
          'Content-Length': String(bytes.length),

          'X-CSRF-Token': csrfToken,
          'X-Photo-Filename': encodeURIComponent('probe.png'),
          Origin: BASE_URL,
          Cookie: cookieHeaderValue,
        },
      },
      (res) => {
        clearTimeout(timer);
        res.resume();
        resolve(res.statusCode ?? 0);
        req.destroy();
      },
    );
    req.on('error', (error) => {
      clearTimeout(timer);
      reject(new Error(`rawPhotoPut network failure for user ${userId} (${bytes.length} bytes): ${error.message}`));
    });
    req.flushHeaders();
  });
}

// ---------------------------------------------------------------------------
// Tiny 1x1 image fixtures (base64 strings, decoded at runtime).
// ---------------------------------------------------------------------------

const PNG_B64 =
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=';
const JPEG_B64 =
  '/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFAABAAAAAAAAAAAAAAAAAAAACf/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AKwA//9k=';
const WEBP_B64 =
  'UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAUAmJZQCdAEO/gbsAAA=';

function decodeB64(b64: string): Buffer {
  return Buffer.from(b64, 'base64');
}

// ---------------------------------------------------------------------------
// B14 — happy-path photo upload / change / remove.
// ---------------------------------------------------------------------------

test('B14 site-a admin uploads PNG, changes to JPEG, accepts WebP, and removes the photo on a single-site user', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await page.goto(`/Users/Details/${target.id}`);
  await submitLogin(page, { loginIdentifier: admin.username, password: admin.password });
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
  await page.goto(`/Users/Details/${target.id}`);
  await expect(page.getByRole('heading', { name: target.displayName })).toBeVisible();

  // Upload PNG: photo appears and DB key is populated.
  const [uploadResp] = await Promise.all([
    page.waitForResponse(
      (res) => res.url().endsWith(`/api/v1/users/${target.id}/photo`) && res.request().method() === 'PUT',
    ),
    page
      .getByLabel('Seleccionar foto de perfil')
      .setInputFiles({ name: 'avatar.png', mimeType: 'image/png', buffer: decodeB64(PNG_B64) }),
  ]);
  expect(uploadResp.status(), 'PUT /users/.../photo (PNG)').toBe(200);
  const pngImg = page.getByRole('img', { name: `Foto de ${target.displayName}` });
  await expect(pngImg).toBeVisible();
  await expect(pngImg).toHaveAttribute('src', /^blob:/);
  expect(await readProfilePictureKey(target.id)).not.toBeNull();
  const pngSrc = (await pngImg.getAttribute('src')) ?? '';

  // Change to JPEG: the image source changes (new object URL after re-fetch).
  const [changeResp] = await Promise.all([
    page.waitForResponse(
      (res) => res.url().endsWith(`/api/v1/users/${target.id}/photo`) && res.request().method() === 'PUT',
    ),
    page
      .getByLabel('Seleccionar foto de perfil')
      .setInputFiles({ name: 'avatar.jpg', mimeType: 'image/jpeg', buffer: decodeB64(JPEG_B64) }),
  ]);
  expect(changeResp.status(), 'PUT /users/.../photo (JPEG)').toBe(200);
  await expect(pngImg).toBeVisible();
  // Wait for the src to change — the new object URL appears only after the
  // usePhotoUrl effect resolves the re-fetch (auto-retry on the assertion).
  await expect(pngImg).not.toHaveAttribute('src', pngSrc);
  await expect(pngImg).toHaveAttribute('src', /^blob:/);
  expect(await readProfilePictureKey(target.id)).not.toBeNull();

  // WebP accepted.
  const [webpResp] = await Promise.all([
    page.waitForResponse(
      (res) => res.url().endsWith(`/api/v1/users/${target.id}/photo`) && res.request().method() === 'PUT',
    ),
    page
      .getByLabel('Seleccionar foto de perfil')
      .setInputFiles({ name: 'avatar.webp', mimeType: 'image/webp', buffer: decodeB64(WEBP_B64) }),
  ]);
  expect(webpResp.status(), 'PUT /users/.../photo (WebP)').toBe(200);
  expect(await readProfilePictureKey(target.id)).not.toBeNull();

  // Remove: window.confirm → initials avatar returns, key nulled.
  page.once('dialog', (d) => {
    void d.accept();
  });
  const [removeResp] = await Promise.all([
    page.waitForResponse(
      (res) => res.url().endsWith(`/api/v1/users/${target.id}/photo`) && res.request().method() === 'DELETE',
    ),
    page.getByRole('button', { name: 'Quitar foto' }).click(),
  ]);
  expect(removeResp.status(), 'DELETE /users/.../photo').toBe(204);
  await expect(page.getByRole('img', { name: `Iniciales de ${target.displayName}` })).toBeVisible();
  expect(await readProfilePictureKey(target.id)).toBeNull();
});

// ---------------------------------------------------------------------------
// B14 — oversize: client UX guard blocks the request; raw PUT > 5 MiB → 413.
// ---------------------------------------------------------------------------

test('B14 oversized photo is blocked client-side; raw PUT over the body limit returns 413', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await page.goto(`/Users/Details/${target.id}`);
  await submitLogin(page, { loginIdentifier: admin.username, password: admin.password });
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
  await page.goto(`/Users/Details/${target.id}`);

  // Spies the photo PUT so we can assert the client guard short-circuits the call.
  const putCalls: string[] = [];
  page.on('request', (req) => {
    if (req.url().endsWith(`/api/v1/users/${target.id}/photo`) && req.method() === 'PUT') {
      putCalls.push(req.url());
    }
  });

  // Oversize PNG (5 MiB + 1 byte). The client should refuse with the localized text.
  const oversize = Buffer.alloc(5 * 1024 * 1024 + 1, 0x89);
  // Real PNG signature in the first 8 bytes so the server-side path is exercised
  // if the client ever lets it through.
  Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]).copy(oversize, 0);
  await page
    .getByLabel('Seleccionar foto de perfil')
    .setInputFiles({ name: 'huge.png', mimeType: 'image/png', buffer: oversize });

  await expect(page.getByRole('alert').filter({ hasText: /supera el tamaño/i })).toBeVisible();
  expect(putCalls, 'no PUT request should have been sent').toEqual([]);

  // Server-side limit: bypass the client guard and PUT a raw 5 MiB + 1 byte body
  // straight from the Node test process so we observe the server's 413 instead
  // of the browser's "Failed to fetch" network reset.
  let rawStatus: number | null = null;
  let networkError: Error | null = null;
  try {
    rawStatus = await rawPhotoPutViaNode(page, target.id, oversize, 'image/png');
  } catch (error) {
    networkError = error as Error;
  }
  if (networkError !== null) {
    throw new Error(
      `raw PUT over body limit should be answered with 413 by the server, ` +
        `but the connection was reset before any status arrived: ${networkError.message}`,
    );
  }
  expect(rawStatus, 'raw PUT of 5 MiB + 1 byte').toBe(413);
});

// ---------------------------------------------------------------------------
// B14 — type mismatch: PNG bytes with image/jpeg, text bytes with image/png.
// ---------------------------------------------------------------------------

test('B14 type-mismatch uploads are rejected server-side (400) and surfaced in the UI', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const target = await seedSiteAUser('Supervisor');

  await page.goto(`/Users/Details/${target.id}`);
  await submitLogin(page, { loginIdentifier: admin.username, password: admin.password });
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();
  await page.goto(`/Users/Details/${target.id}`);

  // PNG bytes labeled image/jpeg → type_mismatch (400) on the raw server path.
  // Small bodies are fine in the browser; the page.evaluate fetch observes the
  // server status without surfacing a network reset.
  const pngBytes = decodeB64(PNG_B64);
  const mismatchStatus = await rawPhotoPutViaBrowser(page, target.id, pngBytes, 'image/jpeg', 'mismatch.jpg');
  expect([400, 415]).toContain(mismatchStatus);

  // Text bytes labeled image/png → signature_mismatch (400/415) on the raw server path.
  const textBytes = Buffer.from('NOT REALLY A PNG', 'utf8');
  const textStatus = await rawPhotoPutViaBrowser(page, target.id, textBytes, 'image/png', 'fake.png');
  expect([400, 415]).toContain(textStatus);

  // Through the file input the client surfaces the translated message.
  await page
    .getByLabel('Seleccionar foto de perfil')
    .setInputFiles({ name: 'mismatch.jpg', mimeType: 'image/jpeg', buffer: pngBytes });
  await expect(
    page.getByRole('alert').filter({ hasText: /tipo declarado|no corresponde/i }).first(),
  ).toBeVisible();

  await page
    .getByLabel('Seleccionar foto de perfil')
    .setInputFiles({ name: 'fake.png', mimeType: 'image/png', buffer: textBytes });
  await expect(
    page.getByRole('alert').filter({ hasText: /tipo declarado|no corresponde/i }).first(),
  ).toBeVisible();
});

// ---------------------------------------------------------------------------
// B14 — multi-site user photo by a site-a admin: 403 (custody not single-site).
// ---------------------------------------------------------------------------

test('B14 site-a admin cannot upload a photo for a multi-site user (custody check)', async ({ page }) => {
  const admin = await seedSiteAUser('Administrador');
  const dual = await seedDualSiteUser('Supervisor');

  await page.goto('/Users/Index');
  await submitLogin(page, { loginIdentifier: admin.username, password: admin.password });
  await expect(page.getByRole('navigation', { name: 'Cabecera principal' })).toBeVisible();

  // The multi-site user is filtered away from the default site-a list; use the
  // raw API to drive the upload probe (the UI does not expose this user).
  const pngBytes = decodeB64(PNG_B64);
  const status = await rawPhotoPutViaBrowser(page, dual.id, pngBytes, 'image/png', 'avatar.png');
  expect(status, 'PUT /users/.../photo on multi-site user').toBe(403);
});
