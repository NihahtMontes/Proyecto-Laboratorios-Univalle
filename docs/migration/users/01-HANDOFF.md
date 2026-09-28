# MIG-001 F1 — Identity & Data Contract handoff

## PHASE

F1 — IDENTITY & DATA CONTRACT

## STATUS

`COMPLETE`

The contract passed independent authentication/security and data-integrity
review after all MUST_FIX findings were resolved. Repository validations and
their actual scope are recorded below.

## SOURCE_SHA

`dccabf50330afc48760d06bd4dbaff8c37ebbd3f`

Source ref: `reference/asp-final`.

## TARGET_START_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`

## TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (verified at close; no commit).

## INPUT_ARTIFACTS

- `docs/migration/users/00-SOURCE-SPEC.md`
- `docs/migration/users/00-PARITY-MATRIX.md`
- `docs/migration/users/00-MASTER-PLAN.md`
- `docs/migration/users/00-HANDOFF.md`
- `docs/PLAN_MIGRACION_REACT_NESTJS.md` (binding decisions in section 2)
- `docs/CONTRATO_MULTISEDE.md`
- directed read-only evidence from legacy Identity and current target
  auth/users/people/migrations/registry

## DECISIONS_FROZEN

`01-DECISIONS.md` freezes F1-D001 through F1-D022:

1. UUID and permanent legacy crosswalk.
2. Complete canonical user model.
3. Split names and generated full name.
4. Shared username/email login namespace.
5. Bounded legacy verifier with rehash-on-login.
6. Unified UTF-8-byte password policy.
7. Global/site role mapping.
8. SuperAdmin lifecycle and last-active invariant.
9. Global-account and active-membership self-protection.
10. Account and membership statuses.
11. Site revoke/global soft-delete semantics.
12. Security-version/session invalidation matrix.
13. UUID actor audit and append-only events.
14. Private global profile-picture storage.
15. User and Person remain independent.
16. Persons tab reuse/RBAC/deleted-filter boundary.
17. Separate admin Details and self Profile routes.
18. Explicit global/active-site operation scopes.
19. Global identity ownership and custodial edit rule.
20. Deterministic conceptual data mapping.
21. Single-writer cutover.
22. Separate pinned control-plane/tenant migration streams.

Decision statuses: **22 DECIDED, 0 DEFERRED, 0 BLOCKED**.

## CANONICAL_USER_FIELDS

Global user fields:

`id`, `username`, `email`, `first_name`, `last_name`,
`second_last_name`, generated `full_name`, derived `initials`,
`identity_card`, `phone_number`, `profile_picture_key`, `account_status`,
`is_super_admin`, `password_hash`, `password_scheme`,
`must_change_password`, `password_migrated_at`, `security_version`,
`row_version`, `created_at`, `created_by_user_id`, `updated_at`,
`modified_by_user_id`.

Related canonical records: two login identifier claims, zero/one or more site
memberships, permanent legacy crosswalk when applicable, persisted sessions and
append-only identity/security audit events.

## GLOBAL_FIELDS

UUID, username, email, split/derived name, CI, phone, photo, password state,
account status, global SuperAdmin, security/concurrency versions and user audit.
Self may edit only the permitted profile subset. A site Administrator can edit
global identity only when transactionally verified as the sole non-revoked-site
custodian. SuperAdmin owns privileged global administration. Username is
immutable in normal operation.

## SITE_FIELDS

Membership site ID, role, status, validity, position, department, hire date,
membership row version and membership audit. Site Administrators operate only
on the active-site membership; SuperAdmin uses an explicit global membership
operation. User self-protection applies to the active membership.

## ROLE_MAPPING

| LEGACY | GLOBAL | PILOT SITE |
|---|---|---|
| Supervisor | none | Supervisor |
| Administrador / Identity Administrator | none | Administrador |
| SuperAdmin | SuperAdmin | Administrador |

Site roles never contain SuperAdmin. At least one active global SuperAdmin must
remain. Site APIs never accept the global-role flag.

## STATUS_MAPPING

