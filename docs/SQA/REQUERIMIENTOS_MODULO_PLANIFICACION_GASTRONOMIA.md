# Informe de requerimientos para SQA — Planificación y operación de laboratorios (piloto Gastronomía)

| Identificación | Valor |
|---|---|
| Destinatario | **Mateo Valeriano**, estudiante de **6to semestre**, materia **SQA** |
| Propósito | Proporcionar requerimientos funcionales y operativos para planificar y ejecutar pruebas de calidad del avance del Sistema de Gestión de Laboratorios — piloto Gastronomía. Mateo es destinatario de pruebas, **no** autor del proyecto. |
| Versión del informe | 1.0 — 28 de septiembre de 2026 |
| Fuente ASP congelada | `origin/reference/asp-final` = `dccabf50330afc48760d06bd4dbaff8c37ebbd3f` (`git rev-parse origin/reference/asp-final`) |
| Naturaleza | Línea base de implementación en ese commit + necesidades comunicadas por el responsable; **no** especificación aprobada de esquema Excel ni promesa de funcionalidad existente. |

## 1. Objetivo, alcance y método de evidencia

Definir qué debe probarse ahora y qué solo puede convertirse en caso de aceptación cuando se implemente. El alcance incluye las ocho rutas de `Pages/LaboratoryOperations/`, el Economato como proceso solicitado, y las integraciones comprobables con salidas L-3, reportes, catálogos, gestiones y notificaciones existentes. No se analiza como producto terminado una futura implementación de React ni cambios locales ASP sin commit. Cada referencia `ruta:archivo:símbolo` de este documento remite **exclusivamente al árbol del SHA de portada**; puede reproducirse con `git show dccabf50330afc48760d06bd4dbaff8c37ebbd3f:<ruta>` y `git grep <patrón> dccabf50330afc48760d06bd4dbaff8c37ebbd3f -- <ruta>`.

**Leyenda de evidencia y estado:** **CONFIRMADO EN ASP** significa comportamiento identificado en código de ese snapshot, no prueba manual autenticada. **REQUERIMIENTO DE NEGOCIO** proviene del responsable y no debe presentarse como entrega existente. **PARCIAL** significa que hay interfaz o piezas independientes pero no se cumple el resultado de negocio completo. **NO VERIFICADO** significa que no fue posible comprobarlo en la ejecución o falta una definición de negocio. `IMPLEMENTADO` se aplica únicamente al comportamiento delimitado explícitamente (p. ej., navegación visual estática), **no** a la funcionalidad integral de operación. `PENDIENTE` implica que no se encontró implementación del resultado en este snapshot.

### 1.1 Contexto y estado real

**CONFIRMADO EN ASP:** Hay ocho Razor Pages accesibles por enlaces del sidebar: `/LaboratoryOperations/Index`, `/Planificacion`, `/Requerimientos`, `/Disponibilidad`, `/L3`, `/L5`, `/Demanda` y `/Calendario` (todas con prefijo `/LaboratoryOperations`; el último enlace se rotula **«Calendarios»** en `Pages/Shared/_Sidebar.cshtml`). Cada PageModel correspondiente tiene solo `OnGet()` vacío, sin `OnPost`, inyección de servicios ni conexión con `ApplicationDbContext` (`Pages/LaboratoryOperations/*.cshtml.cs`). Las vistas contienen opciones, filas, identificadores y totales literales; sus filtros y cálculos son del navegador. **Los ejemplos visuales no son registros históricos comprobados ni inventario vivo.** La frase de una vista sobre datos de Excel/L-3 no equivale a una importación o consulta real.

**CONFIRMADO EN ASP, pero separado:** Existen entidades `Departure`, `DepartureItem`, `Article`, `EquipmentUnit`, `Management` y pantallas operativas en `Pages/Departures/`; hay reportes institucionales L-3, L-6, L-7, L-8/Kardex, L-48 y adquisición. Ninguna de estas implementaciones alimenta automáticamente las ocho páginas de Operación en el SHA. `Pages/Prototype/UtensilPlanning.cshtml` es otro prototipo que se declara demostrativo; no es prueba de persistencia del módulo.

**REQUERIMIENTO DE NEGOCIO:** La usuaria principal es la **Coordinadora de Laboratorios** de Gastronomía. La prioridad operativa es **importar un Excel al comenzar la gestión → revisar por semana → corregir/editar → preparar y contrastar recursos → registrar entrega, devolución e incidencias → consultar histórico y reportes**. El mapeo de este actor a roles técnicos todavía requiere acuerdo: en el código hay `Supervisor`, `Administrator`, `SuperAdmin`, no un rol específico `Coordinadora` (`Helpers/AuthorizationHelper.cs`).

## 2. Actores y límites de responsabilidad

| Actor | Responsabilidad esperada | Evidencia/limitación |
|---|---|---|
| Coordinadora de Laboratorios | Cargar, revisar y ajustar planificación; anticipar demanda y decidir sobre conflictos. | **REQUERIMIENTO DE NEGOCIO**; no hay política propia por rol en `Pages/LaboratoryOperations/*.cshtml.cs`. |
| Economato / personal de entrega | Entregar recursos y registrar cantidades reales. | **REQUERIMIENTO DE NEGOCIO**; no se encontró ruta/rol Economato integrado al módulo. |
| Receptor del préstamo (docente, grupo o responsable) | Recibir, usar y devolver recursos; asociar incidencias a responsables verificables. | **REQUERIMIENTO DE NEGOCIO**; `Departure.BorrowerId` existe en el flujo L-3 real, pero las pantallas nuevas muestran personas/grupos literales. |
| Administrador/supervisor | Gestión y reportes según políticas existentes. | **CONFIRMADO EN ASP** en `Pages/Departures/*` y `Controllers/ReportsController.cs`; el módulo visual solo exige usuario autenticado por `Program.cs`. |
| Mateo (SQA) | Diseñar y ejecutar pruebas y comunicar hallazgos. | Destinatario de este informe; no es actor transaccional ni autor. |

## 3. Flujo general comprobado frente al flujo objetivo

**Flujo visual CONFIRMADO EN ASP:** `Resumen → Planificación (paso 1) → Requerimientos (paso 2) → Disponibilidad (paso 3) → L-3 Devolución (paso 4) → L-5 Incidentes (enlace condicional desde diferencias)`; `Demanda` y `Calendario` son enlaces laterales independientes. `Pages/LaboratoryOperations/_StepsBar.cshtml` rotula **paso 5 «Cerrado»**, mientras el sidebar llama al quinto enlace **«Incidentes L-5»**: se debe aclarar si es etapa, excepción o cierre. La barra solo recibe números literales 1–5. No es el wizard institucional L-6→L-7→L-8→L-3→Kardex→L-12 cuyo `Completados` visual se identifica como paso 7; **son procesos diferentes**, no se debe equiparar sus pasos.

