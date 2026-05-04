# CONTEXT.md — Sistema de Gestión de Laboratorios Univalle
> **LEE ESTE ARCHIVO COMPLETO ANTES DE TOCAR CUALQUIER ARCHIVO.**
> Cualquier decisión arquitectónica que no esté aquí debe ser consultada antes de implementarse.

---

## 1. QUÉ ES ESTE SOFTWARE

Sistema de Gestión de Laboratorios para la Universidad del Valle (Bolivia). Administra el ciclo de vida completo de equipos de laboratorio mediante formularios institucionales estandarizados (L-3, L-6, L-7, L-8, L-12, L-48), orquestados por un Wizard de 7 pasos.

**Flujo principal (Wizard):**
```
L-6 Verificación → L-7 Solicitud → L-8 Mantenimiento → L-3 Salida → L-48 Kardex → L-12 Adquisición → Desembolso
```

---

## 2. STACK — NO CAMBIAR, NO AGREGAR DEPENDENCIAS SIN CONSULTAR

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 9.0 — **Razor Pages** (NO MVC Controllers salvo ReportsController) |
| ORM | Entity Framework Core 9.0 + SQL Server |
| Frontend | Razor Pages (.cshtml) + Bootstrap 4 + NiceAdmin template |
| JS | jQuery 3.x, Select2, jqBootstrapValidation, SweetAlert2 |
| Excel | **EPPlus (OfficeOpenXml)** — NO ClosedXML, NO System.Drawing |
| Auth | ASP.NET Core Identity con roles |
| Cache | IMemoryCache (5 min TTL) |

---

## 3. ESTRUCTURA DE CARPETAS

```
Pages/
  Index.cshtml(.cs)       — Dashboard
  Equipment/              — CRUD equipos (catálogo)
  EquipmentUnits/         — CRUD unidades físicas
  Faculties/              — CRUD facultades
  Laboratories/           — CRUD laboratorios
  Persons/                — CRUD personas (TPT: Intern/Extern)
  Users/                  — CRUD usuarios Identity
  Verifications/          — L-6
  Requests/               — L-7
  Maintenances/           — L-8
  Departures/             — L-3
  Kardex/                 — L-48
  Acquisitions/           — L-12
  Managements/            — Gestión semestral
  Shared/                 — _Layout, _WizardSteps, _ValidationScriptsPartial
Models/                   — Entidades EF Core
  Enums/                  — EquipmentStatus, GeneralStatus, RequestStatus, WizardPhase, etc.
Data/
  ApplicationDbContext.cs — DbContext + QueryFilters + seed
Services/
  ReportService.cs        — Excel (todos los reportes)
  ManagementContextService.cs — Caché de gestión activa
  Reporting/              — Servicios de reportes específicos
  AGENTS.md               — Manifiesto de servicios (leer antes de tocar Services/)
Helpers/
  PaginatedList.cs
  AuthorizationHelper.cs  — Constantes de roles
  EnumHelper.cs           — SelectList para enums
  TempDataExtensions.cs   — .Success(), .Error(), .Warning()
Controllers/
  ReportsController.cs    — Solo descarga de Excel
```

---

## 4. REGLAS OBLIGATORIAS — SIEMPRE

### Soft-delete (CRÍTICO)
- **NUNCA** usar `_context.Remove()` ni `_context.{Entity}.Remove()`
- Siempre: `entity.Status = GeneralStatus.Eliminado` (o `CurrentStatus = EquipmentStatus.Deleted`)
- El QueryFilter en ApplicationDbContext excluye automáticamente los eliminados

### Tracking (CRÍTICO)
- NoTracking es **global** en Program.cs (`UseQueryTrackingBehavior(NoTracking)`)
- Toda query que luego modifica y guarda debe tener `.AsTracking()` explícito
- `FindAsync()` siempre trackea (excepción a la regla global)
- Si modificas una entidad sin `.AsTracking()` → los cambios NO se guardan

### TempData (CRÍTICO)
- **NUNCA** `TempData["Success"] = "..."` — es vulnerable a XSS
- **SIEMPRE** `TempData.Success("mensaje")` (extensión en TempDataExtensions.cs)
- También: `TempData.Error()`, `TempData.Warning()`

### EPPlus / Excel (CRÍTICO)
- **NUNCA** `Worksheets.Add(sourceSheet.Name, sourceSheet)` → crash nativo 0xffffffff por corrupción XML
- Reportes nuevos: generar desde cero (sin template), como hace L-6
- Reportes con template: `new ExcelPackage(templateFile)` → `.Copy()` → rellenar por posición exacta
- `EliminarHojasExtra` es un método vacío (no-op). **NO eliminarlo, NO reactivarlo.** Existe como documentación.

