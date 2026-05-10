# Modulo Dashboard Y Navegacion

## Objetivo De La Proxima Intervencion

Mejorar el dashboard y sus opciones de navegacion sin mezclar preventivo con correctivo.

La tarea pedida para la siguiente colaboradora se concentra en:

- Calendario/L-48 dentro del dashboard.
- Navegacion entre cards, pasos y acciones del dashboard.
- Notificaciones visibles desde dashboard, no solo dentro del wizard.
- Flujo correctivo separado, aplicando la estabilidad ya lograda en preventivo.

## Archivos Probables

- `Pages/Index.cshtml` y `Pages/Index.cshtml.cs`: dashboard/wizard principal.
- `Pages/Managements/Details.cshtml` y `.cshtml.cs`: cards de gestion y accesos.
- `Pages/Shared/_WizardStep.cshtml`: acciones por tarjeta del wizard.
- `Pages/Shared/_WizardSteps.cshtml`: stepper.
- `Pages/Shared/_Layout.cshtml`: campana/notificaciones globales si aplica.
- `Pages/Api/Notifications.cshtml.cs` o endpoints equivalentes si se toca API de notificaciones.
- Modulos concretos si una accion navega a L-48, L-3, L-12 o correctivo.

Si la solucion exige tocar mas de 5 archivos, presentar plan antes de editar.

## Reglas De Navegacion

- Toda accion del dashboard que entra al wizard debe preservar `ManagementId`.
- Si hay filtro de laboratorio, preservar `SelectedLabId`.
- Si una accion vuelve desde historico o detalle, preservar `returnStep` cuando aplique.
- `Completados` es `Step = 7`; no redirigir el card verde a Kardex/L-48.
- No crear caminos paralelos para L-3 del wizard: usar `Departures/MassCreate`.

## Calendario L-48 En Dashboard

- El calendario debe respetar el contexto de gestion: preventivo o correctivo.
- No mezclar planes preventivos con correctivos en la misma consulta si la pantalla esta filtrada por gestion.
- Filtrar tareas activas con `!MaintenanceTask.IsDeleted`.
- Si se muestran pendientes/ejecutadas, derivar estado desde `ManagementPlan` + `Maintenance` + tareas L-48, no solo desde UI.
- Cualquier toggle o edicion que guarde cambios debe cargar entidades con `.AsTracking()`.

## Notificaciones En Dashboard

- Las notificaciones ya existen en el sistema, pero deben ser visibles/utiles desde dashboard.
- No duplicar el sistema de notificaciones si ya hay endpoint o polling global.
- Separar notificaciones preventivas y correctivas por `ManagementId`/`ManagementType` cuando corresponda.
- No generar notificaciones durante borradores; solo en acciones finales.
- Las notificaciones del dashboard deben llevar al destino correcto con `ManagementId` y `Step`.

## Correctivo Desde Dashboard

- Correctivo debe operar separado de preventivo.
- No usar fallback de gestion activa preventivo cuando el usuario esta en correctivo.
- El dashboard correctivo debe enviar a L-7 con `ManagementId` correctivo.
- Dropdowns correctivos no deben bloquearse por `isWizard`.
- Aplicar la misma logica estable de preventivo: reconstruir desde servidor, preservar contexto, usar `.AsTracking()`.

## Checklist Para La Colaboradora

- Leer `context.md`.
- Leer `.agent/context/00-router.md`.
- Leer `.agent/context/modules/dashboard-navigation.md`.
- Leer `.agent/context/modules/kardex-l48.md` si toca calendario L-48.
- Leer `.agent/context/modules/corrective.md` si toca correctivo.
- Leer `.agent/context/areas/ui.md` y `backend-page-models.md` si toca vistas y PageModels.
- Probar una ruta preventiva y una correctiva antes de cerrar la tarea.
