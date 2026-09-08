# Migracion segura de SQL Server en nube a local

Esta guia describe el procedimiento operativo. Ningun comando debe ejecutarse
sin confirmar primero el servidor, la base de origen, la base de destino y un
respaldo recuperable.

## 1. Identificar el tipo de origen

- **SQL Server o Azure SQL Managed Instance**: preferir una copia nativa
  `.bak` cuando el proveedor lo permita.
- **Azure SQL Database**: exportar e importar un `.bacpac` con SSMS, Azure
  Portal o `sqlpackage`; Azure SQL Database no entrega un `.bak` nativo.
- **Otro proveedor administrado**: revisar si ofrece `.bak`, `.bacpac` o una
  copia lógica. No copiar tablas manualmente antes de conocer esta respuesta.

Nunca incluir usuarios, contrasenas ni cadenas reales en scripts versionados.

## 2. Preparar SQL Server local

1. Instalar SQL Server Developer y SSMS.
2. Restaurar o importar siempre con un nombre nuevo, por ejemplo
   `DB_Laboratorios_Univalle_MIGRACION`; no usar directamente el nombre de la
   futura base oficial.
3. No apuntar la aplicacion a esa base hasta terminar las validaciones.
4. Conservar `Database:AutoMigrate=false` y `Database:RunSeed=false`.

## 3. Ruta A: restaurar un `.bak`

Inspeccionar primero los nombres logicos:

```sql
RESTORE FILELISTONLY
FROM DISK = N'<RUTA_AL_ARCHIVO_ORIGEN.bak>';
```

Restaurar a archivos locales nuevos usando los nombres logicos obtenidos:

```sql
RESTORE DATABASE [DB_Laboratorios_Univalle_MIGRACION]
FROM DISK = N'<RUTA_AL_ARCHIVO_ORIGEN.bak>'
WITH
    MOVE N'<NOMBRE_LOGICO_DATOS>' TO N'<RUTA_LOCAL_DATOS.mdf>',
    MOVE N'<NOMBRE_LOGICO_LOG>' TO N'<RUTA_LOCAL_LOG.ldf>',
    RECOVERY,
    CHECKSUM,
    STATS = 10;
```

No usar `WITH REPLACE`: puede sobrescribir una base existente.

## 4. Ruta B: importar un `.bacpac`

Importar el archivo como una base nueva mediante **Import Data-tier
Application** de SSMS o `sqlpackage /Action:Import`. Un BACPAC transporta
esquema y datos, pero no todos los objetos de instancia; revisar logins,
usuarios, permisos y trabajos por separado.

## 5. Validaciones obligatorias

Antes de usar la copia:

```sql
DBCC CHECKDB ([DB_Laboratorios_Univalle_MIGRACION]) WITH NO_INFOMSGS;

SELECT MigrationId, ProductVersion
FROM [DB_Laboratorios_Univalle_MIGRACION].[dbo].[__EFMigrationsHistory]
ORDER BY MigrationId;
```

Comparar origen y destino para cada tabla funcional:

- conteo total y conteo por estado;
- claves de negocio duplicadas, especialmente inventario, CI y codigo de
  laboratorio;
- claves foraneas huerfanas;
- valores de enums fuera de los rangos definidos por `Models/Enums`;
- una sola gestion activa por tipo;
- cadenas L-6, L-7, L-8, L-3, Kardex y L-12 por muestreo.

Si la copia ya contiene `__EFMigrationsHistory`, no ejecutar
`dotnet ef database update` hasta comparar esa historia con
`Data/SqlServerMigrations`.

## 6. Configurar la aplicacion local

Guardar la conexion en secretos de usuario, no en `appsettings.json`. La
aplicacion oficial usa un login dedicado de privilegio minimo; la cuenta de
Windows administradora se reserva para migraciones y mantenimiento:

```powershell
dotnet run --project Tools/DatabaseProvisioning
```

Ejecutar primero pruebas de lectura y los recorridos funcionales. La
importacion historica generada por `Tools/HistoricalDataImport` debe probarse
en otra copia antes de combinarse con los datos restaurados.

## 7. Crear y verificar el backup local final

Usar un nombre de archivo nuevo y una carpeta accesible para la cuenta del
servicio SQL Server:

```sql
BACKUP DATABASE [DB_Laboratorios_Univalle_MIGRACION]
TO DISK = N'<RUTA_BACKUP_NUEVO.bak>'
WITH COPY_ONLY, CHECKSUM, COMPRESSION, STATS = 10;

RESTORE VERIFYONLY
FROM DISK = N'<RUTA_BACKUP_NUEVO.bak>'
WITH CHECKSUM;
```

`RESTORE VERIFYONLY` no reemplaza una restauracion de prueba. Para considerar
el respaldo recuperable, restaurarlo con otro nombre y ejecutar `DBCC CHECKDB`.

## 8. Estado de la migracion historica local

La promocion desde `Plantilla_Original.xlsx` se completo el 23-08-2026 sobre
`localhost/DB_Laboratorios_Univalle`. El proyecto usa User Secrets, mantiene
`AutoMigrate=false` y `RunSeed=false`, y la cuenta de aplicacion no puede
alterar esquema ni realizar backups. El origen
`DB_Laboratorios_Univalle_MIGRACION` quedo en solo lectura y la base destructiva
de pruebas es `DB_Laboratorios_Univalle_QA`.

Las 562 filas L-6 sin inventario se preservan en
`HistoricalVerificationQuarantines`, sin inventar relaciones. Las 556 unidades
y 520 solicitudes sin ubicacion verificable usan `LaboratoryId=NULL` y
`LocationResolutionStatus=Pending`. El backup FULL oficial, cifrado y probado
por restauracion real, esta en:

`C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle\Full\DB_Laboratorios_Univalle-FULL-20260823-001459.bak`

SHA-256: `E1255FA7E07A253971A930E3E87A41B4C2457198EEAB2D06AB11FF72AF7E2C3F`.

No ejecutar `DbInitializer.SeedAsync` sobre esta base: mezcla cuentas demo con
datos funcionales. La primera cuenta SuperAdmin debe crearse mediante un flujo
de bootstrap separado y una contrasena proporcionada de forma segura. Ver
`docs/OPERACION_BASE_OFICIAL_LOCAL.md`.
