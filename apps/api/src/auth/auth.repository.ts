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
  AuthSessionContext,
  Membership,
  MembershipRole,
  Session,
  SiteStatus,
  User,
  UserStatus,
} from './auth.types.js';

interface DbUser {
  id: string;
  email: string;
  full_name: string;
  password_hash: string;
  is_super_admin: boolean;
  status: UserStatus;
  security_version: string;
}

interface DbMembershipJoined {
  user_id: string;
  site_id: string;
  role: MembershipRole;
  status: 'active' | 'suspended' | 'revoked';
  valid_from: Date | null;
  valid_until: Date | null;
  site_code: string;
  site_name: string;
  site_status: SiteStatus;
}

interface DbSession {
  id: string;
  token_hash: string;
  user_id: string;
  active_site_id: string | null;
  security_version: string;
  created_at: Date;
  last_seen_at: Date;
  idle_expires_at: Date;
  absolute_expires_at: Date;
  revoked_at: Date | null;
  revocation_reason: string | null;
}

interface DbSessionWithUserRow {
  readonly id: string;
  readonly token_hash: string;
  readonly session_user_id: string;
  readonly session_active_site_id: string | null;
  readonly security_version: string;
  readonly created_at: Date;
  readonly last_seen_at: Date;
  readonly idle_expires_at: Date;
  readonly absolute_expires_at: Date;
  readonly revoked_at: Date | null;
  readonly revocation_reason: string | null;
  readonly user_id: string;
  readonly email: string;
  readonly full_name: string;
  readonly password_hash: string;
  readonly is_super_admin: boolean;
  readonly user_status: UserStatus;
  readonly user_security_version: string;
  readonly active_site_id: string | null;
  readonly active_site_code: string | null;
  readonly active_site_name: string | null;
  readonly active_site_status: SiteStatus | null;
}

interface DbSessionUserRow extends DbSession {
  readonly email: string;
  readonly full_name: string;
  readonly password_hash: string;
  readonly is_super_admin: boolean;
  readonly user_status: UserStatus;
  readonly user_security_version: string;
}

interface RateLimitRow {
  attempt_count: number;
  reset_at: Date;
}

export type SecurityEventType =
  | 'login_success'
  | 'login_failure'
  | 'login_rate_limited'
  | 'logout'
  | 'active_site_changed'
  | 'admin_bootstrap';

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

function mapUser(row: DbUser): User {
  return {
    id: row.id,
    email: row.email,
    fullName: row.full_name,
    passwordHash: row.password_hash,
    isSuperAdmin: row.is_super_admin,
    status: row.status,
    securityVersion: parseSecurityVersion(row.security_version),
  };
}

function mapMembership(row: DbMembershipJoined): Membership {
  return {
    userId: row.user_id,
    siteId: row.site_id,
    role: row.role,
    status: row.status,
    validFrom: row.valid_from,
    validUntil: row.valid_until,
    site: {
      id: row.site_id,
      code: row.site_code,
      name: row.site_name,
      status: row.site_status,
    },
  };
}

function mapSession(row: DbSession): Session {
  return {
    id: row.id,
    tokenHash: row.token_hash,
    userId: row.user_id,
    activeSiteId: row.active_site_id,
    securityVersion: parseSecurityVersion(row.security_version),
    createdAt: row.created_at,
    lastSeenAt: row.last_seen_at,
    idleExpiresAt: row.idle_expires_at,
    absoluteExpiresAt: row.absolute_expires_at,
    revokedAt: row.revoked_at,
    revocationReason: row.revocation_reason,
  };
}

function mapUserFromSessionRow(row: DbSessionWithUserRow | DbSessionUserRow): User {
  return {
    id: row.user_id,
    email: row.email,
    fullName: row.full_name,
    passwordHash: row.password_hash,
    isSuperAdmin: row.is_super_admin,
    status: row.user_status,
    securityVersion: parseSecurityVersion(row.user_security_version),
  };
}

@Injectable()
export class AuthRepository {
  constructor(
    @Inject(AUTH_PG_POOL) private readonly pool: IPgPool,
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
  ) {}

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
    readonly emailIpKeyHash: string;
    readonly ipKeyHash: string;
    readonly subjectHash: string;
    readonly ipHash: string;
    readonly now: Date;
    readonly windowSeconds: number;
    readonly emailIpMaxAttempts: number;
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

