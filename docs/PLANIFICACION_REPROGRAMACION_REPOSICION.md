# Planificación, reprogramación y reposición de día

**Corte:** 2026-10-09 · trabajo OC-2026-10-09-02. Guía de uso y QA, subordinada al código citado al final; los conteos describen solo la consulta de la base QA indicada, no producción.

## Navegación y permisos

| Página | Ruta Razor Pages | Uso |
|---|---|---|
| Planificaciones | `/LaboratoryOperations/Planificaciones?managementId={id}` | Consultar por gestión; filtrar por semana, estado y laboratorio. Reabrir una aprobada cuando proceda. |
| Grilla | `/LaboratoryOperations/Grilla?managementId={id}&laboratoryId={id}&date=yyyy-MM-dd` | Consultar horario por ambiente y semana; `week={n}` y `twoWeeks=true` permiten cambiar la vista. |
| Traslado individual | `/LaboratoryOperations/Grilla?reprogramar={planningId}` | Elegir destino de una planificación. |
| Calendario | `/LaboratoryOperations/CalendarioAcademico?managementId={id}` | Generar semanas, ver días, suspender o iniciar reposición. |
| Bandeja | `/LaboratoryOperations/Reposiciones?managementId={id}&recoveryId={id}` | Consultar pendientes y avance de cada día. |
| Traslado desde bandeja | `/LaboratoryOperations/Grilla?reponer={planningId}&recoveryId={id}` | Reubicar **una** planificación pendiente del día. |

Las cuatro páginas exigen roles de gestión para entrar. Los handlers de **reprogramar**, **reponer día con sesiones** y **reabrir** exigen **SuperAdmin**; la bandeja solo ofrece reubicación a SuperAdmin en gestión editable. En el calendario, **Administrador o SuperAdmin** pueden generar semanas y suspender/habilitar un día sin planificaciones vigentes en una gestión editable; suspender un día con sesiones para reposición es solo de SuperAdmin. Los servicios rechazan escrituras cuando la gestión no está Activa o Inactiva; una gestión completada es de consulta. [Grilla.cshtml.cs; CalendarioAcademico.cshtml.cs; Reposiciones.cshtml.cs; Planificaciones.cshtml.cs; DayRecoveryService.cs; AcademicCalendarService.cs]

## Dos flujos distintos

1. **Reprogramación individual:** desde Grilla, entrar con `reprogramar`, arrastrar el bloque o seleccionar fecha, período inicial, ambiente y docente; previsualizar, indicar motivo y confirmar. Se mueve el bloque **completo** (misma cantidad de períodos), dentro de su gestión. El servicio vuelve a validar el destino bajo bloqueos transaccionales y exige aceptar los códigos de advertencia presentes al confirmar; un bloqueo o una advertencia nueva no aceptada impide guardar. El origen queda **Reprogramada** y se crea un destino nuevo y un registro `Rescheduling`. [Grilla.cshtml(.cs); ReschedulingService.cs; PlanningLock.cs]
2. **Suspensión y reposición de día:** en Calendario, un día habilitado **sin** sesiones vigentes se suspende con motivo por `SetDay` y no crea bandeja; si **tiene** planificaciones vigentes, SuperAdmin usa **Reponer día**, aporta motivo y confirma. `DayRecoveryService` suspende el día, crea `DayRecovery` **En reposición**, guarda el estado previo y pone las planificaciones del día en **PendienteReposicion** dentro de una transacción. Si no hay planificaciones vigentes, remite a la suspensión normal; si hay estados incompatibles, rechaza la operación. Desde Reposiciones se traslada cada pendiente por separado: origen **Repuesta**, nuevo destino y `Rescheduling` asociado a esa reposición. La reposición pasa a **Completada** al trasladar la última pendiente; el día original sigue suspendido y no se habilita desde el calendario cuando tiene `DayRecoveryId`. [CalendarioAcademico.cshtml(.cs); AcademicCalendarService.cs; DayRecoveryService.cs; Reposiciones.cshtml(.cs); ReschedulingService.cs]

**Estados del destino:** origen Planificada → Planificada; Aprobada o Finalizada → Aprobada. Si el origen es PendienteReposicion, el estado anterior Aprobada/Finalizada produce destino Aprobada; el anterior Planificada produce Planificada. Una Finalizada trasladada **no** conserva Finalizada en el destino. No se traslada una PendienteReposicion por `reprogramar`: debe usarse `reponer` con su reposición activa. [ReschedulingService.cs: `InheritedStatus`, `ExecuteAsync`; Grilla.cshtml.cs: `LoadOriginAsync`]

**Material e historial:** los requerimientos de planificación se copian al nuevo destino, que recibe otro identificador y código; no se editan al mover. La salida de material **L-3** y los incidentes **L-5** permanecen vinculados al origen: no interpretar la copia de requerimientos como traslado o devolución de material. Si había `PracticeSchedule`, el servicio actualiza fecha, horario, ambiente, docente, semana y estado a reprogramada. [ReschedulingService.cs: `ExecuteAsync`; Reposiciones.cshtml]

## QA manual pendiente: guion propuesto, no resultados

