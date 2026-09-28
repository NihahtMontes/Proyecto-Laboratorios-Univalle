# MIG-001 F3 — NestJS + Auth handoff

## PHASE

F3 — NESTJS + AUTH

## STATUS

`COMPLETE`

The NestJS identity, authentication, session, authorization and user
administration backend required by the F1 contract is implemented on the F2
schema and validated by the local API test suites. F4 was not started.

## SOURCE_SHA

`dccabf50330afc48760d06bd4dbaff8c37ebbd3f` (`reference/asp-final`)

## TARGET_START_SHA / TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` / same (no commit).

## INPUT_ARTIFACTS

`01-IDENTITY-CONTRACT.md`, `01-DECISIONS.md`, `01-DATA-MAPPING.md`,
`01-HANDOFF.md` (F3_BACKEND_REQUIREMENTS), `02-F2-APPLIED.md`,
`02-HANDOFF.md`, `apps/api/migrations/control-plane/0006_mig001_users_identity.sql`
(read-only).

## WHAT_WAS_DELIVERED

Architecture and patterns are described in `03-SOLID-PATTERNS.md`.

### Authentication (F1 §6, §7)

- `POST /auth/login` takes `loginIdentifier` (username or email) and resolves it
  with one lookup in `lu_login_identifier` using the database normalizer
  `public.lu_login_identifier_normalize`. The deprecated `email` field is
  accepted only when `loginIdentifier` is absent (both together → 400) and is
  kept for the current React client until F4/F5. Unknown keys, including
  `isSuperAdmin`/`globalRole`, are rejected.
- Password transport bound: 512 UTF-8 bytes. Absent user, inactive/deleted
  account, `reset_required`, wrong password and a reached legacy deadline all
  return the same 401. Each path spends verification time: absent or
  unverifiable accounts run a dummy bcrypt comparison, and legacy verifications
  run PBKDF2 alongside it. Every outcome past the rate limiter lasts at least
  `AUTH_LOGIN_FLOOR_MS` (default 200 ms). Rate limiting keys on the normalized
  identifier.
- Verification strategies: bcrypt, ASP.NET Identity V2 and V3 (exact F1 §7
  parser bounds, asynchronous PBKDF2, constant-time comparison; malformed or
  unsupported payloads fail closed).
- Legacy deadline: legacy verification is refused once a recorded
  `lu_identity_migration_state.legacy_password_deadline` is reached
  (`now >= deadline`). With no recorded deadline no deadline-based refusal
  applies; the deadline is never inferred from deployment time.
- Rehash on login, only after successful verification and before any session:
  one transaction with compare-and-swap on the verified scheme/hash,
  current-cost bcrypt write, `security_version` rotation revoking all existing
  sessions, and a `credential_rehashed` audit event. Legacy → bcrypt sets
  `must_change_password=false` and `password_migrated_at`; a bcrypt cost
  rehash replaces only the hash and preserves `must_change_password`.
- A verified legacy password longer than 72 bytes is not rehashed (bcrypt
  would truncate it) and yields only a `password_change` session.
- New and changed passwords use one policy: at least 12 code points, at most 72
  UTF-8 bytes, upper/lower/digit/symbol, 4 distinct code points, no trim.

### Sessions (F1 §12, §17)

- Session purposes: `normal`, `password_change` (must change password or
  over-72-byte legacy proof; session inspection, CSRF, `POST /auth/password`,
  logout), `site_selection` (several eligible sites; 15-minute absolute expiry
  with no idle extension; session inspection with the eligible site list,
  CSRF, `PUT /auth/session/active-site`, logout).
- Initial site decision: zero eligible sites denies a non-SuperAdmin and gives
  a SuperAdmin a normal null-site session; one site is selected automatically;
  a requested site is honored only if eligible.
- Selecting a site from a `site_selection` session revokes it and returns a new
  session cookie; from a normal session it updates only that session. Neither
  rotates `security_version`; both append `active_site_changed`.
- Every authenticated request revalidates, in one transaction, the session
  (unrevoked, unexpired, security version), the account status and the
  active-site eligibility (membership, site status, validity).
- `POST /auth/password` verifies the current password, applies the policy,
  writes bcrypt, rotates and revokes every session in one transaction, then
  issues the replacement session.
- Guards: `SiteContextGuard` (tenant routes; eligible active site) and
  `GlobalSessionGuard` (profile, users, photos; null site only for SuperAdmin).
  Both reject restricted purposes and set `request.identity`, whose
  `sessionId` is the `lu_session` UUID. The raw token exists only in the
  cookie.

### Users, memberships and RBAC (F1 §8–§11, §15–§17)

- Global and membership commands are separate: create; global fields;
  admin password reset; account status; global delete; account restore; add
  membership; role; status; validity; work profile; site revoke; membership
  restore; self profile.
- `UserAccessPolicy` implements the F1 §10/§17 matrices: Supervisors cannot
  administer users; a site Administrador acts only on its active site, never on
  a SuperAdmin target, and changes global fields, identity card, password or
  account status only as the single-site custodian (exactly one non-revoked
  membership, in the active site, re-checked on locked rows). Only a SuperAdmin
  links an existing user to a site. Nobody changes their own identity card,
  admin-resets their own password, deactivates/deletes their own account or
  changes their own active-site membership.
