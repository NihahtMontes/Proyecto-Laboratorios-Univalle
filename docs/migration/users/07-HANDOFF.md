# MIG-001 F7 — E2E + live PostgreSQL handoff

## PHASE

F7 — E2E + VISUAL VERIFICATION (browser E2E, live PostgreSQL, system reconciliation)

## STATUS

`COMPLETE` — with one open authorization decision recorded for F8 (see
"OPEN DECISION"). F8 was not started.

## EXECUTION_MODE

Claude + MiniMax. Claude owned the test strategy, the disposable-stack harness,
security-sensitive scenarios, triage, product fixes and acceptance. MiniMax
(`minimax-coding-plan/MiniMax-M3`, agent `build`, `routing_verified: true` for every run)
wrote the Playwright specs and updated the opt-in PostgreSQL suites.

| Run | Role | Result |
|---|---|---|
| W-F3PG + fix | tests | integration suites updated to F2 schema / F3 runtime; COMPLETE |
| W-A + fix | tests | auth/session/password/site-selection/legacy specs; COMPLETE |
| W-B + fix (1 process retry: OpenCode "database is locked" at start) | tests | users/RBAC/self-protection/photo specs; COMPLETE |
| W-C + fix | tests | people/profile/errors/navigation/responsive specs; COMPLETE |

Remaining small defects after each worker's single retry were fixed directly by Claude.

## TARGET_START_SHA / TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` / same (no commit).

## HARNESS (new, tracked, opt-in)

`tests/e2e` is now the `@lu/e2e` workspace package (Playwright 1.63.0, installed Chrome via
`channel: 'chrome'`, no browser download). Entry point:

```
node tests/e2e/stack/run-f7.mjs --phase=e2e|integration|all [-- <playwright args>]
```

