import { Injectable } from '@nestjs/common';
import type {
  CreateMaintenanceInput,
  MaintenanceCostInput,
  MaintenanceDetail,
  MaintenancePage,
  MaintenanceQuery,
  MaintenanceRecord,
  MaintenanceSatisfaction,
  MaintenanceStatus,
  MaintenanceTaskInput,
  MaintenanceType,
  UpdateMaintenanceInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface MaintenanceRow {
  id: number | string;
  equipment_unit_id: number | string;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  management_id: number | string;
  management_code: string | null;
  request_id: number | string | null;
  maintenance_type: number;
  service_type: number;
  institutional_code: string | null;
  technician_id: number | string | null;
  scheduled_date: Date | string | null;
  start_date: Date | string | null;
  end_date: Date | string | null;
  description: string | null;
  status: number;
  completion_percentage: number;
  estimated_cost: number | string | null;
  actual_cost: number | string | null;
  recommendations: string | null;
  suggested_next_maintenance_date: Date | string | null;
  satisfaction_level: number | null;
  observations: string | null;
  calculated_total: number | string;
  is_draft: boolean;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface TaskRow {
  id: number | string;
  description: string;
  is_completed: boolean;
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
interface TargetRow {
  plan_id: number | string;
  management_id: number | string;
  equipment_unit_id: number | string;
  request_id: number | string | null;
}
type QueryExecutor = Pick<IPgPool, 'query'>;

const SELECT = `
  SELECT m.id, m.equipment_unit_id, u.inventory_number, e.name AS equipment_name,
         l.name AS laboratory_name, m.management_id, g.code AS management_code,
         m.request_id, m.maintenance_type, m.service_type, m.institutional_code,
         m.technician_id, m.scheduled_date, m.start_date, m.end_date, m.description,
         m.status, m.completion_percentage, m.estimated_cost, m.actual_cost,
         m.recommendations, m.suggested_next_maintenance_date, m.satisfaction_level,
         m.observations,
         coalesce((SELECT sum(c.quantity*c.unit_price) FROM public.lu_maintenance_cost c
                   WHERE c.site_id=m.site_id AND c.maintenance_id=m.id AND c.is_deleted=false),0) AS calculated_total,
         coalesce(p.is_draft,false) AS is_draft, m.created_at, m.updated_at
    FROM public.lu_maintenance m
    LEFT JOIN public.lu_equipment_unit u ON u.site_id=m.site_id AND u.id=m.equipment_unit_id
    LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id
    LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id
    LEFT JOIN public.lu_management g ON g.site_id=m.site_id AND g.id=m.management_id
    LEFT JOIN public.lu_management_plan p ON p.site_id=m.site_id AND p.management_id=m.management_id
      AND p.equipment_unit_id=m.equipment_unit_id AND p.plan_status<>3`;

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function decimal(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined) return null;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : null;
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
function maintenanceType(value: number): MaintenanceType {
  return [1, 2, 3, 4, 5, 99].includes(value) ? (value as MaintenanceType) : 99;
}
function maintenanceStatus(value: number): MaintenanceStatus {
  return [0, 1, 2, 3, 99].includes(value) ? (value as MaintenanceStatus) : 99;
}
function satisfaction(value: number | null): MaintenanceSatisfaction | null {
  return value !== null && [1, 2, 3, 4, 5].includes(value)
    ? (value as MaintenanceSatisfaction)
    : null;
}
function map(row: MaintenanceRow): MaintenanceRecord {
  return {
    id: integer(row.id),
    equipmentUnitId: integer(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    managementId: integer(row.management_id),
    managementCode: row.management_code,
    requestId: row.request_id === null ? null : integer(row.request_id),
    maintenanceType: maintenanceType(row.maintenance_type),
    serviceType: row.service_type === 1 ? 1 : 0,
    institutionalCode: row.institutional_code,
    technicianId: row.technician_id === null ? null : integer(row.technician_id),
    scheduledDate: dateOnly(row.scheduled_date),
    startDate: row.start_date === null ? null : iso(row.start_date),
    endDate: row.end_date === null ? null : iso(row.end_date),
    description: row.description,
    status: maintenanceStatus(row.status),
    completionPercentage: Math.min(100, Math.max(0, integer(row.completion_percentage))),
    estimatedCost: decimal(row.estimated_cost),
    actualCost: decimal(row.actual_cost),
    recommendations: row.recommendations,
    suggestedNextMaintenanceDate: dateOnly(row.suggested_next_maintenance_date),
    satisfactionLevel: satisfaction(row.satisfaction_level),
    observations: row.observations,
    calculatedTotal: decimal(row.calculated_total) ?? 0,
    isDraft: row.is_draft,
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
function clean(value: string | null | undefined, max: number, field: string): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return result === '' ? null : result;
}
function date(value: string | null | undefined, field: string): string | null {
  if (value === null || value === undefined || value === '') return null;
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
export class MaintenanceRepository {
  async list(pool: IPgPool, siteId: string, query: MaintenanceQuery): Promise<MaintenancePage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['m.site_id=$1', 'm.status<>99'];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`m.management_id=$${params.length}`);
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`m.status=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(e.name ILIKE $${params.length} OR u.inventory_number ILIKE $${params.length} OR g.code ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_maintenance m LEFT JOIN public.lu_equipment_unit u ON u.site_id=m.site_id AND u.id=m.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id LEFT JOIN public.lu_management g ON g.site_id=m.site_id AND g.id=m.management_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<MaintenanceRow>(
      `${SELECT} WHERE ${where} ORDER BY m.scheduled_date NULLS LAST, m.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
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

  async find(pool: QueryExecutor, siteId: string, id: number): Promise<MaintenanceDetail | null> {
    const rows = await pool.query<MaintenanceRow>(`${SELECT} WHERE m.site_id=$1 AND m.id=$2`, [
      siteId,
      id,
    ]);
    const row = rows.rows[0];
    if (row === undefined) return null;
    const tasks = await pool.query<TaskRow>(
      `SELECT id,description,is_completed FROM public.lu_maintenance_task WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, id],
    );
    const costs = await pool.query<CostRow>(
      `SELECT id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number FROM public.lu_maintenance_cost WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, id],
    );
    return {
      ...map(row),
      tasks: tasks.rows.map((task) => ({
        id: integer(task.id),
        description: task.description,
        isCompleted: task.is_completed,
      })),
      costs: costs.rows.map((cost) => ({
        id: integer(cost.id),
        concept: cost.concept,
        description: cost.description,
        quantity: decimal(cost.quantity) ?? 0,
        unitOfMeasure: cost.unit_of_measure,
        unitPrice: decimal(cost.unit_price) ?? 0,
        category: cost.category,
        provider: cost.provider,
        costDate: dateOnly(cost.cost_date),
        invoiceNumber: cost.invoice_number,
      })),
    };
  }

  private async target(
    client: IPgClient,
    siteId: string,
    managementId: number,
    equipmentUnitId: number,
    requestId: number | null,
  ): Promise<TargetRow> {
    const management = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, managementId],
    );
    if (management.rows[0] === undefined)
      throw new CatalogConflictError('The management is not available.', 'managementId');
    const unit = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$2 AND current_status<>99 AND laboratory_id IS NOT NULL`,
      [siteId, equipmentUnitId],
    );
    if (unit.rows[0] === undefined)
      throw new CatalogConflictError('The equipment unit is not available.', 'equipmentUnitId');
    const plan = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
      [siteId, managementId, equipmentUnitId],
    );
    const planRow = plan.rows[0];
    if (planRow === undefined)
      throw new CatalogConflictError('The unit has no active management plan.', 'equipmentUnitId');
    if (requestId !== null) {
      const request = await client.query<{ id: number | string }>(
        `SELECT id FROM public.lu_request WHERE site_id=$1 AND id=$2 AND management_id=$3 AND equipment_unit_id=$4 AND status<>99`,
        [siteId, requestId, managementId, equipmentUnitId],
      );
      if (request.rows[0] === undefined)
        throw new CatalogConflictError(
          'The request does not match the selected plan.',
          'requestId',
        );
    }
    return {
      plan_id: integer(planRow.id),
      management_id: managementId,
      equipment_unit_id: equipmentUnitId,
      request_id: requestId,
    };
  }

  private async saveChildren(
    client: IPgClient,
    siteId: string,
    userId: string,
    maintenanceId: number,
    tasks: readonly MaintenanceTaskInput[],
    costs: readonly MaintenanceCostInput[],
  ): Promise<number> {
    const normalizedTasks = tasks
      .map((task) => ({ ...task, description: clean(task.description, 255, 'tasks') }))
      .filter(
        (task): task is MaintenanceTaskInput & { description: string } => task.description !== null,
      );
    if (normalizedTasks.length > 100)
      throw new CatalogConflictError('No more than 100 maintenance tasks are allowed.', 'tasks');
    const normalizedCosts = costs.map((cost) => ({
      ...cost,
      concept: clean(cost.concept, 200, 'costs'),
    }));
    if (normalizedCosts.length > 100)
      throw new CatalogConflictError('No more than 100 maintenance costs are allowed.', 'costs');
    const existingTasks = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_maintenance_task WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false`,
      [siteId, maintenanceId],
    );
    const taskIds = new Set(
      normalizedTasks.filter((task) => task.id !== undefined).map((task) => task.id),
    );
    for (const task of existingTasks.rows)
      if (!taskIds.has(integer(task.id)))
        await client.query(
          `UPDATE public.lu_maintenance_task SET is_deleted=true, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$3`,
          [siteId, maintenanceId, task.id],
        );
    let completed = 0;
    for (const task of normalizedTasks) {
      if (task.isCompleted === true) completed += 1;
      if (task.id !== undefined) {
        await client.query(
          `UPDATE public.lu_maintenance_task SET description=$4,is_completed=$5,is_deleted=false,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$3`,
          [siteId, maintenanceId, task.id, task.description, task.isCompleted === true],
        );
      } else {
        await client.query(
          `INSERT INTO public.lu_maintenance_task(site_id,maintenance_id,description,is_completed) VALUES($1,$2,$3,$4)`,
          [siteId, maintenanceId, task.description, task.isCompleted === true],
        );
      }
    }
    const existingCosts = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_maintenance_cost WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false`,
      [siteId, maintenanceId],
    );
    const costIds = new Set(
      normalizedCosts.filter((cost) => cost.id !== undefined).map((cost) => cost.id),
    );
    for (const cost of existingCosts.rows)
      if (!costIds.has(integer(cost.id)))
        await client.query(
          `UPDATE public.lu_maintenance_cost SET is_deleted=true, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$4`,
          [siteId, maintenanceId, userId, cost.id],
        );
    for (const cost of normalizedCosts) {
      if (cost.concept === null) continue;
      const quantity = Number(cost.quantity);
      const unitPrice = Number(cost.unitPrice);
      if (
        !Number.isFinite(quantity) ||
        quantity <= 0 ||
        !Number.isFinite(unitPrice) ||
        unitPrice < 0
      )
        throw new CatalogConflictError('Cost values are invalid.', 'costs');
      const costDate = date(cost.costDate, 'costDate');
      const description = clean(cost.description, 500, 'costDescription');
      const unit = clean(cost.unitOfMeasure, 50, 'unitOfMeasure');
      const provider = clean(cost.provider, 200, 'provider');
      const invoice = clean(cost.invoiceNumber, 100, 'invoiceNumber');
      if (cost.id !== undefined)
        await client.query(
          `UPDATE public.lu_maintenance_cost SET concept=$4,description=$5,quantity=$6,unit_of_measure=$7,unit_price=$8,category=$9,provider=$10,cost_date=$11,invoice_number=$12,is_deleted=false,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$13`,
          [
            siteId,
            maintenanceId,
            userId,
            cost.concept,
            description,
            quantity,
            unit,
            unitPrice,
            cost.category ?? 1,
            provider,
            costDate,
            invoice,
            cost.id,
          ],
        );
      else
        await client.query(
          `INSERT INTO public.lu_maintenance_cost(site_id,maintenance_id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number,created_by_id) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)`,
          [
            siteId,
            maintenanceId,
            cost.concept,
            description,
            quantity,
            unit,
            unitPrice,
            cost.category ?? 1,
            provider,
            costDate,
            invoice,
            userId,
          ],
        );
    }
    return normalizedTasks.length === 0
      ? 0
      : Math.round((completed / normalizedTasks.length) * 100);
  }

  private async saveWithClient(
    client: IPgClient,
    siteId: string,
    userId: string,
    input: CreateMaintenanceInput | UpdateMaintenanceInput,
    id: number | null,
  ): Promise<MaintenanceDetail> {
    const requestId = input.requestId ?? null;
    const target = await this.target(
      client,
      siteId,
      input.managementId,
      input.equipmentUnitId,
      requestId,
    );
    const description = clean(input.description, 2000, 'description');
    const recommendations = clean(input.recommendations, 1000, 'recommendations');
    const observations = clean(input.observations, 1000, 'observations');
    const institutionalCode = clean(input.institutionalCode, 50, 'institutionalCode');
    const maintenanceType = input.maintenanceType ?? 99;
    const serviceType = input.serviceType ?? 0;
    const requestedStatus = input.status ?? 0;
    if (![1, 2, 3, 4, 5, 99].includes(maintenanceType))
      throw new CatalogConflictError('Maintenance type is invalid.', 'maintenanceType');
    if (![0, 1].includes(serviceType))
      throw new CatalogConflictError('Service type is invalid.', 'serviceType');
    if (![0, 1, 2, 3, 99].includes(requestedStatus))
      throw new CatalogConflictError('Status is invalid.', 'status');
    const scheduledDate = date(input.scheduledDate, 'scheduledDate');
    const startDate = input.startDate ?? null;
    const endDate = input.endDate ?? null;
    if (startDate !== null && Number.isNaN(Date.parse(startDate)))
      throw new CatalogConflictError('startDate is invalid.', 'startDate');
    if (endDate !== null && Number.isNaN(Date.parse(endDate)))
      throw new CatalogConflictError('endDate is invalid.', 'endDate');
    if (startDate !== null && endDate !== null && new Date(endDate) < new Date(startDate))
      throw new CatalogConflictError('End date cannot precede start date.', 'endDate');
    const isDraft = input.isDraft === true;
    let maintenanceId = id;
    if (maintenanceId === null) {
      const inserted = await client.query<{ id: number | string }>(
        `INSERT INTO public.lu_maintenance(site_id,equipment_unit_id,maintenance_type,management_id,service_type,institutional_code,technician_id,request_id,scheduled_date,start_date,end_date,description,status,estimated_cost,actual_cost,recommendations,suggested_next_maintenance_date,satisfaction_level,observations,created_by_id) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14,$15,$16,$17,$18,$19,$20) RETURNING id`,
        [
          siteId,
          input.equipmentUnitId,
          maintenanceType,
          input.managementId,
          serviceType,
          institutionalCode,
          input.technicianId ?? null,
          requestId,
          scheduledDate,
          startDate,
          endDate,
          description,
          isDraft ? 1 : requestedStatus,
          input.estimatedCost ?? null,
          input.actualCost ?? null,
          recommendations,
          date(input.suggestedNextMaintenanceDate, 'suggestedNextMaintenanceDate'),
          input.satisfactionLevel ?? null,
          observations,
          userId,
        ],
      );
      if (inserted.rows[0] === undefined)
        throw new CatalogMissingError('Maintenance was not created.');
      maintenanceId = integer(inserted.rows[0].id);
    } else {
      const exists = await client.query<{ id: number | string }>(
        `SELECT id FROM public.lu_maintenance WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      if (exists.rows[0] === undefined) throw new CatalogMissingError('Maintenance was not found.');
      await client.query(
        `UPDATE public.lu_maintenance SET equipment_unit_id=$3,maintenance_type=$4,management_id=$5,service_type=$6,institutional_code=$7,technician_id=$8,request_id=$9,scheduled_date=$10,start_date=$11,end_date=$12,description=$13,status=$14,estimated_cost=$15,actual_cost=$16,recommendations=$17,suggested_next_maintenance_date=$18,satisfaction_level=$19,observations=$20,updated_by_id=$21,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [
          siteId,
          id,
          input.equipmentUnitId,
          maintenanceType,
          input.managementId,
          serviceType,
          institutionalCode,
          input.technicianId ?? null,
          requestId,
          scheduledDate,
          startDate,
          endDate,
          description,
          isDraft ? 1 : requestedStatus,
          input.estimatedCost ?? null,
          input.actualCost ?? null,
          recommendations,
          date(input.suggestedNextMaintenanceDate, 'suggestedNextMaintenanceDate'),
          input.satisfactionLevel ?? null,
          observations,
          userId,
        ],
      );
    }
    const effectiveId = maintenanceId!;
    const tasks = input.tasks ?? [];
    const costs = input.costs ?? [];
    const completion = await this.saveChildren(client, siteId, userId, effectiveId, tasks, costs);
    await client.query(
      `UPDATE public.lu_maintenance SET completion_percentage=$3,step1_cleaning=$4,step2_calibration=$5,step3_testing=$6,step4_final_review=$7,actual_cost=COALESCE($8,(SELECT sum(quantity*unit_price) FROM public.lu_maintenance_cost WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false)),updated_by_id=$9,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
      [
        siteId,
        effectiveId,
        completion,
        tasks[0]?.isCompleted === true,
        tasks[1]?.isCompleted === true,
        tasks[2]?.isCompleted === true,
        tasks[3]?.isCompleted === true,
        input.actualCost ?? null,
        userId,
      ],
    );
    const planDraft = isDraft;
    const completed = requestedStatus === 2;
    await client.query(
      `UPDATE public.lu_management_plan SET maintenance_id=$3,is_draft=$4,draft_phase=CASE WHEN $4 THEN 3 ELSE NULL END,draft_saved_at=CASE WHEN $4 THEN CURRENT_TIMESTAMP ELSE NULL END,draft_summary=CASE WHEN $4 THEN 'L-8 draft saved' ELSE NULL END,current_phase=CASE WHEN $5 THEN 4 ELSE 3 END,current_state=CASE WHEN $5 THEN 6 ELSE 5 END,plan_status=1,updated_by_id=$6,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
      [siteId, target.plan_id, effectiveId, planDraft, completed, userId],
    );
    const detail = await this.find(client, siteId, effectiveId);
    if (detail === null) throw new CatalogMissingError('Maintenance was not created.');
    return detail;
  }

  async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateMaintenanceInput,
  ): Promise<MaintenanceDetail> {
    try {
      return await pool.transaction((client) =>
        this.saveWithClient(client, siteId, userId, input, null),
      );
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active maintenance already exists for this plan and unit.',
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
    input: UpdateMaintenanceInput,
  ): Promise<MaintenanceDetail> {
    try {
      if (input.status === 2 && (input.actualCost ?? 0) <= 0)
        throw new CatalogConflictError('Completed maintenance requires actual cost.', 'actualCost');
      if (input.status === 2 && input.satisfactionLevel === null)
        throw new CatalogConflictError(
          'Completed maintenance requires satisfaction.',
          'satisfactionLevel',
        );
      return await pool.transaction((client) =>
        this.saveWithClient(client, siteId, userId, input, id),
      );
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active maintenance already exists for this plan and unit.',
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
  ): Promise<MaintenanceDetail> {
    return pool.transaction(async (client) => {
      const current = await client.query<MaintenanceRow>(
        `${SELECT} WHERE m.site_id=$1 AND m.id=$2 AND m.status<>99`,
        [siteId, id],
      );
      const row = current.rows[0];
      if (row === undefined) throw new CatalogMissingError('Maintenance was not found.');
      if ((decimal(row.actual_cost) ?? 0) <= 0)
        throw new CatalogConflictError('Completed maintenance requires actual cost.', 'actualCost');
      if (row.satisfaction_level === null)
        throw new CatalogConflictError(
          'Completed maintenance requires satisfaction.',
          'satisfactionLevel',
        );
      await client.query(
        `UPDATE public.lu_maintenance SET status=2,end_date=COALESCE(end_date,CURRENT_TIMESTAMP),updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, id, userId],
      );
      const plan = await client.query<{ id: number | string }>(
        `SELECT id FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
        [siteId, row.management_id, row.equipment_unit_id],
      );
      if (plan.rows[0] === undefined)
        throw new CatalogConflictError('The maintenance has no active plan.', 'id');
      await client.query(
        `UPDATE public.lu_management_plan SET maintenance_id=$3,is_draft=false,draft_phase=NULL,draft_saved_at=NULL,draft_summary=NULL,current_phase=4,current_state=6,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, plan.rows[0].id, id, userId],
      );
      const detail = await this.find(client, siteId, id);
      if (detail === null) throw new CatalogMissingError('Maintenance was not found.');
      return detail;
    });
  }

  async cancel(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_maintenance SET status=99,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Maintenance was not found.');
  }
}
