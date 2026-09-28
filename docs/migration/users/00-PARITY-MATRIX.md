# 00-PARITY-MATRIX.md — User/Person mig F0 parity matrix

> Contrato operativo: la matriz descompone cada capacidad, campo, comportamiento, mensaje,
> redirect, elemento visual, decision de seguridad, dato, migration o test en filas individuales
> para permitir conteo inequivoco de estados. Una sola fila tiene un solo `STATUS` final entre los
> admitidos:
>
> - `PARITY` (1:1 con legacy, sin gap)
> - `PARTIAL` (existe en ambos pero con diferencias menores o faltantes)
> - `MISSING` (existe en ASP y no existe en target)
> - `CONFLICT` (existe en ambos pero el modelo/resultado observable difiere)
> - `NOT_APPLICABLE` (no aplica en el target por cambio arquitectonico aprobado)
> - `UNKNOWN` (no resuelto en F0; requiere decision F1)
>
> Las filas marcadas `NOT_APPLICABLE` o `UNKNOWN` **deben** justificarse in situ. No se acepta
> "indeterminado" sin causa. Cada fila lleva `ASP SOURCE` con la ruta exacta bajo
> `reference/asp-final @ dccabf50330afc48760d06bd4dbaff8c37ebbd3f` y `TARGET CURRENT STATE` con
> la ruta exacta bajo `migration/react @ 7cb4ff5bafdd3cc7188aca7464c6dcbd9aa19480`.
>
> Las columnas `PHASE OWNER` y `NOTES` no son vinculantes pero enrutan la accion esperada.

---

## Bloque A — Login / Identidad

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| A-01 | Login | Login por username o email | `Pages/Login.cshtml.cs` (`FindByNameAsync(login) ?? FindByEmailAsync(login)`) | Login acepta `UserName` legacy como credencial | `apps/api/src/auth/auth.service.ts -> auth.repository.findUserWithMembershipsByEmail(email)`; solo email | email legacy puede no coincidir con `UserName`; perderia acceso | F2 / F3 / F4 / F5 | PARITY | F1-D004 congela namespace compartido; falta implementar; F5: login UI envia `loginIdentifier` (usuario o correo) (antes CONFLICT) |
| A-02 | Login | RememberMe persistente 8h + cookie sliding | `Program.cs` `ConfigureApplicationCookie(options.ExpireTimeSpan = TimeSpan.FromHours(8); SlidingExpiration = true)` | Cookie `.ProyectoUnivalle.Auth.vUniversal` mantiene sesion 8h sliding | cookie `__Host-lu_session`; default idle 30 min y absoluto 12 h; `rememberMe` agrega max-age absoluto | target no replica sliding 8 h | F3 (auth fastify) | CONFLICT | `auth.constants.ts`, `auth.config.ts`; F8: modelo de sesion aprobado (idle 30 min / absoluto 12 h, rememberMe absoluto) en lugar de cookie sliding 8 h; decision F8: se conserva. (antes PARTIAL) |
| A-03 | Login | Redirect residual a `LoginWith2fa` | `Pages/Login.cshtml.cs`; pagina destino ausente del arbol | una cuenta que requiriera 2FA caeria en una ruta inexistente | target no implementa 2FA | no hay pantalla legacy observable que calcar; decidir seguridad futura fuera de F0 | F1 | NOT_APPLICABLE | referencia muerta confirmada |
| A-04 | Login | Lockout con redirect a `Lockout` | `Pages/Login.cshtml.cs` (`RedirectToPage("./Lockout")`) y `Program.cs` (`MaxFailedAccessAttempts = 5; DefaultLockoutTimeSpan = 15min`) | Despues de 5 intentos, redirige a Lockout | `auth.rate-limit.ts` consume bucket `email_ip`/`ip` y emite `AuthRateLimitException`; `App.tsx` muestra "Demasiados intentos. Intente mas tarde." via 429 toast | sin pagina "Lockout" dedicada; mensaje inline | F3 | CONFLICT | tabla `lu_auth_rate_limit` mantiene buckets; F8: sin pagina Lockout; buckets email_ip/ip y 429 inline; decision F8: se conserva (misma familia que A-08). (antes PARTIAL) |
| A-05 | Login | CSRF token en meta | `Pages/Shared/_Layout.cshtml` (meta `request-verification-token`) | Formularios usan `__RequestVerificationToken` | `ApiClient.ensureCsrf` -> `GET /auth/csrf`; CSRF cookie `httpOnly`; `X-CSRF-Token` header en mutaciones | mecanismo de obtencion diferente (cookie vs meta) | F3 | PARITY | ambos exigen CSRF en mutaciones |
| A-06 | Login | Limpieza de `ExternalScheme` | `Pages/Login.cshtml.cs` OnGetAsync | Borra cookies externas antes de mostrar form | `App.tsx::App` limpia state (`setEmail('')`) al re-mount; sin `SignOutAsync(ExternalScheme)` | no se observo manejo explicito de external login | F3 | NOT_APPLICABLE | no hay login externo en el sistema actual |
| A-07 | Login | Mensaje "Intento de inicio de sesion no valido." | `Pages/Login.cshtml.cs` | Generico en fallo | `auth.exceptions.AuthUnauthorizedException`; frontend mapea 401 a "Usuario o contrasena incorrectos." | mensaje casi identico | F3 | PARTIAL | diferencia terminologica minima; F8: mensaje generico con otra redaccion ("Usuario o contrasena incorrectos."). |
| A-08 | Login | Lockout automatic Identity (`lockoutOnFailure: true`) | `Pages/Login.cshtml.cs` | Incrementa `AccessFailedCount`, aplica `LockoutEnd` | `auth.service.login` usa rate-limit buckets en `lu_auth_rate_limit`; no usa `AccessFailedCount` por usuario | sin columna `AccessFailedCount` | F3 | CONFLICT | semantica: ventana por IP+email en lugar de por usuario; F8: buckets por identificador+IP en vez de AccessFailedCount por usuario (aprobado). |
| A-09 | Login | `EmailConfirmed` no exigido (`RequireConfirmedAccount = false`) | `Program.cs` (`options.SignIn.RequireConfirmedAccount = false`) y `Create.cshtml.cs` (`EmailConfirmed = true`) | Usuario entra sin confirmar email | `lu_user` no tiene `email_confirmed`; login no exige confirmacion | ambos confirm ausencia | F3 | PARITY | sin email confirmation |

## Bloque B — Shell / Sidebar / Header

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| B-01 | Shell | Topbar logo + "LABORATORIOS" | `Pages/Shared/_Layout.cshtml` (navbar-brand) | Logo + texto "LABORATORIOS" en `letter-spacing:1px` | `App.tsx` `<span class="logo-text">LABORATORIOS</span>` con `<img src={logoLightIcon}/>` | paridad visual | F6 (visual parity) | PARITY | logo llega via `assets/images/logo-light-icon.png` |
| B-02 | Shell | Sidebar multi-seccion NiceAdmin | `Pages/Shared/_Sidebar.cshtml` | Aside con `first-level` colapsable, iconos MDI y badges | `App.tsx::NAVIGATION` array con `SidebarSection/Link/Group`; `aside.left-sidebar.sidebar-nav`; `<ul id="sidebarnav">` | paridad estructural | F6 | PARITY | item "Personas" agregado en `NAVIGATION` (no estaba en legacy) |
| B-03 | Shell | Sidebar item "Usuarios" en grupo "Personas" | `Pages/Shared/_Sidebar.cshtml` (lineas finales "Personas" > "Usuarios") | `asp-page="/Users/Index"` en grupo | `App.tsx::NAVIGATION` seccion `Personas` -> grupo `users` -> `Usuarios` (`/Users/Index`) | misma ruta | F6 | PARITY | ver tambien A/B/J |
| B-04 | Shell | Sidebar item "Personas" (separado) | `Pages/Shared/_Sidebar.cshtml` | **No existe** item directo; el directorio se ve desde `/Users/Index` tab personas | `App.tsx::NAVIGATION` anade `Personas` (`/Persons/Index`) | navegacion observable distinta | F5 | PARITY | comportamiento ampliado, registrado en A-J.A; F5: item Personas retirado del sidebar; directorio via tab de `/Users/Index` (antes CONFLICT) |
| B-05 | Shell | Campana de notificaciones condicional | `Pages/Shared/_Layout.cshtml` (`showBool` variable) | Solo en Dashboard/Managements/Acquisitions/Requests/Maintenances/Departures | `App.tsx` muestra campana **siempre** en workspace; `useEffect` carga `notifications({ unreadOnly: true })` | condicion eliminada | F6 | PARITY | sin alcance legacy de notificaciones del modulo Usuarios; F6: campana solo en Dashboard y Gestiones/Adquisiciones/Solicitudes/Mantenimientos/Salidas (`showBell` legacy) (antes CONFLICT) |
| B-06 | Shell | Avatar perfil en topbar con foto | `Pages/Shared/Components/UserProfile/Default.cshtml` | `img src="~/uploads/users/<file>?v=<Ticks>"` o iniciales | `App.tsx` solo usa `initials(displayName)` en `profile-avatar` | sin foto | F2 / F3 / F4 / F5 / F6 | PARITY | F1-D014; storage privado global; F6: avatar 31/50 px `rounded-circle bg-info` con foto autorizada (F5) o iniciales, como `UserProfile/Default.cshtml` (antes MISSING) |
| B-07 | Shell | Menu dropdown con "Mi Perfil" + "Cerrar Sesion" | `Pages/Shared/Components/UserProfile/Default.cshtml` (form `asp-page="/Logout"` POST) | Enlace a `/Users/Details` y submit `/Logout` | `App.tsx::profile-menu` con `<a href="/Users/Details" onClick={navigate}>` "Mi Perfil"; `Cerrar Sesion` llama `await api.logout()` | comportamiento equivalente | F5 | PARITY | |
| B-08 | Shell | Toast SweetAlert2 global | `Pages/Shared/_Layout.cshtml` (script `mixin(toast:true, position:'top-end', timer:4000)`) | Lee `TempData` y dispara Toast top-end | `UsersPanel.tsx` muestra alertas success/error inline | feedback existe, pero posicion, libreria y persistencia visual difieren | F5 | PARTIAL | sin toast global; F8: mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| B-09 | Shell | Preloader `lds-ripple` en login | `Pages/Login.cshtml` (preloader + auth-wrapper) | Spinner visible hasta `fadeOut()` | `App.tsx`: `view === 'loading'` muestra `<div className="lds-ripple"><div/></div>` "Validando sesion…" | paridad visual | F6 | PARITY | |
| B-10 | Shell | Selector de sede activa | inexistente en ASP | n/a | `App.tsx::site-switcher` dropdown + `view === 'site-picker'` si `activeSiteId === null` | feature nueva multi-sede | F4 | NOT_APPLICABLE | arquitectura multi-sede (decision 12 plan) |
| B-11 | Shell | Migas de pan (breadcrumb) | `Pages/Shared/_Layout.cshtml` (page-breadcrumb/page-title) | Solo `<h4 class="page-title">@ViewData["Title"]</h4>` | `App.tsx`: `<nav aria-label="Migas de pan"><ol class="breadcrumb"><li>Inicio</li>...` | paridad mejorada | F6 | PARITY | target agrega migas reales, legacy solo titulo; F6: cabecera solo con titulo de pagina; migas retiradas (el chip de sede es B-10) (antes PARTIAL) |
| B-12 | Shell | `HidePageTitle` toggle | `Pages/Shared/_Layout.cshtml` (`if (!(ViewData["HidePageTitle"] is bool hidePageTitle && hidePageTitle))`) | Permite ocultar titulo por vista | `App.tsx` siempre muestra titulo via `findRouteTitle` | toggle legacy no expuesto en target | F6 | NOT_APPLICABLE | sin uso conocido en Users/Persons; baja prioridad; F8: HidePageTitle no se usa en ninguna superficie MIG-001; no hay comportamiento que portar. (antes PARTIAL) |

