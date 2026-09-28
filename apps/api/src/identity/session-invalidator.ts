/**
 * PostgreSQL implementation of `SessionInvalidator`.
 *
 * Two statements, both inside the caller's control-plane transaction
 * (the `IPgClient` argument is the transactional wrapper around
 * `lu_auth_runtime`):
 *
 *   1. UPDATE public.lu_user
 *        SET security_version = security_version + 1
 *      WHERE id = $1
 *      RETURNING security_version::text;
 *   The new version is returned as a decimal string (bigint-safe).
 *   Exactly one row must be returned; missing or duplicate rows throw.
 *
 *   2. UPDATE public.lu_session
 *        SET revoked_at = now(), revocation_reason = $2
 *      WHERE user_id = $1 AND revoked_at IS NULL;
 *   This revokes every non-revoked session of the subject. The
 *   `lu_session` table CHECK constraint
 *     (revoked_at IS NULL OR revocation_reason IS NOT NULL)
 *     AND (revocation_reason IS NULL OR btrim(revocation_reason) <> '')
 *   permits any non-blank reason value; we map the typed
 *   `SecurityRotationReason` enum to a stable, non-blank string so the
 *   audited evidence is machine-readable.
 *
 * F2-W2 (control-plane 0006) granted `lu_auth_runtime` UPDATE on
 * `lu_session.revoked_at` and `lu_session.revocation_reason` and UPDATE
 * on `lu_user.security_version`; both statements succeed under the
 * runtime role.
 *
 * Plaintext, hashes, sessions tokens or actor PII are NEVER logged.
 */
import type { IPgClient } from '../auth/auth.pg-pool.js';
import type { SecurityRotationReason, SessionInvalidator } from './identity.contracts.js';

const REVOCATION_REASONS: Readonly<Record<SecurityRotationReason, string>> = {
  email_change: 'email_change',
  username_repair: 'username_repair',
  self_password_change: 'self_password_change',
  admin_password_reset: 'admin_password_reset',
  credential_rehash: 'credential_rehash',
  global_role_change: 'global_role_change',
  site_role_change: 'site_role_change',
  membership_change: 'membership_change',
  account_status_change: 'account_status_change',
  access_revoked: 'access_revoked',
  access_restored: 'access_restored',
  legacy_retirement: 'legacy_retirement',
};

export class PgSessionInvalidator implements SessionInvalidator {
  async rotateAndRevokeAll(
    client: IPgClient,
    userId: string,
    reason: SecurityRotationReason,
  ): Promise<string> {
    if (typeof userId !== 'string' || userId.length === 0) {
      throw new Error('PgSessionInvalidator: userId is required.');
    }
    const revocationReason = REVOCATION_REASONS[reason];
    if (typeof revocationReason !== 'string' || revocationReason.length === 0) {
      throw new Error('PgSessionInvalidator: invalid rotation reason.');
    }
    const userResult = await client.query<{ security_version: string }>(
      'UPDATE public.lu_user SET security_version = security_version + 1 ' +
        'WHERE id = $1 RETURNING security_version::text',
      [userId],
    );
    if (userResult.rows.length !== 1) {
      throw new Error(
        `PgSessionInvalidator: expected exactly one lu_user row for id (got ${userResult.rows.length}).`,
      );
    }
    const newVersion = userResult.rows[0]?.security_version;
    if (typeof newVersion !== 'string') {
      throw new Error('PgSessionInvalidator: missing returned security_version.');
    }
    await client.query(
      'UPDATE public.lu_session SET revoked_at = now(), revocation_reason = $1 ' +
        'WHERE user_id = $2 AND revoked_at IS NULL',
      [revocationReason, userId],
    );
    return newVersion;
  }
}
