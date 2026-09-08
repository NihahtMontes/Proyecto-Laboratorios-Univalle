---
name: database
description: "Implementa o revisa EF Core 9 y SQL Server en Laboratorios Univalle: entidades, DbContext, relaciones, enums, auditoria, soft-delete, query filters, seeds y migraciones. Usar con `Models/`, `Data/` o consultas de escritura; no usar para consultas de solo lectura triviales."
---

# Datos y EF Core

Lee `context.md` y el `Models/AGENTS.md` aplicable antes de editar.

## Invariantes

- SQL Server es el proveedor vigente. No agregues Npgsql, LocalDB ni cambies de proveedor sin aprobacion.
- El modelo debe conservar historial: prefiere estados, soft-delete o desvinculacion segura a hard-delete.
- No modifiques migraciones existentes. Un cambio de schema requiere una migracion nueva y aprobacion explicita antes de crearla o aplicarla.
- No ejecutes migraciones ni mutaciones contra una base real sin autorizacion especifica.
- NoTracking global exige `.AsTracking()` o `FindAsync()` para modificar entidades.
- Revisa el impacto de nuevos query filters sobre Includes, reportes, conteos y recuperacion historica.
- Conserva auditoria centralizada en `ApplicationDbContext`; no la dupliques manualmente.
- `Person.FullName` es `[NotMapped]` y no puede traducirse en LINQ.
- `Management.Semester` admite `0` para correctivo y `1`/`2` para preventivo.
- Nunca versionas credenciales ni connection strings reales.

## Forma de trabajo

1. Localiza entidad, configuracion de `DbContext`, consumidores y migraciones vigentes antes de disenar el cambio.
2. Explica cardinalidad, nulabilidad, borrado, compatibilidad historica y rollback.
3. Para cambios de schema, presenta primero el plan y el nombre propuesto de la migracion.
4. Verifica consultas de lectura y escritura, tracking, concurrencia y comportamiento con datos legacy.
5. Ejecuta `dotnet build`; ejecuta pruebas o comandos EF solo si el alcance y la autorizacion lo permiten.

Reporta impacto de datos, compatibilidad, comandos ejecutados y cualquier operacion que quedo pendiente de autorizacion.
