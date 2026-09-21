# Matriz de paridad ASP.NET → React + NestJS

**Trabajo:** `UI-MIG-2026-09-20-01`
**Fuente de verdad:** rama y working tree actuales de `RamaDani`
**Regla:** una fila solo pasa a `Verificado` con UI, API, persistencia, autorización, pruebas y evidencia local.

**Ejecución vigente (21-09-2026):** PostgreSQL directo por vertical; dashboard, Países/Ciudades, organización académica, definiciones de Equipos, unidades físicas, gestiones y L-6 ya tienen contratos, API, persistencia y UI React por su ámbito. Cada fase se cierra con evidencia verificable. El runtime tenant se resuelve server-side y el sandbox local conserva únicamente fixtures reversibles.

**Directiva del usuario (21-09-2026):** las fases quedan autorizadas para ejecutarse de forma continua, sin pausas de aprobación entre ellas. Se mantienen las restricciones de no escribir SQL Server/Neon, no modificar `0001–0003`, no hacer despliegue/commit y no marcar una fila como `Verificado` sin evidencia completa. El runtime tenant local sigue siendo un prerrequisito técnico.

## Leyenda

- `Inventariado`: contrato legacy identificado; todavía no migrado.
- `Implementado`: existe equivalente nuevo, pendiente de alguna puerta de validación.
- `Verificado`: pasó las puertas indicadas en esta matriz.
- `Bloqueado`: falta una decisión o dependencia explícita.

## Shell y componentes globales

| Superficie ASP.NET | Fuente                                           | Assets/comportamiento                                                                 | Equivalente React/NestJS                                          | Estado       |
| ------------------ | ------------------------------------------------ | ------------------------------------------------------------------------------------- | ----------------------------------------------------------------- | ------------ |
| Layout principal   | `Pages/Shared/_Layout.cshtml`                    | topbar azul, marca, búsqueda, campana, perfil, sidebar, breadcrumb, contenido, footer | `apps/web/src/App.tsx` + `styles.css`                             | Implementado |
| Sidebar            | `Pages/Shared/_Sidebar.cshtml`                   | secciones, grupos colapsables e iconos MDI/FA                                         | navegación tipada y filtrada por rol                              | Implementado |
| Perfil             | `UserProfileViewComponent.cs` + `Default.cshtml` | nombre, correo, iniciales, perfil y logout                                            | sesión entrega `displayName`/`email`; menú React                  | Implementado |
| Login              | `Pages/Login.cshtml(.cs)`                        | tarjeta split, usuario/correo, contraseña, recordar, rate limit                       | `POST /api/v1/auth/login` + vista React                           | Implementado |
| Sesión             | Identity cookie + sesión ASP.NET                 | login protegido, sesión, expiración                                                   | cookie HttpOnly + `GET /api/v1/auth/session`                      | Implementado |
| Sede activa        | no existe como selector global en Razor          | contexto multisedes nuevo                                                             | `PUT /api/v1/auth/session/active-site` + `GET /api/v1/context`    | Implementado |
| Logout             | `Pages/Logout.cshtml.cs::OnPost`                 | limpia sesión/cookie y redirige                                                       | `POST /api/v1/auth/logout` + retorno al login                     | Implementado |
| Feedback global    | TempData en `_Layout.cshtml`                     | Success/Error/Warning/Info; SweetAlert2 v7                                            | contrato de feedback React pendiente                              | Inventariado |
| Notificaciones     | `_Layout.cshtml` + `Pages/Api/Notifications`     | campana, grupos, lectura y descarte                                                   | `GET /api/v1/notifications` + comandos de lectura y campana React | Implementado |
| Wizard global      | `_WizardSteps.cshtml`, `_WizardStep.cshtml`      | pasos, ManagementId y filtros                                                         | sin equivalente todavía                                           | Inventariado |

## Menú y permisos

La vista Razor muestra todos los enlaces, pero la autorización efectiva está en los PageModels. React usa esta autorización efectiva para no ofrecer rutas que el rol no puede abrir.

