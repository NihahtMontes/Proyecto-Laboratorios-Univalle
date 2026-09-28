import { Inject, Injectable } from '@nestjs/common';
import { randomUUID } from 'node:crypto';
import { AUTH_CONFIG, AUTH_PG_POOL } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import {
  AuthForbiddenException,
  AuthServiceUnavailableException,
  AuthUnauthorizedException,
} from './auth.exceptions.js';
import type { IPgClient, IPgPool } from './auth.pg-pool.js';
import type {
  IdentityMembership,
  IdentitySite,
  IdentityUser,
  AuthSessionRecord,
} from './auth.types.js';
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus,
  PasswordScheme,
  SessionPurpose,
} from '../identity/identity.contracts.js';
import type { SiteStatus } from './auth.types.js';

interface DbIdentityUserRow {
  readonly id: string;
  readonly username: string | null;
  readonly email: string;
  readonly first_name: string;
  readonly last_name: string;
  readonly second_last_name: string | null;
  readonly full_name: string;
  readonly identity_card: string;
  readonly phone_number: string;
  readonly account_status: AccountStatus;
  readonly is_super_admin: boolean;
  readonly password_scheme: PasswordScheme;
  readonly password_hash: string | null;
  readonly must_change_password: boolean;
  readonly password_migrated_at: Date | null;
  readonly security_version: string;
}

interface DbMembershipJoined {
  readonly user_id: string;
  readonly site_id: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly valid_from: Date | null;
  readonly valid_until: Date | null;
  readonly site_code: string;
  readonly site_name: string;
  readonly site_status: SiteStatus;
}

interface DbSessionRow {
  readonly id: string;
  readonly token_hash: string;
  readonly user_id: string;
  readonly active_site_id: string | null;
  readonly purpose: SessionPurpose;
  readonly security_version: string;
  readonly created_at: Date;
  readonly last_seen_at: Date;
  readonly idle_expires_at: Date;
  readonly absolute_expires_at: Date;
  readonly revoked_at: Date | null;
  readonly revocation_reason: string | null;
}

interface DbSessionWithIdentityRow extends DbSessionRow {
  readonly user_id: string;
  readonly username: string | null;
  readonly email: string;
  readonly first_name: string;
  readonly last_name: string;
  readonly second_last_name: string | null;
  readonly full_name: string;
  readonly identity_card: string;
  readonly phone_number: string;
  readonly account_status: AccountStatus;
  readonly is_super_admin: boolean;
  readonly password_scheme: PasswordScheme;
  readonly password_hash: string | null;
  readonly must_change_password: boolean;
  readonly security_version: string;
  readonly user_security_version: string;
}

export type SecurityEventType =
  | 'login_success'
  | 'login_failure'
  | 'login_rate_limited'
  | 'logout'
  | 'active_site_changed'
  | 'admin_bootstrap'
  | 'identity_audit';

export interface SecurityEventInput {
  readonly eventType: SecurityEventType;
  readonly userId?: string | null;
  readonly siteId?: string | null;
  readonly subjectHash?: string | null;
  readonly ipHash?: string | null;
  readonly occurredAt: Date;
  readonly metadata?: Readonly<Record<string, string | number | boolean | null>>;
}

const SECURITY_VERSION_REGEX = /^\d+$/;

function parseSecurityVersion(value: unknown): string {
  if (typeof value !== 'string' || !SECURITY_VERSION_REGEX.test(value)) {
    throw new AuthUnauthorizedException();
  }
  return value;
}

function mapIdentitySite(row: DbMembershipJoined): IdentitySite {
  return {
    id: row.site_id,
    code: row.site_code,
    name: row.site_name,
    status: row.site_status,
  };
}

function mapIdentityMembership(row: DbMembershipJoined): IdentityMembership {
  return {
    userId: row.user_id,
    siteId: row.site_id,
    role: row.role,
    status: row.status,
    validFrom: row.valid_from,
    validUntil: row.valid_until,
    site: mapIdentitySite(row),
  };
}

