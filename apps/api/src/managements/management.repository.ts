import { Injectable } from '@nestjs/common';
import type {
  CreateManagementInput,
  ManagementPage,
  ManagementPlanPage,
  ManagementPlanQuery,
  ManagementPlanRecord,
  ManagementQuery,
  ManagementRecord,
  ManagementStatus,
  ManagementType,
  SyncManagementPlanInput,
  UpdateManagementInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface ManagementRow {
  id: number | string;
  year: number;
  semester: number;
  code: string;
  description: string | null;
  start_date: Date | string | null;
  planned_end_date: Date | string | null;
  actual_closed_date: Date | string | null;
  status: number;
  responsible: string | null;
  type: number;
  faculty_id: number | string | null;
  plan_count: number | string;
  completed_plan_count: number | string;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface PlanRow {
  id: number | string;
  management_id: number | string;
  equipment_unit_id: number | string | null;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  current_phase: number;
  current_state: number;
  responsible: string | null;
  planned_week: number | null;
  executed_week: number | null;
  document_reference: string | null;
  notes: string | null;
  planned_date: Date | string | null;
  plan_status: number;
  is_draft: boolean;
}
interface CountRow {
  total_count: number | string;
}
interface ExistingManagement {
  status: number;
  type: number;
  year: number;
  semester: number;
}

type QueryExecutor = Pick<IPgPool, 'query'>;

const MANAGEMENT_SELECT = `
  SELECT m.id, m.year, m.semester, m.code, m.description, m.start_date,
         m.planned_end_date, m.actual_closed_date, m.status, m.responsible,
         m.type, m.faculty_id,
         count(p.id) FILTER (WHERE p.equipment_unit_id IS NOT NULL AND p.plan_status <> 3)::int AS plan_count,
         count(p.id) FILTER (WHERE p.equipment_unit_id IS NOT NULL AND p.plan_status = 2)::int AS completed_plan_count,
         m.created_at, m.updated_at
    FROM public.lu_management m
    LEFT JOIN public.lu_management_plan p
      ON p.site_id=m.site_id AND p.management_id=m.id`;

const PLAN_SELECT = `
  SELECT p.id, p.management_id, p.equipment_unit_id, u.inventory_number,
         e.name AS equipment_name, l.name AS laboratory_name,
         p.current_phase, p.current_state, p.responsible, p.planned_week,
         p.executed_week, p.document_reference, p.notes, p.planned_date,
         p.plan_status, p.is_draft
    FROM public.lu_management_plan p
    LEFT JOIN public.lu_equipment_unit u
      ON u.site_id=p.site_id AND u.id=p.equipment_unit_id
    LEFT JOIN public.lu_equipment e
      ON e.site_id=p.site_id AND e.id=u.equipment_id
    LEFT JOIN public.lu_laboratory l
      ON l.site_id=p.site_id AND l.id=u.laboratory_id`;

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function nullableInteger(value: number | string | null): number | null {
  return value === null ? null : integer(value);
}
function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function dateOnly(value: Date | string | null): string | null {
  if (value === null) return null;
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? value
    : iso(value).slice(0, 10);
}
function status(value: number): ManagementStatus {
  return value === 0 || value === 1 || value === 2 || value === 99 ? value : 99;
}
function type(value: number): ManagementType {
  return value === 1 ? 1 : 0;
}
function mapManagement(row: ManagementRow): ManagementRecord {
  return {
    id: integer(row.id),
    year: row.year,
    semester: row.semester,
    code: row.code,
    description: row.description,
    startDate: dateOnly(row.start_date),
    plannedEndDate: dateOnly(row.planned_end_date),
    actualClosedDate: dateOnly(row.actual_closed_date),
    status: status(row.status),
    responsible: row.responsible,
    type: type(row.type),
    facultyId: nullableInteger(row.faculty_id),
    planCount: integer(row.plan_count),
    completedPlanCount: integer(row.completed_plan_count),
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
function mapPlan(row: PlanRow): ManagementPlanRecord {
  return {
    id: integer(row.id),
    managementId: integer(row.management_id),
    equipmentUnitId: nullableInteger(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    currentPhase: Math.min(
      6,
      Math.max(1, integer(row.current_phase)),
    ) as ManagementPlanRecord['currentPhase'],
    currentState: Math.min(
      9,
      Math.max(1, integer(row.current_state)),
    ) as ManagementPlanRecord['currentState'],
    responsible: row.responsible,
    plannedWeek: row.planned_week,
    executedWeek: row.executed_week,
    documentReference: row.document_reference,
    notes: row.notes,
    plannedDate: dateOnly(row.planned_date),
    planStatus: Math.min(
      3,
      Math.max(0, integer(row.plan_status)),
    ) as ManagementPlanRecord['planStatus'],
    isDraft: row.is_draft,
  };
}
function clean(
  value: string | null | undefined,
  max: number,
  field: string,
  required = false,
): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (required && result.length === 0)
    throw new CatalogConflictError(`${field} is required.`, field);
  if (result.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return result === '' ? null : result;
}
function validInput(input: CreateManagementInput | UpdateManagementInput): {
  year: number;
  semester: number;
  code: string;
  description: string | null;
  startDate: string | null;
  plannedEndDate: string | null;
  status: ManagementStatus;
  type: ManagementType;
} {
  if (!Number.isInteger(input.year) || input.year < 2000 || input.year > 2100)
    throw new CatalogConflictError('Year is invalid.', 'year');
  if (input.type !== 0 && input.type !== 1)
    throw new CatalogConflictError('Management type is invalid.', 'type');
  const semester = input.type === 1 ? 0 : input.semester;
  if (
    !Number.isInteger(semester) ||
    semester < 0 ||
    semester > 2 ||
    (input.type === 0 && semester === 0)
  )
    throw new CatalogConflictError('Semester is invalid.', 'semester');
  const statusValue = input.status ?? 0;
  if (![0, 1, 2].includes(statusValue))
    throw new CatalogConflictError('Management status is invalid.', 'status');
  const startDate = input.startDate ?? null;
  const plannedEndDate = input.plannedEndDate ?? null;
  if (startDate !== null && plannedEndDate !== null && plannedEndDate < startDate)
    throw new CatalogConflictError('Planned end date cannot precede start date.', 'plannedEndDate');
  const code = input.type === 1 ? `CORR-${input.year}-${semester}` : `${input.year}-${semester}`;
  return {
    year: input.year,
    semester,
    code,
    description: clean(input.description, 1000, 'description'),
    startDate,
    plannedEndDate,
    status: statusValue as ManagementStatus,
    type: input.type,
  };
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

@Injectable()
export class ManagementRepository {
  async list(pool: IPgPool, siteId: string, query: ManagementQuery): Promise<ManagementPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['m.site_id=$1', 'm.status<>99'];
    if (query.type !== undefined) {
      params.push(query.type);
      filters.push(`m.type=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`m.status=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(`(m.code ILIKE $${params.length} OR m.description ILIKE $${params.length})`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_management m WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<ManagementRow>(
      `${MANAGEMENT_SELECT} WHERE ${where} GROUP BY m.id ORDER BY m.status=0 DESC, m.year DESC, m.semester DESC, m.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, (pageIndex - 1) * pageSize],
    );
    return {
      items: rows.rows.map(mapManagement),
      totalCount,
      pageIndex,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async find(pool: QueryExecutor, siteId: string, id: number): Promise<ManagementRecord | null> {
    const rows = await pool.query<ManagementRow>(
      `${MANAGEMENT_SELECT} WHERE m.site_id=$1 AND m.id=$2 GROUP BY m.id`,
      [siteId, id],
    );
    return rows.rows[0] === undefined ? null : mapManagement(rows.rows[0]);
  }

  async plans(
    pool: IPgPool,
    siteId: string,
    managementId: number,
    query: ManagementPlanQuery,
  ): Promise<ManagementPlanPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 50;
    const params: unknown[] = [siteId, managementId];
    const filters = [
      'p.site_id=$1',
      'p.management_id=$2',
      'p.equipment_unit_id IS NOT NULL',
      'p.plan_status<>3',
    ];
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(u.inventory_number ILIKE $${params.length} OR e.name ILIKE $${params.length})`,
      );
    }
    if (query.onlyAvailable) filters.push('p.current_state NOT IN (2,9)');
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_management_plan p LEFT JOIN public.lu_equipment_unit u ON u.site_id=p.site_id AND u.id=p.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=p.site_id AND e.id=u.equipment_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<PlanRow>(
      `${PLAN_SELECT} WHERE ${where} ORDER BY u.inventory_number, p.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, (pageIndex - 1) * pageSize],
    );
    return {
      items: rows.rows.map(mapPlan),
      totalCount,
      pageIndex,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async create(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateManagementInput,
  ): Promise<ManagementRecord> {
    const values = validInput(input);
    try {
      return await pool.transaction(async (client) => {
        if (values.status === 0)
          await this.closeActiveType(client, siteId, values.type, null, userId);
        const result = await client.query<{ id: number | string }>(
          `INSERT INTO public.lu_management (site_id, year, semester, code, description, start_date, planned_end_date, status, responsible, type, faculty_id, created_by_id) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,NULL,$9,$10,$11) RETURNING id`,
          [
            siteId,
            values.year,
            values.semester,
            values.code,
            values.description,
            values.startDate,
            values.plannedEndDate,
            values.status,
            values.type,
            input.facultyId ?? null,
            userId,
          ],
        );
        const row = result.rows[0];
        if (row === undefined) throw new CatalogMissingError('Management was not created.');
        const created = await this.find(client, siteId, integer(row.id));
        if (created === null) throw new CatalogMissingError('Management was not created.');
        return created;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'A management with this type and period already exists or another active management is present.',
          'code',
        );
      throw error;
    }
  }

  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateManagementInput,
  ): Promise<ManagementRecord> {
    const values = validInput(input);
    try {
      return await pool.transaction(async (client) => {
        const current = await client.query<ExistingManagement>(
          `SELECT status, type, year, semester FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
          [siteId, id],
        );
        const existing = current.rows[0];
        if (existing === undefined) throw new CatalogMissingError('Management was not found.');
        if (values.type !== type(existing.type))
          throw new CatalogConflictError(
            'Management type cannot be changed after creation.',
            'type',
          );
        if (values.status === 0)
          await this.closeActiveType(client, siteId, existing.type, id, userId);
        const result = await client.query(
          `UPDATE public.lu_management SET year=$3, semester=$4, code=$5, description=$6, start_date=$7, planned_end_date=$8, status=$9, actual_closed_date=CASE WHEN $9=2 THEN COALESCE(actual_closed_date,CURRENT_TIMESTAMP) WHEN $9=0 THEN NULL ELSE actual_closed_date END, faculty_id=$10, updated_by_id=$11, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>99`,
          [
            siteId,
            id,
            values.year,
            values.semester,
            values.code,
            values.description,
            values.startDate,
            values.plannedEndDate,
            values.status,
            input.facultyId ?? null,
            userId,
          ],
        );
        if (result.rowCount !== 1) throw new CatalogMissingError('Management was not found.');
        const updated = await this.find(client, siteId, id);
        if (updated === null) throw new CatalogMissingError('Management was not found.');
        return updated;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'A management with this type and period already exists or another active management is present.',
          'code',
        );
      throw error;
    }
  }

  async activate(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
  ): Promise<ManagementRecord> {
    return pool.transaction(async (client) => {
      const current = await client.query<ExistingManagement>(
        `SELECT status, type, year, semester FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      const existing = current.rows[0];
      if (existing === undefined) throw new CatalogMissingError('Management was not found.');
      await this.closeActiveType(client, siteId, existing.type, id, userId);
      await client.query(
        `UPDATE public.lu_management SET status=0, actual_closed_date=NULL, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, id, userId],
      );
      const result = await this.find(client, siteId, id);
      if (result === null) throw new CatalogMissingError('Management was not found.');
      return result;
    });
  }

  private async closeActiveType(
    client: IPgClient,
    siteId: string,
    managementType: number,
    exceptId: number | null,
    userId: string,
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_management SET status=2, actual_closed_date=COALESCE(actual_closed_date,CURRENT_TIMESTAMP), updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND type=$2 AND status=0 AND ($4::bigint IS NULL OR id<>$4)`,
      [siteId, managementType, userId, exceptId],
    );
  }

  async close(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
  ): Promise<ManagementRecord> {
    const result = await pool.query(
      `UPDATE public.lu_management SET status=2, actual_closed_date=COALESCE(actual_closed_date,CURRENT_TIMESTAMP), updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Management was not found.');
    const found = await this.find(pool, siteId, id);
    if (found === null) throw new CatalogMissingError('Management was not found.');
    return found;
  }

  async remove(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_management SET status=99, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status NOT IN (0,99)`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1)
      throw new CatalogConflictError(
        'An active management must be closed before logical removal.',
        'status',
      );
  }

  async syncPlans(
    pool: IPgPool,
    siteId: string,
    userId: string,
    managementId: number,
    input: SyncManagementPlanInput,
  ): Promise<void> {
    await pool.transaction(async (client) => {
      const management = await client.query<{ type: number; status: number }>(
        `SELECT type, status FROM public.lu_management WHERE site_id=$1 AND id=$2`,
        [siteId, managementId],
      );
      const row = management.rows[0];
      if (row === undefined) throw new CatalogMissingError('Management was not found.');
      if (row.type !== 0 || row.status !== 0)
        throw new CatalogConflictError(
          'Only an active preventive management can plan assets.',
          'managementId',
        );
      const addIds = [...new Set(input.addUnitIds)];
      const removeIds = [...new Set(input.removeUnitIds)];
      if (
        addIds.some((value) => !Number.isSafeInteger(value) || value < 1) ||
        removeIds.some((value) => !Number.isSafeInteger(value) || value < 1)
      )
        throw new CatalogConflictError('Plan asset identifiers are invalid.');
      for (const unitId of addIds) {
        const unit = await client.query(
          `SELECT u.id FROM public.lu_equipment_unit u JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id WHERE u.site_id=$1 AND u.id=$2 AND u.current_status<>99 AND u.laboratory_id IS NOT NULL AND e.classification_review_status=2`,
          [siteId, unitId],
        );
        if (unit.rows[0] === undefined)
          throw new CatalogConflictError(
            'One selected unit is unavailable or not confirmed.',
            'addUnitIds',
          );
        const existingPlan = await client.query<{ id: number | string }>(
          `SELECT id FROM public.lu_management_plan
             WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3
             LIMIT 1`,
          [siteId, managementId, unitId],
        );
        if (existingPlan.rows[0] === undefined) {
          await client.query(
            `INSERT INTO public.lu_management_plan
               (site_id, management_id, equipment_unit_id, current_phase, current_state, plan_status, created_by_id)
             VALUES ($1,$2,$3,1,1,0,$4)`,
            [siteId, managementId, unitId, userId],
          );
        } else {
          await client.query(
            `UPDATE public.lu_management_plan
                SET plan_status=0, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP
              WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3`,
            [siteId, managementId, unitId, userId],
          );
        }
      }
      for (const unitId of removeIds)
        await client.query(
          `UPDATE public.lu_management_plan SET plan_status=3, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
          [siteId, managementId, unitId, userId],
        );
    });
  }
}
