# MIG-001 F5 — React functional parity handoff

## PHASE

F5 — REACT FUNCTIONAL PARITY

## STATUS

`COMPLETE`

The React application runs on the canonical F4 contracts/client against the F3
backend. Visual parity (F6) and the E2E suite (F7) were not started.

## EXECUTION_MODE

Claude Opus direct; no MiniMax dispatch, no subagents.

## TARGET_START_SHA / TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` / same (no commit).

## INPUT_ARTIFACTS

`00-MASTER-PLAN.md` (F5), `00-SOURCE-SPEC.md` §3.2–3.8 and §9, `01-DECISIONS.md`
(D009–D011, D016–D019), `01-IDENTITY-CONTRACT.md` §8–§11, §15–§18,
`04-HANDOFF.md`, `packages/contracts/src/{auth,users,people,api,site}.ts`,
`packages/api-client/src/index.ts`, current `apps/web/src/{App,UsersPanel,PeoplePanel}.tsx`.
Backend read only to confirm two behaviors: `POST /auth/password` returns 401
for both a wrong current password and an invalid session; People search matches
`name/email/actor_code`.

## FUNCTIONAL INVENTORY (before F5)

| Flow | Class | Resolution |
|---|---|---|
| Login | STALE | `signIn({ loginIdentifier })`; label "Usuario o Correo"; password `maxLength` 512 (login transport bound) |
| Session bootstrap | STALE | `currentSession()`; routes on `purpose`; workspace only after site context resolves |
| Site selection | STALE | picker from `eligibleSites`; `selectActiveSite`; 401 → login with 15-minute message |
| Password change | MISSING | restricted `password_change` view + `/Profile` form (`changePassword`) |
| SuperAdmin null-site normal session | CONFLICT (LC-11) | global workspace: Usuarios + Mi Perfil + site chooser; tenant routes hidden |
| Session failure | MISSING | central 401 guard (Proxy over the client) → login, no loop |
| Users list/create/edit/delete | STALE (`Legacy*`, `PUT /users/:id`) | split F3 commands, see below |
| Admin details | MISSING (G-01) | `/Users/Details/:userId` |
| Accounts + Personnel tabs | CONFLICT (C-01) | same-route tabs in `/Users/Index` |
| Profile | STALE | `/Profile` on `ProfileRecord` |
| Profile photo | MISSING | authorized byte routes, object URL, etag cache busting |
| People search/pagination | MISSING | server `searchTerm`, pagination UI, deleted filter `statusFilter=2` |

## WHAT_WAS_DELIVERED

### Auth / shell (`apps/web/src/App.tsx`)

- `AuthApi` = canonical client surface (`currentSession`, `signIn`,
  `selectActiveSite`, `changePassword`, `siteContext`, `logout`, users, profile, photo).
- Routing strictly on `purpose`: `normal` → workspace (site context first, or
  null-site global workspace for a SuperAdmin), `site_selection` → picker,
  `password_change` → forced change. `mustChangePassword` only selects the
  explanatory text (legacy > 72-byte password shows the policy text).
- No privileged render before resolution: `loading` until `siteContext()`
  answers.
- Central session failure: `withSessionGuard` wraps every non-auth client
  method; a 401 clears identity state and returns to login with a message. Auth
  flow methods handle their own 401 (no redirect loop).
- Wrong current password vs expired session: on 401 from `changePassword` the
  UI re-reads `currentSession()`; still valid → "La contraseña actual es
  incorrecta", else → login.
- Logout: `logout()` then clears auth, context, avatar, dashboard,
  notifications, identifier and password; cookies are left to the server.
- Site switcher lists `eligibleSites`; a normal session re-selects via
  `selectActiveSite`.
- Navigation: Users link is `global` (reachable without site); the separate
  "Personas" sidebar item was removed (B-04); `/Persons/Index` remains a valid
  admin route. Routes: `/Profile` (self), `/Users/Index[?tab=personas]`,
  `/Users/Create`, `/Users/Details/:id`, `/Users/Edit/:id[?returnUrl=Details]`,
  `/Users/Delete/:id`; `/Users/Details?id=` → admin path, `/Users/Details` → `/Profile`.
- Topbar avatar loads the own photo (`ownProfile` + `ownPhoto`), initials otherwise.

### Users module (`UsersPanel.tsx`, `usersModel.ts`)

- Index: search (`searchTerm`), status filter active/inactive/deleted (default
  hides deleted), Limpiar filtros, `X cuenta(s)`, server pagination; columns
  identity/role/effective status/work profile/registro; Editar/Detalles/Eliminar
  hidden where impossible (SuperAdmin target for site admins, self delete).