| Sección / enlace           | Ruta Razor                                     | Permiso efectivo                      | Ruta React                     | Estado                                                                          |
| -------------------------- | ---------------------------------------------- | ------------------------------------- | ------------------------------ | ------------------------------------------------------------------------------- |
| Inicio · Ver               | `/AssetView/Index`                             | Supervisor, Administrador, SuperAdmin | igual                          | Inventario visual y Kardex operativo implementados                              |
| Operación · Resumen        | `/LaboratoryOperations/Index`                  | autenticado                           | igual                          | Panel React con métricas, gestión activa, estados vacíos y datos tenant reales  |
| Operación · Planificación  | `/LaboratoryOperations/Planificacion`          | autenticado                           | igual                          | Panel React con planificación de activos y fase actual desde PostgreSQL         |
| Operación · Requerimientos | `/LaboratoryOperations/Requerimientos`         | autenticado                           | igual                          | Panel React con solicitudes/activos disponibles; sin eventos simulados          |
| Operación · Disponibilidad | `/LaboratoryOperations/Disponibilidad`         | autenticado                           | igual                          | Panel React con disponibilidad derivada del inventario y estado vacío explícito |
| Operación · L-3 Devolución | `/LaboratoryOperations/L3`                     | autenticado                           | igual                          | Panel React enlazado a la operación L-3 y datos reales de la sede               |
| Operación · Incidentes L-5 | `/LaboratoryOperations/L5`                     | autenticado                           | igual                          | Panel React enlazado a solicitudes correctivas; sin contenido inventado         |
| Operación · Demanda        | `/LaboratoryOperations/Demanda`                | autenticado                           | igual                          | Panel React con métricas tenant y estado vacío cuando no hay movimientos        |
| Operación · Calendarios    | `/LaboratoryOperations/Calendario`             | autenticado                           | igual                          | Panel React con fechas reales disponibles y estado vacío explícito              |
| Procesos · Gestiones       | `/Managements/Index?type=Preventive`           | Administrador, SuperAdmin             | igual                          | CRUD, activación, cierre y planificación implementados                          |
| Procesos · Preventivos     | `/`                                            | autenticado                           | igual                          | Dashboard real implementado; pendiente E2E con usuario inicial                  |
| Procesos · Correctivos     | `/Managements/Index?handler=CurrentCorrective` | Administrador, SuperAdmin             | igual                          | Shell implementado; pantalla inventariada                                       |
| Equipo                     | `/Equipment/Index`                             | Administrador, SuperAdmin             | igual                          | CRUD tenant, unidades físicas e historial implementados                         |
| Verificaciones L-6         | `/Verifications/Index`                         | Administrador, SuperAdmin             | igual                          | CRUD individual/masivo, borradores y transición de plan implementados           |
| Solicitudes L-7            | `/Requests/Index`                              | Supervisor, Administrador, SuperAdmin | igual                          | CRUD tenant, borrador, filtros y transición L-8 implementados                   |
| Adquisiciones              | `/Acquisitions/Index`                          | Administrador, SuperAdmin             | igual                          | CRUD tenant, costos, borradores y transición L-12 implementados                 |
| Mantenimiento L-8          | `/Maintenances/Index`                          | Administrador, SuperAdmin             | igual                          | CRUD tenant, tareas, costos y transición L-3 implementados                      |
| Salidas L-3                | `/Departures/Index`                            | Administrador, SuperAdmin             | igual                          | Salida masiva, devolución y transición a Kardex implementadas                   |
| Catálogo · Laboratorios    | `/Laboratories/Index`                          | Administrador, SuperAdmin             | igual                          | Shell implementado; pantalla inventariada                                       |
| Catálogo · Regiones        | `/Cities/Index`                                | Administrador, SuperAdmin             | igual                          | Shell implementado; pantalla inventariada                                       |
| Catálogo · Países          | `/Countries/Index`                             | Administrador, SuperAdmin             | `/Cities/Index` (panel Países) | CRUD tenant completo; pendiente E2E navegador                                   |
| Catálogo · Ciudades        | `/Cities/Index`                                | Administrador, SuperAdmin             | `/Cities/Index`                | CRUD tenant completo; pendiente E2E navegador                                   |
| Catálogo · Facultades      | `/Faculties/Index`                             | Administrador, SuperAdmin             | `/Laboratories/Index`          | API control plane + selector operativo; pendiente E2E navegador                 |
| Catálogo · Carreras        | `/Career/Index`                                | Administrador, SuperAdmin             | `/Laboratories/Index`          | API control plane + SiteCareer; pendiente E2E navegador                         |
| Catálogo · Laboratorios    | `/Laboratories/Index`                          | Administrador, SuperAdmin             | `/Laboratories/Index`          | CRUD tenant completo; pendiente E2E navegador                                   |
| Personas · Usuarios        | `/Users/Index`                                 | Administrador, SuperAdmin             | igual                          | Gestión de cuentas, membresías, roles y baja lógica implementada                |
| Personas · Directorio      | `/Persons/Index`                               | Administrador, SuperAdmin             | igual                          | CRUD tenant de personas internas/externas, filtros y baja lógica implementados  |
| Reportes oficiales         | `/Reports/Index`                               | Supervisor, Administrador, SuperAdmin | igual                          | Centro React y descargas Excel server-side; paridad archivo-a-archivo pendiente |

