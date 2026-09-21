import { Injectable, NotFoundException } from '@nestjs/common';
import ExcelJS from 'exceljs';
import { access } from 'node:fs/promises';
import { constants } from 'node:fs';
import { join, resolve } from 'node:path';
import type { ReportKind, ReportManifestItem, SiteRequestContext } from '@lu/contracts';
import { SiteRole } from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { AuthForbiddenException } from '../auth/auth.exceptions.js';
import { TenantPgPoolRegistry } from '../core/tenant-pg-pool.js';
import { TenantRouter } from '../core/tenant-routing.js';

export interface GeneratedReport {
  readonly buffer: Buffer;
  readonly filename: string;
  readonly contentType: string;
}

interface ReportRow {
  readonly id?: number | string;
  readonly management_id?: number | string;
  readonly inventory_number: string | null;
  readonly equipment_name: string | null;
  readonly brand: string | null;
  readonly model: string | null;
  readonly serial_number: string | null;
  readonly laboratory_name: string | null;
  readonly current_phase?: number;
  readonly current_state?: number;
  readonly plan_status?: number;
  readonly is_draft?: boolean;
  readonly planned_date?: Date | string | null;
  readonly verification_date?: Date | string | null;
  readonly physical_condition?: number | null;
  readonly observations?: string | null;
  readonly faults?: string | null;
  readonly request_id?: number | string | null;
  readonly description?: string | null;
  readonly priority?: number | null;
  readonly request_date?: Date | string | null;
  readonly estimated_repair_time?: string | null;
  readonly investment_code?: string | null;
  readonly cost_center?: string | null;
  readonly departure_id?: number | string | null;
  readonly departure_date?: Date | string | null;
  readonly destination?: string | null;
  readonly departure_status?: number | null;
  readonly product_name?: string | null;
  readonly quantity?: number | null;
  readonly unit_of_measure?: string | null;
  readonly returned_quantity?: number | null;
  readonly maintenance_id?: number | string | null;
  readonly maintenance_status?: number | null;
  readonly scheduled_date?: Date | string | null;
  readonly start_date?: Date | string | null;
  readonly end_date?: Date | string | null;
  readonly actual_cost?: number | string | null;
  readonly satisfaction_level?: number | null;
  readonly task_description?: string | null;
  readonly task_completed?: boolean;
  readonly cost_concept?: string | null;
  readonly cost_description?: string | null;
  readonly cost_quantity?: number | string | null;
  readonly cost_unit_price?: number | string | null;
  readonly unit_price?: number | string | null;
  readonly management_code?: string | null;
  readonly management_type?: number | null;
  readonly year?: number;
  readonly semester?: number;
}

const MANIFEST: readonly ReportManifestItem[] = [
  { kind: 'l6', label: 'L-6 Verificación', format: 'xlsx', available: true, route: '' },
  { kind: 'l7', label: 'L-7 Solicitud', format: 'xlsx', available: true, route: '' },
  { kind: 'l8', label: 'L-8 Mantenimiento', format: 'xlsx', available: true, route: '' },
  { kind: 'l3', label: 'L-3 Salida', format: 'xlsx', available: true, route: '' },
  { kind: 'l48', label: 'L-48 Planificación', format: 'xlsx', available: true, route: '' },
  { kind: 'l12', label: 'L-12 Adquisición', format: 'xlsx', available: true, route: '' },
];

function iso(value: Date | string | null | undefined): string {
  if (value === null || value === undefined || value === '') return '';
  return value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10);
}

function upper(value: string | null | undefined, fallback = ''): string {
  return (value ?? fallback).trim().toUpperCase();
}

