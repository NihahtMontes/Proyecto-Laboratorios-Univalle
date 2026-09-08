# Plan Operativo De Normalizacion Historica V2

Este documento convierte el rediseño del Excel historico en un proceso
controlado de datos. Su objetivo es que la planilla, el importador y el modelo
EF Core compartan el mismo contrato sin reemplazar ni completar por inferencia
la evidencia existente.

## 1. Resultado Esperado

El flujo V2 separa las responsabilidades que antes estaban mezcladas:

1. `Equipment` representa el catalogo reutilizable de tipos o modelos.
2. `EquipmentUnit` representa cada bien fisico identificado por inventario.
3. Las relaciones N:M vinculan solicitudes con unidades y mantenimientos con
   solicitudes sin duplicar sus cabeceras.
4. `Article` representa consumibles de L-3 que no son bienes inventariados.
5. Las filas de origen, conflictos y decisiones se conservan como evidencia de
   importacion y hallazgos de calidad.

La implantacion es aditiva y compatible: las columnas legadas necesarias para
las pantallas actuales se conservan durante la transicion. Su retiro requiere
una fase posterior, cuando todos los consumidores utilicen las relaciones V2.

## 2. Flujo Autorizado

```text
Excel original inmutable
        |
        v
V2 tecnica + trazabilidad ----- Planilla guiada para la cliente
        |                                  |
        |                           Respuesta congelada + hash
        |                                  |
        +----------- Conciliacion <--------+
                         |
                 Paquete canonico validado
                         |
                 SQL incremental en QA
                         |
             Revision y autorizacion explicita
                         |
                     Base oficial
```

La planilla respondida nunca se ejecuta directamente. Primero se valida su
contrato, se compara con la evidencia y se produce un delta revisable.

## 3. Prioridad Y Urgencia

| Nivel | Urgencia | Bloquea la carga | Ejemplos | Tratamiento |
|---|---|---:|---|---|
| **P0 - Critica** | Inmediata | Si | Inventario duplicado, catalogo inexistente, unidad sin identidad, FK invalida, detalle L-3 con dos o ninguna referencia | Corregir con evidencia; si queda en cuarentena, conserva la evidencia pero el lote sigue bloqueado |
| **P1 - Alta** | Primera ronda | Condicional | Categoria y clasificacion de catalogo, laboratorio, responsable patrimonial, condicion, estado operativo, actores | Resolver o justificar expresamente como pendiente; `Otro` no reemplaza a `Por confirmar` y no se infiere. Si SQL no puede representar el pendiente, se convierte en bloqueo tecnico de carga |
| **P2 - Programada** | Rondas siguientes | No, si esta aislada | Duplicidad historica posible, L-6 ambiguo, texto L-3 no estructurable | Conservar como registro independiente o hallazgo abierto |

Una incidencia P0 no puede cerrarse con una observacion generica. Debe tener
una correccion verificable. La cuarentena no la cierra ni habilita SQL.

## 4. Reglas De Integridad V2

- `CatalogCode`, `Career.Code` y `Person.ActorCode` son codigos externos
  estables; durante la transicion pueden quedar nulos para registros legados,
  pero no pueden duplicarse cuando tienen valor.
- `InventoryNumber` se trata siempre como texto y debe ser unico.
- Una unidad fisica debe referenciar un catalogo valido.
- La ubicacion desconocida se representa con laboratorio nulo y estado de
  resolucion `Pending`; nunca con un laboratorio ficticio.
- Una solicitud puede afectar varias unidades mediante
  `RequestEquipmentUnits`.
- Un mantenimiento puede estar vinculado con cero o varias solicitudes
  mediante `MaintenanceRequests`.
- La sugerencia historica de una solicitud se conserva en
  `Request.Suggestion` nullable (maximo 2.000 caracteres), separada de las
  observaciones generales y sin truncamiento silencioso.
- Los participantes de mantenimiento son la relacion operativa; el tecnico
  simple se conserva temporalmente por compatibilidad.
- Un costo puede corresponder a una solicitud o a un mantenimiento, pero no a
  ambos simultaneamente.
- Un detalle de salida referencia una unidad inventariada o un articulo, pero
  no ambos simultaneamente.
- Fecha, satisfaccion, tecnico, costo y ubicacion desconocidos permanecen
  nulos. No se usa la fecha actual ni se crean registros de relleno.
