# Mapa completo `BaseEstructuraFinal_V4.xlsx` → base de datos

**Para:** el agente del proyecto. El análisis del Excel lo hice yo. Este documento es la **fuente única** para saber qué columna va a qué propiedad, cómo se traduce cada valor, qué ya está cargado y qué falta.
**Fecha:** 2026-10-04 · **Excel:** V4, SHA-256 `8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2`
**Base analizada:** `DB_Laboratorios_Univalle_DEMO_QA`, migración `20261004054609_ArticleReviewStatus`, HEAD `d23d43a`. Todo en solo lectura.
**Complementa a:** `docs/PLAN_MIGRACION_V4.md` (esquema de la migración `AlignStructuralBaseV4` y decisiones D1–D5) y al plan `quiero-que-puebles-mi-lovely-wind.md`.

Leyenda de estado: ✅ cargado y correcto · 🟡 cargado parcial o con diferencias · 🔴 falta (estructura o datos) · ⚪ vacío en el Excel (solo estructura)

---

## 0. Resumen: lo que falta, en orden de ejecución

| # | Qué falta | Tipo | Bloquea | Sección |
|---|---|---|---|---|
| 1 | Migración `AlignStructuralBaseV4`: `PorConfirmar` en `ManagementType` y `MaintenanceType`, `PracticeSchedule`, cambios L-3, devoluciones y ajustes de receta | Esquema | Todo lo siguiente | PLAN §3 |
| 2 | **Corregir el estado de las 556 unidades:** hoy todas están `Operational` con condición NULL; el Excel dice 595 "Por confirmar", 45 Operativo y 4 De baja | Datos (DEMO_QA) | Reportes de estado | §3.9 |
| 3 | **Catálogo:** marcar los 107 equipos como `PendingClient` (hoy `LegacyInferred`) y cargar los 15 catálogos nuevos (CAT-0108…0122) | Datos | Unidades nuevas | §3.8 |
| 4 | **97 unidades nuevas** + 126 reasignaciones de catálogo (como `EquipmentClassificationDecision`) | Datos | Históricos de esas unidades | §3.9 |
| 5 | **20 gestiones históricas** (2014-1 … 2024-1) con `Type = PorConfirmar`; `1900-1` a cuarentena | Datos | L-8 y L-48 históricos | §3.5 |
| 6 | **7 títulos de actores** de la V1 sin cargar (en BD hay 15 títulos; en el Excel, 22) | Datos | — | §3.6 |
| 7 | **19 materias** reales + retiro lógico de las materias, grupos y prácticas demo | Datos | Programación | §3.18 |
| 8 | Laboratorios: `Piso = "Por Confirmar"` (3) y `Tipo`; verificar que coinciden | Datos | — | §3.4 |
| 9 | **Fase E: históricos L-6, L-7, L-8, participantes, mant-solicitud y L-48**, con la conciliación de §4 | Datos | Dashboard, Kardex, reportes | §3.10–3.17, §4 |
| 10 | Tratamiento de lo **inferido** que ya está en la BD (492 L-6, 140 L-7, 1 L-8, 120 costos) | Decisión + datos | Fase E | §4.5 |
| 11 | Interfaz: programación, L-3 por artículo, devoluciones | Código | Uso real | PLAN §7 |
| 12 | Fuente de la cliente (programaciones, recetas, stock) | Externo | Datos reales académicos | PLAN §0 |

---

## 1. Reglas globales de importación (para todas las tablas)

1. **Solo DEMO_QA.** Antes de escribir, comprobar `DB_NAME() = 'DB_Laboratorios_Univalle_DEMO_QA'`. Lotes de 100 como máximo, una transacción por lote y `ChangeTracker.Clear()` (o lote SQL) entre lotes.
2. **Un `ImportBatch` por ejecución**, con el SHA-256 de la V4. Cada fila que se crea o actualiza lleva `ImportBatchId`.
3. **Idempotencia por clave natural.** La clave natural del Excel se guarda en `Code` o en `HistoricalSourceKey` (ver cada tabla). Una segunda pasada debe insertar 0 filas.
4. **Las FKs se resuelven por código** (`CatalogCode`, `InventoryNumber`, `ActorCode`, `Management.Code`, `Laboratory.Code`, `Subject.Code`, etc.). Una referencia que no resuelve no se inventa: va a `DataQualityIssue` y la fila no se carga.
5. **"Por confirmar" nunca se convierte en un valor real.** Se usa el valor `PorConfirmar` del enum, o NULL si la columna lo admite, y se registra un `DataQualityIssue` por fila.
6. **No se borra nada que esté solo en la BD.** Lo inferido que no tenga par en el Excel se marca, no se elimina (§4.5).
7. **Fechas:** en L-8, `FechaInicio`, `FechaFin` y `ProximoMantenimiento` vienen como **texto** `AAAA-MM-DD` y hay que convertirlas. El resto son `datetime`.
8. **Textos:** recortar los espacios y saltos de línea de los bordes (72 en `ProblemaReportado` y 47 en `Sugerencia`) sin tocar el interior. Todas las longitudes caben (§5).
9. **No se importan** las hojas que empiezan con `_`.

