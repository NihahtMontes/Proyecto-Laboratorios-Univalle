# CONTEXT.md - Indice Operativo

Este archivo es el punto de entrada rapido. No contiene todo el conocimiento del proyecto; enruta al contexto correcto para evitar leer documentacion irrelevante.

## Lectura Obligatoria

Antes de modificar codigo:

1. Leer este archivo.
2. Leer `.agent/context/00-router.md`.
3. Leer el contexto especifico del area o modulo que vas a tocar.
4. Leer el `AGENTS.md` local si existe en esa carpeta.
5. Leer la skill correspondiente en `.agent/skills/`.

El contexto historico completo anterior quedo archivado en `.agent/archive/context/context-legacy-2026-05-10.md`. Usarlo solo para auditoria o rastrear decisiones antiguas.

## Stack Fijo

| Capa | Tecnologia |
|---|---|
| Backend | ASP.NET Core 9.0 con Razor Pages |
| ORM | Entity Framework Core 9.0 con SQL Server |
| Frontend | Razor Pages, Bootstrap 4, NiceAdmin, jQuery, Select2 |
| Alertas | SweetAlert2 v7.19.3 |
| Excel | EPPlus 7.5.2 |
| PDF | QuestPDF 2025.12.3 |
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

## Orquestacion De Agentes

- Codex es el orquestador: lee contexto, divide trabajo, evita ediciones solapadas, integra resultados y verifica build.
- Agente UI/UX tipo Kimi K2.6: revisar vistas `.cshtml`, SweetAlert2 v7, textos, botones, accesibilidad visual y consistencia NiceAdmin/Bootstrap 4.
- Agente Backend/Data tipo DeepSeek V4 Pro: revisar PageModels `.cshtml.cs`, `InputModel`, EF Core tracking, soft-delete, redirects y `TempData`.
- Para tareas de mas de 5 archivos, Codex debe entregar un plan y prompts de subagente antes de ejecutar.

## Mapa Rapido

| Si vas a tocar | Lee primero |
|---|---|
| Vistas Razor, JS, botones, modales | `.agent/context/areas/ui.md` + `.agent/skills/ui_premium/SKILL.md` + `Pages/AGENTS.md` |
| PageModels, handlers, InputModel | `.agent/context/areas/backend-page-models.md` + `.agent/skills/backend_methods/SKILL.md` |
| Entidades, DbContext, migraciones | `.agent/context/areas/database-ef.md` + `.agent/skills/database/SKILL.md` + `Models/AGENTS.md` |
| Excel, PDF, reportes | `.agent/context/areas/reporting.md` + `.agent/skills/reporting/SKILL.md` + `Services/AGENTS.md` |
| Wizard preventivo/correctivo | `.agent/context/modules/wizard.md` + modulo especifico |
| Dashboard, cards, navegacion, notificaciones | `.agent/context/modules/dashboard-navigation.md` |
| Borradores | `.agent/context/modules/drafts.md` |
| Correctivo | `.agent/context/modules/corrective.md` |
| L-6 Verificaciones | `.agent/context/modules/l6-verifications.md` |
| L-3 Salidas | `.agent/context/modules/l3-departures.md` |
| Kardex/L-48 | `.agent/context/modules/kardex-l48.md` |
| L-12 Adquisiciones | `.agent/context/modules/acquisitions-l12.md` |
| Build, entorno, despliegue | `.agent/context/areas/infrastructure.md` |
| Crash Excel/EPPlus | `.agent/context/troubleshooting/excel-crashes.md` |
| Crash Visual Studio | `.agent/context/troubleshooting/visual-studio-crash.md` |

## Estado Actual Critico

- Wizard preventivo: L-6 -> L-7 -> L-8 -> L-3 -> Kardex/L-48 -> L-12 -> Completados.
- Correctivo salta L-6 y empieza en L-7.
- L-3 del wizard esta centralizado en `Pages/Departures/MassCreate`.
- Completados es fase visual `Step = 7`, no un enum nuevo.
- Borradores existen en L-6 masivo, L-7, L-8, L-3 masivo, Kardex y L-12.
- Build verificado previamente con salida temporal y 0 errores; persisten warnings de nullability existentes.

## Documentacion Separada

- Contexto operativo de IA: `.agent/`.
- Documentacion de usuario y despliegue: `docs/`.
- Material viejo o contradictorio: `.agent/archive/`.
