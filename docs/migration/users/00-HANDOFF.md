# 00-HANDOFF.md — User/Person mig F0 handoff

> Documento de cierre del trabajo `MIG-001-F0`. Propietario: `migration-worker` (MiniMax-M3). El
> orquestador lo recibe para decidir si acepta F0 como `COMPLETE/PARTIAL/BLOCKED` y autorizar
> `F1_IDENTITY & DATA CONTRACT`. Este archivo cumple las 15 condiciones del alcance F0 y se
> limita a registrar informacion sin diseno futuro. No incluye otras secciones principales fuera
> del orden exacto exigido.

## PHASE

F0

SOURCE FORENSICS del modulo Users/Persons + Login + Shell. Trabajo: `MIG-001-F0`.
Branch de trabajo: `migration/react`.

## STATUS

COMPLETE

Evidencia consolidada en los otros tres artefactos y revisada de forma independiente. El worktree
conserva cambios previos ajenos en `.opencode/**` y `opencode.json`; los unicos cambios propios de
F0 son estos cuatro documentos. Los comandos realmente ejecutados se registran en `VALIDATION`.

## SOURCE_BRANCH

`reference/asp-final` (rama de referencia aprobada por el orquestador).

## SOURCE_SHA

`dccabf50330afc48760d06bd4dbaff8c37ebbd3f`

## TARGET_BRANCH

`migration/react`.

## TARGET_START_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (HEAD inicial apuntado por el orquestador).

## TARGET_END_SHA

`7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480` (sin commit al cierre de F0; el worker solo escribio
los cuatro documentos nuevos en `docs/migration/users/`).

## FILES_INSPECTED

### Legacy (via `git show reference/asp-final:<ruta>`)

- `Pages/Users/Index.cshtml`
- `Pages/Users/Index.cshtml.cs`
- `Pages/Users/Create.cshtml`
- `Pages/Users/Create.cshtml.cs`
- `Pages/Users/Edit.cshtml`
- `Pages/Users/Edit.cshtml.cs`
- `Pages/Users/Details.cshtml`
- `Pages/Users/Details.cshtml.cs`
- `Pages/Users/Delete.cshtml`
- `Pages/Users/Delete.cshtml.cs`
- `Pages/Login.cshtml`
- `Pages/Login.cshtml.cs`
- `Pages/Shared/_Layout.cshtml` (completo)
- `Pages/Shared/_Sidebar.cshtml`
- `Pages/Shared/_PersonsTable.cshtml`
- `Pages/Shared/Components/UserProfile/Default.cshtml`
- `Pages/Shared/Components/UserProfileViewComponent.cs`
- `Pages/Persons/Index.cshtml`
- `Pages/Persons/Create.cshtml` y `.cshtml.cs`
- `Pages/Persons/Edit.cshtml` y `.cshtml.cs`
- `Pages/Persons/Details.cshtml` y `.cshtml.cs`
- `Pages/Persons/Delete.cshtml` y `.cshtml.cs`
- `Pages/_ValidationScriptsPartial.cshtml`
- `Pages/Shared/_ValidationScriptsPartial.cshtml`
- `Models/User.cs`
- `Models/Person.cs`
- `Models/Intern.cs`
- `Models/Extern.cs`
- `Models/Enums/UserRole.cs`
- `Models/Enums/GeneralStatus.cs`
- `Helpers/AuthorizationHelper.cs`
- `Helpers/SafeImageUpload.cs`
- `Helpers/IdentityRoleExtensions.cs`
- `Helpers/TempDataExtensions.cs`
- `Helpers/EnumHelper.cs`
- `Helpers/StringExtensions.cs`
- `Helpers/PaginatedList.cs`
- `Data/ApplicationDbContext.cs`
- `Program.cs`
- `wwwroot/js/site.js`

Inventario confirmado con `git ls-tree reference/asp-final -r --name-only | Select-String
"^(Pages|Models|Helpers|Data|Program|Startup|wwwroot)"`.

### Target (worktree `migration/react @ 7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`)

- `apps/web/src/App.tsx` (completo, 2132 lineas)
- `apps/web/src/UsersPanel.tsx`
- `apps/web/src/PeoplePanel.tsx`
- `apps/web/src/App.test.tsx`
- `apps/web/src/styles.css` (zonas Users/People/Profile/shell relevantes)
- `apps/api/src/users/user.controller.ts`
- `apps/api/src/users/user.service.ts`
- `apps/api/src/users/user.repository.ts`
- `apps/api/src/people/person.controller.ts`
- `apps/api/src/people/person.service.ts`
- `apps/api/src/people/person.repository.ts`
- `apps/api/src/auth/auth.controller.ts`
- `apps/api/src/auth/auth.service.ts`
- `apps/api/src/auth/auth.repository.ts`
- `apps/api/src/auth/auth.constants.ts`
- `apps/api/src/auth/auth.config.ts`
- `apps/api/src/auth/auth.types.ts`
- `apps/api/src/core/core.guard.ts`
- `apps/api/src/core/core.module.ts`
- `apps/api/src/core/tenant-routing.ts`
- `apps/api/src/database/migration-registry.ts`
- `apps/api/src/database/migration-runner.ts`
- `apps/api/src/database/migration-cli.ts`
- `apps/api/migrations/0001_create_identity_control_plane.sql`
- `apps/api/migrations/control-plane/0004_academic_catalogs.sql`
- `apps/api/migrations/control-plane/0005_user_management.sql`
- `apps/api/migrations/tenant/0012_people.sql`
- `apps/api/migrations/tenant/0001_dashboard_foundation.sql` (existencia)
- `packages/contracts/src/users.ts`
- `packages/contracts/src/auth.ts`
- `packages/contracts/src/site.ts`
- `packages/contracts/src/people.ts`
- `packages/api-client/src/index.ts` (parcial: 200..450)
- `apps/api/test/auth.*.e2e-spec.ts` y `migration-registry.e2e-spec.ts` (lectura dirigida; no hay
  suite dedicada Users/People).

## ASP_FLOWS_DISCOVERED

Resumen ejecutivo. Detalles por campo/redirect/mensaje, en `00-SOURCE-SPEC.md` secciones 3 y 5.

### FLOW NAME: LOGIN BY-USERNAME-OR-EMAIL

- **ENTRY POINT**: `/Login` (`Pages/Login.cshtml`).
- **AUTHORIZED ROLES**: `[AllowAnonymous]`.
- **INPUT**: `Input.UserName` (acepta username O email), `Input.Password`, `Input.RememberMe`.
- **NORMALIZATION**: `login = Input.UserName.Trim()`. Email normalizado en `CreateAsync`
  (`Email.Trim().ToLowerInvariant()`). No hay normalizacion de username en login.
- **VALIDATIONS**: model state `Required` para UserName/Password; `[EmailAddress]` no se usa
  porque Input.UserName admite ambos. Longitud no validada en login. Anti-tampering via modelo.
