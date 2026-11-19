# Plan Sprint 3 — Rol Docente, bandeja de aprobación y stock por horario

**Fecha:** 2026-11-09 · **Rama base:** `qa` (`3af6fe8`) · **BD verificada:** MonsterASP `db70630` (solo lectura: metadatos y conteos).
**Estado:** propuesta para validar. No se modificó código ni base de datos.

---

## 1. Qué hay hoy (verificado)

### 1.1 La aplicación
- ASP.NET Core 9 Razor Pages + EF Core 9 + SQL Server + Identity. UI NiceAdmin/Bootstrap 4, SweetAlert2 v7.
- Roles Identity actuales: `SuperAdmin`, `Administrator`, `Supervisor`, `PracticeClosure` (encargado de cierre). **No existe rol Docente.**
- Módulo de planificación ya construido en `Pages/LaboratoryOperations/`:

| Pieza existente | Qué hace | Se reutiliza para |
|---|---|---|
| `Plannings` + `PlanningRequirements` | Práctica programada (fecha, periodos, laboratorio, docente, grupo) + artículos pedidos | La **solicitud del docente** es una `Planning` |
| `Grilla` + `ClassPeriods` (12 periodos 07:35–18:35) | Horario por ambiente y semana | Selector de horario del docente |
| `SlotValidationService` | Valida choque de laboratorio, docente ocupado, día suspendido | **Alerta de choque** en la solicitud |
| `ArticleAvailabilityService` + `DemandPeak` | Demanda por **tramos horarios** vs stock: OK / Rotación / Cruce / Falta | Semáforo de insumos por horario |
| `Practices` + `PracticeRequirements` (196 prácticas, 2111 requisitos) | Kit estándar de cada práctica | Prellenar los insumos |
| `CourseGroups` (147) | Grupo, materia, docente, nº estudiantes | Lo que el docente puede elegir |
| `Cierre` + `L3CircuitService` + `L5` | Entrega/devolución de material e incidentes | Cierre de la práctica |
| `Notifications` | Avisos en campana | Avisar aprobación/rechazo |

Estados actuales de `Planning.Status`: `Planificada → Aprobada → Finalizada` (+ Reprogramada, PendienteReposicion, Repuesta, Cancelada-legado).

### 1.2 La base real (MonsterASP)
- 32 migraciones; la última `20261018152946_CorrectiveCoexistenceAndL7Checklist` = la última del repo (**repo y nube alineados**).
- `Plannings` 1038 (817 Finalizada, 220 Aprobada, 1 Planificada) · `PlanningRequirements` 11 461 · `ArticleStocks` 700 filas en 9 ambientes (ECONO concentra 429) · `Articles` 1374 · `People` 57 (21 con rol Docente activo) · `Users` 3.
- `Departures` (salidas L-3) = **0** en la nube.
- ⚠ `EstructuraBD.sql` (2026-10-02) **está desactualizado**: aún tiene `L3Requests` y no tiene `Plannings`, `CourseGroups`, `ArticleStocks`, etc. Tomar como verdad la BD viva / migraciones del repo.

---

## 2. Diagnóstico del problema de stock

**La reserva (planificación) NO descuenta stock.** Lo que descuenta es la **salida L-3**:

- `L3CircuitService` (línea ~225): al entregar, `ArticleStocks.AvailableQuantity -= cantidad`; al devolver (línea ~390) `+= cantidad`.
- `ArticleAvailabilityService.GetStockAsync` usa ese **mismo** `AvailableQuantity` para evaluar **cualquier fecha futura**.
- Para equipos (unidades físicas) pasa igual: una unidad "En préstamo" **hoy** se cuenta como no disponible **para todas las fechas**.

Consecuencia (su ejemplo del fixer, 1 unidad):
1. 09:00–10:00 Práctica A, 11:00–12:00 Práctica B. El motor por tramos dice OK (no se cruzan). ✔
2. Se entrega el fixer a A → stock pasa a 0.
3. Mientras no se registre la devolución, la práctica B (y cualquier práctica futura) ve stock 0 → conflicto falso. ✘
4. Si nunca se registra la devolución, el stock queda reducido para siempre. ✘

