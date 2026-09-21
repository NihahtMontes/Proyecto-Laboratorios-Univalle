import { API_PREFIX } from './healthz.js';

export type CatalogStatus = 0 | 1 | 2;

export interface CatalogCollectionQuery {
  readonly currentPage?: number;
  readonly searchTerm?: string;
  readonly statusFilter?: CatalogStatus;
}

export interface CatalogPage<T> {
  readonly items: readonly T[];
  readonly totalCount: number;
  readonly pageIndex: number;
  readonly totalPages: number;
  readonly pageSize: number;
}

export interface Country {
  readonly id: number;
  readonly name: string;
  readonly status: CatalogStatus;
  readonly cityCount: number;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface City {
  readonly id: number;
  readonly countryId: number;
  readonly countryName: string;
  readonly name: string;
  readonly region: string | null;
  readonly status: CatalogStatus;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface CreateCountryInput {
  readonly name: string;
}

export interface UpdateCountryInput {
  readonly name: string;
  readonly status: CatalogStatus;
}

export interface CreateCityInput {
  readonly countryId: number;
  readonly name: string;
  readonly region?: string | null;
}

export interface UpdateCityInput {
  readonly countryId: number;
  readonly name: string;
  readonly region?: string | null;
  readonly status: CatalogStatus;
}

export const CATALOG_ROUTES = {
  countries: `${API_PREFIX}/countries`,
  country: (id: number) => `${API_PREFIX}/countries/${id}`,
  cities: `${API_PREFIX}/cities`,
  city: (id: number) => `${API_PREFIX}/cities/${id}`,
} as const;
