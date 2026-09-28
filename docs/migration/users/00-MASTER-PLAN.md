# 00-MASTER-PLAN.md — User/Person mig F0..F8 plan

> **Identidad**: trabajo `MIG-001-F0..F8`. Propietario temporal del plan (esta entrega):
> `migration-worker` (MiniMax-M3). Subagent_depth=1, no coordina delega. Este archivo solo congela los
> nombres exactos de las fases, sus entradas/salidas, superficies, agentes responsables, validadores,
> criterios de salida y artefactos a leer despues. **No implementa ni disena mas alla del handoff**.
>
> **Arquitectura fija e inamovible** (recordatorio ejecutivo):
>
> - Frontend: React + Vite + TypeScript (`apps/web/`).
> - Backend: NestJS + Fastify (`apps/api/`).
> - Base de datos final: PostgreSQL con **control plane central** + una **tenant DB por sede**
>   (unico destino).
> - Identidad local propia (sin SSO institucional; Keycloak diferido). **Sin JWT en
>   `localStorage`**.
> - Roles: `SuperAdmin` global; `Administrador`/`Supervisor` por sede.
> - Control plane central; routing de tenant DB server-side; transacciones solo dentro de una DB.
> - Escritor unico por capacidad y sede (`LEGACY_SQLSERVER`, `NEST_SQLSERVER`, `NEST_POSTGRES`).
>
> Esta arquitectura fue aprobada el 18-09-2026 y registrada en `docs/PLAN_MIGRACION_REACT_NESTJS.md`
> y `docs/CONTRATO_MULTISEDE.md`. **No se modifica aqui.** Las fases debajo la presuponen y la
> respetan. Donde haya duda, prevalece la arquitectura.

---

## F0 — SOURCE FORENSICS

