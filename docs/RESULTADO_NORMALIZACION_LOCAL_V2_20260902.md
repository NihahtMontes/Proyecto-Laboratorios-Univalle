# Resultado de normalizacion local V2 - 2026-09-02

## Alcance ejecutado

La implementacion se realizo exclusivamente sobre la base SQL Server local
`DB_Laboratorios_Univalle` en `localhost`, mediante autenticacion integrada.
No se inicio la aplicacion, no se ejecuto semilla y no se modifico la base de
MonsterASP ni otro servicio externo.

## Respaldo previo

- Archivo: `C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle\Full\DB_Laboratorios_Univalle-PRE-NORMALIZACION-V2-20260902-091703.bak`.
- Tipo: copia completa `COPY_ONLY`, cifrada con AES-256 y con `CHECKSUM`.
- Tamano: 4.865.024 bytes.
- SHA-256: `35FC4FF4BF7B54322521645A0026D9C030115D8FFA4F3F066955E1B92955A6AD`.
- Verificacion inicial: `RESTORE VERIFYONLY` finalizo correctamente antes de
  modificar el esquema.
- Prueba de recuperacion real: se restauro sin reemplazar ninguna base en
  `DB_Laboratorios_Univalle_PRE_NORMALIZACION_20260902_QA`. La copia quedo
  `ONLINE`, paso `DBCC CHECKDB` y reprodujo 7 migraciones, 107 catalogos, 556
  unidades, 520 solicitudes, 157 mantenimientos y 1.020 verificaciones.

## Respaldo posterior normalizado

- Archivo: `C:\Users\monte\Documents\SQLServerBackups\DB_Laboratorios_Univalle\Full\DB_Laboratorios_Univalle-POST-NORMALIZACION-V2-20260902-095908.bak`.
- Tipo: copia completa `COPY_ONLY`, cifrada con AES-256, comprimida y con
  `CHECKSUM`.
- Tamano: 4.919.808 bytes.
- SHA-256: `D20DC457CDE6E1A26EF325B26E53E9249D07DAD27AA18C0EC56E5F7A52DEBBDE`.
- `RESTORE VERIFYONLY`: correcto.
- Restauracion fisica: correcta en
  `DB_Laboratorios_Univalle_POST_NORMALIZACION_20260902_QA`, sin reemplazar
  bases existentes. La copia quedo `ONLINE`, paso `DBCC CHECKDB` y contiene
  11 migraciones, 107 codigos de catalogo, 556 unidades, 520/121 relaciones
  puente y 158 filas de procedencia.

## Esquema aplicado

Se aplico un script idempotente y transaccional con las cuatro migraciones que
estaban pendientes en la base local:

1. `20260827022733_AlignHistoricalWorkbookV2`.
2. `20260827105439_AddHistoricalRequestSuggestion`.
3. `20260828161956_CompleteHistoricalWorkbookV2Model`.
4. `20260830202548_AddEquipmentClassificationGovernance`.

El script ejecutado esta en
`tmp/local-normalization-20260902/schema-v2-idempotent.sql` y su SHA-256 es
`72E6C5BA02E0B8D8D425830278F049FEBF41A3CD3EF6EB4E3C4218DAE3C20A2D`.
No contiene `DROP TABLE`, `DROP COLUMN`, `DELETE` ni `TRUNCATE`.

El primer intento fue rechazado por SQL Server porque la sesion no tenia
`QUOTED_IDENTIFIER` habilitado. La transaccion revirtio por completo: se
comprobo que no existian migraciones ni objetos V2 parciales. Se agregaron
solamente las opciones de sesion requeridas y el segundo intento finalizo
correctamente.

## Alineacion de catalogos y unidades

La V2 utilizada fue
`outputs/01a03b82-a789-7962-a84d-efffbf41fc20/Plantilla_Historicos_Normalizada_v2.xlsx`,
con SHA-256
`27D48AB1ADC97C85039749D538C1C78BAE289AF179A4DA3CA6F6E3DAD51F52EB`.

El original `D:\proyectoSis\Excels\Plantilla_Original.xlsx` permanece intacto:
284.414 bytes y SHA-256
`FAEAF00640BD4AF05D78C22B3B5FB7E105260BF8AF8A4B5E8FC2FAD4A78484A4`,
igual al hash conservado en el lote legacy.

- Los 107 catalogos V2 coincidieron 1:1 con los 107 catalogos locales por
  nombre, marca y modelo.
