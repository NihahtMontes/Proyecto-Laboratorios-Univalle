# Instrucciones para el agente: `BaseEstructuraFinal_V7.xlsx` (bloque académico)

**Fecha:** 2026-10-05 · **Base de comparación:** `BaseEstructuraFinal_V4.xlsx`, ya importada (commits `dd4ef99`, `66e1368`, `ef5eaca`, `bfa1148`). La V3 no se usa para nada.
**Análisis hecho en solo lectura:** Excel con `openpyxl` (comparación celda por celda V4 → V7) más `SELECT` sobre `DB_Laboratorios_Univalle_DEMO_QA` (migración `20261004214711_AddSubjectKit`).
**Estado: EJECUTADO en DEMO_QA el 2026-10-05** (resultados y hallazgos en `docs/GUIA_IMPORTACION_HISTORICA.md`, sección V7).
**Revisión del orquestador (2026-10-05):** se verificaron contra el Excel, la BD y el código los conteos, los índices y el impacto en el módulo de planificación. Se corrigieron la regla de la semana 6 (la anterior marcaba 48 filas, no 33), la columna del *checksum* de L-8 y la prueba funcional T5, y se agregaron D12, §2.1 y el respaldo previo a T3.

| Archivo | SHA-256 |
|---|---|
| `C:\Users\monte\Downloads\BaseEstructuraFinal_V4.xlsx` | `8ebb238c24a7ce94a71ff0506675fdc216db8d2831b7c24d27f7aab5ff1eb9d2` |
| `C:\Users\monte\Downloads\BaseEstructuraFinal_V7.xlsx` | `c677c7cc81d63f5e74b00daab60c214be032c5089b1876c61b9ddb66dc4320e9` |

Lectura previa obligatoria: `AGENTS.md`, `context.md`, `docs/MAPA_EXCEL_V4_A_BD.md` (§1 reglas globales y §2 traducción de valores, que siguen vigentes), `docs/PLAN_MIGRACION_V4.md` (D3, D4) y `Tools/ExcelImport/gen_v4_academic_sql.py`. Para entender cómo se consume el stock: `Services/ArticleAvailabilityService.cs`.

---

## 0. Regla principal

**De la V7 solo se importa el bloque académico. Todo lo demás sigue saliendo de la V4.**

La V7 tiene el mismo contenido histórico que la V4, pero alguien aplicó "Tipo Título" (`NOMPROPIO`) a **todo el libro**: códigos, encabezados, estados y textos. Además, en los textos largos los saltos de línea se reemplazaron por espacios. Si se reimporta el histórico desde la V7, **se degradan datos ya cargados**.

| Se importa de la V7 | Se ignora de la V7 |
|---|---|
| `Practicas`, `ProgramacionPracticas`, `RequerimientosPractica`, `ExistenciasArticulos` (solo filas con datos) y la limpieza de `Articulos` (fusiones y artículos nuevos) | Sedes, Facultades, Carreras, Laboratorios, Gestiones, Actores, ActorRoles, CatalogoEquipos, UnidadesFisicas, L-6, L-7, SolicitudUnidades, L-8, MantSolicitudes, MantParticipantes, PlanL48, Materias, CategoriasArticulo, UnidadesMedida, todo L-3 (vacío) y las hojas `_*` |

## 1. Qué cambió de la V4 a la V7 (verificado)

### 1.1 Sin cambio real (solo formato)
Todas las hojas institucionales, de inventario e históricas son **idénticas a la V4** si se comparan sin distinguir mayúsculas. Los únicos cambios son de formato:
- **Códigos:** `CAT-0001` → `Cat-0001`, `ACT-0046` → `Act-0046`, `ART-REQ-00001` → `Art-req-00001`, `PRA-BAS100-01` → `Pra-bas100-01`, `CBBA` → `Cbba`.
- **Encabezados:** `PracticaCodigo` → `Practicacodigo`. **El generador V4 compara los encabezados exactos y se detiene.**
- **Textos y estados:** "Por confirmar" → "Por Confirmar", "Sí"/"No".
- **Saltos de línea perdidos:** 1.192 `TrabajoRealizado` de L-8, 494 `ProblemaReportado`/`Sugerencia` de L-7 y 511 `ProblemasObservados` de L-6. **No tocar esos registros en la BD.**

