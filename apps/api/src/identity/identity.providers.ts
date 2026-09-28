/**
 * NestJS DI wiring for the identity kernel.
 *
 * The auth slice (`apps/api/src/auth`) spreads these into its
 * `AuthModule.forRoot(...)` / `imports` via `IDENTITY_PROVIDERS` and
 * re-exports the kernel interfaces through `IDENTITY_EXPORTS`. The
 * providers module here never imports `AuthModule` directly (this
 * worker does not edit `auth.module.ts`).
 *
 * Cost and dummy-hash come from the existing `AUTH_CONFIG` provider
 * (token in `apps/api/src/auth/auth.constants.ts`; class
 * `AuthConfig` in `auth.config.ts`), so the kernel inherits the same
 * env-driven `AUTH_BCRYPT_COST` / `AUTH_BCRYPT_DUMMY_HASH` semantics
 * the auth slice already enforces (validated range 4..15 in test seam,
 * 10..15 in production). The verifier floor follows the same split.
 *
 * The `PASSWORD_VERIFIERS` multi-provider token is intentionally NOT
 * exported (it is a construction-time list, not a consumer-facing
 * service); every other kernel token is exported so the auth/users
 * slices can inject the dispatcher, hasher, policy, invalidator,
 * audit writer and legacy window.
 */
import type { Provider } from '@nestjs/common';
import { AUTH_CONFIG } from '../auth/auth.constants.js';
import type { AuthConfig } from '../auth/auth.config.js';
import type {
  IdentityAuditWriter,
  LegacyPasswordWindow,
  PasswordHasher,
  PasswordPolicy,
  PasswordVerificationService,
  PasswordVerifier,
  SessionInvalidator,
} from './identity.contracts.js';
import {
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  PASSWORD_VERIFIERS,
  PASSWORD_VERIFICATION_SERVICE,
  SESSION_INVALIDATOR,
} from './identity.contracts.js';
import { AspNetIdentityPasswordVerifier } from './password/aspnet-identity.verifier.js';
import { BcryptPasswordVerifier } from './password/bcrypt.verifier.js';
import { BcryptPasswordHasher } from './password/bcrypt.hasher.js';
import { Utf8PasswordPolicy } from './password/password-policy.js';
import { DefaultPasswordVerificationService } from './password/password-verification.service.js';
import { PgIdentityAuditWriter } from './identity-audit.writer.js';
import { PgLegacyPasswordWindow } from './legacy-password-window.js';
import { PgSessionInvalidator } from './session-invalidator.js';

/**
 * F1 §7 production bcrypt cost range is 10..15. A lower floor applies only when
 * the configured current cost is itself below 10, which AuthConfig permits
 * exclusively in its test seam; production configuration therefore never
 * accepts a stored hash below cost 10.
 */
const BCRYPT_PRODUCTION_MIN_COST = 10;
const BCRYPT_VERIFIER_COST_CEILING = 15;

export function bcryptVerifierMinCost(currentCost: number): number {
  return Math.min(BCRYPT_PRODUCTION_MIN_COST, currentCost);
}

function isAuthConfig(value: unknown): value is AuthConfig {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as { bcryptCost?: unknown }).bcryptCost === 'number' &&
    typeof (value as { dummyHash?: unknown }).dummyHash === 'string'
  );
}

export const IDENTITY_PROVIDERS: Provider[] = [
  {
    provide: PASSWORD_VERIFIERS,
    inject: [AUTH_CONFIG],
    useFactory: (config: unknown): PasswordVerifier[] => {
      if (!isAuthConfig(config)) {
        throw new Error('IDENTITY_PROVIDERS: AUTH_CONFIG must be an AuthConfig instance.');
      }
      return [
        new BcryptPasswordVerifier({
          currentCost: config.bcryptCost,
          minCost: bcryptVerifierMinCost(config.bcryptCost),
          maxCost: BCRYPT_VERIFIER_COST_CEILING,
        }),
        new AspNetIdentityPasswordVerifier('legacy_identity_v2'),
        new AspNetIdentityPasswordVerifier('legacy_identity_v3'),
      ];
    },
  },
  {
    provide: PASSWORD_VERIFICATION_SERVICE,
    inject: [PASSWORD_VERIFIERS, AUTH_CONFIG],
    useFactory: (
      verifiers: readonly PasswordVerifier[],
      config: unknown,
    ): PasswordVerificationService => {
      if (!isAuthConfig(config)) {
        throw new Error('IDENTITY_PROVIDERS: AUTH_CONFIG must be an AuthConfig instance.');
      }
      return new DefaultPasswordVerificationService(verifiers, config.dummyHash);
    },
  },
  {
    provide: PASSWORD_HASHER,
    inject: [AUTH_CONFIG],
    useFactory: (config: unknown): PasswordHasher => {
      if (!isAuthConfig(config)) {
        throw new Error('IDENTITY_PROVIDERS: AUTH_CONFIG must be an AuthConfig instance.');
      }
      return new BcryptPasswordHasher(config.bcryptCost);
    },
  },
  {
    provide: PASSWORD_POLICY,
    useFactory: (): PasswordPolicy => new Utf8PasswordPolicy(),
  },
  {
    provide: SESSION_INVALIDATOR,
    useFactory: (): SessionInvalidator => new PgSessionInvalidator(),
  },
  {
    provide: IDENTITY_AUDIT_WRITER,
    useFactory: (): IdentityAuditWriter => new PgIdentityAuditWriter(),
  },
  {
    provide: LEGACY_PASSWORD_WINDOW,
    useFactory: (): LegacyPasswordWindow => new PgLegacyPasswordWindow(),
  },
];

/**
 * Kernel tokens the auth/users slices are allowed to import. The
 * `PASSWORD_VERIFIERS` multi-provider list is intentionally excluded
 * because it is consumed only by the verification dispatcher factory.
 */
export const IDENTITY_EXPORTS: symbol[] = [
  PASSWORD_VERIFICATION_SERVICE,
  PASSWORD_HASHER,
  PASSWORD_POLICY,
  SESSION_INVALIDATOR,
  IDENTITY_AUDIT_WRITER,
  LEGACY_PASSWORD_WINDOW,
];
