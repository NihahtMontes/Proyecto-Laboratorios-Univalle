# 🔧 AGENTS_SETUP.md (Configuración Post-Clonación y Conexión)
**Objetivo**: Prevenir errores de infraestructura al descargar el proyecto y asegurar la integridad del entorno local de base de datos.

## 1. El Check-list del Recién Llegado (Post-Clonación)
Después de realizar un `git clone`, SIGUE ESTE ORDEN para evitar fallos críticos de ejecución:

### 1.1 Verificación de Dependencias
- Corre `dotnet build` en la terminal para restaurar paquetes NuGet y validar la compilación base.
- **QuestPDF**: El sistema usa una licencia comunitaria configurada en `Program.cs` (QuestPDF.Settings.License = LicenseType.Community).

### 1.2 Configuración de la Base de Datos (SQL Server)
Este es el punto más propenso a errores. Verifica tu `appsettings.json`:

#### Sintaxis Correcta del Connection String
La cadena de conexión **DEBE** comenzar obligatoriamente con la palabra clave `Server=` o `Data Source=`.

*   ✅ **Correcto (Standard/Local):** `"Server=.\\SQLEXPRESS;Database=DB_Laboratorios_Univalle;..."`
*   ❌ **Error de Sintaxis:** `"\\SQLEXPRESS;Database=..."` (Esto genera el error: `Keyword not supported`)

> **⚠️ NOTA TÉCNICA (Escape de JSON):** En el archivo JSON, la barra invertida `\` debe escaparse con otra barra `\\`. Al final, C# leerá un solo `.\SQLEXPRESS`.

### 1.3 Inicialización de Datos
El proyecto está configurado para **auto-migrar** al iniciar. En `Program.cs`:
- Se llama a `db.Database.MigrateAsync()` para crear tablas si no existen.
- Se llama a `DbInitializer.SeedAsync(services)` para cargar datos de prueba (usuarios, roles, catálogos).

Si prefieres hacerlo manualmente, usa:
```bash
dotnet ef database update
```

## 2. Resolución de Errores Comunes de Infraestructura

### `Keyword not supported: '\sqlexpress;database'`
Ocurre cuando la cadena de conexión está mal escrita. El parser del driver de SQL Server (SqlClient) no reconoce el inicio de la cadena porque le falta el identificador `Server=`.
- **Solución**: Asegúrate de que la cadena empiece con `Server=.\\SQLEXPRESS` (o el nombre de tu instancia).

### `An error occurred while connecting to the database`
Si la sintaxis es correcta pero no conecta:
1. Verifica que el servicio de **SQL Server (SQLEXPRESS)** esté en ejecución en Windows.
2. Asegúrate de que `TrustServerCertificate=True` esté presente en la cadena de conexión si no tienes certificados SSL configurados localmente.

### Confusión PostgreSQL vs SQL Server
Si en el pasado se usó PostgreSQL y los paquetes `Npgsql` están presentes en el `.csproj`, verifica en `Program.cs` si el servicio `AddDbContext` está llamando a `.UseSqlServer()` o `.UseNpgsql()`. No mezcles cadenas de conexión de un motor con el driver del otro.
