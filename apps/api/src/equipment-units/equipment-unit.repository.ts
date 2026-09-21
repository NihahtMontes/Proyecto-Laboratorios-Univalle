import { Injectable } from '@nestjs/common';
import type {
  CreateEquipmentUnitInput,
  EquipmentUnitDetail,
  EquipmentUnitPage,
  EquipmentUnitQuery,
  EquipmentUnitRecord,
  EquipmentUnitStateHistory,
  EquipmentUnitStatus,
  PhysicalCondition,
  UpdateEquipmentUnitInput,
} from '@lu/contracts';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

type QueryExecutor = Pick<IPgPool, 'query'>;

interface UnitRow {
  id: number | string;
  equipment_id: number | string;
  equipment_name: string | null;
  equipment_category: number | null;
  laboratory_id: number | string | null;
  laboratory_code: string | null;
  laboratory_name: string | null;
  faculty_id: number | string | null;
  career_id: number | string | null;
  inventory_number: string;
  serial_number: string | null;
  internal_location: string | null;
  acquisition_date: Date | string | null;
  manufacturing_date: Date | string | null;
  acquisition_value: number | string | null;
  current_status: number;
  physical_condition: number | null;
  location_resolution_status: number;
  notes: string | null;
  created_at: Date | string;
  updated_at: Date | string | null;
}

interface HistoryRow {
  id: number | string;
  equipment_unit_id: number | string;
  status: number;
  start_date: Date | string;
  end_date: Date | string | null;
  reason: string | null;
}

interface CountRow {
  total_count: number | string;
}

interface ExistingRow {
  current_status: number;
  physical_condition: number | null;
}

function integer(value: number | string | null | undefined): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}

function nullableInteger(value: number | string | null): number | null {
  return value === null ? null : integer(value);
}

function unitStatus(value: number): EquipmentUnitStatus {
  const allowed: readonly EquipmentUnitStatus[] = [0, 1, 2, 3, 4, 5, 6, 10, 99];
  return allowed.includes(value as EquipmentUnitStatus) ? (value as EquipmentUnitStatus) : 99;
}

function physical(value: number | null): PhysicalCondition | null {
  return value !== null && [1, 2, 3, 4, 5].includes(value) ? (value as PhysicalCondition) : null;
}

function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}

function dateOnly(value: Date | string | null): string | null {
  if (value === null) return null;
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  return iso(value).slice(0, 10);
}

