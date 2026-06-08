# README - Correccion Para Importacion Historica Del Excel

Documento de control para entender el estado actual del Excel, corregirlo manualmente y comparar si las correcciones quedaron listas antes de regenerar `load.sql` o intentar importar en SQL Server Developer Edition.

No modificar migraciones existentes. No ejecutar SQL automaticamente. No inventar datos faltantes. No guardar credenciales reales en documentacion.

## Fuentes Revisadas

- Excel original: `C:\Users\Nihaht\Downloads\PLANTILLA_CARGA_DATOS_v3 1 (1) 1 1.xlsx`.
- Auditoria generada: `tmp/data-audit/audit.md`.
- Incidencias: `tmp/data-audit/issues.csv`.
- Rechazos: `tmp/data-audit/rejects.csv`.
- Vista normalizada: `tmp/data-audit/normalized-preview.csv`.
- SQL generado offline: `tmp/data-audit/load.sql`.
- Modelos actuales: `EquipmentUnit`, `Request`, `Maintenance`, `CostDetail`, `Departure`, `ManagementPlan`.
- Modulos vigentes: L-3 Salidas, Kardex/L-48 y L-12 Adquisiciones.

## Estado Actual De Auditoria

| Entidad | Detectado / insertable actual | Estado |
|---|---:|---|
| Faculties | 3 | Insertable |
| Careers | 4 | Insertable |
| Laboratories | 14 | Insertable |
| People / Providers | 8 | Insertable, pero 2 son autocreados desde L-3 |
| Managements | 3 | Insertable |
| Equipments | 107 | Insertable |
| EquipmentUnits | 556 | Insertable, pero solo 7 con laboratorio |
| Verifications | 0 | No insertable |
| Requests | 15 | Insertable parcial |
| Maintenances | 1 | Insertable parcial |
| CostDetails | 4 | Insertable parcial |
| Departures | 2 | Insertable parcial |
| DepartureItems | 2 | Insertable parcial |
| ManagementPlans | 2 | Insertable parcial |
| Issues | 727 | 416 errores |
| Rejects | 1053 | Deben reducirse antes de importar |

Rechazos por hoja:

| Hoja | Rechazos | Causa principal |
|---|---:|---|
| `3 · Verificaciones` | 660 | Falta inventario real en columna A o hay codigos de laboratorio/area en vez de inventario |
| `4 · Solicitudes` | 388 | Inventarios sin laboratorio, tokens multiples invalidos o referencias sin unidad valida |
| `8 · Plan de Mantenimiento` | 2 | Faltan gestion/inventario |
| `1 · Catálogos` | 2 | Inventario duplicado `3289` y una fila basura/incompleta |
| `5 · Kardex Mantenimiento` | 1 | Fila incompleta/no enlazable |

## Reglas De Base De Datos Y Flujo

SQL Server debe apuntar al motor completo local:

- Usar `Server=localhost` o `Data Source=.`.
- No usar LocalDB.
- No usar `SQLEXPRESS`.
- Se puede usar autenticacion Windows o mixta con `sa`, pero nunca documentar contrasenas reales.

El wizard real del sistema no es solo insercion de tablas. Para que los datos queden navegables en interfaz:

1. L-6 crea o enlaza `Verification`.
2. L-7 crea o enlaza `Request` tecnica.
3. L-8 crea o enlaza `Maintenance`.
4. L-3 crea o enlaza `Departure`.
5. Kardex/L-48 cierra mantenimiento, devuelve salida si aplica y actualiza historial.
6. L-12 crea o enlaza `Request` de adquisicion/desembolso.
7. Completados es `Step = 7` visual, no un nuevo enum.

`ManagementPlan` es el eje que debe conservar el hilo:

