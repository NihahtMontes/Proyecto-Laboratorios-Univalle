/**
 * PostgreSQL implementation of `LegacyPasswordWindow`.
 *
 * Reads `legacy_password_deadline` from `public.lu_identity_migration_state`.
 * The legacy verification window is CLOSED only once a recorded
 * `legacy_password_deadline` has been reached (`now >= deadline`): login
 * fails closed at the boundary even before the retirement batch runs
 * (F1 §7). When no deadline is recorded (missing row or null cutover) no
 * deadline-based refusal applies. The deadline is never inferred from
 * deployment time.
 *
 * The `lu_identity_migration_state` table is migration-owned: runtime
 * has SELECT only (control-plane 0006 grants). The query never mutates
 * the row.
 */
import type { IPgClient } from '../auth/auth.pg-pool.js';
import type { LegacyPasswordWindow } from './identity.contracts.js';

export class PgLegacyPasswordWindow implements LegacyPasswordWindow {
  async isOpen(client: IPgClient, now: Date): Promise<boolean> {
    if (!(now instanceof Date) || Number.isNaN(now.getTime())) {
      throw new Error('PgLegacyPasswordWindow: now must be a valid Date.');
    }
    const result = await client.query<{ legacy_password_deadline: Date | null }>(
      'SELECT legacy_password_deadline FROM public.lu_identity_migration_state WHERE id = 1',
      [],
    );
    if (result.rows.length === 0) return true;
    const deadline = result.rows[0]?.legacy_password_deadline;
    if (deadline === null || deadline === undefined) return true;
    return now.getTime() < new Date(deadline).getTime();
  }
}