function mapIdentityUser(row: DbIdentityUserRow): IdentityUser {
  return {
    id: row.id,
    username: row.username ?? '',
    email: row.email,
    firstName: row.first_name,
    lastName: row.last_name,
    secondLastName: row.second_last_name,
    fullName: row.full_name,
    identityCard: row.identity_card,
    phoneNumber: row.phone_number,
    accountStatus: row.account_status,
    isSuperAdmin: row.is_super_admin,
    passwordScheme: row.password_scheme,
    passwordHash: row.password_hash,
    mustChangePassword: row.must_change_password,
    securityVersion: parseSecurityVersion(row.security_version),
  };
}

function mapIdentityUserFromSession(row: DbSessionWithIdentityRow): IdentityUser {
  return {
    id: row.user_id,
    username: row.username ?? '',
    email: row.email,
    firstName: row.first_name,
    lastName: row.last_name,
    secondLastName: row.second_last_name,
    fullName: row.full_name,
    identityCard: row.identity_card,
    phoneNumber: row.phone_number,
    accountStatus: row.account_status,
    isSuperAdmin: row.is_super_admin,
    passwordScheme: row.password_scheme,
    passwordHash: row.password_hash ?? null,
    mustChangePassword: row.must_change_password,
    securityVersion: parseSecurityVersion(row.user_security_version),
  };
}

function mapSession(row: DbSessionRow): AuthSessionRecord {
  return {
    id: row.id,
    tokenHash: row.token_hash,
    userId: row.user_id,
    activeSiteId: row.active_site_id,
    purpose: row.purpose,
    securityVersion: parseSecurityVersion(row.security_version),
    createdAt: row.created_at,
    lastSeenAt: row.last_seen_at,
    idleExpiresAt: row.idle_expires_at,
    absoluteExpiresAt: row.absolute_expires_at,
    revokedAt: row.revoked_at,
    revocationReason: row.revocation_reason,
  };
}

