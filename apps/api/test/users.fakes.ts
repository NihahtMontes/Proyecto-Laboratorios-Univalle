/**
 * Test fakes for the users slice (MIG-001 F3 W3).
 *
 * Mirrors the contract of auth.fakes.ts but stays inside apps/api/test/users/
 * so we can evolve the fakes without touching the auth fakes (which the auth
 * worker owns). Every fake honours the IPgPool / IPgClient interfaces
 * declared in apps/api/src/auth/auth.pg-pool.ts (read-only).
 *
 * The fake tokens mirror the identity contract symbols so they can be
 * registered via `Test.createTestingModule({...}).overrideProvider(...).useValue(...)`.
 */
import type { IPgClient, IPgPool } from '../src/auth/auth.pg-pool.js';
import type {
  IdentityAuditAction,
  IdentityAuditEvent,
  IdentityAuditWriter,
  PasswordHasher,
  PasswordPolicy,
  PasswordPolicyViolation,
  SecurityRotationReason,
  SessionInvalidator,
  AccountStatus,
  MembershipRole,
  MembershipStatus,
  PasswordScheme,
  RequestIdentity,
  SelfSessionRenewer,
} from '../src/identity/identity.contracts.js';

// ---------------------------------------------------------------------------
// PG pool fake
// ---------------------------------------------------------------------------

export interface QueryResult<T = Record<string, unknown>> {
  readonly rows: T[];
  readonly rowCount: number | null;
}

export interface QueryLog {
  readonly sql: string;
  readonly params: readonly unknown[];
}

export class FakeUsersPgPool implements IPgPool {
  readonly queries: QueryLog[] = [];
  readonly transactions: QueryLog[][] = [];
  private nextResults: Array<{ rows: unknown[]; rowCount: number | null } | { error: Error }> = [];
  private currentTransaction: QueryLog[] | null = null;

  query<T = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: T[]; rowCount: number | null }> {
    const log = { sql: sql.trim().replace(/\s+/g, ' '), params: params ?? [] };
    this.queries.push(log);
    if (this.currentTransaction !== null) this.currentTransaction.push(log);
    const normalized = log.sql.toUpperCase();
    const startsWithNonReturning =
      normalized.startsWith('INSERT') ||
      normalized.startsWith('UPDATE') ||
      normalized.startsWith('DELETE');
    const hasReturning = /\bRETURNING\b/.test(normalized);
    const needsRows =
      normalized.startsWith('SELECT') ||
      normalized.startsWith('WITH') ||
      (startsWithNonReturning && hasReturning);
    if (!needsRows) {
      return Promise.resolve({ rows: [] as T[], rowCount: 0 });
    }
    const next = this.nextResults.shift();
    if (next === undefined) {
      throw new Error(
        `FakeUsersPgPool: no next result configured. Query #${this.queries.length}: ${log.sql.slice(0, 80)}`,
      );
    }
    if ('error' in next) throw next.error;
    return Promise.resolve({ rows: next.rows as T[], rowCount: next.rowCount });
  }

  async transaction<T>(fn: (client: IPgClient) => Promise<T>): Promise<T> {
    const log: QueryLog[] = [];
    this.transactions.push(log);
    const previous = this.currentTransaction;
    this.currentTransaction = log;
    try {
      const result = await fn(this as unknown as IPgClient);
      return result;
    } finally {
      this.currentTransaction = previous;
    }
  }

  async close(): Promise<void> {
    /* noop */
  }

  queueResult(rows: unknown[], rowCount: number | null = rows.length): void {
    this.nextResults.push({ rows, rowCount });
  }

  queueError(error: Error): void {
    this.nextResults.push({ error });
  }

  reset(): void {
    this.queries.length = 0;
    this.transactions.length = 0;
    this.nextResults = [];
    this.currentTransaction = null;
  }
}

// ---------------------------------------------------------------------------
// Identity kernel fakes
// ---------------------------------------------------------------------------

export class FakePasswordPolicy implements PasswordPolicy {
  private readonly alwaysAccept: boolean;
  constructor(alwaysAccept = true) {
    this.alwaysAccept = alwaysAccept;
  }
  validate(plaintext: string): readonly PasswordPolicyViolation[] {
    if (this.alwaysAccept) return [];
    if (plaintext.length < 12) return ['too_short'];
    if (Buffer.byteLength(plaintext, 'utf8') > 72) return ['too_long'];
    const violations: PasswordPolicyViolation[] = [];
    if (!/[A-Z]/.test(plaintext)) violations.push('missing_uppercase');
    if (!/[a-z]/.test(plaintext)) violations.push('missing_lowercase');
    if (!/[0-9]/.test(plaintext)) violations.push('missing_digit');
    if (!/[^A-Za-z0-9]/.test(plaintext)) violations.push('missing_symbol');
    const distinct = new Set([...plaintext]);
    if (distinct.size < 4) violations.push('insufficient_distinct');
    return violations;
  }
}

export class FakePasswordHasher implements PasswordHasher {
  readonly currentCost = 12;
  private counter = 0;
  hash(_plaintext: string): Promise<string> {
    void _plaintext;
    this.counter += 1;
    return Promise.resolve(`$2b$12$fake-hash-${this.counter}`);
  }
}

export interface RotationEvent {
  readonly userId: string;
  readonly reason: SecurityRotationReason;
  readonly atCallIndex: number;
}