| LEGACY | ACCOUNT | MEMBERSHIP |
|---|---|---|
| Activo | active | active |
| Inactivo | inactive | suspended |
| Eliminado | deleted | revoked |

All deletion is logical. Default lists exclude deleted/revoked; explicit
filters and administrative detail can retrieve them. Restoration reuses the
same UUID/crosswalk. A site Administrator can restore a non-SuperAdmin
active-site membership and the sole-site account deletion; SuperAdmin restores
global/SuperAdmin accounts and explicitly selected memberships. Stored role and
work data remain, expired validity requires new bounds, and every restore
rotates/revokes/audits.

## LOGIN_POLICY

The request field is `loginIdentifier` and accepts username or email. Both are
required, normalized case-insensitively and stored in one globally unique
cross-kind claim namespace. Username is immutable; email is governed mutable.
No username(A)/email(B) ambiguity is permitted.

New/change/reset passwords require at least 12 Unicode code points, at most 72
UTF-8 bytes, upper/lower/digit/non-alphanumeric and four distinct code points.
Passwords are not trimmed or normalized.

Valid imported legacy passwords at or below 72 bytes are grandfathered on
successful proof even if they do not satisfy new composition. New/admin-reset
or `must_change_password` credentials issue only a password-change session
until a compliant self-selected secret is stored. `reset_required` cannot
authenticate before administrative provisioning.

## PASSWORD_MIGRATION_POLICY

Valid Identity V2/V3 hashes are copied opaquely with an explicit scheme. A
self-contained Node verifier validates them and rehashes to current-cost bcrypt
after successful proof of the plaintext password. A legacy password over 72
bytes enters a restricted password-change flow. Invalid hashes require reset.
New/admin-reset credentials are bcrypt and require first-login change.

Legacy verification ends no later than 90 calendar days after cutover; all
remaining legacy states then atomically become reset-required with null hash,
version rotation, session revocation and audit. `cutover_at` and the exact UTC
deadline are immutable control-plane migration-state values; login fails closed
at the boundary even before the batch. No hash-to-hash conversion and no
post-cutover .NET dependency exist.

## SESSION_INVALIDATION_POLICY

Rotate global `security_version` and revoke subject sessions on email/username,
password, global role, site role, membership eligibility/status, account
status, delete/revoke and legacy/bcrypt-cost rehash. Self email/password changes
replace the current session and revoke others. Cosmetic name/phone/CI/photo and
work-profile-only changes do not rotate. Sensitive rules win in mixed updates.

Normal tenant requests revalidate account, session version/expiry, active site,
site state, membership status and validity. A SuperAdmin may hold a null-site
session only for allowlisted global/auth/Profile routes. CSRF-protected site
change locks/revalidates and updates only the current session, emits audit and
does not rotate the user's security version or other sessions.

Initial site resolution after valid credentials is fixed: zero eligible sites
denies a non-SuperAdmin; one is auto-selected; multiple produce a 15-minute
`site_selection` session limited to auth/session/CSRF/site selection/logout.
Successful CSRF-protected choice revokes it and issues a normal site session.
A SuperAdmin may instead hold a normal null-site session for allowlisted global
routes.

## AUDIT_POLICY

User and membership rows keep created/modified UTC timestamp and actor UUID.
Actors are nullable only for labeled system/bootstrap/import actions and use
`ON DELETE RESTRICT`. Legacy actor ints resolve through the permanent crosswalk;
an unresolved non-null actor is a migration error. Sensitive operations also
write append-only events with actor, subject, optional site, action, reason,
correlation and timestamp, never credentials or hashes.

## PHOTO_POLICY

Private global object storage; development may use an untracked filesystem
adapter. PostgreSQL stores an opaque server-generated key. Accept only
JPG/JPEG/PNG/WEBP up to 5 MiB after extension, MIME and signature validation.
Serve via authorized API/short-lived response. Replacement commits the new key
before controlled old-object cleanup; missing photo uses initials.

## USER_PERSON_RELATIONSHIP

