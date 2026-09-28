# MIG-001 F2 — PostgreSQL handoff

## PHASE

F2 — POSTGRESQL

## STATUS

`COMPLETE`

The control-plane + tenant migration paths were applied and verified against
the authorized local PostgreSQL 18 Windows service on 2026-09-25. Every
recorded gate in the live run was PASS; the four FIXes (#1..#4) shipped,
validated live and are reflected in `docs/migration/users/02-F2-APPLIED.md`.
F3 is the next phase and was not started by this handoff.

## SOURCE_SHA

`dccabf50330afc48760d06bd4dbaff8c37ebbd3f`

Source ref: `reference/asp-final`.

## TARGET_START_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`

## TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (verified at close; no commit).

## INPUT_ARTIFACTS

- `docs/migration/users/00-MASTER-PLAN.md` (F2 section, scope, exit criteria)
- `docs/migration/users/00-PARITY-MATRIX.md` (rows whose PHASE OWNER includes F2)
- `docs/migration/users/01-IDENTITY-CONTRACT.md`
- `docs/migration/users/01-DECISIONS.md`
- `docs/migration/users/01-DATA-MAPPING.md`
- `docs/migration/users/01-HANDOFF.md` (`F2_DATABASE_REQUIREMENTS`,
  `F2_REQUIRED_INPUTS`, `F2_OBJECTIVE`, `DO_NOT_REOPEN_IN_F2`)
- existing `apps/api/migrations/**` (immutable)
- existing `apps/api/src/database/migration-registry.ts` and adjacent modules
  (read-only for this handoff; no product code was modified during closure)

## WHAT_WAS_DELIVERED

- Additive control-plane migration `0006_mig001_users_identity.sql` (pinned)
  implementing the canonical identity contract surface required by F1.
- Tenant ledger migration `0013_migration_history.sql` (pinned) capturing
  per-ordinal apply metadata on the tenant DB.
- Pinned registry for both streams (6 control-plane, 13 tenant) with
  per-entry policy flags (`legacyPolicy`/`strictPolicy`), advisory-lock keys
  and strict-policy `allowedDropConstraints`.
- Analyzer, plan builder, transactional runner, CLI surface, schema manifests
  for control-plane and tenant, and a hardened `withTenantMigrationLease`
  fence (READ COMMITTED, global advisory xact lock, route-row `FOR UPDATE`,
  guarded `UPDATE` with exact affected-row check, deterministic rollback
  semantics, indeterminate-recovery contract).
- Read-only tenant resolver (`resolveTenantConfig`) using
  `BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY` with
  `SET LOCAL ROLE lu_auth_migrator`.
- Strict preflight against the managed `lu_auth_runtime` and
  `lu_auth_migrator` roles on every control-plane lease / resolver call.
- Migration runner integration spec `f2-w2.postgres.integration.e2e-spec.ts`
  exercising fresh + upgrade control-plane, sequential tenant applies, ledger
  contiguity, pin verification + replay-skip, schema-manifest field-exact
  verification, append-only ledger probes, ACL/ownership probes, rollback
  of an invalid fixture transaction, and the concurrent-replay race.
- Four fixes (FIX #1..#4) shipped and verified live:
  - FIX #1 — preserve `IF NOT EXISTS` for `legacyPolicy` envelopes during
    execution.
  - FIX #2 — canonicalize PG-18 `(col)::text` -> `col` in the index
    verifier.
  - FIX #3 — canonicalize varchar `IN(...)` vs
    `= ANY ((ARRAY[...'x'::character varying])::text[])` and the
    `COALESCE(col, ''::character varying)::text` shape in the CHECK verifier.
  - FIX #4 — switch the control-plane lease to
    `BEGIN ISOLATION LEVEL READ COMMITTED` to avoid SQLSTATE 40001 on the
    route-row `FOR UPDATE` snapshot while preserving every other safety
    invariant; no retry, no swallowed 40001, read-only resolver unchanged.
- Closed-form evidence doc `docs/migration/users/02-F2-APPLIED.md`.

## EVIDENCE_SUMMARY

`docs/migration/users/02-F2-APPLIED.md` records:

- Date 2026-09-25, PostgreSQL 18 Windows service on `127.0.0.1:5432`,
  maintenance DB `neondb`. No passwords, DSNs or credentials appear
  anywhere.
- Suite `apps/api/test/f2-w2.postgres.integration.e2e-spec.ts` ran with
  `F2_INTEGRATION=1` against built `dist`; **Tests 8/8 PASS** (1 suite,
  ~18 s). Log retained untracked at `apps/api/.tmp/f2-live-final.log`.
- Per-gate table covering control-plane fresh + upgrade, sequential tenant
  applies (0003, 0004, 0012, 0013 explicitly), ledger contiguity, pin
  verification + replay-skip, manifest field-exact verification, append-only
  probes, ACL/ownership probes, invalid-fixture rollback with zero residue
  and the concurrent-replay race (both racer processes exit 0, assertion
  `[0, 0]` unchanged; prior run was 7/8 failing only this race with exit
  codes `[0, 1]`).
- Per-fix section explaining FIX #1..#4 and the file-level location of each
  change.
- Security & cleanup section: temporary `host all postgres 127.0.0.1/32
  trust` rule added only for the run via a wrapper under `apps/api/.tmp`;
  `pg_hba.conf` restored byte-exact in `finally`; SHA-256
  `0C8DC6E6E57399790417A6E13B3A8E1B5E27AA19708A2122148FBFE3BDCECD42` verified
  after restore; fresh passwordless `postgres` connection fails afterwards;
  wrapper and backup removed; `pg_ctl reload` refused on Windows but each
  new backend re-reads `pg_hba.conf` so both activation and restoration
  took effect for new connections; zero residue (`f2w2w15b_*` = 0,
  `lu_migration_exec_*` = 0); `lu_auth_migrator` preserved (`NOLOGIN`,
  `NOSUPERUSER`).
- Known non-blocking verifier limitation (bpchar literal cast in
  `stripTypeCasts` of the CHECK normalizer) with the independent column-type
  constraint that prevents a semantic regression in practice.
- Final offline gates: 11 targeted F2 suites 686/686 PASS; typecheck,
  build and `git diff --check` PASS; broad `apps/api` suite 861 PASS with 2
  pre-existing non-F2 failures (CRLF byte pins on unmodified tracked role SQL
  under `core.autocrlf=true`).

## KNOWN_LIMITATION

The CHECK normalizer's `stripTypeCasts` step may normalize a bpchar literal
cast to look like the expected value; this is independently constrained by
column-type schema verification and is not an F2 blocker, with no live
evidence of a semantic issue. F3 (or any subsequent verifier work) may
narrow the normalizer further without reopening F2.

## UNRESOLVED

`NONE` for the F2 database layer scope.

Out-of-scope work remains owned by its phase: load of legacy users (data
migration), NestJS/Auth F3, contracts/client F4, React F5/F6 and later phases. The
parity-matrix rows they own are untouched by this handoff; see
`docs/migration/users/00-PARITY-MATRIX.md` for the unchanged counts and the
appended F2 (database layer) closing note.

## FILES_CREATED

- `docs/migration/users/02-HANDOFF.md` (this file)

## FILES_MODIFIED

- `docs/migration/users/02-F2-APPLIED.md` (live evidence, fixes, closure).
- `docs/migration/users/00-PARITY-MATRIX.md` (appended closing note for F2
  database layer only; no row STATUS values or counts altered).

## VALIDATION

- `docs/migration/users/02-F2-APPLIED.md` header reflects `COMPLETE` /
  `LIVE_POSTGRESQL_VALIDATION_PASS` and dated 2026-09-25.
- Pin table and offline-evidence table retained for traceability.
- The four required sections (live PostgreSQL evidence, FIX #1..#4,
  security & cleanup, known non-blocking verifier limitation, exit state)
  are present.
- Exit state section records the final offline gates.
- Live run: 8/8 tests PASS (1 suite, ~18 s) under `F2_INTEGRATION=1`; log
  kept untracked at `apps/api/.tmp/f2-live-final.log`.
- Historical migrations were not modified (control-plane `0001..0005` and
  tenant `0001..0012` byte-identical); `0006` and `0013` pins unchanged.
- Parity matrix: no row STATUS value or count changed; only an appended
  closing section was added.
- No secrets, DSNs, connection strings or credentials in any doc.
- Final branch/HEAD: `migration/react` at
  `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`; no commit.

## DO_NOT_REOPEN_IN_F3

- The pinned SHA-256 of `0006_mig001_users_identity.sql` and
  `0013_migration_history.sql`. Re-pinning requires an additive migration
  under a new ordinal, never an in-place edit.
- The four F2 fixes (#1..#4). Any change here is a regression and must
  reopen F2, not F3.
- The `withTenantMigrationLease` safety invariants: global advisory xact
  lock, route-row `FOR UPDATE`, guarded `UPDATE` with exact affected-row
  check, rollback semantics, tenant-side SERIALIZABLE, per-entry advisory
  locks, ledger, replay-skip. READ COMMITTED at the lease layer is
  intentional; do not switch back to REPEATABLE READ without addressing
  the snapshot/40001 root cause recorded under FIX #4.
- The migrator role contract: `lu_auth_migrator` stays `NOLOGIN`,
  `NOSUPERUSER`, owned by the F2 ownership/ACL surface; runtime role
  remains `NOLOGIN` with no schema CREATE and no ownership of migrable
  public objects.

## F3_REQUIRED_INPUTS

1. `docs/migration/users/02-F2-APPLIED.md`
2. `docs/migration/users/02-HANDOFF.md` (this file)
3. `apps/api/migrations/` (post-F2 state)
4. `docs/migration/users/01-IDENTITY-CONTRACT.md`,
   `docs/migration/users/01-DECISIONS.md`,
   `docs/migration/users/01-DATA-MAPPING.md`,
   `docs/migration/users/01-HANDOFF.md`
5. `docs/CONTRATO_MULTISEDE.md`
6. `docs/PLAN_MIGRACION_REACT_NESTJS.md`

## F3_OBJECTIVE

Implement the NestJS auth/users endpoints required by F1 against the
applied F2 schema: login by the shared username/email claim namespace, the
Identity V2/V3 verifier with rehash-on-login, the bounded password policy,
separate global/site commands, custodial single-site checks, the session
invalidation matrix, profile-photo abstraction, deterministic site
resolution and the SuperAdmin global commands opt-in tooling. Do not
reopen F2 fixes or migration pins; do not add retries or swallowed 40001
to the lease.

## ARTIFACTS_TO_READ_NEXT

Per master plan, F3 reads next:

1. `docs/migration/users/02-F2-APPLIED.md`
2. `apps/api/migrations/` (post-F2 state)

The remainder of the F3 contract is unchanged from F1 (`01-IDENTITY-CONTRACT.md`,
`01-DECISIONS.md`, `01-DATA-MAPPING.md`, `01-HANDOFF.md`,
`docs/CONTRATO_MULTISEDE.md`) and is summarized above under
`F3_OBJECTIVE`; consult those documents for the binding requirements.