Es decir: el campo mezcla **"cuánto tenemos"** con **"cuánto está fuera ahora"**.

### 2.1 Solución propuesta: stock patrimonial + ocupación por intervalo

Tres conceptos separados:

| Concepto | Dónde vive | Cuándo cambia |
|---|---|---|
| **Existencia utilizable** (lo que la universidad tiene en buen estado) | `ArticleStocks.AvailableQuantity` (o renombrar a `OnHandQuantity`) | Solo por: alta/compra, ajuste de inventario, **baja por L-5** (rotura/pérdida), paso a mantenimiento / retorno de mantenimiento |
| **Reservado en un intervalo** | Calculado: suma de `PlanningRequirements` de prácticas que se cruzan con el intervalo (+ margen de preparación/limpieza opcional) | Al aprobar / reprogramar / rechazar una práctica (nada se escribe en stock) |
| **En uso ahora** | Calculado: salidas L-3 abiertas | Al entregar / devolver (solo informativo y para alertas de "no devuelto") |

**Disponible(artículo, intervalo) = Existencia utilizable − Σ reservas que se cruzan − unidades en mantenimiento en ese intervalo.**

Cambios concretos:
1. `L3CircuitService`: dejar de restar/sumar `AvailableQuantity` en entrega/devolución para artículos reutilizables (todos los actuales: ver §6-P4).
2. Devolución con faltante → L-5 obligatorio → **la baja de existencia la hace el L-5** (`AvailableQuantity -= AffectedQuantity` al confirmar el incidente, con registro de auditoría). Equipos con daño → mantenimiento (ya bloquea la unidad).
3. Equipos: "en préstamo" solo resta en el **intervalo** de la salida (fecha salida → devolución estimada), no en todas las fechas. Mantenimiento `InProgress` sí bloquea siempre (hasta cerrarse con resultado).
4. Alerta nueva: salida L-3 no devuelta pasada la hora de fin → aviso al administrador ("material no devuelto, afecta a la práctica X de las 11:00").
5. Datos: como `Departures = 0` en la nube no hay que reparar stock allí; en la base local REAL verificar salidas abiertas antes del cambio.

---

## 3. Flujo nuevo de la solicitud

```
Docente: Borrador ──enviar──▶ Solicitada ──▶ (Admin)
                                   │
            ┌──────────────────────┼─────────────────────────┐
            ▼                      ▼                         ▼
        Observada             Aprobada                  Rechazada
   (vuelve al docente,   (admin puede ajustar          (motivo; no ocupa
    corrige y reenvía)    insumos/horario/lab)          horario ni stock)
                               │
                 día de la práctica: entrega/devolución L-3 (Cierre)
                               │
              Docente marca "Entregado" (EntregadaDocente)
                               │
           Admin: Finalizada  ó  Finalizada con incidencia (abre L-5)
```

- Nuevos estados en `PlanningStatus`: `Borrador`, `Solicitada`, `Observada`, `Rechazada`, `EntregadaDocente`. `Rechazada` y `Borrador` entran en `NonOccupying`.
- `Solicitada`/`Observada`: ver pregunta P3 (¿ocupan horario?).

---

## 4. Cambios de base de datos — BORRADOR INICIAL (reemplazado por §8)

