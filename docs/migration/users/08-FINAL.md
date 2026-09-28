# MIG-001 F8 — Final acceptance

## PHASE

F8 — RECONCILIATION + FINAL ACCEPTANCE

## STATUS

- MIG-001: `COMPLETE` (repository scope).
- READY TO COMMIT: `YES`.
- READY TO DEPLOY/CUTOVER: `NO`.

Reasons `READY TO DEPLOY/CUTOVER` is `NO` (all operational prerequisites live
outside the repository):

1. Legacy identity import + crosswalk (F1-D020): no executable import tool
   exists in the repository — only the pure mapper
   `apps/api/src/database/legacy-user-mapping.ts`. This run needs its own plan
   and evidence.
2. Legacy password cutover + 90-day deadline must be recorded by the operator
   with the migrator login inside the cutover window (see step 10 of the
   checklist). The deadline is never inferred from deployment time.
3. Production environment, off-site backups, restore drills and the
   maintenance-window fencing of the ASP surface are operational tasks not
   covered by the repository.

## EXECUTION_MODE

Claude (orchestrator) + MiniMax (`minimax-coding-plan/MiniMax-M3`, agent
`build`). Every MiniMax run reports `routing_verified: true`. Reconnaissance
was read-only; the two implementation batches and the corrective + final-review
passes carried out the work.

### Reconnaissance (read-only)

| Run | Role | Output | Result |
|---|---|---|---|
| W1 parity review | review | `.claude/runtime/f8/w1-parity.md` | 47 rows analyzed (26 PARTIAL + 6 MISSING + 15 CONFLICT) with code citations and F1 references. |
| W2 gates audit | tests | `.claude/runtime/f8/w2-gates.md` | Canonical commands, current failures, mechanical vs semantic debt, P-05 findings. |
| W3 cutover audit | review | `.claude/runtime/f8/w3-cutover.md` | Exact cutover order, role/grant requirements, legacy-password mechanism, email-alias consumers, missing automation. |

### Batch 1 (implementation)

| Worker | Output | Result |
|---|---|---|
| AUTH backend | `.claude/runtime/f8/b1-auth.md` | Stopped by the orchestrator after a worker process ran `git stash` / `git stash pop` against the DO_NOT rules. Integrity verified (worktree = stash content + owned-file edits; no loss). Completed/fixed by Claude: `apps/api/src/auth/auth.types.ts`, `auth.service.ts`, `auth.controller.ts`, `auth.repository.ts` (F7 fixes preserved), `auth-managed-role-cli.ts`; alias `{email,password}` rejected with 400, loginIdentifier with username/email passes; `POST /auth/password` per-identifier rate limit via the existing `AuthRateLimitService` (no schema change, scope labels `email_ip`/`ip` unchanged). |
| DBGUARD database | `.claude/runtime/f8/b1-dbguard.md` | Stopped after the same process incident. Fixed by Claude: `apps/api/src/database/cutover-preflight.ts` (new), `migration-cli.ts`, `auth-managed-role-cli.ts` (ledger-absent bugs); both fail-closed guards implemented and unit-tested. |
| WEB general | `.claude/runtime/f8/b1-web.md` | `COMPLETE`. SuperAdmin-without-sites truthful `alert alert-info` (Crear Cuenta disabled, no API call); voluntary password-change success message on `/Profile`; vitest cases for the F8 surface. |

### Batch 2 (cleanup, specs, matrix, review)

| Worker | Output | Result |
|---|---|---|
| GATES cleanup | `.claude/runtime/f8/b2-gates.md` | Resolved 41 lint errors + 2 unused-disable warnings without semantic change (14 imports, 2 helpers, 1 empty interface, callback arity / interface signatures, 1 unused param, `any`→`LoginResponseBody`). |
| E2E specs | `.claude/runtime/f8/b2-e2e.md` | Updated F7 Playwright specs to F8 behavior: B8 site-admin restore, B8(SuperAdmin), B9 SuperAdmin-without-sites, B14(a/b/c), A14 success message, A14b per-identifier rate-limit on password, A18 `{email,password}` alias rejected. Total 103 tests in 11 files; no `.skip`/`.fixme`/`.only`; retries 0; no arbitrary waits. |
| MATRIX documentation | `.claude/runtime/f8/b2-matrix.md` | Applied adjudication: 26 Changed rows updated with `; F8: … (antes X)`, 21 Kept rows annotated, recount `PARITY 106 / PARTIAL 14 / MISSING 1 / CONFLICT 10 / NOT_APPLICABLE 27 / UNKNOWN 0` = 158. |
| E2E correction | `.claude/runtime/f8/e2e-fix.md` | Three precise corrections: B14(b) setup revoked site b (not a); A14b expectClean extended with `POST /api/v1/auth/login 401`; A8b expectClean extended with the expected revoked-session 401s. |
| FINAL review | `.claude/runtime/f8/final-review.md` / `.claude/runtime/f8/final-review.json` | Independent read-only review: behavior-correct, security-neutral widening, consistent with F1/F7 contracts. 0 BLOCKER, 0 HIGH; 1 MEDIUM (doc tension with historical F7 handoff — kept historical, resolved here), 4 LOW (accepted/follow-ups; one rejected as a misread). |

## PROCESS INCIDENT

During batch 1 a MiniMax worker ran `git stash` / `git stash pop` despite the
worker-level `DO_NOT` rule. The orchestrator verified integrity immediately:

- `git status --short` and stash content reconciled: worktree = stash content +
  owned-file edits of the AUTH worker; no file loss.
