import 'reflect-metadata';
import { Test } from '@nestjs/testing';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { AUTH_CONFIG, AUTH_PG_POOL } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { createSession, createSite, createUser, FakePgPool } from './auth.fakes.js';

function membershipRow(siteId: string): Record<string, unknown> {
  return {
    user_id: '11111111-1111-1111-1111-111111111111',
    site_id: siteId,
    role: 'Administrador',
    status: 'active',
    valid_from: null,
    valid_until: null,
    site_code: 'SITE',
    site_name: 'Test Site',
    site_status: 'active',
  };
}

function sessionRow(session: ReturnType<typeof createSession>): Record<string, unknown> {
  return {
    id: session.id,
    token_hash: session.tokenHash,
    user_id: session.userId,
    active_site_id: session.activeSiteId,
    security_version: session.securityVersion,
    created_at: session.createdAt,
    last_seen_at: session.lastSeenAt,
    idle_expires_at: session.idleExpiresAt,
    absolute_expires_at: session.absoluteExpiresAt,
    revoked_at: session.revokedAt,
    revocation_reason: session.revocationReason,
  };
}

function sessionUserRow(
  session: ReturnType<typeof createSession>,
  user: ReturnType<typeof createUser>,
): Record<string, unknown> {
  return {
    ...sessionRow(session),
    user_status: user.status,
    user_security_version: user.securityVersion,
    email: user.email,
    full_name: user.fullName,
    password_hash: user.passwordHash,
    is_super_admin: user.isSuperAdmin,
  };
}

