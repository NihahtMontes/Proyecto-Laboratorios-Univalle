# AGENTS.md - Services Y Reportes

**Ambito**: servicios backend, reportes Excel/PDF y generacion documental.

## Lectura Obligatoria

- `../context.md`.
- `../.agents/skills/reporting/SKILL.md` si toca reportes.
- Evidencia del template, servicio y endpoint si hay crash de Excel o un archivo corrupto.

## Reglas Reportes

- EPPlus es el flujo principal de Excel.
- No usar `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- No usar `Merge = false` en rangos merged.
- No usar `System.Drawing.Color`.
- Limpiar datos dummy con `.Value = null` antes de escribir.
- Mantener coordenadas exactas de plantillas institucionales.
- Para montos literales usar los helpers del parcial de adquisicion de `ReportService`.

## Datos

- Cargar Includes necesarios antes de mapear.
- No ocultar errores nuevos con catch silencioso.
- Si se agrega libreria externa, pedir aprobacion antes.
