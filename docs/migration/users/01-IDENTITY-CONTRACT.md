# MIG-001 F1 — Canonical identity and data contract

**PHASE:** F1 — Identity & Data Contract
**STATUS:** FROZEN
**SOURCE:** `reference/asp-final @ dccabf50330afc48760d06bd4dbaff8c37ebbd3f`
**TARGET BASELINE:** `migration/react @ 7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`

This document is the binding contract for MIG-001 F2/F3/F4/F5. It defines the
target behavior; it does not state that the current PostgreSQL, NestJS or React
implementation already complies. Existing migrations remain immutable.

## 1. Canonical definitions

### USER

A `USER` is a global control-plane account that authenticates one human and can
hold zero or more site memberships. It is not a tenant record, a role, a
`Person`, or a copy per site. The canonical technical identifier is a UUID.

### USER IDENTITY

`USER IDENTITY` is the global set of login, personal and security fields owned
by the account: UUID, immutable username, mutable email, split name, identity
card, phone, photo key, account status, password state, global SuperAdmin flag,
security version and audit summary. A change to one global field is visible in
every site where the user has a membership.

### SITE MEMBERSHIP

A `SITE MEMBERSHIP` is the relationship `(user_id, site_id)`. It owns the site
role, membership status, validity and work profile (`position`, `department`,
`hire_date`). A user can have at most one membership row per site and different
roles/work profiles in different sites.

### GLOBAL ROLE

The only global role is `SuperAdmin`, represented by
`lu_user.is_super_admin = true`. Absence of that flag means there is no global
role. The general site-user API cannot set or clear this flag.

### SITE ROLE

The only site roles are `Administrador` and `Supervisor`, stored on a site
membership. `SuperAdmin` is never stored as a membership role.

### ACCOUNT STATUS

The global account status is one of `active`, `inactive`, `deleted`.

- `active`: the account can authenticate if it otherwise has an eligible site
  or is a SuperAdmin using a global control-plane capability.
- `inactive`: reversible global suspension; login is denied in every site.
- `deleted`: reversible logical deletion; login is denied, history is retained
  and the row is omitted from default lists. It is never a hard delete.

### MEMBERSHIP STATUS

The membership status is one of `active`, `suspended`, `revoked`.

- `active`: eligible while its validity range and site status are valid.
- `suspended`: reversible denial for that site.
- `revoked`: logical removal from that site; retained for history and omitted
  from default lists.

Account and membership status are independent. Access to a site requires both
an `active` account and an eligible `active` membership.

### LOGIN IDENTIFIER

`login_identifier` is one input that accepts either username or email. The
server normalizes it once and resolves it through a global identifier-claim
table. The normalized value can belong to exactly one user and one kind
(`username` or `email`), so `username(A) == email(B)` is structurally
impossible. The response is generic for absent users, ambiguous/invalid legacy
data and wrong passwords.

### PASSWORD STATE

Password state is the tuple `password_scheme`, `password_hash`,
`must_change_password` and `password_migrated_at`:

- `legacy_identity_v2`: ASP.NET Identity payload with marker `0x00`.
- `legacy_identity_v3`: ASP.NET Identity payload with marker `0x01`; the
  embedded PRF, iteration count, salt length and subkey length are honored.
- `bcrypt`: bcrypt hash owned by the new identity runtime.
- `reset_required`: no usable credential; an authorized provisioning/reset
  flow must establish one.

No hash-to-hash conversion exists. A legacy hash becomes bcrypt only after the
plain password has been successfully verified, or after reset.

`must_change_password=true` never grants a normal application session. After a
successful bcrypt verification it grants a `password_change` session whose
only capabilities are session inspection, CSRF acquisition, password change
and logout. `reset_required` has no verifiable credential and cannot create a
session; an authorized administrator must provision a temporary bcrypt
credential first.

After password change, authentication continues through the initial-site
decision below; it does not bypass membership selection.

### AUDIT ACTOR

An `AUDIT ACTOR` is the canonical UUID of the authenticated user that caused a
change. It is nullable only for bootstrap, migration or an identified system
operation. Row audit summaries coexist with append-only security/identity audit
events; they are not a substitute for each other.

### PROFILE PICTURE

A profile picture is a global user asset. PostgreSQL stores an opaque object
key, never file bytes, an absolute filesystem path or a client-supplied URL.
The binary lives in private object storage behind the API; a local filesystem
adapter outside tracked files is permitted only for local development.

### LEGACY CROSSWALK

The permanent crosswalk maps `(source_system, legacy_user_id)` to one canonical
UUID and records migration provenance. It is retained after cutover because it
is required to translate historical actor references and reconcile source
records. A PostgreSQL-native user simply has no crosswalk row.