function yearsInOperation(value: Date | string | null): number | null {
  if (value === null) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  const now = new Date();
  let years = now.getUTCFullYear() - date.getUTCFullYear();
  const birthday = new Date(Date.UTC(now.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  if (now < birthday) years -= 1;
  return Math.max(0, years);
}

function text(value: string | null, max: number, field: string, required = false): string | null {
  const normalized = value === null ? '' : value.trim().replace(/\s+/g, ' ');
  if (required && normalized.length < 1)
    throw new CatalogConflictError(`${field} is required.`, field);
  if (normalized.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return normalized === '' ? null : normalized;
}

function statusValue(value: number | undefined): EquipmentUnitStatus {
  const result = value ?? 0;
  if (![0, 1, 2, 3, 4, 5, 6, 10, 99].includes(result))
    throw new CatalogConflictError('Current status is invalid.', 'currentStatus');
  return result as EquipmentUnitStatus;
}

function conditionValue(value: number | null | undefined): PhysicalCondition | null {
  if (value === null || value === undefined) return null;
  if (![1, 2, 3, 4, 5].includes(value))
    throw new CatalogConflictError('Physical condition is invalid.', 'physicalCondition');
  return value as PhysicalCondition;
}

function normalizedInput(input: CreateEquipmentUnitInput | UpdateEquipmentUnitInput): {
  inventoryNumber: string;
  serialNumber: string | null;
  internalLocation: string | null;
  notes: string | null;
  currentStatus: EquipmentUnitStatus;
  physicalCondition: PhysicalCondition | null;
} {
  const inventoryNumber = text(input.inventoryNumber, 50, 'inventoryNumber', true)!;
  if (inventoryNumber.length < 3)
    throw new CatalogConflictError(
      'Inventory number must contain at least 3 characters.',
      'inventoryNumber',
    );
  const acquisitionValue = input.acquisitionValue;
  if (
    acquisitionValue !== null &&
    acquisitionValue !== undefined &&
    (!Number.isFinite(acquisitionValue) || acquisitionValue < 0 || acquisitionValue > 999999999)
  )
    throw new CatalogConflictError('Acquisition value is invalid.', 'acquisitionValue');
  return {
    inventoryNumber: inventoryNumber.toUpperCase(),
    serialNumber: text(input.serialNumber ?? null, 100, 'serialNumber'),
    internalLocation: text(input.internalLocation ?? null, 200, 'internalLocation'),
    notes: text(input.notes ?? null, 2000, 'notes'),
    currentStatus: statusValue(input.currentStatus),
    physicalCondition: conditionValue(input.physicalCondition),
  };
}

const SELECT = `
  SELECT u.id, u.equipment_id, e.name AS equipment_name, e.category AS equipment_category,
         u.laboratory_id, l.code AS laboratory_code, l.name AS laboratory_name, l.faculty_id,
         u.career_id, u.inventory_number, u.serial_number, u.internal_location,
         u.acquisition_date, u.manufacturing_date, u.acquisition_value,
         u.current_status, u.physical_condition, u.location_resolution_status, u.notes,
         u.created_at, u.updated_at
    FROM public.lu_equipment_unit u
    LEFT JOIN public.lu_equipment e
      ON e.site_id = u.site_id AND e.id = u.equipment_id
    LEFT JOIN public.lu_laboratory l
      ON l.site_id = u.site_id AND l.id = u.laboratory_id`;

function mapUnit(row: UnitRow): EquipmentUnitRecord {
  const category = row.equipment_category;
  return {
    id: integer(row.id),
    equipmentId: integer(row.equipment_id),
    equipmentName: row.equipment_name,
    equipmentCategory: category === 0 || category === 1 || category === 2 ? category : null,
    laboratoryId: nullableInteger(row.laboratory_id),
    laboratoryCode: row.laboratory_code,
    laboratoryName: row.laboratory_name,
    facultyId: nullableInteger(row.faculty_id),
    careerId: nullableInteger(row.career_id),
    inventoryNumber: row.inventory_number,
    serialNumber: row.serial_number,
    internalLocation: row.internal_location,
    acquisitionDate: dateOnly(row.acquisition_date),
    manufacturingDate: dateOnly(row.manufacturing_date),
    acquisitionValue: row.acquisition_value === null ? null : Number(row.acquisition_value),
    currentStatus: unitStatus(row.current_status),
    physicalCondition: physical(row.physical_condition),
    locationResolutionStatus: row.location_resolution_status === 1 ? 1 : 0,
    notes: row.notes,
    yearsInOperation: yearsInOperation(row.manufacturing_date),
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}

function mapHistory(row: HistoryRow): EquipmentUnitStateHistory {
  return {
    id: integer(row.id),
    equipmentUnitId: integer(row.equipment_unit_id),
    status: unitStatus(row.status),
    startDate: iso(row.start_date),
    endDate: row.end_date === null ? null : iso(row.end_date),
    reason: row.reason,
  };
}

function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

@Injectable()
export class EquipmentUnitRepository {
  async list(pool: IPgPool, siteId: string, query: EquipmentUnitQuery): Promise<EquipmentUnitPage> {
    const pageIndex = Math.max(1, query.currentPage ?? 1);
    const pageSize = 20;
    const params: unknown[] = [siteId];
    const filters = ['u.site_id = $1', 'u.current_status <> 99'];
    if (!query.includeUnresolved) filters.push('u.laboratory_id IS NOT NULL');
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(u.inventory_number ILIKE $${params.length} OR u.serial_number ILIKE $${params.length} OR e.name ILIKE $${params.length} OR l.name ILIKE $${params.length})`,
      );
    }
    if (query.laboratoryId !== undefined) {
      params.push(query.laboratoryId);
      filters.push(`u.laboratory_id = $${params.length}`);
    }
    if (query.equipmentId !== undefined) {
      params.push(query.equipmentId);
      filters.push(`u.equipment_id = $${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`u.current_status = $${params.length}`);
    }
    if (query.locationResolutionStatus !== undefined) {
      params.push(query.locationResolutionStatus);
      filters.push(`u.location_resolution_status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_equipment_unit u
       LEFT JOIN public.lu_equipment e ON e.site_id=u.site_id AND e.id=u.equipment_id
       LEFT JOIN public.lu_laboratory l ON l.site_id=u.site_id AND l.id=u.laboratory_id
       WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<UnitRow>(
      `${SELECT} WHERE ${where} ORDER BY u.inventory_number, u.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, (pageIndex - 1) * pageSize],
    );
    return {
      items: rows.rows.map(mapUnit),
      totalCount,
      pageIndex,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async find(pool: QueryExecutor, siteId: string, id: number): Promise<EquipmentUnitDetail | null> {
    const result = await pool.query<UnitRow>(`${SELECT} WHERE u.site_id=$1 AND u.id=$2`, [
      siteId,
      id,
    ]);
    const row = result.rows[0];
    if (row === undefined) return null;
    const history = await pool.query<HistoryRow>(
      `SELECT id, equipment_unit_id, status, start_date, end_date, reason
         FROM public.lu_equipment_unit_state_history
        WHERE site_id=$1 AND equipment_unit_id=$2
        ORDER BY start_date DESC, id DESC`,
      [siteId, id],
    );
    return { ...mapUnit(row), stateHistory: history.rows.map(mapHistory) };
  }

  async history(
    pool: IPgPool,
    siteId: string,
    id: number,
  ): Promise<readonly EquipmentUnitStateHistory[]> {
    const rows = await pool.query<HistoryRow>(
      `SELECT id, equipment_unit_id, status, start_date, end_date, reason
         FROM public.lu_equipment_unit_state_history
        WHERE site_id=$1 AND equipment_unit_id=$2
        ORDER BY start_date DESC, id DESC`,
      [siteId, id],
    );
    return rows.rows.map(mapHistory);
  }

  private async assertReferences(
    pool: IPgPool,
    siteId: string,
    input: CreateEquipmentUnitInput,
  ): Promise<void> {
    const equipment = await pool.query(
      `SELECT id FROM public.lu_equipment WHERE site_id=$1 AND id=$2 AND status<>2`,
      [siteId, input.equipmentId],
    );
    if (equipment.rows[0] === undefined)
      throw new CatalogConflictError('The selected equipment is not active.', 'equipmentId');
    if (!Number.isSafeInteger(input.laboratoryId) || input.laboratoryId < 1)
      throw new CatalogConflictError('A laboratory is required.', 'laboratoryId');
    const laboratory = await pool.query(
      `SELECT id FROM public.lu_laboratory WHERE site_id=$1 AND id=$2 AND status<>2`,
      [siteId, input.laboratoryId],
    );
    if (laboratory.rows[0] === undefined)
      throw new CatalogConflictError('The selected laboratory is not active.', 'laboratoryId');
  }

  async create(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateEquipmentUnitInput,
  ): Promise<EquipmentUnitDetail> {
    const values = normalizedInput(input);
    await this.assertReferences(pool, siteId, input);
    try {
      return await pool.transaction(async (client) => {
        const inserted = await client.query<{ id: number | string }>(
          `INSERT INTO public.lu_equipment_unit
             (site_id, equipment_id, laboratory_id, location_resolution_status, inventory_number,
              serial_number, career_id, internal_location, acquisition_date, manufacturing_date,
              acquisition_value, current_status, physical_condition, notes, created_by_id)
           VALUES ($1,$2,$3,1,$4,$5,$6,$7,$8,$9,$10,$11,$12,$13,$14)
           RETURNING id`,
          [
            siteId,
            input.equipmentId,
            input.laboratoryId,
            values.inventoryNumber,
            values.serialNumber,
            input.careerId ?? null,
            values.internalLocation,
            input.acquisitionDate ?? null,
            input.manufacturingDate ?? null,
            input.acquisitionValue ?? null,
            values.currentStatus,
            values.physicalCondition,
            values.notes,
            userId,
          ],
        );
        const row = inserted.rows[0];
        if (row === undefined) throw new CatalogMissingError('Equipment unit was not created.');
        await client.query(
          `INSERT INTO public.lu_equipment_unit_state_history
             (site_id, equipment_unit_id, status, reason, created_by_id)
           VALUES ($1,$2,$3,$4,$5)`,
          [siteId, row.id, values.currentStatus, 'Initial registration.', userId],
        );
        const result = await this.find(client, siteId, integer(row.id));
        if (result === null) throw new CatalogMissingError('Equipment unit was not created.');
        return result;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active unit with this inventory number already exists.',
          'inventoryNumber',
        );
      throw error;
    }
  }

  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateEquipmentUnitInput,
  ): Promise<EquipmentUnitDetail> {
    const values = normalizedInput(input);
    await this.assertReferences(pool, siteId, input);
    try {
      return await pool.transaction(async (client) => {
        const current = await client.query<ExistingRow>(
          `SELECT current_status, physical_condition FROM public.lu_equipment_unit
            WHERE site_id=$1 AND id=$2 AND current_status<>99`,
          [siteId, id],
        );
        const existing = current.rows[0];
        if (existing === undefined) throw new CatalogMissingError('Equipment unit was not found.');
        const stateChanged =
          existing.current_status !== values.currentStatus ||
          existing.physical_condition !== values.physicalCondition;
        const result = await client.query(
          `UPDATE public.lu_equipment_unit SET
             equipment_id=$3, laboratory_id=$4, location_resolution_status=1,
             inventory_number=$5, serial_number=$6, career_id=$7, internal_location=$8,
             acquisition_date=$9, manufacturing_date=$10, acquisition_value=$11,
             current_status=$12, physical_condition=$13, notes=$14,
             updated_by_id=$15, updated_at=CURRENT_TIMESTAMP
           WHERE site_id=$1 AND id=$2 AND current_status<>99`,
          [
            siteId,
            id,
            input.equipmentId,
            input.laboratoryId,
            values.inventoryNumber,
            values.serialNumber,
            input.careerId ?? null,
            values.internalLocation,
            input.acquisitionDate ?? null,
            input.manufacturingDate ?? null,
            input.acquisitionValue ?? null,
            values.currentStatus,
            values.physicalCondition,
            values.notes,
            userId,
          ],
        );
        if (result.rowCount !== 1) throw new CatalogMissingError('Equipment unit was not found.');
        if (stateChanged) {
          await client.query(
            `UPDATE public.lu_equipment_unit_state_history
                SET end_date=CURRENT_TIMESTAMP, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP
              WHERE site_id=$1 AND equipment_unit_id=$2 AND end_date IS NULL`,
            [siteId, id, userId],
          );
          await client.query(
            `INSERT INTO public.lu_equipment_unit_state_history
               (site_id, equipment_unit_id, status, reason, created_by_id)
             VALUES ($1,$2,$3,$4,$5)`,
            [
              siteId,
              id,
              values.currentStatus,
              'Manual status or physical condition update.',
              userId,
            ],
          );
        }
        const detail = await this.find(client, siteId, id);
        if (detail === null) throw new CatalogMissingError('Equipment unit was not found.');
        return detail;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An active unit with this inventory number already exists.',
          'inventoryNumber',
        );
      throw error;
    }
  }

  async remove(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    await pool.transaction(async (client: IPgClient) => {
      const current = await client.query<ExistingRow>(
        `SELECT current_status, physical_condition FROM public.lu_equipment_unit
          WHERE site_id=$1 AND id=$2 AND current_status<>99`,
        [siteId, id],
      );
      if (current.rows[0] === undefined)
        throw new CatalogMissingError('Equipment unit was not found.');
      const result = await client.query(
        `UPDATE public.lu_equipment_unit SET current_status=99, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP
          WHERE site_id=$1 AND id=$2 AND current_status<>99`,
        [siteId, id, userId],
      );
      if (result.rowCount !== 1) throw new CatalogMissingError('Equipment unit was not found.');
      await client.query(
        `UPDATE public.lu_equipment_unit_state_history
            SET end_date=CURRENT_TIMESTAMP, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP
          WHERE site_id=$1 AND equipment_unit_id=$2 AND end_date IS NULL`,
        [siteId, id, userId],
      );
      await client.query(
        `INSERT INTO public.lu_equipment_unit_state_history
           (site_id, equipment_unit_id, status, reason, created_by_id)
         VALUES ($1,$2,99,'Logical removal.', $3)`,
        [siteId, id, userId],
      );
    });
  }
}
