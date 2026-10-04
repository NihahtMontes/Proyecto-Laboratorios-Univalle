# Instrucciones para el agente: `BaseEstructuraFinal_V3_ARTICULOS.xlsx`

**Fecha:** 2026-10-04 · **Para:** el agente que ejecuta la Fase A, ya commiteada (`f4e5449`), y la Fase B/C, en curso con la migración `20261004051440_AcademicAndArticleCatalogs`, todavía sin commitear pero ya aplicada en DEMO_QA.
**Análisis hecho en solo lectura:** Excel con `openpyxl` + `SELECT` sobre `DB_Laboratorios_Univalle` y `DB_Laboratorios_Univalle_DEMO_QA`.

| Archivo | SHA-256 |
|---|---|
| `BaseEstructuraFinal.xlsx` (V1) | `e78cfce6a4764ca67841c4c70a0de0c296b33fa8f3f8721b6b2dc2c3cf010ad8` |
| `BaseEstructuraFinal_V3_ARTICULOS.xlsx` (V3) | `1e70f5e4cbf9b298ee3690bfc65e4a0aec07efae109ca398aa10b774ca87680e` |

---

## 0. Regla principal

**A partir de ahora la única fuente es la V3. La V1 queda obsoleta.** La V3 contiene todo lo de la V1 sin cambios y agrega datos nuevos. Hay que registrar el hash de la V3 en `docs/GUIA_IMPORTACION_HISTORICA.md`.

## 1. Qué cambia de la V1 a la V3 (verificado celda por celda)

| Hoja | V1 | V3 | Cambio |
|---|---:|---:|---|
| Sedes, Facultades, Carreras, Laboratorios, Gestiones, CatalogoEquipos, UnidadesFisicas, L-6, L-7, SolicitudUnidades, L-8, MantSolicitudes, MantParticipantes, Costos, PlanL48 | — | — | **Idénticas.** La Fase A no necesita rehacerse |
| `Actores` | 36 | **51** | +15 docentes ACT-0037 a ACT-0051. Las 36 filas originales no cambian |
| `ActorRoles` | 25 | **40** | +15 roles `Docente` (AR-0026 a AR-0040) |
| `CategoriasArticulo` | 0 | **10** | CAT-ART-01 a CAT-ART-10 |
| `UnidadesMedida` | 0 | **3** | UNI-01 Juego, UNI-02 Paquete, UNI-03 Pieza |
| `Articulos` | 0 | **1.023** | Columnas: `ArticuloCodigo, Nombre, CategoriaCodigo, UnidadMedidaCodigo, Estado` |
| `Materias`, `GruposMateria`, `Practicas`, `ProgramacionPracticas`, `RequerimientosPractica`, `ExistenciasArticulos`, todo L-3 | 0 | 0 | **Siguen vacías** |
| `_POBLADO_DOCENTES/CATEGORIAS/UNIDADES/ARTICULOS` | — | nuevas | Bitácoras de poblado. **No se importan** (empiezan con `_`) |

Calidad de los datos nuevos (comprobado):

- PK sin duplicados.
- `Nombre` único sin distinguir mayúsculas ni acentos.
- FKs a categoría y unidad sin huérfanos.
- Nombre de hasta 92 caracteres (cabe en `Article.Name` de 200).
- Código de hasta 13 caracteres (cabe en `Code` de 30).

## 2.0 Aclaraciones del usuario (2026-10-04), que prevalecen sobre §2

1. `_INDICE` y `_DICCIONARIO` no se actualizaron. **`TipoArticulo` se quitó a propósito** porque era redundante con `CategoriaArticulo`. **`CatalogoCodigo` se quitó** porque apuntaba a Equipos y no aplica a artículos. → `Article.ArticleType` y `Article.EquipmentId` quedan en NULL y no se generan incidencias por los 13 artículos que coinciden con el catálogo. Si `ArticleType` es redundante, conviene retirarlo de la interfaz o eliminarlo en la próxima migración.
2. Las unidades se homologaron a propósito (jgo/juego/juegos → Juego; paq → Paquete; pza/pieza → Pieza). Se usan los 3 códigos de la hoja; las 5 unidades de la bitácora no cuentan.
3. "Por confirmar" en `Articulos.Estado` es el valor **literal de la base de la ingeniera**: no dice si el artículo está activo. → Se conserva como `ReviewStatus = PendingConfirmation`. No es un `Status` real de ciclo de vida.

## 2. Inconsistencias del propio Excel: no confiar en la documentación interna