### SESSION INVALIDATION

`security_version` is a global monotonic integer copied into each session.
Sensitive changes increment it transactionally and revoke applicable persisted
sessions. A version mismatch is rejected on every authenticated request.

## 2. Required physical concepts for F2

Names below are contractual recommended names. F2 may choose equivalent DDL
only if the semantics and names exposed to F3/F4 remain unambiguous.

| CONCEPT | REQUIRED KEY / CONSTRAINT |
|---|---|
| `lu_user` | UUID PK; global identity and security fields; no hard delete |
| `lu_login_identifier` | PK/unique normalized value; unique `(user_id, kind)`; kind in `username,email`; FK user `RESTRICT` |
| `lu_site_membership` | PK `(user_id, site_id)`; role/status checks; FKs `RESTRICT` |
| `lu_legacy_user_xref` | unique `(source_system, legacy_user_id)` and `(source_system, user_id)`; FK user `RESTRICT` |
| identity audit event | append-only actor/subject/site/action/timestamp/correlation record; nullable actor only for system/import |
| `lu_session` | user/security-version snapshot, active site, expiry and explicit revocation data |
| identity migration state | authoritative UTC `cutover_at` and immutable `legacy_password_deadline = cutover_at + 90 days` |

`lu_user.username` and `lu_user.email` are canonical.
`lu_login_identifier` is a database-maintained, non-directly-writable normalized
projection. A deferred database invariant/constraint trigger guarantees exactly
one username claim and one email claim matching every user at transaction
commit. The username/email write, synchronized claims, audit summary and
security changes are one control-plane transaction. No application-level
pre-check is a replacement for these database constraints.

## 3. Complete canonical field table

`GLOBAL` means one value for the account. `SITE` means one value per
membership. `DERIVED` is not client-writable. Length is measured after the
field's stated normalization, except passwords.

