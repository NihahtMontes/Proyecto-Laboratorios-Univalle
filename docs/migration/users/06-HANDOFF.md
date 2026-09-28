# MIG-001 F6 — Visual parity handoff

## PHASE

F6 — VISUAL PARITY

## STATUS

`COMPLETE`

The Users/Auth/Profile/People surfaces of `apps/web` reproduce the legacy
Razor markup, classes and stylesheet values on top of the F5 behavior. F7 (E2E
suite) was not started.

## EXECUTION_MODE

Claude Opus direct; no MiniMax dispatch, no subagents.

## TARGET_START_SHA / TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` / same (no commit).

## INPUT_ARTIFACTS

`00-MASTER-PLAN.md` (F6), `00-SOURCE-SPEC.md` §3.1, §5.1–5.6, `00-PARITY-MATRIX.md`,
`05-HANDOFF.md`. Legacy sources from `reference/asp-final @ dccabf5`:
`Pages/Login.cshtml`, `Pages/Shared/_Layout.cshtml`,
`Pages/Shared/Components/UserProfile/Default.cshtml`, `Pages/Shared/_PersonsTable.cshtml`,
`Pages/Users/{Index,Create,Edit,Details,Delete}.cshtml` (markup and `@section Styles`),
and the stylesheet `_Layout` loads: `wwwroot/dist/css/style.min.css`.

## VISUAL REFERENCE METHOD

**MIXED.** The legacy server was not launched (it needs SQL Server/Identity data,
and `AGENTS.md` forbids starting it unrequested). Instead:

- Legacy: the Razor markup of each page, transcribed with the same sample data,
  rendered statically with the legacy `wwwroot/dist/css/style.min.css` and FontAwesome.
- React: the real `<App/>` built by Vite with an in-memory API (same sample data).
- Both captured with headless Chrome (React 1440 px with shell; legacy page
  content at 1180 px, the React content width), and compared side by side.
- Surfaces without a legacy screen (site selection, forced password change)
  were checked against the login/`_Layout` language only.

Evidence (local, untracked): `.claude/runtime/f6-visual/shots/`
(`legacy-*.png`, `react-*.png`, `mobile-*.png`) and the reproducible tooling in
`.claude/runtime/f6-visual/tooling/` (preview harness, legacy static pages,
static server, capture script). The tooling is outside the app and was only run
temporarily; the server and browser were stopped afterwards.

## KEY FINDING — TWO NICEADMIN BUILDS

Legacy `_Layout` loads `wwwroot/dist/css/style.min.css`; the React app imports
`wwwroot/assets/css/style.min.css` (another NiceAdmin build). They differ in the
tokens the Users pages rely on: success `#5ac146` vs `#36bea6`, danger `#fa5838`
vs `#f62d51`, base font `.875rem` vs `1rem`, bold 700 vs 600, table cell padding
`1rem`, active page `#2962FF` vs `#7460ee`, `xl` breakpoint 1600 px vs 1200 px,
card borders, form-control borders/heights. Switching the global import would
restyle every other migrated module (outside F6), so F6 ports the dist values
**scoped to `.users-module`** (`styles.css`, "MIG-001 F6" blocks). The global
mismatch remains for the other modules — flagged for F7/F8.

## VISUAL INVENTORY (before → after F6)

| Surface | Route | Legacy | React | Before | After |
|---|---|---|---|---|---|
| Shell/topbar/sidebar | all | `_Layout`, `_Sidebar`, `UserProfile/Default` | `App.tsx` | PARTIAL (breadcrumb list, bell everywhere, avatar colors) | PARITY |
| Login | `/` (anon) | `Login.cshtml` | `App.tsx` | PARITY except email-only placeholder | PARITY |
| Users index + tabs + table | `/Users/Index` | `Users/Index.cshtml` | `UsersIndex`/`UserRow` | PARTIAL (generic `catalog-*`, text actions, invisible status pill) | PARITY |
| Personnel Directory | `/Users/Index?tab=personas` | `Index.cshtml` tab 2 + `_PersonsTable` | `PeoplePanel` | PARTIAL | PARITY (visual); D-01/D-07 functional gaps remain |
| Create | `/Users/Create` | `Users/Create.cshtml` | `UserCreate` | CONFLICT (flat fieldsets) | PARITY |
| Edit | `/Users/Edit/:id` | `Users/Edit.cshtml` | `UserEdit` | CONFLICT (no dark sidebar, no photo) | PARITY |
| Details | `/Users/Details/:id` | `Users/Details.cshtml` | `UserDetails` | CONFLICT (no layout, unstyled avatar) | PARITY |
| Delete | `/Users/Delete/:id` | `Users/Delete.cshtml` | `UserDelete` | PARTIAL | PARITY |
| Profile | `/Profile` | `Users/Details.cshtml` (self) | `ProfilePanel` | CONFLICT | PARITY (J-05 audit card omitted) |
| Profile photo | details/profile/edit/topbar/list | `Details`, `Edit`, `UserProfile` | `ProfilePhoto`, `UserRowAvatar` | MISSING styling | PARITY |
| Site selection | restricted session | none | `App.tsx` | consistent | consistent |
| Password change | restricted session + `/Profile` | none | `PasswordChangeForm` | plain form | consistent (legacy form language) |

