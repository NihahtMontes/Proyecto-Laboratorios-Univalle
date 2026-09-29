# Sistema de Gestión de Laboratorios — Gastronomía

Aplicación **ASP.NET Core 9** con Razor Pages y controladores, Entity Framework Core 9 sobre SQL Server e Identity. Proyecto web: `Proyecto Laboratorios Univalle.csproj`; solución: `Proyecto Laboratorios Univalle.sln`. `Program.cs` es el punto de entrada. El repositorio incluye herramientas locales bajo `Tools/` que **no** forman parte de la publicación web.

## Empezar una conversación con un agente

1. Entregarle el contenido de **[`PROMPT_AGENTE_MONSTERASP.md`](PROMPT_AGENTE_MONSTERASP.md)**: es el texto listo para copiar.
2. El agente debe leer **[`INICIO_AGENTE_MONSTERASP.md`](INICIO_AGENTE_MONSTERASP.md)**: contexto, rutas de documentación y precauciones del destino nuevo.
3. Para tareas del proyecto en general: [`AGENTS.md`](AGENTS.md), [`context.md`](context.md) y [`docs/AGENT_WORKFLOW.md`](docs/AGENT_WORKFLOW.md). Leer las instrucciones/skills del módulo afectado, no todo el repositorio.

## Preparación MonsterASP

- **Guía de despliegue:** [`docs/deployment/README.md`](docs/deployment/README.md).
- **Puertas de publicación:** [`docs/deployment/CHECKLIST_PUBLICACION.md`](docs/deployment/CHECKLIST_PUBLICACION.md).
- La base nueva fue presentada mediante un **hostname SQL interno y puerto 1433**. El **nombre físico de la base, el estado de las migraciones y el sitio web de destino siguen pendientes de comprobación**: el login SQL no identifica necesariamente la base. El hostname interno no debe probarse desde fuera de la red del proveedor dando por supuesto que hay acceso.
- `appsettings.json` mantiene `ConnectionStrings:DefaultConnection` vacío a propósito. El hosting debe proporcionar `ConnectionStrings__DefaultConnection` por un canal privado, junto con `ASPNETCORE_ENVIRONMENT=Production`. **No guardar contraseñas en Git, prompts, README, perfiles de publicación ni capturas compartidas.** La contraseña divulgada en una captura debe rotarse antes de usarla.
- En Production el arranque **no** aplica migraciones ni carga datos de ejemplo; preparar la base y el primer usuario requiere una misión de producción separada, destino comprobado y respaldo recuperable.
- Los perfiles FTP existentes pertenecen al sitio anterior; **no** usar esos perfiles para el nuevo destino. `Tools/SyncCloudDb` se retiró por contener una credencial y operaciones destructivas; los secretos antiguos siguen presentes en la **historia Git** hasta que sean rotados y se acuerde su tratamiento con el equipo.

## Ingeniería local (sin conectar con producción)

```powershell
dotnet build "Proyecto Laboratorios Univalle.csproj" -c Release -m:1
```

El comando de publicación **para una misión posterior**, si el hosting soporta Windows x64 y el Hosting Bundle de .NET 9, está en la guía de despliegue. Publicar el árbol de trabajo actual incluye sus cambios sin commit: verificar `git status --short`, elegir explícitamente la revisión e inspeccionar el paquete antes de subirlo. No ejecutar `dotnet ef database update`, importar datos, publicar ni crear usuarios sobre la nube como parte del simple arranque de un agente.
