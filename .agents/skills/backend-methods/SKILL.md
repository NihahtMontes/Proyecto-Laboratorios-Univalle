---
name: backend-methods
description: Implementa o revisa PageModels de Razor Pages, handlers, InputModel, consultas, redirects, TempData y persistencia EF Core de Laboratorios Univalle. Usar con `.cshtml.cs`, POST/GET, filtros server-side o escritura de entidades.
---

# Backend de Razor Pages

Lee `context.md` y el `Pages/AGENTS.md` aplicable antes de editar.

## Invariantes

- Usa `InputModel` con campos editables; no bindeas entidades completas desde formularios.
- El proyecto usa NoTracking global. Toda entidad que vaya a cambiar debe cargarse con `.AsTracking()` o `FindAsync()`.
- En un POST invalido repuebla listas y `ViewData` antes de devolver `Page()`.
- Redirecciona solo despues de guardar correctamente.
- Preserva `ManagementId`, `ManagementPlanId`, `Step`, `SelectedLabId`, filtros y contexto de navegacion cuando apliquen.
- No confies en selects deshabilitados ni en estado de sesion como fuente unica; reconstruye la verdad desde identificadores validados y la base de datos.
- No uses `Person.FullName` dentro de LINQ porque es `[NotMapped]`.
- Usa `TempData.Success/Error/Warning/Info`; no inventes claves incompatibles con el layout.
- Usa una transaccion cuando varias escrituras acopladas deban confirmarse o revertirse juntas.
- No generes notificaciones durante guardados de borrador.

## Forma de trabajo

1. Traza GET, POST, validacion, carga, escritura y redirect antes de cambiar el handler.
2. Identifica reglas de autorizacion y valida que el registro pertenezca al contexto solicitado.
3. Si cambia el modelo o el schema, carga tambien `database` y separa esa decision del cambio de handler.
4. Cubre entrada valida, invalida, inexistente y no autorizada cuando sean relevantes.
5. Ejecuta `dotnet build` y las pruebas disponibles; revisa el diff por campos sobreescritos o contexto perdido.

Reporta handlers afectados, escenarios comprobados y limitaciones de prueba.
