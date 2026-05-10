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
| Excel | **EPPlus 7.5.2** (OfficeOpenXml) |
| Auth | ASP.NET Core Identity con roles |
| Cache | IMemoryCache (5 min TTL), cache keys separadas por ManagementType |
| Sessions | `AddDistributedMemoryCache()` + `AddSession()` (4h timeout) |

### Dependencias NuGet actuales (csproj)
- `EPPlus` 7.5.2 — actualizado pre-despliegue por fixes de crash nativo
- `ClosedXML` 0.105.0 — solo referenciado, NO usado en ReportService
- `QuestPDF` 2025.12.3 — Community license, PDF futuro
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.0
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` 9.0.12 — **causa de crash VS** (ver sección 20)

---

## 3. ESTRUCTURA DE CARPETAS

```
Pages/
  Index.cshtml(.cs)       — Dashboard (auto-redirige a Details si hay gestión activa)
  Verifications/          — L-6: MassCreate (masivo) + Index (sesiones) + Details
  Requests/               — L-7: Create, Edit, Details, Delete, Index
  Maintenances/           — L-8: Create, Edit, Details, Delete, Index
  Departures/             — L-3: Create, Edit, Details, Delete, Index, **MassCreate** (masivo por lab)
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
  Api/                    — Notifications API (Unread + MarkAsRead)
  Reports/                — Centro de Reportes (selección de lab + descargas)
  Shared/                 — _Layout, _Sidebar, _WizardSteps, _WizardStep
Models/
  Enums/                  — EquipmentStatus, GeneralStatus, RequestStatus, WizardPhase, ManagementType, etc.
  EquipmentNote.cs        — Notas del fabricante/prevención
  Notification.cs         — Notificaciones (bell campana)
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
- Reportes con template: `new ExcelPackage(templateFile)` → usar `Workbook.Worksheets[0]` directamente → guardar con `SavePackage()` helper
- `EliminarHojasExtra` es un método vacío (no-op). **NO eliminarlo, NO reactivarlo.** Existe como documentación.
- **NUNCA** usar `System.Drawing.Color` para colores de celda → usar helpers EPPlus con `ExcelColor.SetColor(alpha, r, g, b)` (ver sección 20)
- **NUNCA** `Merge = false` en rangos ya merged → crash nativo 0xffffffff

### EF Core
- `HasQueryFilter` **solo** en Equipment (y entidades raíz con Status). **NO** agregarlo en Maintenance, Management, Verification, etc. — rompe queries existentes
- **NUNCA** modificar migraciones existentes — siempre crear nuevas
- `FullName` en Person es `[NotMapped]` — **no usarlo en LINQ**. Hacer `.ToList()` primero, luego filtrar en memoria
- `Semester` en Management puede ser 0 (Correctivo), 1 o 2 (Preventivo). El rango de validación es `[Range(0, 2)]`

### Paginación
- Todos los Index usan `PaginatedList<T>.CreateAsync()`

### SweetAlert2 — Confirmaciones (CRÍTICO — ARREGLADO 8 MAYO 2026)
- **Versión instalada**: SweetAlert2 **v7.19.3** — **NO tiene la propiedad `result.isConfirmed`** (se introdujo en v8+).
- **En v7, la confirmación se evalúa con `result.value`** (booleano `true`). Usar solo `result.isConfirmed` hace que el diálogo aparezca pero **el submit NUNCA se ejecuta**, porque `isConfirmed` siempre es `undefined`.
- **PATRÓN OBLIGATORIO** para TODOS los `Swal.fire` con confirmación — usar check retrocompatible con v7 y v8+:
  ```javascript
  Swal.fire({...}).then(function(result) {
      if (result && (result.isConfirmed === true || result.value === true)) {
          // acción de confirmación (ej: form.submit())
      }
  });
  ```
- **Este bug crítico fue corregido el 8 Mayo 2026 en 8 archivos**: `Departures/MassCreate.cshtml`, `Departures/Create.cshtml`, `Kardex/Create.cshtml`, `Verifications/Edit.cshtml`, `Verifications/Create.cshtml`, `Managements/Details.cshtml`, `Maintenances/Edit.cshtml`, `Acquisitions/Create.cshtml`.
- **Cualquier nuevo Swal.fire con confirmación DEBE usar este patrón**. Si se actualiza SweetAlert2 a v8+, el check `result.value === true` será redundante pero inofensivo.
- **Fallback si SweetAlert2 no carga**: usar `alert()` nativo antes de permitir submit directo (ver `Departures/MassCreate.cshtml` para el patrón).

### Checkbox boolean binding en filas dinámicas (CRÍTICO — ARREGLADO 8 MAYO 2026)
- **NO** usar hidden `value="false"` antes de checkbox `value="true"` con el mismo `name`. ASP.NET recibe `false,true` y puede bindear `false` aunque esté checked.
- **Fix**: solo checkbox `value="true"`. Checked → envía `true`; unchecked → no envía valor (el modelo usa su default `false`).
- Hidden inputs (`PlanId`, `EquipmentUnitId`, `ProductName`) en filas dinámicas deben ir **dentro de un `<td>`**, nunca directamente dentro de `<tr>` — HTML inválido en tablas causa que el navegador reubique los inputs y el backend reciba `PlanId=0`.

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
| `System.Drawing.Color` en ReportService | Usar helpers EPPlus `SetBg`, `SetFontColor`, `SetBorderAround` |
| `Merge = false` en rangos merged de Excel | Solo limpiar `.Value = null` |
| Ejecutar desde VS debugger sin desactivar Browser Link | Usar `dotnet run` o `UseSetting("BrowserLink:Enabled", "false")` |
| `if (result.isConfirmed)` en SweetAlert2 v7 | `if (result && (result.isConfirmed === true \|\| result.value === true))` |
| Hidden `value="false"` antes de checkbox `value="true"` (mismo name) | Solo checkbox; unchecked no envía valor, modelo usa default |
| Hidden inputs (`PlanId`, etc.) fuera de `<td>` en tablas dinámicas | Siempre dentro de un `<td>` válido |

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

### Rehacer L-6 desde Wizard
- Si una L-6 previamente observada se edita a `PhysicalCondition.Excellent` o `PhysicalCondition.Good`, la vista muestra SweetAlert2 antes de guardar.
- Al confirmar, la verificacion queda `Completed`, las fallas activas quedan con `IsDeleted = true`, una solicitud L-7 pendiente se cancela, y el `ManagementPlan` pasa a `CurrentState = VerifiedGood`.
- El plan conserva `VerificationId` pero queda en `CurrentPhase = Verification`; asi no aparece en L-7 ni en L-8, y tampoco vuelve a L-6 porque `Step1Plans` exige `VerificationId == null`.

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

### Trabajo actual de estabilización del Wizard
El trabajo actual NO es publicar. Se está probando módulo por módulo con capturas del usuario, siguiendo el flujo real del laboratorio:

```
L-6 Verificación → L-7 Solicitud técnica → L-8 Mantenimiento → L-3 → L-48 → L-12
```

Objetivo inmediato: que cada paso registre datos reales, actualice el `ManagementPlan`, preserve `ManagementId` y muestre el siguiente paso correcto en el wizard.

#### Regla de oro
El wizard debe confiar en el servidor, no en el HTML. Los combos pueden estar disabled, precargados o congelados visualmente, especialmente en preventivo. Por eso el backend debe reconstruir la verdad desde:

1. `ManagementPlanId`
2. `ManagementId`
3. Entidades relacionadas (`EquipmentUnit`, `Laboratory`, `Verification`, `Request`, etc.)
4. Solo al final, los valores posteados por el formulario

Si un formulario de wizard tiene selects disabled, recordar que un `<select disabled>` no postea valor confiable por sí solo. Se deben usar hidden inputs y, aun así, validar/derivar en backend desde el plan.

#### L-7 Create — corrección aplicada / patrón a seguir
Archivo principal: `Pages/Requests/Create.cshtml.cs`
Vista principal: `Pages/Requests/Create.cshtml`

