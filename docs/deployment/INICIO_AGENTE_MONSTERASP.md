# Inicio de conversación — preparación ASP.NET / MonsterASP

> El mensaje inicial listo para copiar está en `PROMPT_AGENTE_MONSTERASP.md`. Este documento es una **ruta de contexto**, no una orden automática de publicar, migrar ni crear usuarios.

## 1. Identidad y alcance

- Sistema: Laboratorios Univalle, ASP.NET Core 9 (`Proyecto Laboratorios Univalle.csproj`, `Proyecto Laboratorios Univalle.sln`), Razor Pages + controladores, EF Core 9/SQL Server, Identity. Arranque en `Program.cs`; migraciones en `Data/SqlServerMigrations/`. Carpeta para publicar por IIS: salida de `dotnet publish` del proyecto web, no el repositorio completo.
- Antes de cualquier intervención: identificar **raíz Git real** (`git rev-parse --show-toplevel`), rama, `git status --short`, commit y diferencias de los archivos a tocar. No tratar `D:\proyectoSis\...` en documentos antiguos como si fuera necesariamente la ubicación de este checkout. Hay cambios locales sin commit en Planificación, `Program.cs`, `Data/DbInitializer.cs`, `appsettings.json` y agentes: **no resetear, limpiar ni sobrescribir ese trabajo**. Las cifras y el estado de esos cambios pueden variar; verificar siempre.
- Prioridad de evidencia: código/contratos ejecutables > self-checks/QA > artefactos de una ejecución concreta > documentación. No promover demos, hashes o conteos históricos a estado actual sin comprobar la ejecución.

## 2. Orden de lectura (solo ampliar por módulo)

1. `AGENTS.md`, `context.md`, `docs/AGENT_WORKFLOW.md`: guardrails, propiedad temporal, flujo de agentes. Leer `docs/COORDINACION_MULTI_CHAT.md` si se toca datos, QA, arquitectura, navegación o sprint; sus referencias a antigua nube/checkout pueden ser históricas.
2. `AGENTS_SETUP.md` para instalación local; `docs/deployment/README.md` y `docs/deployment/CHECKLIST_PUBLICACION.md` para este destino. **La guía de despliegue no acredita que se haya desplegado nada.**
3. Contratos vigentes: `Proyecto Laboratorios Univalle.csproj`, `Program.cs`, `appsettings.json` (**leer solo las claves, no difundir valores**), `Data/ApplicationDbContext.cs`, `Data/SqlServerMigrations/`, `Properties/launchSettings.json` y los perfiles de publicación si corresponden. `launchSettings` es de desarrollo. `Properties/PublishProfiles/FTPProfile1.pubxml` apunta a sitio antiguo: no invocarlo para el nuevo destino.
4. Solo si afecta código de módulos: `Pages/AGENTS.md` + skill `backend-methods`/`ui-premium`, `Models/AGENTS.md` + `database`, `Services/AGENTS.md` + `reporting`; después leer archivos concretos. Para Operación de Gastronomía ver `Pages/LaboratoryOperations/` y servicios/modelos realmente enlazados; **si está disponible en ese checkout**, `docs/SQA/REQUERIMIENTOS_MODULO_PLANIFICACION_GASTRONOMIA.md` documenta un commit fijo, no necesariamente el working tree actual.
5. Si el trabajo exige datos históricos/importación, leer `docs/GUIA_IMPORTACION_HISTORICA.md`, `Tools/HistoricalDataImport/` y `Tools/HistoricalDataApply/` **solo** en ese alcance; no confundir importación histórica con planificación semanal ni ejecutar migraciones existentes por rutina.

## 3. Destino nuevo: hechos vs incógnitas

| Confirmado por la captura proporcionada | Todavía NO confirmado |
|---|---|
| SQL Server MonsterASP; panel confirma base `db70630`, endpoints Local/Remote access. Origen local con 11 migraciones. Panel muestra BAK subido, Restore Done y backup SAFETY. DNS/TCP públicos pasan desde esta sesión. | El User Secret vigente sigue apuntando al SQL local, no a la nube. Autenticación SQL remota y comparación del contenido/migraciones restaurados aún no verificadas. |

El hostname interno es para sitios dentro del proveedor; desde el PC se usa Remote access. DNS/TCP públicos pasan, pero TCP no prueba autenticación SQL. El panel reporta restore completado y backup SAFETY previo; falta cotejo SQL. La app local actualmente usa el User Secret de `localhost/DB_Laboratorios_Univalle`; no se cambió a la nube. **La contraseña expuesta en capturas debe rotarse y no volver a copiarse a código, README, chat, Git, `.pubxml` ni shell history.** Credenciales antiguas siguen en Git history.

`Program.cs` exige `ConnectionStrings:DefaultConnection` y tiene `Database:AutoMigrate=false`, `Database:RunSeed=false`; en Production prohíbe activar migración/semilla automáticas. Proveer **`ConnectionStrings__DefaultConnection`** mediante configuración privada del hosting, `ASPNETCORE_ENVIRONMENT=Production`, HTTPS y permisos persistentes de escritura en `DataProtectionKeys/` y `wwwroot/uploads/{equipment,users}/`. Verificar las licencias EPPlus/QuestPDF. El programa de bootstrap `Tools/AdminBootstrap` exige host y base esperados como argumentos para un destino remoto, y `--check` **abre una conexión SQL** aunque no cree usuarios; no usarlo sin validar destino y permiso operativo.

## 4. Protocolo para cualquier trabajo futuro

- Alcance, ID, propietario único por archivo, tipo de escritura, dependencias y evidencia esperada primero en la conversación. `migration-worker` puede hacer lecturas extensas; el orquestador integra, decide y verifica. No delegar ediciones solapadas.
- Para ingeniería local ordinaria se permiten shell, Git, builds y pruebas acotadas. No destruir cambios ajenos. **Producción, despliegues, escrituras en BD y migraciones requieren autorización específica, verificación del destino y respaldo recuperable.** La captura con credenciales no sustituye esa verificación.
- Nunca tocar la fuente institucional `D:/proyectoSis/Excels/Plantilla_Original.xlsx`; si se usa, SHA-256 antes y después e igualdad. No subir claves `DataProtectionKeys` de desarrollo, uploads históricos no comprobados, backups, secretos ni carpeta `Tools/` como si fuese la web.
- Al cerrar: revisar diff/estado Git, qué se compiló/probó **de verdad**, qué no pudo verificarse por la red interna MonsterASP, archivos afectados y riesgos pendientes. Si se publica en una misión posterior, probar autenticación, roles, cargas, reportes, migraciones aplicadas y logs con evidencia del destino exacto.
