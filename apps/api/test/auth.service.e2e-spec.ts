/**
 * MIG-001 F3 — session issuance planner (pure F1 §17 decision tree) and the
 * production DI wiring of the identity kernel. Login, session and password
 * flows are covered by auth.login-f3, auth.sessions-f3 and
 * auth.password-change.
 */
import { SITE_SELECTION_ABSOLUTE_TTL_SECONDS } from '../src/auth/auth.constants.js';
import {
  buildRequestIdentity,
  resolvePostPasswordChangePlan,
  resolveSessionIssuance,
} from '../src/auth/session/index.js';
import {
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  PASSWORD_VERIFICATION_SERVICE,
  SELF_SESSION_RENEWER,
  SESSION_INVALIDATOR,
} from '../src/identity/identity.contracts.js';
import { DefaultPasswordVerificationService } from '../src/identity/password/password-verification.service.js';
import { BcryptPasswordHasher } from '../src/identity/password/bcrypt.hasher.js';
import { Utf8PasswordPolicy } from '../src/identity/password/password-policy.js';
import { PgSessionInvalidator } from '../src/identity/session-invalidator.js';
import { PgIdentityAuditWriter } from '../src/identity/identity-audit.writer.js';
import { PgLegacyPasswordWindow } from '../src/identity/legacy-password-window.js';
import { SelfSessionRenewerImpl } from '../src/auth/auth.service.js';
import { createIdentity, createMembership, createSite, FakeAuthRepository } from './auth.fakes.js';
import { createAuthTestApp } from './auth.test-helpers.js';