| FIELD | SCOPE | TYPE | NULLABLE | UNIQUE | NORMALIZATION | MUTABLE | LEGACY SOURCE | VISIBLE IN ASP | SECURITY IMPACT |
|---|---|---|---|---|---|---|---|---|---|
| `lu_user.id` | GLOBAL | `uuid` | no | PK | generated UUID | no | `User.Id` through crosswalk | route/id | canonical subject |
| `username` | GLOBAL | `varchar(256)` | no | global and cross-kind | trim, Unicode NFKC, locale-independent lowercase | no through product APIs | `UserName` | Create/Edit/Details/Index/login | login identifier |
| `email` | GLOBAL | `varchar(256)` | no | global and cross-kind | trim, Unicode NFKC, locale-independent lowercase | yes, governed | `Email` | Create/Edit/Details/header/login | login identifier; rotates sessions |
| `first_name` | GLOBAL | `varchar(100)` | no | no | Unicode NFC, trim, collapse internal whitespace; preserve case | yes, governed | `FirstName` | Create/Edit/Details/Index | cosmetic/PII |
| `last_name` | GLOBAL | `varchar(100)` | no | no | same as first name | yes, governed | `LastName` | Create/Edit/Details/Index | cosmetic/PII |
| `second_last_name` | GLOBAL | `varchar(100)` | yes | no | same; blank becomes null | yes, governed | `SecondLastName` | Create/Edit/Details | cosmetic/PII |
| `full_name` | DERIVED GLOBAL | generated text/view | no | no | join the three canonical name parts with one space | no | legacy `[NotMapped] FullName` | Details/Index/header | display only |
| `initials` | DERIVED GLOBAL | API string | no | no | first Unicode grapheme of first and last name | no | legacy `[NotMapped] Initials` | avatar/Index/header | display only |
| `identity_card` | GLOBAL | `varchar(10)` | no | global, including deleted rows | trim, Unicode NFKC, uppercase; `^[0-9A-Z-]+$` | privileged only | `IdentityCard` | Create/Edit/Details/Index/search | institutional identifier/PII |
| `phone_number` | GLOBAL | `varchar(30)` | no | no | trim; preserve leading `+`; validated phone syntax | owner/privileged | `PhoneNumber` | Create/Edit/Details/header | PII, no session rotation |
| `profile_picture_key` | GLOBAL | `varchar(512)` | yes | object key unique | server generated only | owner/privileged | `ProfilePictureUrl` | Index/Edit/Details/header | file security, no rotation |
| `account_status` | GLOBAL | enum/text | no | no | `active,inactive,deleted` | privileged only | `Status` | Edit/Details/Index/Delete | login gate; rotates sessions |
| `is_super_admin` | GLOBAL | boolean | no | no | default false | privileged global command only | `Role=SuperAdmin` | role badge/protected forms | global authorization; rotates sessions |
| `password_hash` | GLOBAL | text | conditional | no | opaque; never trimmed/logged | password flow only | `PasswordHash` | never | credential secret |
| `password_scheme` | GLOBAL | enum/text | no | no | four states defined above | password flow only | detected from `PasswordHash` | never | selects verifier |
| `must_change_password` | GLOBAL | boolean | no | no | true for new/admin-reset/reset-required; false for valid imported legacy | password flow only | generated | reset UX only | restricts session capabilities |
| `password_migrated_at` | GLOBAL | `timestamptz` | yes | no | UTC | server only | generated on legacy rehash/reset | no | compatibility retirement evidence |
| `security_version` | GLOBAL | `bigint >= 0` | no | no | monotonic increment | server only | replaces `SecurityStamp` semantics | no | invalidates sessions |
| `row_version` | GLOBAL | `bigint >= 0` | no | no | monotonic increment | server only | replaces `ConcurrencyStamp` semantics | no | optimistic concurrency |
| `created_at` | GLOBAL | `timestamptz` | no | no | UTC | no | `CreatedDate` | Index/Details | audit |
| `created_by_user_id` | GLOBAL | `uuid` FK user | yes | no | legacy actor mapped via crosswalk | no | `CreatedById` | Index/Details | audit actor |
| `updated_at` | GLOBAL | `timestamptz` | no | no | UTC | server only | `LastModifiedDate` or created time | Details | audit |
| `modified_by_user_id` | GLOBAL | `uuid` FK user | yes | no | legacy actor mapped via crosswalk | server only | `ModifiedById` | Details | audit actor |
| `membership.user_id` | SITE | `uuid` | no | PK part | none | no | generated/crosswalk | implicit | authorization subject |
| `membership.site_id` | SITE | `uuid` | no | PK part | server-resolved | no | pilot site ID from migration manifest | active-site UI | tenant boundary |
| `membership.role` | SITE | enum/text | no | one value per membership | `Administrador,Supervisor` | privileged, not self active-site | legacy `Role` | Create/Edit/Details/Index | authorization; rotates sessions |
| `membership.status` | SITE | enum/text | no | no | `active,suspended,revoked` | privileged, not self active-site | derived from legacy `Status` | effective status | site access; rotates sessions |
| `membership.position` | SITE | `varchar(100)` | yes | no | NFC, trim, collapse; blank null | site admin | `Position` | Create/Edit/Details/Index | no rotation |
| `membership.department` | SITE | `varchar(100)` | yes | no | NFC, trim, collapse; blank null | site admin | `Department` | Create/Edit/Details/Index | no rotation |
| `membership.hire_date` | SITE | `date` | yes | no | ISO date | site admin | `HireDate` | Create/Edit/Details | no rotation |
| `membership.valid_from` | SITE | `timestamptz` | yes | no | UTC | privileged | initial `CreatedDate` | no | eligibility; rotates sessions |
| `membership.valid_until` | SITE | `timestamptz` | yes | no | UTC; exclusive end | privileged | initial null | no | eligibility; rotates sessions |
| `membership.created_at` | SITE | `timestamptz` | no | no | UTC | no | `CreatedDate` | Details | audit |
| `membership.created_by_user_id` | SITE | `uuid` FK user | yes | no | crosswalk/system rules | no | `CreatedById` | Details | audit actor |
| `membership.updated_at` | SITE | `timestamptz` | no | no | UTC | server only | `LastModifiedDate` or created time | Details | audit |
| `membership.modified_by_user_id` | SITE | `uuid` FK user | yes | no | crosswalk/system rules | server only | `ModifiedById` | Details | audit actor |
| `membership.row_version` | SITE | `bigint >= 0` | no | no | monotonic | server only | generated | no | optimistic concurrency |
| `login_identifier.normalized_value` | GLOBAL | `varchar(256)` | no | global PK | same normalizer as its kind | database-maintained with user field | UserName/Email | login | prevents ambiguity |
| `login_identifier.kind` | GLOBAL | enum/text | no | unique per user/kind | `username,email` | server only | inferred | no | verifier lookup |
| `legacy_xref.source_system` | GLOBAL | text | no | composite | constant `asp_sqlserver` for this import | no | generated | no | provenance |
| `legacy_xref.legacy_user_id` | GLOBAL | integer | no | composite | exact source value | no | `User.Id` | no | historical linkage |
| `legacy_xref.user_id` | GLOBAL | uuid FK user | no | one per source/user | none | no | generated | no | historical linkage |
| crosswalk provenance | GLOBAL | run id, source fingerprint, UTC time | no | run-dependent | immutable | no | migration execution | no | reconciliation |

