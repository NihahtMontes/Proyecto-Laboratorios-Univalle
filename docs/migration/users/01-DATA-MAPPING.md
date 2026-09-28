# MIG-001 F1 — Legacy SQL Server to PostgreSQL data mapping

**PHASE:** F1 — conceptual mapping only
**STATUS:** FROZEN
**NO SQL OR DATA OPERATION IS AUTHORIZED BY THIS DOCUMENT**

Mapping actions use only: `DIRECT_COPY`, `TRANSFORM`, `DERIVE`, `GENERATE`,
`DEFER`, `NOT_MIGRATED`.

`LOSSLESS? = yes` means the institutional/domain value is preserved after the
declared canonical normalization; it does not mean byte-for-byte equality.
Rows that deliberately discard source state say `no, intentional`. Opaque
password hashes and legacy IDs are copied byte/value-exact until their defined
transition.

## 1. Target representation

One legacy SQL Server User becomes:

1. one global PostgreSQL `lu_user` row with a generated UUID;
2. two `lu_login_identifier` claims (username and email);
3. one permanent `lu_legacy_user_xref` row;
4. one initial `lu_site_membership` for the pilot/local site;
5. zero or one copied profile-picture object;
6. row audit summaries and a migration audit event;
7. no `Person` row and no User↔Person link.

The migration is all-or-reconciled per user. It does not silently create a
partial identity when identifiers, roles or actors conflict.

The initial membership uses the canonical pilot `site_id` supplied by the
migration-run manifest, never inferred from a user field. It starts with
`valid_from=CreatedDate` and `valid_until=null` (unbounded future). A null lower
bound means “since indefinite”, a null upper bound means “no expiry”, and when
both are present the end is exclusive and strictly greater than the start.

## 2. Complete User field map

