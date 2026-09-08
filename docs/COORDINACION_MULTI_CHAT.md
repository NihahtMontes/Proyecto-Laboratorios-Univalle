# Coordinacion multi-chat del proyecto

Este documento es la fuente comun de contexto entre las tareas de Codex. Su
objetivo es permitir trabajo paralelo sin duplicar cambios, modificar una base
equivocada ni diseñar modulos sobre un contrato de datos desactualizado.

## Repositorio y fuentes oficiales

- Repositorio vigente: `D:\proyectoSis\Proyecto-Laboratorios-Univalle`.
- Las copias bajo OneDrive o `Desktop\Nihaht Peligro V2` son referencia
  historica y no reciben cambios.
- El Excel original, la V2 tecnica, la planilla de levantamiento y la base SQL
  son fuentes diferentes. Ninguna se reemplaza silenciosamente con otra.
- Este archivo lo mantiene el chat coordinador de datos. Los otros chats
  informan resultados mediante el formato de handoff; no editan este documento
  simultaneamente.

## Roles y autoridad

| Tarea Codex | Rol | Puede hacer | No puede hacer sin coordinacion |
|---|---|---|---|
| `Auditar datos históricos Excel` | Coordinacion de datos y esquema | Excel historico, normalizacion, modelo conceptual, EF Core, migraciones, contratos de importacion, integracion final | Promover datos ambiguos, aplicar nube sin puertas o aceptar cambios de modelo sin revisar impacto |
| `QA, navegación y regresiones` | QA funcional y tecnico | Probar login, navegacion, filtros, CRUD, reportes, restricciones y regresiones; documentar evidencia reproducible | Aplicar migraciones, reclasificar datos, cambiar modelos o corregir directamente codigo de negocio salvo encargo explicito |
| `Planificar sprints y módulos` | Producto y arquitectura funcional | Backlog, historias, dependencias, criterios de aceptacion, riesgos, secuencia de sprints | Editar codigo/base, declarar estable un modelo pendiente o inventar campos y relaciones sin validacion de datos |

Regla de escritor unico: durante una tarea activa solo un chat es propietario
de cada archivo. QA informa el fallo al propietario; no compite por la
correccion. Si el usuario encarga una implementacion al chat de QA o de
planificacion, primero debe declarar archivos y obtener el contrato vigente.

## Flujo obligatorio entre roles

1. **Propuesta funcional:** Sprint define necesidad, actor, valor y criterios;
   no define aun tablas o FK como definitivas.
2. **Contrato de datos:** Datos decide entidad, clave, nulabilidad, cardinalidad,
   trazabilidad, compatibilidad legacy y migracion requerida.
3. **Mapa de impacto:** se enumeran modelos, DbContext, PageModels, vistas,
   servicios, importadores, reportes y pruebas afectados.
4. **Implementacion:** un propietario por archivo; migraciones existentes no se
   modifican y no se ejecutan automaticamente.
5. **QA:** prueba primero en una base `_QA`, registra pasos, esperado, actual,
   evidencia, severidad y alcance. QA no corrige el defecto en la misma pasada.
6. **Integracion:** Datos valida coherencia y Sprint actualiza dependencias.
7. **Promocion:** requiere P0 = 0, respaldo recuperable, QA aprobado y
   autorizacion explicita para la base oficial.

## Contrato de impacto de datos

Toda propuesta que agregue o cambie datos debe responder antes de entrar a un
sprint:

- entidad propietaria del dato;
- clave natural y clave tecnica;
- campos obligatorios, opcionales y calculados;
- relaciones y cardinalidades;
- reglas de unicidad, FK y CHECK;
- tratamiento de desconocido, pendiente y eliminado;
- procedencia y auditoria historica;
- compatibilidad con campos legacy y pantallas actuales;
- estrategia idempotente de migracion/rollback;
- escenarios QA y conteos de reconciliacion.

El planificador puede proponer alternativas. Solo el rol de Datos puede marcar
el contrato como `Aprobado para implementar`.

## Estado compartido vigente

### QA-DATA-001 — Base sintetica reproducible para pruebas

- Estado: `Completado tecnicamente; prueba manual pendiente de autorizacion`.
- Fuente de solo lectura: `localhost / DB_Laboratorios_Univalle`.
- Destino exclusivo: `DB_Laboratorios_Univalle_SCENARIOS_QA`.
- Alcance: 107 catalogos, 556 unidades, maestros validos, actores e historicos
  sinteticos, tres roles Identity, integridad, transacciones, concurrencia y
  rendimiento.
- Esta tarea de Datos es propietaria de la herramienta, manifiestos y base QA.
  `QA, navegacion y regresiones` es propietaria de la compatibilidad en Pages,
  Services y Controllers, sin migraciones ni escrituras de base.
