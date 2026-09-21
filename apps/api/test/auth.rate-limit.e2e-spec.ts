import 'reflect-metadata';
import { Test } from '@nestjs/testing';
import { AUTH_CONFIG } from '../src/auth/auth.constants.js';
import { AuthConfig } from '../src/auth/auth.config.js';
import { AuthRateLimitException } from '../src/auth/auth.exceptions.js';
import { AuthRateLimitService } from '../src/auth/auth.rate-limit.js';
import { AuthRepository } from '../src/auth/auth.repository.js';
import { FakeAuthRepository } from './auth.fakes.js';

describe('AuthRateLimitService distributed contract', () => {
  let service: AuthRateLimitService;
  let repository: FakeAuthRepository;

  beforeEach(async () => {
    repository = new FakeAuthRepository();
    const moduleRef = await Test.createTestingModule({
      providers: [
        AuthRateLimitService,
        { provide: AuthRepository, useValue: repository },
        {
          provide: AUTH_CONFIG,
          useValue: new AuthConfig({
            AUTH_ALLOWED_ORIGINS: 'http://localhost:3000',
            AUTH_LOGIN_FLOOR_MS: '0',
            AUTH_RATE_LIMIT_MAX_ATTEMPTS: '3',
            AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS: '6',
            AUTH_RATE_LIMIT_WINDOW_SECONDS: '60',
            AUTH_AUDIT_HMAC_KEY: 'test-only-auth-hmac-key-32-characters-long',
          }),
        },
      ],
    }).compile();
    service = moduleRef.get(AuthRateLimitService);
  });

  it('allows attempts through the configured email+IP limit', async () => {
    await expect(service.consume('a@b.co', '1.2.3.4')).resolves.toBeUndefined();
    await expect(service.consume('a@b.co', '1.2.3.4')).resolves.toBeUndefined();
    await expect(service.consume('a@b.co', '1.2.3.4')).resolves.toBeUndefined();
  });

  it('blocks the next attempt and durably records a hashed rate event', async () => {
    const now = 1_000_000;
    await service.consume('a@b.co', '1.2.3.4', now);
    await service.consume('a@b.co', '1.2.3.4', now);
    await service.consume('a@b.co', '1.2.3.4', now);

    await expect(service.consume('a@b.co', '1.2.3.4', now)).rejects.toMatchObject({
      name: 'AuthException',
      statusCode: 429,
      retryAfterSeconds: 60,
    });
    expect(repository.securityEvents).toHaveLength(1);
    expect(repository.securityEvents[0]?.eventType).toBe('login_rate_limited');
    expect(repository.securityEvents[0]?.subjectHash).toMatch(/^[0-9a-f]{64}$/);
    expect(repository.securityEvents[0]?.ipHash).toMatch(/^[0-9a-f]{64}$/);
  });

  it('keeps email+IP buckets separate while enforcing the aggregate IP bucket', async () => {
    const now = 2_000_000;
    for (let i = 0; i < 6; i += 1) {
      await service.consume(`user${i}@example.com`, '1.2.3.4', now);
    }
    await expect(service.consume('another@example.com', '1.2.3.4', now)).rejects.toBeInstanceOf(
      AuthRateLimitException,
    );
    await expect(service.consume('another@example.com', '5.6.7.8', now)).resolves.toBeUndefined();
  });

  it('normalizes email case into the same distributed bucket', async () => {
    const now = 3_000_000;
    await service.consume('A@B.CO', '1.2.3.4', now);
    await service.consume('a@b.co', '1.2.3.4', now);
    await service.consume('A@b.co', '1.2.3.4', now);
    await expect(service.consume('a@B.co', '1.2.3.4', now)).rejects.toBeInstanceOf(
      AuthRateLimitException,
    );
  });

  it('starts a fresh fixed window after reset_at', async () => {
    const now = 4_000_000;
    await service.consume('a@b.co', '1.2.3.4', now);
    await service.consume('a@b.co', '1.2.3.4', now);
    await service.consume('a@b.co', '1.2.3.4', now);
    await expect(service.consume('a@b.co', '1.2.3.4', now + 60_001)).resolves.toBeUndefined();
  });

  it('uses a keyed digest so raw email and IP never become repository keys', async () => {
    await service.consume('person@example.com', '203.0.113.9', 5_000_000);
    const serialized = JSON.stringify(repository.securityEvents);
    expect(serialized).not.toContain('person@example.com');
    expect(serialized).not.toContain('203.0.113.9');
  });
});
