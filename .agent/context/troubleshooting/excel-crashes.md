# Troubleshooting Excel Y EPPlus

## Sintomas

- La app crashea al descargar Excel.
- Archivo generado no abre.
- Crash nativo `0xffffffff`.

## Causas Frecuentes

- Template con dibujos, VML, imagenes pesadas o formato corrupto.
- Copia de worksheet con `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- Uso de `Merge = false` en rangos ya merged.
- Uso de `System.Drawing.Color` en entorno ASP.NET Core.
- Catch silencioso que oculta error hasta `SaveAs()`.
- Relaciones no cargadas antes de mapear datos.

## Patron Seguro

- Para templates: abrir `new ExcelPackage(templateFile)`, usar worksheet existente y limpiar valores.
- Para reportes nuevos: crear desde cero.
- Guardar con stream y `package.SaveAs(stream)`.
- Usar helpers EPPlus para color.
- Limpiar rangos dummy con `.Value = null`.

## Reportes Actuales

- L-6 y L-3: desde cero.
- L-7, L-8, L-48, L-12: con template.

## Si Falla

1. Reproducir con `dotnet run`, no Visual Studio debugger.
2. Probar solo el reporte afectado.
3. Revisar template y tamaño anormal.
4. Revisar Includes en el controller/service.
5. Evitar refactor masivo antes de aislar el rango/celda que falla.
