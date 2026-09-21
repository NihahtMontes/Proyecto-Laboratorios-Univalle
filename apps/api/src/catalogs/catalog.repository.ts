import { Injectable } from '@nestjs/common';
import type {
  CatalogCollectionQuery,
  CatalogPage,
  City,
  Country,
  CreateCityInput,
  CreateCountryInput,
  UpdateCityInput,
  UpdateCountryInput,
} from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';

export class CatalogConflictError extends Error {
  constructor(
    message: string,
    public readonly field?: string,
  ) {
    super(message);
    this.name = 'CatalogConflictError';
  }
}

export class CatalogMissingError extends Error {
  constructor(message = 'Catalog record was not found.') {
    super(message);
    this.name = 'CatalogMissingError';
  }
}

interface CountryRow {
  id: number | string;
  name: string;
  status: number;
  city_count: number | string;
  created_at: Date | string;
  updated_at: Date | string | null;
}

interface CityRow {
  id: number | string;
  country_id: number | string;
  country_name: string;
  name: string;
  region: string | null;
  status: number;
  created_at: Date | string;
  updated_at: Date | string | null;
}

interface CountRow {
  total_count: number | string;
}

interface CityCountRow {
  city_count: number | string;
}

function integer(value: number | string): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isSafeInteger(parsed) ? parsed : 0;
}

function isoDate(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}

function nullableIsoDate(value: Date | string | null): string | null {
  return value === null ? null : isoDate(value);
}

function status(value: number): 0 | 1 | 2 {
  return value === 1 ? 1 : value === 2 ? 2 : 0;
}

function mapCountry(row: CountryRow): Country {
  return {
    id: integer(row.id),
    name: row.name,
    status: status(row.status),
    cityCount: integer(row.city_count),
    createdAt: isoDate(row.created_at),
    updatedAt: nullableIsoDate(row.updated_at),
  };
}

function mapCity(row: CityRow): City {
  return {
    id: integer(row.id),
    countryId: integer(row.country_id),
    countryName: row.country_name,
    name: row.name,
    region: row.region,
    status: status(row.status),
    createdAt: isoDate(row.created_at),
    updatedAt: nullableIsoDate(row.updated_at),
  };
}

function pageQuery(query: CatalogCollectionQuery): {
  page: number;
  pageSize: number;
  offset: number;
} {
  const page = Math.max(1, query.currentPage ?? 1);
  const pageSize = 20;
  return { page, pageSize, offset: (page - 1) * pageSize };
}

function isUniqueViolation(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}

function normalized(value: string): string {
  return value.trim().replace(/\s+/g, ' ');
}

function assertName(value: string, field: string): string {
  const result = normalized(value);
  if (result.length < 2 || result.length > 100) {
    throw new CatalogConflictError(
      `${field} must contain between 2 and 100 characters.`,
      field === 'Country name' || field === 'City name' ? 'name' : undefined,
    );
  }
  if (!/^[\p{L}\s-]+$/u.test(result)) {
    throw new CatalogConflictError(
      `${field} contains invalid characters.`,
      field === 'Country name' || field === 'City name' ? 'name' : undefined,
    );
  }
  return result;
}

function nullableRegion(value: string | null | undefined): string | null {
  const result = value === null || value === undefined ? '' : value.trim().replace(/\s+/g, ' ');
  if (result.length > 100)
    throw new CatalogConflictError('Region must not exceed 100 characters.', 'region');
  return result === '' ? null : result;
}

const COUNTRY_SELECT = `
    SELECT c.id, c.name, c.status, c.created_at, c.updated_at,
           count(ci.id) FILTER (WHERE ci.status <> 2)::int AS city_count
      FROM public.lu_country c
      LEFT JOIN public.lu_city ci
        ON ci.site_id = c.site_id AND ci.country_id = c.id
`;

const CITY_SELECT = `
    SELECT c.id, c.country_id, co.name AS country_name, c.name, c.region,
           c.status, c.created_at, c.updated_at
      FROM public.lu_city c
      JOIN public.lu_country co
        ON co.site_id = c.site_id AND co.id = c.country_id
`;