### 1.2 Datos nuevos

| Hoja | V4 | V7 | Notas |
|---|---:|---:|---|
| `Practicas` | 0 | 196 | Columnas nuevas: `PracticaCodigo, MateriaCodigo, NumeroPractica, Descripcion, Estado, Observaciones`. Se quitaron `EstadoCobertura`, `MetodoCobertura` y `FuenteReferencia`. Todas tienen `Estado` "Activo". `NumeroPractica` es texto (`1`…`16`, `11.1`, `11.2`, `E1p`, `E2p`, `E3p`). 2 descripciones superan los 200 caracteres (máximo 241) |
| `ProgramacionPracticas` | 0 | 422 | Gestión `2027-1`, 19 materias, 41 grupos, 15 docentes (ACT-0037…0051, todos con rol Docente), 8 laboratorios (H-01…H-07, K-01). `HoraInicio`/`HoraFin` vacías. `Estado` siempre "Programada", un valor que asumió el autor del Excel |
| `RequerimientosPractica` | 0 | 2.111 | 180 de 196 prácticas, 493 artículos. Sin FK rotas ni duplicados práctica+artículo. 11 cantidades vacías. `EstadoValidacion`: Validado Historico 1.529, Referencia - Revisar Docente 372, Validado Por Practica 169, Validado Plan 41 |
| `ExistenciasArticulos` | 0 | 1.063 | **Solo 220 filas tienen laboratorio y cantidad** (Econo 114, H-03 26, K-01 16, H-06 14, H-01 13, H-05 12, H-04 11, H-02 8, H-07 6). Las otras 843 tienen laboratorio y cantidad en NULL. No hay `FechaActualizacion` |
| `Articulos` | 1.023 | 1.002 | 24 duplicados fusionados, 1 nota del formato L-3 eliminada (`ART-REQ-00348`) y 4 artículos nuevos (`ART-REQ-00566…00569`). Detalle en `_POBLADO_REQUERIMIENTOS` §C |
| `Gestiones` | 26 | 27 | Se agrega `2027-1`, que **ya existe en la BD** |
| `UnidadesMedida` | 3 | 5 | Se agregan `UNI-04` Rollo y `UNI-05` Metro, pero **ningún artículo las usa** |

### 1.3 Errores de la fuente que hay que registrar, no corregir
1. **La semana 6 tiene las fechas de la semana 3.** Las 33 programaciones con `Semana = 6` tienen `FechaEjecucion` del 09-03 al 12-03-2027, que son las fechas de la semana 3. La semana 5 va del 23-03 al 26-03 y la 7 del 30-03 al 02-04, así que la 6 rompe el orden cronológico. Por eso, en 26 casos un mismo grupo tiene dos prácticas el mismo día (por ejemplo, `PROG-0040` y `PROG-0043`, ambas de FUN100-A el 09-03). La fecha correcta no se puede deducir.
2. **2 programaciones con fecha posterior a la gestión:** la más tardía es el 02-07-2027 y la gestión termina el 30-06.
3. **`FactorMateria` varía dentro de TAL300-A y TAL300-B** (vale 1 o 5 según la fila).
4. **4 casos de un docente en 2 laboratorios el mismo día.** Además hay hasta 8 programaciones en un mismo laboratorio y día; sin horas no se puede saber si chocan.
5. **11 recetas sin cantidad.**

## 2. Estado actual de la BD (DEMO_QA, verificado)

- **Esquema:** **no hace falta ninguna migración.** `Practices` (incluido `Name` NOT NULL de 200, de `AddPracticeName`), `PracticeSchedules`, `PracticeRequirements` (con `Code`), `ArticleStocks` y `CourseGroups` ya existen.
- **Índices únicos que hay que respetar:**
  - `IX_Practices_Code`
  - `IX_PracticeSchedules_Code`
  - `IX_PracticeSchedules_ManagementId_PracticeId_GroupName` (filtro `IsDeleted = 0`)
  - `IX_PracticeRequirements_PracticeId_ArticleId` (filtro `IsRemoved = 0`)
  - `IX_PracticeRequirements_Code`
  - `IX_ArticleStocks_LaboratoryId_ArticleId`
  - `IX_CourseGroups_ManagementId_SubjectId_Name` (filtro `Status <> 2`)
  - `UX_Articles_Name_Active` (filtro `Status <> 2`)
