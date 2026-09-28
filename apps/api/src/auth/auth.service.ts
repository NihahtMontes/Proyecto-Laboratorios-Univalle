import { Inject, Injectable } from '@nestjs/common';
import type { CsrfResponse } from '@lu/contracts';
import { AUTH_CONFIG, SESSION_COOKIE } from './auth.constants.js';
import { AuthConfig } from './auth.config.js';
import {
  AuthForbiddenException,
  AuthRateLimitException,
  AuthUnauthorizedException,
  AuthValidationException,
} from './auth.exceptions.js';
import { AuthRateLimitService } from './auth.rate-limit.js';
import { AuthRepository } from './auth.repository.js';
import {
  generateCsrfToken,
  generateSessionToken,
  hashAuthIdentifier,
  hashSessionToken,
  serializeSessionCookie,
} from './auth.crypto.js';
import {
  resolvePostPasswordChangePlan,
  resolveSessionIssuance,
  type SessionIssuancePlan,
} from './session/index.js';
import type {
  AuthSessionRecord,
  AuthSessionResponse,
  IdentityUser,
  LoginDto,
  ResolvedSession,
} from './auth.types.js';
import {
  buildAuthSessionResponse,
  validateLoginBody,
  validatePasswordChangeBody,
  validateSetActiveSiteBody,
} from './auth.types.js';
import {
  BCRYPT_PASSWORD_MAX_BYTES,
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
  LOGIN_PASSWORD_MAX_BYTES,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  PASSWORD_VERIFICATION_SERVICE,
  SESSION_INVALIDATOR,
  type IdentityAuditWriter,
  type LegacyPasswordWindow,
  type MembershipRole,
  type PasswordHasher,
  type PasswordPolicy,
  type PasswordScheme,
  type PasswordVerificationResult,
  type PasswordVerificationService,
  type RequestIdentity,
  type SelfSessionRenewer,
  type SessionInvalidator,
} from '../identity/identity.contracts.js';

type LoginFailureReason =
  | 'absent_user'
  | 'inactive_account'
  | 'reset_required'
  | 'closed_window'
  | 'wrong_password'
  | 'verify_exception'
  | 'rehash_conflict'
  | 'no_eligible_site'
  | 'bad_active_site';

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function isLegacyScheme(scheme: PasswordScheme): boolean {
  return scheme === 'legacy_identity_v2' || scheme === 'legacy_identity_v3';
}

function utf8Length(value: string): number {
  return Buffer.byteLength(value, 'utf8');
}

export interface LoginResult {
  readonly token: string;
  readonly response: AuthSessionResponse;
  readonly persistent: boolean;
  readonly activeSiteRole: MembershipRole | null;
}

/**
 * Application service for authentication and session lifecycle (F1 §6, §7,
 * §12, §17). Password algorithms, hashing, policy, rotation, audit and the
 * legacy window are injected identity-kernel abstractions; persistence goes
 * through AuthRepository. Controllers only translate HTTP.
 */
@Injectable()
export class AuthService {
  constructor(
    @Inject(AUTH_CONFIG) private readonly config: AuthConfig,
    @Inject(PASSWORD_VERIFICATION_SERVICE)
    private readonly verifier: PasswordVerificationService,
    @Inject(PASSWORD_HASHER) private readonly hasher: PasswordHasher,
    @Inject(PASSWORD_POLICY) private readonly passwordPolicy: PasswordPolicy,
    @Inject(IDENTITY_AUDIT_WRITER) private readonly identityAudit: IdentityAuditWriter,
    @Inject(SESSION_INVALIDATOR) private readonly sessionInvalidator: SessionInvalidator,
    @Inject(LEGACY_PASSWORD_WINDOW) private readonly legacyWindow: LegacyPasswordWindow,
    private readonly repository: AuthRepository,
    private readonly rateLimit: AuthRateLimitService,
  ) {}

  generateCsrf(): CsrfResponse {
    return { csrfToken: generateCsrfToken() };
  }

  /** Cookie for a freshly issued session token. */
  sessionCookie(token: string, persistent: boolean): string {
    return serializeSessionCookie(
      SESSION_COOKIE,
      token,
      persistent ? this.config.absoluteTtlSeconds : undefined,
    );
  }

  // ---------------------------------------------------------------------------
  // Login
  // ---------------------------------------------------------------------------