- Last active SuperAdmin: demotion, inactivation and deletion paths take the
  fixed advisory lock, lock the active SuperAdmin rows and recount.
- Site revoke deletes the account only when no non-revoked membership remains
  and the user is not a SuperAdmin. Membership restore requires a revoked
  membership, keeps the stored role and work profile, requires new bounds for
  expired validity, and lets a site Administrador restore a deleted account
  only when the deletion was that site's own revoke cascade.
- `security_version` rotates for email change, admin reset, role, membership
  add/status/validity, account status, delete/revoke and restore; never for
  names, phone, identity card, photo or work profile. A self email change
  replaces the current session after commit.
- Lists: a site Administrador gets the active-site membership projection only
  (filters, paging and totals computed in SQL for that site); a SuperAdmin gets
  the global account view. Deleted accounts and revoked memberships are hidden
  by default and returned by explicit status filters. Admin details include the
  audit actors; a site Administrador sees only its site's membership.
- Payloads are allowlisted per endpoint; `isSuperAdmin`, security/row
  versions, password hash and full name are rejected with 400. Errors map to
  400/401/403/404/409.
- SuperAdmin grant/revoke exists only as the CLI `users:superadmin`
  (`node dist/users/superadmin-cli.js --grant|--revoke --user --actor --reason`),
  executed by `SuperAdminRoleService` as `lu_auth_runtime`: active SuperAdmin
  actor, no self-revoke, no grant to deleted accounts, last-active check,
  rotation and audit with actor and reason.
- People: the default list excludes deleted persons and an explicit status
  filter (including 2) returns that status. People CRUD still requires an
  eligible active-site Administrador.

### Profile pictures (F1 §14)

`PUT|GET|DELETE /users/:id/photo` and `/profile/photo`. Uploads are raw
JPEG/PNG/WEBP bytes (5 MiB limit at the transport and in validation) with the
file name in `X-Photo-Filename`; extension, declared MIME and magic signature
must agree. Keys are server generated
(`users/{user_uuid}/{random_uuid}.{ext}`); PostgreSQL stores only the key. The
new object is written, the key committed, then the old object deleted (retried,
audited on persistent failure); a failed transaction deletes the new object.
Reads use the view authorization and are served with `nosniff`, a sandboxing
CSP and `no-store`; records expose an ETag derived from the key. Development
storage is `PROFILE_PHOTO_STORAGE_DIR` or `tmp/profile-photos` (ignored by
Git); production without configured storage fails closed.

### Bootstrap and baseline

- `auth:bootstrap` writes the canonical F2 fields (username, split names,
  identity card, phone), `must_change_password=true`, and `user_created` /
  `superadmin_granted` audit rows, and runs only while no SuperAdmin exists.
- The two failures inherited from F2 (`auth-managed-role-cli`, `auth.roles`)
  were caused by `core.autocrlf=true` adding CR bytes to the checkout of the
  two role SQL files, whose pins match the LF bytes in Git. The CLI and the
  test now hash with the canonical helpers from `database/migration-plan.ts`
  (CRLF → LF only; BOM and lone CR rejected). Pins and SQL are unchanged, and
  regression tests prove that any other byte change still fails.

## EVIDENCE_SUMMARY

| Gate | Result |
|---|---|
| F3 targeted suites (24: identity kernel, CRLF, auth login/sessions/password change/controller/repository/service/rate-limit/pg-pool/bootstrap, core context, users policy/service/repository/controller, SuperAdmin CLI, people repository/controller, profile photo, grant conformance) | 479 / 479 PASS |
| `auth-managed-role-cli` | PASS |
| `auth.roles` | PASS |
| TypeScript `typecheck` | PASS |
| `nest build` | PASS |
| Broad `apps/api` suite (`--runInBand`) | 40 suites passed, 2 skipped (opt-in PostgreSQL); 1223 passed, 9 skipped, 0 failed (F2 close: 861 passed, 9 skipped, 2 failed) |
| Master-plan F3 exit: `users.controller` and `people.controller` specs pass locally; `auth.controller` rejects privileged keys; no endpoint sets `is_super_admin` | PASS |
| `git diff --check` (tracked) and `--no-index --check` (new files) | PASS |
| Historical migrations, 0006/0013 pins, role SQL | unchanged (0006 canonical SHA-256 `16d7b3e0…`) |

`test/f3.grant-conformance.e2e-spec.ts` statically checks every runtime
`INSERT`/`UPDATE` on `lu_user`, `lu_site_membership` and `lu_session`
against the `lu_auth_runtime` column grants in 0006, and forbids writes to
trigger-managed columns. It found and now guards a defect where user creation
listed the non-granted `reconciliation_state` column.

## KNOWN_LIMITATIONS