- **Materias:** las 19 materias reales ya están cargadas en MAYÚSCULAS. Las 8 `GAS-*` están dadas de baja (`Status = 2`).
- **Prácticas actuales:** `PRA-GAS101-P1` y `PRA-GAS210-P1` están dadas de baja. `PRC-00001` ("Primera practica") y `PRC-00002` ("hola") las crearon pruebas manuales; `PRC-00002` tiene la programación `PRG-00001` (2026-2), a la que apunta el `L3Request` Id 6 (la práctica planificada `PRA-00006`, usada para probar el módulo de planificación). **No tocarlas** (ver D7).
- **Códigos que genera la aplicación:** `Helpers/CatalogCodes.cs` genera `PRC-00001`, `GRP-00001`, etc., y solo considera los sufijos numéricos. Los códigos importados (`PRA-BAS100-01`, `GRP-2027-1-…`, `PROG-0001`, `REQ-0001`) no colisionan con ellos (verificado). Las prácticas planificadas (`L3Requests`) se muestran como `PRA-00006` (`L3Codes.Format`), que no es un código guardado pero se parece al de la práctica de catálogo: es un riesgo de confusión en la interfaz, no de datos.
- **Artículos:** los 998 que comparten código con la V7 coinciden en estado (Activo → `ReviewStatus` 0; Por confirmar → 9). Los 25 que la V7 elimina están `Status = 0` y **no tienen dependencias**: 0 recetas, 0 stock, 0 `DepartureItems`, 0 `L3RequestDetails`, 0 `SubjectKitItems`. Los 4 nuevos no existen.
- **Stock actual:** 8 filas en ECONO (artículos demo `ART-0001…0008` y `ART-REQ-00462`).
- **Gestión `2027-1`:** `Status = 1` (Inactiva), del 01-02-2027 al 30-06-2027. No tiene `AcademicPlanning`, así que `AcademicWeekId` quedará NULL.
- **Intercalación:** `Modern_Spanish_CI_AS`, así que los `JOIN` por código no distinguen mayúsculas. Aun así, **todo código que se guarde va en MAYÚSCULAS**.

### 2.1 Impacto en el módulo de planificación (verificado en el código)
- **Planificación, Disponibilidad y Demanda leen `L3Requests`, no `PracticeSchedules`.** Las 422 programaciones importadas **no aparecerán** en el calendario ni en la vista semanal de Disponibilidad hasta que exista la `AcademicPlanning` de 2027-1 y cada programación se convierta en una práctica planificada. Que no aparezcan es lo esperado; no es un error de la importación.
- **El wizard choca con las programaciones importadas.** `Pages/LaboratoryOperations/Practica.cshtml.cs` crea un `PracticeSchedule` nuevo para cada práctica planificada y rechaza la combinación gestión + práctica + grupo si ya existe ("El grupo X ya tiene programada esta práctica en la gestión"). Cuando se cree la planificación 2027-1, **no se podrá planificar ninguna de las 422 programaciones importadas** hasta que el wizard reutilice la programación existente (vincular `L3Request.PracticeScheduleId` en lugar de crear otra). **No bloquea la importación**, pero es una tarea de código obligatoria antes de usar 2027-1. El agente de esta tarea **no** la hace.
- **Stock por laboratorio.** `ArticleAvailabilityService` compara contra un solo laboratorio de origen (ECONO por defecto). Con la V7, de los 493 artículos usados en recetas, **48 tienen stock en ECONO y 82 en algún laboratorio** (ECONO o H-01…K-01). Ver D12.
- **`AcademicWeekId` quedará NULL** en las 422 programaciones porque 2027-1 no tiene semanas. Cuando se cree la planificación habrá que vincular cada `WeekNumber` con su `AcademicWeek` (tarea posterior), teniendo en cuenta que la semana 6 es sospechosa.

## 3. Decisiones (valores por defecto; el usuario puede cambiarlas antes de ejecutar)