## Bloque C — Users/Persons Index (tab Usuarios)

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| C-01 | Index Users | Card-shell con dos `tab-pane` (`#usuarios`, `#personas`) | `Pages/Users/Index.cshtml` (lineas 1-90) | Tabs NiceAdmin; Cuentas activo por defecto | `App.tsx` muestra `/Users/Index` -> `UsersPanel`, `/Persons/Index` -> `PeoplePanel`; sin tabs | target separa rutas | F5 | PARITY | A-J.A; F5: tabs Cuentas de Acceso / Directorio de Personal en `/Users/Index` (`?tab=personas`) (antes CONFLICT) |
| C-02 | Index Users | Smart Index Zone (`bg-light p-3 mb-4 rounded border-left border-info`) | `Pages/Users/Index.cshtml` (lineas 50-85) | Buscador + filtro estado + boton "Limpiar filtros" + contador `X cuenta(s)` | `UsersPanel.tsx`: `<div class="catalog-toolbar"><select statusFilter>...</select></div>` (sin buscador, sin limpiar, sin contador; el contador si esta en `catalog-count`) | buscador y "limpiar filtros" no presentes | F5 | PARITY | solo filtro estado; busqueda textual solo desde backend; F5: buscador + filtro estado + Limpiar filtros + contador `X cuenta(s)`; estilo visual en F6 (antes MISSING) |
| C-03 | Index Users | Buscar por nombre, usuario o CI | `Pages/Users/Index.cshtml.cs` (`Where u.FirstName/LastName/UserName/IdentityCard Contains term`) | Filtra por 4 campos | `users.repository.list`: `WHERE full_name ILIKE %term% OR email ILIKE %term%` | no busca por username ni por CI | F4 (contract) / F5 | PARITY | F5: UI envia `searchTerm`; F3 busca nombre, usuario, CI y correo (antes PARTIAL) |
| C-04 | Index Users | Filtrar por estado con `GetStatusSelectList<GeneralStatus>` | `Pages/Users/Index.cshtml` (select con `EnumHelper.GetStatusSelectList<GeneralStatus>()`) | Default: excluye `Eliminado`; permite `Activo/Inactivo` | `UsersPanel.tsx` `statusFilter` en {active, disabled} | sin "Inactivo" explicito (target lo colapsa en disabled) | F5 | PARITY | equivalente: 'disabled' cubre Inactivo y Eliminado; F5: filtro active/inactive/deleted; por defecto oculta eliminados (F1-D010) (antes PARTIAL) |
| C-05 | Index Users | Indicador `X cuenta(s)` con `aria-live="polite"` | `Pages/Users/Index.cshtml` | Live region anuncia conteo | `UsersPanel.tsx::page.totalCount` mostrado en `<span class="catalog-count">{page?.totalCount ?? 0} cuentas</span>` | sin aria-live | F6 (paridad a11y) | PARITY | LC-08 a11y; F6: `<strong>N</strong> cuenta(s)` en `span.small.text-muted` con `aria-live="polite"` (antes MISSING) |
| C-06 | Index Users | Paginacion 20 por pagina en la misma card | `Pages/Users/Index.cshtml.cs` (`PaginatedList.CreateAsync(..., pageSize: 20)`) | Muestra hasta 20 filas por pagina | `users.repository.list`: `size = 20` | sin UI de paginacion en `UsersPanel` (muestra siempre `currentPage = 1`) | F5 | PARITY | contrato soporta paginacion; UI no la expone; F5: paginacion Anterior/Siguiente sobre `pageIndex/totalPages` del servidor (antes PARTIAL) |
| C-07 | Index Users | Vacio con icono `fas fa-user-slash fa-3x opacity-2` | `Pages/Users/Index.cshtml` | colspan 6 con "No se encontraron cuentas bajo los criterios definidos." | `UsersPanel.tsx` `<div class="empty-state"><i class="mdi mdi-account-key-outline"/><h3>No hay cuentas</h3></div>` | copy y clase distintos; sin `colspan` (tabla) | F6 | PARITY | F6: fila `colspan=6` italica con icono `fa-user-slash opacity-2` y copy legacy (antes PARTIAL) |
| C-08 | Index Users | Encabezados thead `bg-light .small .uppercase .font-weight-bold` | `Pages/Users/Index.cshtml` (lineas thead) | 6 columnas con texto en mayuscula con tracking | `UsersPanel.tsx` tabla `<thead><tr><th>Cuenta</th><th>Rol</th><th>Estado</th><th>Acciones</th></tr></thead>` (4 columnas) | 4 cols vs 6 cols | F6 | PARITY | columnas legacy no presentes: Identidad, Seguridad, Perfil, Registro; F6: thead `bg-light` `small uppercase` con las 6 columnas legacy (antes CONFLICT) |
| C-09 | Index Users | Avatar circular con foto o iniciales | `Pages/Users/Index.cshtml` (img si foto, sino span `btn btn-circle btn-info`) | Foto con `style="width:36px;height:36px"` o iniciales | sin avatar en tabla | col foto eliminada | F6 | PARITY | LC-05; F6: foto 36 px por ruta autorizada o `btn-circle btn-info` con iniciales (antes MISSING) |
| C-10 | Index Users | Badge de rol con icono `fa-shield-alt` y `@UserName` italico | `Pages/Users/Index.cshtml` (celda seguridad) | Texto combinado | `UsersPanel.tsx`: `<td>{item.siteRole}</td>` (sin badge visual) y `<small>{item.email}</small>` | sin icono ni badge | F6 | PARITY | simplificacion visual; F6: `fa-shield-alt` + rol efectivo y `@usuario` italico (antes CONFLICT) |
| C-11 | Index Users | Columna `Registro` con fecha y `CreatedBy?.Initials` | `Pages/Users/Index.cshtml` | `CreatedDate.ToString("dd/MM/yyyy")` + `CreatedBy?.Initials ?? 'Sist.'` | sin columna Registro | columna eliminada | F6 | PARITY | F6: fecha `dd/MM/yyyy` + iniciales de `createdBy` o "Sist." (antes MISSING) |
| C-12 | Index Users | Acciones `[Editar][Detalles][Eliminar]` con tooltips | `Pages/Users/Index.cshtml` (btn-group) | Iconos fas en outline warning/info/danger | `UsersPanel.tsx`: `<button>Editar</button>` y `<button>Deshabilitar</button>` (sin iconos, sin "Detalles") | sin Detalles; cambio textual | F5 / F6 | PARITY | tabla simplificada; F6: `btn-group` outline warning/info/danger `btn-rounded` con iconos y `title`; se ocultan solo las acciones que el contrato prohibe (antes CONFLICT) |
| C-13 | Index Users | `asp-page="./Create"` (Vincular Usuario) | `Pages/Users/Index.cshtml` | Boton a la derecha | `UsersPanel.tsx`: el formulario inline es el alta; no hay boton "Vincular Usuario" separado | patron diferente: form inline | F5 / F6 | PARITY | F6: boton `bg-white border btn-rounded` "Vincular Usuario" arriba a la derecha -> `/Users/Create` (antes CONFLICT) |
| C-14 | Index Users | Captura de errores de servidor en TempData -> toast | `Pages/Shared/_Layout.cshtml` (script TempData toast) | Errores aparecen como toast top-end | `UsersPanel.tsx::error` renderizado como `<div class="alert alert-danger">`; sin toast | sin toast | F5 | PARTIAL | B-08; F5: errores tipados visibles (`describeApiError`) como alerta inline; sin toast (B-08) (antes MISSING); F8: mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |

