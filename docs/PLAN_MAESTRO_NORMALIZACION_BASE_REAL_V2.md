# Plan Maestro De Normalizacion De La Base Real Segun Excel V2

## 1. Objetivo Final

Conseguir que la base SQL Server oficial de MonsterASP represente el mismo
modelo normalizado que `Plantilla_Historicos_Normalizada_v2.xlsx`, preservando
todos los datos actuales, su procedencia y la compatibilidad temporal con la
aplicacion.

El trabajo termina solamente cuando se compruebe en la base oficial que:

- catalogo y unidades fisicas estan separados;
- todas las relaciones V2 existen y no tienen huerfanos;
- los datos confirmados del Excel estan conciliados e importados una sola vez;
- los datos desconocidos permanecen nulos o en cuarentena, nunca inventados;
- las pantallas y reportes principales funcionan con el esquema resultante;
- EF Core informa cero migraciones pendientes;
- existe evidencia de respaldo, QA, hashes, conteos y postflight.

## 2. Alcance Y Exclusiones

### Incluido

- Entidades EF Core, relaciones, indices, FK y restricciones CHECK.
- Migraciones DB-001 y DB-002.
- Conciliacion Excel V2 contra los datos legacy existentes.
- Carga idempotente de datos confirmados y trazabilidad de cada fila.
- Pruebas de integridad, aplicacion y reversa en una base `_QA`.
- Promocion controlada a `db65393` en MonsterASP.

### Excluido

- Rediseñar otra vez el Excel V2.
- Eliminar registros de la base porque no aparezcan en el Excel.
- Completar vacios por aproximacion.
- Ejecutar semillas, `Down`, `DELETE`, `TRUNCATE` o migraciones automaticas.
- Cambiar credenciales, proveedor SQL o infraestructura del sitio.

## 3. Estado De Partida Verificado

| Elemento | Estado actual | Evidencia |
|---|---|---|
| Proyecto | Compila correctamente | `dotnet build`: 0 errores y 0 advertencias, 30-08-2026 |
| Modelo EF | Sin cambios posteriores a la ultima migracion local | `migrations has-pending-model-changes`: sin cambios |
| Base oficial | `db65393`, siete migraciones aplicadas en el ultimo preflight | `tmp/cloud-v2-run/preflight-cloud.json` |
| DB-001 | Dos migraciones pendientes en la nube; ensayo QA anterior aprobado | `AlignHistoricalWorkbookV2` y `AddHistoricalRequestSuggestion` |
| DB-002 | Migracion creada localmente; no aplicada a ninguna base | `CompleteHistoricalWorkbookV2Model` |
| Datos actuales | 107 catalogos, 556 unidades, 520 solicitudes, 157 mantenimientos, 1.020 verificaciones y 519 planes | ultimo manifiesto remoto |
| Paquete V2 | 11.939 filas; contiene incidencias que no pueden resolverse por inferencia | paquete canonico V4 |
| Respuesta de cliente | No existe archivo `RESPONDIDA` en el workspace | inspeccion de `Excels` y `outputs` |

El manifiesto remoto debe repetirse antes de ejecutar cualquier cambio; el de
27-08-2026 es evidencia historica y no una autorizacion vigente.

## 4. Arquitectura Objetivo

| Dominio V2 | Destino SQL/EF | Regla obligatoria |
|---|---|---|
| Catalogo de equipos | `Equipments` | Un registro por definicion reutilizable; `CatalogCode` unico cuando existe |
| Unidad fisica | `EquipmentUnits` | Un registro por `InventoryNumber`; serie, laboratorio, valor y condicion pertenecen aqui |
| Solicitud y unidades | `Requests` + `RequestEquipmentUnits` | Cabecera unica y relacion N:M |
| Mantenimiento y solicitudes | `Maintenances` + `MaintenanceRequests` | Cero o varias solicitudes por mantenimiento |
| Participantes | `MaintenanceParticipants` | Varios actores y un principal controlado |
| Costos | `CostDetails` | Exactamente un padre: solicitud o mantenimiento |
| Salidas | `Departures` + `DepartureItems` | Cabecera independiente y detalle por unidad o articulo |
| Articulos L-3 | `Articles` | Consumibles no inventariados separados de equipos |
| Plan L-48 | `MaintenancePlans` | No reutilizar `ManagementPlans`, que pertenece al wizard |
| Procedencia | `ImportBatches` + `ImportSourceRows` | Clave determinista, hash y estado por fila |
| Calidad | `DataQualityIssues` y cuarentenas | Ambiguedades preservadas y auditables |

## 5. Estrategia De Ejecucion

### Fase 0 — Congelamiento Y Autorizacion De QA

**Objetivo:** asegurar que el ensayo represente exactamente la base actual.

