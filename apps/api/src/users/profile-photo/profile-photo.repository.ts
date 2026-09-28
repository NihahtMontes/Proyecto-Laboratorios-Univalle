/**
 * Persists only the opaque `lu_user.profile_picture_key` (F1 §14). The
 * lu_auth_runtime role holds a column-level UPDATE grant on it (control-plane
 * 0006); row_version/updated_at are trigger-managed.
 */
import { Inject, Injectable } from '@nestjs/common';
import { AUTH_PG_POOL } from '../../auth/auth.constants.js';
import type { IPgClient, IPgPool } from '../../auth/auth.pg-pool.js';

@Injectable()
export class ProfilePhotoRepository {
  constructor(@Inject(AUTH_PG_POOL) private readonly pool: IPgPool) {}

  async findKey(userId: string): Promise<{ readonly key: string | null } | null> {
    const result = await this.pool.query<{ profile_picture_key: string | null }>(
      'SELECT profile_picture_key FROM public.lu_user WHERE id = $1',
      [userId],
    );
    const row = result.rows[0];
    return row === undefined ? null : { key: row.profile_picture_key };
  }

  async setKey(
    client: IPgClient,
    userId: string,
    key: string | null,
    modifiedByUserId: string,
  ): Promise<void> {
    const result = await client.query(
      `UPDATE public.lu_user
          SET profile_picture_key = $2,
              modified_by_user_id = $3
        WHERE id = $1`,
      [userId, key, modifiedByUserId],
    );
    if (result.rowCount !== 1) {
      throw new Error('Profile picture key update affected an unexpected number of rows.');
    }
  }
}
