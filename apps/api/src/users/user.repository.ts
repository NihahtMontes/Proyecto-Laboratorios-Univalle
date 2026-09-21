import { Injectable } from '@nestjs/common';
import type {
  CreateManagedUserInput,
  ManagedUserPage,
  ManagedUserQuery,
  ManagedUserRecord,
  ProfileRecord,
  SiteRole,
  UpdateManagedUserInput,
  UpdateProfileInput,
} from '@lu/contracts';
import type { IPgPool } from '../auth/auth.pg-pool.js';
import { CatalogConflictError, CatalogMissingError } from '../catalogs/catalog.repository.js';

interface UserRow {
  id: string;
  email: string;
  full_name: string;
  status: 'active' | 'disabled';
  is_super_admin: boolean;
  role: SiteRole;
  membership_status: 'active' | 'suspended' | 'revoked';
  created_at: Date | string;
  updated_at: Date | string;
}
interface CountRow {
  total_count: number | string;
}

function iso(value: Date | string): string {
  return value instanceof Date ? value.toISOString() : new Date(value).toISOString();
}
function map(row: UserRow): ManagedUserRecord {
  return {
    id: row.id,
    email: row.email,
    fullName: row.full_name,
    status: row.status,
    isSuperAdmin: row.is_super_admin,
    siteRole: row.role,
    membershipStatus: row.membership_status,
    createdAt: iso(row.created_at),
    updatedAt: iso(row.updated_at),
  };
}
function page(value: number | undefined): { page: number; size: number; offset: number } {
  const current = Math.max(1, value ?? 1);
  const size = 20;
  return { page: current, size, offset: (current - 1) * size };
}
function unique(error: unknown): boolean {
  return (
    typeof error === 'object' && error !== null && (error as { code?: unknown }).code === '23505'
  );
}
const SELECT = `SELECT u.id,u.email,u.full_name,u.status,u.is_super_admin,m.role,m.status AS membership_status,u.created_at,u.updated_at FROM public.lu_user u JOIN public.lu_site_membership m ON m.user_id=u.id`;