None. User and Person are independent aggregates; either can exist without the
other. Legacy contains no FK/navigation/promotion operation. F5 reuses the
People API/panel as the second `/Users/Index` tab and does not duplicate data.
Tenant People CRUD requires active-site Administrador; global SuperAdmin alone
does not bypass tenant membership.

## ADMIN_DETAIL_ROUTE_POLICY

- Admin detail: `/Users/Details/:userId`.
- Self profile: `/Profile`.
- Temporary compatibility: query-ID redirects to the admin path; no-ID
  `/Users/Details` redirects to `/Profile`.
- Topbar uses `/Profile`; row Details action uses the admin path.

## LEGACY_ID_CROSSWALK

Permanent `lu_legacy_user_xref` with unique `(source_system, legacy_user_id)`
and `(source_system, user_id)`, migration run/fingerprint and UTC timestamp.
It is allocated before actor mapping and retained after cutover. Target-native
users have no row.

## F2_DATABASE_REQUIREMENTS

1. Do not edit migrations 0001–0005 or tenant 0001–0012.
2. Create additive control-plane migration(s) implementing all canonical user,
   membership, login-claim, crosswalk, actor-audit, row-version, status and
   password-state constraints.
3. Enforce UUID/cross-kind identifier/CI/membership uniqueness in PostgreSQL,
   not only application pre-checks.
   Treat user username/email as canonical and make exactly one matching claim of
   each kind a database-enforced, non-directly-writable projection.
4. Make full name database-generated/non-writable or expose an equivalent
   generated projection; split names are authority.
5. Use `ON DELETE RESTRICT` for user, actor, membership, crosswalk and session
   identity relationships; no user hard delete.
6. Support account `active,inactive,deleted` and membership
   `active,suspended,revoked` separately.
7. Preserve current target data through an explicit backfill/reconciliation
   plan; do not infer split names silently for authoritative migration data.
8. Add append-only identity audit storage or a demonstrably equivalent extension
   of the existing security event model.
9. Add session purpose/capability for `normal`, `password_change` and
   `site_selection` and
   immutable identity migration state (`cutover_at`, legacy password deadline).
10. Implement separate pinned `control-plane` and `tenant` registry/manifests,
   registering existing files by exact current hash and serializing tenants by
   site. Do not put tenant tables in the flat control-plane manifest.
11. Produce tests for checks, claim existence/mirroring, unique/cross-kind
    constraints, FKs, soft-delete
    history and registry rejection/tamper behavior.
12. Do not load production/legacy users in F2 unless a separately authorized
    data-run plan is approved.
13. Local PostgreSQL credentials are provisioned outside tracked files.

## F3_BACKEND_REQUIREMENTS

1. Resolve login by the shared username/email claim namespace with generic
   failures and existing rate-limit protections.
2. Implement Identity V2/V3 Node verification, locked rehash-on-login,
   exact bounded parser, reset-required, restricted password-change sessions and
   fail-closed 90-day retirement without invoking .NET.
3. Apply one password validator using Unicode code points and UTF-8 bytes.
4. Separate global user commands from site-membership commands; remove the
   current unconditional mixed-scope update.
5. Implement custodial single-site checks transactionally.
6. Enforce self, SuperAdmin and last-active-SuperAdmin rules server-side under
   concurrency.
7. Centralize the session invalidation matrix and fix self-profile email
   rotation.
8. Preserve actor/time audit and append sensitive events.
9. Implement profile-photo abstraction/validation/serving/cleanup.
10. Make global Profile and SuperAdmin user administration available without an
    active site; keep tenant/People operations behind eligible active-site
    membership.
    Site selection/change must be CSRF-protected and every tenant request must
    revalidate session, site, membership status and validity.
11. Fix People explicit deleted filtering for the reused tab; do not redesign
    the rest of Persons.
12. Expose admin Details and self Profile data separately.
13. Implement deterministic zero/one/many initial membership resolution,
    restricted selection-session expiry/capabilities and normal-session
    replacement.

## UNRESOLVED

`NONE` for the decisions required by MIG-001 F1.

Out-of-scope work is not an unresolved F1 contract: full Persons subtype
parity, general infrastructure choice of production object-storage vendor,
production deployment topology and the execution date of cutover remain owned
by their respective phases/runbooks. Their interfaces required by Users are
frozen here.

