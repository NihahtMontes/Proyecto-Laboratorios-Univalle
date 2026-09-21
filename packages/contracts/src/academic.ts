import { API_PREFIX } from './healthz.js';
import type { CatalogPage, CatalogStatus } from './catalogs.js';

export interface Faculty {
  readonly id: number;
  readonly name: string;
  readonly code: string | null;
  readonly description: string | null;
  readonly status: CatalogStatus;
  readonly laboratoryCount: number;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface Career {
  readonly id: number;
  readonly code: string | null;
  readonly name: string;
  readonly facultyId: number | null;
  readonly facultyName: string | null;
  readonly status: CatalogStatus;
  readonly siteAssigned: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface Laboratory {
  readonly id: number;
  readonly facultyId: number;
  readonly facultyName: string | null;
  readonly code: string;
  readonly name: string;
  readonly type: string | null;
  readonly building: string | null;
  readonly block: string | null;
  readonly floor: string | null;
  readonly room: string | null;
  readonly description: string | null;
  readonly cityId: number | null;
  readonly cityName: string | null;
  readonly status: CatalogStatus;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface CreateFacultyInput {
  readonly name: string;
  readonly code?: string | null;
  readonly description?: string | null;
}

export interface UpdateFacultyInput extends CreateFacultyInput {
  readonly status: CatalogStatus;
}

export interface CreateCareerInput {
  readonly name: string;
  readonly code?: string | null;
  readonly facultyId?: number | null;
  readonly status?: CatalogStatus;
}

export interface UpdateCareerInput extends CreateCareerInput {
  readonly status: CatalogStatus;
}

export interface CreateLaboratoryInput {
  readonly facultyId: number;
  readonly code: string;
  readonly name: string;
  readonly type?: string | null;
  readonly building?: string | null;
  readonly block?: string | null;
  readonly floor?: string | null;
  readonly room?: string | null;
  readonly description?: string | null;
  readonly cityId?: number | null;
}

export interface UpdateLaboratoryInput extends CreateLaboratoryInput {
  readonly status: CatalogStatus;
}

export interface AcademicQuery {
  readonly currentPage?: number;
  readonly searchTerm?: string;
  readonly statusFilter?: CatalogStatus;
}

export interface SiteCareer {
  readonly careerId: number;
  readonly careerName: string;
  readonly siteId: string;
  readonly status: CatalogStatus;
}

export const ACADEMIC_ROUTES = {
  faculties: `${API_PREFIX}/faculties`,
  faculty: (id: number) => `${API_PREFIX}/faculties/${id}`,
  careers: `${API_PREFIX}/careers`,
  career: (id: number) => `${API_PREFIX}/careers/${id}`,
  siteCareers: `${API_PREFIX}/site-careers`,
  siteCareer: (careerId: number) => `${API_PREFIX}/site-careers/${careerId}`,
  laboratories: `${API_PREFIX}/laboratories`,
  laboratory: (id: number) => `${API_PREFIX}/laboratories/${id}`,
} as const;

export type FacultyPage = CatalogPage<Faculty>;
export type CareerPage = CatalogPage<Career>;
export type LaboratoryPage = CatalogPage<Laboratory>;
