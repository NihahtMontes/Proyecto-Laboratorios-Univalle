# Guia De Entrega Y Recepcion De La Planilla Historica

Esta guia define el procedimiento oficial para solicitar a la cliente la
validacion de datos historicos sin exponerle las hojas tecnicas del importador
ni permitir que se pierda la evidencia original.

## 1. Archivos Y Responsabilidades

- **Archivo interno:** `Plantilla_Historicos_Normalizada_v2.xlsx`.
  Conserva la migracion completa, el mapa de procedencia, las incidencias y el
  contrato del futuro importador. No se entrega para edicion.
- **Archivo para la cliente, Fase 1:**
  `Plantilla_Levantamiento_Cliente_Fase1_v2.xlsx`. Contiene exclusivamente
  maestros y bienes fisicos: laboratorios, unidades, catalogos y actores.
- **Archivo para la cliente, Fase 2:**
  `Plantilla_Levantamiento_Cliente_Fase2_Historicos_v2.xlsx`. Se entrega
  despues de la orientacion del equipo tecnico y contiene solo los bloqueos
  historicos y decisiones institucionales que no pueden inferirse.
- **Archivo respondido, Fase 1:**
  `Plantilla_Levantamiento_Cliente_Fase1_v2_RESPONDIDA_yyyy-mm-dd.xlsx`.
- **Archivo respondido, Fase 2:**
  `Plantilla_Levantamiento_Cliente_Fase2_Historicos_v2_RESPONDIDA_yyyy-mm-dd.xlsx`.

La cliente no debe reemplazar ningun archivo recibido: debe guardar una copia
con la convencion correspondiente. La Fase 2 complementa la Fase 1; no la
reemplaza ni autoriza por si sola una carga a la base de datos.

## 2. Prioridades Y Urgencia

| Prioridad | Tratamiento | Datos | Condicion De Cierre |
|---|---|---|---|
| **P0 - Critica** | Resolver antes de cualquier carga | Numero de inventario, laboratorio fisico, catalogo asociado y referencias inexistentes | Cero claves invalidas y toda unidad identificable queda confirmada o marcada expresamente como pendiente |
| **P1 - Alta** | Resolver en la primera ronda | Categoria y clasificacion de los 107 catalogos, carrera responsable, ubicacion interna, estado operativo, condicion y actores responsables | Cada respuesta tiene estado y evidencia u observacion; `Otro` no se usa como sinonimo de `Por confirmar` |
| **P2 - Programada** | Resolver en rondas posteriores | Verificaciones sin inventario, posibles duplicados historicos y movimientos L-3 no estructurados | Cada ambiguedad queda vinculada con evidencia o permanece en cuarentena |

Una fila sin evidencia no se completa por aproximacion. Debe mantenerse vacia
y marcarse `Pendiente`.

La Fase 1 concentra 14 laboratorios, 556 unidades, 107 catalogos y 26 actores.
Cerrar sus controles no significa que toda la historia ya pueda cargarse.

La Fase 2 contiene 60 respuestas administrativas organizadas en 5 gestiones,
35 verificaciones, 2 solicitudes, 4 costos, 2 salidas, 5 politicas y 7 grupos
de mantenimiento. Dentro de ese universo se conservan las 45 incidencias P0
tecnicas del paquete canonico. Una misma fila puede responder mas de un campo
faltante; por ello el numero de respuestas de la planilla no debe confundirse
con el conteo de incidencias del auditor.

La auditoria tecnica del 28-08-2026 encontro ademas 528 fechas de verificacion
que difieren entre la celda fuente y la carga SQL anterior, y 144 mantenimientos
con diferencias en fecha programada. Las 528 verificaciones no se trasladan a
la cliente en Fase 1 porque el original conserva una fecha concreta y deben
tratarse primero como defecto de migracion. Las 144 diferencias de mantenimiento
se agrupan por regla fecha/gestion; solo las decisiones que no puedan resolverse
con la evidencia original se incluiran en Fase 2. No se pedira confirmar 672
filas una por una si una regla documentada y aprobada puede resolver el grupo.

## 3. Instrucciones Para La Cliente

1. Abrir el archivo en Microsoft Excel de escritorio.
2. Leer completa la hoja `00_LEA_PRIMERO`.
3. Completar unicamente las celdas amarillas.
4. Usar las listas desplegables; no escribir variantes nuevas cuando exista
   una opcion controlada.
5. No renombrar hojas, columnas, codigos, catalogos ni numeros de inventario.
6. No eliminar filas. Usar `No corresponde`, `Duplicado` o `Pendiente` y
   explicar el motivo.
7. Registrar cualquier correccion de inventario en la columna de correccion;
   el valor original debe conservarse.
8. No colocar la fecha actual cuando la fecha real sea desconocida.
9. Escribir en `ReferenciaEvidencia` el acta, etiqueta, fotografia, responsable
   o documento utilizado para confirmar el dato.
10. En `03_CATALOGOS_POR_CONFIRMAR`, clasificar cada catalogo como `Equipo`,
    `Utensilio` u `Otro`. Si no existe evidencia, seleccionar
    `Pendiente por evidencia`; nunca usar `Otro` para ocultar una duda.
