import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';

export type RequestType = 1 | 2 | 3;
export type RequestPriority = 0 | 1 | 2 | 3;
export type RequestStatus = 0 | 1 | 2 | 3 | 4 | 5 | 99;
export type RequestLocationResolutionStatus = 0 | 1;

export interface RequestRecord {
  readonly id: number;
  readonly laboratoryId: number | null;
  readonly laboratoryName: string | null;
  readonly locationResolutionStatus: RequestLocationResolutionStatus;
  readonly equipmentId: number | null;
  readonly equipmentName: string | null;
  readonly equipmentUnitId: number | null;
  readonly inventoryNumber: string | null;
  readonly managementId: number;
  readonly managementCode: string | null;
  readonly description: string;
  readonly priority: RequestPriority;
  readonly observations: string | null;
  readonly suggestion: string | null;
  readonly requestDate: string | null;
  readonly estimatedRepairTime: string | null;
  readonly status: RequestStatus;
  readonly approvedById: string | null;
  readonly approvalDate: string | null;
  readonly rejectionReason: string | null;
  readonly type: RequestType;
  readonly investmentCode: string | null;
  readonly costCenter: string | null;
  readonly linkCount: number;
  readonly isDraft: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface RequestDetail extends RequestRecord {
  readonly linkedEquipmentUnitIds: readonly number[];
}

export interface RequestQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly statusFilter?: RequestStatus;
  readonly priorityFilter?: RequestPriority;
  readonly type?: RequestType;
  readonly searchTerm?: string;
}

export interface CreateRequestInput {
  readonly managementId: number;
  readonly equipmentUnitId: number;
  readonly description: string;
  readonly priority?: RequestPriority;
  readonly observations?: string | null;
  readonly suggestion?: string | null;
  readonly requestDate?: string | null;
  readonly estimatedRepairTime?: string | null;
  readonly type?: RequestType;
  readonly investmentCode?: string | null;
  readonly costCenter?: string | null;
  readonly isDraft?: boolean;
}

export interface UpdateRequestInput extends CreateRequestInput {
  readonly status: RequestStatus;
  readonly rejectionReason?: string | null;
}

export const REQUEST_ROUTES = {
  requests: `${API_PREFIX}/requests`,
  request: (id: number) => `${API_PREFIX}/requests/${id}`,
  complete: (id: number) => `${API_PREFIX}/requests/${id}/complete`,
  cancel: (id: number) => `${API_PREFIX}/requests/${id}/cancel`,
} as const;

export type RequestPage = CatalogPage<RequestRecord>;
