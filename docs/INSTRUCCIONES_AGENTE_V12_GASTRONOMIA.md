# Instrucciones para el agente: `BaseGastronomia_v1.xlsx` (V12 de la clienta)

**Fecha:** 2026-10-09 · **Análisis hecho en solo lectura:** comparación celda por celda (por clave primaria) del Excel nuevo contra la V7 y la V4 con `openpyxl`, más `SELECT` sobre `DB_Laboratorios_Univalle_DEMO_QA` (última migración `20261005144021_AddPracticeClosureRole`, HEAD `fb9b839`) y lectura del código en el árbol de trabajo.
**Estado (2026-10-09):** **Bloque A EJECUTADO en DEMO_QA** con `Tools/ExcelImport/gen_v12_gastronomia_sql.py` (lote `XLSX-V12-GASTRO-EDCA5F8B4A66`, respaldo `DEMO_QA_pre_V12_20261009.bak`); resultados en `docs/GUIA_IMPORTACION_HISTORICA.md` §V12. **Bloque B pendiente** (espera la migración `20261009141121_RestructurePlanningModel`, creada por el refactor pero aún sin aplicar).

| Archivo | SHA-256 |
|---|---|
| `C:\Users\monte\Downloads\BaseGastronomia_v1.xlsx` (internamente "V12") | `edca5f8b4a6601091a4e85939f9cf1aaa586a4e27c2e4df0dc1278afbb47a3f2` |
| `C:\Users\monte\Downloads\BaseEstructuraFinal_V7.xlsx` (último importado, bloque académico) | `c677c7cc81d63f5e74b00daab60c214be032c5089b1876c61b9ddb66dc4320e9` |
| `C:\Users\monte\Downloads\BaseEstructuraFinal_V4.xlsx` (fuente del histórico) | `8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2` |

Lectura previa obligatoria: `AGENTS.md`, `context.md`, `docs/MAPA_EXCEL_V4_A_BD.md` (§1 reglas globales y §2 traducción de valores: siguen vigentes), `docs/INSTRUCCIONES_AGENTE_V7_ACADEMICO.md` (mismo patrón de generador) y `Tools/ExcelImport/gen_v7_academic_sql.py` (base para el generador nuevo).

> El libro dice ser la **V12** y se comparó contra una V11 que nosotros no tenemos. Todas las diferencias de esta guía son **contra la V7**, que es lo último que está cargado en la BD.

---

## 0. Resumen ejecutivo

**Qué trae de nuevo (lo valioso):**
1. **El circuito L-3 con datos reales por primera vez:** 1.037 solicitudes (`SolicitudesL3`) y 11.451 líneas de artículos (`SolicitudL3Detalles`) de las gestiones **2026-1** (559) y **2026-2** (478), con fecha, horario, laboratorio, docente, grupo y estado. Es la historia de uso real de utensilios.
2. **22 materias nuevas** (19 → 41), 6 docentes nuevos con su rol, y **372 artículos nuevos** (1.002 → 1.374), casi todos (`ART-L33-*`) creados a partir de los formularios L-3.3.
3. **Existencias reemplazadas:** 700 filas, todas con laboratorio y cantidad (en la V7 había 1.063 filas, pero solo 220 tenían dato).
4. **Vida útil** (`Vidautilanios`) de los 121 equipos del catálogo (en la BD está vacía en todos).
5. Nueva tabla **`Incidentes`** (vacía) con su diseño y reglas de negocio.
6. **Códigos en MAYÚSCULAS**: se corrigió el problema "Tipo Título" de la V7 en los identificadores.

**Qué cambió en el modelo de negocio (lo importante para el código):**
- La clienta **eliminó** `SalidasL3`, `SalidaDetalles`, `DevolucionesL3` y `DevolucionL3Detalles`: *"la solicitud aprobada hace de salida y no hay devoluciones; lo que no tiene incidente se entiende devuelto"*. Hoy la app sí tiene salidas, devoluciones y el módulo Cierre de práctica basado en ellas. **Esto es una decisión de producto, no de importación (D10).**
- Estados de la solicitud: `Aprobada`, `Terminada`, `Parcial` (con al menos un incidente), `Cancelada`.
- `ProgramacionPracticas` se renombró a **`Planificacion`** (mismos 422 registros, más la columna `Dia`), y `SolicitudesL3` perdió la columna `Programacioncodigo`.

**Qué NO hay que reimportar:**
- Todo el histórico (L-6, L-7, L-8, L-48, unidades, catálogo, actores existentes) es **idéntico a la V4**, salvo formato. Además, los textos ahora están en "tipo oración" (`Laboratorio de cocina h-1`, `Casatermo srl`, `Tec. alex quispe`) y **siguen sin saltos de línea** (1.192 L-8, 480 L-7, 424 L-6). Reimportarlos degradaría lo que ya está cargado.
- Prácticas (196), recetas (2.111) y planificación 2027-1 (422): **sin cambios reales**.

**Riesgo principal:** el destino natural de `SolicitudesL3` es la entidad **`Planning`** del refactor en curso (renombra `L3Request` → `Planning`, `L3RequestDetail` → `PlanningRequirement` y agrega `ClassPeriod`, `AcademicDay`, `Rescheduling`, `DayRecovery`). Ese refactor **está sin commitear y no tiene migración todavía**. El bloque L-3 no se puede importar hasta que exista y se aplique esa migración (§5, T0).