- `ManagementPlan.ManagementId`: gestion.
- `ManagementPlan.EquipmentUnitId`: unidad/inventario.
- `ManagementPlan.VerificationId`: L-6.
- `ManagementPlan.RequestId`: solicitud tecnica L-7.
- `ManagementPlan.MaintenanceId`: mantenimiento/Kardex L-8.
- `ManagementPlan.DepartureId`: salida L-3.
- `ManagementPlan.KardexHistoryId`: historial final de estado.
- `ManagementPlan.AcquisitionRequestId`: adquisicion/desembolso L-12.

Si se insertan planes sin esos enlaces, la base puede aceptar registros, pero la interfaz no podra reconstruir correctamente el flujo.

## Enums Criticos Para Comparar

| Enum | Valores |
|---|---|
| `WizardPhase` | `Verification=1`, `TechnicalRequest=2`, `Maintenance=3`, `Exit=4`, `Kardex=5`, `Disbursement=6` |
| `WizardEquipmentState` | `PendingVerification=1`, `VerifiedGood=2`, `AwaitingRequest=3`, `AwaitingMaintenance=4`, `InMaintenance=5`, `AwaitingDeparture=6`, `AwaitingKardex=7`, `AwaitingDisbursement=8`, `Completed=9` |
| `ManagementPlanStatus` | `Pending=0`, `InProgress=1`, `Completed=2`, `Overdue=3` |
| `RequestType` | `Technical=1`, `Purchasing=2`, `Calibration=3` |
| `RequestPriority` | `Low=0`, `Medium=1`, `High=2`, `VeryHigh=3` |
| `RequestStatus` | `Pending=0`, `Scheduled=1`, `InProgress=2`, `Approved=3`, `Rejected=4`, `Completed=5`, `Cancelled=99` |
| `MaintenanceType` | `Preventivo=1`, `Correctivo=2`, `Calibracion=3`, `Limpieza=4`, `Electrico=5`, `Otros=99` |
| `MaintenanceStatus` | `Pending=0`, `InProgress=1`, `Completed=2`, `Scheduled=3`, `Cancelled=99` |
| `DepartureType` | `ExternalLoan=1`, `InternalLoan=2`, `ExternalMaintenance=3`, `DefinitiveExit=4`, `InternalMaintenance=5` |
| `LoanStatus` | `Active=0`, `Returned=1`, `Overdue=2`, `Cancelled=99` |
| `CostCategory` | `None=0`, `SparePart=1`, `Labor=2`, `Tool=3`, `Transport=4`, `ExternalService=5`, `Calibration=6`, `Consumables=7`, `TravelExpenses=8`, `Staff=9`, `Others=99` |

## Correcciones Manuales Del Excel