- Toda fila importada mantiene una clave determinista de origen para que una
  reejecucion no genere duplicados.
- La compatibilidad actual sincroniza desde los campos legacy hacia las tablas
  puente. Cualquier adaptador que escriba directamente relaciones V2 debe
  actualizar tambien la referencia legacy primaria mientras existan pantallas
  que la consumen; no hay sincronizacion inversa automatica.

## 5. Responsables

| Responsable | Obligacion |
|---|---|
| Cliente | Confirmar solo datos solicitados, conservar filas/codigos y adjuntar referencia de evidencia |
| Custodio de datos | Congelar la respuesta, calcular hash, validar estructura y clasificar diferencias |
| Equipo tecnico | Generar el paquete canonico, ejecutar preflight y producir SQL incremental auditable |
| Revisor funcional | Resolver conflictos P0 y aprobar decisiones o cuarentenas P1/P2 |
| Administrador autorizado | Respaldar, probar en QA y autorizar expresamente la promocion oficial |

Ninguna persona debe reunir por defecto las etapas de preparacion, revision y
aprobacion final del mismo lote.

## 6. Puertas De Calidad

### Puerta A - Recepcion

- archivo, fecha, responsable y SHA-256 registrados;
- hojas, encabezados y codigos protegidos sin cambios;
- formulas sin `#REF!`, `#VALUE!` o `#N/A`;
- todas las filas clasificadas como migradas, pendientes o duplicadas por
  confirmar.

### Puerta B - Paquete Canonico

- cero inventarios duplicados;
- cero referencias obligatorias inexistentes;
- exactamente una referencia valida en costos y detalles L-3;
- conteos reconciliados entre origen, migrados, pendientes y duplicados;
- cero incidencias P0 abiertas; toda P0 en cuarentena sigue bloqueando.
- todos los desconocidos son representables como `NULL` o estado explicito,
  sin reutilizar estados de negocio como `Pendiente`, `Otro` o `Activo`;
- manifiesto de reconciliacion entre claves historicas legacy y codigos V2,
  para evitar altas duplicadas.

### Puerta C - QA

- respaldo previo disponible y restaurado de prueba;
- migraciones revisadas, pero nunca aplicadas implicitamente por la app;
- carga ejecutada en una base QA mediante transaccion;
- segunda ejecucion sin duplicados;
- `DBCC CHECKCONSTRAINTS` y pruebas funcionales correctas.

### Puerta D - Base Oficial

- informe de diferencias aprobado;
- ventana y responsable de ejecucion definidos;
- autorizacion explicita registrada;
- plan de reversa probado;
- evidencia final y conteos posteriores archivados.

## 7. Secuencia De Trabajo Con La Cliente

1. Entregar `Plantilla_Levantamiento_Cliente_Fase1_v2.xlsx` junto con la
   seccion de instrucciones de
   `docs/GUIA_ENTREGA_PLANILLA_CLIENTE.md`.
2. Solicitar primero la resolucion P0 y P1 de laboratorios y unidades fisicas.
3. Recibir la copia con sufijo `_RESPONDIDA_yyyy-mm-dd` y congelarla.
4. Generar un reporte de diferencias; no modificar la respuesta recibida.
5. Enviar una segunda ronda solo para conflictos que sigan abiertos y que
   puedan resolverse con evidencia nueva.
6. Promover los datos confirmados al paquete tecnico V2.
7. Ejecutar validaciones y carga en QA.
8. Solicitar autorizacion separada antes de cualquier operacion sobre la base
   oficial.

## 8. Limites De Esta Entrega

- Las dos migraciones de Fase A alinean relaciones y codigos, pero no cubren
  todavia todas las diferencias semanticas de la carga V2 ni se ejecutan desde
  esta fase de datos.
- El importador genera y valida artefactos; no debe conectarse ni escribir por
  defecto en SQL Server. Su SQL actual es staging de gobierno y no promueve
  entidades de negocio.
- La pantalla de Migracion es informativa y no dispara cargas.
- El original y la V2 tecnica siguen siendo evidencia interna inmutable.
- Las pantallas existentes permanecen operativas mediante compatibilidad; la
  captura nativa de todas las relaciones N:M puede evolucionar en una fase de
  interfaz posterior.

## 9. Siguientes Pasos Priorizados

