# MIG-001 F3 — SOLID principles and design patterns (as implemented)

This document describes the structure that exists in `apps/api` at the close of
F3. Every statement names the file that implements it. Nothing here is a
proposal.

## 1. Layering (SRP)

| Layer | Responsibility | Files |
|---|---|---|
| Controllers | HTTP only: payload allowlists, parameter validation, cookies, mapping domain errors to the API error envelope | `auth/auth.controller.ts`, `users/user.controller.ts` (`UserController`, `ProfileController`), `users/profile-photo/profile-photo.controller.ts`, `people/person.controller.ts` |
| Guards | Request authentication and context: correlation id, Origin/CSRF, session resolution | `core/core.guard.ts` (`ApiCorrelationGuard`, `SiteContextGuard`, `GlobalSessionGuard`), `auth/auth.guard.ts` (`AuthSecurityGuard`) |
| Application services | Use cases and transaction boundaries | `auth/auth.service.ts` (`AuthService`), `users/user.service.ts` (`UserService`), `users/superadmin-role.service.ts`, `users/profile-photo/profile-photo.service.ts` |
| Pure policy / planning | Decisions without I/O | `users/user.policy.ts` (`UserAccessPolicy`), `auth/session/session-issuance.ts` (`resolveSessionIssuance`, `resolvePostPasswordChangePlan`, `buildRequestIdentity`), `users/user.projection.ts` |
| Repositories | Parameterized SQL only | `auth/auth.repository.ts`, `users/user.repository.ts`, `users/membership.repository.ts`, `users/profile-photo/profile-photo.repository.ts` |
| Identity kernel | Password algorithms, hashing, policy, session invalidation, audit, legacy window | `identity/**` |

Examples of single responsibilities that were separated during F3:

- `UserAccessPolicy` answers the F1 §10/§17 authorization questions from a
  `RequestIdentity` and a custody probe; `UserService` loads and locks the rows
  and asks the policy inside the transaction.
- `user.projection.ts` owns the read model (effective ASP status per
  membership, initials, role badge, profile-picture ETag). Repositories return
  rows only; the former duplicate projection in the repository was removed.
- `session-issuance.ts` decides the session purpose, active site and expiries;
  `AuthService` persists what the plan says.
- The SuperAdmin CLI (`users/superadmin-cli.ts`) only parses arguments and
  opens the connection; the command itself is `SuperAdminRoleService`, the same
  class any future caller would use.

## 2. Dependency inversion and NestJS DI

The seam between the auth flows, the users slice and the kernel is
`identity/identity.contracts.ts`: interfaces plus DI tokens.

| Token | Interface | Production binding (`identity/identity.providers.ts`) |
|---|---|---|
| `PASSWORD_VERIFIERS` | `PasswordVerifier[]` | `BcryptPasswordVerifier`, `AspNetIdentityPasswordVerifier('legacy_identity_v2')`, `AspNetIdentityPasswordVerifier('legacy_identity_v3')` |
| `PASSWORD_VERIFICATION_SERVICE` | `PasswordVerificationService` | `DefaultPasswordVerificationService` |
| `PASSWORD_HASHER` | `PasswordHasher` | `BcryptPasswordHasher` (configured cost) |
| `PASSWORD_POLICY` | `PasswordPolicy` | `Utf8PasswordPolicy` |
| `SESSION_INVALIDATOR` | `SessionInvalidator` | `PgSessionInvalidator` |
| `IDENTITY_AUDIT_WRITER` | `IdentityAuditWriter` | `PgIdentityAuditWriter` |
| `LEGACY_PASSWORD_WINDOW` | `LegacyPasswordWindow` | `PgLegacyPasswordWindow` |
| `SELF_SESSION_RENEWER` | `SelfSessionRenewer` | `SelfSessionRenewerImpl` (auth slice) |
| `PROFILE_PHOTO_STORAGE` | `ProfilePhotoStorage` | `createProfilePhotoStorage(env)` |

`AuthModule` spreads `IDENTITY_PROVIDERS` and re-exports `IDENTITY_EXPORTS`;
`UserModule` imports `AuthModule`. `AuthService`, `UserService`,
`SuperAdminRoleService` and `ProfilePhotoService` receive these abstractions by
token and never construct kernel classes. The users slice renews a session
through `SELF_SESSION_RENEWER` without depending on `AuthService`.

The test `test/auth.service.e2e-spec.ts` ("AuthModule production wiring")
resolves every token from the real module graph and checks the bound class.

## 3. Strategy — password verification

`PasswordVerifier` (`identity.contracts.ts`) is one strategy per stored
`password_scheme`:

- `identity/password/bcrypt.verifier.ts` — bcrypt; rejects plaintexts over 72
  UTF-8 bytes without comparing; accepts stored costs from 10 (4 only in the
  test seam) to 15 and reports `needsRehash` when the cost differs from the
  configured one.
- `identity/password/aspnet-identity.verifier.ts` — ASP.NET Identity V2/V3;
  one instance per scheme; a payload whose marker does not match its scheme
  fails.

