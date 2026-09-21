import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';
import type { RequestPriority, RequestStatus } from './requests.js';
import type { MaintenanceCostInput, MaintenanceCostRecord } from './maintenances.js';

export interface AcquisitionRecord {
  readonly id: number;
  readonly managementId: number;
  readonly managementCode: string | null;
  readonly equipmentUnitId: number;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly description: string;
  readonly observations: string | null;
  readonly priority: RequestPriority;
  readonly status: RequestStatus;
  readonly investmentCode: string | null;
  readonly costCenter: string | null;
  readonly isDraft: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface AcquisitionDetail extends AcquisitionRecord {
  readonly costs: readonly MaintenanceCostRecord[];
}

export interface AcquisitionQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly statusFilter?: RequestStatus;
  readonly searchTerm?: string;
}

export interface CreateAcquisitionInput {
  readonly managementId: number;
  readonly equipmentUnitId: number;
  readonly description: string;
  readonly observations?: string | null;
  readonly priority?: RequestPriority;
  readonly investmentCode: string;
  readonly costCenter: string;
  readonly costs?: readonly MaintenanceCostInput[];
  readonly isDraft?: boolean;
}

export interface UpdateAcquisitionInput extends CreateAcquisitionInput {
  readonly status: RequestStatus;
}

export const ACQUISITION_ROUTES = {
  acquisitions: `${API_PREFIX}/acquisitions`,
  acquisition: (id: number) => `${API_PREFIX}/acquisitions/${id}`,
  complete: (id: number) => `${API_PREFIX}/acquisitions/${id}/complete`,
  cancel: (id: number) => `${API_PREFIX}/acquisitions/${id}/cancel`,
} as const;

export type AcquisitionPage = CatalogPage<AcquisitionRecord>;