**Resultado actualmente verificable:** desde Planificación el botón «Definir» es un enlace, no guarda datos; Requerimientos mantiene ítems solo en un array JS; Disponibilidad muestra otra lista fija; L-3 calcula diferencias solo en JS y «Confirmar» muestra un aviso de guardado futuro antes de navegar; L-5 anuncia un borrador/PDF futuro sin crearlo. **No hay transacción de extremo a extremo, identidad de sesión persistente ni paso de datos entre pantallas.** El identificador ilustrativo `L3-2026-0095` es literal (`Planificacion.cshtml`, `Requerimientos.cshtml`, `Disponibilidad.cshtml`, `L3.cshtml`, `L5.cshtml`). El flujo esperado de negocio es: `Excel o alta manual → revisión/corrección semanal → necesidades → disponibilidad real y conflictos → autorización/entrega Economato → devolución real → incidencia si procede → historial/reportes`; sus tramos transaccionales están **PENDIENTES**.

## 4. Requerimientos funcionales verificables

En todos los requisitos, «entradas» son campos o datos necesarios para probar el resultado, **no** una propuesta de columnas Excel aprobadas. El criterio expresa una observación falsable para una futura aceptación; si el estado es PENDIENTE, SQA no debe registrar un fallo funcional de producto terminado sin acordar primero su incorporación al alcance de una entrega.

### RF-PLAN — planificación

**RF-PLAN-001 — Consultar planificación de un período y sus prácticas.** Actor: Coordinadora. **Precondiciones:** autenticación y período con prácticas registradas. **Flujo:** abrir Resumen/Planificación, seleccionar período o semana y consultar grupo, materia/práctica, fecha, horario, ambiente y estado. **Entradas:** gestión/período y filtro temporal. **Resultado:** registros reales del período y totales coherentes, sin mezclar gestiones. **Reglas:** el período/semana académica debe tener definición institucional; no usar IDs de demostración como datos reales. **Prioridad:** Alta. **Estado:** PARCIAL — solo navegación/cards/filtro JS con datos literales. **Evidencia:** `/LaboratoryOperations/Index` → `Pages/LaboratoryOperations/Index.cshtml:aplicarFiltro`, `Index.cshtml.cs:OnGet`; `/Planificacion` → `Planificacion.cshtml`. **Aceptación:** tras crear/importar una práctica, reaparece al recargar y filtrarla; cifras y filas coinciden con la fuente persistida; otro período no la incluye.

**RF-PLAN-002 — Alta manual de una práctica.** Actor: Coordinadora. **Precondiciones:** catálogos y gestión válidos. **Flujo:** completar sede/carrera, laboratorio, docente, materia, grupo, fecha, horas, número de práctica y observaciones; guardar. **Entradas:** los campos de la pantalla, incluidos horarios. **Resultado:** práctica identificable y recuperable. **Reglas:** validar obligatorios, relación con catálogos y rango horario en servidor; reglas de colisión por confirmar. **Prioridad:** Alta. **Estado:** PARCIAL — campos visuales y `required` HTML, sin POST. **Evidencia:** `/LaboratoryOperations/Planificacion`, `Planificacion.cshtml:form-planificacion`, `Planificacion.cshtml.cs:OnGet`. **Aceptación:** una alta válida persiste y se consulta tras reinicio; falta de campo o fin anterior al inicio produce error sin guardar.

**RF-PLAN-003 — Revisar y corregir planificación importada o creada.** Actor: Coordinadora. **Precondiciones:** práctica previamente registrada y permisos definidos. **Flujo:** localizar, abrir detalle, corregir campos/recursos y guardar. **Entradas:** identificador estable, cambios y motivo si la política de auditoría lo exige. **Resultado:** versión vigente visible sin perder trazabilidad del cambio. **Reglas:** no duplicar la práctica por editar; política de edición tras entrega pendiente de decisión. **Prioridad:** Alta. **Estado:** PENDIENTE — no se encontraron detalle, Edit ni OnPost en el módulo. **Evidencia:** `Pages/LaboratoryOperations/*.cshtml.cs` y enlaces del sidebar. **Aceptación:** un cambio válido reaparece tras recarga y en calendario/demanda; edición inválida no altera el registro ni el histórico.

**RF-PLAN-004 — Organizar recursos por semana académica.** Actor: Coordinadora. **Precondiciones:** gestión y prácticas con fecha/horario definidos. **Flujo:** seleccionar semana y consultar recurso, cantidad, fecha/hora de salida, grupo/práctica, laboratorio y retorno previsto. **Entradas:** semana, recurso y práctica. **Resultado:** agenda consolidada y exportable/consultable sin omitir simultaneidades. **Reglas:** definición de semana, zona horaria, unidad de medida y retorno previsto aún por acordar. **Prioridad:** Alta. **Estado:** PENDIENTE — no hay agregación semanal en el módulo. **Evidencia:** `/LaboratoryOperations/Planificacion`, `/Demanda`, `/Calendario` (datos literales). **Aceptación:** al añadir dos prácticas de la misma semana, ambas aparecen y sus cantidades se pueden reconciliar con el detalle; otra semana no las muestra.

### RF-IMP — importación Excel de planificación

**RF-IMP-001 — Importar planificación al inicio de gestión.** Actor: Coordinadora. **Precondiciones:** gestión destino verificada, archivo autorizado y plantilla/mapeo aprobados. **Flujo:** cargar Excel, validar estructura y contenido, mostrar previsualización/conteos y confirmar carga. **Entradas:** archivo y gestión destino; **campos de negocio a mapear**: semana, grupo, materia/práctica si aplica, fecha, horario, laboratorio, recurso/tipo, cantidad, insumo/material y retorno cuando el origen lo permita. **Resultado:** prácticas y necesidades vinculadas correctamente a la gestión sin alta manual repetitiva. **Reglas:** no inventar hojas/columnas: plantilla, obligatoriedad, claves y conversiones están **NO VERIFICADAS**. **Prioridad:** Alta. **Estado:** PENDIENTE. **Evidencia:** `/LaboratoryOperations/Planificacion` no ofrece archivo/handler; `Tools/HistoricalDataImport/` procesa un libro histórico **distinto**, no esta planificación web. **Aceptación:** con archivo de contrato aprobado y registros válidos, preview y conteos coinciden con lo persistido y aparecen en planificación; no se cargan registros al cancelar.