**Códigos de `DataQualityIssue` que hay que usar** (los existentes se mantienen):

| Código | Cuándo |
|---|---|
| `VALUE_PENDING_CONFIRMATION` | Cualquier "Por confirmar" o "Sin especificar" del Excel (indicar tabla y columna) |
| `LEGACY_INFERRED_UNMATCHED` | Registro `INF_*` de la BD sin par en el Excel |
| `LEGACY_INFERRED_SUPERSEDED` | Registro `INF_*` de la BD que tiene par en el Excel (el Excel manda) |
| `LEGACY_ONLY_IN_DB` | Registro real de la BD sin par en el Excel (se conserva) |
| `CONTENT_CONFLICT` | Par encontrado con valores distintos (indicar el campo) |
| `MANAGEMENT_SHIFTED_BY_RULE` | L-6/L-7: gestión un semestre posterior a la fecha (regla de negocio confirmada; registro informativo) |
| `INVALID_MANAGEMENT` | Fila que apunta a `1900-1` |
| `ORGANIZATION_AS_TECHNICIAN` | Participante L-8 que es una organización con rol Técnico |
| `CATALOG_REASSIGNED` | Unidad cuyo catálogo cambia respecto de la BD |
| `SUBJECT_CAREER_HOMOLOGATED`, `STOCK_NOT_REGISTERED` | Ya definidos en PLAN §0 |

---

## 2. Tablas de traducción de valores (texto del Excel → enum)

Valores reales observados en la V4, con su número de filas.

| Columna Excel | Valor | → Enum / valor BD |
|---|---|---|
| `*.Estado` (Sedes, Facultades, Carreras, Laboratorios, Actores, ActorRoles, Materias) | Activo | `GeneralStatus.Activo (0)` |
| `Gestiones.Estado` | Abierta (1) / Cerrada (25) | `ManagementStatus.Active (0)` / `Completed (2)`. Solo puede haber una `Active` (índice único); hoy es 2026-2 |
| `Gestiones.TipoGestion` | Por confirmar (26) | `ManagementType.PorConfirmar (9)` (nuevo en `AlignStructuralBaseV4`) |
| `Actores.TipoActor` | Persona (30) / Organización (10) / Por confirmar (11) | Persona → `Intern` (si `Vinculacion` ≠ Externo); Organización → `Extern` con `IsEntity = 1`; Por confirmar → `Intern` + issue |
| `Actores.Vinculacion` | Interno (6) / Externo (2) / Por confirmar (43) | Interno → `Intern`; Externo → `Extern`; Por confirmar → issue `ACTOR_LINK_PENDING` (ya existe) |
| `ActorRoles.Rol` | Técnico 20, Docente 15, Solicitante 2, Verificador 1, Proveedor 1, Responsable 1 | `PersonOperationalRole`: Technician 1, Docente 8, Requester 5, Verifier 7, ExternalProvider 3, Responsible 6 |
| `CatalogoEquipos.Categoria` | Por confirmar (120) / Equipo (1) | `Category` NOT NULL: se conserva el valor actual o, en los nuevos, `Equipment (0)`, siempre con `ClassificationReviewStatus = PendingClient (1)` + issue. No es un valor confirmado |
| `CatalogoEquipos.TipoUtensilio` | No aplica 102 / Por confirmar 15 / textos sueltos 4 | No aplica → `UtensilType.NoAplica (0)`; resto → NULL + issue |
| `CatalogoEquipos.Clasificacion` | Por confirmar (121) | `TypeClassification = NULL` |
| `CatalogoEquipos.Estado` | Por confirmar (121) | `Status = Activo` + `ClassificationReviewStatus = PendingClient` (mismo criterio que los artículos) |
| `UnidadesFisicas.EstadoOperativo` | Por confirmar 595 / Operativo 45 / De baja 4 | `EquipmentStatus.PorConfirmar (98)` / `Operational (0)` / `Decommissioned (3)` |
| `UnidadesFisicas.CondicionFisica`, `VerificacionesL6.CondicionFisica` | Excelente / Bueno / Regular / Malo / Baja del equipo / Por confirmar | `PhysicalCondition`: Excellent 5 / Good 4 / Regular 3 / Bad 2 / Decommissioned 1 / PorConfirmar 9 |
| `UnidadesFisicas.EstadoUbicacion` | Pendiente (644, aunque traen laboratorio) | Con laboratorio → `LocationResolutionStatus.Confirmed (1)` (lo exige `CK_EquipmentUnits_LocationResolution`) + issue informativo |
| `VerificacionesL6.EstadoEquipo` | Operativo 665 / No operativo 933 / Por confirmar 1 | `ObservedEquipmentStatus`: `Operational (0)` / `OutOfService (2)` / `PorConfirmar (98)` |
| `SolicitudesL7.Prioridad` | Sin especificar (329) | `RequestPriority.PorConfirmar (9)` |
| `SolicitudesL7.Estado` | Por confirmar (329) | `RequestStatus.PorConfirmar (98)` |
| `SolicitudesL7.TipoSolicitud` | Mantenimiento (329) | `RequestType.Technical (1)` |
| `SolicitudesL7.TiempoEstimado` | XXX / XX / xxx (329) | **Se descarta** → `EstimatedRepairTime = NULL` |
| `MantenimientosL8.TipoMantenimiento`, `PlanL48.TipoMantenimiento` | Preventivo / Correctivo / Por confirmar | `MaintenanceType`: Preventivo 1 / Correctivo 2 / **PorConfirmar 9** (nuevo; **no** `Otros 99`) |
| `MantenimientosL8.TipoServicio`, `PlanL48.TipoServicio` | Interno / Externo / Por confirmar | `ServiceType`: Internal 0 / External 1 / PorConfirmar 9 |
| `MantenimientosL8.Estado` | Completado (1.209) | `MaintenanceStatus.Completed (2)`, `CompletionPercentage = 100` |
| `MantenimientosL8.EstadoCalidad`, `PlanL48.EstadoCalidad` | Válida 432 / Requiere revisión 777 (+387) | No tiene columna: "Requiere revisión" → issue `CONTENT_REVIEW_REQUIRED`; "Válida" → nada |
| `MantParticipantes.Rol` / `EsPrincipal` | Técnico / Sí (1.209) | `MaintenanceParticipantRole.Technician (1)`, `IsPrimary = 1`, `IsActive = 1` |
| `PlanL48.Estado` | Por confirmar (387) | `MaintenancePlan.Status = NULL` (es *nullable*) |
| `Articulos.Estado` | Activo 607 / Por confirmar 416 | Ya cargado (V3): `Status = Activo` + `ReviewStatus` |