`DefaultPasswordVerificationService` (`identity/password/password-verification.service.ts`)
selects the strategy by scheme from a map built at construction (a duplicate
scheme is a construction error). `reset_required`, a null hash or an unknown
scheme run a dummy bcrypt comparison and fail.

## 4. Adapter — ASP.NET Identity hash format

`identity/password/aspnet-identity-hash.ts` (`parseAspNetIdentityHash`) adapts
the stored ASP.NET Identity Base64 payload into a typed structure
(`version`, `prf`, `iterations`, `salt`, `subkey`) under the F1 §7 bounds:
strict canonical Base64, at most 141 decoded bytes, V2 exactly 49 bytes
(`0x00`, 16-byte salt, 32-byte subkey, HMAC-SHA1, 1,000 iterations), V3 marker
`0x01` with big-endian PRF/iterations/salt length, PRF 0/1/2 only, iterations
1,000–1,000,000, salt and subkey 16–64 bytes and an exact remaining length.
Anything else returns `null`. The verifier then uses Node's asynchronous
`crypto.pbkdf2` and `crypto.timingSafeEqual`. A test asserts that this accept
set equals the importer's (`database/legacy-user-mapping.ts`).

Other adapters: `ProfilePhotoStorage` with `FilesystemProfilePhotoStorage`
(development) and `CliTransactionPool`, which adapts one `pg` client to
`IPgPool` for the SuperAdmin CLI (every transaction assumes
`lu_auth_runtime`).

## 5. Open/closed

- Adding or retiring a password scheme changes only the provider list in
  `identity.providers.ts`. `AuthService` asks the verification service and
  reads `needsRehash`; it has no per-algorithm branches. When the legacy
  window ends, removing the two ASP.NET strategies leaves `AuthService`
  unchanged (such accounts then fail like any unknown scheme).
- The legacy window is an injected `LegacyPasswordWindow`; the retirement rule
  can change without touching login.
- Session purposes and expiries live in `resolveSessionIssuance`; a new purpose
  is added there and in the guards, not in each flow.

## 6. Interface segregation

- The kernel is split into narrow interfaces (verifier, verification service,
  hasher, policy, invalidator, audit writer, legacy window, renewer) instead of
  one "identity service".
- Persistence is split by aggregate: `UserRepository` (global identity),
  `MembershipRepository` (site memberships), `ProfilePhotoRepository` (only the
  picture key), `AuthRepository` (claims, sessions, credential writes).
- Guards expose only `RequestIdentity` (`correlationId`, `sessionId`,
  `userId`, `isSuperAdmin`, `activeSiteId`, `activeSiteRole`); the raw session
  token never leaves the auth slice.

## 7. Liskov substitution

All implementations keep their interface contracts, and the tests substitute
them freely:

- Every `PasswordVerifier` returns `{ verified, needsRehash }`, never throws for
  malformed input and never reports `needsRehash` without `verified`.
- `SessionInvalidator.rotateAndRevokeAll` increments `security_version` exactly
  once and revokes every live session of the subject inside the caller's
  transaction; the test double (`test/auth.harness.ts`) implements the same
  semantics against the in-memory repository.
- `FakeAuthRepository` (`test/auth.fakes.ts`) mirrors the real repository's
  SQL behavior (claim lookup, eligibility predicate, compare-and-swap writes
  that never touch `security_version`, the session CHECK constraint), so
  service, controller and guard tests run against faithful semantics.

## 8. Other patterns in use

| Pattern | Where | Purpose |
|---|---|---|
| Unit of work / transaction script | `AuthService.rehashAfterVerification`, `AuthService.changePassword`, every `UserService` command | one control-plane transaction per sensitive change: lock, compare-and-swap, write, rotate, audit |
| Compare-and-swap | `AuthRepository.rehashUserPassword`, `rehashUserPasswordBcryptCost`, `changePasswordHash` | a concurrent credential change makes the loser fail generically |
| Pessimistic locking | `FOR UPDATE` on user, membership and session rows; `pg_advisory_xact_lock` on the fixed last-SuperAdmin key | F1 §8 last-active-SuperAdmin and custody checks under concurrency |
| Policy object | `UserAccessPolicy` | F1 §9/§10/§17 matrices as pure, table-tested functions |
| Planner (pure function) | `resolveSessionIssuance` | F1 §17 zero/one/many decision and per-purpose expiry |
| Guard chain | `ApiCorrelationGuard → AuthSecurityGuard → GlobalSessionGuard / SiteContextGuard` | correlation, Origin/CSRF before any session work, then identity |
| Read-model projection | `user.projection.ts` | effective ASP status and display fields |

## 9. Deliberate non-abstractions

- The users slice does not wrap `IPgPool` in another interface; repositories
  already isolate SQL and the pool is the transaction boundary.
- `UserService` instantiates `UserAccessPolicy` directly: it is pure, has one
  implementation and needs no substitution.
- `packages/contracts` was not changed in F3; backend-local request and
  response types live in `auth/auth.types.ts` and `users/user.types.ts` and are
  aligned in F4.
