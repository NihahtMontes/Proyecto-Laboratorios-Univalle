# MIG-001 F1 — Decision log

**PHASE:** F1 — Identity & Data Contract
**SOURCE SHA:** `dccabf50330afc48760d06bd4dbaff8c37ebbd3f`
**TARGET START SHA:** `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`

Allowed statuses are `DECIDED`, `DEFERRED` and `BLOCKED`. This log contains
**22 DECIDED, 0 DEFERRED and 0 BLOCKED**. A consequence assigned to a later
implementation phase is not a deferred architectural decision.

## F1-D001 — Canonical user ID and legacy crosswalk

- **STATUS:** DECIDED
- **CONTEXT:** Legacy uses `IdentityUser<int>`; the target already uses UUID.
- **EVIDENCE:** `Models/User.cs`; target
  `0001_create_identity_control_plane.sql`; F0 matrix M-04.
- **DECISION:** UUID remains the canonical permanent PK. A permanent
  `lu_legacy_user_xref` maps `(source_system='asp_sqlserver', legacy_user_id)`
  uniquely to UUID and records migration provenance. It also uniquely prevents
  two source IDs in that source from mapping to the same user. PostgreSQL-native
  users have no crosswalk row.
- **RATIONALE:** UUID preserves the approved control-plane design while the
  crosswalk provides lossless historical identity and actor translation.
- **CONSEQUENCES:** F2 creates the crosswalk. F2 migration allocates every UUID
  and crosswalk before resolving self-referential audit actors. The crosswalk is
  not dropped after cutover.
- **AFFECTED PHASES:** F2, F3, F4, F5, data migration/cutover.

## F1-D002 — Canonical user model and field disposition

- **STATUS:** DECIDED
- **CONTEXT:** Current `lu_user` omits visible legacy identity, work and audit
  fields.
- **EVIDENCE:** `Models/User.cs`; F0 Source Spec section 6.1; current
  `packages/contracts/src/users.ts` and target migration 0001.
- **DECISION:** Preserve username, split name, CI, phone, photo and complete
  audit summary as global user data. Move position, department and hire date to
  site membership. Preserve password state/security version and introduce row
  versions. Treat Identity confirmation/2FA/lockout counters as not required or
  legacy-only exactly as classified in the identity contract.
- **RATIONALE:** No ASP-visible functional field is lost, while work attributes
  can differ in a multi-site future.
- **CONSEQUENCES:** F2 extends control-plane structures; F3/F4 expose the full
  model; F5 restores the five administrative screens. No User fields are moved
  into tenant `Person`.
- **AFFECTED PHASES:** F2–F5, F7.

## F1-D003 — Split names are canonical; full name is generated

- **STATUS:** DECIDED
- **CONTEXT:** ASP stores three name parts and derives `FullName`; current target
  persists only `full_name`.
- **EVIDENCE:** `Models/User.cs`; Create/Edit/Details/Index behavior in F0.
- **DECISION:** Persist `first_name`, `last_name`, `second_last_name`; derive
  `full_name` by joining normalized parts. A physical materialization is allowed
  only as a database-generated column/projection and is never client-writable.
  Initials are API-derived.
- **RATIONALE:** All legacy forms remain possible and no two writable name
  representations can diverge.
- **CONSEQUENCES:** F2 must replace the writable target full-name authority.
  F4 exposes split and derived forms; F5 edits split fields and displays the
  derivative.
- **AFFECTED PHASES:** F2, F3, F4, F5.

## F1-D004 — Username, email and unambiguous login identifier

- **STATUS:** DECIDED
- **CONTEXT:** ASP login accepts username or email; target accepts email only.
- **EVIDENCE:** `Pages/Login.cshtml.cs` dual lookup; target
  `auth.repository.findUserWithMembershipsByEmail`; F0 A-01/N-07.
- **DECISION:** Username and email are both required, globally
  case-insensitive unique identifiers. Username is immutable after creation;
  email is governed mutable. Both are normalized by one server function and
  claimed in a shared `lu_login_identifier` namespace. Login accepts
  `loginIdentifier` and performs one exact claim lookup. Cross-kind collisions
  block import or write. User columns are canonical and the claim table is a
  database-maintained projection with exactly one row of each kind per user;
  neither F3 nor migration writes claims independently. Existing target users
  are not auto-matched by email.
- **RATIONALE:** This preserves visible ASP behavior and eliminates lookup-order
  ambiguity such as username(A) equaling email(B).