- Tabs: Cuentas de Acceso / Directorio de Personal. The directory reuses
  `PeoplePanel` with shared search/status (`deleted` → person status 2) and
  requires an active site with role Administrador (F1-D016).
- Create: site admin sends `CreateManagedUserSiteInput` (`role`); SuperAdmin
  sends `CreateManagedUserGlobalInput` (`memberships` from eligible sites). No
  SuperAdmin option anywhere. Work profile and photo are separate follow-up
  commands; failures are reported ("creada, pero no se aplicó: …") and the user
  lands on the details page.
- Edit: diffed `updateUserGlobalFields` (only changed keys), then
  `changeMembershipRole`, `updateMembershipWorkProfile`, `adminResetPassword`,
  in order; stops at the first failure and reports what was applied. Username
  read-only; own active-site role locked; SuperAdmin target read-only for site
  admins; own password is changed from `/Profile`, never via admin reset.
- Details: identity, contact, work profile per membership, account status,
  pending password change, audit actors (`createdBy/modifiedBy`; null =
  "Sistema"/"Sin modificaciones"), membership table with role toggle,
  suspend/reactivate, revoke, restore (with `restoreAccount` when the account
  was deleted by the cascade), validity and work-profile editors; account
  inactivate/activate; SuperAdmin-only restore account and add membership;
  admin password reset; photo upload/remove.
- Delete: dedicated confirmation. Site admin → `revokeMembership` (active
  site); SuperAdmin → site revoke or `deleteAccount` (global). Self and
  SuperAdmin-target blocked; already-deleted warning (H-08).
- Profile: names, email, phone (diffed `updateOwnProfile`), read-only username
  and CI, memberships, photo, password change. Identity changes refresh the
  canonical session (a self email change replaces the cookie server-side).

### Errors (`apiErrors.ts`)

`describeApiError` maps `kind`/`code`/`fieldErrors`/`retryAfterSeconds` to
Spanish text: 400 field errors (password violations and photo violations
translated), 401, 403 (`SITE_ACCESS_DENIED`, `CSRF_INVALID`/`ORIGIN_DENIED`,
RBAC), 404, 409 (server message), 413, 415, 429 (seconds), 503. Raw bodies and
stacks are never shown.

### Photo (`photo.ts`, `ProfilePhoto.tsx`)

Client checks (UX only): non-empty, ≤ 5 MiB, MIME in JPEG/PNG/WebP, extension
jpg/jpeg/png/webp. Upload sends raw bytes via the F4 client; server violations
are displayed. Images come from authorized byte routes as object URLs,
re-fetched when `etag` changes.

### People (`PeoplePanel.tsx`)

Server search, pagination, typed errors, optional shared filters from the
Users tab. The deleted filter sends `statusFilter: 2` (F3 returns deleted).

### F4 temporary compatibility removed

- `@lu/contracts`: all `Legacy*` types removed (`LegacyEmailLoginRequest`,
  `LegacyManagedUser*`, `LegacyCreate/Update*`, `LegacyProfileRecord`,
  `LegacyUpdateProfileInput`, `LegacyManagedUserPage`).
- `ApiClient`: deprecated `login`, `session`, `setActiveSite`, `users`, `user`,
  `createUser`, `updateUser` (`PUT /users/:id`), `deleteUser`, `profile`,
  `updateProfile` removed; their client tests replaced/removed.
- No remaining source consumer (`git grep` over `apps/web`, `packages`, `tests`).

### Deployed auth script

`tests/e2e/deployed-auth.js` sends `{ loginIdentifier, password, activeSiteId }`
and asserts `meta.purpose === 'normal'`. It reads `E2E_ADMIN_LOGIN` (username or
email) and still accepts the previous `E2E_ADMIN_EMAIL` variable name.

### Backend `email` login alias

Not removed (not assigned to F5). Remaining consumer: backend tests only
(`apps/api/test/auth.postgres.integration.e2e-spec.ts` calls
`AuthService.login({ email })` directly). React, the API client and the
deployed script no longer send it. Retirement belongs to F7/cutover.

## EVIDENCE_SUMMARY

| Gate | Result |
|---|---|
| `@lu/web` vitest (`App.test.tsx` 16, `UsersPanel.test.tsx` 19) | 35 / 35 PASS |
| `@lu/api-client` `node --test` | 30 / 30 PASS (deprecated-login test removed; command test no longer calls `deleteUser`) |
| `@lu/contracts` test (tsc) | PASS |
| `apps/api` `f4.contract-conformance` + `people.controller` specs | 9 / 9 PASS |
| `tests/e2e/deployed-auth.js` | `node --check` PASS; missing-env guard verified; live run not executed (needs deployed target) |
| Root `typecheck` | PASS |
| Root `build` | PASS |
| ESLint (`apps/web/src`, `packages/{api-client,contracts}/src`, script; `--max-warnings=0`) | PASS |
| Prettier (same scope) | PASS |
| Master-plan F5 exit: App.test covers login + workspace; UsersPanel/PeoplePanel parity tests exist and pass | PASS |
| `git diff --check` | PASS |
| `apps/api/src/**`, historical migrations | unchanged |