## WHAT_WAS_DELIVERED

Markup/CSS only on top of F5 logic (state, API calls, commands and routing
unchanged), except the two small UI additions listed under "Behavior touched".

- **Shell** (`App.tsx`, `styles.css`): page header keeps only the page title
  (legacy `h4.page-title`; breadcrumb list removed, site chip kept — B-10);
  bell shown only on Dashboard, Managements, Acquisitions, Requests,
  Maintenances, Departures (legacy `showBell`); topbar avatar 31/50 px
  `bg-info` initials or photo; login placeholder `Ej: admin` (username or
  email, never "email only"); restricted views' "Cerrar Sesión" in legacy
  outline-rounded style.
- **Shared primitives** (`legacyUi.tsx`): `LegacyPagination` (numbered,
  Anterior/Siguiente only when applicable), `SmartIndexZone` (search input
  group + filter row + `aria-live` count), `FilterSelect`, `StatusBadge`.
- **Users index**: card with `nav-tabs customtab` (icons), header + "Vincular
  Usuario", Smart Index Zone, 6-column `table-hover v-middle` (avatar, CI,
  `fa-shield-alt` role, `@usuario` italic, `badge-pill` status, work profile,
  `dd/MM/yyyy` + creator initials/"Sist."), icon `btn-group` with titles,
  numbered pagination, `colspan=6` empty row.
- **Personnel Directory**: "Directorio Base de Identidades" header + "Nueva
  Identidad" (opens the inline form), legacy info alert (only with rows),
  `_PersonsTable` columns (initial circle, `ID:`, Interno/Externo badge-pill,
  phone/email, status badge, Editar/Eliminar icon buttons), dashed empty state.
- **Create**: `col-lg-10 col-xl-9` card, two columns "Identidad del Usuario" /
  "Seguridad y Privilegios" + "Perfil Laboral", legacy labels, placeholders and
  required markers, `custom-file` photo, username box with `fa-user-tag`,
  password input group with Ver/Ocultar and Generar icon buttons, right-aligned
  "Crear Cuenta"/"Cancelar".
- **Edit**: `col-lg-8` form (Identidad y Contacto / Privilegios de Acceso,
  C.I. info box, current photo `img-thumbnail` or "Sin foto de perfil",
  "Gestión de Credenciales" box, Cargo/Área-Dpto.) + `col-lg-4` dark "Estatus de
  Cuenta" card and role notice.
- **Details**: avatar 150 px, "Ficha de usuario", badges, Identidad / Contacto y
  perfil laboral with `user-detail-label/value` and legacy fallbacks, Auditoria
  card, dark "Seguridad de acceso", Acciones card; target-only membership table,
  add-membership form and credential reset in the same visual language.
- **Delete**: centered `col-lg-5` card, `fa-user-times`, key-data box, Ver
  Expediente / Sí, Confirmar Baja, "Volver al Control de Usuarios", Sugerencia
  Técnica.
- **Profile**: Details layout for self (editable identity/contact fields,
  photo controls, dark security card, memberships card, password change card).
- **CSS** (`styles.css`): legacy page `<style>` rules and NiceAdmin extras
  (`btn-circle`, `btn-rounded`, `customtab`, `v-middle`, `uppercase`, `italic`,
  `opacity-2`, `border-3`, `user-detail-*`), dist token block under
  `.users-module`, `xl`=1600 px emulation for `col-xl-9`, synthesized italics
  (Rubik has no italic face; React sets `font-synthesis: none` globally).

### Behavior touched (UI only, backend untouched)

1. **Edit photo** (F-11): choosing a file in Edit uploads it with
   `uploadUserPhoto` as one more ordered step when saving (after identity, role,
   work profile, password). Same partial-failure reporting as the other steps.
