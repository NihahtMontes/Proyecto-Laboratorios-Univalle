# Informe: `BaseEstructuraFinal.xlsx` vs base de datos actual

**Fecha:** 2026-10-04 · **Rama:** `integration/planificacion-2026-10`
**Alcance:** análisis de solo lectura. No se modificó código, migraciones ni datos.

Fuentes comparadas:

| Lado | Fuente | Detalle |
|---|---|---|
| Excel objetivo | `BaseEstructuraFinal.xlsx` (generado 2026-10-03 desde `BASE_LIMPIA_SUPABASE_CORREGIDA.xlsx`) | 32 tablas, `_DICCIONARIO` (PK/FK/tipos PostgreSQL), `_VERIFICACION` |
| Esquema actual | `ApplicationDbContextModelSnapshot.cs` + `ApplicationDbContext.cs` + `Models/` (incluye cambios sin commitear de planificación) | 51 tablas EF (6 de Identity) |
| Datos actuales | SQL Server local `DB_Laboratorios_Univalle` (última migración `20260830202548`) y `DB_Laboratorios_Univalle_DEMO_QA` (`20261004022804`) | Solo consultas `SELECT` |

---

## 1. Resumen ejecutivo

1. **El Excel cubre bien el núcleo histórico** (institucional, actores, catálogo, unidades, L-6, L-7, L-8, costos, L-48): 15 de sus 32 tablas tienen datos y casi todas tienen una tabla equivalente en la BD. Los códigos naturales (`CAT-xxxx`, nº de inventario, `ACT-xxxx`, `AAAA-S`) **coinciden** con los que ya guarda la BD.
2. **El diseño de base es distinto:** el Excel usa **códigos de texto como clave primaria** y tipos **PostgreSQL** (pensado para Supabase). La BD usa **`int IDENTITY` + código natural único**, enums enteros con `CHECK`, auditoría, `RowVersion` y soft-delete, sobre **SQL Server**. No hay que copiar la estructura tal cual: hay que **mapearla**.
3. **Los módulos nuevos (académico, artículos, circuito L-3) son donde más difiere.** El Excel trae 11 tablas vacías que modelan cosas que hoy la BD no tiene o simplifica mucho: `Sedes`, `Practicas`, `ProgramacionPracticas`, `RequerimientosPractica`, `CategoriasArticulo`, `UnidadesMedida`, `ExistenciasArticulos`, `DevolucionesL3` y grupos con horario/docente.
4. **Diferencia de concepto en L-3:** en el Excel, L-3 pide **artículos** (`ArticuloCodigo` obligatorio). En la BD, `L3RequestDetail` pide **unidades físicas** (`EquipmentUnitId` obligatorio). Es la decisión de diseño más importante que queda pendiente.
5. **Hay bloqueos de carga:** valores `Por confirmar` / `Sin especificar` que no caben en columnas `NOT NULL` con enum (unidades, mantenimientos, solicitudes); la gestión `1900-1`, que choca con `CK_Managements_Year`, y la gestión de L-6/L-7 corrida un semestre respecto de la fecha.
6. **El Excel no trae datos que la BD sí tiene:** 124 costos (Bs 22.900), 121 movimientos de Kardex, 519 planes del wizard y la trazabilidad de importación. Si el Excel se usara como "la base", estos datos **se perderían**.

**Recomendación:** usar el Excel como **contrato funcional y de datos**, no como esquema físico. Hay que conservar el modelo EF (ids enteros + códigos únicos), cerrar los huecos con migraciones aditivas y resolver con la cliente las decisiones de la sección 7 antes de cargar nada.

---

## 2. Diferencias estructurales globales

