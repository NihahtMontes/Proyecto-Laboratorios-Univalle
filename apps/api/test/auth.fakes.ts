import { AuthForbiddenException, AuthUnauthorizedException } from '../src/auth/auth.exceptions.js';
import type { SecurityEventInput } from '../src/auth/auth.repository.js';
import { randomUUID } from 'node:crypto';
import type { IPgClient, IPgPool } from '../src/auth/auth.pg-pool.js';
import type {
  AuthSessionRecord,
  IdentityMembership,
  IdentitySite,
  IdentityUser,
} from '../src/auth/auth.types.js';
import type {
  AccountStatus,
  MembershipRole,
  MembershipStatus,
  PasswordScheme,
  SessionPurpose,
  SiteStatus,
} from '../src/auth/auth.types.js';

export type QueryLog = { readonly sql: string; readonly params: readonly unknown[] };

export class FakePgPool implements IPgPool {
  readonly queries: QueryLog[] = [];
  private nextResults: Array<{ rows: unknown[]; rowCount: number | null } | { error: Error }> = [];

  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    this.queries.push({ sql, params: params ?? [] });
    const next = this.nextResults.shift();
    if (next === undefined) {
      throw new Error('FakePgPool: no next result configured');
    }
    if ('error' in next) {
      throw next.error;
    }
    return Promise.resolve({ rows: next.rows as T[], rowCount: next.rowCount });
  }

  async transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T> {
    await this.query('BEGIN');
    try {
      const result = await fn(this);
      await this.query('COMMIT');
      return result;
    } catch (error) {
      await this.query('ROLLBACK');
      throw error;
    }
  }

  queueResult(rows: unknown[], rowCount: number | null = rows.length): void {
    this.nextResults.push({ rows, rowCount });
  }

  queueError(error: Error): void {
    this.nextResults.push({ error });
  }

  queueIdentityLookup(row: IdentityUser | null): void {
    if (row === null) {
      this.queueResult([]);
      return;
    }
    this.queueResult([
      {
        id: row.id,
        username: row.username,
        email: row.email,
        first_name: row.firstName,
        last_name: row.lastName,
        second_last_name: row.secondLastName,
        full_name: row.fullName,
        identity_card: row.identityCard,
        phone_number: row.phoneNumber,
        account_status: row.accountStatus,
        is_super_admin: row.isSuperAdmin,
        password_scheme: row.passwordScheme,
        password_hash: row.passwordHash,
        must_change_password: row.mustChangePassword,
        security_version: row.securityVersion,
      },
    ]);
  }

  queueSessionWithIdentityRow(session: AuthSessionRecord, identity: IdentityUser): void {
    this.queueResult([
      {
        id: session.id,
        token_hash: session.tokenHash,
        user_id: session.userId,
        active_site_id: session.activeSiteId,
        purpose: session.purpose,
        security_version: String(session.securityVersion),
        created_at: session.createdAt,
        last_seen_at: session.lastSeenAt,
        idle_expires_at: session.idleExpiresAt,
        absolute_expires_at: session.absoluteExpiresAt,
        revoked_at: session.revokedAt,
        revocation_reason: session.revocationReason,
        username: identity.username,
        email: identity.email,
        first_name: identity.firstName,
        last_name: identity.lastName,
        second_last_name: identity.secondLastName,
        full_name: identity.fullName,
        identity_card: identity.identityCard,
        phone_number: identity.phoneNumber,
        account_status: identity.accountStatus,
        is_super_admin: identity.isSuperAdmin,
        password_scheme: identity.passwordScheme,
        password_hash: identity.passwordHash,
        must_change_password: identity.mustChangePassword,
        user_security_version: identity.securityVersion,
      },
    ]);
  }

  reset(): void {
    this.queries.length = 0;
    this.nextResults = [];
  }
}

export function createIdentity(overrides?: Partial<IdentityUser>): IdentityUser {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    username: 'test.user',
    email: 'user@example.com',
    firstName: 'Test',
    lastName: 'User',
    secondLastName: null,
    fullName: 'Test User',
    identityCard: 'A1234567',
    phoneNumber: '+15555550100',
    accountStatus: 'active' as AccountStatus,
    isSuperAdmin: false,
    passwordScheme: 'bcrypt' as PasswordScheme,
    passwordHash: '',
    mustChangePassword: false,
    securityVersion: '1',
    ...overrides,
  };
}