- **CONSEQUENCES:** F2 creates identifier claims/constraints; F3 implements
  atomic writes and dual login; F4 renames the request property; F5 labels the
  input “Usuario o Correo”.
- **AFFECTED PHASES:** F2–F5, F7.

## F1-D005 — Bounded legacy-password compatibility and rehash

- **STATUS:** DECIDED
- **CONTEXT:** Hash-to-hash conversion from ASP.NET Identity PBKDF2 to bcrypt is
  impossible, but forcing every account to reset would unnecessarily break
  existing credentials.
- **EVIDENCE:** No `PasswordHasherCompatibilityMode` override exists in the
  legacy ref; .NET 9 default is Identity V3 (`0x01`, PBKDF2-HMAC-SHA512,
  100,000 iterations) and its verifier also recognizes V2 (`0x00`). Target uses
  `bcryptjs@3.0.3`, default cost 12.
- **DECISION:** Import valid hashes with explicit
  `legacy_identity_v2`/`legacy_identity_v3` state. NestJS verifies the documented
  binary formats and bounded parameters defined in the identity contract and,
  after success, writes bcrypt and changes state to
  `bcrypt` in one locked transaction before issuing a session. Malformed or
  unsupported hashes become `reset_required`. Admin/self reset writes bcrypt.
  Valid legacy states start with no forced change; invalid states require one.
  Compatibility ends at the immutable UTC deadline exactly 90 calendar days
  after recorded cutover; a fail-closed login check and idempotent retirement
  batch null remaining hashes, require reset, rotate/revoke and disable the
  verifier.
- **RATIONALE:** Users retain valid passwords, no conversion is invented, no
  runtime .NET dependency remains and compatibility cannot become permanent.
- **CONSEQUENCES:** F2 stores password state. F3 implements format parsing,
  constant-time failure behavior, rehash concurrency and the deadline
  transition. F5 provides first-login/change/reset UX.
- **AFFECTED PHASES:** F2, F3, F4, F5, F7, cutover.

## F1-D006 — One password policy measured in UTF-8 bytes

- **STATUS:** DECIDED
- **CONTEXT:** Current create/edit count UTF-16 code units while login enforces
  the bcrypt 72-byte boundary.
- **EVIDENCE:** `user.controller.ts` uses `string.length` 8–72;
  `auth.types.ts` uses `TextEncoder` bytes; legacy effective Identity policy is
  minimum 12 with upper/lower/digit/non-alphanumeric and four unique chars.
- **DECISION:** New/change/reset password is 12 or more Unicode code points,
  at most 72 UTF-8 bytes, with the legacy effective composition requirements
  and four distinct code points. It is never trimmed or normalized. The login
  transport accepts at most 512 bytes for bounded legacy verification; bcrypt
  states reject over 72 bytes. A verified over-72-byte legacy password grants
  only a restricted password-change capability. A proven legacy password at or
  below 72 bytes is grandfathered even if it fails the new composition policy;
  composition applies whenever a new secret is selected. Any bcrypt credential
  marked `must_change_password` also yields only a password-change session.
- **RATIONALE:** No target-created password can become unverifiable, while a
  possible long PBKDF2 legacy credential is not incorrectly rejected before
  migration.
- **CONSEQUENCES:** Shared F3/F4 validation is mandatory for every password
  entry point. Current 8-character target behavior is replaced by the actual
  effective ASP policy.
- **AFFECTED PHASES:** F3, F4, F5, F7.

## F1-D007 — Global and site role mapping

- **STATUS:** DECIDED
- **CONTEXT:** Legacy has one role; target has global SuperAdmin and per-site
  roles.
- **EVIDENCE:** `UserRole` values and Identity role synchronization; approved
  `CONTRATO_MULTISEDE.md` sections 1–2; current `is_super_admin` and membership
  role checks.
- **DECISION:** Supervisor maps to no global role + pilot Supervisor;
  Administrador/Identity Administrator maps to no global role + pilot
  Administrador; SuperAdmin maps to global SuperAdmin + pilot Administrador.
  Future roles are explicit memberships. Display role in a site is SuperAdmin
  when the global flag is true, otherwise the site role, while Details exposes
  both.
- **RATIONALE:** The local pilot preserves ASP permissions and the target keeps
  correct multi-site dimensions.
- **CONSEQUENCES:** Source enum and Identity-role mismatch is a migration blocker,
  not silently resolved. F3 authorization distinguishes global control-plane
  actions from tenant business actions.
