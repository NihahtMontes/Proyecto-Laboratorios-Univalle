import bcrypt from 'bcryptjs';
import { Inject, Injectable } from '@nestjs/common';
import { AUTH_CONFIG } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import { AuthRateLimitException, AuthUnauthorizedException } from './auth.exceptions.js';
import { AuthRateLimitService } from './auth.rate-limit.js';
import { AuthRepository } from './auth.repository.js';
import {
  generateCsrfToken,
  generateSessionToken,
  hashAuthIdentifier,
  hashSessionToken,
} from './auth.crypto.js';
import type { ActiveSiteSession, CsrfResponse } from './auth.types.js';
import {
  buildActiveSiteSession,
  parseCookies,
  validateLoginBody,
  validateSetActiveSiteBody,
} from './auth.types.js';

const BCRYPT_HASH_REGEX = /^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$/;

function isValidBcryptHash(value: string): boolean {
  return BCRYPT_HASH_REGEX.test(value);
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

@Injectable()
export class AuthService {
  constructor(
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
    private readonly repository: AuthRepository,
    private readonly rateLimit: AuthRateLimitService,
  ) {}

  generateCsrf(): CsrfResponse {
    return { csrfToken: generateCsrfToken() };
  }

  private identifierHashes(
    email: string,
    ip: string | undefined,
  ): {
    subjectHash: string;
    ipHash: string;
  } {
    return {
      subjectHash: hashAuthIdentifier(
        this.config.auditHmacKey,
        'subject',
        email.trim().toLowerCase(),
      ),
      ipHash: hashAuthIdentifier(this.config.auditHmacKey, 'ip', ip?.trim() || 'unknown'),
    };
  }

  private async rejectLogin(
    subjectHash: string,
    ipHash: string,
    userId: string | null,
  ): Promise<never> {
    await this.repository.recordSecurityEvent({
      eventType: 'login_failure',
      userId,
      subjectHash,
      ipHash,
      occurredAt: new Date(),
      metadata: {},
    });
    throw new AuthUnauthorizedException();
  }

  private async constantTimeBcryptCompare(password: string, hash: string): Promise<boolean> {
    const hashIsUsable =
      isValidBcryptHash(hash) && bcrypt.getRounds(hash) === this.config.bcryptCost;

    if (!hashIsUsable) {
      // Maintain similar timing but never accept against an invalid or
      // differently-costed hash, even if the password literally matches the dummy.
      try {
        await Promise.all([
          bcrypt.compare(password, this.config.dummyHash),
          sleep(this.config.loginFloorMs),
        ]);
      } catch {
        try {
          await bcrypt.compare(password, this.config.dummyHash);
        } catch {
          // Ignore secondary failure.
        }
      }
      return false;
    }

    try {
      const [result] = await Promise.all([
        bcrypt.compare(password, hash),
        sleep(this.config.loginFloorMs),
      ]);
      return result;
    } catch {
      // Keep timing similar even on unexpected bcrypt errors.
      try {
        await bcrypt.compare(password, this.config.dummyHash);
      } catch {
        // Ignore secondary failure.
      }
      return false;
    }
  }

  async login(
    body: unknown,
    ip: string | undefined,
  ): Promise<{ token: string; session: ActiveSiteSession; persistent: boolean }> {
    this.config.validate();
    const dto = validateLoginBody(body);
    const { subjectHash, ipHash } = this.identifierHashes(dto.email, ip);

    try {
      await this.rateLimit.consume(dto.email, ip);
    } catch (error) {
      if (error instanceof AuthRateLimitException) {
        throw error;
      }
      throw new AuthUnauthorizedException();
    }

    const user = await this.repository.findUserWithMembershipsByEmail(dto.email);
    const hashToCompare =
      user !== null && user.status === 'active' ? user.passwordHash : this.config.dummyHash;

    const passwordValid = await this.constantTimeBcryptCompare(dto.password, hashToCompare);

    if (!passwordValid || user === null || user.status !== 'active') {
      return this.rejectLogin(subjectHash, ipHash, user?.id ?? null);
    }

    const now = new Date();
    const memberships = await this.repository.findEligibleMemberships(user.id, now);
    let activeSiteId: string | null = null;

    if (dto.activeSiteId !== null) {
      const belongs = memberships.some((m) => m.siteId === dto.activeSiteId);
      if (!belongs) {
        return this.rejectLogin(subjectHash, ipHash, user.id);
      }
      activeSiteId = dto.activeSiteId;
    } else if (memberships.length === 0) {
      if (!user.isSuperAdmin) {
        return this.rejectLogin(subjectHash, ipHash, user.id);
      }
      activeSiteId = null;
    } else if (memberships.length === 1) {
      activeSiteId = memberships[0]!.siteId;
    }

    const token = generateSessionToken();
    const tokenHash = hashSessionToken(token);

    const session = await this.repository.createSession({
      user,
      activeSiteId,
      tokenHash,
      now,
      subjectHash,
      ipHash,
    });

    return {
      token,
      session: buildActiveSiteSession(user, memberships, session.activeSiteId),
      persistent: dto.rememberMe,
    };
  }

  async getSession(sessionCookieValue: string | undefined): Promise<ActiveSiteSession> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      throw new AuthUnauthorizedException();
    }
    const tokenHash = hashSessionToken(sessionCookieValue);
    const now = new Date();
    const { user, memberships, activeSiteId } = await this.repository.getSessionContext(
      tokenHash,
      now,
    );
    return buildActiveSiteSession(user, memberships, activeSiteId);
  }

  async setActiveSite(
    sessionCookieValue: string | undefined,
    body: unknown,
  ): Promise<ActiveSiteSession> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      throw new AuthUnauthorizedException();
    }
    const dto = validateSetActiveSiteBody(body);
    const tokenHash = hashSessionToken(sessionCookieValue);
    const now = new Date();

    const { user, memberships, activeSiteId } = await this.repository.setActiveSiteContext({
      tokenHash,
      activeSiteId: dto.activeSiteId,
      now,
    });

    return buildActiveSiteSession(user, memberships, activeSiteId);
  }

  async logout(sessionCookieValue: string | undefined): Promise<void> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      return;
    }
    const tokenHash = hashSessionToken(sessionCookieValue);
    await this.repository.revokeSession(tokenHash, 'logout');
  }
}

export { parseCookies };