| Tabla | Cambio | Motivo |
|---|---|---|
| `Users` | `PersonId int NULL` FK → `People`, índice único filtrado | Vincular la cuenta del docente con su `Person` (hoy **no existe vínculo**) |
| `Roles` | Insertar rol `Teacher` (Docente); `UserRole.Docente = 4` | Nuevo rol |
| `Plannings` | `StudentCount int NULL`, `RequestedByUserId`, `SubmittedAt`, `ReviewedByUserId`, `ReviewedAt`, `ReviewComment nvarchar(1000)`, `RejectionReason nvarchar(500)`, `TeacherDeliveredAt`, `ClosureResult int NULL` (SinNovedad/ConIncidencia) | Trazabilidad de la solicitud y cierre |
| `PlanningReviewLogs` (nueva) | PlanningId, Acción, EstadoAnterior, EstadoNuevo, Comentario, UserId, Fecha, Diff JSON | Historial de qué cambió el admin (requisito de no sobrescribir) |
| `PlanningRequirements` | `ApprovedQuantity decimal NULL` | Mostrar al docente lo que pidió vs lo aprobado |
| `Articles` | (si P4 lo pide) valor `Consumible` en `ArticleType` | Distinguir insumos que sí se consumen |

No se tocan migraciones existentes. Datos: script para crear cuentas Docente a partir de las 21 `People` con rol Docente (reusar `Tools/QaTestUsers`), primero en copia QA.

---

## 5. Vistas

### 5.1 Docente (menú propio, solo ve lo suyo — filtro por `TeacherPersonId = User.PersonId`)

1. **Inicio docente**: tarjetas (Próxima práctica · Solicitudes pendientes · Observadas que requieren acción · Por marcar entregado), agenda de la semana y avisos.
2. **Nueva solicitud** (asistente de 4 pasos, barra de progreso como el wizard actual):
   1. *Qué*: materia → grupo (solo sus `CourseGroups`) → práctica del catálogo (o "práctica extraordinaria" con nombre libre). N.º de estudiantes prellenado del grupo, editable.
   2. *Cuándo y dónde*: calendario semanal tipo Grilla del laboratorio elegido. Celdas ocupadas en gris con "Ocupado" (sin datos de otros docentes), propias en color, pendientes rayadas. Al elegir día + periodos se valida en vivo con `SlotValidationService`: **choque de laboratorio = alerta roja que bloquea**, docente ocupado / día suspendido = alerta. Botón "Ver laboratorios libres en ese horario".
   3. *Insumos*: lista prellenada con el kit de la práctica escalado por estudiantes; buscador por catálogo/categoría. Por fila: cantidad, **stock total**, **disponible en ese horario** y semáforo (verde OK · ámbar rotación/justo · rojo falta o cruce, con "lo usa Práctica X 09:00–10:00"). Faltantes advierten pero no bloquean (regla D2 actual).
   4. *Resumen y enviar*: todo en una tarjeta, observaciones para el administrador, "Guardar borrador" / "Enviar solicitud".
3. **Mis solicitudes**: pestañas Borrador / Solicitadas / Observadas / Rechazadas, con comentario del admin; editar si Borrador u Observada, retirar si Solicitada.
4. **Mis prácticas** (aprobadas): calendario mes/semana + lista. Detalle con horario, laboratorio, insumos **aprobados** (resaltando cambios vs lo pedido) y botón **"Marcar como entregado"** (habilitado al terminar la práctica).
5. **Consultar disponibilidad** (solo lectura): elegir fecha/horario y ver laboratorios libres y stock de artículos para ese intervalo.

### 5.2 Administrador
1. **Bandeja de solicitudes** (nueva): contador en menú y campana; filtros por fecha, laboratorio, docente; orden por fecha de práctica (lo más próximo primero), indicador de choques.
2. **Revisión de solicitud**: a la izquierda los datos y la grilla del laboratorio ese día; a la derecha el análisis (choques, semáforo de insumos). Acciones: **Aprobar**, **Aprobar con cambios** (editar cantidades, laboratorio, horario — se registra el diff), **Observar** (devuelve al docente con comentario), **Rechazar** (motivo obligatorio). Confirmaciones SweetAlert.
3. **Cierre**: lista de prácticas "Entregadas por docente" → **Finalizar** o **Finalizar con incidencia** (abre L-5 existente). Integrado con la pantalla `Cierre` actual.

---

## 6. Inconsistencias encontradas y preguntas

