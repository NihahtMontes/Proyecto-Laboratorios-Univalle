import 'reflect-metadata';
import bcrypt from 'bcryptjs';
import { Test } from '@nestjs/testing';
import type { ActiveSiteSession } from '@lu/contracts';
import { AuthService } from '../src/auth/auth.service.js';
import { AUTH_CONFIG } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthRateLimitService } from '../src/auth/auth.rate-limit.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { hashSessionToken } from '../src/auth/auth.crypto.js';
import type { Session, User } from '../src/auth/auth.types.js';
import {
  createMembership,
  createSession,
  createSite,
  createUser,
  FakeAuthRepository,
} from './auth.fakes.js';

function expectContractShape(session: ActiveSiteSession): void {
  const keys = Object.keys(session).sort();
  expect(keys).toEqual([
    'activeSiteId',
    'activeSiteName',
    'displayName',
    'email',
    'globalRole',
    'memberships',
    'userId',
  ]);
  expect(session.userId).toEqual(expect.any(String));
  expect(session.displayName).toEqual(expect.any(String));
  expect(session.email).toEqual(expect.any(String));
  for (const membership of session.memberships) {
    expect(Object.keys(membership).sort()).toEqual(['role', 'siteId', 'siteName', 'state']);
  }
}

async function hashPassword(password: string): Promise<string> {
  return bcrypt.hash(password, 12);
}

