import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';
import type { SiteId } from './site.js';

/**
 * MIG-001 users contract (F1 §3, §8–§17), mirroring the F3 `/users` and
 * `/profile` routes. Identity is global; role, status, validity and work
 * profile belong to a site membership. SuperAdmin is a global flag managed only
 * by the `users:superadmin` CLI and is never writable over HTTP.
 *
 * Mutating payloads are closed: unknown keys and privileged keys
 * (`isSuperAdmin`, `globalRole`, `securityVersion`, `rowVersion`, `fullName`,
 * `passwordHash`) are rejected with 400.
 */

export type AccountStatus = 'active' | 'inactive' | 'deleted';
export type MembershipStatus = 'active' | 'suspended' | 'revoked';
export type MembershipRole = 'Administrador' | 'Supervisor';

/**
 * F1 §11 effective ASP label for one membership: account inactive or membership
 * suspended/not yet valid -> Inactivo; account deleted or membership revoked ->
 * Eliminado; otherwise Activo.
 */
export type EffectiveAspStatus = 'Activo' | 'Inactivo' | 'Eliminado';

/** 'SuperAdmin' for the global flag, else the role in the viewer's active site. */
export type DisplayRole = 'SuperAdmin' | MembershipRole;

/**
 * Stored profile picture reference. Bytes are served only by the authorized
 * photo routes; `etag` changes whenever the picture changes (use it to bust
 * caches). Null on a record means "render initials".
 */
export interface ProfilePictureRef {
  readonly etag: string;
}

/** Row audit actor (F1 §13); returned on the administrative detail view. */
export interface AuditActor {
  readonly userId: string;
  readonly fullName: string;
}

/** Timestamps are ISO-8601 UTC strings. */
export interface MembershipProjection {
  readonly siteId: SiteId;
  readonly siteName: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly effectiveStatus: EffectiveAspStatus;
  readonly validFrom: string | null;
  readonly validUntil: string | null;
  readonly position: string | null;
  readonly department: string | null;
  readonly hireDate: string | null;
}

/**
 * Administrative user view. A site Administrador receives only its active-site
 * membership in `memberships`; a SuperAdmin receives every membership.
 */
export interface ManagedUserRecord {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName: string | null;
  /** Server-derived display name; never accepted as input. */
  readonly fullName: string;
  readonly initials: string;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly accountStatus: AccountStatus;
  readonly isSuperAdmin: boolean;
  readonly displayRole: DisplayRole | null;
  readonly profilePicture: ProfilePictureRef | null;
  readonly mustChangePassword: boolean;
  readonly memberships: readonly MembershipProjection[];
  readonly createdAt: string;
  readonly updatedAt: string;
  /** Present on `GET /users/:id`; null for a labeled system action. */
  readonly createdBy?: AuditActor | null;
  readonly modifiedBy?: AuditActor | null;
}

export type ManagedUserPage = CatalogPage<ManagedUserRecord>;

/**
 * `GET /users` query. Deleted accounts and revoked memberships are hidden unless
 * `statusFilter` asks for them. `siteScope` is SuperAdmin-only: `'all'` for the
 * global account view or a site UUID; a site Administrador is always scoped to
 * its active site.
 */
export interface ManagedUserQuery {
  readonly currentPage?: number;
  readonly statusFilter?: AccountStatus;
  /** Trimmed and truncated to 200 characters by the server. */
  readonly searchTerm?: string;
  readonly siteScope?: SiteId | 'all';
}

export interface CreateUserMembershipInput {
  readonly siteId: SiteId;
  readonly role: MembershipRole;
  readonly validFrom?: string | null;
  readonly validUntil?: string | null;
}

interface CreateManagedUserBase {
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName?: string | null;
  readonly identityCard: string;
  readonly phoneNumber: string;
  /** Must satisfy the shared password policy (`passwordPolicyViolations`). */
  readonly password: string;
}

/** `POST /users` as a SuperAdmin: explicit memberships in any site. */
export interface CreateManagedUserGlobalInput extends CreateManagedUserBase {
  readonly memberships: readonly CreateUserMembershipInput[];
}

/** `POST /users` as a site Administrador: one membership in the active site. */
export interface CreateManagedUserSiteInput extends CreateManagedUserBase {
  readonly role: MembershipRole;
}

/** The server selects the shape from the caller (SuperAdmin or site admin). */
export type CreateManagedUserInput = CreateManagedUserGlobalInput | CreateManagedUserSiteInput;

/**
 * `PUT /users/:id/global-fields`. A site Administrador may use it only as the
 * single-site custodian; email change rotates the target's sessions.
 */
export interface UpdateUserGlobalFieldsInput {
  readonly email?: string;
  readonly firstName?: string;
  readonly lastName?: string;
  readonly secondLastName?: string | null;
  readonly identityCard?: string;
  readonly phoneNumber?: string;
  readonly reason?: string;
}

