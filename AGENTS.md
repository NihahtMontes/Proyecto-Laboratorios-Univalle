# AGENTS.md - Orquestador Principal

**Proyecto**: Laboratorios Univalle  
**Rol**: enrutar contexto tecnico y evitar cambios amplios innecesarios.

## Principios

- Alta precision tecnica: cambios pequenos, correctos y verificables.
- UI premium: NiceAdmin/Bootstrap 4 con jerarquia visual, no formularios planos.
- Integridad historica: evitar hard-delete; preferir estados, soft-delete o desvinculacion segura.
- Contexto modular: no leer todo el historico si una tarea solo toca un modulo.

## Flujo Obligatorio

Antes de tocar archivos:

1. Leer `context.md`.
2. Leer `docs/COORDINACION_MULTI_CHAT.md` si el trabajo afecta datos,
   modelos, QA, navegacion o planificacion de sprints.
3. Leer el `AGENTS.md` local mas cercano si existe.
4. Leer la skill correspondiente en `.agents/skills/`.
5. Inspeccionar solo los archivos y documentacion del modulo afectado.

Si la tarea toca mas de 5 archivos, proponer plan antes de editar.

No hacer commit sin aprobacion explicita del usuario.

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

## Politica De Bajo Consumo De Recursos

- Ejecutar un solo build, prueba, importador o proceso pesado a la vez; para
  .NET usar `dotnet build -m:1` y evitar servidores en segundo plano.
- Antes de una operacion de datos o una prueba amplia, comprobar memoria:
  detenerse si quedan menos de 2 GB libres o si el uso supera 85%.
- Procesar escrituras EF/SQL en lotes de hasta 100 filas, limpiar el
  `ChangeTracker` entre lotes y limitar la conexion a un maximo de 10
  conexiones cuando la herramienta controle el pool.
- Las pruebas de estres son opt-in, exigen al menos 6 GB libres y autorizacion
  explicita. No cambiar automaticamente la memoria global de SQL Server.
- No iniciar la aplicacion web, browser ni servicios persistentes salvo pedido
  expreso. Todo proceso temporal debe cerrarse y verificarse al finalizar.

## Orquestacion Multiagente

Codex actua como orquestador principal: define alcance, reparte tareas independientes, integra resultados, resuelve conflictos y verifica build. No delegar por rutina ni permitir cambios solapados sobre los mismos archivos.

La coordinacion persistente entre tareas de Codex se define en
`docs/COORDINACION_MULTI_CHAT.md`. Antes de actuar, cada tarea declara rol, ID,
archivos/superficies, dependencias y si realizara lectura, escritura local o
escritura externa. El coordinador de datos mantiene el estado canonico; QA y
Sprint entregan handoffs y no modifican simultaneamente ese documento.

- `ui_ux`: propietario de `.cshtml`, SweetAlert2, textos visibles, botones, confirmaciones y consistencia NiceAdmin/Bootstrap 4.
- `backend_data`: propietario de `.cshtml.cs`, `InputModel`, EF tracking, soft-delete, redirects, `TempData` y validaciones servidor.
- `reviewer`: revision independiente y read-only de correccion, seguridad, regresiones y pruebas.
- El agente principal conserva decisiones, integracion, reglas de datos y verificacion final.

## Documentacion

- `.codex/agents/`: agentes especializados del proyecto.
- `.agents/skills/`: procedimientos especializados cargados bajo demanda.
- `docs/`: documentacion humana y despliegue sin secretos.
- `docs/GUIA_IMPORTACION_HISTORICA.md`: guia viva de importacion de datos historicos (Excel -> SQL Server), mapa modelo<->hoja y vulnerabilidades abiertas.
