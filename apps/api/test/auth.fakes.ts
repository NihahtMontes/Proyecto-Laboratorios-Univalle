import { AuthForbiddenException, AuthUnauthorizedException } from '../src/auth/auth.exceptions.js';
import type { AuthRepository, SecurityEventInput } from '../src/auth/auth.repository.js';
import type { IPgClient, IPgPool } from '../src/auth/auth.pg-pool.js';
import type {
  AuthSessionContext,
  Membership,
  MembershipRole,
  MembershipStatus,
  Session,
  Site,
  SiteStatus,
  User,
  UserStatus,
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

  queueSessionWithUserRow(session: Session, user: User, activeSite: Site | null): void {
    this.queueResult([
      {
        id: session.id,
        token_hash: session.tokenHash,
        session_user_id: session.userId,
        session_active_site_id: session.activeSiteId,
        security_version: String(session.securityVersion),
        created_at: session.createdAt,
        last_seen_at: session.lastSeenAt,
        idle_expires_at: session.idleExpiresAt,
        absolute_expires_at: session.absoluteExpiresAt,
        revoked_at: session.revokedAt,
        revocation_reason: session.revocationReason,
        email: user.email,
        full_name: user.fullName,
        password_hash: user.passwordHash,
        is_super_admin: user.isSuperAdmin,
        user_status: user.status,
        user_security_version: String(user.securityVersion),
        active_site_id: activeSite?.id ?? null,
        active_site_code: activeSite?.code ?? null,
        active_site_name: activeSite?.name ?? null,
        active_site_status: activeSite?.status ?? null,
      },
    ]);
  }

  reset(): void {
    this.queries.length = 0;
    this.nextResults = [];
  }
}

export function createUser(overrides?: Partial<User>): User {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'user@example.com',
    fullName: 'Test User',
    passwordHash: '',
    isSuperAdmin: false,
    status: 'active' as UserStatus,
    securityVersion: '1',
    ...overrides,
  };
}

export function createSite(overrides?: Partial<Site>): Site {
  return {
    id: '22222222-2222-2222-2222-222222222222',
    code: 'SITE',
    name: 'Test Site',
    status: 'active' as SiteStatus,
    ...overrides,
  };
}

