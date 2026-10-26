# Sistema de Gestión de Laboratorios — Gastronomía

Aplicación **ASP.NET Core 9** con Razor Pages y controladores, Entity Framework Core 9 sobre SQL Server e Identity. Proyecto web: `Proyecto Laboratorios Univalle.csproj`; solución: `Proyecto Laboratorios Univalle.sln`. `Program.cs` es el punto de entrada. El repositorio incluye herramientas locales bajo `Tools/` que **no** forman parte de la publicación web.

## Empezar una conversación con un agente

1. Entregarle el contenido de **[`docs/deployment/PROMPT_AGENTE_MONSTERASP.md`](docs/deployment/PROMPT_AGENTE_MONSTERASP.md)**: es el texto listo para copiar.
2. El agente debe leer **[`docs/deployment/INICIO_AGENTE_MONSTERASP.md`](docs/deployment/INICIO_AGENTE_MONSTERASP.md)**: contexto, rutas de documentación y precauciones del destino nuevo.
3. Para tareas del proyecto en general: [`AGENTS.md`](AGENTS.md), [`context.md`](context.md) y [`docs/AGENT_WORKFLOW.md`](docs/AGENT_WORKFLOW.md). Leer las instrucciones/skills del módulo afectado, no todo el repositorio.

## Preparación MonsterASP

- **Guía de despliegue:** [`docs/deployment/README.md`](docs/deployment/README.md).
- **Puertas de publicación:** [`docs/deployment/CHECKLIST_PUBLICACION.md`](docs/deployment/CHECKLIST_PUBLICACION.md).
- La cadena del panel confirma base **`db70630`**, servidor interno `db70630.databaseasp.net`, puerto `1433`, y endpoint Remote access `db70630.public.databaseasp.net:1433`. Login y base son distintos. El origen local tiene 11 migraciones. Panel muestra `.bak` subido, Restore **Done** y backup `SAFETY`; autenticación SQL y cotejo de datos/migraciones aún pendientes.
- `appsettings.json` mantiene `ConnectionStrings:DefaultConnection` vacío a propósito. El hosting debe proporcionar `ConnectionStrings__DefaultConnection` por un canal privado, junto con `ASPNETCORE_ENVIRONMENT=Production`. **No guardar contraseñas en Git, prompts, README, perfiles de publicación ni capturas compartidas.** La contraseña divulgada en una captura debe rotarse antes de usarla.
- En esta PC, User Secrets actualmente apunta a `localhost/DB_Laboratorios_Univalle` mediante autenticación integrada, no a MonsterASP. Para desarrollo local deberá sustituirse por la cadena Remote access guardada privadamente en User Secrets; no se ha cambiado todavía.
- En Production el arranque **no** aplica migraciones ni carga datos de ejemplo; preparar la base y el primer usuario requiere una misión de producción separada, destino comprobado y respaldo recuperable.
- Los perfiles FTP existentes pertenecen al sitio anterior; **no** usar esos perfiles para el nuevo destino. `Tools/SyncCloudDb` se retiró por contener una credencial y operaciones destructivas; los secretos antiguos siguen presentes en la **historia Git** hasta que sean rotados y se acuerde su tratamiento con el equipo.

## Ingeniería local (sin conectar con producción)

```powershell
dotnet build "Proyecto Laboratorios Univalle.csproj" -c Release -m:1
```

El comando de publicación **para una misión posterior**, si el hosting soporta Windows x64 y el Hosting Bundle de .NET 9, está en la guía de despliegue. Publicar el árbol de trabajo actual incluye sus cambios sin commit: verificar `git status --short`, elegir explícitamente la revisión e inspeccionar el paquete antes de subirlo. No ejecutar `dotnet ef database update`, importar datos, publicar ni crear usuarios sobre la nube como parte del simple arranque de un agente.

## Troubleshooting – SQL Server / DNS en Windows

El endpoint para PC/SSMS con Remote Access habilitado es `db70630.public.databaseasp.net`, puerto `1433`. No uses una IP fija: el proveedor indica que puede cambiar.

### Distinguir DNS, TCP y autenticación SQL

1. Comprueba la resolución normal del nombre:

   ```powershell
   Resolve-DnsName db70630.public.databaseasp.net
   ```

   Si devuelve `DNS_ERROR_RCODE_NAME_ERROR` pero la consulta explícita a otro resolvedor funciona, el problema es DNS de Windows/red, antes de cualquier conexión SQL.

2. Compara con Cloudflare DNS:

   ```powershell
   Resolve-DnsName db70630.public.databaseasp.net -Server 1.1.1.1
   ```

3. Si resuelve, comprueba la conexión de transporte:

   ```powershell
   Test-NetConnection db70630.public.databaseasp.net -Port 1433
   ```

   `TcpTestSucceeded : True` solo verifica TCP host/puerto; **no** confirma autenticación SQL, nombre de base, permisos ni que la aplicación pueda iniciar sesión.

4. Si falla el DNS normal, identifica el alias y conserva su configuración actual antes de cambiarla:

   ```powershell
   Get-NetAdapter
   Get-DnsClientServerAddress -InterfaceAlias "Wi-Fi"
   ```

   Con permisos de administrador y tras guardar cómo restaurar la configuración anterior, el cambio probado fue:

   ```powershell
   Set-DnsClientServerAddress -InterfaceAlias "Wi-Fi" -ServerAddresses ("1.1.1.1","8.8.8.8")
   ipconfig /flushdns
   Resolve-DnsName db70630.public.databaseasp.net
   Test-NetConnection db70630.public.databaseasp.net -Port 1433
   ```

   Sustituye `Wi-Fi` si la interfaz tiene otro alias. `ipconfig /flushdns` **solo vacía la caché local**; no cambia el servidor DNS. Si una organización administra la red, coordina el ajuste con su administrador.

En esta sesión, tras corregir DNS, se verificaron la resolución del endpoint y TCP 1433. La autenticación SQL y el login web aún requieren pruebas separadas.

### Connection strings y credenciales

La aplicación requiere `ConnectionStrings:DefaultConnection`. Para desarrollo local, configúrala en User Secrets del proyecto o en `ConnectionStrings__DefaultConnection`; para producción, en la configuración privada del sitio. Nunca guardes la contraseña ni la cadena completa en `appsettings.json` versionado, README, prompts, logs o Git. Si una contraseña fue capturada/compartida, rótala antes de seguir usándola.
