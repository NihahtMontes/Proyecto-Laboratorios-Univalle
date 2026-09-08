# AGENTS.md - Models Y Data

**Ambito**: entidades, enums, DbContext, migraciones y reglas de persistencia.

## Lectura Obligatoria

- `../context.md`.
- `../.agents/skills/database/SKILL.md`.
- Pages y Services consumidores si el modelo pertenece al wizard.

## Reglas Criticas

- No modificar migraciones existentes.
- No hard-delete en entidades de negocio.
- NoTracking global exige `.AsTracking()` para modificar desde PageModels.
- SQL Server es el proveedor vigente.
- Correctivo usa `Semester = 0`; preventivo usa gestiones semestrales (`Semester = 1/2`).
- `Person.FullName` es `[NotMapped]`; no usar en LINQ.
- No agregar query filters globales sin revisar impacto.

## Auditoria

- Preservar propiedades de auditoria existentes.
- No asignar manualmente auditoria si `ApplicationDbContext` ya la centraliza.

## Soft Delete

Usar estados, `IsDeleted` o desvinculacion FK nullable. Si la entidad no soporta borrado logico, crear modelo/migracion antes de cambiar comportamiento.

Residuales conocidos que requieren modelado antes de corregirse:

- `EquipmentNotes.RemoveRange`.
- `VerificationCheckResults.RemoveRange`.