| Tema | Excel | BD actual | Impacto |
|---|---|---|---|
| Motor | Tipos PostgreSQL (`text`, `boolean`, `timestamp`, `numeric`) | SQL Server (`nvarchar`, `bit`, `datetime2`, `decimal`) | Hay que confirmar si existe un plan de migrar a Supabase/PostgreSQL. Si no, los tipos son solo orientativos |
| Clave primaria | Código de texto (`varchar(50)`) | `int`/`bigint IDENTITY` + código natural con `UNIQUE` filtrado | Conviene mantener ids enteros y resolver FKs por código durante la importación (lo que ya contempla `HistoricalDataApply`) |
| Nº de inventario | `integer` | `nvarchar(50)` | Mantener texto: es más seguro (ceros a la izquierda, sufijos) |
| Estados y catálogos | Texto libre (`Activo`, `Por confirmar`, `Sin especificar`...) | Enums `int` con `CHECK` | Hace falta una tabla de mapeo texto→enum y un valor para "desconocido" donde no existe |
| Auditoría/concurrencia | No hay | `CreatedBy/Date`, `ModifiedBy/Date`, `RowVersion` | Solo existe en la BD; se conserva |
| Soft-delete | `Estado` textual | `Status`/`IsDeleted`/`IsActive` + query filters | Solo existe en la BD; se conserva |
| Fechas L-8 | Guardadas como **texto** `AAAA-MM-DD` (1.209 × 3 columnas) | `datetime2` | Hay que convertirlas al importar |

---

## 3. Mapeo tabla por tabla (32 tablas del Excel)

Leyenda: ✅ equivalente directo · 🟡 existe con diferencias · 🔴 no existe en la BD

### 3.1 Institucional

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 1 | `Sedes` (3: LPZ, CBBA, SCZ) | — (solo `Cities`/`Countries`; `Laboratory.CityId`) | 🔴 | Falta la entidad Sede/Campus (código, nombre, ciudad, dirección). `Laboratorios.SedeCodigo` es FK obligatoria en el Excel |
| 2 | `Facultades` (2: FGYT, FCSN) | `Faculties` (4: GASTRO, NUTRI, TEC, SALUD) | 🟡 | **Los códigos son distintos.** `Faculties.Code` no tiene índice único. TEC y SALUD parecen semilla demo |
| 3 | `Carreras` (4: LGS, LTH, LND, LCG) | `Careers` (6, códigos `CAR-000x`) | 🟡 | Códigos distintos. **LND (Nutrición):** el Excel la pone en FGYT y la BD en NUTRI/Salud (el propio Excel lo marca como advertencia). La BD tiene "Gastronomía" duplicada y "Bioquímica" (demo). La FK se llama `FacultadId` y es *nullable* (en el Excel es NOT NULL) |
| 4 | `Laboratorios` (24) | `Laboratories` (17) | 🟡 | **Códigos no normalizados:** `H-01`↔`H-1`, `K-01`↔`K-1`, `CIRC-GEN`↔`CIRCULACION`. Faltan en la BD H-105/106/107, K-02..K-06, ECONO, SALA-DOC, OFI-COORD y CAS-VES. La BD tiene códigos que el Excel no tiene: `OFICINAS ...`, `PENDIENTE`, `LAB-GAS-01` y `LAB-BIO-01` (demo). La BD ya tiene `Building/Block/Floor`; le falta `SedeId` |
| 5 | `Gestiones` (26: 2014-1 a 2026-2 + `1900-1`) | `Managements` (5: 2024-2 a 2026-2) | 🟡 | El formato del código `AAAA-S` coincide. **`1900-1` viola `CK_Managements_Year` (2000-2100)** y la usan 2 mantenimientos. **Falta `2027-1`**, necesaria para la planificación. `TipoGestion` = "Por confirmar" en todas, mientras que la BD exige `Type` Preventiva/Correctiva y una sola gestión activa |