1. **No live PostgreSQL run of F3 SQL.** Local database credentials were not
   available to this session and the F2 procedure required a temporary
   `pg_hba.conf` change, which was not repeated. The gated suites
   (`AUTH_PG_INTEGRATION=1`: `auth.postgres.integration`,
   `auth.bootstrap.postgres.integration`) compile against the F2 schema but
   were not executed. The master-plan F3 gates are local and do not require a
   database; the grant-conformance test mitigates the main risk. A live run is
   recommended before cutover.
2. `POST /auth/password` has no dedicated rate limit (it requires a valid
   session). Low risk; candidate for F7 hardening.
3. `packages/contracts` and `packages/api-client` are unchanged: the login
   field, session response (`purpose`, `mustChangePassword`, `eligibleSites`),
   user records (split names, username, memberships, `displayRole`,
   `profilePicture`) and new routes are defined backend-locally in
   `auth/auth.types.ts` and `users/user.types.ts` for F4 to align.
4. CORS preflight is handled only under `/auth`; `DELETE` routes outside it
   assume same-origin deployment as before F3.
5. The legacy retirement batch (at the deadline) is a cutover runbook task; the
   runtime already refuses legacy verification once the recorded deadline is
   reached.

## FILES_CREATED

- `apps/api/src/identity/` (`identity.contracts.ts`, `identity.providers.ts`,
  `session-invalidator.ts`, `identity-audit.writer.ts`,
  `legacy-password-window.ts`, `password/*`)
- `apps/api/src/auth/session/` (`session-issuance.ts`, `index.ts`),
  `apps/api/src/auth/auth-bootstrap.errors.ts`
- `apps/api/src/users/`: `membership.repository.ts`, `user.policy.ts`,
  `user.projection.ts`, `user.types.ts`, `superadmin-role.service.ts`,
  `superadmin-cli.ts`, `profile-photo/*`
- Tests: `auth.harness.ts`, `auth.login-f3`, `auth.sessions-f3`,
  `auth.password-change`, `identity-vectors.ts`, `identity.password`,
  `identity.policy`, `identity.kernel`, `users.fakes.ts`, `users.policy`,
  `users.service`, `users.repository`, `users.controller`,
  `users.profile-photo`, `superadmin-cli`, `people.repository`,
  `people.controller`, `f3.grant-conformance`
- `docs/migration/users/03-SOLID-PATTERNS.md`, `docs/migration/users/03-HANDOFF.md`

## FILES_MODIFIED

- `apps/api/src/auth/`: `auth.controller.ts`, `auth.service.ts`,
  `auth.repository.ts`, `auth.types.ts`, `auth.module.ts`, `auth.constants.ts`,
  `auth.crypto.ts`, `auth.exceptions.ts`, `auth.fastify.ts`,
  `auth.rate-limit.ts`, `auth-bootstrap.ts`, `auth-bootstrap-cli.ts`,
  `auth-managed-role-cli.ts`
- `apps/api/src/core/core.guard.ts`
- `apps/api/src/users/`: `user.controller.ts`, `user.service.ts`,
  `user.repository.ts`, `user.module.ts`
- `apps/api/src/people/person.repository.ts`
- `apps/api/package.json` (scripts `preusers:superadmin`, `users:superadmin`)
- Tests: `auth.controller`, `auth.service`, `auth.repository`,
  `auth.bootstrap`, `auth.bootstrap.postgres.integration`,
  `auth.postgres.integration`, `auth.roles`, `auth-managed-role-cli`,
  `core.context` specs; `auth.fakes.ts`, `auth.test-helpers.ts`
- `docs/migration/users/00-PARITY-MATRIX.md` (F3 rows N-03, N-07, K-04,
  counts and an appended F3 section)

## DO_NOT_REOPEN_IN_F4

- The kernel seam (`identity/identity.contracts.ts`) and the rule that auth and
  users depend on tokens, not kernel classes.
- Login semantics: single claim lookup, generic timing-floored failures,
  rehash-before-session, the >72-byte legacy rule and the recorded-deadline
  refusal.
- Session purposes, their capabilities and expiries; `RequestIdentity` carries
  the session UUID only.
- The authorization matrix in `UserAccessPolicy`, custody on locked rows, the
  last-active-SuperAdmin lock and SuperAdmin changes only through the CLI.
- The `security_version` rotation set above.

## F4_REQUIRED_INPUTS

1. `docs/migration/users/03-HANDOFF.md`, `03-SOLID-PATTERNS.md`
2. `apps/api/src/auth/auth.types.ts` (login body, `AuthSessionResponse`),
   `apps/api/src/auth/auth.controller.ts`
3. `apps/api/src/users/user.types.ts`, `user.controller.ts`,
   `profile-photo/profile-photo.controller.ts`
4. `packages/contracts/src/{auth,users,site,people}.ts`,
   `packages/api-client/src/index.ts`

## F4_OBJECTIVE

Align `packages/contracts` and `packages/api-client` with the F3 API:
`loginIdentifier` (then retire the `email` alias), session `purpose` /
`mustChangePassword` / `eligibleSites`, site selection and password change,
the split global/membership user commands and records, profile and photo
routes, and the 400/401/403/404/409 error envelope.