---

## 3. Mapa por tabla

### 3.1 `Sedes` → `Campuses` ✅
`SedeCodigo`→`Code` (clave) · `NombreSede`→`Name` · `Ciudad`→`CityId` (por nombre en `Cities`: Cochabamba 1, La Paz 2, Santa Cruz 3) · `Direccion`→`Address` · `Estado`→`Status`. **Cargado (3).**

### 3.2 `Facultades` → `Faculties` ✅
`FacultadCodigo`→`Code` · `Nombre`→`Name` · `Descripcion`→`Description` · `Estado`→`Status`. **Cargado.** Restos TEC y SALUD: `Inactivo`, se conservan.

### 3.3 `Carreras` → `Careers` ✅
`CarreraCodigo`→`Code` · `Nombre`→`Name` · `FacultadCodigo`→`FacultadId` · `Estado`→`Status`. **Cargado** (LND en FGYT, con la advertencia documentada).

### 3.4 `Laboratorios` → `Laboratories` 🟡
`LaboratorioCodigo`→`Code` · `Nombre`→`Name` · `SedeCodigo`→`CampusId` · `FacultadCodigo`→`FacultyId` · `Tipo`→`Type` · `Edificio`→`Building` · `Bloque`→`Block` · `Piso`→`Floor` · `Estado`→`Status`.

- Los 24 están cargados.
- **Pendiente:** comprobar que `Type`, `Building`, `Block` y `Floor` tienen los valores del Excel en los 24 (en la Fase A solo se verificaron los códigos).
- `Piso = "Por Confirmar"` (3 laboratorios) → `Floor = NULL` + issue.
- OFICINAS 1ER PISO / PLANTA BAJA: activos con issue (D5).

### 3.5 `Gestiones` → `Managements` 🔴 (6 de 26)
`GestionCodigo`→`Code` · `TipoGestion`→`Type` · `Anio`→`Year` · `Semestre`→`Semester` · `FechaInicio`→`StartDate` · `FechaFin`→`PlannedEndDate` · `Estado`→`Status`.