Realizar únicamente en entorno autorizado y con datos de prueba; registrar gestión, IDs de origen/destino, captura de previsualización, respuesta y estado posterior. **No se ejecutó este guion en este trabajo.**

| Caso | Acción y comprobación esperada según el código |
|---|---|
| Acceso | Ingresar con rol de gestión sin SuperAdmin: consultar páginas; intentar `reprogramar`, `reponer`, POST de reprogramación/reapertura y `RecoverDay`; comprobar restricción de escritura y que no se creó destino. Probar Administrador con suspensión sin sesiones frente a reposición con sesiones. |
| Individual y advertencias | Mover una Planificada y una Aprobada; previsualizar y confirmar con motivo. Ante advertencias, aceptar explícitamente las mostradas; intentar confirmar sin aceptarlas y después cambiar las condiciones entre vista previa y confirmación: verificar rechazo/revisión sin persistencia indebida. Comprobar estado origen/destino, períodos y copia de requerimientos. |
| Finalizada / material L-3 | Previsualizar traslado de Finalizada y revisar aviso de completada en interfaz; si se confirma, destino Aprobada. Con L-3/L-5 existentes, comprobar que sigan en el origen y que el nuevo destino no se presente como devolución ni como transferencia física. |
| Colisión | Intentar un destino ocupado o inválido: observar los mensajes de validación y verificar que no se guarde si hay bloqueo. Ensayar dos confirmaciones al mismo origen/destino solo en una prueba de concurrencia autorizada: el bloqueo SQL `sp_getapplock` y la revalidación están implementados, **no verificados aquí** bajo concurrencia real. |
| Día vacío y fechas | Suspender día sin sesiones con motivo y habilitarlo; no debe crearse reposición. Probar motivo vacío/>200 caracteres, fecha fuera del calendario y cambio del rango de clases que dejaría historial fuera; documentar rechazos reales. |
| Día con varias prácticas X/Y | Suspender un día habilitado con al menos dos planificaciones elegibles X/Y. Verificar estados previos guardados, día suspendido, bandeja agrupada por ambiente. Reubicar **solo X**: X Repuesta, Y aún PendienteReposicion, reposición En reposición; no habilitar el día ni marcarlo Completado por progreso parcial. Reubicar Y: comprobar destino, avance sin pendientes y reposición Completada. |
| Reabrir | Desde Planificaciones, SuperAdmin reabre una Aprobada sin salida L-3 no cancelada ni incidente L-5: pasa a Planificada y redirige a requerimientos. Con L-3 no cancelada o L-5, o con estado distinto de Aprobada/gestión no editable, debe rechazar; la reprogramación **no** equivale a reabrir. |
| Sin fechas de clases confirmadas | En Calendario, comprobar el aviso de que se usan fechas de la gestión; **no asumir inicio/fin efectivo de clases** por la existencia de semanas. Antes de operar con datos reales, confirmar fechas académicas institucionales y volver a evaluar semanas y días vinculados a historial. |

## Estado de validación y límites (2026-10-09)

- Evidencia **aportada por el orquestador**, no ejecutada por este escritor: `dotnet build --no-restore -m:1`: 0 errores / 0 warnings; `dotnet test Tests/Laboratorios.Tests/Laboratorios.Tests.csproj -m:1 --no-restore`: 60/60 pasan; `dotnet ef migrations has-pending-model-changes --no-build`: sin cambios pendientes del modelo, con advertencias EF de query filter. Estos resultados no sustituyen QA manual ni validación concurrente.
- Consulta **SQL SELECT de solo lectura**, reportada para `localhost/DB_Laboratorios_Univalle_DEMO_QA` el 2026-10-09: migraciones `20261009141121` y `20261009143600` aplicadas; 6 Plannings, 12 PlanningRequirements, 14 AcademicWeeks, 82 AcademicDays, 12 ClassPeriods, 427 PracticeSchedules; **0 Reschedulings y 0 DayRecoveries** en ese corte. Por ello, esta base no aporta evidencia de una reprogramación/reposición persistida.
- En ese mismo SELECT: 2026-1: 01/01–30/06; 2026-2: 01/07–31/12; `ClassesStartDate` y `ClassesEndDate` **NULL**. El calendario usa fechas de gestión como fallback cuando faltan las de clases; **no son fechas de clases confirmadas**. [AcademicCalendarService.cs: `GenerateWeeksAsync`; CalendarioAcademico.cshtml]
- **No ejecutado:** HTTP ni navegador manual, escrituras SQL, ensayos de concurrencia SQL; tampoco build, test ni consultas SQL por este escritor. Los resultados varían con código/datos: repetir validación y registrar entorno/fecha antes de presentarlos como estado vigente.

**Evidencia técnica consultada:** `Services/{DayRecoveryService,ReschedulingService,PlanningLock,AcademicCalendarService}.cs` y `Pages/LaboratoryOperations/{Grilla,Reposiciones,CalendarioAcademico,Planificaciones}.cshtml(.cs)`; resultados del orquestador anteriores. El comportamiento exacto de reglas de previsualización ajenas a esos archivos no se especifica aquí: confirmar con la validación de servidor y prueba manual antes de prometer un tipo concreto de colisión o advertencia.
