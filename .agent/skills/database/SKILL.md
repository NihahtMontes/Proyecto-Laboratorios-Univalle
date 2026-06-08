---
name: database
description: EF Core 9 con SQL Server, entidades, enums, DbContext, query filters, auditoria, soft-delete y migraciones.
trigger: Cambios en `Models/`, `Data/`, enums, relaciones EF, migraciones, seeds, query filters o consultas de escritura.
scope: Models/Data/Infrastructure
context: .agent/context/areas/database-ef.md
---
# Skill Database

## 1. Contexto Del Modulo

La capa de datos usa `ApplicationDbContext` con SQL Server, Identity y entidades de mantenimiento, activos, personas, gestion, wizard y reportes. El modelo conserva historial: se prefieren estados, soft-delete o desvinculaciones en vez de hard-delete.

Flujo: PageModel/Service -> `ApplicationDbContext` -> entidades y query filters -> SQL Server -> SaveChanges con auditoria.

## 2. Arquitectura Y Archivos Clave

- DbContext: `Data/ApplicationDbContext.cs`.
- Seed: `Data/DbInitializer.cs`.
- Migraciones vigentes SQL Server: `Data/SqlServerMigrations/`.
- Migraciones legacy/historicas: `Data/PostgresMigrations/`, `Data/Migrations/` y `Migrations/`.
- Entidades core: `Models/Equipment.cs`, `EquipmentUnit.cs`, `Management.cs`, `ManagementPlan.cs`, `Maintenance.cs`, `Request.cs`, `Verification.cs`, `Departure.cs`, `Notification.cs`.
- Enums: `Models/Enums/*.cs`, incluyendo `EquipmentCategory.cs` y `EquipmentTypeClassification.cs`.

## 3. Integracion Con NiceAdmin

- Los enums deben tener `Display(Name=...)` para badges/selects.
- Las vistas usan `EnumHelper.GetDisplayName(...)`; mantener nombres de display claros.
- Query filters impactan tablas visuales: unidades eliminadas no aparecen en inventario operativo.
- Para tarjetas y contadores, proyectar desde entidades vigentes y no cargar historicos completos.

## 4. Patrones Y Convenciones

- Proveedor fijo: SQL Server con Microsoft.EntityFrameworkCore.SqlServer.
- NoTracking global configurado en `Program.cs`; para modificar usar `.AsTracking()` o `FindAsync()`.
- Query filters actuales excluyen eliminados/cancelados en entidades principales.
- No modificar migraciones existentes; crear una nueva en `Data/SqlServerMigrations/` cuando cambie schema.
- No versionar credenciales ni connection strings reales.
- Taxonomia local actual de activos:
  - `EquipmentCategory.Equipment = 0`
  - `EquipmentCategory.Utensil = 1`
  - `EquipmentCategory.Other = 2`
  - `UtensilType` conserva legacy `Vidrio/Plastico/Metal/Porcelana` y agrega subclasificaciones nuevas.
  - `EquipmentTypeClassification` conserva legacy y agrega clasificaciones tecnicas nuevas.

## 5. Contexto Para Agente

- Leer `Models/AGENTS.md` y `.agent/context/areas/database-ef.md`.
- No agregar query filters nuevos sin revisar reportes, Includes y conteos.
- `EquipmentUnit.LaboratoryId` es nullable.
- `Management.Semester` puede ser 1/2 para gestiones semestrales; no inventar formatos sin revisar modulo correctivo.
- Hard-delete residual conocido: `EquipmentNotes.RemoveRange` existe; no cambiarlo sin modelado/migracion.
