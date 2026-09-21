import { API_PREFIX } from './healthz.js';
import type { CatalogPage } from './catalogs.js';

export type DepartureType = 1 | 2 | 3 | 4 | 5;
export type LoanStatus = 0 | 1 | 2 | 99;

export interface DepartureItemRecord {
  readonly id: number;
  readonly equipmentUnitId: number | null;
  readonly productName: string | null;
  readonly quantity: number | null;
  readonly unitOfMeasure: string | null;
  readonly returnedQuantity: number | null;
  readonly observations: string | null;
  readonly isRemoved: boolean;
}

export interface DepartureRecord {
  readonly id: number;
  readonly managementId: number;
  readonly managementCode: string | null;
  readonly equipmentUnitId: number | null;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly borrowerId: number | null;
  readonly originLaboratoryId: number | null;
  readonly destination: string | null;
  readonly type: DepartureType;
  readonly departureDate: string;
  readonly estimatedReturnDate: string | null;
  readonly actualReturnDate: string | null;
  readonly departureObservations: string | null;
  readonly returnObservations: string | null;
  readonly status: LoanStatus;
  readonly isDraft: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface DepartureDetail extends DepartureRecord {
  readonly items: readonly DepartureItemRecord[];
}

export interface DepartureQuery {
  readonly currentPage?: number;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly statusFilter?: LoanStatus;
  readonly type?: DepartureType;
  readonly searchTerm?: string;
}

export interface DepartureItemInput {
  readonly id?: number;
  readonly equipmentUnitId?: number | null;
  readonly productName?: string | null;
  readonly quantity?: number | null;
  readonly unitOfMeasure?: string | null;
  readonly returnedQuantity?: number | null;
  readonly observations?: string | null;
}

export interface CreateDepartureInput {
  readonly managementId: number;
  readonly equipmentUnitId?: number | null;
  readonly borrowerId?: number | null;
  readonly originLaboratoryId?: number | null;
  readonly destination?: string | null;
  readonly type?: DepartureType;
  readonly departureDate: string;
  readonly estimatedReturnDate?: string | null;
  readonly actualReturnDate?: string | null;
  readonly departureObservations?: string | null;
  readonly returnObservations?: string | null;
  readonly status?: LoanStatus;
  readonly items?: readonly DepartureItemInput[];
  readonly isDraft?: boolean;
}

export interface UpdateDepartureInput extends CreateDepartureInput {
  readonly status: LoanStatus;
}

export interface MassDepartureInput {
  readonly managementId: number;
  readonly type?: DepartureType;
  readonly destination?: string | null;
  readonly departureDate: string;
  readonly estimatedReturnDate?: string | null;
  readonly rows: readonly { readonly equipmentUnitId: number; readonly planId?: number }[];
  readonly isDraft?: boolean;
}

export const DEPARTURE_ROUTES = {
  departures: `${API_PREFIX}/departures`,
  departure: (id: number) => `${API_PREFIX}/departures/${id}`,
  complete: (id: number) => `${API_PREFIX}/departures/${id}/complete`,
  return: (id: number) => `${API_PREFIX}/departures/${id}/return`,
  cancel: (id: number) => `${API_PREFIX}/departures/${id}/cancel`,
  mass: `${API_PREFIX}/departures/mass`,
} as const;

export type DeparturePage = CatalogPage<DepartureRecord>;