---

## 1. Inventario de hojas: V7 → V12

| Hoja | V7 | V12 | Cambio real | Acción |
|---|---:|---:|---|---|
| Sedes, Facultades, Carreras, Laboratorios | 3/2/4/24 | igual | Solo mayúsculas/minúsculas | Ninguna |
| Gestiones | 27 | 27 | Solo formato de `Tipogestion` | Ver D7 (fechas faltantes en la BD) |
| **Actores** | 51 | **57** | +6 docentes (`ACT-0052…0057`) | Cargar (§3.2) |
| **ActorRoles** | 40 | **46** | +6 roles Docente (`AR-0041…0046`) | Cargar (§3.2) |
| **CatalogoEquipos** | 121 | 121 | **`Vidautilanios` lleno en los 121** | Actualizar `UsefulLifeYears` (§3.1) |
| UnidadesFisicas, VerificacionesL6, SolicitudesL7, SolicitudUnidades, MantenimientosL8, MantSolicitudes, MantParticipantes, PlanL48 | igual | igual | Solo formato (ver §2) | **No tocar** |
| **Materias** | 19 | **41** | +22 materias | Cargar (§3.3) |
| Practicas | 196 | 196 | Solo formato | Ninguna |
| **Planificacion** (antes `ProgramacionPracticas`) | 422 | 422 | Renombrada; `Programacioncodigo` → `Planificacioncodigo` (mismos valores `PROG-0001…`); nueva columna `Dia` (coherente con la fecha en las 422) | Ninguna (el día se deriva de la fecha) |
| CategoriasArticulo | 10 | 10 | Solo formato | Ninguna |
| UnidadesMedida | 5 | 5 | Sin cambio, pero **ahora un artículo usa `UNI-05` Metro** | Crear `UNI-05` (§3.4) |
| **Articulos** | 1.002 | **1.374** | +372, 0 eliminados, 0 cambios de estado | Cargar (§3.4) |
| RequerimientosPractica | 2.111 | 2.111 | Solo formato | Ninguna |
| **ExistenciasArticulos** | 1.063 (220 con dato) | **700** (todas con dato) | Reemplazo completo: ver §3.5 | Decisión D5 |
| **SolicitudesL3** | 0 | **1.037** | Nueva con datos; sin `Programacioncodigo` | Cargar tras el refactor (§4) |
| **SolicitudL3Detalles** | 0 | **11.451** | Nueva con datos | Cargar tras el refactor (§4) |
| **Incidentes** | — | 0 | Hoja nueva (solo estructura) | Ajuste de modelo (§4.4) |
| SalidasL3, SalidaDetalles, DevolucionesL3, DevolucionL3Detalles | 0 | — | **Eliminadas** | No borrar nada de la BD (D10) |
| `_POBLADO_*`, `_REESTRUCTURA_PROGRAMACION` | sí | — | Eliminadas (documentación) | — |

---

## 2. Formato del libro (afecta al generador)

1. **Encabezados** siguen con la primera letra en mayúscula (`Sedecodigo`, `Practicacodigo`): comparar con `strip().casefold()` como en el generador V7.
2. **Identificadores en MAYÚSCULAS** (`ACT-0052`, `SOL3-0001`, `H-06`). Aun así, aplicar `.strip().upper()` a toda columna `*codigo` y a `Grupo`.
3. **Textos en "tipo oración"**: primera letra mayúscula, resto minúscula. Pierde siglas y nombres propios (`Laboratorio k-1 cocteleria`, `Ing.zabala`). Regla D2 de la V7, que sigue vigente: **nunca actualizar con estos textos un nombre que ya existe en la BD**; los nombres nuevos se cargan como vienen, salvo D3.
4. **Saltos de línea**: siguen perdidos en los textos largos del histórico. Motivo adicional para no tocar el histórico.
5. **Fechas** `datetime` y **horas** `datetime.time`. `Cantidadsolicitada` y `Cantidaddisponible` son enteros.
6. Valores booleanos: `Sí` / `No` (con tilde) y `False` en `Todosloslaboratorios`.
7. Las hojas que empiezan con `_` no se importan.

---

## 3. Bloque A: catálogos y maestros (no depende del refactor)

### 3.1 `CatalogoEquipos.Vidautilanios` → `Equipments.UsefulLifeYears`
- 121 de 121 con valor: 6 años (28), 8 (7), 10 (48), 12 (38). Fuente declarada por la clienta: `CatalogoEquipos_manuales_completados.xlsx`.
- En la BD: **0 de 127** tienen valor. Los 121 `CatalogCode` existen.
- `UPDATE` por `CatalogCode` solo si `UsefulLifeYears IS NULL` o es distinto. Respeta `CK_Equipments_UsefulLife` (≥ 0).
- **No** actualizar ninguna otra columna del catálogo (el resto es idéntico salvo formato).