**RF-IMP-002 — Rechazar y explicar Excel inválido.** Actor: Coordinadora. **Precondiciones:** archivo preparado según contrato aprobado. **Flujo:** subir libro con hoja/columna obligatoria ausente o valor inválido; revisar errores por ubicación; corregir y reintentar. **Entradas:** Excel incorrecto. **Resultado:** diagnóstico específico y sin cargas parciales no autorizadas. **Reglas:** formato admitido, máximo de tamaño y estrategia transaccional requieren decisión. **Prioridad:** Alta. **Estado:** PENDIENTE. **Evidencia:** sin `IFormFile` ni parser en `Pages/LaboratoryOperations/`. **Aceptación:** error identifica hoja/fila/campo según contrato y deja intactos datos previos; el archivo corregido se importa.

**RF-IMP-003 — Controlar duplicados y reimportaciones.** Actor: Coordinadora. **Precondiciones:** gestión destino y práctica ya existente o repetida dentro del libro. **Flujo:** detectar coincidencias antes de confirmar, informar crear/actualizar/omitir y registrar procedencia. **Entradas:** libro repetido o filas duplicadas. **Resultado:** no aparecen prácticas duplicadas accidentalmente. **Reglas:** clave de coincidencia e idempotencia por archivo/fila se definirán con Datos antes de implementar; no inferirlas solo por fecha o nombre. **Prioridad:** Alta. **Estado:** PENDIENTE. **Evidencia:** sin importador de planificación; `Models/ImportBatch.cs` sirve a importación histórica, no vincula esta pantalla. **Aceptación:** cargar dos veces el mismo archivo bajo política aprobada deja conteos y vínculos correctos; los conflictos son trazables.

### RF-REQ — necesidades de recursos

**RF-REQ-001 — Registrar recursos y cantidades por práctica.** Actor: Coordinadora. **Precondiciones:** práctica identificada y catálogo disponible. **Flujo:** seleccionar laboratorio/recurso, indicar cantidad y añadir o retirar ítems antes de confirmar. **Entradas:** práctica, identificador de recurso, cantidad y unidad aplicable. **Resultado:** detalle persistido, asociado a esa práctica; no a otra. **Reglas:** cantidad positiva, tipo/unidad coherentes y posible distinción entre consumible y prestable. **Prioridad:** Alta. **Estado:** PARCIAL — `agregarUtensilio`, `quitarItem`, `renderLista` actúan sobre `items[]` local; listas y stock son literales. **Evidencia:** `/LaboratoryOperations/Requerimientos` → `Requerimientos.cshtml:agregarUtensilio/renderLista`, `Requerimientos.cshtml.cs:OnGet`. **Aceptación:** ítems añadidos conservan identidad/cantidad tras navegar y recargar; entradas nulas o cantidad inválida no pasan al siguiente paso.

**RF-REQ-002 — Consultar necesidades desde catálogos gobernados.** Actor: Coordinadora. **Precondiciones:** catálogo, unidades y ambientes activos. **Flujo:** filtrar por laboratorio/tipo y elegir recurso existente. **Entradas:** ambiente y categoría. **Resultado:** solo artículos/unidades aplicables y sus unidades de medida; sin nombres escritos libremente como claves. **Reglas:** clasificación de utensilios/insumos y cardinalidad catálogo-unidad se acuerdan con Datos. **Prioridad:** Alta. **Estado:** PARCIAL — selects HTML con `data-stock`, pero no consulta a `Article`/`EquipmentUnit`. **Evidencia:** `Requerimientos.cshtml:sel-lab/sel-utensilio`, `Models/Article.cs`, `Models/EquipmentUnit.cs`; `filtrarUtensilios` es invocada en vista pero no se encontró definición en el módulo. **Aceptación:** cambiar laboratorio no lanza error JS y modifica opciones según catálogo real; recurso inexistente es rechazado por servidor.

### RF-DISP — disponibilidad y conflictos

**RF-DISP-001 — Comprobar existencias frente a necesidades.** Actor: Coordinadora. **Precondiciones:** necesidades persistidas e inventario confiable. **Flujo:** abrir disponibilidad para una práctica/semana; comparar requerido con disponible por recurso. **Entradas:** gestión, rango, práctica, recurso y cantidades. **Resultado:** saldo, suficiente/insuficiente y origen del stock comprobables. **Reglas:** disponibilidad física no es igual necesariamente a stock nominal; reservas, unidades fuera de servicio y retornos deben definirse. **Prioridad:** Alta. **Estado:** PARCIAL — Razor calcula `Disponible - Solicitado` sobre cinco filas fijas; no usa lo añadido en Requerimientos. **Evidencia:** `/LaboratoryOperations/Disponibilidad`, `Disponibilidad.cshtml:itemsDisp` y clase `table-danger`, `Disponibilidad.cshtml.cs:OnGet`. **Aceptación:** cambios en necesidades o stock alteran el saldo tras recarga; jamás se declara «suficiente» una petición que supera la disponibilidad calculada.

**RF-DISP-002 — Detectar solapamientos y demanda superior a stock.** Actor: Coordinadora. **Precondiciones:** fechas, franjas de entrega/retorno y cantidades de varias prácticas. **Flujo:** evaluar actividad simultánea o cercanía de horarios sobre el mismo recurso. **Entradas:** actividades, tiempos y cantidades por recurso. **Resultado:** conflictos identifican recurso, prácticas competidoras, intervalo y déficit. **Reglas:** límites de solapamiento y tiempo de preparación por recurso **NO VERIFICADOS**. **Prioridad:** Alta. **Estado:** PENDIENTE — el aviso en Resumen es texto literal; el Paso 3 solo detecta una fila de ejemplo marcada en rojo. **Evidencia:** `Index.cshtml` sección alertas; `Disponibilidad.cshtml:itemsDisp`. **Aceptación:** dos prácticas concurrentes cuya suma supera stock generan conflicto reproducible; al separar horarios o aumentar stock, desaparece según regla aprobada.

**RF-DISP-003 — Señalar cercanía al límite y períodos críticos.** Actor: Coordinadora. **Precondiciones:** umbral por recurso/tipo y calendario acordados. **Flujo:** comparar disponibilidad proyectada con umbral para semana/día. **Entradas:** recursos, cupos, reservas y umbrales configurados. **Resultado:** nivel de riesgo con causa y fechas. **Reglas:** no existe porcentaje/umbral técnico aprobado en el snapshot; evitar imponer uno arbitrario. **Prioridad:** Alta. **Estado:** PENDIENTE. **Evidencia:** sin configuración de umbrales en `Program.cs`, `appsettings.json`, `Data/ApplicationDbContext.cs` ni servicios de operación. **Aceptación:** valores debajo/sobre el umbral configurado cambian el estado de modo determinista; se prueba el valor frontera acordado.