- Resultado: 107 catalogos, 556 unidades, 1.668 verificaciones, 669
  solicitudes, 333 mantenimientos, 834 costos, 111 salidas y 1.668 planes de
  gestion; semilla idempotente.
- Integridad: 40 controles aprobados, `DBCC CHECKDB` y
  `DBCC CHECKCONSTRAINTS` limpios, rollback, restricciones negativas,
  `RowVersion` y cinco lectores concurrentes aprobados.
- Rendimiento: peor P95 de 11,926 ms; perfil stress no ejecutado.
- Manifiesto: `outputs/qa-data-001/qa-data-001-manifest.json`, estado
  `Completed`, huella fuente
  `03E33E8E7E2FD4769227C737D24910FEA15EA9ADA5A7C5EB1FC4001124DA4B59`.
- Backup restaurado realmente y verificado: SHA-256
  `F8C18BB976DE8F2CC8CFA24DC827B32D5859D3CC2B45B1E3F3667B9A6D4FAF1D`.
- La tarea `QA, navegacion y regresiones` adapto la compatibilidad de
  solicitudes, L-3, L-6 y clasificaciones nullable; build tecnico aprobado.
- La aplicacion no se inicia hasta que el usuario autorice sus pruebas
  manuales y se inyecte la conexion QA temporal; MonsterASP queda fuera de
  alcance.
- Limites de recursos: un proceso pesado, `dotnet -m:1`, lotes de 100, abortar
  bajo 2 GB libres o sobre 85% de uso; estres solo con 6 GB y autorizacion.
- Informe completo: `docs/QA_DATA_001_RESULTADO.md`.

### DB-LOCAL-001 — Normalizacion de la base local segun V2

- Estado: `Esquema y relaciones inequivocas aplicados y verificados localmente`.
- Destino exclusivo: `localhost / DB_Laboratorios_Univalle`; MonsterASP no fue
  modificado.
- Respaldo previo verificado: SHA-256
  `35FC4FF4BF7B54322521645A0026D9C030115D8FFA4F3F066955E1B92955A6AD`.
- El respaldo previo fue restaurado realmente en
  `DB_Laboratorios_Univalle_PRE_NORMALIZACION_20260902_QA`; desde esa copia se
  reprodujeron los tres scripts y 16 metricas coincidieron con la base local.
- Respaldo posterior normalizado restaurado y verificado: SHA-256
  `D20DC457CDE6E1A26EF325B26E53E9249D07DAD27AA18C0EC56E5F7A52DEBBDE`.
- Se aplicaron localmente DB-001, DB-002 y
  `AddEquipmentClassificationGovernance`; EF registra las cuatro migraciones
  nuevas y no detecta cambios de modelo pendientes.
- Conteos preservados: 107 catalogos, 556 unidades, 520 solicitudes, 157
  mantenimientos y 1.020 verificaciones.
- Backfills verificados e idempotentes: 520 relaciones solicitud-unidad y 121
  mantenimiento-solicitud.
- Los 107 `CatalogCode` de la V2 fueron conciliados 1:1 y asignados con 107
  filas de procedencia. Las 556 unidades apuntan al codigo esperado.
- Codigos maestros: 4 carreras y 24 actores conciliados y asignados. Dos
  actores V2 permanecen `SourceOnly`; dos personas y el laboratorio
  `PENDIENTE` permanecen `DatabaseOnly`. El lote registra 51 filas, 49
  coincidencias y 2 pendientes.
- Clasificacion: 107 `LegacyInferred`, 0 `Confirmed`; no se promovio la
  reclasificacion tecnica local.
- Integridad: `DBCC CHECKDB`, `DBCC CHECKCONSTRAINTS`, FK y CHECK correctos.
- `CloudMigrationGuard snapshot` fue incorporado para certificar bases ya
  normalizadas sin reutilizar el preflight de DB-001. Compilacion de Guard y
  `HistoricalDataApply`: 0 errores y 0 advertencias.
- Snapshot normalizado vigente: aprobado, SHA-256
  `E74FAE012A96A6A55B50B686D3B3001B9EDA719C245472C290391F234335AAF6`.
- Hallazgo documental: `94_MAPEO_IMPORTADOR` aun menciona `ManagementPlan`
  para L-48; el contrato vigente y SQL usan `MaintenancePlan`. No modificar la
  V2 inmutable sin versionarla.
- Acta completa: `docs/RESULTADO_NORMALIZACION_LOCAL_V2_20260902.md`.

### DATA-001 — Excel historico V2

- Estado: `Bloqueado para carga de negocio`.
- V2 tecnica y las planillas de levantamiento Fase 1 y Fase 2 existen.
- Paquete canonico: 11.939 filas.
- Bloqueos: 45 P0, compuestos por 41 campos obligatorios vacios y cuatro
  costos sin un padre exclusivo.
