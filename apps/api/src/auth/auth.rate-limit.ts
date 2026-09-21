import { Inject, Injectable } from '@nestjs/common';
import { AUTH_CONFIG } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import { hashAuthIdentifier } from './auth.crypto.js';
import { AuthRateLimitException } from './auth.exceptions.js';
import { AuthRepository } from './auth.repository.js';

@Injectable()
export class AuthRateLimitService {
  constructor(
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
    private readonly repository: AuthRepository,
  ) {}

  async consume(email: string, ip: string | undefined, now = Date.now()): Promise<void> {
    this.config.validate();
    const normalizedEmail = email.trim().toLowerCase();
    const normalizedIp = ip?.trim() || 'unknown';
    const subjectHash = hashAuthIdentifier(this.config.auditHmacKey, 'subject', normalizedEmail);
    const ipHash = hashAuthIdentifier(this.config.auditHmacKey, 'ip', normalizedIp);
    const retryAfterSeconds = await this.repository.consumeLoginRateLimit({
      emailIpKeyHash: hashAuthIdentifier(
        this.config.auditHmacKey,
        'rate-email-ip',
        `${normalizedEmail}|${normalizedIp}`,
      ),
      ipKeyHash: hashAuthIdentifier(this.config.auditHmacKey, 'rate-ip', normalizedIp),
      subjectHash,
      ipHash,
      now: new Date(now),
      windowSeconds: this.config.rateLimitWindowSeconds,
      emailIpMaxAttempts: this.config.rateLimitMaxAttempts,
      ipMaxAttempts: this.config.rateLimitIpMaxAttempts,
    });
    if (retryAfterSeconds !== null) {
      throw new AuthRateLimitException(retryAfterSeconds);
    }
  }

  /** Test seam only; the production repository intentionally exposes no global reset. */
  reset(): void {
    const candidate = this.repository as AuthRepository & {
      resetRateLimitsForTests?: () => void;
    };
    candidate.resetRateLimitsForTests?.();
  }
}