### 3.2 Actores y roles
| Código | Nombre en el Excel | Título | Observación |
|---|---|---|---|
| ACT-0052 | `Ing.zabala` | (vacío) | El título viene pegado al nombre. **Probable duplicado de `ACT-0037` ALBERTO ZABALA** |
| ACT-0053 | Mariana rojas | Lic. | |
| ACT-0054 | Vania vargas | Lic. | |
| ACT-0055 | Alvaro gonzales | Ing. | |
| ACT-0056 | Monica natali | Dra. | |
| ACT-0057 | Paola udaeta | Lic. | |

- Todos `Tipoactor = Persona`, `Vinculacion = Por confirmar` → `Intern` + issue `ACTOR_LINK_PENDING` (regla V4 §2).
- Roles `AR-0041…0046`: `Docente` → `PersonOperationalRole.Docente (8)`. Los 6 dan clases en `SolicitudesL3` (ACT-0052: 21 solicitudes).
- **ACT-0052:** cargarlo (las solicitudes lo referencian), con `POSSIBLE_PERSON_DUPLICATE` apuntando a `ACT-0037`. No fusionar (regla V4 §3.6).
- Nombres: D3.

### 3.3 Materias (+22)
| Carrera | Códigos |
|---|---|
| LGS (17) | COI3G6, CMA1G3, ENO1G6, IGA1G6, RYP1G3, TAG1G3, TAG4G6, TDA1G3, BAR200, CBT1G4, COI1G4, FUN200, PAN200, IDA1G4, NUT200, TAL200, TAG2G4 |
| LND (3) | NNA1N6, DIE1N4, TDI1N4 |
| LTH (2) | AYB3T3, AYB4T4 (primeras materias de Turismo y Hotelería) |

- Ninguna existe en la BD; las 19 existentes coinciden en carrera y estado. Sin choques con `IX_Subjects_Code` ni con `IX_Subjects_CareerId_Name`.
- `Semestre` del Excel → `Semester`. `Estado` "Activo" → `Status = 0`.
- **Ninguna de las 22 tiene prácticas ni recetas** en el Excel (ver D8).

### 3.4 Artículos (+372) y unidad `UNI-05`
- Por prefijo: `ART-L33-*` 302 (todos "Por confirmar"), `ART-REQ-*` 47, `ART-LAB-*` 19, `GA-05-04-*` 3, `GA-05-02-*` 1. Estado: 302 "Por confirmar" → `ReviewStatus = 9`; 70 "Activo" → `ReviewStatus = 0`. `Status = 0` en todos.
- Los 1.002 existentes coinciden en código y `ReviewStatus` con la BD (0 diferencias). **No** actualizar sus nombres.
- **Todos los nuevos se usan:** 1.657 líneas L-3 y 78 existencias. Ninguno está en recetas.
- **Colisión con `UX_Articles_Name_Active`:** `ART-L33-0007` y `ART-L33-0256` se llaman igual ("Paila para chicharron"). Ninguno de los nuevos choca con un nombre activo de la BD. → D4.
- Unidad: 369 `UNI-03` Pieza, 2 `UNI-01` Juego y **1 `UNI-05` Metro** (`ART-REQ-00615`, "Tela gasa transparente…"). En la BD no existe `UNI-05`: crearla (`Code = 'UNI-05'`, `Name = 'Metro'`). `UNI-04` Rollo sigue sin uso y `ROL` ya existe: no se carga (D5 de la V7).
- Columnas *legacy* `Category` y `UnitOfMeasure` con el **nombre** (como en V3/V4/V7).
- Lotes de 100.

### 3.5 Existencias → `ArticleStocks` (requiere D5)

| | Filas |
|---|---:|
| BD actual (8 demo + 220 de la V7) | 228 |
| Excel V12 | 700 (ECONO 429, H-03 48, H-04 37, H-02 35, H-05 34, H-06 33, K-01 33, H-01 31, H-07 20) |
| Iguales en (lab, artículo) | 95 (4 con cantidad distinta) |
| **Solo en la BD** | **133** (116 `ART-REQ`, 8 `ECO-R0328`, 7 demo `ART-000x`, 2 `ART-LAB`) |
| Solo en el Excel | 605 (78 de artículos nuevos) |

- **Retroceso detectado:** la clienta quitó 125 existencias de la V7 sin reasignarlas a otro código (por ejemplo, `ECONO / ART-REQ-00435` "Bowl mediano" 80, `ECONO / ART-REQ-00109` "Boquilla" 88). Por eso, **los artículos de receta con stock bajan de 82 a 28** (de 493). La disponibilidad de la planificación empeora si se reemplaza.
- 13 filas con cantidad 0 (válidas). `Fechaactualizacion`: 271 con 2024-02-07, 62 con 2026-10-07 y 367 vacías → `UpdatedOn`.
- Las 4 cantidades distintas: `ECONO/GA-05-03-012` 1→7, `ECONO/ECO-R0365` 6→30, `H-03/ART-LAB-0079` 3→1, `H-03/ART-LAB-0085` 13→5.
- Ya existe el importador de existencias en la app (`Catalogs/Stock/Import`, commit `fb9b839`); se puede usar el mismo criterio, pero la carga va en el generador para tener lote, idempotencia e incidencias.

---

## 4. Bloque B: circuito L-3 (depende del refactor `Planning`)