describe('AuthRepository', () => {
  let repository: AuthRepository;
  let pool: FakePgPool;

  beforeEach(async () => {
    pool = new FakePgPool();
    const moduleRef = await Test.createTestingModule({
      providers: [
        AuthRepository,
        { provide: AUTH_PG_POOL, useValue: pool },
        {
          provide: AUTH_CONFIG,
          useValue: new AuthConfig({
            AUTH_SESSION_IDLE_TTL_SECONDS: '1800',
            AUTH_SESSION_ABSOLUTE_TTL_SECONDS: '43200',
            AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
          }),
        },
      ],
    }).compile();
    repository = moduleRef.get(AuthRepository);
  });

  describe('parameterized SQL and UPDATE semantics', () => {
    it('consumes both distributed rate buckets atomically with hashed parameter keys', async () => {
      const now = new Date('2026-09-19T12:00:00.000Z');
      const resetAt = new Date(now.getTime() + 60_000);
      const emailIpKeyHash = 'a'.repeat(64);
      const ipKeyHash = 'b'.repeat(64);
      pool.queueResult([]); // BEGIN
      pool.queueResult([]); // bounded cleanup
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([]); // COMMIT

      await expect(
        repository.consumeLoginRateLimit({
          emailIpKeyHash,
          ipKeyHash,
          subjectHash: 'c'.repeat(64),
          ipHash: 'd'.repeat(64),
          now,
          windowSeconds: 60,
          emailIpMaxAttempts: 3,
          ipMaxAttempts: 6,
        }),
      ).resolves.toBeNull();

      expect(pool.queries[0]?.sql).toBe('BEGIN');
      expect(pool.queries[1]?.sql).toContain('LIMIT 100');
      const upserts = pool.queries.filter((q) => q.sql.includes('ON CONFLICT (scope, key_hash)'));
      expect(upserts).toHaveLength(2);
      expect(upserts[0]?.params).toEqual(['email_ip', emailIpKeyHash, now, resetAt]);
      expect(upserts[1]?.params).toEqual(['ip', ipKeyHash, now, resetAt]);
      expect(pool.queries.at(-1)?.sql).toBe('COMMIT');
    });

    it('records a rate-limited event inside the same transaction before commit', async () => {
      const now = new Date('2026-09-19T12:00:00.000Z');
      const resetAt = new Date(now.getTime() + 60_000);
      pool.queueResult([]); // BEGIN
      pool.queueResult([]); // cleanup
      pool.queueResult([{ attempt_count: 4, reset_at: resetAt }]);
      pool.queueResult([{ attempt_count: 2, reset_at: resetAt }]);
      pool.queueResult([]); // audit INSERT
      pool.queueResult([]); // COMMIT

      await expect(
        repository.consumeLoginRateLimit({
          emailIpKeyHash: 'a'.repeat(64),
          ipKeyHash: 'b'.repeat(64),
          subjectHash: 'c'.repeat(64),
          ipHash: 'd'.repeat(64),
          now,
          windowSeconds: 60,
          emailIpMaxAttempts: 3,
          ipMaxAttempts: 6,
        }),
      ).resolves.toBe(60);

      const audit = pool.queries.at(-2)!;
      expect(audit.sql).toContain('INSERT INTO public.lu_security_event');
      expect(audit.params[1]).toBe('login_rate_limited');
      expect(audit.params[4]).toBe('c'.repeat(64));
      expect(audit.params[5]).toBe('d'.repeat(64));
      expect(pool.queries.at(-1)?.sql).toBe('COMMIT');
    });

    it('uses parameterized query to find user by email', async () => {
      const user = createUser({ email: 'repo@example.com' });
      pool.queueResult([
        {
          id: user.id,
          email: user.email,
          full_name: user.fullName,
          password_hash: user.passwordHash,
          is_super_admin: user.isSuperAdmin,
          status: user.status,
          security_version: user.securityVersion,
        },
      ]);

      await repository.findUserWithMembershipsByEmail(user.email);

      expect(pool.queries).toHaveLength(1);
      const q = pool.queries[0]!;
      expect(q.sql).toContain('lower($1)');
      expect(q.params).toEqual([user.email]);
    });

    it('revokeSession uses UPDATE not DELETE', async () => {
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: '33333333-3333-3333-3333-333333333333',
          user_id: '11111111-1111-1111-1111-111111111111',
          active_site_id: null,
        },
      ]);
      pool.queueResult([]); // UPDATE
      pool.queueResult([]); // audit INSERT
      pool.queueResult([]); // COMMIT

      await repository.revokeSession('token-hash', 'logout');

      const q = pool.queries.find((query) => query.sql.includes('UPDATE public.lu_session'))!;
      expect(q.sql.toUpperCase()).toContain('UPDATE');
      expect(q.sql.toUpperCase()).not.toContain('DELETE');
      expect(q.sql).toContain('public.lu_session');
      expect(q.sql).toContain('revoked_at');
      expect(q.params[2]).toBe('logout');
      expect(pool.queries.some((query) => query.sql.includes('public.lu_security_event'))).toBe(
        true,
      );
    });

    it('createSession revalidates user and membership inside transaction', async () => {
      const user = createUser({ isSuperAdmin: true });
      const session = createSession(user);
      // BEGIN, user SELECT, session INSERT, audit INSERT, COMMIT.
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: user.id,
          status: 'active',
          security_version: user.securityVersion,
          is_super_admin: user.isSuperAdmin,
        },
      ]);
      pool.queueResult([sessionRow(session)]);
      pool.queueResult([]); // audit INSERT
      pool.queueResult([]); // COMMIT

      await repository.createSession({
        user,
        activeSiteId: null,
        tokenHash: session.tokenHash,
        now: new Date(),
      });

      expect(pool.queries.length).toBeGreaterThanOrEqual(3);
      const begin = pool.queries[0]!;
      expect(begin.sql).toBe('BEGIN');
      const userQuery = pool.queries[1]!;
      expect(userQuery.sql).toContain('is_super_admin');
      const commit = pool.queries[pool.queries.length - 1]!;
      expect(commit.sql).toBe('COMMIT');
    });

    it('createSession rolls back the session when durable audit insertion fails', async () => {
      const user = createUser({ isSuperAdmin: true });
      const session = createSession(user);
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: user.id,
          status: 'active',
          security_version: user.securityVersion,
          is_super_admin: true,
        },
      ]);
      pool.queueResult([sessionRow(session)]);
      pool.queueError(new Error('audit unavailable'));
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.createSession({
          user,
          activeSiteId: null,
          tokenHash: session.tokenHash,
          now: new Date(),
        }),
      ).rejects.toThrow('audit unavailable');

      expect(pool.queries.some((q) => q.sql === 'COMMIT')).toBe(false);
      expect(pool.queries.at(-1)?.sql).toBe('ROLLBACK');
    });

    it('createSession rejects SuperAdmin when DB role changed to normal and has no memberships', async () => {
      const user = createUser({ isSuperAdmin: true });
      const session = createSession(user);
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: user.id,
          status: 'active',
          security_version: user.securityVersion,
          is_super_admin: false,
        },
      ]);
      pool.queueResult([]); // eligible memberships empty
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.createSession({
          user,
          activeSiteId: null,
          tokenHash: session.tokenHash,
          now: new Date(),
        }),
      ).rejects.toThrow('Invalid credentials.');

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('createSession locks active membership/site and rejects missing membership', async () => {
      const user = createUser();
      const site = createSite();
      const session = createSession(user);
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: user.id,
          status: 'active',
          security_version: user.securityVersion,
          is_super_admin: user.isSuperAdmin,
        },
      ]);
      pool.queueResult([]); // membership SELECT returns nothing
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.createSession({
          user,
          activeSiteId: site.id,
          tokenHash: session.tokenHash,
          now: new Date(),
        }),
      ).rejects.toThrow('Invalid credentials.');

      const membershipQuery = pool.queries[2]!;
      expect(membershipQuery.sql).toContain('FOR UPDATE OF m, s');
      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('createSession rejects normal user with zero eligible memberships', async () => {
      const user = createUser();
      const session = createSession(user);
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: user.id,
          status: 'active',
          security_version: user.securityVersion,
          is_super_admin: user.isSuperAdmin,
        },
      ]);
      pool.queueResult([]); // eligible memberships empty
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.createSession({
          user,
          activeSiteId: null,
          tokenHash: session.tokenHash,
          now: new Date(),
        }),
      ).rejects.toThrow('Invalid credentials.');

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('getSessionContext runs atomic transaction with locks and monotonic touch', async () => {
      const user = createUser({ securityVersion: '9007199254740993' });
      const site = createSite();
      const session = createSession(user, { activeSiteId: site.id });
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          ...sessionUserRow(session, user),
          session_user_id: session.userId,
          session_active_site_id: session.activeSiteId,
        },
      ]);
      pool.queueResult([membershipRow(site.id)]);
      pool.queueResult([sessionRow(session)]);
      pool.queueResult([]); // COMMIT

      const ctx = await repository.getSessionContext(session.tokenHash, new Date());

      expect(ctx.user.securityVersion).toBe('9007199254740993');
      expect(ctx.activeSiteId).toBe(site.id);
      expect(pool.queries[0]!.sql).toBe('BEGIN');
      const selectSession = pool.queries[1]!;
      expect(selectSession.sql).toContain('FROM public.lu_session');
      expect(selectSession.sql).toContain('JOIN public.lu_user');
      expect(selectSession.sql).toContain('FOR UPDATE OF s, u');
      expect(selectSession.sql).not.toContain('LEFT JOIN');
      const membershipQuery = pool.queries[2]!;
      expect(membershipQuery.sql).toContain('FROM public.lu_site_membership');
      expect(membershipQuery.sql).toContain('JOIN public.lu_site');
      expect(membershipQuery.sql).toContain('FOR UPDATE OF m, s');
      const update = pool.queries[3]!;
      expect(update.sql).toContain('UPDATE public.lu_session');
      expect(update.sql).toContain('GREATEST');
      expect(update.sql).toContain('GREATEST(last_seen_at');
      expect(update.sql).toContain('LEAST');
      expect(pool.queries[4]!.sql).toBe('COMMIT');
    });

    it('getSessionContext normalizes active_site when membership is no longer eligible for SuperAdmin', async () => {
      const user = createUser({ isSuperAdmin: true });
      const site = createSite({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' });
      const session = createSession(user, { activeSiteId: site.id });
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          ...sessionUserRow(session, user),
          session_user_id: session.userId,
          session_active_site_id: session.activeSiteId,
        },
      ]);
      pool.queueResult([]); // memberships empty
      pool.queueResult([sessionRow(session)]);
      pool.queueResult([]); // COMMIT

      const ctx = await repository.getSessionContext(session.tokenHash, new Date());

      expect(ctx.activeSiteId).toBeNull();
      const update = pool.queries[3]!;
      expect(update.params[1]).toBeNull();
    });

    it('getSessionContext rejects normal user with zero eligible memberships', async () => {
      const user = createUser();
      const site = createSite({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' });
      const session = createSession(user, { activeSiteId: site.id });
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          ...sessionUserRow(session, user),
          session_user_id: session.userId,
          session_active_site_id: session.activeSiteId,
        },
      ]);
      pool.queueResult([]); // memberships empty
      pool.queueResult([]); // ROLLBACK

      await expect(repository.getSessionContext(session.tokenHash, new Date())).rejects.toThrow(
        'Invalid credentials.',
      );

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('getSessionContext rolls back when security_version differs', async () => {
      const user = createUser({ securityVersion: '9007199254740994' });
      const session = createSession(user, { securityVersion: '9007199254740993' });
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          ...sessionUserRow(session, user),
          session_user_id: session.userId,
          session_active_site_id: session.activeSiteId,
        },
      ]);
      pool.queueResult([]); // ROLLBACK

      await expect(repository.getSessionContext(session.tokenHash, new Date())).rejects.toThrow(
        'Invalid credentials.',
      );

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('getSessionContext treats consecutive bigint security versions as different', async () => {
      const previous = '9007199254740992';
      const next = '9007199254740993';
      expect(previous).not.toBe(next);
      expect(Number(previous)).toBe(Number(next)); // both exceed MAX_SAFE_INTEGER and are rounded by Number
      const user = createUser({ securityVersion: next });
      const session = createSession(user, { securityVersion: previous });
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          ...sessionUserRow(session, user),
          session_user_id: session.userId,
          session_active_site_id: session.activeSiteId,
        },
      ]);
      pool.queueResult([]); // ROLLBACK

      await expect(repository.getSessionContext(session.tokenHash, new Date())).rejects.toThrow(
        'Invalid credentials.',
      );
    });

    it('setActiveSiteContext locks session+user+memberships and updates conditionally', async () => {
      const user = createUser();
      const site = createSite();
      const session = createSession(user, { activeSiteId: null });
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionUserRow(session, user)]);
      pool.queueResult([membershipRow(site.id)]);
      pool.queueResult([sessionRow(session)]);
      pool.queueResult([]); // audit INSERT
      pool.queueResult([]); // COMMIT

      await repository.setActiveSiteContext({
        tokenHash: session.tokenHash,
        activeSiteId: site.id,
        now: new Date(),
      });

      expect(pool.queries[0]!.sql).toBe('BEGIN');
      const selectSession = pool.queries[1]!;
      expect(selectSession.sql).toContain('FROM public.lu_session');
      expect(selectSession.sql).toContain('JOIN public.lu_user');
      expect(selectSession.sql).toContain('FOR UPDATE OF s, u');
      const membershipQuery = pool.queries[2]!;
      expect(membershipQuery.sql).toContain('FROM public.lu_site_membership');
      expect(membershipQuery.sql).toContain('JOIN public.lu_site');
      expect(membershipQuery.sql).toContain('FOR UPDATE OF m, s');
      const update = pool.queries[3]!;
      expect(update.sql).toContain('UPDATE public.lu_session');
      expect(update.sql).toContain('GREATEST');
      expect(update.sql).toContain('GREATEST(last_seen_at');
      expect(pool.queries[4]!.sql).toContain('public.lu_security_event');
      expect(pool.queries[5]!.sql).toBe('COMMIT');
    });

    it('setActiveSiteContext rolls back when site is not eligible', async () => {
      const user = createUser();
      const session = createSession(user, { activeSiteId: null });
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionUserRow(session, user)]);
      pool.queueResult([]); // memberships empty
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.setActiveSiteContext({
          tokenHash: session.tokenHash,
          activeSiteId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          now: new Date(),
        }),
      ).rejects.toThrow('Site access denied.');

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });

    it('setActiveSiteContext compares session and user security_version separately', async () => {
      const user = createUser({ securityVersion: '2' });
      const session = createSession(user, { securityVersion: '1' });
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionUserRow(session, user)]);
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.setActiveSiteContext({
          tokenHash: session.tokenHash,
          activeSiteId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          now: new Date(),
        }),
      ).rejects.toThrow('Invalid credentials.');

      expect(pool.queries[pool.queries.length - 1]!.sql).toBe('ROLLBACK');
    });
  });
});
