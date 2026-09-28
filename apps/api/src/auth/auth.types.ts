import {
  GlobalRole,
  SiteRole,
  SiteState,
  type ActiveSiteSession,
  type SiteId,
} from '@lu/contracts';
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus as KernelMembershipStatus,
  PasswordScheme,
  SessionPurpose,
} from '../identity/identity.contracts.js';
import { AuthValidationException } from './auth.exceptions.js';

export type { ActiveSiteSession } from '@lu/contracts';
export type { CsrfResponse } from '@lu/contracts';

export type { AccountStatus, MembershipRole, PasswordScheme, SessionPurpose };
export type MembershipStatus = KernelMembershipStatus;

export type SiteStatus = 'provisioning' | 'active' | 'migrating' | 'degraded' | 'disabled';

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function utf8ByteLength(value: string): number {
  return new TextEncoder().encode(value).length;
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

interface MinimalPasswordPolicy {
  readonly maxBytes: number;
}

function validateLoginPassword(value: unknown, field: string, max: MinimalPasswordPolicy): string {
  if (typeof value !== 'string') {
    throw new AuthValidationException(`${field} must be a string.`, {
      [field]: ['must be a non-empty string'],
    });
  }
  if (value.length === 0) {
    throw new AuthValidationException(`${field} must be a non-empty string.`, {
      [field]: ['must be a non-empty string'],
    });
  }
  const byteLength = utf8ByteLength(value);
  if (byteLength > max.maxBytes) {
    throw new AuthValidationException(
      `${field} must be at most ${max.maxBytes} UTF-8 bytes (transport bound).`,
      {
        [field]: [`must be at most ${max.maxBytes} UTF-8 bytes (transport bound)`],
      },
    );
  }
  return value;
}

function hasAllowedKeys(
  value: Record<string, unknown>,
  required: readonly string[],
  optional: readonly string[],
): boolean {
  const allowed = new Set([...required, ...optional]);
  const actual = Object.keys(value);
  return actual.every((k) => allowed.has(k)) && required.every((k) => actual.includes(k));
}

export type LoginDto = {
  readonly loginIdentifier: string;
  readonly password: string;
  readonly rememberMe: boolean;
  readonly activeSiteId: SiteId | null;
};

export interface LoginDtoTransportLimits {
  readonly loginPasswordMaxBytes: number;
}

/**
 * Validates the login HTTP body. The transport bound lets a >72-byte plaintext pass
 * the boundary check so the legacy verifier has a chance; the bcrypt limit and the
 * password policy are enforced by the verifier/policy services during processing.
 *
 * Identifier matches `^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$` OR `^[a-z0-9._-]{1,256}$`
 * after NFKC + locale-independent lowercase normalization. Unknown top-level keys are
 * rejected so the privileged global flag (`isSuperAdmin`, `globalRole`) and any
 * protocol metadata cannot reach the service. The deprecated `email` alias from
 * F4/F5 was removed in F8: the body must use `loginIdentifier` only.
 */
export function validateLoginBody(value: unknown, limits?: LoginDtoTransportLimits): LoginDto {
  if (!isPlainObject(value)) {
    throw new AuthValidationException('Request body must be a JSON object.');
  }
  const allowedRequired = ['password'] as const;
  const allowedOptional = ['loginIdentifier', 'rememberMe', 'activeSiteId'] as const;
  if (
    !hasAllowedKeys(
      value,
      allowedRequired as readonly string[],
      allowedOptional as readonly string[],
    )
  ) {
    throw new AuthValidationException(
      'Request body must contain loginIdentifier and password, and optional rememberMe/activeSiteId. Unknown keys are not accepted.',
    );
  }

  const loginIdentifierRaw = value.loginIdentifier;
  if (loginIdentifierRaw === undefined) {
    throw new AuthValidationException('loginIdentifier is required.', {
      loginIdentifier: ['is required'],
    });
  }

  if (typeof loginIdentifierRaw !== 'string') {
    throw new AuthValidationException('loginIdentifier must be a string.', {
      loginIdentifier: ['must be a string'],
    });
  }
  const loginIdentifier = normalizeLoginIdentifier(loginIdentifierRaw);
  if (loginIdentifier === '') {
    throw new AuthValidationException('loginIdentifier must not be blank.', {
      loginIdentifier: ['must not be blank'],
    });
  }

  const max = limits?.loginPasswordMaxBytes ?? 512;
  const password = validateLoginPassword(value.password, 'password', { maxBytes: max });

  const rawActiveSiteId = value.activeSiteId;
  const activeSiteId =
    rawActiveSiteId === undefined || rawActiveSiteId === null
      ? null
      : validateUuid(rawActiveSiteId, 'activeSiteId');
  if (value.rememberMe !== undefined && typeof value.rememberMe !== 'boolean') {
    throw new AuthValidationException('rememberMe must be a boolean.', {
      rememberMe: ['must be a boolean'],
    });
  }
  return { loginIdentifier, password, activeSiteId, rememberMe: value.rememberMe === true };
}

/**
 * Returns the canonical normalized form for lookup against `lu_login_identifier`:
 * NFKC + locale-independent lowercase + btrim. Used both for input validation and
 * for symmetric hashing inside rate-limit.
 */
export function normalizeLoginIdentifier(value: string): string {
  // NFKC + locale-independent lowercase equivalent of public.lu_login_identifier_normalize
  return value.trim().normalize('NFKC').toLowerCase();
}

export type SetActiveSiteDto = { readonly activeSiteId: SiteId };

export function validateSetActiveSiteBody(value: unknown): SetActiveSiteDto {
  if (!isPlainObject(value)) {
    throw new AuthValidationException('Request body must be a JSON object.');
  }
  const allowed = ['activeSiteId'] as const;
  if (!hasAllowedKeys(value, allowed as readonly string[], [])) {
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

// ---------------------------------------------------------------------------
// Identity-domain shape used by the auth slice. Superset of @lu/contracts
// ActiveSiteSession; carries the F1 session purpose + mustChangePassword hints
// for client routing. The HTTP response keeps `data` as the contract shape and
// the `meta` object carries the strict-extension fields.
// ---------------------------------------------------------------------------

export interface IdentityUser {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName: string | null;
  readonly fullName: string;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly accountStatus: AccountStatus;
  readonly isSuperAdmin: boolean;
  readonly passwordScheme: PasswordScheme;
  readonly passwordHash: string | null;
  readonly mustChangePassword: boolean;
  readonly securityVersion: string;
}

export interface IdentitySite {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly status: SiteStatus;
}

export interface IdentityMembership {
  readonly userId: string;
  readonly siteId: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly validFrom: Date | null;
  readonly validUntil: Date | null;
  readonly site: IdentitySite;
}

export interface AuthSessionRecord {
  readonly id: string;
  readonly tokenHash: string;
  readonly userId: string;
  readonly activeSiteId: string | null;
  readonly purpose: SessionPurpose;
  readonly securityVersion: string;
  readonly createdAt: Date;
  readonly lastSeenAt: Date;
  readonly idleExpiresAt: Date;
  readonly absoluteExpiresAt: Date;
  readonly revokedAt: Date | null;
  readonly revocationReason: string | null;
}

/**
 * Backend-local superset of `@lu/contracts` `ActiveSiteSession`. The controller
 * only exposes this internally; the wire response keeps the contract subset so
 * packages/contracts is left untouched.
 */
export interface AuthSessionResponse {
  readonly session: ActiveSiteSession;
  readonly purpose: SessionPurpose;
  readonly mustChangePassword: boolean;
  readonly eligibleSites: readonly {
    readonly siteId: SiteId;
    readonly siteName: string;
    readonly role: 'Administrador' | 'Supervisor';
  }[];
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
  identity: IdentityUser,
  eligibleMemberships: ReadonlyArray<IdentityMembership>,
  activeSiteId: string | null,
): ActiveSiteSession {
  const activeMembership = activeSiteId
    ? (eligibleMemberships.find((m) => m.siteId === activeSiteId) ?? null)
    : null;

  return {
    userId: identity.id,
    displayName: identity.fullName,
    email: identity.email,
    activeSiteId: activeMembership ? (activeMembership.siteId as SiteId) : null,
    activeSiteName: activeMembership ? activeMembership.site.name : null,
    globalRole: identity.isSuperAdmin ? GlobalRole.SuperAdmin : null,
    memberships: eligibleMemberships.map((m) => ({
      siteId: m.siteId as SiteId,
      siteName: m.site.name,
      role: toSiteRole(m.role),
      state: toSiteState(m.site.status),
    })),
  };
}

export function buildAuthSessionResponse(
  identity: IdentityUser,
  eligibleMemberships: ReadonlyArray<IdentityMembership>,
  purpose: SessionPurpose,
  activeSiteId: string | null,
): AuthSessionResponse {
  // Restricted sessions never expose an active site or memberships in the
  // contract-shaped session; a site_selection session still lists its eligible
  // sites so the client can choose one, a password_change session lists none.
  const restricted = purpose !== 'normal';
  const listed = purpose === 'password_change' ? [] : eligibleMemberships;
  const session: ActiveSiteSession = buildActiveSiteSession(
    identity,
    restricted ? [] : eligibleMemberships,
    restricted ? null : activeSiteId,
  );
  return {
    session,
    purpose,
    mustChangePassword: identity.mustChangePassword,
    eligibleSites: listed.map((m) => ({
      siteId: m.siteId as SiteId,
      siteName: m.site.name,
      role: m.role,
    })),
  };
}

/**
 * Server-side view of a validated session, used by guards and auth flows.
 * `sessionId` is the lu_session primary key; the raw cookie token never
 * leaves the auth slice.
 */
export interface ResolvedSession {
  readonly sessionId: string;
  readonly purpose: SessionPurpose;
  readonly identity: IdentityUser;
  readonly eligibleMemberships: readonly IdentityMembership[];
  readonly activeSiteId: string | null;
}

/**
 * Allowed key set for /auth/password.
 */
export const PASSWORD_CHANGE_REQUIRED_KEYS = ['currentPassword', 'newPassword'] as const;
export const PASSWORD_CHANGE_OPTIONAL_KEYS = [] as const;

export interface PasswordChangeDto {
  readonly currentPassword: string;
  readonly newPassword: string;
}

export function validatePasswordChangeBody(value: unknown): PasswordChangeDto {
  if (!isPlainObject(value)) {
    throw new AuthValidationException('Request body must be a JSON object.');
  }
  if (
    !hasAllowedKeys(
      value,
      PASSWORD_CHANGE_REQUIRED_KEYS as readonly string[],
      PASSWORD_CHANGE_OPTIONAL_KEYS as readonly string[],
    )
  ) {
    throw new AuthValidationException(
      'Request body must contain exactly currentPassword and newPassword.',
    );
  }
  const currentPassword = value.currentPassword;
  const newPassword = value.newPassword;
  if (typeof currentPassword !== 'string' || typeof newPassword !== 'string') {
    throw new AuthValidationException('currentPassword and newPassword must be strings.', {
      currentPassword: ['must be a string'],
      newPassword: ['must be a string'],
    });
  }
  if (currentPassword.length === 0 || newPassword.length === 0) {
    throw new AuthValidationException('currentPassword and newPassword must be non-empty.', {
      currentPassword: ['must be non-empty'],
      newPassword: ['must be non-empty'],
    });
  }
  return { currentPassword, newPassword };
}

// ---------------------------------------------------------------------------
// Compatibility helpers used by tests and CLI
// ---------------------------------------------------------------------------

export type { KernelMembershipStatus };