| # | Hallazgo | Pregunta |
|---|---|---|
| P1 | `Users` no tiene vínculo con `People`; los docentes no tienen email y sus nombres son alias ("Ing.zabala", "Pedro guereca"). | ¿Cómo se crean las cuentas docentes (usuario/correo) y quién las entrega? ¿Las 21 personas con rol Docente son todos? |
| P2 | Ya existe el rol **Encargado de cierre** que registra la entrega/devolución física (L-3) y los L-5. | El "Entregado" del docente: ¿significa que **devolvió el material** (y el encargado lo verifica) o solo "práctica realizada"? ¿Se mantiene el encargado de cierre? |
| P3 | Dos docentes pueden pedir el mismo laboratorio/horario. | ¿Una solicitud **pendiente** bloquea el horario para otros (primero que pide) o solo bloquea la **aprobada** (el admin decide)? Recomiendo: pendiente avisa, aprobada bloquea. |
| P4 | Los 1374 artículos son utensilios/equipo/vajilla (no hay categoría de comestibles) y `ArticleType` está vacío en todos. | ¿El docente también pedirá insumos consumibles (harina, huevos…)? Si sí, se agrega tipo `Consumible` que **sí** se descuenta. |
| P5 | Stock repartido en 9 ambientes (ECONO = 429 filas); la salida L-3 sale del Economato, la disponibilidad usa el total institucional. | ¿El stock se considera un **fondo común** o cada laboratorio solo usa lo suyo? |
| P6 | El Documento Explicativo dice que aprueba el "Responsable de laboratorio". | ¿Aprueban solo `Administrator`/`SuperAdmin` o también `Supervisor`? |
| P7 | Hipótesis D-11 del backlog sin resolver. | ¿Hay margen de preparación/limpieza entre prácticas (p. ej. 1 periodo) para considerar el material ocupado? |
| P8 | `Laboratories.Capacity` es NULL en los 24 ambientes. | ¿Validar estudiantes vs capacidad? Si sí, hay que cargar capacidades. |
| P9 | `ClassesStartDate/EndDate` de las gestiones vacías; 2027-1 tiene programación pero sin planificaciones. | ¿Para qué gestión solicitarán los docentes y con qué anticipación mínima (p. ej. 48 h / hasta el viernes anterior)? |
| P10 | El docente puede tener prácticas del catálogo (196) o extraordinarias. | ¿Se permiten prácticas fuera del catálogo? |
| P11 | `EstructuraBD.sql` está desactualizado frente a la BD viva. | Usar migraciones como fuente; regenerar el script si se entrega como documento. |

---

## 7. Orden de implementación (Sprint 3)

| Fase | Entregable | Archivos principales |
|---|---|---|
| 0 | Respuestas a P1–P11 | — |
| 1 | **Stock por horario** (§2.1) + tests de `L3CircuitService` y `ArticleAvailabilityService` | `Services/L3CircuitService.cs`, `Services/ArticleAvailabilityService.cs`, `Pages/LaboratoryOperations/L5.cshtml.cs`, Tests |
| 2 | Migración: rol Docente, `Users.PersonId`, campos de revisión, `PlanningReviewLogs` | `Models/*`, `Data/ApplicationDbContext.cs`, nueva migración, `AuthorizationHelper`, `UserRole` |
| 3 | Servicio `TeacherRequestService` (crear/enviar/editar/retirar, transiciones de estado validadas) + scoping por docente | `Services/` nuevo, `PlanningStatus` |
| 4 | Vistas docente (§5.1) | `Pages/Docente/*`, `_Sidebar.cshtml` |
| 5 | Bandeja y revisión del administrador + notificaciones | `Pages/LaboratoryOperations/Solicitudes*`, `NotificationHelper` |
| 6 | Cierre: "Entregado" docente + Finalizar con/sin incidencia | `Pages/Cierre`, `Pages/Docente/MisPracticas` |
| 7 | QA: copia de BD, usuarios de prueba por rol, scripts E2E (`Tools/E2E`), navegador; luego publicar en MonsterASP con respaldo | `Tools/QaTestUsers`, `Tools/E2E` |

