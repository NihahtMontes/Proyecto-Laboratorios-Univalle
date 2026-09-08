# Operacion de la base oficial local

## Estado oficial

- Instancia: `localhost`, instancia predeterminada `MSSQLSERVER`.
- Base de trabajo: `DB_Laboratorios_Univalle`.
- Base de QA destructiva: `DB_Laboratorios_Univalle_QA`.
- Origen preservado: `DB_Laboratorios_Univalle_MIGRACION`, en `READ_ONLY`.
- La aplicacion usa el login restringido `laboratorios_app`; su contrasena solo
  existe en User Secrets.
- `sa` permanece deshabilitado.
- `Database:AutoMigrate=false` y `Database:RunSeed=false` son obligatorios.

## Activacion unica pendiente en Windows

Estas acciones muestran UAC y deben hacerse manualmente como administrador:

1. Reiniciar **SQL Server (MSSQLSERVER)**, o reiniciar Windows. Esto activa el
   modo de autenticacion mixta ya configurado.
2. Abrir **SQL Server Configuration Manager** o `services.msc`, establecer
   **SQL Server Agent (MSSQLSERVER)** en inicio Automatico e iniciarlo.
3. Verificar la cuenta restringida desde la raiz del proyecto:

```powershell
dotnet run --project Tools/DatabaseProvisioning -- --verify
```

La verificacion debe confirmar conexion, lectura/escritura funcional y que el
login no es `sysadmin`, `db_owner` ni puede ejecutar DDL o backup.

## Crear el primer administrador de la aplicacion

No usar la semilla demo. Ejecutar una sola vez:

```powershell
dotnet run --project Tools/AdminBootstrap -- --username admin --email <CORREO> --first-name <NOMBRE> --last-name <APELLIDO> --identity-card <CI> --phone <TELEFONO>
```

La herramienta pide y confirma la contrasena de forma oculta, exige al menos
12 caracteres, verifica que no haya migraciones pendientes y se niega a crear
un segundo SuperAdmin. No pegar la contrasena en chat, scripts o documentos.

## Respaldos

Carpeta oficial:

`C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle`

Trabajos instalados en SQL Server Agent:

- FULL cifrado diario a las 02:00.
- LOG cifrado cada 15 minutos.
- Restauracion de verificacion semanal los domingos a las 04:00.

El primer FULL cifrado fue restaurado en una base temporal y paso
`DBCC CHECKDB`. Los archivos `.cer` y `.pvk` de `Keys` son indispensables para
restaurar en otra instancia. Deben copiarse, junto con la contrasena de
certificado guardada en User Secrets, a una ubicacion externa segura y con
acceso restringido. Una copia que permanece solo en el mismo disco no cubre
la perdida total del equipo.

## Cambios de esquema y datos

- Ejecutar migraciones con la cuenta administradora de Windows y revisar el
  script antes de aplicarlo. El login de la aplicacion no tiene permiso DDL.
- Hacer primero las pruebas destructivas en la base terminada en `_QA`.
- No volver escribible el origen ni apuntar la aplicacion a
  `DB_Laboratorios_Univalle_MIGRACION`.
- No inventar ubicaciones, checklist, tareas ni identidades ausentes en la
  fuente. Registrar la deuda en `DataQualityIssues` y resolverla con evidencia.
- No fusionar personas candidatas automaticamente.
- Evitar hard-delete de historia; usar estado, soft-delete o desvinculacion
  auditada.

## Comprobaciones utiles

```powershell
dotnet run --project Tools/DatabaseQa -- --baseline "<EVIDENCIA>\preflight-qa.json"
dotnet ef migrations has-pending-model-changes --context ApplicationDbContext
dotnet build "Proyecto Laboratorios Univalle.csproj" --no-restore
```

`Tools/DatabaseQa` se niega a trabajar contra una base cuyo nombre no termine
en `_QA`. El baseline aprobado aporta los conteos reales de la copia; no se
usan cantidades historicas hardcodeadas.
