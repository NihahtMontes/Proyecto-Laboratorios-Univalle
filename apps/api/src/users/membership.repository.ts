/**
 * Site membership repository (lu_site_membership).
 *
 * Reads/writes ONLY the membership-level columns per F2 control-plane 0006.
 * Never touches row_version, created_at, updated_at (trigger-managed).
 * Parameterized SQL only.
 *
 * All write operations are passed the IPgClient from the calling service so
 * the membership lock + the user-level advisory lock can be coordinated.
 */
import { Inject, Injectable } from '@nestjs/common';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import type { MembershipRole, MembershipStatus } from '../identity/identity.contracts.js';

export interface MembershipRow {
  user_id: string;
  site_id: string;
  role: MembershipRole;
  status: MembershipStatus;
  valid_from: Date | string | null;
  valid_until: Date | string | null;
  position: string | null;
  department: string | null;
  hire_date: Date | string | null;
}

@Injectable()
export class MembershipRepository {
  constructor(@Inject(AUTH_PG_POOL) private readonly pool: IPgPool) {}

  async findByUserAndSite(
    pool: IPgPool | IPgClient,
    userId: string,
    siteId: string,
  ): Promise<MembershipRow | null> {
    const result = await pool.query<MembershipRow>(
      `SELECT user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date
         FROM public.lu_site_membership
        WHERE user_id = $1 AND site_id = $2
        LIMIT 1`,
      [userId, siteId],
    );
    return result.rows[0] ?? null;
  }

  async findByUserAndSiteForUpdate(
    client: IPgClient,
    userId: string,
    siteId: string,
  ): Promise<MembershipRow | null> {
    const result = await client.query<MembershipRow>(
      `SELECT user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date
         FROM public.lu_site_membership
        WHERE user_id = $1 AND site_id = $2
        FOR UPDATE`,
      [userId, siteId],
    );
    return result.rows[0] ?? null;
  }

  async listForUser(userId: string): Promise<MembershipRow[]> {
    const result = await this.pool.query<MembershipRow>(
      `SELECT user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date
         FROM public.lu_site_membership
        WHERE user_id = $1
        ORDER BY site_id ASC`,
      [userId],
    );
    return result.rows;
  }

  async listForUserForUpdate(client: IPgClient, userId: string): Promise<MembershipRow[]> {
    const result = await client.query<MembershipRow>(
      `SELECT user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date
         FROM public.lu_site_membership
        WHERE user_id = $1
        ORDER BY site_id ASC
        FOR UPDATE`,
      [userId],
    );
    return result.rows;
  }

  async insertMembership(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      role: MembershipRole;
      status: MembershipStatus;
      validFrom: Date | null;
      validUntil: Date | null;
      createdByUserId: string;
    },
  ): Promise<MembershipRow> {
    const result = await client.query<MembershipRow>(
      `INSERT INTO public.lu_site_membership
         (user_id, site_id, role, status, valid_from, valid_until,
          created_by_user_id, modified_by_user_id)
       VALUES
         ($1, $2, $3, $4, $5, $6, $7, $7)
       RETURNING user_id, site_id, role, status, valid_from, valid_until, position, department, hire_date`,
      [
        args.userId,
        args.siteId,
        args.role,
        args.status,
        args.validFrom,
        args.validUntil,
        args.createdByUserId,
      ],
    );
    const row = result.rows[0];
    if (row === undefined) {
      throw new Error('MembershipRepository.insertMembership: missing returned row.');
    }
    return row;
  }

  async updateRole(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      role: MembershipRole;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_site_membership
          SET role = $3, modified_by_user_id = $4
        WHERE user_id = $1 AND site_id = $2`,
      [args.userId, args.siteId, args.role, args.modifiedByUserId],
    );
  }

  async updateStatus(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      status: MembershipStatus;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_site_membership
          SET status = $3, modified_by_user_id = $4
        WHERE user_id = $1 AND site_id = $2`,
      [args.userId, args.siteId, args.status, args.modifiedByUserId],
    );
  }

  async updateValidity(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      validFrom: Date | null;
      validUntil: Date | null;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    if (args.validFrom !== null && args.validUntil !== null && args.validUntil <= args.validFrom) {
      throw new Error('valid_until must be greater than valid_from.');
    }
    await client.query(
      `UPDATE public.lu_site_membership
          SET valid_from = $3, valid_until = $4, modified_by_user_id = $5
        WHERE user_id = $1 AND site_id = $2`,
      [args.userId, args.siteId, args.validFrom, args.validUntil, args.modifiedByUserId],
    );
  }

  async updateWorkProfile(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      position: string | null;
      department: string | null;
      hireDate: Date | null;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_site_membership
          SET position = $3, department = $4, hire_date = $5, modified_by_user_id = $6
        WHERE user_id = $1 AND site_id = $2`,
      [
        args.userId,
        args.siteId,
        args.position,
        args.department,
        args.hireDate,
        args.modifiedByUserId,
      ],
    );
  }

  async revoke(
    client: IPgClient,
    args: { userId: string; siteId: string; modifiedByUserId: string },
  ): Promise<void> {
    await client.query(
      `UPDATE public.lu_site_membership
          SET status = 'revoked', modified_by_user_id = $3
        WHERE user_id = $1 AND site_id = $2`,
      [args.userId, args.siteId, args.modifiedByUserId],
    );
  }

  async restore(
    client: IPgClient,
    args: {
      userId: string;
      siteId: string;
      role: MembershipRole;
      validFrom: Date | null;
      validUntil: Date | null;
      modifiedByUserId: string;
    },
  ): Promise<void> {
    if (args.validFrom !== null && args.validUntil !== null && args.validUntil <= args.validFrom) {
      throw new Error('valid_until must be greater than valid_from.');
    }
    await client.query(
      `UPDATE public.lu_site_membership
          SET status = 'active',
              role = $3,
              valid_from = $4,
              valid_until = $5,
              modified_by_user_id = $6
        WHERE user_id = $1 AND site_id = $2`,
      [args.userId, args.siteId, args.role, args.validFrom, args.validUntil, args.modifiedByUserId],
    );
  }
}