function number(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function condition(value: number | null | undefined): string {
  return (
    (
      {
        1: 'EXCELENTE',
        2: 'BUENO',
        3: 'REGULAR',
        4: 'MALO',
        5: 'BAJA',
      } as Record<number, string>
    )[value ?? 0] ?? 'N/A'
  );
}

function status(value: number | null | undefined): string {
  return (
    (
      {
        0: 'BORRADOR',
        1: 'PENDIENTE',
        2: 'COMPLETADO',
        3: 'ANULADO',
        4: 'APROBADO',
        5: 'RECHAZADO',
        99: 'ELIMINADO',
      } as Record<number, string>
    )[value ?? 0] ?? 'PENDIENTE'
  );
}

function safeSheetName(value: string): string {
  const cleaned =
    value
      .replace(/[\\/?*:]/g, '_')
      .replaceAll('[', '_')
      .replaceAll(']', '_')
      .trim() || 'Reporte';
  return cleaned.slice(0, 31);
}

@Injectable()
export class ReportService {
  constructor(
    private readonly router: TenantRouter,
    private readonly pools: TenantPgPoolRegistry,
  ) {}

  manifest(): readonly ReportManifestItem[] {
    return MANIFEST.map((item) => ({
      ...item,
      route: `/api/v1/reports/download/${item.kind}`,
    }));
  }

  private async tenant(context: SiteRequestContext): Promise<IPgPool> {
    if (context.siteRole !== SiteRole.Supervisor && context.siteRole !== SiteRole.Administrador) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    return this.pools.get(await this.router.resolve(context));
  }

  private async template(name: string): Promise<ExcelJS.Workbook> {
    const candidates = [
      resolve(process.cwd(), 'wwwroot', 'templates', name),
      resolve(process.cwd(), '..', '..', 'wwwroot', 'templates', name),
      join(import.meta.dirname, '..', '..', '..', '..', 'wwwroot', 'templates', name),
    ];
    for (const candidate of candidates) {
      try {
        await access(candidate, constants.R_OK);
        const workbook = new ExcelJS.Workbook();
        await workbook.xlsx.readFile(candidate);
        return workbook;
      } catch {
        // Continue with the next repository/runtime layout.
      }
    }
    throw new NotFoundException(`Plantilla ${name} no disponible.`);
  }

  private async finish(workbook: ExcelJS.Workbook, filename: string): Promise<GeneratedReport> {
    const raw = await workbook.xlsx.writeBuffer();
    return {
      buffer: Buffer.isBuffer(raw) ? raw : Buffer.from(raw),
      filename,
      contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    };
  }

  private cellText(worksheet: ExcelJS.Worksheet, row: number, column: number): string {
    const value = worksheet.getCell(row, column).value;
    if (value === null || value === undefined) return '';
    return String(
      typeof value === 'object'
        ? ((value as { richText?: readonly { text: string }[] }).richText
            ?.map((item) => item.text)
            .join('') ?? value)
        : value,
    );
  }

  private findLabel(
    worksheet: ExcelJS.Worksheet,
    matcher: (value: string) => boolean,
    maxRow = 80,
    maxColumn = 30,
  ): { readonly row: number; readonly column: number } | null {
    for (let row = 1; row <= maxRow; row += 1) {
      for (let column = 1; column <= maxColumn; column += 1) {
        if (matcher(this.cellText(worksheet, row, column).trim().toUpperCase())) {
          return { row, column };
        }
      }
    }
    return null;
  }

  private clearRows(
    worksheet: ExcelJS.Worksheet,
    start: number,
    end: number,
    columns: number,
  ): void {
    for (let row = start; row <= end; row += 1) {
      for (let column = 1; column <= columns; column += 1) {
        worksheet.getCell(row, column).value = null;
      }
    }
  }

  private async plan(pool: IPgPool, siteId: string, planId: number): Promise<ReportRow> {
    const result = await pool.query<ReportRow>(
      `SELECT p.id, p.management_id, p.current_phase, p.current_state, p.plan_status,
              p.is_draft, p.planned_date, m.code AS management_code, m.type AS management_type,
              u.inventory_number, u.serial_number, e.name AS equipment_name, e.brand, e.model,
              l.name AS laboratory_name
         FROM public.lu_management_plan p
         LEFT JOIN public.lu_management m ON m.id = p.management_id AND m.site_id = p.site_id
         LEFT JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
         LEFT JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = u.site_id
         LEFT JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = u.site_id
        WHERE p.site_id = $1 AND p.id = $2 AND p.equipment_unit_id IS NOT NULL
        LIMIT 1`,
      [siteId, planId],
    );
    const row = result.rows[0];
    if (row === undefined) throw new NotFoundException('Plan de gestión no encontrado.');
    return row;
  }

  private async l6(
    context: SiteRequestContext,
    managementId: number,
    laboratoryId: number,
  ): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const result = await pool.query<ReportRow>(
      `SELECT p.id, p.current_phase, p.current_state, p.plan_status,
              p.is_draft, e.name AS equipment_name, e.brand, u.inventory_number,
              l.name AS laboratory_name, v.verification_date, v.physical_condition,
              v.observations,
              string_agg(vf.description, '; ' ORDER BY vf.id) FILTER (WHERE vf.is_deleted = false) AS faults
         FROM public.lu_management_plan p
         JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
         JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = u.site_id
         JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = u.site_id
         LEFT JOIN public.lu_verification v ON v.management_id = p.management_id
            AND v.equipment_unit_id = p.equipment_unit_id AND v.site_id = p.site_id AND v.status <> 99
         LEFT JOIN public.lu_verification_fault vf ON vf.verification_id = v.id AND vf.site_id = v.site_id
        WHERE p.site_id = $1 AND p.management_id = $2 AND u.laboratory_id = $3
        GROUP BY p.id, u.inventory_number, e.name, e.brand, l.name, v.verification_date,
                 v.physical_condition, v.observations
        ORDER BY u.inventory_number, p.id`,
      [context.siteId, managementId, laboratoryId],
    );
    const workbook = await this.template('L6V2.xlsx');
    const worksheet = workbook.worksheets[0]!;
    const labLabel = result.rows[0]?.laboratory_name ?? `Laboratorio ${laboratoryId}`;
    worksheet.name = safeSheetName(labLabel);
    const metadata = [
      [/(^|:)LABORATORIO:?$/, labLabel],
      [/^RESPONSABLE:?$/, context.userId],
      [/^FECHA VERIFICACI[ÓO]N/, new Date().toLocaleDateString('es-BO')],
    ] as const;
    for (const [matcher, value] of metadata) {
      const found = this.findLabel(worksheet, (text) => matcher.test(text));
      if (found) worksheet.getCell(found.row, found.column + 2).value = value;
    }
    const header = this.findLabel(worksheet, (text) => text.includes('DESCRIPCI'));
    const headerRow = header?.row ?? 12;
    const columns = { description: 2, inventory: 3, brand: 4, condition: 5, observations: 6 };
    for (let column = 1; column <= 8; column += 1) {
      const text = this.cellText(worksheet, headerRow, column).toUpperCase();
      if (text.includes('DESCRIPCI')) columns.description = column;
      else if (text.includes('INV')) columns.inventory = column;
      else if (text.includes('MARCA')) columns.brand = column;
      else if (text.includes('ESTADO')) columns.condition = column;
      else if (text.includes('OBSERVACION')) columns.observations = column;
    }
    const start = headerRow + 1;
    this.clearRows(worksheet, start, start + 40, 8);
    result.rows.forEach((row, index) => {
      const current = start + index;
      worksheet.getCell(current, 1).value = index + 1;
      worksheet.getCell(current, columns.description).value = upper(row.equipment_name, 'EQUIPO');
      worksheet.getCell(current, columns.inventory).value = row.inventory_number ?? '';
      worksheet.getCell(current, columns.brand).value = upper(row.brand, 'S/M');
      worksheet.getCell(current, columns.condition).value = condition(row.physical_condition);
      worksheet.getCell(current, columns.observations).value = upper(
        row.faults ?? row.observations,
        'SIN OBSERVACIONES',
      );
    });
    return this.finish(workbook, `Verificacion_L6_${managementId}_Lab_${laboratoryId}.xlsx`);
  }

  private async l7(context: SiteRequestContext, requestId: number): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const result = await pool.query<ReportRow>(
      `SELECT r.id AS request_id, r.description, r.priority, r.request_date,
              r.estimated_repair_time, r.observations, r.investment_code,
              r.cost_center, r.created_at, e.name AS equipment_name, e.brand, e.model,
              u.serial_number, u.inventory_number, l.name AS laboratory_name
         FROM public.lu_request r
         LEFT JOIN public.lu_equipment_unit u ON u.id = r.equipment_unit_id AND u.site_id = r.site_id
         LEFT JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = u.site_id
         LEFT JOIN public.lu_laboratory l ON l.id = COALESCE(r.laboratory_id, u.laboratory_id) AND l.site_id = r.site_id
        WHERE r.site_id = $1 AND r.id = $2 AND r.type = 1 AND r.status <> 99
        LIMIT 1`,
      [context.siteId, requestId],
    );
    const row = result.rows[0];
    if (row === undefined) throw new NotFoundException('Solicitud L-7 no encontrada.');
    const workbook = await this.template('L7.xlsx');
    const worksheet = workbook.worksheets[0]!;
    const cells: Record<string, string | number> = {
      D4: `RE-10-LAB-${String(requestId).padStart(3, '0')}`,
      B11: upper(row.equipment_name),
      B12: upper(row.laboratory_name),
      D12: iso(row.request_date),
      B17: upper(row.brand),
      D17: upper(row.serial_number),
      B18: upper(row.model),
      B19: row.inventory_number ?? '',
      A23: row.description ?? 'Sin descripción',
      A28: row.observations ?? 'Sin observaciones',
      A33: 'AÑOS: PENDIENTE',
      A35: `TIEMPO ESTIMADO DE REPARACIÓN: ${row.estimated_repair_time ?? ''}`,
    };
    for (const [address, value] of Object.entries(cells)) worksheet.getCell(address).value = value;
    return this.finish(workbook, `Solicitud_L7_${requestId}.xlsx`);
  }

  private async l8(context: SiteRequestContext, planId: number): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const plan = await this.plan(pool, context.siteId, planId);
    const maintenance = await pool.query<ReportRow>(
      `SELECT m.id AS maintenance_id, m.status AS maintenance_status, m.scheduled_date,
              m.start_date, m.end_date, m.actual_cost, m.satisfaction_level,
              m.description, m.recommendations, t.description AS task_description,
              t.is_completed AS task_completed, c.concept AS cost_concept,
              c.quantity AS cost_quantity, c.unit_price AS cost_unit_price
         FROM public.lu_maintenance m
         LEFT JOIN public.lu_maintenance_task t ON t.maintenance_id = m.id AND t.site_id = m.site_id AND t.is_deleted = false
         LEFT JOIN public.lu_maintenance_cost c ON c.maintenance_id = m.id AND c.site_id = m.site_id AND c.is_deleted = false
        WHERE m.site_id = $1 AND m.management_id = $2 AND m.equipment_unit_id = (
          SELECT equipment_unit_id FROM public.lu_management_plan WHERE site_id = $1 AND id = $3
        )
        ORDER BY m.id, t.id, c.id`,
      [context.siteId, plan.management_id, planId],
    );
    const workbook = await this.template('L8.xlsx');
    const worksheet = workbook.worksheets[0]!;
    worksheet.getCell('B8').value = upper(plan.equipment_name);
    worksheet.getCell('B9').value = upper(plan.laboratory_name);
    worksheet.getCell('B10').value = upper(plan.brand);
    worksheet.getCell('B11').value = upper(plan.model);
    worksheet.getCell('B12').value = upper(plan.serial_number);
    worksheet.getCell('B13').value = plan.inventory_number ?? '';
    this.clearRows(worksheet, 15, 70, 8);
    maintenance.rows.forEach((row, index) => {
      const current = 15 + index;
      worksheet.getCell(current, 1).value = iso(row.scheduled_date);
      worksheet.getCell(current, 2).value = row.description ?? row.task_description ?? '';
      worksheet.getCell(current, 3).value = row.task_description ?? '';
      worksheet.getCell(current, 4).value = row.task_completed === true ? 'COMPLETA' : 'PENDIENTE';
      worksheet.getCell(current, 5).value = row.actual_cost === null ? '' : number(row.actual_cost);
      worksheet.getCell(current, 6).value = status(row.maintenance_status);
    });
    return this.finish(workbook, `Kardex_L8_${planId}.xlsx`);
  }

  private async l3(context: SiteRequestContext, departureId: number): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const result = await pool.query<ReportRow>(
      `SELECT d.id AS departure_id, d.departure_date, d.destination, d.status AS departure_status,
              d.departure_observations, d.return_observations, l.name AS laboratory_name,
              u.inventory_number, e.name AS equipment_name, i.product_name, i.quantity,
              i.unit_of_measure, i.returned_quantity
         FROM public.lu_departure d
         LEFT JOIN public.lu_equipment_unit u ON u.id = d.equipment_unit_id AND u.site_id = d.site_id
         LEFT JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = d.site_id
         LEFT JOIN public.lu_laboratory l ON l.id = d.origin_laboratory_id AND l.site_id = d.site_id
         LEFT JOIN public.lu_departure_item i ON i.departure_id = d.id AND i.site_id = d.site_id AND i.is_removed = false
        WHERE d.site_id = $1 AND d.id = $2
        ORDER BY i.id`,
      [context.siteId, departureId],
    );
    const first = result.rows[0];
    if (first === undefined) throw new NotFoundException('Salida L-3 no encontrada.');
    const workbook = await this.template('L3.xlsx');
    const worksheet = workbook.worksheets[0]!;
    const labels: Record<string, string> = {
      'LABORATORIO:': upper(first.laboratory_name),
      'LABORATORIO DE:': upper(first.laboratory_name),
      'ENTREGADO POR': `ENTREGADO POR: ${context.userId}`,
      'GESTIÓN:': `GESTIÓN: ${iso(first.departure_date)}`,
      'GESTION:': `GESTIÓN: ${iso(first.departure_date)}`,
      'NRO. DE SOLICITUD': `NRO. DE SOLICITUD: L3-${String(departureId).padStart(5, '0')}`,
      'NRO DE SOLICITUD': `NRO. DE SOLICITUD: L3-${String(departureId).padStart(5, '0')}`,
    };
    for (const [label, value] of Object.entries(labels)) {
      const found = this.findLabel(worksheet, (text) => text.startsWith(label));
      if (found)
        worksheet.getCell(
          found.row,
          found.column + (label.startsWith('LABORATORIO') ? 1 : 0),
        ).value = value;
    }
    const header = this.findLabel(worksheet, (text) => text.includes('PRODUCTO'));
    const headerRow = header?.row ?? 18;
    const columns = { product: 1, quantity: 2, unit: 3, returned: 4, balance: 5 };
    for (let column = 1; column <= 10; column += 1) {
      const text = this.cellText(worksheet, headerRow, column);
      if (text.includes('PRODUCTO')) columns.product = column;
      else if (text.includes('CANTIDAD')) columns.quantity = column;
      else if (text.includes('UNIDAD')) columns.unit = column;
      else if (text.includes('DEVOLUCION') || text.includes('DEVUELTO')) columns.returned = column;
      else if (text.includes('SALDO')) columns.balance = column;
    }
    this.clearRows(worksheet, headerRow + 1, headerRow + 30, 8);
    result.rows.forEach((row, index) => {
      const current = headerRow + 1 + index;
      worksheet.getCell(current, columns.product).value = upper(
        row.product_name,
        row.equipment_name ?? 'EQUIPO PRINCIPAL',
      );
      worksheet.getCell(current, columns.quantity).value = row.quantity ?? 1;
      worksheet.getCell(current, columns.unit).value = upper(row.unit_of_measure, 'UNIDAD');
      worksheet.getCell(current, columns.returned).value = row.returned_quantity ?? 0;
      worksheet.getCell(current, columns.balance).value =
        (row.quantity ?? 1) - (row.returned_quantity ?? 0);
    });
    return this.finish(workbook, `Salida_L3_${departureId}.xlsx`);
  }

  private async l48(
    context: SiteRequestContext,
    managementId: number,
    laboratoryId: number,
  ): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const result = await pool.query<ReportRow>(
      `SELECT p.id, p.planned_date, p.current_phase, p.current_state, p.plan_status,
              u.inventory_number, e.name AS equipment_name, l.name AS laboratory_name,
              m.scheduled_date, m.start_date, m.end_date, m.status AS maintenance_status,
              m.observations, m.actual_cost
         FROM public.lu_management_plan p
         JOIN public.lu_equipment_unit u ON u.id = p.equipment_unit_id AND u.site_id = p.site_id
         JOIN public.lu_equipment e ON e.id = u.equipment_id AND e.site_id = p.site_id
         JOIN public.lu_laboratory l ON l.id = u.laboratory_id AND l.site_id = p.site_id
         LEFT JOIN public.lu_maintenance m ON m.management_id = p.management_id
            AND m.equipment_unit_id = p.equipment_unit_id AND m.site_id = p.site_id AND m.status <> 99
        WHERE p.site_id = $1 AND p.management_id = $2 AND u.laboratory_id = $3
        ORDER BY u.inventory_number, p.id`,
      [context.siteId, managementId, laboratoryId],
    );
    const workbook = await this.template('L48.xlsx');
    const worksheet = workbook.worksheets[0]!;
    worksheet.getCell('A5').value = `PLAN DE MANTENIMIENTO · GESTIÓN ${managementId}`;
    this.clearRows(worksheet, 13, 80, 22);
    result.rows.forEach((row, index) => {
      const current = 13 + index;
      const values = [
        index + 1,
        upper(row.equipment_name),
        row.inventory_number ?? '',
        row.maintenance_status === null ? 'PLANIFICADO' : status(row.maintenance_status),
        row.scheduled_date ? iso(row.scheduled_date) : iso(row.planned_date),
        iso(row.start_date),
        iso(row.end_date),
        number(row.actual_cost),
        row.observations ?? '',
      ];
      values.forEach((value, column) => {
        worksheet.getCell(current, column + 1).value = value;
      });
    });
    return this.finish(workbook, `Plan_Gantt_L48_${managementId}_Lab_${laboratoryId}.xlsx`);
  }

  private async l12(context: SiteRequestContext, requestId: number): Promise<GeneratedReport> {
    const pool = await this.tenant(context);
    const result = await pool.query<ReportRow>(
      `SELECT r.id AS request_id, r.description, r.investment_code, r.cost_center,
              r.request_date, r.observations, l.name AS laboratory_name,
              c.concept AS cost_concept, c.description AS cost_description,
              c.quantity AS cost_quantity, c.unit_of_measure, c.unit_price
         FROM public.lu_request r
         LEFT JOIN public.lu_laboratory l ON l.id = r.laboratory_id AND l.site_id = r.site_id
         LEFT JOIN public.lu_request_cost c ON c.request_id = r.id AND c.site_id = r.site_id AND c.is_deleted = false
        WHERE r.site_id = $1 AND r.id = $2 AND r.type = 2 AND r.status <> 99
        ORDER BY c.id`,
      [context.siteId, requestId],
    );
    const row = result.rows[0];
    if (row === undefined) throw new NotFoundException('Adquisición L-12 no encontrada.');
    const workbook = await this.template('Adquisicion.xlsx');
    const worksheet = workbook.worksheets[0]!;
    worksheet.getCell('D6').value = upper(row.laboratory_name, 'GENERAL');
    worksheet.getCell('D7').value = upper(row.cost_center);
    worksheet.getCell('D8').value = context.userId;
    worksheet.getCell('D9').value = row.investment_code ?? '';
    worksheet.getCell('Q8').value = requestId;
    const date = row.request_date ? new Date(row.request_date) : new Date();
    worksheet.getCell('D10').value = date.getUTCDate();
    worksheet.getCell('F10').value = date.getUTCMonth() + 1;
    worksheet.getCell('H10').value = date.getUTCFullYear();
    worksheet.getCell('C14').value = row.description ?? '';
    this.clearRows(worksheet, 19, 55, 26);
    let total = 0;
    result.rows.forEach((cost, index) => {
      if (19 + index >= 38) return;
      const current = 19 + index;
      const quantity = number(cost.cost_quantity) ?? 1;
      const unitPrice = number(cost.unit_price) ?? 0;
      const subtotal = quantity * unitPrice;
      total += subtotal;
      worksheet.getCell(current, 1).value = quantity;
      worksheet.getCell(current, 3).value = cost.unit_of_measure ?? 'UNIDAD';
      worksheet.getCell(current, 5).value = cost.cost_concept ?? cost.cost_description ?? '';
      worksheet.getCell(current, 14).value = unitPrice;
      worksheet.getCell(current, 17).value = subtotal;
    });
    worksheet.getCell('A38').value = 'Son:';
    worksheet.getCell('Q38').value = total;
    worksheet.getCell('Q39').value = 'Monto TOTAL';
    return this.finish(workbook, `Adquisicion_${requestId}.xlsx`);
  }

  async download(
    context: SiteRequestContext,
    kind: ReportKind,
    values: {
      readonly managementId?: number;
      readonly laboratoryId?: number;
      readonly planId?: number;
      readonly requestId?: number;
      readonly departureId?: number;
    },
  ): Promise<GeneratedReport> {
    switch (kind) {
      case 'l6':
        if (values.managementId === undefined || values.laboratoryId === undefined)
          throw new NotFoundException('L-6 requiere managementId y laboratoryId.');
        return this.l6(context, values.managementId, values.laboratoryId);
      case 'l7':
        if (values.requestId === undefined) throw new NotFoundException('L-7 requiere requestId.');
        return this.l7(context, values.requestId);
      case 'l8':
        if (values.planId === undefined) throw new NotFoundException('L-8 requiere planId.');
        return this.l8(context, values.planId);
      case 'l3':
        if (values.departureId === undefined)
          throw new NotFoundException('L-3 requiere departureId.');
        return this.l3(context, values.departureId);
      case 'l48':
        if (values.managementId === undefined || values.laboratoryId === undefined)
          throw new NotFoundException('L-48 requiere managementId y laboratoryId.');
        return this.l48(context, values.managementId, values.laboratoryId);
      case 'l12':
        if (values.requestId === undefined) throw new NotFoundException('L-12 requiere requestId.');
        return this.l12(context, values.requestId);
    }
  }
}
