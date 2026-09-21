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