### Conditional password hash invariant

`password_hash` is non-null for `legacy_identity_v2`,
`legacy_identity_v3` and `bcrypt`; it is null for `reset_required`. The API
always performs a dummy verification path when no usable hash exists.

Imported valid legacy states start with `must_change_password=false` and
`password_migrated_at=null`. Imported invalid/missing hashes start as
`reset_required`, `password_hash=null`, `must_change_password=true` and
`password_migrated_at=null`. New accounts start as bcrypt with
`must_change_password=true` and `password_migrated_at=null`. Transition from a
legacy/reset-required imported account to bcrypt sets
`password_migrated_at=now()`; target-native password changes leave that field
unchanged.

Membership validity uses open bounds: null `valid_from` means no lower bound;
null `valid_until` means no upper bound; if both are present then
`valid_until > valid_from`. Eligibility uses inclusive start and exclusive end.
The imported pilot membership takes the canonical pilot `site_id` from the
migration-run manifest, `valid_from=CreatedDate` and `valid_until=null`; no site
is inferred from user text or tenant data.

## 4. Legacy field disposition

| LEGACY FIELD | DISPOSITION | TARGET AUTHORITY |
|---|---|---|
| `Id` | TRANSITIONAL reference, permanently crosswalked | UUID user + crosswalk |
| `UserName`, `Email`, names, CI, phone, photo | CANONICAL | global user identity |
| `Role` | TRANSITIONAL | global flag plus site role |
| `Status` | TRANSITIONAL | account status plus membership status |
| `Position`, `Department`, `HireDate` | CANONICAL | site membership |
| `CreatedBy/Date`, `ModifiedBy/Date` | CANONICAL | row audit summary + event audit |
| `FullName`, `Initials`, normalized login fields | DERIVED | generated/API normalization |
| `PasswordHash` | TRANSITIONAL credential | password state until bcrypt |
| `SecurityStamp` | LEGACY_ONLY value | semantics replaced by generated `security_version` |
| `ConcurrencyStamp` | LEGACY_ONLY value | semantics replaced by generated row versions |
| `EmailConfirmed`, `PhoneNumberConfirmed` | NOT_REQUIRED | no confirmation workflow in approved local identity |
| `TwoFactorEnabled` | NOT_REQUIRED as target feature | no working legacy 2FA flow; a true source value migrates the account but forces `reset_required` so 2FA is never bypassed |
| `LockoutEnd`, `LockoutEnabled`, `AccessFailedCount` | LEGACY_ONLY | target rate-limit/account status/session model |

## 5. Name contract

The split fields are canonical. `full_name` is a generated, non-writable value:

`first_name + ' ' + last_name + optional(' ' + second_last_name)`.

It may be physically materialized only as a PostgreSQL generated column or an
equivalent generated projection. There may not be an independent writable
`full_name`. Create and Edit write the split fields; Details and Index consume
the derived value. This prevents divergence while retaining search/index use.

## 6. Login and collision contract

1. Username is required for migrated and new users, immutable after creation
   through all product APIs and privileged role commands,
   1–256 normalized characters and globally case-insensitive unique.
2. Email is required, editable under the global-field policy and globally
   case-insensitive unique.
3. Every user has exactly one username claim and one email claim in
   `lu_login_identifier`.
4. Both kinds share one unique namespace. A normalized username may not equal
   any normalized email, including those of the same user.
5. Login takes `loginIdentifier`, not an email-named field. It normalizes, does
   one claim lookup, then verifies the resolved user's password state.
6. A legacy collision blocks that row set from cutover reconciliation. It is
   never resolved by lookup order.

An emergency username correction is an out-of-band F2 data-repair runbook, not
domain mutability. It must atomically update the canonical field/projection,
rotate sessions and append reconciliation evidence; SuperAdmin alone cannot do
it through F3/F5. Existing PostgreSQL users are never matched or overwritten by
email alone: claim collisions require an explicit reviewed crosswalk/adoption
decision.

## 7. Password policy and migration runtime

### New and changed passwords

- Minimum: 12 Unicode code points.
- Maximum: 72 UTF-8 bytes; the same check is used in create, admin reset,
  self-change and bcrypt login.
- Required composition: at least one uppercase letter, one lowercase letter,
  one digit, one non-alphanumeric character and four distinct code points.