1. **`_DICCIONARIO` e `_INDICE` están desactualizados respecto de `Articulos`.**
   - Anuncian 7 columnas (`TipoArticulo`, `CatalogoCodigo`, `Activo` boolean).
   - La hoja real tiene 5 columnas y `Estado` es texto (`Activo` 607 / `Por confirmar` 416).
   - La bitácora `_POBLADO_ARTICULOS` dice que se generaron `TipoArticulo` (EQUIPO 100 / UTENSILIO 923) y 3 `CatalogoCodigo`, **pero esas columnas se eliminaron después**.
   - **Hay que importar según los encabezados reales de la hoja, no según el diccionario.**
2. **`UnidadesMedida`:** `_INDICE` y `_POBLADO_UNIDADES` dicen 5 unidades (`UNI-01` = "jgo de 3" ... `UNI-05` = "pieza"), pero la hoja tiene 3 (`UNI-03` = **Pieza**). **Los códigos de la bitácora no sirven.** Solo vale la hoja; la sección B de `_POBLADO_ARTICULOS` coincide con ella.
3. **Se perdieron `TipoArticulo` y `CatalogoCodigo`.** No hay que deducirlos de la categoría `CAT-ART-05 "Equipo"`: tiene 115 artículos, la bitácora decía 100 de tipo EQUIPO, y varias categorías se asignaron por heurística ("heredada de artículos que empiezan por 'PLATO'"). Hay que pedir al usuario la versión intermedia que conservaba esas columnas o que confirme que se quitaron a propósito.

## 3. Tareas, en orden

### T1. Migración nueva y aditiva: estado de revisión del artículo

`Article.Status` es `GeneralStatus` (Activo/Inactivo/Eliminado) y **no tiene valor "Por confirmar"**. No hay que agregar `PorConfirmar` a `GeneralStatus`: ese enum lo comparten unas 12 tablas con `CHECK`.

- Agregar a `Article` una columna `ReviewStatus` (enum nuevo `DataReviewStatus { Confirmed = 0, PendingConfirmation = 9 }`, `Display` "Confirmado"/"Por confirmar"), NOT NULL con default 0 y su `CHECK`.
- Hacerlo en una **migración nueva**. `AcademicAndArticleCatalogs` ya está aplicada en DEMO_QA, así que no hay que editarla. Si se prefiere fusionarla, primero revertirla en DEMO_QA con `dotnet ef database update 20261004050109_MasterDataAndPendingValues`, y solo porque no está commiteada.
- Importación: `Estado = "Activo"` → `Status = Activo` y `ReviewStatus = Confirmed`. `Estado = "Por confirmar"` → `ReviewStatus = PendingConfirmation`, más un `DataQualityIssue` por fila. `Status` es NOT NULL y no tiene valor desconocido, así que se le asigna `Activo` con el significado "registrado, no dado de baja". Hay que documentarlo en la guía de importación como decisión, porque la fuente no dice que estén activos. **Pendiente del usuario:** ¿estos artículos deben poder elegirse en L-3 mientras no se confirmen?
- Interfaz: mostrar el badge "Por confirmar" en los listados y en `Pages/Catalogs/Articles`. Ofrecerlo como filtro, no como opción en los formularios de alta (mismo criterio que `EnumHelper.FormSelectList`).

### T2. Columnas *legacy* de `Article`

`Article.Category` (texto) y `Article.UnitOfMeasure` (texto NOT NULL) siguen en uso en `Pages/Departures/*`.

- Al importar, llenarlas con el **nombre** de la categoría y de la unidad ("Vajilla", "Pieza"), además de `ArticleCategoryId` y `MeasurementUnitId`.
- Dejar `ArticleType` y `EquipmentId` en NULL (ver §2.3).

### T3. Script de carga idempotente en **DB_Laboratorios_Univalle_DEMO_QA únicamente**

Antes de escribir, verificar que `DB_NAME() = 'DB_Laboratorios_Univalle_DEMO_QA'`. Lotes de 100 como máximo, `MERGE`/upsert por código y una transacción por lote.

1. `ArticleCategories` (10) por `Code`. `MeasurementUnits` (3) por `Code`.
2. `Articles` (1.023) por `Code`.
3. **Conflicto obligatorio de resolver antes de cargar:** el artículo demo `ART-0005 "Plato plano redondo"` choca con `ART-REQ-00462 "PLATO PLANO REDONDO"` en `UX_Articles_Name_Active`, porque la intercalación es `Modern_Spanish_CI_AS` y no distingue mayúsculas. Hay 7 `PracticeRequirements` demo que apuntan a artículos demo.
   - Volver a apuntar los requerimientos de ART-0005 a ART-REQ-00462.
   - Después, dar de baja lógica ART-0005 (`Status = Eliminado`).
   - No hay que borrar nada.
   - Los otros 7 artículos demo (ART-0001 a 0004, 0006 a 0008) no chocan. Se conservan con su marca demo, y se reporta si la tienen o no.
