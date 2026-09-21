import { Injectable } from '@nestjs/common';
import type {
  AcquisitionDetail,
  AcquisitionPage,
  AcquisitionQuery,
  AcquisitionRecord,
  CreateAcquisitionInput,
  MaintenanceCostInput,
  RequestPriority,
  RequestStatus,
  UpdateAcquisitionInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';
interface Row {
  id: number | string;
  management_id: number | string;
  management_code: string | null;
  equipment_unit_id: number | string;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  description: string;
  observations: string | null;
  priority: number;
  status: number;
  investment_code: string | null;
  cost_center: string | null;
  is_draft: boolean;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface CostRow {
  id: number | string;
  concept: string;
  description: string | null;
  quantity: number | string;
  unit_of_measure: string | null;
  unit_price: number | string;
  category: number;
  provider: string | null;
  cost_date: Date | string | null;
  invoice_number: string | null;
}
interface CountRow {
  total_count: number | string;
}
type QueryExecutor = Pick<IPgPool, 'query'>;
const SELECT = `SELECT r.id,r.management_id,g.code AS management_code,r.equipment_unit_id,u.inventory_number,e.name AS equipment_name,l.name AS laboratory_name,r.description,r.observations,r.priority,r.status,r.investment_code,r.cost_center,coalesce(p.is_draft,false) AS is_draft,r.created_at,r.updated_at FROM public.lu_request r LEFT JOIN public.lu_management g ON g.site_id=r.site_id AND g.id=r.management_id LEFT JOIN public.lu_equipment_unit u ON u.site_id=r.site_id AND u.id=r.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id LEFT JOIN public.lu_management_plan p ON p.site_id=r.site_id AND p.management_id=r.management_id AND p.equipment_unit_id=r.equipment_unit_id AND p.acquisition_request_id=r.id AND p.plan_status<>3`;
function integer(v: number | string | null | undefined): number {
  const p = typeof v === 'number' ? v : Number(v);
  return Number.isSafeInteger(p) ? p : 0;
}
function decimal(v: number | string | null | undefined): number {
  if (v === null || v === undefined) return 0;
  const p = typeof v === 'number' ? v : Number(v);
  return Number.isFinite(p) ? p : 0;
}
function iso(v: Date | string): string {
  return v instanceof Date ? v.toISOString() : new Date(v).toISOString();
}
function dateOnly(v: Date | string | null): string | null {
  if (v === null) return null;
  return typeof v === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(v) ? v : iso(v).slice(0, 10);
}
function clean(
  v: string | null | undefined,
  max: number,
  field: string,
  required = false,
): string | null {
  const r = v === null || v === undefined ? '' : v.trim().replace(/\s+/g, ' ');
  if (required && r.length === 0) throw new CatalogConflictError(`${field} is required.`, field);
  if (r.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return r === '' ? null : r;
}
function map(row: Row): AcquisitionRecord {
  const status = [0, 1, 2, 3, 4, 5, 99].includes(row.status) ? (row.status as RequestStatus) : 99;
  return {
    id: integer(row.id),
    managementId: integer(row.management_id),
    managementCode: row.management_code,
    equipmentUnitId: integer(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    description: row.description,
    observations: row.observations,
    priority: [0, 1, 2, 3].includes(row.priority) ? (row.priority as RequestPriority) : 1,
    status,
    investmentCode: row.investment_code,
    costCenter: row.cost_center,
    isDraft: row.is_draft,
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
@Injectable()
export class AcquisitionRepository {
  async list(pool: IPgPool, siteId: string, query: AcquisitionQuery): Promise<AcquisitionPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['r.site_id=$1', 'r.type=2', 'r.status<>99'];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`r.management_id=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`r.status=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(r.description ILIKE $${params.length} OR r.investment_code ILIKE $${params.length} OR u.inventory_number ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_request r LEFT JOIN public.lu_equipment_unit u ON u.site_id=r.site_id AND u.id=r.equipment_unit_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<Row>(
      `${SELECT} WHERE ${where} ORDER BY r.created_at DESC,r.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
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
  async find(pool: QueryExecutor, siteId: string, id: number): Promise<AcquisitionDetail | null> {
    const rows = await pool.query<Row>(`${SELECT} WHERE r.site_id=$1 AND r.id=$2 AND r.type=2`, [
      siteId,
      id,
    ]);
    const row = rows.rows[0];
    if (row === undefined) return null;
    const costs = await pool.query<CostRow>(
      `SELECT id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number FROM public.lu_request_cost WHERE site_id=$1 AND request_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, id],
    );
    return {
      ...map(row),
      costs: costs.rows.map((cost) => ({
        id: integer(cost.id),
        concept: cost.concept,
        description: cost.description,
        quantity: decimal(cost.quantity),
        unitOfMeasure: cost.unit_of_measure,
        unitPrice: decimal(cost.unit_price),
        category: cost.category,
        provider: cost.provider,
        costDate: dateOnly(cost.cost_date),
        invoiceNumber: cost.invoice_number,
      })),
    };
  }
  private async target(client: IPgClient, siteId: string, managementId: number, unitId: number) {
    const mgmt = await client.query(
      `SELECT id FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, managementId],
    );
    if (mgmt.rows[0] === undefined)
      throw new CatalogConflictError('The management is not available.', 'managementId');
    const unit = await client.query(
      `SELECT id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$2 AND current_status<>99 AND laboratory_id IS NOT NULL`,
      [siteId, unitId],
    );
    if (unit.rows[0] === undefined)
      throw new CatalogConflictError('The equipment unit is not available.', 'equipmentUnitId');
    const plan = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND current_phase=6 AND plan_status<>3`,
      [siteId, managementId, unitId],
    );
    if (plan.rows[0] === undefined)
      throw new CatalogConflictError('The unit is not ready for L-12.', 'equipmentUnitId');
    return integer(plan.rows[0].id);
  }
  private async costs(
    client: IPgClient,
    siteId: string,
    userId: string,
    requestId: number,
    costs: readonly MaintenanceCostInput[],
  ) {
    if (costs.length > 100)
      throw new CatalogConflictError('No more than 100 acquisition costs are allowed.', 'costs');
    const existing = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_request_cost WHERE site_id=$1 AND request_id=$2 AND is_deleted=false`,
      [siteId, requestId],
    );
    const ids = new Set(costs.filter((cost) => cost.id !== undefined).map((cost) => cost.id));
    for (const row of existing.rows)
      if (!ids.has(integer(row.id)))
        await client.query(
          `UPDATE public.lu_request_cost SET is_deleted=true,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND request_id=$2 AND id=$4`,
          [siteId, requestId, userId, row.id],
        );
    for (const cost of costs) {
      const concept = clean(cost.concept, 200, 'concept', true)!;
      if (
        !Number.isFinite(cost.quantity) ||
        cost.quantity <= 0 ||
        !Number.isFinite(cost.unitPrice) ||
        cost.unitPrice < 0
      )
        throw new CatalogConflictError('Cost values are invalid.', 'costs');
      const values = [
        siteId,
        requestId,
        userId,
        concept,
        clean(cost.description, 500, 'description'),
        cost.quantity,
        clean(cost.unitOfMeasure, 50, 'unitOfMeasure'),
        cost.unitPrice,
        cost.category ?? 1,
        clean(cost.provider, 200, 'provider'),
        cost.costDate ?? null,
        clean(cost.invoiceNumber, 100, 'invoiceNumber'),
      ];
      if (cost.id !== undefined)
        await client.query(
          `UPDATE public.lu_request_cost SET concept=$4,description=$5,quantity=$6,unit_of_measure=$7,unit_price=$8,category=$9,provider=$10,cost_date=$11,invoice_number=$12,is_deleted=false,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND request_id=$2 AND id=$13`,
          [...values, cost.id],
        );
      else
        await client.query(
          `INSERT INTO public.lu_request_cost(site_id,request_id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number,created_by_id) VALUES($1,$2,$4,$5,$6,$7,$8,$9,$10,$11,$12,$3)`,
          values,
        );
    }
  }
  private async saveRecord(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateAcquisitionInput | UpdateAcquisitionInput,
    id: number | null,
    complete: boolean,
  ): Promise<AcquisitionDetail> {
    return pool.transaction(async (client) => {
      const planId = await this.target(client, siteId, input.managementId, input.equipmentUnitId);
      const description = clean(input.description, 1000, 'description', true)!;
      const observations = clean(input.observations, 500, 'observations');
      const investment = clean(input.investmentCode, 50, 'investmentCode', true)!;
      const costCenter = clean(input.costCenter, 100, 'costCenter', true)!;
      const priority = input.priority ?? 1;
      const status = 'status' in input ? input.status : 0;
      const draft = input.isDraft === true && !complete;
      let requestId = id;
      if (requestId === null) {
        const inserted = await client.query<{ id: number | string }>(
          `INSERT INTO public.lu_request(site_id,laboratory_id,location_resolution_status,equipment_id,equipment_unit_id,management_id,description,priority,observations,request_date,status,type,investment_code,cost_center,created_by_id) SELECT $1,u.laboratory_id,1,u.equipment_id,$2,$3,$4,$5,$6,CURRENT_DATE,$7,2,$8,$9,$10 FROM public.lu_equipment_unit u WHERE u.site_id=$1 AND u.id=$2 RETURNING id`,
          [
            siteId,
            input.equipmentUnitId,
            input.managementId,
            description,
            priority,
            observations,
            draft ? 0 : status,
            investment,
            costCenter,
            userId,
          ],
        );
        if (inserted.rows[0] === undefined)
          throw new CatalogMissingError('Acquisition was not created.');
        requestId = integer(inserted.rows[0].id);
      } else {
        const exists = await client.query(
          `SELECT id FROM public.lu_request WHERE site_id=$1 AND id=$2 AND type=2 AND status<>99`,
          [siteId, id],
        );
        if (exists.rows[0] === undefined)
          throw new CatalogMissingError('Acquisition was not found.');
        await client.query(
          `UPDATE public.lu_request SET description=$3,priority=$4,observations=$5,status=$6,investment_code=$7,cost_center=$8,updated_by_id=$9,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [
            siteId,
            id,
            description,
            priority,
            observations,
            draft ? 0 : status,
            investment,
            costCenter,
            userId,
          ],
        );
      }
      await this.costs(client, siteId, userId, requestId!, input.costs ?? []);
      if (draft)
        await client.query(
          `UPDATE public.lu_management_plan SET acquisition_request_id=$3,is_draft=true,draft_phase=6,draft_saved_at=CURRENT_TIMESTAMP,draft_summary='L-12 draft saved',current_phase=6,current_state=8,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, planId, requestId, userId],
        );
      else if (complete)
        await client.query(
          `UPDATE public.lu_management_plan SET acquisition_request_id=$3,is_draft=false,draft_phase=NULL,draft_saved_at=NULL,draft_summary=NULL,current_phase=6,current_state=9,plan_status=2,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, planId, requestId, userId],
        );
      else
        await client.query(
          `UPDATE public.lu_management_plan SET acquisition_request_id=$3,current_phase=6,current_state=8,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, planId, requestId, userId],
        );
      const detail = await this.find(client, siteId, requestId!);
      if (detail === null) throw new CatalogMissingError('Acquisition was not created.');
      return detail;
    });
  }
  async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateAcquisitionInput,
  ): Promise<AcquisitionDetail> {
    return this.saveRecord(pool, siteId, userId, input, null, false);
  }
  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateAcquisitionInput,
  ): Promise<AcquisitionDetail> {
    return this.saveRecord(pool, siteId, userId, input, id, false);
  }
  async complete(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
  ): Promise<AcquisitionDetail> {
    const detail = await this.find(pool, siteId, id);
    if (detail === null) throw new CatalogMissingError('Acquisition was not found.');
    return this.saveRecord(
      pool,
      siteId,
      userId,
      {
        managementId: detail.managementId,
        equipmentUnitId: detail.equipmentUnitId,
        description: detail.description,
        observations: detail.observations,
        priority: detail.priority,
        investmentCode: detail.investmentCode ?? '',
        costCenter: detail.costCenter ?? '',
        costs: detail.costs,
      },
      id,
      true,
    );
  }
  async cancel(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_request SET status=99,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND type=2 AND status<>99`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Acquisition was not found.');
  }
}
