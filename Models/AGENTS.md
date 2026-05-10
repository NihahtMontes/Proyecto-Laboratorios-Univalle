# AGENTS.md - Models Y Data

**Ambito**: entidades, enums, DbContext, migraciones y reglas de persistencia.

## Lectura Obligatoria

- `../context.md`.
- `../.agent/context/areas/database-ef.md`.
- `../.agent/skills/database/SKILL.md`.
- Modulo especifico si el modelo pertenece al wizard.

## Reglas Criticas

- No modificar migraciones existentes.
- No hard-delete en entidades de negocio.
- NoTracking global exige `.AsTracking()` para modificar desde PageModels.
- PostgreSQL/Npgsql es el proveedor vigente.
- `Management.Semester = 0` es valido para correctivo.
- `Person.FullName` es `[NotMapped]`; no usar en LINQ.
- No agregar query filters globales sin revisar impacto.

## Auditoria

- Preservar propiedades de auditoria existentes.
- No asignar manualmente auditoria si `ApplicationDbContext` ya la centraliza.

## Soft Delete

Usar estados, `IsDeleted` o desvinculacion FK nullable. Si la entidad no soporta borrado logico, crear modelo/migracion antes de cambiar comportamiento.
