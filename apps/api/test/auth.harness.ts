/**
 * Shared MIG-001 F3 auth test harness.
 *
 * Uses the REAL identity-kernel strategies (bcrypt at the test-seam cost and
 * the ASP.NET Identity V2/V3 adapters) and the real password policy/hasher, so
 * service, controller and guard tests exercise genuine credential behavior.
 * Persistence, rotation, audit and the legacy window are stateful fakes bound
 * to one FakeAuthRepository.
 */
import 'reflect-metadata';
import { pbkdf2Sync } from 'node:crypto';
import bcrypt from 'bcryptjs';
import { FastifyAdapter, type NestFastifyApplication } from '@nestjs/platform-fastify';
import { Test, type TestingModuleBuilder } from '@nestjs/testing';
import { AppModule } from '../src/app.module.js';
import { AUTH_CONFIG, AUTH_PG_POOL } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthRateLimitService } from '../src/auth/auth.rate-limit.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { AuthService } from '../src/auth/auth.service.js';
import type { IPgClient } from '../src/auth/auth.pg-pool.js';
import {
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  PASSWORD_VERIFICATION_SERVICE,
  SESSION_INVALIDATOR,
  type IdentityAuditEvent,
  type IdentityAuditWriter,
  type LegacyPasswordWindow,
  type SecurityRotationReason,
  type SessionInvalidator,
} from '../src/identity/identity.contracts.js';
import { AspNetIdentityPasswordVerifier } from '../src/identity/password/aspnet-identity.verifier.js';
import { BcryptPasswordHasher } from '../src/identity/password/bcrypt.hasher.js';
import { BcryptPasswordVerifier } from '../src/identity/password/bcrypt.verifier.js';
import { Utf8PasswordPolicy } from '../src/identity/password/password-policy.js';
import { DefaultPasswordVerificationService } from '../src/identity/password/password-verification.service.js';
import { FakeAuthRepository } from './auth.fakes.js';

export const TEST_BCRYPT_COST = 4;
export const TEST_DUMMY_HASH = bcrypt.hashSync('dummy-not-a-credential', TEST_BCRYPT_COST);
export const TRUSTED_ORIGIN = 'http://localhost:3000';

export const TEST_ENV: Readonly<Record<string, string>> = {
  AUTH_ALLOWED_ORIGINS: TRUSTED_ORIGIN,
  AUTH_SESSION_IDLE_TTL_SECONDS: '1800',
  AUTH_SESSION_ABSOLUTE_TTL_SECONDS: '43200',
  AUTH_LOGIN_FLOOR_MS: '0',
  AUTH_RATE_LIMIT_MAX_ATTEMPTS: '50',
  AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '100',
  AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
  AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
  AUTH_CSRF_MAX_AGE_SECONDS: '600',
  AUTH_BCRYPT_COST: String(TEST_BCRYPT_COST),
  AUTH_BCRYPT_DUMMY_HASH: TEST_DUMMY_HASH,
};

export function createTestConfig(overrides: Record<string, string> = {}): AuthConfig {
  return new AuthConfig({ ...TEST_ENV, ...overrides }, { testSeam: true });
}

export class RecordingAuditWriter implements IdentityAuditWriter {
  readonly events: IdentityAuditEvent[] = [];
  async append(_client: IPgClient, event: IdentityAuditEvent): Promise<void> {
    this.events.push(event);
  }
  actions(): string[] {
    return this.events.map((e) => e.action);
  }
}

export class SettableLegacyWindow implements LegacyPasswordWindow {
  open = true;
  async isOpen(): Promise<boolean> {
    return this.open;
  }
}

export class FakeSessionInvalidator implements SessionInvalidator {
  readonly calls: { readonly userId: string; readonly reason: SecurityRotationReason }[] = [];
  constructor(private readonly repository: FakeAuthRepository) {}
  async rotateAndRevokeAll(
    _client: IPgClient,
    userId: string,
    reason: SecurityRotationReason,
  ): Promise<string> {
    this.calls.push({ userId, reason });
    return this.repository.rotateAndRevokeAll(userId, reason);
  }
}

export interface AuthKernel {
  readonly verification: DefaultPasswordVerificationService;
  readonly hasher: BcryptPasswordHasher;
  readonly policy: Utf8PasswordPolicy;
  readonly invalidator: FakeSessionInvalidator;
  readonly audit: RecordingAuditWriter;
  readonly window: SettableLegacyWindow;
}