| SOURCE | DESTINATION | ACTION | TRANSFORMATION | LOSSLESS? | SPECIAL HANDLING |
|---|---|---|---|---|---|
| `User.Id` (`int`) | `lu_user.id` + crosswalk | GENERATE | generate UUID; store exact int in xref | yes | allocate all UUIDs/xrefs before actor FKs |
| `UserName` | `lu_user.username` + username claim | TRANSFORM | trim, NFKC, locale-independent lowercase | yes for accepted values | required; cross-kind collision blocks row set |
| `NormalizedUserName` | none independent | DERIVE | canonical username/claim is normalization authority | semantic yes | do not copy stale Identity normalization |
| `Email` | `lu_user.email` + email claim | TRANSFORM | trim, NFKC, locale-independent lowercase | yes for accepted values | required; global/cross-kind uniqueness |
| `NormalizedEmail` | none independent | DERIVE | derive from target email | semantic yes | do not copy stale value |
| `EmailConfirmed` | none | NOT_MIGRATED | target does not require confirmation | no, intentional | record disposition in run manifest, not schema |
| `PasswordHash` | `password_hash`, `password_scheme` | TRANSFORM | base64-decode and classify marker/structure; copy opaque hash unchanged | yes until rehash | malformed/unknown becomes reset-required issue |
| `SecurityStamp` | `security_version=0` | GENERATE | do not copy GUID | semantic only | legacy cookies are invalid at cutover |
| `ConcurrencyStamp` | `row_version=0` | GENERATE | do not copy GUID | semantic only | target optimistic token starts at import |
| `PhoneNumber` | `lu_user.phone_number` | TRANSFORM | trim; validate target syntax | yes | source required; invalid non-empty value blocks/reconciles |
| `PhoneNumberConfirmed` | none | NOT_MIGRATED | no target phone-confirmation workflow | no, intentional | no behavioral legacy dependency |
| `TwoFactorEnabled` | password state + migration audit | TRANSFORM | false follows normal hash classification; true forces `reset_required`, null hash and `must_change_password=true` | no, intentional | user/xref/membership still migrate; source flag is audited and admin provisioning is required |
| `LockoutEnd` | no direct column | NOT_MIGRATED | account status and target rate-limit govern future access | no | permanent lockout must agree with status; temporary counters reset at cutover |
| `LockoutEnabled` | none | NOT_MIGRATED | target rate-limit applies to all accounts | no, intentional | documented aggregate count only |
| `AccessFailedCount` | none | NOT_MIGRATED | target rate-limit starts empty | no, intentional | do not copy transient attempt counters |
| `FirstName` | `lu_user.first_name` | TRANSFORM | NFC, trim, collapse whitespace; preserve case | yes | required, max 100 |
| `LastName` | `lu_user.last_name` | TRANSFORM | NFC, trim, collapse whitespace; preserve case | yes | required, max 100 |
| `SecondLastName` | `lu_user.second_last_name` | TRANSFORM | same; blank to null | yes | max 100 |
| derived `FullName` | generated `full_name` | DERIVE | join canonical split fields with one space | yes | never import as independent authority |
| derived `Initials` | API `initials` | DERIVE | first grapheme of first and last names | semantic yes | not persisted |
| `IdentityCard` | `lu_user.identity_card` | TRANSFORM | trim, NFKC, uppercase | yes | required; global uniqueness includes deleted accounts |
| `ProfilePictureUrl` | `profile_picture_key` + object | TRANSFORM | resolve safe legacy file, verify, copy to generated object key | yes if file exists | missing/invalid file → null plus explicit reconciliation issue |
| `Role=Supervisor` | membership role | TRANSFORM | no global role; pilot `Supervisor` | yes | Identity role assignment must agree |
| `Role=Administrador` | membership role | TRANSFORM | no global role; pilot `Administrador` | yes | Identity role name `Administrator` must agree |
| `Role=SuperAdmin` | global flag + membership role | TRANSFORM | `is_super_admin=true`; pilot `Administrador` | yes | Identity role must agree; last-active check after load |
| `Status=Activo` | account + membership status | TRANSFORM | `active` + `active` | yes | eligibility also depends on site/validity |
| `Status=Inactivo` | account + membership status | TRANSFORM | `inactive` + `suspended` | yes | reversible, login denied globally |
| `Status=Eliminado` | account + membership status | TRANSFORM | `deleted` + `revoked` | yes | retained; excluded by default filters |
| `Position` | membership `position` | TRANSFORM | NFC, trim/collapse; blank null | yes | initial pilot membership only |
| `Department` | membership `department` | TRANSFORM | NFC, trim/collapse; blank null | yes | initial pilot membership only |
| `HireDate` | membership `hire_date` | DIRECT_COPY | date component | yes | initial pilot membership only |
| `CreatedDate` | user + initial membership `created_at` | TRANSFORM | preserve instant as UTC | yes if source kind known | timezone interpretation recorded by migration run |
| `CreatedById` | user + initial membership actor UUID | TRANSFORM | resolve through xref | yes | source null → labeled import/system; unresolved non-null blocks |
| `LastModifiedDate` | user + initial membership `updated_at` | TRANSFORM | preserve UTC; if null use created timestamp | yes with null semantics | Details still distinguishes no modification through actor/event data |
| `ModifiedById` | user + initial membership modified actor UUID | TRANSFORM | resolve through xref | yes | source null stays null; unresolved non-null blocks |
| Identity user-role row | role consistency check | TRANSFORM | compare managed role with `User.Role` | yes when consistent | zero/multiple/mismatched managed roles block account |
| Identity claims/logins/tokens | none | NOT_MIGRATED | local password/session model starts clean | no, intentional | no external login/2FA/token contract in MIG-001 |

## 3. Role mapping

| SOURCE ENUM | SOURCE IDENTITY ROLE | GLOBAL ROLE | PILOT SITE ROLE | MIGRATION RULE |
|---|---|---|---|---|
| `Supervisor (2)` | `Supervisor` | none | `Supervisor` | both representations must agree |
| `Administrador (1)` | `Administrator` | none | `Administrador` | translate spelling only |
| `SuperAdmin (99)` | `SuperAdmin` | `SuperAdmin` | `Administrador` | count active global admins after mapping |

