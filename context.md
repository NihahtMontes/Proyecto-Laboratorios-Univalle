# CONTEXT.md — Sistema de Gestión de Laboratorios Univalle

> **LEE ESTE ARCHIVO COMPLETO ANTES DE TOCAR CUALQUIER ARCHIVO.**
> Cualquier decisión arquitectónica que no esté aquí debe ser consultada antes de implementarse.

---

## 1. QUÉ ES ESTE SOFTWARE

Sistema de Gestión de Laboratorios para la Universidad del Valle (Bolivia). Administra el ciclo de vida completo de equipos de laboratorio mediante formularios institucionales estandarizados (L-3, L-6, L-7, L-8, L-12, L-48), orquestados por un Wizard de 7 pasos. Soporta dos tipos de gestión: **Preventivo** (planificado por semestre) y **Correctivo** (reactivo, fallas imprevistas).

**Flujo Preventivo:**
```
L-6 Verificación Masiva → L-7 Solicitud → L-8 Mantenimiento → L-3 Salida → L-48 Kardex → L-12 Adquisición → Desembolso
```

**Flujo Correctivo (salta L-6):**
```
Reportar Falla Crítica → L-7 Solicitud → L-8 Mantenimiento → L-3 Salida → L-48 → L-12
```

---

## 2. STACK — NO CAMBIAR, NO AGREGAR DEPENDENCIAS SIN CONSULTAR

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 9.0 — **Razor Pages** (NO MVC Controllers salvo ReportsController y WizardRollbackController) |
| ORM | Entity Framework Core 9.0 + **PostgreSQL** (Npgsql) |
| Frontend | Razor Pages (.cshtml) + Bootstrap 4 + NiceAdmin template |
| JS | jQuery 3.x, Select2, jqBootstrapValidation, SweetAlert2 |
| Excel | **EPPlus (OfficeOpenXml)** — NO ClosedXML, NO System.Drawing |
| Auth | ASP.NET Core Identity con roles |
| Cache | IMemoryCache (5 min TTL), cache keys separadas por ManagementType |
| Sessions | `AddDistributedMemoryCache()` + `AddSession()` (4h timeout) |

---

## 3. ESTRUCTURA DE CARPETAS

```
Pages/
  Index.cshtml(.cs)       — Dashboard (auto-redirige a Details si hay gestión activa)
  Verifications/          — L-6: MassCreate (masivo) + Index (sesiones) + Details
  Requests/               — L-7: Create, Edit, Details, Delete, Index
  Maintenances/           — L-8: Create, Edit, Details, Delete, Index
  Departures/             — L-3: Create, Edit, Details, Delete, Index
  Kardex/                 — L-48: Create, Edit (Index no existe como archivo separado)
  Acquisitions/           — L-12: Create, Edit, Details, Delete, Index
  Managements/            — Gestión semestral (Preventivo + Correctivo con tabs)
  Equipment/              — CRUD equipos (catálogo) + Notas del Fabricante
  EquipmentUnits/         — CRUD unidades físicas
  Faculties/              — CRUD facultades
  Laboratories/           — CRUD laboratorios
  Persons/                — CRUD personas (TPT: Intern/Extern)
  Users/                  — CRUD usuarios Identity
  Countries/              — CRUD países
  Cities/                 — CRUD ciudades
  Career/                 — CRUD carreras
  Account/                — (no existe aún, planeado para perfil)
  Shared/                 — _Layout, _Sidebar, _WizardSteps, _WizardStep
Models/
  Enums/                  — EquipmentStatus, GeneralStatus, RequestStatus, WizardPhase, ManagementType, etc.
  EquipmentNote.cs        — Notas del fabricante/prevención
Data/
  ApplicationDbContext.cs — DbContext + QueryFilters + seed
  DbInitializer.cs        — SeedData: crea gestión preventiva + correctiva
Services/
  ReportService.cs        — Excel (todos los reportes)
  ManagementContextService.cs — Caché de gestión activa (con filtro por ManagementType)
  Reporting/              — Servicios de reportes específicos
Helpers/
  PaginatedList.cs
  AuthorizationHelper.cs  — Constantes de roles
  EnumHelper.cs           — SelectList para enums
  TempDataExtensions.cs   — .Success(), .Error(), .Warning()
Controllers/
  ReportsController.cs    — Solo descarga de Excel
  WizardRollbackController.cs — API para rollback de fases del wizard
```