- **Faltan 20:** 2014-1, 2014-2, 2015-1, 2015-2, 2016-1, 2016-2, 2017-1, 2017-2, 2018-1, 2018-2, 2019-1, 2019-2, 2020-1, 2020-2, 2021-2, 2022-1, 2022-2, 2023-1, 2023-2, 2024-1.
- Las existentes conservan su `Type`, salvo decisión en contrario.
- **`1900-1` no se carga** (`CK_Managements_Year`): las 2 filas L-8 que la usan van a `INVALID_MANAGEMENT`.
- `2027-1` existe en la BD (Inactiva) y no en el Excel: se conserva.

### 3.6 `Actores` → `People` + `Interns`/`Externs` + `PersonAliases` 🟡
`ActorCodigo`→`People.ActorCode` · `NombreRazonSocial`→`Intern.Name`/`Extern.Name` · `Titulo`→`People.Title` · `TipoActor`/`Vinculacion`→subtipo TPT (§2) · `Email`→`Email` · `Telefono`→`PhoneNumber` · `Direccion`→`Extern.Address` · `Estado`→`Status`.

- Los 51 están cargados.
- **Pendiente:** `Title` de los 7 actores de la V1 (ING./TEC.); hoy solo tienen título los 15 docentes.
- Duplicados probables, que **no se fusionan** (issue `POSSIBLE_PERSON_DUPLICATE`): 0007/0008/0016 (Sara Pérez), 0018/0019/0020 (Alex Quispe) y 0005/0010/0033 (PCS).
- Restos demo `DEMO-DOC-001…004` y los 4 actores sin código: se conservan.

### 3.7 `ActorRoles` → `PersonRoleAssignments` ✅
`ActorCodigo`→`PersonId` · `Rol`→`Role` (§2) · `Estado`→`IsActive`. 40 en el Excel; la BD tiene 43, con 3 roles previos. Verificar que los 40 del Excel existen.

### 3.8 `CatalogoEquipos` → `Equipments` 🟡 (107 de 121)
`CatalogoCodigo`→`CatalogCode` · `NombreGenerico`→`Name` · `Categoria`→`Category` · `TipoUtensilio`→`UtensilType` · `Clasificacion`→`TypeClassification` · `Marca`→`Brand` · `Modelo`→`Model` · `PaisOrigen`→`CountryId` (vacío) · `VidaUtilAnios`→`UsefulLifeYears` (vacío) · `Especificaciones`→`Description` (máx. 315 caracteres) · `Estado`→`Status` + `ClassificationReviewStatus`.

- **Faltan 15:** CAT-0108 a CAT-0122.
- CAT-0057 está solo en la BD: se conserva y se registra `LEGACY_ONLY_IN_DB`.
- **Hoy los 112 tienen `ClassificationReviewStatus = LegacyInferred` y `Category = Other`.** Pasar a `PendingClient` los que el Excel marca "Por confirmar"; el CAT con "Equipo" pasa a `Category = Equipment`.
- 9 códigos con diferencias menores de nombre o marca respecto de la BD: el Excel manda (actualizar `Name`/`Brand`), con issue `CONTENT_CONFLICT`.

### 3.9 `UnidadesFisicas` → `EquipmentUnits` 🔴 estado / 🟡 filas (556 de 644)
`NumeroInventario`→`InventoryNumber` (texto) · `CatalogoCodigo`→`EquipmentId` · `NumeroSerie`→`SerialNumber` (69 filas) · `LaboratorioCodigo`→`LaboratoryId` · `EstadoUbicacion`→`LocationResolutionStatus` · `UbicacionInterna`→`InternalLocation` (vacío) · `FechaAdquisicion`/`FechaFabricacion` (vacíos) · `ValorAdquisicionBs`→`AcquisitionValue` (536 filas) · `EstadoOperativo`→`CurrentStatus` · `CondicionFisica`→`PhysicalCondition` · `Observaciones`→`Notes` (vacío).

1. **Corregir las 547 existentes:** `CurrentStatus` y `PhysicalCondition` según el Excel (hoy las 556 están `Operational`/NULL, que es un valor inventado). Verificar también `AcquisitionValue` y `SerialNumber`.
2. **Crear 97 unidades nuevas** (necesitan los 15 catálogos nuevos).
3. **126 unidades cambian de catálogo:** actualizar `EquipmentId` y registrar una `EquipmentClassificationDecision` (`DecisionKey = V4-RECAT-{inv}`) + issue `CATALOG_REASSIGNED`.
4. 9 unidades solo en la BD (37070, 37077, 43486–43491, 45840): se conservan con `[INFERIDO-DEMO]` + `LEGACY_ONLY_IN_DB`.

