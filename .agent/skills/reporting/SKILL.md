---
name: reporting
description: Reportes institucionales Excel/PDF, EPPlus, QuestPDF, templates y endpoints oficiales de descarga.
trigger: Modificacion de `Services/ReportService.cs`, `Services/Reporting/`, `Controllers/ReportsController.cs`, `Pages/Reports/` o templates `.xlsx`.
scope: Services/Reporting/Controllers/Reports
context: .agent/context/areas/reporting.md
---
# Skill Reporting

## 1. Contexto Del Modulo

El modulo de reportes genera documentos institucionales del flujo de mantenimiento: L-6, L-7, L-8, L-3, L-48/Kardex y L-12/adquisicion. Usa EPPlus para Excel y QuestPDF para documentos PDF especificos.

Flujo: UI o wizard -> endpoint/pagina de reportes -> `ReportService` -> consulta EF con Includes -> plantilla Excel/PDF -> archivo descargable.

## 2. Arquitectura Y Archivos Clave

- Controller API: `Controllers/ReportsController.cs`.
- Servicio Excel principal: `Services/ReportService.cs`.
- Servicio PDF verificacion: `Services/Reporting/VerificationReportService.cs`.
- Interfaz PDF: `Services/Reporting/IVerificationReportService.cs`.
- Pagina centro reportes: `Pages/Reports/Index.cshtml(.cs)`.
- Templates: `wwwroot/templates/**`.

Endpoints actuales del controller incluyen descargas para `l6`, `l7`, `l8`, `l3`, `l48` y `adquisicion`.

## 3. Integracion Con NiceAdmin

- Las paginas de reportes usan cards/botones de descarga; deben preservar contexto de gestion cuando aplique.
- Los botones deben apuntar a endpoints con IDs exactos cuando el flujo oficial lo requiere.
- Mensajes de error deben ser legibles y no romper la UI.

## 4. Patrones Y Convenciones

- EPPlus seguro:
  - no usar `Worksheets.Add(sourceSheet.Name, sourceSheet)`;
  - no usar `Merge = false` en rangos merged;
  - no usar `System.Drawing.Color`;
  - guardar con stream y `package.SaveAs(stream)`.
- Las coordenadas de plantillas son contrato institucional; no mover celdas sin validar.
- Cargar Includes necesarios antes de mapear.
- Usar fallbacks legibles: `Sin dato`, `Pendiente`, etc.
- Reportes oficiales del wizard deben usar `requestId`, `maintenanceId`, `departureId`, `managementPlanId` o `managementId/labId` segun corresponda; evitar "ultimo por unidad" en flujos oficiales.

## 5. Contexto Para Agente

- Leer `Services/AGENTS.md`, `.agent/context/areas/reporting.md` y troubleshooting de Excel si hay crash.
- No ocultar excepciones nuevas con catch silencioso.
- No agregar librerias externas sin aprobacion.
- Separar preventivo/correctivo por `ManagementId` y tipo cuando la pantalla lo indique.