### 3.2 Actores

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 6 | `Actores` (36) | `People` + `Interns`/`Externs` (TPT, 28; 24 con `ActorCode`) | 🟡 | **Faltan 12 actores en la BD:** ACT-0008, **ACT-0009** (lo usan L-6 y L-7) y ACT-0027 a ACT-0036. **Columna nueva `Titulo`** sin equivalente. `TipoActor` corresponde a `Extern.IsEntity`, y `Vinculacion` (Interno/Externo), a la subclase TPT. Hay 11 actores con tipo "Por confirmar". Posibles duplicados señalados por el Excel: 0008/0016 (Sara Pérez), 0018/0019/0020 (Alex Quispe), 0005/0010/0033, 0007/0008/0016. Los nombres agrupados (ACT-0022, 0024, 0025, 0036) representan varias personas en un solo registro |
| 7 | `ActorRoles` (25) | `PersonRoleAssignments` (26) | 🟡 | Correspondencia: Técnico→1, Proveedor→3, Solicitante→5, Responsable→6, Verificador→7. **Falta el rol Docente** en `PersonOperationalRole` (existe como `PersonCategory`, no como rol), y `GruposMateria`/`SolicitudesL3` lo necesitan |

### 3.3 Inventario físico

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 8 | `CatalogoEquipos` (121) | `Equipments` (107 con código; 112 filas) | 🟡 | 106 códigos en común: **97 idénticos** en nombre y marca, 9 con diferencias menores (la marca falta en la BD o hay variantes del nombre). **Nuevos en el Excel:** CAT-0108 a CAT-0122. **Solo en la BD:** CAT-0057. `Categoria`/`TipoUtensilio`/`Clasificacion` vienen como texto con "Por confirmar"; la BD usa enums con `ClassificationReviewStatus`. `PaisOrigen` (texto) corresponde a `CountryId`. `Estado` = "Por confirmar" en las 121 filas |
| 9 | `UnidadesFisicas` (644) | `EquipmentUnits` (556) | 🟡 | 547 en común, **97 nuevas** en el Excel, 9 solo en la BD (37070, 37077, 43486-43491, 45840). **126 unidades cambian de catálogo** (el Excel subdivide el catálogo). **El Excel asigna laboratorio a las 644; en la BD las 556 tienen `LaboratoryId` NULL.** Es un aporte importante, pero el Excel marca `EstadoUbicacion` = "Pendiente" en todas, lo que se contradice con tener laboratorio asignado. **595 unidades con `EstadoOperativo` = "Por confirmar"**: no caben en `CurrentStatus` (NOT NULL, sin valor desconocido). `ValorAdquisicionBs` es `integer` en el Excel y `decimal(18,2)` en la BD |