### 3.10 `VerificacionesL6` → `Verifications` 🔴 (Fase E)
`VerificacionCodigo`→`HistoricalSourceKey` (formato `V4:VER-S03-R0001`) · `NumeroInventario`→`EquipmentUnitId` · `GestionCodigo`→`ManagementId` (**tal cual**, regla de negocio) · `FechaVerificacion`→`Date` · `CondicionFisica`→`PhysicalCondition` · `EstadoEquipo`→`ObservedEquipmentStatus` · `ProblemasObservados`→`Observations` (634 caracteres máx.; **no** `VerificationFault.Description`, que es de 255) · `Observaciones` (vacío) · `ResponsableActorCodigo`→`ResponsiblePersonId` · `Status = Completed (1)`.
- Requiere las gestiones 2025-2, 2026-1 y 2026-2 (ya existen). Conciliación en §4.1.

### 3.11 `SolicitudesL7` → `Requests` 🔴 (Fase E)
`SolicitudCodigo`→`HistoricalSourceKey` (`V4:SOL-…`) · `GestionCodigo`→`ManagementId` · `FechaSolicitud`→`RequestDate` (**en la BD las 520 solicitudes tienen `RequestDate` NULL**) · `Prioridad`→`Priority` · `ProblemaReportado`→`Description` · `Sugerencia`→`Suggestion` · `TiempoEstimado`→descartar · `SolicitanteActorCodigo`→`RequestedByPersonId` · `TipoSolicitud`→`Type` · `Estado`→`Status`.
- Laboratorio y unidad: si tiene 1 unidad, `EquipmentUnitId` y `LaboratoryId` salen de la unidad, con `LocationResolutionStatus = Confirmed`; si tiene varias, `LaboratoryId` solo cuando todas comparten laboratorio.
- 21 solicitudes no tienen unidades y no tienen mantenimiento: se cargan sin unidad, con `LocationResolutionStatus = Pending`.

### 3.12 `SolicitudUnidades` → `RequestEquipmentUnits` 🔴
`SolicitudCodigo`→`RequestId` · `NumeroInventario`→`EquipmentUnitId` · `IsLegacyPrimary = 1` en la primera unidad · `IsActive = 1`.
- Distribución: 284 solicitudes con 1 unidad, 5 con 4, 5 con 5 y 14 con 6.

### 3.13 `MantenimientosL8` → `Maintenances` 🔴 (157 de 1.209)
`MantenimientoCodigo`→`HistoricalSourceKey` · `NumeroInventario`→`EquipmentUnitId` · `GestionCodigo`→`ManagementId` · `FechaProgramada`→`ScheduledDate` (vacío) · `FechaInicio`→`StartDate` · `FechaFin`→`EndDate` (en las 1.209 son la misma fecha) · `TipoMantenimiento`→`MaintenanceType` · `TipoServicio`→`ServiceType` · `TrabajoRealizado`→`Description` (920 caracteres máx.) · `Estado`→`Status` · `Satisfaccion`/`Recomendaciones` (vacíos) · `ProximoMantenimiento`→`SuggestedNextMaintenanceDate` · `CostoTotalFuenteBs`→`ActualCost` (suma Bs 496.545; 23 en cero) · `EstadoCalidad`→issue.
- El técnico principal (`TechnicianId`) sale de `MantParticipantes`. `RequestId` se llena solo con el primer vínculo de `MantSolicitudes` (es 1 a 1 único); el resto va a `MaintenanceRequests`.
- 88 filas comparten unidad y fecha con otra, pero con trabajo distinto: son registros legítimos, no duplicados.

### 3.14 `MantSolicitudes` → `MaintenanceRequests` 🟡 (121 = 121)
`MantenimientoCodigo`→`MaintenanceId` · `SolicitudCodigo`→`RequestId` · `Observacion` (siempre "Relación comprobada por NumeroInventario") → no se guarda.
- 22 solicitudes tienen más de un mantenimiento: `IsLegacyPrimary` va solo en el primero.
- Los conteos coinciden con la BD, pero **hay que verificar que se trata de los mismos pares** después de conciliar L-7 y L-8.

### 3.15 `MantParticipantes` → `MaintenanceParticipants` 🔴
`ParticipanteCodigo`→no hay columna; la idempotencia es `UNIQUE(MaintenanceId, PersonId, Role)` · `MantenimientoCodigo`→`MaintenanceId` · `ActorCodigo`→`PersonId` · `Rol`→`Role` · `EsPrincipal`→`IsPrimary` · `Estado`→`IsActive`.
- **Hay organizaciones como "Técnico" principal:** FRIGOREX 244, WILDA 96, MANTENIMIENTO INDUSTRIAL 66, PCS 42+42, en total 300+ filas. `CK_MaintenanceParticipants_PrimaryRole` exige `Role = Technician` para el principal, así que se cargan como Technician (fiel al Excel) + issue `ORGANIZATION_AS_TECHNICIAN`.
- Se sincroniza `Maintenance.TechnicianId` con el principal.