---

## 4. REGLAS OBLIGATORIAS — SIEMPRE

### Soft-delete (CRÍTICO)
- **NUNCA** usar `_context.Remove()` ni `_context.{Entity}.Remove()`
- Siempre: `entity.Status = GeneralStatus.Eliminado` (o `CurrentStatus = EquipmentStatus.Deleted`)
- El QueryFilter en ApplicationDbContext excluye automáticamente los eliminados
- Para correctivo activo: se permite soft-delete aunque esté Active (el delete en Managements/Index.cshtml.cs tiene excepción `Type != Corrective`)

### Tracking (CRÍTICO)
- NoTracking es **global** en Program.cs (`UseQueryTrackingBehavior(NoTracking)`)
- Toda query que luego modifica y guarda debe tener `.AsTracking()` explícito
- `FindAsync()` siempre trackea (excepción a la regla global)
- Si modificas una entidad sin `.AsTracking()` → los cambios NO se guardan

### TempData (CRÍTICO)
- **NUNCA** `TempData["Success"] = "..."` — es vulnerable a XSS
- **SIEMPRE** `TempData.Success("mensaje")` (extensión en TempDataExtensions.cs)
- También: `TempData.Error()`, `TempData.Warning()`
- En `_Layout.cshtml`, para mostrar en SweetAlert2: **`@Html.Raw(TempData["Error"]?.ToString() ?? "")`** — NUNCA `@TempData["Error"]` solo (causa `&#xF3;` en acentos)

### EPPlus / Excel (CRÍTICO)
- **NUNCA** `Worksheets.Add(sourceSheet.Name, sourceSheet)` → crash nativo 0xffffffff por corrupción XML
- Reportes nuevos: generar desde cero (sin template), como hace L-6
- Reportes con template: `new ExcelPackage(templateFile)` → usar `Workbook.Worksheets[0]` directamente → guardar con `SaveAs(MemoryStream)`
- `EliminarHojasExtra` es un método vacío (no-op). **NO eliminarlo, NO reactivarlo.** Existe como documentación.

### EF Core
- `HasQueryFilter` **solo** en Equipment (y entidades raíz con Status). **NO** agregarlo en Maintenance, Management, Verification, etc. — rompe queries existentes
- **NUNCA** modificar migraciones existentes — siempre crear nuevas
- `FullName` en Person es `[NotMapped]` — **no usarlo en LINQ**. Hacer `.ToList()` primero, luego filtrar en memoria
- `Semester` en Management puede ser 0 (Correctivo), 1 o 2 (Preventivo). El rango de validación es `[Range(0, 2)]`

### Paginación
- Todos los Index usan `PaginatedList<T>.CreateAsync()`

### Confirmaciones UI
- Botones destructivos usan SweetAlert2 de confirmación antes del submit

---

## 5. REGLAS OBLIGATORIAS — NUNCA

| ❌ Nunca | ✅ En su lugar |
|---|---|
| `_context.Remove(entity)` | `entity.Status = GeneralStatus.Eliminado` |
| `TempData["Success"] = "..."` | `TempData.Success("...")` |
| `@TempData["Error"]` en JS | `@Html.Raw(TempData["Error"]?.ToString() ?? "")` |
| `Worksheets.Add(sheet.Name, sheet)` | Generar desde cero o `.Copy()` |
| Modificar migraciones existentes | Crear nueva migración |
| `FullName` en LINQ | `.ToList()` → filtrar en memoria |
| Modificar sin `.AsTracking()` | `.AsTracking()` en la query de carga |
| `HasQueryFilter` en entidades dependientes | Solo en Equipment (raíz) |
| Eliminar `EliminarHojasExtra` | Dejarlo como no-op |
| Hard-delete de cualquier entidad principal | Soft-delete siempre |
| Commit sin aprobación del usuario | Preguntar antes de commitear |
| `[Range(1, 2)]` en Semester de Management | `[Range(0, 2)]` (Correctivo usa 0) |
| `.Include(m => m.MaintenanceType)` en EF | Es un enum, no nav prop — no incluir |