- No hubo coincidencias ambiguas, catalogos faltantes ni catalogos sobrantes.
- Se asignaron los 107 `CatalogCode` unicos (`CAT-####`).
- El backfill transaccional esta en
  `tmp/local-normalization-20260902/catalog-code-backfill-v2.sql`, SHA-256
  `2563D30BBEB4D51E1BF86E519BE0F7026F4E3DD51E2D3D903E9512EE53F22F6D`.
- Se creo el lote auditable
  `LOCAL-CATALOG-CODE-V2-27D48AB1ADC9`, contrato
  `catalog-code-alignment-v1`, con 107 filas en `ImportSourceRows`.
- Las 556 unidades V2 coincidieron 1:1 por `InventoryNumber` y cada una esta
  relacionada con el `CatalogCode` esperado.
- La segunda ejecucion del backfill produjo 0 actualizaciones y 0 inserciones.

La asignacion de codigo no confirma clasificaciones. Los 107 catalogos se
mantienen en `LegacyInferred`; existen 0 catalogos `Confirmed` y 0 decisiones
en `EquipmentClassificationDecisions`.

## Alineacion de codigos maestros

- Las 4 carreras V2 coincidieron 1:1 por nombre y facultad; se asignaron 4
  codigos `CAR-####` unicos.
- 24 de 26 actores V2 coincidieron 1:1 por nombre; se asignaron 24
  `ActorCode` unicos.
- Dos actores V2 permanecen `SourceOnly`: `ACT-0008` y `ACT-0009`. No se
  crearon personas ni se infirieron equivalencias.
- Dos personas locales permanecen `DatabaseOnly`: Id 21 e Id 26. Sus nombres
  no se forzaron contra actores diferentes de la V2.
- Las 2 facultades, 14 laboratorios y 5 gestiones V2 ya tenian codigos
  coincidentes. El laboratorio local `PENDIENTE` se conservo como
  `DatabaseOnly`.
- El script transaccional esta en
  `tmp/local-normalization-20260902/master-code-backfill-v2.sql`, SHA-256
  `979C8608BAD8437195D5DBC07805D34875CD929A774CC3A50FA12635ECB175DA`.
- Se creo el lote `LOCAL-MASTER-CODES-V2-27D48AB1ADC9`, contrato
  `master-code-alignment-v1`, con 51 filas: 49 coincidencias y 2 pendientes
  `SourceOnly`.
- La segunda ejecucion produjo 0 actualizaciones y 0 inserciones.

## Reconciliacion de conteos

| Entidad | Antes | Despues |
|---|---:|---:|
| `Equipments` | 107 | 107 |
| `EquipmentUnits` | 556 | 556 |
| `Requests` | 520 | 520 |
| `Maintenances` | 157 | 157 |
| `Verifications` | 1.020 | 1.020 |
| `MaintenancePlans` | 0 | 0 |
| `DataQualityIssues` | 1.289 | 1.289 |
| `RequestEquipmentUnits` | No existia | 520 |
| `MaintenanceRequests` | No existia | 121 |
| `Articles` | No existia | 0 |
| `EquipmentClassificationDecisions` | No existia | 0 |
| `ImportBatches` | 1 | 3 |
| `ImportSourceRows` | No existia | 158 |
| Carreras con codigo | 0 | 4 |
| Personas con `ActorCode` | 0 | 24 |

No se redujo ninguna tabla historica existente.

## Validaciones ejecutadas

- Preflight: 0 salidas sin unidad, 0 solicitudes con unidad invalida, 0
  mantenimientos con solicitud invalida y 0 inventarios duplicados.
- Postflight: 0 relaciones puente huerfanas y 0 unidades sin catalogo.
- FK deshabilitadas o no confiables: 0.
- restricciones `CHECK` deshabilitadas o no confiables: 0.
- `DBCC CHECKCONSTRAINTS`: correcto.
- `DBCC CHECKDB`: correcto.
- Segunda ejecucion del script de migraciones: sin cambios ni duplicados.
- Los scripts de catalogos y maestros se ejecutaron dos veces: la segunda
  ejecucion produjo 0 actualizaciones y 0 inserciones.
- Replay completo desde el backup previo en la base `_QA`: 16 metricas
  coincidieron con la base local oficial, con FK y CHECK confiables.
- `dotnet ef migrations has-pending-model-changes`: no existen cambios de
  modelo sin migracion.
- Compilacion secuencial, sin restaurar paquetes: 0 errores y 0 advertencias.

La auditoria del diccionario y `94_MAPEO_IMPORTADOR` confirmo que las columnas,
nulabilidad, indices unicos, relaciones N:M y restricciones de exclusividad
existen en SQL. Se detecto una inconsistencia documental en la V2 inmutable:
`19_PLAN_L48.ResponsableActorCodigo` figura asociado a `ManagementPlan`, pero
el contrato aprobado, el paquete canonico y la base usan `MaintenancePlan`.
No se modifico la V2; la correccion debe realizarse en una copia controlada o
en su siguiente version.

