/**
 * MIG-001 F3 — login contract (F1 §6, §7, §12, §17) exercised through
 * AuthService with the real bcrypt / ASP.NET Identity strategies.
 */
import bcrypt from 'bcryptjs';
import { AuthUnauthorizedException, AuthValidationException } from '../src/auth/auth.exceptions.js';
import { hashSessionToken } from '../src/auth/auth.crypto.js';
import type { AuthService } from '../src/auth/auth.service.js';
import type { IdentityUser } from '../src/auth/auth.types.js';
import {
  createIdentity,
  createMembership,
  createSession,
  createSite,
  FakeAuthRepository,
} from './auth.fakes.js';
import {
  aspNetV3Hash,
  bcryptHash,
  createAuthService,
  createKernel,
  createTestConfig,
  type AuthKernel,
} from './auth.harness.js';
import { IDENTITY_VECTORS, MALFORMED_IDENTITY_VECTORS } from './identity-vectors.js';

const PASSWORD = 'Correct-Horse-9!';
const SITE_A = createSite({
  id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  code: 'A',
  name: 'Sede A',
});
const SITE_B = createSite({
  id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
  code: 'B',
  name: 'Sede B',
});
const IP = '203.0.113.10';

describe('AuthService login (F3)', () => {
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  let service: AuthService;

  beforeEach(() => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    service = createAuthService(repository, kernel);
  });

  function seedUser(overrides: Partial<IdentityUser> = {}, sites = [SITE_A]): IdentityUser {
    const identity = createIdentity({
      username: 'ana.perez',
      email: 'ana.perez@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
      ...overrides,
    });
    return repository.addIdentity(
      identity,
      sites.map((site) => createMembership(site, { userId: identity.id })),
    );
  }

  function login(body: Record<string, unknown>) {
    return service.login(body, IP);
  }

  describe('identifier resolution', () => {
    it('authenticates by username and auto-selects the single eligible site', async () => {
      const user = seedUser();
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      expect(result.response.purpose).toBe('normal');
      expect(result.response.session.userId).toBe(user.id);
      expect(result.response.session.activeSiteId).toBe(SITE_A.id);
      expect(result.activeSiteRole).toBe('Administrador');
      const stored = repository.sessions.get(hashSessionToken(result.token));
      expect(stored?.activeSiteId).toBe(SITE_A.id);
      expect(stored?.securityVersion).toBe(user.securityVersion);
    });

    it('authenticates by loginIdentifier=email with the same normalization as the claim table', async () => {
      const user = seedUser();
      const result = await login({
        loginIdentifier: 'ana.perez@univalle.test',
        password: PASSWORD,
      });
      expect(result.response.purpose).toBe('normal');
      expect(result.response.session.userId).toBe(user.id);
    });

    it('normalizes email-style identifiers with whitespace and case differences', async () => {
      seedUser();
      const result = await login({
        loginIdentifier: '  ANA.Perez@Univalle.TEST ',
        password: PASSWORD,
      });
      expect(result.response.purpose).toBe('normal');
    });

    it('rejects the deprecated email alias when loginIdentifier is absent (F8 removed the alias)', async () => {
      seedUser();
      await expect(
        login({ email: 'ana.perez@univalle.test', password: PASSWORD }),
      ).rejects.toBeInstanceOf(AuthValidationException);
    });

    it('rejects loginIdentifier combined with the deprecated email alias', async () => {
      seedUser();
      await expect(
        login({
          loginIdentifier: 'ana.perez',
          email: 'ana.perez@univalle.test',
          password: PASSWORD,
        }),
      ).rejects.toBeInstanceOf(AuthValidationException);
    });

    it('rejects a login body with no loginIdentifier', async () => {
      seedUser();
      await expect(login({ password: PASSWORD } as Record<string, unknown>)).rejects.toBeInstanceOf(
        AuthValidationException,
      );
    });

    it.each([['isSuperAdmin'], ['globalRole'], ['securityVersion']])(
      'rejects the privileged key %s',
      async (key) => {
        seedUser();
        await expect(
          login({ loginIdentifier: 'ana.perez', password: PASSWORD, [key]: true }),
        ).rejects.toBeInstanceOf(AuthValidationException);
      },
    );

    it('bounds the password transport at 512 UTF-8 bytes', async () => {
      seedUser();
      await expect(
        login({ loginIdentifier: 'ana.perez', password: 'é'.repeat(256) + 'x' }),
      ).rejects.toBeInstanceOf(AuthValidationException);
      await expect(
        login({ loginIdentifier: 'ana.perez', password: 'é'.repeat(256) }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    });
  });

  describe('generic failures', () => {
    it.each([
      ['absent user', () => undefined, 'nobody', 'absent_user'],
      [
        'inactive account',
        (r: FakeAuthRepository) => seedInto(r, { accountStatus: 'inactive' }),
        'ana.perez',
        'inactive_account',
      ],
      [
        'deleted account',
        (r: FakeAuthRepository) => seedInto(r, { accountStatus: 'deleted' }),
        'ana.perez',
        'inactive_account',
      ],
      [
        'reset_required',
        (r: FakeAuthRepository) =>
          seedInto(r, {
            passwordScheme: 'reset_required',
            passwordHash: null,
            mustChangePassword: true,
          }),
        'ana.perez',
        'reset_required',
      ],
    ] as const)(
      '%s yields the generic failure and no session',
      async (_label, seed, identifier, reason) => {
        seed(repository);
        await expect(
          login({ loginIdentifier: identifier, password: PASSWORD }),
        ).rejects.toBeInstanceOf(AuthUnauthorizedException);
        expect(repository.sessions.size).toBe(0);
        expect(repository.securityEvents.at(-1)).toMatchObject({
          eventType: 'login_failure',
          metadata: { reason },
        });
      },
    );

    it('a wrong bcrypt password writes nothing and rotates nothing', async () => {
      const user = seedUser();
      await expect(
        login({ loginIdentifier: 'ana.perez', password: 'Wrong-Horse-9!' }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
      expect(repository.getIdentity(user.id)).toEqual(user);
      expect(kernel.invalidator.calls).toHaveLength(0);
      expect(repository.sessions.size).toBe(0);
    });

    it('never echoes the password in errors or security events', async () => {
      seedUser();
      const secret = 'Leaky-Secret-123!';
      await expect(login({ loginIdentifier: 'ana.perez', password: secret })).rejects.toThrow();
      expect(JSON.stringify(repository.securityEvents)).not.toContain(secret);
      expect(JSON.stringify(kernel.audit.events)).not.toContain(secret);
    });
  });

  describe('legacy ASP.NET Identity compatibility', () => {
    const v2 = IDENTITY_VECTORS.find((v) => v.version === 2)!;
    const v3 = IDENTITY_VECTORS.find((v) => v.version === 3 && v.prf === 'sha512')!;

    it.each([
      ['legacy_identity_v2', v2],
      ['legacy_identity_v3', v3],
    ] as const)(
      'verifies %s, rehashes to bcrypt and rotates before issuing the session',
      async (scheme, vector) => {
        const user = seedUser({
          passwordScheme: scheme,
          passwordHash: vector.base64,
          securityVersion: '5',
        });
        const stale = createSession(user, { tokenHash: 'stale-session', id: 'stale' });
        repository.sessions.set(stale.tokenHash, stale);

        const result = await login({ loginIdentifier: 'ana.perez', password: vector.password });

        const after = repository.getIdentity(user.id);
        expect(after.passwordScheme).toBe('bcrypt');
        expect(after.mustChangePassword).toBe(false);
        expect(bcrypt.getRounds(after.passwordHash!)).toBe(4);
        expect(await bcrypt.compare(vector.password, after.passwordHash!)).toBe(true);
        expect(repository.passwordMigratedAt.get(user.id)).toBeInstanceOf(Date);
        expect(after.securityVersion).toBe('6');
        expect(kernel.invalidator.calls).toEqual([
          { userId: user.id, reason: 'credential_rehash' },
        ]);
        expect(repository.sessions.get('stale-session')?.revokedAt).not.toBeNull();
        const issued = repository.sessions.get(hashSessionToken(result.token));
        expect(issued?.securityVersion).toBe('6');
        expect(result.response.purpose).toBe('normal');
        expect(kernel.audit.events).toContainEqual(
          expect.objectContaining({
            action: 'credential_rehashed',
            subjectUserId: user.id,
            metadata: { from_scheme: scheme, to_scheme: 'bcrypt' },
          }),
        );
      },
    );

    it('proceeds when no legacy deadline is recorded (window open) and refuses once it is reached', async () => {
      const user = seedUser({ passwordScheme: 'legacy_identity_v2', passwordHash: v2.base64 });
      kernel.window.open = false;
      await expect(
        login({ loginIdentifier: 'ana.perez', password: v2.password }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
      expect(repository.getIdentity(user.id).passwordScheme).toBe('legacy_identity_v2');
      expect(kernel.audit.actions()).toEqual(['legacy_password_rejected']);
      expect(repository.sessions.size).toBe(0);

      kernel.window.open = true;
      const result = await login({ loginIdentifier: 'ana.perez', password: v2.password });
      expect(result.response.purpose).toBe('normal');
    });

    it('a wrong legacy password is never rehashed', async () => {
      const user = seedUser({ passwordScheme: 'legacy_identity_v3', passwordHash: v3.base64 });
      await expect(
        login({ loginIdentifier: 'ana.perez', password: 'not-the-password' }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
      expect(repository.getIdentity(user.id)).toEqual(user);
      expect(kernel.audit.events).toHaveLength(0);
    });

    it.each(MALFORMED_IDENTITY_VECTORS.map((v) => [v.name, v.base64] as const))(
      'fails closed for the malformed stored hash %s',
      async (_name, base64) => {
        const user = seedUser({ passwordScheme: 'legacy_identity_v3', passwordHash: base64 });
        await expect(
          login({ loginIdentifier: 'ana.perez', password: v3.password }),
        ).rejects.toBeInstanceOf(AuthUnauthorizedException);
        expect(repository.getIdentity(user.id)).toEqual(user);
      },
    );

    it('a verified legacy password over 72 bytes yields only a password_change session and no rehash', async () => {
      const longPassword = 'Legacy-Long-Password-'.repeat(4); // 84 ASCII bytes
      const hash = aspNetV3Hash(longPassword);
      const user = seedUser({ passwordScheme: 'legacy_identity_v3', passwordHash: hash });

      const result = await login({ loginIdentifier: 'ana.perez', password: longPassword });

      expect(result.response.purpose).toBe('password_change');
      expect(result.response.session.activeSiteId).toBeNull();
      expect(result.response.eligibleSites).toEqual([]);
      expect(repository.getIdentity(user.id)).toEqual(user);
      expect(kernel.invalidator.calls).toHaveLength(0);
      const issued = repository.sessions.get(hashSessionToken(result.token));
      expect(issued).toMatchObject({ purpose: 'password_change', activeSiteId: null });
    });
  });

  describe('bcrypt state', () => {
    it('rehashes an allowed non-current cost, rotating security_version', async () => {
      const user = seedUser({ passwordHash: bcryptHash(PASSWORD, 5) });
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      const after = repository.getIdentity(user.id);
      expect(bcrypt.getRounds(after.passwordHash!)).toBe(4);
      expect(after.securityVersion).toBe('2');
      expect(repository.passwordMigratedAt.has(user.id)).toBe(false);
      expect(repository.sessions.get(hashSessionToken(result.token))?.securityVersion).toBe('2');
    });

    it('a cost rehash preserves must_change_password (forced change is not bypassed)', async () => {
      const user = seedUser({ passwordHash: bcryptHash(PASSWORD, 5), mustChangePassword: true });
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      expect(repository.getIdentity(user.id).mustChangePassword).toBe(true);
      expect(result.response.purpose).toBe('password_change');
      expect(result.response.mustChangePassword).toBe(true);
    });

    it('must_change_password issues a password_change session without site capability', async () => {
      seedUser({ mustChangePassword: true }, [SITE_A, SITE_B]);
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      expect(result.response.purpose).toBe('password_change');
      expect(result.response.session.activeSiteId).toBeNull();
      expect(result.response.session.memberships).toEqual([]);
      expect(result.response.eligibleSites).toEqual([]);
    });

    it('a lost compare-and-swap during rehash fails generically without issuing a session', async () => {
      const user = seedUser({ passwordHash: bcryptHash(PASSWORD, 5) });
      repository.rehashUserPasswordBcryptCost = async () => {
        throw new AuthUnauthorizedException();
      };
      await expect(
        login({ loginIdentifier: 'ana.perez', password: PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
      expect(repository.getIdentity(user.id)).toEqual(user);
      expect(repository.sessions.size).toBe(0);
    });
  });

  describe('initial site resolution (F1 §17)', () => {
    it('denies a non-SuperAdmin without eligible memberships', async () => {
      seedUser({}, []);
      await expect(
        login({ loginIdentifier: 'ana.perez', password: PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    });

    it('ignores ineligible memberships (suspended, expired, inactive site)', async () => {
      const identity = createIdentity({
        username: 'ana.perez',
        email: 'a@x.test',
        passwordHash: bcryptHash(PASSWORD),
      });
      repository.addIdentity(identity, [
        createMembership(SITE_A, { userId: identity.id, status: 'suspended' }),
        createMembership(SITE_B, { userId: identity.id, validUntil: new Date(Date.now() - 1000) }),
      ]);
      await expect(
        login({ loginIdentifier: 'ana.perez', password: PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    });

    it('gives a SuperAdmin without memberships a normal null-site session', async () => {
      seedUser({ isSuperAdmin: true }, []);
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      expect(result.response.purpose).toBe('normal');
      expect(result.response.session.activeSiteId).toBeNull();
      expect(result.response.session.globalRole).toBe('SuperAdmin');
    });

    it('issues a 15-minute site_selection session without idle extension for several sites', async () => {
      seedUser({}, [SITE_A, SITE_B]);
      const before = Date.now();
      const result = await login({ loginIdentifier: 'ana.perez', password: PASSWORD });
      expect(result.response.purpose).toBe('site_selection');
      expect(result.response.eligibleSites.map((s) => s.siteId)).toEqual([SITE_A.id, SITE_B.id]);
      const session = repository.sessions.get(hashSessionToken(result.token))!;
      expect(session.activeSiteId).toBeNull();
      const ttl = session.absoluteExpiresAt.getTime() - before;
      expect(ttl).toBeGreaterThanOrEqual(15 * 60 * 1000 - 50);
      expect(ttl).toBeLessThanOrEqual(15 * 60 * 1000 + 1000);
      expect(session.idleExpiresAt.getTime()).toBe(session.absoluteExpiresAt.getTime());
    });

    it('honors a requested eligible site and rejects an ineligible one', async () => {
      seedUser({}, [SITE_A, SITE_B]);
      const ok = await login({
        loginIdentifier: 'ana.perez',
        password: PASSWORD,
        activeSiteId: SITE_B.id,
      });
      expect(ok.response.purpose).toBe('normal');
      expect(ok.response.session.activeSiteId).toBe(SITE_B.id);
      await expect(
        login({
          loginIdentifier: 'ana.perez',
          password: PASSWORD,
          activeSiteId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    });
  });

  describe('timing floor', () => {
    it('every outcome past the rate limit takes at least AUTH_LOGIN_FLOOR_MS', async () => {
      service = createAuthService(
        repository,
        kernel,
        createTestConfig({ AUTH_LOGIN_FLOOR_MS: '120' }),
      );
      seedUser();
      for (const body of [
        { loginIdentifier: 'nobody', password: PASSWORD },
        { loginIdentifier: 'ana.perez', password: 'Wrong-Horse-9!' },
        { loginIdentifier: 'ana.perez', password: PASSWORD },
      ]) {
        const started = Date.now();
        await login(body).catch(() => undefined);
        expect(Date.now() - started).toBeGreaterThanOrEqual(115);
      }
    });
  });
});

function seedInto(repository: FakeAuthRepository, overrides: Partial<IdentityUser>): void {
  const identity = createIdentity({
    username: 'ana.perez',
    email: 'ana.perez@univalle.test',
    passwordHash: bcryptHash(PASSWORD),
    ...overrides,
  });
  repository.addIdentity(identity, [createMembership(SITE_A, { userId: identity.id })]);
}