---

## 6. ARQUITECTURA CORRECTIVO (NUEVO)

### Modelo
```csharp
// Models/Enums/ManagementType.cs
public enum ManagementType { Preventive = 0, Corrective = 1 }

// Models/Management.cs
public ManagementType Type { get; set; } = ManagementType.Preventive;
```

### Gestión Correctiva en BD
- `Semester = 0` (no aplica semestre para correctivos)
- `Code = "CORR-YYYY"` (ej: CORR-2026)
- `Status = ManagementStatus.Active`
- **NO sincroniza equipos** al crearse. ManagementPlans se crean de a uno al reportar falla.

### Sidebar
```
📊 Dashboard       → /Index
📋 Gestiones       → /Managements/Index?type=Preventive (tabs: Preventivo | Correctivo)
🔧 Mant. Correctivo → /Managements/Details?type=Corrective (dashboard directo, auto-crea CORR-YYYY)
```

### Flujo Correctivo
```
Sidebar "Mant. Correctivo" → Details?type=Corrective
  → EnsureCorrectiveContainerExists() (auto-crea CORR-2026 si no existe)
  → "Reportar Falla Crítica" → /Index?ShowWizard=true&ManagementId=X (Step=2 directo)
  → L-7 Create (dropdowns habilitados, sin referencia L-6)
  → Guardar → Redirect Index?Step=3&ManagementId=X
  → Wizard L-8 → L-3 → Kardex → L-12
```

### Routing — ManagementId OBLIGATORIO
Todos los redirects del wizard DEBEN incluir `ManagementId` para preservar el contexto:
- `Details.cshtml`: "Iniciar Ronda", "Reportar Falla Crítica", 5 KPI cards
- `Index.cshtml`: "Reportar Nueva Falla Crítica", "Volver al Dashboard", "Finalizar Wizard"
- 5 Create pages: "Volver al Wizard"
- 5 Create PageModels: OnPostAsync redirects
- OnPostNextStep / OnPostPreviousStep

### Create Pages — Resolver ManagementId
Cada Create PageModel debe resolver la gestión desde `ManagementPlanId` (si existe), luego `ManagementId` (query param), y solo como último recurso `GetCurrentManagementAsync()`:

```csharp
// Requests/Create.cshtml.cs — ResolveManagementAsync()
if (ManagementPlanId.HasValue) → buscar plan → obtener plan.ManagementId
if (ManagementId.HasValue) → buscar Management directo
fallback → GetCurrentManagementAsync()
```

### Wizard Steps
`_WizardSteps.cshtml` usa `ViewData["IsCorrective"]`:
- Correctivo: 5 burbujas (L7, L8, L3, Kardex, Desembolso). Step numbers: `i + 2`
- Preventivo: 6 burbujas (L6-L12). Step numbers: `i + 1`

### Dropdowns L-7
En `Requests/Create.cshtml`: `@if (isWizard && !isCorrective)` → bloquea selects. En correctivo sin plan preexistente, los dropdowns deben ser editables.

### Delete Correctivo
`Managements/Index.cshtml.cs:105`: excepción `Type != ManagementType.Corrective` para permitir soft-delete de gestión correctiva activa. Redirect preserva `Type` param.

### ManagementContextService
`GetCurrentManagementAsync(ManagementType? type = null)`:
- Con type: busca `Status == Active && Type == type`
- Sin type: busca `Status == Active && Type == Preventive` (default)
- Cache keys separadas: `ActiveManagement`, `ActiveManagement_Preventive`, `ActiveManagement_Corrective`
- `InvalidateCache()` limpia las 3 keys

---

## 7. L-6 VERIFICACIONES — ARQUITECTURA ACTUAL

### MassCreate (NUEVO)
- `Pages/Verifications/MassCreate.cshtml` + `.cs`
- Verificación **por laboratorio** (no por equipo individual)
- AJAX carga equipos sin verificación (`VerificationId == null && CurrentPhase == Verification`)
- Dropdown PhysicalCondition por fila + modal Bootstrap para Bad
- OnPost: crea Verification por equipo. Bad → auto-crea Request L-7