- La Fase 1 cubre maestros y bienes fisicos. La Fase 2 cubre 60 respuestas
  administrativas focalizadas: 5 gestiones, 35 verificaciones, 2 solicitudes,
  4 costos, 2 salidas, 5 politicas y 7 grupos de mantenimiento. Su SHA-256 es
  `596AE87FA5E4112EC08DA4E1631116F7CFA7AC946CB9DEEC1B707E4189DA4A3C`.
- Las respuestas de la cliente se congelan por hash y se promueven despues a
  una copia de trabajo de la V2. No se cargan directamente y no modifican por
  si solas el conteo canonico de P0.

### DB-001 — Alineacion del esquema en la nube

- Estado: `QA aprobado; produccion pendiente`.
- Base remota: `db65393`, con siete migraciones aplicadas.
- Pendientes:
  - `20260827022733_AlignHistoricalWorkbookV2`;
  - `20260827105439_AddHistoricalRequestSuggestion`.
- BACPAC restaurado en una base local nueva terminada en `_QA`.
- Script aprobado por hash y probado dos veces de forma idempotente.
- Resultado esperado: 520 relaciones solicitud-unidad y 121
  mantenimiento-solicitud, sin reducir tablas preexistentes.
- Produccion espera `app_offline.htm`, backup final de MonsterASP.NET,
  preflight final y ventana de mantenimiento.

### DATA-002 — Congruencia semantica V2 con EF/SQL

- Estado: `Esquema aplicado localmente; carga de negocio bloqueada`.
- `HistoricalDataApply plan` contrato 4 detecto 11 clases de bloqueo
  semantico y 10 advertencias nullable, ademas de los 45 P0 actuales, y dejo
  `CanApply=false`.
- `19_PLAN_L48` se dirige a `MaintenancePlans`; `ManagementPlans` conserva la
  unicidad gestion-unidad requerida por el wizard.
- `20260828161956_CompleteHistoricalWorkbookV2Model` y
  `20260830202548_AddEquipmentClassificationGovernance` estan aplicadas en la
  base local oficial. No estan promovidas a MonsterASP por este trabajo.
- Plan local regenerado contra el snapshot normalizado: `SchemaReady=true`,
  `SnapshotValid=true`, `CanApply=false`. SHA-256 del plan:
  `B1CC19A34DCF12554D3409D2FFE9539926548A81998E17ACA4985247A391BEB7`.
- Sus 686 condiciones se desglosan en 45 P0 agrupadas, 11 bloqueos semanticos,
  672 conflictos por fila, falta de aprobacion de reconciliacion y ausencia
  deliberada del delta SQL. No existen bloqueos de esquema ni snapshot.
- El reconciliador contrato 4 genero 2.037 mapeos legacy→V2: 391 coincidencias
  de contenido, 672 conflictos, 214 solo V2, 760 solo base, 76 solicitudes
  legacy explotadas y cero ambiguedades de clave. Los conflictos son 528
  verificaciones y 144 mantenimientos; costos, salidas y detalles L-3 ya estan
  cubiertos. Se exige una decision revisada por fila y no existe aceptacion
  global.
- El plan V9 conserva tambien los valores comparados y los campos diferentes
  por fila. Su huella de mapeos es
  `E3766F21F530C2CC3252FED74B11700D7A1BDD267FE29F6DC439A611298EA4CC`.
  Confirmo 528 diferencias en `FechaVerificacion` —34 tambien en gestion— y
  144 en `FechaProgramada` —88 tambien en gestion—, sin diferencias en los
  demas campos estables comparados.
- El informe local `Informe_Conciliacion_Contenido_V2.xlsx` organiza las 672
  filas y los diez patrones de mantenimiento para revision. Su SHA-256 es
  `8074F04E06F7EA069492B52A06C65CBDD29B0F83CD050593C7CD9ADA46AD5D73`.
  Es evidencia de decision; no sustituye el manifiesto JSON aprobado ni
  autoriza escrituras.
- DB-001 sigue siendo una correccion aditiva inmediata y ensayada; no debe
  interpretarse como autorizacion para cargar los datos V2.

### DB-002 — Completar el contrato historico V2

- Estado: `Aplicado y verificado en la base local; QA de aplicacion pendiente`.
- Agrega fecha de solicitud, estado observado de verificacion, roles de actor,
  nulabilidad fiel de L-3 y el contrato historico de `MaintenancePlans`.
- No modifica la migracion ni el hash aprobados de DB-001.
- El script local fue revisado, aplicado dos veces de forma idempotente y paso
  DBCC. Requiere BACPAC/QA y autorizacion separada antes de incorporarse a la
  nube o a una publicacion de la aplicacion.

### MOD-CLAS-001 — Clasificacion auditable de catalogos