An absent, duplicate or contradictory managed Identity role is not resolved by
preference. The record is reported and held from cutover until reconciled.

New PostgreSQL accounts default to no global role and receive only the explicit
site membership role requested by an authorized creator. Global SuperAdmin is
created/granted only by the privileged audited control-plane command.

## 4. Status mapping

| LEGACY STATUS | ACCOUNT STATUS | MEMBERSHIP STATUS | LOGIN | DEFAULT USER LIST | EXPLICIT FILTER/DETAIL |
|---|---|---|---|---|---|
| `Activo (0)` | `active` | `active` | yes if eligible | visible | visible |
| `Inactivo (1)` | `inactive` | `suspended` | no | visible with inactive badge | visible |
| `Eliminado (2)` | `deleted` | `revoked` | no | hidden | visible |

Restoration changes the existing row and membership; it never creates a new
UUID or reuses an identifier for a second account.

## 5. Password migration state machine

### 5.1 Import classification

```text
SOURCE PasswordHash
  ├─ null/blank/malformed/unknown marker ──> RESET_REQUIRED
  ├─ decoded marker 0x00 + valid V2 shape ─> LEGACY_IDENTITY_V2
  └─ decoded marker 0x01 + valid V3 shape ─> LEGACY_IDENTITY_V3
```

String prefixes are not the authority. Classification decodes Base64 and
validates the binary marker and lengths. V3 retains its embedded PRF, iteration
count, salt length and subkey length. Current source configuration defaults to
V3 PBKDF2-HMAC-SHA512 with 100,000 iterations, but valid older V3 parameters
are verified according to their payload.

The classifier is canonical: strict standard Base64; decoded maximum 141 bytes;
V2 exactly 49 bytes (`0x00`, 16-byte salt, 32-byte subkey,
PBKDF2-HMAC-SHA1/1,000); V3 marker `0x01`, then unsigned big-endian UInt32 PRF,
iterations and salt length, followed by salt/subkey. V3 accepts only
`0=HMAC-SHA1`, `1=HMAC-SHA256`, `2=HMAC-SHA512`, 1,000–1,000,000 iterations,
16–64-byte salt and 16–64-byte subkey with exact remaining length. Password
bytes are UTF-8 without normalization. Out-of-bound payloads do not execute;
they become `reset_required`.

Valid imported legacy states initialize `must_change_password=false` and
`password_migrated_at=null`. Missing/invalid hashes initialize
`reset_required`, `password_hash=null`, `must_change_password=true` and
`password_migrated_at=null`. A later reset/provisioning that retires an imported
state sets `password_migrated_at=now()`.

### 5.2 Runtime transitions

```text
NEW ACCOUNT
  └─ compliant temporary password
       -> BCRYPT / must_change=true / migrated_at=null
            └─ successful bcrypt proof -> PASSWORD_CHANGE session only
                 ├─ logout/no change -> remains must_change=true
                 └─ compliant self change
                      -> BCRYPT / must_change=false / security_version++
                      -> revoke restricted session; normal session may issue

LEGACY_IDENTITY_V2 or LEGACY_IDENTITY_V3
  (initial must_change=false / migrated_at=null)
  ├─ failed verification
  │    -> same state; generic error; rate-limit/audit only
  ├─ successful verification, password <= 72 UTF-8 bytes
  │    -> grandfather composition; bcrypt(current cost)
  │    -> must_change=false + migrated_at=now + security_version++
  │    -> issue normal session with new version
  └─ successful verification, password > 72 UTF-8 bytes
       -> PASSWORD_CHANGE session only
       -> compliant new password
       -> BCRYPT / must_change=false + migrated_at + security_version++

BCRYPT
  ├─ failed verification -> same state
  ├─ success at current cost + must_change=false -> normal session
  ├─ success + must_change=true -> PASSWORD_CHANGE session only
  └─ success at allowed non-current cost
       -> BCRYPT(current cost) + security_version++ before session issue

ANY STATE -- authorized admin reset -->
  BCRYPT / must_change=true + security_version++ + revoke all sessions
  (set migrated_at=now when leaving an imported legacy/reset-required state)

RESET_REQUIRED -- authorized provisioning -->
  BCRYPT / must_change=true + security_version++ + revoke all sessions

LEGACY_* at now >= immutable legacy_password_deadline -->
  RESET_REQUIRED + hash=null + must_change=true + security_version++
  + revoke all sessions + audit; disable legacy verifier
```