@Injectable()
export class UserRepository {
  async list(pool: IPgPool, siteId: string, query: ManagedUserQuery): Promise<ManagedUserPage> {
    const { page: currentPage, size, offset } = page(query.currentPage);
    const params: unknown[] = [siteId];
    const filters = ['m.site_id=$1', "m.status<>'revoked'"];
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`u.status=$${params.length}`);
    }
    if (query.searchTerm?.trim()) {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(`(u.full_name ILIKE $${params.length} OR u.email ILIKE $${params.length})`);
    }
    const where = filters.join(' AND ');
    const count = await pool.query<CountRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_user u JOIN public.lu_site_membership m ON m.user_id=u.id WHERE ${where}`,
      params,
    );
    const totalCount = Number(count.rows[0]?.total_count ?? 0);
    const rows = await pool.query<UserRow>(
      `${SELECT} WHERE ${where} ORDER BY u.full_name ASC,u.id ASC LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, size, offset],
    );
    return {
      items: rows.rows.map(map),
      totalCount,
      pageIndex: currentPage,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / size),
      pageSize: size,
    };
  }

  async find(pool: IPgPool, siteId: string, id: string): Promise<ManagedUserRecord | null> {
    const result = await pool.query<UserRow>(`${SELECT} WHERE m.site_id=$1 AND u.id=$2`, [
      siteId,
      id,
    ]);
    return result.rows[0] === undefined ? null : map(result.rows[0]);
  }

  async create(
    pool: IPgPool,
    siteId: string,
    id: string,
    input: CreateManagedUserInput,
    passwordHash: string,
  ): Promise<ManagedUserRecord> {
    const fullName = input.fullName.trim().replace(/\s+/g, ' ');
    const email = input.email.trim().toLowerCase();
    return pool.transaction(async (client) => {
      try {
        await client.query(
          `INSERT INTO public.lu_user(id,email,full_name,password_hash,is_super_admin,status) VALUES($1,$2,$3,$4,false,'active')`,
          [id, email, fullName, passwordHash],
        );
        await client.query(
          `INSERT INTO public.lu_site_membership(user_id,site_id,role,status) VALUES($1,$2,$3,'active')`,
          [id, siteId, input.siteRole],
        );
      } catch (error) {
        if (unique(error))
          throw new CatalogConflictError('This email is already registered.', 'email');
        throw error;
      }
      const result = await client.query<UserRow>(`${SELECT} WHERE m.site_id=$1 AND u.id=$2`, [
        siteId,
        id,
      ]);
      const row = result.rows[0];
      if (row === undefined) throw new CatalogMissingError('User was not created.');
      return map(row);
    });
  }

  async update(
    pool: IPgPool,
    siteId: string,
    id: string,
    input: UpdateManagedUserInput,
    passwordHash: string | null,
  ): Promise<ManagedUserRecord> {
    const fullName = input.fullName.trim().replace(/\s+/g, ' ');
    const email = input.email.trim().toLowerCase();
    return pool.transaction(async (client) => {
      try {
        const result = await client.query(
          `UPDATE public.lu_user SET email=$2,full_name=$3,status=$4,security_version=security_version+1,updated_at=CURRENT_TIMESTAMP WHERE id=$1 AND is_super_admin=false`,
          [id, email, fullName, input.status],
        );
        if (result.rowCount !== 1)
          throw new CatalogMissingError('User was not found or is protected.');
        if (passwordHash !== null)
          await client.query(`UPDATE public.lu_user SET password_hash=$2 WHERE id=$1`, [
            id,
            passwordHash,
          ]);
        await client.query(
          `UPDATE public.lu_site_membership SET role=$3,status=$4,updated_at=CURRENT_TIMESTAMP WHERE user_id=$1 AND site_id=$2`,
          [id, siteId, input.siteRole, input.status === 'active' ? 'active' : 'suspended'],
        );
      } catch (error) {
        if (unique(error))
          throw new CatalogConflictError('This email is already registered.', 'email');
        throw error;
      }
      const value = await client.query<UserRow>(`${SELECT} WHERE m.site_id=$1 AND u.id=$2`, [
        siteId,
        id,
      ]);
      const row = value.rows[0];
      if (row === undefined) throw new CatalogMissingError('User was not found.');
      return map(row);
    });
  }

  async profile(pool: IPgPool, userId: string, siteId: string): Promise<ProfileRecord | null> {
    const result = await pool.query<{
      id: string;
      email: string;
      full_name: string;
      role: SiteRole;
    }>(
      `SELECT u.id,u.email,u.full_name,m.role FROM public.lu_user u JOIN public.lu_site_membership m ON m.user_id=u.id WHERE u.id=$1 AND m.site_id=$2 AND u.status='active' AND m.status='active'`,
      [userId, siteId],
    );
    const row = result.rows[0];
    return row === undefined
      ? null
      : { id: row.id, email: row.email, fullName: row.full_name, siteRole: row.role };
  }

  async updateProfile(
    pool: IPgPool,
    userId: string,
    input: UpdateProfileInput,
  ): Promise<ProfileRecord> {
    const result = await pool.query<{ id: string; email: string; full_name: string }>(
      `UPDATE public.lu_user SET email=$2,full_name=$3,updated_at=CURRENT_TIMESTAMP WHERE id=$1 AND status='active' RETURNING id,email,full_name`,
      [userId, input.email.trim().toLowerCase(), input.fullName.trim().replace(/\s+/g, ' ')],
    );
    const row = result.rows[0];
    if (row === undefined) throw new CatalogMissingError('Profile was not found.');
    const role = await pool.query<{ role: SiteRole }>(
      `SELECT role FROM public.lu_site_membership WHERE user_id=$1 AND status='active' ORDER BY site_id LIMIT 1`,
      [userId],
    );
    return {
      id: row.id,
      email: row.email,
      fullName: row.full_name,
      siteRole: role.rows[0]?.role ?? ('Supervisor' as SiteRole),
    };
  }
}