- Requires `E2E_ADMIN_DSN` (loopback admin, used only for role snapshot/restore,
  CREATE/DROP of the run's databases and a temporary executor login). Optional
  `E2E_ADMIN_WINDOW_MODULE` (local access window kept outside the repo) and
  `E2E_RECOVERY_FILE` (outside the repo). Nothing secret is printed or written in the repo.
- Per run: disposable `f7_<run>_control`, `f7_<run>_tenant_a`, `f7_<run>_tenant_b` owned by
  `lu_auth_migrator`; migrations applied through the real CLI with a temporary executor
  login; control-plane role grants applied from the tracked
  `apps/api/database/roles/003_provision_auth_runtime_managed.sql` (LF SHA-256 pin
  `7fd48d71…` verified; only its four `neondb` references retargeted in memory; its
  postconditions run) **between CP 0003 and 0004**; two sites with active tenant routes.
- `lu_auth_login`: exact state captured before any change; the integration phase runs with
  the suites' `NOLOGIN PASSWORD NULL` precondition; the E2E phase gets a random per-run
  password whose SCRAM verifier is computed client-side; the original state is restored
  byte-exact and verified every run.
- NestJS (127.0.0.1:3000) and Vite (127.0.0.1:5173) are started and stopped by the runner;
  it refuses to start if a port is busy (no unrelated process is touched).
- Teardown (always, in `finally`): stop processes, restore `lu_auth_login`, drop the run's
  databases, drop the executor login, assert residue 0, remove the photo dir.
- Only the per-IP login ceiling is raised (`AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS=1000`, all
  traffic is 127.0.0.1); the per-identifier limit keeps its default and is asserted (A15).
- Do not wrap the runner in `timeout`: the Playwright phase is synchronous, so a killed
  runner cannot tear down.

Local PostgreSQL admin access used the F2 procedure: byte-exact `pg_hba.conf` backup, pin
`0C8DC6E6…CD42` verified, only `host all postgres 127.0.0.1/32 trust` prepended during admin
operations (not during browser runs), byte-exact restore in `finally`, SHA re-verified and a
new password-less `postgres` connection proven rejected after every window.

## PRODUCT DEFECTS FOUND BY THE LIVE RUN AND FIXED

| # | Layer | Defect | Fix | Regression test |
|---|---|---|---|---|
| 1 | F3 `auth.repository.ts` | rate-limit bucket stored scope `identifier_ip`, rejected by the frozen 0002 CHECK (`email_ip`, `ip`) → **every login failed closed with 401** on real PostgreSQL | store the identifier+IP bucket under the schema label `email_ip` (key hash unchanged: normalized username/email + IP) | `auth.repository.e2e-spec` "stores only scope labels accepted by the 0002 CHECK" (reads the migration) |
| 2 | F3 `auth.repository.ts` (security) | `findSessionByTokenHashForUpdate` selected a bare `u.security_version` after `s.security_version`; node-pg keys rows by name, so the session snapshot was overwritten and the "security_version must equal the session snapshot" check never fired | select only `u.security_version AS user_security_version` | `auth.repository.e2e-spec` "keeps the session security_version snapshot distinct" (mutation-verified) + live integration test 4 |
| 3 | F5 `UsersPanel.tsx`, `PeoplePanel.tsx` | list loads had no stale-response guard: a slower, older response (e.g. the unfiltered first load) could overwrite a newer search result (caught as an intermittent E2E failure) | request-sequence guard: only the newest request commits state | 2 vitest cases resolving responses out of order (mutation-verified) |

Environment finding (not a product defect): a fresh control plane needs the separate role
provisioning (roles/003) after CP 0003 and before 0004+; the migration chain alone leaves
`lu_auth_runtime` without grants on 0001–0003 tables (42501). This is a cutover runbook step.

## OPEN DECISION (security-relevant, not changed)

Site-admin restore of a revoked membership is unreachable from the UI.
`01-IDENTITY-CONTRACT.md` §11 lets the active-site Administrador query the site's history,
show historical rows in Details and restore a revoked membership (including the account
deleted by the site-revoke cascade). The API allows the restore command, and the Eliminado
list shows the user, but `UserAccessPolicy.canViewUser` denies Details when the subject's
membership in the actor's site is revoked (404). A policy change that widens read access was
prepared, flagged as an authorization-weakening change by the session's permission check,
and **reverted** — it needs an explicit owner decision in F8. Today the SuperAdmin restores
from Details (E2E B8 proves it; the site-admin 404 is asserted as observed).

## E2E COVERAGE (97 tests, 11 files, retries 0)

| File | Tests | Scope |
|---|---|---|
| `00-stack.smoke` | 1 | real stack login → workspace |
| `auth.session` | 12 | username/email login, wrong password, inactive/deleted/reset_required, no membership, reload bootstrap, logout (server session dead), expired + revoked session → login, multi-site picker + switch + reload, site_selection cannot reach workspace/API, SuperAdmin null-site and with memberships |
| `auth.password` | 10 | purpose-based forced change, wrong current password, policy violation, valid change (old refused / new works), voluntary change revokes other sessions, per-identifier 429, session secret exposure (HttpOnly/SameSite, no leak to storage/DOM/console) |
| `auth.legacy` (serial) | 5 | ASP.NET V2 and V3 login → bcrypt rehash, >72-byte legacy → password_change without rehash then bcrypt, wrong legacy password, deadline reached → refused |
| `users.flows` | 10 | index scope/search/status/pagination, tabs, details, create, duplicate 409, edit, inactivate/reactivate, membership role/suspend/revoke/restore (SuperAdmin), known gap B9, SuperAdmin create + add membership + global delete |
| `users.rbac` | 11 | Supervisor refused, cross-site 404, custody 403 (multi-site / zero-membership targets), LINK_REQUIRES_SUPERADMIN, SuperAdmin target protected, not-active-site, SUPERADMIN_REQUIRED, self-protection, no SuperAdmin grant over HTTP, concurrent SuperAdmin cross-deletes keep ≥ 1 active |
| `users.photo` | 4 | PNG → JPEG → WebP → remove, oversize blocked client-side + server 413, type mismatch refused, custody 403 |
| `people` | 10 | site isolation, search by name/email/code, Id not matched (I-09), deleted filter, inline create, external address validation, edit, soft delete, Supervisor/SuperAdmin access |
| `profile` | 4 | real data, edit + persistence + top bar, own email change keeps session, own photo; audit card absent (J-05) |
| `errors` | 9 | real 400/401/403/404/409 inline, no stack/SQL leak, navigation and fallbacks |
| `responsive` | 21 | 7 surfaces × 490, 768, 1440 px: no page overflow, menu usable, tables scroll in container |

Happy paths assert console/network health (no page errors, failed requests, 5xx or
unexpected 4xx). Legacy last-SuperAdmin refusal (`LAST_SUPERADMIN`) is covered by policy,
service (advisory lock + recount) and CLI unit tests; over HTTP it cannot be reached by a
single actor, so E2E asserts the global invariant under concurrency.

## EVIDENCE SUMMARY

| Gate | Result |
|---|---|
| F3 live PostgreSQL (`auth.postgres.integration`, `auth.bootstrap.postgres.integration`, real F3 providers, F2 schema + grants) | 10 / 10 PASS |
| Playwright F7 (final consolidated run, `--phase=all`) | 97 / 97 PASS |
| Teardown every run | ports free, `lu_auth_login` restored, temp DB 0, temp role 0, pg_hba pin match, trust inactive |
| `apps/api` jest (`--runInBand`) | 1228 passed, 10 skipped (the opt-in PG suites, run live above), 0 failed |
| `@lu/web` vitest | 42 / 42 PASS |
| `@lu/api-client` node --test | 30 / 30 PASS |
| Typecheck (contracts, api-client, api, web, e2e, vercel) | PASS |
| Build (contracts, api-client, api, web) | PASS |
| ESLint `tests/e2e/**` and changed web files | PASS |
| Prettier (F7-authored files, `tests/**/*.{js,md}`) | PASS |
| `git diff --check` (tracked + new untracked F7 files) | PASS |
| Secret scan of new/changed files | no secrets (3 reviewed false positives) |

Root `pnpm run typecheck/build` could not be invoked because the corepack pnpm native binary
disappeared from the local cache mid-session; the identical per-package commands were run
directly (`.claude/runtime/f7-checkpoint/gates.sh`, local). Restoring corepack is a local
environment task.

## TEST DATA STRATEGY

Every test seeds its own synthetic actors (`seedUser`, `seedPerson`) with unique names;
tests never share users, sessions or order. Fixture writes use the run's executor login only
for preconditions the UI cannot create (legacy hashes, deleted accounts, expired sessions)
and to verify persisted effects; hashes are never asserted. The legacy file is serial because
the migration-deadline singleton is immutable. All data dies with the run's databases.

## REMAINING SKIPS

None in the F7 suite. The two opt-in PostgreSQL suites stay gated
(`AUTH_PG_INTEGRATION=1`) in the normal jest run by design and ran live through the runner.

## KNOWN PRODUCT GAPS (carried, not changed)

No global toast; no pre-submit confirmation; no letters-only name blocking; People search
does not match Id; People create inline; no Person details; no `/Error` page; SuperAdmin
cannot filter users by site; site Admin cannot predict global-field permission before
submit; multi-step create/profile/photo not atomic; J-05 profile audit actors unavailable.

New from F7:
1. OPEN DECISION above (site-admin restore unreachable from UI).
2. A SuperAdmin without memberships has no site to choose in Create (no sites catalog);
   the POST fails with 400 and the UI shows the untranslated server message
   "At least one membership is required."
3. Voluntary password change on `/Profile` gives no success message (fields reset only).
4. Command routes answer 403 (custody) for an existing out-of-scope UUID and 404 for an
   unknown one, while reads answer 404 for both — a weak existence oracle (UUIDs are not
   guessable); contract unchanged.

## PRE-CUTOVER ITEMS

1. `POST /auth/password` still has no endpoint-specific rate limit (requires a valid session;
   hardening, not changed in F7).
2. Backend `email` login alias still accepted. Consumers: none left besides its own contract
   test (`apps/api/test/auth.controller.e2e-spec.ts`); the integration suite now uses
   `loginIdentifier`. Removal is a cutover decision.
3. Global stylesheet (assets vs dist): the F7 responsive smoke of MIG-001 surfaces found no
   regression depending on it — decision carried to F8.
4. Cutover runbook must apply roles/003 between CP 0003 and 0004 on a fresh control plane.
5. CORS preflight only under `/auth` (unchanged).
6. Pre-existing lint/format debt in F3 files not authored in F7 (`user.policy.ts` unused
   `actor` in `canAccessProfile`, `auth.fakes.ts` unused `_client`, an unused import in
   `auth.repository.e2e-spec.ts`; Prettier drift in those F3 files).

## FILES_CREATED

- `tests/e2e/package.json`, `tsconfig.json`, `playwright.config.ts`, `.gitignore`
- `tests/e2e/stack/run-f7.mjs`
- `tests/e2e/support/{env,db,auth,hashes,fixtures,index,auth-flows,users-flows,ui-checks}.ts`
- `tests/e2e/specs/*.spec.ts` (11 files)
- `docs/migration/users/07-HANDOFF.md`

## FILES_MODIFIED

- `apps/api/src/auth/auth.repository.ts` (fixes 1 and 2)
- `apps/web/src/UsersPanel.tsx`, `apps/web/src/PeoplePanel.tsx` (fix 3)
- `apps/api/test/auth.repository.e2e-spec.ts`, `auth.fakes.ts`, `users.policy.e2e-spec.ts`,
  `auth.postgres.integration.e2e-spec.ts`, `auth.bootstrap.postgres.integration.e2e-spec.ts`
- `apps/web/src/UsersPanel.test.tsx`
- `tests/e2e/README.md` (E2E active; history kept)
- `pnpm-lock.yaml` (`@lu/e2e` dependencies)
- `docs/migration/users/00-PARITY-MATRIX.md`

`apps/api/src/users/user.policy.ts` was restored to its pre-F7 text (the prepared
read-access change was reverted; see OPEN DECISION).

## PARITY MATRIX

PARITY 98, PARTIAL 26, MISSING 6, CONFLICT 15, NOT_APPLICABLE 13, UNKNOWN 0 — total 158
(direct recount). Transitions: P-02 and P-03 MISSING → PARITY, P-05 MISSING → PARTIAL
(concurrent editing still without executable test). See "Estado F7".

## F8_READY

YES — with the OPEN DECISION and the new gaps above as explicit F8 inputs.

## F8_REQUIRED_INPUTS

1. This handoff, `06-HANDOFF.md`, `05-HANDOFF.md`
2. `00-PARITY-MATRIX.md` ("Estado F7")
3. `tests/e2e/README.md` and `tests/e2e/stack/run-f7.mjs` to re-run the evidence