### 4.1 Perfil de `SolicitudesL3` (1.037)
| Columna | Hallazgo |
|---|---|
| `Solicitudl3codigo` | `SOL3-0001…`, únicos. Faltan `SOL3-0334…0336` (la clienta los eliminó; no se renumera) |
| `Gestioncodigo` | 2026-1: 559 · 2026-2: 478. Ambas existen (Ids 4 y 5) |
| `Fechasolicitud` | **Vacía en las 1.037** (los formularios no la traen) → D1 |
| `Sedecodigo` / `Carreracodigo` | CBBA / LGS en todas. **122 solicitudes son de materias de LND (109) o LTH (13)** → `Carreracodigo` incoherente; `Planning` ya no tiene `CareerId` → se ignora + issue `CAREER_MISMATCH` |
| `Laboratoriocodigo` | 12 ambientes (H-01…H-07, K-01 y las aulas H-102, H-103, H-104, H-106). **4 vacías** (SOL3-0016/17/18 y 0312, 2026-1) → choca con `CK_Plannings_Laboratory` → D2 |
| `Todosloslaboratorios` | `False` en todas |
| `Docenteactorcodigo` | 20 docentes, todos con rol Docente, 0 FK rotas |
| `Materiacodigo` | 38 materias, 0 FK rotas |
| `Grupo` | A 385, B 286, B2 187, A1 80, C 70, A2 29. 106 combinaciones (gestión, materia, grupo) |
| `Fechapractica` | 2026-1: 11-02 a 26-06-2026; 2026-2: 31-07 a 17-12-2026. Ninguna en domingo ni fuera de la gestión. **9 vacías** (2026-1) → D2 |
| `Horainicio` / `Horafin` | Ver §4.2 |
| `Numeropractica` | Texto (`1`…`14`, `11.1`, `11.2`, `E1p`, `E2p`, `E3p`) |
| `Estado` | Terminada 817 · Aprobada 220. Coherente con la fecha al 2026-10-09 (Aprobada = futura). Sin `Parcial` ni `Cancelada` |
| `Observaciones` | Siempre la fuente (`Fuente: l-3.3_<materia>.xlsx / …`), hasta 303 caracteres → `Observations` (1.000) |

**Relación con la práctica del catálogo:** con (`Materiacodigo`, `Numeropractica`) se resuelven **456** solicitudes a una `Practice` existente; **581 no** (las 22 materias nuevas no tienen prácticas, más 3 de DIE2N5). No hay duplicados (gestión, materia, grupo, número).

**Coherencia receta ↔ pedido real:** en las 438 solicitudes que tienen receta, la mediana de coincidencia de artículos es 100 %. Las recetas de la V7 salieron de estos mismos formularios: los datos son consistentes.

**Choques de horario (no bloquean):** 69 pares en el mismo laboratorio y 62 del mismo docente, con periodos solapados el mismo día. En su mayoría son subgrupos (A1/A2) de la misma materia con el mismo docente y laboratorio, que es legítimo. La importación **no** debe pasar por la validación de conflictos de la UI; se registra `SCHEDULE_OVERLAP_SOURCE` (informativo).

### 4.2 Horarios → `ClassPeriod` (catálogo fijo de 12 periodos del refactor)
| Resultado | Solicitudes |
|---|---:|
| Inicio y fin coinciden exactamente con límites de periodo | **996** (bloques típicos de 4 periodos: 08:35–12:05, 13:05–16:45, 07:35–11:15, 14:05–17:35) |
| Inicio a las **15:45** (ningún periodo empieza ahí; P9 termina 15:45 y P10 empieza 15:55) | 22 |
| Fin que no es límite de periodo (13:15 ×6, 10:25 ×1) | 7 |
| Solo hora de inicio | 8 |
| Sin horas | 4 |

Regla (D6): si mapea exacto, `PlanningSlot.Apply(fecha, periodoInicial, cantidad)`. Si no, `StartPeriodId` y `PeriodCount` NULL (lo permite `CK_Plannings_Periods`), `StartTime`/`EndTime` con el valor de la fuente cuando exista y la issue `TIME_NOT_ALIGNED_TO_PERIOD`. **No redondear** 15:45 a 15:55 sin decisión.

### 4.3 Perfil de `SolicitudL3Detalles` (11.451)
- 2026-1: 6.281 · 2026-2: 5.170. Entre 1 y 40 líneas por solicitud; todas las solicitudes tienen al menos una.
- 0 FK rotas (solicitud y artículo). `Numeroinventario` vacío en todas → `EquipmentUnitId` NULL.
- `Cantidadsolicitada`: **11 vacías** y **30 en 0**. `CK_PlanningRequirements_RequestedQuantity` exige NULL o > 0 → ambas van a NULL + `VALUE_MISSING` / `VALUE_ZERO_IN_SOURCE`.
- **102 líneas con cantidad ≥ 100** (máximo 5.000): son insumos (frutilla, uva, durazno, papaya…) cuya cantidad parece estar en gramos, cargados como artículo con unidad Pieza. Se cargan tal cual + `QUANTITY_UNIT_SUSPECT`.
- **80 pares (solicitud, artículo) repetidos** (por ejemplo, `SOL3D-01309` y `SOL3D-01325`, ambos con 4). No hay índice único en `PlanningRequirements (PlanningId, ArticleId)`: cargar las dos líneas (cada una tiene su código) + `DUPLICATE_LINE_IN_SOURCE`. No sumar.
- `Observaciones` (máx. 105): trazabilidad de la fuente (`Unidad fuente: pieza`, `Producto fuente: 'plato redondo plano' (conjunto)`, `Columna devolución fuente: economato`) → `Observations`.
- 9.412 líneas apuntan a artículos "Por confirmar".