- Remaining workers of batch 1 were stopped.
- Batch 2 ran with a stash watchdog (no further events).
- A second worker of the same batch also attempted the same pattern; it was
  stopped at the same point.

## SCOPE IMPLEMENTED IN F8

- Membership restore from the admin Details view: a site `Administrador` may
  restore a membership of their active site in status `revoked` from
  `/Users/Details/:id` (F1 §11). `UserAccessPolicy.canViewUser` widens to
  subjects that have any membership row in the actor's active site; the
  service still projects only the active-site row to a site admin;
  `canUpdateGlobalFields` / `canManageMembership('restore')` remain gated on
  `nonRevokedMemberships` / active-site (custody not widened).
- `POST /auth/password` endpoint rate limit via the existing
  `AuthRateLimitService` (no second limiter, no schema change, scope labels
  `email_ip`/`ip` per the frozen 0002 CHECK). Bucket key
  `password-change:<userId>` + IP, consumed after a read-only session lookup
  and BEFORE verifying the current password; the bucket commits in its own
  transaction so failed verifications count; invalid/stale (security_version
  mismatch, inactive account) and site_selection sessions are rejected BEFORE
  consuming. 429 + `Retry-After` like login.
- Backend `email` login alias removed: `{email,password}` → 400;
  `{loginIdentifier,email,...}` → 400; `loginIdentifier` with username and
  with email passes (unit + live A1/A2 + API-level live test).
- Managed-role ordering guards (roles/003): `db:migrate:up 0004…` on stream
  `control-plane` refuses before any DDL when the roles/003 postconditions
  are not met; `auth:managed-role provision` refuses when control-plane ≥
  0004 is recorded or the ledger is unreadable. Role-file pin now hashed over
  canonical LF bytes.
- SuperAdmin-without-sites truthful UX: `Create` shows `alert alert-info` with
  the F1 §17 text, disables `Crear Cuenta`, and `submit` returns early
  without calling the API. SuperAdmins with eligible sites and site
  `Administradores` are unaffected.
- Password-change success feedback on `/Profile`: inline `alert alert-success`
  with `Contraseña actualizada correctamente.`; cleared at the start of every
  submit; errors keep their previous behavior.
- 41 lint errors + 2 unused-disable warnings resolved without semantic change
  (GATES worker).
- F7 Playwright specs updated to F8 product behavior (103 tests in 11 files).
- `00-PARITY-MATRIX.md` `Estado F8` section: 26 transitions, 21 kept-decisions,
  recount, six product changes, no-promotion-by-documentation note.

## SOURCE→TARGET RECONCILIATION

### Final counts (`STATUS` column, direct recount, 158 rows)

- PARITY **106**
- PARTIAL **14**
- MISSING **1**
- CONFLICT **10**
- NOT_APPLICABLE **27**
- UNKNOWN **0**
- **Total 158**

### Transitions F8 (26 rows)

| Bucket | Movement | Rows |
|---|---|---|
| PARTIAL → CONFLICT | 3 | A-02, A-04, E-11 |
| PARTIAL → NOT_APPLICABLE | 5 | B-12, I-01, I-03, I-04, N-04 |
| PARTIAL → PARITY | 5 | F-02, F-06, F-07, Q-01, Q-02 |
| CONFLICT → PARITY | 3 | F-05, N-09, N-10 |
| CONFLICT → NOT_APPLICABLE | 5 | F-12, H-10, M-03, M-05, N-01 |
| MISSING → NOT_APPLICABLE | 4 | F-15, I-10, I-11, O-05 |
| MISSING → PARTIAL | 1 | J-05 |
| Kept (no `STATUS` change, F8 note appended) | 21 | A-07, A-08, B-08, C-14, D-01, D-06, D-07, E-16, F-08, F-13, G-10, H-06, H-07, H-09, I-07, I-09, L-02, L-03, N-08, O-07, P-05 |

The full 26-row transitions table with motives and the 21-row kept-decisions
table live in `00-PARITY-MATRIX.md` `Estado F8` (orchestrator-authored; this
handoff does not duplicate the per-row content).

### Non-`PARITY` rows grouped (master plan: `NOT_APPLICABLE` rows justified
in situ)

`PARTIAL` (14)

| ID | Area | Justification |
|---|---|---|
| A-07 | Login | mensaje generico con otra redaccion ("Usuario o contrasena incorrectos."). |
| B-08 | Shell | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| C-14 | Index Users | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| D-06 | Index Persons | busqueda y estado compartidos; paginacion independiente por tab. |
| D-07 | Index Persons | Editar/Eliminar; sin Detalles de Persona (F1-D016). |
| E-16 | Create | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| F-13 | Edit | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| G-10 | Details | 404/403 tipados inline con Volver al Listado; sin pagina `/Error` (desviacion aceptada F8). |
| H-07 | Delete | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| H-09 | Delete | error visible en la pagina Delete; sin redireccion de reintento. |
| I-09 | Persons | busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |
| J-05 | Perfil | auditores visibles en `/Users/Details/:id`; `/Profile` no los expone (no se inventan). |
| O-07 | CSS | fuentes de iconos Simple-Line/Weather no cargadas. |
| P-05 | Tests | sin contrato de concurrencia optimista; `FOR UPDATE` serializa; ultimo SuperAdmin probado en vivo. |

`MISSING` (1)

| ID | Area | Justification |
|---|---|---|
| L-03 | Personas | busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |

`CONFLICT` (10)