`CloudMigrationGuard preflight` no debe ejecutarse como certificacion final de
una base ya normalizada: por contrato espera exactamente DB-001 pendiente y
cero objetos V2. Al ejecutarlo contra el resultado final rechazo correctamente
la base porque ya existen las migraciones y objetos esperados.

Para evitar reutilizar incorrectamente ese preflight se agrego el modo de solo
lectura `CloudMigrationGuard snapshot`. Este exige las cuatro migraciones, el
contrato historico completo, el gobierno de clasificacion, cero relaciones
huerfanas, cero restricciones deshabilitadas/no confiables y
`DBCC CHECKCONSTRAINTS` limpio. El snapshot local fue aprobado:

- archivo: `tmp/local-normalization-20260902/snapshot-normalized-local.json`;
- SHA-256: `E74FAE012A96A6A55B50B686D3B3001B9EDA719C245472C290391F234335AAF6`;
- `SchemaReady = true` y `SnapshotValid = true` al consumirlo desde
  `HistoricalDataApply`.

## Plan de carga vigente

Se regenero `HistoricalDataApply plan` contra la base local normalizada y el
paquete canonico, en modo de solo lectura:

- plan: `tmp/local-normalization-20260902/historical-apply-plan-current.json`;
- SHA-256: `B1CC19A34DCF12554D3409D2FFE9539926548A81998E17ACA4985247A391BEB7`;
- propuesta de reconciliacion:
  `tmp/local-normalization-20260902/reconciliation-current.proposed.json`;
- SHA-256 de la propuesta:
  `DA3A0EE44ADB5DAD0D691F25F0F9F00E5EFBC8954D344AD0C0F199BE185322A0`.

Resultado actual: `CanApply = false`, con 686 condiciones explicitas:

- 1 puerta que agrupa las 45 incidencias P0;
- 11 bloqueos semanticos por valores institucionales sin confirmar;
- 672 diferencias historicas de contenido;
- 1 ausencia de manifiesto de reconciliacion aprobado;
- 1 ausencia deliberada del delta SQL, que no se genera mientras las puertas
  anteriores sigan abiertas.

El reconciliador mantiene 2.037 mapeos: 391 coincidencias de contenido, 214
`SourceOnly`, 760 `DatabaseOnly`, 76 duplicados legacy explotados, 672
conflictos de negocio y 0 ambiguedades. Por tanto, el impedimento restante es
de evidencia y decision funcional; no es una falla de esquema o integridad de
la base local.

EF mantiene una advertencia de diseno: `Equipment` tiene filtro global y es el
extremo requerido de `EquipmentClassificationDecision`. No impide la
migracion, pero debe incluirse en la regresion de consultas historicas para
evitar que una decision quede oculta cuando su catalogo sea filtrado.

## Trabajo que permanece bloqueado

La normalizacion estructural local y las relaciones legacy inequívocas estan
implementadas. La carga completa de datos de negocio de la V2 no se ejecuto
porque aun no existe evidencia funcional suficiente:

- los 107 catalogos necesitan confirmacion de categoria, subtipo y estado;
- el ultimo paquete canonico conserva 45 incidencias P0;
- existen 672 diferencias historicas de contenido que requieren conciliacion
  revisada;
- `HistoricalDataApply` permanece con `CanApply=false` hasta recibir y congelar
  la respuesta de la cliente, reducir P0 a cero y aprobar el manifiesto de
  reconciliacion.

No se transformo ningun `Por confirmar` en `Other`, no se inventaron fechas,
estados, laboratorios, costos ni responsables y no se promovio ninguna
clasificacion tecnica local.

## Proximas puertas

1. **P0 - Cliente/Datos:** completar y aprobar las planillas de levantamiento;
   congelar la respuesta por SHA-256.
2. **P0 - Datos:** regenerar paquete e incidencias; exigir P0 igual a cero y
   aprobar por fila la reconciliacion legacy-V2.
3. **P0 - QA local:** probar clasificacion efectiva, historicos y navegacion
   contra esta base ya alineada.
4. **P1 - Carga QA:** aplicar el delta idempotente primero en una base `_QA`,
   nunca directamente sobre la base local oficial ni sobre la nube.
5. **P1 - MonsterASP:** tratar la nube como una operacion independiente con su
   propio backup, preflight, ventana y autorizacion explicita.
