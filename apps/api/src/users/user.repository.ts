/**
 * Global identity repository (lu_user).
 *
 * ISP boundary: reads/writes ONLY global identity columns per F2 control-plane
 * 0006. Never touches full_name, row_version, created_at, updated_at
 * (trigger-managed) or lu_login_identifier (DB-maintained).
 *
 * Parameterized SQL only. SELECT statements only; mutation UPDATEs run inside
 * the service-provided client so they share the session-invalidation lock.
 */
import { Inject, Injectable } from '@nestjs/common';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import type { AccountStatus, PasswordScheme } from '../identity/identity.contracts.js';

export interface UserRow {
  id: string;
  username: string | null;
  email: string;
  first_name: string;
  last_name: string;
  second_last_name: string | null;
  full_name: string;
  identity_card: string;
  phone_number: string;
  profile_picture_key?: string | null;
  account_status: AccountStatus;
  status: AccountStatus;
  is_super_admin: boolean;
  password_scheme: PasswordScheme | null;
  must_change_password: boolean;
  security_version: string;
  created_at: Date | string;
  updated_at: Date | string;
}

/** One row of the active-site membership projection (F1 §17 site list). */
export interface SiteUserRow extends UserRow, MembershipJoinRow {}

/**
 * Effective ASP status filter for the site projection (F1 §11):
 *  - active:   account active and membership active;
 *  - inactive: membership not revoked and (account inactive or membership suspended);
 *  - deleted:  account deleted or membership revoked.
 * Without a filter, deleted accounts and revoked memberships are hidden.
 */
function siteStatusPredicate(statusFilter: AccountStatus | undefined): string {
  switch (statusFilter) {
    case 'active':
      return "u.account_status = 'active' AND m.status = 'active'";
    case 'inactive':
      return "m.status <> 'revoked' AND u.account_status <> 'deleted' AND (u.account_status = 'inactive' OR m.status = 'suspended')";
    case 'deleted':
      return "(u.account_status = 'deleted' OR m.status = 'revoked')";
    default:
      return "u.account_status <> 'deleted' AND m.status <> 'revoked'";
  }
}

export interface ListRow {
  total_count: number | string;
}

export interface MembershipJoinRow {
  site_id: string;
  site_name: string;
  role: 'Administrador' | 'Supervisor';
  membership_status: 'active' | 'suspended' | 'revoked';
  valid_from: Date | string | null;
  valid_until: Date | string | null;
  position: string | null;
  department: string | null;
  hire_date: Date | string | null;
}

const PAGE_SIZE = 20;

@Injectable()
export class UserRepository {
  constructor(@Inject(AUTH_PG_POOL) private readonly pool: IPgPool) {}

