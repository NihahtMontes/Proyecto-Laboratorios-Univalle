# Area Reporting

Leer junto con `Services/AGENTS.md` y `.agent/skills/reporting/SKILL.md`.

## Librerias

- Excel actual: EPPlus 7.5.2.
- ClosedXML esta referenciado, pero el flujo principal usa EPPlus en `ReportService`.
- PDF futuro: QuestPDF 2025.12.3.

## Reglas EPPlus Criticas

- No copiar worksheets con `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- No usar `package.GetAsByteArray()` si el patron del proyecto usa helper `SavePackage()`.
- No hacer `Merge = false` sobre rangos merged.
- No usar `System.Drawing.Color`; usar helpers EPPlus con `ExcelColor.SetColor`.
- Limpiar celdas/rangos dummy con `.Value = null` antes de escribir.

## Tipos De Reporte

| Reporte | Generacion |
|---|---|
| L-6 | Desde cero |
| L-3 | Desde cero |
| L-7 | Template |
| L-8 | Template |
| L-48 | Template |
| L-12 | Template |

## Mapeo

- Las coordenadas institucionales son rigidas.
- No mover celdas por conveniencia visual.
- Si un texto se corta, usar wrap y ajuste de altura, no cambiar estructura sin validar plantilla.
- Para monto literal usar `ReportService.ConvertirEnteroATexto`.

## Seguridad De Datos

- Reportes deben cargar Includes completos antes de acceder relaciones.
- Usar fallback claro cuando un dato institucional no existe.
- No ocultar errores con catch silencioso en zonas nuevas.

## Troubleshooting

Para crashes nativos o Excel corrupto leer `troubleshooting/excel-crashes.md`.
