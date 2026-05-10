# AGENTS.md - Services Y Reportes

**Ambito**: servicios backend, reportes Excel/PDF y generacion documental.

## Lectura Obligatoria

- `../context.md`.
- `../.agent/context/areas/reporting.md` si toca reportes.
- `../.agent/skills/reporting/SKILL.md`.
- `../.agent/context/troubleshooting/excel-crashes.md` si hay crash o template corrupto.

## Reglas Reportes

- EPPlus es el flujo principal de Excel.
- No usar `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- No usar `Merge = false` en rangos merged.
- No usar `System.Drawing.Color`.
- Limpiar datos dummy con `.Value = null` antes de escribir.
- Mantener coordenadas exactas de plantillas institucionales.
- Para montos literales usar `ReportService.ConvertirEnteroATexto`.

## Datos

- Cargar Includes necesarios antes de mapear.
- No ocultar errores nuevos con catch silencioso.
- Si se agrega libreria externa, pedir aprobacion antes.