1. Confirmar que `AutoMigrate=false` y `RunSeed=false`.
2. Obtener un BACPAC o backup nuevo de `db65393`.
3. Registrar nombre, fecha, tamaño y SHA-256.
4. Importarlo con un nombre nuevo terminado en `_QA`.
5. Ejecutar preflight y guardar conteos, migraciones, objetos y bloqueos.
6. Comparar contra el manifiesto anterior y explicar cualquier diferencia.

**Puerta 0:** backup restaurable, base correcta y autorización explicita para
aplicar migraciones solamente sobre `_QA`.

### Fase 1 — Alineacion Estructural DB-001 En QA

**Objetivo:** corregir el error inmediato de `Equipments.CatalogCode` y crear
las estructuras V2 aditivas ya ensayadas.

1. Aplicar, en orden:
   - `20260827022733_AlignHistoricalWorkbookV2`;
   - `20260827105439_AddHistoricalRequestSuggestion`.
2. Verificar `CatalogCode`, indice unico filtrado y `Suggestion` nullable.
3. Verificar las tablas de articulos, procedencia y relaciones puente.
4. Confirmar los backfills esperados:
   - 520 relaciones solicitud-unidad;
   - 121 relaciones mantenimiento-solicitud.
5. Ejecutar el mismo script una segunda vez; no debe cambiar nada.

**Puerta 1:** conteos preexistentes sin reduccion, cero FK huerfanas, cero
restricciones deshabilitadas y las dos migraciones registradas.

### Fase 2 — Contrato Completo DB-002 En QA

**Objetivo:** cubrir las diferencias semanticas restantes entre el Excel V2 y
el modelo SQL.

1. Revisar el SQL idempotente de
   `20260828161956_CompleteHistoricalWorkbookV2Model`.
2. Bloquearlo si contiene eliminacion de tablas, columnas o datos.
3. Aplicarlo sobre la misma QA despues de DB-001.
4. Verificar:
   - `Requests.RequestDate` nullable;
   - `Verifications.ObservedEquipmentStatus` nullable;
   - roles operativos completos de actores;
   - nulabilidad fiel de salidas L-3;
   - contrato historico de `MaintenancePlans`;
   - indices unicos y FK de planes, responsables, gestion y lote.
5. Reejecutar el script para comprobar idempotencia.
6. Ejecutar `DBCC CHECKDB` y `DBCC CHECKCONSTRAINTS`.

**Puerta 2:** EF y SQL coinciden, cero migraciones pendientes en QA, integridad
limpia y ninguna tabla existente pierde filas.

### Fase 3 — Conciliacion De Datos Excel V2 Contra QA

**Objetivo:** decidir por clave y contenido que se conserva, corrige, agrega o
mantiene en cuarentena.

1. Congelar la V2 utilizada y calcular su SHA-256.
2. Regenerar paquete canonico e incidencias desde ese archivo exacto.
3. Generar snapshot nuevo de QA despues de DB-002.
4. Conciliar las 2.037 correspondencias legacy→V2 existentes:
   - coincidencias: conservar sin reescribir;
   - solo Excel: proponer alta;
   - solo base: conservar, nunca eliminar;
   - conflicto: exigir decision y evidencia;
   - duplicado legacy explotado: preservar la cabecera y sus relaciones.
5. Tratar las 528 fechas transpuestas de verificaciones como correccion
   tecnica verificable contra la celda original, no como pregunta a la cliente.
6. Resolver los patrones de gestion/fecha de los 144 mantenimientos mediante
   una regla aprobada y excepciones documentadas.
7. Mantener nulos o cuarentena para datos sin evidencia.

**Puerta 3:** manifiesto de reconciliacion revisado por fila, cero ambiguedad de
clave y cero P0 que impida materializar una entidad o relacion valida.

### Fase 4 — Delta Idempotente De Negocio En QA

**Objetivo:** aplicar los datos normalizados sin duplicar ni borrar.

1. Ejecutar `HistoricalDataApply plan` en modo de solo lectura.
2. Revisar hashes de paquete, incidencias, snapshot y reconciliacion.
3. Generar un delta con UPSERT por claves estables:
   - catalogo por `CatalogCode`;
   - unidad por `InventoryNumber`;
   - actor por `ActorCode`;
   - maestros por codigo;
   - historicos por `HistoricalSourceKey`.
4. Aplicarlo dentro de una transaccion solamente en `_QA`.
5. Ejecutarlo por segunda vez y comparar conteos y huellas de contenido.
6. Registrar toda fila en `ImportSourceRows` y todo conflicto no promovido en
   `DataQualityIssues`.

**Puerta 4:** segunda ejecucion sin altas ni cambios adicionales, cero
huerfanos, cero perdida de datos legacy y reconciliacion completa de conteos.

### Fase 5 — Regresion De Aplicacion En QA

**Objetivo:** demostrar que el esquema y los datos normalizados funcionan con
la aplicacion real.