export function createMembership(site: Site, overrides?: Partial<Membership>): Membership {
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

export function createSession(user: User, overrides?: Partial<Session>): Session {
  const now = new Date();
  return {
    id: '33333333-3333-3333-3333-333333333333',
    tokenHash: 'hash',
    userId: user.id,
    activeSiteId: null,
    securityVersion: user.securityVersion,
    createdAt: now,
    lastSeenAt: now,
    idleExpiresAt: new Date(now.getTime() + 30 * 60 * 1000),
    absoluteExpiresAt: new Date(now.getTime() + 12 * 60 * 60 * 1000),
    revokedAt: null,
    revocationReason: null,
    ...overrides,
  };
}

export class FakeAuthRepository implements Omit<AuthRepository, 'constructor'> {
  users = new Map<string, User>();
  memberships = new Map<string, Membership[]>();
  sessions = new Map<string, Session>();
  revoked = new Set<string>();
  securityEvents: SecurityEventInput[] = [];
  private readonly rateBuckets = new Map<string, { count: number; resetAt: Date }>();

  async recordSecurityEvent(event: SecurityEventInput): Promise<void> {
    this.securityEvents.push(event);
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
    const consume = (scope: string, key: string) => {
      const bucketKey = `${scope}|${key}`;
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
    const emailIp = consume('email_ip', args.emailIpKeyHash);
    const ip = consume('ip', args.ipKeyHash);
    if (emailIp.count <= args.emailIpMaxAttempts && ip.count <= args.ipMaxAttempts) return null;
    this.securityEvents.push({
      eventType: 'login_rate_limited',
      subjectHash: args.subjectHash,
      ipHash: args.ipHash,
      occurredAt: args.now,
      metadata: {},
    });
    const retryAt = emailIp.resetAt > ip.resetAt ? emailIp.resetAt : ip.resetAt;
    return Math.max(1, Math.ceil((retryAt.getTime() - args.now.getTime()) / 1000));
  }

  resetRateLimitsForTests(): void {
    this.rateBuckets.clear();
  }

  async findUserWithMembershipsByEmail(email: string): Promise<User | null> {
    for (const user of this.users.values()) {
      if (user.email === email) return user;
    }
    return null;
  }

  async findUserById(id: string): Promise<User | null> {
    return this.users.get(id) ?? null;
  }

  async findEligibleMemberships(userId: string): Promise<Membership[]> {
    const now = new Date();
    return (this.memberships.get(userId) ?? []).filter(
      (m) =>
        m.status === 'active' &&
        m.site.status === 'active' &&
        (m.validFrom === null || m.validFrom <= now) &&
        (m.validUntil === null || m.validUntil > now),
    );
  }

  async createSession(args: {
    user: User;
    activeSiteId: string | null;
    tokenHash: string;
    now: Date;
    subjectHash?: string;
    ipHash?: string;
  }): Promise<Session> {
    const now = args.now;
    const session: Session = {
      id: 'session-id',
      tokenHash: args.tokenHash,
      userId: args.user.id,
      activeSiteId: args.activeSiteId,
      securityVersion: args.user.securityVersion,
      createdAt: now,
      lastSeenAt: now,
      idleExpiresAt: new Date(now.getTime() + 30 * 60 * 1000),
      absoluteExpiresAt: new Date(now.getTime() + 12 * 60 * 60 * 1000),
      revokedAt: null,
      revocationReason: null,
    };
    this.sessions.set(args.tokenHash, session);
    this.securityEvents.push({
      eventType: 'login_success',
      userId: args.user.id,
      siteId: args.activeSiteId,
      subjectHash: args.subjectHash,
      ipHash: args.ipHash,
      occurredAt: args.now,
      metadata: {},
    });
    return session;
  }

  async getSessionContext(tokenHash: string, now: Date): Promise<AuthSessionContext> {
    const session = this.sessions.get(tokenHash);
    if (session === undefined) {
      throw new AuthUnauthorizedException();
    }
    if (session.revokedAt !== null) {
      throw new AuthUnauthorizedException();
    }
    if (session.idleExpiresAt <= now || session.absoluteExpiresAt <= now) {
      throw new AuthUnauthorizedException();
    }

    const user = this.users.get(session.userId);
    if (user === undefined) {
      throw new AuthUnauthorizedException();
    }
    if (user.status !== 'active' || user.securityVersion !== session.securityVersion) {
      throw new AuthUnauthorizedException();
    }

    const memberships = await this.findEligibleMemberships(user.id);
    const activeSiteId =
      session.activeSiteId !== null && memberships.some((m) => m.siteId === session.activeSiteId)
        ? session.activeSiteId
        : null;

    if (memberships.length === 0 && !user.isSuperAdmin) {
      throw new AuthUnauthorizedException();
    }

    // Monotonic touch: keep the fake consistent with the real repository.
    this.sessions.set(tokenHash, { ...session, lastSeenAt: now });

    return { user, memberships, activeSiteId };
  }

  async setActiveSiteContext(args: {
    tokenHash: string;
    activeSiteId: string;
    now: Date;
  }): Promise<AuthSessionContext> {
    const session = this.sessions.get(args.tokenHash);
    if (session === undefined) {
      throw new AuthUnauthorizedException();
    }
    const user = this.users.get(session.userId);
    if (user === undefined) {
      throw new AuthUnauthorizedException();
    }
    if (session.revokedAt !== null) {
      throw new AuthUnauthorizedException();
    }
    if (session.idleExpiresAt <= args.now || session.absoluteExpiresAt <= args.now) {
      throw new AuthUnauthorizedException();
    }
    if (user.status !== 'active' || user.securityVersion !== session.securityVersion) {
      throw new AuthUnauthorizedException();
    }
    const eligible = await this.findEligibleMemberships(user.id);
    const belongs = eligible.some((m) => m.siteId === args.activeSiteId);
    if (!belongs) {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }
    const updated: Session = {
      ...session,
      activeSiteId: args.activeSiteId,
      lastSeenAt: args.now,
    };
    this.sessions.set(args.tokenHash, updated);
    this.securityEvents.push({
      eventType: 'active_site_changed',
      userId: user.id,
      siteId: args.activeSiteId,
      occurredAt: args.now,
      metadata: {},
    });
    return { user, memberships: eligible, activeSiteId: args.activeSiteId };
  }

  async revokeSession(tokenHash: string, reason: string): Promise<void> {
    const session = this.sessions.get(tokenHash);
    if (session === undefined) return;
    this.sessions.set(tokenHash, {
      ...session,
      revokedAt: new Date(),
      revocationReason: reason,
    });
    this.revoked.add(tokenHash);
    this.securityEvents.push({
      eventType: 'logout',
      userId: session.userId,
      siteId: session.activeSiteId,
      occurredAt: new Date(),
      metadata: {},
    });
  }
}
