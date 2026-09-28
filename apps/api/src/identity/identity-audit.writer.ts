/**
 * PostgreSQL implementation of `IdentityAuditWriter`.
 *
 * Appends a single row to `public.lu_identity_audit_event` using only
 * the columns the F2-W2 control-plane migration granted to
 * `lu_auth_runtime` (id, occurred_at defaults on insert, action,
 * subject_user_id, actor_user_id, site_id, correlation_id, reason,
 * before_status, after_status, metadata). The audit table is append-only:
 * UPDATE and DELETE are blocked by a BEFORE trigger. The `id` column
 * has no DEFAULT, so the writer generates a UUID server-side.
 *
 * Defense in depth: the writer REJECTS metadata keys that look like
 * secrets (`/pass|hash|token|secret|salt|key/i`) and any non-scalar
 * metadata value. The intent is that a caller passing a misnamed key
 * (e.g. `password_changed` mapped to `{ passwordHash: '...' }`) is
 * rejected BEFORE the row is written, even though the F1 contract
 * already says such material must never be put there.
 *
 * The writer NEVER logs plaintext, hash material, derived keys or
 * session tokens. Error messages name the offending key or the schema
 * field only.
 */
import { randomUUID } from 'node:crypto';
import type { IPgClient } from '../auth/auth.pg-pool.js';
import type { IdentityAuditEvent, IdentityAuditWriter } from './identity.contracts.js';

const FORBIDDEN_METADATA_KEY_RE = /pass|hash|token|secret|salt|key/i;

export class PgIdentityAuditWriter implements IdentityAuditWriter {
  async append(client: IPgClient, event: IdentityAuditEvent): Promise<void> {
    if (event === null || typeof event !== 'object') {
      throw new Error('PgIdentityAuditWriter: event is required.');
    }
    if (typeof event.action !== 'string' || event.action.length === 0) {
      throw new Error('PgIdentityAuditWriter: action is required.');
    }
    const metadata = event.metadata ?? {};
    for (const [k, v] of Object.entries(metadata)) {
      if (FORBIDDEN_METADATA_KEY_RE.test(k)) {
        throw new Error(
          `PgIdentityAuditWriter: metadata key "${k}" is forbidden (looks like a secret).`,
        );
      }
      if (v !== null && typeof v !== 'string' && typeof v !== 'number' && typeof v !== 'boolean') {
        throw new Error(
          `PgIdentityAuditWriter: metadata value for "${k}" must be a scalar or null.`,
        );
      }
    }
    const id = randomUUID();
    await client.query(
      'INSERT INTO public.lu_identity_audit_event ' +
        '(id, action, subject_user_id, actor_user_id, site_id, correlation_id, ' +
        'reason, before_status, after_status, metadata) ' +
        'VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10::jsonb)',
      [
        id,
        event.action,
        event.subjectUserId ?? null,
        event.actorUserId ?? null,
        event.siteId ?? null,
        event.correlationId ?? null,
        event.reason ?? null,
        event.beforeStatus ?? null,
        event.afterStatus ?? null,
        JSON.stringify(metadata),
      ],
    );
  }
}
