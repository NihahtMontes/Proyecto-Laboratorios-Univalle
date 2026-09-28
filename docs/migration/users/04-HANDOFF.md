# MIG-001 F4 — Contracts + API client handoff

## PHASE

F4 — CONTRACTS + API CLIENT

## STATUS

`COMPLETE`

`packages/contracts` and `packages/api-client` now describe the F3 API exactly.
The F3 backend was not modified. F5 was not started.

## EXECUTION_MODE

Claude Opus direct (MiniMax Token Plan exhausted); no worker dispatch.

## TARGET_START_SHA / TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` / same (no commit).

## INPUT_ARTIFACTS

`00-MASTER-PLAN.md` (F4), `01-IDENTITY-CONTRACT.md`, `01-DECISIONS.md`
(D004, D006, D015, D018), `03-HANDOFF.md`, `03-SOLID-PATTERNS.md`, and the F3
sources: `auth/auth.types.ts`, `auth/auth.controller.ts`, `auth/auth.exceptions.ts`,
`auth/auth.filter.ts`, `core/core.filter.ts`, `core/core.guard.ts`,
`users/user.types.ts`, `users/user.controller.ts`,
`users/profile-photo/profile-photo.{controller,validator}.ts`,
`identity/password/password-policy.ts`, `people/person.{controller,service}.ts`.

## ENDPOINT_INVENTORY (F3, frontend-consumable)

All routes are under `/api/v1`, use the session cookie (`credentials: 'include'`),
and require `X-CSRF-Token` (double-submit cookie from `GET /auth/csrf`) on every
non-GET call. Errors use `{ success: false, error: { code, message, correlationId?, fieldErrors? } }`.

| Method | Path | Request | Success | Session / site |
|---|---|---|---|---|
| GET | `/auth/csrf` | — | 200 `{ csrfToken }` | none |
| POST | `/auth/login` | `LoginRequest` | 200 `AuthSessionEnvelope` + cookie | none; 401 generic, 429 `Retry-After` |
| GET | `/auth/session` | — | 200 `AuthSessionEnvelope` | any purpose |
| PUT | `/auth/session/active-site` | `SetActiveSiteRequest` | 200 envelope (+ new cookie from `site_selection`) | `normal` or `site_selection` |
| POST | `/auth/password` | `ChangePasswordRequest` | 200 envelope + new cookie | `normal` or `password_change` |
| POST | `/auth/logout` | — | 204 | any |
| GET | `/users` | `ManagedUserQuery` | 200 `ManagedUserPage` | normal; global route (SuperAdmin may have no site) |
| POST | `/users` | `CreateManagedUserInput` (shape chosen by caller role) | 201 `ManagedUserRecord` | normal |
| GET | `/users/:id` | — | 200 `ManagedUserRecord` (+ audit actors) | normal |
| PUT | `/users/:id/global-fields` | `UpdateUserGlobalFieldsInput` | 200 `ManagedUserRecord` | normal |
| POST | `/users/:id/admin-reset-password` | `AdminResetPasswordInput` | 204 | normal |
| PUT | `/users/:id/account-status` | `SetAccountStatusInput` | 200 `data: null` | normal |
| POST | `/users/:id/restore` | `RestoreAccountInput` (`{}` allowed) | 204 | normal |
| DELETE | `/users/:id` | — | 204 | normal |
| POST | `/users/:id/memberships` | `AddMembershipInput` | 201 `data: null` | normal (SuperAdmin links) |
| PUT | `/users/:id/memberships/:siteId/{role,status,validity,work-profile}` | matching input | 200 `data: null` | normal |
| DELETE | `/users/:id/memberships/:siteId` | — | 204 | normal |
| POST | `/users/:id/memberships/:siteId/restore` | `RestoreMembershipInput` | 204 | normal |
| GET / PUT | `/profile` | — / `UpdateProfileInput` | 200 `ProfileRecord` | normal |
| GET / PUT / DELETE | `/users/:id/photo`, `/profile/photo` | raw bytes + `Content-Type` + `X-Photo-Filename` | image bytes / 200 `ProfilePictureRef` / 204 | normal |
| GET / POST / PUT / DELETE | `/people`, `/people/:id` | `PersonQuery` / `CreatePersonInput` / `UpdatePersonInput` | 200 | normal + eligible active site + Administrador |

Status codes: 400 `VALIDATION_ERROR` (+`fieldErrors`), 401 `INVALID_CREDENTIALS`
(bad login, missing/expired/revoked session), 403 `ACCESS_DENIED` (RBAC) /
`SITE_ACCESS_DENIED` (restricted purpose, no eligible site, non-admin on people) /
`CSRF_INVALID` / `ORIGIN_DENIED`, 404 `NOT_FOUND`, 409 `CONFLICT`, 429
`RATE_LIMITED`, 503 `SERVICE_UNAVAILABLE`.

