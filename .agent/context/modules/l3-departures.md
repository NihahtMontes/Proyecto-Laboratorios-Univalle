# Modulo L-3 Salidas

## Decision Vigente

En wizard, L-3 esta centralizado en `Pages/Departures/MassCreate` por laboratorio.

`Pages/Departures/Create` queda para uso standalone/fuera del wizard.

## Entrada Al Paso

Planes esperados:

- `CurrentPhase = WizardPhase.Exit`.
- `CurrentState = WizardEquipmentState.AwaitingDeparture`.
- L-8 debe tener `TechnicianId` asignado.

## Tecnico Y Tipo

- En wizard, tecnico se deriva de `plan.Maintenance.TechnicianId`.
- No confiar en tecnico posteado.
- Tipo de salida se infiere del tecnico:
  - Interno -> `InternalMaintenance`.
  - Externo -> `ExternalMaintenance`.

## MassCreate

- Fechas globales obligatorias.
- Minimo un equipo incluido para guardado final.
- Producto se deriva desde `ManagementPlan.EquipmentUnit.Equipment`, no del texto posteado.
- Equipos sin tecnico o laboratorio se muestran deshabilitados.
- Guardado final marca unidad `OnLoan` y avanza a Kardex.
- Borrador crea/reusa `Departure` contenedora sin marcar `OnLoan`.

## HTML Dinamico

- Inputs de filas solo para equipos elegibles.
- Indices contiguos en `Rows[]`.
- Hidden dentro de `<td>`.
- Checkbox `Include` sin hidden `false`.

## Reporte L-3

Genera desde cero. Si se toca, leer `areas/reporting.md`.