### 3.16 `Costos` → `CostDetails` ⚪ (0 filas en el Excel)
Ya existe estructura (padre exclusivo). **No se crean `CostDetail` a partir de `CostoTotalFuenteBs`**: sería inventar un desglose. El costo de L-8 va a `Maintenance.ActualCost`. Los 124 `CostDetails` de la BD: ver §4.5.

### 3.17 `PlanL48` → `MaintenancePlans` 🔴 (0 de 387)
`PlanCodigo`→`PlanCode` · `GestionCodigo`→`ManagementId` (2024-2 y 2025-1) · `NumeroInventario`→`EquipmentUnitId` (+ `LaboratoryId` de la unidad) · `TipoMantenimiento`→`MaintenanceType` · `TipoServicio`→`ServiceType` · `ResponsableActorCodigo`→`ResponsiblePersonId` (1 vacío) · `FechaPlanificada`→`PlannedDate` (196) · `Estado`→`Status = NULL` · `EstadoCalidad`→issue (387).
- Hay 21 repetidos por (gestión, inventario, tipo): se cargan todos (cada uno tiene su `PlanCode`), con issue.
- **No tocar `ManagementPlans` (519, del wizard).**

### 3.18 `Materias` → `Subjects` 🔴 (0 de 19)
`MateriaCodigo`→`Code` · `Nombre`→`Name` · `CarreraCodigo`→`CareerId` · `Semestre`→`Semester` · `Estado`→`Status`.
- 19 materias: LGS 15 (incluidas las 6 homologadas de LCG, D3) y LND 4.
- Issues: `SUBJECT_CAREER_HOMOLOGATED` (6), ALI1N7 (semestre 6 frente a código 7), nombres con sufijo "LND".
- Las 8 materias demo `GAS-xxx` → `Eliminado` (PLAN §4.2).

### 3.19 `Practicas` → `Practices` ✅
`PracticaCodigo`→`Code` · `MateriaCodigo`→`SubjectId` · `NumeroPractica`→`Number` · `Descripcion`→`Description` · `Observaciones`→`Observations`. `EstadoCobertura`, `MetodoCobertura` y `FuenteReferencia` **no existen en la V7** → NULL. `Estado = Normal (0)`. `Name = LEFT(Descripcion, 200)` (regla de la migración `AddPracticeName`); si la descripción supera los 200 caracteres, `Description` conserva el texto completo y se emite `VALUE_TRUNCATED_IN_NAME` (2 filas).
**Fuente: V7 (bloque académico), cargado 2026-10-05** — 196 prácticas (`PRA-%`, salvo `PRC-*` de pruebas y `PRA-GAS%`), `Status = 0`. Lote `XLSX-V7-ACADEMIC-C677C7CC81D6`. Detalle operativo en `INSTRUCCIONES_AGENTE_V7_ACADEMICO.md` y `docs/GUIA_IMPORTACION_HISTORICA.md` §V7.

### 3.20 `ProgramacionPracticas` → `PracticeSchedule` (nueva) + `CourseGroup` derivado 🟡
Columna por columna en PLAN §3.2 y §3.3.
**Fuente: V7 (bloque académico), cargado 2026-10-05** — 422 programaciones de la gestión `2027-1`, 19 materias, 41 grupos `GRP-2027-1-*`, 15 docentes (ACT-0037…0051, todos con rol Docente), 8 laboratorios (H-01…H-07, K-01). `HoraInicio` / `HoraFin` vacías → `VALUE_PENDING_CONFIRMATION` (41, una por grupo). 33 filas con `Semana = 6` y fecha 09-03…12-03-2027 → `SCHEDULE_DATE_SUSPECT`; 2 programaciones fuera del rango de la gestión → `SCHEDULE_OUT_OF_MANAGEMENT`. `FactorMateria` varía dentro de TAL300-A y TAL300-B → `COURSE_GROUP_VALUES_VARY` (2). `AcademicWeekId` queda NULL (2027-1 no tiene `AcademicPlanning`); cuando se cree la planificación, el wizard adopta la programación importada (gestión + práctica + grupo) en lugar de crear otra (corregido el 2026-10-05 en `Pages/LaboratoryOperations/Practica.cshtml.cs`). Lote `XLSX-V7-ACADEMIC-C677C7CC81D6`.

### 3.21 `CategoriasArticulo` → `ArticleCategories` ✅ (10) · 3.22 `UnidadesMedida` → `MeasurementUnits` ✅ (3) · 3.23 `Articulos` → `Articles` ✅ (1.023)
Cargados en V3. Pendiente: retirar los restos demo (PLAN §4.3).

