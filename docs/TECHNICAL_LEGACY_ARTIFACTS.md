# Artefactos Legacy Y Fuente Activa

## Migraciones

- Activa para SQL Server: `Data/SqlServerMigrations/`.
- Historicas excluidas del build: `Migrations/`, `Data/Migrations/` y `Data/PostgresMigrations/`.
- No editar migraciones existentes. Los cambios nuevos de esquema deben generar una migracion nueva en `Data/SqlServerMigrations/`.

## Prototipos Y Archivos No Productivos

Estos archivos se conservan para auditoria o soporte, pero no deben tratarse como artefactos de produccion:

- `wwwroot/css/tailwind-dashboard.css`: CSS Tailwind no referenciado por las vistas actuales.

Eliminados el 2026-10-26 con aprobacion del usuario (siguen en el historial de git): `Gastro/`, `tmp/`, `recursos/`, `dashboard.html`, `database_seed.sql`, `migration_equipment_softdelete.sql`, `fix_script.py`, `fix-encoding.ps1`, `read_excel.py` y `CleanTemplates.csx`. La documentacion antigua de la raiz se movio a `docs/archive/` y las guias de MonsterASP a `docs/deployment/`.

El proyecto excluye estos artefactos del build/publish mediante el `.csproj`, sin borrarlos. Cualquier eliminacion fisica requiere aprobacion explicita.
