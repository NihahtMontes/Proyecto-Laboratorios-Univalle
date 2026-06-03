---
name: api_controllers
description: Controllers HTTP/API, endpoints de descarga, rollback del wizard, DTOs y respuestas de archivo/JSON.
trigger: Modificacion de `Controllers/`, rutas `[Route]`, acciones `[HttpGet]/[HttpPost]`, DTOs de API, descargas o rollback wizard.
scope: Controllers/API
context: .agent/context/areas/backend-page-models.md
---
# Skill API Controllers

## 1. Contexto Del Modulo

Los controllers exponen endpoints HTTP fuera del flujo Razor directo. Actualmente cubren descargas oficiales de reportes y operaciones API del wizard.

Flujo: cliente/UI -> ruta API -> controller -> servicio/DbContext -> `IActionResult` con archivo, JSON, error o estado HTTP.

## 2. Arquitectura Y Archivos Clave

- `Controllers/ReportsController.cs`: API `api/Reports/download/...` para Excel institucional.
- `Controllers/WizardRollbackController.cs`: API `api/WizardRollback/redo` para rehacer/retroceder fases del wizard.
- Servicios relacionados: `Services/ReportService.cs`, `Services/ManagementContextService.cs`.
- Modelos relacionados: `ManagementPlan`, `Request`, `Maintenance`, `Departure`, `EquipmentUnit`.

Relaciones: Controller -> Service o DbContext -> Models/Data -> respuesta HTTP.

## 3. Integracion Con NiceAdmin

- Los endpoints suelen ser llamados desde botones/cards del wizard, dashboard o centro de reportes.
- Enlaces de descarga deben conservar `managementId`, `labId` o ID exacto del documento.
- Errores deben devolverse con estado HTTP claro; la UI puede traducirlos a SweetAlert o mensaje visible.

## 4. Patrones Y Convenciones

- Mantener `[Route("api/[controller]")]` salvo necesidad real.
- Para archivos Excel usar MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
- No mezclar preventivo/correctivo: filtrar por `ManagementId` o ID exacto.
- En POST de API, validar DTO antes de tocar BD.
- Para escritura EF, cargar entidades con `.AsTracking()`.

## 5. Contexto Para Agente

- No mover logica de reportes compleja al controller; delegar a services.
- No crear endpoints paralelos si ya existe una ruta oficial.
- Cuidar compatibilidad con botones existentes en Razor Pages.
- No devolver datos sensibles ni connection strings en mensajes de error.