### Index (REFACTORIZADO)
- Modelo `SessionGroup`: agrupado por `Date + LaboratoryId`
- Vista vacía hasta seleccionar laboratorio ("Seleccione un laboratorio")
- Columnas: Fecha, Laboratorio, Equipos (conteo buenos/malos), Inspector, "Ver Sesión"
- Reporte Excel L-6 al final de la página

### Details (REFACTORIZADO)
- Modo sesión: `Details?labId=X&date=YYYY-MM-DD` → tabla de todos los equipos
- Modo individual: `Details?id=X` (fallback, conservado)
- Sin CheckResults (checklist obsoleto)

---

## 8. WIZARD — CÓMO FUNCIONA

- `ManagementPlan.CurrentPhase` → enum `WizardPhase` rastrea la fase actual
- `ManagementPlan.CurrentState` → enum `WizardEquipmentState` rastrea el estado
- `isWizard=true` en QueryString activa el flujo guiado
- `ManagementId` query param preserva el contexto de la gestión
- Dropdowns del wizard se precargan con IDs de fases anteriores (disabled) — **solo en preventivo**
- `_WizardSteps` partial: 5 o 6 burbujas según `ViewData["IsCorrective"]`
- Correctivo: Step 0 va directo a Step 2 (L-7). PreviousStep min=2.
- L-6 con fallas → `CurrentPhase = TechnicalRequest`
- L-6 sin fallas → `CurrentPhase = Maintenance`
- Contador `CountL6` y `Step1Plans` filtran `VerificationId == null`
- `EquiposTerminados` usa `CurrentState == Completed` (no `PlanStatus`)

---

## 9. L-48 CRONOGRAMA — REGLAS

- Filtro "Todos": `CurrentState >= AwaitingRequest` (excluye Activos Sanos)
- "Planeado": `PlannedWeek != null`
- "En Progreso": `Maintenance.Status == InProgress`
- "Ejecutado": `ExecutedWeek != null`
- Regla verde≥púrpura: no se puede marcar Ejecutado sin Planeado previo. Semana ejecutada ≥ planeada. Backend bloquea y frontend muestra SweetAlert2.
- `.AsTracking()` en `OnPostToggleWeekAsync`

---

## 10. CACHÉ DE GESTIÓN ACTIVA

```csharp
ManagementContextService.GetCurrentManagementAsync(ManagementType? type = null)
  → Cache key: "ActiveManagement" o "ActiveManagement_{type}"
  → TTL: 5 min
  → Si tipo especificado: WHERE Status == Active AND Type == type
  → Sin tipo: WHERE Status == Active AND Type == Preventive (default)
InvalidateCache() → elimina las 3 keys de cache
```

---

## 11. SWEETALERT2 — REGLAS DE DISPLAY

En `_Layout.cshtml`:
```javascript
// CORRECTO (acentos funcionan):
title: '@Html.Raw(TempData["Success"]?.ToString() ?? "")'
text: '@Html.Raw(TempData["Error"]?.ToString() ?? "")'

// INCORRECTO (acentos rotos → &#xF3;):
title: '@TempData["Success"]'
text: '@TempData["Error"]'
```

---

## 12. LOGIN

- `body { overflow: hidden }` — evita scroll
- `max-width: 95%` — no toca bordes en móvil
- Sin `drop-shadow` en logo, sin `transition` en input-group-text
- Sin gradiente hover en botón
- Responsive simplificado (sin padding fijos en @media)
- `ExpireTimeSpan = 7 días` (antes 1h) — Recordarme funciona correctamente
- Logout: `asp-page="/Logout"` con `method="post"`

---

## 13. EQUIPO — NOTAS DEL FABRICANTE

- Entidad `EquipmentNote` (Id, EquipmentId FK, Note)
- FK cascade a Equipment
- UI dinámica (JS template) en Create/Edit/Details
- Mismo patrón que L-8 tasks/costs

---