### 5.3 Failure and concurrency rules

- Wrong password never changes scheme/hash/version.
- Unknown/malformed state always runs the configured dummy-cost path.
- Rehash locks or compare-and-swaps the user row so concurrent successful
  logins cannot overwrite a newer password/reset.
- Hash, plaintext and password length are absent from logs/audit metadata.
- The Node verifier is self-contained; it never calls the legacy .NET app.
- `PASSWORD_CHANGE` is a persisted session purpose with `active_site_id=null`;
  it can call only session/CSRF, password-change and logout endpoints.
- `reset_required` cannot authenticate or create a restricted session because
  it has no credential to prove.
- `cutover_at` and `legacy_password_deadline=cutover_at+90 days` are immutable
  UTC control-plane migration-state values. At the boundary (`now >= deadline`)
  login fails closed even before the mandatory idempotent retirement batch.

## 6. Audit mapping

### 6.1 Ordering

1. Load source rows into read-only migration staging without target FKs.
2. Allocate all canonical UUIDs and crosswalks and verify their uniqueness.
3. In one controlled target transaction, insert every user with nullable actor
   FKs temporarily null; all referenced target user rows then exist.
4. Resolve every non-null source actor through the crosswalk and update the
   self-referential FKs. Insert initial membership audit with the same actors.
5. Require zero staged non-null actors without a resolved UUID before commit;
   otherwise roll back the target transaction. The temporary null never becomes
   committed target history.
6. Append an import audit event containing source system, run ID and source
   fingerprint, but no secrets, then commit.

### 6.2 Null semantics

- Source actor null is retained as null and labeled `legacy_import`/`system` in
  the migration audit evidence.
- Source `LastModifiedDate` null remains “not modified”; the physical target
  `updated_at` may equal `created_at`, while `modified_by_user_id` stays null.
- Actor FKs use `ON DELETE RESTRICT`; logical deletion does not erase names or
  relationships.

## 7. ID crosswalk

Required logical columns:

| COLUMN | VALUE FOR MIG-001 |
|---|---|
| `source_system` | `asp_sqlserver` |
| `legacy_user_id` | exact legacy `int` |
| `user_id` | generated canonical UUID |
| `migration_run_id` | immutable execution identifier |
| `source_fingerprint` | hash/reference of the consulted source snapshot, no credentials |
| `migrated_at` | UTC |

Required uniqueness:

- `(source_system, legacy_user_id)` unique;
- `(source_system, user_id)` unique;
- `user_id` FK to user with delete restriction.

The import is idempotent by looking up the crosswalk first. It never generates
a second UUID for a previously mapped source user. A PostgreSQL-native user has
no row and is not assigned a fake negative/zero legacy ID.

## 8. Photo mapping

| SOURCE CONDITION | TARGET ACTION | ACTION CLASS | LOSSLESS? |
|---|---|---|---|
| null/blank URL | null key, initials fallback | DIRECT_COPY | yes |
| safe relative filename and valid file | validate and copy to `users/{uuid}/{random}.{ext}`; store key | TRANSFORM | yes |
| referenced file missing | null key + reconciliation issue | DEFER | no until resolved/accepted |
| invalid MIME/signature/size/path | do not copy; reconciliation issue | DEFER | no until resolved/accepted |

The source file is read-only. Migration never renames/deletes it. Target object
creation and DB key update require a compensating cleanup plan; no absolute
legacy path is stored.

## 9. Login identifier migration and collision checks

For each source account, produce two proposed normalized claims. Before loading:

1. check username duplicates;
2. check email duplicates;
3. check every username against every email, including the same user;
4. check blank/invalid identifiers and target length;
5. report all conflicting source IDs.