| Hoja | Columna actual | Tabla / campo SQL Server | Problema detectado | Modificacion requerida | Prioridad |
|---|---|---|---|---|---|
| `1 · Catálogos` | Inventario | `EquipmentUnit.InventoryNumber` | Inventario duplicado critico `3289` en filas 315/316 | Dejar un solo `3289` o corregir el numero real de la fila duplicada. No pueden existir dos unidades con el mismo inventario | Critica |
| `1 · Catálogos` | Nombre / Inventario | `Equipment`, `EquipmentUnit` | Fila `1048168` sin nombre o inventario util | Eliminarla del rango util o completar nombre e inventario real | Critica |
| `1 · Catálogos` | Nombre, Marca, Modelo | `Equipment.Name`, `Brand`, `Model` | Se generan 107 equipos, no 109, porque hay normalizaciones/filas rechazadas y claves que se agrupan por `(Nombre, Marca, Modelo)` | Revisar equipos que esperabas separados. Si son realmente modelos diferentes, corregir marca/modelo para que la clave sea distinta | Importante |
| `2 · IInventario` | Laboratorio | `EquipmentUnit.LaboratoryId` | Solo 7 unidades tienen laboratorio; muchas solicitudes se rechazan por no tener laboratorio | Completar laboratorio valido para cada inventario usado en solicitudes, verificaciones, L-3, Kardex, costos y L-48 | Critica |
| `2 · IInventario` | Laboratorio | `EquipmentUnit.LaboratoryId` | `49236` aparece como `H-3` en unidad, pero L-48 fila 7 dice `H-1` | Definir el laboratorio correcto y dejarlo igual en inventario y L-48 | Critica |
| `3 · Verificaciones` | A | `Verification.EquipmentUnitId` | Hay 0 verificaciones insertables; columna A trae vacios, `H1`, `H-6`, `k1`, areas u oficinas | Reemplazar columna A por el `N° Inventario` real existente en hoja 1/2 para cada verificacion | Critica |
| `3 · Verificaciones` | Resultado / observaciones | `Verification`, `VerificationFaults` | Sin inventario real no se puede asociar falla al equipo | Completar resultado/falla solo despues de tener inventario real. No importar verificaciones huerfanas | Critica |
| `4 · Solicitudes` | Inventario | `Request.EquipmentUnitId` | 388 rechazos por inventarios sin laboratorio o tokens invalidos | Cada inventario debe existir en hoja 1/2 y tener laboratorio. Separar multiples inventarios solo si todos son tokens validos | Critica |
| `4 · Solicitudes` | Prioridad | `Request.Priority` | Prioridad vacia al 100%; auditor usa `Medium=1` automaticamente | Recomendado completar manualmente `Baja`, `Media`, `Alta` o `Critica`. Usar `Medium=1` solo como fallback tecnico | Importante |
| `4 · Solicitudes` | Descripcion | `Request.Description` | Algunas descripciones son debiles, por ejemplo `NINGUNO` o `FACTORES` | Completar problema real o mantenimiento solicitado; no usar texto vacio o generico | Importante |
| `5 · Kardex Mantenimiento` | Inventario / tecnico / fechas | `Maintenance`, `CostDetail.MaintenanceId` | Solo 1 mantenimiento insertable: `MNT_R6_35528`; falta encaje con L-3/adquisicion de `34179` | Si `34179` tiene salida/adquisicion por mantenimiento, agregar o corregir Kardex para `34179`; si no, cambiar L-3/costos al inventario correcto | Critica |
| `6 · Detalles de Costo` | Tipo | `CostDetail.RequestId` o `CostDetail.MaintenanceId` | `CostDetail` se comparte como tabla entre adquisicion y Kardex, pero cada fila debe apuntar a un destino concreto | Usar `Mantenimiento/Kardex` para costos de L-8 y `Desembolso/Adquisicion` para costos L-12. No mezclar destino en la misma fila | Critica |
| `6 · Detalles de Costo` | Referencia | `Request.InvestmentCode`, correlacion interna | Filas 8/9 usan `CUCHARA-001` para adquisicion de `34179` | Mantener codigo limpio y unico si es la misma adquisicion; si pertenece a mantenimiento, agregar referencia al Kardex correcto | Importante |
| `6 · Detalles de Costo` | Concepto, cantidad, unidad, precio | `CostDetail.Concept`, `Quantity`, `UnitOfMeasure`, `UnitPrice` | Se requieren datos completos y monto calculable | Completar concepto, cantidad mayor a 0, unidad y precio unitario. El subtotal se calcula como cantidad * precio | Critica |
| `7 · Salidas (L-3)` | Facultad | `Faculty.Name` / contexto de laboratorio | Facultad escrita como `Facultad de Arquitectura y Turismo - Carrera de Gastronomía` | Normalizar a `Facultad de Gastronomia y Turismo - Carrera de Gastronomía` | Importante |
| `7 · Salidas (L-3)` | Responsable | `Departure.BorrowerId` / `Person` | `ING. SARA PEREZ YAÑEZ` y `DOCENTE MARTINEZ` se autocrean desde L-3, no existen en hoja 9 | Agregar ambos exactamente en `9 · Técnicos y Proveedores`, con tipo/categoria correcta | Critica |
| `7 · Salidas (L-3)` | Retorno estimado | `Departure.EstimatedReturnDate` | Fila 6 no tiene retorno estimado | Completar fecha si la salida requiere devolucion. Si no aplica, documentar excepcion y ajustar tipo/flujo | Critica |
| `7 · Salidas (L-3)` | Tipo | `Departure.Type` | Fila 7 es `Préstamo Interno`; el wizard L-3 espera salida por mantenimiento derivada del tecnico de L-8 | Decidir si es prestamo standalone o parte del wizard. Si es wizard, debe existir `Maintenance` vinculado y tipo se infiere desde tecnico | Critica |
| `8 · Plan de Mantenimiento` | A Gestion | `Management.Year`, `Management.Semester` | Fila 9 no tiene gestion valida | Completar formato exacto `YYYY-S`, por ejemplo `2025-1` | Critica |
| `8 · Plan de Mantenimiento` | B Inventario | `ManagementPlan.EquipmentUnitId` | Fila 6 y fila 9 no tienen inventario | Completar inventario real existente en hoja 1/2 | Critica |
| `8 · Plan de Mantenimiento` | G Tecnico / Empresa | `ManagementPlan.Responsible`, `Maintenance.TechnicianId` | Responsable debe existir como persona/proveedor si el plan enlaza mantenimiento | Agregar o normalizar nombre en hoja 9 exactamente igual | Importante |
| `8 · Plan de Mantenimiento` | I Completado | `ManagementPlan.PlanStatus`, `WizardEquipmentState` | Valores vacios o ambiguos pueden dejar estado incorrecto | Usar `Sí`, `No` o `Pendiente` de forma consistente | Importante |
| `9 · Técnicos y Proveedores` | Nombre / Tipo / Empresa | `Person`, `Intern`, `Extern` | Responsables usados en L-3 y L-48 deben existir antes de referenciarlos | Agregar `ING. SARA PEREZ YAÑEZ`, `DOCENTE MARTINEZ` y cualquier tecnico de L-48 faltante | Critica |

