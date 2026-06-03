# Indice Maestro De Skills

Este indice enruta agentes al skill correcto usando el estado local actual del proyecto como fuente de verdad.

| Skill | Carpeta/Modulo Cubierto | Usar Cuando | Contexto Relacionado |
|---|---|---|---|
| `ui_premium` | `Pages/**/*.cshtml`, `Pages/Shared`, UI Razor, JS embebido | Cambios visuales, NiceAdmin/WrapPixel, SweetAlert2, tabs, cards, tablas, sidebar, wizard visual | `.agent/context/areas/ui.md`, `Pages/AGENTS.md` |
| `backend_methods` | `Pages/**/*.cshtml.cs` | PageModels, handlers, `InputModel`, filtros, redirects, `TempData`, EF tracking desde paginas | `.agent/context/areas/backend-page-models.md`, `Pages/AGENTS.md` |
| `database` | `Models`, `Data`, enums, migraciones | Entidades, DbContext, query filters, soft-delete, PostgreSQL/Npgsql, auditoria | `.agent/context/areas/database-ef.md`, `Models/AGENTS.md` |
| `reporting` | `Services/ReportService.cs`, `Services/Reporting`, `Controllers/ReportsController.cs`, `wwwroot/templates` | Excel/PDF institucional, EPPlus, QuestPDF, endpoints de descarga L-6/L-7/L-8/L-3/L-48/L-12 | `.agent/context/areas/reporting.md`, `Services/AGENTS.md` |
| `api_controllers` | `Controllers` | API controllers, rutas HTTP, respuestas `File`, JSON, rollback wizard | `.agent/context/areas/backend-page-models.md` |
| `frontend_assets` | `wwwroot` | CSS/JS global, WrapPixel/NiceAdmin assets, librerias, templates, uploads | `.agent/context/areas/ui.md` |
| `asset_inventory` | `Pages/AssetView`, `Pages/Equipment`, `Pages/EquipmentUnits`, `Models/Equipment*` | Catalogo de activos, unidades fisicas, categorias Equipo/Utensilio/Otro, navegacion en cascada | `.agent/context/areas/ui.md`, `.agent/context/areas/backend-page-models.md` |
| `services_core` | `Services` no-reporting, `Program.cs` DI | Usuario actual/sistema, contexto de gestion, errores DB, claims Identity | `.agent/context/areas/backend-page-models.md` |

## Reglas De Enrutamiento Rapidas

- Si tocas `.cshtml`, usar `ui_premium`.
- Si tocas `.cshtml.cs`, usar `backend_methods`.
- Si tocas activos o inventario, usar `asset_inventory` ademas de UI/backend/database segun archivo.
- Si tocas `Controllers`, usar `api_controllers`; si es reporte, tambien `reporting`.
- Si tocas `wwwroot`, usar `frontend_assets`.
- Si tocas `Models` o `Data`, usar `database`.
- Si tocas servicios no-reporting, usar `services_core`.

## Fuente De Verdad

La fuente de verdad es el working tree local actual. No recuperar reglas antiguas desde ramas remotas o historicas si contradicen las correcciones locales.
