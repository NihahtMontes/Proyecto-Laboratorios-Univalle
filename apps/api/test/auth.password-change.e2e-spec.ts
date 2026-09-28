/**
 * MIG-001 F3 — self password change (F1 §7, §12) through AuthService.
 * F8 — endpoint-specific rate limit on POST /auth/password.
 */
import type { NestFastifyApplication } from '@nestjs/platform-fastify';
import bcrypt from 'bcryptjs';
import { CSRF_COOKIE, SESSION_COOKIE } from '../src/auth/auth.constants.js';
import {
  AuthForbiddenException,
  AuthRateLimitException,
  AuthUnauthorizedException,
  AuthValidationException,
} from '../src/auth/auth.exceptions.js';
import { hashSessionToken } from '../src/auth/auth.crypto.js';
import type { AuthService } from '../src/auth/auth.service.js';
import type { IdentityUser } from '../src/auth/auth.types.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import {
  aspNetV3Hash,
  bcryptHash,
  createAuthService,
  createKernel,
  createTestApp,
  createTestConfig,
  extractCookie,
  TRUSTED_ORIGIN,
  type AuthKernel,
} from './auth.harness.js';

const PASSWORD = 'Correct-Horse-9!';
const NEW_PASSWORD = 'Brand-New-Secret-7?';
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
const CHANGE_IP = '203.0.113.40';