2. **Calendar dates**: hire date and validity were formatted as instants
   (`2024-02-01` showed `31/01/2024` west of UTC); they are now rendered from
   their `YYYY-MM-DD` part (`formatDay`). Regression test runs in `America/La_Paz`.
3. Details "Fecha de alta" and Edit "Registro Auditado" bind to the membership
   `hireDate` as legacy binds `User.HireDate` (creation date stays in Auditoria).

## INTENTIONAL DEVIATIONS

| Item | Legacy | Target | Reason |
|---|---|---|---|
| Stylesheet | dist build globally | dist values scoped to `.users-module` | avoid restyling non-MIG-001 modules |
| Page titles | empty `h4` for Index/Create/Edit/Delete (no `ViewData["Title"]`) | route titles (Usuarios, Vincular Usuario, …) | legacy defect; titles kept |
| Site chip | none | "Sede activa" chip | multi-site (B-10) |
| Smart Index Zone in Personas tab | zone only in tab 1 | shared zone in both tabs + type filter | tabs are route states; keeps F5 shared filters and type filter reachable |
| Person row subtitle | `ID: n` | `ID: n · code · category` | keeps F5 information |
| Personas "Nueva Identidad" | link to `/Persons/Create` | opens the inline form | D-01 (create stays inline) |
| Personas "Detalles" | present | absent | no Person detail page (D-07/I-10) |
| Row actions | always three | hidden when the contract forbids (SuperAdmin target, self delete) | F1 §8 / F5 rule |
| Role select | "-- Seleccionar Nivel de Acceso --" placeholder | defaults to Supervisor, options Supervisor/Administrador only | no SuperAdmin option (F1-D008) |
| Role label context | "Rol de Aplicación" (global) | same label + "Membresía en {sede}" help | role is per site |
| Password help | "Mínimo 8 caracteres…" (wrong, LC-16) | "Mínimo 12 caracteres, con mayúsculas, minúsculas, números y símbolos." | truthful policy |
| Edit sidebar id | `ID: #14` | `@usuario` | ids are UUIDs |
| "Ingreso al Sistema" | always "Habilitado" | "Habilitado" or the effective membership status | truthful |
| Aviso de rol | "…siguiente inicio de sesión" | "…siguiente solicitud del usuario" | F3 revalidates per request |
| Sugerencia Técnica | "desde el editor" | "desde la ficha del usuario" | inactivation lives in Details |
| Delete confirm label | "Sí, Confirmar Baja" | "Sí, Confirmar Baja en {sede}" (+ SuperAdmin global delete) | site revoke vs global delete (F3) |
| Extra target fields | — | Fecha de ingreso, Sede de la membresía, membership table, credential reset, account inactivate/restore, photo controls in Details/Profile | F3/F5 capabilities, styled in legacy language |
| Profile audit | Auditoria card (Details for self) | omitted | J-05: `GET /profile` has no actors; nothing fabricated |
| Labels without accents | "Telefono", "Auditoria", "Ultima modificacion" (Details) | copied as in legacy | literal |
| FA icons ≥ 5.1 (`fa-users-cog`, `fa-user-slash`, `fa-user-check`, `fa-user-shield`, `fa-user-tag`, `fa-user-edit`, `fa-user-minus`) | referenced, absent in repo FA 5.0.9 → blank | same classes, same blank | same asset; no assets downloaded |

## REMAINING VISUAL GAPS

- Global toast (SweetAlert2 top-end) not ported: messages are inline alerts
  styled as NiceAdmin alerts (B-08, C-14, E-16, F-13, H-07, O-05).
- No SweetAlert pre-submit confirmation (F-15, H-10); membership/account
  commands keep `window.confirm`.
- `HidePageTitle` toggle not ported (B-12, unused by Users/Persons).
- Other migrated modules still render with the `assets` NiceAdmin build.
- Shell background `#eef5f9` (styles.css) vs dist `#f2f4f5`: shell-wide, not
  changed.

## FUNCTIONAL GAPS INTENTIONALLY NOT ADDRESSED (carried from F5)

1. No global toast / pre-submit confirmation.
2. Letters-only blocking on name fields not ported (E-11).
3. People search does not match Id (I-09, L-03).
4. People create remains inline (D-01); no Person details (D-07, I-10).
5. No dedicated `/Error` page (G-10).
6. SuperAdmin users list has no site filter (`siteScope` not exposed).
7. A site Administrador cannot predict global-field-edit authorization (server 403 shown).
8. Create + follow-up work profile/photo is not atomic (reported, not rolled back);
   the Edit photo step is likewise a separate command.
