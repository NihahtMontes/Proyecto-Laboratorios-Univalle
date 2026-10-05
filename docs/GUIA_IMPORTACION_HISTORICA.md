# Guia De Importacion De Datos Historicos

Esta guia define el contrato operativo para transformar el Excel historico en
un paquete auditable y, solo despues de conciliarlo, en un delta SQL revisable.
La herramienta no debe conectarse ni escribir directamente en SQL Server.

Archivos de entrada admitidos:

- `Plantilla_Original.xlsx`: contrato legado, preservado como evidencia.
- `Plantilla_Historicos_Normalizada_v2.xlsx`: contrato tecnico normalizado.
- `Plantilla_Levantamiento_Cliente_Fase1_v2.xlsx`: no es una fuente de carga;
  confirma laboratorios, unidades, catalogos y actores; sus respuestas deben
  promoverse primero a una copia de trabajo de la V2.
- `Plantilla_Levantamiento_Cliente_Fase2_Historicos_v2.xlsx`: tampoco es una
  fuente de carga; confirma vacios historicos y reglas institucionales. Sus
  60 respuestas se revisan contra la evidencia y se promueven a la copia de
  trabajo solo despues de congelar el archivo respondido por hash.

Herramienta: `Tools/HistoricalDataImport`.

## 1. Principios No Negociables

- El Excel original y la respuesta recibida se conservan inmutables.
- Toda ejecucion comienza en modo validacion.
- No se deducen laboratorios por el nombre del equipo.
- No se crean personas, tecnicos, fechas, satisfacciones, costos o eventos para
  completar vacios.
- Un inventario se normaliza solo cuando la coincidencia es inequivoca y el
  valor de origen queda registrado.
- Los registros ambiguos se mantienen en `Pendiente` o cuarentena.
- Ninguna fila se elimina silenciosamente: debe quedar `Migrada`, `Pendiente`
  o `Duplicado por confirmar`.
- El SQL se genera offline y nunca se ejecuta automaticamente.
- La base oficial requiere respaldo, prueba en QA y autorizacion separada.

## 2. Modelo Normalizado

| Dominio V2 | Entidad EF Core | Regla principal |
|---|---|---|
| Catalogo de equipos | `Equipment` | Un registro reutilizable por tipo/modelo; codigo externo estable y unico cuando tiene valor |
| Unidad fisica | `EquipmentUnit` | Un bien por inventario; serie, laboratorio, valor y condicion pertenecen aqui |
| Articulo L-3 | `Article` | Consumible no inventariado con codigo y unidad de medida |
| Solicitud-unidad | `RequestEquipmentUnit` | Una solicitud puede afectar varias unidades sin duplicar cabecera |
| Sugerencia de solicitud | `Request.Suggestion` | Campo nullable independiente, maximo 2.000 caracteres; no se trunca dentro de observaciones |
| Mantenimiento-solicitud | `MaintenanceRequest` | Un mantenimiento puede relacionarse con cero o varias solicitudes |
| Participante | `MaintenanceParticipant` | Relacion operativa de actores; el tecnico simple se conserva por compatibilidad |
| Fila fuente | `ImportSourceRow` | Hoja, fila, clave determinista, estado y huella para trazabilidad/idempotencia |
| Hallazgo | `DataQualityIssue` | Fuente, severidad, candidatos y resolucion, sin perder la ambiguedad original |

La migracion `AlignHistoricalWorkbookV2` es aditiva. Conserva los campos
legados para que las pantallas actuales sigan funcionando y sincroniza las
relaciones primarias legadas cuando los cambios pasan por
`ApplicationDbContext.SaveChangesAsync`. La sincronizacion es legacy hacia V2;
el futuro adaptador debe escribir tambien la referencia legacy primaria cuando
cree o cambie una relacion directamente en las tablas puente.

## 3. Contrato V2 Por Hoja

### Maestros

- `01_FACULTADES`, `02_CARRERAS`, `03_LABORATORIOS`.
- `04_CATALOGO_EQUIPOS`, `05_UNIDADES_FISICAS`.
- `06_ACTORES`, `07_ACTOR_ROLES`, `08_GESTIONES`, `09_ARTICULOS`.

