---
name: ui_premium
description: UI Razor/NiceAdmin, SweetAlert2, formularios, tablas dinamicas y validaciones frontend.
trigger: Modificacion de `.cshtml`, JavaScript embebido, botones, modales, wizard visual o feedback al usuario.
scope: Pages/UI
context: .agent/context/areas/ui.md
---
# Skill UI Premium

## Leer Antes

- `.agent/context/areas/ui.md`.
- `Pages/AGENTS.md`.
- Modulo especifico si la vista pertenece al wizard.

## Reglas Visuales

- Mantener NiceAdmin/Bootstrap 4.
- Index debe tener busqueda/filtros claros, no solo tabla plana.
- Create/Edit debe usar layout asimetrico y panel lateral cuando aporte contexto.
- Details debe mostrar estado, historial y acciones con jerarquia.
- UI visible en espanol.

## Botones

- Accion principal: `btn-info btn-rounded shadow-sm` o color semantico equivalente existente.
- Edicion: `btn-warning`.
- Riesgo: `btn-danger`.
- Volver/cancelar: `btn-outline-secondary`.

## SweetAlert2 Critico

La version instalada es v7.19.3. No usar solo `result.isConfirmed`.

```javascript
Swal.fire({...}).then(function (result) {
    if (result && (result.isConfirmed === true || result.value === true)) {
        HTMLFormElement.prototype.submit.call(form);
    }
});
```

Agregar fallback si `Swal` no esta disponible.

## Patron CRUD

- Create: no pedir confirmacion previa en altas comunes; confiar en validacion cliente/servidor y mostrar exito posterior via TempData/SweetAlert global.
- Edit: interceptar submit y pedir confirmacion SweetAlert antes de guardar.
- Delete/soft-delete: siempre confirmar con SweetAlert; no usar `window.confirm`.
- Confirmar con `result && (result.isConfirmed === true || result.value === true)` y luego `HTMLFormElement.prototype.submit.call(form)`.
- Si `Swal` no existe, permitir fallback nativo que no bloquee la accion.
- El mensaje posterior debe venir de `TempData.Success/Error/Warning/Info`, no de alertas inline duplicadas.

## Formularios

- Botones con handlers deben ser submits reales.
- Preservar `formaction` del boton que disparo el submit.
- Usar submit nativo para evitar bloqueos de validadores JS en wizard.
- No reintroducir `jqBootstrapValidation` en `Pages/Maintenances/Create.cshtml`.

## Tablas Dinamicas

- Hidden inputs siempre dentro de `<td>`.
- Templates ocultos con inputs `disabled` hasta insertar fila real.
- Checkbox de fila sin hidden `false` con el mismo name.

## Seguridad JS

- Al inyectar TempData o strings C# en JS, usar helpers seguros del proyecto.
- No concatenar strings no confiables en JavaScript inline.
