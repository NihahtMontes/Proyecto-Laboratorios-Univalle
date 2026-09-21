import {
  GlobalRole,
  SiteRole,
  SiteState,
  type ActiveSiteSession,
  type LoginRequest,
  type SetActiveSiteRequest,
  type SiteId,
} from '@lu/contracts';
import { AuthValidationException } from './auth.exceptions.js';

export type { ActiveSiteSession } from '@lu/contracts';
export type { CsrfResponse } from '@lu/contracts';

export type UserStatus = 'active' | 'disabled';
export type SiteStatus = 'provisioning' | 'active' | 'migrating' | 'degraded' | 'disabled';
export type MembershipStatus = 'active' | 'suspended' | 'revoked';
export type MembershipRole = 'Administrador' | 'Supervisor';

export interface User {
  readonly id: string;
  readonly email: string;
  readonly fullName: string;
  readonly passwordHash: string;
  readonly isSuperAdmin: boolean;
  readonly status: UserStatus;
  /** Canonical decimal string; never converted from PostgreSQL bigint to JavaScript Number. */
  readonly securityVersion: string;
}

export interface Site {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly status: SiteStatus;
}

export interface Membership {
  readonly userId: string;
  readonly siteId: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly validFrom: Date | null;
  readonly validUntil: Date | null;
  readonly site: Site;
}

export interface Session {
  readonly id: string;
  readonly tokenHash: string;
  readonly userId: string;
  readonly activeSiteId: string | null;
  /** Canonical decimal string; never converted from PostgreSQL bigint to JavaScript Number. */
  readonly securityVersion: string;
  readonly createdAt: Date;
  readonly lastSeenAt: Date;
  readonly idleExpiresAt: Date;
  readonly absoluteExpiresAt: Date;
  readonly revokedAt: Date | null;
  readonly revocationReason: string | null;
}

export interface SessionWithUser extends Session {
  readonly user: User;
  readonly activeSite: Site | null;
}

export interface AuthSessionContext {
  readonly user: User;
  readonly memberships: Membership[];
  readonly activeSiteId: string | null;
}

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function hasExactKeys(value: Record<string, unknown>, keys: readonly string[]): boolean {
  const actual = Object.keys(value);
  if (actual.length !== keys.length) {
    return false;
  }
  const expected = new Set(keys);
  return actual.every((k) => expected.has(k));
}

function normalizeHeaderValue(value: string | string[] | undefined): string | undefined {
  if (value === undefined) {
    return undefined;
  }
  return Array.isArray(value) ? value[0] : value;
}

export function parseCookies(header: string | string[] | undefined): Record<string, string> {
  const result: Record<string, string> = {};
  if (header === undefined) {
    return result;
  }
  const raw = Array.isArray(header) ? header.join('; ') : header;
  for (const segment of raw.split(';')) {
    const separatorIndex = segment.indexOf('=');
    if (separatorIndex < 0) {
      continue;
    }
    const name = segment.slice(0, separatorIndex).trim();
    const value = segment.slice(separatorIndex + 1).trim();
    if (name === '') {
      continue;
    }
    try {
      result[decodeURIComponent(name)] = decodeURIComponent(value);
    } catch {
      // Ignore malformed cookie segments.
    }
  }
  return result;
}

export function validateUuid(value: unknown, field: string): SiteId {
  if (typeof value !== 'string') {
    throw new AuthValidationException(`${field} must be a string.`, {
      [field]: ['must be a canonical UUID'],
    });
  }
  const trimmed = value.trim();
  if (!UUID_REGEX.test(trimmed)) {
    throw new AuthValidationException(`${field} must be a canonical UUID.`, {
      [field]: ['must be a canonical UUID'],
    });
  }
  return trimmed.toLowerCase() as SiteId;
}

function validatePassword(value: unknown, field: string): string {
  if (typeof value !== 'string') {
    throw new AuthValidationException(`${field} must be a string.`, {
      [field]: ['must be a non-empty string up to 72 UTF-8 bytes'],
    });
  }
  const byteLength = new TextEncoder().encode(value).length;
  if (value.length === 0 || byteLength > 72) {
    throw new AuthValidationException(
      `${field} must be non-empty and no more than 72 UTF-8 bytes.`,
      {
        [field]: ['must be non-empty and no more than 72 UTF-8 bytes'],
      },
    );
  }
  return value;
}