| Orden | Prioridad | Accion | Responsable | Condicion De Inicio |
|---:|---|---|---|---|
| 1 | **P0 - Critica** | Entregar y recibir la Fase 1; responder los 14 laboratorios y 556 unidades, y completar en la misma ronda los 107 catalogos y 26 actores P1. `Pendiente por evidencia` permite devolver, pero no habilita carga | Cliente + custodio de datos | Planilla y guia entregadas |
| 2 | **P0 - Critica** | Congelar respuesta, calcular hash y generar nueva conciliacion | Custodio de datos | Archivo respondido recibido |
| 3 | **P0 - Critica** | Revalidar los 45 P0 tecnicos actuales; resolver 41 campos obligatorios vacios y 4 costos sin un padre exclusivo confirmado, internamente o mediante una Fase 2 | Custodio + revisor funcional | Conciliacion de Fase 1 terminada |
| 4 | **P0 - Critica tecnica** | Ensayar y aprobar la migracion nueva `CompleteHistoricalWorkbookV2Model`: fecha de solicitud, retorno y detalle L-3 nullable, estado observado, roles exactos y L-48 en `MaintenancePlans` | Equipo de datos + QA | Implementada localmente; no aplicada |
| 5 | **P0 - Critica tecnica** | Generar y aprobar el manifiesto legacy→V2; nunca emparejar historicos solo por el nuevo codigo | Equipo de datos | Snapshot QA vigente |
| 6 | **P0 - Critica** | Implementar el adaptador transaccional que resuelve FK por codigos y carga entidades de negocio de forma idempotente | Equipo tecnico | Cero P0 y cero bloqueos semanticos en el paquete aprobado |
| 7 | **P1 - Alta** | Aplicar la migracion de datos y el lote unicamente en QA, restaurar respaldo y probar reejecucion | Administrador + revisor | Adaptador y SQL revisados |
| 8 | **P1 - Alta** | Habilitar captura nativa multiinventario/N:M en las pantallas que hoy usan compatibilidad legacy | Equipo funcional y tecnico | QA estable |
| 9 | **P2 - Programada** | Resolver cuarentenas L-6, duplicados posibles y texto L-3 ambiguo en rondas sucesivas | Cliente + custodio | Evidencia adicional disponible |

La base oficial no forma parte de los pasos 1 a 8. Su promocion requiere una
autorizacion nueva, explicita y posterior a la evidencia de QA.

## 10. Diagnostico Semantico Del 28-08-2026

`HistoricalDataApply plan` contrato 4 se ejecuto contra la copia local QA sin
escrituras. El resultado fue `CanApply=false`: 45 P0, 11 clases de bloqueo
semantico y 10 advertencias nullable. Los bloqueos de datos que siguen abiertos
son 107 catalogos, 556 unidades, cinco gestiones, 343 solicitudes, 320
mantenimientos y dos salidas con estados o clasificaciones no confirmados, mas
35 verificaciones sin fecha. Los desconocidos que ya tienen destino nullable
permanecen como advertencia y nunca se reemplazan por valores inventados.

La correccion de modelo local dirige las 387 filas de `19_PLAN_L48` a
`MaintenancePlans`. `ManagementPlans` continua reservado al wizard y conserva
su indice unico gestion-unidad. La migracion nueva
`20260828161956_CompleteHistoricalWorkbookV2Model` no forma parte del script de
Fase A, no se ha aplicado a QA ni a produccion y exige un ensayo/autorizacion
independiente. Su script idempotente local tiene SHA-256
`9482A00632F486D073529BA25366807E5DAB54CDB8FE2FD34ECE1F93F18127D9`.

Ademas, las nuevas claves V2 no coinciden directamente con las claves legacy.
El reconciliador de contenido contrato 4 genero 2.037 mapeos: 391 coincidencias
de contenido, 672 conflictos de negocio, 214 filas solo V2, 760 filas solo base,
76 solicitudes legacy explotadas y cero ambiguedades de clave. Los conflictos
incluyen 528 verificaciones cuya fecha fuente es distinta a la importada en SQL
y 144 mantenimientos con fecha programada distinta; tambien cubre por primera
vez costos, salidas y detalles L-3. La propuesta exige una decision revisada
por cada fila; se elimino la aceptacion global. Un `MERGE`
basado solo en la clave V2 generaria duplicados. Por ello, las dos migraciones
de Fase A siguen siendo validas para corregir el esquema inmediato de la
aplicacion, pero no autorizan ni completan la carga de datos V2.