describe('AuthService', () => {
  let service: AuthService;
  let repository: FakeAuthRepository;
  let rateLimit: AuthRateLimitService;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    const moduleRef = await Test.createTestingModule({
      providers: [
        AuthService,
        { provide: AuthRepository, useValue: repository },
        AuthRateLimitService,
        {
          provide: AUTH_CONFIG,
          useValue: new AuthConfig({
            AUTH_ALLOWED_ORIGINS: 'http://localhost:3000',
            AUTH_LOGIN_FLOOR_MS: '0',
            AUTH_RATE_LIMIT_MAX_ATTEMPTS: '10',
            AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '50',
            AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
            AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
          }),
        },
      ],
    }).compile();

    service = moduleRef.get(AuthService);
    rateLimit = moduleRef.get(AuthRateLimitService);
    rateLimit.reset();
  });

  describe('login', () => {
    it('succeeds with valid credentials', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password) });
      const site = createSite();
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      const result = await service.login(
        { email: user.email, password, activeSiteId: null },
        '127.0.0.1',
      );

      expect(result.session.userId).toBe(user.id);
      expect(result.session.activeSiteId).toBe(site.id);
      expect(result.session.activeSiteName).toBe(site.name);
      expect(result.token.length).toBeGreaterThan(0);
      expectContractShape(result.session);
    });

    it('returns uniform failure for bad password', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password) });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password: 'wrong', activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('returns uniform failure for inactive user', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password), status: 'disabled' });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('returns uniform failure for malformed password hash', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: 'not-a-bcrypt-hash' });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('returns uniform failure for password hash with wrong bcrypt cost', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await bcrypt.hash(password, 4) });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('rejects normal user when no eligible memberships remain', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password) });
      const site = createSite({ status: 'disabled' });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('rejects active normal user with zero eligible memberships', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password) });
      repository.users.set(user.id, user);
      // no memberships

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('allows SuperAdmin global session with zero memberships', async () => {
      const password = 'valid-pass';
      const user = createUser({
        passwordHash: await hashPassword(password),
        isSuperAdmin: true,
      });
      repository.users.set(user.id, user);

      const result = await service.login(
        { email: user.email, password, activeSiteId: null },
        '127.0.0.1',
      );
      expect(result.session.activeSiteId).toBeNull();
      expect(result.session.activeSiteName).toBeNull();
      expect(result.session.memberships).toHaveLength(0);
      expect(result.session.globalRole).toBe('SuperAdmin');
      expectContractShape(result.session);
    });

    it('does not bypass active site for SuperAdmin without membership', async () => {
      const password = 'valid-pass';
      const user = createUser({
        passwordHash: await hashPassword(password),
        isSuperAdmin: true,
      });
      repository.users.set(user.id, user);

      await expect(
        service.login(
          {
            email: user.email,
            password,
            activeSiteId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          },
          '127.0.0.1',
        ),
      ).rejects.toThrow('Invalid credentials.');
    });

    it('rejects activeSiteId not in eligible memberships', async () => {
      const password = 'valid-pass';
      const user = createUser({ passwordHash: await hashPassword(password) });
      const site = createSite({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      await expect(
        service.login(
          { email: user.email, password, activeSiteId: 'bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb' },
          '127.0.0.1',
        ),
      ).rejects.toThrow('Invalid credentials.');
    });
  });

  function storeSession(
    repository: FakeAuthRepository,
    user: User,
    overrides?: Partial<Session>,
  ): { rawToken: string; tokenHash: string; session: Session } {
    const rawToken = `raw-token-${user.id}`;
    const tokenHash = hashSessionToken(rawToken);
    const session = createSession(user, { tokenHash, ...overrides });
    repository.sessions.set(tokenHash, session);
    return { rawToken, tokenHash, session };
  }

  describe('constantTimeBcryptCompare dummy hardening', () => {
    beforeEach(async () => {
      repository = new FakeAuthRepository();
      const moduleRef = await Test.createTestingModule({
        providers: [
          AuthService,
          { provide: AuthRepository, useValue: repository },
          AuthRateLimitService,
          {
            provide: AUTH_CONFIG,
            useValue: new AuthConfig({
              AUTH_ALLOWED_ORIGINS: 'http://localhost:3000',
              AUTH_LOGIN_FLOOR_MS: '0',
              AUTH_RATE_LIMIT_MAX_ATTEMPTS: '10',
              AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '50',
              AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
              AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
              AUTH_BCRYPT_COST: '10',
            }),
          },
        ],
      }).compile();

      service = moduleRef.get(AuthService);
      rateLimit = moduleRef.get(AuthRateLimitService);
      rateLimit.reset();
    });

    it('always rejects an invalid hash even if the password matches the dummy', async () => {
      const password = 'dummy';
      const user = createUser({ passwordHash: 'not-a-bcrypt-hash' });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
      expect(repository.sessions.size).toBe(0);
    });

    it('always rejects a hash with a different cost even if the password matches', async () => {
      const password = 'dummy';
      const user = createUser({ passwordHash: await bcrypt.hash(password, 4) });
      repository.users.set(user.id, user);

      await expect(
        service.login({ email: user.email, password, activeSiteId: null }, '127.0.0.1'),
      ).rejects.toThrow('Invalid credentials.');
      expect(repository.sessions.size).toBe(0);
    });
  });

  describe('getSession', () => {
    it('rejects revoked session', async () => {
      const user = createUser();
      const { rawToken, tokenHash } = storeSession(repository, user, { revokedAt: new Date() });
      repository.users.set(user.id, user);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
      expect(repository.sessions.get(tokenHash)!.revokedAt).not.toBeNull();
    });

    it('rejects expired session', async () => {
      const user = createUser();
      const now = new Date();
      const { rawToken } = storeSession(repository, user, {
        idleExpiresAt: new Date(now.getTime() - 1),
        absoluteExpiresAt: new Date(now.getTime() + 1000),
      });
      repository.users.set(user.id, user);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
    });

    it('rejects session when user security version changed', async () => {
      const user = createUser({ securityVersion: '2' });
      const { rawToken } = storeSession(repository, user, { securityVersion: '1' });
      repository.users.set(user.id, user);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
    });

    it('rejects session when user status is disabled', async () => {
      const user = createUser({ status: 'disabled' });
      const { rawToken } = storeSession(repository, user);
      repository.users.set(user.id, user);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
    });

    it('rejects normal user session after losing all eligible memberships', async () => {
      const user = createUser();
      const site = createSite({ status: 'disabled' });
      const { rawToken } = storeSession(repository, user, { activeSiteId: site.id });
      repository.users.set(user.id, user);
      repository.memberships.set(user.id, [createMembership(site)]);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
    });

    it('preserves SuperAdmin global session with zero memberships', async () => {
      const user = createUser({ isSuperAdmin: true });
      const { rawToken } = storeSession(repository, user);
      repository.users.set(user.id, user);

      const session = await service.getSession(rawToken);
      expect(session.userId).toBe(user.id);
      expect(session.activeSiteId).toBeNull();
      expect(session.globalRole).toBe('SuperAdmin');
      expect(session.memberships).toHaveLength(0);
      expectContractShape(session);
    });

    it('preserves string security versions above MAX_SAFE_INTEGER', async () => {
      const previous = '9007199254740992';
      const next = '9007199254740993';
      expect(previous).not.toBe(next);
      expect(Number(previous)).toBe(Number(next));

      const user = createUser({ securityVersion: next });
      const { rawToken } = storeSession(repository, user, { securityVersion: previous });
      repository.users.set(user.id, user);

      await expect(service.getSession(rawToken)).rejects.toThrow('Invalid credentials.');
    });
  });
});