describe('AuthService.changePassword (F3)', () => {
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  let service: AuthService;

  beforeEach(() => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    service = createAuthService(repository, kernel);
  });

  function seed(overrides: Partial<IdentityUser> = {}, sites = [SITE_A]): IdentityUser {
    const identity = createIdentity({
      username: 'ana.perez',
      email: 'ana.perez@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
      ...overrides,
    });
    return repository.addIdentity(
      identity,
      sites.map((s) => createMembership(s, { userId: identity.id })),
    );
  }

  async function loginToken(
    password = PASSWORD,
    extra: Record<string, unknown> = {},
  ): Promise<string> {
    const result = await service.login(
      { loginIdentifier: 'ana.perez', password, ...extra },
      CHANGE_IP,
    );
    return result.token;
  }

  function change(token: string | undefined, body: Record<string, unknown>) {
    return service.changePassword(token, body, CHANGE_IP);
  }

  it('from a normal session: writes bcrypt, rotates, revokes every session and keeps the active site', async () => {
    const user = seed({}, [SITE_A, SITE_B]);
    const token = await loginToken(PASSWORD, { activeSiteId: SITE_B.id });
    const otherToken = await loginToken(PASSWORD, { activeSiteId: SITE_A.id });

    const result = await change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD });

    const after = repository.getIdentity(user.id);
    expect(await bcrypt.compare(NEW_PASSWORD, after.passwordHash!)).toBe(true);
    expect(after.passwordScheme).toBe('bcrypt');
    expect(after.mustChangePassword).toBe(false);
    expect(after.securityVersion).toBe('2');
    expect(repository.passwordMigratedAt.has(user.id)).toBe(false);
    expect(kernel.invalidator.calls).toEqual([{ userId: user.id, reason: 'self_password_change' }]);
    expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).not.toBeNull();
    expect(repository.sessions.get(hashSessionToken(otherToken))!.revokedAt).not.toBeNull();
    expect(result.response.purpose).toBe('normal');
    expect(result.response.session.activeSiteId).toBe(SITE_B.id);
    const replacement = repository.sessions.get(hashSessionToken(result.token))!;
    expect(replacement).toMatchObject({
      securityVersion: '2',
      activeSiteId: SITE_B.id,
      revokedAt: null,
    });
    expect(kernel.audit.events).toContainEqual(
      expect.objectContaining({
        action: 'password_changed',
        subjectUserId: user.id,
        actorUserId: user.id,
      }),
    );
    expect(JSON.stringify(kernel.audit.events)).not.toContain(NEW_PASSWORD);
  });

  it('from a password_change session: clears the forced change and runs the initial site decision', async () => {
    const user = seed({ mustChangePassword: true }, [SITE_A, SITE_B]);
    const token = await loginToken();
    const result = await change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD });
    expect(repository.getIdentity(user.id).mustChangePassword).toBe(false);
    expect(result.response.purpose).toBe('site_selection');
  });

  it('upgrades a verified legacy password longer than 72 bytes and stamps password_migrated_at', async () => {
    const longPassword = 'Legacy-Long-Password-'.repeat(4);
    const user = seed({
      passwordScheme: 'legacy_identity_v3',
      passwordHash: aspNetV3Hash(longPassword),
    });
    const token = await loginToken(longPassword);
    const result = await change(token, {
      currentPassword: longPassword,
      newPassword: NEW_PASSWORD,
    });
    const after = repository.getIdentity(user.id);
    expect(after.passwordScheme).toBe('bcrypt');
    expect(repository.passwordMigratedAt.get(user.id)).toBeInstanceOf(Date);
    expect(result.response.purpose).toBe('normal');
  });

  it('refuses the legacy current password once the deadline is reached and audits it', async () => {
    const longPassword = 'Legacy-Long-Password-'.repeat(4);
    const user = seed({
      passwordScheme: 'legacy_identity_v3',
      passwordHash: aspNetV3Hash(longPassword),
    });
    const token = await loginToken(longPassword);
    kernel.window.open = false;
    await expect(
      change(token, { currentPassword: longPassword, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    expect(repository.getIdentity(user.id).passwordScheme).toBe('legacy_identity_v3');
    expect(kernel.audit.actions()).toContain('legacy_password_rejected');
  });

  it('a wrong current password changes nothing', async () => {
    const user = seed();
    const token = await loginToken();
    await expect(
      change(token, { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    expect(repository.getIdentity(user.id)).toEqual(user);
    expect(kernel.invalidator.calls).toHaveLength(0);
  });

  it('a policy violation reports only violation codes and rolls back', async () => {
    const user = seed();
    const token = await loginToken();
    const error = await change(token, { currentPassword: PASSWORD, newPassword: 'qzqzq' }).catch(
      (e: unknown) => e,
    );
    expect(error).toBeInstanceOf(AuthValidationException);
    expect(JSON.stringify(error)).not.toContain('qzqzq');
    expect(JSON.parse(JSON.stringify(error)).fieldErrors).toEqual({
      newPassword: [
        'too_short',
        'insufficient_distinct',
        'missing_uppercase',
        'missing_digit',
        'missing_symbol',
      ],
    });
    expect(repository.getIdentity(user.id)).toEqual(user);
    expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).toBeNull();
  });

  it('is forbidden for a site_selection session and unauthorized without a session', async () => {
    seed({}, [SITE_A, SITE_B]);
    const token = await loginToken();
    await expect(
      change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthForbiddenException);
    await expect(
      change(undefined, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
  });
});

describe('AuthService.changePassword rate limit (F8)', () => {
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  let service: AuthService;
  const LIMIT = 3;
  const IP_LIMIT = 6;

  beforeEach(() => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    service = createAuthService(
      repository,
      kernel,
      createTestConfig({
        AUTH_RATE_LIMIT_MAX_ATTEMPTS: String(LIMIT),
        AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: String(IP_LIMIT),
      }),
    );
  });

  function seed(overrides: Partial<IdentityUser> = {}, sites = [SITE_A]): IdentityUser {
    const identity = createIdentity({
      username: 'ana.perez',
      email: 'ana.perez@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
      ...overrides,
    });
    return repository.addIdentity(
      identity,
      sites.map((s) => createMembership(s, { userId: identity.id })),
    );
  }

  async function loginToken(
    password = PASSWORD,
    extra: Record<string, unknown> = {},
  ): Promise<string> {
    const result = await service.login(
      { loginIdentifier: 'ana.perez', password, ...extra },
      CHANGE_IP,
    );
    return result.token;
  }

  function change(
    token: string | undefined,
    body: Record<string, unknown>,
    ip: string | undefined = CHANGE_IP,
  ) {
    return service.changePassword(token, body, ip);
  }

  it('returns 429 after the configured attempts even with the correct current password and leaves the hash unchanged', async () => {
    const user = seed();
    const token = await loginToken();
    const originalHash = user.passwordHash;

    for (let i = 0; i < LIMIT; i += 1) {
      await expect(
        change(token, { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    }

    const error = await change(token, {
      currentPassword: PASSWORD,
      newPassword: NEW_PASSWORD,
    }).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(AuthRateLimitException);
    expect((error as AuthRateLimitException).retryAfterSeconds).toBeGreaterThanOrEqual(1);

    const after = repository.getIdentity(user.id);
    expect(after.passwordHash).toBe(originalHash);
    expect(after.passwordScheme).toBe('bcrypt');
    expect(after.securityVersion).toBe('1');
    expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).toBeNull();
    expect(kernel.invalidator.calls).toHaveLength(0);
  });

  it('isolates the password-change bucket per user', async () => {
    const ana = seed();
    const bob = createIdentity({
      id: '22222222-2222-2222-2222-222222222222',
      username: 'bob.lin',
      email: 'bob.lin@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
    });
    repository.addIdentity(bob, [createMembership(SITE_A, { userId: bob.id })]);

    const BOB_IP = '203.0.113.55';
    const anaToken = await loginToken();
    for (let i = 0; i < LIMIT; i += 1) {
      await expect(
        change(anaToken, { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    }
    await expect(
      change(anaToken, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthRateLimitException);

    // Bob uses a different IP so his IP bucket and password-change bucket are
    // both fresh: he can still change his password.
    const bobResult = await service.login(
      { loginIdentifier: 'bob.lin', password: PASSWORD },
      BOB_IP,
    );
    const bobChange = await service.changePassword(
      bobResult.token,
      { currentPassword: PASSWORD, newPassword: NEW_PASSWORD },
      BOB_IP,
    );
    const afterBob = repository.getIdentity(bob.id);
    expect(await bcrypt.compare(NEW_PASSWORD, afterBob.passwordHash!)).toBe(true);
    expect(bobChange.response.purpose).toBe('normal');

    // Ana's hash was never touched.
    expect(repository.getIdentity(ana.id).passwordHash).toBe(ana.passwordHash);
  });

  it('returns 401 for an invalid session without consuming the password-change bucket', async () => {
    const user = seed();
    const token = await loginToken();
    const tokenHash = hashSessionToken(token);
    const existing = repository.sessions.get(tokenHash)!;
    repository.sessions.set(tokenHash, {
      ...existing,
      revokedAt: new Date(),
      revocationReason: 'logout',
    });

    const eventsBefore = repository.securityEvents.length;
    await expect(
      change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);

    expect(repository.getIdentity(user.id).passwordHash).toBe(user.passwordHash);
    expect(repository.securityEvents).toHaveLength(eventsBefore);

    // The bucket is untouched, so a valid subsequent attempt is not blocked.
    await expect(
      change(token, { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
  });

  it('a stale session (rotated security_version) never consumes the owner bucket', async () => {
    const user = seed();
    const staleToken = await loginToken();
    // Rotate the subject's version: the first session is now stale.
    repository.identities.set(user.id, {
      ...repository.getIdentity(user.id),
      securityVersion: '2',
    });

    for (let i = 0; i < LIMIT + 2; i += 1) {
      await expect(
        change(staleToken, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    }

    // The owner, with a fresh session, is not locked out by the stale attempts.
    const freshToken = await loginToken();
    const result = await change(freshToken, {
      currentPassword: PASSWORD,
      newPassword: NEW_PASSWORD,
    });
    expect(result.response.purpose).toBe('normal');
    expect(await bcrypt.compare(NEW_PASSWORD, repository.getIdentity(user.id).passwordHash!)).toBe(
      true,
    );
  });

  it('returns 401 with no session cookie without consuming the bucket', async () => {
    seed();
    const eventsBefore = repository.securityEvents.length;
    await expect(
      change(undefined, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD }),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    expect(repository.securityEvents).toHaveLength(eventsBefore);
  });

  it('password-change attempts do not consume the login bucket for the same user/ip', async () => {
    seed();
    const { token } = await service.login(
      { loginIdentifier: 'ana.perez', password: PASSWORD },
      CHANGE_IP,
    );
    // LIMIT wrong-current-password attempts consume the password-change bucket
    // and the shared IP bucket; the login email_ip bucket stays at 1.
    for (let i = 0; i < LIMIT; i += 1) {
      await expect(
        change(token, { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD }),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    }
    // A fresh wrong-password login must still fail 401, not 429.
    await expect(
      service.login({ loginIdentifier: 'ana.perez', password: 'Wrong-Horse-9!' }, CHANGE_IP),
    ).rejects.toBeInstanceOf(AuthUnauthorizedException);
  });

  it('login attempts do not consume the password-change bucket for the same user/ip', async () => {
    seed();
    const LOGIN_IP = '203.0.113.50';
    // LIMIT wrong-password logins consume the login bucket on LOGIN_IP only.
    for (let i = 0; i < LIMIT; i += 1) {
      await expect(
        service.login({ loginIdentifier: 'ana.perez', password: 'Wrong-Horse-9!' }, LOGIN_IP),
      ).rejects.toBeInstanceOf(AuthUnauthorizedException);
    }
    // A password-change from CHANGE_IP (a fresh IP bucket) succeeds without
    // being blocked by the exhausted login bucket on LOGIN_IP.
    const { token } = await service.login(
      { loginIdentifier: 'ana.perez', password: PASSWORD },
      CHANGE_IP,
    );
    const result = await change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD });
    expect(result.response.purpose).toBe('normal');
  });

  it('a normal single change succeeds and issues the replacement session (F8 happy path)', async () => {
    const user = seed({}, [SITE_A, SITE_B]);
    // Pin a single eligible site so the login yields a normal (not site_selection) session.
    const token = await loginToken(PASSWORD, { activeSiteId: SITE_A.id });
    const result = await change(token, { currentPassword: PASSWORD, newPassword: NEW_PASSWORD });

    const after = repository.getIdentity(user.id);
    expect(await bcrypt.compare(NEW_PASSWORD, after.passwordHash!)).toBe(true);
    expect(after.securityVersion).toBe('2');
    expect(repository.sessions.get(hashSessionToken(token))!.revokedAt).not.toBeNull();
    const replacement = repository.sessions.get(hashSessionToken(result.token))!;
    expect(replacement).toMatchObject({ securityVersion: '2', revokedAt: null });
    expect(result.response.purpose).toBe('normal');
  });
});

describe('POST /auth/password rate limit at the controller (F8)', () => {
  let app: NestFastifyApplication;
  let repository: FakeAuthRepository;
  let kernel: AuthKernel;
  let csrf: string;
  let sessionToken: string;
  let userId: string;
  const LIMIT = 3;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    kernel = createKernel(repository);
    const config = createTestConfig({
      AUTH_RATE_LIMIT_MAX_ATTEMPTS: String(LIMIT),
      AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: String(LIMIT * 2),
    });
    app = await createTestApp(repository, kernel, config);

    const identity = createIdentity({
      username: 'ana.perez',
      email: 'ana.perez@univalle.test',
      passwordHash: bcryptHash(PASSWORD),
    });
    userId = repository.addIdentity(identity, [
      createMembership(SITE_A, { userId: identity.id }),
    ]).id;

    const csrfResponse = await app.inject({ method: 'GET', url: '/api/v1/auth/csrf' });
    csrf = (JSON.parse(csrfResponse.payload) as { csrfToken: string }).csrfToken;

    const loginResponse = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/login',
      headers: {
        origin: TRUSTED_ORIGIN,
        'x-csrf-token': csrf,
        cookie: `${CSRF_COOKIE}=${encodeURIComponent(csrf)}`,
      },
      payload: { loginIdentifier: 'ana.perez', password: PASSWORD },
    });
    expect(loginResponse.statusCode).toBe(200);
    sessionToken = extractCookie(loginResponse.headers['set-cookie'], SESSION_COOKIE)!;
    expect(sessionToken).toBeDefined();
  });

  afterEach(async () => {
    await app.close();
  });

  function passwordHeaders(): Record<string, string> {
    return {
      origin: TRUSTED_ORIGIN,
      'x-csrf-token': csrf,
      cookie: `${CSRF_COOKIE}=${encodeURIComponent(csrf)}; ${SESSION_COOKIE}=${sessionToken}`,
    };
  }

  it('replies with 429 and a numeric Retry-After header after the configured attempts', async () => {
    for (let i = 0; i < LIMIT; i += 1) {
      const rejected = await app.inject({
        method: 'POST',
        url: '/api/v1/auth/password',
        headers: passwordHeaders(),
        payload: { currentPassword: 'Wrong-Horse-9!', newPassword: NEW_PASSWORD },
      });
      expect(rejected.statusCode).toBe(401);
    }

    const limited = await app.inject({
      method: 'POST',
      url: '/api/v1/auth/password',
      headers: passwordHeaders(),
      payload: { currentPassword: PASSWORD, newPassword: NEW_PASSWORD },
    });
    expect(limited.statusCode).toBe(429);
    const retryAfter = Number(limited.headers['retry-after']);
    expect(Number.isFinite(retryAfter)).toBe(true);
    expect(retryAfter).toBeGreaterThanOrEqual(1);

    // Real state: the hash is unchanged and the existing session is not revoked.
    const after = repository.getIdentity(userId);
    expect(await bcrypt.compare(PASSWORD, after.passwordHash!)).toBe(true);
    expect(repository.sessions.get(hashSessionToken(sessionToken))!.revokedAt).toBeNull();
  });
});
