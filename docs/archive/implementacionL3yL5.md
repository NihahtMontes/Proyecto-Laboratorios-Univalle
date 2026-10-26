# Documento de Handoff: Implementación de Módulo L-3 y L-5

> **Autor del handoff original:** Dani (rama `RamaDani`).
> **Fusión:** `RamaDani` → `integration/planificacion-2026-10` (2026-10-03). El orquestador consolidó aquí los cambios de seguridad, migraciones, validación y persistencia transaccional descritos en este documento.

Este documento resume el trabajo de implementación, configuración y la arquitectura del nuevo flujo operativo de Laboratorios (Formularios L-3 y L-5). Sirve como guía de transferencia para que el equipo de desarrollo pueda retomar el proyecto, corregir bugs pendientes e implementar mejoras.

---

## 1. Resumen de Configuración Inicial y Seguridad

Para llegar al estado actual de la rama, se realizaron las siguientes configuraciones de infraestructura y seguridad. La rama `RamaDani` quedó **integrada** en `integration/planificacion-2026-10` el 2026-10-03; los puntos sensibles de autenticación fueron retirados por el orquestador antes de la fusión.

*   **Migraciones Aplicadas:** Se generaron y aplicaron exitosamente a la base de datos las migraciones de Entity Framework Core:
    *   `ImplementacionL3L5`: Creación de la estructura base.
    *   `RefactorL5ResponsibleToText`: Modificación de `L5Incidents` para desligar la clave foránea de usuarios y usar texto libre en su lugar (`ResponsibleName` y `ResponsibleType`).
    *   `20261003155613_IntegratePlanningTeacherPersonAndRestrict` (integración 2026-10-03):
        *   El docente se modela como **persona**: nueva columna `TeacherPersonId` en `L3Requests` con FK hacia `People`, restringida a la categoría `Docente`.
        *   `TeacherId` en `Users` permanece como referencia **opcional/legado** para no romper cuentas históricas.
        *   Nuevas columnas en `L3Requests`: `ScheduledEndDate` y `Observations`.
        *   Las FK de planificación cambiaron de borrado en cascada a `Restrict` para **no perder trazabilidad histórica** ante bajas de catálogos.
        *   Filtros globales `IsDeleted` activos en `L3Request` y `L5Incident` para honrar el soft-delete.

### Seguridad (integración 2026-10-03)

*   Se **retiró el *bypass* de Login**: ya no es válido iniciar sesión con la contraseña `Admin123!` (antes aceptada para cualquier usuario). La autenticación volvió al flujo normal de hash + credenciales por usuario.
*   Se **eliminó el bloque de `Program.cs`** que, en cada arranque, reseteaba la contraseña del usuario `admin` o del primer usuario de la tabla.
*   La cadena de conexión **nunca** se escribe en `appsettings.json`: se resuelve desde *user-secrets* bajo la clave `ConnectionStrings:DefaultConnection`. El `appsettings.json` se mantiene sin valores sensibles.

---

## 2. Flujo Operativo Implementado (Arquitectura)

El módulo se compone de 5 pasos secuenciales que simulan el ciclo de vida de un laboratorio práctico.

**Eje Central:** El identificador principal que viaja por la URL (como `planId`) es el `Id` de la tabla `L3Requests`. Este ID conecta todos los formularios y garantiza la integridad referencial.

**Estados de la planificación:** `Planificado`, `Aprobado`, `Finalizado` (definidos como constantes en `L3RequestStatus`).

### Paso 1 — Planificación
*   Se publica mediante un `InputModel` propio (sin *overposting*): solo las propiedades explícitas del modelo se enlazan al `POST`.
*   **Validaciones servidor:** facultad, laboratorio, docente (persona categoría `Docente` activa), materia y grupo deben ser consistentes; además se valida que **no exista choque de horario en el mismo laboratorio** (otra planificación aprobada/planificada que se solape en tiempo sobre el mismo `LaboratoryId`).
*   Al crear el registro se asigna el **código visible `PLN-00001`** (secuencial con prefijo `PLN-`) que el usuario ve en listados y reportes.
*   Endpoint AJAX `OnGetSearchPlansAsync` para el modal de búsqueda de planificaciones previas.

### Paso 2 — Requerimientos
*   Los detalles (`L3RequestDetails`) se capturan **por tipo de equipo** del laboratorio del plan. Cada detalle apunta a una **unidad representativa** del tipo (no a un número de serie fijo), y el conteo físico se resuelve después contra las unidades reales del laboratorio.
*   **Solo editable en estado `Planificado`.** Cualquier intento de edición en `Aprobado`/`Finalizado` rechaza silenciosamente la mutación servidor.

### Paso 3 — Disponibilidad y Aprobación
*   Compara el `ExpectedQuantity` por tipo contra las **`EquipmentUnits` con `Status == Operational` del mismo `EquipmentType` y del mismo laboratorio** del plan (ya no contra el pool global).
*   Aprueba el plan **dentro de una transacción**: antes, el *NoTracking* global impedía que `SaveChanges` persistiera la transición a `Aprobado`. Ahora la operación se ejecuta con tracking explícito y se confirma o se revierte atómicamente.
*   Si la disponibilidad es insuficiente, el flujo redirige al Paso 2 anexando `?conflict=true` para que el usuario ajuste cantidades.

