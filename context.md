# CONTEXT.md - Indice Operativo

Este archivo es el punto de entrada rapido. No contiene todo el conocimiento del proyecto; enruta al contexto correcto para evitar leer documentacion irrelevante.

## Lectura Obligatoria

Antes de modificar codigo:

1. Leer este archivo.
2. Leer `docs/COORDINACION_MULTI_CHAT.md` cuando la tarea afecte datos,
   modelos, QA, navegacion, arquitectura o planificacion de sprints.
3. Leer el `AGENTS.md` local si existe en la carpeta afectada.
4. Leer la skill correspondiente en `.agents/skills/`.
5. Inspeccionar de forma dirigida el modulo y sus pruebas o consumidores reales.

## Stack Fijo

| Capa | Tecnologia |
|---|---|
| Backend | ASP.NET Core 9.0 con Razor Pages |
| ORM | Entity Framework Core 9.0 con SQL Server |
| Frontend | Razor Pages, Bootstrap 4, NiceAdmin, jQuery, Select2 |
| Alertas | SweetAlert2 v7.19.3 |
| Excel | EPPlus 7.5.3 |
| PDF | QuestPDF 2025.12.4 |
| Auth | ASP.NET Core Identity |
| Cache/Sesion | IMemoryCache + Session 4h |

No agregar dependencias ni cambiar stack sin aprobacion explicita.

## Reglas Globales Criticas

- No hacer commit sin aprobacion explicita del usuario.
- No usar hard-delete en entidades del negocio. Evitar `_context.Remove()` y `.RemoveRange()` salvo entidades temporales claramente descartables y aprobadas.
- No modificar migraciones existentes. Crear migraciones nuevas.
- El proyecto usa NoTracking global. Toda entidad que se modifica debe cargarse con `.AsTracking()` o `FindAsync()`.
- Preservar `ManagementId` en rutas, redirects y acciones del wizard.
- SweetAlert2 instalado es v7. Usar `result && (result.isConfirmed === true || result.value === true)`.
- Para submits criticos usar `HTMLFormElement.prototype.submit.call(form)` despues de confirmar.
- TempData debe usar helpers seguros: `TempData.Success()`, `TempData.Error()`, `TempData.Warning()`.
- El layout global debe consumir las claves reales de helpers: `SuccessMessage`, `ErrorMessage`, `WarningMessage`, `InfoMessage` y mantener fallback legacy si existe.
- CRUD estandar: Create muestra exito posterior sin confirmacion previa; Edit requiere confirmacion SweetAlert antes de guardar; Delete/soft-delete requiere confirmacion SweetAlert antes de enviar.
- No usar `FullName` de `Person` en LINQ porque es `[NotMapped]`.
- `Semester` en `Management` acepta `0` para correctivo, `1` y `2` para preventivo.
- Si la tarea toca mas de 5 archivos, proponer plan antes de editar.
- Bajo consumo obligatorio: un proceso pesado a la vez, builds con `-m:1`,
  escrituras en lotes maximos de 100 y sin servidor web en segundo plano.
- Detener operaciones amplias con menos de 2 GB de RAM libre o mas de 85% de
  uso. El estres requiere 6 GB libres y autorizacion explicita; nunca ajustar
  automaticamente la memoria global de SQL Server.

## Orquestacion De Agentes

- El agente principal de Codex u OpenCode es el orquestador: lee contexto, divide solo trabajo independiente, evita ediciones solapadas, integra resultados y verifica evidencia.
- La coordinacion entre tareas/chats se rige por `docs/COORDINACION_MULTI_CHAT.md`.
- La propiedad temporal, los permisos y handoffs de OpenCode se rigen por `docs/AGENT_WORKFLOW.md`; no existe locking transaccional por archivo.
- Cada tarea declara rol, ID de trabajo, archivos, dependencias y tipo de escritura antes de actuar.
- `ui_ux`: vistas `.cshtml`, SweetAlert2 v7, textos, botones, accesibilidad visual y consistencia NiceAdmin/Bootstrap 4.
- `backend_data`: PageModels `.cshtml.cs`, `InputModel`, EF Core tracking, soft-delete, redirects y `TempData`.
- `reviewer`: revision final read-only basada en evidencia.
- Para tareas de mas de 5 archivos, el orquestador debe presentar un plan antes de editar.

## Mapa Rapido

| Si vas a tocar | Lee primero |
|---|---|
| Vistas Razor, JS, botones, modales | `Pages/AGENTS.md` + `.agents/skills/ui-premium/SKILL.md` |
| PageModels, handlers, InputModel | `Pages/AGENTS.md` + `.agents/skills/backend-methods/SKILL.md` |
| Entidades, DbContext, migraciones | `Models/AGENTS.md` + `.agents/skills/database/SKILL.md` |
| Excel, PDF, reportes | `Services/AGENTS.md` + `.agents/skills/reporting/SKILL.md` |
| Wizard preventivo/correctivo | este archivo + Pages y Services del paso afectado |
| Dashboard, cards, navegacion, notificaciones | `Pages/AGENTS.md` + archivos afectados |
| Borradores o correctivo | este archivo + PageModels y modelos relacionados |
| L-6, L-3, Kardex/L-48, L-12 | este archivo + busqueda dirigida del flujo real |
| Build, entorno, despliegue | `AGENTS_SETUP.md` |
| Crash Excel/EPPlus | `.agents/skills/reporting/SKILL.md` + evidencia del archivo/template |

## Estado Actual Critico

- Wizard preventivo: L-6 -> L-7 -> L-8 -> L-3 -> Kardex/L-48 -> L-12 -> Completados.
- Correctivo salta L-6 y empieza en L-7.
- L-3 del wizard esta centralizado en `Pages/Departures/MassCreate`.
- Completados es fase visual `Step = 7`, no un enum nuevo.
- Borradores existen en L-6 masivo, L-7, L-8, L-3 masivo, Kardex y L-12.
- Build verificado previamente con salida temporal y 0 errores; persisten warnings de nullability existentes.
- `QA-DATA-001` completo `DB_Laboratorios_Univalle_SCENARIOS_QA`: conserva
  107 catalogos y 556 unidades, agrega historicos sinteticos reproducibles,
  aprobo integridad, transacciones, concurrencia, rendimiento y restauracion
  de backup. No toca la base oficial ni MonsterASP. No levantar el servidor
  hasta indicacion del usuario; ver `docs/QA_DATA_001_RESULTADO.md`.

## Documentacion Separada

- Agentes Codex del proyecto: `.codex/agents/`.
- Agentes OpenCode del proyecto: `.opencode/agents/`.
- Skills del proyecto: `.agents/skills/`.
- Documentacion de usuario y despliegue: `docs/`.
- Coordinacion de datos, QA y sprints: `docs/COORDINACION_MULTI_CHAT.md`.
- Flujo multiagente OpenCode: `docs/AGENT_WORKFLOW.md`.
- Importacion de datos historicos (Excel -> SQL): `docs/GUIA_IMPORTACION_HISTORICA.md` (guia viva con vulnerabilidades abiertas).
