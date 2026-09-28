/**
 * MIG-001 F3 backend-local user administration types.
 *
 * These DTOs are intentionally NOT exported through packages/contracts. They
 * describe the strict-shape HTTP payload for the /users and /profile control
 * plane routes bound by F1 sections 3, 6, 8, 9, 10, 11, 12, 13, 15, 16, 17.
 *
 * Every mutating payload rejects unknown keys, including the privileged
 * `isSuperAdmin`, `globalRole`, `securityVersion`, `rowVersion`, `fullName`
 * and `passwordHash` keys (controller enforces this).
 */
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus,
} from '../identity/identity.contracts.js';

export type SiteRole = MembershipRole;
export type ManagedAccountStatus = AccountStatus;
export type ManagedMembershipStatus = MembershipStatus;

export interface ManagedUserRecord {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName: string | null;
  readonly fullName: string;
  readonly initials: string;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly accountStatus: ManagedAccountStatus;
  readonly isSuperAdmin: boolean;
  /**
   * F1 §8 role badge: 'SuperAdmin' when the global flag is set, otherwise the
   * role of the membership in the viewer's active site (null without one).
   */
  readonly displayRole: 'SuperAdmin' | SiteRole | null;
  /** Null when no picture is stored (clients render initials). */
  readonly profilePicture: ProfilePictureRef | null;
  readonly mustChangePassword: boolean;
  readonly memberships: readonly MembershipProjection[];
  readonly createdAt: string;
  readonly updatedAt: string;
  /** Audit actors (F1 §13); populated on the administrative detail view. */
  readonly createdBy?: AuditActor | null;
  readonly modifiedBy?: AuditActor | null;
}

/**
 * Reference to a stored profile picture. The bytes are served only by the
 * authorized photo endpoints; the ETag changes whenever the object key does.
 */
export interface ProfilePictureRef {
  readonly etag: string;
}

/** Actor reference for row audit; null actor means a labeled system action. */
export interface AuditActor {
  readonly userId: string;
  readonly fullName: string;
}

export interface MembershipProjection {
  readonly siteId: string;
  readonly siteName: string;
  readonly role: SiteRole;
  readonly status: ManagedMembershipStatus;
  readonly effectiveStatus: EffectiveAspStatus;
  readonly validFrom: string | null;
  readonly validUntil: string | null;
  readonly position: string | null;
  readonly department: string | null;
  readonly hireDate: string | null;
}

/**
 * Section 11 "Effective ASP display" status table:
 *  - account=active, membership=active+eligible -> Activo
 *  - account=inactive                            -> Inactivo
 *  - account=active, membership=suspended        -> Inactivo
 *  - account=deleted                             -> Eliminado
 *  - account!=deleted, membership=revoked        -> Eliminado for that site
 */
export type EffectiveAspStatus = 'Activo' | 'Inactivo' | 'Eliminado';

export interface ManagedUserPage {
  readonly items: readonly ManagedUserRecord[];
  readonly totalCount: number;
  readonly pageIndex: number;
  readonly totalPages: number;
  readonly pageSize: number;
}

export interface ManagedUserQuery {
  readonly currentPage?: number;
  readonly statusFilter?: ManagedAccountStatus;
  readonly searchTerm?: string;
  readonly siteScope?: string | null;
}

export interface CreateMembershipInput {
  readonly siteId: string;
  readonly role: SiteRole;
  readonly validFrom?: string | null;
  readonly validUntil?: string | null;
}

export interface CreateUserGlobalInput {
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName?: string | null;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly password: string;
  readonly memberships: readonly CreateMembershipInput[];
}

export interface CreateUserSiteAdminInput {
  readonly username: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName?: string | null;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly password: string;
  readonly role: SiteRole;
}

export type CreateUserInput =
  | { readonly mode: 'global'; readonly input: CreateUserGlobalInput }
  | { readonly mode: 'site'; readonly input: CreateUserSiteAdminInput };

export interface UpdateGlobalFieldsInput {
  readonly email?: string;
  readonly firstName?: string;
  readonly lastName?: string;
  readonly secondLastName?: string | null;
  readonly identityCard?: string;
  readonly phoneNumber?: string;
  readonly reason?: string;
}

export interface AdminResetPasswordInput {
  readonly password: string;
  readonly reason?: string;
}

export interface SetAccountStatusInput {
  readonly accountStatus: 'active' | 'inactive';
  readonly reason?: string;
}

export interface RestoreAccountInput {
  readonly reason?: string;
}

export interface GlobalDeleteInput {
  readonly reason?: string;
}

export interface AddMembershipInput {
  readonly siteId: string;
  readonly role: SiteRole;
  readonly validFrom?: string | null;
  readonly validUntil?: string | null;
}

export interface ChangeRoleInput {
  readonly role: SiteRole;
  readonly reason?: string;
}

export interface ChangeStatusInput {
  readonly status: 'active' | 'suspended';
  readonly reason?: string;
}

export interface ChangeValidityInput {
  readonly validFrom: string | null;
  readonly validUntil: string | null;
  readonly reason?: string;
}

export interface UpdateWorkProfileInput {
  readonly position?: string | null;
  readonly department?: string | null;
  readonly hireDate?: string | null;
}

export interface RevokeActiveSiteInput {
  readonly reason?: string;
}

export interface RestoreMembershipInput {
  readonly validFrom?: string | null;
  readonly validUntil?: string | null;
  readonly restoreAccount?: boolean;
  readonly reason?: string;
}

export interface UpdateProfileInput {
  readonly email?: string;
  readonly firstName?: string;
  readonly lastName?: string;
  readonly secondLastName?: string | null;
  readonly phoneNumber?: string;
  readonly identityCard?: string;
}

export const SUPERADMIN_FIXED_ADVISORY_KEY = 'lu:users:last-active-superadmin';

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
  readonly accountStatus: ManagedAccountStatus;
  readonly profilePicture: ProfilePictureRef | null;
  readonly memberships: readonly MembershipProjection[];
}

export const FORBIDDEN_PAYLOAD_KEYS: readonly string[] = [
  'isSuperAdmin',
  'is_super_admin',
  'globalRole',
  'global_role',
  'securityVersion',
  'security_version',
  'rowVersion',
  'row_version',
  'fullName',
  'full_name',
  'passwordHash',
  'password_hash',
];
