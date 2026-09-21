import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';

export type MaintenanceType = 1 | 2 | 3 | 4 | 5 | 99;
export type MaintenanceStatus = 0 | 1 | 2 | 3 | 99;
export type MaintenanceServiceType = 0 | 1;
export type MaintenanceSatisfaction = 1 | 2 | 3 | 4 | 5;

export interface MaintenanceTaskRecord {
  readonly id: number;
  readonly description: string;
  readonly isCompleted: boolean;
}

export interface MaintenanceCostRecord {
  readonly id: number;
  readonly concept: string;
  readonly description: string | null;
  readonly quantity: number;
  readonly unitOfMeasure: string | null;
  readonly unitPrice: number;
  readonly category: number;
  readonly provider: string | null;
  readonly costDate: string | null;
  readonly invoiceNumber: string | null;
}

export interface MaintenanceRecord {
  readonly id: number;
  readonly equipmentUnitId: number;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly managementId: number;
  readonly managementCode: string | null;
  readonly requestId: number | null;
  readonly maintenanceType: MaintenanceType;
  readonly serviceType: MaintenanceServiceType;
  readonly institutionalCode: string | null;
  readonly technicianId: number | null;
  readonly scheduledDate: string | null;
  readonly startDate: string | null;
  readonly endDate: string | null;
  readonly description: string | null;
  readonly status: MaintenanceStatus;
  readonly completionPercentage: number;
  readonly estimatedCost: number | null;
  readonly actualCost: number | null;
  readonly recommendations: string | null;
  readonly suggestedNextMaintenanceDate: string | null;
  readonly satisfactionLevel: MaintenanceSatisfaction | null;
  readonly observations: string | null;
  readonly calculatedTotal: number;
  readonly isDraft: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface MaintenanceDetail extends MaintenanceRecord {
  readonly tasks: readonly MaintenanceTaskRecord[];
  readonly costs: readonly MaintenanceCostRecord[];
}

export interface MaintenanceQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly statusFilter?: MaintenanceStatus;
  readonly searchTerm?: string;
}

export interface MaintenanceTaskInput {
  readonly id?: number;
  readonly description: string;
  readonly isCompleted?: boolean;
}

export interface MaintenanceCostInput {
  readonly id?: number;
  readonly concept: string;
  readonly description?: string | null;
  readonly quantity: number;
  readonly unitOfMeasure?: string | null;
  readonly unitPrice: number;
  readonly category?: number;
  readonly provider?: string | null;
  readonly costDate?: string | null;
  readonly invoiceNumber?: string | null;
}

export interface CreateMaintenanceInput {
  readonly managementId: number;
  readonly equipmentUnitId: number;
  readonly requestId?: number | null;
  readonly maintenanceType?: MaintenanceType;
  readonly serviceType?: MaintenanceServiceType;
  readonly institutionalCode?: string | null;
  readonly technicianId?: number | null;
  readonly scheduledDate?: string | null;
  readonly startDate?: string | null;
  readonly endDate?: string | null;
  readonly description?: string | null;
  readonly status?: MaintenanceStatus;
  readonly estimatedCost?: number | null;
  readonly actualCost?: number | null;
  readonly recommendations?: string | null;
  readonly suggestedNextMaintenanceDate?: string | null;
  readonly satisfactionLevel?: MaintenanceSatisfaction | null;
  readonly observations?: string | null;
  readonly tasks?: readonly MaintenanceTaskInput[];
  readonly costs?: readonly MaintenanceCostInput[];
  readonly isDraft?: boolean;
}

export interface UpdateMaintenanceInput extends CreateMaintenanceInput {
  readonly status: MaintenanceStatus;
}

export const MAINTENANCE_ROUTES = {
  maintenances: `${API_PREFIX}/maintenances`,
  maintenance: (id: number) => `${API_PREFIX}/maintenances/${id}`,
  complete: (id: number) => `${API_PREFIX}/maintenances/${id}/complete`,
  cancel: (id: number) => `${API_PREFIX}/maintenances/${id}/cancel`,
} as const;

export type MaintenancePage = CatalogPage<MaintenanceRecord>;