export function createSite(overrides?: Partial<IdentitySite>): IdentitySite {
  return {
    id: '22222222-2222-2222-2222-222222222222',
    code: 'SITE',
    name: 'Test Site',
    status: 'active' as SiteStatus,
    ...overrides,
  };
}

export function createMembership(
  site: IdentitySite,
  overrides?: Partial<IdentityMembership>,
): IdentityMembership {
  return {
    userId: '11111111-1111-1111-1111-111111111111',
    siteId: site.id,
    role: 'Administrador' as MembershipRole,
    status: 'active' as MembershipStatus,
    validFrom: null,
    validUntil: null,
    site,
    ...overrides,
  };
}

export function createSession(
  identity: IdentityUser,
  overrides?: Partial<AuthSessionRecord>,
): AuthSessionRecord {
  const now = new Date();
  return {
    id: '33333333-3333-3333-3333-333333333333',
    tokenHash: 'hash',
    userId: identity.id,
    activeSiteId: null,
    purpose: 'normal' as SessionPurpose,
    securityVersion: identity.securityVersion,
    createdAt: now,
    lastSeenAt: now,
    idleExpiresAt: new Date(now.getTime() + 30 * 60 * 1000),
    absoluteExpiresAt: new Date(now.getTime() + 12 * 60 * 60 * 1000),
    revokedAt: null,
    revocationReason: null,
    ...overrides,
  };
}

/**
 * The legacy shape (`User`, `Site`, `Membership`, `Session`) was used by the
 * pre-F3 test suite. Re-export those names with deprecation shims to keep the
 * F2 net/integration suite compiling while the F3 unit suites move to the
 * identity-aware shapes.
 */
export interface User {
  readonly id: string;
  readonly email: string;
  readonly fullName: string;
  readonly passwordHash: string;
  readonly isSuperAdmin: boolean;
  readonly status: AccountStatus;
  readonly securityVersion: string;
}

export interface Site {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly status: SiteStatus;
}

export interface Session {
  readonly id: string;
  readonly tokenHash: string;
  readonly userId: string;
  readonly activeSiteId: string | null;
  readonly securityVersion: string;
  readonly createdAt: Date;
  readonly lastSeenAt: Date;
  readonly idleExpiresAt: Date;
  readonly absoluteExpiresAt: Date;
  readonly revokedAt: Date | null;
  readonly revocationReason: string | null;
}

export interface Membership {
  readonly userId: string;
  readonly siteId: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly validFrom: Date | null;
  readonly validUntil: Date | null;
  readonly site: Site;
}

export interface AuthSessionContext {
  readonly user: User;
  readonly memberships: Membership[];
  readonly activeSiteId: string | null;
}

export function createUser(overrides?: Partial<User>): User {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'user@example.com',
    fullName: 'Test User',
    passwordHash: '',
    isSuperAdmin: false,
    status: 'active' as AccountStatus,
    securityVersion: '1',
    ...overrides,
  };
}

/** Mirrors public.lu_login_identifier_normalize: trim, NFKC, lowercase. */
export function normalizeClaim(value: string): string {
  return value.trim().normalize('NFKC').toLowerCase();
}

/**
 * Stateful in-memory stand-in for AuthRepository. It mirrors the SQL
 * semantics of the real repository (claim lookup, eligibility predicate,
 * compare-and-swap password writes that never touch security_version, session
 * validation and the site-selection/normal active-site paths) so service,
 * controller and guard tests exercise real behavior. Queries issued through
 * the transaction client are recorded in `queries`.
 */
export class FakeAuthRepository {
  readonly identities = new Map<string, IdentityUser>();
  readonly memberships = new Map<string, IdentityMembership[]>();
  /** Sessions keyed by token hash. */
  readonly sessions = new Map<string, AuthSessionRecord>();
  readonly securityEvents: SecurityEventInput[] = [];
  readonly queries: QueryLog[] = [];
  readonly passwordMigratedAt = new Map<string, Date | null>();
  transactions = 0;
  private readonly rateBuckets = new Map<string, { count: number; resetAt: Date }>();

  private readonly client: IPgClient = {
    query: async <T = Record<string, unknown>>(sql: string, params?: unknown[]) => {
      this.queries.push({ sql, params: params ?? [] });
      return { rows: [] as T[], rowCount: 0 };
    },
  };