  async login(body: unknown, ip: string | undefined): Promise<LoginResult> {
    this.config.validate();
    const dto = validateLoginBody(body, { loginPasswordMaxBytes: LOGIN_PASSWORD_MAX_BYTES });
    const now = new Date();
    const subjectHash = hashAuthIdentifier(
      this.config.auditHmacKey,
      'subject',
      dto.loginIdentifier,
    );
    const ipHash = hashAuthIdentifier(this.config.auditHmacKey, 'ip', ip?.trim() || 'unknown');

    try {
      await this.rateLimit.consume(dto.loginIdentifier, ip, now.getTime());
    } catch (error) {
      if (error instanceof AuthRateLimitException) {
        throw error;
      }
      throw new AuthUnauthorizedException();
    }

    // Every outcome past the rate limit (success or any generic failure) takes
    // at least the configured floor, as in the pre-F3 login path.
    const floor = sleep(this.config.loginFloorMs);
    try {
      return await this.authenticate(dto, now, subjectHash, ipHash);
    } finally {
      await floor;
    }
  }

  private async authenticate(
    dto: LoginDto,
    now: Date,
    subjectHash: string,
    ipHash: string,
  ): Promise<LoginResult> {
    const fail = async (
      identity: IdentityUser | null,
      reason: LoginFailureReason,
      spendVerificationTime: boolean,
    ): Promise<never> => {
      if (spendVerificationTime) {
        await this.verifier.dummyVerify(dto.password);
      }
      await this.repository.recordSecurityEvent({
        eventType: 'login_failure',
        userId: identity?.id ?? null,
        subjectHash,
        ipHash,
        occurredAt: now,
        metadata: { reason },
      });
      throw new AuthUnauthorizedException();
    };

    let identity: IdentityUser | null;
    try {
      identity = await this.repository.findIdentityByLoginIdentifier(dto.loginIdentifier);
    } catch {
      await this.verifier.dummyVerify(dto.password);
      throw new AuthUnauthorizedException();
    }
    if (identity === null) {
      return fail(null, 'absent_user', true);
    }
    if (identity.accountStatus !== 'active') {
      return fail(identity, 'inactive_account', true);
    }
    if (identity.passwordScheme === 'reset_required' || identity.passwordHash === null) {
      return fail(identity, 'reset_required', true);
    }
    const legacy = isLegacyScheme(identity.passwordScheme);
    if (legacy && !(await this.legacyWindow.isOpen(this.repository.getPool(), now))) {
      await this.identityAudit.append(this.repository.getPool(), {
        action: 'legacy_password_rejected',
        subjectUserId: identity.id,
        actorUserId: null,
        reason: 'legacy_password_deadline_reached',
        metadata: { scheme: identity.passwordScheme },
      });
      return fail(identity, 'closed_window', true);
    }

    let verification: PasswordVerificationResult;
    try {
      const verifying = this.verifier.verify(
        identity.passwordScheme,
        identity.passwordHash,
        dto.password,
      );
      // PBKDF2 (1,000..100,000 iterations) is much cheaper than bcrypt; pairing
      // a legacy verification with the dummy bcrypt comparison keeps a legacy
      // account's response time indistinguishable from bcrypt/absent accounts.
      verification = legacy
        ? (await Promise.all([verifying, this.verifier.dummyVerify(dto.password)]))[0]
        : await verifying;
    } catch {
      return fail(identity, 'verify_exception', false);
    }
    if (!verification.verified) {
      return fail(identity, 'wrong_password', false);
    }

    // F1 §7: a verified legacy password over 72 bytes is never stored as bcrypt
    // (bcrypt would truncate it); it yields only a password_change session.
    const exceedsBcryptLimit = legacy && utf8Length(dto.password) > BCRYPT_PASSWORD_MAX_BYTES;
    let current = identity;
    if (verification.needsRehash && !exceedsBcryptLimit) {
      try {
        current = await this.rehashAfterVerification(identity, dto.password, now);
      } catch {
        return fail(identity, 'rehash_conflict', false);
      }
    }

    const memberships = await this.repository.findEligibleMemberships(current.id, now);
    const plan = resolveSessionIssuance({
      identity: current,
      memberships,
      requestedActiveSiteId: dto.activeSiteId,
      forcePasswordChange: exceedsBcryptLimit,
      idleTtlSeconds: this.config.idleTtlSeconds,
      absoluteTtlSeconds: this.config.absoluteTtlSeconds,
      now,
    });
    if (plan === null) {
      return fail(
        current,
        dto.activeSiteId === null ? 'no_eligible_site' : 'bad_active_site',
        false,
      );
    }

    const issued = await this.issueSession(current, plan, now);
    await this.repository.recordLoginSuccess({
      user: current,
      session: issued.session,
      now,
      subjectHash,
      ipHash,
    });
    return {
      token: issued.token,
      response: buildAuthSessionResponse(current, memberships, plan.purpose, plan.activeSiteId),
      persistent: dto.rememberMe,
      activeSiteRole:
        plan.activeSiteId === null
          ? null
          : (memberships.find((m) => m.siteId === plan.activeSiteId)?.role ?? null),
    };
  }