Criterios de aceptación clave:
- Un docente nunca ve ni modifica planificaciones de otro docente (prueba por URL directa).
- Fixer 1 unidad: prácticas 09–10 y 11–12 aprobables ambas; 09–10 y 09:30–10:30 → alerta de cruce.
- Entregar material no cambia la existencia; un L-5 de rotura sí la reduce y queda auditado.
- Rechazada/Borrador no ocupan laboratorio ni material.

---

## 8. Decisiones tomadas (2026-11-09) y cambios de BD definitivos

### 8.1 Decisiones

| # | Decisión |
|---|---|
| R1 | El administrador crea la cuenta del docente desde **Usuarios › Nuevo** (ya existe): rol *Docente*, elegir la **persona docente** del catálogo, correo, y contraseña escrita **o generada por el sistema** (se muestra una sola vez; el docente debe cambiarla en su primer ingreso). |
| R2 | No se implementa el "Entregado" del docente: el cierre sigue con el flujo existente (Encargado de cierre: L-3/L-5 → Finalizada). |
| R3 | Solo la solicitud **Aprobada** ocupa laboratorio y material. Solicitada/Observada solo **avisan** ("hay otra solicitud pendiente en ese horario"). |
| R4 | Solo artículos actuales (reutilizables). Insumos consumibles quedan para el siguiente sprint. |
| R5 | Stock = total institucional (sin importar el ambiente). Es lo que ya hace el motor cuando no se indica origen. |
| R6 | Aprueban `Supervisor`, `Administrator` y `SuperAdmin` (`ManagementRoles`). |
| R7 | Ya existe en código: entre prácticas del **mismo ambiente** debe quedar **un periodo libre** (`OccupancyService`, `SlotValidationService` → `LAB_OCCUPIED`). Se mantiene tal cual; no se agrega margen nuevo para el material. |
| R8 | Ya existe en código: la capacidad se valida si el ambiente la tiene registrada; hoy todas son NULL → solo aviso informativo. No se toca. |
| R9 | Ventana de solicitud: solo la gestión preventiva **Activa** (hoy 2026-2); fecha entre **hoy + 2 días hábiles** y **min(hoy + 60 días, fin de gestión)**. Configurable en `appsettings` (`TeacherRequests:MinLeadBusinessDays=2`, `MaxLeadDays=60`). |
| R10 | Solo prácticas del **catálogo** de las materias de los grupos del docente (196 prácticas en 19 materias; 180 con kit). Se elige práctica → se cargan sus artículos escalados por estudiantes; el docente puede ajustar cantidades y agregar artículos. |
| R11 | Se trabaja contra la BD viva; ningún cambio se aplica sin aprobación. Orden: copia QA → pruebas → respaldo → nube. |

### 8.2 Cambios de base de datos para aprobación (una migración nueva, aditiva)

