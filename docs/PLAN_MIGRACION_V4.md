# Plan de migración puntual: `BaseEstructuraFinal_V4.xlsx`

**Fecha:** 2026-10-04 · **Rama:** `integration/planificacion-2026-10` (HEAD `d23d43a`)
**Análisis hecho en solo lectura:** Excel con `openpyxl` (V3 y V4 comparadas celda por celda) + `SELECT` sobre `DB_Laboratorios_Univalle_DEMO_QA` (última migración `20261004054609_ArticleReviewStatus`) + revisión de `Models/` y `ApplicationDbContext.cs`.

| Archivo | SHA-256 |
|---|---|
| `BaseEstructuraFinal_V3_ARTICULOS.xlsx` | `1e70f5e4cbf9b298ee3690bfc65e4a0aec07efae109ca398aa10b774ca87680e` |
| `BaseEstructuraFinal_V4.xlsx` | `8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2` |

---

## 0. Entrega para el agente: decisiones tomadas y avisos (2026-10-04)

**Punto de partida:**

- Rama `integration/planificacion-2026-10`, commit `d23d43a`, sin push. Los cambios solo se aplicaron en `DB_Laboratorios_Univalle_DEMO_QA`.
- Plan de trabajo detallado (migración, carga V4, wizard por artículo y verificación): `C:\Users\monte\.claude\plans\quiero-que-puebles-mi-lovely-wind.md`. Se apoya en este documento.

**Decisiones del usuario (sustituyen a las recomendaciones de §6):**

| # | Decisión |
|---|---|
| D1 | Los 416 artículos "Por confirmar" **se pueden pedir en L-3**, mostrando la etiqueta |
| D2 | El stock se valida contra el **laboratorio de origen de la salida (Economato por defecto)**. La solicitud L-3 **solo advierte**; la salida descuenta y bloquea si no alcanza; la devolución repone |
| D2b | **Artículo sin registro de stock:** la salida se permite **sin descontar** y queda marcada (`DataQualityIssue` `STOCK_NOT_REGISTERED`) |
| D3 | Las 6 materias que venían como LCG **se cargan como LGS** (igual que en la V4), con una incidencia (`SUBJECT_CAREER_HOMOLOGATED`) para confirmar con la cliente |
| D4 | Se **conservan los grupos de materia (`CourseGroup`)** derivados de las programaciones (§3.3) |
| D5 | Los laboratorios `OFICINAS 1ER PISO` y `OFICINAS PLANTA BAJA` **quedan activos**, con una incidencia |

**Avisos obligatorios:**

1. **Antes de cambiar `L3RequestDetail`** (`ArticleId` NOT NULL, `EquipmentUnitId` nullable), comprobar con `SELECT COUNT(*)` que `L3Requests` y `L3RequestDetails` **siguen vacías** en la base destino. Si no lo están, detenerse y reportarlo: la migración asume tablas vacías.
2. **MiniMax no debe levantar servidores** (`dotnet run`, `dotnet watch`, navegador). En Windows la herramienta bash de OpenCode se queda colgada esperando al proceso. Las pruebas HTTP con servidor las hace el orquestador.
3. **Sigue sin llegar** `BASE_NORMALIZADA_UTENSILIOS_1-2027_COMPLETADA.xlsx` de la cliente (programaciones, recetas y stock reales). La estructura y los importadores se dejan listos, pero no hay que inventar datos para esas tablas.

---

**Objetivo:** una sola migración aditiva (`AlignStructuralBaseV4`) más una carga idempotente en DEMO_QA que dejen la BD capaz de recibir **todas** las hojas de la V4. Hoy no es así, por los huecos de §3.

---

## 1. Qué cambia de la V3 a la V4