## Bloque D — Index Persons (tab Personas)

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| D-01 | Index Persons | Cabecera "Directorio Base de Identidades" + "Nueva Identidad" | `Pages/Users/Index.cshtml` (tab #personas) | Enlace a `/Persons/Create` | `PeoplePanel.tsx` heading "Personas"; formulario inline es el alta | layout inline vs pagina Crear separada | F5 | CONFLICT | F8: alta de Persona inline (F1-D016, sin /Persons/Create). |
| D-02 | Index Persons | Tabla `_PersonsTable.cshtml` (5 columnas) | `Pages/Shared/_PersonsTable.cshtml` | thead Nombre Completo, Tipo, Contacto, Estado, Acciones | `PeoplePanel.tsx` thead Persona, Tipo, Contacto, Estado, Acciones | columnas equivalentes | F6 | PARITY | |
| D-03 | Index Persons | Badge Intern (azul) / Extern (amarillo) | `Pages/Shared/_PersonsTable.cshtml` (`item is Intern`) | Discriminacion por tipo | `PeoplePanel.tsx`: `TYPES.internal/external` con `<td>TYPES[item.type]{item.isEntity ? <small>Entidad</small> : null}</td>` | texto plano sin badge-pill | F6 | PARITY | visual mas pobre; F6: `badge-pill` Interno `badge-info` / Externo `badge-warning` (antes CONFLICT) |
| D-04 | Index Persons | Vacio con dashed border y `fa-id-badge fa-4x` | `Pages/Users/Index.cshtml` (tab #personas vacio) | "No se han registrado identidades en la base de datos base." | `PeoplePanel.tsx`: `<div className="empty-state"><i className="mdi mdi-account-group-outline"/><h3>No hay personas</h3></div>` | copy + clase distintas; sin dashed | F6 | PARITY | F6: bloque con borde discontinuo, `fa-id-badge fa-4x` y copy legacy; el enlace abre el alta (ver D-01) (antes PARTIAL) |
| D-05 | Index Persons | Aviso informativo "Pueden ser promovidas a Usuarios del Sistema" | `Pages/Users/Index.cshtml` | alert-light info border-left border-info | sin aviso equivalente | sin mensaje | F5 | PARITY | F5: aviso presente en el tab; se muestra aun con directorio vacio (antes MISSING); F6: residuo visual resuelto: aviso `alert-light border-left border-info` solo con directorio no vacio (antes PARTIAL) |
| D-06 | Index Persons | Filtros compartidos con tab Usuarios | `Pages/Users/Index.cshtml.cs` (`SearchTerm/StatusFilter/PageIndex` comunes) | Un cambio afecta a ambos tabs | `PeoplePanel.tsx` y `UsersPanel.tsx` son independientes; cada uno tiene su filtro | comportamiento de filtros distinto | F5 | PARTIAL | F5: busqueda y estado compartidos entre tabs; paginacion independiente por tab (antes CONFLICT); F8: busqueda/estado compartidos; paginacion independiente por tab. |
| D-07 | Index Persons | Acciones Editar/Detalles/Eliminar | `Pages/Shared/_PersonsTable.cshtml` | 3 botones outline | `PeoplePanel.tsx`: Editar + Baja | sin boton Detalles | F5 / F6 | PARTIAL | F6: Editar/Eliminar con la presentacion legacy; Detalles sigue ausente (sin ficha de Persona, I-10) (antes MISSING); F8: Editar/Eliminar; sin Detalles de Persona (F1-D016). |

## Bloque E — Create

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| E-01 | Create | FirstName, LastName, SecondLastName | `Pages/Users/Create.cshtml.cs` InputModel | obligatorios/apellidos opcionales | `CreateManagedUserInput.fullName: string` (campo unico) | sin split | F2 / F3 / F4 / F5 | PARITY | F1-D003: split canonico, FullName generado; F5: formulario con nombres separados; `fullName` derivado por servidor (antes CONFLICT) |
| E-02 | Create | C.I. `[Required, StringLength(10), RegularExpression("^[0-9A-Z-]*$")]` | `Pages/Users/Create.cshtml.cs` y `Create.cshtml` | input "Cedula de Identidad" con regex | `CreateManagedUserInput.identityCard` (F4); F3 exige CI <= 10 `[0-9A-Z-]` y unicidad `ux_lu_user_identity_card_ci` | ninguno | F4 (contract) | PARITY | LC-03; F4 contrato + F3 validacion |
| E-03 | Create | Email `[Required, EmailAddress]` | `Pages/Users/Create.cshtml.cs` | input Correo Institucional placeholder `usuario@univalle.edu` | `CreateManagedUserInput.email: string` | equivalencia | F4 | PARITY | email es login en target |
| E-04 | Create | PhoneNumber `[Required, Phone]` | `Pages/Users/Create.cshtml.cs` | requerido, formato valido | `CreateManagedUserInput.phoneNumber` requerido (F4); F3 valida formato telefonico | ninguno | F4 | PARITY | LC-09; F4 contrato + F3 validacion |
| E-05 | Create | ProfilePicture upload via `SafeImageUpload` | `Pages/Users/Create.cshtml.cs` + `Pages/Users/Create.cshtml` | sube a `wwwroot/uploads/users/{guid}{name}` | sin upload | sin almacenamiento | F2 / F3 / F4 / F5 | PARITY | F1-D014; F5: foto opcional en alta: `uploadUserPhoto` tras crear; fallo reportado sin fingir transaccion (antes MISSING) |
| E-06 | Create | Role `[Required] UserRole` (Administrador, Supervisor, SuperAdmin) | `Pages/Users/Create.cshtml.cs` + `EnumHelper.ToSelectList<UserRole>()` | select con `display-name`; oculta SuperAdmin si solicitante no SuperAdmin | `CreateManagedUserInput.siteRole: SiteRole` (Administrador \| Supervisor) | sin SuperAdmin como selectable | F2 / F3 / F4 / F5 | PARITY | F1-D007/F1-D008: global fuera del payload de sede; F5: rol de membresia Administrador/Supervisor; SuperAdmin solo por CLI (F1-D008) (antes CONFLICT) |
| E-07 | Create | UserName `[Required, StringLength(256)]` (login) | `Pages/Users/Create.cshtml.cs` | login name | `CreateManagedUserInput.email` es login | consolidado | F2 / F3 / F4 / F5 | PARITY | F1-D004; falta campo/claim username; F5: campo Usuario (login) en alta (antes CONFLICT) |
| E-08 | Create | Password: DataAnnotation minimo 8, Identity exige 12 + mayuscula/minuscula/digito/simbolo | `Pages/Users/Create.cshtml.cs`, `Program.cs` | puede pasar validacion del formulario y fallar en `CreateAsync` | Users API acepta 8-72 unidades UTF-16; login limita 72 bytes UTF-8 | politicas y unidad de longitud incompatibles | F3 / F4 / F5 | PARITY | F1-D005/F1-D006; F5: politica compartida `passwordPolicyViolations` (12 code points, 72 bytes) (antes CONFLICT) |
| E-09 | Create | Veto SuperAdmin para no-SuperAdmin | `Pages/Users/Create.cshtml.cs` | muestra error si solicitante no es SuperAdmin y elige SuperAdmin | sin opcion SuperAdmin; sin veto necesario | n/a | F3 / F5 | NOT_APPLICABLE | F1-D008: comando global auditado, no UserController |
| E-10 | Create | Generar password automatica + toggle ver/ocultar | `Pages/Users/Create.cshtml` scripts `generatePassword()` / `togglePassword()` | botones JS | sin equivalentes | feature legacy no portada | F5 | PARITY | UX; F5: botones Ver/Ocultar y Generar (politica valida) (antes MISSING) |
| E-11 | Create | Bloqueo letras solo en nombres | `Pages/Users/Create.cshtml` (regex `[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]`) | no permite numeros ni simbolos | sin bloqueo (texto libre) | sin gate | F5 | CONFLICT | F8: decision F8: no se bloquean caracteres; F1 §5 no exige solo letras y el bloqueo legacy rechazaria nombres validos (guion, apostrofo, letras Unicode). El servidor normaliza y valida longitud. (antes PARTIAL) |
| E-12 | Create | jqBootstrapValidation activo | `Pages/Users/Create.cshtml` script | `$("input,select,textarea").jqBootstrapValidation()` | sin jQuery, sin `jqBootstrapValidation` | stack diferente | F5 | NOT_APPLICABLE | jQuery eliminado |
| E-13 | Create | Validacion unicidad de CI/Email/UserName contra DB antes de crear | `Pages/Users/Create.cshtml.cs` | `_context.Users.IgnoreQueryFilters().AnyAsync(...)` | F2 indices unicos email/username/CI; F3 devuelve 409 `CONFLICT`; F4 `ApiClientError.kind === 'conflict'` | ninguno | F4 | PARITY | unicidad de CI/email/username |
| E-14 | Create | Transaccion (`CreateAsync` + `SynchronizeManagedRoleAsync`) | `Pages/Users/Create.cshtml.cs` | rollback si falla cualquiera, borra foto | `users.repository.create` envuelve en `pool.transaction` y maneja `unique` violation; sin lockout | sin carga de foto | F4 | PARITY | atomicidad mantenida |
| E-15 | Create | `EmailConfirmed = true` + `CreatedDate = UtcNow` automaticos | `Pages/Users/Create.cshtml.cs` | sin confirmar email | sin columna `email_confirmed`; `created_at` por default | ambos cumplen | F4 | PARITY | |
| E-16 | Create | Mensaje exito `TempData.Success($"Cuenta de usuario para '{user.FullName}' creada exitosamente.")` | `Pages/Users/Create.cshtml.cs` | toast global | `UsersPanel.tsx::setFeedback('Cuenta creada correctamente.')` render inline | sin toast global | F5 | PARTIAL | B-08; F5: mensaje exacto como flash inline tras redirect; sin toast (B-08) (antes MISSING); F8: mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| E-17 | Create | Redirect `./Index` | `Pages/Users/Create.cshtml.cs` | redirect tras exito | `UsersPanel.tsx::submit` -> `await load()` y resetea form | sin redirect explicito | F5 | PARITY | patron SPA; F5: exito navega a `/Users/Index` (antes PARTIAL) |
| E-18 | Create | `SetLockoutEndDateAsync(null)` en alta (no aplica, usuarios nuevos pueden entrar) | `Pages/Users/Create.cshtml.cs` | lockoutEnabled true por default | `bcrypt.hash` + insert; `is_super_admin=false`, `status='active'` | ambos permiten login | F4 | PARITY | |

## Bloque F — Edit

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| F-01 | Edit | Layout asimetrico: form `col-lg-8` + sidebar `col-lg-4` oscuro | `Pages/Users/Edit.cshtml` | form con 2 columnas + sidebar `bg-dark text-white` Estatus/Seguridad/Acciones | `UsersPanel.tsx` formulario inline + tabla lado a lado (sin sidebar oscuro) | sin sidebar | F5 / F6 | PARITY | LC-08 colapso admin; F6: formulario `col-lg-8` en dos columnas + panel `col-lg-4` `bg-dark` Estatus de Cuenta y aviso de rol (antes MISSING) |
| F-02 | Edit | `[Authorize(Roles = AdminRoles)]` + veto a SuperAdmin ajeno | `Pages/Users/Edit.cshtml` lineas `@page "{id:int}"` y `OnGetAsync` | solo admin puede ver/editar | `user.service.list/find/update` llama `administrator(context)` que rechaza no-`Administrador`/`SuperAdmin` | mas restrictivo (no edita Supervisor) | F3 / F4 | PARITY | semantica: Supervisor no edita cuentas; F8: Administrador y SuperAdmin administran; Supervisor no (igual que AdminRoles legacy); veto sobre SuperAdmin ajeno (F1 §8). (antes PARTIAL) |
| F-03 | Edit | Campo Usuario `readonly` | `Pages/Users/Edit.cshtml` | readonly con nota "permanente" | sin campo username | sin username | F2 / F3 / F4 / F5 | PARITY | F1-D004 restablece username inmutable; F5: Usuario readonly con nota de permanencia (antes MISSING) |
| F-04 | Edit | `NewPassword` opcional | `Pages/Users/Edit.cshtml.cs` | placeholder + nota "Deje en blanco para conservar la actual" | `UpdateManagedUserInput.password?: string \| null`; repo hashea solo si no vacio | comportamiento equivalente | F4 | PARITY | |
| F-05 | Edit | Setear `Status != Activo` aplica `LockoutEndDateAsync(MaxValue)` | `Pages/Users/Edit.cshtml.cs` | revoquea acceso | `users.repository.update` setea `status='disabled'` y `security_version+=1` (invalida sesiones) | sin Lockout permanente (rotacion sessions) | F3 / F4 | PARITY | LC-04; F8: inactivacion = account_status + rotacion security_version + revocacion de sesiones; mismo efecto observable que Lockout MaxValue (igual criterio que H-03). (antes CONFLICT) |
| F-06 | Edit | Reactivar limpia Lockout y resetea `AccessFailedCount` | `Pages/Users/Edit.cshtml.cs` | si `Status == Activo && previousStatus != Activo` | reactivacion solo via set `status='active'` -> membership reactivable; sin reset de counter | comportamiento equivalente parcial | F4 | PARITY | F8: reactivar devuelve el acceso y rota security_version; AccessFailedCount no existe en el target (A-08). (antes PARTIAL) |
| F-07 | Edit | Veto self-edit | `Pages/Users/Edit.cshtml.cs` | no cambiar rol ni desactivar la propia cuenta | `user.service.update`: si `id === context.userId && (status !== 'active' \|\| siteRole !== context.siteRole)` lanza `AuthForbiddenException` | mas restrictivo (no cambia tampoco su role) | F4 | PARITY | F8: F1 §9: sin cambio de rol ni inactivacion propia, como legacy. (antes PARTIAL) |
| F-08 | Edit | Veto unico SuperAdmin activo | `Pages/Users/Edit.cshtml.cs` | permite editar SuperAdmin, pero no rebajarlo/desactivarlo si es el ultimo | `users.repository.update` bloquea dejar cero SuperAdmins activos (lock bajo serializacion) | proteccion mas amplia y sin conteo de ultimo SuperAdmin | F2 / F3 / F7 | CONFLICT | F1-D008; falta comando global y conteo serializado; F8: F1-D008 (rol SuperAdmin solo por CLI; ultimo SuperAdmin activo bajo lock). |
| F-09 | Edit | `UpdateSecurityStampAsync` cada save | `Pages/Users/Edit.cshtml.cs` | invalida cookies | `security_version+=1` (rechaza sesiones con `securityVersion` desactualizada) | diferente mecanismo, mismo objetivo | F3 / F4 | PARITY | invalidar sesiones |
| F-10 | Edit | `RefreshSignInAsync` si editas tu propia cuenta | `Pages/Users/Edit.cshtml.cs` | renueva cookie de sesion | n/a en target (sesion por token API) | sin necesidad | F3 | NOT_APPLICABLE | sin cookies sliding |
| F-11 | Edit | Subir foto con preview de la anterior | `Pages/Users/Edit.cshtml` (`<img src="~/uploads/users/@ExistingProfilePictureUrl" class="img-thumbnail">`) | preview 100px antes de upload | sin upload | sin feature | F2 / F3 / F4 / F5 / F6 | PARITY | F1-D014; F6: foto actual `img-thumbnail` 100 px o "Sin foto de perfil" + `custom-file`; la subida es un comando mas al guardar (antes MISSING) |
| F-12 | Edit | Cambio de roles via `SynchronizeManagedRoleAsync` (IdentityRoleExtensions) | `Pages/Users/Edit.cshtml.cs` + `Helpers/IdentityRoleExtensions.cs` | mantiene un solo rol manejado por user | `users.repository.update` setea `lu_site_membership.role = $3` directamente | sin helper de sincronizacion; patron multi-sede | F4 | NOT_APPLICABLE | contratos distintos; F8: F1-D007: rol por membresia; no hay rol Identity que sincronizar (igual que M-07). (antes CONFLICT) |
| F-13 | Edit | Mensaje exito | `Pages/Users/Edit.cshtml.cs` | `TempData.Success("Datos de la cuenta '{FullName}' actualizados correctamente.")` | `setFeedback('Cuenta actualizada correctamente.')` inline | sin toast global | F5 | PARTIAL | B-08; F5: mensaje exacto como flash inline; sin toast (B-08) (antes MISSING); F8: mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| F-14 | Edit | Redirect segun `returnUrl` | `Pages/Users/Edit.cshtml.cs` | a Details si returnUrl=Details, sino Index | sin param returnUrl; queda en `/Users/Index` | sin Detail admin | F5 | PARITY | F5: `?returnUrl=Details` vuelve a la ficha; si no, al listado (antes PARTIAL) |
| F-15 | Edit | Confirmar pre-submit con SweetAlert | `Pages/AGENTS.md` y `context.md` | deberia disparar confirm antes del POST | sin confirmacion previa; submit directo | sin UX de confirmacion | F5 | NOT_APPLICABLE | LC-01; F8: LC-01: la confirmacion SweetAlert previa solo existe en guias (context.md, Pages/AGENTS.md); el ASP ejecutable envia directo. El target reproduce el comportamiento ejecutable. (antes MISSING) |

## Bloque G — Details

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| G-01 | Details | Ruta `/Users/Details/{id}` admin (sidebar Estatus/Acciones) | `Pages/Users/Details.cshtml` | admin abre ficha de cualquier cuenta | `/Users/Details` monta `ProfilePanel` (Mi Perfil autocentrado) | semantica cambiada | F5 / F6 | PARITY | A-J.C; F6: ficha `col-lg-8` + barra `col-lg-4` Seguridad de acceso/Acciones (antes CONFLICT) |
| G-02 | Details | Avatar circular 150px (foto o iniciales grandes) | `Pages/Users/Details.cshtml` | `.user-detail-avatar { width:150px; height:150px; min-width:150px; ... border: 4px solid #e0f2fe;}` y `.user-detail-initials { font-size: 3.1rem }` | sin avatar; `ProfilePanel` sin avatar | sin feature | F6 | PARITY | LC-05; F6: `.user-detail-avatar` 150 px con foto o `.user-detail-initials` (antes MISSING) |
| G-03 | Details | Bloque "Identidad" Nombres/Ap Paterno/Materno/CI/Usuario/Fecha Alta | `Pages/Users/Details.cshtml` | 6 campos | `ProfilePanel.tsx` solo `email`, `fullName`, `siteRole` | sin nombres/CI/phone/fecha | F4 / F5 | PARITY | colapso a Mi Perfil; F5: ficha `/Users/Details/:id` con nombres, CI, usuario y fecha de alta (antes CONFLICT) |
| G-04 | Details | Bloque "Contacto y perfil laboral" | `Pages/Users/Details.cshtml` | correo, telefono, cargo, departamento | sin bloque | sin exposicion | F5 | PARITY | LC-09; F5: correo, telefono, cargo, departamento, fecha de ingreso por membresia (antes MISSING) |
| G-05 | Details | Bloque "Auditoria" | `Pages/Users/Details.cshtml` | Creado por + fecha, Ultima modificacion + fecha | sin bloque | sin auditoria | F2 / F3 / F4 / F5 | PARITY | F1-D013; F5: Creado por/Ultima modificacion desde `createdBy/modifiedBy` (null = Sistema) (antes MISSING) |
| G-06 | Details | Sidebar `Estatus de Cuenta` con badge-pill success/danger | `Pages/Users/Details.cshtml` (sidebar oscuro `bg-dark text-white`) | muestra Rol, Estado, Foto | sin sidebar | sin panel | F6 | PARITY | F6: tarjeta `bg-dark` "Seguridad de acceso": Rol, Estado `badge-pill`, Foto (antes MISSING) |
| G-07 | Details | Acciones `Modificar Perfil` (volver a Details) / `Volver al Listado` / `Revocar Acceso` | `Pages/Users/Details.cshtml` (sidebar Acciones) | tres botones | sin acciones admin en Mi Perfil | sin feature | F5 / F6 | PARITY | F6: Modificar Perfil (`btn-warning btn-block`) / Volver al Listado / Revocar Acceso (`btn-link text-danger`) (antes MISSING) |
| G-08 | Details | Helper inline `ValueOrFallback` con fallbacks "Sin dato" / "Sin dato registrado" / "Sin modificaciones" / "Sin fecha registrada" | `Pages/Users/Details.cshtml` | fiel a UI | sin fallback explicito; `ProfilePanel` muestra "Cargando…" durante load | copy distinto | F6 | PARITY | F6: fallbacks legacy "Sin dato", "No registrado", "Cargo no registrado", "Sistema", "Sin modificaciones", "Sin fecha registrada" (antes PARTIAL) |
| G-09 | Details | Cache-busting avatar `?v=Ticks` | `Pages/Users/Details.cshtml` (linea avatar) | refresca imagen tras upload | sin imagen | sin equivalente implementado | F3 / F5 / F6 | PARITY | F1-D014 usa key/version o ETag; F6: imagen re-solicitada por `etag` (F5) y mostrada en ficha, listado, edicion y topbar (antes MISSING) |
| G-10 | Details | Redirect a `/Error` con `module/entityId/message/returnUrl/listUrl` | `Pages/Users/Details.cshtml.cs` | 404 estructurado en pagina propia | sin redireccion a `/Error` (no leido si existe target equivalente); `App.tsx::unavailable` muestra servicio no disponible | flujos distintos | F5 | PARTIAL | la pagina `/Error` no fue inspeccionada en target; F5: 404/403 tipados con mensaje y Volver al Listado; sin pagina `/Error` dedicada (antes MISSING); F8: 404/403 tipados inline con Volver al Listado; sin pagina /Error (desviacion aceptada F8). |

## Bloque H — Delete

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| H-01 | Delete | Ruta `/Users/Delete/{id}` | `Pages/Users/Delete.cshtml` `@page "{id:int}"` | confirmacion dedicada | sin ruta dedicada; `UsersPanel.disable` ejecuta directo | sin pantalla confirmacion | F5 / F6 | PARITY | LC-01; F6: tarjeta centrada `col-lg-5` con `fa-user-times`, datos clave y botones legacy (antes MISSING) |
| H-02 | Delete | Soft delete (`Status = Eliminado`) | `Pages/Users/Delete.cshtml.cs` | `user.Status = Eliminado` | `updateUser` -> `update(status='disabled')` + `security_version++` | tercera opcion (Eliminado) no existe | F2 / F3 / F4 / F5 | PARITY | F1-D010/F1-D011; F5: revocacion de sede (`revokeMembership`) o baja global SuperAdmin (`deleteAccount`) (antes CONFLICT) |
| H-03 | Delete | Lockout permanente via `SetLockoutEndDateAsync(MaxValue)` | `Pages/Users/Delete.cshtml.cs` | revoquea acceso | `status='disabled'` + rotura security_version | diferente mecanismo, mismo efecto | F3 / F4 | PARITY | sesiones invalidadas |
| H-04 | Delete | `UpdateSecurityStampAsync` (rotacion stamp) | `Pages/Users/Delete.cshtml.cs` | invalida cookies existentes | `security_version++` (mismo objetivo) | equivalente | F3 / F4 | PARITY | |
| H-05 | Delete | Veto self-delete | `Pages/Users/Delete.cshtml.cs` | "No puede dar de baja su propia cuenta" | `user.service.update`: `id === context.userId && (status !== 'active' \|\| siteRole !== context.siteRole)` -> `AuthForbiddenException` | semantica equivalente (prohibe modificar role/status del propio user) | F4 | PARITY | |
| H-06 | Delete | Veto unico SuperAdmin | `Pages/Users/Delete.cshtml.cs` | solo bloquea dejar cero SuperAdmins activos | `users.repository.update` bloquea dejar cero SuperAdmins activos (lock bajo serializacion) | regla observable distinta | F2 / F3 / F7 | CONFLICT | F1-D008; F8: F1-D008 (rol SuperAdmin solo por CLI; ultimo SuperAdmin activo bajo lock). |
| H-07 | Delete | Mensaje exito `TempData.Success("El acceso para '{FullName}' ha sido revocado correctamente.")` | `Pages/Users/Delete.cshtml.cs` | toast global | `setFeedback('Cuenta deshabilitada correctamente.')` inline | sin toast | F5 | PARTIAL | B-08; F5: mensaje exacto como flash inline; sin toast (B-08) (antes MISSING); F8: mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| H-08 | Delete | Mensaje ya-dado-de-baja (warning) | `Pages/Users/Delete.cshtml.cs` | `TempData.Warning(...)` | sin warning equivalente | sin control de estado previo en UI | F3 / F5 | PARITY | F1-D010/F1-D011 restablecen deleted/revoked; F5: advertencia "ya se encuentra dado de baja" en la confirmacion (antes MISSING) |
| H-09 | Delete | Captura de error de Identity y redirect | `Pages/Users/Delete.cshtml.cs` (try/catch + rollback) | reintento via Delete/{id} | `users.repository.update` envuelve en transaction y rechaza conflictos; UI muestra `setError` | sin rollback explicito de foto | F4 | PARTIAL | sin upload no hay rollback de archivo; F8: error visible en la pagina Delete; sin redireccion de reintento. |
| H-10 | Delete | Confirmacion SweetAlert pre-submit | `Pages/AGENTS.md` y `context.md` | deberia disparar confirmacion | `UsersPanel.disable` usa `window.confirm(...)` nativo | sin SweetAlert2 v7 | F5 | NOT_APPLICABLE | LC-01; F8: LC-01 (idem F-15): el Delete legacy es su pagina de confirmacion (H-01 PARITY); los comandos de membresia usan window.confirm adicional. (antes CONFLICT) |
| H-11 | Delete | Sugerencia tecnica "Se recomienda Inactivar desde el editor" | `Pages/Users/Delete.cshtml` | hint externo al usuario | sin equivalente | sin feature | F6 | PARITY | F6: sugerencia italica bajo la tarjeta; indica la ficha del usuario, donde vive "Inactivar cuenta" en el target (antes MISSING) |

## Bloque I — Persons / PeoplePanel

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| I-01 | Persons | `ActorCode` opcional | `Models/Person.cs`, `Pages/Persons/*` | campo opcional del modelo; el CRUD Razor auditado no lo expone en su InputModel | `CreatePersonInput.actorCode?: string \| null` + `PersonRecord.actorCode`; PeoplePanel lo expone | target agrega control visible respecto al CRUD inspeccionado | F5 | NOT_APPLICABLE | modelo legacy si contiene el campo; F8: F1-D016: el tab reutiliza la API/panel de People sin rediseño; actorCode pertenece al modelo People. (antes PARTIAL) |
| I-02 | Persons | `Category` (Tecnico=1, Docente=2, Administrativo=3, Estudiante=4, Proveedor=5, Otro=99) | `Models/Person.cs` y `Pages/Persons/*` | seleccion | `PersonCategory = 1 \| 2 \| 3 \| 4 \| 5 \| 99` | paridad | F4 | PARITY | |
| I-03 | Persons | `Type` Intern/Extern + `isEntity` + `address` (solo Extern) | `Models/Person.cs`, `Models/Intern.cs`, `Models/Extern.cs`, `ApplicationDbContext.cs` | herencia TPT y Address obligatorio para Extern | `lu_person` plano con discriminador `person_type`, `is_entity`, `address` | comportamiento de campos parcial, persistencia distinta | F1 / F4 | NOT_APPLICABLE | TPT legacy vs tabla plana; F8: F1-D016 / F1 §15: estados/persistencia de subtipo fuera de MIG-001. (antes PARTIAL) |
| I-04 | Persons | `Status` 0/1/2 mas `InternStatus`/`ExternStatus` | `Models/Person.cs`, `Models/Intern.cs`, `Models/Extern.cs`, `Pages/Persons/Edit.cshtml.cs` | estado base y estado de subtipo editables | solo `lu_person.status`; UI permite 0/1/2, pero no estados de subtipo | columnas de subtipo ausentes | F1 / F4 | NOT_APPLICABLE | mismo enum base, perdida de estados derivados; F8: F1-D016 / F1 §15 (idem). (antes PARTIAL) |
| I-05 | Persons | Validacion `address` obligatoria para externos | `PeoplePanel.tsx::submit` | inline validation | confirm: `external + (address ?? '').trim() === ''` -> `setError('La direccion es obligatoria para externos.')` | paridad | F4 | PARITY | misma regla que `ck_lu_person_external_address` |
| I-06 | Persons | `PaginatedList<Person>.CreateAsync(...)` page size 20 | `Pages/Users/Index.cshtml.cs`, `Helpers/PaginatedList.cs` | 20 por pagina | `person.repository.page()` fija 20 | misma capacidad backend | F5 | PARITY | UI target aun no pagina: ver I-08 |
| I-07 | Persons | `OrderByDescending(p => p.Id)` | `Pages/Users/Index.cshtml.cs` | mas recientes primero | `person.repository.list`: `ORDER BY name ASC,id ASC` | orden observable distinto | F4 / F5 | CONFLICT | resuelto en F0; F8: orden People name ASC (API reutilizada, F1-D016). |
| I-08 | Persons | Paginacion UI (Si TotalPages > 1) | `Pages/Users/Index.cshtml` | muestra paginacion solo si > 1 | `PeoplePanel.tsx`: pagination UI no presente (siempre `currentPage=1`) | sin paginacion | F5 | PARITY | F5: paginacion UI en PeoplePanel (antes MISSING) |
| I-09 | Persons | SearchTerm solo filtra Email o Id == term | `Pages/Users/Index.cshtml.cs` | busqueda textual simple | sin input de busqueda | sin UI | F5 | PARTIAL | F5: busqueda servidor por nombre/correo/codigo; sin igualdad por Id (L-03) (antes MISSING); F8: busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |
| I-10 | Persons | Auditoria readonly (CreatedBy, ModifiedBy, fechas) | `Models/Person.cs` (`IAuditable`) | navegaciones | sin exposicion en `PersonRecord` | sin detail admin | F5 | NOT_APPLICABLE | sin detalles panel; F8: F1-D016: ficha/auditoria de Persona fuera de MIG-001 (solo el tab reutilizado). (antes MISSING) |
| I-11 | Persons | ImportBatchId + RowVersion | `Models/Person.cs` (`[Timestamp]`) | control optimista | sin columna `row_version` en `lu_person` | sin version optimista | F1 / F4 | NOT_APPLICABLE | riesgo si edicion concurrente; F8: F1 §15: concurrencia optimista de Persona e ImportBatchId fuera de MIG-001. (antes MISSING) |
| I-12 | Persons | Consultar bajas con filtro `Status=Eliminado` | `Pages/Users/Index.cshtml.cs` permite filtro exacto | lista registros eliminados cuando se selecciona 2 | `person.repository.list` combina siempre `status<>2` con `status=2` | filtro visible Baja devuelve cero | F3 / F5 | PARITY | LC-15; F5: UI envia `statusFilter=2`; F3 devuelve eliminados (antes CONFLICT) |

## Bloque J — Mi Perfil (nuevo)

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| J-01 | Perfil | Editar perfil propio desde Details -> Edit administrativo | `Pages/Users/Details.cshtml`, `Pages/Users/Edit.cshtml` | comparte ficha/admin y campos completos | `ProfilePanel.tsx` tiene formulario propio limitado a email/fullName | flujo observable distinto | F5 | PARITY | A-J.C; F5: `/Profile` edita nombres, correo y telefono propios (F1-D017) (antes CONFLICT) |
| J-02 | Perfil | Cambiar mi propia password | indirectamente via `/Users/Edit/{id}` si eres admin | el propio user (no admin) no tenia path para cambiar password | sin endpoint `POST /profile/password` | feature faltante | F3 / F4 / F5 | PARITY | F1-D005/F1-D006/F1-D012; F5: cambio de password propio en `/Profile` (`changePassword`) (antes MISSING) |
| J-03 | Perfil | Cambiar mi propia foto | via `/Users/Edit/{id}` | solo admin | sin upload | feature faltante | F2 / F3 / F4 / F5 | PARITY | F1-D014; F5: foto propia subir/quitar en `/Profile` (antes MISSING) |
| J-04 | Perfil | Ver mi CI | via `/Users/Details/{id}` | admin | sin columna | campo canonico aun ausente | F2 / F3 / F4 / F5 | PARITY | F1-D002/F1-D019; F5: CI propio visible de solo lectura (antes MISSING) |
| J-05 | Perfil | Auditoria del propio perfil (creado/modificado) | via Details | visible | sin detalle | sin feature | F4 | PARTIAL | LC-07; F8: los actores de auditoria del propio usuario son visibles en la ficha administrativa (/Users/Details/:id, G-05) para Administrador/SuperAdmin, como legacy; /Profile no los expone (el backend GET /profile no los devuelve; no se inventan). (antes MISSING) |
| J-06 | Perfil | Mensaje exito update | `<div className="alert alert-success">` | inline | `setFeedback('Perfil actualizado correctamente.')` inline | paridad funcional sin toast | F5 | PARITY | |

## Bloque K — Persons / Users actions / Permissions

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| K-01 | Permisos | `[Authorize(Roles = "Administrator,SuperAdmin")]` en todas las Pages Users | `Pages/Users/*.cshtml.cs` | admin/SuperAdmin ven todas las acciones | `user.service.list/find/create/update`: `administrator(context)` rechaza si `siteRole !== Administrador && globalRole !== SuperAdmin` | equivalente | F3 / F4 | PARITY | |
| K-02 | Permisos | `User.IsInRole(RoleSuperAdmin)` para veto de SuperAdmin | `Pages/Users/Create.cshtml.cs`, `Edit.cshtml.cs`, `Delete.cshtml.cs` | middleware role check | `user.controller.update`: valida `id === context.userId && ...`; SuperAdmin global manejable via `is_super_admin=true` en lu_user; **NO hay endpoint para asignar/quitar SuperAdmin** | falta comando global auditado y protecciones exactas | F3 / F5 | PARITY | F1-D008; no exponer flag en UserController; F5: UI oculta edicion/baja de SuperAdmin a Administrador de sede; servidor autoritativo (antes PARTIAL) |
| K-03 | Sesion | Modo `AllowAnonymousToPage("/Login")` | `Program.cs` (`options.Conventions.AllowAnonymousToPage("/Login")`) | `/Login` accesible sin auth | `App.tsx::view = 'login'` mostrado tras 401 o al inicio sin sesion | equivalente | F3 | PARITY | |
| K-04 | Sesion | FallbackPolicy RequireAuthenticatedUser | `Program.cs` | todo el resto exige auth | F3: todas las rutas no-auth pasan por `AuthSecurityGuard` + `GlobalSessionGuard` (perfil/usuarios/fotos) o `SiteContextGuard` (tenant); sesiones restringidas rechazadas | sin gap; el contexto de sede es arquitectura aprobada (F1 §17) | F3 | PARITY | ver K-07; `03-HANDOFF.md` |
| K-05 | Multi-sede | Sin multi-sede (sede unica) | `Program.cs` | sin routing por sede | `App.tsx::NAVIGATION` con item "Personas" como ruta administrativa; `SiteContextGuard` activa `siteRole/siteName` y `SiteRequestContext` | arquitectura nueva | F4 / F5 | NOT_APPLICABLE | decision 12 plan |
| K-06 | Permisos | SuperAdmin accede al directorio Persons incluido en Users | `AuthorizationHelper.AdminRoles`, `Pages/Users/Index.cshtml.cs` | Administrator y SuperAdmin ven ambos tabs | `people/person.service.ts` acepta solo `SiteRole.Administrador`; Users service si acepta `GlobalRole.SuperAdmin` | RBAC inconsistente rompe reuso del tab | F3 / F5 | PARITY | F1-D016: requiere membership Administrador para People; F5: tab exige sede activa Administrador (F1-D016); mensaje explicito sin membresia (antes CONFLICT) |
| K-07 | Permisos | SuperAdmin puede administrar sin concepto de sede | ASP monosedes | acceso global directo | Auth permite sesion SuperAdmin con `activeSiteId=null`, pero `SiteContextGuard` rechaza workspace sin sede | login exitoso sin acceso a Users/Persons | F3 / F5 | PARITY | F1-D018: Users/Profile globales sin sede; tenant exige membership; F5: sesion normal sin sede: Usuarios y Mi Perfil; datos de sede requieren elegir sede (antes CONFLICT) |

## Bloque L — Personas: reuso vs duplicacion

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| L-01 | Personas | Persona usada como FK en Departures/RoleAssignments/Maintenance/Requests/Verifications/CostDetails/ManagementPlans/MaintenancePlans | `Models/Person.cs` (colecciones) | sin duplicacion | `lu_person` id + `lu_*` (gestion, solicitudes, etc) | sin duplicar identidades | F5 | PARITY | F1-D015 confirma User/Person separados |
| L-02 | Personas | `FullName` virtual `[NotMapped]` "Ficha de Persona" (en `Person` base), implementado en Intern/Extern | `Models/Person.cs` | derivado en jerarquia | `lu_person.name` columna unica | sin split nombres/apellidos | F4 | CONFLICT | requisito de migracion; F8: nombre unico de Persona (modelo People reutilizado). |
| L-03 | Personas | `Id.ToString() == term` (busqueda por Id puro) | `Pages/Users/Index.cshtml.cs` (`personQuery.Where(p => p.Id.ToString() == term)`) | soporte de busqueda por Id | sin busqueda textual | sin gate | F5 | MISSING | F8: busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |

## Bloque M — Datos persistidos

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| M-01 | Schema | `Users.ToTable("Users")` con check `CK_Users_Role IN (1,2,99)` y `CK_Users_Status BETWEEN 0 AND 2` | `Data/ApplicationDbContext.cs` | constraint SQL Server | `lu_user` no tiene checks equivalentes para `role` (vive en membership) ni para `status` (default `'active'`) | check `lu_user.status IN ('active','disabled')` ya existe en PG | F2 | PARITY | |
| M-02 | Schema | `People.ToTable("People")` con check `CK_People_Status BETWEEN 0 AND 2` y `CK_People_Category IN (1,2,3,4,5,99)` | `Data/ApplicationDbContext.cs` | constraint SQL Server | `lu_person` con `ck_lu_person_status`, `ck_lu_person_category`, `ck_lu_person_type`, `ck_lu_person_external_address` | paridad | F2 | PARITY | |
| M-03 | Schema | TPT `Person -> People`, `Intern -> Interns`, `Extern -> Externs` | `Data/ApplicationDbContext.cs` | JOIN por subtipo, sin discriminador en una sola tabla | `lu_person.person_type` en tabla plana | mapeo de herencia cambiado | F1 / F2 | NOT_APPLICABLE | no confundir con TPH; F8: F1-D016: esquema People (TPT vs tabla plana) no se rediseña en MIG-001. (antes CONFLICT) |
| M-04 | Schema | `Users` con `IdentityUser<int>` integer PK | `Models/User.cs` | int PK | `lu_user.id uuid` | uuid intencional con crosswalk permanente | F2 | NOT_APPLICABLE | F1-D001: UUID canonico, no se busca paridad de tipo |
| M-05 | Schema | Password hash Identity | `Program.cs` | PBKDF2 default | `bcryptjs` `password_hash` column | algoritmo distinto | F2 / F3 | NOT_APPLICABLE | F1-D005: estado legacy temporal + rehash; F8: F1-D005: bcrypt + verificadores ASP.NET Identity V2/V3 con rehash; los usuarios conservan su password (probado en vivo F7). (antes CONFLICT) |
| M-06 | Schema | `Users.SecurityStamp` GUID | `Models/User.cs` (heredado de Identity) | GUID | `lu_user.security_version` bigint monotono | mecanismo destino aprobado | F3 / F7 | NOT_APPLICABLE | F1-D012 congela semantica equivalente |
| M-07 | Schema | `IdentityRole<int>` y `IdentityRoleClaim<int>` | (no inspeccionado) | tablas de roles globales | `lu_site_membership.role` texto | modelo destino multi-sede aprobado | F2 / F3 | NOT_APPLICABLE | F1-D007; global flag + membership role |

## Bloque N — Auditoria / Identity operaciones

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| N-01 | Identity | `_userManager.CreateAsync(user, password)` + `SynchronizeManagedRoleAsync` | `Pages/Users/Create.cshtml.cs`, `Helpers/IdentityRoleExtensions.cs` | crea user + asigna rol manejado | `users.repository.create`: `INSERT lu_user + INSERT lu_site_membership` (sin tablas de rol); sin helper de sincronizacion (no se reasignan roles globales) | sin rol global sincronizable | F4 | NOT_APPLICABLE | F8: F1-D007 (idem F-12). (antes CONFLICT) |
| N-02 | Identity | `_userManager.GetUserAsync(User)` para `CreatedById` | `Create.cshtml.cs`, `Edit.cshtml.cs`, `Delete.cshtml.cs` | identity user lookup | F3 persiste `created_by_user_id`/`modified_by_user_id`; F4 expone `ManagedUserRecord.createdBy/modifiedBy` en el detalle | ninguno | F4 | PARITY | actores de auditoria (F1-D013) |
| N-03 | Identity | `_userManager.SetLockoutEndDateAsync/MaxValue` | `Edit.cshtml.cs`, `Delete.cshtml.cs` | lockout permanente | F3: `account_status` inactive/deleted + rotacion de `security_version` + revocacion de sesiones en la misma transaccion; login y cada request lo rechazan | sin gap; mecanismo aprobado por F1 §11/§12 | F3 | PARITY | `03-HANDOFF.md` |
| N-04 | Identity | `_userManager.GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` | `Edit.cshtml.cs` | token efimero | sin token; `users.repository.update` actualiza `password_hash` directo con bcrypt | flujo simplificado | F4 | NOT_APPLICABLE | tokens email no implementados; F8: F1 §7/§12: reset administrativo escribe bcrypt, must_change_password=true, rota version y revoca sesiones; el token Identity interno no tiene equivalente. (antes PARTIAL) |
| N-05 | Identity | `_userManager.UpdateSecurityStampAsync` | `Edit.cshtml.cs`, `Delete.cshtml.cs` | invalida cookies | `security_version++` | equivalente | F3 / F4 | PARITY | |
| N-06 | Identity | `_signInManager.RefreshSignInAsync(userToUpdate)` | `Edit.cshtml.cs` | renueva cookie | n/a (sin cookies) | equivalente a re-login | F3 | NOT_APPLICABLE | sin cookies |
| N-07 | Identity | `_userManager.FindByNameAsync(user) ?? FindByEmailAsync(user)` en Login | `Login.cshtml.cs` | dual lookup | F3: `loginIdentifier` (username o email) resuelto con una sola busqueda en `lu_login_identifier` (espacio de claims unico y sin ambiguedad) | sin gap | F3 | PARITY | LC-02; `03-HANDOFF.md` |
| N-08 | Identity | Cookie `.ProyectoUnivalle.Auth.vUniversal` | `Program.cs` (cookie name) | cookie de sesion legacy | `auth.constants.ts`: `__Host-lu_session` | nombre y semantica de TTL distintos por arquitectura | F3 | CONFLICT | resuelto en F0; F8: cookie __Host-lu_session y TTL del modelo aprobado. |
| N-09 | Identity | Cambio del identificador invalida/renueva sesion propia | `Edit.cshtml.cs` actualiza security stamp y `RefreshSignInAsync` | cookies previas quedan reconciliadas | `user.repository.updateProfile` rota `security_version`, revoca otras sesiones y reemplaza la propia | sin sesiones previas tras cambio de email | F3 / F7 | PARITY | F1-D012; falta implementar reemplazo/revocacion; F8: cambio de email rota security_version, revoca otras sesiones y reemplaza la propia (F1 §12; F7 E2E profile). (antes CONFLICT) |
| N-10 | Identity | Politica coherente de longitud de password | `Program.cs`, `Create.cshtml.cs` | Identity aplica politica tras DataAnnotations | Create/Edit target usa 8-72 unidades UTF-16; login usa maximo 72 bytes UTF-8 | una password Unicode aceptada puede no autenticar | F3 / F4 / F5 / F7 | PARITY | F1-D006: 12 code points y max 72 bytes UTF-8; F8: politica unica 12 code points / 72 bytes UTF-8 en alta, reset, cambio propio y login (F1-D006; igual que E-08). (antes CONFLICT) |

## Bloque O — Visual / CSS clases

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| O-01 | CSS | `.customtab`, `.v-middle`, `.drop-shadow`, `.uppercase`, `.italic`, `.opacity-2`, `.border-3` | `Pages/Users/Index.cshtml` `<style>` final | reuso entre vistas | sin equivalentes (excepto `.uppercase` en `styles.css`?) | sin captura | F6 | PARITY | requiere captura selectiva; F6: reglas legacy portadas en `styles.css` bajo `.users-module` (border-3: 3 px Index, 4 px Edit/Delete) (antes PARTIAL) |
| O-02 | CSS | `.user-detail-avatar`, `.user-detail-initials`, `.user-detail-label`, `.user-detail-value` | `Pages/Users/Details.cshtml` `<style>` | selectores unicos | sin equivalentes | sin captura | F6 | PARITY | F6: `.user-detail-*` con los valores de `Details.cshtml` (antes MISSING) |
| O-03 | CSS | `.btn-circle`, `.btn-rounded`, `.bg-dark`, `.border-left-{info,warning,danger}` | `Pages/Users/*` | clases utility NiceAdmin | `styles.css` define otras reglas; no 1:1 | sin captura 1:1 | F6 | PARITY | F6: `.btn-circle`/`.btn-rounded` de `dist/css/style.min.css`; `bg-dark`/`border-left-*` y paleta dist bajo `.users-module` (antes PARTIAL) |
| O-04 | CSS | Selectores `.users-panel`, `.people-panel`, `.profile-panel` | `UsersPanel.tsx`, `PeoplePanel.tsx`, `ProfilePanel` | anaden variantes al `.catalog-panel` | selectores no definidos en `styles.css` | CSS sileciosa | F6 | PARITY | LC-06; F6: selectores silenciosos retirados; el alcance CSS es `.users-module` (antes MISSING) |
| O-05 | CSS | SweetAlert2 v7.19.3 (Uso: `result.value \|\| result.isConfirmed`) | `Pages/Shared/_Layout.cshtml` (script), `Pages/AGENTS.md` | toast global | sin uso de SweetAlert2 v7 | sin captura | F5 | NOT_APPLICABLE | B-08; F8: libreria SweetAlert2 reemplazada por el stack React (igual que O-06); la conducta toast se sigue en B-08. (antes MISSING) |
| O-06 | NiceAdmin | Paquete `NiceAdmin` Bootstrap 4 + jQuery + select2 + jqBootstrapValidation | `wwwroot/lib/`, `wwwroot/dist/` | stack legacy | React + Vite + TS sin jQuery; sin NiceAdmin directo | sin captura 1:1 | F6 | NOT_APPLICABLE | stack nuevo por plan |
| O-07 | Iconos | FontAwesome + Material-Design-Iconic-Font + Simple-Line-Icons + Themify-Icons + Weather-Icons | `wwwroot/dist/css/icons/*` | multiplicidad de fuentes | `ti-user`, `ti-lock`, `ti-close` en App.tsx (referenciados por CSS legacy); `mdi mdi-*` para sidebar | mix de fuentes | F6 | PARTIAL | serif requerido; F8: fuentes de iconos Simple-Line/Weather no cargadas. |
| O-08 | Imagenes | `logo-icon.png` / `logo-light-icon.png` / `favicon.png` | `wwwroot/assets/images/*` y `apps/web/src/App.tsx` (imports) | paridad via imports | `logoIcon`, `logoLightIcon` importados | paridad | F6 | PARITY | |

## Bloque P — Tests / QA

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| P-01 | Test | xUnit/NUnit (legacy?) | (no especificado en contexto; `context.md` dice "El repositorio no tiene un runner convencional xUnit, NUnit o MSTest") | sin suite oficial | `apps/api/test/*.e2e-spec.ts` + `apps/web/src/App.test.tsx` | suite moderna | F7 (e2e visual) | NOT_APPLICABLE | sin legacy runner |
| P-02 | Test | `apps/web/src/App.test.tsx` cubre Login, shell, sidebar, supervisors | `apps/web/src/App.test.tsx` | 6 tests | `UsersPanel.test.tsx` (vitest) + Playwright `tests/e2e/specs/{users.*,people}.spec.ts` contra el stack real | sin gap | F7 | PARITY | F7: ejecutado en vivo (ver "Estado F7") |
| P-03 | Test | `apps/api/test/auth.*.e2e-spec.ts` | `apps/api/test/auth.*.e2e-spec.ts` | cubre login/csrf/session/logout/rate-limit | `users.{controller,service,policy,repository,profile-photo}.e2e-spec.ts`, `people.*.e2e-spec.ts` + suites PostgreSQL opt-in ejecutadas en vivo | sin gap | F7 | PARITY | F7: `auth.postgres.integration` 10/10 en vivo |
| P-04 | Test | Matriz `MATRIZ_PARIDAD_ASP_REACT_NEST.md` ya existe | `docs/MATRIZ_PARIDAD_ASP_REACT_NEST.md` | 38KB | complementario; este 00-PARITY-MATRIX.md es F0-especifico | coexistentes | F5 | PARITY | la matriz global es otra fuente |
| P-05 | Test | Protecciones self, ultimo SuperAdmin y concurrencia | `Pages/Users/Edit.cshtml.cs`, `Delete.cshtml.cs` | reglas criticas del flujo | self-protection y RBAC negativos probados en navegador + servidor (`users.rbac.spec.ts`); ultimo SuperAdmin en unit/servicio/CLI y carrera concurrente serializada en vivo | sin prueba ejecutable de edicion concurrente (`row_version`) | F7 | PARTIAL | F7: queda solo la edicion concurrente; F8: sin contrato de concurrencia optimista (If-Match/row_version) en la API de usuarios; ediciones concurrentes se serializan por FOR UPDATE (ultimo escritor gana, como el formulario legacy); el invariante critico (ultimo SuperAdmin) esta probado en vivo bajo concurrencia. |

## Bloque Q — PostgreSQL migrations / registry

| ID | AREA | ASP FEATURE | ASP SOURCE | OBSERVABLE BEHAVIOR | TARGET CURRENT STATE | GAP | PHASE OWNER | STATUS | NOTES |
|---|---|---|---|---|---|---|---|---|---|
| Q-01 | Migration | Campos completos de `User` persistidos por EF/Identity | `Models/User.cs`, `Data/ApplicationDbContext.cs` | username, CI, telefono, foto, rol, estado, perfil laboral y auditoria | `0006_mig001_users_identity.sql` implementa offline el modelo canonico, crosswalk, auditoria y estado de migracion | falta validacion PostgreSQL real, carga de datos y backend posterior | F2 | PARITY | pin y gates offline verificados; aplicacion real pendiente; F8: 0006 aplicado y verificado en vivo (F2, F7); backend F3. La carga de datos legacy es prerequisito de cutover (F1-D020/D021), no brecha de paridad. (antes PARTIAL) |
| Q-02 | Migration | Permisos de gestion para Users | n/a | n/a | `0006` implementa roles, grants, RLS/ACL y ownership esperados; manifests y runner los verifican offline | catalogo y privilegios reales no fueron ejecutados | F2 | PARITY | implementado offline; validacion PostgreSQL pendiente; F8: roles/grants verificados en vivo (F2, F7); orden roles/003 protegido por guardas F8. (antes PARTIAL) |
| Q-03 | Registry | Migraciones posteriores ejecutables por tooling aprobado | n/a | n/a | registry pinneado procesa 6 migraciones control-plane, incluidas 0004-0006, con hash/bytes, analyzer, runner y CLI | ejecucion contra PostgreSQL permanece sin verificar | F2 | PARITY | capacidad estrecha de registry demostrada offline; no acredita aplicacion DB |
| Q-04 | Registry tenant | `tenant/0012_people.sql` existente | `Person`/Persons CRUD | Persons requiere schema tenant | stream tenant pinneado procesa 0001-0013 con manifest, ledger, lease global y runner | ejecucion tenant contra PostgreSQL permanece sin verificar | F2 | PARITY | capacidad estrecha de registry demostrada offline; no acredita aplicacion DB |

---

## Vinculacion de decisiones F1

F1 congela arquitectura pero no convierte gaps de implementacion en `PARITY`.
Las filas siguientes quedan enlazadas para sus fases propietarias:

| DECISION F1 | FILAS PRINCIPALES | IMPLEMENTATION OWNER |
|---|---|---|
| F1-D001 | M-04, Q-01 | F2 |
| F1-D002/F1-D003 | E-01..E-05, G-03..G-05, J-04, Q-01/Q-02 | F2/F3/F4/F5 |
| F1-D004 | A-01, E-07, F-03, N-07 | F2/F3/F4/F5 |
| F1-D005/F1-D006 | E-08, J-02, M-05, N-04, N-10 | F2/F3/F4/F5/F7 |
| F1-D007..F1-D011 | E-06/E-09, F-07/F-08/F-12, H-02/H-05/H-06/H-08, K-01/K-02/K-07, M-07 | F2/F3/F4/F5/F7 |
| F1-D012/F1-D013 | F-09, G-05, H-03/H-04, N-02/N-05/N-09 | F2/F3/F4/F5/F7 |
| F1-D014 | B-06, C-09, E-05, F-11, G-02/G-09, J-03 | F2/F3/F4/F5/F6/F7 |
| F1-D015/F1-D016 | D-01..D-07, I-12, K-06, L-01 | F3/F5/F7; resto Persons fuera de MIG-001 |
| F1-D017 | G-01..G-08, J-01/J-05 | F4/F5/F6/F7 |
| F1-D018/F1-D019 | F-02/F-07, H-02/H-05, K-01/K-07, N-09 | F3/F4/F5/F7 |
| F1-D020 | M-01..M-07, N-01..N-10, Q-01/Q-02 | F2/F3/data migration |
| F1-D021 | no dedicated F0 row; writer cutover contract added in F1 | cutover |
| F1-D022 | Q-03/Q-04 | F2 |

## Resumen de conteo

Estados contados sobre las filas anteriores. Las filas `UNKNOWN` cuentan como tales (sin
clasificar). Recuento:

- `PARITY`: **106**
- `PARTIAL`: **14**
- `MISSING`: **1**
- `CONFLICT`: **10**
- `NOT_APPLICABLE`: **27**
- `UNKNOWN`: **0**

> Total: **158** filas (recuento directo de la columna `STATUS` al cierre de F8;
> ver "Estado F8"). F1-D022 resolvio el contrato arquitectonico de Q-04;
> su implementacion sigue `MISSING` (L-03, busqueda People sin igualdad por Id;
> gap aceptado F8) y pertenece a F2. Ningun gap de codigo fue promovido a `PARITY`
> por documentacion; los ascensos a `PARITY` (F-02, F-05, F-06, F-07, N-09, N-10,
> Q-01, Q-02) se sostienen en evidencia de producto verificada en vivo (F2/F7).

---

## Leyenda de archivos / referencias validas

- ASP referenced paths se verifican con:
  `git ls-tree -r --name-only reference/asp-final -- Pages/Users Pages/Persons Pages/Login.cshtml Pages/Login.cshtml.cs Pages/Shared Models Helpers Data/ApplicationDbContext.cs Program.cs wwwroot`
- Target referenced paths se verifican con:
  `git ls-tree -r --name-only HEAD -- apps/web/src apps/api/src apps/api/migrations apps/api/test packages/contracts packages/api-client`
- Contratos: `packages/contracts/src/{users,auth,site,people}.ts` y
  `packages/api-client/src/index.ts`.
- Migraciones: `apps/api/migrations/0001_create_identity_control_plane.sql`,
  `apps/api/migrations/control-plane/0004_academic_catalogs.sql`,
  `apps/api/migrations/control-plane/0005_user_management.sql`,
  `apps/api/migrations/tenant/0012_people.sql`.
- Tests target: `apps/web/src/App.test.tsx` (vitest + Testing Library); `apps/api/test/auth.*.e2e-spec.ts`.

> Cualquier fila referenciando un archivo no listado aqui queda en categoria `UNKNOWN` por
> defecto hasta que se confirme en F1/F3/F5.

---

## Estado F2 (base de datos) — 2026-09-25

La capa de base de datos de la fase F2 (schema, ledger, roles/ACL,
manifests) fue aplicada y verificada en vivo contra un servicio PostgreSQL
18 Windows local (`127.0.0.1:5432`, base de mantenimiento `neondb`).
**DATABASE PARITY** queda `COMPLETE` para la porcion F2 de las filas
cuyos `PHASE OWNER` incluyen F2. Los propietarios restantes (F3, F4, F5,
F6, F7) conservan el `STATUS` de cada fila hasta el cierre de su fase.
Evidencia detallada en `docs/migration/users/02-F2-APPLIED.md` y handoff
en `docs/migration/users/02-HANDOFF.md`. Los conteos de `STATUS` y el
total `158` permanecen inalterados.

---
## Estado F3 (NestJS + Auth) — 2026-09-25

F3 implemento el backend de identidad, autenticacion, sesiones, RBAC,
administracion de usuarios y fotos de perfil (`03-HANDOFF.md`,
`03-SOLID-PATTERNS.md`). Solo cambiaron de `STATUS` las filas cuyo unico
`PHASE OWNER` es F3 y cuyo comportamiento quedo completo: **K-04**, **N-03**
(PARTIAL → PARITY) y **N-07** (CONFLICT → PARITY). Conteos actualizados:
PARITY 32, PARTIAL 30, MISSING 47, CONFLICT 36, NOT_APPLICABLE 13, UNKNOWN 0,
total 158.

Las filas compartidas con F4/F5/F6/F7 conservan su `STATUS` hasta que esas
fases cierren contratos/cliente/UI; su parte backend queda lista en F3:

| Filas | Parte backend entregada en F3 |
|---|---|
| A-01, E-07, F-03 | login por username o email; username inmutable en todas las rutas |
| E-01, E-08, N-10 | nombres separados validados; politica unica de password (12 code points, 72 bytes UTF-8) |
| E-05, F-11, J-03, B-06, G-09 | fotos de perfil: validacion extension/MIME/firma, 5 MiB, almacenamiento privado, ETag |
| F-02, E-09, K-02, K-06, K-07 | matriz de autorizacion global/sede, veto SuperAdmin, SuperAdmin sin sede solo en rutas globales |
| F-05, H-02, H-06, F-08, H-08 | estados de cuenta/membresia, revocacion por sede con cascada, restauracion, ultimo SuperAdmin activo bajo lock |
| F-09, H-03, H-04, N-05, N-09 | matriz de `security_version` (rotacion solo en cambios sensibles) y reemplazo de sesion propia |
| G-05 | actores de auditoria en el detalle administrativo; eventos append-only |
| I-12 | filtro explicito de personas eliminadas |
| J-02, J-04 | cambio de password propio (`POST /auth/password`); perfil propio con CI de solo lectura |
| M-05 | verificadores ASP.NET Identity V2/V3 y rehash a bcrypt |

Filas de F3 sin cambio por diferencias arquitectonicas ya aprobadas:
A-02, A-04, A-07, A-08, N-08.

---

## Estado F4 (contratos + API client) — 2026-09-26

F4 alineo `packages/contracts` y `packages/api-client` con el backend F3
(`04-HANDOFF.md`). Solo cambiaron de `STATUS` las filas cuyo gap cerraba en el
contrato y cuyos demas propietarios (F2/F3) ya cerraron: **E-02**, **E-04**,
**N-02** (MISSING → PARITY) y **E-13** (PARTIAL → PARITY). Conteos
actualizados: PARITY 36, PARTIAL 29, MISSING 44, CONFLICT 36,
NOT_APPLICABLE 13, UNKNOWN 0, total 158.

Las filas compartidas con F5/F6/F7 conservan su `STATUS`; la parte de
contrato/cliente queda lista para F5:

| Filas | Parte contrato/cliente entregada en F4 |
|---|---|
| A-01, E-07, F-03 | `LoginRequest.loginIdentifier` (usuario o correo); `ApiClient.signIn`; `username` en los registros |
| E-01, G-03 | nombres separados + `fullName`/`initials` derivados en `ManagedUserRecord`/`ProfileRecord`; CI, telefono, fechas |
| E-06, F-12, N-01 | roles solo por membresia (`MembershipRole`); `CreateManagedUserInput` global/sede; SuperAdmin fuera de todo payload |
| E-08, N-10 | `PASSWORD_POLICY` + `passwordPolicyViolations` compartidos, verificados contra `Utf8PasswordPolicy` |
| E-05, F-11, B-06, J-03 | rutas y cliente de foto (bytes crudos, `X-Photo-Filename`, `ProfilePictureRef.etag`, `PROFILE_PHOTO`) |
| F-05, H-02, F-06 | comandos separados: `setAccountStatus`, `deleteAccount`, `restoreAccount`, estado/vigencia/restauracion de membresia |
| G-05 | `createdBy`/`modifiedBy` en el detalle administrativo |
| J-02, J-04 | `changePassword` (`POST /auth/password`); `ownProfile`/`updateOwnProfile` sin CI editable |
| C-03 | `ManagedUserQuery.searchTerm` (F3 busca nombre, usuario, CI y correo); falta la UI (F5) |
| I-12 | `PersonQuery.statusFilter` explicito para eliminados |
| B-10 | `purpose` `site_selection` + `eligibleSites`; `selectActiveSite` |

Compatibilidad temporal (retirar en F5): tipos `Legacy*` en `@lu/contracts` y
metodos `@deprecated` del cliente (`login`, `session`, `setActiveSite`,
`users`, `user`, `createUser`, `updateUser`, `deleteUser`, `profile`,
`updateProfile`) usados por `App.tsx`/`UsersPanel.tsx` sin migrar. El alias de
backend `email` en `POST /auth/login` se conserva (lo usa
`tests/e2e/deployed-auth.js`); el cliente ya envia `loginIdentifier`.

Sin cambio por pertenecer al backend y no al contrato: **J-05** (el perfil
propio `GET /profile` no incluye actores de auditoria; el detalle
administrativo si). Queda para decision de F5/F7.

---

## Estado F5 (paridad funcional React) — 2026-09-26

F5 migro `apps/web` al contrato/cliente canonico F4 (`05-HANDOFF.md`). Solo
cambiaron de `STATUS` filas cuyo `PHASE OWNER` no incluye F6 ni F7 y cuyo
requisito **funcional** quedo cumplido; cada fila modificada lleva en `NOTES`
el texto `F5: ... (antes X)`.

- CONFLICT → PARITY (13): A-01, B-04, C-01, E-01, E-06, E-07, E-08, G-03,
  H-02, I-12, J-01, K-06, K-07.
- MISSING → PARITY (11): C-02, E-05, E-10, F-03, G-04, G-05, H-08, I-08,
  J-02, J-03, J-04.
- PARTIAL → PARITY (6): C-03, C-04, C-06, E-17, F-14, K-02.
- MISSING → PARTIAL (7): C-14, D-05, E-16, F-13, G-10, H-07, I-09.
- CONFLICT → PARTIAL (1): D-06.

Conteos por recuento directo de la tabla: PARITY 68, PARTIAL 31, MISSING 24,
CONFLICT 22, NOT_APPLICABLE 13, UNKNOWN 0, total 158.

Nota de conciliacion: el recuento de la tabla inmediatamente antes de F5 era
PARITY 38, PARTIAL 29, MISSING 42, CONFLICT 36, NOT_APPLICABLE 13 (158). El
resumen F4 declaraba PARITY 36 / MISSING 44; la diferencia de 2 filas es previa
a F5. Ademas la fila I-01 tenia un `|` sin escapar dentro de codigo que la
partia en 13 celdas; F5 solo escapo ese caracter (su `STATUS` no cambio).

Filas F5 sin cambio de `STATUS` y motivo:

| Filas | Motivo |
|---|---|
| B-08, O-05, H-10, F-15 | sin toast/SweetAlert2 global ni confirmacion previa al guardar Edit; el feedback es inline |
| E-11 | sin bloqueo de caracteres en nombres (validacion de servidor autoritativa) |
| D-01 | el directorio conserva alta inline de PeoplePanel (sin pagina Crear separada) |
| I-07, L-03 | orden `name ASC` y busqueda sin igualdad por Id son del backend de Personas |
| I-10 | auditoria de Personas fuera de MIG-001 (F1-D016) |
| B-07, I-06, J-06, L-01, P-04 | ya en PARITY (B-07 ahora enlaza `/Profile`, F1-D017) |
| E-09, E-12, K-05 | NOT_APPLICABLE justificado previamente |

Filas compartidas con F6/F7 con su parte funcional entregada en F5 (sin cambio
de `STATUS` hasta el cierre de esas fases): B-06 (avatar con foto en topbar),
C-12 (Editar/Detalles/Eliminar), C-13 (boton Vincular Usuario → `/Users/Create`),
D-07 (Personas: Editar/Baja; sin Detalles), F-01 (formulario de edicion sin
sidebar oscuro), F-11 (subir/quitar foto con vista previa en la ficha), G-01
(`/Users/Details/:userId` administrativo), G-07 (Modificar Perfil / Volver al
Listado / Revocar Acceso), G-09 (cache-busting por `etag`), H-01
(`/Users/Delete/:userId` con confirmacion), N-10 (politica unica en UI). P-02
(F7): existen pruebas de UsersPanel/PeoplePanel (`UsersPanel.test.tsx`).

J-05 (actores de auditoria en `GET /profile`) permanece abierto: `/Profile` no
muestra actores porque el backend no los expone; no se inventan datos.

---

## Estado F6 (paridad visual React) — 2026-09-26

F6 reprodujo en `apps/web` la maqueta de `Pages/Users/*`, `_PersonsTable`,
`UserProfile/Default` y la cabecera de `_Layout` (`06-HANDOFF.md`). Solo
cambiaron filas cuyo requisito visual quedo cumplido con evidencia (capturas
React vs. marcado Razor renderizado con `wwwroot/dist/css/style.min.css`); cada
fila modificada lleva en `NOTES` el texto `F6: ... (antes X)`.

- MISSING → PARITY (14): B-06, C-05, C-09, C-11, F-01, F-11, G-02, G-06, G-07,
  G-09, H-01, H-11, O-02, O-04.
- CONFLICT → PARITY (7): B-05, C-08, C-10, C-12, C-13, D-03, G-01.
- PARTIAL → PARITY (7): B-11, C-07, D-04, D-05, G-08, O-01, O-03.
- MISSING → PARTIAL (1): D-07.

Conteos por recuento directo de la tabla (158 filas): PARITY 96, PARTIAL 25,
MISSING 9, CONFLICT 15, NOT_APPLICABLE 13, UNKNOWN 0. Recuento previo (cierre
F5) verificado igual al declarado: 68 / 31 / 24 / 22 / 13. Las tablas auxiliares
de dos columnas de las secciones F3–F5 no son filas de la matriz y no se cuentan.

D-05 no lista F6 como propietario, pero su unico residuo tras F5 era visual (el
aviso se mostraba con el directorio vacio); se cerro y se deja constancia aqui.

Filas con parte visual que F6 no cierra, y motivo:

| Filas | Motivo |
|---|---|
| B-08, C-14, E-16, F-13, H-07, O-05 | sin toast global SweetAlert2; los mensajes siguen como alerta inline (brecha funcional F5) |
| F-15, H-10 | sin confirmacion SweetAlert previa; Delete usa su pagina y los comandos de membresia `window.confirm` |
| B-12 | `HidePageTitle` sin uso en Users/Persons; no se porto el interruptor |
| D-01 | cabecera y boton "Nueva Identidad" legacy, pero el alta sigue en linea (sin `/Persons/Create`) |
| D-06 | busqueda/estado compartidos; paginacion independiente por tab |
| D-07 | falta "Detalles" de Persona (no existe ficha, I-10) |
| E-11 | bloqueo de letras en nombres no portado |
| G-10 | sin pagina `/Error` dedicada |
| I-09, L-03 | la busqueda de Personas no compara por Id |
| J-05 | `GET /profile` no expone actores de auditoria; `/Profile` no muestra tarjeta de Auditoria |
| O-07 | iconos de las superficies MIG-001 salen de los mismos assets (FA 5.0.9, MDI, Themify); Simple-Line/Weather no se cargan en el target |

---

## Estado F7 (E2E + validacion PostgreSQL en vivo) — 2026-09-27

Transiciones (solo filas cuya verificacion es el objeto de F7):

| ID | Antes | Despues | Evidencia |
|---|---|---|---|
| P-02 | MISSING | PARITY | vitest `UsersPanel.test.tsx` + Playwright users/people contra el stack real |
| P-03 | MISSING | PARITY | suites users/people de `apps/api` + suites PostgreSQL opt-in 10/10 en vivo |
| P-05 | MISSING | PARTIAL | self-protection/RBAC negativos en vivo; ultimo SuperAdmin en unit/servicio/CLI; falta edicion concurrente |

Sin cambio de `STATUS` (gaps de producto conocidos, no se promueven por tener prueba):
global toast/confirmacion previa (B-08, C-14, E-16, F-13, F-15, H-07, H-10, O-05), E-11, D-01,
D-07/I-10, I-09/L-03, G-10, J-05, F-08/H-06 (CONFLICT aprobado por F1-D008).

Hallazgos F7 abiertos (no promovidos; decision F8): restauracion de membresia revocada por
el Administrador de sede inalcanzable desde la UI (`canViewUser` oculta la ficha; F1 §11 la
permite), SuperAdmin sin membresias no puede elegir sede en Crear, mensaje 400 del servidor sin
traducir en ese caso. Detalle en `07-HANDOFF.md`.

Recuento directo al cierre de F7: PARITY 98, PARTIAL 26, MISSING 6, CONFLICT 15,
NOT_APPLICABLE 13, UNKNOWN 0 — total 158.

---

## Estado F8 (reconciliacion final) — 2026-09-27

F8 cierra la paridad funcional de MIG-001 con adjudicacion final sobre los gaps abiertos
tras F7 y con la entrega de los cambios de producto identificados como prerequisito. Las
transiciones siguientes son la unica fuente de verdad para `STATUS`; se aplican las notas
F8 tal como aparecen en `NOTES` (apendice, no reemplazo).

### Transiciones F8

| ID | Antes | Despues | Motivo |
|---|---|---|---|
| A-02 | PARTIAL | CONFLICT | modelo de sesion aprobado (idle 30 min / absoluto 12 h, rememberMe absoluto) en lugar de cookie sliding 8 h; decision F8: se conserva. |
| A-04 | PARTIAL | CONFLICT | sin pagina Lockout; buckets email_ip/ip y 429 inline; decision F8: se conserva (misma familia que A-08). |
| B-12 | PARTIAL | NOT_APPLICABLE | HidePageTitle no se usa en ninguna superficie MIG-001; no hay comportamiento que portar. |
| E-11 | PARTIAL | CONFLICT | decision F8: no se bloquean caracteres; F1 §5 no exige solo letras y el bloqueo legacy rechazaria nombres validos (guion, apostrofo, letras Unicode). El servidor normaliza y valida longitud. |
| F-02 | PARTIAL | PARITY | Administrador y SuperAdmin administran; Supervisor no (igual que AdminRoles legacy); veto sobre SuperAdmin ajeno (F1 §8). |
| F-05 | CONFLICT | PARITY | inactivacion = account_status + rotacion security_version + revocacion de sesiones; mismo efecto observable que Lockout MaxValue (igual criterio que H-03). |
| F-06 | PARTIAL | PARITY | reactivar devuelve el acceso y rota security_version; AccessFailedCount no existe en el target (A-08). |
| F-07 | PARTIAL | PARITY | F1 §9: sin cambio de rol ni inactivacion propia, como legacy. |
| F-12 | CONFLICT | NOT_APPLICABLE | F1-D007: rol por membresia; no hay rol Identity que sincronizar (igual que M-07). |
| F-15 | MISSING | NOT_APPLICABLE | LC-01: la confirmacion SweetAlert previa solo existe en guias (context.md, Pages/AGENTS.md); el ASP ejecutable envia directo. El target reproduce el comportamiento ejecutable. |
| H-10 | CONFLICT | NOT_APPLICABLE | LC-01 (idem F-15): el Delete legacy es su pagina de confirmacion (H-01 PARITY); los comandos de membresia usan window.confirm adicional. |
| I-01 | PARTIAL | NOT_APPLICABLE | F1-D016: el tab reutiliza la API/panel de People sin rediseño; actorCode pertenece al modelo People. |
| I-03 | PARTIAL | NOT_APPLICABLE | F1-D016 / F1 §15: estados/persistencia de subtipo fuera de MIG-001. |
| I-04 | PARTIAL | NOT_APPLICABLE | F1-D016 / F1 §15 (idem). |
| I-10 | MISSING | NOT_APPLICABLE | F1-D016: ficha/auditoria de Persona fuera de MIG-001 (solo el tab reutilizado). |
| I-11 | MISSING | NOT_APPLICABLE | F1 §15: concurrencia optimista de Persona e ImportBatchId fuera de MIG-001. |
| J-05 | MISSING | PARTIAL | los actores de auditoria del propio usuario son visibles en la ficha administrativa (/Users/Details/:id, G-05) para Administrador/SuperAdmin, como legacy; /Profile no los expone (el backend GET /profile no los devuelve; no se inventan). |
| M-03 | CONFLICT | NOT_APPLICABLE | F1-D016: esquema People (TPT vs tabla plana) no se rediseña en MIG-001. |
| M-05 | CONFLICT | NOT_APPLICABLE | F1-D005: bcrypt + verificadores ASP.NET Identity V2/V3 con rehash; los usuarios conservan su password (probado en vivo F7). |
| N-01 | CONFLICT | NOT_APPLICABLE | F1-D007 (idem F-12). |
| N-04 | PARTIAL | NOT_APPLICABLE | F1 §7/§12: reset administrativo escribe bcrypt, must_change_password=true, rota version y revoca sesiones; el token Identity interno no tiene equivalente. |
| N-09 | CONFLICT | PARITY | cambio de email rota security_version, revoca otras sesiones y reemplaza la propia (F1 §12; F7 E2E profile). |
| N-10 | CONFLICT | PARITY | politica unica 12 code points / 72 bytes UTF-8 en alta, reset, cambio propio y login (F1-D006; igual que E-08). |
| O-05 | MISSING | NOT_APPLICABLE | libreria SweetAlert2 reemplazada por el stack React (igual que O-06); la conducta toast se sigue en B-08. |
| Q-01 | PARTIAL | PARITY | 0006 aplicado y verificado en vivo (F2, F7); backend F3. La carga de datos legacy es prerequisito de cutover (F1-D020/D021), no brecha de paridad. |
| Q-02 | PARTIAL | PARITY | roles/grants verificados en vivo (F2, F7); orden roles/003 protegido por guardas F8. |

### Filas conservadas (decisiones F8 explicitas, sin cambio de `STATUS`)

| ID | STATUS | Decision F8 |
|---|---|---|
| A-07 | PARTIAL | mensaje generico con otra redaccion ("Usuario o contrasena incorrectos."). |
| A-08 | CONFLICT | buckets por identificador+IP en vez de AccessFailedCount por usuario (aprobado). |
| B-08 | PARTIAL | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| C-14 | PARTIAL | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| D-01 | CONFLICT | alta de Persona inline (F1-D016, sin /Persons/Create). |
| D-06 | PARTIAL | busqueda/estado compartidos; paginacion independiente por tab. |
| D-07 | PARTIAL | Editar/Eliminar; sin Detalles de Persona (F1-D016). |
| E-16 | PARTIAL | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| F-08 | CONFLICT | F1-D008 (rol SuperAdmin solo por CLI; ultimo SuperAdmin activo bajo lock). |
| F-13 | PARTIAL | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| G-10 | PARTIAL | 404/403 tipados inline con Volver al Listado; sin pagina /Error (desviacion aceptada F8). |
| H-06 | CONFLICT | F1-D008 (rol SuperAdmin solo por CLI; ultimo SuperAdmin activo bajo lock). |
| H-07 | PARTIAL | mensajes con el texto legacy como alerta inline; sin toast global (desviacion aceptada F8). |
| H-09 | PARTIAL | error visible en la pagina Delete; sin redireccion de reintento. |
| I-07 | CONFLICT | orden People name ASC (API reutilizada, F1-D016). |
| I-09 | PARTIAL | busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |
| L-02 | CONFLICT | nombre unico de Persona (modelo People reutilizado). |
| L-03 | MISSING | busqueda People sin igualdad por Id (gap aceptado F8; F1-D016 no lo exige). |
| N-08 | CONFLICT | cookie __Host-lu_session y TTL del modelo aprobado. |
| O-07 | PARTIAL | fuentes de iconos Simple-Line/Weather no cargadas. |
| P-05 | PARTIAL | sin contrato de concurrencia optimista (If-Match/row_version) en la API de usuarios; ediciones concurrentes se serializan por FOR UPDATE (ultimo escritor gana, como el formulario legacy); el invariante critico (ultimo SuperAdmin) esta probado en vivo bajo concurrencia. |

### Recuento directo al cierre de F8

Recuento directo sobre la columna `STATUS` (158 filas, verificado por script):
PARITY 106, PARTIAL 14, MISSING 1, CONFLICT 10, NOT_APPLICABLE 27, UNKNOWN 0 — total 158.

### Cambios de producto F8

- Restauracion de una membresia revocada desde la ficha administrativa: el Administrador
  de la sede activa puede restaurar una membresia de su propia sede en estado `revoked`
  desde `/Users/Details/:id` (F1 §11). Cierra el hallazgo F7 de la UI que ocultaba la
  ficha para usuarios revocados.
- Rate-limit por identificador en `POST /auth/password` (voluntario y forzado por
  proposito): mismo bucket `email_ip` / `ip` que el login; cierra el riesgo de enumeracion
  o de abuso del endpoint de cambio de password.
- Alias `email` eliminado en backend `POST /auth/login`: solo `loginIdentifier` (usuario o
  correo) es aceptado. Compatibilidad temporal retirada (F4 la marcaba como deprecada).
- SuperAdmin sin sede elegible: la UI de Create ahora muestra un aviso veraz (membresias
  inactivas / sin sede elegible) en lugar de un 400 crudo del servidor. No se inventa
  ninguna sede.
- Mensaje de exito tras cambio de password (`Contrasena actualizada correctamente.`)
  propagado como flash inline en `/Profile` (consistente con B-08).
- Guardas de cutover para el orden de `roles/003` en el registry de migraciones:
  el runner rechaza aplicar `roles/003` antes de las migraciones que crean los roles que
  ese SQL asume; protege Q-02 en vivo.

### Garantia de no-promocion por documentacion

Ningun gap de codigo fue promovido a `PARITY` por documentacion. Las transiciones a
`PARITY` (F-02, F-05, F-06, F-07, N-09, N-10, Q-01, Q-02) se sostienen en evidencia de
producto verificada en vivo (F2/F7) o en el codigo observable de F8; las decisiones F8
solo formalizan o reorientan el `STATUS` sin alterar el alcance del producto. Las filas
clasificadas como `NOT_APPLICABLE` lo son por decision arquitectonica aprobada (F1-D007,
F1-D016, F1 §15, F1 §5) y no por omision.