- **DATABASE READS**: `_userManager.FindByNameAsync(login) ?? _userManager.FindByEmailAsync(login)`.
- **DATABASE WRITES**: ninguno en login; cookie issued via `SignInManager` (Identity).
- **IDENTITY OPERATIONS**: `PasswordSignInAsync(user, password, Input.RememberMe, lockoutOnFailure:
  true)`; alterna ramas `Succeeded` -> `LocalRedirect(returnUrl)`, `RequiresTwoFactor` ->
  `LoginWith2fa`, `IsLockedOut` -> `Lockout`, resto -> `Page()` con error generico.
- **SIDE EFFECTS**: cookies auth `.ProyectoUnivalle.Auth.vUniversal` (sliding 8h), antisorgery
  `.ProyectoUnivalle.Antiforgery.vUniversal`. `HttpContext.SignOutAsync(ExternalScheme)` en OnGet
  limpia cookies externas.
- **SUCCESS MESSAGE**: indirecto via `LocalRedirect(returnUrl)`.
- **ERROR MESSAGES**: `Intento de inicio de sesion no valido.` (text generico).
- **REDIRECT/RESULT**: `LocalRedirect(returnUrl ?? Url.Content("~/"))`. 2FA ->
  `./LoginWith2fa`. Lockout -> `./Lockout`.
- **VISIBLE UI STATE**: split layout (`auth-wrapper` con fondo Unsplash + overlay navy), `auth-box`
  950x550, panel izquierdo `auth-left` (gradient azul oscuro + logo + brand "Univalle | Gestion
  de Gastronomia"), panel derecho `auth-right` con `Ingreso al Sistema`, label `Usuario o Correo`
  (`ti-user`), `Contrasena` (`ti-lock`), checkbox `Recordarme en este equipo`, boton INICIAR SESION
  (`btn-login-glow`).
- **SOURCE FILES**: `Pages/Login.cshtml`, `Pages/Login.cshtml.cs`, `Program.cs` (Identity +
  cooke config), `Pages/Shared/_Layout.cshtml`.

### FLOW NAME: USERS INDEX (Cuentas de Acceso) + TABS

- **ENTRY POINT**: `GET /Users/Index?pageIndex=&SearchTerm=&StatusFilter=`.
- **AUTHORIZED ROLES**: `[Authorize(Roles = AuthorizationHelper.AdminRoles)]` =
  `"Administrator,SuperAdmin"`.
- **INPUT**: `SearchTerm` (texto), `StatusFilter` (enum `GeneralStatus?`), `PageIndex` (int?).
- **NORMALIZATION**: `term = SearchTerm.Trim().ToLower()` en code-behind.
- **VALIDATIONS**: `[BindProperty(SupportsGet = true)]` para captura de querystring.
- **DATABASE READS**: `_context.Users.Include(CreatedBy, ModifiedBy).AsNoTracking()`;
  `Where(FirstName/LastName/UserName/IdentityCard Contains term)`;
  `Where(Status == StatusFilter)` o si no `Status != Eliminado`;
  `OrderBy(LastName).ThenBy(FirstName)`;
  `PaginatedList.CreateAsync(..., pageIndex ?? 1, 20)`. Igual para `Persons` con `OrderByDescending
  (Id)`.
- **DATABASE WRITES**: ninguno.
- **IDENTITY OPERATIONS**: ninguno.
- **SIDE EFFECTS**: ninguno.
- **SUCCESS MESSAGE**: `hasActiveFilters` flag en UI, sin TempData.
- **ERROR MESSAGES**: ninguno (solo render vacio en data).
- **REDIRECT/RESULT**: `Page()`.
- **VISIBLE UI STATE**: card `.card.shadow-sm.border-0`; `ul.nav.nav-tabs.customtab` (Cuentas de
  Acceso + Directorio de Personal); en tab Cuentas: cabecera "Control de Usuarios", boton
  "Vincular Usuario", zona `bg-light p-3 mb-4 rounded border-left border-info` con buscador
  (`fa-search`) + select estado (`btn-rounded w-auto mw-100`, default excluye `Eliminado`) +
  "Limpiar filtros" + contador `X cuenta(s)` (aria-live polite); tabla 6 columnas (Identidad /
  Usuario, Seguridad / Rol, Estatus, Perfil Laboral, Registro, Acciones); paginacion
  `<nav><ul class="pagination justify-content-center">Anterior / 1..N / Siguiente</ul></nav>`
  cuando `TotalPages > 1`; empty state con `fa-user-slash fa-3x opacity-2`; en tab Personas:
  cabecera "Directorio Base de Identidades", boton "Nueva Identidad", partial
  `<partial name="_PersonsTable">`, vacio con dashed border y `fa-id-badge fa-4x`.
- **SOURCE FILES**: `Pages/Users/Index.cshtml`, `Pages/Users/Index.cshtml.cs`,
  `Pages/Shared/_PersonsTable.cshtml`.

### FLOW NAME: USERS CREATE

- **ENTRY POINT**: `GET /Users/Create` o `POST /Users/Create`.
- **AUTHORIZED ROLES**: `[Authorize(Roles = AdminRoles)]`.
- **INPUT (InputModel)**: FirstName, LastName, SecondLastName?, IdentityCard, Email, PhoneNumber,
  Role (`UserRole`), Position?, Department?, HireDate?, Password, ProfilePictureUpload?
  (`IFormFile`, `accept="image/*"`), UserName.
- **NORMALIZATION**: `IdentityCard.Trim().ToUpperInvariant()`, `Email.Trim().ToLowerInvariant()`,
  `UserName.Trim().ToLowerInvariant()`, `string.Clean()` aplicado a nombres (extension no leida
  en F0).
- **VALIDATIONS**: data annotations (ver `00-SOURCE-SPEC.md` seccion 3.3); `Enum.IsDefined(
  UserRole)`; veto SuperAdmin si solicitante no; `SafeImageUpload.ValidateAsync`; unicidad pre-
  escritura (CI + Email + UserName). Display name `Display(Name=)` por campo.
- **DATABASE READS** (pre-check): `_context.Users.IgnoreQueryFilters().AnyAsync(... u.IdentityCard
  .Trim() == normalizedCI && u.Status != Eliminado)` para CI; igual para email y username.
- **DATABASE WRITES**: transaccion `BeginTransactionAsync` -> `_userManager.CreateAsync(user,
  Input.Password)` -> `_userManager.SynchronizeManagedRoleAsync(user, Input.Role)` ->
  `CommitAsync`. Si fallo: rollback + `SafeImageUpload.DeleteIfExists(uploadedFilePath)` + recargar
  `LoadRoles()`.
- **IDENTITY OPERATIONS**: `UserManager.CreateAsync`, `UserManager.SynchronizeManagedRoleAsync`
  (helper que limpia roles Identity manejados y agrega el rol objetivo), `FindByNameAsync`/
  `FindByEmailAsync` indirectamente via checks.
- **SIDE EFFECTS**: subida de imagen (`SafeImageUpload.SaveAsync` a
  `WebRootPath/uploads/users/{guid:N}{ext}`); rollback del archivo si la transaccion falla; log
  estructurado en catch generico.
- **SUCCESS MESSAGE**: `TempData.Success($"Cuenta de usuario para '{user.FullName}' creada
  exitosamente.")`.
- **ERROR MESSAGES**:
  - Identity: errores de `_userManager.CreateAsync` propagados a ModelState (`AddIdentityErrors`).
  - UniDocs: "Este documento de identidad ya esta vinculado a otra cuenta.",
  - UniEmail: "Este correo electronico ya se encuentra registrado.",
  - UniUser: "El nombre de usuario ya esta en uso."
  - Imagen: "La imagen debe ser JPG, PNG o WEBP.", "La imagen no puede superar los 5 MB.", "El
    contenido del archivo no corresponde a una imagen valida."
  - Catch generico: "No se pudo crear la cuenta. Revise los datos e intente nuevamente."
- **REDIRECT/RESULT**: exito -> `RedirectToPage("./Index")`. Fallo -> `Page()` con `LoadRoles()` y
  errores.
- **VISIBLE UI STATE**: card `col-lg-10 col-xl-9`, dos columnas (`col-md-6` con `border-right`),
  header "Alta de Cuenta de Usuario"; columna 1 "Identidad del Usuario" (Nombres, A. Paterno,
  A. Materno, C.I., Correo Institucional, Telefono, Foto de Perfil); columna 2 "Seguridad y
  Privilegios" (Rol de Aplicacion en `border-info`, Nombre de Usuario en `border-warning`,
  Contrasena con botones ver/generar, Cargo, Departamento); pie `Crear Cuenta` (`btn btn-info`)
  y `Cancelar`.
