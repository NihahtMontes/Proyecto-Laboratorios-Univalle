import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';

export type PersonType = 'internal' | 'external';
export type PersonStatus = 0 | 1 | 2;
export type PersonCategory = 1 | 2 | 3 | 4 | 5 | 99;

export interface PersonRecord {
  readonly id: number;
  readonly actorCode: string | null;
  readonly type: PersonType;
  readonly name: string;
  readonly email: string | null;
  readonly phoneNumber: string | null;
  readonly isEntity: boolean;
  readonly address: string | null;
  readonly category: PersonCategory;
  readonly status: PersonStatus;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

/**
 * `GET /people` query. People are tenant data: every route (reads included)
 * requires a normal session whose active site is eligible and whose role there
 * is Administrador; otherwise 403 `SITE_ACCESS_DENIED`. Deleted persons
 * (status 2) are hidden unless `statusFilter` requests a status explicitly
 * (including 2). `DELETE /people/:id` soft-deletes (status 2) and returns
 * `data: null`; data conflicts are reported as 400 with `fieldErrors`.
 */
export interface PersonQuery {
  readonly currentPage?: number;
  readonly statusFilter?: PersonStatus;
  readonly type?: PersonType;
  readonly category?: PersonCategory;
  readonly searchTerm?: string;
}

export interface CreatePersonInput {
  readonly type: PersonType;
  readonly name: string;
  readonly email?: string | null;
  readonly phoneNumber?: string | null;
  readonly isEntity?: boolean;
  readonly address?: string | null;
  readonly category?: PersonCategory;
  readonly actorCode?: string | null;
}

export interface UpdatePersonInput extends CreatePersonInput {
  readonly status: PersonStatus;
}

export const PERSON_ROUTES = {
  people: `${API_PREFIX}/people`,
  person: (id: number) => `${API_PREFIX}/people/${id}`,
} as const;

export type PersonPage = CatalogPage<PersonRecord>;
