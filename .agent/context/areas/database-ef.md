# Area Database Y EF Core

Leer junto con `Models/AGENTS.md` y `.agent/skills/database/SKILL.md`.

## Proveedor Actual

- SQL Server con Microsoft.EntityFrameworkCore.SqlServer 9.0.12.
- Base dev esperada: `DB_Laboratorios_Univalle_DEV`.
- Produccion: `DB_Laboratorios_Univalle`.
- Connection strings reales no deben estar versionadas.

## Tracking

- NoTracking global esta activo.
- Para actualizar: `.AsTracking()` o `FindAsync()`.
- Si una actualizacion no persiste, revisar tracking antes de refactorizar.

## Migraciones

- Migraciones SQL Server vigentes en `Data/SqlServerMigrations/`.
- Migraciones PostgreSQL en `Data/PostgresMigrations/` quedan historicas/excluidas.
- Migraciones antiguas SQL Server en `Data/Migrations/` y `Migrations/` quedan historicas/excluidas.
- Nunca modificar una migracion existente.
- Crear migracion nueva en `Data/SqlServerMigrations/` para cambios de schema.

## Soft Delete

- Entidades core deben conservar historial.
- Preferir `Status = Eliminado/Deleted/Cancelled`, `IsDeleted = true`, o desvincular FK nullable.
- Costos retirados se desvinculan si la FK es nullable.
- Tareas L-48 usan `MaintenanceTask.IsDeleted`.

## Query Filters

- No agregar filtros globales nuevos sin analizar reportes y joins.
- Si se necesita excluir borrados en una lista puntual, filtrar explicitamente.

## Modelado Vigente

- `Management.Semester` acepta `0`, `1`, `2`.
- `EquipmentUnit.LaboratoryId` es nullable.
- `ManagementPlan` contiene campos de borrador: `IsDraft`, `DraftPhase`, `DraftSavedAt`, `DraftSummary`.
- `MaintenanceTask` contiene `IsDeleted`.

## Residuales Con Riesgo

Persisten usos de hard-delete que requieren modelado antes de corregirse:

- `EquipmentNotes.RemoveRange`.
- `VerificationCheckResults.RemoveRange`.

No cambiarlos de forma silenciosa si no hay migracion/estado claro.