El problema observado: al registrar L-7 desde el wizard, la pantalla redirigía hacia L-8, pero la solicitud no quedaba correctamente visible/asociada para el siguiente paso.

Patrón aplicado:
- `Pages/Requests/Create.cshtml` conserva `ManagementPlanId` y `ManagementId` como hidden inputs.
- La tarjeta de referencia a L-6 solo muestra "Ver Detalles"; el botón "Rehacer L-6" fue retirado porque no pertenece al flujo estable actual.
- `OnPostAsync` abre transacción.
- Si existe `ManagementPlanId`, carga el plan con `.AsTracking()` e incluye `EquipmentUnit`.
- El `EquipmentUnitId` y `LaboratoryId` se derivan del `ManagementPlan`, no del combo visual.
- Se crea `Request`.
- Se asigna `wizardPlan.RequestId = request.Id`.
- Se mueve el plan a:
  - `CurrentPhase = WizardPhase.Maintenance`
  - `CurrentState = WizardEquipmentState.AwaitingMaintenance`
- Se guarda todo en una transacción y recién después redirige al wizard Step 3.

Esto debe repetirse como criterio en los demás pasos: antes de culpar al frontend, verificar si el `ManagementPlan` se está actualizando con tracking y si el redirect conserva `ManagementId`.

#### L-8 Create — planificación inicial vs cierre real
Archivos principales:
- `Pages/Maintenances/Create.cshtml`
- `Pages/Maintenances/Create.cshtml.cs`

Decisión vigente: en L-8 Create se registra la planificación/tentativa inicial del mantenimiento. No se debe exigir información que normalmente se completa después, durante el cierre o edición del mantenimiento.

Campos opcionales en creación inicial:
- Técnico responsable (`TechnicianId`)
- Descripción del trabajo realizado
- Costos y repuestos (`CostDetails`)
- Costo real total
- Fecha de inicio real
- Fecha de finalización real
- Fecha sugerida del próximo mantenimiento
- Nivel de satisfacción
- Recomendaciones post-servicio

Patrón aplicado:
- La referencia a L-7 solo muestra "Ver Detalles"; el botón "Rehacer L-7" fue retirado.
- Ya no se agrega automáticamente una fila vacía de costos al cargar la vista.
- Los inputs de costos del template no son `required`.
- El backend elimina errores de `ModelState` de `Input.CostDetails` antes de validar, filtra filas sin concepto y solo guarda detalles realmente escritos.
- Si no hay técnico asignado, no se genera notificación de mantenimiento próximo.
- Si existe `ManagementPlanId`, el backend carga el plan con `.AsTracking()` y deriva `EquipmentUnitId`, `LaboratoryId` y `RequestId` desde el plan.
- Al guardar, el plan pasa a:
  - `CurrentPhase = WizardPhase.Exit`
  - `CurrentState = WizardEquipmentState.AwaitingDeparture`

Importante: si en el futuro se quiere exigir costo/satisfacción/comentarios, debe hacerse en `Maintenances/Edit` o en el punto real de cierre/completado, no en la creación inicial del L-8.

#### Combos bloqueados o congelados
Cuando un combo aparece congelado, bloqueado o con valores inconsistentes, revisar en este orden:

1. ¿Es preventivo o correctivo?
   - Preventivo en wizard: combos bloqueados es comportamiento esperado.
   - Correctivo: combos deben estar editables porque no siempre hay L-6 previo.

2. ¿El `.cshtml` bloquea solo cuando corresponde?
   - Correcto: `isWizard && !isCorrective`
   - Riesgoso: `isWizard` solamente, porque bloquea también correctivo.

3. ¿Hay hidden inputs para los IDs críticos?
   - `ManagementPlanId`
   - `ManagementId`
   - IDs necesarios de fase anterior cuando el select está disabled

4. ¿El backend deriva los datos desde `ManagementPlanId`?
   - Si depende solo de `Input.XId`, puede fallar cuando el select está disabled o no postea.

5. ¿La entidad que se modifica fue cargada con `.AsTracking()`?
   - El proyecto tiene NoTracking global. Si se carga sin tracking, el cambio al plan puede no persistir.

#### Rehacer L-6 y activos sanos
Archivo principal: `Pages/Verifications/Edit.cshtml.cs`

Cuando una verificación observada se corrige a `Excellent` o `Good`:
- Mostrar SweetAlert2 antes de guardar.
- Marcar `Verification.Status = Completed`.
- Soft-delete de fallas activas (`IsDeleted = true`), nunca hard-delete.
- Si había L-7 pendiente, cancelarla y soltar `RequestId`.
- Dejar el `ManagementPlan` en:
  - `CurrentPhase = WizardPhase.Verification`
  - `CurrentState = WizardEquipmentState.VerifiedGood`

Resultado esperado: el activo queda sano, no aparece en L-7, no pasa a L-8 y tampoco vuelve a L-6 porque conserva `VerificationId`.

#### Cómo debe trabajar el siguiente agente
Antes de tocar una fase del wizard:
- Leer esta sección completa.
- Abrir el PageModel del paso (`Create.cshtml.cs` o `Edit.cshtml.cs`).
- Buscar `ManagementPlanId`, `ManagementId`, `isWizard`, `isCorrective`.
- Verificar redirects.
- Verificar `.AsTracking()`.
- Verificar TempData con extensiones seguras.
- Probar con un equipo concreto desde captura del usuario, no solo con teoría.

No hacer refactor grande si el bug es de persistencia del plan. Resolver primero el flujo exacto que el usuario está probando.

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

### Entorno de producción
- **Mismo servidor**: app y PostgreSQL en la misma máquina (localhost)
- Database: `DB_Laboratorios_Univalle` (sin `_DEV` en producción)
- Crear usuario dedicado PostgreSQL: `univalle_app` con permisos limitados
- `Include Error Detail=false` en producción
- Migraciones se aplican automáticamente al arrancar (`db.Database.MigrateAsync()`)

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
| Centro de Reportes (selección de lab + descargas) | ✅ Operativo |
| Notificaciones (bell + polling 30s) | ✅ Operativo |
| Subida de imágenes (Equipment Create/Edit) | ✅ Operativo |

### Pendientes
| Item | Prioridad |
|------|-----------|
| Verificar `.AsTracking()` en `OnPostToggleWeekAsync` | 🟡 Media |
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
11. **Probar con `dotnet run`**, NO desde Visual Studio debugger (ver sección 20)

---

## 20. BUG DIAGNOSTICADO — CRASH 0xffffffff EN VISUAL STUDIO

### Problema
La aplicación crashea con código `-1 (0xffffffff)` cuando se abre/cierra el explorador de archivos (file picker) en páginas con `<input type="file">` — Equipment Create y Equipment Edit.

### Causa raíz confirmada
**Visual Studio Browser Link** forza la inyección de un WebSocket en cada página. Cuando el diálogo de archivos se abre, todo JavaScript se congela. VS detecta la desconexión del WebSocket y **fuerza la terminación del proceso Kestrel** con exit code `-1`.

- El crash **NO ocurre** cuando se ejecuta con `dotnet run` desde terminal.
- El `AppDomain.CurrentDomain.UnhandledException` handler **NO captura** este crash (es terminación externa, no excepción .NET).
- El archivo `crash.log` solo registra 1 evento: "Failed to bind to address" (puerto ocupado por instancia previa).

