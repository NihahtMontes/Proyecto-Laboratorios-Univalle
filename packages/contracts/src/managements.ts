import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';

export type ManagementType = 0 | 1;
export type ManagementStatus = 0 | 1 | 2 | 99;
export type ManagementPlanStatus = 0 | 1 | 2 | 3;
export type WizardPhase = 1 | 2 | 3 | 4 | 5 | 6;
export type WizardEquipmentState = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;

export interface ManagementRecord {
  readonly id: number;
  readonly year: number;
  readonly semester: number;
  readonly code: string;
  readonly description: string | null;
  readonly startDate: string | null;
  readonly plannedEndDate: string | null;
  readonly actualClosedDate: string | null;
  readonly status: ManagementStatus;
  readonly responsible: string | null;
  readonly type: ManagementType;
  readonly facultyId: number | null;
  readonly planCount: number;
  readonly completedPlanCount: number;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface ManagementQuery {
  readonly currentPage?: number;
  readonly type?: ManagementType;
  readonly statusFilter?: ManagementStatus;
  readonly searchTerm?: string;
}

export interface CreateManagementInput {
  readonly year: number;
  readonly semester: number;
  readonly description?: string | null;
  readonly startDate?: string | null;
  readonly plannedEndDate?: string | null;
  readonly status?: ManagementStatus;
  readonly type: ManagementType;
  readonly facultyId?: number | null;
}

export interface UpdateManagementInput extends CreateManagementInput {
  readonly status: ManagementStatus;
}

export interface ManagementPlanRecord {
  readonly id: number;
  readonly managementId: number;
  readonly equipmentUnitId: number | null;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly currentPhase: WizardPhase;
  readonly currentState: WizardEquipmentState;
  readonly responsible: string | null;
  readonly plannedWeek: number | null;
  readonly executedWeek: number | null;
  readonly documentReference: string | null;
  readonly notes: string | null;
  readonly plannedDate: string | null;
  readonly planStatus: ManagementPlanStatus;
  readonly isDraft: boolean;
}

export interface ManagementPlanQuery {
  readonly currentPage?: number;
  readonly laboratoryId?: number;
  readonly searchTerm?: string;
  readonly onlyAvailable?: boolean;
}

export interface ManagementPlanPage {
  readonly items: readonly ManagementPlanRecord[];
  readonly totalCount: number;
  readonly pageIndex: number;
  readonly totalPages: number;
  readonly pageSize: number;
}

export interface SyncManagementPlanInput {
  readonly addUnitIds: readonly number[];
  readonly removeUnitIds: readonly number[];
}

export const MANAGEMENT_ROUTES = {
  managements: `${API_PREFIX}/managements`,
  management: (id: number) => `${API_PREFIX}/managements/${id}`,
  activate: (id: number) => `${API_PREFIX}/managements/${id}/activate`,
  close: (id: number) => `${API_PREFIX}/managements/${id}/close`,
  plans: (id: number) => `${API_PREFIX}/managements/${id}/plans`,
  syncPlans: (id: number) => `${API_PREFIX}/managements/${id}/plans/sync`,
} as const;

export type ManagementPage = CatalogPage<ManagementRecord>;
