import { Injectable } from '@nestjs/common';
import type {
  CreateDepartureInput,
  DepartureDetail,
  DeparturePage,
  DepartureQuery,
  DepartureRecord,
  DepartureType,
  LoanStatus,
  MassDepartureInput,
  UpdateDepartureInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface DepartureRow {
  id: number | string;
  management_id: number | string;
  management_code: string | null;
  equipment_unit_id: number | string | null;
  inventory_number: string | null;
  equipment_name: string | null;
  laboratory_name: string | null;
  borrower_id: number | string | null;
  origin_laboratory_id: number | string | null;
  destination: string | null;
  type: number;
  departure_date: Date | string;
  estimated_return_date: Date | string | null;
  actual_return_date: Date | string | null;
  departure_observations: string | null;
  return_observations: string | null;
  status: number;
  is_draft: boolean;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface ItemRow {
  id: number | string;
  equipment_unit_id: number | string | null;
  product_name: string | null;
  quantity: number | null;
  unit_of_measure: string | null;
  returned_quantity: number | null;
  observations: string | null;
  is_removed: boolean;
}
interface CountRow {
  total_count: number | string;
}
type QueryExecutor = Pick<IPgPool, 'query'>;

const SELECT = `
  SELECT d.id,d.management_id,g.code AS management_code,d.equipment_unit_id,
         u.inventory_number,e.name AS equipment_name,l.name AS laboratory_name,
         d.borrower_id,d.origin_laboratory_id,d.destination,d.type,d.departure_date,
         d.estimated_return_date,d.actual_return_date,d.departure_observations,
         d.return_observations,d.status,coalesce(p.is_draft,false) AS is_draft,
         d.created_at,d.updated_at
    FROM public.lu_departure d
    LEFT JOIN public.lu_management g ON g.site_id=d.site_id AND g.id=d.management_id
    LEFT JOIN public.lu_equipment_unit u ON u.site_id=d.site_id AND u.id=d.equipment_unit_id
    LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id
    LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id
    LEFT JOIN public.lu_management_plan p ON p.site_id=d.site_id AND p.management_id=d.management_id
      AND p.equipment_unit_id=d.equipment_unit_id AND p.plan_status<>3`;

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
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
function departureType(value: number): DepartureType {
  return [1, 2, 3, 4, 5].includes(value) ? (value as DepartureType) : 2;
}
function status(value: number): LoanStatus {
  return [0, 1, 2, 99].includes(value) ? (value as LoanStatus) : 99;
}
function map(row: DepartureRow): DepartureRecord {
  return {
    id: integer(row.id),
    managementId: integer(row.management_id),
    managementCode: row.management_code,
    equipmentUnitId: row.equipment_unit_id === null ? null : integer(row.equipment_unit_id),
    inventoryNumber: row.inventory_number,
    equipmentName: row.equipment_name,
    laboratoryName: row.laboratory_name,
    borrowerId: row.borrower_id === null ? null : integer(row.borrower_id),
    originLaboratoryId:
      row.origin_laboratory_id === null ? null : integer(row.origin_laboratory_id),
    destination: row.destination,
    type: departureType(row.type),
    departureDate: dateOnly(row.departure_date)!,
    estimatedReturnDate: dateOnly(row.estimated_return_date),
    actualReturnDate: row.actual_return_date === null ? null : iso(row.actual_return_date),
    departureObservations: row.departure_observations,
    returnObservations: row.return_observations,
    status: status(row.status),
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
function date(value: string | null | undefined, field: string, required = false): string | null {
  if (value === null || value === undefined || value === '') {
    if (required) throw new CatalogConflictError(`${field} is required.`, field);
    return null;
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value))
    throw new CatalogConflictError(`${field} is invalid.`, field);
  return value;
}

@Injectable()
export class DepartureRepository {
  async list(pool: IPgPool, siteId: string, query: DepartureQuery): Promise<DeparturePage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['d.site_id=$1', 'd.status<>99'];
    if (query.managementId !== undefined) {
      params.push(query.managementId);
      filters.push(`d.management_id=$${params.length}`);
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id=$${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`d.status=$${params.length}`);
    }
    if (query.type !== undefined) {
      params.push(query.type);
      filters.push(`d.type=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(e.name ILIKE $${params.length} OR u.inventory_number ILIKE $${params.length} OR d.destination ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_departure d LEFT JOIN public.lu_equipment_unit u ON u.site_id=d.site_id AND u.id=d.equipment_unit_id LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<DepartureRow>(
      `${SELECT} WHERE ${where} ORDER BY d.departure_date DESC,d.id DESC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
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
  async find(pool: QueryExecutor, siteId: string, id: number): Promise<DepartureDetail | null> {
    const rows = await pool.query<DepartureRow>(`${SELECT} WHERE d.site_id=$1 AND d.id=$2`, [
      siteId,
      id,
    ]);
    const row = rows.rows[0];
    if (row === undefined) return null;
    const items = await pool.query<ItemRow>(
      `SELECT id,equipment_unit_id,product_name,quantity,unit_of_measure,returned_quantity,observations,is_removed FROM public.lu_departure_item WHERE site_id=$1 AND departure_id=$2 AND is_removed=false ORDER BY id`,
      [siteId, id],
    );
    return {
      ...map(row),
      items: items.rows.map((item) => ({
        id: integer(item.id),
        equipmentUnitId: item.equipment_unit_id === null ? null : integer(item.equipment_unit_id),
        productName: item.product_name,
        quantity: item.quantity,
        unitOfMeasure: item.unit_of_measure,
        returnedQuantity: item.returned_quantity,
        observations: item.observations,
        isRemoved: item.is_removed,
      })),
    };
  }
  private async target(
    client: IPgClient,
    siteId: string,
    managementId: number,
    equipmentUnitId: number,
  ): Promise<number> {
    const management = await client.query(
      `SELECT id FROM public.lu_management WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, managementId],
    );
    if (management.rows[0] === undefined)
      throw new CatalogConflictError('The management is not available.', 'managementId');
    const unit = await client.query<{
      id: number | string;
      equipment_id: number | string;
      laboratory_id: number | string | null;
    }>(
      `SELECT id,equipment_id,laboratory_id FROM public.lu_equipment_unit WHERE site_id=$1 AND id=$2 AND current_status<>99 AND laboratory_id IS NOT NULL`,
      [siteId, equipmentUnitId],
    );
    if (unit.rows[0] === undefined)
      throw new CatalogConflictError(
        'The equipment unit must have an active laboratory.',
        'equipmentUnitId',
      );
    const plan = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_management_plan WHERE site_id=$1 AND management_id=$2 AND equipment_unit_id=$3 AND plan_status<>3`,
      [siteId, managementId, equipmentUnitId],
    );
    if (plan.rows[0] === undefined)
      throw new CatalogConflictError('The unit has no active Exit plan.', 'equipmentUnitId');
    return integer(plan.rows[0].id);
  }
  private async saveWithClient(
    client: IPgClient,
    siteId: string,
    userId: string,
    input: CreateDepartureInput | UpdateDepartureInput,
    id: number | null,
  ): Promise<DepartureDetail> {
    const equipmentUnitId =
      input.equipmentUnitId ??
      input.items?.find(
        (item) => item.equipmentUnitId !== null && item.equipmentUnitId !== undefined,
      )?.equipmentUnitId ??
      null;
    if (equipmentUnitId === null)
      throw new CatalogConflictError('An equipment unit is required.', 'equipmentUnitId');
    const planId = await this.target(client, siteId, input.managementId, equipmentUnitId);
    const departureDate = date(input.departureDate, 'departureDate', true)!;
    const returnDate = date(input.estimatedReturnDate, 'estimatedReturnDate');
    if (returnDate !== null && returnDate < departureDate)
      throw new CatalogConflictError(
        'Estimated return cannot precede departure.',
        'estimatedReturnDate',
      );
    const type = input.type ?? 2;
    if (![1, 2, 3, 4, 5].includes(type))
      throw new CatalogConflictError('Departure type is invalid.', 'type');
    const desiredStatus = input.status ?? 0;
    if (![0, 1, 2, 99].includes(desiredStatus))
      throw new CatalogConflictError('Departure status is invalid.', 'status');
    const draft = input.isDraft === true;
    const destination = clean(input.destination, 200, 'destination');
    const departureObservations = clean(input.departureObservations, 500, 'departureObservations');
    const returnObservations = clean(input.returnObservations, 500, 'returnObservations');
    let departureId = id;
    if (departureId === null) {
      const inserted = await client.query<{ id: number | string }>(
        `INSERT INTO public.lu_departure(site_id,management_id,equipment_unit_id,borrower_id,origin_laboratory_id,destination,type,departure_date,estimated_return_date,actual_return_date,departure_observations,return_observations,status,created_by_id) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14) RETURNING id`,
        [
          siteId,
          input.managementId,
          equipmentUnitId,
          input.borrowerId ?? null,
          input.originLaboratoryId ?? null,
          destination,
          type,
          departureDate,
          returnDate,
          input.actualReturnDate ?? null,
          departureObservations,
          returnObservations,
          draft ? 0 : desiredStatus,
          userId,
        ],
      );
      if (inserted.rows[0] === undefined)
        throw new CatalogMissingError('Departure was not created.');
      departureId = integer(inserted.rows[0].id);
    } else {
      const existing = await client.query(
        `SELECT id FROM public.lu_departure WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      if (existing.rows[0] === undefined) throw new CatalogMissingError('Departure was not found.');
      await client.query(
        `UPDATE public.lu_departure SET management_id=$3,equipment_unit_id=$4,borrower_id=$5,origin_laboratory_id=$6,destination=$7,type=$8,departure_date=$9,estimated_return_date=$10,actual_return_date=$11,departure_observations=$12,return_observations=$13,status=$14,updated_by_id=$15,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [
          siteId,
          id,
          input.managementId,
          equipmentUnitId,
          input.borrowerId ?? null,
          input.originLaboratoryId ?? null,
          destination,
          type,
          departureDate,
          returnDate,
          input.actualReturnDate ?? null,
          departureObservations,
          returnObservations,
          draft ? 0 : desiredStatus,
          userId,
        ],
      );
    }
    const effectiveId = departureId!;
    const incoming = (input.items ?? []).filter(
      (item) =>
        item.productName !== null ||
        item.equipmentUnitId !== null ||
        item.equipmentUnitId === equipmentUnitId,
    );
    const rows =
      incoming.length > 0
        ? incoming
        : [
            {
              equipmentUnitId,
              productName: null,
              quantity: 1,
              unitOfMeasure: 'UNIDAD',
              observations: null,
            },
          ];
    if (rows.length > 100)
      throw new CatalogConflictError('No more than 100 departure items are allowed.', 'items');
    const existingItems = await client.query<{ id: number | string }>(
      `SELECT id FROM public.lu_departure_item WHERE site_id=$1 AND departure_id=$2 AND is_removed=false`,
      [siteId, effectiveId],
    );
    const ids = new Set(rows.filter((item) => item.id !== undefined).map((item) => item.id));
    for (const item of existingItems.rows)
      if (!ids.has(integer(item.id)))
        await client.query(
          `UPDATE public.lu_departure_item SET is_removed=true,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND departure_id=$2 AND id=$4`,
          [siteId, effectiveId, userId, item.id],
        );
    for (const item of rows) {
      const itemUnit: number = item.equipmentUnitId ?? equipmentUnitId;
      if (itemUnit !== equipmentUnitId)
        await this.target(client, siteId, input.managementId, itemUnit);
      const productName = clean(item.productName, 200, 'productName');
      const unit = clean(item.unitOfMeasure ?? 'UNIDAD', 50, 'unitOfMeasure');
      const quantity = item.quantity ?? 1;
      if (!Number.isInteger(quantity) || quantity < 1)
        throw new CatalogConflictError('Item quantity is invalid.', 'quantity');
      const returned = item.returnedQuantity ?? 0;
      if (!Number.isInteger(returned) || returned < 0 || returned > quantity)
        throw new CatalogConflictError('Returned quantity is invalid.', 'returnedQuantity');
      if (item.id !== undefined)
        await client.query(
          `UPDATE public.lu_departure_item SET equipment_unit_id=$4,product_name=$5,quantity=$6,unit_of_measure=$7,returned_quantity=$8,observations=$9,is_removed=false,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND departure_id=$2 AND id=$10`,
          [
            siteId,
            effectiveId,
            userId,
            itemUnit,
            productName,
            quantity,
            unit,
            returned,
            clean(item.observations, 500, 'observations'),
            item.id,
          ],
        );
      else
        await client.query(
          `INSERT INTO public.lu_departure_item(site_id,departure_id,equipment_unit_id,product_name,quantity,unit_of_measure,returned_quantity,observations,created_by_id) VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9)`,
          [
            siteId,
            effectiveId,
            itemUnit,
            productName,
            quantity,
            unit,
            returned,
            clean(item.observations, 500, 'observations'),
            userId,
          ],
        );
    }
    if (!draft) {
      await client.query(
        `UPDATE public.lu_equipment_unit SET current_status=10,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, equipmentUnitId, userId],
      );
      await client.query(
        `UPDATE public.lu_management_plan SET departure_id=$3,is_draft=false,draft_phase=NULL,draft_saved_at=NULL,draft_summary=NULL,current_phase=5,current_state=7,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, planId, effectiveId, userId],
      );
    } else
      await client.query(
        `UPDATE public.lu_management_plan SET departure_id=$3,is_draft=true,draft_phase=4,draft_saved_at=CURRENT_TIMESTAMP,draft_summary='L-3 draft saved',current_phase=4,current_state=6,plan_status=1,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, planId, effectiveId, userId],
      );
    const detail = await this.find(client, siteId, effectiveId);
    if (detail === null) throw new CatalogMissingError('Departure was not created.');
    return detail;
  }
  async save(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateDepartureInput,
  ): Promise<DepartureDetail> {
    return pool.transaction((client) => this.saveWithClient(client, siteId, userId, input, null));
  }
  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateDepartureInput,
  ): Promise<DepartureDetail> {
    return pool.transaction((client) => this.saveWithClient(client, siteId, userId, input, id));
  }
  async mass(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: MassDepartureInput,
  ): Promise<readonly DepartureDetail[]> {
    if (input.rows.length === 0 || input.rows.length > 100)
      throw new CatalogConflictError('Mass departure must contain between 1 and 100 rows.', 'rows');
    return pool.transaction(async (client) => {
      const result: DepartureDetail[] = [];
      for (const row of input.rows)
        result.push(
          await this.saveWithClient(
            client,
            siteId,
            userId,
            {
              managementId: input.managementId,
              equipmentUnitId: row.equipmentUnitId,
              departureDate: input.departureDate,
              estimatedReturnDate: input.estimatedReturnDate,
              isDraft: input.isDraft,
              type: input.type ?? 2,
              destination: input.destination,
              items: [
                { equipmentUnitId: row.equipmentUnitId, quantity: 1, unitOfMeasure: 'UNIDAD' },
              ],
            },
            null,
          ),
        );
      return result;
    });
  }
  async return(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    observations: string | null,
  ): Promise<DepartureDetail> {
    return pool.transaction(async (client) => {
      const row = await client.query<{ equipment_unit_id: number | string | null }>(
        `SELECT equipment_unit_id FROM public.lu_departure WHERE site_id=$1 AND id=$2 AND status<>99`,
        [siteId, id],
      );
      if (row.rows[0] === undefined) throw new CatalogMissingError('Departure was not found.');
      await client.query(
        `UPDATE public.lu_departure SET status=1,actual_return_date=CURRENT_TIMESTAMP,return_observations=$3,updated_by_id=$4,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
        [siteId, id, clean(observations, 500, 'returnObservations'), userId],
      );
      if (row.rows[0].equipment_unit_id !== null)
        await client.query(
          `UPDATE public.lu_equipment_unit SET current_status=0,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2`,
          [siteId, row.rows[0].equipment_unit_id, userId],
        );
      const detail = await this.find(client, siteId, id);
      if (detail === null) throw new CatalogMissingError('Departure was not found.');
      return detail;
    });
  }
  async cancel(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_departure SET status=99,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>99`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Departure was not found.');
  }
}