/** `POST /users/:id/admin-reset-password` (204). Never for one's own account. */
export interface AdminResetPasswordInput {
  readonly password: string;
  readonly reason?: string;
}

/** `PUT /users/:id/account-status`. Deletion uses `DELETE /users/:id`. */
export interface SetAccountStatusInput {
  readonly accountStatus: 'active' | 'inactive';
  readonly reason?: string;
}

/** `POST /users/:id/restore` (204). */
export interface RestoreAccountInput {
  readonly reason?: string;
}

/** `POST /users/:id/memberships` (201). Linking an existing user is SuperAdmin-only. */
export type AddMembershipInput = CreateUserMembershipInput;

export interface ChangeMembershipRoleInput {
  readonly role: MembershipRole;
  readonly reason?: string;
}

/** Revocation uses `DELETE /users/:id/memberships/:siteId`. */
export interface ChangeMembershipStatusInput {
  readonly status: 'active' | 'suspended';
  readonly reason?: string;
}

export interface ChangeMembershipValidityInput {
  readonly validFrom: string | null;
  readonly validUntil: string | null;
  readonly reason?: string;
}

export interface UpdateWorkProfileInput {
  readonly position?: string | null;
  readonly department?: string | null;
  readonly hireDate?: string | null;
}

/**
 * `POST /users/:id/memberships/:siteId/restore` (204). Keeps the stored role and
 * work profile; expired validity requires new bounds. `restoreAccount` also
 * restores an account deleted by that site's own revoke cascade.
 */
export interface RestoreMembershipInput {
  readonly validFrom?: string | null;
  readonly validUntil?: string | null;
  readonly restoreAccount?: boolean;
  readonly reason?: string;
}

/** `GET|PUT /profile`: the caller's own account with all its memberships. */
export interface ProfileRecord {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName: string | null;
  readonly fullName: string;
  readonly initials: string;
  readonly phoneNumber: string;
  readonly identityCard: string;
  readonly isSuperAdmin: boolean;
  readonly accountStatus: AccountStatus;
  readonly profilePicture: ProfilePictureRef | null;
  readonly memberships: readonly MembershipProjection[];
}

/**
 * `PUT /profile`. The identity card is not self-editable. A self email change
 * replaces the current session cookie.
 */
export interface UpdateProfileInput {
  readonly email?: string;
  readonly firstName?: string;
  readonly lastName?: string;
  readonly secondLastName?: string | null;
  readonly phoneNumber?: string;
}

// ---------------------------------------------------------------------------
// Profile photo (F1 §14): raw image bytes, never JSON. The server checks size,
// extension, declared MIME and magic signature; these values are for UX only.
// ---------------------------------------------------------------------------

export const PROFILE_PHOTO = {
  maxBytes: 5 * 1024 * 1024,
  mimeTypes: ['image/jpeg', 'image/png', 'image/webp'],
  extensions: ['jpg', 'jpeg', 'png', 'webp'],
  /** URI-encoded original file name, used only for the extension check. */
  fileNameHeader: 'X-Photo-Filename',
} as const;

export type ProfilePhotoMimeType = (typeof PROFILE_PHOTO.mimeTypes)[number];

/** Values reported under `fieldErrors.photo` on a 400. */
export type ProfilePhotoViolation =
  | 'empty'
  | 'too_large'
  | 'invalid_file_name'
  | 'unsupported_extension'
  | 'unsupported_media_type'
  | 'signature_mismatch'
  | 'type_mismatch';

const id = (value: string): string => encodeURIComponent(value);

export const USER_ROUTES = {
  users: `${API_PREFIX}/users`,
  user: (userId: string) => `${API_PREFIX}/users/${id(userId)}`,
  globalFields: (userId: string) => `${API_PREFIX}/users/${id(userId)}/global-fields`,
  adminResetPassword: (userId: string) => `${API_PREFIX}/users/${id(userId)}/admin-reset-password`,
  accountStatus: (userId: string) => `${API_PREFIX}/users/${id(userId)}/account-status`,
  restore: (userId: string) => `${API_PREFIX}/users/${id(userId)}/restore`,
  memberships: (userId: string) => `${API_PREFIX}/users/${id(userId)}/memberships`,
  membership: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}`,
  membershipRole: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}/role`,
  membershipStatus: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}/status`,
  membershipValidity: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}/validity`,
  membershipWorkProfile: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}/work-profile`,
  membershipRestore: (userId: string, siteId: string) =>
    `${API_PREFIX}/users/${id(userId)}/memberships/${id(siteId)}/restore`,
  userPhoto: (userId: string) => `${API_PREFIX}/users/${id(userId)}/photo`,
  profile: `${API_PREFIX}/profile`,
  profilePhoto: `${API_PREFIX}/profile/photo`,
} as const;
