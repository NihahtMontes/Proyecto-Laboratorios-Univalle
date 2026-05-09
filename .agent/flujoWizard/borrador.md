# Flujo de Borrador en Wizard

## Objetivo

Permitir que una fase del wizard guarde avances parciales sin avanzar a la siguiente fase ni exigir todos los datos finales. El caso inicial sera Kardex/Paso 5, porque el cierre de mantenimiento concentra muchos datos y puede requerir varias sesiones de trabajo.

## Caso Inicial: Kardex / Paso 5

Kardex representa el cierre formal del mantenimiento L-8. No siempre se completa en una sola carga, por lo que debe tener dos acciones diferenciadas:

- Guardar como borrador.
- Cerrar Kardex y pasar a desembolso.

## Accion 1: Guardar Como Borrador

### Comportamiento

- Guarda los datos ingresados hasta ese momento.
- No exige todos los campos obligatorios del cierre final.
- No crea `EquipmentStateHistory`.
- No marca la salida L-3 como devuelta.
- No marca el mantenimiento como `Completed`.
- No avanza el `ManagementPlan` a desembolso.
- Mantiene el plan en `WizardPhase.Kardex`.
- Mantiene el equipo visible en Paso 5 Kardex cuando el usuario vuelve al wizard.

### Datos que puede guardar parcialmente

- `Maintenance.TechnicianId`.
- `Maintenance.StartDate`.
- `Maintenance.EndDate`.
- `Maintenance.Description`.
- `Maintenance.SuggestedNextMaintenanceDate`.
- `Maintenance.SatisfactionLevel`.
- `Maintenance.Recommendations`.
- `Maintenance.Observations`.
- `Maintenance.CostDetails`.
- `Maintenance.ActualCost` calculado desde `CostDetails` disponibles.
- Checks tecnicos L-48:
  - `Step1_Cleaning`.
  - `Step2_Calibration`.
  - `Step3_Testing`.
  - `Step4_FinalReview`.
  - `CompletionPercentage`.
- `Departure.ActualReturnDate` si el usuario ya la lleno, pero sin cambiar `Departure.Status` a `Returned`.

### Estado esperado despues de guardar borrador

```csharp
maintenance.Status = MaintenanceStatus.InProgress;
plan.CurrentPhase = WizardPhase.Kardex;
plan.CurrentState = WizardEquipmentState.AwaitingKardex;
```

Si ya existia otro estado de mantenimiento distinto de `Completed`, puede conservarse o normalizarse a `InProgress`. No debe quedar `Completed` por guardar borrador.

### Validacion de borrador

Solo debe exigir lo minimo estructural:

- Existe `ManagementPlan`.
- Existe `Maintenance` vinculado.
- Existe `EquipmentUnit` vinculado.

No debe exigir:

- Tecnico.
- Fechas completas.
- Costos.
- Satisfaccion.
- Recomendaciones.
- Observaciones.
- Devolucion L-3.

### Respuesta al usuario

Mostrar confirmacion con SweetAlert2 antes de guardar:

```text
¿Guardar avance como borrador?
Podras continuar el cierre de Kardex mas tarde sin avanzar a desembolso.
```

Despues de guardar:

```text
Borrador de Kardex guardado. Puede continuar despues.
```

## Accion 2: Cerrar Kardex y Pasar a Desembolso

### Comportamiento

- Exige todos los datos finales.
- Marca `Maintenance.Status = Completed`.
- Marca `Departure.Status = Returned` si existe L-3 vinculada.
- Guarda `Departure.ActualReturnDate`.
- Crea `EquipmentStateHistory`.
- Marca `EquipmentUnit.CurrentStatus = Operational`.
- Avanza el plan a `WizardPhase.Disbursement`.
- Cambia `CurrentState` a `AwaitingDisbursement`.

### Validaciones obligatorias de cierre

No se puede cerrar Kardex si falta:

- `Maintenance.TechnicianId`.
- `Maintenance.StartDate`.
- `Maintenance.EndDate`.
- `Departure.ActualReturnDate` cuando hay salida L-3 vinculada.
- `Maintenance.Description`.
- Al menos un `CostDetail` real.
- `Maintenance.ActualCost > 0`.
- `Maintenance.SuggestedNextMaintenanceDate`.
- `Maintenance.SatisfactionLevel`.
- `Maintenance.Recommendations`.
- `Maintenance.Observations`.

