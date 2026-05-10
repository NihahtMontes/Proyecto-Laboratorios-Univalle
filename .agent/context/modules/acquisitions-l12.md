# Modulo L-12 Adquisiciones

## Rol

L-12 registra la solicitud de adquisicion/desembolso despues de Kardex.

## Entrada

Plan esperado:

- `CurrentPhase = WizardPhase.Disbursement`.
- `CurrentState = AwaitingDisbursement` para pendientes.
- Mantenimiento vinculado y costos disponibles desde L-8/Kardex.

## Reglas Backend

- Reconstruir unidad, laboratorio, facultad y mantenimiento desde `ManagementPlanId`.
- Importar o actualizar costos desde mantenimiento cuando corresponde.
- Crear o reutilizar `AcquisitionRequestId`.
- En guardado final, completar plan con:
  - `WizardPhase.Disbursement`.
  - `WizardEquipmentState.Completed`.
  - `ManagementPlanStatus.Completed`.
- Completados se muestra en `Step = 7`.

## Borrador

- `OnPostDraftAsync` crea/reusa solicitud parcial.
- Mantiene fase Disbursement y estado AwaitingDisbursement.
- No genera notificacion final.
- No marca plan como Completed.

## UI

- Dos acciones: `Guardar Borrador` y `Crear Solicitud L-12`.
- Confirmacion SweetAlert2 v7-compatible.
- Submit nativo preservando handler.

## Reporte

Si se toca Excel de adquisicion, leer `areas/reporting.md`. Las plantillas tienen coordenadas rigidas y datos dummy que deben limpiarse.
