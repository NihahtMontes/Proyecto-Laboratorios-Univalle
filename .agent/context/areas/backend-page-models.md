# Area Backend PageModels

Leer junto con `.agent/skills/backend_methods/SKILL.md`.

## InputModel

- No bindear entidades desnudas del dominio en formularios.
- Usar `InputModel` anidado por PageModel.
- El `InputModel` debe contener solo campos editables por esa vista.
- Limpiar `ModelState` de campos que el servidor reconstruye desde `ManagementPlan`.
- En Edit de entidades con navegaciones requeridas, no bindear la entidad completa; cargar la entidad con `.AsTracking()` y aplicar campos del `InputModel`.
- Las validaciones del `InputModel` deben reflejar solo lo que el usuario puede editar en pantalla.

## Handlers

- Handlers de escritura deben manejar errores con try/catch cuando hay EF complejo, archivos o reportes.
- Usar mensajes via `TempData.Error()` y volver a cargar listas antes de `Page()`.
- Usar `TempData.Success()`, `TempData.Error()`, `TempData.Warning()` o `TempData.Info()`; no escribir nuevas claves manuales como `TempData["Success"]`.
- Si se modifica `ManagementPlan`, cargar con `.AsTracking()`.
- En redirects del wizard conservar `ShowWizard=true`, `Step`, `ManagementId` y `SelectedLabId` cuando aplique.
- Create normal muestra exito posterior; Edit y Delete coordinan confirmacion desde la vista y resultado desde `TempData`.

## Resolver Contexto De Gestion

Orden recomendado:

1. `ManagementPlanId` -> cargar plan -> `plan.ManagementId`.
2. `ManagementId` explicito.
3. Fallback a `ManagementContextService.GetCurrentManagementAsync()` solo si la pantalla lo permite.

## Rehidratacion

- Cuando un POST falla, repoblar dropdowns y datos laterales.
- Si hay borrador, el `OnGet` debe cargar datos existentes y pintar el formulario completo.
- No confiar en IDs posteados por selects disabled. Derivar desde el plan.

## Transacciones

Usar transaccion cuando una accion:

- crea entidad principal,
- vincula esa entidad al `ManagementPlan`,
- avanza fase/estado,
- y redirige al siguiente paso.

La entidad creada debe tener ID real antes de sincronizar hijos como costos/tareas.

## Notificaciones

- Crear notificaciones solo en guardado final, no en borrador.
- No generar notificaciones si faltan datos necesarios como tecnico o fecha real.
