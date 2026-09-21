import { Injectable } from '@nestjs/common';
import type {
  CreateVerificationInput,
  EquipmentUnitStatus,
  MassVerificationInput,
  PhysicalCondition,
  VerificationCheckItem,
  VerificationDetail,
  VerificationPage,
  VerificationQuery,
  VerificationRecord,
  VerificationStatus,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface VerificationRow {
  id: number | string;
  equipment_unit_id: number | string;
  management_id: number | string;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  verification_date: Date | string;
  observations: string | null;
  physical_condition: number;
  observed_equipment_status: number | null;
  status: number;
  fault_count: number | string;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface ResultRow {
  check_item_id: number | string;
  result: number;
}
interface FaultRow {
  id: number | string;
  description: string;
  is_deleted: boolean;
}
interface CheckItemRow {
  id: number | string;
  name: string;
  category: string | null;
  display_order: number;
  is_active: boolean;
}
interface CountRow {
  total_count: number | string;
}
interface PlanRow {
  id: number | string;
  current_phase: number;
  current_state: number;
  plan_status: number;
  is_draft: boolean;
}
type QueryExecutor = Pick<IPgPool, 'query'>;

const SELECT = `
  SELECT v.id, v.equipment_unit_id, v.management_id, u.inventory_number,
         e.name AS equipment_name, l.name AS laboratory_name, v.verification_date,
         v.observations, v.physical_condition, v.observed_equipment_status, v.status,
         (SELECT count(*)::int FROM public.lu_verification_fault f
           WHERE f.site_id=v.site_id AND f.verification_id=v.id AND f.is_deleted=false) AS fault_count,
         v.created_at, v.updated_at
    FROM public.lu_verification v
    LEFT JOIN public.lu_equipment_unit u ON u.site_id=v.site_id AND u.id=v.equipment_unit_id
    LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id
    LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id`;

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function dateOnly(value: Date | string): string {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? value
    : iso(value).slice(0, 10);
}
function verificationStatus(value: number): VerificationStatus {
  return [0, 1, 2, 3, 99].includes(value) ? (value as VerificationStatus) : 99;
}
function condition(value: number): PhysicalCondition {
  return [1, 2, 3, 4, 5].includes(value) ? (value as PhysicalCondition) : 1;
}
function equipmentStatus(value: number | null): EquipmentUnitStatus | null {
  return value === null
    ? null
    : [0, 1, 2, 3, 4, 5, 6, 10, 99].includes(value)
      ? (value as EquipmentUnitStatus)
      : null;
}
function map(row: VerificationRow): VerificationRecord {
  return {
    id: integer(row.id),
    equipmentUnitId: integer(row.equipment_unit_id),
    managementId: integer(row.management_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    date: dateOnly(row.verification_date),
    observations: row.observations,
    physicalCondition: condition(row.physical_condition),
    observedEquipmentStatus: equipmentStatus(row.observed_equipment_status),
    status: verificationStatus(row.status),
    completionPercentage: 0,
    faultsCount: integer(row.fault_count),
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
function clean(value: string | null | undefined, max: number, field: string): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return result === '' ? null : result;
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

@Injectable()
export class VerificationRepository {
  async list(pool: IPgPool, siteId: string, query: VerificationQuery): Promise<VerificationPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['v.site_id=$1', 'v.status<>99'];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`v.management_id=$${params.length}`);
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`v.status=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(u.inventory_number ILIKE $${params.length} OR e.name ILIKE $${params.length} OR l.name ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_verification v LEFT JOIN public.lu_equipment_unit u ON u.site_id=v.site_id AND u.id=v.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<VerificationRow>(
      `${SELECT} WHERE ${where} ORDER BY v.verification_date DESC, v.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, (pageIndex - 1) * pageSize],
    );
    return {
      items: rows.rows.map(map),
      totalCount,
      pageIndex,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async find(pool: QueryExecutor, siteId: string, id: number): Promise<VerificationDetail | null> {
    const rows = await pool.query<VerificationRow>(`${SELECT} WHERE v.site_id=$1 AND v.id=$2`, [
      siteId,
      id,
    ]);
    const row = rows.rows[0];
    if (row === undefined) return null;
    const results = await pool.query<ResultRow>(
      `SELECT check_item_id, result FROM public.lu_verification_check_result WHERE site_id=$1 AND verification_id=$2 ORDER BY check_item_id`,
      [siteId, id],
    );
    const faults = await pool.query<FaultRow>(
      `SELECT id, description, is_deleted FROM public.lu_verification_fault WHERE site_id=$1 AND verification_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, id],
    );
    const base = map(row);
    const completion =
      results.rows.length === 0
        ? 0
        : Math.round(
            (results.rows.filter((item) => item.result !== 0).length / results.rows.length) * 100,
          );
    return {
      ...base,
      completionPercentage: completion,
      checkResults: results.rows.map((item) => ({
        checkItemId: integer(item.check_item_id),
        result: item.result === 1 ? 1 : 0,
      })),
      faults: faults.rows.map((item) => ({
        id: integer(item.id),
        description: item.description,
        isDeleted: item.is_deleted,
      })),
    };
  }

  async checkItems(pool: IPgPool): Promise<readonly VerificationCheckItem[]> {
    const rows = await pool.query<CheckItemRow>(
      `SELECT id, name, category, display_order, is_active FROM public.lu_verification_check_item WHERE is_active=true ORDER BY display_order, id`,
    );
    return rows.rows.map((item) => ({
      id: integer(item.id),
      name: item.name,
      category: item.category,
      order: item.display_order,
      isActive: item.is_active,
    }));
  }

  private async validatePlan(
    client: IPgClient,
    siteId: string,
    managementId: number,
    equipmentUnitId: number,
  ): Promise<PlanRow> {
    const management = await client.query(
      `SELECT id FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, managementId],
    );
    if (management.rows[0] === undefined)
      throw new CatalogConflictError('The management is not available.', 'managementId');
    const unit = await client.query(
      `SELECT id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$2 AND current_status<>99 AND laboratory_id IS NOT NULL`,
      [siteId, equipmentUnitId],
    );
    if (unit.rows[0] === undefined)
      throw new CatalogConflictError('The equipment unit is not available.', 'equipmentUnitId');
    const plan = await client.query<PlanRow>(
      `SELECT id, current_phase, current_state, plan_status, is_draft FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
      [siteId, managementId, equipmentUnitId],
    );
    const row = plan.rows[0];
    if (row === undefined)
      throw new CatalogConflictError(
        'The unit is not included in this management plan.',
        'equipmentUnitId',
      );
    return row;
  }

  private async saveWithClient(
    client: IPgClient,
    siteId: string,
    userId: string,
    input: CreateVerificationInput,
    forcedPlanId?: number,
  ): Promise<VerificationDetail> {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(input.date))
      throw new CatalogConflictError('Verification date is invalid.', 'date');
    const observations = clean(input.observations, 2000, 'observations');
    const faults = [...(input.faults ?? [])]
      .map((fault) => clean(fault, 255, 'faults'))
      .filter((fault): fault is string => fault !== null);
    if (faults.length > 50)
      throw new CatalogConflictError('No more than 50 faults are allowed.', 'faults');
    const plan = await this.validatePlan(client, siteId, input.managementId, input.equipmentUnitId);
    if (forcedPlanId !== undefined && integer(plan.id) !== forcedPlanId)
      throw new CatalogConflictError(
        'The plan does not match the selected unit.',
        'managementPlanId',
      );
    const hasFailures = observations !== null || faults.length > 0;
    const requestedStatus = input.status ?? (hasFailures ? 2 : 1);
    const verificationState: VerificationStatus =
      requestedStatus === 0 ? 0 : hasFailures ? 2 : requestedStatus === 3 ? 3 : 1;
    const existing = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_verification WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND status<>99 LIMIT 1`,
      [siteId, input.managementId, input.equipmentUnitId],
    );
    let verificationId: number;
    if (existing.rows[0] === undefined) {
      const inserted = await client.query<{ id: number | string }>(
        `INSERT INTO public.lu_verification (site_id,equipment_unit_id,management_id,verification_date,observations,physical_condition,observed_equipment_status,status,created_by_id) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9) RETURNING id`,
        [
          siteId,
          input.equipmentUnitId,
          input.managementId,
          input.date,
          observations,
          input.physicalCondition,
          input.observedEquipmentStatus ?? null,
          verificationState,
          userId,
        ],
      );
      if (inserted.rows[0] === undefined)
        throw new CatalogMissingError('Verification was not created.');
      verificationId = integer(inserted.rows[0].id);
    } else {
      verificationId = integer(existing.rows[0].id);
      await client.query(
        `UPDATE public.lu_verification SET verification_date=$3, observations=$4, physical_condition=$5, observed_equipment_status=$6, status=$7, updated_by_id=$8, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [
          siteId,
          verificationId,
          input.date,
          observations,
          input.physicalCondition,
          input.observedEquipmentStatus ?? null,
          verificationState,
          userId,
        ],
      );
      await client.query(
        `DELETE FROM public.lu_verification_check_result WHERE site_id=$1 AND verification_id=$2`,
        [siteId, verificationId],
      );
      await client.query(
        `UPDATE public.lu_verification_fault SET is_deleted=true, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND verification_id=$2 AND is_deleted=false`,
        [siteId, verificationId, userId],
      );
    }
    for (const result of input.checkResults ?? [])
      await client.query(
        `INSERT INTO public.lu_verification_check_result (site_id,verification_id,check_item_id,result) VALUES ($1,$2,$3,$4)`,
        [siteId, verificationId, result.checkItemId, result.result],
      );
    for (const fault of faults)
      await client.query(
        `INSERT INTO public.lu_verification_fault (site_id,verification_id,description,is_deleted,created_by_id) VALUES ($1,$2,$3,false,$4)`,
        [siteId, verificationId, fault, userId],
      );
    if (verificationState === 0)
      await client.query(
        `UPDATE public.lu_management_plan SET verification_id=$3, is_draft=true, draft_phase=1, draft_saved_at=CURRENT_TIMESTAMP, draft_summary='L-6 draft saved', current_phase=1, current_state=1, plan_status=1, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, plan.id, verificationId, userId],
      );
    else if (hasFailures)
      await client.query(
        `UPDATE public.lu_management_plan SET verification_id=$3, is_draft=false, draft_phase=NULL, draft_saved_at=NULL, draft_summary=NULL, current_phase=2, current_state=3, plan_status=1, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, plan.id, verificationId, userId],
      );
    else
      await client.query(
        `UPDATE public.lu_management_plan SET verification_id=$3, is_draft=false, draft_phase=NULL, draft_saved_at=NULL, draft_summary=NULL, current_phase=1, current_state=2, plan_status=2, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, plan.id, verificationId, userId],
      );
    const detail = await this.find(client, siteId, verificationId);
    if (detail === null) throw new CatalogMissingError('Verification was not created.');
    return detail;
  }

  async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateVerificationInput,
  ): Promise<VerificationDetail> {
    try {
      return await pool.transaction((client) => this.saveWithClient(client, siteId, userId, input));
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'A verification already exists for this management and unit.',
          'equipmentUnitId',
        );
      throw error;
    }
  }

  async mass(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: MassVerificationInput,
  ): Promise<readonly VerificationDetail[]> {
    if (input.rows.length === 0 || input.rows.length > 100)
      throw new CatalogConflictError(
        'Mass verification must contain between 1 and 100 rows.',
        'rows',
      );
    return pool.transaction(async (client) => {
      const results: VerificationDetail[] = [];
      for (const row of input.rows)
        results.push(
          await this.saveWithClient(
            client,
            siteId,
            userId,
            {
              managementId: input.managementId,
              equipmentUnitId: row.equipmentUnitId,
              date: input.date,
              physicalCondition: row.physicalCondition,
              observations: row.observations,
              faults: row.faults,
              status: input.saveDraft ? 0 : undefined,
            },
            row.managementPlanId,
          ),
        );
      return results;
    });
  }
}