4. **Actores (15 docentes nuevos)**, por `ActorCode`:
   - `Person` como **`Intern`**, `Category = Docente (2)`, `Title` (ING./LIC./CHEF/ARQ.) y rol `Docente` en `PersonRoleAssignments`.
   - `Vinculacion` = "Por confirmar" → un `DataQualityIssue` por actor.
   - Registrar el "Nombre en la planificación" de `_POBLADO_DOCENTES` (p. ej., "ING PEDRO GUERECA") como `PersonAlias`. Sirve para resolver al docente cuando llegue `GruposMateria`.
   - Aprovechar el mismo paso para dar de alta los 12 actores de la V1 que faltan (ACT-0008, **ACT-0009**, ACT-0027 a ACT-0036). En DEMO_QA solo existen 28 con código.
5. `DataQualityIssues` (lote nuevo en `ImportBatches` con el hash de la V3):
   - 416 artículos "Por confirmar".
   - 15 docentes con vinculación sin confirmar.
   - **21 grupos de posibles duplicados no fusionados**, con las mismas palabras en otro orden. Ejemplos: ART-REQ-00003 "PLATO BLANCO REDONDO" ~ ART-REQ-00464 "PLATO REDONDO BLANCO"; ART-LAB-0041 ~ ART-LAB-0101 "PISA PURE ... TRAMONTINA".
   - Nombres sospechosos: ECO-R0349 (truncado de 102 a 92 caracteres), ART-REQ-00492 "PLATO REDONDAS BLANCAS CON" (incompleto) y ART-REQ-00479 "REDONDOBORDE" (errata).

### T4. Verificación (evidencia en la respuesta)

- `dotnet build -m:1`: 0 errores y 0 advertencias nuevas.
- Conteos en DEMO_QA por código:
  - `ArticleCategories` = 10, `MeasurementUnits` = 3.
  - Artículos con código de la V3 = 1.023, de los cuales 416 con `ReviewStatus = PendingConfirmation`.
  - 15 docentes con rol `Docente`.
  - 0 huérfanos en las FKs de los artículos.
- Volver a ejecutar el script: 0 inserciones nuevas (idempotencia).
- Prueba de humo: `Catalogs/Articles` (listado, filtro "Por confirmar"), `Catalogs/ArticleCategories`, `Catalogs/MeasurementUnits` y `Departures/MassCreate`, que debe seguir funcionando con los artículos nuevos.
- Commit local con migración, enum, ajustes de interfaz y script. Sin push.

## 4. Lo que **no** hay que hacer

- No tocar `DB_Laboratorios_Univalle` (base local "oficial") ni MonsterASP.
- No modificar migraciones commiteadas ni la de la Fase A.
- No inventar `ArticleType`, `EquipmentId`, categorías ni fusiones: todo lo dudoso va a `DataQualityIssues`.
- No importar las hojas que empiezan con `_`.
- No modificar el Excel.

## 5. Impacto en el diseño de L-3 (preparar la Fase D)

- Hoy DEMO_QA tiene **0 `L3Requests` y 0 `L3RequestDetails`**. Cambiar `L3RequestDetail` a `ArticleId` obligatorio + `EquipmentUnitId` opcional + cantidad decimal **no cuesta migración de datos**. Conviene hacerlo **ahora**, en la siguiente migración, antes de que se generen L-3 por unidad representativa.
- Con artículos reales disponibles, la planificación debería pedir por `Article`. La solución provisional por tipo de equipo con unidad representativa debe retirarse de la interfaz. El histórico no se ve afectado porque está vacío.

## 6. Pendiente de pedir al usuario (no bloquea T1–T4)

1. ~~La versión con `TipoArticulo`/`CatalogoCodigo`~~: resuelto, se quitaron a propósito (§2.0).
2. La fuente `BASE_NORMALIZADA_UTENSILIOS_1-2027_COMPLETADA.xlsx`. Según las bitácoras, contiene **422 filas de planificación, 41 combinaciones materia-grupo y 682 productos de requerimientos**: es la materia prima de `Materias`, `GruposMateria`, `Practicas` y `RequerimientosPractica`, que en la V3 siguen vacías.
3. Si los 21 grupos de casi-duplicados se fusionan y cuál código se conserva.
4. La vinculación (Interno/Externo) de los 15 docentes.