## 14. BASE DE DATOS — PostgreSQL (desde 7 Mayo 2026)

### Proveedor
- **PostgreSQL** (Npgsql 9.0.0) vía `UseNpgsql()`
- `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` en Program.cs
- Connection string: `Host=localhost;Port=5432;Database=DB_Laboratorios_Univalle_DEV;Username=postgres;Password=...;Include Error Detail=true`
- `appsettings.json` usa placeholder; credenciales reales solo en `appsettings.Development.json` ignorado por Git o user-secrets
- `SplitQuery`, `CommandTimeout(120)`, `NoTracking` intactos

### Migración baseline
- `Data/PostgresMigrations/Initial_PostgreSQL` — baseline limpio para PostgreSQL
- Migraciones SQL Server antiguas excluidas del build (`<Compile Remove>` en csproj)
- Archivos históricos (SQL Server): `Migrations/` y `Data/Migrations/` (excluidos)

### Migraciones SQL Server históricas (excluidas del build)
1. `FixVerificationDynamicArchitecture` — checklist dinámico
2. `ManangmentIdFix` — FK ManagementId en todas las entidades
3. `RemoveSpecialtyAndBuildingTypeFromLaboratory` — limpieza Lab
4. `AddVerificationFaults` — fallas en verificaciones
5. `AddPerformanceIndexes` — índices de rendimiento
6. `AddEquipmentSoftDelete` — soft-delete en Equipment
7. `AddWeeksToManagementPlan` — PlannedWeek, ExecutedWeek
8. `AddEquipmentNotes` — notas del fabricante
9. `FixCascadeDeleteAndLaboratoryCityFK` — Restrict en FK críticos
10. `AddManagementType` — Type en Managements
11. `AddUserProfilePicture` — placeholder (vacío, sin implementar)
12. `Add_KardexHistoryId_And_DepartureItems` — Kardex FK + DepartureItems (SQL Server, ahora en baseline PG)

### Cambios de tipos para PostgreSQL
- `HasColumnType("decimal(18,2)")` → `HasPrecision(18, 2)` en ApplicationDbContext
- `HasColumnType("decimal(10,2)")` → `HasPrecision(10, 2)` en ApplicationDbContext
- `[Column(TypeName = "decimal(18,2)")]` → `[Precision(18, 2)]` en modelos
- `[Column(TypeName = "decimal(10,2)")]` → `[Precision(10, 2)]` en modelos
- `HasFilter("[Status] != 2")` → `HasFilter("\"Status\" <> 2")`
- `HasFilter("[CurrentStatus] != 99")` → `HasFilter("\"CurrentStatus\" <> 99")`
- `Microsoft.Data.SqlClient` → eliminado (sin dependencia SQL Server)
- Paquete `Microsoft.EntityFrameworkCore.SqlServer` removido del csproj

### SeedData
`DbInitializer.cs` crea:
- 3 usuarios (admin, jefe cocina, jefe química)
- 2 técnicos (interno + externo)
- Bolivia → Cochabamba → 2 facultades → 2 carreras → 2 laboratorios
- 5 equipos (Batidora, Microondas, Horno, Microscopio, Espectrofotómetro)
- **Gestión Preventiva 1-2026**: 7 equipos con escenarios (L-6, L-7, L-8 planeado/ejecutado)
- **Gestión Correctiva CORR-2026**: vacía, lista para reportar fallas

---

## 15. CONVENCIONES DE NOMBRES

- **PageModels**: `{Entity}CreateModel`, `{Entity}EditModel`, `{Entity}IndexModel`, `{Entity}DetailsModel`, `{Entity}DeleteModel`
- **Input models**: clase `InputModel` anidada dentro del PageModel
- **Enums**: PascalCase en C#, almacenados como int en BD
- **Auditoría**: `CreatedDate`, `CreatedById`, `LastModifiedDate`, `ModifiedById`
- **Roles**: usar constantes de `AuthorizationHelper.cs` (no strings sueltos)

---

## 16. CONVENCIONES UI (NiceAdmin — RESPETAR)

