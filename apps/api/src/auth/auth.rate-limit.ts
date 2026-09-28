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

  /**
   * Login rate limit keyed by the NORMALIZED login identifier (username OR
   * email) plus the socket IP. The transport-bound identifier is reduced to
   * its NFKC + locale-independent lowercase canonical form so case-only or
   * unicode-normalization variations collide on the same bucket.
   */
  async consume(identifier: string, ip: string | undefined, now = Date.now()): Promise<void> {
    this.config.validate();
    const normalizedIdentifier = identifier.trim().normalize('NFKC').toLowerCase();
    const normalizedIp = ip?.trim() || 'unknown';
    const subjectHash = hashAuthIdentifier(
      this.config.auditHmacKey,
      'subject',
      normalizedIdentifier,
    );
    const ipHash = hashAuthIdentifier(this.config.auditHmacKey, 'ip', normalizedIp);
    const retryAfterSeconds = await this.repository.consumeLoginRateLimit({
      identifierIpKeyHash: hashAuthIdentifier(
        this.config.auditHmacKey,
        'rate-identifier-ip',
        `${normalizedIdentifier}|${normalizedIp}`,
      ),
      ipKeyHash: hashAuthIdentifier(this.config.auditHmacKey, 'rate-ip', normalizedIp),
      subjectHash,
      ipHash,
      now: new Date(now),
      windowSeconds: this.config.rateLimitWindowSeconds,
      identifierIpMaxAttempts: this.config.rateLimitMaxAttempts,
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