function validateEmail(value: unknown, field: string): string {
  if (typeof value !== 'string') {
    throw new AuthValidationException(`${field} must be a string.`, {
      [field]: ['must be a string'],
    });
  }
  const trimmed = value.trim().toLowerCase();
  if (trimmed === '' || trimmed.length > 254) {
    throw new AuthValidationException(
      `${field} must be a non-empty email of at most 254 characters.`,
      {
        [field]: ['must be a non-empty email of at most 254 characters'],
      },
    );
  }
  return trimmed;
}

export type LoginDto = LoginRequest & {
  readonly activeSiteId: SiteId | null;
  readonly rememberMe: boolean;
};

function hasAllowedKeys(
  value: Record<string, unknown>,
  required: readonly string[],
  optional: readonly string[],
): boolean {
  const allowed = new Set([...required, ...optional]);
  const actual = Object.keys(value);
  return actual.every((k) => allowed.has(k)) && required.every((k) => actual.includes(k));
}

export function validateLoginBody(value: unknown): LoginDto {
  if (!isPlainObject(value)) {
    throw new AuthValidationException('Request body must be a JSON object.');
  }
  if (!hasAllowedKeys(value, ['email', 'password'], ['activeSiteId', 'rememberMe'])) {
    throw new AuthValidationException(
      'Request body must contain email and password, and optional activeSiteId and rememberMe.',
    );
  }
  const email = validateEmail(value.email, 'email');
  const password = validatePassword(value.password, 'password');
  const rawActiveSiteId = value.activeSiteId;
  const activeSiteId =
    rawActiveSiteId === undefined
      ? null
      : rawActiveSiteId === null
        ? null
        : validateUuid(rawActiveSiteId, 'activeSiteId');
  if (value.rememberMe !== undefined && typeof value.rememberMe !== 'boolean') {
    throw new AuthValidationException('rememberMe must be a boolean.', {
      rememberMe: ['must be a boolean'],
    });
  }
  return { email, password, activeSiteId, rememberMe: value.rememberMe === true };
}

export type SetActiveSiteDto = SetActiveSiteRequest;

export function validateSetActiveSiteBody(value: unknown): SetActiveSiteDto {
  if (!isPlainObject(value)) {
    throw new AuthValidationException('Request body must be a JSON object.');
  }
  if (!hasExactKeys(value, ['activeSiteId'])) {
    throw new AuthValidationException('Request body must contain exactly activeSiteId.');
  }
  const activeSiteId = validateUuid(value.activeSiteId, 'activeSiteId');
  return { activeSiteId };
}

export function getSingleHeader(
  headers: Record<string, string | string[] | undefined>,
  name: string,
): string | undefined {
  const raw = headers[name];
  return normalizeHeaderValue(raw);
}

function toSiteRole(value: MembershipRole): SiteRole {
  switch (value) {
    case 'Administrador':
      return SiteRole.Administrador;
    case 'Supervisor':
      return SiteRole.Supervisor;
    default:
      throw new AuthValidationException('Invalid membership role.', {
        role: ['must be a valid SiteRole'],
      });
  }
}

function toSiteState(value: SiteStatus): SiteState {
  switch (value) {
    case 'provisioning':
      return SiteState.Provisioning;
    case 'active':
      return SiteState.Active;
    case 'migrating':
      return SiteState.Migrating;
    case 'degraded':
      return SiteState.Degraded;
    case 'disabled':
      return SiteState.Disabled;
    default:
      throw new AuthValidationException('Invalid site state.', {
        state: ['must be a valid SiteState'],
      });
  }
}

export function buildActiveSiteSession(
  user: User,
  eligibleMemberships: ReadonlyArray<Membership>,
  activeSiteId: string | null,
): ActiveSiteSession {
  const activeMembership = activeSiteId
    ? (eligibleMemberships.find((m) => m.siteId === activeSiteId) ?? null)
    : null;

  return {
    userId: user.id,
    displayName: user.fullName,
    email: user.email,
    activeSiteId: activeMembership ? (activeMembership.siteId as SiteId) : null,
    activeSiteName: activeMembership ? activeMembership.site.name : null,
    globalRole: user.isSuperAdmin ? GlobalRole.SuperAdmin : null,
    memberships: eligibleMemberships.map((m) => ({
      siteId: m.siteId as SiteId,
      siteName: m.site.name,
      role: toSiteRole(m.role),
      state: toSiteState(m.site.status),
    })),
  };
}
