import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';
import type {
  MaintenanceCostInput,
  MaintenanceCostRecord,
  MaintenanceSatisfaction,
  MaintenanceTaskInput,
  MaintenanceTaskRecord,
  MaintenanceType,
} from './maintenances.js';

export interface KardexRecord {
  readonly planId: number;
  readonly managementId: number;
  readonly managementCode: string | null;
  readonly equipmentUnitId: number;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly maintenanceId: number;
  readonly departureId: number | null;
  readonly maintenanceType: MaintenanceType;
  readonly technicianId: number | null;
  readonly scheduledDate: string | null;
  readonly startDate: string | null;
  readonly endDate: string | null;
  readonly actualReturnDate: string | null;
  readonly description: string | null;
  readonly actualCost: number;
  readonly suggestedNextMaintenanceDate: string | null;
  readonly satisfactionLevel: MaintenanceSatisfaction | null;
  readonly recommendations: string | null;
  readonly observations: string | null;
  readonly completionPercentage: number;
  readonly currentPhase: 5 | 6;
  readonly currentState: 7 | 8 | 9;
  readonly isDraft: boolean;
  readonly tasks: readonly MaintenanceTaskRecord[];
  readonly costs: readonly MaintenanceCostRecord[];
  readonly history: readonly {
    readonly id: number;
    readonly status: number;
    readonly startDate: string;
    readonly endDate: string | null;
    readonly reason: string | null;
  }[];
}

export interface KardexQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly searchTerm?: string;
}

export interface KardexInput {
  readonly planId: number;
  readonly technicianId?: number | null;
  readonly scheduledDate?: string | null;
  readonly startDate?: string | null;
  readonly endDate?: string | null;
  readonly actualReturnDate?: string | null;
  readonly description?: string | null;
  readonly actualCost?: number | null;
  readonly suggestedNextMaintenanceDate?: string | null;
  readonly satisfactionLevel?: MaintenanceSatisfaction | null;
  readonly recommendations?: string | null;
  readonly observations?: string | null;
  readonly tasks?: readonly MaintenanceTaskInput[];
  readonly costs?: readonly MaintenanceCostInput[];
  readonly isDraft?: boolean;
}

export const KARDEX_ROUTES = {
  kardex: `${API_PREFIX}/kardex`,
  detail: (planId: number) => `${API_PREFIX}/kardex/${planId}`,
  draft: (planId: number) => `${API_PREFIX}/kardex/${planId}/draft`,
  complete: (planId: number) => `${API_PREFIX}/kardex/${planId}/complete`,
} as const;

export type KardexPage = CatalogPage<KardexRecord>;