Test coverage (behavioral): login by username and by email (no `email` key),
invalid credentials, 429 with retry seconds, `site_selection` routing and
selection, site-selection expiry, `password_change` routing with
`mustChangePassword=false`, local policy check, wrong current password vs
expired session, bootstrap (unauthenticated, normal without premature render,
restricted), SuperAdmin null-site, central 401, route redirects, sidebar,
Supervisor restrictions, logout state clearing; users list/search/filter/
pagination, SuperAdmin/self action hiding, tabs + shared filters + People
Administrador requirement, create (site and global shapes, no SuperAdmin
option, work-profile follow-up, 409), edit (split commands, partial failure,
403), self/SuperAdmin locks, details and audit actors, account/membership
commands (409), membership restore with account, revoke site vs global delete,
self delete block, already-deleted warning, photo client validation and server
violation, profile update, People deleted filter and search, 403 People.

## FILES_CREATED

- `apps/web/src/apiErrors.ts`, `photo.ts`, `ProfilePhoto.tsx`, `usersModel.ts`,
  `UsersPanel.test.tsx`
- `docs/migration/users/05-HANDOFF.md`

## FILES_MODIFIED

- `apps/web/src/App.tsx`, `App.test.tsx`, `UsersPanel.tsx` (rewritten),
  `PeoplePanel.tsx`
- `packages/contracts/src/auth.ts`, `users.ts` (Legacy removal)
- `packages/api-client/src/index.ts`, `index.test.ts` (deprecated removal)
- `tests/e2e/deployed-auth.js`
- `docs/migration/users/00-PARITY-MATRIX.md` (F5 rows, counts, F5 section, I-01 pipe escape)

## KNOWN_REMAINING_FUNCTIONAL_GAPS

1. **J-05**: `GET /profile` exposes no audit actors; `/Profile` shows none (no
   fabricated data). Admin details show them.
2. No global toast/SweetAlert2 (B-08, O-05); feedback is inline/flash. No
   confirmation before submitting Edit (F-15). Legacy names letter-only input
   blocking not ported (E-11).
3. People: search has no Id equality (L-03), ordering by name (I-07), no
   Persons audit/detail page (I-10, outside MIG-001), directory keeps inline
   create (D-01), tab paging is not shared (D-06 PARTIAL).
4. No dedicated `/Error` page for 404 (G-10 PARTIAL).
5. `siteScope` (SuperAdmin site-scoped list) is not exposed in the UI; the
   SuperAdmin list is the global account view.
6. A site Administrador cannot know client-side whether it is the single-site
   custodian; global-field/status/reset actions are shown and the server's 403
   is displayed.
7. SuperAdmin create offers only its eligible sites as membership targets;
   other sites are linked from Details (also limited to eligible sites).
8. Create + follow-up commands are not atomic by design (F3 split); partial
   results are reported, not rolled back.

## KNOWN_PRE_CUTOVER_ITEMS

1. F3 live PostgreSQL validation still recommended before cutover.
2. `POST /auth/password` endpoint-specific rate limiting remains hardening backlog.
3. J-05 profile audit actors unresolved (backend/F7 decision).
4. Backend `email` login alias still accepted; retire after F7 confirms no consumer.
5. CORS preflight only under `/auth`: users/profile/photo/people assume
   same-origin deployment (unchanged from F4).

## DO_NOT_REOPEN_IN_F6

- Purpose-based routing, central 401 guard, split user commands, route map
  (`/Profile`, `/Users/Details/:id`, …), and the no-SuperAdmin-mutation rule.
- F6 restyles existing markup/classes (`catalog-*`, `nav-tabs customtab`,
  `user-detail-avatar`, `user-detail-initials`, `status-pill status-*`); it
  should not change behavior.

## F6_REQUIRED_INPUTS

1. `docs/migration/users/05-HANDOFF.md`
2. `apps/web/src/UsersPanel.tsx`, `PeoplePanel.tsx`, `ProfilePhoto.tsx`, `App.tsx`
3. `apps/web/src/App.test.tsx`, `UsersPanel.test.tsx` (behavior guard)
4. `00-PARITY-MATRIX.md` rows owned by F6 (B-06, C-12, C-13, D-07, F-01, F-11,
   G-01, G-07, G-09, H-01, O-*)