### Solución aplicada / pendiente
1. **`appsettings.Development.json`** ya tiene `"BrowserLink": { "Enabled": false }` (línea 11-13) — **NO es suficiente** por sí solo.
2. **Aplicado en `Program.cs`**: `builder.WebHost.UseSetting("BrowserLink:Enabled", "false");` antes de `builder.Build()` — `UseBrowserLink(false)` no está disponible en el stack actual sin agregar otra dependencia.
3. **Aplicado en `Program.cs`**: logging explícito con `ClearProviders()`, `AddConsole()` y `AddDebug()` para evitar que el logger intente escribir en Windows Event Log sin permisos y mate el arranque.
4. **Aplicado en `Program.cs`**: DataProtection persiste llaves en `DataProtectionKeys/` dentro del proyecto con `SetApplicationName("ProyectoLaboratoriosUnivalle")`; esto evita errores de DPAPI/AppData y cookies inválidas durante pruebas locales.
5. **En Visual Studio**: Tools → Options → Debugging → desmarcar "Enable Hot Reload" y "Enable Diagnostic Tools while debugging"
6. **Alternativa**: ejecutar SIEMPRE con `dotnet run` desde terminal durante desarrollo

### Para despliegue en producción
- El crash **NO ocurre** en producción (el ejecutable .exe es standalone, sin VS)
- El Browser Link solo existe en el entorno de desarrollo de VS
- Configurar `ASPNETCORE_ENVIRONMENT=Production` como variable de entorno
- Crear `appsettings.Production.json` con logging reducido y `DetailedErrors: false`

### System.Drawing.Common — Riesgo secundario
- `ReportService.cs` ya no usa `System.Drawing.Color` para estilos de celda generados desde cero; usa helpers con `ExcelColor.SetColor(alpha, r, g, b)`.
- Microsoft declara `System.Drawing.Common` como **NO soportado en ASP.NET Core** desde .NET 6 — puede causar Access Violation nativo
- **Aplicado pre-despliegue**: colores hardcodeados migrados a helpers EPPlus (`SetBg`, `SetFontColor`, `SetBorderAround`) sin `System.Drawing.Color`.
- EPPlus internamente llama a GDI+ (`gdiplus.dll`) — riesgo de crash en server-side por thread-affinity

---

## 21. DESPLIEGUE — PRODUCCIÓN

### Entorno
- **Servidor**: misma máquina local de la universidad (Windows)
- **Base de datos**: PostgreSQL en `localhost:5432`
- **Runtime**: .NET 9.0 con Kestrel self-hosted o IIS reverse proxy

### Publicar como ejecutable
```bash
# Opción A: framework-dependent (requiere .NET 9 instalado en el servidor)
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish

# Opción B: self-contained (NO requiere .NET instalado)
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish-selfcontained
```

### Configuración de producción
1. Crear `appsettings.Production.json`:
   - `"DetailedErrors": false`
   - `"Logging.LogLevel.Default": "Warning"`
   - Connection string con usuario dedicado `univalle_app` (NO `postgres`)
   - `"Include Error Detail=false"` en connection string
   - `"BrowserLink": { "Enabled": false }`

2. Variable de entorno: `ASPNETCORE_ENVIRONMENT=Production`

3. Crear usuario PostgreSQL dedicado:
```sql
CREATE USER univalle_app WITH PASSWORD '[contraseña-segura]';
GRANT CONNECT ON DATABASE "DB_Laboratorios_Univalle" TO univalle_app;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO univalle_app;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO univalle_app;
```

4. Migraciones se aplican automáticamente al arrancar (`db.Database.MigrateAsync()` en Program.cs línea 144)

5. Ejecutar como Windows Service con `nssm`:
```bash
nssm install ProyectoLaboratorios "C:\path\to\publish\Proyecto Laboratorios Univalle.exe"
nssm start ProyectoLaboratorios
```

### Seguridad en producción
- HTTPS obligatorio (Kestrel o IIS reverse proxy con certificado SSL)
- Password de PostgreSQL en user-secrets o variable de entorno (NO en appsettings.json committed)
- `Include Error Detail=false` en connection string de producción
- Crear backup de BD antes de cada migración

---

## 22. REPORTES EXCEL — ARQUITECTURA DETALLADA

### Helper `SavePackage()`
```csharp
private static byte[] SavePackage(ExcelPackage package)
{
    using var stream = new MemoryStream();
    package.SaveAs(stream);
    return stream.ToArray();
}
```
- **NUNCA** usar `package.GetAsByteArray()` — causa crash 0xffffffff
- **NUNCA** usar `Worksheets.Add(name, sourceSheet)` — causa corrupción XML

### Reportes con template (L-7, L-8, L-48, Adquisición)
- Patrón: `new ExcelPackage(new FileInfo(templatePath))`
- Limpiar celdas con `.Value = null` (NO `.Merge = false` en rangos merged)
- Guardar con `SavePackage(package)`

### Reportes from-scratch (L-6, L-3)
- Patrón: `new ExcelPackage()` + `Worksheets.Add("nombre")`
- Colores con helpers EPPlus (`SetBg`, `SetFontColor`, `SetBorderAround`) sin `System.Drawing.Color`
- Guardar con `SavePackage(package)`

### Estado de reportes
| Reporte | Template | Estado |
|---------|----------|--------|
| L-3 Salida | Desde cero | ✅ Operativo (soporta DepartureItems multi-ítem) |
| L-6 Verificación | Desde cero | ✅ Operativo |
| L-7 Solicitud Mantenimiento | `wwwroot/templates/L7.xlsx` | ✅ Operativo (template 138KB) |
| L-8 Kardex | `wwwroot/templates/L8.xlsx` | ✅ Operativo |
| L-48 Gantt | `wwwroot/templates/L48.xlsx` | ✅ Operativo |
| L-12 Adquisición | `wwwroot/templates/Adquisicion.xlsx` | ✅ Operativo |

---

## 23. NOTIFICACIONES — ARQUITECTURA

### Modelo
- `Notification` (Id, UserId, Title, Message, ActionUrl, IconClass, IsRead, CreatedAt)
- FK a `User` via `UserId`

### API endpoints
- `GET /Api/Notifications?handler=Unread` — lista de no leídas (con filtro opcional `&filter=X`)
- `GET /Api/Notifications?handler=MarkAsRead&id={id}` — marcar como leída

### Clientes
- `_Layout.cshtml`: polling cada 30s con `setInterval(loadNotifications, 30000)`
- `Acquisitions/Index.cshtml`: filtro `&filter=Adquisición`
- `Maintenances/Index.cshtml`: filtro `&filter=Mantenimiento`

### Productores
- `Acquisitions/Create.cshtml.cs` — al crear adquisición
- `Maintenances/Create.cshtml.cs` — al crear mantenimiento programado dentro de 7 días

---

## 24. L-3 SALIDAS — ARQUITECTURA (REFACTORIZADO EN SESIÓN ACTUAL)

### ¿Qué se hizo?
Se refactorizó completamente el módulo L-3 Salidas para integrarlo de forma coherente al Wizard de Mantenimiento. Los cambios aplican tanto al flujo **individual** (por equipo) como al flujo **masivo** (por laboratorio).

---

### Tabs de Tipo de Salida
- **Solo en modo wizard**: tabs "Préstamo Interno / Mantenimiento Externo" están **eliminados visualmente**.
- **Fuera del wizard**: tabs visibles y funcionales — sin cambios.
- En wizard el tipo se **infiere automáticamente** del técnico de L-8:
  - Técnico `Intern` → `DepartureType.InternalMaintenance = 5`
  - Técnico `Extern` → `DepartureType.ExternalMaintenance = 3`

---

### Técnico en Wizard (L-3 Create individual — `Pages/Departures/Create.cshtml.cs`)
- **SIEMPRE** derivado de `plan.Maintenance.TechnicianId` — nunca del form POST.
- En POST del wizard, `EquipmentUnitId`, `LaboratoryId` y `FacultyId` se derivan desde `ManagementPlan.EquipmentUnit`.
- Si L-8 sin técnico → alerta roja + botón "Asignar Técnico en L-8". Botón guardar `disabled`.
- Si L-8 con técnico → card verde con nombre del técnico + badge del tipo inferido.
- **Bug corregido (sesión actual)**: `FindAsync` no carga relaciones, `plan.Maintenance` era siempre `null` bloqueando todo registro. Fix: en wizard se usa `.Include(p => p.Maintenance).AsTracking()`.