- Passwords are opaque: no trim, Unicode normalization, case conversion or
  logging.
- New/admin-provisioned credentials use bcrypt at the configured current cost
  (default 12; production range 10–15) and set
  `must_change_password = true`. A self-selected compliant password clears it.
- Valid bcrypt hashes with an allowed non-current cost are verified and
  rehashed at the current cost after success; they are not rejected merely for
  cost mismatch.

A valid imported legacy password of 72 bytes or fewer is grandfathered for
verification even if it does not satisfy the new creation composition policy.
After successful proof it becomes bcrypt with `must_change_password=false`.
The new policy applies when a human selects a new password, not retroactively
to the proven legacy secret. A bcrypt login with `must_change_password=true`
creates only a `password_change` session; successful compliant change rotates
the version, revokes that restricted session and may issue a normal session.

The login transport accepts up to 512 UTF-8 bytes solely to allow verification
of a possible legacy Identity password. For a bcrypt account, more than 72
bytes fails generically. A successfully verified legacy password longer than
72 bytes grants only a one-time password-change capability; no normal session
is issued until a compliant bcrypt password is set.

### Legacy compatibility boundary

The NestJS runtime implements the documented ASP.NET Identity V2/V3 format
using Node cryptographic primitives. It does not call .NET. V3 must parse the
embedded PRF/iteration/salt/subkey metadata; the current legacy configuration
has no compatibility override and .NET 9 defaults to V3 PBKDF2-HMAC-SHA512,
100,000 iterations, 128-bit salt and 256-bit subkey. V2 remains accepted only
for a source hash actually marked `0x00`.

Canonical parser rules are:

- input is strict standard Base64 and decoded bytes are bounded to 141 bytes;
- V2 is exactly 49 bytes: marker `0x00`, 16-byte salt and 32-byte subkey,
  PBKDF2-HMAC-SHA1 with 1,000 iterations;
- V3 is marker `0x01`, followed by three unsigned 32-bit big-endian values:
  PRF, iteration count and salt length, then salt and subkey;
- accepted PRFs are the ASP.NET Identity values `0=HMAC-SHA1`,
  `1=HMAC-SHA256` and `2=HMAC-SHA512`; iterations must be
  1,000–1,000,000, salt 16–64 bytes and subkey
  16–64 bytes; remaining length must exactly equal salt plus subkey;
- password bytes use UTF-8 without normalization and comparison is fixed-time;
- any malformed, unsupported or out-of-bound payload is `reset_required` at
  import (or generic failure plus security audit if encountered at runtime).

The bounds are a fail-closed import contract. Evidence shows no legacy hasher
override; a source value outside them is quarantined, not executed with an
attacker-controlled work factor.

Compatibility lasts until the immutable UTC
`legacy_password_deadline = cutover_at + 90 days`. At `now >= deadline`, login
fails closed for legacy schemes even if the retirement batch has not run. An
idempotent locked batch atomically changes every remaining legacy state to
`reset_required`, nulls its hash, sets `must_change_password=true`, increments
`security_version`, revokes its sessions and writes audit evidence. The legacy
verifier is then disabled. It may be removed earlier only when a reconciled
query proves zero legacy states. The exact UTC timestamps are recorded in the
cutover/control-plane migration state, never inferred from deployment time.

## 8. Role mapping and effective display role

| LEGACY ROLE | GLOBAL ROLE | INITIAL PILOT MEMBERSHIP ROLE |
|---|---|---|
| `Supervisor` | none | `Supervisor` |
| `Administrador` / Identity `Administrator` | none | `Administrador` |
| `SuperAdmin` | `SuperAdmin` (`is_super_admin=true`) | `Administrador` |

The pilot membership is necessary to preserve local ASP behavior. Future sites
receive explicit memberships; global SuperAdmin does not silently create a
tenant/business membership.

In an active-site list the role badge is `SuperAdmin` when the global flag is
set, otherwise the site role. Administrative details expose both dimensions so
that global and site authorization are never conflated.

### SuperAdmin administration

- A site Administrator may see a SuperAdmin who belongs to that site but may
  not edit, demote, deactivate, delete or revoke that account.
- Only an active SuperAdmin may view all global accounts or operate the
  privileged global command that creates, grants or revokes SuperAdmin.
- The normal Users HTTP create/update payload never accepts
  `is_super_admin`/`globalRole`. Bootstrap is a system-actor exception allowed
  only while no SuperAdmin exists. Later changes require actor UUID, reason and
  append-only audit evidence.
- At least one row must satisfy `is_super_admin=true AND
  account_status='active'`. The last such account cannot be demoted,
  inactivated or deleted. Membership status is not part of this count.