| ID | Area | Justification |
|---|---|---|
| A-02 | Login | modelo de sesion aprobado (idle 30 min / absoluto 12 h, rememberMe absoluto) en lugar de cookie sliding 8 h; decision F8: se conserva. |
| A-04 | Login | sin pagina Lockout; buckets `email_ip`/`ip` y 429 inline; decision F8: se conserva (misma familia que A-08). |
| A-08 | Login | buckets por identificador+IP en vez de `AccessFailedCount` por usuario (aprobado F1). |
| D-01 | Index Persons | alta de Persona inline (F1-D016, sin `/Persons/Create`). |
| E-11 | Create | no se bloquean caracteres; F1 §5 no exige solo letras y el bloqueo legacy rechazaria nombres validos. |
| F-08 | Edit | F1-D008 (rol SuperAdmin solo por CLI; ultimo SuperAdmin activo bajo lock). |
| H-06 | Delete | F1-D008 (idem F-08). |
| I-07 | Persons | orden People `name ASC` (API reutilizada, F1-D016). |
| L-02 | Personas | nombre unico de Persona (modelo People reutilizado). |
| N-08 | Auditoria | cookie `__Host-lu_session` y TTL del modelo aprobado. |

`NOT_APPLICABLE` (27)

| ID | Area | Justification |
|---|---|---|
| A-03 | Login | referencia muerta confirmada. |
| A-06 | Login | no hay login externo en el sistema actual. |
| B-10 | Shell | arquitectura multi-sede (decision 12 plan). |
| B-12 | Shell | `HidePageTitle` no se usa en ninguna superficie MIG-001; no hay comportamiento que portar. |
| E-09 | Create | F1-D008: comando global auditado, no UserController. |
| E-12 | Create | jQuery eliminado. |
| F-10 | Edit | sin cookies sliding. |
| F-12 | Edit | F1-D007: rol por membresia; no hay rol Identity que sincronizar (igual que M-07). |
| F-15 | Edit | LC-01: la confirmacion SweetAlert previa solo existe en guias; el ASP ejecutable envia directo. |
| H-10 | Delete | LC-01 (idem F-15): el Delete legacy es su pagina de confirmacion; comandos de membresia usan `window.confirm` adicional. |
| I-01 | Persons | F1-D016: el tab reutiliza la API/panel de People sin rediseño; `actorCode` pertenece al modelo People. |
| I-03 | Persons | F1-D016 / F1 §15: estados/persistencia de subtipo fuera de MIG-001. |
| I-04 | Persons | F1-D016 / F1 §15 (idem). |
| I-10 | Persons | F1-D016: ficha/auditoria de Persona fuera de MIG-001 (solo el tab reutilizado). |
| I-11 | Persons | F1 §15: concurrencia optimista de Persona e `ImportBatchId` fuera de MIG-001. |
| K-05 | Multi-sede | decision 12 plan. |
| M-03 | Schema | F1-D016: esquema People (TPT vs tabla plana) no se rediseña en MIG-001. |
| M-04 | Schema | F1-D001: UUID canonico, no se busca paridad de tipo. |
| M-05 | Schema | F1-D005: bcrypt + verificadores ASP.NET Identity V2/V3 con rehash; usuarios conservan su password (probado en vivo F7). |
| M-06 | Schema | F1-D012 congela semantica equivalente. |
| M-07 | Schema | F1-D007; global flag + membership role. |
| N-01 | Identity | F1-D007 (idem F-12). |
| N-04 | Identity | F1 §7/§12: reset administrativo escribe bcrypt + `must_change_password=true` + rota version + revoca sesiones; token Identity interno sin equivalente. |
| N-06 | Identity | sin cookies. |
| O-05 | CSS | libreria SweetAlert2 reemplazada por el stack React (idem O-06). |
| O-06 | NiceAdmin | stack nuevo por plan. |
| P-01 | Test | sin legacy runner. |

> Master-plan requirement: "las `NOT_APPLICABLE` deben justificarse en una nota
> al pie del handoff final" — listed above.

### Transitions summary

```
PARITY     98 → 106  (+8 from F-02, F-05, F-06, F-07, N-09, N-10, Q-01, Q-02)
PARTIAL    26 → 14   (-13 out: 5→PARITY, 5→NOT_APPLICABLE, 3→CONFLICT; +1 in: J-05 from MISSING)
MISSING     6 → 1    (-5 out: 4→NOT_APPLICABLE, 1→PARTIAL)
CONFLICT   15 → 10   (-8 out: 3→PARITY, 5→NOT_APPLICABLE; +3 in: A-02, A-04, E-11)
NOT_APPLICABLE 13 → 27  (+14 in: 5 from PARTIAL, 5 from CONFLICT, 4 from MISSING)
UNKNOWN     0 → 0    (unchanged)
total     158        (recount verified by node script)
```

## DECISIONS

