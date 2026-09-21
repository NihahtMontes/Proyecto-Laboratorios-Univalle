import { Injectable } from '@nestjs/common';
import type {
  CreateRequestInput,
  RequestLocationResolutionStatus,
  RequestDetail,
  RequestPage,
  RequestPriority,
  RequestQuery,
  RequestRecord,
  RequestStatus,
  RequestType,
  UpdateRequestInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface RequestRow {
  id: number | string;
  laboratory_id: number | string | null;
  laboratory_name: string | null;
  location_resolution_status: number;
  equipment_id: number | string | null;
  equipment_name: string | null;
  equipment_unit_id: number | string | null;
  inventory_number: string | null;
  management_id: number | string;
  management_code: string | null;
  description: string;
  priority: number;
  observations: string | null;
  suggestion: string | null;
  request_date: Date | string | null;
  estimated_repair_time: string | null;
  status: number;
  approved_by_id: string | null;
  approval_date: Date | string | null;
  rejection_reason: string | null;
  type: number;
  investment_code: string | null;
  cost_center: string | null;
  link_count: number | string;
  is_draft: boolean;
  created_at: Date | string;
  updated_at: Date | string | null;
}

interface CountRow {
  total_count: number | string;
}

interface ExistingRow {
  id: number | string;
  management_type: number;
  plan_id: number | string | null;
  plan_phase: number | null;
  plan_state: number | null;
  plan_status: number | null;
}

type QueryExecutor = Pick<IPgPool, 'query'>;

const SELECT = `
  SELECT r.id, r.laboratory_id, l.name AS laboratory_name,
         r.location_resolution_status, r.equipment_id, e.name AS equipment_name,
         r.equipment_unit_id, u.inventory_number, r.management_id, m.code AS management_code,
         r.description, r.priority, r.observations, r.suggestion, r.request_date,
         r.estimated_repair_time, r.status, r.approved_by_id, r.approval_date,
         r.rejection_reason, r.type, r.investment_code, r.cost_center,
         (SELECT count(*)::int FROM public.lu_request_equipment_unit ru
           WHERE ru.site_id=r.site_id AND ru.request_id=r.id AND ru.is_active=true) AS link_count,
         coalesce(p.is_draft, false) AS is_draft, r.created_at, r.updated_at
    FROM public.lu_request r
    LEFT JOIN public.lu_management m
      ON m.site_id=r.site_id AND m.id=r.management_id
    LEFT JOIN public.lu_equipment_unit u
      ON u.site_id=r.site_id AND u.id=r.equipment_unit_id
    LEFT JOIN public.lu_equipment e
      ON e.site_id=r.site_id AND e.id=r.equipment_id
    LEFT JOIN public.lu_laboratory l
      ON l.site_id=r.site_id AND l.id=r.laboratory_id
    LEFT JOIN public.lu_management_plan p
      ON p.site_id=r.site_id AND p.management_id=r.management_id
     AND p.equipment_unit_id=r.equipment_unit_id AND p.plan_status<>3`;

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
function requestType(value: number): RequestType {
  return [1, 2, 3].includes(value) ? (value as RequestType) : 1;
}
function priority(value: number): RequestPriority {
  return [0, 1, 2, 3].includes(value) ? (value as RequestPriority) : 1;
}
function status(value: number): RequestStatus {
  return [0, 1, 2, 3, 4, 5, 99].includes(value) ? (value as RequestStatus) : 99;
}
function locationStatus(value: number): RequestLocationResolutionStatus {
  return value === 0 ? 0 : 1;
}
function map(row: RequestRow): RequestRecord {
  return {
    id: integer(row.id),
    laboratoryId: nullableInteger(row.laboratory_id),
    laboratoryName: row.laboratory_name,
    locationResolutionStatus: locationStatus(row.location_resolution_status),
    equipmentId: nullableInteger(row.equipment_id),
    equipmentName: row.equipment_name,
    equipmentUnitId: nullableInteger(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    managementId: integer(row.management_id),
    managementCode: row.management_code,
    description: row.description,
    priority: priority(row.priority),
    observations: row.observations,
    suggestion: row.suggestion,
    requestDate: dateOnly(row.request_date),
    estimatedRepairTime: row.estimated_repair_time,
    status: status(row.status),
    approvedById: row.approved_by_id,
    approvalDate: row.approval_date === null ? null : iso(row.approval_date),
    rejectionReason: row.rejection_reason,
    type: requestType(row.type),
    investmentCode: row.investment_code,
    costCenter: row.cost_center,
    linkCount: integer(row.link_count),
    isDraft: row.is_draft,
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
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
function date(value: string | null | undefined, field: string): string | null {
  if (value === undefined || value === null || value === '') return null;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value))
    throw new CatalogConflictError(`${field} is invalid.`, field);
  return value;
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

@Injectable()
export class RequestRepository {
  async list(pool: IPgPool, siteId: string, query: RequestQuery): Promise<RequestPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['r.site_id=$1', 'r.status<>99'];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`r.management_id=$${params.length}`);
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`r.laboratory_id=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`r.status=$${params.length}`);
    }
    if (query.priorityFilter !== undefined) {
      params.push(query.priorityFilter);
      filters.push(`r.priority=$${params.length}`);
    }
    if (query.type !== undefined) {
      params.push(query.type);
      filters.push(`r.type=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(r.description ILIKE $${params.length} OR e.name ILIKE $${params.length} OR u.inventory_number ILIKE $${params.length} OR m.code ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_request r LEFT JOIN public.lu_management m ON m.site_id=r.site_id AND m.id=r.management_id LEFT JOIN public.lu_equipment_unit u ON u.site_id=r.site_id AND u.id=r.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=r.site_id AND e.id=r.equipment_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<RequestRow>(
      `${SELECT} WHERE ${where} ORDER BY r.priority DESC, r.request_date DESC NULLS LAST, r.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
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

  async find(pool: QueryExecutor, siteId: string, id: number): Promise<RequestDetail | null> {
    const rows = await pool.query<RequestRow>(`${SELECT} WHERE r.site_id=$1 AND r.id=$2`, [
      siteId,
      id,
    ]);
    const row = rows.rows[0];
    if (row === undefined) return null;
    const links = await pool.query<{ equipment_unit_id: number | string }>(
      `SELECT equipment_unit_id FROM public.lu_request_equipment_unit WHERE site_id=$1 AND request_id=$2 AND is_active=true ORDER BY id`,
      [siteId, id],
    );
    return {
      ...map(row),
      linkedEquipmentUnitIds: links.rows.map((item) => integer(item.equipment_unit_id)),
    };
  }

  private async validateTarget(
    client: IPgClient,
    siteId: string,
    managementId: number,
    equipmentUnitId: number,
  ): Promise<ExistingRow> {
    const management = await client.query<{ id: number | string; type: number }>(
      `SELECT id, type FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, managementId],
    );
    const managementRow = management.rows[0];
    if (managementRow === undefined)
      throw new CatalogConflictError('The management is not available.', 'managementId');
    const unit = await client.query<{ id: number | string; equipment_id: number | string }>(
      `SELECT id, equipment_id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$2 AND current_status<>99 AND laboratory_id IS NOT NULL`,
      [siteId, equipmentUnitId],
    );
    const unitRow = unit.rows[0];
    if (unitRow === undefined)
      throw new CatalogConflictError(
        'The equipment unit must be active and assigned to a laboratory.',
        'equipmentUnitId',
      );
    const plan = await client.query<{
      id: number | string;
      current_phase: number;
      current_state: number;
      plan_status: number;
    }>(
      `SELECT id, current_phase, current_state, plan_status FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
      [siteId, managementId, equipmentUnitId],
    );
    const planRow = plan.rows[0];
    if (planRow === undefined && managementRow.type !== 1)
      throw new CatalogConflictError(
        'The unit is not included in this management plan.',
        'equipmentUnitId',
      );
    return {
      id: 0,
      management_type: managementRow.type,
      plan_id: planRow === undefined ? null : planRow.id,
      plan_phase: planRow?.current_phase ?? null,
      plan_state: planRow?.current_state ?? null,
      plan_status: planRow?.plan_status ?? null,
    };
  }

  private async findExisting(
    client: IPgClient,
    siteId: string,
    managementId: number,
    equipmentUnitId: number,
    excludeId: number | null,
  ): Promise<number | null> {
    const params: unknown[] = [siteId, managementId, equipmentUnitId];
    let exclusion = '';
    if (excludeId !== null) {
      params.push(excludeId);
      exclusion = ` AND id<>$${params.length}`;
    }
    const result = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_request WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND type=1 AND status<>99${exclusion} LIMIT 1`,
      params,
    );
    return result.rows[0] === undefined ? null : integer(result.rows[0].id);
  }

  private async saveWithClient(
    client: IPgClient,
    siteId: string,
    userId: string,
    input: CreateRequestInput | UpdateRequestInput,
    id: number | null,
    forceComplete: boolean,
  ): Promise<RequestDetail> {
    const description = clean(input.description, 1000, 'description', true)!;
    const observations = clean(input.observations, 500, 'observations');
    const suggestion = clean(input.suggestion, 2000, 'suggestion');
    const estimatedRepairTime = clean(input.estimatedRepairTime, 100, 'estimatedRepairTime');
    const investmentCode = clean(input.investmentCode, 50, 'investmentCode');
    const costCenter = clean(input.costCenter, 100, 'costCenter');
    const requestDate = date(input.requestDate, 'requestDate');
    const type = input.type ?? 1;
    const priorityValue = input.priority ?? 1;
    if (![1, 2, 3].includes(type)) throw new CatalogConflictError('Type is invalid.', 'type');
    if (![0, 1, 2, 3].includes(priorityValue))
      throw new CatalogConflictError('Priority is invalid.', 'priority');
    const target = await this.validateTarget(
      client,
      siteId,
      input.managementId,
      input.equipmentUnitId,
    );
    const duplicate = await this.findExisting(
      client,
      siteId,
      input.managementId,
      input.equipmentUnitId,
      id,
    );
    if (duplicate !== null)
      throw new CatalogConflictError(
        `An active technical request already exists for this management and unit (#${duplicate}).`,
        'equipmentUnitId',
      );
    const isDraft = input.isDraft === true && !forceComplete;
    let requestId = id;
    if (requestId === null) {
      const inserted = await client.query<{ id: number | string }>(
        `INSERT INTO public.lu_request (site_id,laboratory_id,location_resolution_status,equipment_id,equipment_unit_id,management_id,description,priority,observations,suggestion,request_date,estimated_repair_time,status,type,investment_code,cost_center,created_by_id) SELECT $1,u.laboratory_id,1,u.equipment_id,$2,$3,$4,$5,$6,$7,COALESCE($8::date,CURRENT_DATE),$9,0,$10,$11,$12,$13 FROM public.lu_equipment_unit u WHERE u.site_id=$1 AND u.id=$2 RETURNING id`,
        [
          siteId,
          input.equipmentUnitId,
          input.managementId,
          description,
          priorityValue,
          observations,
          suggestion,
          requestDate,
          estimatedRepairTime,
          type,
          investmentCode,
          costCenter,
          userId,
        ],
      );
      if (inserted.rows[0] === undefined) throw new CatalogMissingError('Request was not created.');
      requestId = integer(inserted.rows[0].id);
    } else {
      const current = await client.query<{ id: number | string }>(
        `SELECT id FROM public.lu_request WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      if (current.rows[0] === undefined) throw new CatalogMissingError('Request was not found.');
      const updateStatus = 'status' in input && !isDraft ? input.status : 0;
      if (![0, 1, 2, 3, 4, 5, 99].includes(updateStatus))
        throw new CatalogConflictError('Status is invalid.', 'status');
      await client.query(
        `UPDATE public.lu_request SET laboratory_id=(SELECT laboratory_id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$3), location_resolution_status=1, equipment_id=(SELECT equipment_id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$3), equipment_unit_id=$3, management_id=$4, description=$5, priority=$6, observations=$7, suggestion=$8, request_date=COALESCE($9::date,request_date,CURRENT_DATE), estimated_repair_time=$10, status=$11, type=$12, investment_code=$13, cost_center=$14, rejection_reason=CASE WHEN $11=4 THEN $15 ELSE NULL END, approved_by_id=CASE WHEN $11 IN (3,4) THEN COALESCE(approved_by_id,$2) ELSE approved_by_id END, approval_date=CASE WHEN $11 IN (3,4) THEN COALESCE(approval_date,CURRENT_TIMESTAMP) ELSE approval_date END, updated_by_id=$2, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$16`,
        [
          siteId,
          userId,
          input.equipmentUnitId,
          input.managementId,
          description,
          priorityValue,
          observations,
          suggestion,
          requestDate,
          estimatedRepairTime,
          updateStatus,
          type,
          investmentCode,
          costCenter,
          'rejectionReason' in input ? (input.rejectionReason ?? null) : null,
          id,
        ],
      );
    }
    const effectiveId = requestId!;
    const existingLink = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_request_equipment_unit WHERE site_id=$1 AND request_id=$2 AND equipment_unit_id=$3`,
      [siteId, effectiveId, input.equipmentUnitId],
    );
    if (existingLink.rows[0] === undefined)
      await client.query(
        `INSERT INTO public.lu_request_equipment_unit (site_id,request_id,equipment_unit_id,is_legacy_primary,is_active,created_by_id) VALUES ($1,$2,$3,true,true,$4)`,
        [siteId, effectiveId, input.equipmentUnitId, userId],
      );
    else
      await client.query(
        `UPDATE public.lu_request_equipment_unit SET is_active=true, deactivated_at=NULL, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND request_id=$2 AND equipment_unit_id=$3`,
        [siteId, effectiveId, input.equipmentUnitId, userId],
      );
    if (target.plan_id === null) {
      await client.query(
        `INSERT INTO public.lu_management_plan (site_id,management_id,equipment_unit_id,current_phase,current_state,plan_status,is_draft,draft_phase,draft_saved_at,draft_summary,request_id,created_by_id) VALUES ($1,$2,$3,CASE WHEN $4 THEN 2 ELSE 3 END,CASE WHEN $4 THEN 3 ELSE 4 END,1,$4,CASE WHEN $4 THEN 2 ELSE NULL END,CASE WHEN $4 THEN CURRENT_TIMESTAMP ELSE NULL END,CASE WHEN $4 THEN 'L-7 draft saved' ELSE NULL END,$5,$6)`,
        [siteId, input.managementId, input.equipmentUnitId, isDraft, effectiveId, userId],
      );
    } else {
      await client.query(
        `UPDATE public.lu_management_plan SET request_id=$3, is_draft=$4, draft_phase=CASE WHEN $4 THEN 2 ELSE NULL END, draft_saved_at=CASE WHEN $4 THEN CURRENT_TIMESTAMP ELSE NULL END, draft_summary=CASE WHEN $4 THEN 'L-7 draft saved' ELSE NULL END, current_phase=CASE WHEN $4 THEN 2 ELSE 3 END, current_state=CASE WHEN $4 THEN 3 ELSE 4 END, plan_status=1, updated_by_id=$5, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, target.plan_id, effectiveId, isDraft, userId],
      );
    }
    const detail = await this.find(client, siteId, effectiveId);
    if (detail === null) throw new CatalogMissingError('Request was not created.');
    return detail;
  }

  async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateRequestInput,
  ): Promise<RequestDetail> {
    try {
      return await pool.transaction((client) =>
        this.saveWithClient(client, siteId, userId, input, null, false),
      );
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active technical request already exists for this management and unit.',
          'equipmentUnitId',
        );
      throw error;
    }
  }

  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateRequestInput,
  ): Promise<RequestDetail> {
    try {
      return await pool.transaction((client) =>
        this.saveWithClient(client, siteId, userId, input, id, false),
      );
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active technical request already exists for this management and unit.',
          'equipmentUnitId',
        );
      throw error;
    }
  }

  async complete(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
  ): Promise<RequestDetail> {
    return pool.transaction(async (client) => {
      const current = await client.query<{
        management_id: number | string;
        equipment_unit_id: number | string;
        description: string;
        priority: number;
      }>(
        `SELECT management_id,equipment_unit_id,description,priority FROM public.lu_request WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      const row = current.rows[0];
      if (row === undefined) throw new CatalogMissingError('Request was not found.');
      await client.query(
        `UPDATE public.lu_request SET status=0, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, id, userId],
      );
      const plan = await client.query<{ id: number | string }>(
        `SELECT id FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
        [siteId, row.management_id, row.equipment_unit_id],
      );
      const planRow = plan.rows[0];
      if (planRow === undefined)
        throw new CatalogConflictError('The request has no active management plan.', 'id');
      await client.query(
        `UPDATE public.lu_management_plan SET request_id=$3, is_draft=false, draft_phase=NULL, draft_saved_at=NULL, draft_summary=NULL, current_phase=3, current_state=4, plan_status=1, updated_by_id=$4, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, planRow.id, id, userId],
      );
      const detail = await this.find(client, siteId, id);
      if (detail === null) throw new CatalogMissingError('Request was not found.');
      return detail;
    });
  }

  async cancel(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_request SET status=99, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Request was not found.');
  }
}