## Correspondencia de la primera vertical

| Razor Page / componente | PageModel / handler      | Entidades/servicios legacy              | Endpoint NestJS                                      | Servicio / repositorio                               | Pantalla React                      |
| ----------------------- | ------------------------ | --------------------------------------- | ---------------------------------------------------- | ---------------------------------------------------- | ----------------------------------- |
| `/Login` GET            | `LoginModel.OnGetAsync`  | Identity external signout               | `GET /api/v1/auth/session` y `GET /api/v1/auth/csrf` | `AuthService`, `AuthRepository`                      | estado inicial/login                |
| `/Login` POST           | `LoginModel.OnPostAsync` | `UserManager`, `SignInManager`, lockout | `POST /api/v1/auth/login`                            | `AuthService.login`, `AuthRepository`                | formulario split                    |
| sede activa (nuevo)     | no aplica                | contrato multisedes PostgreSQL          | `PUT /api/v1/auth/session/active-site`               | `AuthService.setActiveSite`                          | selector y dropdown de sede         |
| contexto de solicitud   | `ICurrentUserService`    | claims de usuario                       | `GET /api/v1/context`                                | guard/core context                                   | shell y permisos                    |
| `/Logout` POST          | `LogoutModel.OnPost`     | `SignInManager.SignOutAsync`            | `POST /api/v1/auth/logout`                           | `AuthService.logout`, `AuthRepository.revokeSession` | menú de perfil                      |
| `_Layout`               | Razor layout             | ViewData, TempData, UserProfile         | sesión + contexto anteriores                         | auth/core                                            | header, sidebar, breadcrumb, footer |

## Correspondencia Fase 2 — Países y Ciudades

| Razor Page                                                | Handlers                                                     | Endpoint NestJS                                                      | Persistencia                               | React                                                 |
| --------------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------- | ------------------------------------------ | ----------------------------------------------------- |
| `/Countries/Index`, `Create`, `Edit`, `Details`, `Delete` | listado, alta, edición, detalle, baja lógica                 | `GET/POST /api/v1/countries`, `GET/PUT/DELETE /api/v1/countries/:id` | `lu_country` tenant, estado y auditoría    | panel de catálogo dentro de `/Cities/Index`           |
| `/Cities/Index`, `Create`, `Edit`, `Details`, `Delete`    | filtros por ciudad/país, alta, edición, detalle, baja lógica | `GET/POST /api/v1/cities`, `GET/PUT/DELETE /api/v1/cities/:id`       | `lu_city` tenant, unicidad por país y sede | tabla, formulario, filtros, paginación y confirmación |

La sede y el usuario se derivan de `SiteRequestContext`; los DTO no aceptan `siteId`, DSN ni tenant desde el navegador. Las operaciones de escritura exigen rol `Administrador` en la sede activa.

