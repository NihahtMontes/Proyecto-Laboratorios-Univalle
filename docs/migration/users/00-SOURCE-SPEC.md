# 00-SOURCE-SPEC.md — User/Person mig F0 source forensics

> **Identidad**: trabajo `MIG-001-F0`. Propietario temporal: `migration-worker` (MiniMax-M3). Solo se
> escriben los cuatro documentos de este handoff. No se modifica ASP, React, NestJS, contratos,
> migrations, tests, PostgreSQL ni configuración. No se ejecuta mutación de base de datos, build ni
> servidor.
>
> **Politica de propiedad**: este autor es el unico escritor inicial de los cuatro archivos listados
> mas abajo. Sobre `reference/asp-final` solo se ejecuta `git show`/`git grep`/`git ls-tree` (no
> checkout, no reset, no cambios ajenos revertidos). Sobre `migration/react` (HEAD) se inspecciona
> sin `git checkout` y se reporta la situacion de `working tree` en la seccion `VALIDATION` del
> handoff.

---

## 1. Alcance, baselines y metodologia

### 1.1 Alcance

Auditoria forense exhaustiva minima del flujo **Login -> shell/header/sidebar -> Usuarios -> tabs
Index/Persons -> Create/Edit/Details/Delete** de la referencia ASP en `reference/asp-final` y auditoria
dirigida del estado actual del target (React + NestJS + PostgreSQL) en el worktree `migration/react`.

No entran en alcance: otros modulos ASP (L-6, L-3, Kardex, L-12, Catalogos, Gestion, Wizard) salvo
en lo que el shell del sidebar y la navegacion de `Pages/Shared/_Sidebar.cshtml` exponen como
referencia de superficie visible. La reconstruccion se limita al subconjunto de archivos listados por
la orquestadora.

### 1.2 Baselines