**RF-DISP-004 — Identificar reutilización consecutiva de alta rotación.** Actor: Coordinadora/Economato. **Precondiciones:** horarios de devolución prevista y siguiente entrega por recurso. **Flujo:** ordenar usos sucesivos, medir tiempo entre retorno y siguiente salida y mostrar prácticas afectadas. **Entradas:** recurso, actividades, cantidades y fechas/horas. **Resultado:** intervalo y riesgo operativo explícitos, sin confundir simultaneidad con rotación. **Reglas:** ventana mínima y tiempo de preparación por tipo pendientes de aprobación. **Prioridad:** Alta. **Estado:** PENDIENTE. **Evidencia:** `Calendario.cshtml:todosLosEventos` es estático; no hay algoritmo de intervalos en el módulo. **Aceptación:** dos actividades consecutivas con margen insuficiente aparecen como riesgo con intervalo calculado; margen suficiente no produce falso positivo.

### RF-ENT — entrega desde Economato

**RF-ENT-001 — Registrar salida real y contrastarla con lo planificado.** Actor: personal autorizado de Economato. **Precondiciones:** planificación confirmada, recursos disponibles y receptor identificado. **Flujo:** buscar práctica, ver cantidades previstas, registrar qué/cánto se entregó, responsable y fecha/hora. **Entradas:** práctica, ítem, cantidad real y receptor. **Resultado:** diferencia previsto/entregado e identificación inequívoca del movimiento. **Reglas:** no sustituir el movimiento L-3 real por un texto en otra pantalla; aprobación de entregas excedentes pendiente. **Prioridad:** Alta. **Estado:** PENDIENTE en este módulo; **PARCIAL en el sistema** porque existe `Departure`/`DepartureItem` sin FK comprobada hacia práctica. **Evidencia:** `Models/Departure.cs`, `Models/DepartureItem.cs`, `Pages/Departures/{Create,MassCreate}.cshtml.cs`; `/LaboratoryOperations/L3` es un mock separado. **Aceptación:** con previsto 3 y entrega 3 se registra concordancia; con 2 o 4 queda diferencia visible y autorizable según regla acordada; registro sobrevive recarga.

### RF-DEV — devolución

**RF-DEV-001 — Registrar devolución completa o parcial por entrega.** Actor: Economato/responsable autorizado. **Precondiciones:** entrega real con ítems y receptor. **Flujo:** localizar salida, ingresar cantidad devuelta y momento real, comprobar diferencia y confirmar. **Entradas:** salida, ítems, cantidades devueltas, condición y fecha/hora. **Resultado:** estado trazable, faltantes claros y saldo pendiente; impedir devolución superior al entregado. **Reglas:** unidad de medida y devoluciones parciales requieren definición; `DepartureItem.ReturnedQuantity` existe en el sistema L-3 real. **Prioridad:** Alta. **Estado:** PARCIAL en el sistema; el Paso 4 visual solo calcula `devuelto - entregado` en JS y simula confirmar. **Evidencia:** `/LaboratoryOperations/L3` → `L3.cshtml:prestamos/calcularDiff/confirmarDevolucion`; `Models/DepartureItem.cs:ReturnedQuantity`, `Pages/Departures/*`. **Aceptación:** 3/3 se registra completo; 2/3 queda saldo 1 y relación con la salida; cantidades fuera de rango son rechazadas en servidor.

**RF-DEV-002 — Evitar cierre silencioso con diferencias.** Actor: Economato/Coordinadora. **Precondiciones:** devolución con diferencia. **Flujo:** confirmar saldo y decidir incidente/cierre bajo regla aprobada. **Entradas:** detalle de diferencia y responsable. **Resultado:** no se muestra éxito de devolución completa si existe faltante; se habilita trazabilidad de incidencia. **Reglas:** política de cierre y obligatoriedad de incidencia no definida. **Prioridad:** Alta. **Estado:** PARCIAL — JS muestra panel «Ítems con diferencia» y enlace a L-5, pero `confirmarDevolucion()` permite aviso de éxito sin guardar ni registrar incidencia. **Evidencia:** `Pages/LaboratoryOperations/L3.cshtml:calcularDiff/confirmarDevolucion`. **Aceptación:** una devolución incompleta no queda «completa» ni «cerrada» sin resolución autorizada y evidencia persistida.

### RF-INC — incidencias L-5

**RF-INC-001 — Crear incidencia vinculada a uso/devolución.** Actor: personal autorizado / Coordinadora. **Precondiciones:** práctica y, si corresponde, salida/devolución identificadas. **Flujo:** registrar fecha, tipo, afectado, cantidad, responsable, descripción y relación con movimiento; guardar borrador o enviar. **Entradas:** los datos disponibles en la pantalla L-5 (incluye materia, docente, estudiante/grupo, tipo daño/faltante/rotura/pérdida, estado, utensilio, costo y observaciones). **Resultado:** identificador real e incidencia recuperable vinculada a hechos. **Reglas:** catálogo de tipos y evidencia de daño o pérdida por definir; no atribuir culpa automáticamente. **Prioridad:** Alta. **Estado:** PARCIAL visual / PENDIENTE transaccional — `generarBorrador()` solo muestra `alert` de futuro guardado/PDF; código L5 literal. **Evidencia:** `/LaboratoryOperations/L5`, `L5.cshtml:form-l5/generarBorrador`, `L5.cshtml.cs:OnGet`. **Aceptación:** guardar no duplica, devuelve ID único y enlaza a entrega/práctica; datos obligatorios ausentes no se persisten.

**RF-INC-002 — Seguimiento y resolución de incidencia.** Actor: Coordinadora/responsable asignado. **Precondiciones:** incidencia existente. **Flujo:** consultar responsable, novedades/acciones, estado, resolución y consecuencias. **Entradas:** incidencia, acción y responsable. **Resultado:** historial de estados/auditoría y cierre consistente con devolución/stock. **Reglas:** transiciones y responsabilidad/costo requieren aprobación; opciones «Pendiente», «En revisión», «Resuelto» son **solo etiquetas HTML**. **Prioridad:** Alta. **Estado:** PENDIENTE — no se encontró entidad ni handler de persistencia L-5; lista de «incidentes recientes» está escrita en la vista. **Evidencia:** `L5.cshtml` selects/lista, `L5.cshtml.cs:OnGet`, `Data/ApplicationDbContext.cs` DbSets. **Aceptación:** cambios autorizados se recuperan por ID y conservan actor/fecha; no se resuelve un faltante sin cumplir la política aprobada.

### RF-ALT — alertas

**RF-ALT-001 — Emitir alertas de riesgo basadas en datos.** Actor: Coordinadora. **Precondiciones:** prácticas, inventario y reglas de evaluación. **Flujo:** calcular y consultar alertas de límite, déficit, rotación, solapamiento, devolución problemática e incidencia relevante. **Entradas:** recursos y actividades del período. **Resultado:** alerta accionable con causa, fecha, recurso, severidad y enlace al hecho; se actualiza al cambiarlo. **Reglas:** severidades/umbrales no definidos. **Prioridad:** Alta. **Estado:** PARCIAL visual — Resumen contiene mensajes estáticos; `Models/Notification.cs` y `Pages/Api/Notifications.cshtml.cs` funcionan en otros dominios sin conexión comprobada con este módulo. **Evidencia:** `/LaboratoryOperations/Index` alertas, `Pages/Api/Notifications.cshtml.cs:OnGetUnreadAsync`. **Aceptación:** un conflicto real crea alerta y su resolución la actualiza; nunca aparecen alertas de demostración atribuidas a datos reales.

