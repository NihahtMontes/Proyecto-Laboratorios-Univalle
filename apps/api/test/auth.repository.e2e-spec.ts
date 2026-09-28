import 'reflect-metadata';
import { readFileSync } from 'node:fs';
import { Test } from '@nestjs/testing';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { AUTH_CONFIG, AUTH_PG_POOL } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { createIdentity, createSite, FakePgPool } from './auth.fakes.js';
import type { IdentityUser } from '../src/auth/auth.types.js';

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

function identityRow(identity: IdentityUser): Record<string, unknown> {
  return {
    id: identity.id,
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
    security_version: identity.securityVersion,
  };
}

function sessionIdentityRow(
  identity: IdentityUser,
  overrides: Record<string, unknown> = {},
): Record<string, unknown> {
  const now = Date.now();
  return {
    id: 'session-1',
    token_hash: 'hash',
    user_id: identity.id,
    active_site_id: null,
    purpose: 'normal',
    security_version: identity.securityVersion,
    created_at: new Date(now),
    last_seen_at: new Date(now),
    idle_expires_at: new Date(now + 30 * 60 * 1000),
    absolute_expires_at: new Date(now + 60 * 60 * 1000),
    revoked_at: null,
    revocation_reason: null,
    ...identityRow(identity),
    user_security_version: identity.securityVersion,
    ...overrides,
  };
}