### Historicos Y Relaciones

- `10_VERIFICACIONES_L6`.
- `11_SOLICITUDES_L7` y `12_SOLICITUD_UNIDADES`.
- `13_MANTENIMIENTOS_L8`, `14_MANT_SOLICITUDES` y
  `15_MANT_PARTICIPANTES`.
- `16_COSTOS`.
- `17_SALIDAS_L3` y `18_SALIDA_DETALLES`.
- `19_PLAN_L48`.

### Gobierno De Datos

- `90_LISTAS_CONTROLADAS`.
- `91_MAPA_ORIGEN`, `92_PENDIENTES_CALIDAD`, `93_CONTROL_CALIDAD` y
  `94_MAPEO_IMPORTADOR`.

El lector V2 es estricto con los encabezados y las referencias. Un campo que
no existe en la fuente permanece nulo; no se completa a partir de otra hoja si
la relacion no es determinista.

## 4. Modos De Ejecucion

Ejecutar desde la raiz del repositorio con .NET para Windows.

### Autodeteccion Y Validacion Segura

```powershell
dotnet run --project Tools/HistoricalDataImport -- "ruta.xlsx" "directorio-salida" --contract auto --mode validate
```

`auto` detecta el contrato legado o V2. Para V2, `validate` tambien es el modo
predeterminado. No genera SQL.

### Auditoria Y Conciliacion V2

```powershell
dotnet run --project Tools/HistoricalDataImport -- "ruta-v2.xlsx" "directorio-salida" --contract v2 --mode audit --snapshot "snapshot.csv"
```

El snapshot representa el estado conocido de la base sin otorgar acceso a la
herramienta. Su cabecera es:

```csv
Entity,NaturalKey,Fingerprint,SourceKind
```

`SourceKind` es opcional. La huella permite detectar coincidencias y conflictos
sin incluir credenciales ni una conexion de base de datos.

### Preparar El Delta SQL V2

```powershell
dotnet run --project Tools/HistoricalDataImport -- "ruta-v2.xlsx" "directorio-salida" --contract v2 --mode sql --snapshot "snapshot.csv" --allow-sql
```

Las tres condiciones son obligatorias: contrato V2, snapshot y
`--allow-sql`. Si falta cualquiera o existe una incidencia P0, la herramienta
genera `sql-bloqueado.md` en vez del script.

### Autoprueba Del Importador

```powershell
dotnet run --project Tools/HistoricalDataImport -- --self-check
```

La autoprueba verifica deteccion de contratos, reglas de seguridad,
idempotencia de claves y bloqueo de SQL sin autorizacion.

## 5. Artefactos De Salida

| Archivo | Uso |
|---|---|
| `resumen.md` | Resultado ejecutivo, contrato detectado, conteos y estado de las puertas |
| `incidencias.csv` | Hallazgos P0/P1/P2 con entidad, clave, fuente y detalle |
| `reconciliacion.csv` | Comparacion entre Excel y snapshot |
| `paquete-canonico.json` | Representacion normalizada para procesamiento reproducible |
| `vista-canonica.csv` | Vista tabular de control para revision humana |
| `stage-v2-delta.sql` | Staging SQL offline de gobierno; registra lote, filas fuente e incidencias, pero no modifica entidades de negocio |
| `sql-bloqueado.md` | Motivos que impiden generar SQL |

Estos archivos forman un mismo lote y deben archivarse junto con el hash del
Excel de entrada y del snapshot utilizado. En esta fase,
`stage-v2-delta.sql` escribe unicamente `ImportBatches`, `ImportSourceRows` y
`DataQualityIssues`. La promocion de catalogos, unidades y transacciones exige
un adaptador transaccional posterior que resuelva las FK por codigos estables.

## 6. Estados De Conciliacion

