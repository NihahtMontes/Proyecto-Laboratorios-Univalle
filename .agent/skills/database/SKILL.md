---
name: database
description: EF Core, PostgreSQL, migraciones, soft-delete, tracking y modelado historico.
trigger: Cambios en `Models/`, `Data/`, migraciones, relaciones, queries LINQ de escritura o borrado.
scope: Data/Models
context: .agent/context/areas/database-ef.md
---
# Skill Database

## Leer Antes

- `.agent/context/areas/database-ef.md`.
- `Models/AGENTS.md`.

## Prohibido

- Modificar migraciones existentes.
- Hard-delete de entidades con valor historico.
- Actualizar entidades cargadas sin tracking.
- Agregar query filters globales sin revisar impacto.

## Tracking

NoTracking global esta activo. Para modificar:

```csharp
var entity = await _context.Entities
    .AsTracking()
    .FirstOrDefaultAsync(...);
```

`FindAsync()` tambien trackea.

## Soft Delete

Preferir:

- `Status = Deleted/Cancelled/Eliminado`.
- `IsDeleted = true`.
- Desvincular FK nullable preservando el registro.

## Migraciones

- Crear nuevas migraciones en `Data/PostgresMigrations/`.
- Revisar PostgreSQL/Npgsql, no SQL Server.
- No incluir credenciales en cambios de configuracion.

## Datos Especiales

- `Management.Semester = 0` es valido para correctivo.
- `EquipmentUnit.LaboratoryId` es nullable.
- `Person.FullName` no se usa en LINQ.