- **SOURCE FILES**: `Pages/Users/Create.cshtml`, `Pages/Users/Create.cshtml.cs`,
  `Helpers/SafeImageUpload.cs`, `Helpers/IdentityRoleExtensions.cs`,
  `Helpers/EnumHelper.cs`, `Helpers/TempDataExtensions.cs`, `Helpers/StringExtensions.cs` (no
  leido en F0), `Data/ApplicationDbContext.cs`.

### FLOW NAME: USERS EDIT

- **ENTRY POINT**: `GET /Users/Edit/{id}?returnUrl=...` o `POST /Users/Edit/{id}`.
- **AUTHORIZED ROLES**: `[Authorize(Roles = AdminRoles)]`; veto SuperAdmin ajeno.
- **INPUT (UserInputModel)**: FirstName, LastName, SecondLastName?, IdentityCard, Email,
  PhoneNumber?, Role, Status, Position?, Department?, HireDate?, UserName (readonly),
  NewPassword? (opcional), ProfilePictureUpload?, ExistingProfilePictureUrl? (campo del modelo).
- **NORMALIZATION**: `Email.Trim().ToLowerInvariant()`, `IdentityCard.Trim().ToUpperInvariant()`,
  `user.NormalizedEmail = _userManager.NormalizeEmail(...)` aplicado en `ApplyInput`.
- **VALIDATIONS**:
  - `Enum.IsDefined` para Role y Status.
  - veto SuperAdmin para no-SuperAdmin.
  - `SafeImageUpload.ValidateAsync` (si archivo nuevo).
  - unicidad CI/email excluyendo self y excluyendo `Status == Eliminado` para CI.
  - veto self-edit (no cambiar rol, no desactivar cuenta propia).
  - veto unico SuperAdmin activo (no permitir deshabilitar/rebajar al unico SuperAdmin).
- **DATABASE READS**: `AsNoTracking().FirstOrDefaultAsync(id)` (carga inicial);
  `_context.Users.IgnoreQueryFilters().AnyAsync(... user.Id != id && user.IdentityCard ==
  normalizedIdentityCard && u.Status != Eliminado)` (CI); igual para email;
  `IgnoreQueryFilters().AsTracking().FirstOrDefaultAsync(id)` (update).
- **DATABASE WRITES**: transaccion `BeginTransactionAsync` -> `ApplyInput` (actualiza campos,
  `NormalizedEmail`) -> `SynchronizeManagedRoleAsync` -> opcional `GeneratePasswordResetTokenAsync
  + ResetPasswordAsync` -> `Status != Activo` -> `SetLockoutEndDateAsync(MaxValue)`, `Status ==
  Activo && previousStatus != Activo` -> `SetLockoutEndDateAsync(null) +
  ResetAccessFailedCountAsync` -> `UpdateSecurityStampAsync` -> `LastModifiedDate = UtcNow,
  ModifiedById = currentUser?.Id` -> `SaveChangesAsync()` -> `CommitAsync()`. Tras commit exitoso,
  eliminar foto anterior (`SafeImageUpload.DeleteStoredFile`).
- **IDENTITY OPERATIONS**: `UserManager.SynchronizeManagedRoleAsync`, `GeneratePasswordResetToken
  Async`, `ResetPasswordAsync`, `SetLockoutEndDateAsync`, `ResetAccessFailedCountAsync`,
  `UpdateSecurityStampAsync`, `SignInManager.RefreshSignInAsync` (si self).
- **SIDE EFFECTS**: subida de foto; `SignInManager.RefreshSignInAsync` si se edita a si mismo;
  log estructurado en catch.
- **SUCCESS MESSAGE**: `TempData.Success($"Datos de la cuenta '{userToUpdate.FullName}'
  actualizados correctamente.")`.
- **ERROR MESSAGES**:
  - "Seleccione un rol valido.", "Seleccione un estado valido.",
  - "Solo un superadministrador puede asignar este rol.",
  - "Este C.I. ya esta asignado a otro usuario.",
  - "Este correo electronico ya se encuentra registrado.",
  - "No puede cambiar su propio rol ni desactivar su propia cuenta.",
  - "Debe existir al menos un superadministrador activo.",
  - mensajes de Identity por `AddIdentityErrors`.
  - catch: `TempData.Error("No se pudieron guardar los datos del usuario. Revise la informacion
    e intente nuevamente.")` y `Input.ExistingProfilePictureUrl = oldProfilePictureUrl` (rollback
    visual).
  - imagen: "La imagen debe ser JPG, PNG o WEBP.", "La imagen no puede superar los 5 MB.", "El
    contenido del archivo no corresponde a una imagen valida."
- **REDIRECT/RESULT**:
  - exito: `returnUrl == "Details"` -> `RedirectToPage("./Details", new { id })`; si no
    `RedirectToPage("./Index")`.
  - self-edit + RefreshSignInAsync antes del redirect.
  - fallo: `Page()` con `LoadRoles()`.