| Hoja | V3 → V4 | Cambio |
|---|---|---|
| Las 26 tablas restantes (institucional, actores, inventario, L-6/7/8, L-48, artículos, categorías, unidades) | = | **Idénticas** |
| `Materias` | 0 → **19** | Materias reales de la planificación 1-2027 (LGS 12, LND 4 + 3 homologadas, ver §5.1) |
| `GruposMateria` | **ELIMINADA** | Sus columnas pasan a `ProgramacionPracticas` |
| `ProgramacionPracticas` | 6 → **15 columnas** (0 filas) | + `GestionCodigo`, `MateriaCodigo`, `Grupo`, `DocenteActorCodigo` (NOT NULL), `LaboratorioCodigo`, `HoraInicio`, `HoraFin`, `CantEstudiantes`, `EsDemostrativa`, `FactorMateria`; − `GrupoCodigo`. Desaparece `DiaSemana` |
| `SolicitudesL3` | renombres (0 filas) | `Asignatura` (texto) → `MateriaCodigo` (FK nullable); `GrupoParalelo` → `Grupo` (varchar 20) |
| `Practicas`, `RequerimientosPractica`, `ExistenciasArticulos`, L-3 | 0 | **Siguen vacías** |
| `_POBLADO_MATERIAS`, `_REESTRUCTURA_PROGRAMACION` | nuevas | Bitácoras: no se importan |

Restricción **sugerida (no aplicada) por el propio Excel:** `UNIQUE(GestionCodigo, PracticaCodigo, Grupo)` en `ProgramacionPracticas`.

---

## 2. Estado actual verificado en DEMO_QA

| Bloque | Estado | Evidencia |
|---|---|---|
| Sedes, facultades, carreras, laboratorios (códigos del Excel) | ✅ | 3 sedes, FGYT/FCSN, LGS/LTH/LND/LCG, 24 laboratorios con `CampusId` |
| Gestión 2027-1 | ✅ en BD, ❌ en el Excel | `Managements.Id = 9`; el Excel sigue avisando que falta |
| Actores | ✅ | 51 `ACT-xxxx` + 4 sin código (legacy) + 4 `DEMO-DOC-00x` |
| Artículos, categorías, unidades | ✅ con restos demo | 1.023 del cliente + 8 demo; **3 categorías demo** (UTEN, VAJI, CONS) y **3 unidades demo** (KG, LT, ROL) mezcladas con las del cliente |
| Laboratorio real de las unidades | ✅ | 547 con el laboratorio del Excel; 9 solo en la BD siguen como `[INFERIDO-DEMO]`, como estaba previsto |
| Materias | ❌ **demo** | 8 `GAS-xxx`; ninguna de las 19 de la V4 |
| Grupos | ❌ **demo** | 4 `CourseGroups` de 2026-2 sobre materias demo |
| Prácticas / recetas / stock | ❌ **demo** | 2 prácticas, 7 líneas de receta, 8 existencias (solo ECONO) |
| Programación de prácticas | ❌ **no existe tabla** | Hoy vive dentro de `L3Request` (+ `AcademicWeek`) |
| Circuito L-3 por artículo | ❌ | `L3RequestDetail` sigue con `EquipmentUnitId` obligatorio y sin `ArticleId` |
| Catálogo (121) / unidades (644) | ⚠️ parcial | 107 catálogos y 556 unidades: **faltan 15 catálogos y 97 unidades** |
| Gestiones históricas | ❌ | 6 en la BD vs 26 en el Excel (faltan 2014-1 … 2024-1) |
| Históricos L-6/L-7/L-8/L-48 | ❌ | Fase E (conciliación por contenido) |

Restos demo de la Fase A que conviene limpiar o documentar:

- Facultades TEC/SALUD y carreras sin código: hoy `Inactivo`, se pueden dejar.
- Laboratorios `OFICINAS 1ER PISO` y `OFICINAS PLANTA BAJA`: activos, sin sede y fuera del Excel, con 6 unidades.
- `LAB-GAS-01`/`LAB-BIO-01`: inactivos. `PENDIENTE`: eliminado.

---

## 3. Huecos de estructura: contenido exacto de la migración `AlignStructuralBaseV4`

> Es aditiva, sin tocar migraciones existentes. Todas las tablas afectadas tienen **0 filas** de negocio en DEMO_QA y en la base principal (`L3Requests`, `L3RequestDetails` y `PracticeRequirements` con datos demo únicamente), así que los cambios de nulabilidad no requieren migrar datos.

### 3.1 Valores "Por confirmar" que faltaban (bloquean la Fase E)

La Fase A cubrió 5 enums. El Excel también usa "Por confirmar" en:

| Enum / columna | Dónde en el Excel | Filas | Acción |
|---|---|---:|---|
| `ManagementType` (`Managements.Type`, NOT NULL) | `Gestiones.TipoGestion` | **26/26** | Agregar `PorConfirmar = 9`, `Display` "Por confirmar", y recrear `CK_Managements_Type` |
| `MaintenanceType` (`Maintenances.MaintenanceType` NOT NULL; `MaintenancePlans.MaintenanceType`) | `MantenimientosL8.TipoMantenimiento`, `PlanL48.TipoMantenimiento` | 44 + 1 | Agregar `PorConfirmar = 9` y recrear `CK_Maintenances_Type` y `CK_MaintenancePlans_MaintenanceType`. **No mapear a `Otros (99)`** |

Mismo criterio de interfaz que en la Fase A (`EnumHelper.FormSelectList`): visible en listados y filtros, no elegible en altas.

Lo que **no** necesita enum nuevo:

- `CatalogoEquipos.Estado/Categoria` = "Por confirmar" → `Equipment.ClassificationReviewStatus = PendingClient`.
- `PlanL48.Estado` = "Por confirmar" → `MaintenancePlans.Status = NULL` (la columna es *nullable*).
- `Actores.TipoActor/Vinculacion` → ya cubiertos con `DataQualityIssue` `ACTOR_LINK_PENDING`.

### 3.2 Nueva entidad `PracticeSchedule` (= `ProgramacionPracticas`, 15 columnas)

| Columna | Tipo | Nulo | Origen V4 | Regla |
|---|---|---|---|---|
| `Id` | int identity | NO | — | |
| `Code` | nvarchar(50) | NO | `ProgramacionCodigo` | UNIQUE |
| `ManagementId` | int FK → Managements | NO | `GestionCodigo` | Restrict |
| `SubjectId` | int FK → Subjects | NO | `MateriaCodigo` | Restrict. En la importación, validar que `Practice.SubjectId = SubjectId` |
| `PracticeId` | int FK → Practices | NO | `PracticaCodigo` | Restrict |
| `CourseGroupId` | int FK → CourseGroups | SÍ | derivado | Ver §3.3 |
| `GroupName` | nvarchar(20) | SÍ | `Grupo` | |
| `TeacherPersonId` | int FK → People | NO | `DocenteActorCodigo` | Debe tener rol `Docente` (validar en la importación y en el servicio) |
| `LaboratoryId` | int FK → Laboratories | SÍ | `LaboratorioCodigo` | |
| `WeekNumber` | int | SÍ | `Semana` | "No deducir de la fecha". `CHECK 1..30` |
| `AcademicWeekId` | int FK → AcademicWeeks | SÍ | derivado | Se resuelve con la planificación (gestión + carrera de la materia) + `WeekNumber`; si no hay coincidencia queda NULL |
| `ExecutionDate` | date | SÍ | `FechaEjecucion` | |
| `StartTime` / `EndTime` | time | SÍ | `HoraInicio/HoraFin` | `CHECK EndTime > StartTime` si ambos existen |
| `StudentCount` | int | SÍ | `CantEstudiantes` | `CHECK >= 0` |
| `IsDemonstrative` | bit | SÍ | `EsDemostrativa` | |
| `ScalingFactor` | int | SÍ | `FactorMateria` | `CHECK >= 0` |
| `Status` | int (enum `PracticeScheduleStatus`: Programada 0, Ejecutada 1, Reprogramada 2, Cancelada 3, PorConfirmar 9) | NO | `Estado` (texto) | Mapear el texto; si es desconocido → `PorConfirmar` + `DataQualityIssue` |
| Auditoría + `IsDeleted`/`DeletedDate` | | | | Query filter `!IsDeleted` |

Índice: `UNIQUE(ManagementId, PracticeId, GroupName) WHERE IsDeleted = 0`, que es la restricción sugerida por el Excel.

### 3.3 Qué hacer con `CourseGroup` (V4 eliminó `GruposMateria`)

**Recomendación: conservarlo** (ya tiene CRUD y pantallas) como la identidad del grupo (gestión, materia, grupo), y **derivarlo** al importar:

- Cada combinación distinta (`GestionCodigo`, `MateriaCodigo`, `Grupo`) de `ProgramacionPracticas` es un `CourseGroup`, con `Code = GRP-{gestión}-{materia}-{grupo}`.
- Sus campos "habituales" (docente, laboratorio, horas, estudiantes, factor) toman el **valor más frecuente** del grupo. Si las filas difieren, se registra un `DataQualityIssue` `COURSE_GROUP_VALUES_VARY`.
- El valor real de cada sesión vive en `PracticeSchedule`: la importación es fiel y no pierde nada.
- `CourseGroup.TeacherPersonId` es NOT NULL; el Excel también exige docente, así que se puede derivar.
- `CourseGroup.Weekday`: la V4 ya no trae `DiaSemana`. Se deriva de `ExecutionDate` solo si todas las fechas del grupo caen el mismo día; si no, queda NULL.

### 3.4 Circuito L-3 (Fase D): cambios de esquema

**`L3Request` (= `SolicitudesL3`)**

| Cambio | Detalle |
|---|---|
| + `Code` nvarchar(50) | `SolicitudL3Codigo`, UNIQUE filtrado `IS NOT NULL` |
| + `ManagementId` int FK | `GestionCodigo` (NO en el Excel). La tabla está vacía, así que puede crearse NOT NULL directamente |
| + `PracticeScheduleId` int FK, nullable | `ProgramacionCodigo` (NULL = extraordinaria) |
| + `RequestDate` datetime2 NOT NULL | `FechaSolicitud` (regla de 24 h; validar en el servicio, no con un `CHECK`) |
| + `CampusId`, `CareerId` FK nullable | `SedeCodigo`, `CarreraCodigo` |
| + `AllLaboratories` bit NOT NULL default 0 | `TodosLosLaboratorios` |
| `LaboratoryId` → **nullable** | + `CHECK (AllLaboratories = 1 OR LaboratoryId IS NOT NULL)` |
| `SubjectId` → **nullable** | `MateriaCodigo` es SÍ en el Excel |
| + `CourseGroupId` FK nullable + `GroupName` nvarchar(20) | Reemplazan a `GroupId` (tabla legacy `Groups` A–D, que no admite `A1`/`A2` de LND). `GroupId` pasa a nullable y queda obsoleto |
| `TeacherPersonId` → **NOT NULL** | `DocenteActorCodigo` es NO en el Excel. `TeacherId` (User) se mantiene nullable como legacy |
| + `StartTime`, `EndTime` time | `HoraInicio/HoraFin`; `ScheduledDate` = `FechaPractica` |
| + `PracticeNumber` nvarchar(20) | `NumeroPractica` |
| `Status` | Agregar `CHECK` con los valores de `L3RequestStatus` (hoy no tiene restricción) |

**`L3RequestDetail` (= `SolicitudL3Detalles`)**: decisión del usuario, híbrido por artículo.

- + `Code` nvarchar(50) UNIQUE filtrado.
- + `ArticleId` int FK **NOT NULL** (tabla vacía).
- `EquipmentUnitId` → **nullable** (`NumeroInventario` opcional).
- + `RequestedQuantity` decimal(12,2) NULL, `CHECK > 0`.
- + `Observations` nvarchar(1000).
- `ExpectedQuantity`, `DeliveredQuantity` y `ReturnedQuantity` (int) quedan **obsoletos**: la entrega y la devolución pasan a Salidas y Devoluciones. Se pueden quitar cuando la interfaz deje de usarlos.

**`Departure` (= `SalidasL3`)**

- + `Code` nvarchar(50) UNIQUE filtrado.
- + `L3RequestId` FK nullable.
- + `EstimatedReturnNote` nvarchar(200), para el `RetornoEstimado` de texto.
- La `Modalidad` (texto) se mapea a `Type`.
- `OriginLaboratoryId` debe ser NOT NULL para salidas L-3 (validar en el servicio, porque las salidas antiguas pueden no tenerlo).

**`DepartureItem` (= `SalidaDetalles`)**

- + `Code`.
- `Quantity` int → **decimal(12,2)** (1 fila demo).
- + `MeasurementUnitId` FK nullable, que reemplaza al texto `UnitOfMeasure`, mantenido como legacy.
- `ProductName` = `DescripcionOriginal`.

**Nuevas `DepartureReturn` / `DepartureReturnItem` (= `DevolucionesL3` / `DevolucionL3Detalles`)**

