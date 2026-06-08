# Artefactos Legacy Y Fuente Activa

## Migraciones

- Activa para SQL Server: `Data/SqlServerMigrations/`.
- Historicas excluidas del build: `Migrations/`, `Data/Migrations/` y `Data/PostgresMigrations/`.
- No editar migraciones existentes. Los cambios nuevos de esquema deben generar una migracion nueva en `Data/SqlServerMigrations/`.

## Prototipos Y Archivos No Productivos

Estos archivos se conservan para auditoria o soporte, pero no deben tratarse como artefactos de produccion:

- `Gastro/`: prototipo independiente no integrado al flujo ASP.NET Core principal.
- `wwwroot/css/tailwind-dashboard.css`: CSS Tailwind no referenciado por las vistas actuales.
- `dashboard.html`: prototipo suelto de dashboard.
- `database_seed.sql` y `migration_equipment_softdelete.sql`: scripts historicos/manuales, no migraciones EF activas.
- `fix_script.py`, `fix-encoding.ps1`, `read_excel.py` y `CleanTemplates.csx`: tooling local/manual.

El proyecto excluye estos artefactos del build/publish mediante el `.csproj`, sin borrarlos. Cualquier eliminacion fisica requiere aprobacion explicita.