| Estado | Significado | Accion |
|---|---|---|
| `Coincide` | Clave y huella equivalentes | Mantener; no sobrescribir |
| `SoloExcel` | La clave existe solo en la planilla | Proponer alta en el delta |
| `SoloBase` | La clave existe solo en el snapshot | Conservar en base; revisar alcance, nunca eliminar automaticamente |
| `Conflicto` | Misma clave con contenido diferente | Requiere decision humana y evidencia |
| `InferidoLegado` | Registro oficial previo creado con una regla de inferencia | Reconciliar contra V2 antes de considerarlo confirmado |
| `PendienteCliente` | Falta confirmacion de la primera ronda | Mantener en cuarentena |

## 7. Reglas De Bloqueo Por Prioridad

### P0 - Critica

Bloquea la generacion de SQL:

- inventario duplicado o vacio;
- unidad con catalogo inexistente;
- clave externa duplicada;
- referencia obligatoria inexistente;
- costo con cero o dos padres;
- detalle L-3 con cero o dos referencias;
- estructura/encabezado incompatible;
- reconciliacion sin decision para una colision de clave.

### P1 - Alta

Debe resolverse o justificarse en la primera ronda:

- laboratorio o responsable patrimonial pendiente;
- condicion o estado operativo sin confirmar;
- actor o rol requerido no identificado;
- ubicacion interna incompleta.

Una P1 se convierte en bloqueo tecnico cuando el modelo SQL no puede conservar
el valor como desconocido. No se permite mapear `Por confirmar` a `Activo`,
`Pendiente`, `Equipo` u `Otro` solo para satisfacer una columna `NOT NULL`.

### P2 - Programada

Puede permanecer aislada sin bloquear lo confirmado:

- verificacion L-6 sin inventario determinista;
- duplicado historico posible;
- texto L-3 que no puede descomponerse;
- relacion historica opcional sin evidencia suficiente.

## 8. Procedimiento De Carga En QA

1. Revisar `resumen.md`, `incidencias.csv` y `reconciliacion.csv`.
2. Resolver cada P0 y obtener aprobacion funcional de los conflictos o
   cuarentenas P1/P2. Una P0 en cuarentena sigue bloqueando.
3. Revisar el diff de `stage-v2-delta.sql`; no agregar inferencias manuales.
4. Confirmar que la base QA tiene la migracion EF Core V2 revisada.
5. Crear un respaldo y probar su restauracion.
6. Ejecutar el staging de gobierno en una transaccion sobre QA.
7. Implementar y revisar el adaptador transaccional de entidades de negocio;
   no convertir el staging actual en una carga mediante SQL manual.
8. Comparar conteos antes/despues y ejecutar `DBCC CHECKCONSTRAINTS`.
9. Reejecutar el mismo lote: no debe crear duplicados.
10. Probar consultas y pantallas de catalogo, unidades, solicitudes,
   mantenimientos, costos y salidas.
11. Archivar resultados y solicitar autorizacion separada para la base oficial.

La aplicacion web y la pantalla `Pages/Migration/Import` no ejecutan este
procedimiento.

La puerta de ejecucion posterior es `Tools/HistoricalDataApply`:

```powershell
dotnet run --project Tools/HistoricalDataApply -- plan \
  --package "paquete-canonico.json" \
  --issues "incidencias.csv" \
  --snapshot "preflight.json" \
  --reconciliation "reconciliacion-aprobada.json" \
  --reconciliation-output "reconciliacion-propuesta.json" \
  --sql "delta-revisado.sql" \
  --output "plan-apply.json" \
  --expected-database "BASE_TERMINADA_EN_QA"
```

`plan` no escribe. `apply` solo acepta un plan aprobado, revalida todos los
hashes y ejecuta dos veces sobre `_QA` para comprobar idempotencia por conteos
y huellas SHA-256 de 27 tablas de negocio. Produccion requiere ademas evidencia
y hash del respaldo. El delta de
negocio no debe confundirse con `stage-v2-delta.sql`, que solo registra
gobierno y procedencia.