- **AFFECTED PHASES:** F2–F5, F7, migration.

## F1-D008 — SuperAdmin lifecycle and last-active invariant

- **STATUS:** DECIDED
- **CONTEXT:** Current target blocks every SuperAdmin update and does not enforce
  exactly the legacy last-active protection.
- **EVIDENCE:** Legacy Edit/Delete checks; target repository condition
  `is_super_admin=false`; F0 F-08/H-06/K-02.
- **DECISION:** Site admins can see an in-site SuperAdmin but cannot mutate it.
  An active SuperAdmin may use a privileged audited global command to create,
  grant, revoke or edit SuperAdmin accounts; general Users HTTP payloads cannot
  set the global flag. The initial bootstrap is the only zero-SuperAdmin system
  exception. At least one `is_super_admin=true AND account_status='active'`
  must remain; membership is irrelevant to the count. Enforce under a
  serialized/locked transaction.
- **RATIONALE:** It preserves the legacy protections without exposing a global
  privilege as a site field or permanently freezing all SuperAdmin profiles.
- **CONSEQUENCES:** F3 supplies the opt-in command/global service and tests race
  conditions. F5 displays protected rows but no site-role control for the
  global flag.
- **AFFECTED PHASES:** F2, F3, F5, F7.

## F1-D009 — Self-protection is account- and membership-aware

- **STATUS:** DECIDED
- **CONTEXT:** A global account and its active-site membership are separate
  objects; legacy self-protection used one monolithic user row.
- **EVIDENCE:** Legacy self role/status/delete checks; current active-site
  context contract.
- **DECISION:** A user cannot inactivate/delete their account, clear their own
  SuperAdmin role, or change/suspend/revoke the membership of their active site.
  Self-service can edit email, names, phone, photo and password only. A
  SuperAdmin may alter a non-active membership through the global operation,
  subject to last-active and audit rules.
- **RATIONALE:** This prevents immediate self-lockout while allowing legitimate
  management of memberships in a multi-site model.
- **CONSEQUENCES:** F3 enforces, F5 hides/disables but never replaces server
  checks, and F7 covers both global-account and active-membership cases.
- **AFFECTED PHASES:** F3, F4, F5, F7.

## F1-D010 — Three account states and three membership states

- **STATUS:** DECIDED
- **CONTEXT:** Legacy has Activo/Inactivo/Eliminado; current account target has
  active/disabled and membership has active/suspended/revoked.
- **EVIDENCE:** `GeneralStatus`; migration 0001 constraints; F0 C-04/H-02.
- **DECISION:** Account states are `active,inactive,deleted`; membership states
  remain `active,suspended,revoked`. Legacy maps respectively to
  active/active, inactive/suspended, deleted/revoked. Default lists omit
  deleted/revoked; explicit filters include them. Restoration uses the same
  identity and is explicit. A site Administrator may restore a non-SuperAdmin
  active-site membership and, only for the sole-site deletion case, atomically
  reactivate its account. SuperAdmin handles global/SuperAdmin restoration and
  explicitly chooses memberships; expired validity is never extended silently.
  Restoration rotates/revokes and audits.
- **RATIONALE:** Inactive and logical deletion remain observably distinct while
  site access can change without necessarily disabling a global account.
- **CONSEQUENCES:** F2 adds the third account state; F3 computes effective site
  status; F4/F5 expose separate data while rendering ASP-equivalent labels.
- **AFFECTED PHASES:** F2–F5, F7, migration.

## F1-D011 — Delete means site revoke or explicit global soft delete

- **STATUS:** DECIDED
- **CONTEXT:** ASP Delete preserves history and revokes all access; the target
  must account for users with other sites.
- **EVIDENCE:** Legacy `Delete.cshtml.cs`; no-hard-delete policy; approved
  multi-site architecture.
- **DECISION:** Site Delete revokes the active-site membership. If no other
  non-revoked membership remains and the user is not SuperAdmin, it also marks
  the account deleted, reproducing the pilot. Otherwise the account remains.
  SuperAdmin may invoke an explicit global soft delete that revokes all
  memberships. Every path audits, rotates security version and revokes sessions;
  self and last-SuperAdmin protections apply.
- **RATIONALE:** Sede A cannot destroy Sede B access, but the one-site observable
  result remains the ASP result.
- **CONSEQUENCES:** F3 needs separate site/global commands. F5 labels the normal
  action “Revocar acceso” and communicates its scope.
- **AFFECTED PHASES:** F2–F5, F7.