Any conflict blocks all affected accounts. Lookup precedence
“username first, then email” is not an accepted resolution.

`lu_user.username/email` are canonical; claims are a database-maintained
projection with exactly one row of each kind per user at commit. Import and F3
never write claims independently. A collision with a PostgreSQL-native account
does not authorize matching by email: it requires an explicit reviewed adoption
or crosswalk decision, otherwise the source account remains blocked.

## 10. User/Person mapping

| SOURCE | DESTINATION | ACTION | REASON |
|---|---|---|---|
| `User` | global user + membership | TRANSFORM | account identity |
| `Person` | existing tenant `lu_person` stream | DEFER | separate aggregate/vertical |
| “promote person” UI phrase | none | NOT_MIGRATED | no source command or relation exists |
| hypothetical User↔Person key | none | NOT_MIGRATED | no evidence; do not invent |

The Users page may show People records in another tab, but data migration does
not match records by email, name, CI or phone.

## 11. Reconciliation gates for a future data run

F1 does not execute these gates; they are mandatory inputs to a later approved
load/cutover:

- every source user, including inactive and deleted rows, migrates exactly once
  to user + xref + initial membership so historical actor references remain
  resolvable. There is no exclusion/tombstone shortcut; an invalid required row
  blocks cutover until reconciled. Source, user, xref and initial-membership
  counts therefore match exactly;
- exactly two login claims per loaded user;
- zero username/email cross-kind collisions;
- zero duplicate canonical identity cards, including deleted accounts;
- zero unresolved required/invalid `username`, `email`, first/last name, CI or
  phone values after length, format and normalization checks;
- zero unresolved optional-field violations: overlength/invalid
  `second_last_name`, `position` or `department`, invalid/out-of-range
  `hire_date`, or ambiguous/out-of-range created/modified timestamps. Every
  transformation is recorded per source row;
- zero unresolved non-null audit actors;
- source enum role agrees with managed Identity role;
- at least one active SuperAdmin after mapping;
- account status counts and initial-membership status counts each independently
  reconcile 1:1 with legacy status counts, with no user exclusions;
- password-state counts sum to loaded users and malformed hashes are listed;
- every `TwoFactorEnabled=true` user is present in user/xref/membership counts
  and explicitly mapped to `reset_required` with the source flag in audit; no
  such user remains in a legacy-verifier state and none is excluded;
- permanent `LockoutEnd` agrees with non-active source status; temporary lockout
  and failed-count resets are counted and explicitly accepted in the cutover
  manifest;
- photo source/copy/missing/invalid counts sum to source references, and every
  missing/invalid file has a per-record approved exception or is resolved;
- no hard deletes, no source writes and no copied secrets;
- target-native pre-existing users are reconciled explicitly, never overwritten
  by matching email alone.

## 12. Cutover authority mapping

| STAGE | WRITER/AUTHORITY | DATA ACTION |
|---|---|---|
| PRE-CUTOVER | `LEGACY_SQLSERVER` | rehearsal extracts only; target is not login authority |
| CUTOVER FREEZE | no user mutation during final transfer | fence ASP login/User routes in maintenance mode, record writer label/time, final extract/fingerprint, import, reconciliation, photo copy, legacy-cookie invalidation |
| AUTHORITY SWITCH | `NEST_POSTGRES` | record immutable UTC `cutover_at`, password deadline, route/config switch and target verification before opening traffic |
| POST-CUTOVER | `NEST_POSTGRES` | all identity writes and logins; SQL Server identity remains fenced/read-only evidence |

There is no dual-write period. Local PostgreSQL credentials are provisioned
outside tracked files.

The cutover evidence records the last successful legacy write, final source
fingerprint/counts, writer-label transition, `cutover_at`, route/config version,
legacy-cookie invalidation and the first authoritative post-cutover identity
write. Before that first post-cutover mutation, rollback may revoke target
sessions, restore the legacy route/writer label and discard/rehearse the target
copy under maintenance. After it, no automatic rollback is allowed: an
approved reverse-reconciliation plan and a new maintenance gate are required.
