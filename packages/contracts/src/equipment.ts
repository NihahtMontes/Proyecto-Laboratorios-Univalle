import { API_PREFIX } from './healthz.js';
import type { CatalogPage, CatalogStatus } from './catalogs.js';

export interface EquipmentRecord {
  readonly id: number;
  readonly category: 0 | 1 | 2;
  readonly utensilType: number | null;
  readonly typeClassification: number | null;
  readonly classificationReviewStatus: number;
  readonly otherClassificationDetail: string | null;
  readonly status: CatalogStatus;
  readonly catalogCode: string | null;
  readonly imageUrl: string | null;
  readonly countryId: number | null;
  readonly countryName: string | null;
  readonly cityId: number | null;
  readonly cityName: string | null;
  readonly name: string;
  readonly brand: string | null;
  readonly model: string | null;
  readonly usefulLifeYears: number | null;
  readonly description: string | null;
  readonly notes: readonly string[];
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface EquipmentQuery {
  readonly currentPage?: number;
  readonly searchTerm?: string;
  readonly category?: 0 | 1 | 2;
  readonly typeClassification?: number;
  readonly utensilType?: number;
  readonly reviewStatus?: number;
  readonly statusFilter?: CatalogStatus;
}

export interface CreateEquipmentInput {
  readonly category: 0 | 1 | 2;
  readonly utensilType?: number | null;
  readonly typeClassification?: number | null;
  readonly otherClassificationDetail?: string | null;
  readonly catalogCode: string;
  readonly countryId?: number | null;
  readonly cityId?: number | null;
  readonly name: string;
  readonly brand?: string | null;
  readonly model?: string | null;
  readonly usefulLifeYears?: number | null;
  readonly description?: string | null;
  readonly notes?: readonly string[];
}

export interface UpdateEquipmentInput extends CreateEquipmentInput {
  readonly status: CatalogStatus;
}

export const EQUIPMENT_ROUTES = {
  equipment: `${API_PREFIX}/equipment`,
  detail: (id: number) => `${API_PREFIX}/equipment/${id}`,
} as const;

export type EquipmentPage = CatalogPage<EquipmentRecord>;