  readonly pool: IPgPool = {
    query: (sql, params) => this.client.query(sql, params),
    transaction: (fn) => this.inTransaction(fn),
  };

  addIdentity(identity: IdentityUser, memberships: IdentityMembership[] = []): IdentityUser {
    this.identities.set(identity.id, identity);
    this.memberships.set(identity.id, memberships);
    return identity;
  }

  getIdentity(id: string): IdentityUser {
    const identity = this.identities.get(id);
    if (identity === undefined) throw new Error('unknown identity ' + id);
    return identity;
  }

  sessionsFor(userId: string): AuthSessionRecord[] {
    return [...this.sessions.values()].filter((s) => s.userId === userId);
  }

  /** Mirrors the SessionInvalidator SQL: +1 security_version, revoke every session. */
  rotateAndRevokeAll(userId: string, reason: string): string {
    const identity = this.getIdentity(userId);
    const next = (BigInt(identity.securityVersion) + 1n).toString();
    this.identities.set(userId, { ...identity, securityVersion: next });
    for (const [hash, session] of this.sessions) {
      if (session.userId === userId && session.revokedAt === null) {
        this.sessions.set(hash, { ...session, revokedAt: new Date(), revocationReason: reason });
      }
    }
    return next;
  }

  getPool(): IPgPool {
    return this.pool;
  }

  async inTransaction<T>(work: (client: IPgClient) => Promise<T>): Promise<T> {
    this.transactions += 1;
    const identities = new Map(this.identities);
    const sessions = new Map(this.sessions);
    const migrated = new Map(this.passwordMigratedAt);
    try {
      return await work(this.client);
    } catch (error) {
      // Roll back in-memory state like a failed PostgreSQL transaction.
      this.identities.clear();
      identities.forEach((v, k) => this.identities.set(k, v));
      this.sessions.clear();
      sessions.forEach((v, k) => this.sessions.set(k, v));
      this.passwordMigratedAt.clear();
      migrated.forEach((v, k) => this.passwordMigratedAt.set(k, v));
      throw error;
    }
  }

  async recordSecurityEvent(event: SecurityEventInput): Promise<void> {
    this.securityEvents.push(event);
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
    const consume = (scope: string, key: string) => {
      const bucketKey = scope + '|' + key;
      const current = this.rateBuckets.get(bucketKey);
      if (current === undefined || current.resetAt <= args.now) {
        const created = {
          count: 1,
          resetAt: new Date(args.now.getTime() + args.windowSeconds * 1000),
        };
        this.rateBuckets.set(bucketKey, created);
        return created;
      }
      current.count += 1;
      return current;
    };
    const identifierIp = consume('email_ip', args.identifierIpKeyHash);
    const ip = consume('ip', args.ipKeyHash);
    if (identifierIp.count <= args.identifierIpMaxAttempts && ip.count <= args.ipMaxAttempts) {
      return null;
    }
    this.securityEvents.push({
      eventType: 'login_rate_limited',
      subjectHash: args.subjectHash,
      ipHash: args.ipHash,
      occurredAt: args.now,
      metadata: {},
    });
    const retryAt = identifierIp.resetAt > ip.resetAt ? identifierIp.resetAt : ip.resetAt;
    return Math.max(1, Math.ceil((retryAt.getTime() - args.now.getTime()) / 1000));
  }

  resetRateLimitsForTests(): void {
    this.rateBuckets.clear();
  }

  async findIdentityByLoginIdentifier(identifier: string): Promise<IdentityUser | null> {
    const needle = normalizeClaim(identifier);
    for (const identity of this.identities.values()) {
      if (normalizeClaim(identity.email) === needle) return identity;
      if (identity.username !== '' && normalizeClaim(identity.username) === needle) return identity;
    }
    return null;
  }

  async findIdentityById(id: string): Promise<IdentityUser | null> {
    return this.identities.get(id) ?? null;
  }

  async findEligibleMemberships(
    userId: string,
    now: Date,
    _client?: IPgClient,
  ): Promise<IdentityMembership[]> {
    void _client;
    return (this.memberships.get(userId) ?? []).filter(
      (m) =>
        m.status === 'active' &&
        m.site.status === 'active' &&
        (m.validFrom === null || m.validFrom <= now) &&
        (m.validUntil === null || m.validUntil > now),
    );
  }

