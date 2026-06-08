# Reglas Globales

Estas reglas aplican a cualquier cambio.

## Integridad De Datos

- No hard-delete para entidades de negocio. Usar soft-delete, estados o desvinculacion FK nullable.
- No usar `_context.Remove()` ni `.RemoveRange()` sin verificar si la entidad tiene historial.
- No modificar migraciones existentes. Crear una migracion nueva.
- No agregar query filters nuevos en entidades dependientes sin revisar impacto. Los filtros globales pueden romper Includes y reportes.

## Entity Framework

- NoTracking global esta activo en `Program.cs`.
- Para modificar una entidad, cargarla con `.AsTracking()` o usar `FindAsync()`.
- Cargar relaciones necesarias con `.Include()`/`.ThenInclude()` en Details/Edit/PageModels que renderizan relaciones.
- `Person.FullName` es `[NotMapped]`; no usarlo en LINQ traducible a SQL.

## Wizard

- `ManagementId` debe preservarse en query string, hidden inputs y redirects.
- La verdad del wizard se reconstruye desde `ManagementPlanId` y BD, no desde selects disabled.
- En preventivo, selects pueden estar bloqueados; en correctivo deben permanecer editables cuando no hay plan previo.
- `Completados` es `Step = 7` visual, no nuevo valor de `WizardPhase`.

## Frontend

- SweetAlert2 instalado es v7.19.3. Confirmar con `result && (result.isConfirmed === true || result.value === true)`.
- Para submits criticos, confirmar y luego ejecutar `HTMLFormElement.prototype.submit.call(form)`.
- Si SweetAlert2 no carga, debe existir fallback que no deje la accion muerta.
- CRUD estandar: Create muestra exito posterior sin confirmacion previa; Edit confirma antes de guardar; Delete/soft-delete confirma antes de enviar.
- Los hidden inputs dentro de tablas deben estar dentro de un `<td>` valido.
- No usar hidden `false` con checkbox `true` para el mismo name en filas dinamicas; checked envia `true`, unchecked no envia y el modelo usa default.

## Mensajes

- Usar `TempData.Success()`, `TempData.Error()`, `TempData.Warning()`.
- El layout debe leer `SuccessMessage`, `ErrorMessage`, `WarningMessage`, `InfoMessage` de `TempDataExtensions`.
- No usar `TempData["Success"] = ...` para mensajes nuevos.
- Al inyectar texto C# en JavaScript, usar helper seguro o `Html.Raw` solo si el contenido ya esta controlado.

## Excel

- No usar `Worksheets.Add(sourceSheet.Name, sourceSheet)`.
- No usar `Merge = false` en rangos ya merged.
- No usar `System.Drawing.Color` para estilos de EPPlus.
- Guardar paquetes con helper seguro basado en `package.SaveAs(stream)`.

## Colaboracion

- No hacer commit sin aprobacion explicita.
- No revertir cambios ajenos.
- Si hay cambios inesperados que no bloquean la tarea, ignorarlos.
- Si el cambio toca mas de 5 archivos, presentar plan antes de editar.