Desde el contrato 4, `plan` tambien audita compatibilidad semantica V2↔SQL:
nulabilidad, valores desconocidos, campos sin destino, roles, colisiones de
unicidad y necesidad de reconciliar claves legacy. La reconciliacion se aprueba
por fila mediante un manifiesto independiente; una bandera global no puede
autorizarla. Cero P0 ya no es suficiente si cualquiera de esas puertas
permanece abierta.

## 9. Estado Legado Que Debe Reconciliarse

La carga oficial verificada el 23-08-2026 conserva un historial funcional,
pero fue construida con el contrato anterior. Sus conteos documentados fueron:

- 107 equipos logicos y 556 unidades fisicas;
- 1.020 verificaciones operativas y 562 filas L-6 en cuarentena;
- 520 solicitudes, 157 mantenimientos y 157 participantes primarios;
- 519 planes en la nube actual, 124 costos, 121 historiales de estado y 1
  salida. El valor anterior de 517 no debe usarse como constante de QA.

Estos datos no deben borrarse ni recargarse desde cero. El snapshot debe
clasificarlos y el delta V2 solo debe agregar o corregir lo aprobado. Las filas
marcadas `InferidoLegado` requieren evidencia antes de convertirse en datos
confirmados.

## 10. Criterios De Cierre

La normalizacion historica puede declararse lista para promocion cuando:

- el original conserva su hash y contenido;
- las 556 unidades estan reconciliadas sin inventarios duplicados;
- todas las unidades tienen un catalogo existente;
- cada fila fuente tiene estado y clave de procedencia;
- las FK y restricciones exclusivas pasan la validacion;
- no hay P0 abierta;
- el SQL fue revisado, ejecutado dos veces en QA sin duplicar y respaldado;
- existe autorizacion explicita para la base oficial.

Mientras falte una de estas condiciones, el lote sigue siendo de preparacion o
QA y no debe promoverse.

## 11. Resultado De La Validacion V2 Actual (27-08-2026)

La herramienta proceso la V2 tecnica sin escribir en SQL Server:

- 11.939 filas canonicas;
- 45 incidencias P0: 41 campos obligatorios vacios y 4 costos sin un padre
  exclusivo confirmado;
- 2.465 incidencias P1;
- 1.398 incidencias P2;
- con snapshot del mismo lote: 8.076 `Coincide`, 3.863
  `PendienteCliente` y 0 `Conflicto`.

Las 66 sugerencias historicas de 516 a 863 caracteres ya no se truncan ni se
clasifican como P0: el esquema normalizado dispone de `Request.Suggestion`
nullable con longitud maxima de 2.000 caracteres, independiente de
`Request.Observations`.

Por tanto, el estado actual es **bloqueado para carga de negocio**. La primera
ronda con la cliente debe resolver los P0/P1 aplicables; los P2 continuan
trazados. Aun con cero P0, el staging solo registra gobierno de datos. Antes
del delta se debe representar fielmente los desconocidos, aprobar un manifiesto
de reconciliacion legacy→V2 e implementar y probar el adaptador transaccional
de FK por codigos. Despues de corregir localmente el contrato de modelo, la
validacion del 28-08-2026 encontro 11 clases de bloqueo semantico y 10
advertencias nullable. L-48 se persiste en `MaintenancePlans`, no en el agregado
`ManagementPlans` del wizard. El reconciliador contrato 4 contiene 2.037
mapeos: 391 coincidencias de contenido, 672 conflictos, 214 solo V2 y 760 solo
base. Los 528 conflictos de verificacion provienen de fechas distintas entre
la celda fuente y SQL; los 144 de mantenimiento corresponden a fecha programada
y, en 88 casos, gestion. Costos y L-3 ya forman parte del manifiesto. El
adaptador no debe generar el delta de negocio mientras sigan abiertas.

