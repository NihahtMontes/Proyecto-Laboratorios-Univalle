# Modulo Kardex Y L-48

## Rol

Kardex en wizard es el cierre formal del mantenimiento L-8 y la transicion a L-12.

## Cierre Final

Debe validar:

- Tecnico responsable.
- Fechas de inicio y fin.
- Devolucion L-3 si existe salida vinculada.
- Descripcion del trabajo realizado.
- Costos reales con total mayor a 0.
- Tareas L-48 activas y avance 100%.
- Proximo mantenimiento.
- Satisfaccion, recomendaciones y observaciones.

Al cerrar:

- `Maintenance.Status = Completed`.
- `Departure.Status = Returned` si aplica.
- Crear `EquipmentStateHistory`.
- `EquipmentUnit.CurrentStatus = Operational`.
- Plan avanza a `WizardPhase.Disbursement` + `AwaitingDisbursement`.
- Limpiar borrador.

## Borrador

- Guarda datos parciales.
- No exige validaciones finales.
- No devuelve L-3.
- No crea historico final.
- No completa mantenimiento.
- Mantiene plan en Kardex.

## Tareas L-48

- `MaintenanceTask.IsDeleted` preserva historial.
- Listas y calculos deben filtrar `!IsDeleted`.
- El template oculto no debe contar en el porcentaje.
- Tareas retiradas se soft-deletean, no se eliminan fisicamente.

## Costos

- Total real se calcula en backend con `Quantity * UnitPrice`.
- Costos retirados se desvinculan si la FK lo permite.

## Historico

`Pages/EquipmentUnits/Kardex` muestra tareas, costos y estados. Si viene desde wizard debe conservar `isWizard`, `ManagementId`, `SelectedLabId` y `returnStep`.