### 3.4 Verificación y mantenimiento

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 10 | `VerificacionesL6` (1.599) | `Verifications` (1.020) + `HistoricalVerificationQuarantines` (562) | 🟡 | `VerificacionCodigo` corresponde a `HistoricalSourceKey`, pero el formato es distinto (`VER-S03-R0001` vs `INF_VER_20242_2791`), así que la conciliación no puede ser por clave. `ProblemasObservados` va a `Observations`/`VerificationFaults`. **El 100% de las filas tiene la gestión un semestre por delante de la fecha** (ver §5.1) |
| 11 | `SolicitudesL7` (329) | `Requests` (520) | 🟡 | **El Excel tiene menos filas que la BD.** `Prioridad` = "Sin especificar" en todas (`Priority` es NOT NULL, 0-3). `Estado` = "Por confirmar" en todas. `TiempoEstimado` = "XXX"/"xx" (basura). Las longitudes caben (problema ≤ 634, sugerencia ≤ 864) |
| 12 | `SolicitudUnidades` (413) | `RequestEquipmentUnits` (520) | ✅ | Tabla puente con la misma unicidad (Solicitud, Unidad) |
| 13 | `MantenimientosL8` (1.209, 2014–2026) | `Maintenances` (157) | 🟡 | **El Excel trae 8 veces más historia.** `TipoServicio` = "Por confirmar" en **1.204** filas (`ServiceType` es NOT NULL 0/1). `TipoMantenimiento` = "Por confirmar" en 44. `EstadoCalidad` (Válida 432 / Requiere revisión 777) no tiene columna: va a `DataQualityIssues`. `CostoTotalFuenteBs` corresponde a `ActualCost`, y `ProximoMantenimiento`, a `SuggestedNextMaintenanceDate`. Tiene 2 filas en la gestión `1900-1` |
| 14 | `MantSolicitudes` (121) | `MaintenanceRequests` (121) | ✅ | Los conteos coinciden |
| 15 | `MantParticipantes` (1.209) | `MaintenanceParticipants` (157) | ✅ | Todos son Técnico principal. Hay organizaciones (p. ej., CASATERMO SRL) registradas como "Técnico"; semánticamente serían `ExternalProvider` |
| 16 | `Costos` (**0**) | `CostDetails` (**124**, Bs 22.900) | 🟡 | **El Excel pierde los 124 costos.** Misma regla de padre exclusivo (Solicitud o Mantenimiento) en ambos. El Excel conserva columnas extra de origen (`CentroCostoOrigen`, etc.) |
| 17 | `PlanL48` (387; 2024-2 y 2025-1) | `MaintenancePlans` (0) / `ManagementPlans` (519) | 🟡 | La guía de importación indica que L-48 va a `MaintenancePlans`. `FechaPlanificada` solo viene en 196 filas. `EstadoCalidad` = "Requiere revisión" en las 387 |

### 3.5 Académico (tablas NUEVAS, vacías)

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 18 | `Materias` (Código, Nombre, Carrera, Semestre, Estado) | `Subjects` (Id, Name, Code) | 🟡 | Faltan `CareerId`, `Semestre` y `Status`. **`Code` no tiene índice único.** El Excel exige `UNIQUE(Carrera, Nombre)` |
| 19 | `GruposMateria` (gestión, materia, grupo, docente, laboratorio, día, horas, nº de estudiantes, demostrativa, factor) | `Groups` (Id, Name) | 🔴 | En la BD `Groups` es solo una lista de nombres. En el Excel es la **sección de una materia en una gestión**, con docente, horario y factor de escalamiento |
| 20 | `Practicas` (materia, nº, descripción, cobertura) | — (`L3Request.PracticeName` texto) | 🔴 | Falta el catálogo de prácticas por materia |
| 21 | `ProgramacionPracticas` (grupo, práctica, semana, fecha) | Parcialmente en `L3Request` + `AcademicWeeks` | 🟡 | En el Excel, **programar** una práctica y **pedir** los insumos (L-3) son tablas separadas. En la BD, `L3Request` hace las dos cosas. `Semana` es un entero "no deducir de la fecha". La BD tiene `AcademicPlannings`/`AcademicWeeks` (gestión + carrera + semanas), que el Excel no modela: es una extensión válida de la app |

### 3.6 Artículos y utensilios

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 22 | `CategoriasArticulo` | `Article.Category` (texto) | 🔴 | Falta la tabla catálogo |
| 23 | `UnidadesMedida` | `Article.UnitOfMeasure`, `DepartureItem.UnitOfMeasure` (texto) | 🔴 | Falta la tabla catálogo |
| 24 | `Articulos` (+ `TipoArticulo`, `CatalogoCodigo`) | `Articles` (0 filas) | 🟡 | Faltan `TipoArticulo` (EQUIPO/UTENSILIO), la FK opcional a `Equipments` y `UNIQUE(Nombre)` |
| 25 | `RequerimientosPractica` (práctica, artículo, cantidad, escalable) | — | 🔴 | Falta la "receta" de insumos por práctica, que es la base para calcular L-3 automáticamente |
| 26 | `ExistenciasArticulos` (laboratorio, artículo, stock) | — | 🔴 | No hay stock por laboratorio. El Excel eliminó `Almacenes` y usa `ECONO` (Economato) como laboratorio de tipo almacén |

