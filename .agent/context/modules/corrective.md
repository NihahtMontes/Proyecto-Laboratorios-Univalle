# Modulo Correctivo

## Flujo

Correctivo salta L-6 y empieza en L-7.

```text
Reportar Falla Critica -> L-7 -> L-8 -> L-3 -> Kardex/L-48 -> L-12 -> Completados
```

## Gestion Correctiva

- `Management.Type = Corrective`.
- `Semester = 0`.
- Codigo esperado tipo `CORR-YYYY`.
- No sincroniza todos los equipos al crearse; los planes se crean por falla.

## Routing

- `ManagementId` obligatorio en todo redirect.
- Resolver gestion por `ManagementPlanId`, luego `ManagementId`, luego fallback.
- Sidebar de correctivo debe ir al dashboard correcto y no a preventivo por cache default.
- El dashboard correctivo no debe depender de la gestion preventiva activa.
- Cualquier notificacion correctiva debe llevar `ManagementId` correctivo y destino correctivo.

## UI

- En correctivo los dropdowns no deben bloquearse solo por `isWizard`.
- Condicion correcta para bloquear selects: `isWizard && !isCorrective`.
- El usuario debe poder seleccionar laboratorio/equipo si no existe L-6 previa.

## Delete

Correctivo activo permite soft-delete de gestion correctiva si la regla actual lo permite. No convertirlo en hard-delete.

## Cache

`ManagementContextService.GetCurrentManagementAsync(ManagementType? type = null)` usa claves separadas por tipo. Si hay bugs de contexto, revisar cache antes de modificar UI.

## Separacion Con Preventivo

- No mezclar planes preventivos y correctivos en conteos, calendario, cards o notificaciones.
- Si una query esta en dashboard correctivo, filtrar por `Management.Type == Corrective` o por `ManagementId` correctivo explicito.
- Si una query esta en dashboard preventivo, no incluir `Semester = 0` ni `Type = Corrective`.
- Aplicar a correctivo la misma regla estabilizada en preventivo: servidor reconstruye verdad, `.AsTracking()` para guardar, redirect conserva contexto.