## Correspondencia Fase 3 — Organización académica y laboratorios

| Superficie      | Persistencia                   | Endpoint NestJS        | Regla preservada                                                  |
| --------------- | ------------------------------ | ---------------------- | ----------------------------------------------------------------- |
| Facultades      | control plane `lu_faculty`     | `/api/v1/faculties`    | catálogo global, baja lógica y códigos únicos                     |
| Carreras        | control plane `lu_career`      | `/api/v1/careers`      | facultad opcional, baja lógica y relación `SiteCareer`            |
| Oferta por sede | control plane `lu_site_career` | `/api/v1/site-careers` | asignación por sede sin JOIN cross-DB                             |
| Laboratorios    | tenant `lu_laboratory`         | `/api/v1/laboratories` | código único dentro de la sede y facultad opaca del control plane |

## Correspondencia Fase 6 — Unidades e inventario visual

| Superficie                 | Persistencia                             | Endpoint NestJS                       | Regla preservada                                                             |
| -------------------------- | ---------------------------------------- | ------------------------------------- | ---------------------------------------------------------------------------- |
| Unidades físicas           | tenant `lu_equipment_unit`               | `/api/v1/equipment-units`             | inventario único por sede, laboratorio obligatorio en altas y baja lógica    |
| Historial de estados       | tenant `lu_equipment_unit_state_history` | `/api/v1/equipment-units/:id/history` | cierre del estado anterior y nuevo registro ante cambios de estado/condición |
| Inventario sin laboratorio | filas legacy con `laboratory_id` nulo    | filtro `includeUnresolved=true`       | queda fuera de operación normal; no se asigna sede por heurística            |

## Correspondencia L-6 — Verificaciones

| Superficie              | Persistencia                                                         | Endpoint NestJS                         | Regla preservada                                                                        |
| ----------------------- | -------------------------------------------------------------------- | --------------------------------------- | --------------------------------------------------------------------------------------- |
| Verificación individual | tenant `lu_verification`                                             | `GET/POST /api/v1/verifications`        | asociación explícita a gestión/unidad, estado de borrador y resultado con observaciones |
| Checklist               | tenant `lu_verification_check_item` + `lu_verification_check_result` | `GET /api/v1/verifications/check-items` | resultados por punto de control, sin columnas hardcodeadas                              |
| Fallas                  | tenant `lu_verification_fault`                                       | incluida en alta/edición                | baja lógica y transición del plan a L-7                                                 |
| Verificación masiva     | mismas tablas                                                        | `POST /api/v1/verifications/mass`       | lotes limitados, retorno por unidad y limpieza reversible                               |

## Correspondencia L-7 — Solicitudes

| Superficie           | Persistencia                       | Endpoint NestJS                                                          | Regla preservada                                                                         |
| -------------------- | ---------------------------------- | ------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Solicitudes técnicas | tenant `lu_request`                | `GET/POST /api/v1/requests`, `GET/PUT /api/v1/requests/:id`              | filtros, prioridad, estados, fecha y datos técnicos con validación de unidad/laboratorio |
| Unidades afectadas   | tenant `lu_request_equipment_unit` | incluido en detalle de solicitud                                         | relación normalizada, histórico conservado y baja lógica de la solicitud                 |
| Comandos del wizard  | tenant `lu_management_plan`        | `POST /api/v1/requests/:id/complete`, `POST /api/v1/requests/:id/cancel` | borrador L-7 y transición explícita a L-8; sin transacción cross-DB                      |

## Correspondencia L-8 — Mantenimiento y L-48