const NOW = new Date('2026-09-25T12:00:00Z');
const TTLS = { idleTtlSeconds: 1800, absoluteTtlSeconds: 43200 };
const SITE_A = createSite({ id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa' });
const SITE_B = createSite({ id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb' });
const user = createIdentity();
const a = createMembership(SITE_A, { userId: user.id });
const b = createMembership(SITE_B, { userId: user.id, role: 'Supervisor' });

function plan(overrides: Partial<Parameters<typeof resolveSessionIssuance>[0]> = {}) {
  return resolveSessionIssuance({
    identity: user,
    memberships: [a],
    requestedActiveSiteId: null,
    now: NOW,
    ...TTLS,
    ...overrides,
  });
}

describe('resolveSessionIssuance', () => {
  it('never issues a session for reset_required', () => {
    expect(
      plan({ identity: { ...user, passwordScheme: 'reset_required', passwordHash: null } }),
    ).toBeNull();
  });

  it('forces password_change for must_change_password or an over-72-byte legacy proof', () => {
    expect(plan({ identity: { ...user, mustChangePassword: true } })).toMatchObject({
      purpose: 'password_change',
      activeSiteId: null,
    });
    expect(plan({ forcePasswordChange: true, memberships: [a, b] })).toMatchObject({
      purpose: 'password_change',
      activeSiteId: null,
    });
  });

  it('denies zero memberships unless SuperAdmin (normal null-site)', () => {
    expect(plan({ memberships: [] })).toBeNull();
    expect(plan({ memberships: [], identity: { ...user, isSuperAdmin: true } })).toMatchObject({
      purpose: 'normal',
      activeSiteId: null,
    });
  });

  it('auto-selects one membership with idle expiry capped by the absolute expiry', () => {
    const result = plan();
    expect(result).toMatchObject({ purpose: 'normal', activeSiteId: SITE_A.id });
    expect(result!.idleExpiresAt.getTime()).toBe(NOW.getTime() + 1800 * 1000);
    expect(result!.absoluteExpiresAt.getTime()).toBe(NOW.getTime() + 43200 * 1000);
    const capped = plan({ idleTtlSeconds: 90000 });
    expect(capped!.idleExpiresAt).toEqual(capped!.absoluteExpiresAt);
  });

  it('returns a 15-minute absolute site_selection plan without idle extension for several memberships', () => {
    const result = plan({ memberships: [a, b] });
    expect(result).toEqual({
      purpose: 'site_selection',
      activeSiteId: null,
      idleExpiresAt: new Date(NOW.getTime() + SITE_SELECTION_ABSOLUTE_TTL_SECONDS * 1000),
      absoluteExpiresAt: new Date(NOW.getTime() + SITE_SELECTION_ABSOLUTE_TTL_SECONDS * 1000),
    });
    expect(SITE_SELECTION_ABSOLUTE_TTL_SECONDS).toBe(15 * 60);
  });

  it('honors only an eligible requested site', () => {
    expect(plan({ memberships: [a, b], requestedActiveSiteId: SITE_B.id })).toMatchObject({
      purpose: 'normal',
      activeSiteId: SITE_B.id,
    });
    expect(plan({ memberships: [a], requestedActiveSiteId: SITE_B.id })).toBeNull();
  });
});

describe('resolvePostPasswordChangePlan', () => {
  it('keeps an eligible active site of a normal session', () => {
    const result = resolvePostPasswordChangePlan({
      identity: user,
      memberships: [a, b],
      previousSession: { purpose: 'normal', activeSiteId: SITE_B.id },
      now: NOW,
      ...TTLS,
    });
    expect(result).toMatchObject({ purpose: 'normal', activeSiteId: SITE_B.id });
  });

  it('keeps a SuperAdmin global session global', () => {
    const result = resolvePostPasswordChangePlan({
      identity: { ...user, isSuperAdmin: true },
      memberships: [a, b],
      previousSession: { purpose: 'normal', activeSiteId: null },
      now: NOW,
      ...TTLS,
    });
    expect(result).toMatchObject({ purpose: 'normal', activeSiteId: null });
  });

  it('re-runs the initial decision after a password_change session or a lost site', () => {
    expect(
      resolvePostPasswordChangePlan({
        identity: user,
        memberships: [a, b],
        previousSession: { purpose: 'password_change', activeSiteId: null },
        now: NOW,
        ...TTLS,
      })?.purpose,
    ).toBe('site_selection');
    expect(
      resolvePostPasswordChangePlan({
        identity: user,
        memberships: [a],
        previousSession: { purpose: 'normal', activeSiteId: SITE_B.id },
        now: NOW,
        ...TTLS,
      }),
    ).toMatchObject({ purpose: 'normal', activeSiteId: SITE_A.id });
  });
});

describe('buildRequestIdentity', () => {
  it('derives the active-site role and nulls it without a site', () => {
    expect(
      buildRequestIdentity({
        correlationId: 'c',
        sessionId: 's',
        identity: user,
        activeSiteId: SITE_B.id,
        memberships: [a, b],
      }).activeSiteRole,
    ).toBe('Supervisor');
    expect(
      buildRequestIdentity({
        correlationId: 'c',
        sessionId: 's',
        identity: user,
        activeSiteId: null,
        memberships: [a],
      }).activeSiteRole,
    ).toBeNull();
  });
});

describe('AuthModule production wiring', () => {
  it('resolves every identity-kernel token to the real implementations', async () => {
    const app = await createAuthTestApp({ repository: new FakeAuthRepository() });
    try {
      expect(app.get(PASSWORD_VERIFICATION_SERVICE)).toBeInstanceOf(
        DefaultPasswordVerificationService,
      );
      expect(app.get(PASSWORD_HASHER)).toBeInstanceOf(BcryptPasswordHasher);
      expect(app.get(PASSWORD_POLICY)).toBeInstanceOf(Utf8PasswordPolicy);
      expect(app.get(SESSION_INVALIDATOR)).toBeInstanceOf(PgSessionInvalidator);
      expect(app.get(IDENTITY_AUDIT_WRITER)).toBeInstanceOf(PgIdentityAuditWriter);
      expect(app.get(LEGACY_PASSWORD_WINDOW)).toBeInstanceOf(PgLegacyPasswordWindow);
      expect(app.get(SELF_SESSION_RENEWER)).toBeInstanceOf(SelfSessionRenewerImpl);
    } finally {
      await app.close();
    }
  });
});
