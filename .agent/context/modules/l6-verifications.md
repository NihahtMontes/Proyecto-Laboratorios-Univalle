# Modulo L-6 Verificaciones

## Arquitectura Actual

- Verificacion masiva por laboratorio en `Pages/Verifications/MassCreate`.
- Index agrupa sesiones por laboratorio/fecha.
- Details soporta modo sesion (`labId + date`) y modo individual fallback.
- Checklist antiguo esta obsoleto.

## MassCreate

- Carga equipos sin verificacion final o con borrador segun fase.
- El AJAX debe poder filtrar por `ManagementId` explicito.
- Hidden inputs dentro de `<td>`.
- Checkbox de inclusion sin hidden `false`.
- Observaciones y condicion se pueden rehidratar desde borrador.

## Estados

- Sin fallas: plan queda en `WizardPhase.Verification` con estado sano/verificado segun flujo actual.
- Con fallas: avanza a `WizardPhase.TechnicalRequest`.
- Borrador: `VerificationStatus.Draft`, plan permanece en `WizardPhase.Verification`, `IsDraft = true`.

## Rehacer L-6

Si una verificacion observada se corrige a Good/Excellent:

- Confirmar con SweetAlert2.
- Marcar verificacion como completada.
- Soft-delete de fallas activas.
- Cancelar solicitud L-7 pendiente si corresponde.
- Mantener plan en fase Verification para que no avance indebidamente.

## Reporte

L-6 se genera desde cero, sin template. Si se toca Excel leer `areas/reporting.md`.
