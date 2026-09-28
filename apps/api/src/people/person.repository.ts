import { Injectable } from '@nestjs/common';
import type {
  CatalogPage,
  CreatePersonInput,
  PersonCategory,
  PersonPage,
  PersonQuery,
  PersonRecord,
  PersonStatus,
  PersonType,
  UpdatePersonInput,
} from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface PersonRow {
  id: number | string;
  actor_code: string | null;
  person_type: PersonType;
  name: string;
  email: string | null;
  phone_number: string | null;
  is_entity: boolean;
  address: string | null;
  category: number;
  status: number;
  created_at: Date | string;
  updated_at: Date | string | null;
}
interface CountRow {
  total_count: number | string;
}

function integer(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}
function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function map(row: PersonRow): PersonRecord {
  const category = [1, 2, 3, 4, 5, 99].includes(row.category)
    ? (row.category as PersonCategory)
    : 99;
  const status = [0, 1, 2].includes(row.status) ? (row.status as PersonStatus) : 2;
  return {
    id: integer(row.id),
    actorCode: row.actor_code,
    type: row.person_type === 'external' ? 'external' : 'internal',
    name: row.name,
    email: row.email,
    phoneNumber: row.phone_number,
    isEntity: row.is_entity,
    address: row.address,
    category,
    status,
    createdAt: iso(row.created_at),
    updatedAt: row.updated_at === null ? null : iso(row.updated_at),
  };
}
function clean(value: string | null | undefined, max: number, field: string): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > max) throw new CatalogConflictError(`${field} is invalid.`, field);
  return result === '' ? null : result;
}
function name(value: string): string {
  const result = value.trim().replace(/\s+/g, ' ');
  if (result.length < 2 || result.length > 200)
    throw new CatalogConflictError('Name must contain between 2 and 200 characters.', 'name');
  return result;
}
function page(query: PersonQuery): { page: number; size: number; offset: number } {
  const current = Math.max(1, query.currentPage ?? 1);
  const size = 20;
  return { page: current, size, offset: (current - 1) * size };
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

const SELECT = `SELECT id,actor_code,person_type,name,email,phone_number,is_entity,address,category,status,created_at,updated_at FROM public.lu_person`;

@Injectable()
export class PersonRepository {
  async list(pool: IPgPool, siteId: string, query: PersonQuery): Promise<PersonPage> {
    const { page: currentPage, size, offset } = page(query);
    const params: unknown[] = [siteId];
    const filters = ['site_id=$1'];
    // F1 §15: the default list hides deleted persons (status 2); an explicit
    // status filter, including 2, returns exactly that status.
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`status=$${params.length}`);
    } else {
      filters.push('status<>2');
    }
    if (query.type !== undefined) {
      params.push(query.type);
      filters.push(`person_type=$${params.length}`);
    }
    if (query.category !== undefined) {
      params.push(query.category);
      filters.push(`category=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(name ILIKE $${params.length} OR email ILIKE $${params.length} OR actor_code ILIKE $${params.length})`,
      );
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_person WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<PersonRow>(
      `${SELECT} WHERE ${where} ORDER BY name ASC,id ASC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    return {
      items: rows.rows.map(map),
      totalCount,
      pageIndex: currentPage,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / size),
      pageSize: size,
    } satisfies CatalogPage<PersonRecord>;
  }

  async find(pool: IPgPool, siteId: string, id: number): Promise<PersonRecord | null> {
    const result = await pool.query<PersonRow>(`${SELECT} WHERE site_id=$1 AND id=$2`, [
      siteId,
      id,
    ]);
    return result.rows[0] === undefined ? null : map(result.rows[0]);
  }

  private values(input: CreatePersonInput | UpdatePersonInput) {
    const type: PersonType = input.type === 'external' ? 'external' : 'internal';
    const personName = name(input.name);
    const address = clean(input.address, 500, 'address');
    if (type === 'external' && address === null)
      throw new CatalogConflictError('Address is required for external persons.', 'address');
    const email = clean(input.email?.toLowerCase(), 100, 'email');
    const phone = clean(input.phoneNumber, 20, 'phoneNumber');
    const actorCode = clean(input.actorCode, 30, 'actorCode');
    const category = input.category ?? 99;
    if (![1, 2, 3, 4, 5, 99].includes(category))
      throw new CatalogConflictError('Category is invalid.', 'category');
    const status = 'status' in input ? input.status : 0;
    if (![0, 1, 2].includes(status)) throw new CatalogConflictError('Status is invalid.', 'status');
    return { type, personName, address, email, phone, actorCode, category, status };
  }

  async create(
    pool: IPgPool,
    siteId: string,
    userId: string,
    input: CreatePersonInput,
  ): Promise<PersonRecord> {
    const value = this.values(input);
    try {
      const result = await pool.query<PersonRow>(
        `INSERT INTO public.lu_person(site_id,actor_code,person_type,name,email,phone_number,is_entity,address,category,status,created_by_id)
         VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11) RETURNING *`,
        [
          siteId,
          value.actorCode,
          value.type,
          value.personName,
          value.email,
          value.phone,
          input.isEntity === true,
          value.address,
          value.category,
          value.status,
          userId,
        ],
      );
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('Person was not created.');
      return map(row);
    } catch (error) {
      if (unique(error)) throw new CatalogConflictError('Actor code already exists.', 'actorCode');
      throw error;
    }
  }

  async update(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdatePersonInput,
  ): Promise<PersonRecord> {
    const value = this.values(input);
    try {
      const result = await pool.query<PersonRow>(
        `UPDATE public.lu_person SET actor_code=$3,person_type=$4,name=$5,email=$6,phone_number=$7,is_entity=$8,address=$9,category=$10,status=$11,updated_by_id=$12,updated_at=CURRENT_TIMESTAMP
         WHERE site_id=$1 AND id=$2 AND status<>2 RETURNING *`,
        [
          siteId,
          id,
          value.actorCode,
          value.type,
          value.personName,
          value.email,
          value.phone,
          input.isEntity === true,
          value.address,
          value.category,
          value.status,
          userId,
        ],
      );
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('Person was not found.');
      return map(row);
    } catch (error) {
      if (unique(error)) throw new CatalogConflictError('Actor code already exists.', 'actorCode');
      throw error;
    }
  }

  async delete(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_person SET status=2,updated_by_id=$3,updated_at=CURRENT_TIMESTAMP WHERE site_id=$1 AND id=$2 AND status<>2`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Person was not found.');
  }
}