- **VISIBLE UI STATE**: `col-lg-8` formulario asim (2 cols) + `col-lg-4` sidebar oscuro "Estatus
  de Cuenta". Bloques: Identidad y Contacto (Nombres, A. Paterno, A. Materno, C.I. en
  `border-info`, Correo Institucional, Telefono, Foto de Perfil con preview), Privilegios de
  Acceso (Rol en `border-warning`, Gestion de Credenciales con UserName readonly y NewPassword
  opcional, Cargo, Departamento). Botones `Guardar Cambios` (`btn-warning`) y `Cancelar`.
  Sidebar `bg-dark text-white`: badges de estatus con `fa-power-off`, lista "Ingreso al Sistema:
  Habilitado", "Registro Auditado: dd/MM/yyyy", aviso amarillo "Al modificar el Rol de
  Aplicacion...".
- **SOURCE FILES**: `Pages/Users/Edit.cshtml`, `Pages/Users/Edit.cshtml.cs`,
  `Helpers/SafeImageUpload.cs`, `Helpers/IdentityRoleExtensions.cs`,
  `Helpers/TempDataExtensions.cs`.

### FLOW NAME: USERS DETAILS

- **ENTRY POINT**: `GET /Users/Details/{id}`.
- **AUTHORIZED ROLES**: `[Authorize(Roles = AdminRoles)]`.
- **INPUT**: `id` (int).
- **NORMALIZATION**: helper `ValueOrFallback(string?, fallback)` en el `.cshtml`.
- **VALIDATIONS**: `id == null` -> redirect `/Error`; `user == null` -> redirect `/Error`.
- **DATABASE READS**: `_context.Users.IgnoreQueryFilters().AsNoTracking().Include(CreatedBy,
  ModifiedBy).FirstOrDefaultAsync(m => m.Id == id)`.
- **DATABASE WRITES**: ninguno.
- **IDENTITY OPERATIONS**: ninguno.
- **SIDE EFFECTS**: ninguno (solo lectura). `ProfilePictureVersion = (LastModifiedDate ??
  CreatedDate).Ticks.ToString()` para cache-busting.
- **SUCCESS MESSAGE**: ninguno.
- **ERROR MESSAGES**: via `/Error` con parametros `module/entityId/message/returnUrl/listUrl`.
- **REDIRECT/RESULT**: `Page()` con `User` y `ProfilePictureVersion` poblados, o `RedirectToPage(
  "/Error", ...)`.
- **VISIBLE UI STATE**: `col-lg-8` ficha con avatar 150px (img circular o iniciales grandes en
  user-detail-initials), nombre + cargo + badges `badge-info` (rol) + `badge-pill` (status);
  bloques Identidad (Nombres, A. Paterno, A. Materno, C.I., Usuario/login como @UserName, Fecha
  de alta), Contacto y perfil laboral (Correo, Telefono, Cargo, Departamento); bloque Auditoria
  (Creado por + fecha, Ultima modificacion + fecha). `col-lg-4`: sidebar `bg-dark text-white`
  con "Seguridad de acceso" (Rol, Estado, Foto); card acciones (Modificar Perfil, Volver al
  Listado, Revocar Acceso).
- **SOURCE FILES**: `Pages/Users/Details.cshtml`, `Pages/Users/Details.cshtml.cs`.

### FLOW NAME: USERS DELETE / REVOKE

- **ENTRY POINT**: `GET /Users/Delete/{id}` o `POST /Users/Delete/{id}`.
- **AUTHORIZED ROLES**: `[Authorize(Roles = AdminRoles)]`.
- **INPUT**: `id` (int).
- **NORMALIZATION**: n/a (la confirmacion es un form con `<input type="hidden" asp-for="AppUser.Id"
  >`).
- **VALIDATIONS**:
  - veto self-delete: `currentUser?.Id == user.Id` -> TempData Error + redirect Index.
  - veto SuperAdmin ajeno: `Forbid()`.
  - veto unico SuperAdmin activo.
  - idempotencia: si `user.Status == Eliminado`, TempData Warning + redirect Index.
- **DATABASE READS**: GET inicial `_context.Users.Include(CreatedBy, ModifiedBy)
  .FirstOrDefaultAsync(u => u.Id == id)`; POST `_context.Users.FindAsync(id)` (tracking).
- **DATABASE WRITES**: `user.Status = Eliminado; LastModifiedDate = UtcNow; ModifiedById =
  currentUser?.Id`; transaccion -> `SetLockoutEndDateAsync(MaxValue)` ->
  `UpdateSecurityStampAsync` -> `SaveChangesAsync + CommitAsync`.
- **IDENTITY OPERATIONS**: `SetLockoutEndDateAsync(MaxValue)`, `UpdateSecurityStampAsync`.
- **SIDE EFFECTS**: cookies invalidadas al rotar stamp.
- **SUCCESS MESSAGE**: `TempData.Success("El acceso para '{FullName}' ha sido revocado
  correctamente.")`.
- **ERROR MESSAGES**:
  - 404 si id nulo o user no encontrado.
  - 403 (Forbid) SuperAdmin ajeno.
  - `TempData.Error("No puede dar de baja su propia cuenta.")`.
  - `TempData.Error("Debe existir al menos un superadministrador activo.")`.
  - `TempData.Warning("El usuario '{FullName}' ya se encuentra dado de baja.")`.
  - `TempData.Error("No se pudo revocar el acceso del usuario.")`.
  - `TempData.Error("No se pudo invalidar la sesion del usuario.")`.
  - `TempData.Error("No se pudo procesar la baja del usuario. Intente nuevamente.")`.
- **REDIRECT/RESULT**:
  - exito: `RedirectToPage("./Index")`.
  - self: `RedirectToPage("./Index")`.
  - IdentityError/IException: `RedirectToPage("./Delete", new { id })` para reintento.
- **VISIBLE UI STATE**: card `col-md-6 col-lg-5 text-center shadow-sm border-0` con padding `p-5`,
  icono `fas fa-user-times display-3 text-danger`, h3 `¿Revocar Acceso al Sistema?`, parrafo con
  FullName y `@@UserName`, mini-card `bg-light p-3 rounded` con Rol Actual / Area Dpto / CI,
  botones `Ver Expediente` (`btn-outline-secondary btn-rounded`) y `Sí, Confirmar Baja`
  (`btn-danger btn-rounded`), y al pie `Volver al Control de Usuarios`. Sugerencia tecnica externa
  en cursiva "Se recomienda Inactivar la cuenta desde el editor si el usuario ha realizado
  registros criticos recientes."
- **SOURCE FILES**: `Pages/Users/Delete.cshtml`, `Pages/Users/Delete.cshtml.cs`.

### FLOW NAME: PERSONS DIRECTORY (reusada via partial `_PersonsTable` y `/Persons/Index`)

- **ENTRY POINT**: `/Users/Index` (tab "Directorio de Personal") o `/Persons/Index` (pagina
  propia).
- **AUTHORIZED ROLES**: ambas paginas con `[Authorize(Roles = AdminRoles)]`.
- **INPUT**: filtros compartidos con tab Usuarios (SearchTerm + StatusFilter + PageIndex).
- **NORMALIZATION**: `term = SearchTerm.Trim().ToLower()`; `personQuery.Where(p => p.Email
  .Contains(term) || p.Id.ToString() == term)`.
