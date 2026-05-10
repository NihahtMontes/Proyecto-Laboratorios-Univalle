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
2. Leer `.agent/context/00-router.md`.
3. Leer `.agent/context/01-global-rules.md`.
4. Leer el contexto de area o modulo segun la tarea.
5. Leer el `AGENTS.md` local si existe.
6. Leer la skill correspondiente en `.agent/skills/`.

Si la tarea toca mas de 5 archivos, proponer plan antes de editar.

No hacer commit sin aprobacion explicita del usuario.

## Matriz De Enrutamiento

| Capa/Tarea | Contexto | Manifesto | Skill |
|---|---|---|---|
| UI Razor, `.cshtml`, JS, SweetAlert2 | `.agent/context/areas/ui.md` | `Pages/AGENTS.md` | `.agent/skills/ui_premium/SKILL.md` |
| PageModels, handlers, InputModel | `.agent/context/areas/backend-page-models.md` | `Pages/AGENTS.md` | `.agent/skills/backend_methods/SKILL.md` |
| Modelos, EF, migraciones | `.agent/context/areas/database-ef.md` | `Models/AGENTS.md` | `.agent/skills/database/SKILL.md` |
| Reportes Excel/PDF | `.agent/context/areas/reporting.md` | `Services/AGENTS.md` | `.agent/skills/reporting/SKILL.md` |
| Infraestructura/build/despliegue | `.agent/context/areas/infrastructure.md` | `AGENTS_SETUP.md` si existe | segun archivos tocados |
| Wizard | `.agent/context/modules/wizard.md` | `Pages/AGENTS.md` | backend/ui segun cambio |
| Borradores | `.agent/context/modules/drafts.md` | `Pages/AGENTS.md` | backend/database |
| Correctivo | `.agent/context/modules/corrective.md` | `Pages/AGENTS.md` | backend |

## Modulos Especificos

- L-6: `.agent/context/modules/l6-verifications.md`.
- L-3: `.agent/context/modules/l3-departures.md`.
- Kardex/L-48: `.agent/context/modules/kardex-l48.md`.
- L-12: `.agent/context/modules/acquisitions-l12.md`.

## Reglas Criticas Cortas

- `ManagementId` se preserva en todo el wizard.
- NoTracking global exige `.AsTracking()` para modificar.
- SweetAlert2 es v7: usar `result.value` ademas de `isConfirmed`.
- `Completados` es `Step = 7` visual.
- Migraciones existentes son intocables.
- No revertir cambios ajenos.

## Documentacion

- `.agent/`: contexto operativo para IA/desarrollo.
- `docs/`: documentacion humana y despliegue sin secretos.
- `.agent/archive/`: material historico u obsoleto.
