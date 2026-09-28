/**
 * SuperAdmin grant/revoke service (CLI-only; never over HTTP).
 *
 *  - The actor must be an active SuperAdmin.
 *  - Never self-revoke (F1 section 9).
 *  - Last-active invariant: the command cannot leave zero active SuperAdmins.
 *    Both grant and revoke hold `pg_advisory_xact_lock(<fixed key>)`, lock the
 *    SuperAdmin rows FOR UPDATE and recount.
 *  - Rotates `global_role_change` for the subject, revokes sessions and writes
 *    a `superadmin_granted` / `superadmin_revoked` audit event with actor +
 *    reason. The CLI is the only path that flips `is_super_admin`.
 */
import { Inject, Injectable } from '@nestjs/common';
import { AUTH_PG_POOL } from '../auth/auth.constants.js';
import type { IPgClient, IPgPool } from '../auth/auth.pg-pool.js';
import {
  IDENTITY_AUDIT_WRITER,
  SESSION_INVALIDATOR,
  type IdentityAuditWriter,
  type SessionInvalidator,
} from '../identity/identity.contracts.js';
import { SUPERADMIN_FIXED_ADVISORY_KEY } from './user.types.js';
import { UserRepository } from './user.repository.js';

export class SuperAdminCliError extends Error {
  constructor(
    message: string,
    public readonly code: string,
  ) {
    super(message);
    this.name = 'SuperAdminCliError';
  }
}

export interface SuperAdminCommandInput {
  readonly actorUserId: string;
  readonly targetUserId: string;
  readonly reason: string;
  readonly correlationId: string;
}

export interface SuperAdminCommandResult {
  readonly status: 'granted' | 'revoked';
  readonly targetUserId: string;
  readonly actorUserId: string;
}

export const SUPERADMIN_LOCK_KEY = SUPERADMIN_FIXED_ADVISORY_KEY;

@Injectable()
export class SuperAdminRoleService {
  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(SESSION_INVALIDATOR) private readonly sessionInvalidator: SessionInvalidator,
    @Inject(IDENTITY_AUDIT_WRITER) private readonly audit: IdentityAuditWriter,
    private readonly users: UserRepository,
  ) {}

  async grant(input: SuperAdminCommandInput): Promise<SuperAdminCommandResult> {
    return this.execute(input, true);
  }

  async revoke(input: SuperAdminCommandInput): Promise<SuperAdminCommandResult> {
    return this.execute(input, false);
  }

  private async execute(
    input: SuperAdminCommandInput,
    desiredState: boolean,
  ): Promise<SuperAdminCommandResult> {
    const reason = input.reason.trim();
    if (reason === '') {
      throw new SuperAdminCliError('Reason is required.', 'REASON_REQUIRED');
    }

    return this.pool.transaction(async (client) => {
      await client.query(`SELECT pg_advisory_xact_lock(hashtext($1))`, [SUPERADMIN_LOCK_KEY]);

      // Actor must exist, be active and SuperAdmin.
      const actor = await this.clientQueryActor(client, input.actorUserId);
      if (actor === null) {
        throw new SuperAdminCliError('Actor was not found.', 'ACTOR_NOT_FOUND');
      }
      if (!actor.is_super_admin || actor.account_status !== 'active') {
        throw new SuperAdminCliError('Actor must be an active SuperAdmin.', 'ACTOR_NOT_SUPERADMIN');
      }

      const target = await this.clientQueryTarget(client, input.targetUserId);
      if (target === null) {
        throw new SuperAdminCliError('Target was not found.', 'TARGET_NOT_FOUND');
      }

      if (desiredState && target.account_status === 'deleted') {
        throw new SuperAdminCliError(
          'A deleted account cannot be granted SuperAdmin.',
          'TARGET_DELETED',
        );
      }

      // Self-revoke is forbidden.
      if (!desiredState && input.actorUserId === input.targetUserId) {
        throw new SuperAdminCliError(
          'A SuperAdmin cannot revoke their own role.',
          'SELF_REVOKE_FORBIDDEN',
        );
      }

      // Last-active invariant: a revoke that would leave zero active
      // SuperAdmins is refused. A grant that would leave zero is impossible
      // because the actor itself is an active SuperAdmin.
      if (!desiredState && target.is_super_admin && target.account_status === 'active') {
        const remaining = await this.users.countActiveSuperAdminsForUpdate(client);
        if (remaining - 1 <= 0) {
          throw new SuperAdminCliError(
            'Cannot revoke the last active SuperAdmin.',
            'LAST_SUPERADMIN',
          );
        }
      }

      if (target.is_super_admin === desiredState) {
        // Idempotent no-op: still log and rotate nothing.
        return {
          status: desiredState ? 'granted' : 'revoked',
          targetUserId: target.id,
          actorUserId: actor.id,
        };
      }

      await client.query(
        `UPDATE public.lu_user
            SET is_super_admin = $2,
                modified_by_user_id = $3
          WHERE id = $1`,
        [target.id, desiredState, actor.id],
      );

      // Section 12: global_role_change always rotates the subject version.
      await this.sessionInvalidator.rotateAndRevokeAll(client, target.id, 'global_role_change');
      await this.audit.append(client, {
        action: desiredState ? 'superadmin_granted' : 'superadmin_revoked',
        subjectUserId: target.id,
        actorUserId: actor.id,
        correlationId: input.correlationId,
        reason,
        metadata: { previousState: target.is_super_admin },
      });

      return {
        status: desiredState ? 'granted' : 'revoked',
        targetUserId: target.id,
        actorUserId: actor.id,
      };
    });
  }

  private async clientQueryActor(
    client: IPgClient,
    actorId: string,
  ): Promise<{ id: string; is_super_admin: boolean; account_status: string } | null> {
    const result = await client.query<{
      id: string;
      is_super_admin: boolean;
      account_status: string;
    }>(
      `SELECT id, is_super_admin, account_status
         FROM public.lu_user
        WHERE id = $1
        FOR UPDATE`,
      [actorId],
    );
    return result.rows[0] ?? null;
  }

  private async clientQueryTarget(
    client: IPgClient,
    targetId: string,
  ): Promise<{ id: string; is_super_admin: boolean; account_status: string } | null> {
    const result = await client.query<{
      id: string;
      is_super_admin: boolean;
      account_status: string;
    }>(
      `SELECT id, is_super_admin, account_status
         FROM public.lu_user
        WHERE id = $1
        FOR UPDATE`,
      [targetId],
    );
    return result.rows[0] ?? null;
  }
}