### 3.7 Circuito L-3 (todas vacías)

| # | Excel | BD | Estado | Diferencias clave |
|---|---|---|---|---|
| 27 | `SolicitudesL3` | `L3Requests` | 🟡 | El Excel tiene `GestionCodigo` (en la BD se llega a la gestión indirectamente, vía `AcademicPlanning`), `ProgramacionCodigo` (NULL = extraordinaria), `FechaSolicitud` (regla de 24 h), `SedeCodigo`, `CarreraCodigo`, `TodosLosLaboratorios` y laboratorio *nullable* (en la BD es NOT NULL), además de `HoraInicio/HoraFin`, `NumeroPractica` y `Asignatura`/`GrupoParalelo` **como texto**. El docente es un Actor; en la BD es `TeacherPersonId` + `TeacherId` (User). En la BD `Status` es texto libre sin `CHECK` |
| 28 | `SolicitudL3Detalles` | `L3RequestDetails` | 🔴 concepto | **El Excel pide artículos (`ArticuloCodigo` NOT NULL, inventario opcional); la BD pide unidades (`EquipmentUnitId` NOT NULL).** Cantidad: `numeric(12,2)` frente a `int` |
| 29 | `SalidasL3` | `Departures` | 🟡 | Encaja bien (modalidad→`Type`, origen, destino, responsable, fecha). Falta la FK a la solicitud L-3. `RetornoEstimado` es texto en el Excel y fecha en la BD |
| 30 | `SalidaDetalles` | `DepartureItems` | ✅ | Misma regla (unidad o artículo). Cantidad decimal frente a int. La unidad de medida debe pasar a ser una FK |
| 31 | `DevolucionesL3` | — (`Departure.ActualReturnDate`/`ReturnObservations`) | 🔴 | El Excel permite **varias devoluciones parciales** por salida, con responsable y estado |
| 32 | `DevolucionL3Detalles` | — (`DepartureItem.ReturnedQuantity`, acumulado) | 🔴 | Se pierde el historial de devoluciones parciales |

---

## 4. Lo que la BD tiene y el Excel no (no se debe perder)

| Tabla BD | Filas | Por qué importa |
|---|---|---|
| `CostDetails` | 124 (Bs 22.900) | El Excel trae `Costos` vacío |
| `EquipmentStateHistories` (Kardex) | 121 | Historial de estado; no existe en el Excel |
| `ManagementPlans` (wizard) | 519 | Es el motor del flujo L-6→L-12 |
| `HistoricalVerificationQuarantines` | 562 | Evidencia de las L-6 no conciliadas |
| `ImportBatches`, `ImportSourceRows`, `DataQualityIssues` | 3 / 158 / 1.289 | Trazabilidad y gobierno de datos |
| `PersonAliases` | 26 | Resolución de nombres de actores |
| `EquipmentClassificationDecisions`, `EquipmentNotes`, `MaintenanceTasks`, `VerificationCheckItems/Results`, `VerificationFaults`, `L5Incidents`, `Notifications`, `AcademicPlannings/Weeks`, Identity | — | Funcionalidad propia de la aplicación |

---

## 5. Problemas de calidad de datos detectados en el Excel