| Superficie          | Persistencia                 | Endpoint NestJS                                                                  | Regla preservada                                           |
| ------------------- | ---------------------------- | -------------------------------------------------------------------------------- | ---------------------------------------------------------- |
| Mantenimientos      | tenant `lu_maintenance`      | `GET/POST /api/v1/maintenances`, `GET/PUT /api/v1/maintenances/:id`              | unidad, gestión, calendario, estado, costos y satisfacción |
| Tareas técnicas     | tenant `lu_maintenance_task` | incluidas en alta/edición/detalle                                                | cuatro pasos L-48, avance porcentual y baja lógica         |
| Costos              | tenant `lu_maintenance_cost` | incluidos en alta/edición/detalle                                                | cantidades, precios, categorías e historial no destructivo |
| Comandos del wizard | tenant `lu_management_plan`  | `POST /api/v1/maintenances/:id/complete`, `POST /api/v1/maintenances/:id/cancel` | borrador L-8, finalización validada y transición a L-3     |

## Correspondencia L-3 — Salidas y devoluciones

| Superficie        | Persistencia                                | Endpoint NestJS                                                 | Regla preservada                                                         |
| ----------------- | ------------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Salida individual | tenant `lu_departure`                       | `GET/POST /api/v1/departures`, `GET/PUT /api/v1/departures/:id` | gestión, unidad, responsable, fechas, destino y estado                   |
| Salida masiva     | tenant `lu_departure` + `lu_departure_item` | `POST /api/v1/departures/mass`                                  | filas limitadas a 100, producto derivado y borrador reversible           |
| Devolución        | tenant `lu_departure` + unidad              | `POST /api/v1/departures/:id/return`                            | fecha real, observaciones y retorno de unidad a estado operativo         |
| Planificación     | tenant `lu_management_plan`                 | incluido en comandos L-3                                        | confirmada → `current_phase=5`, `current_state=7`; borrador conserva L-3 |

## Correspondencia Fase 14 — L-12 adquisiciones

| Superficie          | Persistencia                     | Endpoint NestJS                                                                  | Regla preservada                                                            |
| ------------------- | -------------------------------- | -------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| Adquisición         | tenant `lu_request` con `type=2` | `GET/POST /api/v1/acquisitions`, `GET/PUT /api/v1/acquisitions/:id`              | descripción, prioridad, código de inversión, centro de costo y baja lógica  |
| Costos              | tenant `lu_request_cost`         | incluidos en alta/edición/detalle                                                | filas normalizadas, cantidades/precios y baja lógica                        |
| Comandos del wizard | tenant `lu_management_plan`      | `POST /api/v1/acquisitions/:id/complete`, `POST /api/v1/acquisitions/:id/cancel` | borrador en fase 6; confirmación en estado completado con `Step = 7` visual |

## Correspondencia Fase 4 — Personas

| Superficie             | Persistencia       | Endpoint NestJS                                                | Regla preservada                                                          |
| ---------------------- | ------------------ | -------------------------------------------------------------- | ------------------------------------------------------------------------- |
| Directorio de personas | tenant `lu_person` | `GET/POST /api/v1/people`, `GET/PUT/DELETE /api/v1/people/:id` | internos/externos, contacto, categoría, actor code, filtros y baja lógica |

## Correspondencia Fase 4 — Usuarios, roles y perfil

| Superficie            | Persistencia                                   | Endpoint NestJS                                              | Regla preservada                                                                    |
| --------------------- | ---------------------------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| Usuarios y membresías | control plane `lu_user` + `lu_site_membership` | `GET/POST /api/v1/users`, `GET/PUT/DELETE /api/v1/users/:id` | rol por sede, baja lógica, protección del SuperAdmin y aislamiento por sede         |
| Perfil                | control plane `lu_user` + membresía activa     | `GET/PUT /api/v1/profile`                                    | edición del nombre/correo sin duplicar identidad ni aceptar sede desde el navegador |

## Correspondencia Fase 15 — Operación de laboratorios

