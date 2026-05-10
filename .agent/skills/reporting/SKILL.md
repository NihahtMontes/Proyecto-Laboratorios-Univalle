---
name: reporting
description: Reportes institucionales Excel/PDF, EPPlus, plantillas rigidas y mapeo posicional.
trigger: Modificacion de `Services/ReportService.cs`, `Services/Reporting/`, `Controllers/ReportsController.cs` o templates `.xlsx`.
scope: Services/Reporting
context: .agent/context/areas/reporting.md
---
# Skill Reporting

## Leer Antes

- `.agent/context/areas/reporting.md`.
- `Services/AGENTS.md`.
- `troubleshooting/excel-crashes.md` si hay crash o template pesado.

## EPPlus Seguro

- No copiar worksheets con `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- No usar `Merge = false` en rangos merged.
- Limpiar datos dummy con `.Value = null`.
- Guardar con stream y `package.SaveAs(stream)`.
- No usar `System.Drawing.Color`.

## Plantillas

- Las coordenadas son contrato institucional.
- No mover celdas sin validar documento fisico.
- Activar wrap text en campos largos.
- Usar orientacion y fit-to-page cuando el impreso lo requiera.

## Datos

- Cargar Includes necesarios antes de mapear.
- Usar fallbacks legibles si falta dato institucional.
- Para montos literales usar `ReportService.ConvertirEnteroATexto`.

## PDF

- QuestPDF solo para documentos nuevos o requeridos.
- Incluir logos desde rutas controladas en `wwwroot`.