- The invariant is checked under a serialized/locked transaction in F3; a UI
  count is not authoritative.

## 9. Self-protection

- No user, including SuperAdmin, can inactivate or logically delete their own
  global account.
- No SuperAdmin can clear their own global role.
- No user can change the role, suspend or revoke the membership corresponding
  to their current active site.
- A SuperAdmin may change one of their non-active-site memberships only through
  the privileged global membership operation and never as a way to evade the
  last-active-SuperAdmin rule.
- Self-service may change email, split name, phone, photo and password, subject
  to validation/audit. It may not change username, CI, roles, statuses,
  employment fields or audit fields.

## 10. Global-field modification policy

`Single-site custodian` below means an Administrator acting in the target's
only non-revoked membership. The check is transactional; a client assertion is
not accepted.

| FIELD GROUP | SELF | SITE ADMINISTRATOR | SUPERADMIN |
|---|---|---|---|
| username | no | no | no; correction is only an out-of-band data-repair runbook |
| email, names, phone, photo | yes | only as single-site custodian and target is not SuperAdmin | yes |
| identity card | no | only as single-site custodian and target is not SuperAdmin | yes |
| password | self-change | admin reset only as single-site custodian and target is not SuperAdmin | admin reset |
| account status | no | only as single-site custodian and target is not SuperAdmin | yes |
| global SuperAdmin role | no | no | privileged command, never self |
| site role/status/work profile | limited by self rules | active site only | global membership operation |
| audit/security fields | no | no | no; server only |

When a user has memberships in multiple sites, a site Administrator can change
only that site's membership fields. This prevents Sede A from changing global
identity seen by Sede B without making the local pilot unadministrable.

## 11. Effective status and operations

The site UI derives the ASP-equivalent status as follows:

| ACCOUNT | MEMBERSHIP | EFFECTIVE ASP DISPLAY | LOGIN/SITE ACCESS |
|---|---|---|---|
| active | active and eligible | Activo | allowed |
| inactive | any | Inactivo | denied globally |
| active | suspended | Inactivo | denied for that site |
| deleted | any | Eliminado | denied globally |
| active/inactive | revoked | Eliminado for that site | denied for that site |

Default lists exclude deleted accounts and revoked memberships. Explicit
historical filters can retrieve all statuses, including revoked memberships;
an active-site Administrator may query that site's history and a SuperAdmin may
query global/explicit-site history. Details can display historical rows.
Reactivation/restoration reuses the same UUID, identifiers and crosswalk; it
never creates a replacement account.

Restoration is explicit and retains the stored role, work profile and audit
history. An active-site Administrator may restore a non-SuperAdmin revoked
membership in that site. If that was the user's only membership and the account
was deleted by the site-revoke rule, the same Administrator may atomically
restore the account to `active` and that membership to `active`. A SuperAdmin
may restore a global account and explicitly selected memberships; only this
global operation may restore a SuperAdmin or an account deleted globally.
Restoring an `inactive` account likewise requires SuperAdmin or its single-site
custodian. Expired validity is never silently extended: new bounds must be
provided. Restoration rotates security version, revokes stale sessions and
audits actor/reason. Self-restoration is forbidden.

### Revoke access / delete user

- A site-scoped revoke changes the active-site membership to `revoked`.
- If no other non-revoked membership remains and the account is not
  SuperAdmin, the account also becomes `deleted`; this reproduces ASP Delete in
  the single-site pilot.
- If another non-revoked membership remains, the global account status is not
  changed.
- A SuperAdmin global-delete operation sets account `deleted` and every
  membership `revoked`.
- All variants increment `security_version`, revoke persisted sessions, write
  modified actor/time and append an audit event. They never delete rows or
  password/audit history.

## 12. Session invalidation matrix

For a self-sensitive update, “replace current” means the old session is
revoked and a new session with the new version is issued only after the
operation succeeds. An administrator changing another account revokes all of
that subject's sessions with no replacement.

| ACTION | ROTATE SECURITY VERSION? | CURRENT SUBJECT SESSION | OTHER SUBJECT SESSIONS | WHY |
|---|---|---|---|---|
| email change | yes | replace current if self | revoke | login identifier changed |
| username emergency repair | yes | revoke | revoke | login identifier changed |
| self password change | yes | replace current | revoke | credential changed |
| admin password reset | yes | revoke | revoke | credential compromise/reset boundary |
| bcrypt cost rehash on login | yes, before session issue | new session gets new version | revoke old sessions | credential representation upgraded |
| global role change | yes | self change forbidden | revoke | global authorization changed |
| site role change | yes | active-site self change forbidden | revoke | authorization changed |
| membership add/status/validity change | yes | active-site self restriction applies | revoke | site eligibility changed |
| account status change | yes | self disable/delete forbidden | revoke | login eligibility changed |
| delete/revoke access | yes | forbidden for self active access | revoke | access removed |
| first/last/second name | no | keep | keep | cosmetic profile data |
| phone or identity card | no | keep | keep | PII, not an auth claim |
| photo | no | keep | keep | cosmetic asset |
| position/department/hire date | no | keep | keep | non-authorization membership metadata |