El plan de conciliacion V9 agrega a cada coincidencia los valores de negocio
comparados y la lista exacta de campos diferentes. La huella vigente de los
2.037 mapeos es
`E3766F21F530C2CC3252FED74B11700D7A1BDD267FE29F6DC439A611298EA4CC`.
El archivo `Informe_Conciliacion_Contenido_V2.xlsx` materializa esa evidencia,
incluye decisiones por fila y diez grupos de regla para mantenimiento, y tiene
SHA-256
`8074F04E06F7EA069492B52A06C65CBDD29B0F83CD050593C7CD9ADA46AD5D73`.
Completar el Excel no concede autorizacion de escritura: la decision debe
trasladarse al manifiesto JSON revisado y volver a validarse contra paquete,
snapshot, servidor y base.

## Decisiones de importación de BaseEstructuraFinal.xlsx (2026-10-04)

1. **L-3 híbrido**: artículo obligatorio + cantidad; la unidad física
   (`EquipmentUnit`) es opcional y solo se utiliza para registrar equipos
   patrimoniales que salen del laboratorio.
2. **Gestión de L-6/L-7**: en el Excel la gestión es la del semestre
   SIGUIENTE a la fecha (1.599 verificaciones y 316 de 329 solicitudes). Es
   REGLA DE NEGOCIO (se verifica al final de un semestre para mantener el
   inventario en el siguiente); se importa tal cual, no se recalcula. L-8
   sí coincide con la fecha.
3. **`Por confirmar` / `Sin especificar`**: se importan como el valor
   `PorConfirmar` agregado a `EquipmentStatus` (98), `RequestStatus` (98),
   `PhysicalCondition` (9), `RequestPriority` (9) y `ServiceType` (9), con un
   `DataQualityIssue` por fila. Nunca se convierten en un valor real. Los
   formularios de alta no ofrecen ese valor y las ediciones lo conservan
   hasta que el usuario elija otro.
4. **Códigos maestros oficiales**: los del Excel (facultades `FGYT`/`FCSN`;
   carreras `LGS`, `LTH`, `LND` y `LCG`, con `LND` en `FGYT` según el Excel;
   advertencia: originalmente Nutrición estaba en Ciencias de la Salud;
   laboratorios `H-01..H-07`, `H-102..H-107`, `K-01..K-06`, `CIRC-GEN`,
   `ECONO`, `SALA-DOC`, `OFI-COORD` y `CAS-VES`; sedes `LPZ`, `CBBA` y
   `SCZ`). Se normalizan los códigos actuales conservando los `Id`.
5. **Lo que solo está en la BD** (191 solicitudes L-7, 124 costos por
   Bs 22.900, 121 movimientos de Kardex, 519 planes del wizard) NO se
   borra.
6. **Columnas 100% vacías del Excel** no sobrescriben datos de la BD:
   `FechaProgramada` de L-8; fechas de adquisición, fabricación y
   ubicación interna de unidades; `PaisOrigen` y `VidaUtilAnios` del
   catálogo; `Email` y `Telefono` de actores. `TiempoEstimado` de L-7 es
   basura en las 329 filas y se descarta.
7. **Gestión `1900-1`** (2 mantenimientos) va a cuarentena; se crea la
   gestión `2027-1`.

### Estado en DB_Laboratorios_Univalle_DEMO_QA

Fase A aplicada (migración `MasterDataAndPendingValues`; 3 sedes; códigos
normalizados; 24 laboratorios del Excel; 547 unidades con el laboratorio real
del Excel marcadas `[EXCEL]` en `Notes`; 9 unidades solo en BD conservan la
inferencia: 37070, 37077, 43486-43491, 45840).

### Versión V3 (artículos y docentes)

1. **Fuente vigente**: la única fuente vigente es
   `BaseEstructuraFinal_V3_ARTICULOS.xlsx`, SHA-256
   `1e70f5e4cbf9b298ee3690bfc65e4a0aec07efae109ca398aa10b774ca87680e`; la V1
   (SHA-256 `e78cfce6a4764ca67841c4c70a0de0c296b33fa8f3f8721b6b2dc2c3cf010ad8`)
   queda obsoleta. Las hojas que empiezan con `_` no se importan y los archivos
   `_DICCIONARIO` e `_INDICE` están desactualizados para Artículos; se importa
   según los encabezados reales del libro.