@Injectable()
export class CatalogRepository {
  async listCountries(
    pool: IPgPool,
    siteId: string,
    query: CatalogCollectionQuery,
  ): Promise<CatalogPage<Country>> {
    const { page, pageSize, offset } = pageQuery(query);
    const params: unknown[] = [siteId];
    const filters = ['c.site_id = $1', 'c.status <> 2'];
    if (query.searchTerm !== undefined && query.searchTerm.trim() !== '') {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(`c.name ILIKE $${params.length}`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`c.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_country c WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const result = await pool.query<CountryRow>(
      `${COUNTRY_SELECT} WHERE ${where}
       GROUP BY c.id, c.name, c.status, c.created_at, c.updated_at
       ORDER BY c.name ASC, c.id ASC
       LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, offset],
    );
    return {
      items: result.rows.map(mapCountry),
      totalCount,
      pageIndex: page,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async findCountry(pool: IPgPool, siteId: string, id: number): Promise<Country | null> {
    const result = await pool.query<CountryRow>(
      `${COUNTRY_SELECT} WHERE c.site_id = $1 AND c.id = $2
       GROUP BY c.id, c.name, c.status, c.created_at, c.updated_at`,
      [siteId, id],
    );
    return result.rows[0] === undefined ? null : mapCountry(result.rows[0]);
  }

  async createCountry(pool: IPgPool, siteId: string, userId: string, input: CreateCountryInput) {
    const name = assertName(input.name, 'Country name');
    return pool.transaction(async (client) => {
      try {
        const result = await client.query<CountryRow>(
          `INSERT INTO public.lu_country (site_id, name, status, created_by_id)
           VALUES ($1, $2, 0, $3)
           RETURNING id, name, status, created_at, updated_at, 0::int AS city_count`,
          [siteId, name, userId],
        );
        const row = result.rows[0];
        if (row === undefined) throw new CatalogMissingError('Country was not created.');
        return mapCountry(row);
      } catch (error) {
        if (isUniqueViolation(error)) {
          throw new CatalogConflictError('A country with this name already exists.', 'name');
        }
        throw error;
      }
    });
  }

  async updateCountry(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateCountryInput,
  ) {
    const name = assertName(input.name, 'Country name');
    return pool.transaction(async (client) => {
      try {
        const result = await client.query<CountryRow>(
          `UPDATE public.lu_country
              SET name = $3, status = $4, updated_by_id = $5, updated_at = CURRENT_TIMESTAMP
            WHERE site_id = $1 AND id = $2 AND status <> 2
            RETURNING id, name, status, created_at, updated_at, 0::int AS city_count`,
          [siteId, id, name, input.status, userId],
        );
        if (result.rows[0] === undefined) throw new CatalogMissingError('Country was not found.');
        const mapped = mapCountry(result.rows[0]);
        const count = await client.query<CityCountRow>(
          `SELECT count(*)::int AS city_count FROM public.lu_city
            WHERE site_id = $1 AND country_id = $2 AND status <> 2`,
          [siteId, id],
        );
        return { ...mapped, cityCount: integer(count.rows[0]?.city_count ?? 0) };
      } catch (error) {
        if (isUniqueViolation(error)) {
          throw new CatalogConflictError('A country with this name already exists.', 'name');
        }
        throw error;
      }
    });
  }

  async deleteCountry(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_country
          SET status = 2, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP
        WHERE site_id = $1 AND id = $2 AND status <> 2`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('Country was not found.');
  }

  async listCities(
    pool: IPgPool,
    siteId: string,
    query: CatalogCollectionQuery,
  ): Promise<CatalogPage<City>> {
    const { page, pageSize, offset } = pageQuery(query);
    const params: unknown[] = [siteId];
    const filters = ['c.site_id = $1', 'c.status <> 2', 'co.status <> 2'];
    if (query.searchTerm !== undefined && query.searchTerm.trim() !== '') {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(`(c.name ILIKE $${params.length} OR co.name ILIKE $${params.length})`);
    }
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`c.status = $${params.length}`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_city c
         JOIN public.lu_country co ON co.site_id = c.site_id AND co.id = c.country_id
        WHERE ${where}`,
      params,
    );
    const totalCount = integer(count.rows[0]?.total_count ?? 0);
    const result = await pool.query<CityRow>(
      `${CITY_SELECT} WHERE ${where}
       ORDER BY c.name ASC, c.id ASC
       LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, pageSize, offset],
    );
    return {
      items: result.rows.map(mapCity),
      totalCount,
      pageIndex: page,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
      pageSize,
    };
  }

  async findCity(pool: IPgPool, siteId: string, id: number): Promise<City | null> {
    const result = await pool.query<CityRow>(`${CITY_SELECT} WHERE c.site_id = $1 AND c.id = $2`, [
      siteId,
      id,
    ]);
    return result.rows[0] === undefined ? null : mapCity(result.rows[0]);
  }

  async createCity(pool: IPgPool, siteId: string, userId: string, input: CreateCityInput) {
    const name = assertName(input.name, 'City name');
    const region = nullableRegion(input.region);
    return pool.transaction(async (client) => {
      const country = await client.query<{ id: number }>(
        `SELECT id FROM public.lu_country WHERE site_id = $1 AND id = $2 AND status = 0`,
        [siteId, input.countryId],
      );
      if (country.rows[0] === undefined) {
        throw new CatalogConflictError('The selected country is not active.', 'countryId');
      }
      try {
        const result = await client.query<CityRow>(
          `INSERT INTO public.lu_city (site_id, country_id, name, region, status, created_by_id)
           VALUES ($1, $2, $3, $4, 0, $5)
           RETURNING id, country_id, name, region, status, created_at, updated_at`,
          [siteId, input.countryId, name, region, userId],
        );
        const city = result.rows[0];
        if (city === undefined) throw new CatalogMissingError('City was not created.');
        const detail = await client.query<CityRow>(
          `${CITY_SELECT} WHERE c.site_id = $1 AND c.id = $2`,
          [siteId, city.id],
        );
        const row = detail.rows[0];
        if (row === undefined) throw new CatalogMissingError('City was not created.');
        return mapCity(row);
      } catch (error) {
        if (isUniqueViolation(error)) {
          throw new CatalogConflictError(
            'A city with this name already exists in the country.',
            'name',
          );
        }
        throw error;
      }
    });
  }

  async updateCity(
    pool: IPgPool,
    siteId: string,
    userId: string,
    id: number,
    input: UpdateCityInput,
  ) {
    const name = assertName(input.name, 'City name');
    const region = nullableRegion(input.region);
    return pool.transaction(async (client) => {
      const country = await client.query<{ id: number }>(
        `SELECT id FROM public.lu_country WHERE site_id = $1 AND id = $2 AND status <> 2`,
        [siteId, input.countryId],
      );
      if (country.rows[0] === undefined) {
        throw new CatalogConflictError('The selected country is not available.', 'countryId');
      }
      try {
        const result = await client.query<CityRow>(
          `UPDATE public.lu_city
              SET country_id = $3, name = $4, region = $5, status = $6,
                  updated_by_id = $7, updated_at = CURRENT_TIMESTAMP
            WHERE site_id = $1 AND id = $2 AND status <> 2
            RETURNING id`,
          [siteId, id, input.countryId, name, region, input.status, userId],
        );
        if (result.rows[0] === undefined) throw new CatalogMissingError('City was not found.');
        const detail = await client.query<CityRow>(
          `${CITY_SELECT} WHERE c.site_id = $1 AND c.id = $2`,
          [siteId, id],
        );
        const row = detail.rows[0];
        if (row === undefined) throw new CatalogMissingError('City was not found.');
        return mapCity(row);
      } catch (error) {
        if (isUniqueViolation(error)) {
          throw new CatalogConflictError(
            'A city with this name already exists in the country.',
            'name',
          );
        }
        throw error;
      }
    });
  }

  async deleteCity(pool: IPgPool, siteId: string, userId: string, id: number): Promise<void> {
    const result = await pool.query(
      `UPDATE public.lu_city
          SET status = 2, updated_by_id = $3, updated_at = CURRENT_TIMESTAMP
        WHERE site_id = $1 AND id = $2 AND status <> 2`,
      [siteId, id, userId],
    );
    if (result.rowCount !== 1) throw new CatalogMissingError('City was not found.');
  }
}
