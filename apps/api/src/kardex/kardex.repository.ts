import { Injectable } from '@nestjs/common';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import type {
  KardexInput,
  KardexPage,
  KardexQuery,
  KardexRecord,
  MaintenanceCostInput,
  MaintenanceTaskInput,
} from '@lu/contracts';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface Row {
  plan_id: number | string;
  management_id: number | string;
  management_code: string | null;
  equipment_unit_id: number | string;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  maintenance_id: number | string;
  departure_id: number | string | null;
  maintenance_type: number;
  technician_id: number | string | null;
  scheduled_date: Date | string | null;
  start_date: Date | string | null;
  end_date: Date | string | null;
  actual_return_date: Date | string | null;
  description: string | null;
  actual_cost: number | string | null;
  suggested_next_maintenance_date: Date | string | null;
  satisfaction_level: number | null;
  recommendations: string | null;
  observations: string | null;
  completion_percentage: number;
  current_phase: number;
  current_state: number;
  is_draft: boolean;
}
interface CountRow {
  total_count: number | string;
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
interface HistoryRow {
  id: number | string;
  status: number;
  start_date: Date | string;
  end_date: Date | string | null;
  reason: string | null;
}
type QueryExecutor = Pick<IPgPool, 'query'>;

const SELECT = `
  SELECT p.id AS plan_id,p.management_id,g.code AS management_code,p.equipment_unit_id,
         u.inventory_number,e.name AS equipment_name,l.name AS laboratory_name,
         p.maintenance_id,p.departure_id,m.maintenance_type,m.technician_id,m.scheduled_date,
         m.start_date,m.end_date,d.actual_return_date,m.description,m.actual_cost,
         m.suggested_next_maintenance_date,m.satisfaction_level,m.recommendations,m.observations,
         m.completion_percentage,p.current_phase,p.current_state,p.is_draft
    FROM public.lu_management_plan p
    JOIN public.lu_maintenance m ON m.site_id=p.site_id AND m.id=p.maintenance_id
    LEFT JOIN public.lu_departure d ON d.site_id=p.site_id AND d.id=p.departure_id
    LEFT JOIN public.lu_management g ON g.site_id=p.site_id AND g.id=p.management_id
    LEFT JOIN public.lu_equipment_unit u ON u.site_id=p.site_id AND u.id=p.equipment_unit_id
    LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id
    LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id`;

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function decimal(value: number | string | null | undefined): number {
  if (value === null || value === undefined) return 0;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
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
function date(value: string | null | undefined, field: string, required = false): string | null {
  if (value === null || value === undefined || value === '') {
    if (required) throw new CatalogConflictError(`${field} is required.`, field);
    return null;
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value))
    throw new CatalogConflictError(`${field} is invalid.`, field);
  return value;
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
function taskMap(row: TaskRow) {
  return { id: integer(row.id), description: row.description, isCompleted: row.is_completed };
}
function costMap(row: CostRow) {
  return {
    id: integer(row.id),
    concept: row.concept,
    description: row.description,
    quantity: decimal(row.quantity),
    unitOfMeasure: row.unit_of_measure,
    unitPrice: decimal(row.unit_price),
    category: row.category,
    provider: row.provider,
    costDate: dateOnly(row.cost_date),
    invoiceNumber: row.invoice_number,
  };
}
function map(row: Row): Omit<KardexRecord, 'tasks' | 'costs' | 'history'> {
  return {
    planId: integer(row.plan_id),
    managementId: integer(row.management_id),
    managementCode: row.management_code,
    equipmentUnitId: integer(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    maintenanceId: integer(row.maintenance_id),
    departureId: row.departure_id === null ? null : integer(row.departure_id),
    maintenanceType: [1, 2, 3, 4, 5, 99].includes(row.maintenance_type)
      ? (row.maintenance_type as KardexRecord['maintenanceType'])
      : 99,
    technicianId: row.technician_id === null ? null : integer(row.technician_id),
    scheduledDate: dateOnly(row.scheduled_date),
    startDate: row.start_date === null ? null : iso(row.start_date),
    endDate: row.end_date === null ? null : iso(row.end_date),
    actualReturnDate: row.actual_return_date === null ? null : dateOnly(row.actual_return_date),
    description: row.description,
    actualCost: decimal(row.actual_cost),
    suggestedNextMaintenanceDate: dateOnly(row.suggested_next_maintenance_date),
    satisfactionLevel:
      row.satisfaction_level !== null && [1, 2, 3, 4, 5].includes(row.satisfaction_level)
        ? (row.satisfaction_level as KardexRecord['satisfactionLevel'])
        : null,
    recommendations: row.recommendations,
    observations: row.observations,
    completionPercentage: Math.min(100, Math.max(0, integer(row.completion_percentage))),
    currentPhase: row.current_phase === 6 ? 6 : 5,
    currentState: [7, 8, 9].includes(row.current_state)
      ? (row.current_state as KardexRecord['currentState'])
      : 7,
    isDraft: row.is_draft,
  };
}

@Injectable()
export class KardexRepository {
  async list(pool: IPgPool, siteId: string, query: KardexQuery): Promise<KardexPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = [
      'p.site_id=$1',
      'p.maintenance_id IS NOT NULL',
      'p.current_phase IN (5,6)',
      'p.plan_status<>3',
    ];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`p.management_id=$${params.length}`);
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(u.inventory_number ILIKE $${params.length} OR e.name ILIKE $${params.length} OR g.code ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_management_plan p LEFT JOIN public.lu_equipment_unit u ON u.site_id=p.site_id AND u.id=p.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id LEFT JOIN public.lu_management g ON g.site_id=p.site_id AND g.id=p.management_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<Row>(
      `${SELECT} WHERE ${where} ORDER BY p.current_phase,p.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, (pageIndex - 1) * pageSize],
    );
    return {
      items: await Promise.all(
        rows.rows.map((row) => this.detail(pool, siteId, integer(row.plan_id), row)),
      ),
      totalCount,
      pageIndex,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }
  async find(pool: QueryExecutor, siteId: string, planId: number): Promise<KardexRecord | null> {
    const rows = await pool.query<Row>(`${SELECT} WHERE p.site_id=$1 AND p.id=$2`, [
      siteId,
      planId,
    ]);
    const row = rows.rows[0];
    return row === undefined ? null : this.detail(pool, siteId, planId, row);
  }
  private async detail(
    pool: QueryExecutor,
    siteId: string,
    planId: number,
    row: Row,
  ): Promise<KardexRecord> {
    const tasks = await pool.query<TaskRow>(
      `SELECT id,description,is_completed FROM public.lu_maintenance_task WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, row.maintenance_id],
    );
    const costs = await pool.query<CostRow>(
      `SELECT id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number FROM public.lu_maintenance_cost WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false ORDER BY id`,
      [siteId, row.maintenance_id],
    );
    const history = await pool.query<HistoryRow>(
      `SELECT id,status,start_date,end_date,reason FROM public.lu_equipment_unit_state_history WHERE site_id=$1 AND equipment_unit_id=$2 ORDER BY start_date DESC,id DESC`,
      [siteId, row.equipment_unit_id],
    );
    return {
      ...map(row),
      tasks: tasks.rows.map(taskMap),
      costs: costs.rows.map(costMap),
      history: history.rows.map((item) => ({
        id: integer(item.id),
        status: item.status,
        startDate: iso(item.start_date),
        endDate: item.end_date === null ? null : iso(item.end_date),
        reason: item.reason,
      })),
    };
  }
  private async resolve(client: IPgClient, siteId: string, planId: number): Promise<Row> {
    const rows = await client.query<Row>(
      `${SELECT} WHERE p.site_id=$1 AND p.id=$2 AND p.maintenance_id IS NOT NULL AND p.current_phase IN (5,6) AND p.plan_status<>3`,
      [siteId, planId],
    );
    const row = rows.rows[0];
    if (row === undefined) throw new CatalogMissingError('Kardex plan was not found.');
    return row;
  }
  private async children(
    client: IPgClient,
    siteId: string,
    userId: string,
    maintenanceId: number,
    tasks: readonly MaintenanceTaskInput[],
    costs: readonly MaintenanceCostInput[],
  ): Promise<number> {
    if (tasks.length > 100 || costs.length > 100)
      throw new CatalogConflictError('Kardex child rows exceed the limit.', 'tasks');
    const existing = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_maintenance_task WHERE site_id=$1 AND maintenance_id=$2 AND is_deleted=false`,
      [siteId, maintenanceId],
    );
    const ids = new Set(tasks.filter((task) => task.id !== undefined).map((task) => task.id));
    for (const item of existing.rows)
      if (!ids.has(integer(item.id)))
        await client.query(
          `UPDATE public.lu_maintenance_task SET is_deleted=true,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$3`,
          [siteId, maintenanceId, item.id],
        );
    let complete = 0;
    for (const task of tasks) {
      const description = clean(task.description, 255, 'taskDescription', true)!;
      if (task.isCompleted === true) complete += 1;
      if (task.id !== undefined)
        await client.query(
          `UPDATE public.lu_maintenance_task SET description=$4,is_completed=$5,is_deleted=false,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$3`,
          [siteId, maintenanceId, task.id, description, task.isCompleted === true],
        );
      else
        await client.query(
          `INSERT INTO public.lu_maintenance_task(site_id,maintenance_id,description,is_completed) VALUES($1,$2,$3,$4)`,
          [siteId, maintenanceId, description, task.isCompleted === true],
        );
    }
    for (const cost of costs) {
      const concept = clean(cost.concept, 200, 'concept', true)!;
      if (
        !Number.isFinite(cost.quantity) ||
        cost.quantity <= 0 ||
        !Number.isFinite(cost.unitPrice) ||
        cost.unitPrice < 0
      )
        throw new CatalogConflictError('Cost values are invalid.', 'costs');
      const costDate = date(cost.costDate, 'costDate');
      if (cost.id !== undefined)
        await client.query(
          `UPDATE public.lu_maintenance_cost SET concept=$4,description=$5,quantity=$6,unit_of_measure=$7,unit_price=$8,category=$9,provider=$10,cost_date=$11,invoice_number=$12,is_deleted=false,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND maintenance_id=$2 AND id=$13`,
          [
            siteId,
            maintenanceId,
            userId,
            concept,
            clean(cost.description, 500, 'description'),
            cost.quantity,
            clean(cost.unitOfMeasure, 50, 'unitOfMeasure'),
            cost.unitPrice,
            cost.category ?? 1,
            clean(cost.provider, 200, 'provider'),
            costDate,
            clean(cost.invoiceNumber, 100, 'invoiceNumber'),
            cost.id,
          ],
        );
      else
        await client.query(
          `INSERT INTO public.lu_maintenance_cost(site_id,maintenance_id,concept,description,quantity,unit_of_measure,unit_price,category,provider,cost_date,invoice_number,created_by_id) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)`,
          [
            siteId,
            maintenanceId,
            concept,
            clean(cost.description, 500, 'description'),
            cost.quantity,
            clean(cost.unitOfMeasure, 50, 'unitOfMeasure'),
            cost.unitPrice,
            cost.category ?? 1,
            clean(cost.provider, 200, 'provider'),
            costDate,
            clean(cost.invoiceNumber, 100, 'invoiceNumber'),
            userId,
          ],
        );
    }
    return tasks.length === 0 ? 0 : Math.round((complete / tasks.length) * 100);
  }
  private async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: KardexInput,
    complete: boolean,
  ): Promise<KardexRecord> {
    return pool.transaction(async (client) => {
      const row = await this.resolve(client, siteId, input.planId);
      const scheduled = date(input.scheduledDate, 'scheduledDate');
      const next = date(input.suggestedNextMaintenanceDate, 'suggestedNextMaintenanceDate');
      const description = clean(input.description, 2000, 'description');
      const recommendations = clean(input.recommendations, 1000, 'recommendations');
      const observations = clean(input.observations, 1000, 'observations');
      const tasks = input.tasks ?? [];
      const costs = input.costs ?? [];
      const completion = await this.children(
        client,
        siteId,
        userId,
        integer(row.maintenance_id),
        tasks,
        costs,
      );
      if (complete) {
        if (
          input.technicianId === null ||
          input.technicianId === undefined ||
          input.technicianId < 1
        )
          throw new CatalogConflictError(
            'Technician is required to complete Kardex.',
            'technicianId',
          );
        if (input.actualReturnDate === null || input.actualReturnDate === undefined)
          throw new CatalogConflictError('Actual return date is required.', 'actualReturnDate');
        if ((input.actualCost ?? 0) <= 0)
          throw new CatalogConflictError('Actual cost is required.', 'actualCost');
        if (input.satisfactionLevel === null || input.satisfactionLevel === undefined)
          throw new CatalogConflictError('Satisfaction is required.', 'satisfactionLevel');
        if (recommendations === null)
          throw new CatalogConflictError('Recommendations are required.', 'recommendations');
        if (observations === null)
          throw new CatalogConflictError('Observations are required.', 'observations');
        if (tasks.length === 0 || completion !== 100)
          throw new CatalogConflictError('All Kardex tasks must be completed.', 'tasks');
      }
      await client.query(
        `UPDATE public.lu_maintenance SET technician_id=$3,scheduled_date=$4,start_date=$5,end_date=$6,description=$7,actual_cost=$8,suggested_next_maintenance_date=$9,satisfaction_level=$10,recommendations=$11,observations=$12,completion_percentage=$13,status=$14,step1_cleaning=$15,step2_calibration=$16,step3_testing=$17,step4_final_review=$18,updated_by_id=$19,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [
          siteId,
          row.maintenance_id,
          input.technicianId ?? null,
          scheduled,
          input.startDate ?? null,
          input.endDate ?? null,
          description,
          input.actualCost ?? null,
          next,
          input.satisfactionLevel ?? null,
          recommendations,
          observations,
          complete ? 100 : completion,
          complete ? 2 : 1,
          tasks[0]?.isCompleted === true,
          tasks[1]?.isCompleted === true,
          tasks[2]?.isCompleted === true,
          tasks[3]?.isCompleted === true,
          userId,
        ],
      );
      if (!complete)
        await client.query(
          `UPDATE public.lu_management_plan SET is_draft=true,draft_phase=5,draft_saved_at=CURRENT_TIMESTAMP,draft_summary='Kardex draft saved',current_phase=5,current_state=7,plan_status=1,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, input.planId, userId],
        );
      else {
        const returnDate = date(input.actualReturnDate, 'actualReturnDate', true)!;
        if (row.departure_id !== null)
          await client.query(
            `UPDATE public.lu_departure SET status=1,actual_return_date=$3,return_observations='Equipo devuelto y validado en cierre Kardex',updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
            [siteId, row.departure_id, returnDate, userId],
          );
        await client.query(
          `UPDATE public.lu_equipment_unit SET current_status=0,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, row.equipment_unit_id, userId],
        );
        await client.query(
          `UPDATE public.lu_equipment_unit_state_history SET end_date=$3,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND equipment_unit_id=$2 AND end_date IS NULL`,
          [siteId, row.equipment_unit_id, returnDate, userId],
        );
        const history = await client.query<{ id: number | string }>(
          `INSERT INTO public.lu_equipment_unit_state_history(site_id,equipment_unit_id,status,start_date,reason,created_by_id) VALUES($1,$2,0,$3,$4,$5) RETURNING id`,
          [
            siteId,
            row.equipment_unit_id,
            returnDate,
            `Kardex cierre L-8 #${row.maintenance_id}`,
            userId,
          ],
        );
        await client.query(
          `UPDATE public.lu_management_plan SET kardex_history_id=$3,is_draft=false,draft_phase=NULL,draft_saved_at=NULL,draft_summary=NULL,current_phase=6,current_state=8,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, input.planId, history.rows[0]?.id, userId],
        );
      }
      const result = await this.find(client, siteId, input.planId);
      if (result === null) throw new CatalogMissingError('Kardex was not created.');
      return result;
    });
  }
  async draft(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: KardexInput,
  ): Promise<KardexRecord> {
    return this.save(pool, siteId, userId, { ...input, isDraft: true }, false);
  }
  async complete(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: KardexInput,
  ): Promise<KardexRecord> {
    return this.save(pool, siteId, userId, { ...input, isDraft: false }, true);
  }
}