| ID | Tema | Decisión por defecto |
|---|---|---|
| D1 | ¿Importar la V7 o esperar una reexportación sin "Tipo Título"? | Importar ya el bloque académico de la V7 normalizando los códigos. Si llega una V8 limpia, el generador debe poder volver a ejecutarse (idempotente) |
| D2 | Textos de la V7 con "Tipo Título" | Los **códigos** se pasan a MAYÚSCULAS. Las **descripciones, observaciones y nombres nuevos** se cargan como vienen, recortando espacios de los bordes (es la única fuente). **Nunca se actualizan textos que ya existen en la BD** (materias, artículos, actores) |
| D3 | Semana 6 | Se carga tal cual viene, con una incidencia `SCHEDULE_DATE_SUSPECT` (Severity 1) en cada una de las 33 filas. No se inventa la fecha |
| D4 | Existencias | Solo se cargan las 220 filas que tienen laboratorio **y** cantidad. Las 843 vacías se descartan y se informa el total en el reporte, sin crear 843 incidencias. ECONO es un laboratorio válido (`Tipo` Almacén) |
| D5 | `UNI-04`/`UNI-05` | No se cargan. Ningún artículo las usa y `ROL` "Rollo" ya existe |
| D6 | Gestión 2027-1 | No se cambian su estado ni sus fechas. Se registra una incidencia informativa si alguna `FechaEjecucion` queda fuera de `StartDate`–`PlannedEndDate` |
| D7 | `PRC-00001`, `PRC-00002`, `PRG-00001` | No se tocan. Se informan como datos de prueba pendientes de que el usuario decida |
| D8 | 24 duplicados + la nota `ART-REQ-00348` | Baja lógica (`Status = 2`, `LastModifiedDate`). Antes de dar de baja, mover las referencias que hubiera al código conservado (hoy son 0, pero el script debe hacerlo por si acaso) |
| D9 | 4 artículos nuevos | `Status = 0`, `ReviewStatus = 9` (`DataReviewStatus.PorConfirmar`). Categoría y unidad según la V7. También se llenan las columnas *legacy* `Category` y `UnitOfMeasure` con el **nombre** (igual que en la V3/V4) |
| D10 | `Practice.Name` | `LEFT(Descripcion, 200)`, la misma regla de `AddPracticeName`. Si la descripción supera los 200 caracteres, se registra la incidencia `VALUE_TRUNCATED_IN_NAME` y `Description` conserva el texto completo |
| D11 | `CoverageStatus`, `CoverageMethod`, `SourceReference` | NULL (la V7 ya no los trae) |
| D12 | Contra qué stock compara la planificación | **No es parte de esta importación.** Las existencias se cargan por laboratorio tal como vienen. Recomendación para la tarea de código posterior: que la vista semanal de Disponibilidad compare contra el **total institucional** (ECONO + laboratorios) y que el selector de origen permita seguir viendo un solo laboratorio. Pendiente de confirmación del usuario |

## 4. Tareas, en orden

### T0. Preparación (solo lectura)
1. `git status`: **hay unos 60 archivos modificados o nuevos y 3 migraciones sin commitear** (`AddPracticeName`, `AddPlanningCampus`, `AddSubjectKit`) de otras líneas de trabajo, ya aplicadas en DEMO_QA. Entre ellos están el módulo de disponibilidad (`Services/ArticleAvailabilityService.cs`, `Pages/LaboratoryOperations/*`) y `docs/PROPUESTA_VINCULO_ARTICULO_EQUIPO.csv`. **No revertir, no formatear y no hacer commit de esos archivos.** Esta tarea solo crea `Tools/ExcelImport/gen_v7_academic_sql.py` y modifica los dos `docs` de T6.
2. Verificar el SHA-256 de la V7 (tabla de arriba).
3. Comprobar que `DB_NAME() = 'DB_Laboratorios_Univalle_DEMO_QA'` y que la última migración es `20261004214711_AddSubjectKit`.

### T1. Generador nuevo `Tools/ExcelImport/gen_v7_academic_sql.py`
Hay que copiar `gen_v4_academic_sql.py` y adaptarlo. **No modificar el generador V4.**