  async findEligibleMembershipsForUpdate(
    _client: IPgClient,
    userId: string,
    now: Date,
  ): Promise<IdentityMembership[]> {
    return this.findEligibleMemberships(userId, now);
  }

  async insertSession(args: {
    readonly userId: string;
    readonly activeSiteId: string | null;
    readonly purpose: SessionPurpose;
    readonly securityVersion: string;
    readonly tokenHash: string;
    readonly createdAt: Date;
    readonly idleExpiresAt: Date;
    readonly absoluteExpiresAt: Date;
  }): Promise<AuthSessionRecord> {
    if (args.purpose !== 'normal' && args.activeSiteId !== null) {
      // ck_lu_session_purpose_site (control-plane 0006).
      throw new Error('ck_lu_session_purpose_site violated');
    }
    const session: AuthSessionRecord = {
      id: randomUUID(),
      tokenHash: args.tokenHash,
      userId: args.userId,
      activeSiteId: args.activeSiteId,
      purpose: args.purpose,
      securityVersion: args.securityVersion,
      createdAt: args.createdAt,
      lastSeenAt: args.createdAt,
      idleExpiresAt: args.idleExpiresAt,
      absoluteExpiresAt: args.absoluteExpiresAt,
      revokedAt: null,
      revocationReason: null,
    };
    this.sessions.set(args.tokenHash, session);
    return session;
  }

  async recordLoginSuccess(args: {
    readonly user: IdentityUser;
    readonly session: AuthSessionRecord;
    readonly now: Date;
    readonly subjectHash?: string;
    readonly ipHash?: string;
  }): Promise<void> {
    this.securityEvents.push({
      eventType: 'login_success',
      userId: args.user.id,
      siteId: args.session.activeSiteId,
      subjectHash: args.subjectHash,
      ipHash: args.ipHash,
      occurredAt: args.now,
      metadata: { purpose: args.session.purpose },
    });
  }

  private liveSession(tokenHash: string, now: Date): AuthSessionRecord | null {
    const session = this.sessions.get(tokenHash);
    if (
      session === undefined ||
      session.revokedAt !== null ||
      session.idleExpiresAt <= now ||
      session.absoluteExpiresAt <= now
    ) {
      return null;
    }
    return session;
  }

  async findSessionByTokenHashForUpdate(
    _client: IPgClient,
    tokenHash: string,
    now: Date,
  ): Promise<{ row: never; session: AuthSessionRecord; identity: IdentityUser } | null> {
    const session = this.liveSession(tokenHash, now);
    if (session === null) return null;
    const identity = this.identities.get(session.userId);
    if (identity === undefined) return null;
    return { row: undefined as never, session, identity };
  }

  async findSessionByTokenHash(
    tokenHash: string,
    now: Date,
  ): Promise<{ session: AuthSessionRecord; identity: IdentityUser } | null> {
    const session = this.liveSession(tokenHash, now);
    if (session === null) return null;
    const identity = this.identities.get(session.userId);
    if (identity === undefined) return null;
    return { session, identity };
  }