  async listGlobal(
    query: {
      currentPage?: number;
      statusFilter?: AccountStatus;
      searchTerm?: string;
      /** Default global list hides logically deleted accounts (F1 §11). */
      excludeDeleted?: boolean;
    },
    limit = PAGE_SIZE,
  ): Promise<{
    items: UserRow[];
    totalCount: number;
    pageIndex: number;
    totalPages: number;
    pageSize: number;
  }> {
    const page = Math.max(1, query.currentPage ?? 1);
    const offset = (page - 1) * limit;
    const params: unknown[] = [];
    const filters: string[] = [];
    if (query.statusFilter !== undefined) {
      params.push(query.statusFilter);
      filters.push(`account_status = $${params.length}`);
    } else if (query.excludeDeleted === true) {
      filters.push("account_status <> 'deleted'");
    }
    if (query.searchTerm !== undefined && query.searchTerm.trim() !== '') {
      params.push(`%${query.searchTerm.trim()}%`);
      filters.push(
        `(lower(email) LIKE lower($${params.length}) OR lower(username) LIKE lower($${params.length}) OR lower(full_name) LIKE lower($${params.length}) OR lower(identity_card) LIKE lower($${params.length}))`,
      );
    }
    const where = filters.length === 0 ? '' : `WHERE ${filters.join(' AND ')}`;
    const count = await this.pool.query<ListRow>(
      `SELECT count(*)::int AS total_count FROM public.lu_user ${where}`,
      params,
    );
    const totalCount = Number(count.rows[0]?.total_count ?? 0);
    const result = await this.pool.query<UserRow>(
      `SELECT id, username, email, first_name, last_name, second_last_name, full_name, identity_card,
              phone_number, profile_picture_key, account_status, status, is_super_admin, password_scheme,
              must_change_password, security_version, created_at, updated_at
         FROM public.lu_user
         ${where}
         ORDER BY full_name ASC, id ASC
         LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, limit, offset],
    );
    return {
      items: result.rows,
      totalCount,
      pageIndex: page,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / limit),
      pageSize: limit,
    };
  }

  /**
   * Site Administrator list: the active-site membership projection only. Users
   * without a membership in the site are never returned, and totals are
   * computed for the site, so nothing about other sites is disclosed.
   */
  async listForSite(
    siteId: string,
    query: { currentPage?: number; statusFilter?: AccountStatus; searchTerm?: string },
    limit = PAGE_SIZE,
  ): Promise<{
    items: SiteUserRow[];
    totalCount: number;
    pageIndex: number;
    totalPages: number;
    pageSize: number;
  }> {
    const page = Math.max(1, query.currentPage ?? 1);
    const params: unknown[] = [siteId];
    const filters = ['m.site_id = $1', siteStatusPredicate(query.statusFilter)];
    if (query.searchTerm !== undefined && query.searchTerm.trim() !== '') {
      params.push(`%${query.searchTerm.trim()}%`);
      const i = params.length;
      filters.push(
        `(lower(u.email) LIKE lower($${i}) OR lower(u.username) LIKE lower($${i}) OR lower(u.full_name) LIKE lower($${i}) OR lower(u.identity_card) LIKE lower($${i}))`,
      );
    }
    const where = `WHERE ${filters.join(' AND ')}`;
    const from = `FROM public.lu_site_membership m
         JOIN public.lu_user u ON u.id = m.user_id
         JOIN public.lu_site s ON s.id = m.site_id`;
    const count = await this.pool.query<ListRow>(
      `SELECT count(*)::int AS total_count ${from} ${where}`,
      params,
    );
    const totalCount = Number(count.rows[0]?.total_count ?? 0);
    const result = await this.pool.query<SiteUserRow>(
      `SELECT u.id, u.username, u.email, u.first_name, u.last_name, u.second_last_name, u.full_name,
              u.identity_card, u.phone_number, u.profile_picture_key, u.account_status, u.status, u.is_super_admin,
              u.password_scheme, u.must_change_password, u.security_version, u.created_at, u.updated_at,
              m.site_id, s.name AS site_name, m.role, m.status AS membership_status,
              m.valid_from, m.valid_until, m.position, m.department, m.hire_date
         ${from}
         ${where}
         ORDER BY u.full_name ASC, u.id ASC
         LIMIT $${params.length + 1} OFFSET $${params.length + 2}`,
      [...params, limit, (page - 1) * limit],
    );
    return {
      items: result.rows,
      totalCount,
      pageIndex: page,
      totalPages: totalCount === 0 ? 0 : Math.ceil(totalCount / limit),
      pageSize: limit,
    };
  }

  /**
   * Latest account deletion event for the subject (append-only audit). Used to
   * confirm that a deleted account came from the site-revoke cascade of a
   * given site before a site Administrator may reverse it (F1 §11).
   */
  async findLatestAccountDeletion(
    client: IPgClient,
    userId: string,
  ): Promise<{ readonly siteId: string | null; readonly reason: string | null } | null> {
    const result = await client.query<{ site_id: string | null; reason: string | null }>(
      `SELECT site_id, reason
         FROM public.lu_identity_audit_event
        WHERE subject_user_id = $1 AND action = 'account_deleted'
        ORDER BY occurred_at DESC
        LIMIT 1`,
      [userId],
    );
    const row = result.rows[0];
    return row === undefined ? null : { siteId: row.site_id, reason: row.reason };
  }

  /** Row audit actors for the administrative detail view (F1 §13). */
  async findAuditActors(id: string): Promise<{
    readonly createdBy: { readonly userId: string; readonly fullName: string } | null;
    readonly modifiedBy: { readonly userId: string; readonly fullName: string } | null;
  }> {
    const result = await this.pool.query<{
      created_id: string | null;
      created_name: string | null;
      modified_id: string | null;
      modified_name: string | null;
    }>(
      `SELECT c.id AS created_id, c.full_name AS created_name,
              m.id AS modified_id, m.full_name AS modified_name
         FROM public.lu_user u
         LEFT JOIN public.lu_user c ON c.id = u.created_by_user_id
         LEFT JOIN public.lu_user m ON m.id = u.modified_by_user_id
        WHERE u.id = $1`,
      [id],
    );
    const row = result.rows[0];
    return {
      createdBy:
        row?.created_id != null
          ? { userId: row.created_id, fullName: row.created_name ?? '' }
          : null,
      modifiedBy:
        row?.modified_id != null
          ? { userId: row.modified_id, fullName: row.modified_name ?? '' }
          : null,
    };
  }

  async findGlobal(id: string): Promise<UserRow | null> {
    const result = await this.pool.query<UserRow>(
      `SELECT id, username, email, first_name, last_name, second_last_name, full_name, identity_card,
              phone_number, profile_picture_key, account_status, status, is_super_admin, password_scheme,
              must_change_password, security_version, created_at, updated_at
         FROM public.lu_user
        WHERE id = $1
        LIMIT 1`,
      [id],
    );
    return result.rows[0] ?? null;
  }

  async findGlobalForUpdate(client: IPgClient, id: string): Promise<UserRow | null> {
    const result = await client.query<UserRow>(
      `SELECT id, username, email, first_name, last_name, second_last_name, full_name, identity_card,
              phone_number, profile_picture_key, account_status, status, is_super_admin, password_scheme,
              must_change_password, security_version, created_at, updated_at
         FROM public.lu_user
        WHERE id = $1
        FOR UPDATE`,
      [id],
    );
    return result.rows[0] ?? null;
  }

  async listMembershipsForUser(userId: string): Promise<MembershipJoinRow[]> {
    const result = await this.pool.query<MembershipJoinRow>(
      `SELECT m.site_id, s.name AS site_name, m.role, m.status AS membership_status,
              m.valid_from, m.valid_until, m.position, m.department, m.hire_date
         FROM public.lu_site_membership m
         JOIN public.lu_site s ON s.id = m.site_id
        WHERE m.user_id = $1
        ORDER BY s.name ASC, m.site_id ASC`,
      [userId],
    );
    return result.rows;
  }

  async listMembershipsForUpdate(client: IPgClient, userId: string): Promise<MembershipJoinRow[]> {
    const result = await client.query<MembershipJoinRow>(
      `SELECT m.site_id, s.name AS site_name, m.role, m.status AS membership_status,
              m.valid_from, m.valid_until, m.position, m.department, m.hire_date
         FROM public.lu_site_membership m
         JOIN public.lu_site s ON s.id = m.site_id
        WHERE m.user_id = $1
        ORDER BY s.name ASC, m.site_id ASC
        FOR UPDATE OF m, s`,
      [userId],
    );
    return result.rows;
  }

  async listMembershipsBySite(userId: string, siteId: string): Promise<MembershipJoinRow | null> {
    const result = await this.pool.query<MembershipJoinRow>(
      `SELECT m.site_id, s.name AS site_name, m.role, m.status AS membership_status,
              m.valid_from, m.valid_until, m.position, m.department, m.hire_date
         FROM public.lu_site_membership m
         JOIN public.lu_site s ON s.id = m.site_id
        WHERE m.user_id = $1 AND m.site_id = $2
        LIMIT 1`,
      [userId, siteId],
    );
    return result.rows[0] ?? null;
  }

  async insertUser(
    client: IPgClient,
    args: {
      id: string;
      email: string;
      username: string;
      firstName: string;
      lastName: string;
      secondLastName: string | null;
      identityCard: string;
      phoneNumber: string;
      passwordHash: string;
      mustChangePassword: boolean;
      createdByUserId: string;
    },
  ): Promise<UserRow> {
    const result = await client.query<UserRow>(
      `INSERT INTO public.lu_user
         (id, email, username, first_name, last_name, second_last_name,
          identity_card, phone_number, password_hash, password_scheme,
          must_change_password, account_status, status,
          is_super_admin, created_by_user_id, modified_by_user_id)
       VALUES
         ($1, $2, $3, $4, $5, $6,
          $7, $8, $9, 'bcrypt',
          $10, 'active', 'active',
          false, $11, $11)
       RETURNING id, username, email, first_name, last_name, second_last_name, full_name, identity_card,
                 phone_number, profile_picture_key, account_status, status, is_super_admin, password_scheme,
                 must_change_password, security_version, created_at, updated_at`,
      [
        args.id,
        args.email,
        args.username,
        args.firstName,
        args.lastName,
        args.secondLastName,
        args.identityCard,
        args.phoneNumber,
        args.passwordHash,
        args.mustChangePassword,
        args.createdByUserId,
      ],
    );
    const row = result.rows[0];
    if (row === undefined) {
      throw new Error('UserRepository.insertUser: missing returned row.');
    }
    return row;
  }

  async updateGlobalFields(
    client: IPgClient,
    args: {
      id: string;
      email?: string;
      firstName?: string;
      lastName?: string;
      secondLastName?: string | null;
      identityCard?: string;
      phoneNumber?: string;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    const sets: string[] = ['modified_by_user_id = $1'];
    const params: unknown[] = [args.modifiedByUserId];
    if (args.email !== undefined) {
      params.push(args.email);
      sets.push(`email = $${params.length}`);
    }
    if (args.firstName !== undefined) {
      params.push(args.firstName);
      sets.push(`first_name = $${params.length}`);
    }
    if (args.lastName !== undefined) {
      params.push(args.lastName);
      sets.push(`last_name = $${params.length}`);
    }
    if (args.secondLastName !== undefined) {
      params.push(args.secondLastName);
      sets.push(`second_last_name = $${params.length}`);
    }
    if (args.identityCard !== undefined) {
      params.push(args.identityCard);
      sets.push(`identity_card = $${params.length}`);
    }
    if (args.phoneNumber !== undefined) {
      params.push(args.phoneNumber);
      sets.push(`phone_number = $${params.length}`);
    }
    if (sets.length === 1) return;
    params.push(args.id);
    await client.query(
      `UPDATE public.lu_user SET ${sets.join(', ')} WHERE id = $${params.length}`,
      params,
    );
  }

  async adminResetPassword(
    client: IPgClient,
    args: {
      id: string;
      passwordHash: string;
      mustChangePassword: boolean;
      passwordMigratedAt: boolean;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    const sets: string[] = [
      'password_hash = $2',
      "password_scheme = 'bcrypt'",
      'must_change_password = $3',
      'modified_by_user_id = $4',
    ];
    const params: unknown[] = [
      args.id,
      args.passwordHash,
      args.mustChangePassword,
      args.modifiedByUserId,
    ];
    if (args.passwordMigratedAt) {
      sets.push('password_migrated_at = CURRENT_TIMESTAMP');
    }
    await client.query(`UPDATE public.lu_user SET ${sets.join(', ')} WHERE id = $1`, params);
  }

  async setAccountStatus(
    client: IPgClient,
    args: {
      id: string;
      accountStatus: 'active' | 'inactive';
      modifiedByUserId: string;
    },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_user
          SET account_status = $2,
              status = $2,
              modified_by_user_id = $3
        WHERE id = $1`,
      [args.id, args.accountStatus, args.modifiedByUserId],
    );
  }