1. **Constantes:**
   - `XLSX` = ruta de la V7; `EXPECTED_SHA` = hash de la V7.
   - `OUT` = `%TEMP%\mm\import_v7_academic.sql`.
   - `batch_code = "XLSX-V7-ACADEMIC-" + sha[:12].upper()`, `SourceName = 'BaseEstructuraFinal_V7.xlsx'`, `ContractVersion = 'base-estructural-v7'`.
2. **Encabezados sin distinguir mayúsculas:**
   - Comparar con `h.strip().casefold()` contra los nombres canónicos y devolver los diccionarios con la clave canónica (`PracticaCodigo`, …).
   - Si falta alguna columna esperada, detenerse.
   - Nuevos encabezados esperados para `Practicas`: `PracticaCodigo, MateriaCodigo, NumeroPractica, Descripcion, Estado, Observaciones`.
3. **Normalización de códigos:** aplicar `.strip().upper()` a todas las columnas `*Codigo` (incluidas `DocenteActorCodigo`, `LaboratorioCodigo` y `GestionCodigo`) y a `Grupo`. `NumeroPractica` también va en mayúsculas (`E1P`).
4. **Booleanos y estados:** comparar sin distinguir mayúsculas ni tildes: `Sí/Si/No`, `Programada`, `Activo`. `as_bool` ya lo hace con `.lower()`; incluir `"sí"`.
5. **Quitar del generador V7:**
   - la sección 1 (Materias: ya están; **no** actualizar su `Name`);
   - la sección 2 (retiro de demo: ya está hecho).
   - Dejar solo una verificación: las 19 `MateriaCodigo` de la V7 existen en `Subjects` con `Status = 0`; si no, detenerse.
6. **Prácticas (§3):** insertar `Code`, `SubjectId`, `Name` (D10), `Number`, `Description`, `Observations`, `Status = 0`, `CreatedDate`. `UPDATE` idempotente solo si `Description`, `Number` o `Observations` cambiaron. **Nunca** tocar `PRC-*` ni `PRA-GAS*`.
7. **Limpieza de artículos (nuevo, va antes de las recetas):**
   - Tabla temporal con el mapeo de `_POBLADO_REQUERIMIENTOS` §C: 24 pares (conservado ← eliminado) y la nota `ART-REQ-00348`. Leerla de la hoja o escribirla literal en el script, pero **verificar que son 24 + 1**.
   - Verificar en Python que **ninguna fila** de `RequerimientosPractica` ni de `ExistenciasArticulos` de la V7 use uno de los 25 códigos eliminados. Si alguna lo hace, detenerse.
   - Mover las referencias de `PracticeRequirements`, `ArticleStocks`, `SubjectKitItems`, `DepartureItems` y `L3RequestDetails` del eliminado al conservado, solo si no generan un duplicado; si lo generan, registrar una incidencia y no moverlas.
   - Después, `Status = 2` en los 25.
   - Insertar los 4 artículos nuevos (D9) en lotes, cuidando `UX_Articles_Name_Active`.
8. **Recetas (§4):** igual que en la V4, con `Code` en mayúsculas (`REQ-0001`). `Cantidad` NULL → incidencia `VALUE_MISSING` (11).
9. **Existencias (§5):** filtrar en Python las filas con `LaboratorioCodigo` y `CantidadDisponible`. `UpdatedOn = NULL`. Si ya existe la fila `(lab, artículo)`, solo actualizar cuando la cantidad sea distinta y registrar `CONTENT_CONFLICT` con el valor anterior.
10. **Programación (§6):**
    - Grupos derivados con código `GRP-2027-1-<MATERIA>-<GRUPO>` (41).
    - `PracticeSchedules` (422) con `Code` `PROG-0001…`.
    - Incidencias nuevas:
      - `SCHEDULE_DATE_SUSPECT` en las 33 filas de `Semana = 6`. **Regla de detección (cronológica, no por número fijo):** ordenar las semanas por número y calcular la fecha mínima y máxima de cada una. Una semana es sospechosa si su fecha mínima no es posterior a la fecha máxima de la última semana anterior **no sospechosa**, o si su fecha máxima no es anterior a la fecha mínima de la siguiente. Se marcan todas sus filas. Con la V7 solo sale la semana 6 (09-03…12-03, entre la 5, que termina el 26-03, y la 7). El generador debe detenerse si el total no es 33. **No usar** la regla "la misma materia, grupo y fecha aparece en otra semana": marca 48 filas (25 de la semana 3, que es correcta, y 23 de la 6) y deja sin marcar 10 de la 6.
      - `SCHEDULE_OUT_OF_MANAGEMENT` en las 2 fechas posteriores al fin de la gestión.
      - `VALUE_PENDING_CONFIRMATION` para la hora, una por grupo y no por fila.
      - `COURSE_GROUP_VALUES_VARY`, que ya existe, para TAL300.
