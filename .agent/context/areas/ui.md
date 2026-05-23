# Area UI Y Razor

Leer junto con `Pages/AGENTS.md` y `.agent/skills/ui_premium/SKILL.md`.

## Patron Visual

- Mantener NiceAdmin + Bootstrap 4.
- Index: zona Smart Index con filtros claros y tabla `table-hover v-middle`.
- Create/Edit: layout asimetrico, normalmente `col-lg-8` + `col-lg-4`.
- Details: ficha tecnica con contexto, estados, historial y acciones visibles.
- Delete: clean card centrada con resumen y advertencia.
- Botones principales: `btn-info btn-rounded shadow-sm` o variante semantica existente.

## SweetAlert2

Version instalada: v7.19.3.

```javascript
Swal.fire({...}).then(function (result) {
    if (result && (result.isConfirmed === true || result.value === true)) {
        HTMLFormElement.prototype.submit.call(form);
    }
});
```

No usar solo `result.isConfirmed`.

## Feedback CRUD

- Create normal: no mostrar confirmacion previa; guardar y mostrar exito posterior con el SweetAlert global.
- Edit: mostrar confirmacion previa con SweetAlert antes de guardar cambios.
- Delete/soft-delete: mostrar confirmacion previa con SweetAlert; reemplazar cualquier `confirm()` nativo.
- Confirmaciones deben tener fallback si `Swal` no esta disponible y usar `HTMLFormElement.prototype.submit.call(form)`.
- Exitos, errores, warnings e info se disparan desde `TempData.Success/Error/Warning/Info`; el layout debe leer `SuccessMessage/ErrorMessage/WarningMessage/InfoMessage`.
- Mantener fallback para claves legacy `Success/Error/Warning/Info` cuando exista codigo antiguo.

## Formularios Criticos

- Preferir botones reales `type="submit"` con `asp-page-handler` cuando hay multiples acciones.
- Detectar el submitter para distinguir borrador vs cierre/final.
- Preservar `formaction` del boton que disparo el submit.
- En flujos criticos, usar `formnovalidate` si el servidor reconstruye valores desde BD.
- `Pages/Maintenances/Create.cshtml` no debe usar `jqBootstrapValidation` porque ya causo bloqueos de submit.

## Tablas Dinamicas

- Hidden inputs dentro de `<td>`, nunca directos bajo `<tr>`.
- Templates ocultos deben tener inputs `disabled` hasta que la fila se inserte realmente.
- Checkboxes de filas: no agregar hidden `false` con el mismo name.

## Textos

- UI visible en espanol.
- Badges y estados deben usar nombres de display cuando existan.
- Evitar `N/A` si el usuario final verá la pantalla; usar `Pendiente`, `Sin dato` o equivalente.