  /**
   * Rehash-on-login (F1 §7, §12), only after a successful verification and
   * before any session is issued, in one transaction: compare-and-swap on the
   * verified (scheme, hash), current-cost bcrypt write, security_version
   * rotation with revocation of every existing session, and an audit event.
   * A legacy rehash stores bcrypt with must_change_password=false and stamps
   * password_migrated_at; a bcrypt cost rehash replaces only the hash and keeps
   * must_change_password/password_migrated_at untouched.
   */
  private async rehashAfterVerification(
    identity: IdentityUser,
    password: string,
    now: Date,
  ): Promise<IdentityUser> {
    const expectedHash = identity.passwordHash;
    if (expectedHash === null) {
      throw new AuthUnauthorizedException();
    }
    const newHash = await this.hasher.hash(password);
    const fromScheme = identity.passwordScheme;
    return this.repository.inTransaction(async (client) => {
      if (fromScheme === 'bcrypt') {
        await this.repository.rehashUserPasswordBcryptCost({
          client,
          userId: identity.id,
          expectedHash,
          newHash,
        });
      } else {
        await this.repository.rehashUserPassword({
          client,
          userId: identity.id,
          expectedScheme: fromScheme,
          expectedHash,
          newHash,
          previousSchemeForMigrationFlag: fromScheme,
          now,
        });
      }
      const securityVersion = await this.sessionInvalidator.rotateAndRevokeAll(
        client,
        identity.id,
        'credential_rehash',
      );
      await this.identityAudit.append(client, {
        action: 'credential_rehashed',
        subjectUserId: identity.id,
        actorUserId: null,
        reason: 'rehash_on_login',
        metadata: { from_scheme: fromScheme, to_scheme: 'bcrypt' },
      });
      return {
        ...identity,
        passwordHash: newHash,
        passwordScheme: 'bcrypt',
        mustChangePassword: fromScheme === 'bcrypt' ? identity.mustChangePassword : false,
        securityVersion,
      };
    });
  }

  private async issueSession(
    identity: IdentityUser,
    plan: SessionIssuancePlan,
    now: Date,
  ): Promise<{ readonly token: string; readonly session: AuthSessionRecord }> {
    const token = generateSessionToken();
    const session = await this.repository.insertSession({
      userId: identity.id,
      activeSiteId: plan.activeSiteId,
      purpose: plan.purpose,
      securityVersion: identity.securityVersion,
      tokenHash: hashSessionToken(token),
      createdAt: now,
      idleExpiresAt: plan.idleExpiresAt,
      absoluteExpiresAt: plan.absoluteExpiresAt,
    });
    return { token, session };
  }

  // ---------------------------------------------------------------------------
  // Session inspection and site selection
  // ---------------------------------------------------------------------------