11. **Incidencias:** igual que en la V4 (lotes de 100, idempotentes por `ImportBatchId + IssueCode + EntityKey + FieldName`).
12. Cada bloque va en su propia transacción y en lotes de 100 como máximo, con `SET XACT_ABORT ON` y el `THROW` si el destino no es DEMO_QA.

### T2. Generar el SQL y revisarlo antes de ejecutar
- Ejecutar el generador y revisar los conteos que imprime: prácticas 196, programaciones 422, recetas 2.111 y existencias 220 (de 1.063 leídas).
- Hacer `grep` en el SQL de `UPDATE dbo.Subjects`, `UPDATE dbo.Verifications`, `Requests` y `Maintenances`: **no debe aparecer ninguno**.

### T3. Ejecutar en DEMO_QA (requiere autorización explícita del usuario para escribir)
- **Antes de la primera ejecución:** respaldo `BACKUP DATABASE DB_Laboratorios_Univalle_DEMO_QA TO DISK = N'<ruta local>\DEMO_QA_pre_V7_<fecha>.bak' WITH COPY_ONLY, CHECKSUM` y `RESTORE VERIFYONLY` del archivo. Informar la ruta. Sin un respaldo verificado no se ejecuta.
- Guardar también antes los valores de control de T4: el *checksum* de los textos históricos, los conteos y las filas de `PRC-00001/00002` y `PRG-00001`.
- Ejecutar con `sqlcmd -S localhost -E -C -d DB_Laboratorios_Univalle_DEMO_QA -b -f 65001 -i <sql>`, con un solo proceso a la vez. **`-f 65001` es obligatorio:** sin él, `sqlcmd` lee el archivo UTF-8 como ANSI y daña las tildes (pasó en la primera ejecución del 2026-10-05; se corrigieron 481 filas en el lugar).
- **Ejecutarlo dos veces:** la segunda pasada debe insertar 0 filas y 0 incidencias nuevas.

### T4. Verificación (criterios de aceptación)

| Comprobación | Esperado |
|---|---|
| `Practices` con código `PRA-%` excepto `PRA-GAS%`, `Status = 0` | 196 |
| `PracticeSchedules` de `2027-1` | 422 |
| `CourseGroups` `GRP-2027-1-%` activos | 41 |
| `PracticeRequirements` con `Code LIKE 'REQ-%'` y `IsRemoved = 0` | 2.111 |
| `ArticleStocks` | 8 previas + 220 = 228 (menos las que coincidan con las previas: informar cuántas). Por laboratorio: ECONO 114 (+ las previas), H-03 26, K-01 16, H-06 14, H-01 13, H-05 12, H-04 11, H-02 8, H-07 6 |
| Artículos de receta con stock | 48 en ECONO, 82 en algún laboratorio (de 493) |
| `Articles` `Status <> 2`, sin contar demo | 1.002 (1.023 − 25 + 4) |
| Incidencias `SCHEDULE_DATE_SUSPECT` | 33 |
| Incidencias `VALUE_MISSING` en recetas | 11 |
| Prácticas `PRC-00001/00002` y programación `PRG-00001` | Sin cambios |
| Textos de L-6/L-7/L-8 | Sin cambios: comparar `CHECKSUM_AGG(CHECKSUM(...))` antes y después de `Maintenances.Description` (L-8; la columna `WorkPerformed` no existe), `Verifications.Observations` (L-6) y `Requests.Description`/`Suggestion` (L-7) |
| Materias | `Name` sin cambios (siguen en MAYÚSCULAS) |
| Códigos guardados en minúscula | `SELECT ... WHERE Code COLLATE Latin1_General_CS_AS <> UPPER(Code)` sobre las tablas tocadas → 0 |