**RF-ALT-002 — Configurar umbrales y alcance de alertas.** Actor: Coordinadora/administración con permiso. **Precondiciones:** política aprobada. **Flujo:** definir ámbito de recurso/laboratorio, umbral y destinatarios; guardar configuración. **Entradas:** regla, valor/unidad y alcance. **Resultado:** alertas reproducibles, configuración auditada. **Reglas:** destinatarios, permisos y umbrales son decisiones pendientes. **Prioridad:** Media. **Estado:** PENDIENTE. **Evidencia:** sin configurador en `Pages/LaboratoryOperations/`, servicio de operación o DbSet específico en `ApplicationDbContext`. **Aceptación:** cambiar un umbral altera solo alertas del ámbito esperado; usuarios sin permiso no pueden cambiarlo.

### RF-DEM — demanda

**RF-DEM-001 — Analizar demanda real por recurso y período.** Actor: Coordinadora. **Precondiciones:** prácticas/requerimientos y/o salidas reales con gestión identificada. **Flujo:** elegir gestión, semana, recurso y orden; ver cantidad requerida, frecuencia, prácticas participantes, picos y déficit. **Entradas:** filtros y movimientos. **Resultado:** agregaciones reproducibles, sin sumar periodos ajenos ni confundir cantidad requerida con número de salidas. **Reglas:** definir si «demanda» es prevista, entregada o ambas; unidades homogéneas. **Prioridad:** Alta. **Estado:** PARCIAL — 12 filas literales con filtro/orden JS; los campos «Desde/Hasta» no intervienen en `filtrarDemanda()`. **Evidencia:** `/LaboratoryOperations/Demanda` → `Demanda.cshtml:filtrarDemanda`, `Demanda.cshtml.cs:OnGet`. **Aceptación:** cambiar fechas/semana cambia las agregaciones reales y una práctica adicional impacta exactamente sus recursos; suma de filas concuerda con indicadores según fórmula aprobada.

### RF-CAL — calendario

**RF-CAL-001 — Navegar calendario y filtrar eventos visuales.** Actor: usuario autenticado. **Precondiciones:** scripts FullCalendar accesibles. **Flujo:** elegir mes/semana/día/lista, laboratorio y abrir evento. **Entradas:** vista y laboratorio. **Resultado:** se muestran/ocultan eventos existentes en el navegador y aparece su ficha simple. **Reglas:** colores demo no representan estados persistidos. **Prioridad:** Media. **Estado:** IMPLEMENTADO **solo para doce eventos ficticios**; no implica calendario de producción. **Evidencia:** `/LaboratoryOperations/Calendario`, `Calendario.cshtml:todosLosEventos/cambiarVista/aplicarFiltroCalendario/eventClick`; FullCalendar 5.11.3 servido desde CDN. **Aceptación (estado actual):** cuatro controles de vista responden y filtrar SAL limita eventos del array; sin CDN disponible queda no verificado.

**RF-CAL-002 — Mostrar planificación temporal real y detalles.** Actor: Coordinadora. **Precondiciones:** prácticas/gestiones persistidas. **Flujo:** seleccionar gestión/rango/laboratorio; consultar horarios, materia, grupo, estado y abrir registro asociado. **Entradas:** rango, práctica y laboratorio. **Resultado:** eventos corresponden exactamente a prácticas reales y reflejan cambios, devoluciones o mantenimientos relevantes según contrato. **Reglas:** solapamiento de ambientes y colores/estados por definir. **Prioridad:** Alta. **Estado:** PENDIENTE — doce eventos JS generados con fechas relativas a `new Date()`; clic muestra `alert`, no abre expediente. **Evidencia:** `Calendario.cshtml:offset/todosLosEventos/eventClick`. **Aceptación:** modificar una fecha actualiza el evento después de recargar; filtros y enlaces preservan ID/período; no se muestran eventos ficticios como reales.

### RF-REP — reportes

**RF-REP-001 — Emitir reportes de planificación y operación.** Actor: Coordinadora/gestión autorizada. **Precondiciones:** datos persistidos y selección de gestión/rango. **Flujo:** elegir tipo (planificación semanal, recursos, demanda, disponibilidad, entregas, devoluciones, incidencias, cumplimiento, críticos) y exportar. **Entradas:** tipo de reporte, fechas, laboratorio y filtros. **Resultado:** archivo legible cuyos totales y detalle corresponden al mismo conjunto de datos visible; sin mezclar gestiones. **Reglas:** formatos, filtros, indicadores y definición de «cumplimiento» pendientes. **Prioridad:** Media. **Estado:** PENDIENTE para reportes de este módulo; **existen** reportes institucionales independientes. **Evidencia:** `Planificacion.cshtml` botón «Exportar Excel» con mensaje «disponible próximamente»; `Controllers/ReportsController.cs:DownloadL3/DownloadL6/DownloadL8/DownloadL48/DownloadL7/DownloadAdquisicion`, `Services/Reports/ReportService.L3.cs`. **Aceptación:** el reporte solicitado se descarga con gestión/filtros y reconciliación contra registros; no presentar L-3 existente como reporte de planificación.

### RF-HIS — históricos

**RF-HIS-001 — Consultar gestiones anteriores y tendencias.** Actor: Coordinadora. **Precondiciones:** al menos dos períodos con datos de operación. **Flujo:** elegir gestión histórica, revisar prácticas, recursos, entregas/devoluciones, incidentes y comparar demanda. **Entradas:** período(s), recurso/laboratorio. **Resultado:** histórico íntegro y comparación trazable sin reescribir registros cerrados. **Reglas:** política de cierre, retención y rectificación por aprobar. **Prioridad:** Media. **Estado:** PARCIAL en el sistema — `Management` y salidas con fecha/fuente histórica existen; **PENDIENTE** para prácticas, incidencias y tendencias del módulo. **Evidencia:** `Models/Management.cs`, `Models/Departure.cs:HistoricalSourceKey/ImportBatchId`, `/LaboratoryOperations/Demanda` totales fijos. **Aceptación:** seleccionar períodos diferentes cambia registros reales y su comparativa; el período cerrado no se altera por cargar el nuevo.

## 5. Requerimientos no funcionales sustentados

