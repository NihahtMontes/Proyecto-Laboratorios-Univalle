import { Injectable } from '@nestjs/common';
import type {
  DashboardManagement,
  DashboardManagementType,
  DashboardNotification,
  DashboardPlan,
  DashboardQuery,
  DashboardSummary,
} from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';

interface ManagementRow {
  id: number | string;
  year: number;
  semester: number;
  code: string;
  description: string | null;
  type: number;
  status: number;
}

interface MetricRow {
  total_assets: number;
  completed_assets: number;
  overdue_assets: number;
  pending_assets: number;
  good_assets: number;
  l6: number;
  l7: number;
  l8: number;
  departures: number;
  disbursements: number;
}

interface ChartRow {
  label: string;
  value: number;
}

interface PlanRow {
  id: number | string;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  current_phase: number;
  current_state: number;
  plan_status: number;
  planned_date: Date | string | null;
  is_draft: boolean;
}

interface NotificationRow {
  id: number | string;
  title: string;
  message: string;
  action_url: string | null;
  icon_class: string | null;
  management_id: number | string | null;
  management_type: number | null;
  scope: string | null;
  is_read: boolean;
  created_at: Date | string;
}

function managementType(value: number): DashboardManagementType {
  return value === 1 ? 'Corrective' : 'Preventive';
}

function integer(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}

function isoDate(value: Date | string | null): string | null {
  if (value === null) return null;
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}

function mapManagement(row: ManagementRow): DashboardManagement {
  return {
    id: integer(row.id),
    year: row.year,
    semester: row.semester,
    code: row.code,
    description: row.description,
    type: managementType(row.type),
    status: row.status === 0 ? 'Active' : row.status === 2 ? 'Completed' : 'Inactive',
  };
}