export function createKernel(repository: FakeAuthRepository): AuthKernel {
  return {
    verification: new DefaultPasswordVerificationService(
      [
        new BcryptPasswordVerifier({
          currentCost: TEST_BCRYPT_COST,
          minCost: TEST_BCRYPT_COST,
          maxCost: 15,
        }),
        new AspNetIdentityPasswordVerifier('legacy_identity_v2'),
        new AspNetIdentityPasswordVerifier('legacy_identity_v3'),
      ],
      TEST_DUMMY_HASH,
    ),
    hasher: new BcryptPasswordHasher(TEST_BCRYPT_COST),
    policy: new Utf8PasswordPolicy(),
    invalidator: new FakeSessionInvalidator(repository),
    audit: new RecordingAuditWriter(),
    window: new SettableLegacyWindow(),
  };
}

export function createAuthService(
  repository: FakeAuthRepository,
  kernel: AuthKernel,
  config: AuthConfig = createTestConfig(),
): AuthService {
  const repo = repository as unknown as AuthRepository;
  return new AuthService(
    config,
    kernel.verification,
    kernel.hasher,
    kernel.policy,
    kernel.audit,
    kernel.invalidator,
    kernel.window,
    repo,
    new AuthRateLimitService(config, repo),
  );
}

/** Boots the real AppModule with the fake persistence and kernel doubles. */
export async function createTestApp(
  repository: FakeAuthRepository,
  kernel: AuthKernel,
  config: AuthConfig = createTestConfig(),
  configure: (builder: TestingModuleBuilder) => TestingModuleBuilder = (builder) => builder,
): Promise<NestFastifyApplication> {
  const moduleRef = await configure(Test.createTestingModule({ imports: [AppModule] }))
    .overrideProvider(AUTH_CONFIG)
    .useValue(config)
    .overrideProvider(AuthRepository)
    .useValue(repository)
    .overrideProvider(AUTH_PG_POOL)
    .useValue(repository.pool)
    .overrideProvider(PASSWORD_VERIFICATION_SERVICE)
    .useValue(kernel.verification)
    .overrideProvider(PASSWORD_HASHER)
    .useValue(kernel.hasher)
    .overrideProvider(PASSWORD_POLICY)
    .useValue(kernel.policy)
    .overrideProvider(LEGACY_PASSWORD_WINDOW)
    .useValue(kernel.window)
    .overrideProvider(IDENTITY_AUDIT_WRITER)
    .useValue(kernel.audit)
    .overrideProvider(SESSION_INVALIDATOR)
    .useValue(kernel.invalidator)
    .compile();
  const app = moduleRef.createNestApplication<NestFastifyApplication>(new FastifyAdapter(), {
    logger: false,
  });
  app.setGlobalPrefix('api/v1');
  await app.init();
  await app.getHttpAdapter().getInstance().ready();
  return app;
}

export function bcryptHash(password: string, cost = TEST_BCRYPT_COST): string {
  return bcrypt.hashSync(password, cost);
}

/** ASP.NET Identity V3 payload (PBKDF2-HMAC-SHA256) built for tests only. */
export function aspNetV3Hash(password: string, iterations = 1000): string {
  const salt = Buffer.alloc(16, 7);
  const subkey = pbkdf2Sync(Buffer.from(password, 'utf8'), salt, iterations, 32, 'sha256');
  const header = Buffer.alloc(13);
  header[0] = 0x01;
  header.writeUInt32BE(1, 1);
  header.writeUInt32BE(iterations, 5);
  header.writeUInt32BE(salt.length, 9);
  return Buffer.concat([header, salt, subkey]).toString('base64');
}

export function extractCookie(
  setCookie: string | string[] | undefined,
  name: string,
): string | undefined {
  const list = setCookie === undefined ? [] : Array.isArray(setCookie) ? setCookie : [setCookie];
  for (const entry of list) {
    const [pair] = entry.split(';');
    const index = pair?.indexOf('=') ?? -1;
    if (pair !== undefined && index > 0 && pair.slice(0, index) === name) {
      return decodeURIComponent(pair.slice(index + 1));
    }
  }
  return undefined;
}