- **VALIDATIONS**: mismas reglas de pagina (data annotations y modelo).
- **DATABASE READS**: `_context.People.Include(CreatedBy, ModifiedBy).AsNoTracking()`;
  `OrderByDescending(p => p.Id)`; `PaginatedList<Person>.CreateAsync(..., 20)`.
- **DATABASE WRITES**: ninguno en el Index.
- **IDENTITY OPERATIONS**: ninguno.
- **SIDE EFFECTS**: ninguno.
- **SUCCESS MESSAGE**: ninguno.
- **ERROR MESSAGES**: ninguno en Index.
- **REDIRECT/RESULT**: `Page()`.
- **VISIBLE UI STATE**: cabecera "Directorio Base de Identidades" con boton "Nueva Identidad" ->
  `/Persons/Create`; partial `_PersonsTable.cshtml` con tabla 5 columnas (Nombre Completo, Tipo,
  Contacto, Estatus, Acciones); badges Intern/Extern con coloracion distinta; acciones
  Editar/Detalles/Eliminar a `/Persons/*`; empty state dashed border con `fa-id-badge fa-4x`;
  paginacion `Anterior / 1..N / Siguiente` solo si `TotalPages > 1`.
- **SOURCE FILES**: `Pages/Users/Index.cshtml`, `Pages/Users/Index.cshtml.cs`,
  `Pages/Shared/_PersonsTable.cshtml`, `Pages/Persons/Index.cshtml`.

### FLOW NAME: PERSONS CREATE / EDIT / DETAILS / DELETE (REUSE BOUNDARY)

- **ENTRY POINTS**: `/Persons/Create`, `/Persons/Edit/{id}`, `/Persons/Details/{id}`,
  `/Persons/Delete/{id}`; todos `Administrator,SuperAdmin` en ASP.
- **INPUT / NORMALIZATION / VALIDATIONS**: Intern/Extern, nombre, contacto, categoria, direccion
  obligatoria para externos y estados base/de subtipo; `Clean()` recorta y colapsa espacios.
- **DATABASE READS/WRITES**: TPT `People` + `Interns`/`Externs`; alta crea subtipo; edit conserva
  subtipo; details carga auditoria e historiales; delete fija `Person.Status=Eliminado` sin tocar
  `InternStatus`/`ExternStatus`.
- **IDENTITY / SIDE EFFECTS**: sin operaciones Identity ni security stamp; mensajes TempData y
  redirects al Index. Details es solo lectura.
- **VISIBLE UI STATE**: tabs Interno/Externo en Create, formulario condicional en Edit, ficha con
  historiales en Details y card de confirmacion directa en Delete.
- **SOURCE FILES**: `Pages/Persons/{Create,Edit,Details,Delete}.cshtml` y `.cshtml.cs`,
  `Models/{Person,Intern,Extern}.cs`, `Helpers/{StringExtensions,PaginatedList}.cs`.
- **SCOPE**: MIG-001 reutiliza estas rutas/capacidades al presentar Persons dentro de Users Index;
  no crea un segundo backend ni duplica `lu_person`.

### FLOW NAME: SHELL GLOBAL (topbar, sidebar, sidebar user dropdown, notifications, preloader)

- **ENTRY POINT**: cualquier pagina autenticada (`Pages/Shared/_Layout.cshtml`).
- **AUTHORIZED ROLES**: `app.UseAuthorization()`; `[Authorize]` por pagina; `FallbackPolicy =
  RequireAuthenticatedUser`.
- **INPUT**: cookie auth, antiforgery token, notificacion opcional, búsqueda decorativa.
- **NORMALIZATION**: `currentPage` se computa de `ViewContext.RouteData.Values["page"]`; `showBool`
  filtra paginas (Dashboard/Managements/Acquisitions/Requests/Maintenances/Departures).
- **VALIDATIONS**: `[Authorize]`, `AllowAnonymousToPage("/Login")`, antiforgery auto-inyectado en
  meta tag.
- **DATABASE READS**: ViewComponent `UserProfile` consulta `_context.Users.AsNoTracking()
  .FirstOrDefaultAsync(u => u.Id == userId.Value)` con `Include` no necesarios.
- **DATABASE WRITES**: ninguno.
- **IDENTITY OPERATIONS**: ninguno (solo lectura).
- **SIDE EFFECTS**: emite CSRF token via `IAntiforgery.GetAndStoreTokens(Context).RequestToken`;
  inyecta `<meta name="request-verification-token">`.
- **SUCCESS MESSAGE**: indirecto via TempData.
- **ERROR MESSAGES**: indirecto via TempData.
- **REDIRECT/RESULT**: layout normal.
- **VISIBLE UI STATE**: preloader `lds-ripple`; topbar con `logo-icon.png`, `LABORATORIOS`,
  busqueda (`mdi-magnify`), campana condicional (`mdi-bell`), badge notificaciones;
  `UserProfile` ViewComponent con avatar circular (`<img src="~/uploads/users/{file}?v=Ticks">`
  o iniciales), menu dropdown con `Mi Perfil` (link `/Users/Details/{userId}`) y `Cerrar Sesion`
  (form POST `/Logout`). Sidebar `_Sidebar.cshtml` con secciones Inicio, Elementos, Equipos y
  Actividades, Personas; dentro de Personas grupo "Usuarios" -> `/Users/Index`. Container
  `page-wrapper` con breadcrumb `<h4 class="page-title">@ViewData["Title"]</h4>`.
- **SOURCE FILES**: `Pages/Shared/_Layout.cshtml`, `Pages/Shared/_Sidebar.cshtml`,
  `Pages/Shared/Components/UserProfile/Default.cshtml`,
  `Pages/Shared/Components/UserProfileViewComponent.cs`.

## VISUAL_STATES_DISCOVERED

(Para detalles ver `00-SOURCE-SPEC.md` seccion 5.)

- INDEX-USERS-TAB: card con tabs, `bg-light` filtros, buscador, select estado, tabla 6 cols,
  paginacion 20/pag, empty state `fa-user-slash fa-3x`.
- INDEX-PERSONS-TAB: misma card, segundo tab, partial `_PersonsTable` con badges Intern/Extern,
  empty dashed `fa-id-badge fa-4x`.
- CREATE: card unica `col-lg-10 col-xl-9`, 2 columnas (`col-md-6` con `border-right`), `Crear
  Cuenta` `btn-info`, `Cancelar` outline.
- EDIT: card `col-lg-8` form + sidebar `col-lg-4` oscuro con Estatus/Seguridad/Acciones, `Guardar
  Cambios` `btn-warning`, panel Estatus `fa-power-off`.
- DETAILS: card `col-lg-8` ficha + sidebar `col-lg-4` con "Seguridad de acceso" (Rol/Estado/Foto)
  + card Acciones; avatar 150px circular, badges `badge-pill`, Auditoria al pie.
- DELETE: card `col-md-6 col-lg-5 text-center`, icono `fa-user-times display-3`, botones
  `Ver Expediente` + `Sí, Confirmar Baja` (`btn-danger`), sugerencia tecnica externa.