1. Site-admin restore of a revoked membership — RESOLVED. F1 §11 ("an
   active-site Administrador may query that site's history … Details can
   display historical rows"; "may restore a non-SuperAdmin revoked
   membership in that site"; cascade-deleted account restored atomically) and
   §17 DETAIL ("target must belong to active site"). `UserAccessPolicy.canViewUser`
   now allows a site `Administrador` to view a subject that has a membership
   of ANY status in the actor's active site; other sites and membership-less
   subjects stay 404; SuperAdmin unchanged. The service still projects only
   the active-site membership to a site admin; custody predicates (single-site
   custodian over NON-REVOKED memberships, ≥1 required) are untouched, so the
   F3 vacuous-custody fix holds. Restore command already scoped
   (`NOT_ACTIVE_SITE` for other sites, 404 when no membership in the site,
   `SUPERADMIN_REQUIRED` unless the deletion was this site's cascade). UI:
   Details hides global actions (edit, inactivate/activate, revoke, password
   reset, photo) for that site-history view; the membership row shows
   `Restaurar`. Evidence: policy unit tests (positive/negative, custody not
   widened), web vitest, Playwright B8 (site-admin restores cascade-deleted
   account → login works), B8(SuperAdmin), B14(a/b/c) (cross-site Details
   404, cross-site restore 403 ACCESS_DENIED with membership unchanged,
   membership-less 404).
2. SuperAdmin without site creating users — ACCEPTED LIMITATION with truthful
   UX. F1 §17 CREATE: SuperAdmin creates non-SuperAdmin users and
   "attach[es] explicit memberships"; no global sites catalog exists and none
   was invented. Create shows an info notice, disables submit and sends no
   request (no raw 400). Remedy: SuperAdmin obtains an eligible membership
   (bootstrap/pilot membership, or another SuperAdmin adds one).
3. Password-change success feedback — RESOLVED: inline `alert alert-success`
   "Contraseña actualizada correctamente." (no toast framework).
4. Resource existence 403/404 — ACCEPTED LOW RISK. Reads (Details, photo)
   answer 404 for both unknown and out-of-scope ids. Command routes answer
   403 for an existing out-of-scope UUID and 404 for an unknown one. F1 is
   silent; ids are random UUIDv4 (not enumerable); fixing it requires a
   viewability pre-check in every command and changes typed 403 reasons the
   client relies on. Follow-up if threat model changes.
5. `POST /auth/password` rate limit — RESOLVED. Existing
   `AuthRateLimitService` (no second limiter, no schema change, scope labels
   still `email_ip`/`ip` per the frozen 0002 CHECK). Key
   `password-change:<userId>` + IP, consumed after a read-only session lookup
   and BEFORE verifying the current password; the bucket commits in its own
   transaction so failed verifications count; invalid/stale (security_version
   mismatch, inactive account) and site_selection sessions are rejected BEFORE
   consuming (an invalidated token cannot lock the owner out). 429 +
   `Retry-After` like login. Uses the same per-identifier limit/window as
   login; a successful change also counts one attempt. Evidence: unit tests
   (incl. mutation-verified stale-session test), controller `Retry-After`
   test, live Playwright A14b (10×401, then correct password → 429
   `RATE_LIMITED`, hash unchanged).
6. Legacy `email` login alias — REMOVED. Consumers verified zero (web,
   api-client, `tests/e2e/deployed-auth.js` send `loginIdentifier`).
   `{email,password}` → 400; `{loginIdentifier,email,...}` → 400;
   `loginIdentifier` username/email pass (unit + live A1/A2 + API-level live
   test).
7. Managed-role cutover order — VERIFIED + GUARDED. roles/003
   (`apps/api/database/roles/003_provision_auth_runtime_managed.sql`, LF
   SHA-256 pin `7fd48d71…`, unchanged) starts with `REVOKE ALL PRIVILEGES`
   on `lu_site`, `lu_user`, `lu_site_membership`, `lu_session`,
   `lu_auth_rate_limit`, `lu_security_event`, `lu_tenant_route`. It must run
   AFTER control-plane 0003 and BEFORE 0004: skipped → runtime 42501 and CP
   0006 fails closed; run late → silently erases 0004+ grants. New fail-closed
   guards (`apps/api/src/database/cutover-preflight.ts`): (a)
   `db:migrate:up 0004_academic_catalogs.sql --stream control-plane` refuses
   before any DDL unless the roles/003 runtime postconditions hold (0004-applied
   detection via ledger `lu_migration_history`, created by CP 0006, or the
   0004 table `lu_faculty` on pre-ledger DBs); (b) `auth:managed-role
   provision` refuses when CP ≥ 0004 is recorded or detectable (`lu_faculty`)
   or the ledger cannot be read. Role-file pin now hashed over canonical LF
   bytes (same canonicalization as the migration registry). Live evidence: the
   F7 harness applied CP 0001–0003, roles/003 (postconditions passed), then
   CP 0004–0006 through the CLI with the guard active.
8. Legacy password cutover order — VERIFIED (F1 §7, F1-D021). Singleton
   `public.lu_identity_migration_state` (id=1): `cutover_at`,
   `legacy_password_deadline`; CHECK `legacy_password_deadline = cutover_at
   + INTERVAL '90 days'` (both null or both set); trigger makes both immutable
   once set; runtime role has SELECT only. Reader
   `apps/api/src/identity/legacy-password-window.ts`: no row / null deadline
   → legacy login allowed and rehashed to bcrypt; `now >= deadline` → legacy
   login refused (fail closed, audited), even if the retirement batch has not
   run. The deadline is never inferred from deployment time. No CLI records
   it: the operator records it with the migrator login inside the cutover
   window (exact SQL in the checklist). Not implemented (follow-up, not
   blocking since login fails closed at the deadline): the F1 §7 idempotent
   retirement batch (remaining legacy states → `reset_required`, null hash,
   `must_change_password`, rotate version, revoke sessions, audit) and
   removal of the legacy verifier (allowed only after a reconciled query
   proves zero legacy states).
9. Global stylesheet — ACCEPTED SCOPED DECISION: MIG-001 surfaces use scoped
   dist-equivalent styling under `.users-module` (F6); the global React
   stylesheet is not replaced (F7 responsive 21/21 and full E2E show no
   dependency).
10. Global toast / pre-submit confirmation — ACCEPTED DEVIATION: inline alerts
    with the legacy texts; LC-01 shows executable ASP has no SweetAlert
    pre-submit confirmation (only guidance docs), so F-15/H-10 are
    NOT_APPLICABLE; toast rows B-08/C-14/E-16/F-13/H-07 stay PARTIAL.
11. Letters-only names — ACCEPTED DEVIATION (E-11 CONFLICT): F1 §5 does not
    require it; it would reject valid names (hyphen, apostrophe, Unicode
    letters); server normalizes and bounds names.
12. People search by Id — ACCEPTED GAP (I-09 PARTIAL, L-03 MISSING): F1-D016
    reuses the People API; not required; no client-side fake search.
13. Person details / Person create page — ACCEPTED GAP (D-07 PARTIAL, D-01
    CONFLICT, I-10 NOT_APPLICABLE): F1-D016 scope (tab reuse only).
14. `/Error` page — ACCEPTED GAP (G-10 PARTIAL): typed inline 404/403 with
    "Volver al Listado"; no stack traces or internals exposed (F7 errors spec).
15. SuperAdmin user-list filter by site — ACCEPTED GAP: legacy is single-site
    (no such feature to reproduce); SuperAdmin with an active site already
    gets the global view with the active-site role column; a server-side site
    filter would be new API surface.
16. Site-admin global-field edit prediction — ACCEPTED: server-authoritative
    403 (`CUSTODY_NOT_SINGLE_SITE`) with typed message; no duplicated custody
    rules in React (only the stable signals already exposed: SuperAdmin
    target, revoked site history).
17. Multi-step create atomicity — ACCEPTED BOUNDARY: identity + all
    memberships are one server transaction (E-14 PARITY); the optional photo
    is a separate command (F1 §14 upload lifecycle); a photo failure is
    reported without a fake client rollback.
18. J-05 profile audit actors — ACCEPTED BACKEND LIMITATION (PARTIAL): admin
    Details shows actors (G-05); `GET /profile` does not return them;
    nothing is fabricated.
19. P-05 concurrent edits — ACCEPTED GAP (PARTIAL): no
    optimistic-concurrency contract (If-Match/row_version) exists in the users
    API; `row_version` is trigger-maintained; edits serialize with `FOR UPDATE`
    (last writer wins, like the legacy form round-trip); the critical
    invariant (last active SuperAdmin) is proven live under concurrency (F7
    `users.rbac`).

## INTENTIONAL DEVIATIONS

- F-15/H-10 (pre-submit SweetAlert confirmation): NOT_APPLICABLE — the
  executable ASP does not show one; only the guidance docs do (LC-01).
- B-08/C-14/E-16/F-13/H-07 (toast): PARTIAL — inline alerts with the legacy
  texts; no global toast.
- A-02 (cookie sliding 8 h): CONFLICT kept — approved target model (idle
  30 min / absolute 12 h, rememberMe max-age absolute).
- A-04 (Lockout page): CONFLICT kept — buckets `email_ip`/`ip` with 429
  inline; no dedicated Lockout page.
- E-11 (letters-only names): CONFLICT kept — server normalizes and bounds;
  blocking legacy letters would reject valid names (hyphen, apostrophe,
  Unicode).
- F-08/H-06 (last-SuperAdmin): CONFLICT kept — `FOR UPDATE` + serialized
  recount; no HTTP path to demote the last SuperAdmin.

## ACCEPTED RISKS

- 403/404 oracle (decision 4): command routes answer 403 for existing
  out-of-scope UUID and 404 for unknown; reads answer 404 for both. UUIDs are
  not enumerable. Follow-up if threat model changes.
- Password-change bucket counts successful changes (decision 5): the
  per-identifier bucket consumes one slot per attempt regardless of outcome
  by design (the bucket commit is independent of the change transaction).
- Legacy deadline recorded manually (decision 8): no CLI records it; the
  operator runs the SQL of step 10 inside the cutover window, once. No
  rollback of the recorded value (`trigger` makes both fields immutable).
- Retirement batch not implemented (decision 8): legacy login is refused
  automatically at `now >= deadline`; the F1 §7 batch that converts remaining
  legacy states is a follow-up. Login fails closed at the deadline regardless.
- P-05 (decision 19): no optimistic-concurrency contract; edits serialize
  via `FOR UPDATE`; the last-SuperAdmin invariant is proven live.

## SECURITY REVIEW

- Authentication: `loginIdentifier`-only (no `{email,password}` alias);
  generic 401; per identifier+IP and per-IP buckets; login floor delay
  unchanged.
- PBKDF2 V2/V3 verifiers, bcrypt rehash, >72-byte legacy → password_change-only
  session, deadline refusal: unchanged in F8, re-proven live (auth.legacy
  A17a–e).
- Session invalidation / `security_version`: F7 alias fix intact in both
  `findSessionByTokenHash` (new read-only use) and
  `findSessionByTokenHashForUpdate`; password change and restore rotate and
  revoke.
- Session secret: HttpOnly `__Host` cookie, never in storage/DOM/console (A16
  live).
- RBAC/site scoping: see decision 1; no mutation uses `canViewUser`.
- SuperAdmin: no HTTP path grants/revokes SuperAdmin
  (`FORBIDDEN_PAYLOAD_KEYS`, `canCreateUser`, CLI-only superadmin).
- Profile photo: validation unchanged (extension/MIME/signature, 5 MiB); read
  authorization = `canViewUser` (site history now included, consistent with
  F1 §14 "equivalent to viewing that user").
- Runtime grants and roles/003 ordering: guarded (decision 7).
- MiniMax final review: 0 BLOCKER, 0 HIGH; 1 MEDIUM (doc tension with the
  historical F7 handoff — kept historical, resolved here), 4 LOW
  (accepted/follow-ups; one rejected as a misread).
- Process incident: during batch 1 a MiniMax worker ran `git stash` / `git
  stash pop` despite DO_NOT; integrity verified (worktree = stash content +
  owned-file edits; no loss); remaining workers stopped; later batches ran
  with a stash watchdog (0 events).

## TEST EVIDENCE

| Suite | Result |
|---|---|
| `corepack pnpm run verify` (format, lint `--max-warnings=0`, typecheck, build, test) | exit 0 |
| `@lu/api` jest | 1288 passed, 10 skipped (the opt-in PostgreSQL suites, run live), 0 failed |
| `@lu/web` vitest | 50 / 50 |
| `@lu/api-client` node --test | 30 / 30 |
| Live PostgreSQL (F3 suites, real F2 schema + grants) | 10 / 10 |
| Playwright F7/F8 (final run `0b853965`, `--phase=all`) | 103 / 103, retries 0, no `skip`/`fixme`/`only`, no arbitrary waits |
| Playwright first run `2b1bf239` | 100 / 103 — 3 test defects fixed without weakening assertions: B14(b) setup revoked the wrong site (was site a); A14b and pre-existing A8b health allowlists missed expected 401s |
| Teardown | ports free, `lu_auth_login` restored byte-exact, temp DB 0, temp role 0, pg_hba pin `0c8dc6e6…cd42` match, trust inactive, photo dir removed |
| `git diff --check` | PASS |
| Migration pins | 19 / 19; roles/003 LF pin `7fd48d71…` unchanged |
| Secret scan | no secrets (all hits reviewed false positives: `must_change_password` comments, public dummy bcrypt constant, ASP.NET test vectors, example-host test DSNs, JPEG test bytes) |

## LIVE ADMIN ACCESS

The F7 procedure was recreated with the user's approval for the F8 run
(`tests/e2e/stack/run-f7.mjs`, `E2E_ADMIN_DSN` loopback admin only). Steps:

1. Byte-exact `pg_hba.conf` backup.
2. Pin check against `0c8dc6e6e57399790417a6e13b3a8e1b5e27aa19708a2122148fbfe3bdcecd42`.
3. Prepend exactly one `host all postgres 127.0.0.1/32 trust` line only
   during admin blocks (never during browser runs).
4. Byte-exact restore in `finally`.
5. Password-less `postgres` login proven rejected after every window.
6. Module deleted afterwards.

Local PostgreSQL credentials are already provisioned outside tracked files.

## CUTOVER CHECKLIST

1. Backup/recovery prerequisite: full logical backup (`pg_dump`) of the
   production control plane and each tenant DB, plus the SQL Server legacy
   identity DB; record checksums; verify restore on a scratch server. Keep
   `lu_auth_login` state snapshot.
2. Freeze (F1-D021): maintenance window; fence legacy login and User writes
   (ASP read-only), record last legacy write and writer label
   `LEGACY_SQLSERVER`.
3. Control-plane migrations 0001 → 0003: `corepack pnpm --filter @lu/api run
   db:migrate:up <file> --stream control-plane` one file at a time, then
   `db:migrate:verify`.
4. Managed-role grants: `corepack pnpm --filter @lu/api run auth:managed-role
   provision` (runs roles/003; pin + postconditions; restricted to the
   `neondb` control database by its own preflight) — AFTER 0003, BEFORE
   0004. The CLI refuses if 0004+ is already applied.
5. Control-plane migrations 0004 → 0006 (the 0004 `up` refuses unless step
   4's postconditions hold); `db:migrate:verify`.
6. Tenant migrations 0001 → 0013 per site (`--stream tenant --site <siteId>`),
   tenant routes active.
7. Runtime-role verification: `auth:managed-role verify`; `lu_auth_runtime`
   has no DDL; `lu_auth_login` stays `NOLOGIN` until it is activated in step 11.
8. Legacy identity import + crosswalk validation (F1-D020): allocate UUIDs,
   import normalized identities, crosswalk, classify hashes (V2/V3/reset_required),
   memberships, audit actors, photos. NOTE: no executable import tool exists
   in the repository (only the pure mapper
   `apps/api/src/database/legacy-user-mapping.ts`); this run needs its own
   plan and evidence (master plan "carga de usuarios legacy requiere plan y
   evidencia separados"). Reconcile counts and blocked records.
9. Username/email namespace checks: no collision in `lu_login_identifier`
   (shared namespace F1-D004), no blocked record left unresolved,
   last-active-SuperAdmin invariant ≥ 1.
10. Record cutover + legacy password deadline (migrator login, inside the
    window, once — immutable):

    ```sql
    INSERT INTO public.lu_identity_migration_state (id, writer_label, cutover_at, legacy_password_deadline)
    VALUES (1, 'NEST_POSTGRES', '<UTC cutover ISO>', '<UTC cutover ISO>'::timestamptz + INTERVAL '90 days');
    ```

    Verify with `SELECT cutover_at, legacy_password_deadline FROM
    public.lu_identity_migration_state;`. Never derive it from deployment
    time. If intentionally left unrecorded, legacy login stays allowed and
    rehashes (F1 literal behavior).

11. Deploy the application (API + web) with production configuration (auth
    HMAC key, cookie settings, rate-limit values, CORS/origin, photo storage),
    `lu_auth_login` activated with `auth:managed-role activate`.
12. Smoke authentication: username and email login, logout, forced password
    change, one legacy V3 account login → bcrypt rehash.
13. Role/site authorization smoke: site `Administrador` list/details own site;
    cross-site 404; Supervisor denied; SuperAdmin null-site workspace; restore
    of a revoked own-site membership.
14. Profile/photo smoke: profile edit, password change success message, photo
    upload/serve/remove.
15. Open traffic; invalidate legacy cookies; switch writer to `NEST_POSTGRES`
    (post-cutover authority; no dual-write).
16. Post-cutover monitoring: 401/403/429 rates, `lu_security_event`
    `legacy_password_rejected` / rate-limit events, 5xx, count of remaining
    legacy password states.
17. Cleanup/retirement: at `now >= legacy_password_deadline` legacy logins
    are refused automatically; run the (to-be-implemented) retirement batch;
    remove the PBKDF2 verifier only after a reconciled query proves zero
    legacy states; retire ASP identity surfaces; SQL Server identity kept
    read-only as evidence.

## LEGACY PASSWORD CUTOVER

- Singleton `public.lu_identity_migration_state` (id=1). CHECK
  `legacy_password_deadline = cutover_at + INTERVAL '90 days'` (both null
  or both set); trigger makes both fields immutable once set; runtime role
  has SELECT only.
- Reader `apps/api/src/identity/legacy-password-window.ts`:
  - no row / null deadline → legacy login allowed and rehashed to bcrypt;
  - `now >= deadline` → legacy login refused (fail closed, audited), even if
    the retirement batch has not run.
- The deadline is never inferred from deployment time.
- No CLI records it. The operator runs the SQL of step 10 with the migrator
  login inside the cutover window, once.

## ROLLBACK / RECOVERY

- Application rollback: redeploy the previous build; safe while the schema
  is forward-compatible (F8 changes no schema).
- Configuration rollback: env/secret revert; `auth:managed-role deactivate`
  disables `lu_auth_login`.
- Role/grant restoration: re-run is NOT safe after 0004 (guard refuses);
  restore grants from the pre-cutover backup or re-apply the specific GRANTs
  of 0004–0006; verify with `auth:managed-role verify`.
- Database: migrations are forward-only (no down migrations); recovery =
  restore from the step-1 backup, or forward-fix migration.
- Password compatibility: bcrypt rehashes are one-way (the original PBKDF2
  hash is replaced on first successful login); rolling back to ASP after
  users logged in requires the legacy DB (untouched, read-only) and loses
  target-side password changes. `cutover_at`/deadline are immutable once
  recorded; a mistaken value requires a DB restore.
- Before traffic opens (steps 1–14) abandoning the cutover = unfence ASP and
  discard the target DBs.

## RUNTIME CONFIGURATION

- Node 24.18.x, pnpm 12.4.2 via Corepack (`packageManager` pin), PostgreSQL
  16+ (local proof on 18).
- Roles: `lu_auth_migrator` (DDL, ledger), `lu_auth_runtime` (`NOLOGIN`,
  grants per roles/003 + 0004–0006), `lu_auth_login` (`LOGIN`, member of
  runtime), executor login for migrations.
- API env: control-plane DSN (runtime login), audit HMAC key, bcrypt cost
  (10–15, default 12), `AUTH_RATE_LIMIT_MAX_ATTEMPTS`,
  `AUTH_RATE_LIMIT_WINDOW_SECONDS`, `AUTH_RATE_LIMIT_IP_MAX_ATTEMPTS`,
  session TTLs, private photo storage path/bucket. No secrets in the
  repository.

## KNOWN LIMITATIONS

- P-05 (PARTIAL): no optimistic-concurrency contract (`If-Match` /
  `row_version`); `FOR UPDATE` serializes; last-SuperAdmin invariant proven
  live.
- B-08/C-14/E-16/F-13/H-07 (PARTIAL): inline alerts; no global toast.
- F-15/H-10 (NOT_APPLICABLE): no pre-submit SweetAlert confirmation (LC-01).
- I-09 / L-03 (PARTIAL / MISSING): People search does not match by Id.
- D-07 (PARTIAL) / I-10 (NOT_APPLICABLE): no Person details / Person create
  page (F1-D016).
- G-10 (PARTIAL): no `/Error` page; typed inline 404/403 with "Volver al
  Listado".
- D-01 (CONFLICT): Persona alta inline; no `/Persons/Create`.
- E-11 (CONFLICT): letters-only names not enforced (valid names would be
  rejected).
- I-07 (CONFLICT): People order `name ASC` (API reused).
- L-02 (CONFLICT): unique Person name (People model reused).
- N-08 (CONFLICT): `__Host-lu_session` cookie + approved TTL.
- J-05 (PARTIAL): profile audit actors only in admin Details, not `/Profile`.
- O-07 (PARTIAL): Simple-Line / Weather icon fonts not loaded.
- Resource existence 403/404 oracle (decision 4).
- Retirement batch not implemented (decision 8): legacy login fails closed
  at the deadline regardless.
- Legacy import executable not in the repository (step 8 of the checklist).

## COMMANDS TO REPRODUCE ACCEPTANCE

```
corepack pnpm run verify
git diff --check
```

Live (F7 harness, all phases, F8 product behavior verified by the final run
`0b853965`):

```
E2E_ADMIN_DSN=<loopback admin DSN> \
  [E2E_ADMIN_WINDOW_MODULE=<outside repo>] \
  [E2E_RECOVERY_FILE=<outside repo>] \
  node tests/e2e/stack/run-f7.mjs --phase=all
```

Never wrap the runner in `timeout`: the Playwright phase is synchronous, so a
killed runner cannot tear down.

## DEPLOYMENT PREREQUISITES

- Local PostgreSQL credentials are already provisioned outside tracked files.
- Production environment with PostgreSQL 16+, control-plane and tenant DBs,
  off-site backups, restore drill on a scratch server.
- Migration executor role (`lu_auth_migrator` or equivalent) reachable from
  the API host.
- Operador con login migrator y acceso al DSN del control plane para
  registrar `lu_identity_migration_state` (cutover step 10).
- ASP maintenance window fencing (legacy login + User writes read-only).
- Network access for the API to its private photo storage bucket/path.
- Secrets (audit HMAC key, bcrypt cost, rate-limit values, cookie settings,
  CORS/origin, photo storage) provisioned outside the repository.

## FOLLOW-UPS

Non-blocking follow-ups (decision-cited where applicable):

- Legacy password retirement batch + verifier removal (F1 §7; decision 8).
- Legacy user import executable (F1-D020) — deploy prerequisite (step 8).
- Optional: CLI to record cutover/deadline; shared SELECT constant for
  session lookups; explicit scope parameter in `AuthRateLimitService`;
  403→404 uniformity on command routes; optimistic concurrency contract
  (P-05); People Id search; Person details; `/Error` page; global toast.

## FILES CREATED / MODIFIED IN F8

Created:

- `apps/api/src/database/cutover-preflight.ts`
- `apps/api/test/cutover-preflight.e2e-spec.ts`
- `docs/migration/users/08-FINAL.md`
- `docs/migration/users/00-INDEX.md`

Modified:

- `apps/api/src/users/user.policy.ts`
- `apps/web/src/UsersPanel.tsx`
- `apps/web/src/UsersPanel.test.tsx`
- `apps/api/src/auth/auth.types.ts`
- `apps/api/src/auth/auth.service.ts`
- `apps/api/src/auth/auth.controller.ts`
- `apps/api/src/auth/auth-managed-role-cli.ts`
- `apps/api/src/database/migration-cli.ts`
- `apps/api/test/users.policy.e2e-spec.ts`
- `apps/api/test/auth.password-change.e2e-spec.ts`
- `apps/api/test/auth.controller.e2e-spec.ts`
- `apps/api/test/auth.login-f3.e2e-spec.ts`
- `apps/api/test/migration-cli.e2e-spec.ts`
- `apps/api/test/auth-managed-role-cli.e2e-spec.ts`
- `tests/e2e/specs/users.flows.spec.ts`
- `tests/e2e/specs/users.rbac.spec.ts`
- `tests/e2e/specs/auth.password.spec.ts`
- `tests/e2e/specs/auth.session.spec.ts`
- `tests/e2e/support/db.ts`
- Lint-only cleanup without semantic change in ~30 `apps/api` files (GATES
  worker scope: removed unused imports/types/helpers, fixed callback arity /
  interface signatures, dropped unused parameters, replaced `any` with a
  precise `LoginResponseBody`).
- Repo-wide Prettier formatting (F8 cosmetic pass on tracked files outside
  the historical migrations / `apps/api/database`).
- `docs/migration/users/00-PARITY-MATRIX.md` (`Estado F8` section
  appended; STATUS counts updated).

## NOT_APPLICABLE FOOTNOTE

All 27 `NOT_APPLICABLE` rows justified in `SOURCE→TARGET RECONCILIATION`
above. Summary, one line each:

- A-03: referencia muerta confirmada.
- A-06: no hay login externo en el sistema actual.
- B-10: arquitectura multi-sede (decision 12 plan).
- B-12: `HidePageTitle` no se usa en ninguna superficie MIG-001.
- E-09: F1-D008: comando global auditado, no UserController.
- E-12: jQuery eliminado.
- F-10: sin cookies sliding.
- F-12: F1-D007: rol por membresia; no hay rol Identity que sincronizar.
- F-15: LC-01: la confirmacion SweetAlert previa solo existe en guias.
- H-10: LC-01 (idem F-15): el Delete legacy es su pagina de confirmacion.
- I-01: F1-D016: el tab reutiliza la API/panel de People sin rediseño.
- I-03: F1-D016 / F1 §15: estados/persistencia de subtipo fuera de MIG-001.
- I-04: F1-D016 / F1 §15 (idem).
- I-10: F1-D016: ficha/auditoria de Persona fuera de MIG-001.
- I-11: F1 §15: concurrencia optimista de Persona e `ImportBatchId` fuera de
  MIG-001.
- K-05: decision 12 plan.
- M-03: F1-D016: esquema People (TPT vs tabla plana) no se rediseña.
- M-04: F1-D001: UUID canonico, no se busca paridad de tipo.
- M-05: F1-D005: bcrypt + verificadores ASP.NET Identity V2/V3 con rehash.
- M-06: F1-D012 congela semantica equivalente.
- M-07: F1-D007; global flag + membership role.
- N-01: F1-D007 (idem F-12).
- N-04: F1 §7/§12: reset administrativo bcrypt + `must_change_password` +
  version + revoke; sin equivalente del token Identity.
- N-06: sin cookies.
- O-05: SweetAlert2 reemplazada por el stack React (idem O-06).
- O-06: stack nuevo por plan.
- P-01: sin legacy runner.
