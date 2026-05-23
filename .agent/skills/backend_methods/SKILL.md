---
name: backend_methods
description: PageModels Razor, handlers OnGet/OnPost, InputModel, reconstruccion de contexto y flujo wizard.
trigger: Modificacion de `.cshtml.cs`, handlers, servicios usados por paginas o logica de formularios.
scope: Backend/PageModels
context: .agent/context/areas/backend-page-models.md
---
# Skill Backend PageModels

## Leer Antes

- `.agent/context/areas/backend-page-models.md`.
- `.agent/context/01-global-rules.md`.
- Modulo especifico si toca wizard.

## InputModel

- No bindear entidades de dominio directamente.
- Usar `InputModel` anidado con solo campos editables.
- Validaciones deben corresponder a la vista, no a toda la entidad.
- En Edit, cargar la entidad real con `.AsTracking()` y copiar campos desde `InputModel`; no confiar en una entidad completa posteada.
- Si el formulario no postea navegaciones, auditoria o campos requeridos no editables, esos campos deben reconstruirse desde BD.

## Wizard

- Resolver `ManagementPlan` con `.AsTracking()` cuando se actualiza.
- Reconstruir `EquipmentUnitId`, `LaboratoryId`, `RequestId`, `MaintenanceId` desde BD.
- No confiar en selects disabled ni texto posteado.
- Preservar `ManagementId` en redirects.

## Borradores

- `Draft` guarda parcial y mantiene fase.
- Accion final valida completo, avanza fase y limpia borrador.
- No generar notificaciones ni estados finales durante borrador.

## Errores

- En errores de POST: cargar listas y retornar `Page()` con `TempData.Error()`.
- No redirigir como exito si no se creo/actualizo ninguna fila.
- Mensajes deben diagnosticar la causa cuando el binding falla.
- Exitos CRUD deben usar `TempData.Success()` para que el layout global emita SweetAlert/Toast.
- Deletes deben ser soft-delete cuando la entidad tenga valor historico; la confirmacion vive en la vista, la integridad en el handler.

## Transacciones

Usar transaccion cuando se crea entidad, se vincula al plan y se avanza fase en una sola accion.