## Puntos Especiales Que Deben Encajar

### 107 Equipments vs 109 esperados

La auditoria actual genera 107 `Equipments` porque la fuente principal es `1 · Catálogos` y agrupa por `(Nombre, Marca, Modelo)`. Dos causas explican la diferencia:

- Filas rechazadas: duplicado `3289` y una fila sin datos utiles.
- Normalizacion/agrupacion: si dos filas tienen el mismo nombre, marca y modelo, son un solo `Equipment` con varias `EquipmentUnits`.

Correccion: revisar los equipos que esperabas como catalogos separados. Si son fisicamente el mismo tipo/modelo, 107 es correcto. Si deben ser modelos distintos, corregir marca/modelo/nombre en hoja 1.

### Duplicado 3289

`EquipmentUnit.InventoryNumber` debe ser unico. El duplicado `3289` bloquea una de las filas.

Correccion: verificar inventario fisico. Opciones validas:

- Mantener una fila `3289` y eliminar la duplicada del rango util.
- Cambiar la fila duplicada al inventario real si fue error de digitacion.

No usar sufijos inventados como `3289-2` sin respaldo institucional.

### 0 Verifications insertables

No hay verificaciones insertables porque `3 · Verificaciones`, columna A, no contiene inventarios reales. Tiene vacios o codigos de laboratorio/area como `H-6`, `H1`, `k1`, `OFICINA`, `AREA CIRCULACION`.

Correccion: cada fila de verificacion debe tener un `InventoryNumber` existente en hoja 1/2. Si la verificacion fue por laboratorio completo y no por equipo, debe dividirse en filas por inventario real o quedar fuera de importacion.

### Priority Medium = 1

Es tecnicamente valido usar `RequestPriority.Medium=1` cuando la prioridad esta vacia, porque la BD requiere un valor. Pero para datos historicos confiables conviene completar prioridades manualmente:

- `Low=0`: baja.
- `Medium=1`: media.
- `High=2`: alta.
- `VeryHigh=3`: critica.

Si no hay criterio historico, dejar `Medium=1` como fallback documentado.

### CostDetail compartido por solicitud de adquisicion, Kardex y reflejo en L-48

`CostDetail` es una sola tabla, pero cada fila se enlaza por FK nullable:

- Costo de Kardex / mantenimiento: `CostDetail.MaintenanceId`.
- Costo de adquisicion / desembolso: `CostDetail.RequestId` apuntando a `Request.Type = Purchasing`.

L-48 no tiene costos directos. L-48 refleja costos mediante `ManagementPlan.MaintenanceId` y `ManagementPlan.AcquisitionRequestId`.

Regla de encaje:

1. Si el costo es trabajo/repuesto ejecutado en L-8/Kardex, debe enlazarse al `Maintenance`.
2. Si el costo es solicitud de compra/desembolso L-12, debe enlazarse al `Request` de adquisicion.
3. Para que se vea en L-48, el `ManagementPlan` debe enlazar el mantenimiento y/o la adquisicion correspondiente.
4. Si una adquisicion nace desde costos del mantenimiento, la interfaz L-12 actualmente copia/sincroniza costos desde mantenimiento hacia la solicitud; no reutiliza necesariamente la misma fila fisica de `CostDetail`.

### L-3 salidas con interfaz

La interfaz vigente para L-3 del wizard es `Pages/Departures/MassCreate`.

Reglas:

- L-3 wizard espera `ManagementPlan.CurrentPhase = Exit`.
- Estado esperado: `ManagementPlan.CurrentState = AwaitingDeparture`.
- Debe existir `Maintenance.TechnicianId`.
- Debe existir `EquipmentUnit.LaboratoryId`.
- El responsable se deriva del tecnico del mantenimiento, no de una columna libre del Excel.
- El tipo de salida se infiere:
  - tecnico externo o categoria externa: `ExternalMaintenance=3`;
  - tecnico interno: `InternalMaintenance=5`.
- El producto se deriva de `Equipment.Name` + `InventoryNumber`.
- Fechas globales son obligatorias.
- Guardado final marca la unidad `OnLoan`, vincula `DepartureId` al plan y avanza a Kardex.

Por eso las salidas historicas deben corregirse para encajar con mantenimiento/plan, no solo insertarse como `Departure` aislada.

## Casos Actuales Que Deben Decidirse

| Inventario | Estado actual | Problema | Decision requerida |
|---|---|---|---|
| `34179` | Tiene L-3, adquisicion y costos de adquisicion | No tiene Kardex/L-8 ni L-48 enlazado; L-3 dice "Ver kardex ID 1" pero el unico Kardex insertable es `35528` | Crear/corregir Kardex y L-48 para `34179`, o confirmar que esos datos pertenecen a otro inventario |
| `17355` | Tiene L-3 y L-48 pendiente | L-3 fila 7 es prestamo interno; no hay mantenimiento enlazado | Decidir si es salida standalone o parte del wizard. Si es wizard, agregar L-8/mantenimiento |
| `49236` | Tiene L-48 completado | Laboratorio inconsistente: unidad `H-3`, L-48 `H-1`; no hay enlaces a solicitud/mantenimiento/adquisicion | Corregir laboratorio y completar enlaces si corresponde |
| `35528` | Tiene Kardex y costos de mantenimiento | No tiene L-48 insertable enlazado | Agregar plan L-48 si debe aparecer en mantenimiento historico |
| `34180`, `34744`, `39170` | Tienen solicitudes tecnicas insertables | Falta continuidad hacia mantenimiento/L-48 si fueron atendidas | Completar Kardex/L-48 solo si hubo ejecucion real |

## Estado De Modulos Especificos

### L-3 Salidas

Insertables actuales:

- Hoja `7 · Salidas (L-3)`, fila 6: inventario `34179`, responsable `ING. SARA PEREZ YAÑEZ`, producto `COCINA INDUSTRIAL`, retorno estimado vacio.
- Hoja `7 · Salidas (L-3)`, fila 7: inventario `17355`, responsable `DOCENTE MARTINEZ`, producto `LICUADORA`, retorno `2025-11-10`.

Correcciones obligatorias:

- Agregar responsables en hoja 9.
- Normalizar facultad.
- Completar retorno fila 6 o documentar excepcion.
- Si se quiere usar wizard, enlazar cada salida a un `ManagementPlan` con `MaintenanceId`.

### Solicitud De Adquisicion / Desembolso

Insertable actual:

- Hoja `6 · Detalles de Costo`, filas 8/9 generan `PUR_CUCHARA001_34179`.
- `Request.Type = Purchasing = 2`.
- Costos:
  - `O-Ring válvula gas cocina industrial`, monto 90.
  - `Mano de obra regulación y limpieza cocina INOX`, monto 350.

Correcciones obligatorias:

- Confirmar que esas filas son realmente adquisicion/desembolso.
- Mantener codigo limpio y unico.
- Si deben reflejarse en L-48, crear/enlazar `ManagementPlan.AcquisitionRequestId`.
- Si tambien son costos del Kardex, agregar costos equivalentes al mantenimiento correcto o ajustar el flujo de sincronizacion. No asumir doble pertenencia sin dato explicito.

### L-48 Plan De Mantenimiento

Insertables actuales:

- Fila 7: gestion `2023-2`, inventario `49236`, completado.
- Fila 8: gestion `2025-1`, inventario `17355`, pendiente.

Rechazadas actuales:

- Fila 6: falta inventario.
- Fila 9: falta gestion e inventario.

Correcciones obligatorias:

- Fila 6 columna B: completar inventario real.
- Fila 9 columna A: completar gestion `YYYY-S`.
- Fila 9 columna B: completar inventario real.
- Revisar laboratorio contra hoja 2.
- Revisar tecnico contra hoja 9.
- Revisar tipo de mantenimiento reconocido.

Resultado esperado: L-48 debe pasar de 2 a 4 planes insertables si ambas filas rechazadas quedan completas.

## Correcciones Del Importador / SQL Generado Para Que Encaje

Estas no son cambios manuales del Excel, sino reglas que debe cumplir la proxima generacion de SQL/importacion.

| Area | Cambio requerido | Prioridad |
|---|---|---|
| `ManagementPlans` | Insertar planes con enlaces reales: `RequestId`, `MaintenanceId`, `DepartureId`, `AcquisitionRequestId`, `VerificationId` cuando existan | Critica |
| L-3 | No crear salidas wizard aisladas; deben enlazar plan, mantenimiento, tecnico y unidad con laboratorio | Critica |
| L-12 | La adquisicion debe enlazarse por `ManagementPlan.AcquisitionRequestId` | Critica |
| Costos | `CostDetail` debe apuntar a `MaintenanceId` o `RequestId` segun destino. Si se refleja en ambos, documentar si se copia o si se cambia modelo | Critica |
| Estados wizard | Calcular `CurrentPhase`, `CurrentState` y `PlanStatus` desde los artefactos existentes | Critica |
| Unidades | No generar solicitudes, salidas, mantenimientos ni planes para inventarios sin laboratorio si la interfaz los requiere | Critica |
| Personas | No depender de autocreacion silenciosa desde L-3; hoja 9 debe ser la fuente limpia | Importante |

## Orden Recomendado De Trabajo

