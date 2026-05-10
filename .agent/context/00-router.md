# Router De Contexto

Usa este archivo para decidir que leer. No leas todo por defecto.

## Flujo Base

1. Identifica la capa principal: UI, backend, datos, reportes, infraestructura o modulo wizard.
2. Lee `01-global-rules.md`.
3. Lee el area tecnica correspondiente.
4. Lee el modulo funcional si aplica.
5. Lee la skill correspondiente.
6. Solo consulta `archive/` si necesitas una decision historica.

## Rutas Por Tarea

| Tarea | Contexto | Skill |
|---|---|---|
| Cambiar `.cshtml`, botones, modales, validacion JS | `areas/ui.md` | `skills/ui_premium/SKILL.md` |
| Cambiar `.cshtml.cs`, handlers, InputModel | `areas/backend-page-models.md` | `skills/backend_methods/SKILL.md` |
| Modificar modelos, relaciones o migraciones | `areas/database-ef.md` | `skills/database/SKILL.md` |
| Generar Excel/PDF o modificar ReportService | `areas/reporting.md` | `skills/reporting/SKILL.md` |
| Ajustar flujo guiado | `modules/wizard.md` | `backend_methods` + area necesaria |
| Dashboard, cards, navegacion, notificaciones visibles | `modules/dashboard-navigation.md` | `ui_premium` + `backend_methods` |
| Guardar/rehidratar borradores | `modules/drafts.md` | `backend_methods` + `database` |
| Correctivo | `modules/corrective.md` | `backend_methods` |
| Verificaciones L-6 | `modules/l6-verifications.md` | `ui_premium` + `backend_methods` |
| Salidas L-3 | `modules/l3-departures.md` | `ui_premium` + `backend_methods` |
| Kardex/L-48 | `modules/kardex-l48.md` | `ui_premium` + `backend_methods` + `database` |
| Adquisiciones L-12 | `modules/acquisitions-l12.md` | `reporting` si toca Excel, si no `backend_methods` |
| Build, appsettings, Visual Studio, publicacion | `areas/infrastructure.md` | ninguna o skill especifica si toca codigo |

## Manifiestos Locales

- `Pages/AGENTS.md`: UI y Razor Pages.
- `Models/AGENTS.md`: entidades, EF Core y migraciones.
- `Services/AGENTS.md`: reportes y servicios backend.

## Regla De Precision

Si el cambio es puntual, toca solo el archivo y contexto necesario. No refactorices modulos vecinos sin pedir aprobacion.
