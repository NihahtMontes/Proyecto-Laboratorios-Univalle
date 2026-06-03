---
name: backend_methods
description: Razor PageModels, handlers OnGet/OnPost, InputModel, EF tracking, redirects, TempData y logica de formularios.
trigger: Modificacion de `.cshtml.cs`, handlers Razor Pages, InputModel, consultas para vistas, redirects, filtros server-side o escritura de entidades.
scope: Pages/PageModels
context: .agent/context/areas/backend-page-models.md
---
# Skill Backend PageModels

## 1. Contexto Del Modulo

Los PageModels conectan UI Razor con EF Core y servicios. Reciben query string/form data, validan `InputModel`, cargan datos con `ApplicationDbContext`, aplican cambios con tracking y responden con `Page()` o `RedirectToPage()`.

Flujo: request HTTP -> handler `OnGet/OnPost` -> validacion -> consulta/proyeccion -> vista o persistencia -> `TempData` -> redirect.

## 2. Arquitectura Y Archivos Clave

- PageModels por modulo: `Pages/<Modulo>/*.cshtml.cs`.
- Activos catalogo: `Pages/Equipment/Create.cshtml.cs`, `Edit.cshtml.cs`, `Details.cshtml.cs`.
- Unidades fisicas: `Pages/EquipmentUnits/*.cshtml.cs`.
- Consulta inventario: `Pages/AssetView/*.cshtml.cs`.
- Wizard: `Pages/Index.cshtml.cs` y modulos L-6/L-7/L-8/L-3/Kardex/L-12.
- Servicios consumidos: `Services/ManagementContextService.cs`, `Services/CurrentUserService.cs`, `Services/ReportService.cs`.

## 3. Integracion Con NiceAdmin

- PageModels deben entregar a Razor datos ya listos para tarjetas, tabs, badges, tablas y selects.
- Para selects, preferir `SelectListItem` controlado cuando el enum contiene valores legacy o internos.
- Para filtros de tablas, usar `[BindProperty(SupportsGet = true)]` y preservar query string en paginacion, limpiar filtro, POST y redirect.
- `TempData.Success/Error/Warning/Info` alimenta el SweetAlert global del layout.

## 4. Patrones Y Convenciones

- No bindear entidades completas en formularios: usar `InputModel` con campos editables.
- El proyecto usa NoTracking global; toda escritura debe cargar con `.AsTracking()` o `FindAsync()`.
- En POST fallido: repoblar listas/ViewData y retornar `Page()`.
- Redireccionar solo despues de guardar correctamente.
- Preservar `ManagementId`, `ManagementPlanId`, `Step`, `SelectedLabId` y filtros cuando aplique.
- CRUD activos actual:
  - `EquipmentCategory.Equipment`: exige clasificacion tecnica valida y guarda `UtensilType.NoAplica`.
  - `EquipmentCategory.Utensil`: exige subclasificacion valida y guarda `TypeClassification.Otro`.
  - `EquipmentCategory.Other`: guarda `TypeClassification.Otro` y `UtensilType.NoAplica`.
  - Edicion permite valores legacy solo si ya existen en el registro.

## 5. Contexto Para Agente

- Leer `.agent/context/areas/backend-page-models.md` y `Pages/AGENTS.md` antes de tocar PageModels.
- No usar `Person.FullName` en LINQ: es `[NotMapped]`.
- No confiar en selects disabled; reconstruir verdad desde BD.
- Si una accion modifica historial o fase de wizard, usar transaccion cuando haya varias escrituras acopladas.
- No crear notificaciones durante borradores.