  async getSessionContext(
    tokenHash: string,
    now: Date,
    idleTtlSeconds = 30 * 60,
  ): Promise<{
    identity: IdentityUser;
    memberships: IdentityMembership[];
    session: AuthSessionRecord;
    activeSiteId: string | null;
  }> {
    const session = this.liveSession(tokenHash, now);
    if (session === null) throw new AuthUnauthorizedException();
    const identity = this.identities.get(session.userId);
    if (
      identity === undefined ||
      identity.accountStatus !== 'active' ||
      identity.securityVersion !== session.securityVersion
    ) {
      throw new AuthUnauthorizedException();
    }
    const memberships = await this.findEligibleMemberships(identity.id, now);
    if (session.purpose === 'normal' && memberships.length === 0 && !identity.isSuperAdmin) {
      throw new AuthUnauthorizedException();
    }
    const activeSiteId =
      session.activeSiteId !== null && memberships.some((m) => m.siteId === session.activeSiteId)
        ? session.activeSiteId
        : null;
    let idleExpiresAt = session.idleExpiresAt;
    if (session.purpose !== 'site_selection') {
      const candidate = new Date(now.getTime() + idleTtlSeconds * 1000);
      const bounded = candidate < session.absoluteExpiresAt ? candidate : session.absoluteExpiresAt;
      if (bounded > idleExpiresAt) idleExpiresAt = bounded;
    }
    const next = { ...session, lastSeenAt: now, idleExpiresAt };
    this.sessions.set(tokenHash, next);
    return { identity, memberships, session: next, activeSiteId };
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
    const session = this.liveSession(args.tokenHash, args.now);
    if (session === null) throw new AuthUnauthorizedException();
    const identity = this.identities.get(session.userId);
    if (
      identity === undefined ||
      identity.accountStatus !== 'active' ||
      identity.securityVersion !== session.securityVersion
    ) {
      throw new AuthUnauthorizedException();
    }
    if (session.purpose === 'password_change') {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    const memberships = await this.findEligibleMemberships(identity.id, args.now);
    if (!memberships.some((m) => m.siteId === args.activeSiteId)) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    if (session.purpose === 'site_selection') {
      const revoked = {
        ...session,
        revokedAt: args.now,
        revocationReason: 'site_selection_resolved',
      };
      this.sessions.set(args.tokenHash, revoked);
      return { identity, memberships, session: revoked };
    }
    const next = { ...session, activeSiteId: args.activeSiteId, lastSeenAt: args.now };
    this.sessions.set(args.tokenHash, next);
    this.securityEvents.push({
      eventType: 'active_site_changed',
      userId: identity.id,
      siteId: args.activeSiteId,
      occurredAt: args.now,
      metadata: { mode: 'normal_session' },
    });
    return { identity, memberships, session: next };
  }

  async revokeSession(tokenHash: string, reason: string): Promise<void> {
    const session = this.sessions.get(tokenHash);
    if (session === undefined || session.revokedAt !== null) return;
    this.sessions.set(tokenHash, { ...session, revokedAt: new Date(), revocationReason: reason });
    this.securityEvents.push({
      eventType: 'logout',
      userId: session.userId,
      siteId: session.activeSiteId,
      occurredAt: new Date(),
      metadata: {},
    });
  }

  private casPasswordWrite(
    userId: string,
    expected: { scheme: PasswordScheme; hash: string },
    apply: (identity: IdentityUser) => IdentityUser,
  ): { readonly newSecurityVersion: string } {
    const identity = this.identities.get(userId);
    if (
      identity === undefined ||
      identity.passwordScheme !== expected.scheme ||
      identity.passwordHash !== expected.hash
    ) {
      throw new AuthUnauthorizedException();
    }
    const updated = apply(identity);
    this.identities.set(userId, updated);
    return { newSecurityVersion: updated.securityVersion };
  }

  async rehashUserPassword(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedScheme: PasswordScheme;
    readonly expectedHash: string;
    readonly newHash: string;
    readonly previousSchemeForMigrationFlag: PasswordScheme;
    readonly now: Date;
  }): Promise<{ readonly newSecurityVersion: string }> {
    return this.casPasswordWrite(
      args.userId,
      { scheme: args.expectedScheme, hash: args.expectedHash },
      (identity) => {
        if (args.previousSchemeForMigrationFlag !== 'bcrypt') {
          this.passwordMigratedAt.set(identity.id, args.now);
        }
        return {
          ...identity,
          passwordHash: args.newHash,
          passwordScheme: 'bcrypt',
          mustChangePassword: false,
        };
      },
    );
  }

  async rehashUserPasswordBcryptCost(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedHash: string;
    readonly newHash: string;
  }): Promise<{ readonly newSecurityVersion: string }> {
    return this.casPasswordWrite(
      args.userId,
      { scheme: 'bcrypt', hash: args.expectedHash },
      (identity) => ({ ...identity, passwordHash: args.newHash }),
    );
  }

  async changePasswordHash(args: {
    readonly client: IPgClient;
    readonly userId: string;
    readonly expectedHash: string;
    readonly expectedScheme: PasswordScheme;
    readonly newHash: string;
    readonly migrateAt: Date;
  }): Promise<{ readonly newSecurityVersion: string }> {
    return this.casPasswordWrite(
      args.userId,
      { scheme: args.expectedScheme, hash: args.expectedHash },
      (identity) => {
        if (args.expectedScheme !== 'bcrypt') {
          this.passwordMigratedAt.set(identity.id, args.migrateAt);
        }
        return {
          ...identity,
          passwordHash: args.newHash,
          passwordScheme: 'bcrypt',
          mustChangePassword: false,
        };
      },
    );
  }
}