  async restoreAccount(
    client: IPgClient,
    args: { id: string; modifiedByUserId: string },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_user
          SET account_status = 'active',
              status = 'active',
              modified_by_user_id = $2
        WHERE id = $1`,
      [args.id, args.modifiedByUserId],
    );
  }

  async softDeleteAccount(
    client: IPgClient,
    args: { id: string; modifiedByUserId: string },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_user
          SET account_status = 'deleted',
              status = 'deleted',
              modified_by_user_id = $2
        WHERE id = $1`,
      [args.id, args.modifiedByUserId],
    );
  }

  /**
   * Counts non-revoked site memberships of the user (used inside the service's
   * transaction to enforce single-site custody + the site-revoke=>deleted
   * cascade). Locks the rows for update.
   */
  async countNonRevokedMembershipsForUpdate(
    client: IPgClient,
    userId: string,
  ): Promise<{ nonRevokedCount: number; inSite: Record<string, number> }> {
    const result = await client.query<{ site_id: string; status: string }>(
      `SELECT site_id, status FROM public.lu_site_membership
        WHERE user_id = $1
        FOR UPDATE`,
      [userId],
    );
    const inSite: Record<string, number> = {};
    let nonRevokedCount = 0;
    for (const row of result.rows) {
      if (row.status !== 'revoked') {
        nonRevokedCount += 1;
        inSite[row.site_id] = (inSite[row.site_id] ?? 0) + 1;
      }
    }
    return { nonRevokedCount, inSite };
  }

  /**
   * Locks every active SuperAdmin row and returns how many exist (F1 §8). Row
   * locks cannot be combined with an aggregate in PostgreSQL, so the rows are
   * selected FOR UPDATE and counted here. Callers hold the fixed advisory lock
   * first so concurrent demotions serialize.
   */
  async countActiveSuperAdminsForUpdate(client: IPgClient): Promise<number> {
    const result = await client.query<{ id: string }>(
      `SELECT id FROM public.lu_user
        WHERE is_super_admin = true
          AND account_status = 'active'
        FOR UPDATE`,
      [],
    );
    return result.rows.length;
  }

  async isUserSuperAdminForUpdate(
    client: IPgClient,
    userId: string,
  ): Promise<{ isSuperAdmin: boolean; accountStatus: AccountStatus } | null> {
    const result = await client.query<{ is_super_admin: boolean; account_status: string }>(
      `SELECT is_super_admin, account_status FROM public.lu_user
        WHERE id = $1
        FOR UPDATE`,
      [userId],
    );
    const row = result.rows[0];
    if (row === undefined) return null;
    const accountStatus: AccountStatus =
      row.account_status === 'active' ||
      row.account_status === 'inactive' ||
      row.account_status === 'deleted'
        ? row.account_status
        : 'inactive';
    return { isSuperAdmin: row.is_super_admin, accountStatus };
  }
}