function mapPlan(row: PlanRow): DashboardPlan {
  return {
    id: integer(row.id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    currentPhase: row.current_phase,
    currentState: row.current_state,
    planStatus: row.plan_status,
    plannedDate: isoDate(row.planned_date),
    isDraft: row.is_draft,
  };
}

function mapNotification(row: NotificationRow): DashboardNotification {
  return {
    id: integer(row.id),
    title: row.title,
    message: row.message,
    actionUrl: row.action_url,
    iconClass: row.icon_class,
    managementId: row.management_id === null ? null : integer(row.management_id),
    managementType: row.management_type === null ? null : managementType(row.management_type),
    scope: row.scope,
    isRead: row.is_read,
    createdAt: isoDate(row.created_at)!,
  };
}

function buildPlanFilters(query: DashboardQuery, params: unknown[]): string {
  const filters = ['p.site_id = $1', 'p.management_id = $2', 'p.equipment_unit_id IS NOT NULL'];
  if (query.laboratoryId !== undefined) {
    params.push(query.laboratoryId);
    filters.push(`u.laboratory_id = $${params.length}`);
  }
  if (query.searchTerm !== undefined && query.searchTerm !== '') {
    params.push(`%${query.searchTerm}%`);
    filters.push(`e.name ILIKE $${params.length}`);
  }
  if (query.serialNumber !== undefined && query.serialNumber !== '') {
    params.push(`%${query.serialNumber}%`);
    filters.push(`u.serial_number ILIKE $${params.length}`);
  }
  if (query.inventoryNumber !== undefined && query.inventoryNumber !== '') {
    params.push(`%${query.inventoryNumber}%`);
    filters.push(`u.inventory_number ILIKE $${params.length}`);
  }
  return filters.join(' AND ');
}

@Injectable()
export class DashboardRepository {
  async findManagement(
    pool: IPgPool,
    siteId: string,
    managementId?: number,
  ): Promise<DashboardManagement | null> {
    const result = await pool.query<ManagementRow>(
      `SELECT id, year, semester, code, description, type, status
         FROM public.lu_management
        WHERE site_id = $1
          AND status <> 99
          AND ($2::integer IS NULL OR id = $2)
        ORDER BY status = 0 DESC, year DESC, semester DESC, id DESC
        LIMIT 1`,
      [siteId, managementId ?? null],
    );
    const row = result.rows[0];
    return row === undefined ? null : mapManagement(row);
  }

  async loadSummary(
    pool: IPgPool,
    siteId: string,
    query: DashboardQuery,
  ): Promise<DashboardSummary> {
    const management = await this.findManagement(pool, siteId, query.managementId);
    const page = Math.max(1, query.currentPage ?? 1);
    const pageSize = 5;
    if (management === null) {
      return {
        activeManagement: null,
        metrics: {
          totalAssets: 0,
          completedAssets: 0,
          overdueAssets: 0,
          globalProgress: 0,
          pendingAssets: 0,
          goodAssets: 0,
          l6: 0,
          l7: 0,
          l8: 0,
          departures: 0,
          disbursements: 0,
        },
        equipmentTypes: [],
        groups: [],
        laboratories: [],
        overduePlans: [],
        plans: [],
        page,
        pageSize,
      };
    }

    const metricResult = await pool.query<MetricRow>(
      `SELECT
          count(*)::int AS total_assets,
          count(*) FILTER (WHERE p.current_state = 9 OR p.plan_status = 2)::int AS completed_assets,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.planned_date < CURRENT_TIMESTAMP)::int AS overdue_assets,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.current_state >= 3)::int AS pending_assets,
          count(*) FILTER (WHERE p.current_state = 2)::int AS good_assets,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND m.type = 0 AND p.current_phase = 1 AND (p.current_state = 1 OR (p.is_draft AND p.draft_phase = 1)))::int AS l6,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.current_phase = 2)::int AS l7,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.current_phase = 3)::int AS l8,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.current_phase = 4)::int AS departures,
          count(*) FILTER (WHERE p.current_state NOT IN (2, 9) AND p.plan_status <> 2 AND p.current_phase = 6)::int AS disbursements
       FROM public.lu_management_plan p
       JOIN public.lu_management m ON m.id = p.management_id AND m.site_id = p.site_id
      WHERE p.site_id = $1 AND p.management_id = $2 AND p.equipment_unit_id IS NOT NULL`,
      [siteId, management.id],
    );
    const metric = metricResult.rows[0] ?? {
      total_assets: 0,
      completed_assets: 0,
      overdue_assets: 0,
      pending_assets: 0,
      good_assets: 0,
      l6: 0,
      l7: 0,
      l8: 0,
      departures: 0,
      disbursements: 0,
    };
    const progressBase = metric.total_assets;
    const globalProgress =
      progressBase === 0
        ? 0
        : Math.round(((metric.good_assets + metric.completed_assets) / progressBase) * 1000) / 10;

    const params: unknown[] = [siteId, management.id];
    const filterSql = buildPlanFilters(query, params);
    const planParams = [...params, (page - 1) * pageSize, pageSize];
    const planResult = await pool.query<PlanRow>(
      `SELECT p.id, u.inventory_number, e.name AS equipment_name, l.name AS laboratory_name,
              p.current_phase, p.current_state, p.plan_status, p.planned_date, p.is_draft
         FROM public.lu_management_plan p
         LEFT JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
         LEFT JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = p.site_id
         LEFT JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = p.site_id
        WHERE ${filterSql}
        ORDER BY p.planned_date NULLS LAST, p.id
        OFFSET $${planParams.length - 1} LIMIT $${planParams.length}`,
      planParams,
    );
    const overdueResult = await pool.query<PlanRow>(
      `SELECT p.id, u.inventory_number, e.name AS equipment_name, l.name AS laboratory_name,
              p.current_phase, p.current_state, p.plan_status, p.planned_date, p.is_draft
         FROM public.lu_management_plan p
         LEFT JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
         LEFT JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = p.site_id
         LEFT JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = p.site_id
        WHERE ${filterSql}
          AND p.current_state NOT IN (2, 9)
          AND p.plan_status <> 2
          AND p.planned_date < CURRENT_TIMESTAMP
        ORDER BY p.planned_date, p.id
        LIMIT 10`,
      params,
    );
    const charts = await this.loadCharts(pool, siteId, management.id);

    return {
      activeManagement: management,
      metrics: {
        totalAssets: metric.total_assets,
        completedAssets: metric.completed_assets,
        overdueAssets: metric.overdue_assets,
        globalProgress,
        pendingAssets: metric.pending_assets,
        goodAssets: metric.good_assets,
        l6: metric.l6,
        l7: metric.l7,
        l8: metric.l8,
        departures: metric.departures,
        disbursements: metric.disbursements,
      },
      ...charts,
      overduePlans: overdueResult.rows.map(mapPlan),
      plans: planResult.rows.map(mapPlan),
      page,
      pageSize,
    };
  }

  private async loadCharts(pool: IPgPool, siteId: string, managementId: number) {
    const base = `FROM public.lu_management_plan p
      JOIN public.lu_management m ON m.id = p.management_id AND m.site_id = p.site_id
      JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
      JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = p.site_id
      LEFT JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = p.site_id
     WHERE p.site_id = $1 AND p.management_id = $2 AND p.equipment_unit_id IS NOT NULL`;
    const [types, groups, laboratories] = await Promise.all([
      pool.query<ChartRow>(
        `SELECT COALESCE(e.type_classification, e.other_classification_detail, 'Otro') AS label, count(*)::int AS value ${base} GROUP BY 1 ORDER BY value DESC, label LIMIT 10`,
        [siteId, managementId],
      ),
      pool.query<ChartRow>(
        `SELECT e.category::text AS label, count(*)::int AS value ${base} GROUP BY 1 ORDER BY value DESC, label LIMIT 10`,
        [siteId, managementId],
      ),
      pool.query<ChartRow>(
        `SELECT COALESCE(l.code || ' - ' || l.name, 'N/A') AS label, count(*)::int AS value ${base} GROUP BY 1 ORDER BY value DESC, label LIMIT 10`,
        [siteId, managementId],
      ),
    ]);
    return {
      equipmentTypes: types.rows,
      groups: groups.rows,
      laboratories: laboratories.rows,
    };
  }

  async listNotifications(
    pool: IPgPool,
    siteId: string,
    query: { unreadOnly?: boolean; managementId?: number; scope?: string },
  ): Promise<readonly DashboardNotification[]> {
    const conditions = ['site_id = $1'];
    const params: unknown[] = [siteId];
    if (query.unreadOnly === true) conditions.push('is_read = false');
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      conditions.push(`management_id = $${params.length}`);
    }
    if (query.scope !== undefined && query.scope !== '') {
      params.push(query.scope);
      conditions.push(`scope = $${params.length}`);
    }
    const result = await pool.query<NotificationRow>(
      `SELECT id, title, message, action_url, icon_class, management_id, management_type,
              scope, is_read, created_at
         FROM public.lu_notification
        WHERE ${conditions.join(' AND ')}
        ORDER BY created_at DESC, id DESC
        LIMIT 100`,
      params,
    );
    return result.rows.map(mapNotification);
  }

  async markNotificationRead(pool: IPgPool, siteId: string, id: number): Promise<void> {
    await pool.query(
      `UPDATE public.lu_notification SET is_read = true WHERE site_id = $1 AND id = $2`,
      [siteId, id],
    );
  }

  async markAllNotificationsRead(pool: IPgPool, siteId: string): Promise<void> {
    await pool.query(`UPDATE public.lu_notification SET is_read = true WHERE site_id = $1`, [
      siteId,
    ]);
  }
}