| ID | Requisito y fundamento | Evidencia actual / criterio SQA | Estado |
|---|---|---|---|
| RNF-PLAN-001 | **Seguridad y autorización.** Acceso autenticado y operaciones de importación/edición/entrega/incidencias restringidas al rol aprobado, no solo por ocultar botones. | `Program.cs:AuthorizeFolder("/")` y fallback autenticado; `Pages/Departures/*` emplea roles; en `LaboratoryOperations` no hay políticas de rol. Probar anónimo, usuario autenticado sin rol y autorizado; mutaciones requieren permiso servidor. | PARCIAL |
| RNF-PLAN-002 | **Integridad y trazabilidad.** Mantener vínculo período→práctica→ítem→entrega→devolución→incidencia, auditar actor/fecha/origen y evitar borrado destructivo de históricos. | `Management`, `Departure`, `DepartureItem`, `ImportBatch` dan patrones independientes; no existe vínculo completo con estas vistas. Aceptar cuando cada salto conserva IDs y cambios pueden reconstruirse. | PARCIAL |
| RNF-PLAN-003 | **Errores e importación segura.** Validar en servidor, explicar fallas de Excel por ubicación y evitar carga parcial/duplicado bajo política aprobada. | Solo `required` HTML en formularios demo; no handler Excel. Probar campos inválidos, archivo defectuoso y reintento sin corrupción. | PENDIENTE |
| RNF-PLAN-004 | **Usabilidad operativa.** Revisar por semana, detectar conflictos antes de práctica y diferenciar claramente datos demo, pendientes y guardados reales. | Hay tarjetas, tabla, cinco pasos y calendario visual, pero acciones simulan éxito. Probar claridad de estado y persistencia tras recarga; textos de guardado futuro no equivalen a éxito. | PARCIAL |
| RNF-PLAN-005 | **Rendimiento medible cuando exista datos reales.** Consultas semanales/reportes deben responder razonablemente al volumen de la gestión sin bloquear la operación; no hay meta numérica aprobada para este módulo. | Actualmente son listas locales; registrar volumen de prueba, tiempos y entorno antes de aceptar un objetivo de rendimiento. | NO VERIFICADO |
| RNF-PLAN-006 | **Compatibilidad y resiliencia visual.** La vista calendario depende del CDN de FullCalendar 5.11.3; la importación futura debe documentar formato Excel válido en vez de asumir cualquier libro. | `Calendario.cshtml` carga CDN; no hay importador ni contrato de columnas. Probar red/CDN disponible e indisponible y formato aprobado cuando exista. | PARCIAL |

## 6. Reglas de negocio y contratos pendientes de decisión

| Clave | Regla comprobada o decisión requerida |
|---|---|
| RN-01 | **CONFIRMADO EN ASP visual:** en Disponibilidad `diferencia = disponible - solicitado`; fila negativa se marca en rojo. No usa stock real ni reserva temporal. |
| RN-02 | **CONFIRMADO EN ASP visual:** Requerimientos marca `OK` si cantidad añadida ≤ `data-stock` del `<option>`; los ítems se pierden al abandonar la página. |
| RN-03 | **CONFIRMADO EN ASP visual:** L-3 muestra diferencia si devuelto < entregado; no actualiza `DepartureItem.ReturnedQuantity`. La validación HTML `max` no sustituye una regla servidor. |
| RN-04 | **NO VERIFICADO / decidir:** primera/última fecha y numeración de semanas académicas, feriados, horarios, huso horario, franjas de preparación y retorno. |
| RN-05 | **NO VERIFICADO / decidir:** stock disponible = existencia física − reservas simultáneas − no disponibles + retornos confirmados; umbral «al límite» y tratamiento de consumibles. |
| RN-06 | **NO VERIFICADO / decidir:** tiempo mínimo de preparación/limpieza para reutilización consecutiva; aprobar responsables, excepción y cierre con faltante. |
| RN-07 | **NO VERIFICADO / decidir:** identificador natural/técnico de práctica, duplicado de importación, política de actualización, plantilla y hojas/columnas Excel, errores parciales. |
| RN-08 | **NO VERIFICADO / decidir:** roles de Coordinadora/Economato, asignación de docente/grupo/receptor, incidencia obligatoria y transiciones/efectos de L-5. |
| RN-09 | **CONFIRMADO EN SISTEMA SEPARADO:** `DepartureItem.Quantity` y `ReturnedQuantity` representan cantidad salida y devuelta; `Models/Departure.cs` incluye gestión, fechas, `OriginLaboratoryId` y `BorrowerId` (persona receptora). No se ha demostrado relación con una práctica académica. |

## 7. Integraciones y dependencias comprobadas

| Componente | Existe en snapshot | Relación con las ocho páginas |
|---|---|---|
| Equipos, unidades, utensilios y artículos | `Models/Equipment.cs`, `EquipmentUnit.cs`, `Article.cs`; `Pages/AssetView/`, `Pages/Equipment/` | **No conectados** a Requerimientos/Disponibilidad/Demanda; categorías/stock de la vista son literales. |
| Ambientes, carreras y personas | `Models/Laboratory.cs`, `Career.cs`, `Person.cs` | Selectores de Planificación son literales; vínculo servidor **pendiente**. |
| Gestión académica y wizard institucional | `Models/Management.cs`, `ManagementPlan.cs`, `Pages/Managements/` | No existe clave de gestión/práctica en el wizard visual de Operación; sus pasos no equivalen al wizard institucional. |
| L-6 verificaciones, L-7 solicitudes, L-8 mantenimiento | `Pages/Verifications/`, `Requests/`, `Maintenances/` | Funcionales en otros flujos; relación automática con práctica/alerta **no comprobada**. |
| L-3 salida real | `Pages/Departures/{Create,MassCreate,Details,Index}`, `Models/Departure.cs`, `DepartureItem.cs` | Existe CRUD/salida institucional separado; `/LaboratoryOperations/L3` **no** lee/escribe esas entidades. |
| L-5 incidencias | `/LaboratoryOperations/L5` | Solo formulario y aviso cliente; sin entidad/handler de incidencia persistida localizado. |
| Adquisiciones | `Pages/Acquisitions/`, reporte adquisición | Propuesta de compra en Demanda es control visual, sin enlace transaccional comprobado. |
| Notificaciones | `Models/Notification.cs`, `Pages/Api/Notifications.cshtml.cs` | Otros ámbitos (mantenimiento/adquisición/salidas); alertas de Operación son literales, sin vinculación comprobada. |
| Calendarios y reportes | `Calendario.cshtml`, `Controllers/ReportsController.cs`, `Services/Reports/` | Calendario de ejemplo y reportes institucionales reales son superficies independientes; no hay reporte de prácticas de Gastronomía. |
| Importación histórica | `Tools/HistoricalDataImport/` y `Models/ImportBatch.cs` | Contrato distinto de la futura importación de Excel de planificación; **no** reutilizar ni afirmar compatibilidad sin mapeo aprobado. |

## 8. Estados, interfaz y restricciones para SQA