### 3.24 `RequerimientosPractica` → `PracticeRequirements` ✅
`RequerimientoCodigo`→`Code` (nuevo) · `PracticaCodigo`→`PracticeId` · `ArticuloCodigo`→`ArticleId` · `Cantidad`→`Quantity` (nullable) · `Escalable`→`IsScalable` · `EstadoValidacion`→`ValidationStatus` · `Observaciones`→`Observations`. `Cantidad` NULL → `VALUE_MISSING` (11). `Code` se guarda en MAYÚSCULAS (`REQ-0001…`).
**Fuente: V7 (bloque académico), cargado 2026-10-05** — 2.111 recetas (`PracticeRequirements` con `Code LIKE 'REQ-%'`, `IsRemoved = 0`), 180 de 196 prácticas, 493 artículos. Sin FK rotas ni duplicados (práctica, artículo). Estado de validación: `Validado Historico` 1.529, `Referencia - Revisar Docente` 372, `Validado Por Practica` 169, `Validado Plan` 41. Lote `XLSX-V7-ACADEMIC-C677C7CC81D6`.

### 3.25 `ExistenciasArticulos` → `ArticleStocks` 🟡
`LaboratorioCodigo`+`ArticuloCodigo`→clave única (`LaboratoryId`, `ArticleId`) · `CantidadDisponible`→`AvailableQuantity` · `FechaActualizacion`→`UpdatedOn`. Solo se cargan las filas con `LaboratorioCodigo` **y** `CantidadDisponible`; las 843 filas vacías se descartan (no se emiten incidencias por ellas).
**Fuente: V7 (bloque académico), cargado 2026-10-05** — 220 existencias nuevas (1.063 leídas). `ArticleStocks` total: 228 (8 previas + 220). Por laboratorio: ECONO 122, H-03 26, K-01 16, H-06 14, H-01 13, H-05 12, H-04 11, H-02 8, H-07 6. **Disponibilidad (D12, aplicado el 2026-10-05):** `ArticleAvailabilityService` compara por defecto contra el total institucional (ECONO + laboratorios); el selector de origen permite ver un solo laboratorio. Tras la carga, 83 de 493 artículos de receta tienen stock en algún laboratorio (49 solo en ECONO, 410 sin stock en ninguno). No se modifica en esta carga. Lote `XLSX-V7-ACADEMIC-C677C7CC81D6`.

### 3.26–3.31 Circuito L-3 → `L3Requests`, `L3RequestDetails`, `Departures`, `DepartureItems`, `DepartureReturns`, `DepartureReturnItems` ⚪
Columna por columna en PLAN §3.4. Todas vacías en el Excel.

---

## 4. Conciliación histórica (Fase E): números reales

### 4.1 L-6: Excel 1.599 vs BD 1.020 (+562 en cuarentena)
- **Las fechas no coinciden casi nunca.** El Excel usa solo 3 fechas de documento (2025-05-16, 2025-10-17, 2026-06-08); la BD usa fechas operativas (2026-07-10, 2024-11-04, 2025-05-09…). Por (inventario, fecha exacta) solo coinciden 8.
- **Regla de emparejamiento:** mismo inventario y la fecha más cercana dentro de ±60 días, uno a uno. Resultado:

| Resultado | Filas | Acción |
|---|---:|---|
| Excel con par en la BD | **685** (485 reales + 200 inferidos) | Actualizar el registro de la BD con los valores del Excel. `HistoricalSourceKey` se conserva; la clave V4 se registra en `ImportSourceRows`. Si es inferido → `LEGACY_INFERRED_SUPERSEDED` |
| Solo en el Excel | **914** | Crear |
| BD sin par | **335** (292 inferidos + 43 reales) | Se conservan: `LEGACY_INFERRED_UNMATCHED` o `LEGACY_ONLY_IN_DB` |

- La gestión del Excel va corrida un semestre: se toma la del Excel (regla) y, si difiere de la BD, issue `MANAGEMENT_SHIFTED_BY_RULE`.
- Las **562 L-6 en cuarentena** (`HistoricalVerificationQuarantines`) se reevalúan: las que ahora tienen inventario en el Excel se resuelven.

### 4.2 L-7: Excel 329 (413 vínculos) vs BD 520 (1 unidad cada una)
- La BD **partió** cada solicitud con varias unidades en varias filas `REQ_Rnn_*` (303 grupos reales) y además tiene **140 `INF_REQ` inferidas**.
- **Emparejar por conjunto de unidades:** **280 de los 303 grupos coinciden exactamente** con una solicitud del Excel.
  - La solicitud del Excel queda como la canónica: se actualiza la primera fila del grupo y las demás del grupo se vinculan a ella vía `RequestEquipmentUnits`.
  - **No se borran:** se marcan con issue `LEGACY_SPLIT_MERGED`. Ojo: anularlas (`Status = Cancelled`) cambiaría las pantallas, así que eso requiere decisión.