Validaciones cronologicas:

- `EndDate >= StartDate`.
- `Departure.ActualReturnDate >= Departure.DepartureDate`.
- `SuggestedNextMaintenanceDate > EndDate`.

### Estado esperado despues de cerrar

```csharp
maintenance.Status = MaintenanceStatus.Completed;
departure.Status = LoanStatus.Returned;
equipmentUnit.CurrentStatus = EquipmentStatus.Operational;
plan.CurrentPhase = WizardPhase.Disbursement;
plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
```

## Implementacion Recomendada

### PageModel

Separar handlers en `Pages/Kardex/Create.cshtml.cs`:

```csharp
public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
public async Task<IActionResult> OnPostCloseAsync(bool isWizard = false)
```

Crear metodos compartidos:

```csharp
ResolvePlanAsync(...)
PopulateInputFromPlan(...)
RebuildServerTruth(...)
SyncPartialMaintenance(...)
SyncCostDetails(...)
ValidateClosure(...)
CloseKardexAsync(...)
```

### Vista

Usar dos botones dentro del mismo formulario:

```html
<button type="submit" asp-page-handler="Draft" formnovalidate>
    Guardar como borrador
</button>

<button type="submit" asp-page-handler="Close" formnovalidate>
    Cerrar Kardex y Pasar a Desembolso
</button>
```

### SweetAlert2 seguro

No usar `onclick` como unico mecanismo de envio.

Patron requerido:

- El boton debe seguir siendo `type="submit"`.
- JavaScript escucha el evento `submit`.
- Se detecta el boton que disparo el submit.
- Se muestra SweetAlert2 correspondiente.
- Si confirma, se marca una bandera `data-confirmed="true"` y se ejecuta `form.submit()` nativo.

Ejemplo conceptual:

```javascript
$('#kardexForm').on('submit', function (event) {
    const form = this;
    const action = document.activeElement?.getAttribute('formaction') || '';

    if ($(form).data('confirmed') === true) return true;

    event.preventDefault();

    const isDraft = action.includes('handler=Draft');
    Swal.fire({
        title: isDraft ? '¿Guardar borrador?' : '¿Cerrar Kardex?',
        text: isDraft
            ? 'Se guardara el avance sin pasar a desembolso.'
            : 'Se completara el mantenimiento y avanzara a desembolso.',
        icon: 'question',
        showCancelButton: true
    }).then((result) => {
        if (result.isConfirmed) {
            $(form).data('confirmed', true);
            form.submit();
        }
    });
});
```

Nota: si `document.activeElement` no conserva el boton correcto, usar un hidden `ActionMode` que cada boton setee antes del submit.

## Persistencia Sin Nuevas Columnas

Para la primera version, no se requiere nueva columna `IsDraft`.

El borrador se identifica porque:

- `plan.CurrentPhase == WizardPhase.Kardex`.
- `plan.CurrentState == WizardEquipmentState.AwaitingKardex`.
- `maintenance.Status != MaintenanceStatus.Completed`.
- Existen datos parciales en `Maintenance` o `CostDetails`.

Si luego se necesita mostrar badge explicito en wizard, se puede agregar una propiedad calculada o una columna futura, pero no es necesario para validar el flujo inicial.

## Visual En Wizard

Primera version:

- El equipo sigue apareciendo en Paso 5 Kardex.
- Al entrar de nuevo a Kardex, se precargan los datos guardados.

Mejora futura:

- Mostrar badge `Borrador Kardex` en la tarjeta del wizard.
- Mostrar porcentaje de cierre.
- Mostrar campos faltantes.

## Extension Futura A Otros Modulos

El patron puede replicarse en fases con formularios largos:

- L-8 mantenimiento.
- Kardex.
- L-12 desembolso/adquisicion.

Regla general:

- `Draft` guarda datos parciales y mantiene fase.
- `Close` o `Submit` valida todo y avanza fase.
- Cada fase debe reconstruir verdad desde `ManagementPlanId` antes de validar.
- Ningun submit critico debe depender solamente de `onclick + $('#form').submit()`.

## Prioridad

Implementar primero en Kardex. Una vez probado y estable, evaluar extender a L-12 y otros modulos.