---

### MassCreate — Registro Masivo (`Pages/Departures/MassCreate.cshtml`)
- Masivo por laboratorio — mismo patrón que `Verifications/MassCreate`.
- En el wizard, L-3 queda **centralizado exclusivamente en MassCreate**. `Pages/Index.cshtml` Step 4 ya no muestra la acción individual `/Departures/Create` por equipo.
- `Pages/Departures/Create` queda reservado para uso standalone/fuera del wizard; no debe ser el camino principal del Paso 4.
- **Equipos cargados**: `WizardPhase.Exit` + `WizardEquipmentState.AwaitingDeparture`.
- **Fechas globales obligatorias** (`DepartureDate`, `EstimatedReturnDate`) — aplican a todos los equipos de la sesión.
- Mínimo 1 equipo incluido para poder guardar; el `ProductName` se deriva en backend desde `ManagementPlan.EquipmentUnit.Equipment`, no desde texto posteado.
- Equipos sin técnico en L-8: aparecen deshabilitados con badge rojo.
- En POST masivo, unidad persistida se deriva desde `ManagementPlan.EquipmentUnitId` — no desde el form.
- En wizard, `Producto / Ítem de Salida` se muestra como combo deshabilitado con el equipo seleccionado; se conserva hidden solo para UI/model binding defensivo.
- Sin columna "Observaciones" (no corresponde al Form L3.3).
- Disponible: **Wizard Paso 4** (botón "Salida Masiva por Laboratorio") y **standalone** desde `Departures/Index`.
- Al guardar: avanza plan a `WizardPhase.Kardex` + `WizardEquipmentState.AwaitingKardex`.

---

### Kardex Create — Cierre formal del mantenimiento (`Pages/Kardex/Create.cshtml` + `.cs`)
Kardex ya no es solo un cambio de estado del activo. En el wizard, Paso 5 Kardex es el **cierre formal de L-8** y utiliza una interfaz tipo `Maintenances/Edit` para completar los datos faltantes antes de pasar a desembolso.

En Kardex se debe completar obligatoriamente:
- Técnico responsable.
- Fecha programada, inicio real y finalización real.
- Fecha real de devolución L-3 (`Departure.ActualReturnDate`).
- Descripción de trabajos realizados.
- Detalle de costos/insumos con al menos 1 ítem real y total > 0.
- Próximo mantenimiento sugerido.
- Nivel de satisfacción.
- Recomendaciones.
- Observaciones finales.
- Estado de mantenimiento queda forzado a `MaintenanceStatus.Completed`.

Al cerrar Kardex correctamente:
- Actualiza `Maintenance` con todos los datos de cierre, calcula `ActualCost` desde `CostDetails` y marca `Status = Completed`.
- Marca la **Salida L-3 vinculada** (`plan.DepartureId`) como `LoanStatus.Returned`.
- Registra `Departure.ActualReturnDate` con la fecha real de devolución.
- Crea `EquipmentStateHistory` con `Status = EquipmentStatus.Operational` y resumen del cierre.
- Marca `EquipmentUnit.CurrentStatus = EquipmentStatus.Operational`.
- Avanza el plan a `WizardPhase.Disbursement` + `WizardEquipmentState.AwaitingDisbursement`.

Validaciones críticas:
- No se puede cerrar Kardex sin `MaintenanceId` vinculado.
- `EndDate >= StartDate`.
- `ActualReturnDate >= Departure.DepartureDate` cuando existe L-3 vinculada.
- `SuggestedNextMaintenanceDate > EndDate`.
- Los datos de ubicación/equipo se reconstruyen desde `ManagementPlan`, no desde HTML.
- El botón usa submit real con SweetAlert2 por evento `submit`, no `onclick` bloqueante.

---

### DepartureType enum (actualizado)
```
ExternalLoan = 1        — Préstamo externo
InternalLoan = 2        — Préstamo interno (fuera del wizard)
ExternalMaintenance = 3 — Salida con técnico Externo (wizard)
DefinitiveExit = 4      — Baja definitiva
InternalMaintenance = 5 — Salida con técnico Interno (wizard) ← NUEVO, sin migración
```

---

### Corrección L-3 Producto/avance (8 Mayo 2026)
- `Pages/Departures/Create.cshtml`: en wizard, el producto del ítem se muestra como combo deshabilitado con el equipo seleccionado; fuera del wizard sigue siendo texto editable.
- `Pages/Departures/Create.cshtml`: el submit confirmado usa submit nativo para evitar que `jqBootstrapValidation` bloquee campos que el servidor reconstruye en wizard.
- `Pages/Departures/Create.cshtml`: estabilización final de submit, botón `Registrar Salida L-3` es `type="submit" formnovalidate`; SweetAlert ya no es requisito para llegar al POST.
- `Pages/Departures/Create.cshtml`: SweetAlert2 usa `onclick` con `type="button"` y luego `document.getElementById('departureForm').submit()` nativo; no interceptar evento `submit` porque vuelve a bloquear el POST con validaciones JS.
- `Pages/Departures/Create.cshtml`: `EstimatedReturnDate` ya no usa `border-danger` fijo; el rojo solo debe venir de validación real.
- `Pages/Departures/Create.cshtml.cs`: el POST wizard reconstruye `DepartureItem.ProductName` desde `ManagementPlan.EquipmentUnit.Equipment` e inventario, evitando depender del HTML.
- `Pages/Departures/Create.cshtml.cs`: si el wizard no resuelve `ManagementPlanId`, muestra error explícito con el valor recibido.
- `Pages/Departures/MassCreate.cshtml`: la columna `Producto / Ítem de Salida` usa select deshabilitado por fila, con hidden defensivo.
- `Pages/Departures/MassCreate.cshtml`: solo filas con técnico generan inputs `Rows[]`; checkbox `Include` SIN hidden `false` (checked envía `true`, unchecked no envía nada y el modelo usa default `false`).
- `Pages/Departures/MassCreate.cshtml`: `EstimatedReturnDate` ya no usa `border-danger` fijo y el submit confirmado usa submit nativo.
- `Pages/Departures/MassCreate.cshtml`: SweetAlert2 usa `onclick` con `type="button"` y luego `HTMLFormElement.prototype.submit.call(form)` para ejecutar submit nativo robusto. No interceptar evento `submit` porque vuelve a bloquear el POST con validaciones JS.
- `Pages/Departures/MassCreate.cshtml`: si SweetAlert2 no carga, el flujo masivo tiene fallback a submit directo despues de validar fechas/seleccion.
- `Pages/Departures/MassCreate.cshtml.cs`: el POST masivo registra por filas incluidas, carga `Maintenance` y `EquipmentUnit.Equipment` con tracking, deriva el producto real, crea `DepartureItem`, marca la unidad `OnLoan` y avanza a Kardex.
- `Pages/Departures/MassCreate.cshtml.cs`: si `created == 0`, muestra error y no redirige como éxito silencioso.
- `Pages/Departures/MassCreate.cshtml.cs`: si no llegan filas incluidas, muestra diagnóstico de `Rows[]` recibidas para depurar binding.

---

### Flujo completo L-3 → Kardex
```
L-8 con TechnicianId asignado
    ↓  Plan: WizardPhase.Exit / AwaitingDeparture

Paso 4 Wizard:
  • L-3 MassCreate (masivo) — único camino del wizard, por laboratorio, fechas globales obligatorias
    ↓  Plan: WizardPhase.Kardex / AwaitingKardex

Paso 5 Wizard — Kardex Create:
  • Crea EquipmentStateHistory
  • Marca equipo como Operativo
  • Cierra Departure: LoanStatus.Returned + ActualReturnDate
    ↓  Plan: WizardPhase.Disbursement / AwaitingDisbursement
```

---

