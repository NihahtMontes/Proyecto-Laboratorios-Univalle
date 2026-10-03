# AGENTS.md - Orquestador Principal Codex/OpenCode

**Proyecto**: Laboratorios Univalle  
**Rol**: coordinar Codex u OpenCode, enrutar contexto tecnico y evitar cambios amplios innecesarios.

## Principios

- Alta precision tecnica: cambios pequenos, correctos y verificables.
- UI premium: NiceAdmin/Bootstrap 4 con jerarquia visual, no formularios planos.
- Integridad historica: evitar hard-delete; preferir estados, soft-delete o desvinculacion segura.
- Contexto modular: no leer todo el historico si una tarea solo toca un modulo.

## AUTONOMOUS ENGINEERING AUTHORITY

Los agentes configurados pueden realizar sin permisos repetidos la ingenieria ordinaria necesaria para una tarea asignada: shell, filesystem, Git, ramas/worktrees locales, builds, tests, lint, restores, instalaciones project-local necesarias, logs/procesos/puertos, browser/runtime y servidores locales temporales (cerrarlos y comprobarlo al terminar). Los commits locales forman parte de esta autonomia solo cuando la mision los incluye; no hacer commits por rutina. Consultas a BD de desarrollo son ordinarias; escrituras y migraciones de desarrollo requieren una mision explicita y verificar el destino. Despliegues, produccion, push y cambios externos requieren pedido especifico. Nunca destruir modificaciones desconocidas, exponer secretos ni modificar el Excel institucional original; consultar `docs/AGENT_WORKFLOW.md`.

## Flujo Obligatorio

Antes de tocar archivos:

1. Leer `context.md`.
2. Leer `docs/COORDINACION_MULTI_CHAT.md` si el trabajo afecta datos,
   modelos, QA, navegacion o planificacion de sprints.
3. Leer el `AGENTS.md` local mas cercano si existe.
4. Leer la skill correspondiente en `.agents/skills/`.
5. Inspeccionar solo los archivos y documentacion del modulo afectado.

Si la tarea toca mas de 5 archivos, proponer plan antes de editar.

Los commits locales estan preautorizados cuando forman parte de la mision; no hacer commit si la mision no lo requiere.

## Matriz De Enrutamiento

| Capa/Tarea | Contexto | Manifesto | Skill |
|---|---|---|---|
| UI Razor, `.cshtml`, JS, SweetAlert2 | `context.md` | `Pages/AGENTS.md` | `.agents/skills/ui-premium/SKILL.md` |
| PageModels, handlers, InputModel | `context.md` | `Pages/AGENTS.md` | `.agents/skills/backend-methods/SKILL.md` |
| Modelos, EF, migraciones | `context.md` | `Models/AGENTS.md` | `.agents/skills/database/SKILL.md` |
| Reportes Excel/PDF | `context.md` | `Services/AGENTS.md` | `.agents/skills/reporting/SKILL.md` |
| Infraestructura/build/despliegue | `context.md` | `AGENTS_SETUP.md` si existe | segun archivos tocados |
| Wizard | `context.md` y flujo real en `Pages/` | `Pages/AGENTS.md` | backend/ui segun cambio |
| Dashboard, navegacion y notificaciones | `context.md` y archivos afectados | `Pages/AGENTS.md` | backend/ui |
| Borradores | `context.md` y archivos afectados | `Pages/AGENTS.md` | backend/database |
| Correctivo | `context.md` y archivos afectados | `Pages/AGENTS.md` | backend |

## Modulos Especificos

- L-6, L-3, Kardex/L-48 y L-12: partir de `context.md`, ubicar el flujo real con busqueda dirigida y leer solo sus Pages, Services y modelos relacionados.

## Reglas Criticas Cortas

- `ManagementId` se preserva en todo el wizard.
- NoTracking global exige `.AsTracking()` para modificar.
- SweetAlert2 es v7: usar `result.value` ademas de `isConfirmed`.
- Crear registros muestra exito posterior; editar y eliminar requieren confirmacion SweetAlert cuando cambian datos existentes o hacen soft-delete.
- Mensajes CRUD deben usar `TempData.Success/Error/Warning/Info`; el layout debe leer `SuccessMessage/ErrorMessage/WarningMessage/InfoMessage`.
- `Completados` es `Step = 7` visual.
- Migraciones existentes son intocables.
- No revertir cambios ajenos.

## Ejecucion Controlada De Recursos

- Ejecutar un solo build, prueba, importador o proceso pesado a la vez; para
  .NET usar `dotnet build -m:1` y evitar servidores en segundo plano.
- Procesar escrituras EF/SQL en lotes de hasta 100 filas, limpiar el
  `ChangeTracker` entre lotes y limitar la conexion a un maximo de 10
  conexiones cuando la herramienta controle el pool.
- Las pruebas de estres son opt-in y requieren autorizacion explicita. El
  agente no impone un umbral fijo de RAM ni detiene tareas por un porcentaje
  determinado de memoria; el operador administra la capacidad de su equipo.
- No cambiar automaticamente la memoria global de SQL Server.
- Los servidores locales y el browser de desarrollo pueden iniciarse durante una tarea que los requiera; cerrar y verificar todo proceso temporal al finalizar.

## Orquestacion Multiagente

El agente principal de Codex u OpenCode actua como orquestador: define alcance, reparte tareas independientes, integra resultados, resuelve conflictos y verifica evidencia. No delegar por rutina ni permitir cambios solapados sobre los mismos archivos.

Para lecturas extensas, busquedas, inventarios y evidencia mecanica usar preferentemente `migration-worker` (MiniMax M3); Sol conserva decisiones y validacion final. `subagent_depth: 1` impide delegacion ulterior del worker.

La coordinacion persistente entre tareas de Codex se define en
`docs/COORDINACION_MULTI_CHAT.md`. Antes de actuar, cada tarea declara rol, ID,
archivos/superficies, dependencias y si realizara lectura, escritura local o
escritura externa. El coordinador de datos mantiene el estado canonico; QA y
Sprint entregan handoffs y no modifican simultaneamente ese documento.

La coordinacion dentro de una sesion OpenCode se define en
`docs/AGENT_WORKFLOW.md`. La propiedad temporal es un protocolo del
orquestador, no un bloqueo transaccional provisto por la herramienta.

- `ui_ux`: propietario de `.cshtml`, SweetAlert2, textos visibles, botones, confirmaciones y consistencia NiceAdmin/Bootstrap 4.
- `backend_data`: propietario de `.cshtml.cs`, `InputModel`, EF tracking, soft-delete, redirects, `TempData` y validaciones servidor.
- `reviewer`: revision independiente y read-only de correccion, seguridad, regresiones y pruebas.
- El agente principal conserva decisiones, integracion, reglas de datos y verificacion final.

## Documentacion

- `.codex/agents/`: agentes especializados del proyecto.
- `.opencode/agents/`: coordinador y subagentes especializados de OpenCode.
- `.agents/skills/`: procedimientos especializados cargados bajo demanda.
- `docs/`: documentacion humana y despliegue sin secretos.
- `docs/AGENT_WORKFLOW.md`: permisos, propiedad temporal, handoffs y evidencia para OpenCode.
- `docs/GUIA_IMPORTACION_HISTORICA.md`: guia viva de importacion de datos historicos (Excel -> SQL Server), mapa modelo<->hoja y vulnerabilidades abiertas.