1. **Gestión corrida en L-6/L-7.** Las 1.599 verificaciones y 316 de las 329 solicitudes tienen fechas que caen en el semestre **anterior** a su gestión: mayo-2025 → `2025-2`, octubre-2025 → `2026-1`, junio-2026 → `2026-2`. En L-8 las fechas cuadran al 100%. Puede ser una regla de negocio (se verifica al final de un semestre para mantener en el siguiente) o un error. **Hay que confirmarlo con la cliente.** También explica los 528 conflictos de fecha que ya registró la conciliación V2.
2. **`1900-1`** es una gestión basura; la usan 2 mantenimientos y la rechaza `CK_Managements_Year`.
3. **Falta `2027-1`**, necesaria para la planificación académica.
4. **"Por confirmar" masivo** en columnas que la BD exige: unidades (`EstadoOperativo` 595/644, `CondicionFisica` 550/644), L-8 (`TipoServicio` 1.204/1.209), L-7 (`Prioridad` y `Estado` 329/329) y catálogo (`Estado` 121/121). La guía vigente prohíbe convertir "Por confirmar" en un valor real.
5. **Ubicación contradictoria:** el 100% de las unidades tiene `LaboratorioCodigo` y, a la vez, `EstadoUbicacion` = "Pendiente".
6. **Actores duplicados o agrupados** (ver §3.2) y 12 actores ausentes de la BD.
7. **Basura en `TiempoEstimado`** ("XXX", "xx", "XX").
8. **Carrera LND** asignada a la facultad que no corresponde, o bien FCSN queda sin carreras.
9. **26 ítems de catálogo sin ninguna unidad.**
10. **Formato de las claves históricas** (`VER-S03-R0001`, `SOL-S04-R...`) distinto del de la BD (`INF_VER_20242_2791`, `MNT_R6_34179`), por lo que la conciliación no puede hacerse por clave.

---

## 6. Conteos comparados

| Entidad | Excel | BD (`DB_Laboratorios_Univalle`) | Diferencia |
|---|---:|---:|---|
| Facultades / Carreras | 2 / 4 | 4 / 6 | La BD incluye semillas demo |
| Laboratorios | 24 | 17 | Códigos con otro formato |
| Gestiones | 26 | 5 | Excel +21 (historia 2014-2024) |
| Actores | 36 | 28 | Excel +12 |
| Catálogo | 121 | 107 (+5 sin código) | Excel +15 / −1 |
| Unidades | 644 | 556 | +97 / −9; 126 cambian de catálogo |
| Verificaciones L-6 | 1.599 | 1.020 + 562 en cuarentena | Excel +17 sobre el total |
| Solicitudes L-7 | 329 | 520 | **Excel −191** |
| Solicitud-unidad | 413 | 520 | Excel −107 |
| Mantenimientos L-8 | 1.209 | 157 | **Excel +1.052** |
| Mant-solicitud | 121 | 121 | = |
| Costos | 0 | 124 | **Excel −124** |
| Plan L-48 | 387 | 0 (+519 en el wizard) | — |

> `DB_Laboratorios_Univalle` no tiene aplicadas las migraciones de planificación académica. `DB_Laboratorios_Univalle_DEMO_QA` sí: tiene 1 `AcademicPlanning`, 19 semanas, 8 materias y 4 grupos.

---

## 7. Decisiones que necesitan respuesta (cliente / equipo)

1. **Motor destino:** ¿se sigue con SQL Server (EF actual) o hay un plan de pasar a Supabase/PostgreSQL? El Excel está tipado para PostgreSQL.
2. **L-3 por artículo o por unidad física:** el Excel dice artículo; la BD dice unidad. ¿Se admiten ambos (como ya hace `DepartureItem`)?
3. **Gestión de L-6/L-7:** ¿el desfase de un semestre es una regla de negocio o un error?
4. **Códigos maestros:** ¿cuál es el oficial para facultades, carreras y laboratorios (`FGYT`/`LGS`/`H-01` del Excel o los actuales `GASTRO`/`CAR-0001`/`H-1`)?
5. **Valor "desconocido":** ¿se agrega un valor `PorConfirmar` a los enums (estado operativo, tipo de servicio, prioridad, estado de solicitud) o se vuelven *nullable* esas columnas?
6. **Fuente de verdad de solicitudes y costos:** el Excel tiene 191 solicitudes y 124 costos menos que la BD. ¿Se descartaron a propósito o faltan en el Excel?
7. **Ubicación de unidades:** ¿se aceptan como confirmados los laboratorios del Excel aunque digan "Pendiente"?
8. **Duplicados de actores:** fusionar o mantener separados (0008/0016, 0018/0019/0020, 0005/0010/0033, 0007/0008/0016).
9. **LND:** ¿FGYT o FCSN?
10. **¿Se crea la gestión `2027-1`** y se elimina o reasigna `1900-1`?