| # | Objeto | Cambio | Por qué / normalización |
|---|---|---|---|
| C1 | `Users` | `PersonId int NULL` FK → `People(Id)`, índice **único filtrado** (`PersonId IS NOT NULL`) | Un usuario ↔ una persona; los datos del docente no se duplican |
| C2 | `Users` | `MustChangePassword bit NOT NULL DEFAULT 0` | Contraseña generada por el sistema |
| C3 | `Users` | Ampliar `CK_Users_Role` para aceptar `4` (Docente) | El check actual solo admite 1, 2, 3, 99 |
| C4 | `Roles` (dato) | Insertar rol Identity `Teacher` | Autorización `[Authorize(Roles="Teacher")]` |
| C5 | `Plannings` | `PracticeId int NULL` FK → `Practices(Id)` | Hoy la planificación solo guarda el **nombre** de la práctica como texto; el vínculo al catálogo evita duplicar/variar nombres. `PracticeName` se conserva como copia histórica |
| C6 | `Plannings` | `StudentCount int NULL` (CHECK > 0) | Estudiantes de la sesión (escala el kit) |
| C7 | `Plannings` | Ampliar `CK_Plannings_Status` con `Solicitada`, `Observada`, `Rechazada` | Estados del flujo docente; `Rechazada` no ocupa |
| C8 | **Nueva** `PlanningReviews` | `Id, PlanningId FK, Action int (Enviada/Observada/Aprobada/AprobadaConCambios/Rechazada/Retirada), FromStatus, ToStatus, Comment nvarchar(1000) NULL, UserId FK Users, CreatedDate` | Historial de la solicitud sin sobrescribir; el comentario/motivo vive aquí, no en `Plannings` |
| C9 | **Nueva** `PlanningReviewLines` | `Id, PlanningReviewId FK, ArticleId FK, PreviousQuantity decimal(12,2) NULL, NewQuantity decimal(12,2) NULL` | "Pedido vs aprobado" por artículo, solo cuando el aprobador cambia cantidades |

Sin cambios de esquema para el stock (§2.1): solo cambia el comportamiento de `L3CircuitService` (la salida/devolución deja de restar/sumar `ArticleStocks` en artículos reutilizables; la baja la hace el L-5). En la nube hay 0 salidas, no hay datos que corregir.

Quién solicitó = `Plannings.CreatedById` (ya existe). "Finalizada con incidencia" = existe L-5 de la planificación (derivado, sin columna).

### 8.3 Otra inconsistencia detectada
- La gestión **2027-1** está en estado *Completada* (histórico) y tipo *Por confirmar*, pero tiene 422 programaciones y 41 grupos futuros. Para el docente no afecta (se usa la gestión Activa 2026-2), pero conviene corregirla.

---

## 9. Implementado (2026-11-09)

Cambios de base: migración `20261109172523_TeacherRequestsAndIntervalStock` (C1–C4, C7–C9; C5/C6 no hicieron falta porque
`Planning → PracticeSchedule` ya guarda la práctica del catálogo y los estudiantes). Además siembra el rol `Teacher` y
devuelve a la existencia lo que estuviera entregado en salidas L-3 abiertas (en la nube: 0 filas).

| Área | Archivos |
|---|---|
| Stock por horario | `Services/L3CircuitService.cs` (`StockRules`), `Services/ArticleAvailabilityService.cs`, `Data/ApplicationDbContext.cs` (baja por L-5) |
| Modelo | `Models/PlanningReview.cs`, `Models/User.cs`, `Models/Planning.cs` (estados Solicitada/Observada/Rechazada), `Models/Enums/UserRole.cs` |
| Servicio | `Services/TeacherRequests/*` (ventana, choques, material por tramo, sugerencias, envío, aprobar/observar/rechazar, ficha PDF) |
| Acceso | `Helpers/TeacherAccess.cs` (alcance del docente + cambio obligatorio de contraseña), `Pages/Account/CambiarClave` |
| Usuarios | `Pages/Users/Create`, `Edit`, `TeacherLink.cs`, `_TeacherLinkFields.cshtml` |
| Portal docente | `Pages/Docente/{Index,NuevaSolicitud,Solicitudes,Solicitud,Calendario,Disponibilidad}`, `wwwroot/js/docente-solicitud.js`, `wwwroot/css/docente.css` |
| Revisor | `Pages/LaboratoryOperations/Solicitudes/{Index,Revisar}`, menú lateral con contador |

Verificación: `dotnet build` 0 errores; `dotnet test` 420/420 (8 pruebas nuevas en `TeacherRequestServiceTests`);
recorrido completo en navegador sobre una copia local de la base (alta de docente → cambio de contraseña → solicitud con
sugerencia de horario → aprobación con cambios → ficha PDF; y observada → corregida → reenviada → retirada).

Pendiente: aplicar la migración y publicar en MonsterASP (con respaldo previo), y crear las cuentas reales de los docentes.