## FILES_CREATED

- `docs/migration/users/01-IDENTITY-CONTRACT.md`
- `docs/migration/users/01-DECISIONS.md`
- `docs/migration/users/01-DATA-MAPPING.md`
- `docs/migration/users/01-HANDOFF.md`

## FILES_MODIFIED

- `docs/migration/users/00-PARITY-MATRIX.md` (F1 decision linkage, justified
  status/owner corrections only)

## VALIDATION

- Four required `01-*.md` artifacts: present (glob/read verification).
- Required handoff headings: 29/29 present. Required identity definitions and
  complete field-table header: present.
- Independent critical auth/security review: `APTO`, no remaining MUST_FIX.
- Independent data-integrity review: `APTO`, no remaining MUST_FIX.
- Coherence review: contract, 22-decision log, field mapping/state machine and
  this handoff agree on IDs, fields, scopes, roles, statuses, passwords,
  sessions, audit, photos, User/Person and cutover.
- Parity matrix automated count (Node fallback because `rg` and Python are not
  installed): `PARITY=29`, `PARTIAL=32`, `MISSING=47`, `CONFLICT=37`,
  `NOT_APPLICABLE=13`, `UNKNOWN=0`, total `158`. F1 does not mark an
  unimplemented gap as parity.
- Secret-pattern scan over all four F1 files: no database URL, connection
  string, credential assignment or environment-secret assignment match. No
  supplied password was copied. Only the approved statement that local
  credentials are provisioned outside tracked files appears.
- `git status --short -- apps packages Pages Models Services Data
  apps/api/migrations`: no output. No product code or migration changed.
- `git diff --check`: no whitespace errors; Git emitted LF→CRLF warnings only
  for pre-existing tracked OpenCode configuration changes.
- The F0/F1 migration documents are currently untracked as one
  `docs/migration/` tree, so normal `git diff` cannot display them. Each F1 file
  and the matrix passed `git diff --no-index --check`; normal `git diff` was
  reviewed and contains only pre-existing `.opencode/**`/`opencode.json`
  changes outside MIG-001 F1.
- Final branch/HEAD: `migration/react` at
  `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`; no commit.
- Final status retains the pre-existing `.opencode/**` and `opencode.json`
  changes plus untracked `.opencode/agents/migration-worker.md` and
  `docs/migration/`. No unrelated change was reverted.
- No build, test suite, PostgreSQL connection, migration, server, browser or
  data operation was executed; none is required for this documentation-only F1.

## DO_NOT_REOPEN_IN_F2

- UUID is canonical; crosswalk is permanent.
- Username and email share one collision-free login namespace.
- Split names are authority; full name is generated.
- Canonical fields and global/site ownership.
- Password state machine, 72-byte policy and 90-day compatibility limit.
- Role mapping and protected SuperAdmin lifecycle.
- Account/membership status model and delete/revoke semantics.
- Session invalidation matrix.
- Actor audit and profile-picture interface.
- No User↔Person relation; People tab is composition.
- Admin Details/Profile routes and operation-scope matrix.
- Single-writer cutover and separate migration streams.

F2 may report an implementation blocker with evidence; it may not silently
reinterpret these decisions.

## F2_REQUIRED_INPUTS

1. `docs/migration/users/00-SOURCE-SPEC.md`
2. `docs/migration/users/00-PARITY-MATRIX.md`
3. `docs/migration/users/01-IDENTITY-CONTRACT.md`
4. `docs/migration/users/01-DECISIONS.md`
5. `docs/migration/users/01-DATA-MAPPING.md`
6. this handoff
7. current immutable migrations and migration registry/runner/tests
8. `docs/CONTRATO_MULTISEDE.md`

## F2_OBJECTIVE

Design and implement the additive PostgreSQL schema and the separate pinned
control-plane/tenant migration-manifest path required by this F1 contract,
verify it in the authorized local environment, and produce evidence. F2 does
not reinterpret identity and does not automatically execute the legacy user
data migration.