- 23 grupos sin coincidencia exacta y 49 solicitudes del Excel sin par: revisar manualmente (exportar CSV).
- 140 `INF_REQ`: `LEGACY_INFERRED_UNMATCHED`, salvo que hagan par.

### 4.3 L-8: Excel 1.209 vs BD 157
- Emparejamiento: mismo inventario, `EndDate` a ±15 días → **123 pares**; **1.086 solo en el Excel** (crear); **34 de la BD sin par** (conservar).

### 4.4 L-48: Excel 387 vs BD 0 en `MaintenancePlans`
Carga directa. No hay conciliación.

### 4.5 Lo inferido que ya está en la BD (decisión del usuario requerida)

| Tabla | Inferido | Detalle |
|---|---:|---|
| `Verifications` | 492 `INF_VER` | 200 tienen par en el Excel; 292 no |
| `Requests` | 140 `INF_REQ` | "Solicitud técnica preventiva inferida para completar la cadena" |
| `Maintenances` | 1 `INF_M…` | |
| `CostDetails` | **120 de 124** ("Servicio preventivo histórico inferido", Bs 22.020) | Los 4 restantes (Bs 880) parecen de prueba ("O-Ring… (Kardex)") |

**Corrección a mi informe anterior:** dije "no perder los 124 costos (Bs 22.900)". En realidad **casi todos son inferidos**; el costo real de L-8 viene del Excel (`CostoTotalFuenteBs`, Bs 496.545).

**Recomendación:** no borrar. Marcar con issue y, con la aprobación del usuario, anular (`Verification.Status = Annulled`, `Request.Status = Cancelled`). `CostDetail` no tiene estado: habría que agregar `IsVoided` en una migración o dejarlos con issue.

**Pregunta pendiente para el usuario:** ¿se anulan los inferidos que el Excel reemplaza o contradice?

### 4.6 Derivados que hay que regenerar después de la Fase E
- `ManagementPlans` (519, wizard) y `EquipmentStateHistories` (121, Kardex) se construyeron sobre el histórico anterior. Tras la carga, el Kardex y el dashboard tienen que reflejar los 1.209 L-8.
- Revisar si `EquipmentKardexSummaryBuilder` calcula a partir de `Maintenances`; si es así, no hace falta regenerar nada.

---

## 5. Longitudes (todas caben)

| Excel | Máx. | Columna BD | Límite |
|---|---:|---|---:|
| `CatalogoEquipos.Especificaciones` | 315 | `Equipment.Description` | 2000 |
| `UnidadesFisicas.NumeroSerie` | 28 | `SerialNumber` | 100 |
| `VerificacionesL6.ProblemasObservados` | 634 | `Verification.Observations` | max |
| `SolicitudesL7.ProblemaReportado` / `Sugerencia` | 634 / 864 | `Request.Description` / `Suggestion` | 1000 / 2000 |
| `MantenimientosL8.TrabajoRealizado` | 920 | `Maintenance.Description` | 2000 |
| `Actores.NombreRazonSocial` | 53 | `Intern.Name` / `Extern.Name` | 100 / 200 |
| `Articulos.Nombre` | 92 | `Article.Name` | 200 |

---

## 6. Herramienta y verificación

- Generadores en `Tools/ExcelImport/`, uno por bloque:
  - `gen_v4_master_fix_sql.py`: §3.4–3.9.
  - `gen_v4_academic_sql.py`: §3.18–3.25.
  - `gen_v4_history_sql.py`: §3.10–3.17 + §4.
- Cada uno valida los encabezados contra este mapa y se detiene si la hoja cambió.
- El script de históricos produce primero **`conciliacion_v4.csv`** (una fila por emparejamiento, con su resultado) **sin escribir nada**. El usuario lo revisa y después se ejecuta.
- Criterios de aceptación:
  - Conteos por tabla iguales a los de §3/§4.
  - 0 huérfanos.
  - `DBCC CHECKCONSTRAINTS` limpio.
  - Segunda pasada con 0 inserciones.
  - Suma de `ActualCost` = Bs 496.545 sobre las filas V4.
  - Ningún `Operational` en las unidades que el Excel marca "Por confirmar".
- Recordatorios: MiniMax no levanta servidores; comprobar que `L3Requests` está vacía antes de cambiar `L3RequestDetail`; la fuente de utensilios 1-2027 sigue sin llegar.
