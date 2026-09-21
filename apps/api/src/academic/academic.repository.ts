import { Inject, Injectable } from '@nestjs/common';
import type {
  AcademicQuery,
  Career,
  CreateCareerInput,
  CreateFacultyInput,
  CreateLaboratoryInput,
  Faculty,
  Laboratory,
  SiteCareer,
  UpdateCareerInput,
  UpdateFacultyInput,
  UpdateLaboratoryInput,
  CatalogPage,
} from '@lu/contracts';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import type { CatalogStatus } from '@lu/contracts';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface CountRow {
  total_count: number | string;
}
interface FacultyRow {
  id: number | string;
  name: string;
  code: string | null;
  description: string | null;
  status: number;
  laboratory_count: number | string;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface CareerRow {
  id: number | string;
  code: string | null;
  name: string;
  faculty_id: number | string | null;
  faculty_name: string | null;
  status: number;
  site_assigned: boolean;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface SiteCareerRow {
  career_id: number | string;
  career_name: string;
  site_id: string;
  status: number;
}
interface LaboratoryRow {
  id: number | string;
  faculty_id: number | string;
  code: string;
  name: string;
  type: string | null;
  building: string | null;
  block: string | null;
  floor: string | null;
  room: string | null;
  description: string | null;
  city_id: number | string | null;
  city_name: string | null;
  status: number;
  created_at: Date | string;
  updated_at: Date | string | null;
}

function integer(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function status(value: number): CatalogStatus {
  return value === 1 ? 1 : value === 2 ? 2 : 0;
}
function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function nullableIso(value: Date | string | null): string | null {
  return value === null ? null : iso(value);
}
function clean(value: string, max: number, field: string): string {
  const result = value.trim().replace(/\s+/g, ' ');
  if (result.length < 2 || result.length > max)
    throw new CatalogConflictError(`${field} must contain between 2 and ${max} characters.`, field);
  return result;
}
function optionalText(value: string | null | undefined, max: number, field: string): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > max)
    throw new CatalogConflictError(`${field} must not exceed ${max} characters.`, field);
  return result === '' ? null : result;
}
function isUnique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}
function page(query: AcademicQuery): { page: number; size: number; offset: number } {
  const current = Math.max(1, query.currentPage ?? 1);
  const size = 20;
  return { page: current, size, offset: (current - 1) * size };
}
function mapFaculty(row: FacultyRow, facultyNameCount = row.laboratory_count): Faculty {
  return {
    id: integer(row.id),
    name: row.name,
    code: row.code,
    description: row.description,
    status: status(row.status),
    laboratoryCount: integer(facultyNameCount),
    createdAt: iso(row.created_at),
    updatedAt: nullableIso(row.updated_at),
  };
}
function mapCareer(row: CareerRow): Career {
  return {
    id: integer(row.id),
    code: row.code,
    name: row.name,
    facultyId: row.faculty_id === null ? null : integer(row.faculty_id),
    facultyName: row.faculty_name,
    status: status(row.status),
    siteAssigned: row.site_assigned,
    createdAt: iso(row.created_at),
    updatedAt: nullableIso(row.updated_at),
  };
}
function mapLaboratory(row: LaboratoryRow, facultyName: string | null): Laboratory {
  return {
    id: integer(row.id),
    facultyId: integer(row.faculty_id),
    facultyName,
    code: row.code,
    name: row.name,
    type: row.type,
    building: row.building,
    block: row.block,
    floor: row.floor,
    room: row.room,
    description: row.description,
    cityId: row.city_id === null ? null : integer(row.city_id),
    cityName: row.city_name,
    status: status(row.status),
    createdAt: iso(row.created_at),
    updatedAt: nullableIso(row.updated_at),
  };
}

const facultySelect = `SELECT f.id, f.name, f.code, f.description, f.status, f.created_at, f.updated_at,
  0::int AS laboratory_count FROM public.lu_faculty f`;
const careerSelect = `SELECT c.id, c.code, c.name, c.faculty_id, f.name AS faculty_name, c.status,
  EXISTS (SELECT 1 FROM public.lu_site_career sc WHERE sc.career_id = c.id AND sc.site_id = $1 AND sc.status = 0) AS site_assigned,
  c.created_at, c.updated_at FROM public.lu_career c LEFT JOIN public.lu_faculty f ON f.id = c.faculty_id`;
const laboratorySelect = `SELECT l.id, l.faculty_id, l.code, l.name, l.type, l.building, l.block, l.floor,
  l.room, l.description, l.city_id, ci.name AS city_name, l.status, l.created_at, l.updated_at
  FROM public.lu_laboratory l LEFT JOIN public.lu_city ci ON ci.site_id = l.site_id AND ci.id = l.city_id`;

