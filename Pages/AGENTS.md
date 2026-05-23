# AGENTS.md - Pages

**Ambito**: Razor Pages, vistas `.cshtml`, PageModels `.cshtml.cs`, JavaScript embebido y wizard visual.

## Lectura Obligatoria

- `../context.md`.
- `../.agent/context/00-router.md`.
- `../.agent/context/areas/ui.md` si tocas vistas.
- `../.agent/context/areas/backend-page-models.md` si tocas PageModels.
- Modulo especifico en `../.agent/context/modules/` si aplica.
- Skills: `ui_premium` y/o `backend_methods`.

## Reglas Criticas

- Preservar `ManagementId` en wizard.
- Reconstruir verdad desde `ManagementPlanId`, no desde selects disabled.
- SweetAlert2 es v7: usar `result.value` ademas de `isConfirmed`.
- Para submits criticos usar `HTMLFormElement.prototype.submit.call(form)`.
- No reintroducir `jqBootstrapValidation` en `Maintenances/Create`.
- Usar `InputModel`, no bindear entidades completas.
- Mensajes con `TempData.Success/Error/Warning`.
- Create: no pedir confirmacion previa salvo flujo critico; mostrar exito posterior via SweetAlert global.
- Edit: confirmar con SweetAlert antes de guardar cambios.
- Delete/soft-delete: confirmar con SweetAlert antes de enviar; no usar `confirm()` nativo.
- El layout debe leer `SuccessMessage/ErrorMessage/WarningMessage/InfoMessage` generados por `TempDataExtensions`.

## UI

- Mantener NiceAdmin/Bootstrap 4.
- Index con filtros/Smart Index cuando corresponda.
- Create/Edit con layout asimetrico y panel de contexto si aporta valor.
- Details con estado, historial y acciones claras.
- Textos visibles en espanol.