11. Revisar `05_CONTROL_ENTREGA`, completar nombre del responsable y fecha, y
    guardar una copia con el nombre de respuesta establecido.

Para la Fase 2 se aplican ademas estas instrucciones:

1. Completar las hojas en el orden indicado por `00_LEA_PRIMERO`.
2. En `01_GESTIONES`, confirmar tipo y estado institucional de cada periodo.
3. En `02_VERIFICACIONES_P0`, registrar fecha y gestion solo cuando exista
   evidencia; si no existe, seleccionar `Pendiente por evidencia` y explicar.
4. En `03_SOLICITUDES_P0`, completar la solicitud o marcarla expresamente como
   `No corresponde`, sin inventar problema, fecha o estado.
5. En `04_COSTOS_P0`, asignar exactamente un padre —solicitud o
   mantenimiento— o declarar la fila en cuarentena.
6. En `05_SALIDAS_P0`, confirmar cantidad, unidad, descripcion y estado; no
   deducirlos por el nombre del equipo.
7. Las hojas `06_POLITICAS_HIST` y `07_GRUPOS_MANT` deben ser respondidas por
   una autoridad institucional e incluir responsable y evidencia.
8. No revisar una por una las 528 fechas de verificacion transpuestas: son un
   defecto tecnico ya identificado y se corregiran primero en QA.
9. Revisar `08_CONTROL_ENTREGA`; la devolucion administrativa exige cero
   respuestas vacias, aunque los casos `Pendiente por evidencia` sigan
   bloqueando la carga tecnica.

Para devolver la planilla debe existir una respuesta en cada fila P0. Elegir
`Pendiente por evidencia` y explicar el motivo cuenta como respuesta de la
cliente, pero no resuelve el dato: esa fila sigue bloqueando la carga tecnica
hasta una ronda o decision posterior.

## 4. Mensaje Sugerido Para La Entrega

> Estimada/o responsable: adjuntamos la primera fase de validacion del
> inventario historico de laboratorios. Por favor lea la hoja
> `00_LEA_PRIMERO` y complete solamente las celdas amarillas. No elimine filas
> ni modifique codigos, encabezados u hojas. Si un dato no puede confirmarse,
> seleccione `Pendiente` y explique el motivo; no lo deduzca. Antes de devolver
> el archivo, revise la hoja `05_CONTROL_ENTREGA` y guardelo con el sufijo
> `_RESPONDIDA_yyyy-mm-dd`. Las dudas deben registrarse en Observaciones para
> conservar la trazabilidad.

Para la Fase 2 puede utilizarse este mensaje:

> Estimada/o responsable: adjuntamos la segunda fase de validacion historica.
> Esta planilla no reemplaza la Fase 1 y no actualiza automaticamente el
> sistema. Complete solamente las celdas amarillas y siga el orden de
> `00_LEA_PRIMERO`. Si no existe evidencia, seleccione `Pendiente por
> evidencia` y explique el motivo; no aproxime fechas, cantidades, costos,
> responsables ni relaciones. Las decisiones de politica deben indicar quien
> las aprueba y su respaldo. Antes de devolverla, revise
> `08_CONTROL_ENTREGA` y guardela con el sufijo `_RESPONDIDA_yyyy-mm-dd`.

## 5. Recepcion Y Congelamiento De Evidencia

Al recibir el archivo:

1. No editarlo directamente.
2. Calcular y registrar su SHA-256, nombre, fecha de recepcion y responsable.
3. Verificar que no cambiaron hojas, encabezados ni codigos protegidos.
4. Abrirlo en modo de validacion y generar un reporte de diferencias.
5. Clasificar cada respuesta como `Coincide`, `SoloExcel`, `SoloBase`,
   `Conflicto`, `InferidoLegado` o `PendienteCliente`.
6. Actualizar la V2 interna mediante una copia de trabajo; nunca sobrescribir
   el original ni la respuesta recibida.
7. Generar la siguiente ronda solo con las incidencias que puedan resolverse
   gracias a la informacion ya confirmada.

## 6. Condiciones Para Pasar A Base De Datos

La respuesta de la cliente no se carga directamente. Antes debe cumplirse:

- contrato y hash verificados;
- cero inventarios duplicados;
- cero catalogos o referencias obligatorias inexistentes;
- cero incidencias P0 abiertas; una P0 en cuarentena conserva la evidencia,
  pero sigue bloqueando el lote;
- conciliacion Excel-base revisada;
- SQL incremental generado sin inferencias ni eliminaciones automaticas;
- ejecucion satisfactoria en la base QA;
- `DBCC CHECKCONSTRAINTS` y `DBCC CHECKDB` correctos;
- respaldo completo restaurado de prueba;
- autorizacion explicita antes de ejecutar sobre la base oficial.

## 7. Prohibiciones

- No ejecutar migraciones o SQL desde la planilla.
- No cargar datos directamente en la base oficial.
- No sustituir valores existentes sin conciliacion.
- No crear laboratorios, personas, costos o eventos para llenar vacios.
- No eliminar registros historicos; las discrepancias se corrigen, se
  desvinculan de forma segura o se mantienen en cuarentena.
