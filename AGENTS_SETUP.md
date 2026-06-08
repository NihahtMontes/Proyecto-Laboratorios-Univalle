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

La cadena de desarrollo esperada usa SQL Server Developer Edition en instancia predeterminada (`localhost` o `.`). No usar LocalDB, SQL Express ni `SQLEXPRESS`.

```json
"DefaultConnection": "Server=localhost;Database=DB_Laboratorios_Univalle_DEV;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Reglas:

- Desarrollo usa normalmente `DB_Laboratorios_Univalle_DEV`.
- Produccion usa `DB_Laboratorios_Univalle`.
- No versionar passwords reales.
- Si se usa autenticacion mixta con `sa` o usuario dedicado, colocar credenciales en user-secrets, variables de entorno o configuracion segura de despliegue.

### 1.3 Inicializacion De Datos

El proyecto esta configurado para auto-migrar al iniciar. En `Program.cs`:

- Se llama a `db.Database.MigrateAsync()` para aplicar migraciones pendientes.
- Se llama a `DbInitializer.SeedAsync(services)` para cargar datos base.

Si prefieres aplicar migraciones manualmente, usa:

```bash
dotnet ef database update --context ApplicationDbContext
```

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
3. Si usas autenticacion mixta, confirma que el login `sa` este habilitado y que la password no este versionada.

### Confusion SQL Server Vs PostgreSQL

El proyecto vigente usa `UseSqlServer()` en `Program.cs`. No uses cadenas tipo `Host=localhost;Port=5432` ni dependencias Npgsql salvo que se apruebe explicitamente otro cambio de proveedor.
