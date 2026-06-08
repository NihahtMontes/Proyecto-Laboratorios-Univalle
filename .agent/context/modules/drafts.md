# Modulo Borradores

## Objetivo

Permitir guardar avance parcial sin avanzar fase ni exigir validacion final.

## Modelo

`ManagementPlan` contiene:

- `IsDraft`.
- `DraftPhase`.
- `DraftSavedAt`.
- `DraftSummary`.

Campos incluidos en la linea base vigente de SQL Server (`Data/SqlServerMigrations/`). La migracion PostgreSQL previa queda como historica/excluida.

## Semantica

- Guardar borrador: persiste datos parciales, mantiene fase, no notifica, no completa.
- Guardar y continuar/cerrar: valida completo, avanza fase, limpia campos de borrador.
- El `OnGet` debe rehidratar lo guardado.
- El borrador no debe cambiar estado fisico definitivo del equipo.

## Modulos Con Borrador

| Modulo | Handler | Comportamiento |
|---|---|---|
| L-6 masivo | `OnPostDraftAsync` | guarda verificaciones `Draft`, no genera L-7 |
| L-7 | `OnPostDraftAsync` | crea/reusa `Request`, no avanza a L-8 |
| L-8 | `OnPostDraftAsync` | crea/reusa `Maintenance`, costos y tareas, no avanza a L-3 |
| L-3 masivo | `OnPostDraftAsync` | crea/reusa `Departure` contenedora, no marca `OnLoan` |
| Kardex | `OnPostDraftAsync` | guarda cierre parcial, no devuelve L-3 ni crea historico final |
| L-12 | `OnPostDraftAsync` | crea/reusa solicitud parcial, no completa plan |

## Patron UI

- Dos botones reales en el mismo form.
- `Guardar Borrador` con handler `Draft`.
- Accion final con handler final o `OnPostAsync`.
- Confirmacion SweetAlert2 v7-compatible.
- Submit nativo preservando `formaction`.

## Patron Backend

1. Resolver `ManagementPlan` con `.AsTracking()`.
2. Reconstruir datos desde plan y relaciones.
3. Sincronizar entidad principal y colecciones hijas.
4. Marcar `IsDraft = true` solo para borrador.
5. Mantener `CurrentPhase`/`CurrentState` de la fase actual.
6. En cierre final, limpiar borrador y avanzar fase.

## Cuidado

No reutilizar estados finales como `Completed`, `Returned` u `OnLoan` durante borrador si esos estados tienen efecto operativo real.