| Concepto | Valor |
|---|---|
| Branch actual | `migration/react` |
| HEAD inicial (apuntado por el orquestador) | `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` |
| HEAD real al finalizar F0 (ver `VALIDATION`) | `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (sin commit) |
| Source ref | `reference/asp-final` |
| Source SHA | `dccabf50330afc48760d06bd4dbaff8c37ebbd3f` |
| Working tree de `apps/**`, `packages/**`, `wwwroot/**`, `Pages/**` | sin cambios del worker (ver `VALIDATION` del handoff); solo se crearon los cuatro docs |

### 1.3 Metodologia

1. Lectura de reglas globales y locales: `AGENTS.md`, `context.md`,
   `docs/COORDINACION_MULTI_CHAT.md`, `docs/AGENT_WORKFLOW.md`, `Pages/AGENTS.md`,
   `Models/AGENTS.md`.
2. Lectura del plan y contratos vigentes: `docs/PLAN_MIGRACION_REACT_NESTJS.md`,
   `docs/CONTRATO_MULTISEDE.md`.
3. Inspeccion de legacy en `reference/asp-final` usando unicamente comandos `git show`,
   `git grep` y `git ls-tree` sobre la ref. Sin `git checkout` contra esa ref. Sin modificacion de
   ninguna ruta fuera de `docs/migration/users/00-*.md`.
4. Inspeccion de target en el worktree actual. Sin `git checkout`, `git reset`, ni mutaciones de
   rama. Solo lectura.
5. Registro de diferencias entre `HEAD` y `working tree` (en `VALIDATION` del handoff). El worker
   no revierte cambios previos y no toca `.opencode/agents/**` ni `opencode.json`.
6. Verificacion explicita de las 10 afirmaciones A-J del orquestador (seccion 9) con evidencia en
   codigo o rutas inexistentes (registro como `UNKNOWN`).
7. Emision de los cuatro documentos autocontenidos requeridos.

### 1.4 Reglas de evidencia aplicadas

- Citamos ruta relativa al ref mostrado o al worktree.
- Citamos linea o simbolo cuando es practico.
- No pegamos secretos. Credenciales solo se mencionan en el lenguaje textual requerido por la
  orquestadora: *"local PostgreSQL credentials are already provisioned outside tracked files"*. No se
  persiste ninguna clave ni valor de conexion.
- Sin secretos: cero connection strings, cero valores de contrasena, cero tokens versionados.
- Toda ruta citada se confirma con `git ls-tree`, `git show` o `read` antes de incluirse.

---

## 2. Inventario de archivos (evidencia)

### 2.1 Legacy (ref `reference/asp-final @ dccabf50330afc48760d06bd4dbaff8c37ebbd3f`)

Archivos inspeccionados via `git show`:

| Capa | Ruta | Lectura |
|---|---|---|
| Pages | `Pages/Users/Index.cshtml` | leido |
| Pages | `Pages/Users/Index.cshtml.cs` | leido |
| Pages | `Pages/Users/Create.cshtml` | leido |
| Pages | `Pages/Users/Create.cshtml.cs` | leido |
| Pages | `Pages/Users/Edit.cshtml` | leido |
| Pages | `Pages/Users/Edit.cshtml.cs` | leido |
| Pages | `Pages/Users/Details.cshtml` | leido |
| Pages | `Pages/Users/Details.cshtml.cs` | leido |
| Pages | `Pages/Users/Delete.cshtml` | leido |
| Pages | `Pages/Users/Delete.cshtml.cs` | leido |
| Pages | `Pages/Login.cshtml` | leido |
| Pages | `Pages/Login.cshtml.cs` | leido |
| Pages | `Pages/Shared/_Layout.cshtml` | leido completo (517 lineas) |
| Pages | `Pages/Shared/_Sidebar.cshtml` | leido |
| Pages | `Pages/Shared/_PersonsTable.cshtml` | leido |
| Pages | `Pages/Shared/Components/UserProfile/Default.cshtml` | leido |
| Pages | `Pages/Shared/Components/UserProfileViewComponent.cs` | leido |
| Pages | `Pages/Persons/Index.cshtml` | leido |
| Pages | `Pages/Persons/Create.cshtml` y `.cshtml.cs` | leidos |
| Pages | `Pages/Persons/Edit.cshtml` y `.cshtml.cs` | leidos |
| Pages | `Pages/Persons/Details.cshtml` y `.cshtml.cs` | leidos |
| Pages | `Pages/Persons/Delete.cshtml` y `.cshtml.cs` | leidos |
| Pages | `Pages/_ValidationScriptsPartial.cshtml` y `Pages/Shared/_ValidationScriptsPartial.cshtml` | leidos |
| Models | `Models/User.cs` | leido |
| Models | `Models/Person.cs` | leido |
| Models | `Models/Intern.cs`, `Models/Extern.cs` | leidos |
| Models | `Models/Enums/UserRole.cs` | leido |
| Models | `Models/Enums/GeneralStatus.cs` | leido |
| Helpers | `Helpers/AuthorizationHelper.cs` | leido |
| Helpers | `Helpers/SafeImageUpload.cs` | leido |
| Helpers | `Helpers/IdentityRoleExtensions.cs` | leido |
| Helpers | `Helpers/TempDataExtensions.cs` | leido |
| Helpers | `Helpers/EnumHelper.cs` | leido |
| Helpers | `Helpers/StringExtensions.cs`, `Helpers/PaginatedList.cs` | leidos |
| Program | `Program.cs` | leido |
| Data | `Data/ApplicationDbContext.cs` | leido (esquema + check constraints; metadatos `User`/`Person`) |
| JavaScript | `wwwroot/js/site.js` | leido; live filters y SweetAlert de rollback de wizard |

Inventario de `wwwroot` conocido por `git ls-tree reference/asp-final` (verificado por
`git ls-tree reference/asp-final -r --name-only | Select-String wwwroot`):

- `wwwroot/lib/{bootstrap,jquery,jquery-validation,jquery-validation-unobtrusive}/dist/*` (assets
  estaticos y JS minimizados); `wwwroot/dist/css/*.min.css`, `wwwroot/dist/js/app.min.js`,
  `wwwroot/dist/js/waves.js`, `wwwroot/dist/js/sidebarmenu.js`, `wwwroot/dist/js/custom.min.js`.
- Iconos FontAwesome, Simple-Line-Icons, Material-Design-Iconic-Font, Themify-Icons, Weather-Icons
  y Flag-Icon-CSS en `wwwroot/dist/css/icons/*`.
- Logos `favicon.ico` y plantillas en `wwwroot/templates/` (no se usan en el flujo Usuarios).
- Uploads de usuario existentes: 6 archivos en `wwwroot/uploads/users/*` (ver tabla 2.3) y carpeta
  `wwwroot/uploads/users/.gitkeep`.
- 13 plantillas `wwwroot/templates/*.xlsx` se usan para reportes, no para usuarios.
- Paquete NiceAdmin duplicado bajo `Pages/niceadmin/*` (carpeta obsoleta con assets duplicados;
  el consumidor real es `wwwroot/lib/...` y `wwwroot/dist/...`).

### 2.2 Target (worktree `migration/react @ 7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`)

Inspeccion `git ls-tree HEAD --name-only`:

- `apps/web/src/App.tsx` (leido completo, 2132 lineas).
- `apps/web/src/UsersPanel.tsx` (leido, 336 lineas).
- `apps/web/src/PeoplePanel.tsx` (leido, 373 lineas).
- `apps/web/src/styles.css` (bloques relevantes: `.catalog-panel`; selectores `.users-panel`, `.people-panel` y `.profile-panel` no definidos; estados y shell).
- `apps/web/src/App.test.tsx` (leido, 240 lineas).
- `apps/api/src/users/user.controller.ts` (leido).
- `apps/api/src/users/user.service.ts` (leido).
- `apps/api/src/users/user.repository.ts` (leido).
- `apps/api/src/people/person.controller.ts`, `person.service.ts`, `person.repository.ts` (leidos).
- `apps/api/src/auth/auth.controller.ts` (leido).
- `apps/api/src/auth/auth.service.ts` (leido).
- `apps/api/src/auth/auth.repository.ts` (leido).
- `apps/api/src/auth/auth.constants.ts`, `auth.config.ts`, `auth.types.ts` (leidos).
- `apps/api/src/auth/auth.guard.ts` (imports).
- `apps/api/src/core/core.guard.ts` (leido).
- `apps/api/src/core/core.module.ts` (leido).
- `apps/api/src/core/tenant-routing.ts` (simbolos `PostgresTenantRouteCatalog` y `TenantRouter`).
- `apps/api/src/database/migration-registry.ts`, `migration-runner.ts`, `migration-cli.ts` (leidos).
- `apps/api/migrations/0001_create_identity_control_plane.sql` (leido).
- `apps/api/migrations/control-plane/0004_academic_catalogs.sql` (leido).
- `apps/api/migrations/control-plane/0005_user_management.sql` (leido).
- `apps/api/migrations/tenant/0001_dashboard_foundation.sql` (existencia).
- `apps/api/migrations/tenant/0012_people.sql` (leido).
- `packages/contracts/src/users.ts` (leido).
- `packages/contracts/src/auth.ts` (leido).
- `packages/contracts/src/site.ts` (leido).
- `packages/contracts/src/people.ts` (leido).
- `packages/api-client/src/index.ts` (leido parcial: 200-450 lineas).
- `apps/api/test/migration-registry.e2e-spec.ts` y pruebas auth relevantes (leidas de forma dirigida).
- `apps/web/src/UsersPanel.tsx` y `apps/web/src/PeoplePanel.tsx` re-leidos para patrones visuales y
  copy literal ("Cargando cuentas…", "Cargando personas…", "No hay cuentas", etc.).

### 2.3 Uploads/users conocidos (referencia, no parte del handoff)

Existentes en `reference/asp-final:wwwroot/uploads/users/`:

| Archivo (stub) | Tamano irrelevante |
|---|---|
| `wwwroot/uploads/users/.gitkeep` | placeholder |
| `wwwroot/uploads/users/454166cfe64840808f86e36a3d3507f4_rayito.png` | archivo |
| `wwwroot/uploads/users/542a2b69-fc24-4892-adf1-58fc3712e014_imagen.png` | archivo |
| `wwwroot/uploads/users/732b9f63-ba40-405e-80e7-06f264db1ff5_rayito.png` | archivo |
| `wwwroot/uploads/users/db24dd84-4e2d-4db2-ae6c-c1a811b5fb22_rayito2.png` | archivo |
| `wwwroot/uploads/users/edd7819d-6085-4e81-91e2-c647a461a649_rayito 3.png` | archivo |

> Los nombres de archivo almacenados son generados por `SafeImageUpload.CreateSafeFileName` (formato
> `{guid:N}{extension}`, p. ej. `454166cfe64840808f86e36a3d3507f4_rayito.png` — antes del guion bajo
> hay un guid hex sin guiones concatenado al nombre original con guion bajo). No se exponen como
> identidad; sirven de cache-busting en `Details.cshtml`/`Default.cshtml` mediante
> `?v={LastModifiedDate.Ticks}`.

---

## 3. Reconstruccion del flujo real

### 3.1 Login -> shell/header/sidebar

#### 3.1.1 Login (`Pages/Login.cshtml`, `Pages/Login.cshtml.cs`)

- Ruta Razor: `/Login` (`@page`, layout nula declarado en `.cshtml`: `@{ Layout = null; }`).
- `Login.cshtml.cs` declara `[AllowAnonymous]` y maneja `OnGetAsync(returnUrl)` y `OnPostAsync`.
- `OnGetAsync`: limpia `IdentityConstants.ExternalScheme` via `HttpContext.SignOutAsync` y muestra el
  formulario; consume `?returnUrl=` query param.
- `OnPostAsync`:
  - Normaliza el input: `var login = Input.UserName.Trim();`
  - `var user = await _userManager.FindByNameAsync(login) ?? await _userManager.FindByEmailAsync(login);`
    **el nombre de usuario o el correo funcionan como identificador de login.** Confirmado en la
    declaracion `InputModel.UserName` con atributo `Display(Name = "Usuario o Correo")` y placeholder
    `Ej: admin` (no se observa placeholder de correo en el `.cshtml` para `UserName`).
  - Si `user == null` o `user.Status != GeneralStatus.Activo`, devuelve error generico.
  - `PasswordSignInAsync(user, password, Input.RememberMe, lockoutOnFailure: true)` usa el lockout
    configurado en `Program.cs`: cinco fallos y bloqueo temporal de 15 minutos; no fija `MaxValue`
    en el login.
  - Ramas: `Succeeded` -> `LocalRedirect(returnUrl)`; `RequiresTwoFactor` -> redirige a
    `./LoginWith2fa`; `IsLockedOut` -> redirige a `./Lockout`; resto -> "Intento de inicio de sesion
    no valido".
  - `SignIn.RequireConfirmedAccount = false`, `EmailConfirmed = true` aplicado por `CreateAsync` en
    alta. No hay confirmacion por email.

#### 3.1.2 Shell global (`Pages/Shared/_Layout.cshtml`)

- Lee cookie CSRF: `meta name="request-verification-token" content="@antiforgeryToken"`.
- Inyecta CDN interna: `dist/css/style.min.css`, `sweetalert2.min.css`, `c3.min.css`,
  `css/smart-index.css` y los `Scripts` con `app.min.js`, `app.init.js`, `custom.min.js`,
  `sweetalert2.all.min.js`, `d3.min.js`, `c3.min.js`, `sidebarmenu.js`, `waves.js`.
- Cabecera (`topbar`): logo `assets/images/logo-icon.png` + logo-light; cuadro de busqueda
  decorativo; boton de notificaciones (`showBell` solo si la pagina es Dashboard, Managements,
  Acquisitions, Requests, Maintenances o Departures); `UserProfile` ViewComponent.
- `UserProfile` lee de `ICurrentUserService.UserId` y construye el ViewModel
  `FullName/Email/PhoneNumber/ProfilePictureUrl/Initials/ProfilePictureVersion` leyendo `_context.Users`
  con `AsNoTracking()`.
- Sidebar se carga como partial `Pages/Shared/_Sidebar.cshtml`.
- Toast SweetAlert2 global al final: lee `TempData["SuccessMessage"] ?? TempData["Success"]` (idem
  `Error/Warning/Info`) y los muestra como `mixin(toast:true, position:'top-end', timer:4000)`. Los
  handlers en `.cshtml.cs` deben llamar `TempData.Success(...)` / `TempData.Error(...)` (ver
  `Helpers/TempDataExtensions.cs`).

#### 3.1.3 Sidebar (`Pages/Shared/_Sidebar.cshtml`)

- Aside `.left-sidebar[data-sidebarbg="skin5"]` con `<ul id="sidebarnav">`.
- Bloques (orden presente):
  - "Inicio" -> `Ver` (`/AssetView/Index`) y grupo "Operacion de laboratorios" -> Resumen, Paso 1,
    Paso 2, Paso 3, Paso 4, Paso 5, Demanda, Calendarios.
  - "Elementos" -> grupo "Procesos" -> Gestiones, Man. Preventivos, Man. Correctivos.
  - "Equipos y Actividades" -> Equipo + grupo "Procesos" -> L-6, L-7, Adquisiciones, L-8, L-3
    + grupo "Catalogos" -> Laboratorios, Regiones.
  - "Personas" -> grupo "Usuarios" -> `Usuarios` (`/Users/Index`).
- Cada `<li class="sidebar-item">` lleva `<a class="sidebar-link waves-effect waves-dark"
  asp-page="/Users/Index">` con icono `mdi mdi-account-multiple` y label "Usuarios". **No hay entrada
  separada "Personas" en el sidebar**; el directorio de Personas se accede desde el tab "Directorio
  de Personal" dentro de `/Users/Index` (pagina con dos tabs) o desde el menu contextual del dropdown
  del usuario (no hay enlace directo en sidebar). Confirmado por
  `Pages/Users/Index.cshtml` (estructura de tabs interna) y por la *ausencia* de un item de sidebar
  `/Persons/Index` en `_Sidebar.cshtml`.

### 3.2 Usuarios - Index con tabs (`Pages/Users/Index.cshtml`, `Index.cshtml.cs`)

- Ruta: `/Users/Index` (`[Authorize(Roles = AuthorizationHelper.AdminRoles)]`,
  `AdminRoles = "Administrator,SuperAdmin"`).
- Vista: card `.card.shadow-sm.border-0` con `ul.nav.nav-tabs.customtab` y dos tabs:
  - `#usuarios` activo por defecto, titulo "Cuentas de Acceso"; icono `fas fa-users-cog`.
  - `#personas`, titulo "Directorio de Personal"; icono `fas fa-id-card`.
- **Tab Usuarios (Cuentas de Acceso):**
  - Card-body con cabecera `Control de Usuarios / Administracion de credenciales, roles y estatus de
    seguridad del sistema.` y boton a la derecha: `Vincular Usuario` -> `asp-page="Create"`.
  - **Smart Index Zone** con `bg-light p-3 mb-4 rounded border-left border-info`. Input de busqueda
    `type="search"` con `placeholder="Buscar por nombre, usuario o C.I."`. Filtro de estado
    `<select>` con `asp-items="EnumHelper.GetStatusSelectList<GeneralStatus>()"` que internamente
    excluye `Eliminado` (filtrado por nombre o por `intValue == 99`). Boton "Limpiar filtros"
    condicional (`hasActiveFilters`). Indicador `X cuenta(s)` con `aria-live="polite"`.
  - Tabla `.table.table-hover.v-middle` con thead:
    `Identidad / Usuario`, `Seguridad / Rol`, `Estatus`, `Perfil Laboral`, `Registro`, `Acciones`.
    Botones por fila: `[Editar] [Detalles] [Eliminar]` en `btn-group` (iconos
    `fas fa-edit`/`fas fa-search-plus`/`fas fa-trash`).
  - **Filtrado server-side**: en `OnGetAsync` se aplican filtros contra Users/People antes de
    paginar. Para `Users`: `userQuery.Where(u => (u.FirstName/LastName/UserName/.IdentityCard
    Contains term))`. Para `Personas`: `personQuery.Where(p => p.Email contains term || p.Id ==
    term)`. Filtro de estado: si `StatusFilter.HasValue` exacto; si no, **`userQuery.Where(u =>
    u.Status != GeneralStatus.Eliminado)` y lo mismo para personas**.
  - **Paginacion**: `PaginatedList<User>.CreateAsync(userQuery.OrderBy(u => u.LastName).ThenBy(u =>
    u.FirstName), pageIndex ?? 1, 20)`. Misma logica para personas, ordenadas por `Id desc`. Tamano
    de pagina = 20.
  - Estado vacio: colspan 6 con `fas fa-user-slash fa-3x`, mensaje "No se encontraron cuentas bajo
    los criterios definidos." Estilo `text-center py-5 text-muted italic`.
  - Paginacion UI: nav bootstrap con `Anterior / i / Siguiente` y preserva filtros via
    `asp-route-SearchTerm` y `asp-route-StatusFilter`.
- **Tab Personas (Directorio de Personal):**
  - Cabecera `Directorio Base de Identidades` con boton `Nueva Identidad` -> `/Persons/Create`.
  - Aviso informativo (solo si hay personas): "Las personas listadas en este directorio pueden ser
    promovidas a Usuarios del Sistema...". Despues `<partial name="_PersonsTable"
    model="Model.PersonsList" />`.
  - Si vacio: dashed border con icono `fas fa-id-badge fa-4x`, "No se han registrado identidades en
    la base de datos base." y enlace a `/Persons/Create`.
  - `_PersonsTable.cshtml` (parcial consumido): tabla con thead `Nombre Completo, Tipo, Contacto,
    Estatus, Acciones` (5 columnas). Badge de tipo `Interno` (azul) si `item is Intern` o
     `Externo` (amarillo) si `item is Extern`. `Models/Intern.cs`, `Models/Extern.cs` y el mapeo de
     `ApplicationDbContext` confirman herencia TPT. Acciones: Editar / Detalles / Eliminar apuntando a
    `/Persons/{Edit,Details,Delete}`.
- Sin SearchTerm ni filtro de texto en el tab Personas: solo el filtro de estado es compartido via
  query string. Confirmado en `Index.cshtml.cs`: los filtros son los mismos para ambos tabs porque
  `PageIndex`, `SearchTerm` y `StatusFilter` son compartidos.

### 3.3 Usuarios - Create (`Pages/Users/Create.cshtml`, `Create.cshtml.cs`)

- Ruta: `/Users/Create` (mismo `[Authorize(Roles = AdminRoles)]`).
- Vista: card `.card.shadow-sm.border-0` con dos columnas (`col-md-6` cada una, divisor vertical
  `border-right`) en un contenedor `col-lg-10 col-xl-9`.
- Columna 1 "Identidad del Usuario": `FirstName*`, `LastName*`, `SecondLastName` (opcional),
  `IdentityCard*` (C.I., `placeholder="1234567"`), `Email*` institucional, `PhoneNumber*`, foto de
  perfil (`<input type="file" accept="image/*">` con `<label class="custom-file-label">`).
- Columna 2 "Seguridad y Privilegios": `Role*` (select con `ViewBag.UserRole` cargado via
  `EnumHelper.ToSelectList<UserRole>()`, ocultando `SuperAdmin` si el solicitante no es SuperAdmin),
  `UserName*` (login) en tarjeta `border-left border-warning`, `Password*` con campos "ver/ocultar" y
  "generar automatica", `Position`, `Department`.
- Botones: `Crear Cuenta` (`btn btn-info`), `Cancelar` (`btn btn-outline-secondary`).
- Scripts: `~/assets/extra-libs/jqbootstrapvalidation/validation.js` + `setupBlocking` que aplica un
  regex `[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]` (solo letras y espacios) sobre los inputs de nombres.
- Code-behind (`CreateModel`):
  - Validacion `DataAnnotation`: FirstName `[Required, StringLength(100)]`; LastName `[Required,
    StringLength(100)]`; SecondLastName `[StringLength(100)]`; IdentityCard `[Required,
    StringLength(10), RegularExpression("^[0-9A-Z-]*$")]`; Email `[Required, EmailAddress,
    StringLength(256)]`; PhoneNumber `[Required, Phone]`; Password `[Required, StringLength(100,
    MinimumLength = 8)]`.
  - `[BindProperty] InputModel Input`.
  - `OnPostAsync`:
    1. `Enum.IsDefined(UserRole)` y veto: si `Input.Role == SuperAdmin && !User.IsInRole(
       RoleSuperAdmin)` -> modelo invalido ("Solo un superadministrador puede asignar este rol.").
    2. `SafeImageUpload.ValidateAsync(Input.ProfilePictureUpload, ct)` -> agrega ModelError si
       falla.
    3. Normaliza `IdentityCard.Trim().ToUpperInvariant()`,
       `Email.Trim().ToLowerInvariant()`, `UserName.Trim().ToLowerInvariant()`.
    4. Verifica unicidad contra `_context.Users.IgnoreQueryFilters().AnyAsync(...)` para CI, Email y
       UserName con `Trim()` y lower/upper ya normalizados (no usa `IgnoreQueryFilters` con
       `AsTracking`).
    5. Construye `User{ Status = Activo, EmailConfirmed = true, CreatedDate = UtcNow,
       CreatedById = currentUser.Id }` y aplica `string.Clean()`: `Trim()` + colapso de espacios
       internos, preservando mayusculas/minusculas.
    6. Si hay foto: `SafeImageUpload.SaveAsync(IFormFile, uploads/users/, ct)` -> nombre
       `{guid:N}{extension}` (`_environment.WebRootPath/uploads/users/`); recupera path absoluto
       para eventual rollback.
    7. **Transaccion EF**: `BeginTransactionAsync` ->
       `_userManager.CreateAsync(user, Input.Password)` ->
       `_userManager.SynchronizeManagedRoleAsync(user, Input.Role)` (helper que reemplaza roles
       Identity manteniendo solo el rol manejado `Supervisor/Administrator/SuperAdmin`) ->
       `CommitAsync`. Si falla, `Rollback` y `SafeImageUpload.DeleteIfExists(uploadedFilePath)`.
    8. Exito: `TempData.Success("Cuenta de usuario para '{FullName}' creada exitosamente.")` y
       `RedirectToPage("./Index")`.

### 3.4 Usuarios - Edit (`Pages/Users/Edit.cshtml`, `Edit.cshtml.cs`)

- Ruta: `/Users/Edit/{id:int}` (`@page "{id:int}"`, `[Authorize(Roles = AdminRoles)]`).
- Vista: `col-lg-8` para formulario + `col-lg-4` para panel lateral oscuro "Estatus de Cuenta".
  Columna 1 (identidad y contacto) similar a Create pero con la C.I. en bloque
  `border-left border-info` y `placeholder` solo lectura; columna 2 (privilegios) lectura: la
  `UserName` es **readonly** con nota "El identificador de acceso es unico y permanente"; `NewPassword`
  opcional con nota "Deje en blanco para conservar la actual". Lateral: badge de estatus `btn-circle`
  success/danger con icono `fa-power-off`, lista `Ingreso al Sistema: Habilitado` y
  `Registro Auditado: dd/MM/yyyy`. Aviso amarillo: "Al modificar el Rol de Aplicacion, los cambios
  de permisos se aplicaran en el siguiente inicio de sesion del usuario".
- Botones: `Guardar Cambios` (`btn btn-warning text-white`), `Cancelar` apuntando a Details si
  `?returnUrl=Details`, si no a `./Index`.
- Code-behind (`EditModel`):
  - `OnGetAsync(int? id)`:
    1. 404 si id nulo.
    2. Lectura `AsNoTracking()` -> `FirstOrDefaultAsync`.
    3. `Forbid()` si el user es `SuperAdmin` y el solicitante no es SuperAdmin.
    4. `Id = user.Id`, `Input = MapToInput(user)`, `ViewData["ReturnUrl"] = HttpContext.Request.Query["returnUrl"]`.
  - `OnPostAsync(int id)`:
    1. Re-lee `ExistingProfilePictureUrl`, `ModelState.Remove("Input.NewPassword")` si vacio.
    2. Validaciones `Role` y `Status` con `Enum.IsDefined` y veto `SuperAdmin`.
    3. `SafeImageUpload.ValidateAsync(...)`.
    4. Normaliza `Email` a lower-invariant y `IdentityCard` a upper-invariant.
    5. Verifica conflictos en CI/email **excluyendo self**. CI tambien excluye usuarios con
       `Status == Eliminado`.
    6. `AsTracking()` -> `IgnoreQueryFilters()` -> carga `userToUpdate`.
    7. Veto si el solicitante edita su propia cuenta con `Role != original` o
       `Status != Activo`: "No puede cambiar su propio rol ni desactivar su propia cuenta."
    8. Veto SuperAdmin unico: "Debe existir al menos un superadministrador activo." antes de
       modificar o desactivar al unico SuperAdmin activo.
    9. Transaccion EF: `BeginTransactionAsync` -> `ApplyInput` (actualiza campos; `ApplyInput`
       re-asigna `user.NormalizedEmail` mediante `_userManager.NormalizeEmail(...)`),
       `SynchronizeManagedRoleAsync`, **si NewPassword** -> `GeneratePasswordResetTokenAsync` +
       `ResetPasswordAsync`, **si `Input.Status != Activo`** -> `SetLockoutEndDateAsync(MaxValue)`,
       **si reactivacion** (`Status == Activo && previousStatus != Activo`) ->
       `SetLockoutEndDateAsync(null)` + `ResetAccessFailedCountAsync`.
    10. **Siempre** `UpdateSecurityStampAsync` (invalida cookies existentes).
    11. `userToUpdate.ModifiedById = currentUser?.Id`, `LastModifiedDate = UtcNow`.
    12. `SaveChangesAsync` + `CommitAsync`. Si hay nueva foto, despues del commit intenta
        `SafeImageUpload.DeleteStoredFile(uploadsFolder, oldProfilePictureUrl)`.
    13. Si el user editado es el solicitante, llama `await _signInManager.RefreshSignInAsync(...)`.
    14. `TempData.Success("Datos de la cuenta '{FullName}' actualizados correctamente.")` y redirect a
        `Details` (`returnUrl == "Details"`) o `./Index`.
- Observado: en `Edit.cshtml.cs` linea `var newProfilePictureUrl = ...;` -> si la imagen nueva fue
  subida con exito y luego falla `ResetPasswordAsync`, hace rollback + borra la nueva foto. Si falla
  en `ApplyInput`, el rollback no reescribe el path borrado. Patron defensivo intencional.

### 3.5 Usuarios - Details (`Pages/Users/Details.cshtml`, `Details.cshtml.cs`)

- Ruta: `/Users/Details/{id:int}`. `[Authorize(Roles = AdminRoles)]`.
- Vista: `col-lg-8` (ficha + auditoria) y `col-lg-4` (seguridad y acciones). Card superior con avatar
  redondo 150px (`user-detail-avatar`, clase `.user-detail-initials` con texto grande si no hay foto),
  badges `badge-info` para rol y `badge-success/badge-danger` para estado. Bloque "Identidad":
  Nombres, Apellido paterno, Apellido materno, C.I., Usuario / login (@UserName), Fecha de alta.
  Bloque "Contacto y perfil laboral": Correo institucional, Telefono, Cargo, Departamento.
  Bloque "Auditoria": Creado por (FullName o "Sistema") + `CreatedDate.ToString("dd/MM/yyyy HH:mm")`,
  Ultima modificacion (ModifiedBy?.FullName o "Sin modificaciones") + `LastModifiedDate?.ToString(
  "dd/MM/yyyy HH:mm")` o "Sin fecha registrada". Lateral:
  - Sidebar "Estatus de Cuenta" con badge-pill success/danger.
  - Card "Acciones": `Modificar Perfil` -> `asp-page="./Edit" asp-route-id=... asp-route-returnUrl=
    "Details"` (boton `btn-warning text-white`), `Volver al Listado` -> `./Index`,
    `Revocar Acceso` -> `./Delete/{id}`.
- Helper inline en `.cshtml`: `string ValueOrFallback(string? value, string fallback = "Sin dato")`
  reemplaza vacios por la cadena indicada.
- Code-behind (`DetailsModel`):
  - 404 si id nulo, redirige a `/Error` con parametros `module/entityId/message/returnUrl/listUrl`.
  - Carga `User` con `IgnoreQueryFilters()` (incluye eliminados) + `Include(CreatedBy, ModifiedBy)`
    y `AsNoTracking()`.
  - `ProfilePictureVersion = (LastModifiedDate ?? CreatedDate).Ticks.ToString()` para cache-busting.

### 3.6 Usuarios - Delete (`Pages/Users/Delete.cshtml`, `Delete.cshtml.cs`)

- Ruta: `/Users/Delete/{id:int}`. `[Authorize(Roles = AdminRoles)]`.
- Vista: confirmacion centrada (`col-md-6 col-lg-5`), icono `fas fa-user-times display-3 text-danger`,
  titulo "¿Revocar Acceso al Sistema?", parrafo "Esta a punto de dar de baja la cuenta de usuario de
  {FullName} (@@{UserName}). El usuario ya no podra iniciar sesion, pero su historial de acciones
  se preservara por integridad institucional.". Mini-card de "Datos clave": Rol actual, Area/Dpto,
  CI. Botones: `Ver Expediente` (-> Details) y `Sí, Confirmar Baja` (`btn-danger btn-rounded`).
- Code-behind (`DeleteModel`):
  - `OnGetAsync`:
    1. 404 si id nulo.
    2. Lee user incluyendo `CreatedBy, ModifiedBy` (con tracking — no `AsNoTracking()`).
    3. `Forbid()` si user es SuperAdmin y solicitante no.
    4. Asigna `AppUser` (rename para evitar colision con `PageModel.User`/`ClaimsPrincipal`).
  - `OnPostAsync`:
    1. `var user = await _context.Users.FindAsync(id);` (esto es tracking).
    2. Si `currentUser.Id == user.Id`, `TempData.Error("No puede dar de baja su propia cuenta.")`
       -> redirect Index. **Aqui bloquea el self-delete.**
    3. Si `user.Role == SuperAdmin && !User.IsInRole(RoleSuperAdmin)` -> Forbid.
    4. Si `user.Role == SuperAdmin`, verifica `hasAnotherActiveSuperAdmin`. Si no,
       `TempData.Error("Debe existir al menos un superadministrador activo.")` -> Index.
    5. `if (user.Status == GeneralStatus.Eliminado)` ->
       `TempData.Warning("El usuario '{FullName}' ya se encuentra dado de baja.")` -> Index.
    6. **Soft delete**: `user.Status = GeneralStatus.Eliminado; user.LastModifiedDate = UtcNow;
       user.ModifiedById = currentUser?.Id`.
    7. **Transaccion**: `BeginTransactionAsync` -> `SetLockoutEndDateAsync(MaxValue)` (revoca
       acceso) -> `UpdateSecurityStampAsync` (invalida sesiones/cookies) -> `SaveChangesAsync` +
       `CommitAsync`.
    8. Mensaje OK: `TempData.Success("El acceso para '{FullName}' ha sido revocado correctamente.")`
       y redirect a `./Index`.
    9. Errores de Identity o excepcion capturada -> `TempData.Error(...)` y redirect a
       `./Delete/{id}` para reintento.

### 3.7 Persons - alcance y reutilizacion

- `_PersonsTable.cshtml` se reusa desde `Pages/Users/Index.cshtml` (tab "personas") y desde
  `Pages/Persons/Index.cshtml` (titulo "Directorio de Personal y Colaboradores" y boton
  "Registrar Persona" -> `/Persons/Create`). Sus acciones enlazan al CRUD real de `Pages/Persons/`.
- La reutilizacion es real: ASP usa herencia **TPT** (`Person -> People`, `Intern -> Interns`,
  `Extern -> Externs`). `Person.FullName` es virtual y devuelve el fallback "Ficha de Persona";
  `Intern` y `Extern` lo sobrescriben con su propio `Name`. La base conserva `Status`, auditoria,
  `ImportBatchId` y `[Timestamp] RowVersion`; cada subtipo agrega `InternStatus` o `ExternStatus`.

### 3.8 Flujos Persons enlazados desde el tab (frontera de reuso)

Estos flujos pertenecen al modulo Persons y **no deben duplicarse** al reconstruir Users Index. MIG-001
solo debe volver a presentar su listado/acciones dentro del tab y reutilizar la API/panel existente.

| FLOW NAME | ENTRY POINT / ROLES | INPUT, NORMALIZATION Y VALIDATIONS | READS / WRITES / SIDE EFFECTS | MENSAJES Y RESULTADO | VISIBLE UI STATE | SOURCE FILES |
|---|---|---|---|---|---|---|
| PERSONS CREATE | `GET/POST /Persons/Create`; `Administrator,SuperAdmin` | `IsInternal`, `Name`, `Email?`, `PhoneNumber?`, `IsEntity`, `Address?`; `Clean()` recorta y colapsa espacios sin cambiar mayusculas; email a minusculas; Address obligatorio para externos | Crea `Intern` o `Extern`, `Status=Activo`, estado de subtipo activo y auditor de creacion; `SaveChangesAsync`, sin transaccion | Exito: `Registro de '{FullName}' completado exitosamente.` -> `./Index`; error: `No se pudo registrar la persona. Intente nuevamente.` -> `Page()` | Card de alta con tabs Interno/Externo, dos columnas, campos condicionales y botones `Confirmar Registro`, `Cancelar` | `Pages/Persons/Create.cshtml`, `.cshtml.cs`, `Helpers/StringExtensions.cs` |
| PERSONS EDIT | `GET/POST /Persons/Edit/{id}`; mismos roles | Campos comunes, `Status`, y `InternStatus?`/`ExternStatus?`; DataAnnotations; no cambia el subtipo | Lee `People`; actualiza campos comunes, estado base y estado del subtipo; `LastModifiedDate=UtcNow`; no escribe `ModifiedById` | Exito: `Datos de '{FullName}' actualizados correctamente.` -> `./Index`; error: `No se pudo actualizar la persona. Intente nuevamente.` -> `Page()` | Formulario equivalente a Create con estado base y estado especifico del subtipo | `Pages/Persons/Edit.cshtml`, `.cshtml.cs`, `Models/Intern.cs`, `Models/Extern.cs` |
| PERSONS DETAILS | `GET /Persons/Details/{id}`; mismos roles | Filtros de historial de mantenimiento y paginacion; id requerido | Lee Person con auditoria, historial de mantenimientos y salidas; no escribe | `NotFound()` si no existe; en otro caso `Page()`; sin TempData propio | Ficha de identidad/contacto/auditoria mas historiales de mantenimientos y salidas | `Pages/Persons/Details.cshtml`, `.cshtml.cs`, `Helpers/PaginatedList.cs` |
| PERSONS DELETE | `GET/POST /Persons/Delete/{id}`; mismos roles | Id; reintento sobre eliminado es idempotente | `FindAsync`; fija solo `Person.Status=Eliminado` y `LastModifiedDate=UtcNow`; no toca `InternStatus`/`ExternStatus`, `ModifiedById` ni security stamp | Exito: `La identidad de '{FullName}' ha sido dada de baja correctamente.`; warning si ya estaba de baja; error `No se pudo procesar la baja. Intente nuevamente.`; redirects a Index o Delete | Card centrada `¿Revocar Acceso?`, resumen y `Sí, Confirmar Baja`; submit directo, sin SweetAlert | `Pages/Persons/Delete.cshtml`, `.cshtml.cs` |

---

## 4. Clasificacion de exposicion

Capacidades que **no** se exponen hoy en el target deben etiquetarse. Listamos todas las capacidades
descubiertas y su clasificacion:

| Capacidad | ASP | Target | EXPOSURE CLASSIFICATION (ASP / TARGET) |
|---|---|---|---|
| Login (form split, username O email) | Pagina anonima dedicada | Login React dentro de `App.tsx` (inline) | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Cambio de sede activa | n/a (no existe) | **`site-picker-stage` + boton en topbar** | DORMANT_OR_NOT_EXPOSED / VISIBLE_AND_REACHABLE |
| Indice de cuentas (Cuentas de Acceso) | `/Users/Index` (tab1) | `UsersPanel` montado en `/Users/Index` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Indice de personas (Directorio de Personal) | `/Users/Index` (tab2) | `PeoplePanel` montado en `/Persons/Index` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Crear cuenta | `/Users/Create` | `UsersPanel.submit` -> `api.createUser` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Editar cuenta (campos, password, rol, status) | `/Users/Edit/{id}` con sidebar | `UsersPanel.edit`/`submit` -> `api.updateUser` (sin sidebar) | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Detalles de cuenta (ficha + auditoria) | `/Users/Details/{id}` | `/Users/Details` es Mi Perfil; `GET /users/:id` solo aporta record administrativo | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Deshabilitar cuenta | `/Users/Delete/{id}` con lockout + security stamp | `UsersPanel.disable` -> `api.deleteUser` -> `status='disabled'` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Confirmacion pre-submit en Edit/Delete | No existe; POST directo | `window.confirm` en bajas; Edit directo | DORMANT_OR_NOT_EXPOSED / VISIBLE_AND_REACHABLE |
| Subir foto de perfil | `SafeImageUpload.SaveAsync` en Create/Edit | Sin campo ni endpoint | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| 2FA / Lockout flow | Redirects a paginas inexistentes | Rate-limit; sin 2FA | DORMANT_OR_NOT_EXPOSED / DORMANT_OR_NOT_EXPOSED |
| Login username **o email** | Busqueda dual | Solo email | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Sidebar de navegacion | `_Sidebar.cshtml` | `NAVIGATION` en `App.tsx` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Sidebar item "Personas" separado | No existe | `/Persons/Index` | DORMANT_OR_NOT_EXPOSED / VISIBLE_AND_REACHABLE |
| Header "Mi Perfil" + "Cerrar Sesion" | ViewComponent | `profile-menu` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Notificaciones en topbar | Campana condicional | Campana siempre en workspace | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Pre-loader UI | `preloader` | `loading-stage` | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Mensajes CRUD | Toast SweetAlert2 | alertas inline | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Busqueda/filtro Users | Nombre/username/CI + estado | Repository soporta full_name/email + estado; no hay input de busqueda | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Paginacion 20 | UI + backend | Backend page size 20; UI fija pagina 1 | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Cambio de password administrado | Campo opcional en Edit | Campo opcional en UsersPanel | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Cambio de password propio | Solo al entrar al Edit como admin | Sin camino desde Profile | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Username readonly | Visible en Details/Edit | No existe campo | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| Buscar por CI | Visible y alcanzable | Sin campo/consulta | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| Generar/ver password | Botones JS | No existe | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| Preview/subida de foto | Create/Edit/header/listado | No existe | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| `Mi Perfil` / ficha administrativa | Ficha completa compartida | Profile autocentrado limitado | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Self-protection | Error visible al intentar baja/cambio prohibido | Excepcion API y feedback UI | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Proteccion ultimo SuperAdmin | Error visible, conteo de activos | Repository bloquea todo SuperAdmin | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Asignar SuperAdmin | Select y veto segun actor | Solo CLI, no UserController | VISIBLE_AND_REACHABLE / DORMANT_OR_NOT_EXPOSED |
| Auditoria User | Fechas/actores visibles | timestamps en record; actores no existen y UI no muestra | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |
| Alta Person Intern/Extern | Form y acciones del tab | PeoplePanel | VISIBLE_AND_REACHABLE / VISIBLE_AND_REACHABLE |
| Registro en Index | Fecha + iniciales del creador | timestamps backend, sin columna UI | VISIBLE_AND_REACHABLE / BACKEND_INTERNAL |

### 4.1 Capacidades BACKEND_INTERNAL

- `SynchronizeManagedRoleAsync` en `Helpers/IdentityRoleExtensions.cs` (mapeo `UserRole` -> rol
  Identity `Supervisor/Administrator/SuperAdmin` y deduplicacion al reasignar).
- Rate-limit `auth.rate-limit.ts` con tabla `lu_auth_rate_limit` (`scope in
  ('email_ip','ip')`, intento + reset_at).
- CSRF: `auth.controller.generateCsrf` -> cookie `csrf`.
- Cookie `session`: `httpOnly`, `secure=true`, `SameSite=Lax`, `path=/`; persistencia opcional via
  `maxAge = absoluteTtlSeconds` cuando `rememberMe=true`.
- `security_version` se incrementa en `users.repository.update` para invalidar sesiones al cambiar
  una cuenta administrada. `users.repository.updateProfile` cambia email/nombre propio **sin**
  incrementarlo; es una contradiccion de seguridad registrada como LC-13.
- `bcryptjs` con costo configurable (`config.bcryptCost`) y `dummyHash` para mitigacion de timing.
- `auth.bootstrap.ts`, `auth-managed-role-cli.ts`: herramientas CLI opcionales para provision
  (`@lu` metadata de roles).

### 4.2 Capacidades DORMANT_OR_NOT_EXPOSED

- `Login.cshtml.cs` conserva redirects a `./LoginWith2fa` y `./Lockout`, pero esas paginas **no
  existen** en `reference/asp-final`; son referencias residuales, no pantallas observables.
- `Pages/Persons/Details` carga historiales de mantenimientos y salidas. Es visible desde las
  acciones del tab, pero su port funcional pertenece al modulo Persons; MIG-001 debe enlazarlo o
  reutilizarlo, no duplicarlo dentro de Users.
- `Helpers/PaginatedList.cs` implementa `CountAsync` + `Skip/Take`, page size default 20. Es helper
  interno; React no lo porta literalmente.
- `Helpers/EnumHelper.GetStatusSelectList<GeneralStatus>` filtra "Eliminado/Deleted" por convencion
  de nombre y por valor 99, pero `GeneralStatus.Eliminado = 2`, no 99. La defensa "intValue == 99"
  parece orientar a `EquipmentStatus.Deleted=99` y **no afecta a Users**.

### 4.3 Capacidades UNKNOWN / no resueltas en F0

No quedan dependencias de codigo Users/Persons sin leer de las detectadas en esta auditoria. El
`UNKNOWN` operativo restante es la via aprobada para aplicar los SQL existentes fuera del registro
pinneado (`control-plane/0004`, `control-plane/0005`, `tenant/0012`): el `migration-cli` actual solo
acepta 0001-0003. La decision de registro/manifiesto corresponde a F1/F2.

---

## 5. Seccion visual obligatoria

### 5.1 INDEX-USERS-TAB

- **ROUTE**: `/Users/Index` (Razor Page; ruta relativa a `Pages/Users/Index.cshtml`).
- **LAYOUT**: NiceAdmin/Bootstrap 4 con fondo blanco, card contenedor en
  `col-12 .card.shadow-sm.border-0`; nav-tabs arriba; `tab-content` con `border-top`; bloque
  `bg-light p-3 mb-4 rounded border-left border-info` como zona de filtros (Smart Index Zone).
- **GRID/COLUMNS**: tabla con 6 columnas; filas en `table-hover v-middle`; cada celda usa clases
  verticales (`v-middle td { vertical-align: middle !important; }`).
- **TEXTS**: cabecera de pagina `Control de Usuarios`; subtitulo `Administración de credenciales,
  roles y estatus de seguridad del sistema.`; placeholder del buscador `Buscar por nombre, usuario
  o C.I.`; option label por defecto `Estado`.
- **CONTROLS**: input `type="search"`, select de estado con
  `asp-items="EnumHelper.GetStatusSelectList<GeneralStatus>()"`, link "Limpiar filtros", boton
  `Vincular Usuario` (`btn bg-white border btn-rounded shadow-sm text-dark px-4`), enlaces
  `[Editar][Detalles][Eliminar]` (`btn-outline-warning / -info / -danger btn-rounded shadow-none`).
- **TABLE STRUCTURE**: thead `bg-light` con clase `.small.uppercase.font-weight-bold`:
  | Identidad / Usuario | Seguridad / Rol | Estatus | Perfil Laboral | Registro | Acciones |
  Filas: avatar circular (img si foto, sino span `btn btn-circle btn-info font-weight-bold text-white
  shadow-sm` con iniciales) + `FullName` (`h6.m-b-0.font-weight-bold.text-dark`) + `CI: <value>`;
  badge de rol (`<i class="fas fa-shield-alt mr-1 text-info"></i> @item.Role`) + `@@@UserName` italico
  gris; `badge-pill px-3 py-1 font-weight-bold` success/danger para `Status`; `Position/Department`;
  `CreatedDate` (`dd/MM/yyyy`) + `CreatedBy?.Initials` o "Sist."; acciones (`btn-group`, sin iconos
  duplicados en los bordes, gap via `.mx-1`).
- **BUTTON ORDER**: arriba-derecha `Vincular Usuario` (nuevo); por fila Editar, Detalles, Eliminar.
  Paginacion debajo de la tabla centrada (`justify-content-center`).
- **ICONS**: `fa-users-cog` (tab Cuentas), `fa-id-card` (tab Directorio), `fa-plus-circle` (crear),
  `fa-search` (buscador), `fa-shield-alt` (rol), `fa-user-check` (audit), `fa-edit /
  fa-search-plus / fa-trash` (acciones), `far fa-calendar-alt` (fecha), `fa-user-slash` (vacio).
- **BADGES**: rol con icono `fa-shield-alt`; estado Activo/Inactivo/Eliminado con `badge-pill`
  success/danger/danger; contador `X cuenta(s)` en `<span class="small text-muted ml-auto mb-2"
  aria-live="polite">`.
- **COLORS/CLASSES**: tipografia body inherente NiceAdmin; `card-title.text-dark.font-weight-bold`;
  subtitulo `text-muted.ml-2`; `border-left border-info` (zona filtros); btn-rounded, shadow-sm,
  shadow-none. CSS custom final: `.customtab .nav-link.active { color: #2962ff; border-bottom: 3px
  solid #2962ff !important; }` y `.uppercase { text-transform: uppercase; letter-spacing: 1px; }`.
- **SPECIAL CSS**: `customtab`, `v-middle`, `drop-shadow`, `uppercase`, `italic`, `opacity-2`,
  `border-3`. (Estos nombres tambien aparecen en otros archivos; el bloque esta en la seccion
  `<style>` de `Index.cshtml`.)
- **RESPONSIVE**: implementado a nivel del contenedor col-12 + clases de Bootstrap; la cabecera
  interna `d-md-flex` apila a vertical en moviles. Sin clases xs/breakpoints custom.
- **EMPTY/ERROR/SUCCESS**:
  - Vacio: colspan 6 con `fas fa-user-slash fa-3x` opacity-2 + "No se encontraron cuentas bajo los
    criterios definidos."
  - Error: el layout global muestra `TempData.ErrorMessage` como toast SweetAlert2 (top-end).
  - Exito: `TempData.Success` (viene de `Create/Edit/Delete` -> toast).
- **NAVIGATION**: tab 1 activo por defecto. El switch a tab #personas conserva `PageIndex,
  SearchTerm, StatusFilter` (todos compartidos); ambos tabs usan `asp-page="./Index"` con
  `asp-route-pageIndex` para paginacion.
- **ASSETS**: depende de `~/dist/css/style.min.css`, `sweetalert2`, `c3`, `css/smart-index.css`; no
  usa `wwwroot/uploads/users/` directamente en este template.

### 5.2 INDEX-PERSONS-TAB

- **ROUTE**: `/Users/Index` con hash `#personas` (mismo archivo Razor, segundo `tab-pane`).
- **LAYOUT**: misma card; cabecera `Directorio Base de Identidades`; subtitulo "Personal registrado
  en la institucion para servicios tecnicos y administrativos."; partial `<partial name="
  _PersonsTable" model="Model.PersonsList" />`.
- **GRID/COLUMNS**: tabla de 5 columnas (`Nombre Completo`, `Tipo`, `Contacto`, `Estado`,
  `Acciones`).
- **TEXTS**: boton `Nueva Identidad` (`fa-plus-circle text-info`) -> `/Persons/Create`; alert
  info opcional "Las personas listadas en este directorio pueden ser promovidas a Usuarios del
  Sistema...".
- **CONTROLS**: filtro estado compartido con tab Cuentas; **no hay busqueda textual** en este tab
  (solo filtro estado), pero en code-behind el SearchTerm tambien aplica a la lista de personas.
- **TABLE STRUCTURE**: `table.table-hover.v-middle`; cada fila: avatar circular `btn btn-circle
  btn-info` con la primera letra de `FullName`, ID mostrado como `ID: @item.Id`; tipo como
  `badge-pill` azul (Intern) o amarillo (Extern); contacto: telefono y email en dos lineas; estado
  como `badge-pill` success/danger; acciones `[Editar][Detalles][Eliminar]` (`btn-outline-warning /
  -info / -danger btn-rounded`).
- **BUTTON ORDER**: arriba-derecha `Nueva Identidad`; por fila Editar, Detalles, Eliminar; paginacion
  debajo.
- **ICONS**: `fa-id-card` (tab), `fa-plus-circle` (crear), `fa-id-badge` (vacio), `fa-edit /
  fa-search-plus / fa-trash` (acciones).
- **BADGES**: tipo `Interno` azul / `Externo` amarillo (texto blanco, `font-weight-bold`); estado
  Activo/Inactivo con `badge-pill success/danger`.
- **COLORS/CLASSES**: mismas globales del tab Cuentas.
- **SPECIAL CSS**: misma seccion `<style>` del archivo (customtab aplica a ambos tabs).
- **RESPONSIVE**: igual al tab Cuentas.
- **EMPTY/ERROR/SUCCESS**:
  - Vacio: dashed border con `fa-id-badge fa-4x text-muted opacity-2` y "No se han registrado
    identidades..." con link `Registrar la primera persona` -> `/Persons/Create`.
  - Sin paginacion si `PersonsList.TotalPages <= 1`.
- **NAVIGATION**: mismo `asp-page="./Index"` con filtros.
- **ASSETS**: mismos del tab Cuentas.

### 5.3 CREATE

- **ROUTE**: `/Users/Create` (`@page`).
- **LAYOUT**: `col-lg-10 col-xl-9 justify-content-center`; card unica con padding `p-4`; cabecera
  `Alta de Cuenta de Usuario` + subtitulo; icono decorativo `fa-user-shield fa-2x text-info
  opacity-5` a la derecha.
- **GRID/COLUMNS**: dos columnas balanceadas `col-md-6` con divisor vertical `border-right` en la
  columna 1. Cada bloque (Identidad / Seguridad) tiene cabecera `h5.font-weight-bold.text-info` con
  icono.
- **TEXTS**: labels en `<span class="uppercase text-muted">` con asterisco rojo en requeridos;
  placeholders ejemplos ("Ej: Juan Antonio"). Help text: `Mínimo 8 caracteres, debe incluir letras y
  números.`
- **CONTROLS**: text inputs, email, tel, file (custom-file), select Role con icono `fa-user-tag`,
  password input con boton "ver/ocultar" (`fa-eye`) + boton "generar" (`fa-magic`).
- **TABLE STRUCTURE**: no hay tabla; es formulario lineal con dos columnas.
- **BUTTON ORDER**: pie de pagina alineado a la derecha: `Crear Cuenta` (`btn btn-info
  btn-rounded px-5 shadow-sm font-weight-bold py-2`) -> `Cancelar` (`btn btn-outline-secondary
  btn-rounded ml-2 py-2 px-4 font-weight-bold`).
- **ICONS**: `fa-user-shield` (cabecera), `fa-address-card` (Identidad), `fa-lock` (Seguridad),
  `fa-user-tag` (Username), `fa-eye / fa-eye-slash / fa-magic` (Password UX).
- **BADGES**: ninguno. Asterisco rojo `text-danger` para campos requeridos.
- **COLORS/CLASSES**: `border-info` en `Role`, `border-warning` en `UserName`, `bg-light p-3
  rounded` para zonas destacadas, alert danger con `validation-summary`.
- **SPECIAL CSS**: bloque final `<style>` con `.help-block ul { list-style: none; padding-left: 0; }
  .italic { font-style: italic; } .uppercase { text-transform: uppercase; letter-spacing: 1px; }
  .drop-shadow { filter: drop-shadow(0 2px 4px rgba(0,0,0,0.05)); }`
- **RESPONSIVE**: col-md-6 -> col-12 apilado en moviles por Bootstrap.
- **EMPTY/ERROR/SUCCESS**: errores via `<span asp-validation-for="..." class="text-danger small
  font-weight-bold">` por control; errores globales en `<div asp-validation-summary="ModelOnly"
  class="alert alert-danger shadow-sm border-0 small" role="alert">`. Exito -> `TempData.Success`
  toast.
- **NAVIGATION**: submit -> POST -> en exito `RedirectToPage("./Index")`.
- **ASSETS**: `~/assets/extra-libs/jqbootstrapvalidation/validation.js` (Sprint 3+, ya cargado por
  layout global; redeclarado aqui para activar `$("input,select,textarea").jqBootstrapValidation()`);
  partial `_ValidationScriptsPartial` (jquery-validation unobtrusive).

### 5.4 EDIT

- **ROUTE**: `/Users/Edit/{id:int}` (`@page "{id:int}"`). Cancelar devuelve a `Details` cuando
  `?returnUrl=Details`, si no a `Index`.
- **LAYOUT**: dos columnas `col-lg-8` formulario + `col-lg-4` panel lateral `bg-dark text-white`. La
  columna 1 = Identidad y Contacto, columna 2 = Privilegios de Acceso. Bloque "Gestion de
  Credenciales" en `bg-light p-3 rounded`. Boton `Modificar Perfil` apunta a `Details` y lleva
  `asp-route-returnUrl="Details"`.
- **GRID/COLUMNS**: `col-md-6` x 2 dentro del formulario (Izquierda Identidad, Derecha
  Privilegios), `border-right` entre ambas. Tarjeta oscura lateral `bg-dark text-white` con
  `border-secondary` separadores.
- **TEXTS**: cabecera `Gestion de Perfil de Usuario`; subtitulo `Actualizacion de privilegios,
  credenciales y datos de contacto de la cuenta institucional.`; Usuario `readonly` con nota "El
  identificador de acceso es unico y permanente."; NewPassword con nota "Deje en blanco para
  conservar la actual."; aviso "Al modificar el Rol de Aplicacion, los cambios de permisos se
  aplicaran en el siguiente inicio de sesion del usuario.".
- **CONTROLS**: mismos campos que Create + lectura de `ExistingProfilePictureUrl` con `<img
  class="img-thumbnail" style="max-height: 100px;">` o fallback dashed border (`Sin foto de
  perfil`).
- **TABLE STRUCTURE**: no aplica; formulario lineal con dos columnas + sidebar.
- **BUTTON ORDER**: pie de pagina: `Guardar Cambios` (`btn btn-warning btn-rounded px-5 shadow-sm
  text-white font-weight-bold py-2`) -> `Cancelar`.
- **ICONS**: `fa-user-edit` (cabecera), `fa-id-badge` (Identidad), `fa-lock` (Privilegios),
  `fa-key` (Credenciales), `fa-shield-alt` (sidebar Estatus), `fa-cogs` (sidebar Acciones),
  `fa-power-off` (sidebar circulo activo/inactivo), `fa-info-circle` (aviso), `fa-edit /
  fa-list / fa-user-minus` (sidebar acciones).
- **BADGES**: en sidebar Estatus `btn-circle btn-lg btn-success` o `btn-danger` con
  `fa-power-off`.
- **COLORS/CLASSES**: `border-warning` para zonas credenciales; `border-info` para C.I.; `bg-dark
  text-white` sidebar; `alert-light border-left border-info border-3` para hint.
- **SPECIAL CSS**: bloque `<style>` final con `.help-block ul ... .italic .uppercase .border-3 {
  border-left-width: 4px !important; }`.
- **RESPONSIVE**: lg-8/lg-4 colapsa en moviles por Bootstrap.
- **EMPTY/ERROR/SUCCESS**: mismas reglas de Create para errores. Aviso amarillo permanente:
  "Deje en blanco para conservar la actual." debajo del input NewPassword.
- **NAVIGATION**: submit -> POST -> redirect a `Details` (`?returnUrl=Details`) o `./Index`. Si la
  edicion es del propio usuario, `await _signInManager.RefreshSignInAsync(userToUpdate)`.
- **ASSETS**: mismos de Create.

### 5.5 DETAILS

- **ROUTE**: `/Users/Details/{id:int}` (`@page "{id:int}"`).
- **LAYOUT**: `col-lg-8` (ficha + auditoria) + `col-lg-4` (sidebar seguridad + sidebar acciones).
- **GRID/COLUMNS**: ficha con avatar 150px + flex-wrap; bloques Identidad (col-md-4 x3),
  Contacto/Perfil laboral (col-md-6 x2). Auditoria (col-md-6 x2). Sidebar: card oscura + card blanca.
- **TEXTS**: cabecera `Ficha de usuario` (small uppercase), `h2.font-weight-bold.text-dark` con
  FullName + Position; subtitulo Cargo; badges `badge-info` (rol) y `badge-pill` (status).
  Bloques titulo `Identidad` (`fa-id-card-alt`), `Contacto y perfil laboral`
  (`fa-address-book`), `Auditoria` (`fa-history`). Valores con fallback `Sin dato / Sin dato
  registrado / Sin modificaciones / Sin fecha registrada`.
- **CONTROLS**: ninguno editable; enlaces a Edit, Index, Delete.
- **TABLE STRUCTURE**: no aplica.
- **BUTTON ORDER** (sidebar acciones): `Modificar Perfil` (`btn-warning btn-block btn-rounded
  text-white`) -> `Volver al Listado` (`btn-outline-secondary btn-block`) -> `Revocar Acceso`
  (`btn-link text-danger small`).
- **ICONS**: `fa-id-card-alt`, `fa-address-book`, `fa-history`, `fa-shield-alt`, `fa-cogs`,
  `fa-edit`, `fa-list`, `fa-user-minus`.
- **BADGES**: `badge-pill badge-info` (rol), `badge-pill badge-success/-danger` (status), todas
  `px-3 py-1`.
- **COLORS/CLASSES**: `user-detail-avatar { width:150px; height:150px; min-width:150px; object-fit:
  cover; border: 4px solid #e0f2fe; }`; `user-detail-initials { font-size: 3.1rem; line-height: 1; }
  `; `user-detail-label { color: #a6b0bf; font-size: 0.68rem; text-transform: uppercase;
  letter-spacing: 0.08em; font-weight: 700; }`; `user-detail-value { color: #1f2d3d; font-weight:
  700; word-break: break-word; }`. Sidebar oscuro `bg-dark text-white` con `border-secondary` hr.
- **SPECIAL CSS**: bloque final `<style>` con `.user-detail-avatar`, `.user-detail-initials`,
  `.user-detail-label`, `.user-detail-value`.
- **RESPONSIVE**: flex-wrap de cabecera; columnas lg-8/lg-4 colapsan en moviles.
- **EMPTY/ERROR/SUCCESS**:
  - 404 -> RedirectToPage `/Error` (no Details). Confirmado en code-behind.
  - Edicion -> vuelve a `Details` cuando `returnUrl="Details"`, si no a `Index`.
- **NAVIGATION**: si el solicitante hace click en `Modificar Perfil`, va a
  `/Users/Edit/{id}?returnUrl=Details`; al guardar desde Edit con `returnUrl=Details`, vuelve aqui.
- **ASSETS**: avatar usa `~/uploads/users/<file>?v=<Ticks>` (cache-busting).

### 5.6 DELETE

- **ROUTE**: `/Users/Delete/{id:int}`.
- **LAYOUT**: `col-md-6 col-lg-5 justify-content-center` en una card `text-center shadow-sm
  border-0` con padding `p-5`.
- **GRID/COLUMNS**: una sola columna centrada con `d-flex justify-content-center`.
- **TEXTS**: titulo `¿Revocar Acceso al Sistema?` (`h3.card-title.text-dark.font-weight-bold`);
  parrafo explicativo con `FullName` y `@@@UserName`; mini-card de datos clave `Rol Actual`, `Area
  Dpto`, `Documento (CI)`. Sugerencia tecnica externa: "Se recomienda Inactivar la cuenta desde el
  editor si el usuario ha realizado registros criticos recientes."
- **CONTROLS**: dos botones (`Ver Expediente` -> Details con icono ausente/`fa-list`, `Sí,
  Confirmar Baja`); enlace `Volver al Control de Usuarios`.
- **TABLE STRUCTURE**: no aplica.
- **BUTTON ORDER**: `Ver Expediente` (outline secondary) seguido de `Sí, Confirmar Baja`
  (`btn-danger btn-rounded`).
- **ICONS**: `fa-user-times display-3 text-danger` (warning grande), `fa-trash-alt` (boton
  confirmar), `fa-info-circle` (sugerencia).
- **BADGES**: ninguno.
- **COLORS/CLASSES**: `border-left border-danger border-3` en mini-card datos; alert-light italic.
- **SPECIAL CSS**: bloque `<style>` con `.border-3 { border-left-width: 4px !important; } .italic
  { font-style: italic; } .uppercase { text-transform: uppercase; letter-spacing: 1px; }`.
- **RESPONSIVE**: max-width col-lg-5; en moviles ocupa toda la fila.
- **EMPTY/ERROR/SUCCESS**:
  - 404 (id nulo o usuario no encontrado) -> NotFound (no redirect).
  - 403 (SuperAdmin ajeno) -> Forbid.
  - Ya eliminado -> TempData Warning + redirect Index.
  - Self-delete -> TempData Error + redirect Index.
  - Unico SuperAdmin -> TempData Error + redirect Index.
  - Exito -> TempData Success + redirect Index.
  - Error transaccional -> TempData Error + redirect a `./Delete/{id}`.
- **NAVIGATION**: navegar a `Details` permite cancelar la baja; `Index` siempre disponible.
- **ASSETS**: ninguno adicional.

---

## 6. Modelo de datos: legacy vs target

### 6.1 Tabla detallada `User` vs `lu_user`

| LEGACY FIELD | TYPE | REQUIRED | NORMALIZATION | UNIQUE | VISIBLE | WRITABLE | CURRENT POSTGRES FIELD | CURRENT CONTRACT FIELD | GAP |
|---|---|---|---|---|---|---|---|---|---|
| `Id` (`IdentityUser<int>.Id`) | int | si | auto | si (PK) | si | no | `lu_user.id uuid` (cambio `int -> uuid`) | `ManagedUserRecord.id: string` | **CONFLICT** (tipo y nombre cambio) |
| `UserName` | string? (override) | si | `lower.Trim()` | si (Identity normalised) | si (mostrado como `@@UserName`) | readonly en Edit; modificable solo en Create | `lu_user.email` (login es por email) | login email en `LoginRequest.email`; no hay `userName` separado | **CONFLICT** (semantica consolidada: solo email) |
| `NormalizedUserName` | string | si | upper | si (Identity) | no | no | sin equivalente (login es case-insensitive por `lower(email)`) | n/a | gap |
| `Email` | string? | si | `lower.Trim()` | si (Identity normalised) | si (`Correo institucional`) | si | `lu_user.email` | `CreateManagedUserInput.email`, `UpdateManagedUserInput.email` | FULL |
| `NormalizedEmail` | string | si | upper | si (Identity) | no | no | sin equivalente explicito | n/a | PARTIAL |
| `EmailConfirmed` | bool | si | n/a | no | no | no | n/a | n/a | gap (sin 2FA / confirmacion) |
| `PasswordHash` | string (Identity) | si | ASP Identity PasswordHasher v3 / PBKDF2 por defecto | n/a | no | no | `lu_user.password_hash` (bcryptjs) | backend; cliente no lo ve | **CONFLICT**: no existe estrategia actual para migrar/verificar hashes PBKDF2 legacy |
| `SecurityStamp` | string (Identity) | si | GUID | n/a | no | si (rotacion en cada save) | `lu_user.security_version` (entero monotono, no GUID) | backend | **CONFLICT** (UUID -> bigint monotono) |
| `ConcurrencyStamp` | string (Identity) | si | GUID | n/a | no | si | n/a (no hay version optimista a nivel de fila para usuarios en target) | n/a | gap |
| `PhoneNumber` | string? | si | `Trim()` | no | si | si | `lu_user.email` solo; `phone_number` no existe en `lu_user` | no expuesto en `ManagedUserRecord`; **si existe como prestamo conceptual en `ProfileRecord`** (no: ProfileRecord expone solo email/fullName/siteRole) | **CONFLICT** (telefono desaparecido) |
| `PhoneNumberConfirmed` | bool | si | n/a | no | no | no | n/a | n/a | gap |
| `TwoFactorEnabled` | bool | si | n/a | no | no | no | n/a (sin 2FA en target) | n/a | gap (sin 2FA) |
| `LockoutEnd` | DateTimeOffset? (Identity) | n/a | n/a | no | no | si (via Identity `SetLockoutEndDateAsync`) | se modela con `security_version` (rotura sesiones); sin columna de LockoutEnd | sin exposicion | **CONFLICT** |
| `LockoutEnabled` | bool | si | true | no | no | si | n/a | n/a | gap |
| `AccessFailedCount` | int | si | n/a | no | no | si | sin columna | n/a | gap (la rate-limit vive en `lu_auth_rate_limit`) |
| `FirstName` | string | si | `Clean()` = trim + colapso de espacios | no | si | si | sin columna: `lu_user.full_name` (un solo campo) | `fullName` en `ManagedUserRecord` | **CONFLICT** (sin split nombres/apellidos) |
| `LastName` | string | si | `Clean()` | no | si | si | sin columna: incluido en `full_name` | no expuesto | gap |
| `SecondLastName` | string? | no | `Clean()` | no | si | si | sin columna: incluido en `full_name` | no expuesto | gap |
| `IdentityCard` | string | si | `Upper.Trim()` (max 10) | si (crea `Index` en DB via `IgnoreQueryFilters().AnyAsync`) | si (`CI:`) | si | sin columna | no expuesto en `ManagedUserRecord` ni en `ProfileRecord` | **CONFLICT** (CI eliminada) |
| `ProfilePictureUrl` | string? | no | guid name | no | si (avatar) | si (subida) | sin columna | no expuesto | **CONFLICT** |
| `Role` (enum `UserRole`) | enum int | si (default `Supervisor`) | enum value | no | si (`badge-info`) | si (solo Admin/SuperAdmin) | sin columna; viviria en `lu_site_membership.role` (`Administrador`/`Supervisor`) | `ManagedUserRecord.siteRole: SiteRole` | **CONFLICT** (rol multi-sede, no global de usuario) |
| `Status` (enum `GeneralStatus`) | enum int | si (`Activo`) | n/a | no | si (`badge-pill`) | si | `lu_user.status: 'active' \| 'disabled'` | `ManagedUserRecord.status: 'active' \| 'disabled'` (no third-state) | **CONFLICT** (sin `Eliminado` como estado user) |
| `Position` | string? | no | `Clean()` | no | si | si | sin columna | no expuesto | gap |
| `Department` | string? | no | `Clean()` | no | si | si | sin columna | no expuesto | gap |
| `HireDate` | DateTime? | no | n/a | no | si (sidebar de Details + ficha "Fecha de Alta" en Edit) | si | sin columna | no expuesto | gap |
| `CreatedById/Date`, `ModifiedById/LastModifiedDate` | int?/DateTime | si/no | n/a | no | si (Details Auditoria) | si (automatica) | sin columnas (`created_by_id` solo en catalogos acad. no en `lu_user`) | backend los omite | **CONFLICT** (UI/Details sin auditoria) |
| `IdentityRole<int>` en `lu_`... | n/a | n/a | n/a | n/a | n/a | n/a | sin tabla de roles; las membresias son tuplas `(user_id, site_id, role)` | expuesto via `memberships[]` | **CONFLICT** (sin tabla de roles) |

> Resumen: `User` se modela como `lu_user` (uuid, email, full_name, password_hash, is_super_admin,
> status enum-like, security_version) + `lu_site_membership (user_id, site_id, role, status,
> valid_from/until)`. Se conserva identidad multi-sede. Auditoria explicita (CreatedBy/ModifiedBy)
> no existe en `lu_user`.

### 6.2 Tabla detallada `Person` vs `lu_person`

| LEGACY FIELD | TYPE | REQUIRED | NORMALIZATION | UNIQUE | VISIBLE | WRITABLE | CURRENT POSTGRES FIELD | CURRENT CONTRACT FIELD | GAP |
|---|---|---|---|---|---|---|---|---|---|
| `Id` | int | si | auto | si (PK) | si | no | `lu_person.id bigint` | `PersonRecord.id: number` | PARTIAL (amplia rango int -> bigint) |
| `ActorCode` | string? | no | `btrim` (CHECK `length > 0` si no nulo) | unico por sede activo: `ux_lu_person_site_actor_code` | si | si | `lu_person.actor_code varchar(30)` | `PersonRecord.actorCode` | FULL |
| `Status` (enum `GeneralStatus`) | enum int | si (default `Activo`) | n/a | no | si | si | `lu_person.status: integer 0/1/2` | `PersonStatus = 0 \| 1 \| 2` | FULL |
| `Category` (enum `PersonCategory`) | enum int | si (default `Otro`) | n/a | no | si | si | `lu_person.category: integer (1..5, 99)` | `PersonCategory = 1 \| 2 \| 3 \| 4 \| 5 \| 99` | FULL |
| `Email` | string? (30 chars) | no | `EmailAddress` | no | si | si | `lu_person.email varchar(100)` | `PersonRecord.email` | FULL |
| `PhoneNumber` | string? (20 chars) | no | `Phone` | no | si | si | `lu_person.phone_number varchar(20)` | `PersonRecord.phoneNumber` | FULL |
| `CreatedDate` | DateTime | si (`UtcNow`) | n/a | no | no (no se muestra en Index) | no | `lu_person.created_at` (no expuesto) | backend | gap (auditoria oculta) |
| `CreatedById/ModifiedById/LastModifiedDate` | int?/DateTime | no | n/a | no | no | no | `created_by_id`, `updated_by_id`, `updated_at` (no expuestos) | backend | gap |
| `ImportBatchId` | int? + nav | no | n/a | no | no | no | n/a (sin import batch en tenant actual) | n/a | gap |
| `RowVersion` | byte[] | si | `[Timestamp]` | no | no | no | n/a (no hay version optimista en `lu_person`) | n/a | gap |
| `FullName` (virtual) | derivado | n/a | `[NotMapped]` | n/a | si | no | `lu_person.name varchar(200)` (single field) | `PersonRecord.name` | **CONFLICT** (sin split; sin FullName derivado) |
| `Aliases` | `ICollection<PersonAlias>` | no | n/a | no | no | no | n/a (sin tabla `lu_person_alias`) | n/a | gap |
| `RoleAssignments` | `ICollection<PersonRoleAssignment>` | no | n/a | no | no | no | n/a (sin tabla `lu_person_role_assignment`) | n/a | gap |
| `MaintenanceParticipations / RequestedRequests / ResponsibleVerifications / ProvidedCostDetails / ResponsibleManagementPlans / ResponsibleMaintenancePlans` | colecciones de navegacion | no | n/a | no | no (no se exponen en `_PersonsTable`) | no | n/a (sin tablas tenant para estas colecciones en este read) | n/a | gap (relevantes para otras migraciones verticales) |
| (Intern/Extern subclass) | TPT | n/a | `Person.ToTable("People")` + `Intern.ToTable("Interns")` + `Extern.ToTable("Externs")` | no | si (badge tipo) | n/a | target aplana a una fila `lu_person` con `person_type`, `is_entity`, `address` | `PersonRecord.type: 'internal' \| 'external'` + `isEntity`, `address` | **CONFLICT** (TPT -> tabla plana con discriminador logico) |
| `InternStatus` / `ExternStatus` | `GeneralStatus` en tablas derivadas | si por subtipo | enum 0/1/2 | no | si en Edit Persons | si | no existen; solo `lu_person.status` | no expuestos | **CONFLICT** (dos estados de subtipo colapsados) |

> Resumen: `Person` en target = `lu_person` con `name` simple, `type` textual, `category` numerico,
> `is_entity/address` solo para externos. `FullName` derivable aparece como `name` y `lu_person` no
> crea un campo `lu_person_full_name`.

### 6.3 Identidad local y autenticacion

| Capa | ASP legacy | Target | Brecha |
|---|---|---|---|
| Identidad | `IdentityUser<int>` + `IdentityRole<int>` + `IdentityDbContext`. | `lu_user` uuid + `lu_site_membership(user_id, site_id, role)` + `lu_session`. Sin tabla de roles. | CONFLICT (esquema distinto). |
| Password | Identity configurable (12 chars, 4 unique; requiere digit/lower/upper/nonAlphaNumeric). IdentityUser.PasswordHash (PBKDF2 por default). | `bcryptjs` con `bcryptCost` configurable, almacenado en `password_hash`. | CONFLICT (PBKDF2 -> bcrypt). |
| Lockout | Identity `SetLockoutEndDateAsync(MaxValue)` (queda permanente hasta limpieza manual). | sin columna LockoutEnd: invalidacion via `security_version++` y rechazo de sesiones con `securityVersion` desactualizada (`getSessionContext`). `SetStatus('disabled')` bannea al usuario. | CONFLICT (lockout permanente -> ban + seguridad version). |
| TwoFactor | `SignInManager.RequiresTwoFactor`-> `LoginWith2fa` redirect (identity estandar). | sin 2FA; rate limit por `lu_auth_rate_limit` (email_ip + ip). | CONFLICT (sin 2FA). |
| Roles Identity | `IdentityRole<int>` + tabla de rol/claims. `SynchronizeManagedRoleAsync` mantiene un solo rol managed por user. | `lu_site_membership.role` (texto `Administrador`/`Supervisor`) + `lu_user.is_super_admin`. `UserController.updateUser` cambia `membership.role`, no una hipotetica tabla de roles. | CONFLICT (roles por sede, no identidad). |
| Login identifier | `UserName` o `Email` ambos posibles (busqueda dual). | email solamente (lower). | CHANGED (ver LC-02). |

---

## 7. Comparacion auth/security

### 7.1 Credenciales y almacenamiento

- Legacy: `ApplicationDbContext.AddEntityFrameworkStores<ApplicationDbContext>()`; password hash via
  Identity (PBKDF2 configurable pero por defecto Identity v3 usa PasswordHasher).
- Target: hash con `bcryptjs`. Validaciones en `auth.service.constantTimeBcryptCompare` con
  proteccion de timing y `dummyHash`. `repo.findUserWithMembershipsByEmail` solo case-insensitive
  email; no se busca por username.

### 7.2 Sesion y CSRF

- Legacy: cookie auth `.ProyectoUnivalle.Auth.vUniversal`, httpOnly true, secure same-as-request en
  dev / always en prod, sliding 8h. Antiforgery cookie `.ProyectoUnivalle.Antiforgery.vUniversal`.
- Target: cookie `__Host-lu_session` (`auth.constants.ts`), `httpOnly`, `secure=true`,
  `sameSite=Lax`. Token de sesion random
  hasheado (`hashSessionToken`) y almacenado como `token_hash char(64)` (SHA-256 lower hex). CSRF
  independiente (`csrf` cookie `httpOnly`, `secure=true`).

### 7.3 Rate limit

- Legacy: `Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15min`.
- Target: `auth.rate-limit.ts` con buckets `email_ip` y `ip`; defaults de `auth.config.ts`: 10
  intentos email+IP, 50 por IP y ventana de 60 segundos, todos configurables por entorno.

### 7.4 Auditoria / seguridad

- Legacy: `SecurityStamp` actualizado en cada `UpdateSecurityStampAsync` invalida cookies.
- Target: `security_version` bigint monotono se incrementa en el update administrativo de
  `users.repository.update`. El update autocentrado de perfil cambia email/nombre sin incrementar
  `security_version`; sesiones ya emitidas sobreviven a ese cambio (LC-13).
- Target valida login a maximo 72 **bytes UTF-8**, pero Create/Edit Users valida 8-72
  `string.length` (unidades UTF-16). Una contrasena Unicode aceptada por alta puede ser rechazada al
  iniciar sesion o truncada semanticamente por bcrypt (LC-14).

### 7.5 Cadenas y secretos

- F0 no abre ni reproduce secretos de entorno. Para fases posteriores: "local PostgreSQL
  credentials are already provisioned outside tracked files". Ningun valor se persiste aqui.

---

## 8. Personas - alcance, reutilizacion, no duplicacion

- ASP mantiene un **unico** agregado `Person` con herencia TPT (`People` + `Interns`/`Externs`) y lo expone desde tres
  frentes: (a) `/Persons/Index/Create/Edit/Details/Delete` dedicados, (b) el tab "Directorio de
  Personal" dentro de `/Users/Index` via partial `_PersonsTable.cshtml`, y (c) relaciones
  inversas (Departures, RoleAssignments, MaintenanceParticipations, RequestedRequests,
  ResponsibleVerifications, ProvidedCostDetails, ResponsibleManagementPlans,
  ResponsibleMaintenancePlans).
- Target: `PersonPage` se expone **solo** desde `PeoplePanel` montado por `App.tsx` cuando la ruta
  es `/Persons/Index`. **El tab equivalente en ASP no existe en target** porque el target separa
  `/Users/Index` y `/Persons/Index` y muestra cada uno en su panel catalog-panel dedicado.
- Reutilizacion en target: `PersonRecord` se usa tambien como filtro de relaciones en otros
  contratos (mantenimientos, solicitudes, verificaciones). El panel actual no las renderiza en UI.
- **Sin duplicacion**: el target **no duplica** personas en `lu_user`. El usuario (cuenta) y la
  persona (actor de negocio) son identidades independientes. Esto preserva la separacion legacy.

---

## 9. Confirmaciones A-J

| ID | Afirmacion | Estado | Evidencia |
|---|---|---|---|
| A | Pestanas Users/Persons dentro de la misma pagina (tab) en ASP -> target usa rutas separadas | **CONFIRMED** | `Pages/Users/Index.cshtml` muestra dos `tab-pane` (`#usuarios` y `#personas`) en una sola ruta. Target monta `PeoplePanel` en `/Persons/Index` y `UsersPanel` en `/Users/Index`. |
| B | ASP expone 5 paginas (`Index/Create/Edit/Details/Delete`) -> el target concentra administracion en `UsersPanel` | **CONFIRMED** | ASP tiene cinco vistas/code-behinds. Target concentra listar/crear/editar/deshabilitar en `UsersPanel` y usa `ProfilePanel` para Mi Perfil. |
| C | Vista `Details` admin (sidebar de Estatus, Acciones, Auditoria) vs `Mi Perfil` (autocentrado) | **CONFIRMED** | Details ASP es ficha administrativa; `/Users/Details` target monta `ProfilePanel` limitado. |
| D | Login ASP acepta username **o** email; target solo email | **CONFIRMED** | `FindByNameAsync ?? FindByEmailAsync` vs `findUserWithMembershipsByEmail`. |
| E | `User` legacy tiene mas campos que `lu_user` actual | **CONFIRMED** | `lu_user`/memberships omiten CI, telefono, foto, campos laborales, nombres separados y actores de auditoria. |
| F | `GeneralStatus` tiene 3 valores; target users usa `active/disabled` | **CONFIRMED** | `Activo=0, Inactivo=1, Eliminado=2` vs `ManagedUserStatus = 'active' \| 'disabled'`. |
| G | `0005_user_management.sql` concede permisos pero no representa todos los campos legacy | **CONFIRMED** | El archivo contiene advisory lock y un `GRANT SELECT, INSERT, UPDATE` sobre `lu_user`/`lu_site_membership`; no contiene DDL ni agrega `UserName`, CI, telefono, foto, cargo, departamento, fecha de ingreso o actores de auditoria. Ademas, existe en disco pero no esta en `MIGRATION_REGISTRY`. |
| H | `migration-registry` puede no registrar las migraciones posteriores necesarias | **CONFIRMED** | `apps/api/src/database/migration-registry.ts` registra exactamente 0001, 0002 y 0003; la prueba exige longitud 3 y que `0004_something.sql` sea desconocida. `control-plane/0004`, `control-plane/0005` y `tenant/0012` no estan registrados; `validatePinnedMigration` rechaza archivos no pinneados. Debe definirse en F1/F2 si el registro central cubrira tambien tenant migrations o si existe otro runner aprobado. |
| I | Shell visual React (topbar + sidebar + breadcrumbs + perfil + cierre) imita NiceAdmin con clases `app-shell`, `topbar`, `left-sidebar`, `top-navbar`, `profile-menu`, `profile-button` | **PARTIAL** | El `App.tsx` reproduce la jerarquia visual NiceAdmin con clases `.app-shell`, `.topbar`, `.navbar.top-navbar.navbar-dark`, `.left-sidebar`, `.sidebar-nav`, `.profile-menu`, `.profile-button`, `.user-menu`, `.site-switcher`, `.notification-button`. Las clases son homonimas (mismo nombre, no mismas reglas CSS), pero la jerarquia visual coincide. NO es port 1:1 - el shell target usa CSS nuevo en `styles.css`. |
| J | Panel generico catalogado (`catalog-panel`) concentra UI para catalogos | **CONFIRMED** | `App.tsx` define `CatalogPanel` (paises/ciudades), `AcademicPanel` (laboratorios), `EquipmentPanel`, etc.; `apps/web/src/styles.css` linea 1327 define `.catalog-panel { display: grid; gap: 1rem; padding: clamp(1rem, 2vw, 1.5rem); border: 1px solid #e5edf2; border-radius: 1rem; background: #fff; box-shadow: 0 12px 28px rgb(43 91 113 / 8%); }`. UsersPanel/PeoplePanel usan `.catalog-panel users-panel` y `.catalog-panel people-panel` respectivamente. La portada visual es comun pero los selectores especificos `.users-panel` y `.people-panel` no aparecen en `styles.css` (registrado como `LEGACY_CONTRADICTION` LC-05 menor). |

---

## 10. LEGACY_CONTRADICTION

Catalogo de contradicciones detectadas entre el codigo ASP, los contratos target y la arquitectura
aprobada. Listadas por severidad. Cada LC incluye `A/B` evidencia, conducta observable probable y la
necesidad de decision para F1.

### LC-01 — Confirmacion SweetAlert pre-Edit/Delete en ASP

- **A**: `context.md` declara "Editar y eliminar requieren confirmacion SweetAlert cuando cambian
  datos existentes o hacen soft-delete" y `Pages/AGENTS.md` reitera "Edit: confirmar con
  SweetAlert antes de guardar cambios" y "Delete/soft-delete: confirmar con SweetAlert antes de
  enviar". Los `.cshtml` Create/Edit/Delete en ASP **no contienen** JS de confirmacion
  SweetAlert inline (`Pages/Users/Edit.cshtml` solo importa `_ValidationScriptsPartial` y
  `jqbootstrapvalidation`; `Delete.cshtml` es submit directo).
- **B**: `_Layout.cshtml` completo solo usa SweetAlert para toasts de TempData; `wwwroot/js/site.js`
  lo usa para rollback del wizard. Ninguno intercepta Users/Persons Edit/Delete. El target usa
  `window.confirm` solo en bajas y guarda Edit directamente.
- **Conducta observable probable**: ASP Edit y Delete envian POST sin confirmacion previa; target
  muestra confirmacion nativa en Delete y tampoco confirma Edit. La contradiccion es entre codigo
  observable y las reglas documentales, no un `UNKNOWN`.
- **Necesidad F1**: ninguna de datos. F5 debe calcar primero la conducta observable o registrar una
  decision explicita antes de introducir SweetAlert.

### LC-02 — Login identifier: ASP username-or-email vs target email-only

- **A**: `Login.cshtml.cs` linea `FindByNameAsync(login) ?? FindByEmailAsync(login)` permite ambos.
- **B**: `auth.repository.findUserWithMembershipsByEmail(email)` solo consulta por email y el
  contract `LoginRequest.email: string` (single field).
- **Conducta observable probable**: si un usuario conserva `UserName` distinto de su email (caso
  real en ASP al crear usuarios con login tecnico), deja de poder entrar al migrar.
- **Necesidad F1**: decidir si el target final aceptara tanto username como email, o si el dominio
  de identidad se consolida en email (consolidacion ya aplicada). Si se consolida, asegurar
  migracion de los `UserName` legacy a `email` o conservar un identificador separado. `0005` no
  realiza siembra ni agrega campos: solo concede permisos.

### LC-03 — `IdentityCard` desaparece del target

- **A**: `Models/User.cs` define `[Required, StringLength(10), RegularExpression("^[0-9A-Z-]*$")]
  IdentityCard`. `Create.cshtml` y `Index.cshtml.cs` la usan para mostrar `CI: <value>` y filtrar
  busqueda.
- **B**: el contrato `ManagedUserRecord` y `ProfileRecord` no exponen `IdentityCard`; la busqueda
  `users.repository.list` busca solo en `full_name` + `email`.
- **Conducta observable probable**: usuarios existentes con CI duplicada o CI como dato clave
  visible quedan sin trazabilidad visual. La integridad referencial se pierde.
- **Necesidad F1**: decidir donde va `IdentityCard` del legacy (en `lu_user`, en `lu_person`, en
  una tabla de identidad extendida del control plane). Tambien resolver la busqueda por CI durante
  la siembra.

### LC-04 — Soft-delete semantics: ASP `Eliminado` (estado, no se cambia password) vs target `disabled` (rotura security_version)

- **A**: ASP elimina lógicamente seteando `Status = Eliminado` y `SetLockoutEndDateAsync(MaxValue)`
  sin `UpdateSecurityStampAsync` automatico (asp lo hace manual en el handler). El usuario
  conserva su `SecurityStamp` hasta otro evento; `LastModifiedDate` cambia.
- **B**: target setea `status='disabled'` (en `users.repository.update`) e incrementa
  `security_version`, invalidando todas las sesiones (`getSessionContext` rechaza si
  `user.securityVersion != session.securityVersion`).
- **Conducta observable probable**: ASP permite una "blanda" reactivacion (no hay rotacion de
  stamp automatic al volver a `Activo`); target la hace obligatoria. La auditoria del legacy
  mostrara `Eliminado` en el detalle; el target nunca expone `Eliminado` (solo
  active/disabled).
- **Necesidad F1**: documentar y conservar el modelo "trestados" en auditoria (puede vivir en un
  flag append-only en `lu_user` o en una vista/snapshot de importacion). Decidir si F1 introduce
  un `lu_user_log` o un campo `status_snapshot`.

### LC-05 — Foto de perfil y `displayName`/`initials`

- **A**: ASP sube fotos a `wwwroot/uploads/users/{guid}{name}` con cache-busting `?v=Ticks`.
  Componente `UserProfile` y `Details` muestran la foto; el listado del sidebar header la usa.
- **B**: target no expone foto en ningun endpoint ni en ninguna UI. `App.tsx` calcula
  `initials(displayName)` y renderiza un `profile-avatar`. `UsersPanel` no tiene columna de avatar.
- **Conducta observable probable**: la fotografia historica de los usuarios se pierde en el target.
- **Necesidad F1**: si la foto es dato institucional, decidir almacenamiento (S3? tenant DB?
  control plane blob? referencia externa?) y mecanismo de subida. Mientras tanto, `SafeImageUpload`
  deberia tener un equivalente Node en `apps/api/src/users/`.

### LC-06 — Selector `.users-panel` / `.people-panel` / `.profile-panel` no definidos

- **A**: `UsersPanel.tsx`/`PeoplePanel.tsx`/`ProfilePanel` usan `className="catalog-panel
  users-panel"`, etc.
- **B**: `styles.css` define `.catalog-panel` (linea 1327) pero no `.users-panel`,
  `.people-panel`, `.profile-panel`. Las clases son **noop** en el CSS actual.
- **Conducta observable probable**: el padding/border/shadow se aplican por `.catalog-panel`; los
  selectores especificos no producen diferencias visibles. Es un olor tecnico no funcional.
- **Necesidad F1**: si F6 (visual parity) decide paridad 1:1 con NiceAdmin, anadir selectores
  especificos o reescribir el HTML. Si no, eliminar `users-panel/people-panel/profile-panel` de las
  classNames.

### LC-07 — Auditoria (CreatedBy/ModifiedBy/CreatedAt/UpdatedAt) sin exposicion

- **A**: `User` y `Person` exponen campos de auditoria y `Index.cshtml` muestra `Creado por` y
  `Registro Auditado` (Details).
- **B**: `lu_user` no tiene `created_by_id/updated_by_id` y los endpoints no exponen `createdAt` ni
  `updatedAt` por managed-user (`ManagedUserRecord.createdAt/updatedAt` existen y vienen de
  `lu_user.created_at/updated_at`, **si**; pero el repositorio solo devuelve 6 columnas y los
  incluye — `createdAt`, `updatedAt` se exponen en `ManagedUserRecord`; por lo tanto es **PARCIAL**,
  no CONFLICT). Perfil no expone; `Mi Perfil` no muestra auditoria.
- **Conducta observable probable**: `Mi Perfil` y `Managed Users` carecen de "auditoria visible"
  en target.
- **Necesidad F1**: enriquecer `ProfileRecord` con campos de auditoria, o agregar
  `ManagedUserRecord.audit` consistente con el detail visual legacy.

### LC-08 — `My Profile` colapso del menu admin

- **A**: `Pages/Users/Details.cshtml` es la **ficha completa del usuario** (sidebar Estatus,
  Acciones, Auditoria), accesible tanto para admin como para el propio usuario desde el header.
  Editable por Admin; el propio usuario edita su `Email`/`PhoneNumber` indirectamente cuando entra
  a Edit (porque el codigo permite: cualquier admin puede editar la cuenta, **incluido** el propio
  user, con el veto de "no cambiar tu propio rol ni desactivar tu propia cuenta").
- **B**: target usa `/Users/Details` solo como Mi Perfil autocentrado con `ProfilePanel` que
  expone `email/fullName/siteRole` y permite editar email/fullName (sin role, sin status, sin
  password).
- **Conducta observable probable**: el propio usuario en target **no puede** cambiar su password
  por si mismo, ni ver su CI, ni ver la auditoria. La ficha admin queda como tool separada
  `/Users/Index` -> Edit.
- **Necesidad F1**: validar que el flujo "Cambiar mi propia password" este disponible por otra
  ruta (probablemente endpoint `POST /profile/password` aun no implementado). Documentarlo.

### LC-09 — PhoneNumber eliminado del modelo target

- **A**: `User` tiene `PhoneNumber [Required, Phone]`. Mostrado en Details y header.
- **B**: `ManagedUserRecord` y `ProfileRecord` no exponen `phoneNumber`. `lu_user` no tiene
  columna.
- **Conducta observable probable**: telefono de contacto desaparece.
- **Necesidad F1**: decidir si `lu_user` debe llevar `phone_number` (compatible con PG VARCHAR(20)
  y patron `Phone`). Si no, mover el telefono a `lu_person` (es donde queda en `0012_people.sql`).

### LC-10 — Selector `data-live-filter-search` y `data-live-filter-form` no usado en target

- **A**: `Pages/Users/Index.cshtml` usa `data-live-filter-search` y `data-live-filter-form`; el
  consumidor real es `wwwroot/js/site.js`, que aplica debounce de 500 ms y submit del formulario.
- **B**: target no tiene `smart-index.css` ni hooks `data-live-filter-*`. El filtrado del
  `UsersPanel` no es "live" (no recarga al teclear); recarga tras `onChange` del filtro estado.
- **Conducta observable probable**: comportamiento diferente del smart-index legacy (debounce + URL
  update) vs React (`useEffect` cuando cambia `statusFilter`).
- **Necesidad F1**: si F5 (paridad funcional) exige debounce + URL, anadir debouncer + history.
  Documentar.

### LC-11 — SuperAdmin global sin sede puede autenticarse pero no entrar al workspace

- **A**: `auth.repository.ts` permite una sesion de SuperAdmin con `activeSiteId=null`, incluso sin
  membresia.
- **B**: `core.guard.ts::SiteContextGuard` rechaza toda solicitud de workspace cuando
  `activeSiteId===null`; React dirige al selector de sede y no ofrece una sede seleccionable si no
  hay membresias.
- **Conducta observable probable**: el SuperAdmin global inicia sesion pero no puede abrir Users ni
  Persons sin una membresia/sede activa.
- **Necesidad F1**: fijar el contrato de acceso global-a-sede; la implementacion corresponde a F3/F5.

### LC-12 — Autorizacion de Persons no reconoce SuperAdmin global

- **A**: `users/user.service.ts::administrator` acepta `SiteRole.Administrador` o
  `GlobalRole.SuperAdmin`.
- **B**: `people/person.service.ts::administrator` acepta solo `SiteRole.Administrador`.
- **Conducta observable probable**: un SuperAdmin con contexto global permitido para Users recibe
  `SITE_ACCESS_DENIED` al reutilizar People, rompiendo el segundo tab.
- **Necesidad F1**: definir la matriz RBAC consistente; implementar en F3.

### LC-13 — Cambio de email propio no rota `security_version`

- **A**: el update administrativo `user.repository.update` incrementa `security_version`.
- **B**: `user.repository.updateProfile` cambia `email/full_name` sin incrementarlo.
- **Conducta observable probable**: sesiones ya emitidas siguen siendo validas tras cambiar el
  identificador email desde Mi Perfil, a diferencia del update administrativo.
- **Necesidad F1**: fijar que cambios de identificador/password/estado invalidan sesiones; F3 debe
  aplicar el contrato de forma uniforme.

### LC-14 — Limite de contrasena por caracteres en alta y por bytes en login

- **A**: `users/user.controller.ts` acepta Create/Edit con `result.length` entre 8 y 72.
- **B**: `auth/auth.types.ts` rechaza login si la contrasena supera 72 bytes UTF-8; bcrypt tiene el
  mismo limite material de 72 bytes.
- **Conducta observable probable**: una contrasena Unicode puede ser aceptada al crear/editar y
  rechazada al iniciar sesion o compartir el prefijo efectivo de bcrypt.
- **Necesidad F1**: contrato unico en bytes UTF-8; correccion en F3/F4.

### LC-15 — Filtro Persons `status=2` es inalcanzable en target

- **A**: PeoplePanel ofrece el estado Baja (`2`) y el contrato permite `PersonStatus=2`.
- **B**: `person.repository.list` siempre agrega `status<>2` y, si se solicita 2, agrega tambien
  `status=2`.
- **Conducta observable probable**: el filtro visible Baja devuelve siempre cero filas.
- **Necesidad F1**: fijar si las bajas deben ser consultables para paridad; implementar en F3/F5.

### LC-16 — Politica de password legacy contradice formulario y DataAnnotations

- **A**: Create declara minimo 8 y el help text habla de letras/numeros.
- **B**: `Program.cs` exige 12 caracteres, mayuscula, minuscula, digito y no alfanumerico.
- **Conducta observable probable**: el formulario acepta inicialmente valores que Identity rechaza
  al ejecutar `CreateAsync`, devolviendo errores de Identity despues del submit.
- **Necesidad F1**: la calca debe conservar el mensaje/orden observable o aprobar un contrato unico;
  no corregir silenciosamente.

---

## 11. ARCHITECTURAL_MAPPING_REQUIRED

Asuntos que requieren mapeo arquitectonico antes de F1:

1. **Identidad legacy -> identidad local target**: mapeo uno-a-uno de `User` (ASP Identity) a
   `lu_user` (PostgreSQL) + `lu_site_membership` por sede. Decidir tratamiento de `UserName`,
   `IdentityCard`, `PhoneNumber`, foto, `Position/Department/HireDate` y auditoria.
2. **Roles**: ASP `UserRole` (`Administrador=1, Supervisor=2, SuperAdmin=99`) ->
   `SiteRole.Administrador/Supervisor + lu_user.is_super_admin`. `SuperAdmin` legacy pasa a
   `is_super_admin=true`; debe resolverse la contradiccion entre sesion global sin membresia y
   `SiteContextGuard`, que exige sede activa.
3. **Estado**: ASP `GeneralStatus` (3 valores) -> target `active/disabled` + eventual
   `lu_user_status_log` o flag append-only. El `Eliminado=2` debe quedar trazable.
4. **Sesion, hashes y credenciales**: ASP usa cookie sliding 8h, PBKDF2 Identity y antiforgery;
   target usa sesiones server-side, bcrypt y CSRF cookie. F1 debe decidir migracion de hashes
   (compatibilidad temporal, reset obligatorio u otra estrategia) y eventos que rotan sesion.
5. **Personas**: `Person`/`Intern`/`Extern` (TPT) -> `lu_person` con `person_type
   ('internal','external')`, `category (1..5,99)`, `is_entity`, `address` (solo externos). El
   `FullName` derivado se aplana a `name`; `InternStatus`/`ExternStatus`, `RowVersion` e
   `ImportBatchId` no tienen equivalente actual. Las relaciones operativas no deben duplicarse.
6. **Multi-sede**: confirmado por `CONTRATO_MULTISEDE.md` y por el header de ActiveSiteSession en
   `site.ts`. Plan: `activeSiteId` se selecciona en `login` o se cambia via `PUT
   /auth/session/active-site`. `SitePicker` aparece cuando `activeSiteId === null` y hay
   memberships > 0 o `is_super_admin`.
7. **Storage de fotos**: sin definir. F1 debe decidir (filesystem tenant? S3? BFF-only?).
8. **PhoneNumber**: sin columna en `lu_user`. Decidir destino (reusar `lu_person`? agregar a
   `lu_user`?).
9. **SafeImageUpload** y `_environment.WebRootPath/uploads/users/`: este artefacto se conserva en
   ASP, pero el target debe tener un equivalente al construir el panel "Mi Foto". Por ahora es
   `DORMANT_OR_NOT_EXPOSED`.
10. **Notificaciones en topbar**: el codigo ASP filtra por `currentPage` para mostrar la campana;
     target muestra campana **siempre** en workspace. Confirmar si F5 exigira condicionalidad.
11. **RBAC Persons**: unificar la regla para SuperAdmin global y Administrador por sede (LC-12).
12. **Seguridad de perfil/password**: resolver rotacion de `security_version` y limite de 72 bytes
    de forma consistente (LC-13/LC-14).
13. **Consulta de bajas Persons**: resolver el filtro contradictorio `status<>2 AND status=2`
    (LC-15).
14. **Registro de migraciones**: `MIGRATION_REGISTRY` solo pinnea 0001-0003. F1 define el contrato
    y F2 registra/manifiesta las migraciones aprobadas; no se aplican SQL no registrados por
    iniciativa propia.

---

## 12. UNKNOWN / hallazgos pendientes

Lecturas de codigo pendientes: **ninguna** de las dependencias Users/Persons detectadas. Unknowns
que son decisiones y no deben resolverse en F0:

1. Via operativa aprobada para `control-plane/0004`, `control-plane/0005` y `tenant/0012`, que
   existen en disco pero no estan en `MIGRATION_REGISTRY`; no se encontro un contrato unico que las
   haga ejecutables por `migration-cli`.
2. Destino institucional y politica de retencion de fotos legacy.
3. Estrategia para hashes PBKDF2 legacy frente a bcrypt target.
4. Representacion final de username, CI, telefono, cargo, departamento, fecha de ingreso,
   auditoria por actor y tercer estado eliminado.
5. Regla final de SuperAdmin global sin membresia/sede activa y autorizacion coherente de Persons.

Hechos cerrados que no son `UNKNOWN`: `LoginWith2fa`/`Lockout` no existen en el source; Users y
Persons no muestran SweetAlert antes de Edit/Delete; `Clean()` recorta/colapsa espacios;
`PaginatedList` pagina con `Skip/Take`; Persons usa TPT.

---

## 13. Referencias y verificabilidad

- `git ls-tree reference/asp-final -r --name-only | Select-String "^(Pages|Models|Helpers|Data|Program|Startup|wwwroot)"`
  confirma la existencia de cada ruta citada.
- `git show reference/asp-final:<ruta>` confirma el contenido del path citado para legacy.
- `git ls-tree HEAD --name-only <path>` confirma cada ruta target citada; `git show HEAD:<path>`
  confirma el contenido de los contracts y migraciones.
- `git rev-parse HEAD` antes y despues de este trabajo = `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`
  (sin commit, en `migration/react`).
- Los archivos creados en F0 son unicamente cuatro: `docs/migration/users/00-SOURCE-SPEC.md`,
  `00-PARITY-MATRIX.md`, `00-MASTER-PLAN.md`, `00-HANDOFF.md`.

> El baseline se tomo al iniciar F0. Se conservan los cambios previos ajenos a `.opencode/**` y
> `opencode.json`; el worker no revierte ni modifica nada fuera de los cuatro docs asignados.
