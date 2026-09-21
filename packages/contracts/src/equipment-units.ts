import { API_PREFIX } from './healthz.js';
import type { CatalogStatus, CatalogPage } from './catalogs.js';

export type EquipmentUnitStatus = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 10 | 99;
export type PhysicalCondition = 1 | 2 | 3 | 4 | 5;
export type LocationResolutionStatus = 0 | 1;

export interface EquipmentUnitRecord {
  readonly id: number;
  readonly equipmentId: number;
  readonly equipmentName: string | null;
  readonly equipmentCategory: 0 | 1 | 2 | null;
  readonly laboratoryId: number | null;
  readonly laboratoryCode: string | null;
  readonly laboratoryName: string | null;
  readonly facultyId: number | null;
  readonly careerId: number | null;
  readonly inventoryNumber: string;
  readonly serialNumber: string | null;
  readonly internalLocation: string | null;
  readonly acquisitionDate: string | null;
  readonly manufacturingDate: string | null;
  readonly acquisitionValue: number | null;
  readonly currentStatus: EquipmentUnitStatus;
  readonly physicalCondition: PhysicalCondition | null;
  readonly locationResolutionStatus: LocationResolutionStatus;
  readonly notes: string | null;
  readonly yearsInOperation: number | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface EquipmentUnitQuery {
  readonly currentPage?: number;
  readonly searchTerm?: string;
  readonly laboratoryId?: number;
  readonly equipmentId?: number;
  readonly statusFilter?: EquipmentUnitStatus;
  readonly locationResolutionStatus?: LocationResolutionStatus;
  readonly includeUnresolved?: boolean;
}

export interface CreateEquipmentUnitInput {
  readonly equipmentId: number;
  readonly laboratoryId: number;
  readonly careerId?: number | null;
  readonly inventoryNumber: string;
  readonly serialNumber?: string | null;
  readonly internalLocation?: string | null;
  readonly acquisitionDate?: string | null;
  readonly manufacturingDate?: string | null;
  readonly acquisitionValue?: number | null;
  readonly currentStatus?: EquipmentUnitStatus;
  readonly physicalCondition?: PhysicalCondition | null;
  readonly notes?: string | null;
}

export interface UpdateEquipmentUnitInput extends CreateEquipmentUnitInput {
  readonly status: EquipmentUnitStatus;
}

export interface EquipmentUnitStateHistory {
  readonly id: number;
  readonly equipmentUnitId: number;
  readonly status: EquipmentUnitStatus;
  readonly startDate: string;
  readonly endDate: string | null;
  readonly reason: string | null;
}

export interface EquipmentUnitDetail extends EquipmentUnitRecord {
  readonly stateHistory: readonly EquipmentUnitStateHistory[];
}

export const EQUIPMENT_UNIT_ROUTES = {
  units: `${API_PREFIX}/equipment-units`,
  unit: (id: number) => `${API_PREFIX}/equipment-units/${id}`,
  history: (id: number) => `${API_PREFIX}/equipment-units/${id}/history`,
} as const;

export type EquipmentUnitPage = CatalogPage<EquipmentUnitRecord>;

// Kept as a type-only reference so consumers can use the shared status shape
// when composing filters without importing implementation details.
export type EquipmentUnitCatalogStatus = CatalogStatus;