@Injectable()
export class AuthRepository {
  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
  ) {}

  /**
   * Pool accessor for callers that need to drive the same connection outside
   * of a structured repository method (e.g. forwarding identity-audit client
   * when no other transaction is in flight). The audit writer owns its own
   * transaction when no client is provided, so this is only used for the
   * fail-closed legacy-window probe and audit appends outside the SQL
   * repository.
   */
  getPool(): IPgPool {
    return this.pool;
  }

  private async insertSecurityEvent(client: IPgClient, event: SecurityEventInput): Promise<void> {
    await client.query(
      `INSERT INTO public.lu_security_event
         (id, event_type, user_id, site_id, subject_hash, ip_hash, occurred_at, metadata)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $8::jsonb)`,
      [
        randomUUID(),
        event.eventType,
        event.userId ?? null,
        event.siteId ?? null,
        event.subjectHash ?? null,
        event.ipHash ?? null,
        event.occurredAt,
        JSON.stringify(event.metadata ?? {}),
      ],
    );
  }

  async recordSecurityEvent(event: SecurityEventInput): Promise<void> {
    await this.insertSecurityEvent(this.pool, event);
  }

  async consumeLoginRateLimit(args: {
    readonly identifierIpKeyHash: string;
    readonly ipKeyHash: string;
    readonly subjectHash: string;
    readonly ipHash: string;
    readonly now: Date;
    readonly windowSeconds: number;
    readonly identifierIpMaxAttempts: number;
    readonly ipMaxAttempts: number;
  }): Promise<number | null> {
    const resetAt = new Date(args.now.getTime() + args.windowSeconds * 1000);
    const cleanupBefore = new Date(args.now.getTime() - 60 * 60 * 1000);

    return this.pool.transaction(async (client) => {
      await client.query(
        `DELETE FROM public.lu_auth_rate_limit
         WHERE (scope, key_hash) IN (
           SELECT scope, key_hash
           FROM public.lu_auth_rate_limit
           WHERE reset_at < $1
           ORDER BY reset_at
           LIMIT 100
         )`,
        [cleanupBefore],
      );

      // Stored scope labels are fixed by the frozen 0002 CHECK (`scope IN ('email_ip', 'ip')`).
      // The identifier+IP bucket keeps the historical `email_ip` label; its key hash is
      // derived from the normalized login identifier (username or email) and the IP.
      const consumeBucket = async (scope: 'email_ip' | 'ip', keyHash: string) => {
        const result = await client.query<{
          attempt_count: number;
          reset_at: Date;
        }>(
          `INSERT INTO public.lu_auth_rate_limit AS bucket
             (scope, key_hash, attempt_count, window_started_at, reset_at, updated_at)
           VALUES ($1, $2, 1, $3, $4, $3)
           ON CONFLICT (scope, key_hash) DO UPDATE
           SET attempt_count = CASE
                 WHEN bucket.reset_at <= EXCLUDED.window_started_at THEN 1
                 ELSE bucket.attempt_count + 1
               END,
               window_started_at = CASE
                 WHEN bucket.reset_at <= EXCLUDED.window_started_at
                   THEN EXCLUDED.window_started_at
                 ELSE bucket.window_started_at
               END,
               reset_at = CASE
                 WHEN bucket.reset_at <= EXCLUDED.window_started_at THEN EXCLUDED.reset_at
                 ELSE bucket.reset_at
               END,
               updated_at = EXCLUDED.updated_at
           RETURNING attempt_count, reset_at`,
          [scope, keyHash, args.now, resetAt],
        );
        const row = result.rows[0];
        if (row === undefined) {
          throw new AuthServiceUnavailableException();
        }
        return row;
      };

      const identifierIp = await consumeBucket('email_ip', args.identifierIpKeyHash);
      const ip = await consumeBucket('ip', args.ipKeyHash);
      const blocked =
        identifierIp.attempt_count > args.identifierIpMaxAttempts ||
        ip.attempt_count > args.ipMaxAttempts;
      if (!blocked) return null;

      await this.insertSecurityEvent(client, {
        eventType: 'login_rate_limited',
        subjectHash: args.subjectHash,
        ipHash: args.ipHash,
        occurredAt: args.now,
        metadata: {},
      });
      const retryAt = identifierIp.reset_at > ip.reset_at ? identifierIp.reset_at : ip.reset_at;
      return Math.max(1, Math.ceil((retryAt.getTime() - args.now.getTime()) / 1000));
    });
  }

  /**
   * Read-only session lookup (no FOR UPDATE, no idle/seen side effects). Used by
   * the password-change flow to resolve the sessioned user before consuming the
   * dedicated rate-limit bucket keyed by `password-change:<userId>` + ip, so an
   * invalid/expired/revoked session fails 401 without consuming the bucket. The
   * locked re-read with idle extension is still performed inside the change
   * transaction (see `findSessionByTokenHashForUpdate`).
   */
  async findSessionByTokenHash(
    tokenHash: string,
    now: Date,
  ): Promise<{ session: AuthSessionRecord; identity: IdentityUser } | null> {
    const result = await this.pool.query<DbSessionWithIdentityRow>(
      `SELECT s.id, s.token_hash, s.user_id, s.active_site_id, s.purpose, s.security_version,
              s.created_at, s.last_seen_at, s.idle_expires_at, s.absolute_expires_at,
              s.revoked_at, s.revocation_reason,
              u.id AS user_id, u.username, u.email, u.first_name, u.last_name,
              u.second_last_name, u.full_name, u.identity_card, u.phone_number,
              u.account_status, u.is_super_admin, u.password_scheme, u.password_hash,
              u.must_change_password, u.security_version AS user_security_version
       FROM public.lu_session s
       JOIN public.lu_user u ON u.id = s.user_id
       WHERE s.token_hash = $1
         AND s.revoked_at IS NULL
         AND s.idle_expires_at > $2
         AND s.absolute_expires_at > $2`,
      [tokenHash, now],
    );
    if ((result.rowCount ?? 0) === 0) {
      return null;
    }
    const row = result.rows[0];
    if (row === undefined) {
      return null;
    }
    return {
      session: mapSession(row),
      identity: mapIdentityUserFromSession(row),
    };
  }

  /**
   * Resolve the user through a single lu_login_identifier claim lookup, joining the
   * user row to pull every field the F1 canonical contract requires for login.
   * Returns null when no claim matches the supplied identifier; the password
   * scheme/hash is part of the row so the verifier can pick the right strategy
   * without an extra round-trip.
   */
  async findIdentityByLoginIdentifier(identifier: string): Promise<IdentityUser | null> {
    const result = await this.pool.query<DbIdentityUserRow>(
      `SELECT u.id, u.username, u.email, u.first_name, u.last_name, u.second_last_name,
              u.full_name, u.identity_card, u.phone_number, u.account_status,
              u.is_super_admin, u.password_scheme, u.password_hash, u.must_change_password,
              u.password_migrated_at, u.security_version
       FROM public.lu_login_identifier li
       JOIN public.lu_user u ON u.id = li.user_id
       WHERE li.normalized_value = public.lu_login_identifier_normalize($1)
       LIMIT 1`,
      [identifier],
    );
    if ((result.rowCount ?? 0) === 0) {
      return null;
    }
    const row = result.rows[0];
    if (row === undefined) {
      return null;
    }
    return mapIdentityUser(row);
  }

  async findIdentityById(id: string): Promise<IdentityUser | null> {
    const result = await this.pool.query<DbIdentityUserRow>(
      `SELECT id, username, email, first_name, last_name, second_last_name,
              full_name, identity_card, phone_number, account_status,
              is_super_admin, password_scheme, password_hash, must_change_password,
              password_migrated_at, security_version
       FROM public.lu_user
       WHERE id = $1
       LIMIT 1`,
      [id],
    );
    if ((result.rowCount ?? 0) === 0) {
      return null;
    }
    const row = result.rows[0];
    if (row === undefined) {
      return null;
    }
    return mapIdentityUser(row);
  }

  async findEligibleMemberships(
    userId: string,
    now: Date,
    client: IPgClient = this.pool,
  ): Promise<IdentityMembership[]> {
    const result = await client.query<DbMembershipJoined>(
      `SELECT m.user_id, m.site_id, m.role, m.status, m.valid_from, m.valid_until,
              s.code AS site_code, s.name AS site_name, s.status AS site_status
       FROM public.lu_site_membership m
       JOIN public.lu_site s ON s.id = m.site_id
       WHERE m.user_id = $1
         AND m.status = 'active'
         AND s.status = 'active'
         AND (m.valid_from IS NULL OR m.valid_from <= $2)
         AND (m.valid_until IS NULL OR m.valid_until > $2)
       ORDER BY s.code`,
      [userId, now],
    );
    return (result.rows ?? []).map(mapIdentityMembership);
  }

  /**
   * Insert a new session row with the supplied purpose/idle/absolute expiry and
   * security version snapshot. Used by the controller after a successful
   * verification (and possible rehash), so the row already reflects the new
   * `security_version` when the invalidator has rotated it.
   */
  async insertSession(args: {
    readonly userId: string;
    readonly activeSiteId: string | null;
    readonly purpose: SessionPurpose;
    readonly securityVersion: string;
    readonly tokenHash: string;
    readonly createdAt: Date;
    readonly idleExpiresAt: Date;
    readonly absoluteExpiresAt: Date;
    readonly siteId?: string | null;
    readonly subjectHash?: string;
    readonly ipHash?: string;
  }): Promise<AuthSessionRecord> {
    const result = await this.pool.query<DbSessionRow>(
      `INSERT INTO public.lu_session
         (id, token_hash, user_id, active_site_id, purpose, security_version,
          created_at, last_seen_at, idle_expires_at, absolute_expires_at)
       VALUES ($1, $2, $3, $4, $5, $6, $7, $7, $8, $9)
       RETURNING id, token_hash, user_id, active_site_id, purpose, security_version,
                 created_at, last_seen_at, idle_expires_at, absolute_expires_at,
                 revoked_at, revocation_reason`,
      [
        randomUUID(),
        args.tokenHash,
        args.userId,
        args.activeSiteId,
        args.purpose,
        args.securityVersion,
        args.createdAt,
        args.idleExpiresAt,
        args.absoluteExpiresAt,
      ],
    );
    const row = result.rows[0];
    if (row === undefined) {
      throw new AuthServiceUnavailableException();
    }
    return mapSession(row);
  }

  /**
   * Mark a successful login in the existing `lu_security_event` table (kept for
   * observability; the append-only `lu_identity_audit_event` table is owned by
   * the identity kernel).
   */
  async recordLoginSuccess(args: {
    readonly user: IdentityUser;
    readonly session: AuthSessionRecord;
    readonly now: Date;
    readonly subjectHash?: string;
    readonly ipHash?: string;
  }): Promise<void> {
    await this.pool.transaction(async (client) => {
      await this.insertSecurityEvent(client, {
        eventType: 'login_success',
        userId: args.user.id,
        siteId: args.session.activeSiteId,
        subjectHash: args.subjectHash,
        ipHash: args.ipHash,
        occurredAt: args.now,
        metadata: { purpose: args.session.purpose },
      });
    });
  }

  /**
   * Used by the password-change flow and the rehash-on-login flow once the
   * kernel has rotated the security version and revoked other sessions in the
   * same transaction. The actual writes happen inside the caller-provided
   * transaction so the new session issued by the caller can rely on the new
   * version atomically.
   */
  async findSessionByTokenHashForUpdate(
    client: IPgClient,
    tokenHash: string,
    now: Date,
  ): Promise<{
    row: DbSessionWithIdentityRow;
    session: AuthSessionRecord;
    identity: IdentityUser;
  } | null> {
    const result = await client.query<DbSessionWithIdentityRow>(
      `SELECT s.id, s.token_hash, s.user_id, s.active_site_id, s.purpose, s.security_version,
              s.created_at, s.last_seen_at, s.idle_expires_at, s.absolute_expires_at,
              s.revoked_at, s.revocation_reason,
              u.id AS user_id, u.username, u.email, u.first_name, u.last_name,
              u.second_last_name, u.full_name, u.identity_card, u.phone_number,
              u.account_status, u.is_super_admin, u.password_scheme, u.password_hash,
              u.must_change_password, u.security_version AS user_security_version
       FROM public.lu_session s
       JOIN public.lu_user u ON u.id = s.user_id
       WHERE s.token_hash = $1
         AND s.revoked_at IS NULL
         AND s.idle_expires_at > $2
         AND s.absolute_expires_at > $2
       FOR UPDATE OF s, u`,
      [tokenHash, now],
    );
    if ((result.rowCount ?? 0) === 0) {
      return null;
    }
    const row = result.rows[0];
    if (row === undefined) {
      return null;
    }
    return {
      row,
      session: mapSession(row),
      identity: mapIdentityUserFromSession(row),
    };
  }

  /**
   * Lookup the eligible memberships for a locked user. The query locks the
   * rows that may need to be re-evaluated after a concurrent membership
   * change.
   */
  async findEligibleMembershipsForUpdate(
    client: IPgClient,
    userId: string,
    now: Date,
  ): Promise<IdentityMembership[]> {
    const result = await client.query<DbMembershipJoined>(
      `SELECT m.user_id, m.site_id, m.role, m.status, m.valid_from, m.valid_until,
              s.code AS site_code, s.name AS site_name, s.status AS site_status
       FROM public.lu_site_membership m
       JOIN public.lu_site s ON s.id = m.site_id
       WHERE m.user_id = $1
         AND m.status = 'active'
         AND s.status = 'active'
         AND (m.valid_from IS NULL OR m.valid_from <= $2)
         AND (m.valid_until IS NULL OR m.valid_until > $2)
       ORDER BY s.code
       FOR UPDATE OF m, s`,
      [userId, now],
    );
    return (result.rows ?? []).map(mapIdentityMembership);
  }

  /**
   * Validates a presented session inside one control-plane transaction (F1 §17:
   * every authenticated request revalidates): the session row is locked, must
   * be unrevoked and unexpired, the account must be active and its
   * security_version must equal the session snapshot. Eligible memberships are
   * re-read; a stored active site that is no longer eligible is reported as
   * null (tenant context rejected, never replaced by a client value). Normal and
   * password_change sessions slide their idle expiry (capped by the absolute
   * expiry); site_selection sessions never idle-extend.
   */
  async getSessionContext(
    tokenHash: string,
    now: Date,
  ): Promise<{
    identity: IdentityUser;
    memberships: IdentityMembership[];
    session: AuthSessionRecord;
    activeSiteId: string | null;
  }> {
    this.config.validate();
    const idleSeconds = this.config.idleTtlSeconds;

    return this.pool.transaction(async (client) => {
      const found = await this.findSessionByTokenHashForUpdate(client, tokenHash, now);
      if (found === null) {
        throw new AuthUnauthorizedException();
      }
      const { identity, session } = found;
      if (
        identity.accountStatus !== 'active' ||
        identity.securityVersion !== session.securityVersion
      ) {
        throw new AuthUnauthorizedException();
      }
      const memberships = await this.findEligibleMemberships(identity.id, now, client);
      if (session.purpose === 'normal' && memberships.length === 0 && !identity.isSuperAdmin) {
        throw new AuthUnauthorizedException();
      }
      const activeSiteId =
        session.activeSiteId !== null && memberships.some((m) => m.siteId === session.activeSiteId)
          ? session.activeSiteId
          : null;

      let idleExpiresAt = session.idleExpiresAt;
      if (session.purpose !== 'site_selection') {
        const candidate = new Date(now.getTime() + idleSeconds * 1000);
        const bounded =
          candidate < session.absoluteExpiresAt ? candidate : session.absoluteExpiresAt;
        if (bounded > idleExpiresAt) {
          idleExpiresAt = bounded;
        }
      }
      await client.query(
        `UPDATE public.lu_session
            SET last_seen_at = GREATEST(last_seen_at, $2),
                idle_expires_at = GREATEST(idle_expires_at, $3)
          WHERE id = $1 AND revoked_at IS NULL`,
        [session.id, now, idleExpiresAt],
      );
      return {
        identity,
        memberships,
        session: { ...session, lastSeenAt: now, idleExpiresAt },
        activeSiteId,
      };
    });
  }

  /**
   * Rehash on login: locked user row, compare-and-swap on `(password_scheme,
   * password_hash)`, write the new bcrypt hash, set `must_change_password = false`
   * for legacy schemes, stamp `password_migrated_at`, but never touch
   * `password_migrated_at` when only a cost rehash was needed. The
   * `SessionInvalidator` (kernel) is responsible for rotating the security
   * version and revoking prior sessions in the same transaction; this method
   * runs inside that very transaction.
   *
   * Returns the snapshot of fields AFTER the rehash; caller passes the
   * new security_version when issuing the replacement session.
   */
  async rehashUserPassword(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedScheme: PasswordScheme;
    readonly expectedHash: string;
    readonly newHash: string;
    readonly previousSchemeForMigrationFlag: PasswordScheme;
    readonly now: Date;
  }): Promise<{ readonly newSecurityVersion: string }> {
    const {
      client,
      userId,
      expectedScheme,
      expectedHash,
      newHash,
      previousSchemeForMigrationFlag,
      now,
    } = args;
    const update = await client.query<{ security_version: string }>(
      `UPDATE public.lu_user
          SET password_scheme = 'bcrypt',
              password_hash = $2,
              must_change_password = false,
              password_migrated_at = CASE
                WHEN $3 = 'legacy_identity_v2' OR $3 = 'legacy_identity_v3' OR $3 = 'reset_required'
                  THEN $4
                ELSE password_migrated_at
              END,
              modified_by_user_id = NULL
        WHERE id = $1
          AND password_scheme = $5
          AND password_hash = $6
        RETURNING security_version`,
      [userId, newHash, previousSchemeForMigrationFlag, now, expectedScheme, expectedHash],
    );
    if ((update.rowCount ?? 0) === 0) {
      // CAS lost (concurrent rehash/password change); the caller turns this into a generic failure.
      throw new AuthUnauthorizedException();
    }
    const row = update.rows[0];
    if (row === undefined) {
      throw new AuthUnauthorizedException();
    }
    return { newSecurityVersion: parseSecurityVersion(row.security_version) };
  }

  /**
   * Cost rehash for an already-bcrypt account: leave `password_scheme`,
   * `must_change_password` and `password_migrated_at` untouched. The CALLER
   * must run this inside the same transaction that locks the user row. CAS is
   * on the previously verified hash so a concurrent rehash loses the second
   * writer and yields a generic failure (the winning writer still rehashes
   * safely; the losing writer surfaces 401).
   */
  async rehashUserPasswordBcryptCost(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedHash: string;
    readonly newHash: string;
  }): Promise<{ readonly newSecurityVersion: string }> {
    const { client, userId, expectedHash, newHash } = args;
    const update = await client.query<{ security_version: string }>(
      `UPDATE public.lu_user
          SET password_hash = $2
        WHERE id = $1
          AND password_scheme = 'bcrypt'
          AND password_hash = $3
        RETURNING security_version`,
      [userId, newHash, expectedHash],
    );
    if ((update.rowCount ?? 0) === 0) {
      throw new AuthUnauthorizedException();
    }
    const row = update.rows[0];
    if (row === undefined) {
      throw new AuthUnauthorizedException();
    }
    return { newSecurityVersion: parseSecurityVersion(row.security_version) };
  }

  async changePasswordHash(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedHash: string;
    readonly expectedScheme: PasswordScheme;
    readonly newHash: string;
    readonly migrateAt: Date;
  }): Promise<{ readonly newSecurityVersion: string }> {
    const { client, userId, expectedHash, expectedScheme, newHash, migrateAt } = args;
    const update = await client.query<{ security_version: string }>(
      `UPDATE public.lu_user
          SET password_scheme = 'bcrypt',
              password_hash = $2,
              must_change_password = false,
              password_migrated_at = CASE
                WHEN $3 IN ('legacy_identity_v2', 'legacy_identity_v3', 'reset_required')
                  THEN $4
                ELSE password_migrated_at
              END
        WHERE id = $1
          AND password_scheme = $5
          AND password_hash = $6
        RETURNING security_version`,
      [userId, newHash, expectedScheme, migrateAt, expectedScheme, expectedHash],
    );
    if ((update.rowCount ?? 0) === 0) {
      throw new AuthUnauthorizedException();
    }
    const row = update.rows[0];
    if (row === undefined) {
      throw new AuthUnauthorizedException();
    }
    return { newSecurityVersion: parseSecurityVersion(row.security_version) };
  }

  async setActiveSiteContext(args: {
    readonly tokenHash: string;
    readonly activeSiteId: string;
    readonly now: Date;
  }): Promise<{
    identity: IdentityUser;
    memberships: IdentityMembership[];
    session: AuthSessionRecord;
  }> {
    this.config.validate();
    const idleSeconds = this.config.idleTtlSeconds;

    return this.pool.transaction(async (client) => {
      const sessionResult = await client.query<DbSessionWithIdentityRow>(
        `SELECT s.id, s.token_hash, s.user_id, s.active_site_id, s.purpose, s.security_version,
                s.created_at, s.last_seen_at, s.idle_expires_at, s.absolute_expires_at,
                s.revoked_at, s.revocation_reason,
                u.id AS user_id, u.username, u.email, u.first_name, u.last_name,
                u.second_last_name, u.full_name, u.identity_card, u.phone_number,
                u.account_status, u.is_super_admin, u.password_scheme, u.password_hash,
                u.must_change_password, u.security_version AS user_security_version
         FROM public.lu_session s
         JOIN public.lu_user u ON u.id = s.user_id
         WHERE s.token_hash = $1
           AND s.revoked_at IS NULL
           AND s.idle_expires_at > $2
           AND s.absolute_expires_at > $2
         FOR UPDATE OF s, u`,
        [args.tokenHash, args.now],
      );
      const row = sessionResult.rows[0];
      if (row === undefined) {
        throw new AuthUnauthorizedException();
      }
      const identity = mapIdentityUserFromSession(row);
      const session = mapSession(row);
      if (
        identity.accountStatus !== 'active' ||
        identity.securityVersion !== session.securityVersion
      ) {
        throw new AuthUnauthorizedException();
      }
      if (session.purpose === 'password_change') {
        // A password_change session has no site capability (F1 §1).
        throw new AuthForbiddenException('SITE_ACCESS_DENIED');
      }
      const memberships = await this.findEligibleMembershipsForUpdate(
        client,
        identity.id,
        args.now,
      );
      const belongs = memberships.some((m) => m.siteId === args.activeSiteId);
      if (!belongs) {
        throw new AuthForbiddenException('SITE_ACCESS_DENIED');
      }

      // Case 1: a site_selection session is revoked here; the service issues
      // the replacement normal session for the chosen, eligible site.
      if (session.purpose === 'site_selection') {
        await client.query(
          `UPDATE public.lu_session
             SET revoked_at = $2, revocation_reason = $3
           WHERE id = $1`,
          [session.id, args.now, 'site_selection_resolved'],
        );
        return {
          identity,
          memberships,
          session: { ...session, revokedAt: args.now, revocationReason: 'site_selection_resolved' },
        };
      }

      // Case 2: normal session -> active site change only, no security_version rotation.

      const candidateIdle = new Date(args.now.getTime() + idleSeconds * 1000);
      const boundedIdle =
        candidateIdle < session.absoluteExpiresAt ? candidateIdle : session.absoluteExpiresAt;

      const updated = await client.query<DbSessionRow>(
        `UPDATE public.lu_session
         SET active_site_id = $2,
             last_seen_at = GREATEST(last_seen_at, $3),
             idle_expires_at = GREATEST(idle_expires_at, $4)
         WHERE id = $1
           AND revoked_at IS NULL
           AND idle_expires_at > $3
           AND absolute_expires_at > $3
         RETURNING id, token_hash, user_id, active_site_id, purpose, security_version,
                   created_at, last_seen_at, idle_expires_at, absolute_expires_at,
                   revoked_at, revocation_reason`,
        [session.id, args.activeSiteId, args.now, boundedIdle],
      );
      const updatedRow = updated.rows[0];
      if (updatedRow === undefined) {
        throw new AuthUnauthorizedException();
      }
      const nextSession = mapSession(updatedRow);
      await this.insertSecurityEvent(client, {
        eventType: 'active_site_changed',
        userId: identity.id,
        siteId: args.activeSiteId,
        occurredAt: args.now,
        metadata: { mode: 'normal_session' },
      });
      return { identity, memberships, session: nextSession };
    });
  }

  async revokeSession(tokenHash: string, reason: string): Promise<void> {
    await this.pool.transaction(async (client) => {
      const session = await client.query<{
        id: string;
        user_id: string;
        active_site_id: string | null;
      }>(
        `SELECT id, user_id, active_site_id
         FROM public.lu_session
         WHERE token_hash = $1 AND revoked_at IS NULL
         FOR UPDATE`,
        [tokenHash],
      );
      const row = session.rows[0];
      if (row === undefined) return;
      const now = new Date();
      await client.query(
        `UPDATE public.lu_session
         SET revoked_at = $2, revocation_reason = $3
         WHERE id = $1 AND revoked_at IS NULL`,
        [row.id, now, reason],
      );
      await this.insertSecurityEvent(client, {
        eventType: 'logout',
        userId: row.user_id,
        siteId: row.active_site_id,
        occurredAt: now,
        metadata: {},
      });
    });
  }

  /**
   * Run an existing control-plane transaction that the kernel already opened.
   * Used so the password-change flow can hash + rotate + write session in one
   * transaction. The pool simply forwards the typed `query/transaction` to
   * whichever client the caller hands in.
   */
  async inTransaction<T>(work: (client: IPgClient) => Promise<T>): Promise<T> {
    return this.pool.transaction(work);
  }
}

// Re-exports for tests/fakes.
export type { DbIdentityUserRow, DbMembershipJoined, DbSessionRow, DbSessionWithIdentityRow };