### 4.4 `Incidentes` (vacía) frente a `L5Incident`
La hoja define el registro de incidentes que se usará de aquí en adelante. Diferencias con el modelo actual:

| Excel | `L5Incident` hoy | Brecha |
|---|---|---|
| `Incidentecodigo` (INC-0001) | — | Falta `Code` |
| `Gestioncodigo` (obligatorio) | — | Falta `ManagementId` (hoy solo se infiere por la planificación; los incidentes fuera de práctica no tienen gestión) |
| `Solicitudl3codigo` (nulo = evento o examen) | `PlanningId` | OK |
| `Solicitudl3detallecodigo` | — | Falta `PlanningRequirementId` |
| `Articulocodigo` (obligatorio) | `ArticleId` (nullable) | OK con `CK_L5Incidents_Target` |
| `Numeroinventario` | `EquipmentUnitId` | OK |
| `Laboratoriocodigo` | — | Falta `LaboratoryId` |
| `Fechaincidente` | `RecordDate` | OK |
| `Tipoincidente` (Rotura, Pérdida, Daño, Faltante) | `IncidentType` (texto libre) | Normalizar los valores |
| `Cantidadafectada` numeric(12,2) | `AffectedQuantity` int | Tipo distinto |
| `Estudiante` | `ResponsibleName` + `ResponsibleType` | OK |
| `Docenteactorcodigo` | — | Falta `TeacherPersonId` |
| `Descripcion` | `DamageDescription` | OK |
| `Resolucion` (Pendiente, Repuesto en especie, Pagado en dinero, Condonado) | `RepositionStatus` | Alinear los valores |
| `Costounitariobs` | — | Falta `UnitCost` |
| `Estado` (Abierto, Resuelto) | — | Falta (hoy se deduce de `RepositionStatus`) |

**No hay datos que importar.** Es una tarea de código aparte (D9) y no bloquea nada de esta guía.

### 4.5 Mapa `SolicitudesL3` → `Planning` (modelo del refactor)
| Excel | `Planning` | Regla |
|---|---|---|
| `Solicitudl3codigo` | `Code` | Tal cual (`SOL3-0001`). Idempotencia por `Code`. La UI muestra `PRA-{Id}` (`L3Codes.Format`); decidir si mostrar `Code` cuando exista (D11) |
| `Gestioncodigo` | `ManagementId` | Por código |
| `Fechasolicitud` | `RequestDate` | D1 |
| `Sedecodigo` | `CampusId` | CBBA |
| `Carreracodigo` | — | Se ignora; issue si no coincide con la carrera de la materia |
| `Laboratoriocodigo` | `LaboratoryId` | D2 si está vacío |
| `Todosloslaboratorios` | `AllLaboratories` | `False` |
| `Docenteactorcodigo` | `TeacherPersonId` | Por `ActorCode` |
| `Materiacodigo` | `SubjectId` | Por código |
| `Grupo` | `GroupName` + `CourseGroupId` | `CourseGroup` derivado `GRP-<gestión>-<MATERIA>-<GRUPO>` (como en la V7). Hoy 2026-1 tiene 0 grupos y 2026-2 tiene 4 de prueba |
| `Fechapractica` | `ScheduledDate` (+ `AcademicWeek`/`AcademicWeekId`) | D2 si está vacía; semana según el calendario de la gestión (D7) |
| `Horainicio`/`Horafin` | `StartPeriodId`, `PeriodCount`, `StartTime`, `EndTime` | §4.2 / D6 |
| `Numeropractica` | `PracticeNumber` | En MAYÚSCULAS (`E1P`) |
| (derivado) | `PracticeName` (obligatorio) | `Practice.Name` si resuelve; si no, `Práctica <n> – <nombre de la materia>` + `PRACTICE_NOT_IN_CATALOG` |
| (derivado) | `PracticeScheduleId` | D8 |
| `Estado` | `Status` | Aprobada → `Aprobada`; Terminada → `Finalizada`; Parcial → `Finalizada` (el "Parcial" se deriva de los incidentes, D10); Cancelada → `Cancelada` |
| `Observaciones` | `Observations` | Tal cual |

`SolicitudL3Detalles` → `PlanningRequirement`: `Solicitudl3detallecodigo` → `Code` · `Solicitudl3codigo` → `PlanningId` · `Articulocodigo` → `ArticleId` · `Numeroinventario` → `EquipmentUnitId` (siempre NULL) · `Cantidadsolicitada` → `RequestedQuantity` (§4.3) · `Observaciones` → `Observations`. Las columnas obsoletas (`ExpectedQuantity`, `DeliveredQuantity`, `ReturnedQuantity`) quedan con su valor por defecto.