## TARGET_CAPABILITIES_FOUND

### Apps web

- `AppsPage` y `App.tsx`:
  - Login form inline (`view === 'login'`) con campos email + password + rememberMe.
  - Workspace shell con header (`<header className="topbar">`), sidebar (`<aside
    className="left-sidebar">`), breadcrumb (`<nav aria-label="Migas de pan">`), perfil dropdown,
    site-switcher dropdown, notificaciones dropdown.
  - SitePicker (`view === 'site-picker'`) cuando `session.activeSiteId === null` y hay
    membresias.
  - Loading stage con `.lds-ripple`.
  - Unavailable stage si falla API.
  - Navigator unico por ruta (`NAVIGATION`) con secciones Inicio/Elementos/Equipos y Actividades/
    Personas, reglas `permission: 'authenticated' | 'admin'`.
  - Render por ruta: `/Users/Index` -> `UsersPanel`; `/Users/Details` -> `ProfilePanel`;
    `/Persons/Index` -> `PeoplePanel`; resto segun contrato.
  - AuthApi expone: session, login, setActiveSite, siteContext, logout, dashboard?,
    notifications?, markNotificationRead?, markAllNotificationsRead?, countries?, cities?,
    faculties?, careers?, laboratories?, equipment?, equipmentUnits?, managements?,
    verifications?, requests?, maintenances?, departures?, kardex?, acquisitions?, people?,
    users?, profile?, updateProfile?, reportManifest?, downloadReport?.
- `UsersPanel.tsx`: lista, alta, edicion (consolidada), disable, profile subpanel `Mi Perfil`.
- `PeoplePanel.tsx`: lista, alta, edicion (con status select), baja. Filters `type` y
  `statusFilter`. Validacion: `extern + address vacio` -> error inline.
- `App.test.tsx`: 6 tests cubre login, site-picker, shell navigation, role-based hiding, logout.

### Apps API

- `auth.controller.ts`: `csrf`, `login`, `session`, `session/active-site`, `logout` (con
  preflight `OPTIONS *`).
- `auth.service.ts` + `auth.repository.ts`: bcrypt (costo configurable), constant time compare,
  rate-limit (`lu_auth_rate_limit`), `security_version`, `session` cookie + CSRF, session tokens
  hasheados, identificador anonimizado (subjectHash/ipHash) en `lu_security_event`.
- `users/controller.ts` + `users/service.ts` + `users/repository.ts`: `list/find/create/update/
  disable/profile`; managed records exponen timestamps, no actores de auditoria. El update de
  perfil propio no incrementa `security_version`.
- `people/controller.ts`: `list`, `create`, `update`, `delete` (soft-delete -> status=2). El service
  permite solo Administrador por sede, no SuperAdmin global; el filtro status=2 es contradictorio.
- `core.guard.ts`: `ApiCorrelationGuard`, `SiteContextGuard` (resuelve `SiteRequestContext`).
- `core.module.ts`: provee `TenantPgPoolRegistry`, `TenantRouter`, `TENANT_CONNECTION_RESOLVER`.

### Migraciones y tooling encontrados

- `0001_create_identity_control_plane.sql`: `lu_site`, `lu_user`, `lu_site_membership`, `lu_session`
  con `token_hash char(64)` (SHA-256 hex).
- `0002_create_auth_security_controls.sql`: `lu_auth_rate_limit`, `lu_security_event`
  (verificado por imports/uso).
- `0003_create_tenant_route_catalog.sql`: `lu_site` extendido o catalogos (verificado por uso).
- `0004_academic_catalogs.sql`: `lu_faculty`, `lu_career`, `lu_site_career` + grants a
  `lu_auth_runtime`.
- `0005_user_management.sql`: GRANT SELECT, INSERT, UPDATE sobre `lu_user`, `lu_site_membership` a
  `lu_auth_runtime`; no agrega campos ni hace seed.
- `tenant/0001_dashboard_foundation.sql`..`tenant/0012_people.sql`: el modulo Personas vive en
  `0012`.
- `MIGRATION_REGISTRY` pinnea unicamente 0001, 0002 y 0003. 0004, 0005 y tenant/0012 no estan
  registrados; `validatePinnedMigration` rechaza archivos no pinneados. No se afirma que los SQL
  no registrados esten aplicados.

### Packages

- `packages/contracts/src/users.ts`: `ManagedUserRecord`, `ManagedUserQuery`, `CreateManagedUser
  Input`, `UpdateManagedUserInput`, `ProfileRecord`, `UpdateProfileInput`, `USER_ROUTES`.
- `packages/contracts/src/site.ts`: `SiteId`, `SiteState`, `SiteRole`, `GlobalRole`,
  `SiteMembership`, `ActiveSiteSession`.
- `packages/contracts/src/auth.ts`: `AUTH_ROUTES`, `CsrfResponse`, `LoginRequest`,
  `SetActiveSiteRequest`, `AuthSessionResponse`.
- `packages/contracts/src/people.ts`: `PersonType`, `PersonStatus`, `PersonCategory`,
  `PersonRecord`, `PersonQuery`, `CreatePersonInput`, `UpdatePersonInput`, `PERSON_ROUTES`.
- `packages/api-client/src/index.ts`: clase `ApiClient` con metodos `csrf`, `login`, `session`,
  `setActiveSite`, `logout`, `siteContext`, `dashboard`, `notifications`, `mark*`, `countries`,
  `cities`, `faculties`, `careers`, `siteCareers`, `assign/unassignSiteCareer`, `laboratories`,
  `equipment`, `equipmentUnits`, `managements`, `verifications`, `saveMassVerifications`,
  `requests`, `maintenances`, `departures`, `createMassDepartures`, `kardex`, `acquisitions`,
  `people`, `users`, `profile`, `updateProfile`, `reportManifest`, `downloadReport`.

## MAJOR_GAPS

1. Login legacy acepta username o email; target solo email y no define migracion de identificadores.
2. Hashes legacy PBKDF2/Identity no tienen estrategia de compatibilidad/reset frente a bcrypt.
3. Faltan CI, telefono, foto, nombres separados, cargo, departamento, fecha de ingreso y actores de
   auditoria; `Eliminado` se colapsa en `disabled`.
4. `/Users/Details` cambio de ficha administrativa completa a Mi Perfil limitado.
5. SuperAdmin global puede autenticarse sin sede, pero `SiteContextGuard` bloquea el workspace;
   People ademas no reconoce GlobalRole.SuperAdmin.
6. `updateProfile` cambia email sin rotar `security_version`; Create/Edit y Login discrepan en el
   limite Unicode/72 bytes de password.
7. El target bloquea toda edicion de SuperAdmin en vez de proteger solo al ultimo SuperAdmin activo.
8. People filtra siempre `status<>2`, por lo que el filtro visible Baja (`2`) devuelve cero.
9. `0005_user_management.sql` solo concede grants y no modela campos legacy.
10. `MIGRATION_REGISTRY` solo registra 0001-0003; 0004/0005/tenant-0012 no son aceptados por el
    `migration-cli` actual.

