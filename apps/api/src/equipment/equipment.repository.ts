import { Injectable } from '@nestjs/common';
import type {
  EquipmentPage,
  EquipmentQuery,
  EquipmentRecord,
  CreateEquipmentInput,
  UpdateEquipmentInput,
} from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

type QueryExecutor = Pick<IPgPool, 'query'>;

interface EquipmentRow {
  id: number | string;
  category: number;
  utensil_type: number | null;
  type_classification: number | null;
  classification_review_status: number;
  other_classification_detail: string | null;
  status: number;
  catalog_code: string | null;
  image_url: string | null;
  country_id: number | string | null;
  country_name: string | null;
  city_id: number | string | null;
  city_name: string | null;
  name: string;
  brand: string | null;
  model: string | null;
  useful_life_years: number | null;
  description: string | null;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface CountRow {
  total_count: number | string;
}
interface NoteRow {
  note: string;
}
function integer(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function status(value: number): 0 | 1 | 2 {
  return value === 1 ? 1 : value === 2 ? 2 : 0;
}
function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function clean(value: string, max: number, field: string): string {
  const result = value.trim().replace(/\s+/g, ' ');
  if (result.length < 1 || result.length > max)
    throw new CatalogConflictError(`${field} is invalid.`, field);
  return result;
}
function optional(value: string | null | undefined, max: number, field: string): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return result === '' ? null : result;
}
function mapped(row: EquipmentRow, notes: readonly string[] = []): EquipmentRecord {
  return {
    id: integer(row.id),
    category: row.category === 1 ? 1 : row.category === 2 ? 2 : 0,
    utensilType: row.utensil_type,
    typeClassification: row.type_classification,
    classificationReviewStatus: row.classification_review_status,
    otherClassificationDetail: row.other_classification_detail,
    status: status(row.status),
    catalogCode: row.catalog_code,
    imageUrl: row.image_url,
    countryId: row.country_id === null ? null : integer(row.country_id),
    countryName: row.country_name,
    cityId: row.city_id === null ? null : integer(row.city_id),
    cityName: row.city_name,
    name: row.name,
    brand: row.brand,
    model: row.model,
    usefulLifeYears: row.useful_life_years,
    description: row.description,
    notes,
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

const select = `SELECT e.id, e.category, e.utensil_type, e.type_classification, e.classification_review_status,
  e.other_classification_detail, e.status, e.catalog_code, e.image_url, e.country_id, co.name AS country_name,
  e.city_id, ci.name AS city_name, e.name, e.brand, e.model, e.useful_life_years, e.description,
  e.created_at, e.updated_at FROM public.lu_equipment e
  LEFT JOIN public.lu_country co ON co.site_id = e.site_id AND co.id = e.country_id
  LEFT JOIN public.lu_city ci ON ci.site_id = e.site_id AND ci.id = e.city_id`;

@Injectable()
export class EquipmentRepository {
  async list(pool: IPgPool, siteId: string, query: EquipmentQuery): Promise<EquipmentPage> {
    const page = Math.max(1, query.currentPage ?? 1);
    const size = 20;
    const offset = (page - 1) * size;
    const params: unknown[] = [siteId];
    const filters = ['e.site_id = $1', 'e.status <> 2'];
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(e.name ILIKE $${params.length} OR e.brand ILIKE $${params.length} OR e.model ILIKE $${params.length} OR e.catalog_code ILIKE $${params.length})`,
      );
    }
    if (query.category !== undefined) {
      params.push(query.category);
      filters.push(`e.category = $${params.length}`);
    }
    if (query.typeClassification !== undefined) {
      params.push(query.typeClassification);
      filters.push(`e.type_classification = $${params.length}`);
    }
    if (query.utensilType !== undefined) {
      params.push(query.utensilType);
      filters.push(`e.utensil_type = $${params.length}`);
    }
    if (query.reviewStatus !== undefined) {
      params.push(query.reviewStatus);
      filters.push(`e.classification_review_status = $${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`e.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_equipment e WHERE ${where}`,
      params,
    );
    const total = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<EquipmentRow>(
      `${select} WHERE ${where} ORDER BY e.name, e.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    return {
      items: rows.rows.map((row) => mapped(row)),
      totalCount: total,
      pageIndex: page,
      totalPages: total === 0 ? 0 : Math.ceil(total / size),
      pageSize: size,
    };
  }
  async find(pool: QueryExecutor, siteId: string, id: number): Promise<EquipmentRecord | null> {
    const result = await pool.query<EquipmentRow>(`${select} WHERE e.site_id=$1 AND e.id=$2`, [
      siteId,
      id,
    ]);
    const row = result.rows[0];
    if (row === undefined) return null;
    const notes = await pool.query<NoteRow>(
      `SELECT note FROM public.lu_equipment_note WHERE site_id=$1 AND equipment_id=$2 ORDER BY id`,
      [siteId, id],
    );
    return mapped(
      row,
      notes.rows.map((note) => note.note),
    );
  }
  private async assertLocation(
    pool: IPgPool,
    siteId: string,
    input: CreateEquipmentInput | UpdateEquipmentInput,
  ): Promise<void> {
    if (input.countryId === null || input.countryId === undefined) {
      if (input.cityId !== null && input.cityId !== undefined)
        throw new CatalogConflictError('City requires a country.', 'cityId');
      return;
    }
    const country = await pool.query(
      `SELECT id FROM public.lu_country WHERE site_id=$1 AND id=$2 AND status=0`,
      [siteId, input.countryId],
    );
    if (country.rows[0] === undefined)
      throw new CatalogConflictError('The selected country is not active.', 'countryId');
    if (input.cityId !== null && input.cityId !== undefined) {
      const city = await pool.query(
        `SELECT id FROM public.lu_city WHERE site_id=$1 AND id=$2 AND country_id=$3 AND status=0`,
        [siteId, input.cityId, input.countryId],
      );
      if (city.rows[0] === undefined)
        throw new CatalogConflictError(
          'The selected city does not belong to the country.',
          'cityId',
        );
    }
  }
  private validate(input: CreateEquipmentInput | UpdateEquipmentInput): {
    code: string;
    name: string;
    model: string | null;
    brand: string | null;
    description: string | null;
    other: string | null;
    notes: readonly string[];
  } {
    if (![0, 1, 2].includes(input.category))
      throw new CatalogConflictError('Category is invalid.', 'category');
    const code = clean(input.catalogCode, 30, 'catalogCode').toUpperCase();
    const name = clean(input.name, 200, 'name');
    const model = optional(input.model, 100, 'model');
    const brand = optional(input.brand, 100, 'brand');
    const description = optional(input.description, 2000, 'description');
    const other = optional(input.otherClassificationDetail, 1000, 'otherClassificationDetail');
    const notes = (input.notes ?? []).map((note) => clean(note, 500, 'notes'));
    if (notes.length > 50)
      throw new CatalogConflictError('No more than 50 notes are allowed.', 'notes');
    if (
      input.usefulLifeYears !== null &&
      input.usefulLifeYears !== undefined &&
      (!Number.isInteger(input.usefulLifeYears) ||
        input.usefulLifeYears < 0 ||
        input.usefulLifeYears > 100)
    )
      throw new CatalogConflictError('Useful life is invalid.', 'usefulLifeYears');
    return { code, name, model, brand, description, other, notes };
  }
  async create(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateEquipmentInput,
  ): Promise<EquipmentRecord> {
    const values = this.validate(input);
    await this.assertLocation(pool, siteId, input);
    try {
      return await pool.transaction(async (client) => {
        const inserted = await client.query<{ id: number | string }>(
          `INSERT INTO public.lu_equipment (site_id, category, utensil_type, type_classification, classification_review_status, other_classification_detail, status, catalog_code, country_id, city_id, name, brand, model, useful_life_years, description, created_by_id) VALUES ($1,$2,$3,$4,2,$5,0,$6,$7,$8,$9,$10,$11,$12,$13,$14) RETURNING id`,
          [
            siteId,
            input.category,
            input.utensilType ?? null,
            input.typeClassification ?? null,
            values.other,
            values.code,
            input.countryId ?? null,
            input.cityId ?? null,
            values.name,
            values.brand,
            values.model,
            input.usefulLifeYears ?? null,
            values.description,
            userId,
          ],
        );
        const row = inserted.rows[0];
        if (row === undefined) throw new CatalogMissingError('Equipment was not created.');
        for (const note of values.notes)
          await client.query(
            `INSERT INTO public.lu_equipment_note (site_id, equipment_id, note, created_by_id) VALUES ($1,$2,$3,$4)`,
            [siteId, row.id, note, userId],
          );
        return (await this.find(client, siteId, integer(row.id)))!;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An equipment with this code or name/model already exists.',
          'catalogCode',
        );
      throw error;
    }
  }
  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateEquipmentInput,
  ): Promise<EquipmentRecord> {
    const values = this.validate(input);
    await this.assertLocation(pool, siteId, input);
    try {
      return await pool.transaction(async (client) => {
        const result = await client.query(
          `UPDATE public.lu_equipment SET category=$3, utensil_type=$4, type_classification=$5, other_classification_detail=$6, status=$7, catalog_code=$8, country_id=$9, city_id=$10, name=$11, brand=$12, model=$13, useful_life_years=$14, description=$15, updated_by_id=$16, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>2`,
          [
            siteId,
            id,
            input.category,
            input.utensilType ?? null,
            input.typeClassification ?? null,
            values.other,
            input.status,
            values.code,
            input.countryId ?? null,
            input.cityId ?? null,
            values.name,
            values.brand,
            values.model,
            input.usefulLifeYears ?? null,
            values.description,
            userId,
          ],
        );
        if (result.rowCount !== 1) throw new CatalogMissingError('Equipment was not found.');
        await client.query(
          `DELETE FROM public.lu_equipment_note WHERE site_id=$1 AND equipment_id=$2`,
          [siteId, id],
        );
        for (const note of values.notes)
          await client.query(
            `INSERT INTO public.lu_equipment_note (site_id, equipment_id, note, created_by_id) VALUES ($1,$2,$3,$4)`,
            [siteId, id, note, userId],
          );
        return (await this.find(client, siteId, id))!;
      });
    } catch (error) {
      if (unique(error))
        throw new CatalogConflictError(
          'An equipment with this code or name/model already exists.',
          'catalogCode',
        );
      throw error;
    }
  }
  async remove(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_equipment SET status=2, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>2`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Equipment was not found.');
  }
}