- **INPUTS**:
  - Codigo ejecutable en `reference/asp-final @ dccabf50330afc48760d06bd4dbaff8c37ebbd3f`:
    `Pages/Users/*`, `Pages/Login*`, `Pages/Shared/_Layout*`, `Pages/Shared/_Sidebar*`,
    `Pages/Shared/_PersonsTable*`, `Pages/Persons/Index*`, `Pages/Shared/Components/UserProfile/*`,
    `Models/User.cs`, `Models/Person.cs`, `Models/Enums/UserRole.cs`,
    `Models/Enums/GeneralStatus.cs`, `Data/ApplicationDbContext.cs`, `Helpers/AuthorizationHelper.cs`,
    `Helpers/SafeImageUpload.cs`, `Helpers/IdentityRoleExtensions.cs`,
    `Helpers/TempDataExtensions.cs`, `Helpers/EnumHelper.cs`, `Program.cs`.
  - Codigo actual en `migration/react @ 7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (target):
    `apps/web/src/App.tsx`, `apps/web/src/UsersPanel.tsx`, `apps/web/src/PeoplePanel.tsx`,
    `apps/web/src/App.test.tsx`, `apps/web/src/styles.css`; `apps/api/src/users/*`,
    `apps/api/src/people/*`, `apps/api/src/auth/*`, `apps/api/src/core/*`;
    `apps/api/migrations/0001_*.sql`, `control-plane/0004_*.sql`, `control-plane/0005_*.sql`,
    `tenant/0012_*.sql`; `packages/contracts/src/{users,auth,site,people}.ts`,
    `packages/api-client/src/index.ts`.
  - Documentos marco: `AGENTS.md`, `context.md`, `docs/COORDINACION_MULTI_CHAT.md`,
    `docs/AGENT_WORKFLOW.md`, `Pages/AGENTS.md`, `Models/AGENTS.md`,
    `docs/PLAN_MIGRACION_REACT_NESTJS.md`, `docs/CONTRATO_MULTISEDE.md`.
- **OBJECTIVE**: producir la fuente de verdad para F1..F8. Cuatro documentos autocontenidos:
  `00-SOURCE-SPEC.md`, `00-PARITY-MATRIX.md`, `00-MASTER-PLAN.md` (este), `00-HANDOFF.md`. Sin
  disenar ni implementar mas alla del handoff.
- **EXPECTED FILE SURFACES**:
  - `docs/migration/users/00-SOURCE-SPEC.md` (alcance, baselines, metodologia, evidencia, inventario,
    recorridos de Login/Shell/Sidebar/Users/Index/Create/Edit/Details/Delete, clasificacion de
    exposicion, seccion visual obligatoria, modelo de datos legacy vs target, auditoria/security
    Personas, A-J, `LEGACY_CONTRADICTION`, `ARCHITECTURAL_MAPPING_REQUIRED`, UNKNOWN).
  - `docs/migration/users/00-PARITY-MATRIX.md` (tabla granular columnas `ID | AREA | ASP FEATURE |
    ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES`;
    conteos por estado).
  - `docs/migration/users/00-MASTER-PLAN.md` (este archivo).
  - `docs/migration/users/00-HANDOFF.md` (PHASE/F0 + secciones exactas pedidas).
- **PRIMARY AGENT**: `migration-worker` (MiniMax-M3) ejecuta F0; resultados validados por el
  orquestador.
- **VALIDATOR**: el orquestador verifica existencia, estado Git, diff y secretos; `critical-reviewer`
  realiza revision read-only por tratarse de autenticacion/datos.
- **EXIT CRITERIA**:
  - Los 4 docs existen, estan autocontenidos y registran todas las rutas mencionadas (verificables
    con `git ls-tree` o `git show`).
  - Las 10 afirmaciones A-J tienen fila dedicada con estado `CONFIRMED/PARTIAL/FALSE/CHANGED` y
    evidencia.
  - El punto H verifica especificamente `MIGRATION_REGISTRY`; no se sustituye por routing de sede.
  - Cada `LEGACY_CONTRADICTION` tiene evidencia A/B, conducta observable probable y necesidad F1.
  - El conteo de estados de la matriz es inequivoco: `PARITY/PARTIAL/MISSING/CONFLICT/
    NOT_APPLICABLE/UNKNOWN` sin cifras "indeterminadas".
  - `git diff --check` no emite warnings; `git status --short` muestra solo los 4 docs como
    cambios propios.
- **ARTIFACTS TO READ NEXT** (consumidos por F1):
  1. `00-SOURCE-SPEC.md` secciones 6 (modelo legacy vs target) y 7 (auth/security).
  2. `00-PARITY-MATRIX.md` bloques E (Create), F (Edit), M (data) y N (identity operations).
  3. `00-HANDOFF.md` secciones `DO_NOT_REOPEN_IN_F1`, `F1_REQUIRED_INPUTS`, `F1_OBJECTIVE`.

## F1 — IDENTITY & DATA CONTRACT

- **INPUTS**:
  - Artefactos de F0: `00-SOURCE-SPEC.md` (secciones 6 y 7), `00-PARITY-MATRIX.md` (bloques E, F,
    I, J, M, N), `00-HANDOFF.md` (`F1_REQUIRED_INPUTS`, `F1_OBJECTIVE`,
    `DO_NOT_REOPEN_IN_F1`).
  - Arquitectura: `docs/PLAN_MIGRACION_REACT_NESTJS.md` seccion 2 (decisiones aprobadas,
    especialmente #6 Identidad, #12 multi-sede).
  - Contrato multisede: `docs/CONTRATO_MULTISEDE.md` (capitulos 1..6).
  - Contratos target vigentes (solo lectura): `packages/contracts/src/{users,auth,site,people}.ts`.
  - Migraciones y registry vigentes (solo lectura): `apps/api/migrations/**`,
    `apps/api/src/database/migration-registry.ts`.
- **OBJECTIVE**: cerrar, sin implementar, el contrato vinculante de identidad/datos: mapeo de
  campos legacy; login username/email; estrategia PBKDF2->bcrypt; politica de password en bytes;
  rol SuperAdmin global y roles por sede; estados; auditoria; foto; relacion User/Person; acceso de
  SuperAdmin sin sede; eventos que rotan `security_version`; y alcance de los registros/manifiestos
  de migrations control-plane/tenant.
- **EXPECTED FILE SURFACES**:
  - Lectura: contratos, auth/users/people, migrations y registry actuales.
  - Escritura documental: `docs/migration/users/01-F1-CONTRACT.md` y
    `docs/migration/users/01-DECISIONS.md`.
  - **Sin** cambios SQL, NestJS, React, contracts ni API client en F1.
- **PRIMARY AGENT**: `architecture-admin` para contrato y decisiones; el orquestador conserva la
  puerta de fase.
- **VALIDATOR**: `data-analyst` valida integridad/mapeo; `critical-reviewer` valida autenticacion,
  SuperAdmin y sesiones. Validan contrato, no implementacion.
- **EXIT CRITERIA**:
  - Mapeo legacy -> target con tipos, nulabilidad, unicidad, normalizacion, procedencia y destino.
  - Decisiones explicitas para LC-02..LC-16 y Q-04; no quedan supuestos implicitos.
  - Contrato de migration registry/manifiestos aprobado para que F2 no ejecute SQL fuera de la
    ruta autorizada.
  - Backlog de implementacion asignado a F2/F3/F4/F5/F7, sin editar esas superficies en F1.
  - Sin secretos; `git diff --check` y estado Git registrados.
- **ARTIFACTS TO READ NEXT** (consumidos por F2):
  1. `01-F1-CONTRACT.md`.
  2. `01-DECISIONS.md`.
  3. Decision de migrations/registry que implementara F2.

## F2 — POSTGRESQL

- **INPUTS**:
  - `01-F1-CONTRACT.md`.
  - `apps/api/migrations/` existentes y `apps/api/src/database/migration-registry.ts`.
  - `apps/api/src/database/migration-*.ts`.
- **OBJECTIVE**: implementar y verificar en PostgreSQL local el esquema aprobado en F1, incluidos
  manifests/registry y separacion control-plane/tenant. Las credenciales locales ya estan
  provisionadas fuera de archivos tracked; no se crea una conexion alternativa.
- **EXPECTED FILE SURFACES**:
  - Nuevas migrations aditivas control-plane/tenant aprobadas en F1; nunca editar existentes.
  - `apps/api/src/database/migration-registry.ts`, schema manifests y pruebas del runner cuando el
    contrato F1 determine que deben cubrirlas.
  - Roles/grants PostgreSQL solo si el contrato lo exige.
  - `docs/migration/users/02-F2-APPLIED.md` (manifiesto: hashes, conteos, `DBCC` no aplica en PG,
    pero si `pg_constraint` y `pg_index` outputs).
- **PRIMARY AGENT**: `database-worker` (DeepSeek V4 Pro max).
- **VALIDATOR**: `data-analyst` (GPT-5.6 Luna max) verifica aplicacion idempotente; `qa-security-
  admin` verifica que `lu_auth_runtime` no tiene DDL y solo lectura/escritura.
- **EXIT CRITERIA**:
  - Schema aplicado en el entorno local ya provisionado; verificado con
    `pg_constraint`/`pg_index`/`pg_class` queries y `EXPLAIN` de indices esperados.
  - Roles `lu_auth_runtime` y `lu_auth_migrator` creados y con grants correctos.
  - Manifiesto `02-F2-APPLIED.md` con huella SHA-256 (no debe contener passwords ni connection
    strings).
  - `git diff --check` y `git status --short` muestran solo los nuevos `.sql` y `docs/`.
- **ARTIFACTS TO READ NEXT** (consumidos por F3):
  1. `02-F2-APPLIED.md`.
  2. `apps/api/migrations/` resultantes.

## F3 — NESTJS + AUTH

- **INPUTS**:
  - `01-F1-CONTRACT.md`, `02-F2-APPLIED.md`.
  - `apps/api/src/auth/*` (incluidos config, constants, types y rate-limit, auditados en F0).
  - `apps/api/src/core/*` (`core.guard.ts`, `core.module.ts`, `tenant-pg-pool.ts`).
  - Decisiones multi-sede (`docs/CONTRATO_MULTISEDE.md`).
- **OBJECTIVE**: implementar los endpoints NestJS para el flujo Users/Persons + login/sesion ya
  en su lugar pero extender para soportar el contrato F1 (campos ampliados, validaciones,
  bcrypt, `security_version`, `lu_auth_rate_limit`). No reescribir la capa de auth si esta
  cumple F1.
- **EXPECTED FILE SURFACES**:
  - `apps/api/src/users/user.repository.ts` (mapeos extendidos si F1 lo aprueba).
  - `apps/api/src/users/user.service.ts`, `user.controller.ts`.
  - `apps/api/src/people/person.repository.ts`, `person.service.ts`, `person.controller.ts`.
  - `apps/api/src/auth/auth.repository.ts`, `auth.service.ts`, `auth.controller.ts`,
    `auth.guard.ts`.
  - `apps/api/src/auth/auth.bootstrap-cli.ts`, `auth-managed-role-cli.ts` (herramientas CLI para
    provisionar role/SuperAdmin; **sin** automatizar en F3, mantener opt-in).
  - Pruebas `apps/api/test/users.controller.e2e-spec.ts`, `users.repository.e2e-spec.ts`,
    `people.controller.e2e-spec.ts`, `people.repository.e2e-spec.ts`, `users.rbac.e2e-spec.ts`,
    etc.
- **PRIMARY AGENT**: `nestjs-worker` (Kimi K2.7 Code) con `OpenCode Go`.
- **VALIDATOR**: `qa-security-admin` revisa el flujo de seguridad (CSRF, bcrypt, rate-limit,
  `security_version`, lockout behavior); `data-analyst` revisa cobertura de tests contra el
  contrato; `critical-reviewer` revisa el camino `SuperAdmin` global (ninguna API publica permite
  crear/quitar SuperAdmin via user controller).
- **EXIT CRITERIA**:
  - `apps/api/test/users.controller.e2e-spec.ts` y `apps/api/test/people.controller.e2e-spec.ts`
    aprobados localmente (sin requerir BD remota).
  - `apps/api/test/auth.controller.e2e-spec.ts` cubre la unificacion con `is_super_admin` (no
    permitido via API).
  - Ningun endpoint permite setear `is_super_admin` por HTTP; el unico camino es
    `auth.bootstrap-cli.ts` opt-in.
  - `git diff --check` y `git status --short` solo contienen los archivos esperados.
- **ARTIFACTS TO READ NEXT** (consumidos por F4):
  1. `apps/api/src/users/*`, `apps/api/src/people/*`, `apps/api/src/auth/*`.
  2. Migrations aplicadas en F2.

## F4 — CONTRACTS + API CLIENT

- **INPUTS**:
  - `apps/api/src/users/*`, `apps/api/src/people/*`, `apps/api/src/auth/*`.
  - `packages/contracts/src/{users,auth,site,people}.ts`.
  - `packages/api-client/src/index.ts`.
- **OBJECTIVE**: sincronizar el contrato API entre NestJS y React. Garantizar que el cliente
  generado manualmente refleja la API real; añadir/quitar campos siguiendo F1 + F3. Sin publicar
  la API todavia: solo alinear.
- **EXPECTED FILE SURFACES**:
  - `packages/contracts/src/users.ts`, `people.ts`, `auth.ts`, `site.ts`, `index.ts`.
  - `packages/api-client/src/index.ts`, `src/index.test.ts`.
- **PRIMARY AGENT**: `nestjs-worker` + el oficial `react-worker` segun se requiera.
- **VALIDATOR**: `architecture-admin`.
- **EXIT CRITERIA**:
  - `index.test.ts` (cliente) cubre los nuevos metodos; sin errores en compile.
  - Cambios compatibles con F5 (UI) sin reescrituras masivas.
- **ARTIFACTS TO READ NEXT** (consumidos por F5):
  1. `packages/contracts/` actualizado.
  2. `packages/api-client/src/index.ts` actualizado.

## F5 — REACT FUNCTIONAL PARITY

- **INPUTS**:
  - `apps/web/src/UsersPanel.tsx`, `PeoplePanel.tsx` (F0 inspect).
  - `packages/api-client/src/index.ts` (post-F4).
- **OBJECTIVE**: llevar el comportamiento funcional del ASP al `UsersPanel`/`PeoplePanel`/
  `ProfilePanel` y los handlers en `App.tsx` sin alteracion visual. Esto incluye: busqueda por
  nombre/email (CI queda pendiente o se reasigna), filtro estado, paginacion, alta, edicion
  (incluyendo `IdentityCard`/`phoneNumber`/`Position`/`Department`/`HireDate` si F1 los aprobo),
  cambio de password opcional, detalles (admin ficha + Mi Perfil autocentrado), soft-delete/
  disable, veto self, veto ultimo SuperAdmin, mensaje de error visible.
- **EXPECTED FILE SURFACES**:
  - `apps/web/src/UsersPanel.tsx`, `PeoplePanel.tsx` (refactor interno).
  - `apps/web/src/ProfilePanel` (o archivo equivalente en `UsersPanel.tsx`).
  - `apps/web/src/App.tsx` (rutas, navegacion, handlers de login/logout/sitePicker).
  - `apps/web/src/styles.css` (clases `.users-panel`, `.people-panel`, `.profile-panel`
    opcional; o se mantiene la regla `.catalog-panel` simple).
- **PRIMARY AGENT**: `react-worker` (Kimi K2.7 Code).
- **VALIDATOR**: `qa-security-admin` (pruebas de seguridad del lado cliente), `critical-reviewer`
  revisa que el veto self-edit y lockout no se rompe en UI.
- **EXIT CRITERIA**:
  - `apps/web/src/App.test.tsx` cubre flujo login + workspace; los tests de paridad
    UsersPanel/PeoplePanel existen y son verdes localmente.
  - Comportamiento equivalente a ASP en busqueda/filtros/paginacion/CRUD.
- **ARTIFACTS TO READ NEXT** (consumidos por F6):
  1. `apps/web/src/UsersPanel.tsx` + `PeoplePanel.tsx` + `ProfilePanel`.
  2. `apps/web/src/App.test.tsx`.

## F6 — VISUAL PARITY

- **INPUTS**:
  - F5 outputs.
  - `apps/web/src/styles.css`, `apps/web/src/main.tsx` (punto de montaje).
  - Estilo legacy documentado en `Pages/Users/*`, `Pages/Shared/_Layout*`,
    `Pages/Shared/_Sidebar*`.
- **OBJECTIVE**: replicar la jerarquia visual NiceAdmin (sin NiceAdmin real) en `apps/web/`.
  Conservar `app-shell`, `topbar`, `left-sidebar`, `page-wrapper`, badges, alerts, iconos MDI/FontAwesome,
  helpers del shell, header dropdown, topbar notifications, dashboard-like nav sidebar.
- **EXPECTED FILE SURFACES**:
  - `apps/web/src/styles.css` (anadir clases/reglas necesarias).
  - `apps/web/src/UsersPanel.tsx`, `PeoplePanel.tsx`, `App.tsx` (ajustes minimos de markup si
    F6 lo requiere).
- **PRIMARY AGENT**: `react-worker`.
- **VALIDATOR**: `qa-security-admin` (accesibilidad a11y basica), `critical-reviewer` (consistencia
  visual global).
- **EXIT CRITERIA**:
  - Paridad visual contra capturas o descripcion documentada en F0.
  - CSS sin overflow ni regresiones en breakpoints.
- **ARTIFACTS TO READ NEXT** (consumidos por F7):
  1. `apps/web/src/styles.css` y los paneles.

## F7 — E2E + VISUAL VERIFICATION

- **INPUTS**:
  - F5 + F6 outputs.
  - `apps/web/src/App.test.tsx`, `packages/api-client/src/index.test.ts`,
    `apps/api/test/*.e2e-spec.ts`.
- **OBJECTIVE**: integrar las pruebas del modulo Users/Persons con el resto. Sin dockers ni
  pipelines externos. Sin secretos pegados. Validacion end-to-end contra PostgreSQL local si
  esta aplicada en F2.
- **EXPECTED FILE SURFACES**:
  - `tests/e2e/users.persons.spec.ts` (si existe convencion) o
    `apps/api/test/users.persons.e2e-spec.ts`.
  - `apps/web/src/UsersPanel.test.tsx`, `PeoplePanel.test.tsx` (vitest).
  - Posibles scripts `scripts/e2e-visual/*.ts` para fotografia de pantallas (opt-in, no
    requerido por la regla de bajo consumo).
- **PRIMARY AGENT**: `test-worker` (Qwen 3.8 Flash xhigh).
- **VALIDATOR**: `qa-security-admin`, `critical-reviewer`.
- **EXIT CRITERIA**:
  - Suite verde localmente (vitest y nestjs e2e).
  - Sin saltar tests; sin markers de skip persistentes.
  - Sin secretos persistidos.
- **ARTIFACTS TO READ NEXT** (consumidos por F8):
  1. Reporte de corrida (local).
  2. Pruebas fixtures.

## F8 — RECONCILIATION + FINAL ACCEPTANCE

- **INPUTS**:
  - Artefactos de F5/F6/F7.
  - `00-PARITY-MATRIX.md` (F0) y eventual actualizacion al cierre de F5..F7.
  - `docs/PLAN_MIGRACION_REACT_NESTJS.md` (referencia arquitectonica).
- **OBJECTIVE**: reconciliar todo el modulo Users/Persons con el ASP. Cerrar las filas
  `PARTIAL/MISSING/CONFLICT` legitimas; las `NOT_APPLICABLE` deben justificarse en una nota al
  pie del handoff final. Decidir que se conserva en `Eliminado` legacy (snapshot/log). Decidir
  si la documentacion requiere un nuevo contrato que conserve paralelismo.
- **EXPECTED FILE SURFACES**:
  - `docs/migration/users/08-FINAL.md` (reconciliacion + matriz final + lista de gaps
    aceptados + decisiones).
  - Actualizaciones menores a `00-PARITY-MATRIX.md` para reflejar cierres (anexar al final o
    crear `08-FINAL-PARITY.md`).
- **PRIMARY AGENT**: `reconciliation-auditor` (GPT-5.6 Luna max). El orquestador hace la ultima
  revision.
- **VALIDATOR**: `critical-reviewer`, `architecture-admin`.
- **EXIT CRITERIA**:
  - `08-FINAL.md` cubre todas las filas del `00-PARITY-MATRIX.md` con resolucion (cerrada,
    diferida, justificada).
  - `docs/migration/users/` contiene la cadena completa F0..F8.
  - Sin secretos.
- **ARTIFACTS TO READ NEXT** (consumidos por orquestador y por sprints posteriores):
  1. `docs/migration/users/08-FINAL.md`.
  2. Indice de F0..F8 dentro de `docs/migration/users/00-INDEX.md` (a crear en F8).

---

## Politica transversal para todas las fases

- **Sin commits** sin aprobacion explicita.
- **Sin secrets persistidos**: solo la frase "local PostgreSQL credentials are already provisioned
  outside tracked files" cuando sea necesario hablar de credenciales en docs.
- **Sin regresiones**: las migraciones existentes son intocables. Nuevas migrations usan la
  siguiente secuencia aprobada por cada stream (control-plane/tenant), son aditivas y deben quedar
  registradas/manifiestadas antes de ejecutarse.
- **Sin servidor ni browser en segundo plano**. Builds con `-m:1`. Lotes <= 100 filas. **Nunca**
  ajustar la memoria global de SQL Server.
- **Propiedad temporal**: el orquestador declara un unico escritor activo por archivo. Los
  analisis de lectura se ejecutan en paralelo.
- **Bajo consumo**: F0/F1 no ejecutan datos. F2 solo aplica el esquema local aprobado y registra
  hashes/manifiestos; una carga de usuarios legacy requiere plan y evidencia separados.

## Riesgos vivos al cierre de F0

- Riesgo principal: el target **no** expone `IdentityCard`, `PhoneNumber`, `Photo`,
  `Position/Department/HireDate`, `SecurityStamp`, `2FA`, ni auditoria completa. F1..F8 deben
  cerrar o justificar cada uno.
- Riesgo secundario: `SuperAdmin` global no se gestiona via user controller. F1 debe definir el
  flujo opt-in (`auth.bootstrap-cli.ts`) sin automatizar.
- Riesgo terciario: la foto/perfil no tiene storage elegido. F1 debe decidir.
- Riesgo de bajo consumo: las migraciones Postgres pueden exceder la ventana de memoria local
  si se corre varios worker paralelos; coordinar con el orquestador.

> El plan respeta la regla "no implementar ni disenar mas alla del handoff" en F0. F1..F8 quedan
> registradas como fases, con superficies y criterios; la ejecucion depende del orquestador.