- `DepartureReturn`: `Id`, `Code` UNIQUE, `DepartureId` FK NOT NULL, `ReturnDate` date NULL, `ResponsiblePersonId` FK NOT NULL, `Status` (enum Parcial/Completa/ConFaltantes/PorConfirmar), `Observations`, auditoría.
- `DepartureReturnItem`: `Id`, `Code` UNIQUE, `DepartureReturnId` FK NOT NULL, `DepartureItemId` FK NOT NULL, `ReturnedQuantity` decimal(12,2) `CHECK >= 0`, `Observations`.
- Regla en el servicio: la suma devuelta no puede superar la cantidad de la salida, y `DepartureItem.ReturnedQuantity` se sincroniza como acumulado para las pantallas antiguas.

### 3.5 Ajustes menores en tablas de la Fase B/C

| Tabla | Cambio | Motivo |
|---|---|---|
| `PracticeRequirement` | + `Code` nvarchar(50) UNIQUE filtrado | `RequerimientoCodigo` (PK del Excel); trazabilidad e idempotencia |
| `PracticeRequirement.Quantity` | → **nullable**, `CHECK (Quantity IS NULL OR Quantity > 0)` | En el Excel `Cantidad` es SÍ; sin dato, se registra un `DataQualityIssue` en lugar de inventar 1 |
| `Practice.Description`, `Observations`, `SourceReference` | Verificar las longitudes al cargar; ampliar a `nvarchar(max)` si alguna supera 2000/500 | En el Excel son `text` |
| `Subject.CareerId` | Mantener nullable mientras existan materias demo; pasar a NOT NULL cuando se retiren | El Excel exige carrera |

---

## 4. Carga de datos V4 en DEMO_QA (después de la migración)

Script idempotente en `Tools/ExcelImport/gen_v4_academic_sql.py`. Antes de escribir, comprobar `DB_NAME()`. Lotes de 100 como máximo, una transacción por lote, `MERGE` por código y un lote nuevo en `ImportBatches` con el hash de la V4.

1. **Materias (19)** por `Code`, con `CareerId` (según la decisión de §5.1), `Semester` y `Status`.
2. **Retirar los datos académicos demo** sin borrarlos:
   - `Subjects` `GAS-xxx`, `CourseGroups` `GRP-2026-2-GAS*`, `Practices` `PRA-GAS*` y sus `PracticeRequirements`: `Status = Eliminado` o `IsRemoved = 1`.
   - `AcademicPlanning` demo: `Inactiva`.
   - Antes, reportar qué `L3Requests` o recetas dependen de ellos (hoy 0 L-3).
3. **Restos demo de catálogos:** las categorías UTEN/VAJI/CONS y las unidades KG/LT/ROL solo las usan los 8 artículos demo `ART-0001…0008` (ART-0005 ya está dado de baja). Si no hay otras referencias: artículos demo → `Eliminado`; categorías y unidades demo → `Inactivo`. Si hay referencias: reportarlas y no tocarlas.
4. `ProgramacionPracticas`, `Practicas`, `RequerimientosPractica`, `ExistenciasArticulos`, L-3: **0 filas en la V4**. El script tiene que soportarlas ya (con encabezados validados), para que la próxima versión del Excel se cargue sin tocar código.
5. **Verificación:**
   - 19 materias del cliente activas, 0 materias demo activas.
   - Volver a ejecutar el script: 0 inserciones.
   - `DBCC CHECKCONSTRAINTS` sin filas.

---

## 5. Problemas del propio Excel V4 (reportar al autor)

1. **Homologación dudosa LCG → LGS.** 6 materias de la planificación venían con carrera **LCG** (BAS100, COM100, FUN100, TSC300, PAN300, TAL300; 186 filas) y se reasignaron a LGS. Pero **LCG "Ciencias Gastronómicas" existe como carrera propia** en `Carreras`. Además, sus códigos (`xxx100`/`xxx300`) siguen otro patrón que los de LGS (`xxx1G5`), lo que sugiere un plan de estudios distinto. **Hay que confirmar con la cliente si son materias de LCG.** Si lo son, se cargan con LCG.
2. **ALI1N7:** el código sugiere semestre 7 y la planificación dice 6 (se usó 6).
3. **Nombres con sufijo de carrera:** "TECNICA DIETETICA LND", "DIETOTERAPIA II LND".
4. **Falta la gestión 2027-1 en `Gestiones`**, pese a que la planificación es 1-2027 y `ProgramacionPracticas.GestionCodigo` es NOT NULL. La BD ya la tiene, pero el Excel debe agregarla.
5. **`_INDICE` desactualizado:** `Articulos` sigue diciendo "columna nueva: TipoArticulo / CatalogoCodigo" y "POBLADO UNIDADES: 5 unidades". Es solo documentación, pero confunde.
6. **`GruposMateria` desapareció junto con `DiaSemana`:** el día habitual de cada grupo ya no existe en el modelo del cliente.
7. **`ProgramacionPracticas.Grupo` es nullable**, pero forma parte de la restricción única sugerida. Un NULL rompe la unicidad, así que la importación debe exigir grupo o registrar una incidencia.