@Injectable()
export class AcademicRepository {
  constructor(@Inject(AUTH_PG_POOL) private readonly control: IPgPool) {}

  async listFaculties(query: AcademicQuery): Promise<CatalogPage<Faculty>> {
    const { page: pageIndex, size, offset } = page(query);
    const params: unknown[] = [];
    const filters = ['f.status <> 2'];
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(`(f.name ILIKE $${params.length} OR f.code ILIKE $${params.length})`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`f.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await this.control.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_faculty f WHERE ${where}`,
      params,
    );
    const total = integer(count.rows[0]?.total_count ?? 0);
    const rows = await this.control.query<FacultyRow>(
      `${facultySelect} WHERE ${where} ORDER BY f.name, f.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    return {
      items: rows.rows.map((row) => mapFaculty(row)),
      totalCount: total,
      pageIndex,
      totalPages: total === 0 ? 0 : Math.ceil(total / size),
      pageSize: size,
    };
  }

  async findFaculty(id: number): Promise<Faculty | null> {
    const rows = await this.control.query<FacultyRow>(`${facultySelect} WHERE f.id = $1`, [id]);
    return rows.rows[0] === undefined ? null : mapFaculty(rows.rows[0]);
  }

  async createFaculty(userId: string, input: CreateFacultyInput): Promise<Faculty> {
    const name = clean(input.name, 200, 'name');
    const code = optionalText(input.code, 50, 'code');
    const description = optionalText(input.description, 500, 'description');
    try {
      const result = await this.control.query<FacultyRow>(
        `INSERT INTO public.lu_faculty (name, code, description, status, created_by_id) VALUES ($1, $2, $3, 0, $4) RETURNING id, name, code, description, status, created_at, updated_at, 0::int AS laboratory_count`,
        [name, code?.toUpperCase() ?? null, description, userId],
      );
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('Faculty was not created.');
      return mapFaculty(row);
    } catch (error) {
      if (isUnique(error))
        throw new CatalogConflictError('A faculty with this name or code already exists.', 'name');
      throw error;
    }
  }

  async updateFaculty(userId: string, id: number, input: UpdateFacultyInput): Promise<Faculty> {
    const name = clean(input.name, 200, 'name');
    const code = optionalText(input.code, 50, 'code');
    const description = optionalText(input.description, 500, 'description');
    try {
      const result = await this.control.query<FacultyRow>(
        `UPDATE public.lu_faculty SET name = $2, code = $3, description = $4, status = $5, updated_by_id = $6, updated_at = CURRENT_TIMESTAMP WHERE id = $1 AND status <> 2 RETURNING id, name, code, description, status, created_at, updated_at, 0::int AS laboratory_count`,
        [id, name, code?.toUpperCase() ?? null, description, input.status, userId],
      );
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('Faculty was not found.');
      return mapFaculty(row);
    } catch (error) {
      if (isUnique(error))
        throw new CatalogConflictError('A faculty with this name or code already exists.', 'name');
      throw error;
    }
  }

  async deleteFaculty(userId: string, id: number): Promise<void> {
    const result = await this.control.query(
      `UPDATE public.lu_faculty SET status = 2, updated_by_id = $2, updated_at = CURRENT_TIMESTAMP WHERE id = $1 AND status <> 2`,
      [id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Faculty was not found.');
  }

  async listCareers(siteId: string, query: AcademicQuery): Promise<CatalogPage<Career>> {
    const { page: pageIndex, size, offset } = page(query);
    const params: unknown[] = [siteId];
    const filters = ['c.status <> 2'];
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(c.name ILIKE $${params.length} OR c.code ILIKE $${params.length} OR f.name ILIKE $${params.length})`,
      );
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`c.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await this.control.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_career c LEFT JOIN public.lu_faculty f ON f.id = c.faculty_id WHERE $1::uuid IS NOT NULL AND ${where}`,
      params,
    );
    const total = integer(count.rows[0]?.total_count ?? 0);
    const rows = await this.control.query<CareerRow>(
      `${careerSelect} WHERE ${where} ORDER BY c.name, c.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    return {
      items: rows.rows.map(mapCareer),
      totalCount: total,
      pageIndex,
      totalPages: total === 0 ? 0 : Math.ceil(total / size),
      pageSize: size,
    };
  }

  async findCareer(siteId: string, id: number): Promise<Career | null> {
    const result = await this.control.query<CareerRow>(`${careerSelect} WHERE c.id = $2`, [
      siteId,
      id,
    ]);
    return result.rows[0] === undefined ? null : mapCareer(result.rows[0]);
  }
  async createCareer(userId: string, siteId: string, input: CreateCareerInput): Promise<Career> {
    const name = clean(input.name, 200, 'name');
    const code = optionalText(input.code, 30, 'code');
    try {
      const result = await this.control.transaction(async (client) => {
        const inserted = await client.query<CareerRow>(
          `INSERT INTO public.lu_career (code, name, faculty_id, status, created_by_id) VALUES ($1, $2, $3, $4, $5) RETURNING id`,
          [code?.toUpperCase() ?? null, name, input.facultyId ?? null, input.status ?? 0, userId],
        );
        const row = inserted.rows[0];
        if (row === undefined) throw new CatalogMissingError('Career was not created.');
        if (input.status === undefined || input.status === 0)
          await client.query(
            `INSERT INTO public.lu_site_career (site_id, career_id, status, created_by_id) VALUES ($1, $2, 0, $3) ON CONFLICT (site_id, career_id) DO UPDATE SET status = 0, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP`,
            [siteId, row.id, userId],
          );
        return row.id;
      });
      return (await this.findCareer(siteId, integer(result)))!;
    } catch (error) {
      if (isUnique(error))
        throw new CatalogConflictError('A career with this name or code already exists.', 'name');
      throw error;
    }
  }
  async updateCareer(
    userId: string,
    siteId: string,
    id: number,
    input: UpdateCareerInput,
  ): Promise<Career> {
    const name = clean(input.name, 200, 'name');
    const code = optionalText(input.code, 30, 'code');
    try {
      const result = await this.control.query<CareerRow>(
        `UPDATE public.lu_career SET code = $2, name = $3, faculty_id = $4, status = $5, updated_by_id = $6, updated_at = CURRENT_TIMESTAMP WHERE id = $1 AND status <> 2 RETURNING id`,
        [id, code?.toUpperCase() ?? null, name, input.facultyId ?? null, input.status, userId],
      );
      if (result.rows[0] === undefined) throw new CatalogMissingError('Career was not found.');
      await this.control.query(
        `INSERT INTO public.lu_site_career (site_id, career_id, status, updated_by_id, updated_at) VALUES ($1, $2, $3, $4, CURRENT_TIMESTAMP) ON CONFLICT (site_id, career_id) DO UPDATE SET status = $3, updated_by_id = $4, updated_at = CURRENT_TIMESTAMP`,
        [siteId, id, input.status, userId],
      );
      return (await this.findCareer(siteId, id))!;
    } catch (error) {
      if (isUnique(error))
        throw new CatalogConflictError('A career with this name or code already exists.', 'name');
      throw error;
    }
  }
  async deleteCareer(userId: string, siteId: string, id: number): Promise<void> {
    const result = await this.control.query(
      `UPDATE public.lu_career SET status = 2, updated_by_id = $2, updated_at = CURRENT_TIMESTAMP WHERE id = $1 AND status <> 2`,
      [id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Career was not found.');
    await this.control.query(
      `UPDATE public.lu_site_career SET status = 2, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP WHERE site_id = $1 AND career_id = $2`,
      [siteId, id, userId],
    );
  }
  async listSiteCareers(siteId: string): Promise<readonly SiteCareer[]> {
    const result = await this.control.query<SiteCareerRow>(
      `SELECT sc.career_id, c.name AS career_name, sc.site_id, sc.status FROM public.lu_site_career sc JOIN public.lu_career c ON c.id = sc.career_id WHERE sc.site_id = $1 AND sc.status <> 2 ORDER BY c.name`,
      [siteId],
    );
    return result.rows.map((row) => ({
      careerId: integer(row.career_id),
      careerName: row.career_name,
      siteId: row.site_id,
      status: status(row.status),
    }));
  }
  async assignSiteCareer(userId: string, siteId: string, careerId: number): Promise<void> {
    const career = await this.control.query(
      `SELECT id FROM public.lu_career WHERE id = $1 AND status = 0`,
      [careerId],
    );
    if (career.rows[0] === undefined)
      throw new CatalogConflictError('The selected career is not active.', 'careerId');
    await this.control.query(
      `INSERT INTO public.lu_site_career (site_id, career_id, status, created_by_id) VALUES ($1, $2, 0, $3) ON CONFLICT (site_id, career_id) DO UPDATE SET status = 0, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP`,
      [siteId, careerId, userId],
    );
  }
  async unassignSiteCareer(userId: string, siteId: string, careerId: number): Promise<void> {
    const result = await this.control.query(
      `UPDATE public.lu_site_career SET status = 2, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP WHERE site_id = $1 AND career_id = $2 AND status <> 2`,
      [siteId, careerId, userId],
    );
    if (result.rowCount !== 1)
      throw new CatalogMissingError('Site career assignment was not found.');
  }

  async facultyNames(ids: readonly number[]): Promise<ReadonlyMap<number, string>> {
    if (ids.length === 0) return new Map();
    const result = await this.control.query<{ id: number | string; name: string }>(
      `SELECT id, name FROM public.lu_faculty WHERE id = ANY($1::bigint[])`,
      [ids],
    );
    return new Map(result.rows.map((row) => [integer(row.id), row.name]));
  }

  async listLaboratories(
    pool: IPgPool,
    siteId: string,
    query: AcademicQuery,
  ): Promise<CatalogPage<Laboratory>> {
    const { page: pageIndex, size, offset } = page(query);
    const params: unknown[] = [siteId];
    const filters = ['l.site_id = $1', 'l.status <> 2'];
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(l.name ILIKE $${params.length} OR l.code ILIKE $${params.length} OR l.type ILIKE $${params.length})`,
      );
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`l.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_laboratory l WHERE ${where}`,
      params,
    );
    const total = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<LaboratoryRow>(
      `${laboratorySelect} WHERE ${where} ORDER BY l.name, l.id LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    const names = await this.facultyNames([
      ...new Set(rows.rows.map((row) => integer(row.faculty_id))),
    ]);
    return {
      items: rows.rows.map((row) => mapLaboratory(row, names.get(integer(row.faculty_id)) ?? null)),
      totalCount: total,
      pageIndex,
      totalPages: total === 0 ? 0 : Math.ceil(total / size),
      pageSize: size,
    };
  }
  async findLaboratory(pool: IPgPool, siteId: string, id: number): Promise<Laboratory | null> {
    const result = await pool.query<LaboratoryRow>(
      `${laboratorySelect} WHERE l.site_id = $1 AND l.id = $2`,
      [siteId, id],
    );
    const row = result.rows[0];
    if (row === undefined) return null;
    const names = await this.facultyNames([integer(row.faculty_id)]);
    return mapLaboratory(row, names.get(integer(row.faculty_id)) ?? null);
  }
  async createLaboratory(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreateLaboratoryInput,
  ): Promise<Laboratory> {
    const code = clean(input.code, 20, 'code').toUpperCase();
    const name = clean(input.name, 200, 'name');
    const values = [
      siteId,
      input.facultyId,
      code,
      name,
      optionalText(input.type, 100, 'type'),
      optionalText(input.building, 100, 'building'),
      optionalText(input.block, 50, 'block'),
      optionalText(input.floor, 50, 'floor'),
      optionalText(input.room, 100, 'room'),
      optionalText(input.description, 1000, 'description'),
      input.cityId ?? null,
      userId,
    ];
    try {
      const result = await pool.query<{ id: number | string }>(
        `INSERT INTO public.lu_laboratory (site_id, faculty_id, code, name, type, building, block, floor, room, description, city_id, status, created_by_id) VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,0,$12) RETURNING id`,
        values,
      );
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('Laboratory was not created.');
      return (await this.findLaboratory(pool, siteId, integer(row.id)))!;
    } catch (error) {
      if (isUnique(error))
        throw new CatalogConflictError(
          'A laboratory with this code or name already exists.',
          'code',
        );
      throw error;
    }
  }
  async updateLaboratory(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateLaboratoryInput,
  ): Promise<Laboratory> {
    const code = clean(input.code, 20, 'code').toUpperCase();
    const name = clean(input.name, 200, 'name');
    const result = await pool.query(
      `UPDATE public.lu_laboratory SET faculty_id=$3, code=$4, name=$5, type=$6, building=$7, block=$8, floor=$9, room=$10, description=$11, city_id=$12, status=$13, updated_by_id=$14, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status <> 2`,
      [
        siteId,
        id,
        input.facultyId,
        code,
        name,
        optionalText(input.type, 100, 'type'),
        optionalText(input.building, 100, 'building'),
        optionalText(input.block, 50, 'block'),
        optionalText(input.floor, 50, 'floor'),
        optionalText(input.room, 100, 'room'),
        optionalText(input.description, 1000, 'description'),
        input.cityId ?? null,
        input.status,
        userId,
      ],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Laboratory was not found.');
    return (await this.findLaboratory(pool, siteId, id))!;
  }
  async deleteLaboratory(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_laboratory SET status=2, updated_by_id=$3, updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status <> 2`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Laboratory was not found.');
  }
}