  /** Validates the presented cookie and returns the server-side session view. */
  async resolveSession(sessionCookieValue: string | undefined): Promise<ResolvedSession> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      throw new AuthUnauthorizedException();
    }
    const ctx = await this.repository.getSessionContext(
      hashSessionToken(sessionCookieValue),
      new Date(),
    );
    return {
      sessionId: ctx.session.id,
      purpose: ctx.session.purpose,
      identity: ctx.identity,
      eligibleMemberships: ctx.memberships,
      activeSiteId: ctx.session.purpose === 'normal' ? ctx.activeSiteId : null,
    };
  }

  async getSession(sessionCookieValue: string | undefined): Promise<AuthSessionResponse> {
    const resolved = await this.resolveSession(sessionCookieValue);
    return buildAuthSessionResponse(
      resolved.identity,
      resolved.eligibleMemberships,
      resolved.purpose,
      resolved.activeSiteId,
    );
  }

  /**
   * CSRF-protected active-site selection (F1 §17). A normal session updates its
   * own active site; a site_selection session is revoked and replaced by a
   * normal session for the chosen eligible site (`token` is then non-null).
   * Neither path rotates security_version or touches other sessions.
   */
  async setActiveSite(
    sessionCookieValue: string | undefined,
    body: unknown,
  ): Promise<{ readonly response: AuthSessionResponse; readonly token: string | null }> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      throw new AuthUnauthorizedException();
    }
    const dto = validateSetActiveSiteBody(body);
    const now = new Date();
    const ctx = await this.repository.setActiveSiteContext({
      tokenHash: hashSessionToken(sessionCookieValue),
      activeSiteId: dto.activeSiteId,
      now,
    });

    let token: string | null = null;
    if (ctx.session.purpose === 'site_selection') {
      const plan = resolveSessionIssuance({
        identity: ctx.identity,
        memberships: ctx.memberships,
        requestedActiveSiteId: dto.activeSiteId,
        idleTtlSeconds: this.config.idleTtlSeconds,
        absoluteTtlSeconds: this.config.absoluteTtlSeconds,
        now,
      });
      if (plan === null || plan.purpose !== 'normal') {
        throw new AuthForbiddenException('SITE_ACCESS_DENIED');
      }
      token = (await this.issueSession(ctx.identity, plan, now)).token;
    }
    await this.identityAudit.append(this.repository.getPool(), {
      action: 'active_site_changed',
      subjectUserId: ctx.identity.id,
      actorUserId: ctx.identity.id,
      siteId: dto.activeSiteId,
      metadata: {
        mode: ctx.session.purpose === 'site_selection' ? 'site_selection' : 'normal_session',
      },
    });
    return {
      response: buildAuthSessionResponse(ctx.identity, ctx.memberships, 'normal', dto.activeSiteId),
      token,
    };
  }

  // ---------------------------------------------------------------------------
  // Self password change
  // ---------------------------------------------------------------------------

  /**
   * POST /auth/password (F1 §7, §12). Allowed for normal and password_change
   * sessions. The current password is verified with the same strategies as
   * login (legacy only while the window is open); the new one must satisfy the
   * password policy. The hash write, security_version rotation (revoking every
   * session, including the current one) and audit commit together; the
   * replacement session is issued after commit.
   *
   * F8 hardening: a dedicated rate-limit bucket keyed by
   * `password-change:<userId>` + ip is consumed through the existing
   * `AuthRateLimitService` BEFORE the current password is verified, so a
   * brute-force attack on a stolen session token is bounded even when the
   * attacker has the right session cookie. The session is first resolved with
   * a read-only lookup so a missing / revoked / idle-expired / absolute-expired
   * session fails 401 without consuming the bucket. The bucket's own
   * transaction commits independently of the change transaction below, so a
   * failed current-password verification still counts. The change transaction
   * (FOR UPDATE, security_version checks, legacy window, audit, session
   * replacement) is byte-for-byte the same as before F8.
   */
  async changePassword(
    sessionCookieValue: string | undefined,
    body: unknown,
    ip: string | undefined,
  ): Promise<{ readonly response: AuthSessionResponse; readonly token: string }> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      throw new AuthUnauthorizedException();
    }
    const dto = validatePasswordChangeBody(body);
    const tokenHash = hashSessionToken(sessionCookieValue);
    const now = new Date();

    const initial = await this.repository.findSessionByTokenHash(tokenHash, now);
    if (initial === null) {
      throw new AuthUnauthorizedException();
    }
    const { identity: initialIdentity, session: initialSession } = initial;
    // A stale session (inactive account, rotated security_version) or a
    // site_selection session must not consume the subject's bucket: otherwise
    // an invalidated token could lock the owner out of changing the password.
    if (
      initialIdentity.accountStatus !== 'active' ||
      initialIdentity.securityVersion !== initialSession.securityVersion
    ) {
      throw new AuthUnauthorizedException();
    }
    if (initialSession.purpose === 'site_selection') {
      throw new AuthForbiddenException('SITE_ACCESS_DENIED');
    }

    try {
      await this.rateLimit.consume(`password-change:${initialIdentity.id}`, ip, now.getTime());
    } catch (error) {
      if (error instanceof AuthRateLimitException) {
        throw error;
      }
      throw new AuthUnauthorizedException();
    }

    const outcome = await this.repository.inTransaction(async (client) => {
      const found = await this.repository.findSessionByTokenHashForUpdate(client, tokenHash, now);
      if (found === null) {
        throw new AuthUnauthorizedException();
      }
      const { identity, session } = found;
      if (
        identity.accountStatus !== 'active' ||
        identity.securityVersion !== session.securityVersion
      ) {
        throw new AuthUnauthorizedException();
      }
      if (session.purpose === 'site_selection') {
        throw new AuthForbiddenException('SITE_ACCESS_DENIED');
      }
      const currentHash = identity.passwordHash;
      if (identity.passwordScheme === 'reset_required' || currentHash === null) {
        throw new AuthUnauthorizedException();
      }
      if (
        isLegacyScheme(identity.passwordScheme) &&
        !(await this.legacyWindow.isOpen(client, now))
      ) {
        await this.verifier.dummyVerify(dto.currentPassword);
        return { kind: 'legacy_window_closed' as const, identity };
      }
      const verification = await this.verifier.verify(
        identity.passwordScheme,
        currentHash,
        dto.currentPassword,
      );
      if (!verification.verified) {
        throw new AuthUnauthorizedException();
      }
      const violations = this.passwordPolicy.validate(dto.newPassword);
      if (violations.length > 0) {
        throw new AuthValidationException('New password violates the password policy.', {
          newPassword: [...violations],
        });
      }
      const newHash = await this.hasher.hash(dto.newPassword);
      await this.repository.changePasswordHash({
        client,
        userId: identity.id,
        expectedHash: currentHash,
        expectedScheme: identity.passwordScheme,
        newHash,
        migrateAt: now,
      });
      const securityVersion = await this.sessionInvalidator.rotateAndRevokeAll(
        client,
        identity.id,
        'self_password_change',
      );
      await this.identityAudit.append(client, {
        action: 'password_changed',
        subjectUserId: identity.id,
        actorUserId: identity.id,
        reason: 'self_password_change',
        metadata: { from_scheme: identity.passwordScheme, session_purpose: session.purpose },
      });
      const updated: IdentityUser = {
        ...identity,
        passwordScheme: 'bcrypt',
        passwordHash: newHash,
        mustChangePassword: false,
        securityVersion,
      };
      return {
        kind: 'changed' as const,
        identity: updated,
        previous: { purpose: session.purpose, activeSiteId: session.activeSiteId },
      };
    });

    if (outcome.kind === 'legacy_window_closed') {
      await this.identityAudit.append(this.repository.getPool(), {
        action: 'legacy_password_rejected',
        subjectUserId: outcome.identity.id,
        actorUserId: outcome.identity.id,
        reason: 'legacy_password_deadline_reached',
        metadata: { scheme: outcome.identity.passwordScheme },
      });
      throw new AuthUnauthorizedException();
    }

    const memberships = await this.repository.findEligibleMemberships(outcome.identity.id, now);
    const plan = resolvePostPasswordChangePlan({
      identity: outcome.identity,
      memberships,
      previousSession: outcome.previous,
      idleTtlSeconds: this.config.idleTtlSeconds,
      absoluteTtlSeconds: this.config.absoluteTtlSeconds,
      now,
    });
    if (plan === null) {
      // The credential change committed; the account simply has no eligible
      // site for a new session (non-SuperAdmin without memberships).
      throw new AuthUnauthorizedException();
    }
    const issued = await this.issueSession(outcome.identity, plan, now);
    return {
      response: buildAuthSessionResponse(
        outcome.identity,
        memberships,
        plan.purpose,
        plan.activeSiteId,
      ),
      token: issued.token,
    };
  }

  // ---------------------------------------------------------------------------
  // Replacement after a self-sensitive change made by another slice
  // ---------------------------------------------------------------------------

  /**
   * Issues the replacement session after a committed self-sensitive change
   * (e.g. self email change) whose transaction already rotated
   * security_version and revoked every session. Keeps the previous active site
   * while eligible and writes the session cookie on the reply.
   */
  async renewAfterSelfChange(
    reply: { header(name: string, value: string | number): unknown },
    identity: RequestIdentity,
  ): Promise<void> {
    this.config.validate();
    const now = new Date();
    const user = await this.repository.findIdentityById(identity.userId);
    if (user === null || user.accountStatus !== 'active') {
      throw new AuthUnauthorizedException();
    }
    const memberships = await this.repository.findEligibleMemberships(user.id, now);
    const plan = resolvePostPasswordChangePlan({
      identity: user,
      memberships,
      previousSession: { purpose: 'normal', activeSiteId: identity.activeSiteId },
      idleTtlSeconds: this.config.idleTtlSeconds,
      absoluteTtlSeconds: this.config.absoluteTtlSeconds,
      now,
    });
    if (plan === null) {
      throw new AuthUnauthorizedException();
    }
    const issued = await this.issueSession(user, plan, now);
    reply.header('Set-Cookie', this.sessionCookie(issued.token, false));
  }

  async logout(sessionCookieValue: string | undefined): Promise<void> {
    this.config.validate();
    if (sessionCookieValue === undefined) {
      return;
    }
    await this.repository.revokeSession(hashSessionToken(sessionCookieValue), 'logout');
  }
}

@Injectable()
export class SelfSessionRenewerImpl implements SelfSessionRenewer {
  constructor(private readonly auth: AuthService) {}

  async renew(
    reply: { header(name: string, value: string | number): unknown },
    identity: RequestIdentity,
  ): Promise<void> {
    await this.auth.renewAfterSelfChange(reply, identity);
  }
}