9. J-05 profile audit actors unavailable from the backend.

## EVIDENCE_SUMMARY

| Gate | Result |
|---|---|
| `@lu/web` vitest (`App.test.tsx` 17, `UsersPanel.test.tsx` 23) | 40 / 40 PASS |
| `@lu/api-client` `node --test` (F5 regression, untouched) | 30 / 30 PASS |
| Web typecheck (`tsc -b`) | PASS |
| Web build (`vite build`) | PASS |
| Root `typecheck` | PASS |
| Root `build` | PASS |
| ESLint (changed `apps/web/src` files, `--max-warnings=0`) | PASS |
| Prettier (changed files) | PASS |
| Master-plan F6 exit: parity against captures/F0 description | PASS (MIXED method above) |
| Master-plan F6 exit: no overflow / breakpoint regressions | PASS — probe `scrollWidth == clientWidth` for all 10 surfaces at ~490 px and 768 px; tables scroll inside `.table-responsive` as in legacy |
| `git diff --check` | PASS |
| `apps/api/src/**`, `packages/contracts/**`, `packages/api-client/**`, migrations | unchanged by F6 |

Test changes: label strings updated where the legacy labels replaced F5 labels
(Primer Apellido, Cédula de Identidad, Correo Institucional, Teléfono de
Contacto, Nombre de Usuario (Login), Contraseña de Acceso, Rol de Aplicación,
"Buscar usuario", Details audit lines split, Profile "Correo institucional").
Behavior assertions unchanged. New: legacy badges/icon actions/aria-live
count/numbered pagination; Edit photo step; Details layout + calendar dates in
UTC−4; Personas "Nueva Identidad" + type filter in the shared zone; legacy
`showBell` + neutral login placeholder.

Accessibility kept: every field has a `<label htmlFor>`; icon-only buttons have
`aria-label` + `title`; decorative icons `aria-hidden`; initials avatar is
`role="img"` with a label; the hidden file input stays focusable with a visible
focus ring; tabs keep `role="tab"`/`aria-selected`.

## FILES_CREATED

- `apps/web/src/legacyUi.tsx`
- `docs/migration/users/06-HANDOFF.md`

## FILES_MODIFIED

- `apps/web/src/UsersPanel.tsx` (markup; Edit photo step; `formatDay`)
- `apps/web/src/PeoplePanel.tsx` (markup; form opens from "Nueva Identidad")
- `apps/web/src/ProfilePhoto.tsx` (markup)
- `apps/web/src/App.tsx` (page header, `showBell`, login placeholder, logout button style)
- `apps/web/src/styles.css` (F6 blocks, topbar avatar)
- `apps/web/src/App.test.tsx`, `apps/web/src/UsersPanel.test.tsx`
- `docs/migration/users/00-PARITY-MATRIX.md` (29 rows, counts, "Estado F6")

## PARITY MATRIX

PARITY 96, PARTIAL 25, MISSING 9, CONFLICT 15, NOT_APPLICABLE 13, UNKNOWN 0 —
total 158 (direct recount; F5 close recount 68/31/24/22/13 matched the declared
figures). Transitions listed in "Estado F6".

## KNOWN_PRE_CUTOVER_ITEMS (carried)

1. F3 live PostgreSQL validation still recommended before cutover.
2. `POST /auth/password` endpoint-specific rate limiting remains hardening backlog.
3. J-05 profile audit actors unavailable from the backend.
4. Backend `email` login alias still accepted; only remaining consumer:
   `apps/api/test/auth.postgres.integration.e2e-spec.ts`.
5. CORS preflight only under `/auth` (same-origin assumption, unchanged).
6. New: the React app imports the `assets` NiceAdmin build while legacy used
   `dist`; decide globally in F7/F8 (MIG-001 surfaces already scoped).

## DO_NOT_REOPEN_IN_F7

- F5 routing/commands/guards and the F6 markup/labels above; F7 asserts them.
- `.users-module` token scope (do not switch the global stylesheet inside a
  test phase without a decision).

## F7_READY

YES.

## F7_REQUIRED_INPUTS

1. `docs/migration/users/06-HANDOFF.md`, `05-HANDOFF.md`
2. `apps/web/src/{App,UsersPanel,PeoplePanel,ProfilePhoto,legacyUi}.tsx`, `styles.css`
3. `apps/web/src/{App,UsersPanel}.test.tsx`
4. `.claude/runtime/f6-visual/` (captures + tooling) for any visual regression step
5. `00-PARITY-MATRIX.md` rows still open (see "Estado F6")