- Cards: `shadow-sm border-0`
- Badges: `badge-success`, `badge-warning`, `badge-info` (soft, no solid)
- Botones: `btn-rounded font-weight-bold shadow-sm`
- Formularios: asimétricos `col-lg-7` + `col-lg-5`
- Tablas: `table-hover v-middle` con `thead class="bg-light"`
- Select2 para dropdowns con búsqueda
- jqBootstrapValidation para validación client-side
- Secciones importantes: `border-left: 4px solid {color}`
- Modal Bootstrap nativo (no SweetAlert2 modal) para formularios

---

## 17. ESTADO DE MÓDULOS

### Completados y en producción
| Módulo | Estado |
|--------|--------|
| Dashboard | ✅ Operativo |
| Equipment CRUD + Notas | ✅ Operativo |
| EquipmentUnits CRUD | ✅ Operativo |
| Faculties, Laboratories | ✅ Operativo |
| Persons (TPT), Users | ✅ Operativo |
| Countries, Cities, Career | ✅ Operativo |
| Verifications L-6 (MassCreate + Index sesiones + Details) | ✅ Operativo |
| Requests L-7 | ✅ Operativo |
| Maintenances L-8 (con tareas dinámicas y costos) | ✅ Operativo |
| Departures L-3 | ✅ Operativo |
| Kardex L-48 | ✅ Operativo |
| Acquisitions L-12 | ✅ Operativo |
| Managements (Preventivo + Correctivo con tabs) | ✅ Operativo |
| Wizard (6 pasos preventivo, 5 pasos correctivo) | ✅ Operativo |
| Paginación (14+ páginas) | ✅ Operativo |
| Excel Reportes | ✅ Operativo |
| TempData Notifications (SweetAlert2 con Html.Raw) | ✅ Operativo |
| Login (visual + RememberMe) | ✅ Operativo |
| L-48 Cronograma (8 semanas + reglas) | ✅ Operativo |

### Pendientes
| Item | Prioridad |
|------|-----------|
| `OnPostToggleWeekAsync` — verificar `.AsTracking()` | 🟡 Media |
| Implementar `AddUserProfilePicture` (modelo + migración) | 🟡 Media |
| Flujo correctivo no probado end-to-end | 🟡 Media |
| Sistema Rehacer (WizardGuard + WizardRollback + notificaciones) | 🟢 Baja (próximo sprint) |
| Campana de notificaciones en top bar | 🟢 Baja |
| Exportación PDF (QuestPDF) | 🟢 Baja |
| Testing unitario | 🟢 Baja |

---

## 18. ARCHIVOS INTOCABLES

| Archivo | Razón |
|---|---|
| `EliminarHojasExtra` en ReportService.cs | No-op intencional, documentación |
| `wwwroot/templates/L6V2.xlsx` | No se usa pero no borrar |
| Todas las migraciones existentes | Solo agregar nuevas |
| QueryFilters existentes en ApplicationDbContext | No agregar en entidades dependientes |
| `NoTracking` global en Program.cs | Intencional para performance |
| `Verifications/Edit.cshtml` + `.cs` | Obsoletos (L-6 ahora es por sesión), se dejan inaccesibles |
| `Verifications/Delete.cshtml` + `.cs` | Ídem |

---

## 19. ANTES DE ENTREGAR CUALQUIER CAMBIO

1. Leer `CONTEXT.md` completo (este archivo)
2. Leer `AGENTS.md` de la capa correspondiente
3. Verificar que no usas ningún anti-patrón de la sección 5
4. Ejecutar `dotnet build` — debe terminar con **0 errores**
5. Si tocaste `Services/` → leer `Services/AGENTS.md` primero
6. Si tocaste Excel → verificar que no hay `Worksheets.Add(sheet, sheet)`
7. Si modificaste entidades → verificar que la query de carga tiene `.AsTracking()`
8. Si tocaste rutas del wizard → verificar que incluyen `ManagementId`
9. No commitear sin aprobación explícita del usuario
10. Respetar estética NiceAdmin (sección 16)

---

*Última actualización: 7 de Mayo de 2026 — Migración a PostgreSQL (Initial_PostgreSQL baseline) + Wizard L-3 multi-ítem + DepartureItems + KardexHistoryId*