| Superficie                    | Datos consultados                                                   | Endpoint NestJS                                              | Regla preservada                                                           |
| ----------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------------------- |
| Resumen y planificación       | tenant `lu_management`, `lu_management_plan`, métricas de dashboard | `GET /api/v1/dashboard`, `GET /api/v1/managements/:id/plans` | sede y gestión se resuelven server-side; no se muestran fixtures simuladas |
| Requerimientos e incidentes   | solicitudes L-7 y gestiones activas                                 | `GET /api/v1/requests`, `GET /api/v1/managements`            | los comandos todavía no migrados permanecen diferidos, no se simulan       |
| Disponibilidad, L-3 y demanda | inventario de unidades, salidas y estados                           | `GET /api/v1/dashboard`, `GET /api/v1/managements/:id/plans` | estado vacío explícito cuando no hay datos operacionales                   |
| Calendario                    | fechas planificadas disponibles en los contratos migrados           | endpoints de dashboard/gestiones                             | no se inventan eventos ni se escribe fuera del tenant                      |

## Correspondencia Fase 16 — Reportes Excel y PDF

| Reporte            | Plantilla legacy                     | Endpoint NestJS                    | Estado                                                                                                    |
| ------------------ | ------------------------------------ | ---------------------------------- | --------------------------------------------------------------------------------------------------------- |
| L-6 Verificación   | `wwwroot/templates/L6V2.xlsx`        | `GET /api/v1/reports/download/l6`  | Generación Excel implementada con filtros por gestión/laboratorio; comparación binaria y visual pendiente |
| L-7 Solicitud      | `wwwroot/templates/L7.xlsx`          | `GET /api/v1/reports/download/l7`  | Generación Excel implementada por `requestId`; comparación binaria y visual pendiente                     |
| L-8 Mantenimiento  | `wwwroot/templates/L8.xlsx`          | `GET /api/v1/reports/download/l8`  | Generación Excel implementada por `planId`; comparación binaria y visual pendiente                        |
| L-3 Salida         | `wwwroot/templates/L3.xlsx`          | `GET /api/v1/reports/download/l3`  | Generación Excel implementada por `departureId`; comparación binaria y visual pendiente                   |
| L-48 Planificación | `wwwroot/templates/L48.xlsx`         | `GET /api/v1/reports/download/l48` | Generación Excel implementada por gestión/laboratorio; comparación binaria y visual pendiente             |
| L-12 Adquisición   | `wwwroot/templates/Adquisicion.xlsx` | `GET /api/v1/reports/download/l12` | Generación Excel implementada por `requestId`; comparación binaria y visual pendiente                     |

La referencia ASP.NET se conserva. No se retirarán las plantillas ni los endpoints legacy hasta ejecutar la comparación con datos idénticos y documentar la evidencia. El servicio legacy contiene una rutina PDF de verificación sin endpoint público activo; no se inventa un endpoint PDF nuevo mientras no exista una superficie legacy equivalente que comparar.

## Correspondencia Fase 5 — Equipos

| Superficie      | Persistencia               | Endpoint NestJS                   | Regla preservada                                                                        |
| --------------- | -------------------------- | --------------------------------- | --------------------------------------------------------------------------------------- |
| Equipos         | tenant `lu_equipment`      | `/api/v1/equipment`               | código y nombre/modelo únicos por sede, clasificación, país/ciudad, notas y baja lógica |
| Notas de equipo | tenant `lu_equipment_note` | incluidas en detalle/alta/edición | reemplazo transaccional y auditoría del usuario de la sede                              |

## Correspondencia Fase 7 — Gestiones y planificación de activos

| Superficie          | Persistencia                | Endpoint NestJS                 | Regla preservada                                                                                       |
| ------------------- | --------------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Gestiones           | tenant `lu_management`      | `/api/v1/managements`           | una gestión activa por `Type`, preventiva y correctiva simultáneas, baja lógica                        |
| Comandos de gestión | tenant `lu_management`      | `/:id/activate`, `/:id/close`   | activar, cerrar y eliminar son comandos explícitos, no efectos laterales de un CRUD genérico           |
| Plan de activos     | tenant `lu_management_plan` | `/:id/plans`, `/:id/plans/sync` | unicidad `ManagementId + EquipmentUnitId`, selección solo con unidad/laboratorio/clasificación válidos |

## Assets reutilizados