## LEGACY_CONTRADICTIONS

(Detalle y evidencia A/B en `00-SOURCE-SPEC.md`.)

- LC-01: reglas documentales exigen SweetAlert, pero Users/Persons ASP hacen POST directo; target
  usa `window.confirm` solo en bajas. Conducta observable cerrada, no `UNKNOWN`.
- LC-02: login username/email vs email-only.
- LC-03: CI desaparece.
- LC-04: `Eliminado` vs `disabled`.
- LC-05: foto/storage ausente.
- LC-06: selectores panel especificos sin CSS.
- LC-07: timestamps parciales, sin actores ni UI consistente.
- LC-08: Details admin colapsado a Mi Perfil.
- LC-09: telefono de User ausente.
- LC-10: smart-index/debounce/URL no portado.
- LC-11: SuperAdmin sin sede autenticado pero bloqueado por `SiteContextGuard`.
- LC-12: People rechaza SuperAdmin global mientras Users lo acepta.
- LC-13: `updateProfile` no incrementa `security_version`.
- LC-14: 72 unidades UTF-16 en Users vs 72 bytes UTF-8 en Login/bcrypt.
- LC-15: filtro Persons `status<>2 AND status=2`.
- LC-16: DataAnnotations/help de password legacy no coinciden con politica Identity de `Program.cs`.

## ARCHITECTURAL_MAPPING_REQUIRED

Asuntos que F1 debe mapear antes de cualquier implementacion:

1. **Identidad legacy -> `lu_user` + `lu_site_membership`**: confirmar el mapeo 1:1, decidir
   tratamiento de `UserName`, `IdentityCard`, `PhoneNumber`, `Photo`, `Position/Department/HireDate`
   y auditoria completa.
2. **Roles**: ASP `UserRole(Administrador=1, Supervisor=2, SuperAdmin=99)` ->
   `SiteRole.Administrador/Supervisor + lu_user.is_super_admin`. SuperAdmin global fuera del
   `UserController`.
3. **Estado**: ASP `GeneralStatus` (3 valores) -> target `active/disabled` + log/snapshot de
   `Eliminado`. Decidir almacenamiento append-only.
4. **Sesion/password**: PBKDF2 legacy, bcrypt target, TTL, rotacion de `security_version` y regla de
   longitud en bytes.
5. **Personas**: TPT `People`/`Interns`/`Externs` -> `lu_person` plano; resolver estados de subtipo,
   `RowVersion` e `ImportBatchId` sin duplicar el modulo People.
6. **Multi-sede**: confirmado por `docs/CONTRATO_MULTISEDE.md`. `activeSiteId` se elige en login
   o `PUT /auth/session/active-site`.
7. **Storage de fotos**: sin definir.
8. **`PhoneNumber`**: destino (reusar `lu_person`? agregar a `lu_user`?).
9. **`SafeImageUpload`**: portar como helper en `apps/api` o mantener solo en ASP.
10. **Notificaciones en topbar**: condicion legacy vs siempre en workspace.
11. **SuperAdmin y sede**: contrato de acceso global-a-tenant y RBAC coherente Users/People.
12. **Migration registry**: alcance de manifests/registro para control-plane 0004/0005 y tenant
    0012; la ausencia actual esta confirmada.

## DECISIONS_ALREADY_FIXED_BY_REPO

Decisiones arquitectonicas vigentes al cierre de F0 (no se reabren):

1. **Stack fijo**: React/Vite/TS + NestJS/Fastify + PostgreSQL; sustituye Razor/EF/SQL Server.
   Fuente: `docs/PLAN_MIGRACION_REACT_NESTJS.md` seccion 3 + decision 6.
2. **Identidad local propia** en NestJS; sin SSO institucional; Keycloak diferido. Sin JWT en
   `localStorage`. Fuente: decision 6.
3. **Multi-sede**: control plane central + tenant DB por sede; routing server-side; transacciones
   solo dentro de una DB. Fuente: `docs/CONTRATO_MULTISEDE.md` + decision 12.
4. **Reportes**: EPPlus/QuestPDF .NET durante transicion; reports Node al final (sin .NET).
5. **`GastroExample`**: sandbox descartable, sin autoridad historica.
6. **`ManagementPlan` mantiene unicidad `ManagementId + EquipmentUnitId`** (resolucion DATA-002).
7. **`EquipmentUnit` no tiene `SiteId` propio**: sede por `LaboratoryId`.
8. **`Management.FacultyId` opcional durante transicion**.
9. **Sin doble escritura**: escritor unico por capacidad/sede (`LEGACY_SQLSERVER`,
   `NEST_SQLSERVER`, `NEST_POSTGRES`).
10. **Sin offline inicial**.
11. **CSV/Excel**: controlado (`D:/proyectoSis/Excels/Plantilla_Original.xlsx` intocable).
12. **NoTracking global**: los update van con `.AsTracking()` o `FindAsync()`.
13. **`ManagementId` se preserva en wizard**.
14. **Migraciones existentes intocables** (0001..0005 control plane; 0001..0012 tenant).
15. **No commits sin aprobacion**.
16. **Bajo consumo**: `dotnet -m:1`, lotes <= 100, sin servidores persistentes, abortar bajo 2 GB
    libres.
17. **Estado ejecutable del tooling**: `migration-cli` acepta solo las entradas pinneadas 0001-0003.
    Esto es evidencia actual, no una decision de que deba permanecer asi; F1/F2 deben resolverlo.

## UNKNOWN

No quedan lecturas de codigo Users/Persons detectadas pendientes. Unknowns de decision:

1. Runner/manifiesto aprobado para SQL existentes fuera de `MIGRATION_REGISTRY`, especialmente
   tenant/0012; la ausencia del registro si esta confirmada.
2. Destino y retencion de fotos legacy.
3. Estrategia PBKDF2->bcrypt.
4. Mapeo final de campos legacy, tercer estado, auditoria por actor y estados de subtipo Person.
5. Regla final de SuperAdmin global sin sede/membresia.

Resueltos en F0: cookie `__Host-lu_session`; defaults rate-limit; orden target People por
`name ASC,id ASC`; `LoginWith2fa`/`Lockout` no existen; `Clean()` y `PaginatedList` leidos; Users y
Persons no tienen confirmacion SweetAlert pre-submit.

## ARTIFACTS_CREATED

Estos son los unicos archivos creados en F0:

| Ruta | Tamano aprox. (bytes) | Estado |
|---|---|---|
| `docs/migration/users/00-SOURCE-SPEC.md` | (escrito) | creado |
| `docs/migration/users/00-PARITY-MATRIX.md` | (escrito) | creado |
| `docs/migration/users/00-MASTER-PLAN.md` | (escrito) | creado |
| `docs/migration/users/00-HANDOFF.md` | (este) | creado |

Ver `VALIDATION` mas abajo para confirmar `git status --short` con solo esos cuatro paths como
cambios propios.

