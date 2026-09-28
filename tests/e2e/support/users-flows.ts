import { randomBytes } from 'node:crypto';
import type { Browser, BrowserContext, Page } from '@playwright/test';
import { openAuthenticated } from './auth.js';
import { controlDb, seedUser, uniqueToken } from './db.js';
import { stack } from './env.js';

/**
 * Shared helpers for the B1-B14 user-administration suite. Keeps the spec files
 * focused on the contract being verified; selectors here stay close to the
 * Spanish copy in `apps/web/src/UsersPanel.tsx` and `apps/web/src/ProfilePhoto.tsx`.
 */

export type SiteKey = 'a' | 'b';

/** F1 §11 effective ASP labels exposed by the UI. */
export const STATUS_LABEL = {
  active: 'Activo',
  inactive: 'Inactivo',
  deleted: 'Eliminado',
} as const;

/** F1 §8 role labels. The UI never offers SuperAdmin over HTTP. */
export const ROLE_LABEL = {
  Administrador: 'Administrador',
  Supervisor: 'Supervisor',
  SuperAdmin: 'SuperAdmin',
} as const;

/** Unique tokens reused across tests without polluting shared users. */
export function freshToken(prefix: string): string {
  return `${prefix}${uniqueToken()}`;
}

/**
 * Seeds a site-a single-custody user (one non-revoked membership in site-a).
 * Each test owns its own user so the suite stays parallel-safe.
 */
export function seedSiteAUser(
  role: 'Administrador' | 'Supervisor' = 'Administrador',
  options: { label?: string; accountStatus?: 'active' | 'inactive' | 'deleted' } = {},
): ReturnType<typeof seedUser> {
  return seedUser({
    label: options.label ?? 'sa',
    memberships: [{ site: 'a', role, status: 'active' }],
    accountStatus: options.accountStatus,
  });
}

/** Seeds a site-b-only user (used to prove site-scoped filtering). */
export function seedSiteBUser(
  role: 'Administrador' | 'Supervisor' = 'Administrador',
): ReturnType<typeof seedUser> {
  return seedUser({
    label: 'sb',
    memberships: [{ site: 'b', role, status: 'active' }],
  });
}

/** Seeds a user with active memberships in both sites (multi-site target). */
export function seedDualSiteUser(
  role: 'Administrador' | 'Supervisor' = 'Administrador',
): ReturnType<typeof seedUser> {
  return seedUser({
    label: 'dual',
    memberships: [
      { site: 'a', role, status: 'active' },
      { site: 'b', role, status: 'active' },
    ],
  });
}

/** Seeds a user with ZERO memberships (vacuous-custody edge case). */
export function seedOrphanUser(): ReturnType<typeof seedUser> {
  return seedUser({ label: 'orphan', memberships: [] });
}

/** Seeds a SuperAdmin (no memberships) for global-only flows. */
export function seedSuperAdmin(): ReturnType<typeof seedUser> {
  return seedUser({ label: 'sa-root', superAdmin: true });
}

/**
 * Seeds a SuperAdmin that also holds site memberships. The user lands on the
 * site picker at login and may administer each linked site. Used by the F7
 * SuperAdmin happy-path scenarios (create a user, add a membership in another
 * site, global-delete another account).
 */
export function seedSuperAdminWithMemberships(
  sites: readonly SiteKey[] = ['a', 'b'],
  role: 'Administrador' | 'Supervisor' = 'Administrador',
): ReturnType<typeof seedUser> {
  return seedUser({
    label: `sa-${sites.join('-')}`,
    superAdmin: true,
    memberships: sites.map((site) => ({ site, role, status: 'active' })),
  });
}

/** Returns the row locator that targets the given user in the index table. */
export function rowOf(page: Page, username: string) {
  return page.getByRole('row').filter({ hasText: username });
}

/** Logs in the actor through the API and lands them on `path`. */
export async function openAs(
  page: Page,
  creds: { loginIdentifier: string; password: string },
  path = '/',
  activeSiteId: string | null = null,
): Promise<void> {
  await openAuthenticated(page, creds, path, activeSiteId);
}

/** Counts live sessions for `userId`. */
export async function liveSessions(userId: string): Promise<number> {
  return controlDb(async (c) => {
    const r = await c.query<{ n: number }>(
      `SELECT count(*)::int AS n FROM public.lu_session
        WHERE user_id = $1 AND revoked_at IS NULL AND idle_expires_at > now() AND absolute_expires_at > now()`,
      [userId],
    );
    return r.rows[0]?.n ?? 0;
  });
}

/** Returns the stored profile-picture key, or null when none. */
export async function readProfilePictureKey(userId: string): Promise<string | null> {
  return controlDb(async (c) => {
    const r = await c.query<{ profile_picture_key: string | null }>(
      `SELECT profile_picture_key FROM public.lu_user WHERE id = $1`,
      [userId],
    );
    return r.rows[0]?.profile_picture_key ?? null;
  });
}

/** Opens a fresh, isolated browser context for a second actor. */
export async function freshContext(browser: Browser): Promise<BrowserContext> {
  return browser.newContext();
}

// ---------------------------------------------------------------------------
// Tiny valid images (1x1) generated inside the test from base64 constants.
// ---------------------------------------------------------------------------

/** 1x1 PNG, 67 bytes. */
export const PNG_1X1_BASE64 =
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=';

/** 1x1 JPEG, 134 bytes. */
export const JPEG_1X1_BASE64 =
  '/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFAABAAAAAAAAAAAAAAAAAAAACf/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AKwA//9k=';

/** 1x1 WebP, 26 bytes. */
export const WEBP_1X1_BASE64 =
  'UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAUAmJZQCdAEO/gbsAAA=';

function decodeB64(b64: string): Buffer {
  return Buffer.from(b64, 'base64');
}

export function png1x1(): Buffer {
  return decodeB64(PNG_1X1_BASE64);
}

export function jpeg1x1(): Buffer {
  return decodeB64(JPEG_1X1_BASE64);
}

export function webp1x1(): Buffer {
  return decodeB64(WEBP_1X1_BASE64);
}

/** 5 MiB + 1 byte payload used to exercise the server's body-size limit. */
export function oversizeBytes(plus = 1): Buffer {
  return Buffer.alloc(5 * 1024 * 1024 + plus, 0x41);
}

/** Plain text bytes the server must reject as a non-image. */
export function fakePngBytes(): Buffer {
  return Buffer.from('NOT REALLY A PNG', 'utf8');
}

/** Builds the site's UUID from the fixture stack without leaking the env. */
export function siteUuid(key: SiteKey): string {
  return stack().sites[key].id;
}

/** Seeds many site-a users that share `token` in their username (pagination test). */
export async function seedPageOfUsers(
  token: string,
  count: number,
  site: SiteKey = 'a',
): Promise<Awaited<ReturnType<typeof seedUser>>[]> {
  const out: Awaited<ReturnType<typeof seedUser>>[] = [];
  for (let i = 0; i < count; i += 1) {
    out.push(
      await seedUser({
        label: `pg${token}${i.toString(36).padStart(3, '0')}`,
        memberships: [{ site, role: 'Supervisor', status: 'active' }],
      }),
    );
  }
  return out;
}

/** Generates a stable random token for parallel-safe data names. */
export function randomTag(prefix: string): string {
  return `${prefix}${randomBytes(3).toString('hex')}`;
}