Provisioning a credential from `reset_required` is an admin reset: write
bcrypt, set `must_change_password=true`, set `password_migrated_at` for an
imported account, increment `security_version` and revoke all subject sessions.

This intentionally narrows ASP's “rotate on every Edit save” behavior to
security-relevant changes while preserving immediate invalidation for every
credential, role, membership and status change. If a request combines cosmetic
and sensitive fields, the sensitive rule wins.

## 13. Audit contract

- `created_by_user_id` and `modified_by_user_id` are UUID FKs to `lu_user` with
  `ON DELETE RESTRICT`; hard deletion is prohibited independently.
- Actor FKs are nullable only for a labeled system/bootstrap/import action.
  Human HTTP actions must have a non-null actor.
- `created_at/created_by` are immutable. `updated_at/modified_by` and row
  version change on every successful mutation.
- Imported actor IDs are resolved through the permanent crosswalk after all
  UUIDs have been allocated. A non-null source actor that cannot be resolved is
  a migration reconciliation error, not silently “Sistema”.
- Details displays current actor full name plus date, matching ASP; null actors
  display `Sistema` only when the action is explicitly system/import.
- Sensitive actions also append an event containing action, subject UUID,
  actor UUID/system label, site if applicable, UTC timestamp, correlation ID,
  reason and non-secret before/after status. Passwords/hashes and file bytes are
  never audit metadata.

## 14. Profile picture contract

- Storage: private global object storage; development may use an untracked
  filesystem adapter. The asset is not tenant-local because the user is global.
- DB value: opaque `profile_picture_key` only.
- Naming: `users/{user_uuid}/{random_uuid}.{jpg|jpeg|png|webp}`; original names
  are not used as paths.
- Validation: maximum 5 MiB; allow JPG/JPEG, PNG and WEBP; validate extension,
  declared MIME and magic signature; reject path components and active content.
- Serving: authenticated API/short-lived signed response with authorization
  equivalent to viewing that user. No arbitrary static path or client URL.
- Replacement: upload new object, validate, update DB, then delete the old
  object after commit. Rollback deletes the new object. Failed cleanup is
  retried and audited.
- Account deletion retains the object while institutional retention applies.
  A dedicated photo removal clears the key and performs controlled cleanup.
- Missing picture: derive initials. Cache invalidation uses a version/ETag based
  on object key or user update metadata, not a mutable public URL.

## 15. USER ↔ PERSON and Persons tab boundary

`User` and `Person` remain distinct aggregates. Legacy has no User→Person FK,
navigation or promotion command; the “promovidas a Usuarios” phrase is UI copy
only. Therefore MIG-001 creates no FK, no bridge and no automatic promotion.

- A User may exist without a Person.
- A Person may exist without a User.
- One does not inherit the other's email, phone, status or audit fields.
- `/Users/Index` in F5 hosts two tabs but reuses the existing People API/panel
  for “Directorio de Personal”; it does not duplicate People data or backend.
- The People deleted filter is a target defect: default excludes status `2`,
  but an explicit status `2` filter must return deleted persons. It is fixed,
  not imitated.
- People CRUD remains site-scoped and requires an active-site
  `Administrador`. A global SuperAdmin needs an eligible Administrator
  membership to operate tenant People data. The imported SuperAdmin receives
  that membership in the pilot.
- Intern/Extern subtype-state parity, Person optimistic concurrency,
  `ImportBatchId` and non-tab Persons histories are outside MIG-001. Their
  omission must not be “solved” by adding fields to User.

## 16. Administrative details and self profile routes

- Canonical admin detail: `/Users/Details/:userId`.
- Canonical self profile: `/Profile`.
- The topbar “Mi Perfil” points to `/Profile`; the Users Index Details action
  points to `/Users/Details/:userId`.
- During route transition, `/Users/Details?id=<uuid>` redirects to the canonical
  admin path and `/Users/Details` without an ID redirects to `/Profile`.
- Admin detail authorization follows the operation-scope matrix. `/Profile` is
  global self-service and does not require an active site, so a SuperAdmin with
  no membership is not trapped after login.