export class FakeSessionInvalidator implements SessionInvalidator {
  readonly rotations: RotationEvent[] = [];
  private counter = 0;
  rotateAndRevokeAll(
    _client: IPgClient,
    userId: string,
    reason: SecurityRotationReason,
  ): Promise<string> {
    this.counter += 1;
    this.rotations.push({ userId, reason, atCallIndex: this.counter });
    return Promise.resolve(String(this.counter));
  }
}

export interface AuditEventRecord {
  readonly action: IdentityAuditAction;
  readonly subjectUserId: string | null;
  readonly actorUserId: string | null;
  readonly siteId: string | null;
  readonly correlationId: string | null;
  readonly reason: string | null;
  readonly beforeStatus: AccountStatus | null;
  readonly afterStatus: AccountStatus | null;
  readonly metadata: Readonly<Record<string, string | number | boolean | null>>;
  readonly atCallIndex: number;
}

export class FakeIdentityAuditWriter implements IdentityAuditWriter {
  readonly events: AuditEventRecord[] = [];
  private counter = 0;
  append(_client: IPgClient, event: IdentityAuditEvent): Promise<void> {
    this.counter += 1;
    this.events.push({
      action: event.action,
      subjectUserId: event.subjectUserId ?? null,
      actorUserId: event.actorUserId ?? null,
      siteId: event.siteId ?? null,
      correlationId: event.correlationId ?? null,
      reason: event.reason ?? null,
      beforeStatus: event.beforeStatus ?? null,
      afterStatus: event.afterStatus ?? null,
      metadata:
        (event.metadata as Readonly<Record<string, string | number | boolean | null>>) ?? {},
      atCallIndex: this.counter,
    });
    return Promise.resolve();
  }
}

export class FakeSelfSessionRenewer implements SelfSessionRenewer {
  renewCalls = 0;
  renew(
    reply: { header(name: string, value: string | number): unknown },
    identity: RequestIdentity,
  ): Promise<void> {
    this.renewCalls += 1;
    reply.header('x-self-session-renewed', identity.sessionId);
    return Promise.resolve();
  }
}

// ---------------------------------------------------------------------------
// Identity fixture
// ---------------------------------------------------------------------------

export interface MembershipFixture {
  readonly siteId: string;
  readonly role: MembershipRole;
  readonly status: MembershipStatus;
  readonly validFrom?: Date | null;
  readonly validUntil?: Date | null;
  readonly position?: string | null;
  readonly department?: string | null;
  readonly hireDate?: Date | null;
}

export interface UserFixture {
  readonly id: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly secondLastName?: string | null;
  readonly fullName?: string;
  readonly identityCard: string;
  readonly phoneNumber: string;
  readonly accountStatus: AccountStatus;
  readonly isSuperAdmin: boolean;
  readonly passwordScheme?: PasswordScheme;
  readonly mustChangePassword?: boolean;
  readonly securityVersion?: string;
  readonly memberships?: readonly MembershipFixture[];
}

export function buildIdentity(
  actor: Partial<RequestIdentity> & { userId: string },
): RequestIdentity {
  return {
    correlationId: actor.correlationId ?? '00000000-0000-4000-8000-000000000000',
    sessionId: actor.sessionId ?? '00000000-0000-4000-8000-000000000001',
    userId: actor.userId,
    isSuperAdmin: actor.isSuperAdmin ?? false,
    activeSiteId: actor.activeSiteId ?? null,
    activeSiteRole: actor.activeSiteRole ?? null,
  };
}

export function buildCommandContext(args: {
  userId: string;
  isSuperAdmin?: boolean;
  activeSiteId?: string | null;
  activeSiteRole?: MembershipRole | null;
}): {
  actor: RequestIdentity;
  correlationId: string;
  reply: null;
} {
  return {
    actor: buildIdentity({
      userId: args.userId,
      isSuperAdmin: args.isSuperAdmin ?? false,
      activeSiteId: args.activeSiteId ?? null,
      activeSiteRole: args.activeSiteRole ?? null,
      correlationId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
      sessionId: 'ssssssss-ssss-4sss-8sss-ssssssssssss',
    }),
    correlationId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
    reply: null,
  };
}

export function makeUserRow(fixture: UserFixture): Record<string, unknown> {
  return {
    id: fixture.id,
    email: fixture.email,
    first_name: fixture.firstName,
    last_name: fixture.lastName,
    second_last_name: fixture.secondLastName ?? null,
    full_name: fixture.fullName ?? `${fixture.firstName} ${fixture.lastName}`.trim(),
    identity_card: fixture.identityCard,
    phone_number: fixture.phoneNumber,
    account_status: fixture.accountStatus,
    status: fixture.accountStatus,
    is_super_admin: fixture.isSuperAdmin,
    password_scheme: fixture.passwordScheme ?? 'bcrypt',
    must_change_password: fixture.mustChangePassword ?? false,
    security_version: fixture.securityVersion ?? '1',
    created_at: new Date('2026-09-19T12:00:00.000Z'),
    updated_at: new Date('2026-09-19T12:00:00.000Z'),
  };
}

export function makeMembershipRow(
  fixture: MembershipFixture,
  siteName: string,
): Record<string, unknown> {
  return {
    site_id: fixture.siteId,
    site_name: siteName,
    role: fixture.role,
    membership_status: fixture.status,
    valid_from: fixture.validFrom ?? null,
    valid_until: fixture.validUntil ?? null,
    position: fixture.position ?? null,
    department: fixture.department ?? null,
    hire_date: fixture.hireDate ?? null,
  };
}