- Resumen: botones hoy/semana/mes/todos modifican valores y visibilidad de filas **solo JS** (`Index.cshtml:aplicarFiltro`); alertas no son notificaciones generadas.
- Planificación: hay selectores de sede/carrera/lab/docente/materia/grupo, fecha/hora, categoría, número de práctica y observaciones; «Definir» salta al Paso 2 sin validar/guardar; «Exportar Excel» avisa que estará disponible después.
- Requerimientos: selección y cantidad agregan/retiran filas locales; cambiar laboratorio invoca `filtrarUtensilios` sin definición encontrada en el módulo (riesgo de error JS). La lista no atraviesa el enlace al Paso 3.
- Disponibilidad: una fila de ejemplo con déficit determina si se presenta panel de conflicto o aprobación; no recalcula a partir de Paso 2.
- Devolución L-3: tabla ejemplo, input de cantidad devuelta y panel de diferencias; `confirmarDevolucion()` utiliza `confirm()` nativo, espera y muestra `alert` de guardado **futuro**; no se debe reportar una devolución real exitosa.
- Incidentes L-5: select de tipo daño/faltante/rotura/pérdida y estado pendiente/en revisión/resuelto, campos de cantidad/costo/observaciones; el supuesto código «auto-generado» y lista reciente son literales; no hay transición real.
- Demanda: filtro por categoría y orden por número de salidas solo sobre doce filas; las fechas Desde/Hasta no afectan `filtrarDemanda()`. La suma de las doce cifras `data-salidas` es **264** frente al total «229» mostrado en la tarjeta: inconsistencia interna del ejemplo, no dato de producción.
- Calendario: mes/semana/día/lista y filtro de laboratorio operan sobre un array estático de eventos desplazados respecto a la fecha actual; colores de ejemplo confirmado/pendiente/mantenimiento/examen, `eventClick` muestra un `alert`, no ficha persistida. Biblioteca desde CDN: comprobarla en la prueba visual.

## 9. Matriz de trazabilidad SQA

| ID | Requerimiento | Módulo/página | Prioridad | Estado actual | Tipo de prueba sugerida |
|---|---|---|---|---|---|
| RF-PLAN-001 | Consultar planificación | Resumen/Planificación | Alta | PARCIAL | funcional, datos, filtros |
| RF-PLAN-002 | Alta manual | Planificación | Alta | PARCIAL | validación, funcional |
| RF-PLAN-003 | Editar tras importar | Planificación | Alta | PENDIENTE | funcional, regresión, trazabilidad |
| RF-PLAN-004 | Agenda semanal | Planificación/Calendario | Alta | PENDIENTE | integración, datos |
| RF-IMP-001 | Importación Excel | Planificación | Alta | PENDIENTE | importación, integración |
| RF-IMP-002 | Rechazo de Excel inválido | Planificación | Alta | PENDIENTE | validación, error handling |
| RF-IMP-003 | Duplicados/reimportación | Planificación | Alta | PENDIENTE | importación, datos |
| RF-REQ-001 | Ítems/cantidades por práctica | Requerimientos | Alta | PARCIAL | funcional, persistencia |
| RF-REQ-002 | Catálogos vivos | Requerimientos | Alta | PARCIAL | integración, interfaz |
| RF-DISP-001 | Existencia vs necesidad | Disponibilidad | Alta | PARCIAL | datos, frontera |
| RF-DISP-002 | Solapamiento/déficit | Disponibilidad/Resumen | Alta | PENDIENTE | integración, concurrencia temporal |
| RF-DISP-003 | Cerca del límite | Disponibilidad/Alertas | Alta | PENDIENTE | frontera, configuración |
| RF-DISP-004 | Reutilización consecutiva | Disponibilidad/Calendario | Alta | PENDIENTE | intervalos, end-to-end |
| RF-ENT-001 | Entrega vs planificado | Economato/L-3 | Alta | PENDIENTE (módulo) | integración, datos, permisos |
| RF-DEV-001 | Devolución | L-3 | Alta | PARCIAL (sistema) | validación, integración |
| RF-DEV-002 | Diferencias y cierre | L-3/L-5 | Alta | PARCIAL | flujo end-to-end, regresión |
| RF-INC-001 | Crear L-5 vinculada | L-5 | Alta | PARCIAL visual | funcional, permisos, integración |
| RF-INC-002 | Seguimiento/resolución | L-5 | Alta | PENDIENTE | estados, auditoría |
| RF-ALT-001 | Alertas basadas en hechos | Resumen/Notificaciones | Alta | PARCIAL visual | integración, datos |
| RF-ALT-002 | Configurar umbrales | Alertas | Media | PENDIENTE | configuración, permisos |
| RF-DEM-001 | Analítica por semana | Demanda | Alta | PARCIAL visual | datos, filtros, regresión |
| RF-CAL-001 | Vista y filtros demo | Calendario | Media | IMPLEMENTADO (demo) | interfaz, dependencia CDN |
| RF-CAL-002 | Eventos reales y detalle | Calendario | Alta | PENDIENTE | integración, interfaz |
| RF-REP-001 | Reportes del módulo | Reportes/Planificación | Media | PENDIENTE (módulo) | reporte, permisos, datos |
| RF-HIS-001 | Historial/tendencias | Gestiones/Operación | Media | PARCIAL (sistema) | datos, histórico |
| RNF-PLAN-001 | Seguridad por rol | Operación y L-3 | Alta | PARCIAL | permisos |
| RNF-PLAN-002 | Integridad/trazabilidad | Flujo completo | Alta | PARCIAL | datos, end-to-end |
| RNF-PLAN-003 | Errores de importación | Planificación | Alta | PENDIENTE | error handling |
| RNF-PLAN-004 | Claridad del estado | Operación | Alta | PARCIAL | interfaz, usabilidad |
| RNF-PLAN-005 | Rendimiento con datos | Demanda/Calendario | Media | NO VERIFICADO | rendimiento |
| RNF-PLAN-006 | Compatibilidad Excel/CDN | Importación/Calendario | Media | PARCIAL | compatibilidad |

## 10. Escenarios críticos y prioridades de prueba