- `wwwroot/assets/images/logo-icon.png`
- `wwwroot/assets/images/logo-light-icon.png`
- `wwwroot/assets/images/favicon.png` (identificado; enlace Vite pendiente)
- `wwwroot/dist/css/style.min.css` con Bootstrap 4, NiceAdmin, Material Design Icons, Font Awesome, Themify y fuentes.
- Fondo de login definido actualmente por `Pages/Login.cshtml` mediante la misma URL de imagen.

## Diferencias eliminadas respecto al prototipo

- Se eliminaron marca `LU`, cabecera blanca propia, paleta roja y tarjetas `Integración pendiente`.
- Se incorporaron topbar, logos, sidebar jerárquico, breadcrumbs, footer, perfil y responsive equivalentes al layout Razor.
- La navegación ahora conserva las rutas Razor y las opciones se filtran con el rol efectivo de la sede.
- El nombre, correo, sede y rol proceden del backend; no son textos de ejemplo.

## Brechas y decisiones pendientes

1. PostgreSQL solo conserva `email`, no un `UserName` separado. La etiqueta replica “Usuario o Correo”, pero la cuenta debe resolverse actualmente por correo; no se inventará una columna sin contrato de datos.
2. El dashboard real ya consulta PostgreSQL tenant con métricas, gestión activa, filtros, paginación y estados vacíos. Falta completar el seed administrativo y el recorrido navegador autenticado.
3. Feedback global, perfil detallado y contenido de cada ruta siguen como verticales separadas; notificaciones ya tienen contrato, API y campana React. Países/Ciudades y la organización académica quedan pendientes únicamente de E2E autenticado en navegador y comparación visual contra Razor.
4. La paridad visual final exige comparación en navegador con ambas aplicaciones ejecutables. Las rutas operativas ya tienen panel real y estados vacíos, pero aún falta el recorrido visual autenticado de cada ruta.
5. El runtime tenant PostgreSQL local ya está provisionado para el sandbox y resuelto server-side por `TenantRouter`; la migración aditiva del dashboard se verificó con fixture reversible. La sede seguirá mostrando estado vacío hasta que exista una gestión real.
6. La comprobación del 20-09-2026 encontró `GastroExample` local activo pero protegido por SCRAM sin credencial migradora disponible; `apps/api/.env.local` apunta al control plane remoto `neondb` con el rol runtime sin DDL. El 21-09-2026 se verificó acceso al sandbox local con la credencial suministrada fuera del repositorio, se aplicó `0003_create_tenant_route_catalog.sql`, se provisionaron los roles locales y la suite PostgreSQL real pasó 8/8 con limpieza completa. La verificación quedó en `apps/api/evidence/MIG-F4-PG-ROUTE-003.verify.json`. No se escribieron datos en Neon.

## Puertas de validación de `UI-MIG-2026-09-20-01`