**No crear `Departures`, `DepartureItems` ni `DepartureReturns`** a partir de estas solicitudes.

---

## 5. Decisiones (valores por defecto; el usuario puede cambiarlas antes de ejecutar)

| ID | Tema | Decisión por defecto |
|---|---|---|
| D1 | `Fechasolicitud` vacía y `Planning.RequestDate` NOT NULL | En la migración del refactor, hacer `RequestDate` **nullable** (solo se asigna al crear; impacto bajo). Si no se acepta: `RequestDate = ScheduledDate` + `VALUE_PENDING_CONFIRMATION`. No usar la fecha de importación |
| D2 | 9 solicitudes sin fecha, 4 de ellas también sin laboratorio (SOL3-0016…0023 y 0312; 2026-1, todas "Terminada") | **No se cargan.** Van a `DataQualityIssue` (`REQUIRED_VALUE_MISSING`) con su código y se informan. Sus 154 líneas tampoco se cargan. Alternativa: cargarlas con `AllLaboratories = 1` y fecha = inicio de la gestión, pero eso inventa datos |
| D3 | Nombres nuevos en "tipo oración" (actores, materias, artículos) | Materias: a MAYÚSCULAS, como las 19 que ya existen. Actores: nombre y título en MAYÚSCULAS como los 15 docentes existentes, separando el título pegado (`Ing.zabala` → título `ING.`, nombre `ZABALA`); el texto original queda como alias. *(Ajustado al ejecutar: cargarlos "como vienen" dejaba `Mariana rojas` junto a `VIRGINIA LOBO`.)* Artículos: como vienen (1.002 ya están en MAYÚSCULAS; mezclar estilos es solo estético; pedir confirmación si se quiere uniformar) |
| D4 | "Paila para chicharron" duplicado (`ART-L33-0007` y `ART-L33-0256`) | Cargar `ART-L33-0007` y cargar `ART-L33-0256` con el nombre `Paila para chicharron (ART-L33-0256)` + `DUPLICATE_NAME_IN_SOURCE`, para respetar `UX_Articles_Name_Active` sin perder las líneas L-3 que lo usan. No fusionar sin la clienta |
| D5 | Existencias: ¿reemplazar o agregar? | **Agregar y actualizar, no borrar:** insertar las 605 nuevas, actualizar las 4 distintas (con `CONTENT_CONFLICT` y el valor anterior) y **conservar las 133 que solo están en la BD** con `STOCK_NOT_IN_SOURCE`. Reemplazar dejaría solo 28 artículos de receta con stock. Confirmar con la clienta si las 125 de la V7 se dieron de baja a propósito |
| D6 | Horas que no coinciden con periodos (22 a las 15:45, 7 con fin raro, 8 sin fin, 4 sin hora) | Sin periodo (`StartPeriodId`/`PeriodCount` NULL), con las horas de la fuente en `StartTime`/`EndTime` cuando existan + `TIME_NOT_ALIGNED_TO_PERIOD`. No redondear |
| D7 | Calendario de 2026-1 y 2026-2 (para `AcademicWeek`) | En la BD, 2024-2…2026-2 tienen `StartDate`/`PlannedEndDate` NULL (nunca se cargaron de la V4) y `AcademicCalendarService.GenerateWeeksAsync` las exige. **Cargar las fechas del Excel solo donde están NULL** (2024-2…2026-2: 01-01 a 30-06 y 01-07 a 31-12) y generar las semanas de 2026-1 y 2026-2 con el servicio. No tocar 2027-1 (D6 de la V7). Ojo: con esas fechas la semana 1 de 2026-1 empieza en enero, aunque la primera práctica es del 11-02; si se quiere que la semana 1 sea la primera de clases, la clienta debe dar la fecha real de inicio |
| D8 | Vínculo de las solicitudes con `Practice` | Para las 456 que resuelven, crear un `PracticeSchedule` por solicitud (código `PROG-<SOL3-código>`) en su gestión y grupo, y enlazar `Planning.PracticeScheduleId`, igual que el wizard adopta las programaciones. Las 581 restantes, `PracticeScheduleId` NULL. **No** crear prácticas ni recetas para las 22 materias nuevas a partir de los pedidos (sería posible, porque los datos coinciden al 100 %, pero es una decisión de la clienta) |
| D9 | `Incidentes` | Tarea de código separada: migración que agregue a `L5Incident` las columnas faltantes de §4.4. No es parte de la importación |
| D10 | Flujo sin salidas ni devoluciones | **No cambiar el código en esta tarea.** Las solicitudes históricas se cargan sin `Departures`. Las 220 "Aprobada" futuras de 2026-2 quedarán pendientes de cierre en el módulo Cierre de práctica. Decidir si el cierre puede finalizar sin salida (como pide la clienta) o se mantiene el circuito actual |
| D11 | Código visible | La UI muestra `PRA-{Id}`. Para el histórico conviene mostrar `Code` (`SOL3-0001`) cuando exista. Tarea de UI menor, aparte |
| D12 | Solicitudes de prueba en 2026-2 (`PRA-00023…26`, `QA-L3-165119`) | No tocar. Informar que conviven con las importadas |