### Reporte Físico L3.3 — Mapeo (PENDIENTE de implementar)
El sistema ya captura todos los datos para generar el Form L3.3 físico: **"SOLICITUD DE INSUMOS Y UTENSILIOS GASTRONÓMICOS — Universidad del Valle"**.

| Campo del Form | Origen | Estado |
|---|---|---|
| SEDE / SUB SEDE | Hardcoded `"Cochabamba / Tiquipaya"` | ⏳ Pendiente reporte |
| FACULTAD | `lab.Faculty.Name` | ⏳ Pendiente reporte |
| DEPARTAMENTO | Hardcoded `"Gastronomía"` | ⏳ Pendiente reporte |
| CARRERA | `unit.Career.Name` (via `EquipmentUnit.Career`) | ⏳ Pendiente reporte |
| LABORATORIO DE | `lab.Name` | ⏳ Pendiente reporte |
| DOCENTE / ESTUDIANTE | `plan.Maintenance.Technician.Name` | ⏳ Pendiente reporte |
| DÍA | `DepartureDate.DayOfWeek` (en español) | ⏳ Pendiente reporte |
| GESTIÓN | `management.Semester + "/" + management.Year` | ⏳ Pendiente reporte |
| PRODUCTO / CANTIDAD / UNIDAD | `DepartureItem.ProductName / Quantity / UnitOfMeasure` | ⏳ Pendiente reporte |
| MATERIA, FECHA CRONOGRAMA, PRÁCTICA, GRUPO, CÓDIGO, HORARIO | ⬜ Vacíos — llenado manual en papel | — |
| ENTREGADO POR, RECIBIDO POR, Nro. SOLICITUD | ⬜ Vacíos — personal de laboratorio, manual | — |
| DEVOLUCIÓN, SALDO en ítems | ⬜ Vacíos — llenado manual en papel | — |

> **Próximo paso**: generar el reporte L3.3 en Excel usando la Skill `reporting` (ClosedXML/EPPlus). Plantilla: `FORM L3.3 v3.0`.

---

### Bug Fixes Sesión Actual (8 Mayo 2026 — segunda ronda)

**1. ModelState.IsValid bloqueaba silenciosamente (Create individual)**
- **Causa**: los select disabled en wizard no postean datos. FacultyId, LaboratoryId, EquipmentUnitId, BorrowerId llegaban como 0 al servidor. Los atributos Required rechazaban el 0 y ModelState.IsValid retornaba false silenciosamente.
- **Fix**: OnPostAsync reestructurado: primero carga plan con Include, reconstruye valores desde BD, limpia las keys de ModelState con ModelState.Remove, y luego valida. Si falla, muestra campos exactos en TempData.Error.

**2. Checkbox binding order (MassCreate)** — CORREGIDO 8 Mayo (sexta ronda)
- **Causa original**: checkbox antes de hidden. ASP.NET toma el primer valor cuando ambos llegan con el mismo name.
- **Fix anterior**: hidden ANTES del checkbox. Unchecked envia solo false, checked envia false+true y ASP.NET toma true.
- **Fix definitivo**: eliminar hidden `value="false"` completamente. Solo checkbox `value="true"`: checked envía `true`, unchecked no envía nada (modelo usa default `false`). Esto elimina toda ambigüedad de binding.

**3. Indices no contiguos en Rows (MassCreate)**
- **Causa**: filas sin tecnico no generaban inputs pero rowCount incrementaba, creando huecos que cortan el model binding de ASP.NET.
- **Fix**: rowCount solo incrementa si item.hasTechnician es true, garantizando indices contiguos.

**4. Continuidad L-3/Kardex (8 Mayo 2026 — tercera ronda)**
- `Pages/Departures/MassCreate.cshtml`: eliminado hidden `Rows[i].Include=false`; solo checkbox `true` (patrón definitivo aplicado en sexta ronda).
- `Pages/Kardex/Create.cshtml.cs`: Kardex ya no usa `maintenance.CostDetails.Remove(existing)` al sincronizar costos omitidos. Para preservar historial, los costos retirados se desvinculan con `MaintenanceId = null`, `Maintenance = null` y `LastModifiedDate`.
- Verificacion: `dotnet build -o "C:\Users\monte\AppData\Local\Temp\opencode\continue-build"` completo con 0 errores; quedan warnings existentes CS8602/CS8601/CS8629 y NETSDK1194 por salida temporal de solucion.

**5. Limpieza hard-delete segura (8 Mayo 2026 — cuarta ronda)**
- `Pages/Requests/Edit.cshtml.cs`: reemplazado `_context.CostDetails.RemoveRange(...)` por desvinculacion de costos (`RequestId = null`, `Request = null`, `LastModifiedDate`).
- `Pages/Acquisitions/Edit.cshtml.cs`: reemplazado `_context.CostDetails.RemoveRange(...)` por desvinculacion de costos y migrado `TempData["Success"]` a `TempData.Success(...)`.
- `Pages/Maintenances/Edit.cshtml.cs`: reemplazados `maintenanceDB.CostDetails.Remove(detail)` y `_context.Remove(detail)` por desvinculacion de costos (`MaintenanceId = null`, `Maintenance = null`, `LastModifiedDate`).
- `Pages/Acquisitions/Delete.cshtml.cs`: reemplazado `_context.Requests.Remove(request)` por borrado logico funcional: `Status = RequestStatus.Cancelled`, `RejectionReason` descriptivo y `LastModifiedDate`.
- Residual intencional: `EquipmentNotes.RemoveRange(...)` y `VerificationCheckResults.RemoveRange(...)` permanecen porque esas entidades tienen FK requerida y no tienen `Status`/`IsDeleted`; corregirlas bien requiere migracion/modelado, no un cambio silencioso.
- Verificacion: `dotnet build -o "C:\Users\monte\AppData\Local\Temp\opencode\hard-delete-build"` completo con 0 errores; quedan warnings existentes CS8602/CS8601/CS8629 y NETSDK1194 por salida temporal de solucion.

**6. Centralizacion L-3 Wizard (8 Mayo 2026 — quinta ronda)**
- `Pages/Index.cshtml`: Step 4 ya no muestra botones individuales `Registrar Salida` hacia `/Departures/Create`; lista los equipos pendientes y ofrece una sola accion principal: `Registrar Salida Masiva L-3`.
- `Pages/Index.cshtml`: se agrego aviso visual indicando que L-3 del wizard se registra unicamente en modo masivo por laboratorio.
- `Pages/Departures/MassCreate.cshtml`: submit masivo reforzado con `HTMLFormElement.prototype.submit.call(form)`, boton se deshabilita al confirmar y muestra estado `Registrando...`.
- `Pages/Departures/MassCreate.cshtml`: validaciones JS tienen fallback con `alert()` si SweetAlert2 no esta disponible, evitando que el boton quede sin accion.
- Verificacion: `dotnet build -o "C:\Users\monte\AppData\Local\Temp\opencode\l3-central-build"` completo con 0 errores; quedan warnings existentes CS8602/CS8601/CS8629 y NETSDK1194 por salida temporal de solucion.

**7. SweetAlert2 result.isConfirmed inexistente en v7.19.3 — CRÍTICO (8 Mayo 2026 — sexta ronda)**
- **Causa**: SweetAlert2 v7.19.3 NO tiene la propiedad `result.isConfirmed` (se introdujo en v8+). En v7 la confirmación se evalúa con `result.value` (booleano). Todos los `.then(result => { if (result.isConfirmed) {...} })` nunca entraban al bloque porque `isConfirmed` siempre es `undefined` en v7. El diálogo aparecía visualmente, el usuario confirmaba, pero el submit NUNCA se ejecutaba.
- **Fix**: Reemplazado en 8 archivos por `if (result && (result.isConfirmed === true || result.value === true))`. Retrocompatible con v7 y v8+. Lista de archivos corregidos:
  - `Pages/Departures/MassCreate.cshtml:296`
  - `Pages/Departures/Create.cshtml:452`
  - `Pages/Kardex/Create.cshtml:270`
  - `Pages/Verifications/Edit.cshtml:161`
  - `Pages/Verifications/Create.cshtml:247`
  - `Pages/Managements/Details.cshtml:608`
  - `Pages/Maintenances/Edit.cshtml:424`
  - `Pages/Acquisitions/Create.cshtml:344`
