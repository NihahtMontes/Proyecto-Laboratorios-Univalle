import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';
import type { SiteRole } from './site.js';

export type ManagedUserStatus = 'active' | 'disabled';

export interface ManagedUserRecord {
  readonly id: string;
  readonly email: string;
  readonly fullName: string;
  readonly status: ManagedUserStatus;
  readonly isSuperAdmin: boolean;
  readonly siteRole: SiteRole;
  readonly membershipStatus: 'active' | 'suspended' | 'revoked';
  readonly createdAt: string;
  readonly updatedAt: string;
}

export interface ManagedUserQuery {
  readonly currentPage?: number;
  readonly statusFilter?: ManagedUserStatus;
  readonly searchTerm?: string;
}

export interface CreateManagedUserInput {
  readonly email: string;
  readonly fullName: string;
  readonly password: string;
  readonly siteRole: SiteRole;
}

export interface UpdateManagedUserInput {
  readonly email: string;
  readonly fullName: string;
  readonly status: ManagedUserStatus;
  readonly siteRole: SiteRole;
  readonly password?: string | null;
}

export interface ProfileRecord {
  readonly id: string;
  readonly email: string;
  readonly fullName: string;
  readonly siteRole: SiteRole;
}

export interface UpdateProfileInput {
  readonly email: string;
  readonly fullName: string;
}

export const USER_ROUTES = {
  users: `${API_PREFIX}/users`,
  user: (id: string) => `${API_PREFIX}/users/${id}`,
  profile: `${API_PREFIX}/profile`,
} as const;

export type ManagedUserPage = CatalogPage<ManagedUserRecord>;