---

## 6. Tareas, en orden

### T0. Preparación (solo lectura)
1. `git status`: hay un refactor sin commitear (`Planning`, `PlanningRequirement`, `ClassPeriod`, `AcademicCalendar`, `Rescheduling`, `DayRecovery`; se eliminan `L3Request`, `L3RequestDetail`, `AcademicPlanning` y `PlanningService`). **No revertir, no formatear y no commitear esos archivos.**
2. Verificar el SHA-256 del Excel (tabla de arriba).
3. `DB_NAME() = 'DB_Laboratorios_Univalle_DEMO_QA'` y la última migración. **El Bloque B solo puede ejecutarse si la migración del refactor (tablas `Plannings`, `PlanningRequirements`, `ClassPeriods`, `AcademicDays`) ya está creada y aplicada en DEMO_QA.** Si no lo está, ejecutar solo el Bloque A y detenerse. No crear esa migración como parte de esta tarea: pertenece al refactor.

### T1. Generador `Tools/ExcelImport/gen_v12_gastronomia_sql.py`
Copiar `gen_v7_academic_sql.py` y adaptarlo. **No modificar los generadores anteriores.**
- Constantes: ruta y hash de `BaseGastronomia_v1.xlsx`; `batch_code = "XLSX-V12-GASTRO-" + sha[:12].upper()`; `SourceName = 'BaseGastronomia_v1.xlsx'`; `ContractVersion = 'base-gastronomia-v12'`; `OUT = %TEMP%\mm\import_v12_*.sql`.
- Dos salidas separadas: `import_v12_bloque_a.sql` (§3) y `import_v12_bloque_b.sql` (§4), para poder ejecutar A sin B.
- Encabezados con `casefold`; detenerse si falta una columna esperada. Hoja `Planificacion` (no `ProgramacionPracticas`).
- **Verificaciones previas en Python** (detenerse si fallan): 22 materias nuevas, 6 actores, 372 artículos nuevos, 700 existencias, 1.037 solicitudes, 11.451 líneas; 0 FK rotas; ningún código de artículo de las existencias o de las líneas que no esté en `Articulos`.
- **Bloque A, en este orden:** `UNI-05` → materias → actores, `Intern` y roles → artículos → `UsefulLifeYears` → existencias (D5) → fechas de gestión (D7, solo donde son NULL).
- **Bloque B, en este orden:** calendarios 2026-1/2026-2 (se generan con la app o con el servicio, no a mano en SQL; ver T3) → `CourseGroups` derivados → `PracticeSchedules` (D8) → `Plannings` → `PlanningRequirements` → incidencias.
- Idempotencia por clave natural (`Code`, `ActorCode`, `CatalogCode`, (lab, artículo)); segunda pasada = 0 inserciones y 0 incidencias nuevas.
- Lotes ≤ 100, una transacción por bloque, `SET XACT_ABORT ON` y `THROW` si el destino no es DEMO_QA.
- **Prohibido en el SQL** (hacer `grep` en T2): `UPDATE dbo.Verifications`, `dbo.Requests`, `dbo.Maintenances`, `dbo.EquipmentUnits`, `dbo.Practices`, `dbo.PracticeRequirements`; `UPDATE` del `Name` de `Subjects`, `Articles` o `People` existentes; cualquier `DELETE`.

**Códigos de `DataQualityIssue` nuevos:** `CAREER_MISMATCH`, `REQUIRED_VALUE_MISSING`, `TIME_NOT_ALIGNED_TO_PERIOD`, `PRACTICE_NOT_IN_CATALOG`, `SCHEDULE_OVERLAP_SOURCE`, `VALUE_ZERO_IN_SOURCE`, `QUANTITY_UNIT_SUSPECT`, `DUPLICATE_LINE_IN_SOURCE`, `DUPLICATE_NAME_IN_SOURCE`, `STOCK_NOT_IN_SOURCE`. Se reutilizan: `VALUE_MISSING`, `VALUE_PENDING_CONFIRMATION`, `ACTOR_LINK_PENDING`, `POSSIBLE_PERSON_DUPLICATE`, `CONTENT_CONFLICT`.

### T2. Generar y revisar el SQL antes de ejecutar
Revisar los conteos impresos y hacer el `grep` de los patrones prohibidos.

### T3. Ejecutar en DEMO_QA (requiere autorización explícita del usuario para escribir)
- Respaldo antes: `BACKUP DATABASE … TO DISK = N'…\DEMO_QA_pre_V12_<fecha>.bak' WITH COPY_ONLY, CHECKSUM` + `RESTORE VERIFYONLY`. Sin respaldo verificado no se ejecuta.
- Guardar antes los valores de control de T4 (checksums de textos históricos, conteos, las solicitudes de prueba de 2026-2).
- `sqlcmd -S localhost -E -C -d DB_Laboratorios_Univalle_DEMO_QA -b -f 65001 -i <sql>` (**`-f 65001` es obligatorio**; en la V7 olvidarlo dañó 481 filas).
- Bloque B: generar primero las semanas de 2026-1 y 2026-2 desde la app (pantalla de gestión académica) o llamando al servicio, y verificar que existen antes de correr el SQL.
- Ejecutar cada bloque dos veces (la segunda debe insertar 0).