1. Corregir hoja `1 · Catálogos`: duplicado `3289`, fila basura y claves de equipo que deban ser catalogos separados.
2. Corregir hoja `2 · IInventario`: completar laboratorio para todo inventario usado por solicitudes, verificaciones, Kardex, L-3, costos y L-48.
3. Corregir hoja `9 · Técnicos y Proveedores`: agregar responsables y tecnicos faltantes con nombres exactos.
4. Corregir hoja `3 · Verificaciones`: poner inventario real en columna A o excluir filas no atribuibles a una unidad.
5. Corregir hoja `4 · Solicitudes`: limpiar inventarios, completar prioridad si se desea calidad historica y mejorar descripciones genericas.
6. Corregir hoja `5 · Kardex Mantenimiento`: completar mantenimientos que dan origen a costos, salidas o adquisiciones.
7. Corregir hoja `6 · Detalles de Costo`: separar costos de mantenimiento vs adquisicion y asegurar concepto/cantidad/unidad/precio.
8. Corregir hoja `7 · Salidas (L-3)`: normalizar facultad, responsables, retorno y decidir standalone vs wizard.
9. Corregir hoja `8 · Plan de Mantenimiento`: completar gestion/inventario de filas rechazadas y alinear laboratorio/tecnico.
10. Regenerar auditoria, revisar `audit.md`, `issues.csv`, `rejects.csv` y `normalized-preview.csv`.
11. Revisar `load.sql` manualmente antes de ejecutar en SQL Server.

## Checklist De Comparacion Antes De Importar

- [ ] No existe inventario duplicado `3289`.
- [ ] No quedan filas utiles sin nombre o inventario en `1 · Catálogos`.
- [ ] Todo inventario usado por hojas 3, 4, 5, 6, 7 y 8 existe en hoja 1/2.
- [ ] Todo inventario usado por solicitudes, L-3, Kardex y L-48 tiene laboratorio valido.
- [ ] `49236` tiene el mismo laboratorio en inventario y L-48.
- [ ] Hoja 3 columna A contiene inventarios reales, no codigos de laboratorio/area.
- [ ] Solicitudes con inventarios multiples estan separadas o tienen tokens validos.
- [ ] Prioridades de solicitudes estan completadas o se acepta explicitamente `Medium=1`.
- [ ] Descripciones `NINGUNO`, `FACTORES` u otras genericas fueron reemplazadas por informacion real.
- [ ] Hoja 9 contiene `ING. SARA PEREZ YAÑEZ` y `DOCENTE MARTINEZ`.
- [ ] L-3 fila 6 tiene retorno estimado o excepcion documentada.
- [ ] L-3 tiene decidido si cada salida es standalone o parte del wizard.
- [ ] Si una salida es del wizard, existe `ManagementPlan` + `Maintenance` + tecnico + laboratorio.
- [ ] Hoja 8 fila 6 tiene inventario real.
- [ ] Hoja 8 fila 9 tiene gestion `YYYY-S` e inventario real.
- [ ] Costos de Kardex tienen destino `Maintenance`.
- [ ] Costos de adquisicion/desembolso tienen destino `Request.Type = Purchasing`.
- [ ] Si costos deben reflejarse en L-48, el plan enlaza `MaintenanceId` y/o `AcquisitionRequestId`.
- [ ] La proxima auditoria reduce rechazos de 1053 y explica cualquier rechazo restante.
- [ ] El SQL generado no inserta verificaciones huerfanas.
- [ ] El SQL generado no deja `ManagementPlan` suelto cuando hay artefactos historicos relacionados.
- [ ] La cadena de conexion final usa `Server=localhost` o `Data Source=.` y no LocalDB/SQLEXPRESS.

## Criterio De Exito

La importacion se considera lista solo cuando:

- Los datos base entran sin duplicados ni referencias rotas.
- Los modulos L-3, Kardex/L-48 y L-12 se pueden reconstruir desde `ManagementPlan`.
- `CostDetail` queda enlazado al destino correcto y visible desde Kardex/adquisicion.
- Las salidas que pertenecen al wizard respetan la logica de la interfaz.
- Los rechazos restantes son decisiones documentadas, no errores accidentales.