## CLASSIFICATION (before F4)

| Item | Class | Resolution |
|---|---|---|
| `ApiFailure` envelope, CSRF transport, cookie credentials | PARITY | unchanged |
| People contract (backend imports it) | PARITY | documented visibility/delete semantics |
| `LoginRequest.email` | STALE | `loginIdentifier` canonical; `LegacyEmailLoginRequest` deprecated |
| `AuthSessionResponse = ActiveSiteSession` | STALE | `{ session, purpose, mustChangePassword, eligibleSites }` + wire `AuthSessionEnvelope` |
| `users.ts` (fullName, status active/disabled, siteRole, single update) | STALE | F3 records, split commands; old shapes renamed `Legacy*` |
| `POST /auth/password`, password policy, 13 user command routes, photo routes, error kinds | MISSING | added |
| Client parsing 204 responses (`DELETE /users/:id` etc. threw `INVALID_API_RESPONSE`) | CONFLICT (client defect) | `command()` accepts 204 or `data: null` |
| F3 vs frozen F1 | none found | backend untouched |

## WHAT_WAS_DELIVERED

### Contracts (`packages/contracts/src`)

- `auth.ts`: `AUTH_ROUTES.password`; `LoginRequest { loginIdentifier, password,
  rememberMe?, activeSiteId? }`; `LOGIN_PASSWORD_MAX_BYTES` (512);
  `SessionPurpose`; `SITE_SELECTION_SESSION_TTL_SECONDS` (900); `EligibleSite`;
  `AuthSessionMeta`; `AuthSessionEnvelope` (wire); `AuthSessionResponse`
  (decoded, identical to the F3 backend type); `ChangePasswordRequest`;
  `PASSWORD_POLICY`, `PasswordPolicyViolation`, `passwordPolicyViolations()`
  (F1-D006 shared validation).
- `users.ts`: `AccountStatus`, `MembershipStatus`, `MembershipRole`,
  `EffectiveAspStatus`, `DisplayRole`, `ProfilePictureRef`, `AuditActor`,
  `MembershipProjection`, `ManagedUserRecord`, `ManagedUserPage`,
  `ManagedUserQuery` (`siteScope: SiteId | 'all'`), create (global/site) and
  every command input, `ProfileRecord`, `UpdateProfileInput` (no identity card),
  `PROFILE_PHOTO` (5 MiB, JPEG/PNG/WebP, header name), `ProfilePhotoViolation`,
  `USER_ROUTES` for all 17 user/profile/photo paths.
- `api.ts`: `API_ERROR_CODES` / `KnownApiErrorCode` (documentation of emitted
  codes; `ApiError.code` stays an open string).
- `people.ts`: comments only (tenant scope, Administrador requirement, deleted
  filtering, soft delete).

Not exposed: `securityVersion`, password scheme/hash, session id or token,
row versions, `isSuperAdmin` as input. Session tokens exist only in the
HttpOnly cookie.

### API client (`packages/api-client/src/index.ts`)

- Auth: `signIn`, `currentSession`, `selectActiveSite`, `changePassword`
  (all return `AuthSessionResponse`, structurally validated: unknown `purpose`
  or missing `meta`/`eligibleSites` → `INVALID_API_RESPONSE`), `logout`.
- Users: `listUsers`, `userDetails`, `createManagedUser`,
  `updateUserGlobalFields`, `adminResetPassword`, `setAccountStatus`,
  `restoreAccount`, `deleteAccount`, `addMembership`, `changeMembershipRole`,
  `changeMembershipStatus`, `changeMembershipValidity`,
  `updateMembershipWorkProfile`, `revokeMembership`, `restoreMembership`.
- Profile: `ownProfile`, `updateOwnProfile`; photos: `userPhoto`,
  `uploadUserPhoto`, `deleteUserPhoto`, `ownPhoto`, `uploadOwnPhoto`,
  `deleteOwnPhoto` (raw bytes, never JSON).
- Errors: `ApiClientError` gains `code`, `kind` (`apiErrorKind`: validation,
  authentication, authorization, not_found, conflict, payload_too_large,
  unsupported_media_type, rate_limited, unavailable, invalid_response,
  unexpected), `fieldErrors`, `correlationId`, `retryAfterSeconds`.
- Transport unchanged: one fetch path, `credentials: 'include'`, CSRF fetched
  once and cleared on logout, HTTPS-only base URL except loopback, no token
  logging, non-JSON bodies never surfaced as messages.

### F5 routing rule

Route on `purpose` (not on `mustChangePassword`): `normal` → workspace;
`site_selection` → site picker from `eligibleSites`, then `selectActiveSite`
(15-minute absolute window; 401 after expiry → back to login);
`password_change` → password form, then `changePassword`. A verified legacy
password over 72 bytes yields `password_change` even when `mustChangePassword` is false.

