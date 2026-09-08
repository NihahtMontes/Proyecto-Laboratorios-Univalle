---
name: reporting
description: Implementa o revisa el codigo que genera reportes institucionales Excel/PDF de Laboratorios Univalle con EPPlus y QuestPDF. Usar con `ReportService`, `Services/Reports`, `Services/Reporting`, `ReportsController`, paginas de reportes o templates `.xlsx`; no usar para editar una hoja independiente fuera del producto.
---

# Reportes institucionales

Lee `context.md` y el `Services/AGENTS.md` aplicable antes de editar.

## Invariantes

- EPPlus es el flujo principal de Excel y QuestPDF el flujo PDF existente. No agregues otra libreria sin aprobacion.
- Las coordenadas, merges y formatos de templates institucionales son parte del contrato; no los muevas sin validar el documento resultante.
- No uses `Worksheets.Add(sourceSheet.Name, sourceSheet)`, `Merge = false` sobre rangos merged ni `System.Drawing.Color`.
- Limpia datos dummy con `.Value = null` antes de escribir y guarda paquetes mediante stream.
- Carga los `Includes` necesarios antes de mapear; no ocultes fallos de carga con `catch` silencioso.
- Los reportes oficiales deben usar el identificador exacto del flujo (`requestId`, `maintenanceId`, `departureId`, `managementPlanId` o `managementId/labId`), no el registro mas reciente por conveniencia.
- Separa preventivo y correctivo por gestion y tipo cuando corresponda.
- Los errores deben ser legibles para el usuario sin revelar rutas, SQL ni secretos.

## Forma de trabajo

1. Traza pagina o endpoint, servicio, consulta, template y archivo de salida.
2. Confirma identificadores, Includes y reglas de datos antes de tocar celdas o layout.
3. Haz el cambio minimo y conserva el resto del template.
4. Valida el archivo generado cuando existan datos y entorno seguros; comprueba apertura, contenido, merges y formato relevante.
5. Ejecuta `dotnet build` y registra si no fue posible generar una muestra.

Reporta el flujo afectado, template utilizado, validaciones del archivo y limitaciones.