2. **`Articulos.Estado` = `Por confirmar`**: es el valor literal de la base de
   la ingeniera. Se importa `Status` = `Activo` con el significado "registrado,
   no dado de baja" y `ReviewStatus` = `Por confirmar` (nuevo campo
   `Article.ReviewStatus` con valores `Confirmado` y `Por confirmar`), además
   de un `DataQualityIssue ARTICLE_PENDING_CONFIRMATION` por fila. Un usuario
   puede confirmar el artículo desde Catálogos > Artículos > Editar.
3. **`TipoArticulo` y `CatalogoCodigo` retirados a propósito**: los campos
   `Article.ArticleType` y `EquipmentId` quedan `NULL` y se retiraron de la
   interfaz.
4. **Columnas legadas con el nombre**: `Article.Category` y
   `Article.UnitOfMeasure` se llenan con el NOMBRE de la categoría y de la
   unidad, respectivamente.
5. **Unidades homologadas a 3**: `UNI-01` Juego, `UNI-02` Paquete y `UNI-03`
   Pieza.
6. **Conflicto de nombre**: cuando un artículo existente con otro código y el
   mismo nombre coincide con uno de la V3 (caso `ART-0005` "Plato plano
   redondo" frente a `ART-REQ-00462`) se da de baja lógica y su receta y stock
   se trasladan al artículo V3.
7. **Docentes**: 15 nuevos `ACT-0037..ACT-0051` como `Intern` de categoría
   `Docente`, con título, rol `Docente` y alias "Nombre en la planificación"
   (p. ej. "ING PEDRO GUERECA"). Además se dieron de alta los 12 actores de
   la V1 que faltaban (`ACT-0008`, `ACT-0009`, `ACT-0027..ACT-0036`); la
   vinculación "Por confirmar" se materializa como un
   `DataQualityIssue ACTOR_LINK_PENDING`.
8. **Casi-duplicados**: 21 grupos (mismas palabras en otro orden, ignorando
   plural simple y conectores) registrados como `ARTICLE_POSSIBLE_DUPLICATE`
   sin fusionar. Los nombres sospechosos `ECO-R0349`, `ART-REQ-00492` y
   `ART-REQ-00479` quedaron como `ARTICLE_SUSPICIOUS_NAME`.
9. **Estado DEMO_QA**: lote `XLSX-V3-ARTICULOS-1E70F5E4CBF9` con 466
   incidencias; 1.023 artículos (de los cuales 416 están por confirmar);
   51 actores con código. La importación es idempotente: una segunda pasada
   produce 0 inserciones.
10. **Pendientes del usuario**: confirmar si los artículos con
    `ReviewStatus` = `Por confirmar` pueden elegirse en L-3; fusión de los
    21 grupos casi-duplicados; vinculación definitiva de los 15 docentes;
    archivo `BASE_NORMALIZADA_UTENSILIOS_1-2027_COMPLETADA.xlsx` con materias,
    grupos, prácticas y requerimientos.

### Versión V7 (bloque académico) — 2026-10-05

1. **Fuente vigente para el bloque académico**:
   `BaseEstructuraFinal_V7.xlsx`, SHA-256
   `c677c7cc81d63f5e74b00daab60c214be032c5089b1876c61b9ddb66dc4320e9`.
   La V7 tiene **el mismo contenido histórico que la V4** si se compara
   sin distinguir mayúsculas, pero alguien aplicó "Tipo Título"
   (`NOMPROPIO`) a **todo el libro** — códigos, encabezados, estados y
   textos — y además reemplazó los saltos de línea de los textos largos
   por espacios. Por eso la V7 **solo se importa para el bloque
   académico**: prácticas, programaciones, recetas y existencias
   nuevas. Todo lo histórico (L-6, L-7, L-8, L-48, catálogo, unidades,
   actores, materias, L-3, `_*`) sigue saliendo de la V4: reimportar
   esos bloques desde la V7 **degrada datos ya cargados**. El contrato
   operativo detallado está en
   `docs/INSTRUCCIONES_AGENTE_V7_ACADEMICO.md`.
2. **Generador**: `Tools/ExcelImport/gen_v7_academic_sql.py`
   (copia adaptada de `gen_v4_academic_sql.py`, **no** modifica el
   generador V4). Lote `ImportBatches`:
   `XLSX-V7-ACADEMIC-C677C7CC81D6`, `SourceName` =
   `BaseEstructuraFinal_V7.xlsx`, `ContractVersion` =
   `base-estructural-v7`. Lotes de 100 como máximo, una transacción por
   bloque y `SET XACT_ABORT ON`; el script se detiene si
   `DB_NAME() <> 'DB_Laboratorios_Univalle_DEMO_QA'`.
3. **Comando de ejecución** (un solo proceso a la vez, ejecutar dos
   veces para confirmar idempotencia):

   ```cmd
   sqlcmd -S localhost -E -C -d DB_Laboratorios_Univalle_DEMO_QA -b -f 65001 -i <sql>
   ```

   **`-f 65001` es obligatorio.** Sin él, `sqlcmd` lee el archivo
   UTF-8 como ANSI y daña las tildes. Esto pasó en la primera pasada
   del 2026-10-05: se repararon en el lugar **481 filas** con un
   script Python que revierte el doble encoding — **49 prácticas**
   (`Name`/descripción), **365 recetas** (`Observations`) y **66
   incidencias** (`Description`) — y se corrigió la fila del lote en
   `ImportBatches` (1 row). La segunda ejecución del mismo script
   insertó 0 filas.
4. **Respaldo previo**: `DEMO_QA_pre_V7_20261005.bak`,
   `WITH COPY_ONLY, CHECKSUM`, `RESTORE VERIFYONLY` OK, guardado en la
   carpeta `Backup` de la instancia. Sin un respaldo verificado no se
   ejecuta la carga.
5. **Resultados verificados** en `DB_Laboratorios_Univalle_DEMO_QA`
   (migración `20261004214711_AddSubjectKit`):

   - 196 prácticas (`PRA-%`, salvo `PRA-GAS%`), `Status = 0`;
   - 422 programaciones de la gestión `2027-1`
     (`PracticeSchedules`, `Status = Programada`);
   - 41 grupos `GRP-2027-1-*` activos;
   - 2.111 recetas (`PracticeRequirements` con `Code LIKE 'REQ-%'`,
     `IsRemoved = 0`);
   - 220 existencias nuevas: total `ArticleStocks` 228
     (8 previas + 220). Por laboratorio: ECONO 122, H-03 26, K-01 16,
     H-06 14, H-01 13, H-05 12, H-04 11, H-02 8, H-07 6;
   - 1.002 artículos activos (1.023 − 25 bajas lógicas + 4 nuevos):
     25 `Status = 2` (24 duplicados fusionados + la nota del formato
     L-3 `ART-REQ-00348`) y 4 nuevos (`ART-REQ-00566…00569`) con
     `Status = 0`, `ReviewStatus = 9`;
   - artículos de receta con stock: **83** en algún laboratorio
     (ECONO o H-01…K-01) y **49** solo en ECONO (de 493 artículos
     usados en recetas);
   - 410 de 493 artículos de receta quedan sin stock en ningún
     laboratorio, y 444 sin stock en ECONO;
   - incidencias del lote: **120** en total —
     `ARTICLE_NEW_V7` 4, `ARTICLE_RETIRED_V7` 25,
     `COURSE_GROUP_VALUES_VARY` 2, `SCHEDULE_DATE_SUSPECT` 33,
     `SCHEDULE_OUT_OF_MANAGEMENT` 2, `VALUE_MISSING` 11,
     `VALUE_PENDING_CONFIRMATION` 41 (una por grupo, campo hora) y
     `VALUE_TRUNCATED_IN_NAME` 2;
   - segunda ejecución del mismo script: **0 filas** y **0
     incidencias** nuevas;
   - *checksums* de los textos históricos L-6 / L-7 / L-8, de los
     nombres de materias, de `PRC-00001` / `PRC-00002`, de
     `PRG-00001` y de los artículos conservados: **idénticos**
     antes y después;
   - códigos guardados en minúscula: **0** (verificado con un
     `SELECT … WHERE Code COLLATE Latin1_General_CS_AS <> UPPER(Code)`
     sobre las tablas tocadas).
6. **Hallazgos corregidos en el generador durante la revisión** (ya
   reflejados en `gen_v7_academic_sql.py` antes de ejecutar):

   - Columnas invertidas en la nota `ART-REQ-00348` (no era la baja
     lógica de un duplicado).
   - Faltaba `SET QUOTED_IDENTIFIER ON` en el bloque de prácticas /
     programaciones: rompía los índices filtrados
     (`IX_PracticeRequirements_PracticeId_ArticleId`,
     `IX_PracticeSchedules_ManagementId_PracticeId_GroupName`,
     `UX_Articles_Name_Active`).
   - Faltaba `IsDeleted = 0` en el `MERGE` de `PracticeSchedules`,
     necesario para respetar el índice filtrado.

   **Lección operativa**: `SET NOEXEC ON` no valida columnas en
   sentencias que crean tablas temporales (las valida recién al
   ejecutar). Validar con un ensayo real en una transacción con
   `ROLLBACK`, o incluyendo `SELECT TOP 0 … FROM #temp` justo después
   de crearla.
7. **Vulnerabilidades / pendientes abiertos** (no bloquean la carga,
   requieren acción posterior):

   - `AcademicPlanning` 2027-1 **no existe**: las 422 programaciones
     importadas quedan con `AcademicWeekId = NULL`. Hay que crear la
     planificación y vincular cada `WeekNumber` con su
     `AcademicWeek` — teniendo en cuenta que la **semana 6 es
     sospechosa** (la fecha del Excel coincide con la semana 3; ver
     `INSTRUCCIONES_AGENTE_V7_ACADEMICO.md` §1.3 y §2.1).
   - Fechas reales de la **semana 6**: no se pueden deducir; pedir a
     la cliente.
   - `HoraInicio` / `HoraFin` de la programación: vacías en las 422
     filas; sin horas no se puede saber si un mismo laboratorio y día
     tienen choques (hay hasta 8 programaciones el mismo día y, en 4
     casos, un docente en 2 laboratorios). Incidencia
     `VALUE_PENDING_CONFIRMATION` (41, una por grupo).
   - Stock por laboratorio. `ArticleAvailabilityService` compara
     contra un solo laboratorio de origen (ECONO por defecto).
     Recomendación para una tarea de código posterior: que la vista
     semanal de Disponibilidad compare contra el **total
     institucional** (ECONO + laboratorios) y mantenga el selector de
     origen. **No** se modifica en esta carga.
   - Vínculo artículo-equipo: `docs/PROPUESTA_VINCULO_ARTICULO_EQUIPO.csv`
     sigue abierto; los artículos de categoría Equipo deberían leer su
     stock de las unidades físicas correspondientes.
   - Reexportar el Excel **sin** "Tipo Título" para que la V8 se
     pueda cargar como una sola corrida limpia, sin necesidad de
     normalizar encabezados, mayúsculas ni textos.
   - Las 422 programaciones **no aparecen** en Disponibilidad ni en el
     calendario de planificación porque Planificación /
     Disponibilidad leen `L3Requests`, no `PracticeSchedules` (ver
     `INSTRUCCIONES_AGENTE_V7_ACADEMICO.md` §2.1). Es lo esperado, no
     un fallo.
   - **Resuelto el 2026-10-05:** el wizard
     (`Pages/LaboratoryOperations/Practica.cshtml.cs`) ya no choca con
     las programaciones importadas: si existe una programación libre de
     la misma gestión + práctica + grupo, la adopta (vincula
     `L3Request.PracticeScheduleId`) en lugar de crear otra; solo
     rechaza si otra práctica planificada vigente ya la usa. Probado
     con el `PracticaModel` real contra DEMO_QA.