## F1-D012 — Security-version and session invalidation policy

- **STATUS:** DECIDED
- **CONTEXT:** ASP rotates SecurityStamp on every admin Edit; current profile
  email update does not rotate target security version.
- **EVIDENCE:** Legacy `UpdateSecurityStampAsync`; target
  `user.repository.update` versus `updateProfile`; session version checks.
- **DECISION:** Rotate for email/username/password, global role, site role,
  membership eligibility/status, account status and delete/revoke. Revoke
  persisted subject sessions in the same security operation. On self email or
  password change, replace the current session after success and revoke others;
  admin changes revoke all. Do not rotate for names, phone, CI, photo or work
  profile alone. Bcrypt cost/legacy rehash rotates before issuing the new
  session.
- **RATIONALE:** All authorization and credential changes invalidate stale
  sessions, while cosmetic updates no longer cause needless logout. The
  intentional difference from “every ASP Edit” is explicit.
- **CONSEQUENCES:** F3 centralizes mutation/invalidation and fixes
  `updateProfile`. F7 tests every matrix row.
- **AFFECTED PHASES:** F3, F4, F5, F7.

## F1-D013 — Actor audit is first-class and preserved

- **STATUS:** DECIDED
- **CONTEXT:** ASP Details shows created/modified actors; target currently stores
  mainly timestamps.
- **EVIDENCE:** `CreatedById/ModifiedById` and navigations in `User`; F0 G-05,
  N-02.
- **DECISION:** User and membership rows store created/modified timestamp and
  actor UUID. Actor FKs are `ON DELETE RESTRICT` and nullable only for labeled
  system/bootstrap/import actions. Sensitive mutations also append an identity
  audit event. Source actor ints map through the permanent crosswalk; unresolved
  non-null actors block reconciliation.
- **RATIONALE:** The ASP-visible audit survives UUID migration and event history
  explains privilege/session changes.
- **CONSEQUENCES:** F2 adds FKs/event storage; F3 always supplies actor and
  correlation; F4/F5 expose actor summaries in Details.
- **AFFECTED PHASES:** F2–F5, F7, migration.

## F1-D014 — Profile pictures use private global object storage

- **STATUS:** DECIDED
- **CONTEXT:** ASP stores validated files under `wwwroot/uploads/users`; target
  has no photo model or endpoint.
- **EVIDENCE:** `Helpers/SafeImageUpload.cs`; F0 LC-05 and upload inventory.
- **DECISION:** Store private binary objects globally, with an untracked local
  adapter for development. PostgreSQL stores only a server-generated key
  `users/{uuid}/{random}.{ext}`. Preserve 5 MiB and JPG/JPEG/PNG/WEBP constraints,
  validating MIME and signature. Serve through an authorized API/short-lived
  response. Replace after transactional DB update and controlled old-object
  cleanup; use initials when absent.
- **RATIONALE:** A global user needs one photo; private object keys avoid public
  path injection and deployment-coupled URLs.
- **CONSEQUENCES:** F2 stores the key; F3 implements storage abstraction and
  endpoints; F5/F6 restore avatar/preview. Migration copies valid existing
  files and records missing-file exceptions.
- **AFFECTED PHASES:** F2–F6, F7, migration.

## F1-D015 — User and Person remain independent

- **STATUS:** DECIDED
- **CONTEXT:** The Users page visually groups accounts and people and claims
  people can be promoted, but a relation may not exist.
- **EVIDENCE:** `Models/User.cs`, `Models/Person.cs` and DbContext have no
  User→Person FK/navigation; the promotion phrase exists only in
  `Pages/Users/Index.cshtml`.
- **DECISION:** No FK, bridge or automatic promotion is added in MIG-001. A User
  may exist without Person and vice versa. Each aggregate keeps its own contact,
  status and audit data.
- **RATIONALE:** Visual grouping is not a relational contract, and inventing a
  link would create unsupported identity semantics.
- **CONSEQUENCES:** F2 adds no user-person field. F5 composes tabs using the
  existing People capability and removes/rewords promotion copy unless a future
  separately contracted feature exists.
- **AFFECTED PHASES:** F2, F3, F4, F5.

## F1-D016 — Persons tab reuses People API and fixes only required defects

- **STATUS:** DECIDED
- **CONTEXT:** MIG-001 needs observable parity for the embedded directory, not a
  redesign of Persons.
- **EVIDENCE:** Shared legacy partial; current People API/panel; target list
  predicate `status<>2 AND status=2`; People service accepts only site Admin.