## 17. Global versus active-site operation matrix

| OPERATION | SITE ADMINISTRATOR | SUPERADMIN | TARGET OBJECT |
|---|---|---|---|
| LIST | default users with non-revoked membership in active site; explicit historical filter may include revoked | same site/history view, or global account view without site | membership projection or global users |
| DETAIL | target must belong to active site | any global user; optional site projection | both |
| CREATE | create non-SuperAdmin global identity plus active-site membership | create non-SuperAdmin globally and attach explicit memberships | both |
| LINK EXISTING USER | no discovery beyond conflict; escalate | may add explicit membership | membership |
| EDIT GLOBAL FIELDS | only single-site custodian; never a SuperAdmin target | yes, except username normal flow | global user |
| EDIT WORK PROFILE | active site only; never a SuperAdmin target | explicit membership/site operation | membership |
| ROLE CHANGE | active-site membership; not self and never a SuperAdmin target | any explicit site membership; not own active site | membership |
| STATUS CHANGE | membership; global account only as single-site custodian; never a SuperAdmin target | global account or explicit membership | explicit target |
| DELETE/REVOKE | active-site revoke; never a SuperAdmin target; account deletion only if it becomes membership-less | site revoke or explicit global delete | both by rule |
| PROFILE | own permitted global subset | own permitted global subset | global user |
| SUPERADMIN GRANT/REVOKE | forbidden | privileged audited command; never self/last active | global role |
| PERSONS TAB CRUD | active-site Administrator | requires active-site Administrator membership | tenant Person |

A SuperAdmin without a membership may authenticate, use `/Profile` and the
global Users control-plane view. They may not access tenant/business data or
the Persons tab until an eligible active site is selected. Global Users routes
must not be guarded as if they were tenant data; tenant operations retain the
site guard.

### Active-site session rules

- After valid credentials, membership choice is deterministic:
  - zero eligible memberships: deny a non-SuperAdmin; a SuperAdmin receives a
    normal null-site session for allowlisted global capabilities;
  - one eligible membership: select it automatically and issue a normal session;
  - multiple eligible memberships with no valid requested site: issue a
    `site_selection` session with `active_site_id=null`, 15-minute absolute
    expiry and no idle extension;
  - an optional requested site is accepted only after the same eligibility
    checks and directly produces a normal session.
- A `site_selection` session may call only auth session/CSRF, eligible-site
  listing from its server-side membership snapshot/query, set-active-site and
  logout. It has no global or tenant data access. CSRF-protected successful
  selection revokes it and issues a normal session with the chosen site.
- A normal session may have `active_site_id=null` only for SuperAdmin and then
  may call only auth/session/logout/CSRF, `/Profile` and global Users routes.
- Selecting/changing site requires CSRF and an eligible membership:
  account/site/membership all active, `valid_from IS NULL OR valid_from <= now`,
  and `valid_until IS NULL OR valid_until > now`. Global SuperAdmin does not
  bypass this requirement for tenant/business routes.
- For an existing normal session, the server locks/revalidates and updates only
  that session's active site; a `site_selection` session is instead revoked and
  replaced as stated above. Both append `active_site_changed`, do not rotate
  `security_version` and do not alter other sessions.
- Every tenant request revalidates session version/expiry, account status, site
  status, membership status and validity. Failure clears/rejects tenant context;
  it never falls back to a client-provided site.
- Global endpoints are separately allowlisted and never accept a client
  connection string or implicit tenant context. Site membership mutations
  rotate the subject's security version per section 12.

## 18. Observable ASP behavior frozen for later phases

F2–F5 must preserve these outcomes even where implementation mechanisms differ:

- login accepts username or email without ambiguity;
- username is visible and immutable in normal Edit;
- split names, CI, phone, photo, work profile and audit actors remain visible;
- Index defaults hide logical deletions and explicit filters can show them;
- Create/Edit/Details/Index use one derived full name;
- Administrador and SuperAdmin administer users; Supervisor cannot;
- normal Administrators cannot administer SuperAdmins;
- self role/status/delete protections and last-active-SuperAdmin protection
  remain;
- Delete means access revocation/soft delete with history and immediate session
  invalidation;
- administrative Details and self Profile both exist;
- the Persons directory is a reused tab, not a User relation;
- no email/phone confirmation or working 2FA flow is introduced by MIG-001.

Mechanism differences explicitly approved here are UUID plus crosswalk,
global/site role split, account/membership status split, server-side sessions
with `security_version`, private object storage and the bounded Node legacy
password verifier.