### EF Core
- `HasQueryFilter` **solo** en Equipment (y entidades raíz con Status). **NO** agregarlo en Maintenance, Management, Verification, etc. — rompe queries existentes
- **NUNCA** modificar migraciones existentes — siempre crear nuevas
- `FullName` en Person es `[NotMapped]` — **no usarlo en LINQ**. Hacer `.ToList()` primero, luego filtrar en memoria

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
| `Worksheets.Add(sheet.Name, sheet)` | Generar desde cero o `.Copy()` |
| Modificar migraciones existentes | Crear nueva migración |
| `FullName` en LINQ | `.ToList()` → filtrar en memoria |
| Modificar sin `.AsTracking()` | `.AsTracking()` en la query de carga |
| `_context.QueryFilters` en entidades dependientes | Solo en Equipment (raíz) |
| Eliminar `EliminarHojasExtra` | Dejarlo como no-op |
| Hard-delete de cualquier entidad principal | Soft-delete siempre |
| Comentarios innecesarios en código | Solo si es estrictamente necesario |

---

## 6. CONVENCIONES DE NOMBRES

- **PageModels**: `{Entity}CreateModel`, `{Entity}EditModel`, `{Entity}IndexModel`, `{Entity}DetailsModel`, `{Entity}DeleteModel`
- **Input models**: clase `InputModel` anidada dentro del PageModel
- **Enums**: PascalCase en C#, almacenados como string en BD (`HasConversion`)
- **Auditoría**: `CreatedDate`, `CreatedById`, `LastModifiedDate`, `ModifiedById`
- **Roles**: usar constantes de `AuthorizationHelper.cs` (no strings sueltos)

---

## 7. CONVENCIONES UI (NiceAdmin — RESPETAR)

- Cards: `shadow-sm border-0`
- Badges: `badge-success`, `badge-warning`, `badge-info` (soft, no solid)
- Botones: `btn-rounded font-weight-bold shadow-sm`
- Formularios: asimétricos `col-lg-7` + `col-lg-5`
- Tablas: `table-hover v-middle` con `thead class="bg-light"`
- Select2 para dropdowns con búsqueda
- jqBootstrapValidation para validación client-side
- Secciones importantes: `border-left: 4px solid {color}`

---

## 8. WIZARD — CÓMO FUNCIONA

- `ManagementPlan.CurrentPhase` → enum `WizardPhase` rastrea la fase actual
- `ManagementPlan.CurrentState` → enum `WizardEquipmentState` rastrea el estado
- `isWizard=true` en QueryString activa el flujo guiado
- Dropdowns del wizard se precargan con IDs de fases anteriores (disabled)
- `_WizardSteps` partial renderiza los pasos con colores semánticos
- L-6 con fallas → `CurrentPhase = TechnicalRequest`
- L-6 sin fallas → `CurrentPhase = Maintenance`

---

## 9. CACHÉ DE GESTIÓN ACTIVA

```
ManagementContextService.GetCurrentManagementAsync()
  → IMemoryCache.TryGetValue (TTL 5 min)
  → Si miss: query BD (Management con Status=Activo)
InvalidateCache() → llamar en Create/Edit de Managements
```

---

## 10. ESTADO DE MÓDULOS

### Completados y en producción
Dashboard, Equipment CRUD, EquipmentUnits, Faculties, Laboratories, Persons (TPT), Users, Countries, Cities, Career, Verifications L-6, Requests L-7, Maintenances L-8, Departures L-3, Kardex L-48, Acquisitions L-12, Managements, Wizard, Paginación (14+ páginas), Excel Reportes, TempData Notifications, Authorization por roles

### Pendientes conocidos
| Item | Estado |
|---|---|
| Verifications: falta `TempData.Success()` al crear | Pendiente |
| Departures: migrar `TempData["Success"]` → `.Success()` | Pendiente |
| Requests L-7: falta SweetAlert de confirmación | Pendiente |
| Exportación PDF (QuestPDF) | No implementado |
| Notificaciones tiempo real (SignalR) | No implementado |
| Testing unitario | No implementado |

---

## 11. ANTES DE ENTREGAR CUALQUIER CAMBIO

1. Verificar que no usas ningún anti-patrón de la sección 5
2. Ejecutar `dotnet build` — debe terminar con **0 errores**
3. Si tocaste `Services/` → leer `Services/AGENTS.md` primero
4. Si tocaste Excel → verificar que no hay `Worksheets.Add(sheet, sheet)`
5. Si modificaste entidades → verificar que la query de carga tiene `.AsTracking()`
6. Respetar estética NiceAdmin (sección 7)

---

## 12. ARCHIVOS INTOCABLES

| Archivo | Razón |
|---|---|
| `EliminarHojasExtra` en ReportService.cs | No-op intencional, documentación |
| `wwwroot/templates/L6V2.xlsx` | No se usa pero no borrar |
| Todas las migraciones existentes | Solo agregar nuevas |
| QueryFilters existentes en ApplicationDbContext | No agregar en entidades dependientes |
| `NoTracking` global en Program.cs | Intencional para performance |