- **Fixes complementarios MassCreate**:
  - Eliminado hidden `Include=false` de checkbox binding (deja solo checkbox `value="true"` — checked envía true, unchecked no envía nada).
  - Hidden inputs `PlanId` y `EquipmentUnitId` movidos dentro del `<td>` de Producto (estaban directamente en `<tr>`, HTML inválido en tablas — el navegador podía reubicarlos y el backend recibía `PlanId=0`).
- **Documentado en**: sección 4 del context como regla obligatoria + tabla NUNCA sección 5.
- Verificacion: `dotnet build -o "C:\Users\monte\AppData\Local\Temp\opencode\sweetalert-fix-build"` completo con 0 errores.

**8. L-8 Create: técnico obligatorio y botón SweetAlert2 estable (8 Mayo 2026 — séptima ronda)**
- **Decisión**: En el wizard, `TechnicianId` es obligatorio en L-8 Create porque L-3 deriva el responsable/recibe del equipo desde `Maintenance.TechnicianId`. Fuera del wizard puede seguir siendo opcional.
- `Pages/Maintenances/Create.cshtml`: el campo Técnico Responsable en wizard muestra `*`, usa `required` y ya no aparece como `Kardex`.
- `Pages/Maintenances/Create.cshtml.cs`: `OnPostAsync` valida `TechnicianId` cuando `isWizard == true`; también bloquea equipos sin `EquipmentUnit.LaboratoryId` para evitar que L-3 quede sin laboratorio.
- `Pages/Maintenances/Create.cshtml`: `confirmMaintenanceSubmit` queda publicado como `window.confirmMaintenanceSubmit()` para que el botón `onclick` lo encuentre. Se eliminó un cierre `});` extra que rompía el bloque JavaScript.
- `Pages/Maintenances/Create.cshtml`: SweetAlert2 de L-8 usa patrón v7/v8 (`result.isConfirmed || result.value`), fallback si `Swal` no carga y submit nativo `HTMLFormElement.prototype.submit.call(form)`.
- Correccion posterior: el envio de L-8 Create se estabilizo con `@Html.AntiForgeryToken()` explicito y un boton real oculto `type="submit"` + `formnovalidate`; SweetAlert2 solo confirma y luego dispara ese submit real. Esto evita botones que muestran el modal pero no hacen POST.
- Correccion final L-8 Create: se retiro `jqBootstrapValidation` de `Pages/Maintenances/Create.cshtml` para evitar handlers acumulados/conflictos con unobtrusive validation. El boton visible es `type="submit"`; el formulario intercepta el evento `submit`, muestra SweetAlert2 v7-compatible y, al confirmar, ejecuta `HTMLFormElement.prototype.submit.call(form)` para hacer POST nativo sin pasar por validadores JS.
- La regla `MaintenanceStatus.Completed` requiere `CompletionPercentage == 100` ahora se valida en backend (`OnPostAsync`), no en JavaScript, para que el POST siempre llegue y el usuario vea errores reales de `ModelState`.
- `Pages/Maintenances/Create.cshtml`: L-48 Tasks ya no tienen el input de descripción dentro del `<label>` del checkbox; ahora el texto es editable como `form-control-sm` real.
- `Pages/Maintenances/Create.cshtml`: las plantillas ocultas `INDEX` de Tasks/CostDetails quedan con inputs `disabled`; al agregar filas se habilitan solo los controles reales para evitar validaciones/envios fantasmas desde templates ocultos.
- `Pages/Maintenances/Create.cshtml`: estado `Completed` en JS se compara como valor enum `"2"`, no como string `"Completed"`.
- `Pages/Departures/MassCreate.cshtml`: diagnóstico visible para equipos sin técnico o sin laboratorio; solo filas completas generan inputs y pueden incluirse en L-3.
- Verificacion: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\maintenance-submit-event"` completo con 0 errores.

**9. L-7 → L-8: avance real del ManagementPlan (9 Mayo 2026)**
- **Causa**: `Pages/Requests/Create.cshtml.cs` redirigía al wizard Step 3 aunque el `ManagementPlan` no se hubiera actualizado. Visualmente parecía que L-7 guardaba, pero el activo seguía en `WizardPhase.TechnicalRequest` y no aparecía correctamente en L-8.
- `Pages/Requests/Create.cshtml`: se agregó hidden `ManagementId` para preservar contexto en POST.
- `Pages/Requests/Create.cshtml.cs`: `OnPostAsync` ahora carga `ManagementPlan` con `.AsTracking()` e incluye `Management` y `EquipmentUnit.Laboratory` antes de validar.
- `Pages/Requests/Create.cshtml.cs`: en wizard, `FacultyId`, `LaboratoryId` y `EquipmentUnitId` se reconstruyen desde el plan, y se limpian esas keys de `ModelState` porque los selects preventivos están disabled.
- `Pages/Requests/Create.cshtml.cs`: el guardado L-7 es transaccional. Crea `Request`, asigna `plan.RequestId`, cambia `plan.CurrentPhase = WizardPhase.Maintenance` y `plan.CurrentState = AwaitingMaintenance` en la misma transacción.
- Si `isWizard == true` y no hay plan válido en flujo preventivo, ya no redirige a Step 3 falsamente; muestra `TempData.Error`.
- Correctivo sin plan conserva comportamiento: crea `ManagementPlan` nuevo en fase `Maintenance` con la solicitud vinculada.
- `LoadLists` ahora repuebla Facultad/Laboratorio/Unidad con valores seleccionados para errores de validación.
- Verificacion: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\l7-l8-flow-fix"` completo con 0 errores.

---

*Ultima actualizacion: 9 Mayo 2026 — Kardex cierre L-8 estabilizado: tareas L-48 editables reales, validaciones completas, costos calculados en servidor, navegación de wizard y UI Kardex en español. Build temporal 0 errores.*


### Actualizaciones Visuales en L-7 (Preventivo)
Se agregaron dos elementos interactivos en el registro de solicitud t�cnica (L-7) dentro del wizard preventivo:
1. **Historial de Kardex**: Un bot�n que abre en una nueva pesta�a el hist�rico de la unidad f�sica (/EquipmentUnits/Kardex?id=X) sin perder el progreso del formulario.
2. **Notas del Fabricante / Prevenci�n**: Un panel alert que renderiza las notas informativas asociadas al modelo l�gico (Equipment) del activo en cuesti�n. Esto gu�a al t�cnico en la generaci�n de sugerencias.

### Estabilización L-6/L-7 (9 Mayo 2026)
- `Pages/Requests/Create.cshtml`: el submit de L-7 ahora muestra confirmación SweetAlert2 compatible con v7/v8 y ejecuta `HTMLFormElement.prototype.submit.call(form)` para evitar bloqueos de validación JavaScript. Se retiró nuevamente el botón `Rehacer L-6` porque el flujo estable documentado solo permite ver la L-6 vinculada desde L-7.
- `Pages/Verifications/Create.cshtml`: el submit individual de L-6 usa confirmación SweetAlert2 v7-compatible y submit nativo cuando no hay fallas registradas. El formulario preserva `ManagementId` en hidden y en el botón `Volver al Wizard`.
- `Pages/Verifications/Create.cshtml.cs`: se agregó `ManagementId` con `SupportsGet`, se resuelve la gestión por `ManagementId` antes del fallback a gestión activa y todos los redirects del wizard desde L-6 preservan `ManagementId`.
- `Pages/Verifications/MassCreate.cshtml`: se agregó confirmación SweetAlert2 al guardado masivo, fallback con `alert()`, estado visual `Guardando...` y submit nativo. Los hidden inputs `PlanId`, `EquipmentUnitId` y `Observations` fueron movidos dentro del primer `<td>` de cada fila dinámica para cumplir HTML válido y evitar binding `PlanId=0`.
- `Pages/Verifications/MassCreate.cshtml.cs`: se agregó `ManagementId` con `SupportsGet`, el AJAX `EquipmentByLab` puede filtrar por gestión explícita y los redirects masivos preservan `ManagementId`.
- `Pages/Index.cshtml` y `Pages/Shared/_WizardStep.cshtml`: los filtros de laboratorio, flechas del wizard, acción L-6 masiva y acciones genéricas del wizard conservan `ManagementId`.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\l6-l7-stabilize"` completo con 0 errores. Persisten warnings existentes de nullability.