- **DECISION:** F5 embeds/reuses People list/actions in `/Users/Index`; no second
  backend. Default list excludes deleted, explicit status `2` returns deleted.
  People CRUD requires active-site Administrador. A SuperAdmin needs an
  Administrador membership for tenant People data. Person subtype states,
  concurrency, import batch and operational histories remain outside MIG-001.
- **RATIONALE:** It restores the tab and corrects a target defect without
  absorbing another vertical into Users.
- **CONSEQUENCES:** F3 fixes filter behavior; F5 tab/routing reuses APIs. The
  out-of-scope Person items remain assigned to their own migration, not
  silently accepted as User gaps.
- **AFFECTED PHASES:** F3, F5, F7; future Persons vertical.

## F1-D017 — Separate administrative details from self profile

- **STATUS:** DECIDED
- **CONTEXT:** Target `/Users/Details` replaced the ASP administrative detail
  with a limited self profile.
- **EVIDENCE:** Legacy Details page and target ProfilePanel routing; F0 G-01,
  J-01.
- **DECISION:** `/Users/Details/:userId` is canonical admin detail and `/Profile`
  is canonical self profile. Query compatibility redirects to the admin path;
  `/Users/Details` without ID redirects to `/Profile` during transition. Topbar
  uses `/Profile`; list action uses admin detail.
- **RATIONALE:** Both capabilities have distinct authorization and neither is
  lost.
- **CONSEQUENCES:** F4 supplies admin/self DTOs; F5 splits routes and panels and
  supports temporary redirects.
- **AFFECTED PHASES:** F4, F5, F6, F7.

## F1-D018 — Operation scope is explicit: global, active site or both

- **STATUS:** DECIDED
- **CONTEXT:** Current site update writes global email/name/status, allowing one
  site administrator to affect all sites.
- **EVIDENCE:** `user.repository.update` has global user update plus one-site
  membership update; approved control-plane/tenant boundary.
- **DECISION:** List/detail/create/edit/delete operate according to the matrix in
  the identity contract. Site admins administer active-site memberships and may
  edit global fields only when they are the target's sole non-revoked-site
  custodian. SuperAdmin has a global control-plane Users view without active
  site; tenant/People access still requires an eligible site. Global and site
  mutations are explicit commands, not one ambiguous status/role update. A
  tenant request revalidates session, active site, membership status/validity
  and site status; changing site is CSRF-protected, updates only the locked
  current session and emits audit. Global routes are separately allowlisted.
  After credentials, zero eligible sites denies a non-SuperAdmin, one is
  auto-selected, and multiple produce a 15-minute restricted site-selection
  session until a CSRF-protected choice creates a normal session. SuperAdmin
  may instead receive a null-site global session.
- **RATIONALE:** Cross-site identity cannot be modified accidentally, while the
  one-site pilot retains Administrador usability.
- **CONSEQUENCES:** F3 removes unconditional mixed-scope updates; F4 contracts
  represent account and membership changes; F5 communicates active scope.
- **AFFECTED PHASES:** F3, F4, F5, F7.

## F1-D019 — Global identity ownership and email-edit rule

- **STATUS:** DECIDED
- **CONTEXT:** Email and other current `lu_user` data affect every site.
- **EVIDENCE:** Global `lu_user` schema and globally unique email; F0 cross-site
  risk.
- **DECISION:** Username, email, names, CI, phone, photo, password and account
  status are global. Role/status/validity and work profile are membership data.
  Self may edit the permitted global subset; SuperAdmin may edit privileged
  global fields; a site admin may do so only as transactional single-site
  custodian. Username stays immutable.
- **RATIONALE:** The owner/global administrator controls shared identity; the
  custody exception preserves local ASP administration without granting a
  multi-site side effect.
- **CONSEQUENCES:** F3 counts non-revoked memberships inside the write
  transaction. Multi-site conflicts return a clear authorization/scope error.
- **AFFECTED PHASES:** F2–F5, F7.

## F1-D020 — Conceptual legacy-to-target data migration

- **STATUS:** DECIDED
- **CONTEXT:** F2 needs a loss-aware map before SQL or data loading exists.
- **EVIDENCE:** Complete legacy/target inventories in F0 and directed F1
  investigation.
