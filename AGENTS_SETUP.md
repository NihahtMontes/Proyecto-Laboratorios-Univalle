# AGENTS_SETUP.md - Configuracion Post-Clonacion Y Conexion

**Objetivo**: prevenir errores de infraestructura al descargar el proyecto y asegurar la integridad del entorno local de base de datos.

## 1. Checklist Post-Clonacion

Despues de realizar un `git clone`, sigue este orden para evitar fallos criticos de ejecucion:

### 1.1 Verificacion De Dependencias

- Ejecuta `dotnet restore` para regenerar `obj/project.assets.json` con la configuracion NuGet de la maquina actual.
- Ejecuta `dotnet build "Proyecto Laboratorios Univalle.csproj"` para validar la compilacion base.
- **QuestPDF**: el sistema usa licencia comunitaria configurada en `Program.cs` con `QuestPDF.Settings.License = LicenseType.Community`.

### 1.2 Configuracion De Base De Datos SQL Server

El proveedor vigente es SQL Server con `Microsoft.EntityFrameworkCore.SqlServer`.

La cadena de desarrollo esperada usa SQL Server Developer Edition en instancia predeterminada (`localhost` o `.`). No usar LocalDB, SQL Express ni `SQLEXPRESS`. La base oficial local es `DB_Laboratorios_Univalle`.

```json
"DefaultConnection": "Server=localhost;Database=DB_Laboratorios_Univalle;User Id=<LOGIN_DE_APLICACION>;Password=<SECRETO>;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

La cadena versionada queda vacia deliberadamente. Configurala en secretos de usuario:

```powershell
dotnet run --project Tools/DatabaseProvisioning
```

Reglas:

- La aplicacion local oficial usa `DB_Laboratorios_Univalle`.
- Las pruebas destructivas usan exclusivamente `DB_Laboratorios_Univalle_QA`.
- El origen `DB_Laboratorios_Univalle_MIGRACION` se conserva en solo lectura.
- No versionar passwords reales.
- No habilitar ni usar `sa` para la aplicacion. El aprovisionador crea
  `laboratorios_app`, guarda su secreto solo en User Secrets y le niega DDL,
  control y backup.
- Las migraciones EF y tareas administrativas se ejecutan con la identidad de
  Windows autorizada, no con el login de la aplicacion.

### 1.3 Inicializacion De Datos

Por defecto, `Database:AutoMigrate=false` y `Database:RunSeed=false`. El arranque no modifica la base de datos. Este comportamiento es intencional para evitar aplicar migraciones o datos demo sobre una base equivocada.

Despues de revisar la cadena, el respaldo y las migraciones pendientes, la actualizacion manual es:

```bash
dotnet ef database update --context ApplicationDbContext
```

La semilla solo puede habilitarse en `Development`. Ademas requiere `Seed:DefaultPassword` fuera del repositorio y ya no cambia la contrasena de usuarios existentes:

```powershell
dotnet user-secrets set "Seed:DefaultPassword" "<CONTRASENA_TEMPORAL_FUERTE>"
```

No habilites `AutoMigrate` ni `RunSeed` para produccion o durante una importacion historica.

## 2. Errores Comunes De Infraestructura

### `Cannot open database ... requested by the login`

Ocurre cuando la base no existe o el usuario no tiene permisos.

Solucion recomendada:

```bash
dotnet ef database update --context ApplicationDbContext
```

### Error De Conexion A SQL Server Developer

Si la cadena tiene formato correcto pero no conecta:

1. Verifica que el servicio de SQL Server Developer Edition este iniciado.
2. Confirma que la instancia predeterminada responda en `localhost` o `.`.
3. Si el login dedicado fue aprovisionado antes de activar el modo mixto,
   reinicia `SQL Server (MSSQLSERVER)` y ejecuta
   `dotnet run --project Tools/DatabaseProvisioning -- --verify`.

### Confusion SQL Server Vs PostgreSQL

El proyecto vigente usa `UseSqlServer()` en `Program.cs`. No uses cadenas tipo `Host=localhost;Port=5432` ni dependencias Npgsql salvo que se apruebe explicitamente otro cambio de proveedor.