Probar como minimo:

1. login y autorizacion;
2. catalogos y unidades fisicas;
3. filtros por tipo/clasificacion y laboratorio;
4. L-6 verificaciones;
5. L-7 solicitudes multiinventario;
6. L-8 mantenimientos y participantes;
7. L-3 salidas y articulos;
8. Kardex/L-48 y planes;
9. dashboard, notificaciones y reportes;
10. alta, edicion y consulta de un registro nuevo con las relaciones V2.

**Puerta 5:** cero P0/P1 funcionales, resultados firmados por QA y restauracion
del backup probada.

### Fase 6 — Promocion A MonsterASP

**Objetivo:** reproducir en produccion exactamente lo aprobado en QA.

1. Abrir ventana de mantenimiento y detener nuevas escrituras.
2. Crear y descargar backup final del proveedor; registrar SHA-256.
3. Repetir preflight remoto.
4. Si cambio esquema o contenido relevante desde QA, cancelar y repetir el
   ensayo con un BACPAC nuevo.
5. Aplicar los mismos scripts DB-001 y DB-002 aprobados por hash.
6. Aplicar el mismo delta de datos aprobado por hash.
7. Ejecutar postflight, DBCC, conteos y validacion de FK/CHECK.
8. Reactivar la aplicacion y repetir los flujos criticos.
9. Observar errores de aplicacion y SQL durante al menos 30 minutos.

**Puerta 6:** requiere autorizacion explicita inmediatamente antes de ejecutar
SQL sobre `db65393`.

### Fase 7 — Cierre Y Auditoria

1. Confirmar cero migraciones EF pendientes.
2. Confirmar hashes y conteos finales.
3. Archivar backup, preflight, scripts, paquete, reconciliacion y postflight.
4. Documentar cuarentenas que permanezcan abiertas sin declararlas migradas.
5. Entregar informe de cambios, validaciones y riesgos residuales.

## 6. Matriz De Responsabilidad

| Actividad | Datos/EF | QA | Usuario/operador |
|---|---:|---:|---:|
| Revisar modelo, migraciones y delta | Responsable | Revisor | Informado |
| Crear backup/BACPAC de MonsterASP | Asiste | Informado | Responsable |
| Autorizar migracion QA | Ejecuta despues | Verifica | Autoriza |
| Resolver conflicto de negocio | Propone | Verifica evidencia | Aprueba decision |
| Ejecutar regresion | Asiste | Responsable | Valida resultado |
| Autorizar produccion | Prepara | Recomienda | Responsable |
| Ejecutar produccion | Responsable tecnico | Observa | Autoriza y mantiene ventana |

## 7. Politica De Reversa

- Fallo antes de `COMMIT`: rollback de la transaccion y QA/produccion permanece
  detenida para diagnostico.
- Esquema correcto, aplicacion incompatible: volver a la version anterior de
  la aplicacion; conservar el esquema aditivo.
- Integridad o conteos incorrectos: restaurar backup/snapshot; no ejecutar
  migraciones `Down` en produccion.
- Hash, base o preflight diferente: no ejecutar nada.
- Una reejecucion crea duplicados: rechazar el adaptador y corregir en QA.

## 8. Criterios Exactos De Aceptacion

- `db65393` contiene DB-001 y DB-002 y EF reporta cero pendientes.
- `Equipments.CatalogCode` existe y es unico cuando no es nulo.
- Existen 107 catalogos y 556 unidades como minimo, sin perder registros
  adicionales validos de la base.
- Cada unidad tiene inventario unico y catalogo valido.
- Laboratorio desconocido permanece nulo con resolucion pendiente.
- Existen 520 relaciones solicitud-unidad y las relaciones adicionales
  aprobadas del Excel, sin huerfanos.
- Existen 121 relaciones mantenimiento-solicitud legacy y las adicionales
  aprobadas, sin imponer 1:1.
- Costos y detalles L-3 cumplen exclusividad de padre/referencia.
- Todas las filas promovidas tienen procedencia determinista.
- Segunda ejecucion del delta produce cero cambios.
- `DBCC CHECKDB` y `DBCC CHECKCONSTRAINTS` son correctos.
- Las pruebas funcionales criticas pasan.
- Backup y restauracion estan demostrados.
- Ninguna credencial queda en repositorio, comandos, logs o artefactos.

## 9. Autorizaciones Necesarias

Se solicitaran dos autorizaciones separadas y concretas:

1. **Autorizacion QA:** aplicar DB-001, DB-002 y el delta aprobado solamente en
   una base cuyo nombre termine en `_QA`.
2. **Autorizacion produccion:** aplicar los artefactos ya aprobados por hash en
   `db65393`, despues del backup final y el preflight vigente.

Autorizar QA no autoriza produccion. Autorizar el esquema no autoriza una carga
de datos distinta o un archivo con otro hash.