      const consumeBucket = async (scope: 'email_ip' | 'ip', keyHash: string) => {
        const result = await client.query<RateLimitRow>(
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

      const emailIp = await consumeBucket('email_ip', args.emailIpKeyHash);
      const ip = await consumeBucket('ip', args.ipKeyHash);
      const blocked =
        emailIp.attempt_count > args.emailIpMaxAttempts || ip.attempt_count > args.ipMaxAttempts;
      if (!blocked) return null;

      await this.insertSecurityEvent(client, {
        eventType: 'login_rate_limited',
        subjectHash: args.subjectHash,
        ipHash: args.ipHash,
        occurredAt: args.now,
        metadata: {},
      });
      const retryAt = emailIp.reset_at > ip.reset_at ? emailIp.reset_at : ip.reset_at;
      return Math.max(1, Math.ceil((retryAt.getTime() - args.now.getTime()) / 1000));
    });
  }

  async findUserWithMembershipsByEmail(email: string): Promise<User | null> {
    const result = await this.pool.query<DbUser>(
      `SELECT id, email, full_name, password_hash, is_super_admin, status, security_version
       FROM public.lu_user
       WHERE lower(email) = lower($1)
       LIMIT 1`,
      [email],
    );
    if ((result.rowCount ?? 0) === 0) {
      return null;
    }
    const row = result.rows[0];
    if (row === undefined) {
      return null;
    }
    return mapUser(row);
  }

  async findUserById(id: string): Promise<User | null> {
    const result = await this.pool.query<DbUser>(
      `SELECT id, email, full_name, password_hash, is_super_admin, status, security_version
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
    return mapUser(row);
  }

  async findEligibleMemberships(userId: string, now: Date): Promise<Membership[]> {
    const result = await this.pool.query<DbMembershipJoined>(
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
    return (result.rows ?? []).map(mapMembership);
  }

  async createSession(args: {
    user: User;
    activeSiteId: string | null;
    tokenHash: string;
    now: Date;
    subjectHash?: string;
    ipHash?: string;
  }): Promise<Session> {
    this.config.validate();
    const idleSeconds = this.config.idleTtlSeconds;
    const absoluteSeconds = this.config.absoluteTtlSeconds;
    const idleExpiresAt = new Date(args.now.getTime() + idleSeconds * 1000);
    const absoluteExpiresAt = new Date(args.now.getTime() + absoluteSeconds * 1000);

    return this.pool.transaction(async (client) => {
      const userResult = await client.query<DbUser>(
        `SELECT id, status, security_version, is_super_admin
         FROM public.lu_user
         WHERE id = $1
           AND lower(email) = lower($2)
           AND status = 'active'
           AND security_version = $3
         FOR UPDATE`,
        [args.user.id, args.user.email, args.user.securityVersion],
      );
      if ((userResult.rowCount ?? 0) === 0) {
        throw new AuthUnauthorizedException();
      }
      const dbUser = userResult.rows[0];
      if (dbUser === undefined) {
        throw new AuthUnauthorizedException();
      }
      const dbIsSuperAdmin = dbUser.is_super_admin;

      if (args.activeSiteId !== null) {
        const membershipResult = await client.query<DbMembershipJoined>(
          `SELECT m.user_id, m.site_id, m.role, m.status, m.valid_from, m.valid_until,
                  s.code AS site_code, s.name AS site_name, s.status AS site_status
           FROM public.lu_site_membership m
           JOIN public.lu_site s ON s.id = m.site_id
           WHERE m.user_id = $1
             AND m.site_id = $2
             AND m.status = 'active'
             AND s.status = 'active'
             AND (m.valid_from IS NULL OR m.valid_from <= $3)
             AND (m.valid_until IS NULL OR m.valid_until > $3)
           FOR UPDATE OF m, s`,
          [args.user.id, args.activeSiteId, args.now],
        );
        if ((membershipResult.rowCount ?? 0) === 0) {
          throw new AuthUnauthorizedException();
        }
      } else if (dbIsSuperAdmin) {
        // SuperAdmin may hold a global session (activeSiteId = null) even with
        // no eligible memberships. The membership path above still applies when
        // an explicit active site is requested.
      } else {
        const eligibleResult = await client.query<DbMembershipJoined>(
          `SELECT m.user_id, m.site_id, m.role, m.status, m.valid_from, m.valid_until,
                  s.code AS site_code, s.name AS site_name, s.status AS site_status
           FROM public.lu_site_membership m
           JOIN public.lu_site s ON s.id = m.site_id
           WHERE m.user_id = $1
             AND m.status = 'active'
             AND s.status = 'active'
             AND (m.valid_from IS NULL OR m.valid_from <= $2)
             AND (m.valid_until IS NULL OR m.valid_until > $2)
           FOR UPDATE OF m, s`,
          [args.user.id, args.now],
        );
        if ((eligibleResult.rowCount ?? 0) === 0) {
          throw new AuthUnauthorizedException();
        }
      }

      const sessionResult = await client.query<DbSession>(
        `INSERT INTO public.lu_session
           (id, token_hash, user_id, active_site_id, security_version,
            created_at, last_seen_at, idle_expires_at, absolute_expires_at)
         VALUES ($1, $2, $3, $4, $5, $6, $6, $7, $8)
         RETURNING *`,
        [
          randomUUID(),
          args.tokenHash,
          args.user.id,
          args.activeSiteId,
          args.user.securityVersion,
          args.now,
          idleExpiresAt,
          absoluteExpiresAt,
        ],
      );
      const row = sessionResult.rows[0];
      if (row === undefined) {
        throw new AuthServiceUnavailableException();
      }
      await this.insertSecurityEvent(client, {
        eventType: 'login_success',
        userId: args.user.id,
        siteId: args.activeSiteId,
        subjectHash: args.subjectHash,
        ipHash: args.ipHash,
        occurredAt: args.now,
        metadata: {},
      });
      return mapSession(row);
    });
  }

  async getSessionContext(tokenHash: string, now: Date): Promise<AuthSessionContext> {
    this.config.validate();
    const idleSeconds = this.config.idleTtlSeconds;

    return this.pool.transaction(async (client) => {
      const sessionResult = await client.query<DbSessionWithUserRow>(
        `SELECT s.id, s.token_hash, s.user_id AS session_user_id, s.active_site_id AS session_active_site_id,
                s.security_version, s.created_at, s.last_seen_at, s.idle_expires_at,
                s.absolute_expires_at, s.revoked_at, s.revocation_reason,
                u.id AS user_id, u.email, u.full_name, u.password_hash,
                u.is_super_admin, u.status AS user_status,
                u.security_version AS user_security_version
         FROM public.lu_session s
         JOIN public.lu_user u ON u.id = s.user_id
         WHERE s.token_hash = $1
           AND s.revoked_at IS NULL
           AND s.idle_expires_at > $2
           AND s.absolute_expires_at > $2
         FOR UPDATE OF s, u`,
        [tokenHash, now],
      );
      if ((sessionResult.rowCount ?? 0) === 0) {
        throw new AuthUnauthorizedException();
      }
      const row = sessionResult.rows[0];
      if (row === undefined) {
        throw new AuthUnauthorizedException();
      }

      const user = mapUserFromSessionRow(row);
      const sessionSecurityVersion = parseSecurityVersion(row.security_version);
      if (user.status !== 'active' || user.securityVersion !== sessionSecurityVersion) {
        throw new AuthUnauthorizedException();
      }

      const session: Session = {
        id: row.id,
        tokenHash: row.token_hash,
        userId: row.session_user_id,
        activeSiteId: row.session_active_site_id,
        securityVersion: sessionSecurityVersion,
        createdAt: row.created_at,
        lastSeenAt: row.last_seen_at,
        idleExpiresAt: row.idle_expires_at,
        absoluteExpiresAt: row.absolute_expires_at,
        revokedAt: row.revoked_at,
        revocationReason: row.revocation_reason,
      };

      const membershipsResult = await client.query<DbMembershipJoined>(
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
        [user.id, now],
      );
      const memberships = (membershipsResult.rows ?? []).map(mapMembership);

      const newActiveSiteId =
        session.activeSiteId !== null && memberships.some((m) => m.siteId === session.activeSiteId)
          ? session.activeSiteId
          : null;

      if (memberships.length === 0 && !user.isSuperAdmin) {
        throw new AuthUnauthorizedException();
      }

      const candidateIdle = new Date(now.getTime() + idleSeconds * 1000);
      const boundedIdle =
        candidateIdle < session.absoluteExpiresAt ? candidateIdle : session.absoluteExpiresAt;

      const updateResult = await client.query<DbSession>(
        `UPDATE public.lu_session
         SET active_site_id = $2,
             last_seen_at = GREATEST(last_seen_at, $3),
             idle_expires_at = GREATEST(idle_expires_at, LEAST($4, absolute_expires_at))
         WHERE id = $1
           AND revoked_at IS NULL
           AND idle_expires_at > $3
           AND absolute_expires_at > $3
         RETURNING *`,
        [session.id, newActiveSiteId, now, boundedIdle],
      );
      if ((updateResult.rowCount ?? 0) === 0) {
        throw new AuthUnauthorizedException();
      }

      return { user, memberships, activeSiteId: newActiveSiteId };
    });
  }

  async setActiveSiteContext(args: {
    tokenHash: string;
    activeSiteId: string;
    now: Date;
  }): Promise<AuthSessionContext> {
    this.config.validate();
    const idleSeconds = this.config.idleTtlSeconds;

    return this.pool.transaction(async (client) => {
      const sessionResult = await client.query<DbSessionUserRow>(
        `SELECT s.*,
                u.status AS user_status,
                u.security_version AS user_security_version,
                u.email, u.full_name, u.password_hash, u.is_super_admin
         FROM public.lu_session s
         JOIN public.lu_user u ON u.id = s.user_id
         WHERE s.token_hash = $1
           AND s.revoked_at IS NULL
           AND s.idle_expires_at > $2
           AND s.absolute_expires_at > $2
         FOR UPDATE OF s, u`,
        [args.tokenHash, args.now],
      );
      if ((sessionResult.rowCount ?? 0) === 0) {
        throw new AuthUnauthorizedException();
      }
      const row = sessionResult.rows[0];
      if (row === undefined) {
        throw new AuthUnauthorizedException();
      }

      const user = mapUserFromSessionRow(row);
      const session = mapSession(row);
      const userSecurityVersion = parseSecurityVersion(row.user_security_version);

      if (user.status !== 'active' || userSecurityVersion !== session.securityVersion) {
        throw new AuthUnauthorizedException();
      }

      const membershipsResult = await client.query<DbMembershipJoined>(
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
        [session.userId, args.now],
      );
      const memberships = (membershipsResult.rows ?? []).map(mapMembership);

      const belongs = memberships.some((m) => m.siteId === args.activeSiteId);
      if (!belongs) {
        throw new AuthForbiddenException('SITE_ACCESS_DENIED');
      }

      const candidateIdle = new Date(args.now.getTime() + idleSeconds * 1000);
      const boundedIdle =
        candidateIdle < session.absoluteExpiresAt ? candidateIdle : session.absoluteExpiresAt;

      const updateResult = await client.query<DbSession>(
        `UPDATE public.lu_session
         SET active_site_id = $2,
             last_seen_at = GREATEST(last_seen_at, $3),
             idle_expires_at = GREATEST(idle_expires_at, LEAST($4, absolute_expires_at))
         WHERE id = $1
           AND revoked_at IS NULL
           AND idle_expires_at > $3
           AND absolute_expires_at > $3
         RETURNING *`,
        [session.id, args.activeSiteId, args.now, boundedIdle],
      );
      if ((updateResult.rowCount ?? 0) === 0) {
        throw new AuthUnauthorizedException();
      }

      await this.insertSecurityEvent(client, {
        eventType: 'active_site_changed',
        userId: user.id,
        siteId: args.activeSiteId,
        occurredAt: args.now,
        metadata: {},
      });

      return { user, memberships, activeSiteId: args.activeSiteId };
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
}