## VALIDATION

- `git rev-parse HEAD` antes y despues de F0 = `7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`. **Sin
  commit** al cierre de F0.
- Branch: `migration/react` (sin cambios).
- `git diff --check` ejecutado: exit code 0, sin errores de whitespace. Git emitio avisos de
  conversion LF->CRLF para cambios preexistentes de configuracion; no son errores del diff.
- Como los cuatro docs son nuevos/no rastreados, se ejecuto ademas `git diff --no-index --check`
  contra `/dev/null` para cada uno: exit code 0, sin errores de whitespace.
- `git status --short` ejecutado. Working tree confirma:
  - `.opencode/agents/**` y `opencode.json` quedan modificados por cambios previos ajenos
    (registrados al inicio de F0; el worker no los toco ni revirtio).
  - `apps/**`, `packages/**`, `wwwroot/**`, `Pages/**`: sin cambios del worker.
  - `docs/migration/users/`: los cuatro docs creados.
- **Working tree status**: cambios previos ajenos ya estaban modificados/no rastreados en
  `.opencode/` y `opencode.json` antes de empezar F0; se preservaron.
- **Sin secretos persistidos**: en ninguno de los cuatro docs se pega connection strings ni
  valores de password ni tokens. Solo se incluye el lenguaje textual aprobado por la regla sobre
  credenciales: "local PostgreSQL credentials are already provisioned outside tracked files" (no
  usado en F0 pero disponible si F1 o posterior lo requiere). Busqueda exacta del valor prohibido
  indicado por el usuario: 0 coincidencias.
- **Sin comandos destructivos**: sin `git reset`, sin `git checkout` contra ninguna ref, sin
  mutaciones de BD ni servidores iniciados.
- **Bajo consumo**: sin builds, sin iniciar `.NET`, React o NestJS; cero procesos persistentes.
- **Sin mutaciones de base de datos**.
- **Confirmacion A-J**: 10/10 registrada con estado y evidencia en Source Spec seccion 9.
- **Conteo automatizado de matriz**: `PARITY=29 | PARTIAL=32 | MISSING=42 | CONFLICT=40 |
  NOT_APPLICABLE=14 | UNKNOWN=1`; total **158** filas; ningun estado fuera del vocabulario.
- **Estructura**: cuatro archivos existentes; 21 secciones exactas del handoff; seis pantallas
  visuales requeridas presentes.
- **Rutas**: validacion automatizada de 45 paths legacy mediante `git cat-file -e
  reference/asp-final:<path>` y 38 paths target mediante existencia local; 0 faltantes.
- **Revision independiente**: `critical-reviewer` revalido autenticacion/datos despues de las
  correcciones; veredicto apto para cierre F0, sin `MUST_FIX`, F1 listo solo para contrato.

## DO_NOT_REOPEN_IN_F1

Solo decisiones ya fijadas por el repositorio; los gaps de paridad **si** deben decidirse en F1:

1. Stack React/Vite/TypeScript + NestJS/Fastify + PostgreSQL.
2. Identidad local propia; sin JWT en `localStorage`; Keycloak/SSO diferido.
3. Control plane central + tenant DB por sede; routing server-side; transacciones no cruzan DB.
4. SuperAdmin global; Administrador/Supervisor por sede.
5. Migraciones existentes no se editan; cualquier cambio es aditivo y debe seguir el contrato de
   registry/manifiesto aprobado.
6. Fuentes institucionales externas permanecen inmutables; no se versionan secretos.
7. Sin commit sin aprobacion y bajo consumo operativo.

No estan congelados por F0: email-only, ausencia de CI/foto/telefono, dos estados, Mi Perfil en lugar
de Details admin, bloqueo total de SuperAdmin, RBAC People, eventos de `security_version`, politica
de password, storage, ni `window.confirm`. Esos puntos son entradas obligatorias de F1/F5.

## F1_REQUIRED_INPUTS

Para arrancar F1 (IDENTITY & DATA CONTRACT), F0 entrega:

1. `00-SOURCE-SPEC.md` secciones 6 (modelo legacy vs target) y 7 (auth/security).
2. `00-PARITY-MATRIX.md` bloques E (Create), F (Edit), M (data) y N (identity operations);
   revisar tambien I, J, K, L para Personas.
3. `00-MASTER-PLAN.md` seccion F1 con superficies, criterios de salida, archivos esperados y
   agentes.
4. Contratos target vigentes al cierre de F0:
   `packages/contracts/src/{users,auth,site,people}.ts`.
5. Migraciones target vigentes:
   `apps/api/migrations/0001_create_identity_control_plane.sql`,
   `apps/api/migrations/control-plane/0004_academic_catalogs.sql`,
   `apps/api/migrations/control-plane/0005_user_management.sql`,
   `apps/api/migrations/tenant/0012_people.sql`.
6. Tooling de migrations: `apps/api/src/database/{migration-registry,migration-runner,migration-cli}.ts`
   y `apps/api/test/migration-registry.e2e-spec.ts`.
7. Arquitectura multi-sede vigente: `docs/CONTRATO_MULTISEDE.md`.
8. Plan rector: `docs/PLAN_MIGRACION_REACT_NESTJS.md`.
9. Reglas del modulo: `Pages/AGENTS.md`, `Models/AGENTS.md`.

## F1_OBJECTIVE

Cerrar **el contrato de datos e identidad** del modulo User/Person sobre la arquitectura ya
fija (React + NestJS + PostgreSQL). Esto incluye, en orden:

1. Decidir mapeo 1:1 de campos legacy (`UserName`, `IdentityCard`, `PhoneNumber`, `Photo`,
   `Position`, `Department`, `HireDate`) a `lu_user` + `lu_site_membership` + `lu_person` o a
   tablas auxiliares.
2. Decidir representacion del estado `Eliminado` legacy (log/snapshot/flag) dado que el target
   usa `'active'/'disabled'` solamente.
3. Decidir `audit` visible en `ManagedUserRecord`/`ProfileRecord` para paridad con `Details`
   legacy.
4. Decidir `SuperAdmin` global: route opt-in (`auth-bootstrap-cli.ts`) sin exponer endpoints
   publicos.
5. Decidir foto de perfil: storage opt-in (sin desarrollar aun en F1).
6. Decidir migracion PBKDF2->bcrypt, politica en bytes y eventos de `security_version`.
7. Decidir el `phoneNumber` destino (reusar `lu_person` o agregar a `lu_user`).
8. Decidir acceso SuperAdmin sin sede, RBAC People y proteccion del ultimo SuperAdmin.
9. Decidir contrato de migrations/manifests para control-plane y tenant; **no escribir SQL en F1**.
10. Publicar `01-F1-CONTRACT.md` y `01-DECISIONS.md`; mantener diff/estado Git y secretos limpios.

> F1 no implementa UI, endpoints, contracts, API client ni SQL. PostgreSQL corresponde a F2;
> Nest/Auth a F3; contracts/client a F4; React a F5/F6.
