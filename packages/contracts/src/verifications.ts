import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';
import type { EquipmentUnitStatus, PhysicalCondition } from './equipment-units.js';

export type VerificationStatus = 0 | 1 | 2 | 3 | 99;
export type VerificationResult = 0 | 1;

export interface VerificationCheckItem {
  readonly id: number;
  readonly name: string;
  readonly category: string | null;
  readonly order: number;
  readonly isActive: boolean;
}

export interface VerificationCheckResult {
  readonly checkItemId: number;
  readonly result: VerificationResult;
}

export interface VerificationFault {
  readonly id: number;
  readonly description: string;
  readonly isDeleted: boolean;
}

export interface VerificationRecord {
  readonly id: number;
  readonly equipmentUnitId: number;
  readonly managementId: number;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly date: string;
  readonly observations: string | null;
  readonly physicalCondition: PhysicalCondition;
  readonly observedEquipmentStatus: EquipmentUnitStatus | null;
  readonly status: VerificationStatus;
  readonly completionPercentage: number;
  readonly faultsCount: number;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface VerificationDetail extends VerificationRecord {
  readonly checkResults: readonly VerificationCheckResult[];
  readonly faults: readonly VerificationFault[];
}

export interface VerificationQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly statusFilter?: VerificationStatus;
  readonly searchTerm?: string;
}

export interface CreateVerificationInput {
  readonly managementId: number;
  readonly equipmentUnitId: number;
  readonly date: string;
  readonly observations?: string | null;
  readonly physicalCondition: PhysicalCondition;
  readonly observedEquipmentStatus?: EquipmentUnitStatus | null;
  readonly status?: VerificationStatus;
  readonly checkResults?: readonly VerificationCheckResult[];
  readonly faults?: readonly string[];
}

export interface MassVerificationRow {
  readonly managementPlanId: number;
  readonly equipmentUnitId: number;
  readonly physicalCondition: PhysicalCondition;
  readonly observations?: string | null;
  readonly faults?: readonly string[];
}

export interface MassVerificationInput {
  readonly managementId: number;
  readonly date: string;
  readonly saveDraft?: boolean;
  readonly rows: readonly MassVerificationRow[];
}

export const VERIFICATION_ROUTES = {
  verifications: `${API_PREFIX}/verifications`,
  verification: (id: number) => `${API_PREFIX}/verifications/${id}`,
  checkItems: `${API_PREFIX}/verifications/check-items`,
  mass: `${API_PREFIX}/verifications/mass`,
} as const;

export type VerificationPage = CatalogPage<VerificationRecord>;