- **DECISION:** Allocate UUID/crosswalk first; copy normalized global identity;
  derive names/claims; classify password hashes; map role/status into global +
  pilot membership; map work fields to that membership; translate audit actors
  after all crosswalks; copy valid photos to object storage. Identity role/enum,
  identifier collision, missing actor and last-SuperAdmin inconsistencies block
  affected records. Every active, inactive and deleted User migrates so actor
  history remains resolvable; no approved exclusion substitutes for a user and
  crosswalk. A source `TwoFactorEnabled=true` account also migrates but enters
  `reset_required` so removal of nonfunctional legacy 2FA cannot grant access.
  The pilot site comes only from the migration-run manifest;
  its initial validity starts at source CreatedDate with no end. Actors load
  through staging and an all-users-first transaction that cannot commit with an
  unresolved non-null source actor. Generated target security/concurrency values
  do not copy legacy stamps.
- **RATIONALE:** The mapping is deterministic and does not hide loss or choose
  between conflicting source authorities.
- **CONSEQUENCES:** F2 implements only schema. A later controlled data run uses
  `01-DATA-MAPPING.md`, reconciliation counts and source fingerprints; it is not
  authorized by F1.
- **AFFECTED PHASES:** F2, migration execution, F3, cutover.

## F1-D021 — Identity cutover uses one writer and no dual-write

- **STATUS:** DECIDED
- **CONTEXT:** Users cannot be safely changed in both legacy and target during
  transition.
- **EVIDENCE:** Approved writer-label contract and local identity decision.
- **DECISION:** Pre-cutover authority is `LEGACY_SQLSERVER`. During a maintenance
  freeze, fence legacy login/User writes, record the last legacy write and
  writer label, take the final fingerprinted extract, import/reconcile users,
  crosswalks, credentials and photos, invalidate legacy cookies, record UTC
  `cutover_at`/password deadline and switch routing.
  Post-cutover authority is `NEST_POSTGRES`; ASP user mutation/login surfaces
  are disabled or redirected and SQL Server identity is read-only evidence.
  There is no dual-write.
- **RATIONALE:** A single authority prevents credential/status divergence and
  creates a clear rollback boundary.
- **CONSEQUENCES:** Evidence includes final counts/fingerprint, route/config
  version, writer-label transition and first authoritative post-cutover write.
  Before that first post-cutover mutation rollback may revoke target sessions
  and restore the legacy label/route under maintenance. After it, rollback
  requires an approved reverse reconciliation; it is never automatic. The
  90-day password clock starts at recorded `cutover_at`.
- **AFFECTED PHASES:** F2, F3, F5, F7, production cutover.

## F1-D022 — Separate pinned migration streams and manifests

- **STATUS:** DECIDED
- **CONTEXT:** Current registry accepts only control-plane 0001–0003, while
  control-plane 0004/0005 and tenant 0001–0012 exist outside that registry.
- **EVIDENCE:** `migration-registry.ts`, runner/CLI/test and F0 Q-03/Q-04;
  `CONTRATO_MULTISEDE.md` requires one central DB and serialized tenant DBs.
- **DECISION:** F2 implements distinct pinned `control-plane` and `tenant`
  streams. Every entry records stream, relative path, SHA-256, bytes, target
  schema/allowlist, manifest and advisory namespace. Control-plane targets one
  central DB; tenant entries run serially per explicit site through a
  server-side secret reference. Existing SQL files are registered by their
  exact current hashes and never edited. A tenant migration is not added to the
  flat control-plane table set.
- **RATIONALE:** One flat single-connection registry cannot safely express or
  verify multi-database topology.
- **CONSEQUENCES:** F2 updates registry/runner/tests and manifests before any
  application. Local PostgreSQL credentials are provisioned outside tracked
  files. F1 performs no PostgreSQL operation.
- **AFFECTED PHASES:** F2 and all later database migrations.

## Decision quality gate

| QUESTION | RESULT |
|---|---|
| Can F2 design PostgreSQL without identity/data assumptions? | YES — fields, keys, scopes, states, constraints, audit and migration streams are frozen. |
| Can F3 implement auth without re-deciding identity? | YES — login namespace, password machine, role/status model and session matrix are frozen. |
| Can F5 implement the five screens without inventing fields? | YES — field/route/scope/tab contracts are frozen. |
| Concrete PBKDF2→bcrypt strategy? | YES — bounded dual verifier with rehash-on-login, reset fallback and 90-day retirement. |
| Concrete int→UUID strategy? | YES — permanent crosswalk. |
| Global/site role and status resolved? | YES. |
| Audit and sessions resolved? | YES. |
| User/Person resolved? | YES — separate, no invented relation. |