- Estado: `Gobierno implementado localmente; decisiones de cliente pendientes`.
- Los 107 catalogos tienen `CatalogCode` V2 unico y trazable, pero conservan
  `ClassificationReviewStatus = LegacyInferred`.
- Existen 0 decisiones vigentes y 0 catalogos confirmados.
- Ningun `Por confirmar` fue convertido en `Other`; la mutacion tecnica local
  no es autoridad funcional.
- La promocion exige exactamente 107 respuestas confirmadas, evidencia,
  responsable, fecha, P0 global igual a cero y QA idempotente.

### UI-001 — Clasificacion y filtros de equipos

- Estado: `Implementado localmente; requiere regresion integrada`.
- Filtro jerarquico `Tipo de recurso -> Subclasificacion` implementado.
- `Otro` dejo de tratarse como subclasificacion valida de Equipo/Utensilio.
- El chat anterior reporto una reclasificacion local de 107 definiciones y 556
  unidades. Este cambio de datos debe reconciliarse contra Excel V2 y no se
  promueve automaticamente a la nube.
- HTTPS local y redireccion fueron comprobados; falta prueba visual autenticada
  y regresion despues de alinear el esquema.
- Reconciliacion DATA (2026-08-28): el paquete V2 contiene 107/107 catalogos
  en `Por confirmar`; la nube conserva el marcador legacy `Equipment + Otro`
  y la base local fue mutada a `Other`. Ninguna de las dos representaciones es
  una clasificacion confirmada y la mutacion local no se promueve.
- Accion de levantamiento: la planilla de cliente incorpora
  `03_CATALOGOS_POR_CONFIRMAR`, listas controladas y 107 controles de
  completitud. `Otro` queda separado semanticamente de `Por confirmar`.

## Dependencias inmediatas

| Trabajo | Depende de | Responsable siguiente |
|---|---|---|
| Aplicar esquema V2 en `db65393` | Backup final MonsterASP, sitio detenido y preflight sin cambios | Datos/esquema |
| Probar login y navegacion completa | DB-001 aplicado y DB-002 ensayado en el destino probado | QA |
| Probar filtros y clasificaciones | Reconciliar UI-001 con reglas de Excel/categoria | QA + Datos |
| Cargar datos V2 | Respuestas Fase 1 y Fase 2 congeladas, P0 = 0, DATA-002 = 0, manifiesto legacy→V2, delta revisado y doble QA | Datos/esquema |
| Planificar nuevos modulos | Contrato de entidades marcado estable o riesgos explicitados | Sprint/arquitectura |

## Entrega de QA

Cada hallazgo debe incluir:

```text
ID:
Fecha y base/entorno:
Modulo/ruta:
Precondiciones:
Pasos reproducibles:
Resultado esperado:
Resultado actual:
Severidad: P0/P1/P2/P3
Evidencia:
Datos afectados:
Archivos sospechosos (sin editarlos):
```

P0 bloquea datos o produccion; P1 bloquea el flujo principal; P2 tiene
alternativa; P3 es mejora no bloqueante.

## Handoff obligatorio entre chats

Al cerrar una tarea, responder con:

```text
Rol y alcance:
Estado: Analizado / Implementado / Verificado / Bloqueado
Contrato o decision usada:
Archivos modificados:
Base o entorno tocado:
Validaciones ejecutadas y resultado:
Pendientes y severidad:
Impacto para Datos / QA / Sprint:
Proximo propietario recomendado:
```

Una afirmacion no acompañada por una validacion ejecutada se registra como
`Pendiente de verificar`, nunca como terminada.

## Protocolo para nuevos modulos y sprints

El chat de Sprint registra primero una ficha por modulo:

```text
MOD-XXX — Nombre
Problema y usuario beneficiado:
Flujo principal:
Entidades que cree que intervienen:
Cambios de datos propuestos (no aprobados):
Integraciones con L-6/L-7/L-8/L-3/L-48/L-12:
Riesgos historicos y de permisos:
Criterios de aceptacion:
Dependencias y orden de sprint:
```

Despues solicita al chat de Datos una revision de encaje. Datos responde
`Encaja`, `Encaja con cambios` o `No encaja`, explicando modelos, relaciones,
migracion y deuda historica. Solo entonces se desglosan tareas de UI, backend y
QA.

## Regla de actualizacion

Antes de iniciar trabajo transversal, cada chat debe leer este documento y
declarar:

1. rol asumido;
2. ID de trabajo (`DATA`, `DB`, `UI`, `QA` o `MOD`);
3. archivos o superficies que pretende tocar;
4. dependencias que consume;
5. si su actividad es lectura, escritura local o escritura externa.

Si el estado real contradice este documento, el chat se detiene, entrega la
evidencia al coordinador y no decide por su cuenta cual version prevalece.