### Paso 4 — Devolución L-3
*   **Validación por línea:** para cada ítem, `ReturnedQuantity` debe estar en el rango `0..DeliveredQuantity` (no se permiten devoluciones negativas ni superiores a lo entregado).
*   La devolución se evalúa **por planificación completa**: `Faltante = DeliveredQuantity − ReturnedQuantity` por cada tipo.
*   **Cierre (Finalizar L-3) exige** que los faltantes por tipo estén **cubiertos por la suma de `AffectedQuantity` de incidentes L-5** registrados contra ese mismo tipo en el mismo plan; de lo contrario el botón permanece deshabilitado.
*   Toda la transición a `Finalizado` ocurre dentro de **una sola transacción** con sus reescrituras de unidades y ajustes de historial.

### Paso 5 — Incidentes L-5
*   El `<select>` de "unidad afectada" lista solo **unidades concretas del tipo prestado y del laboratorio del plan** (no del pool global).
*   Al guardar el incidente, `ApplicationDbContext`:
    1.  Cierra el **`EquipmentUnitHistory` abierto** de la unidad afectada.
    2.  Marca la unidad como `Broken` (`Status = Broken`).
    3.  Abre un nuevo historial con motivo `INCIDENTE L5` enlazando al incidente recién creado.
*   El campo *Involucrado* se persiste como texto libre (`ResponsibleName`, `ResponsibleType`) para alinearse con los excels originales.

### Páginas de soporte
*   **Index, Calendario y Demanda** de planificación ahora **leen datos reales** desde la base (antes consumían una sesión demo en memoria). El calendario respeta el estado y la facultad/laboratorio del plan; la página Demanda agrega requerimientos por tipo y fecha.

---

## 3. Estado Actual: Errores Conocidos y Tareas Pendientes (Bugs)

El flujo está construido y es funcional a nivel base de datos. La rama `RamaDani` traía dos bugs del Paso 1 que ya fueron atendidos en la integración 2026-10-03; quedan pendientes nuevas tareas detectadas durante la fusión.

### Resueltos (integración 2026-10-03)
1.  ✅ **Comboboxes de Materia y Grupo vacíos — RESUELTO.** Se incorporaron semillas (`Seed Data`) de `Subjects` y `Groups` en la base de datos de desarrollo `DEMO_QA`, y se aseguró la inyección explícita del `SelectList` en el `PageModel` del Paso 1.
2.  ✅ **Filtro de Docentes con rol erróneo — RESUELTO.** El combo "Docente Responsable" ahora consulta exclusivamente `People` con **categoría `Docente` y `IsActive = true`**. Las cuentas de Administrador Sistema y los `Users` que no tengan persona asociada quedan excluidos.

### Pendientes
3.  🟠 **`AffectedQuantity > 1` no amplia el daño.** Cuando un incidente L-5 declara más de una unidad afectada, hoy **solo la unidad seleccionada** se marca como `Broken` y se le cierra el historial. Las unidades restantes del mismo tipo en el plan **no** se ven afectadas por el mismo incidente. Aceptable mientras la captura siga siendo por unidad individual; quedará como deuda cuando se agregue selección múltiple.
4.  🟠 **No hay reserva de stock entre planificaciones simultáneas.** Dos planificaciones que aprueban el mismo laboratorio y tipo de equipo en horarios solapados **pueden comprometer unidades operativas en común**, porque la reserva física se realiza al pasar a `Aprobado` sin lock transaccional inter-plan. Riesgo concreto cuando el personal aprueba varias solicitudes a la vez.
5.  🟠 **Editar / cancelar planificación aprobada no implementado.** Las acciones `Edit` y `Cancel` sobre un plan en estado `Aprobado` o `Finalizado` no están habilitadas. Hoy la mutación se rechaza servidor porque solo `Planificado` es editable (Paso 2); cualquier reasignación, cancelación o reagenda requiere intervención directa sobre la base.

---

## 4. Propuestas de Mejora (UX/UI)

Para futuras iteraciones, se recomienda implementar las siguientes mejoras en la experiencia de usuario. Las tres primeras ya están adoptadas donde aplica tras la integración 2026-10-03 (se mantienen en la lista como referencia para asegurar cobertura completa en las pantallas restantes):

1.  **Búsqueda Predictiva (Select2/Choices.js):** **Pendiente.** Los combos de Materia, Docente, Grupo y Utensilio siguen siendo `select` nativos; conviene aplicar Select2 (ya incluido en el proyecto) al crecer los catálogos.
2.  **Alertas con SweetAlert (Toast Notifications):** ✅ **Adoptada** para *feedback* de agregar ítem, faltantes y confirmaciones ligeras. Sigue pendiente cubrir todos los flujos CRUD que hoy aún usan banners Bootstrap.
3.  **Mini-Calendario Visual en Paso 1:** ✅ **Adoptado** en la página de Calendario (FullCalendar) consumiendo datos reales. Queda pendiente integrarlo *también* dentro del modal "Buscar Planificación" del Paso 1 (hoy es tabla).
4.  **Confirmaciones Inline en Paso 4 (Devolución):** ✅ **Adoptada** la confirmación con SweetAlert antes de finalizar L-3 cuando hay faltantes cubiertos por L-5. Pendiente sumar los *checkmarks* animados/opacidad por fila cuando una devolución queda completa.
5.  **Persistencia en Modal L-5:** Pendiente. Hoy el incidente L-5 se sigue registrando en una página nueva (Paso 5). Cargarlo como modal/*side-panel* sobre el Paso 4 evitaría romper la inmersión de quien revisa el inventario.