## TEMPORARY_COMPATIBILITY (remove in F5)

- `@lu/contracts`: `LegacyEmailLoginRequest`, `LegacyManagedUserStatus`,
  `LegacyManagedUserRecord`, `LegacyManagedUserQuery`, `LegacyManagedUserPage`,
  `LegacyCreateManagedUserInput`, `LegacyUpdateManagedUserInput`,
  `LegacyProfileRecord`, `LegacyUpdateProfileInput` (all `@deprecated`).
- `ApiClient` `@deprecated` methods: `login` (maps `email` → `loginIdentifier`
  and returns only `session`), `session`, `setActiveSite`, `users`, `user`,
  `createUser`, `updateUser` (targets `PUT /users/:id`, which F3 replaced by
  commands — unchanged pre-F4 behavior), `deleteUser` (now `deleteAccount`),
  `profile`, `updateProfile`.
- `apps/web/src/App.tsx`, `UsersPanel.tsx`: type-only import re-pointing to the
  `Legacy*` names (aliased), marked with a MIG-001 F4 comment. No UI or runtime
  change other than the login body now carrying `loginIdentifier`.
- Backend `email` login alias: preserved (not removed by F4). The client no
  longer sends it; `tests/e2e/deployed-auth.js` still does. Retire it after F5
  migrates the login and that script.

## EVIDENCE_SUMMARY

| Gate | Result |
|---|---|
| `@lu/api-client` tests (`node --test`) | 31 / 31 PASS (4 pre-existing + 27 F4; the stale email-login test was replaced) |
| `apps/api/test/f4.contract-conformance.e2e-spec.ts` | 3 / 3 PASS — type-level identity of auth/user/profile/policy/photo types with the backend, request assignability, route existence via Nest metadata, password-policy parity on 18 vectors against `Utf8PasswordPolicy` |
| Negative probe of the type assertions | a deliberately wrong assertion fails `tsc` (probe removed) |
| `@lu/web` tests (vitest) | 6 / 6 PASS |
| `@lu/contracts` test (tsc) | PASS |
| Root `typecheck` | PASS |
| Root `build` | PASS |
| ESLint (touched files, `--max-warnings=0`) / Prettier | PASS |
| Master-plan F4 exit: `index.test.ts` covers new methods; compiles; F5-compatible without mass rewrites | PASS |
| `git diff --check` (tracked) / `--no-index --check` (new files) | PASS |
| F3 backend source, historical migrations | unchanged |

Broad `apps/api` suite not rerun: no backend source changed; the only API-side
addition is the conformance spec above.

## FILES_CREATED

- `apps/api/test/f4.contract-conformance.e2e-spec.ts`
- `docs/migration/users/04-HANDOFF.md`

## FILES_MODIFIED

- `packages/contracts/src/auth.ts`, `users.ts`, `api.ts`, `people.ts`
- `packages/api-client/src/index.ts`, `index.test.ts`
- `apps/web/src/App.tsx`, `apps/web/src/UsersPanel.tsx` (type imports only)
- `docs/migration/users/00-PARITY-MATRIX.md` (E-02, E-04, E-13, N-02, counts,
  appended F4 section)

## KNOWN_LIMITATIONS / PRE-CUTOVER

1. F3 SQL still not run against live PostgreSQL (`AUTH_PG_INTEGRATION=1`
   suites compile only). Recommended before cutover.
2. `POST /auth/password` has no endpoint-specific rate limit (requires a
   valid session). Hardening backlog (F7).
3. `GET /profile` carries no audit actors (parity row J-05); the admin detail
   does. Backend decision for F5/F7; not a contract gap.
4. Password-policy `fieldErrors` differ by route: identifiers on
   `POST /auth/password` (`newPassword`), readable rules on user create/admin
   reset (`password`). F5 should map both or rely on `passwordPolicyViolations`.
5. CORS preflight exists only under `/auth`; `/users`, `/profile`, photo and
   people routes (including `X-Photo-Filename`) assume same-origin deployment.
6. Oversized photo bodies are rejected by the transport limit (413) before
   validation; the client classifies it as `payload_too_large`.

## DO_NOT_REOPEN_IN_F5

- Canonical names: `loginIdentifier`, `AuthSessionResponse.purpose`,
  membership-scoped roles, split user commands, `Legacy*` = removal only.
- The conformance spec is the drift guard; change contract and backend together.

## F5_REQUIRED_INPUTS

1. `docs/migration/users/04-HANDOFF.md`
2. `packages/contracts/src/{auth,users,people}.ts`
3. `packages/api-client/src/index.ts` (canonical methods; delete the
   deprecated block and `Legacy*` types when the panels migrate)
4. `00-PARITY-MATRIX.md` rows owned by F5