| Escenarios del encargo | Situación en el SHA; acción de Mateo |
|---|---|
| 1 Excel válido; 2 columnas faltantes; 3 duplicados; 4 editar importado | **Requeridos, PENDIENTES.** No existe interfaz de carga; diseñar datos esperados y esperar contrato Excel/entrega antes de ejecutar aceptación. |
| 5 crear manualmente | **PARCIAL visual.** Probar campos y verificar que hoy no persiste, sin marcar alta real como aprobada. |
| 6 suficiente; 7 insuficiente; 8 dos prácticas compiten | La vista simula 6/7 mediante cinco filas; 8 es texto demo. **Pendiente** probar stock/reservas y solapes reales. |
| 9 reutilización consecutiva | **PENDIENTE.** Preparar dos actividades con retorno y nueva entrega próximos; ventana a decidir. |
| 10 entrega igual; 11 distinta | `DepartureItem.Quantity` real soporta salida, pero no está ligado a previsto de la práctica; **pendiente** integración y contraste. |
| 12 devolución completa; 13 incompleta | Diferencia local JS visible; registrar/recuperar devolución en `/LaboratoryOperations/L3` **pendiente**. Contrastar aparte con L-3 institucional real. |
| 14 dañado; 15 perdido; 16 generar incidencia | Opciones L-5 de ejemplo; **pendiente** ID real, responsabilidad y persistencia. |
| 17 cierre | Paso 5 dice «Cerrado» pero enlaza L-5; **pendiente** semántica y efecto de cierre. |
| 18 histórico; 19 alertas; 20 reportes | Gestiones/reporte L-3/notificaciones existen en otros módulos; **pendiente** su integración con planificación; no confundir datos ejemplo con cumplimiento. |

**Prioridad P1 de prueba cuando haya backend:** importación y duplicados, aislamiento de gestión, persistencia/edición, suma de necesidades frente a stock, doble reserva, entrega vs previsto, devolución parcial y creación/trazabilidad de L-5. **Riesgos detectables ya en el prototipo:** navegación que descarta información, falso éxito visual, filtro de laboratorio con función JS no localizada, rango de fechas de Demanda ignorado, resumen numérico inconsistente, ruta L-5 confundida con estado cerrado y falta de restricción de rol del módulo. **Seguridad:** no utilizar una conexión de producción ni exponer credenciales al probar; comprobar el destino antes de cualquier prueba con BD.

## 11. Funcionalidades existentes vs pendientes y evidencia técnica

| Superficie | Confirmado en snapshot | Aún no demostrado |
|---|---|---|
| Ocho rutas + sidebar | Enlaces y vistas; PageModels `OnGet()` vacíos | CRUD, permisos por actor, datos persistidos |
| Filtros/estados de pantalla | JS local: Resumen, Demanda, Calendario, Requerimientos, L-3 | Filtros por gestión/semana sobre DB, estados autorizados |
| Excel | EPPlus/reporte L-3 en sistema separado; exportación de esta pantalla anuncia futuro | Importación de planificación, plantilla y exportación de planificación |
| Datos e histórico institucional | Gestiones, artículos/unidades, salidas, reportes y notificaciones generales | Prácticas, reservas, vínculo plan→entrega→devolución→incidencia, tendencias de operación |
| Alertas/rotación | Tarjetas y textos de ejemplo | Motor, configuración, umbrales e intervalos |

**Índice reproducible de evidencia principal (SHA de portada):**

1. `Pages/Shared/_Sidebar.cshtml` — ocho enlaces; `Pages/LaboratoryOperations/_StepsBar.cshtml` — pasos y etiquetas.
2. `Pages/LaboratoryOperations/{Index,Planificacion,Requerimientos,Disponibilidad,L3,L5,Demanda,Calendario}.cshtml.cs` — ocho `OnGet()` vacíos, sin POST.
3. `Pages/LaboratoryOperations/Index.cshtml:aplicarFiltro`; `Planificacion.cshtml:form-planificacion`; `Requerimientos.cshtml:agregarUtensilio`; `Disponibilidad.cshtml:itemsDisp`; `L3.cshtml:calcularDiff/confirmarDevolucion`; `L5.cshtml:generarBorrador`; `Demanda.cshtml:filtrarDemanda`; `Calendario.cshtml:todosLosEventos/aplicarFiltroCalendario`.
4. `Program.cs:AuthorizeFolder/FallbackPolicy`; `Helpers/AuthorizationHelper.cs:AdminRoles/ManagementRoles`; `Data/ApplicationDbContext.cs` DbSets.
5. `Models/{Management,Departure,DepartureItem,Article,EquipmentUnit,Notification}.cs`; `Pages/Departures/{Create,MassCreate}.cshtml.cs`; `Pages/Api/Notifications.cshtml.cs`; `Controllers/ReportsController.cs`; `Services/Reports/ReportService.L3.cs`.
6. `Tools/HistoricalDataImport/` y `Models/ImportBatch.cs` — importador histórico **no equivalente** a importación web de planificación.

## 12. Validación ejecutada, observaciones y no verificados

- Se resolvió `git rev-parse origin/reference/asp-final` y se investigaron **objetos del commit**, sin incorporar cambios locales sin commit como hechos del producto.
- Se compiló el proyecto web **desde un worktree detached del SHA**, mediante `dotnet build "Proyecto Laboratorios Univalle.csproj" -m:1 --nologo`: **0 errores, 0 advertencias**. Esta evidencia de compilación **no** demuestra operación funcional ni persistencia.
- Se inició temporalmente el ASP aislado con `AutoMigrate=false`, `RunSeed=false` y conexión de desarrollo dirigida explícitamente a una base local `_QA`; `GET /Login` devolvió **HTTP 200**. El servidor se detuvo y se verificó que el puerto quedó libre. **No se autenticó usuario, no se probó el módulo en navegador ni se consultó/escribió la BD.**
- No se ejecutaron pruebas de importación, reportes, roles ni end-to-end con datos reales: aún no existen handlers de importación/operación integrados y no se dispone de un contrato Excel aprobado. La fidelidad visual de FullCalendar, roles reales de Coordinadora/Economato, rendimiento y política temporal permanecen **NO VERIFICADOS**.
- Antes de programar pruebas transaccionales, acordar con responsable y Datos: plantilla Excel, clave de práctica/duplicado, unidad de medida, stock proyectado, definición de semana, umbrales/rotación, estados L-5, responsabilidades y enlaces al L-3 institucional. QA debe registrar entorno, precondiciones, esperado, obtenido, evidencia y severidad; nunca convertir el ejemplo estático en baseline de base de datos.

## 13. Texto breve para enviar a Mateo por WhatsApp

Hola, Mateo. Te comparto los requerimientos del Sistema de Gestión de Laboratorios para el piloto de Gastronomía. El módulo que te corresponde revisar es **Operación de Laboratorios / Planificación**: busca organizar prácticas, recursos, horarios y laboratorios, anticipar conflictos y conectar la entrega de Economato con devoluciones e incidencias.

El flujo esperado es **importar o crear la planificación → revisar y editar por semana → definir recursos → comprobar disponibilidad → registrar entrega y devolución → documentar incidencias → consultar demanda, calendario e históricos**. El informe adjunto distingue lo que ya se ve en el ASP de lo que todavía es un requerimiento: varias pantallas actuales son demostrativas y no guardan datos. Incluye requisitos con criterios de aceptación, una matriz para preparar tus pruebas y los riesgos principales.