### Kardex / Cierre de Mantenimiento (9 Mayo 2026)
- `Pages/Kardex/Create.cshtml.cs`: Kardex ahora carga `Maintenance.Tasks` junto a costos, técnico, salida L-3 y unidad/laboratorio. `InputModel` incluye `Tasks` y el avance L-48 se recalcula en servidor antes de guardar.
- `Pages/Kardex/Create.cshtml.cs`: si un mantenimiento no tiene tareas L-48, Kardex presenta 4 tareas base: limpieza y desinfección, calibración y ajustes, pruebas de funcionamiento y revisión final de seguridad.
- `Pages/Kardex/Create.cshtml.cs`: cierre bloquea con mensajes específicos si falta técnico, fechas, devolución L-3, descripción, costos reales, tareas L-48, próximo mantenimiento, satisfacción, recomendaciones u observaciones finales. También valida `EndDate >= StartDate`, `ActualReturnDate >= DepartureDate`, próximo mantenimiento posterior a finalización, total de costos > 0 y L-48 al 100%.
- `Pages/Kardex/Create.cshtml.cs`: costos se normalizan y el total real se calcula únicamente en backend con `Quantity * UnitPrice`; `Input.ActualCost` posteado desde HTML no es fuente de verdad.
- `Pages/Kardex/Create.cshtml.cs`: tareas existentes se actualizan por `Id` y tareas nuevas se agregan. No se hace hard-delete de `MaintenanceTask` porque no tiene soft-delete y preserva historial.
- `Pages/Kardex/Create.cshtml`: los 4 checkboxes fijos fueron reemplazados por tareas L-48 editables con descripción, checkbox, barra de avance y botón para agregar tarea.
- `Pages/Kardex/Create.cshtml`: los inputs de costos en templates ocultos quedan `disabled` hasta agregarse a la tabla real; el cálculo JS solo recorre `#costsContainer .cost-row`, evitando distorsiones por plantillas ocultas. Hidden inputs de costos quedan dentro de `<td>` válido.
- `Pages/Kardex/Create.cshtml`: submit usa SweetAlert2 v7-compatible, fallback si `Swal` no carga y `HTMLFormElement.prototype.submit.call(form)` para POST nativo robusto.
- `Pages/Kardex/Create.cshtml`: se agregó botón `Volver al Wizard` con `ShowWizard=true`, `Step=5`, `SelectedLabId` y `ManagementId`. Al cerrar correctamente redirige a Step 6 preservando laboratorio y gestión.
- `Helpers/EnumHelper.cs`: `GetDisplayName(Enum value)` ahora es público para que las vistas rendericen `[Display(Name="...")]` y no `.ToString()` en inglés.
- `Pages/EquipmentUnits/Kardex.cshtml`: estados, tipo de mantenimiento y satisfacción se muestran en español usando `EnumHelper.GetDisplayName`. La página acepta contexto opcional `isWizard`, `managementId`, `selectedLabId`, `returnStep` y muestra `Volver al Wizard` cuando corresponde.
- `Pages/Kardex/Edit.cshtml` y `.cshtml.cs`: edición de Kardex conserva contexto de wizard (`IsWizard`, `ManagementId`, `SelectedLabId`, `ReturnStep`) y al cancelar/guardar vuelve al paso 5 si viene del wizard.
- `Pages/Shared/_WizardStep.cshtml`: acciones de Kardex preservan `ManagementId`, `SelectedLabId` y contexto para histórico del equipo; textos de Kardex pendientes usan español (`Pendiente`) en lugar de `N/A`.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\kardex-fix"` completo con 0 errores. Persisten warnings existentes de nullability.

### Kardex L-48 / L-12 / Completados Wizard (9 Mayo 2026)
- `Models/MaintenanceTask.cs`: `IsDeleted` queda persistido mediante nueva migración PostgreSQL `20260509094006_AddMaintenanceTaskSoftDelete`; las tareas L-48 se eliminan lógicamente, nunca con hard-delete.
- `Pages/Kardex/Create.cshtml`: el cálculo de avance L-48 ahora recorre solo `#tasksContainer .maintenance-check`, evitando contar el checkbox del template oculto. Esto corrige el avance falso del 50% cuando solo había una tarea real.
- `Pages/Kardex/Create.cshtml.cs`: Kardex filtra tareas activas (`!IsDeleted`), recalcula porcentaje solo con tareas visibles y `SyncTasks` marca como `IsDeleted = true` las tareas existentes retiradas de la UI.
- `Pages/Kardex/Edit.cshtml` y `.cshtml.cs`: edición de Kardex permite añadir, modificar y quitar tareas L-48. El backend localiza el plan por `KardexHistoryId`/mantenimiento, sincroniza tareas con soft-delete y actualiza los checks legacy/porcentaje.
- `Pages/EquipmentUnits/Kardex.cshtml` y `.cshtml.cs`: el histórico de Kardex incluye tareas L-48 activas por mantenimiento, con estado completado/pendiente, además de costos y satisfacción.
- `Pages/Requests/Create.cshtml`: el botón `Ver Histórico` usa ruta Razor `/EquipmentUnits/Kardex/{id}` y conserva `isWizard`, `ManagementId`, `SelectedLabId` y `returnStep=2`.
- `Pages/Acquisitions/Create.cshtml` y `.cshtml.cs`: L-12 reconstruye unidad, laboratorio, facultad y mantenimiento desde `ManagementPlanId` antes de validar; importa costos desde L-8, registra la solicitud L-12, asigna `AcquisitionRequestId` y cierra el plan con `WizardPhase.Disbursement`, `WizardEquipmentState.Completed` y `ManagementPlanStatus.Completed`.
- `Pages/Acquisitions/Create.cshtml`: confirmación SweetAlert2 mantiene patrón v7/v8 y usa `HTMLFormElement.prototype.submit.call(form)` con fallback si `Swal` no está disponible.
- `Pages/Index.cshtml` y `.cshtml.cs`: el wizard muestra sección `Procesos Completados` con recapitulación de equipos completados y accesos rápidos a L-6, L-7, L-8, L-3, Kardex histórico y L-12 cuando existan.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\kardex-l12-completed-fix"` completo con 0 errores. Persisten warnings existentes de nullability.

### Sistema de Borradores — Infraestructura + Piloto Kardex (9 Mayo 2026)
- `Models/ManagementPlan.cs`: se agregaron campos explícitos para borradores del wizard: `IsDraft`, `DraftPhase`, `DraftSavedAt` y `DraftSummary`.
- Nueva migración PostgreSQL: `Data/PostgresMigrations/20260509211650_AddManagementPlanDraftFields.cs`. No se modificaron migraciones existentes.
- `Pages/Shared/_WizardStep.cshtml`: las tarjetas del wizard muestran badge `Borrador` cuando `ManagementPlan.IsDraft == true`, incluyendo tooltip con `DraftSummary`.
- Piloto implementado en `Pages/Kardex/Create.cshtml.cs`:
  - `OnPostDraftAsync`: guarda avance parcial de Kardex sin exigir campos finales, sin crear `EquipmentStateHistory`, sin devolver L-3, sin completar mantenimiento y sin avanzar a desembolso.
  - `OnPostCloseAsync`: mantiene validaciones completas, cierra L-8, devuelve L-3, crea histórico Kardex, marca equipo operativo y avanza a L-12.
  - El borrador mantiene `CurrentPhase = WizardPhase.Kardex`, `CurrentState = AwaitingKardex`, `PlanStatus = InProgress`, `Maintenance.Status = InProgress` e identifica el plan con `IsDraft = true`.
  - Al cierre definitivo se limpia el borrador (`IsDraft = false`, `DraftPhase/DraftSavedAt/DraftSummary = null`).
  - `ActualReturnDate` ya no se precarga con `DateTime.Today`; solo se guarda en borrador si el usuario realmente la llenó.