---

## 8. Plan de alineación propuesto (por fases, migraciones aditivas)

> Toca más de 5 archivos: cada fase requiere aprobación antes de editar. No se modifican migraciones existentes.

**Fase A: maestros (bajo riesgo)**
- Nueva entidad `Campus` (Sede): `Code`, `Name`, `CityId`, `Address`, `Status`; `Laboratory.CampusId` nullable.
- Índices únicos filtrados en `Faculty.Code` y `Subject.Code`.
- `Person.Title` (Titulo) nullable.
- Rol `Docente` en `PersonOperationalRole`.
- Normalizar los códigos de laboratorio (requiere la decisión 4).

**Fase B: académico**
- `Subject` + `CareerId`, `Semester`, `Status`; `UNIQUE(CareerId, Name)`.
- Nueva `CourseGroup` (GruposMateria): `ManagementId`, `SubjectId`, `Name`, `TeacherPersonId`, `LaboratoryId`, día, horas, nº de estudiantes, demostrativa, factor. `Groups` se mantiene como catálogo legacy o se migra.
- Nuevas `Practice` (Practicas) y `PracticeSchedule` (ProgramacionPracticas), integradas con `AcademicPlanning`/`AcademicWeeks`.

**Fase C: artículos e inventario consumible**
- Nuevas `ArticleCategory` y `UnitOfMeasure`; `Article` + `CategoryId`, `UnitOfMeasureId`, `ArticleType`, `EquipmentId?`, `UNIQUE(Name)`.
- Nuevas `PracticeRequirement` (RequerimientosPractica) y `ArticleStock` (ExistenciasArticulos, PK laboratorio + artículo).

**Fase D: circuito L-3 (requiere la decisión 2)**
- `L3Request`: `ManagementId`, `PracticeScheduleId?`, `RequestDate`, `CampusId?`, `CareerId?`, `AllLaboratories`, `LaboratoryId` nullable con `CHECK`, `CHECK` sobre `Status`.
- `L3RequestDetail`: `ArticleId?` + `EquipmentUnitId?` con `CHECK` "exactamente uno"; cantidad decimal.
- `Departure.L3RequestId?`; nuevas `DepartureReturn` y `DepartureReturnItem` (devoluciones parciales).

**Fase E: carga histórica (sobre QA, con `HistoricalDataImport`/`HistoricalDataApply`)**
- Mapa de valores texto→enum y tratamiento de "Por confirmar" según la decisión 5.
- Conciliación por clave natural: inventario, `CatalogCode`, `ActorCode`, código de gestión. Para L-6/L-7/L-8, la conciliación tiene que ser por contenido, porque las claves de origen difieren.
- Nunca borrar lo que está solo en la BD (costos, Kardex, wizard).

---

## 9. Evidencia y método

- Excel: perfil completo de las 34 hojas (32 tablas + `_DICCIONARIO` + `_VERIFICACION`) con `openpyxl`.
- Esquema: inventario de las 51 tablas EF hecho por MiniMax (mm-explorer) y verificado por muestreo por Claude (`FacultadId` en `Career`, ausencia de entidad Sede, `L3RequestDetail.EquipmentUnitId`, índices de `Subject`/`Faculty`).
- Datos: solo consultas `SELECT` en las BD locales `DB_Laboratorios_Univalle` y `..._DEMO_QA`. No se escribió nada.
- Cruces calculados: inventarios, códigos de catálogo (nombre y marca normalizados), laboratorios por unidad, coherencia fecha↔gestión, distribución de "Por confirmar", actores ausentes.
