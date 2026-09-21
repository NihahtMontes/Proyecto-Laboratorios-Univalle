import { API_PREFIX } from './healthz.js';

export const DASHBOARD_ROUTES = {
  summary: `${API_PREFIX}/dashboard`,
  notifications: `${API_PREFIX}/notifications`,
  markNotificationRead: (id: number): string => `${API_PREFIX}/notifications/${id}/read`,
  markAllNotificationsRead: `${API_PREFIX}/notifications/read-all`,
} as const;

export type DashboardManagementType = 'Preventive' | 'Corrective';

export interface DashboardManagement {
  readonly id: number;
  readonly year: number;
  readonly semester: number;
  readonly code: string;
  readonly description: string | null;
  readonly type: DashboardManagementType;
  readonly status: string;
}

export interface DashboardMetrics {
  readonly totalAssets: number;
  readonly completedAssets: number;
  readonly overdueAssets: number;
  readonly globalProgress: number;
  readonly pendingAssets: number;
  readonly goodAssets: number;
  readonly l6: number;
  readonly l7: number;
  readonly l8: number;
  readonly departures: number;
  readonly disbursements: number;
}

export interface DashboardPlan {
  readonly id: number;
  readonly inventoryNumber: string | null;
  readonly equipmentName: string | null;
  readonly laboratoryName: string | null;
  readonly currentPhase: number;
  readonly currentState: number;
  readonly planStatus: number;
  readonly plannedDate: string | null;
  readonly isDraft: boolean;
}

export interface DashboardChart {
  readonly label: string;
  readonly value: number;
}

export interface DashboardSummary {
  readonly activeManagement: DashboardManagement | null;
  readonly metrics: DashboardMetrics;
  readonly equipmentTypes: readonly DashboardChart[];
  readonly groups: readonly DashboardChart[];
  readonly laboratories: readonly DashboardChart[];
  readonly overduePlans: readonly DashboardPlan[];
  readonly plans: readonly DashboardPlan[];
  readonly page: number;
  readonly pageSize: number;
}

export interface DashboardQuery {
  readonly managementId?: number;
  readonly step?: number;
  readonly currentPage?: number;
  readonly laboratoryId?: number;
  readonly searchTerm?: string;
  readonly serialNumber?: string;
  readonly inventoryNumber?: string;
}

export interface DashboardNotification {
  readonly id: number;
  readonly title: string;
  readonly message: string;
  readonly actionUrl: string | null;
  readonly iconClass: string | null;
  readonly managementId: number | null;
  readonly managementType: DashboardManagementType | null;
  readonly scope: string | null;
  readonly isRead: boolean;
  readonly createdAt: string;
}

export interface DashboardNotificationsQuery {
  readonly unreadOnly?: boolean;
  readonly managementId?: number;
  readonly scope?: string;
}