- `Pages/Kardex/Create.cshtml`: se agregaron dos botones reales dentro del mismo form: `Guardar Borrador` (`asp-page-handler="Draft"`) y `Concluir Mantenimiento` (`asp-page-handler="Close"`).
- La confirmación JavaScript de Kardex detecta el botón submitter, usa SweetAlert2 v7/v8-compatible y ejecuta submit nativo preservando el `formaction` del handler correcto.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\draft-kardex-pilot"` completo con 0 errores. Persisten warnings existentes de nullability.

### Wizard Completados como Fase 7 (9 Mayo 2026)
- `Pages/Index.cshtml`: `Completados` deja de ser una sección inferior suelta y pasa a ser una fase propia del wizard (`Step = 7`) después de `Desembolso`.
- Stepper preventivo: `Progreso → L6 → L7 → L8 → L3 → Kardex → Desembolso → Completados`.
- Stepper correctivo: `Progreso → L7 → L8 → L3 → Kardex → Desembolso → Completados`.
- `Pages/Index.cshtml`: la tabla de `Procesos Completados` se renderiza solo dentro de `Model.Step == 7`; ya no se duplica debajo del wizard.
- `Pages/Index.cshtml`: la flecha derecha se muestra hasta Step 6 y avanza a Step 7; en Step 7 se oculta y queda botón `Volver al Dashboard`.
- `Pages/Index.cshtml.cs`: `OnPostNextStep()` limita el avance máximo a `Step = 7`, evitando saltos a Step 8.
- `Pages/Managements/Details.cshtml`: el card verde `Completados` del dashboard ya no envía a L-48/Kardex; ahora abre `/Index?ShowWizard=true&Step=7&ManagementId={id}`.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\completed-step7"` completo con 0 errores. Persisten warnings existentes de nullability.

### Sistema de Borradores — L-8 y L-12 (9 Mayo 2026)
- `Pages/Maintenances/Create.cshtml.cs`: L-8 ahora tiene `OnPostDraftAsync` y un flujo compartido de guardado. El borrador guarda/actualiza el `Maintenance` vinculado al `ManagementPlan`, costos y tareas L-48 sin avanzar a L-3, sin cambiar el estado físico del equipo a mantenimiento y sin generar notificaciones.
- `Pages/Maintenances/Create.cshtml.cs`: el guardado final de L-8 reutiliza un mantenimiento existente si venía de borrador; limpia `IsDraft`, avanza a `WizardPhase.Exit` + `AwaitingDeparture` y mantiene `ManagementId`/laboratorio en el redirect.
- `Pages/Maintenances/Create.cshtml`: se agregaron botones reales `Guardar Borrador` y `Registrar Mantenimiento`. La confirmación detecta el submitter, preserva el handler con `formaction`, usa SweetAlert2 v7/v8-compatible y ejecuta `HTMLFormElement.prototype.submit.call(form)`.
- `Pages/Maintenances/Create.cshtml`: los costos y tareas existentes del borrador se renderizan con `Id` oculto para permitir edición posterior sin duplicar ni perder historial; costos retirados se desvinculan y tareas retiradas se soft-deletean con `IsDeleted`.
- `Pages/Acquisitions/Create.cshtml.cs`: L-12 ahora tiene `OnPostDraftAsync`. El borrador crea o actualiza la solicitud de adquisición vinculada (`AcquisitionRequestId`) como solicitud parcial, mantiene `CurrentPhase = WizardPhase.Disbursement`, `CurrentState = AwaitingDisbursement`, `PlanStatus = InProgress` e identifica el plan con `IsDraft = true`.
- `Pages/Acquisitions/Create.cshtml.cs`: el cierre final de L-12 reutiliza la solicitud creada por el borrador si existe, importa/actualiza costos desde L-8, genera notificación solo en guardado final, limpia el borrador y mueve el plan a `WizardEquipmentState.Completed` + `ManagementPlanStatus.Completed`.
- `Pages/Acquisitions/Create.cshtml`: se agregaron botones reales `Guardar Borrador` y `Crear Solicitud L-12`, con confirmación SweetAlert2 v7-compatible y submit nativo preservando `formaction`.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\draft-l8-l12"` completo con 0 errores. Persisten warnings existentes de nullability.

### Sistema de Borradores — L-6, L-7, L-8 y L-3 (9 Mayo 2026)
- `Pages/Maintenances/Create.cshtml.cs`: corrección del borrador L-8. El draft ya no queda bloqueado por la validación `Completed + 100%`; el `OnGet` rehidrata técnico, costos y tareas antes de renderizar; el mantenimiento nuevo se guarda primero para obtener ID real y después se sincronizan costos/tareas. Esto evita el síntoma de "guarda borrador pero no reaparece como Kardex/Adquisitions".
- `Pages/Maintenances/Create.cshtml`: el total de costos se recalcula al cargar la pantalla, por lo que los costos rehidratados del borrador se muestran correctamente.
- `Pages/Requests/Create.cshtml.cs`: L-7 ahora tiene `OnPostDraftAsync`. El borrador crea o actualiza la `Request` técnica vinculada, mantiene `CurrentPhase = WizardPhase.TechnicalRequest`, `CurrentState = AwaitingRequest`, `IsDraft = true` y no avanza a L-8. El guardado final reutiliza esa solicitud, limpia el borrador y avanza a L-8.
- `Pages/Requests/Create.cshtml`: se agregaron botones `Guardar Borrador` y `Guardar y continuar`; la confirmación preserva el handler correcto con `formaction` y submit nativo.
- `Pages/Verifications/MassCreate.cshtml.cs`: L-6 masivo ahora soporta borrador por filas. El draft crea/actualiza `Verification` con `VerificationStatus.Draft`, vincula `VerificationId` al plan y mantiene la fase `Verification` sin generar L-7 ni avanzar. El guardado final reutiliza esa verificación, cambia su estado a `Completed` o `WithObservations`, crea L-7 solo si corresponde, limpia draft y avanza fase.
- `Pages/Verifications/MassCreate.cshtml`: al cargar equipos también trae condiciones/observaciones de borrador, rehidrata selects y muestra el icono de observación. Se agregó botón `Guardar Borrador` con SweetAlert2 v7-compatible.
- `Pages/Departures/MassCreate.cshtml.cs`: L-3 masivo ahora soporta borrador. El draft crea/actualiza `Departure` vinculada con estado `Cancelled` como contenedor no activo, guarda fechas/ítem sin marcar el equipo `OnLoan` y conserva `CurrentPhase = WizardPhase.Exit`. El guardado final reutiliza esa salida, la marca `Active`, actualiza el equipo a `OnLoan`, limpia draft y avanza a Kardex.
- `Pages/Departures/MassCreate.cshtml`: rehidrata fechas desde la salida borrador y agrega botón `Guardar Borrador`, usando el handler correcto con `formaction` y submit nativo.
- `Pages/Index.cshtml.cs`: Step 1 ahora incluye planes L-6 con `VerificationStatus.Draft`, porque tienen `VerificationId` vinculado pero siguen siendo borradores de verificación.
- Verificación: `dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore -p:UseSharedCompilation=false -o "C:\Users\monte\AppData\Local\Temp\opencode\drafts-l6-l7-l8-l3"` completo con 0 errores. Persisten warnings existentes de nullability.