---

## 6. Decisiones (ya resueltas: ver §0)

| # | Decisión | Recomendación |
|---|---|---|
| D1 | ¿Se pueden pedir en L-3 los 416 artículos "Por confirmar"? | **Sí**, mostrando la etiqueta. "Por confirmar" es una marca de revisión, no una baja; bloquearlos deja fuera el 40% del catálogo |
| D2 | ¿Contra qué stock se valida un pedido? | El Excel lo resuelve: `SalidasL3.LaboratorioOrigenCodigo` es NOT NULL. **La disponibilidad se valida contra el laboratorio de origen de la salida, que por defecto es ECONO.** Al crear la *solicitud* L-3 solo se **advierte** (no se bloquea), porque el stock real todavía no se ha cargado. La salida descuenta el stock y la devolución lo repone; los faltantes van a L-5 |
| D3 | ¿Las 6 materias "LCG" son de LCG o de LGS? | Preguntar a la cliente (§5.1) |
| D4 | ¿Se conserva `CourseGroup` derivado (§3.3)? | Sí; no hay que tirar el CRUD ya hecho |
| D5 | ¿Qué hacer con los laboratorios `OFICINAS 1ER PISO`/`PLANTA BAJA` (6 unidades, fuera del Excel)? | Mapearlos a `OFI-COORD` si la cliente lo confirma; si no, dejarlos activos con su incidencia |

---

## 7. Qué falta para que la BD sea la base completa, en orden

1. **Migración `AlignStructuralBaseV4`** (§3) + interfaz mínima. Interfaz:
   - CRUD de `PracticeSchedule`; el módulo de Planificación pasa a crear programaciones en vez de `L3Request`.
   - L-3 por artículo.
   - Salidas con `L3RequestId`.
   - Devoluciones parciales.
2. **Carga V4** (§4) y retiro de los datos demo académicos.
3. **Fuente pendiente de la cliente**, sin la cual no hay datos reales de programación ni recetas: `BASE_NORMALIZADA_UTENSILIOS_1-2027_COMPLETADA.xlsx` (422 filas de planificación → `ProgramacionPracticas`; 682 productos → `RequerimientosPractica`) y el stock real (`ExistenciasArticulos`).
4. **Completar maestros de inventario:**
   - 15 catálogos (CAT-0108 a CAT-0122) y 97 unidades nuevas.
   - 126 cambios de catálogo, como `EquipmentClassificationDecision`.
   - Las 20 gestiones históricas 2014-1…2024-1, con `Type = PorConfirmar`; `1900-1` va a cuarentena.
5. **Fase E: históricos.** L-6 (1.599), L-7 (329), L-8 (1.209), participantes, mant-solicitud y L-48 (387), conciliados por contenido contra lo existente. Nunca borrar los 191 L-7 ni los 124 costos que solo están en la BD.
6. **Prueba en navegador** con sesión iniciada, respaldo, base oficial y despliegue.

---

## 8. Criterios de aceptación de la migración puntual

- `dotnet build -m:1`: 0 errores y 0 advertencias nuevas.
- `dotnet ef migrations script` revisado: solo `ADD`/`ALTER` sobre tablas vacías, `CREATE TABLE` e índices/`CHECK`; ningún `DROP` de datos.
- Aplicada solo en DEMO_QA. `DBCC CHECKCONSTRAINTS` limpio.
- Prueba de inserción de cada tabla nueva o cambiada con una fila válida y una inválida por cada `CHECK` (en transacción con `ROLLBACK`).
- La pantalla de Salidas (`Departures/Create`, `MassCreate`) sigue funcionando con `Quantity` decimal.
- Commit local, sin push.