describe('AuthRepository (F3)', () => {
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

  describe('rate limit login buckets', () => {
    it('consumes both distributed rate buckets atomically with hashed parameter keys', async () => {
      const now = new Date('2026-09-19T12:00:00.000Z');
      const resetAt = new Date(now.getTime() + 60_000);
      const identifierIpKeyHash = 'a'.repeat(64);
      const ipKeyHash = 'b'.repeat(64);
      pool.queueResult([]); // BEGIN
      pool.queueResult([]); // bounded cleanup
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([]); // COMMIT

      await expect(
        repository.consumeLoginRateLimit({
          identifierIpKeyHash,
          ipKeyHash,
          subjectHash: 'c'.repeat(64),
          ipHash: 'd'.repeat(64),
          now,
          windowSeconds: 60,
          identifierIpMaxAttempts: 3,
          ipMaxAttempts: 6,
        }),
      ).resolves.toBeNull();

      expect(pool.queries[0]?.sql).toBe('BEGIN');
      expect(pool.queries[1]?.sql).toContain('LIMIT 100');
      const upserts = pool.queries.filter((q) => q.sql.includes('ON CONFLICT (scope, key_hash)'));
      expect(upserts).toHaveLength(2);
      // Schema-valid scope label (0002 CHECK: 'email_ip' | 'ip').
      expect(upserts[0]?.params).toEqual(['email_ip', identifierIpKeyHash, now, resetAt]);
      expect(upserts[1]?.params).toEqual(['ip', ipKeyHash, now, resetAt]);
      expect(pool.queries.at(-1)?.sql).toBe('COMMIT');
    });

    it('stores only scope labels accepted by the 0002 CHECK constraint (MIG-001 F7 live regression)', async () => {
      // F7 live PostgreSQL run: the 'identifier_ip' label violated ck_lu_auth_rate_limit_scope
      // (SQLSTATE 23514) and every login failed closed. The frozen migration is the contract.
      const migration = readFileSync(
        new URL('../migrations/0002_create_auth_security_controls.sql', import.meta.url),
        'utf8',
      );
      const match = /ck_lu_auth_rate_limit_scope\s+CHECK \(scope IN \(([^)]*)\)\)/.exec(migration);
      expect(match).not.toBeNull();
      const allowed = match![1]!.split(',').map((s) => s.trim().replace(/^'|'$/g, ''));
      const resetAt = new Date(Date.now() + 60_000);
      pool.queueResult([]);
      pool.queueResult([]);
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([{ attempt_count: 1, reset_at: resetAt }]);
      pool.queueResult([]);
      await repository.consumeLoginRateLimit({
        identifierIpKeyHash: 'a'.repeat(64),
        ipKeyHash: 'b'.repeat(64),
        subjectHash: 'c'.repeat(64),
        ipHash: 'd'.repeat(64),
        now: new Date(),
        windowSeconds: 60,
        identifierIpMaxAttempts: 3,
        ipMaxAttempts: 6,
      });
      const scopes = pool.queries
        .filter((q) => q.sql.includes('ON CONFLICT (scope, key_hash)'))
        .map((q) => q.params?.[0]);
      expect(scopes).toHaveLength(2);
      for (const scope of scopes) expect(allowed).toContain(scope);
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
          identifierIpKeyHash: 'a'.repeat(64),
          ipKeyHash: 'b'.repeat(64),
          subjectHash: 'c'.repeat(64),
          ipHash: 'd'.repeat(64),
          now,
          windowSeconds: 60,
          identifierIpMaxAttempts: 3,
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
  });

  describe('identity lookup', () => {
    it('uses the lu_login_identifier claim lookup to find the user', async () => {
      const identity = createIdentity({ email: 'lookup@example.com' });
      pool.queueResult([identityRow(identity)]);

      await repository.findIdentityByLoginIdentifier(identity.email);

      expect(pool.queries).toHaveLength(1);
      const q = pool.queries[0]!;
      expect(q.sql).toContain('lu_login_identifier li');
      expect(q.sql).toContain('JOIN public.lu_user u');
      expect(q.sql).toContain('lu_login_identifier_normalize($1)');
      expect(q.params).toEqual([identity.email]);
    });

    it('returns null when no claim matches', async () => {
      pool.queueResult([]);
      await expect(repository.findIdentityByLoginIdentifier('nobody')).resolves.toBeNull();
    });
  });

  describe('session write/read', () => {
    it('keeps the session security_version snapshot distinct from the user version (MIG-001 F7 live regression)', async () => {
      // node-pg keys rows by column name: a bare `u.security_version` after
      // `s.security_version` overwrote the snapshot, so a rotated user version
      // never invalidated an existing session (found by the F7 live PostgreSQL run).
      pool.queueResult([
        {
          id: '33333333-3333-3333-3333-333333333333',
          token_hash: 'a'.repeat(64),
          user_id: '11111111-1111-1111-1111-111111111111',
          active_site_id: null,
          purpose: 'normal',
          security_version: '1',
          user_security_version: '2',
          created_at: new Date(),
          last_seen_at: new Date(),
          idle_expires_at: new Date(Date.now() + 60_000),
          absolute_expires_at: new Date(Date.now() + 60_000),
          revoked_at: null,
          revocation_reason: null,
          username: 'user',
          email: 'user@example.invalid',
          first_name: 'A',
          last_name: 'B',
          second_last_name: null,
          full_name: 'A B',
          identity_card: '1',
          phone_number: '70000000',
          account_status: 'active',
          is_super_admin: false,
          password_scheme: 'bcrypt',
          password_hash: null,
          must_change_password: false,
        },
      ]);
      const found = await repository.findSessionByTokenHashForUpdate(
        { query: (sql: string, params?: unknown[]) => pool.query(sql, params) } as never,
        'a'.repeat(64),
        new Date(),
      );
      const sql = pool.queries.at(-1)?.sql ?? '';
      const selectList = sql.slice(0, sql.indexOf('FROM'));
      const unaliased = selectList.match(/\b[su]\.security_version\b(?!\s+AS\b)/g) ?? [];
      expect(unaliased).toEqual(['s.security_version']);
      expect(selectList).toMatch(/u\.security_version AS user_security_version/);
      expect(found?.session.securityVersion).not.toBe(found?.identity.securityVersion);
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
      expect(pool.queries.some((query) => query.sql.includes('lu_security_event'))).toBeTruthy();
    });

    it('insertSession stores purpose and security version snapshot', async () => {
      const identity = createIdentity();
      const now = new Date('2026-09-19T12:00:00.000Z');
      const idle = new Date(now.getTime() + 30 * 60 * 1000);
      const abs = new Date(now.getTime() + 12 * 60 * 60 * 1000);
      pool.queueResult([
        {
          id: 'session-id',
          token_hash: 'hash',
          user_id: identity.id,
          active_site_id: null,
          purpose: 'site_selection',
          security_version: identity.securityVersion,
          created_at: now,
          last_seen_at: now,
          idle_expires_at: idle,
          absolute_expires_at: abs,
          revoked_at: null,
          revocation_reason: null,
        },
      ]);

      const session = await repository.insertSession({
        userId: identity.id,
        activeSiteId: null,
        purpose: 'site_selection',
        securityVersion: identity.securityVersion,
        tokenHash: 'hash',
        createdAt: now,
        idleExpiresAt: idle,
        absoluteExpiresAt: abs,
      });

      const insert = pool.queries[0]!;
      expect(insert.sql).toContain('INSERT INTO public.lu_session');
      // (id, token_hash, user_id, active_site_id, purpose, security_version, created_at, idle, absolute)
      expect(insert.params.slice(1, 6)).toEqual([
        'hash',
        identity.id,
        null,
        'site_selection',
        identity.securityVersion,
      ]);
      expect(insert.params[7]).toBe(idle);
      expect(insert.params[8]).toBe(abs);
      expect(session.purpose).toBe('site_selection');
    });

    it('getSessionContext validates in one transaction and rejects a security_version mismatch', async () => {
      const identity = createIdentity({ securityVersion: '9007199254740993' });
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionIdentityRow(identity, { security_version: '9007199254740992' })]);
      pool.queueResult([]); // ROLLBACK

      await expect(repository.getSessionContext('hash', new Date())).rejects.toThrow(
        'Invalid credentials.',
      );
      expect(pool.queries.map((q) => q.sql.trim().split(/\s+/)[0])).toEqual([
        'BEGIN',
        'SELECT',
        'ROLLBACK',
      ]);
      expect(pool.queries[1]!.sql).toContain('FOR UPDATE');
      expect(pool.queries.some((q) => q.sql.includes('lu_security_event'))).toBe(false);
    });

    it('getSessionContext allows a SuperAdmin null-site session and slides idle expiry without events', async () => {
      const identity = createIdentity({ isSuperAdmin: true });
      const now = new Date();
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionIdentityRow(identity)]);
      pool.queueResult([]); // eligible memberships
      pool.queueResult([], 1); // UPDATE last_seen/idle
      pool.queueResult([]); // COMMIT

      const ctx = await repository.getSessionContext('hash', now);

      expect(ctx.identity.isSuperAdmin).toBe(true);
      expect(ctx.activeSiteId).toBeNull();
      const update = pool.queries.find((q) => q.sql.includes('UPDATE public.lu_session'))!;
      expect(update.sql).not.toContain('active_site_id');
      expect((update.params[2] as Date).getTime()).toBeGreaterThan(now.getTime() + 29 * 60 * 1000);
      expect(pool.queries.some((q) => q.sql.includes('lu_security_event'))).toBe(false);
    });

    it('getSessionContext never idle-extends a site_selection session', async () => {
      const identity = createIdentity();
      const row = sessionIdentityRow(identity, {
        purpose: 'site_selection',
        idle_expires_at: new Date(Date.now() + 5 * 60 * 1000),
        absolute_expires_at: new Date(Date.now() + 5 * 60 * 1000),
      });
      pool.queueResult([]); // BEGIN
      pool.queueResult([row]);
      pool.queueResult([
        membershipRow('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'),
        membershipRow('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb'),
      ]);
      pool.queueResult([], 1); // UPDATE
      pool.queueResult([]); // COMMIT

      const ctx = await repository.getSessionContext('hash', new Date());

      expect(ctx.session.purpose).toBe('site_selection');
      expect(ctx.activeSiteId).toBeNull();
      const update = pool.queries.find((q) => q.sql.includes('UPDATE public.lu_session'))!;
      expect(update.params[2]).toEqual(row['idle_expires_at']);
    });

    it('getSessionContext reports an ineligible stored active site as null without rewriting it', async () => {
      const identity = createIdentity();
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        sessionIdentityRow(identity, { active_site_id: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc' }),
      ]);
      pool.queueResult([membershipRow('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa')]);
      pool.queueResult([], 1); // UPDATE
      pool.queueResult([]); // COMMIT

      const ctx = await repository.getSessionContext('hash', new Date());
      expect(ctx.activeSiteId).toBeNull();
    });
  });

  describe('password writes', () => {
    it('rehashUserPassword is a compare-and-swap that stamps password_migrated_at for legacy', async () => {
      const identity = createIdentity({
        passwordScheme: 'legacy_identity_v2',
        passwordHash: 'legacy',
      });
      const now = new Date('2026-09-19T12:00:00.000Z');
      pool.queueResult([]); // BEGIN
      pool.queueResult([{ security_version: '4' }], 1);
      pool.queueResult([]); // COMMIT

      await pool.transaction((client) =>
        repository.rehashUserPassword({
          client,
          userId: identity.id,
          expectedScheme: 'legacy_identity_v2',
          expectedHash: 'legacy',
          newHash: 'new-hash',
          previousSchemeForMigrationFlag: 'legacy_identity_v2',
          now,
        }),
      );

      const update = pool.queries.find((q) => q.sql.includes("SET password_scheme = 'bcrypt'"))!;
      expect(update.sql).toContain('must_change_password = false');
      expect(update.sql).toContain('AND password_scheme = $5');
      expect(update.sql).toContain('AND password_hash = $6');
      expect(update.sql).not.toContain('security_version =');
      expect(update.params).toEqual([
        identity.id,
        'new-hash',
        'legacy_identity_v2',
        now,
        'legacy_identity_v2',
        'legacy',
      ]);
    });

    it('rehashUserPasswordBcryptCost only replaces the hash (flags and migration stamp untouched)', async () => {
      pool.queueResult([]); // BEGIN
      pool.queueResult([{ security_version: '2' }], 1);
      pool.queueResult([]); // COMMIT

      await pool.transaction((client) =>
        repository.rehashUserPasswordBcryptCost({
          client,
          userId: 'user',
          expectedHash: 'old',
          newHash: 'new',
        }),
      );

      const update = pool.queries.find((q) => q.sql.includes('SET password_hash = $2'))!;
      expect(update.sql).not.toContain('password_migrated_at');
      expect(update.sql).not.toContain('must_change_password');
      expect(update.sql).toContain("AND password_scheme = 'bcrypt'");
      expect(update.sql).toContain('AND password_hash = $3');
    });

    it('a lost compare-and-swap surfaces as a generic failure', async () => {
      pool.queueResult([]); // BEGIN
      pool.queueResult([], 0);
      pool.queueResult([]); // ROLLBACK

      await expect(
        pool.transaction((client) =>
          repository.rehashUserPasswordBcryptCost({
            client,
            userId: 'user',
            expectedHash: 'old',
            newHash: 'new',
          }),
        ),
      ).rejects.toThrow('Invalid credentials.');
    });
  });

  describe('setActiveSiteContext', () => {
    it('locks session+user+memberships and updates only the active site', async () => {
      const identity = createIdentity();
      const site = createSite();
      const session = {
        id: 'session-1',
        token_hash: 'hash',
        user_id: identity.id,
        active_site_id: null,
        purpose: 'normal' as const,
        security_version: identity.securityVersion,
        created_at: new Date(),
        last_seen_at: new Date(),
        idle_expires_at: new Date(Date.now() + 30 * 60 * 1000),
        absolute_expires_at: new Date(Date.now() + 60 * 60 * 1000),
        revoked_at: null,
        revocation_reason: null,
      };
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: session.id,
          token_hash: session.token_hash,
          user_id: session.user_id,
          active_site_id: session.active_site_id,
          purpose: session.purpose,
          security_version: session.security_version,
          created_at: session.created_at,
          last_seen_at: session.last_seen_at,
          idle_expires_at: session.idle_expires_at,
          absolute_expires_at: session.absolute_expires_at,
          revoked_at: session.revoked_at,
          revocation_reason: session.revocation_reason,
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
      pool.queueResult([membershipRow(site.id)]);
      pool.queueResult([
        {
          id: session.id,
          token_hash: session.token_hash,
          user_id: session.user_id,
          active_site_id: site.id,
          purpose: session.purpose,
          security_version: session.security_version,
          created_at: session.created_at,
          last_seen_at: session.last_seen_at,
          idle_expires_at: session.idle_expires_at,
          absolute_expires_at: session.absolute_expires_at,
          revoked_at: session.revoked_at,
          revocation_reason: session.revocation_reason,
        },
      ]);
      pool.queueResult([]); // audit INSERT
      pool.queueResult([]); // COMMIT

      await repository.setActiveSiteContext({
        tokenHash: 'hash',
        activeSiteId: site.id,
        now: new Date(),
      });

      const update = pool.queries.find((q) => q.sql.includes('UPDATE public.lu_session'))!;
      expect(update.sql).toContain('active_site_id = $2');
      expect(update.sql).toContain('GREATEST');
    });

    it('rejects site not eligible for normal session', async () => {
      const identity = createIdentity();
      const session = {
        id: 'session-1',
        token_hash: 'hash',
        user_id: identity.id,
        active_site_id: null,
        purpose: 'normal' as const,
        security_version: identity.securityVersion,
        created_at: new Date(),
        last_seen_at: new Date(),
        idle_expires_at: new Date(Date.now() + 30 * 60 * 1000),
        absolute_expires_at: new Date(Date.now() + 60 * 60 * 1000),
        revoked_at: null,
        revocation_reason: null,
      };
      pool.queueResult([]); // BEGIN
      pool.queueResult([
        {
          id: session.id,
          token_hash: session.token_hash,
          user_id: session.user_id,
          active_site_id: session.active_site_id,
          purpose: session.purpose,
          security_version: session.security_version,
          created_at: session.created_at,
          last_seen_at: session.last_seen_at,
          idle_expires_at: session.idle_expires_at,
          absolute_expires_at: session.absolute_expires_at,
          revoked_at: session.revoked_at,
          revocation_reason: session.revocation_reason,
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
      pool.queueResult([]); // empty memberships
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.setActiveSiteContext({
          tokenHash: 'hash',
          activeSiteId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          now: new Date(),
        }),
      ).rejects.toThrow('Site access denied.');
    });

    it('revokes a site_selection session only after confirming the chosen site is eligible', async () => {
      const identity = createIdentity();
      const site = createSite();
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionIdentityRow(identity, { purpose: 'site_selection' })]);
      pool.queueResult([membershipRow(site.id)]); // eligible memberships FOR UPDATE
      pool.queueResult([], 1); // UPDATE revoked_at
      pool.queueResult([]); // COMMIT

      const result = await repository.setActiveSiteContext({
        tokenHash: 'hash',
        activeSiteId: site.id,
        now: new Date(),
      });

      expect(result.session.revokedAt).toBeInstanceOf(Date);
      expect(result.session.revocationReason).toBe('site_selection_resolved');
      const revoke = pool.queries.find((q) => q.sql.includes('SET revoked_at'))!;
      expect(revoke.params[2]).toBe('site_selection_resolved');
    });

    it('refuses site selection from a password_change session before touching memberships', async () => {
      const identity = createIdentity({ mustChangePassword: true });
      pool.queueResult([]); // BEGIN
      pool.queueResult([sessionIdentityRow(identity, { purpose: 'password_change' })]);
      pool.queueResult([]); // ROLLBACK

      await expect(
        repository.setActiveSiteContext({
          tokenHash: 'hash',
          activeSiteId: createSite().id,
          now: new Date(),
        }),
      ).rejects.toThrow('Site access denied.');
      expect(pool.queries.some((q) => q.sql.includes('lu_site_membership'))).toBe(false);
    });
  });
});