### T4. Verificación (criterios de aceptación)
| Comprobación | Esperado |
|---|---|
| `Subjects` activas | 41 (19 + 22) |
| `People` con `ActorCode` `ACT-%` | 57 |
| Roles Docente activos de actores `ACT-%` | 21 |
| `Articles` `Status <> 2` sin demo | 1.374 |
| `MeasurementUnits` `UNI-05` | 1 |
| `Equipments` con `UsefulLifeYears` | 121 (distribución 6:28, 8:7, 10:48, 12:38) |
| `ArticleStocks` (con D5) | 228 + 605 = 833; 4 actualizadas; 133 con `STOCK_NOT_IN_SOURCE` |
| `Plannings` con `Code LIKE 'SOL3-%'` | 1.028 (1.037 − 9 de D2) → 2026-1: 550, 2026-2: 478 |
| `PlanningRequirements` de esas planificaciones | 11.297 (11.451 − 154 de las 9 excluidas) |
| `Plannings` con periodo asignado | 996 menos las excluidas que tuvieran horario (informar) |
| `PracticeSchedules` creadas por D8 | 456 menos las excluidas (informar) |
| Textos L-6/L-7/L-8 | `CHECKSUM_AGG` sin cambios |
| Nombres de materias/artículos/actores existentes | Sin cambios |
| Códigos guardados en minúscula (`COLLATE Latin1_General_CS_AS <> UPPER(Code)`) | 0 |
| Segunda pasada | 0 inserciones, 0 incidencias nuevas |

### T5. Prueba funcional
- `dotnet build -m:1`. Si el ejecutable está bloqueado por un servidor del usuario, no matarlo: compilar con `-o %TEMP%\labbuild` y usar otro puerto.
- Planificación de 2026-2: el calendario muestra las 478 solicitudes en su semana y día; las 258 "Finalizada" en solo lectura y las 220 "Aprobada" en Cierre de práctica.
- 2026-1 (gestión completada): visible en modo consulta (`AcademicManagementService.ReadOnlyMessage`).
- Disponibilidad y Demanda con las existencias nuevas (ECONO como origen y total institucional).
- Catálogos: las 22 materias, los 6 docentes y una muestra de artículos `ART-L33-*` con su badge "Por confirmar".
- Cerrar el servidor y comprobar con `netstat` que no queda ningún proceso propio.

### T6. Documentación
- `docs/GUIA_IMPORTACION_HISTORICA.md`: agregar la V12 con su hash, alcance y resultados.
- `docs/MAPA_EXCEL_V4_A_BD.md`: §3.6–3.7 (actores), §3.8 (vida útil), §3.18 (materias), §3.23 (artículos), §3.25 (existencias) y §3.26–3.31 (circuito L-3 → `Plannings`; salidas y devoluciones eliminadas por la clienta).
- No commitear salvo que la misión lo pida; si se pide, solo los archivos de esta tarea con rutas explícitas.

---

## 7. Lo que NO hay que hacer
- No reimportar ni actualizar nada histórico (L-6, L-7, L-8, L-48, unidades, catálogo salvo `UsefulLifeYears`, actores existentes) desde este Excel.
- No reescribir nombres existentes con el texto en "tipo oración".
- No importar `SolicitudesL3` a `L3Requests` (tabla que el refactor renombra) ni crear la migración del refactor dentro de esta tarea.
- No crear salidas, devoluciones ni incidentes a partir de las solicitudes.
- No borrar existencias, artículos ni datos de prueba; no hacer hard-delete.
- No crear prácticas ni recetas para las 22 materias nuevas (D8).
- No inventar fechas, horas, laboratorios ni cantidades. No redondear horarios.
- No editar migraciones existentes. No tocar la base oficial ni la nube. No modificar el Excel.

## 8. Pendientes para pedir a la clienta (no bloquean el Bloque A)
1. ¿Las 125 existencias de la V7 que ya no aparecen (por ejemplo, Bowl mediano 80, Boquilla 88) se dieron de baja o se omitieron por error?
2. Fecha (9) y laboratorio (4) de las solicitudes incompletas de 2026-1 (SOL3-0016…0023 y 0312).
3. Las 22 solicitudes que empiezan a las 15:45 (¿15:55?) y las 7 que terminan a las 13:15 o 10:25.
4. Fechas reales de inicio de clases de 2026-1 y 2026-2 (para numerar bien las semanas).
5. Prácticas y recetas de las 22 materias nuevas (o autorización para derivarlas de los pedidos L-3).
6. Unidad de las cantidades de insumos (102 líneas ≥ 100: ¿gramos?).
7. `ACT-0052` "Ing. Zabala": ¿es `ACT-0037` Alberto Zabala?
8. Los dos "Paila para chicharron" (`ART-L33-0007` y `ART-L33-0256`): ¿son el mismo artículo?
9. Los pendientes de la V7 siguen abiertos: semana 6 de 2027-1 con fechas de la semana 3, horas vacías en `Planificacion`, estado de 2027-1 ("Cerrada", aunque es futura) y reexportar sin perder siglas ni saltos de línea.