- [x] formato/lint/typecheck/build (`corepack pnpm run verify`, 2026-09-20).
- [x] pruebas de contrato y API (318 aprobadas; 9 omitidas por configuración).
- [x] pruebas React de login, sede, roles, ruta y logout (6 aprobadas).
- [x] smoke local NestJS + PostgreSQL (`/api/v1/healthz` y login real).
- [x] E2E local en navegador a 127.0.0.1: login → shell → sede → ruta → logout.
- [x] comparación visual del login ASP.NET frente a React y evidencia responsive móvil.
- [x] compilación de la referencia ASP.NET: 0 errores y 0 advertencias.
- [x] dashboard tenant: métricas, filtros, paginación, notificaciones y aislamiento `site_id` verificados con PostgreSQL local; fixture eliminada.
- [x] catálogos Países/Ciudades: migración aditiva, CRUD, validación de duplicados, baja lógica, aislamiento y limpieza verificados en PostgreSQL local; evidencia `apps/api/evidence/MIG-F2-PG-CATALOGS-001.verify.json`.
- [x] organización académica: Facultades/Carreras/SiteCareer en control plane y Laboratorios tenant con CRUD, asignación, routing y limpieza verificados; evidencia `apps/api/evidence/MIG-F3-PG-ACADEMIC-001.verify.json`.
- [x] equipos: CRUD tenant, ubicación país/ciudad, notas, filtro, baja lógica, pool server-side y limpieza verificados; evidencia `apps/api/evidence/MIG-F5-PG-EQUIPMENT-001.verify.json`.
- [x] unidades físicas e inventario visual: CRUD, historial, aislamiento tenant, filtros, baja lógica y limpieza verificados; evidencia `apps/api/evidence/MIG-F6-PG-EQUIPMENT-UNITS-001.verify.json`.
- [x] gestiones y planificación: CRUD, activación por tipo, coexistencia preventiva/correctiva, selección de activos, cierre, baja lógica y limpieza verificados; evidencia `apps/api/evidence/MIG-F7-PG-MANAGEMENTS-001.verify.json`.
- [x] L-6 verificaciones: borrador, individual con falla, masivo, checklist, transición del plan y limpieza verificados; evidencia `apps/api/evidence/MIG-F9-PG-VERIFICATIONS-001.verify.json`.
- [x] L-7 solicitudes: borrador, finalización, filtros, transición del plan, duplicado técnico y limpieza verificados; evidencia `apps/api/evidence/MIG-F10-PG-REQUESTS-001.verify.json`.
- [x] L-8 mantenimiento: tareas, costos, avance, finalización, transición del plan y limpieza verificados; evidencia `apps/api/evidence/MIG-F11-PG-MAINTENANCES-001.verify.json`.
- [x] L-3 salidas: creación masiva, transición a Kardex, estado `OnLoan`, devolución, restauración y limpieza verificados; evidencia `apps/api/evidence/MIG-F12-PG-DEPARTURES-001.verify.json`.
- [x] L-48/Kardex: borrador, cierre, tareas completas, costos, historial de estados, transición a L-12 y limpieza verificados; evidencia `apps/api/evidence/MIG-F13-PG-KARDEX-001.verify.json`.
- [x] L-12 adquisiciones: borrador, costos normalizados, confirmación, transición a estado completado, listado y limpieza verificados; evidencia `apps/api/evidence/MIG-F14-PG-ACQUISITIONS-001.verify.json`.
- [x] Personas: personas internas/externas, edición, filtros, baja lógica y limpieza verificados; evidencia `apps/api/evidence/MIG-F4-PG-PEOPLE-001.verify.json`.
- [x] Usuarios y perfil: cuentas, membresías, roles por sede, baja lógica, seguridad de sesión y perfil verificados; evidencia `apps/api/evidence/MIG-F4-CP-USERS-001.verify.json`.
- [x] Operación: ocho rutas operativas cargan métricas, gestiones y planificación desde contratos/API existentes, con aislamiento por sede y estados vacíos explícitos; no se agregaron fixtures visibles ni una migración nueva.
- [x] Reportes Excel: contratos, cliente, centro React, endpoint autenticado y generación sobre las seis plantillas institucionales implementados; smoke local L-7 produjo un archivo real sin escribir datos.
- [ ] Reportes: comparación archivo-a-archivo, estilos, fórmulas, contenido PDF si se habilita y pruebas E2E con datos idénticos.

## Rebaseline Fase 0 — 20-09-2026

- [x] Se confirmó que auth, sesión, sede activa, logout y shell React son la línea base existente.
- [x] Se documentó PostgreSQL directo por vertical y aprobación entre fases.
- [x] Se documentó dashboard como siguiente vertical.
- [x] Se verificó que la API nueva solo expone healthz, auth y contexto; no se inventó un endpoint de dashboard.
- [ ] Provisionar y verificar runtime tenant PostgreSQL server-side antes de iniciar Fase 1.
- [x] Configurar localmente las credenciales runtime/migrator de `GastroExample` sin versionarlas ni compartirlas por chat; `apps/api/.env.example` documenta el hostname Docker (`postgres`/`db`) y el rol runtime separado.

Advertencia no bloqueante observada en build: el CSS NiceAdmin heredado referencia
`../images/curve.jpg`, archivo ausente también en `wwwroot`. La regla no afecta las
vistas implementadas, pero debe resolverse al sanear el paquete de assets.