### T5. Prueba funcional
- `dotnet build -m:1` (sin servidores en segundo plano mientras se compila). Si el `.exe` está bloqueado por un servidor del usuario, **no matarlo**: compilar con `-o %TEMP%\labbuild` y levantar esa copia en otro puerto.
- **Planificación y Disponibilidad no sirven para verificar esta carga** (ver §2.1: leen `L3Requests`, y 2027-1 no tiene planificación). Comprobar en cambio:
  - **Catálogos académicos → Prácticas:** aparecen las 196 prácticas con su materia, número y nombre, y el detalle muestra la receta.
  - **Buscador de requerimientos** de la práctica planificada `PRA-00006` (`Requerimientos?planId=6`). **Solo abrir el buscador, no agregar nada.** Los artículos con existencias muestran el número en la columna Stock.
  - Un `SELECT` que una las `PracticeSchedules` de 2027-1 con `Practices`, `CourseGroups`, `Persons` y `Laboratories`, sin nulos inesperados.
- Cerrar el servidor al terminar y comprobar que no queda ningún proceso propio (`netstat` del puerto usado).

### T6. Documentación
- `docs/GUIA_IMPORTACION_HISTORICA.md`: agregar la V7 con su hash, alcance (solo académico) y el motivo (formato "Tipo Título").
- `docs/MAPA_EXCEL_V4_A_BD.md`: en §3.19–3.25, poner como fuente la V7 y el estado ✅ o 🟡.
- **No commitear** salvo que la misión lo pida. Si se pide, el commit debe incluir solo los archivos de esta tarea (`git add` con rutas explícitas).

## 5. Lo que NO hay que hacer
- No reimportar ni actualizar nada histórico (L-6, L-7, L-8, L-48, unidades, catálogo, actores) desde la V7.
- No actualizar el `Name` de materias, artículos ni actores existentes con el texto de la V7.
- No crear `AcademicPlanning` ni semanas para 2027-1: es una decisión de negocio aparte.
- No crear `L3Requests` a partir de las programaciones, ni tocar el wizard (`Pages/LaboratoryOperations/*`) ni `Services/ArticleAvailabilityService.cs`: los ajustes de §2.1 y D12 son una tarea de código aparte.
- No inventar horas, fechas, estados ni cantidades.
- No hacer hard-delete: todo se da de baja con `Status = 2` o `IsRemoved = 1`.
- No editar migraciones existentes ni crear migraciones nuevas: no hacen falta.
- No tocar la base `DB_Laboratorios_Univalle` (oficial) ni la nube.
- No modificar el Excel.

## 6. Pendientes para pedir a la clienta (no bloquean la carga)
1. Reexportar el Excel sin "Tipo Título".
2. Las fechas reales de la semana 6.
3. `HoraInicio`/`HoraFin` de la programación.
4. Stock de H-01…H-07 y K-01 para los artículos de recetas: tras la carga, 410 de 493 siguen sin stock en ningún laboratorio y 444 sin stock en ECONO (ART-REQ-00462 ya tenía stock previo).
5. Confirmar los 387 artículos "Por Confirmar" que se usan en recetas y las 372 recetas "Referencia - Revisar Docente".
6. Estado de la gestión 2027-1 (el Excel dice "Cerrada", aunque es futura).
7. Vínculo de los artículos de categoría Equipo con su catálogo (`docs/PROPUESTA_VINCULO_ARTICULO_EQUIPO.csv`), para que su stock salga de las unidades físicas.

## 7. Formato del reporte del agente
1. Archivos creados o modificados (deben ser solo `Tools/ExcelImport/gen_v7_academic_sql.py` y los dos `docs`).
2. Salida del generador (conteos).
3. Resultado de las 2 ejecuciones (filas por bloque; la segunda en 0).
4. Tabla de T4 con los valores obtenidos.
5. Incidencias por `IssueCode`.
6. Decisiones propias tomadas fuera de este documento, omisiones, riesgos y todo lo `UNVERIFIED`.
