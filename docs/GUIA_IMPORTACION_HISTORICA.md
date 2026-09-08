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
