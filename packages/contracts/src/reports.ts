import { API_PREFIX } from './healthz.js';

export type ReportKind = 'l6' | 'l7' | 'l8' | 'l3' | 'l48' | 'l12';

export interface ReportDownloadQuery {
  readonly kind: ReportKind;
  readonly managementId?: number;
  readonly laboratoryId?: number;
  readonly planId?: number;
  readonly requestId?: number;
  readonly departureId?: number;
}

export interface ReportManifestItem {
  readonly kind: ReportKind;
  readonly label: string;
  readonly format: 'xlsx';
  readonly available: boolean;
  readonly route: string;
}

export const REPORT_ROUTES = {
  manifest: `${API_PREFIX}/reports`,
  download: `${API_PREFIX}/reports/download`,
} as const;
