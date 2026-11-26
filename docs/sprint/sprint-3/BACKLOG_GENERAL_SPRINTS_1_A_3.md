# Backlog general del proyecto

**Proyecto:** Laboratorios Univalle — Gastronomía
**Alcance:** lo entregado en los Sprints 1, 2 y 3
**Fecha de corte:** 18/11/2026
**Equipo:** Fernando Muñoz Aranda · Danielo Romero Daza · Nihaht Poly Montes Peñarrieta

---

## 1. Resumen

| Bloque | Entregado | Detalle |
|---|---|---|
| Sprint 1 | 5 frentes de trabajo | 2 frentes se descartaron por decisión técnica |
| Sprint 2 | 41 historias | 6 épicas |
| Sprint 3 | 50 historias | 8 épicas |
| **Sprints 2 y 3** | **91 historias** | Lo que continúa está en la sección 6 |

Al cierre del 26/10, el Sprint 2 registraba 30 historias hechas. Hoy son 41 porque se terminaron varias, se precisó el alcance de otras según lo realmente entregado y se agregaron 3 entregas que no estaban registradas.

**Leyenda:**
- ✅ **Hecho:** la función existe y está probada. Lo del Sprint 3 se probó en la copia EXP y aún no está en la base real.
- ❌ **Descartado:** se decidió no continuar.

Los códigos se repiten entre sprints, por eso aquí llevan prefijo: **S2·BD-01** es la historia BD-01 del Sprint 2.

---

## 2. Antecedente

El backlog de producto inicial fue una propuesta de 20 historias y 130 puntos para el módulo de utensilios. Se replanificó durante el proyecto: los sprints reales priorizaron los datos, la migración y después el circuito docente.

---

## 3. Sprint 1 (hasta el 28/09/2026)

El Sprint 1 se organizó por frentes de trabajo, no por historias de usuario.

| ID | Frente de trabajo | Responsable según el informe | Estado | Resultado |
|---|---|---|---|---|
| S1-01 | Datos históricos: selección, duplicados, correcciones y referencias | Fernando Muñoz Aranda | ✅ | La normalización se reutilizó para cargar la base real del Sprint 2. |
| S1-02 | Base de datos en PostgreSQL: restricciones, relaciones e índices | Fernando / Nihaht | ❌ | PostgreSQL se descartó con la migración; los datos se cargaron en SQL Server. |
| S1-03 | Vida útil en el modelo y la interfaz | Fernando / Danielo | ✅ | 121/121 equipos con años de vida útil, visibles en la ficha de la unidad. |
| S1-04 | Prototipo de planificación y consulta de equipos | Danielo Romero Daza | ✅ | Evolucionó al módulo de Planificación del Sprint 2. |
| S1-05 | Migración a React + NestJS + PostgreSQL | Nihaht Montes | ❌ | Se descartó el 29/09 tras medir su costo; lo hecho queda en la rama `migration/react`. |
| S1-06 | Calidad de arquitectura: SOLID, patrones y evidencia | Sin asignación en el informe | ✅ | Los principios se demostraron después con clases del sistema vigente. |
| S1-07 | Tablero, evidencias y demostración | Sin asignación en el informe | ✅ | Informe ejecutivo del Sprint 1 presentado. |

---

## 4. Sprint 2 (29/09/2026 → 26/10/2026)

### E1 · Base de datos real: inventario, procesos e historial

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·BD-01 | Como **administradora**, quiero que el sistema tenga exactamente los datos de `BaseGastronomia_v1.xlsx`, para confiar en él desde el primer día. | ✅ | 27 hojas, 23.542 filas y 44.743 celdas comparadas: **0 diferencias**; repetido el 18/11 con el mismo resultado |
| S2·BD-02 | Como **responsable de los datos**, quiero saber de qué fila del Excel salió cada registro y qué problemas tenía, para rastrear y corregir. | ✅ | 23.542 filas de origen guardadas; hoy hay 7.459 observaciones de calidad registradas |
| S2·BD-03 | Como **administradora**, quiero el inventario completo, para gestionar equipos e insumos. | ✅ | 121 equipos, 644 unidades, 1.374 artículos, 702 existencias y 24 laboratorios |
| S2·BD-04 | Como **administradora**, quiero ver el **historial de mantenimiento** de gestiones pasadas. | ✅ | 27 gestiones históricas: 1.599 verificaciones (L-6), 329 solicitudes (L-7), 1.209 mantenimientos (L-8) y 387 planes (L-48) |
| S2·BD-05 | Como **administradora**, quiero ver las **solicitudes de material de 2026** como planificaciones. | ✅ | 1.037 planificaciones (559 + 478) y 11.451 líneas |
| S2·BD-06 | Como **administradora**, quiero el **catálogo académico** cargado. | ✅ | 41 materias, 196 prácticas, 2.111 líneas de receta, 869 horarios y 147 grupos |
| S2·BD-07 | Como **administradora**, quiero registrar los **años de vida útil** de cada equipo, para planificar su reposición. | ✅ | **121/121** equipos con años de vida útil; la ficha de la unidad los muestra |
| S2·BD-08 | Como **encargado de almacén**, quiero cargar existencias desde un Excel con **vista previa**, para revisar antes de guardar. | ✅ | Vista previa con las 700 filas de `BaseGastronomia_v1.xlsx`: 699 sin cambio, 1 por actualizar y 0 errores (EXP, 18/11) |
| S2·BD-10 | Como **administradora**, quiero las gestiones del semestre listas para operar. | ✅ | `2026-2` (preventiva) y `CORR-2026-2` (correctiva), activadas el 19/10 con respaldo previo |
| S2·BD-11 | Como **administradora**, quiero consultar lo importado en pantallas de solo lectura: unidades, planes L-48 y horarios de práctica. | ✅ | Commit `8d41c39`; pantallas revisadas en EXP el 18/11 |

### E2 · Planificación académica de laboratorios

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·PL-01 | Como **administradora**, quiero registrar prácticas con su **receta de insumos** y el kit de cada materia. | ✅ | Catálogo con 196 prácticas |
| S2·PL-02 | Como **administradora**, quiero crear grupos de estudiantes («A1» = grupo A de 1.er semestre) con su horario, para organizar cada cohorte. | ✅ | Pantallas de grupos y horario y pruebas de `StudentGroupService`; aún sin grupos registrados en la base |
| S2·PL-03 | Como **administradora**, quiero un calendario académico con semanas y días de clase. | ✅ | 41 semanas y 237 días |
| S2·PL-04 | Como **administradora**, quiero **planificar una práctica en 3 pasos**: datos → requerimientos → disponibilidad. | ✅ | Capturas W03, W05 y D01 |
| S2·PL-05 | Como **administradora**, quiero un **panel** con las prácticas de hoy, los conflictos de la semana y las devoluciones pendientes. | ✅ | Captura P02 |
| S2·PL-06 | Como **administradora**, quiero una **grilla semanal por laboratorio** para ver la ocupación. | ✅ | Captura P05 |
| S2·PL-07 | Como **administradora**, quiero ver los **conflictos de material** y una recomendación para resolverlos. | ✅ | Captura P07 |
| S2·PL-08 | Como **administradora**, quiero **reprogramar** una práctica arrastrándola y **reponer un día** perdido. | ✅ | Capturas E13 y E11 |
| S2·PL-09 | Como **administradora**, quiero ver la **demanda** de insumos por periodo, para comprar a tiempo. | ✅ | Corrección del 21/10 |
| S2·PL-10 | Como **administradora**, quiero que el sistema **bloquee el choque de docente**, y que si insisto quede constancia. | ✅ | Capturas final21 |
| S2·PL-11 | Como **administradora**, quiero una regla que impida planificar más estudiantes que la **capacidad** registrada del ambiente. | ✅ | Regla y pruebas listas; actúa cuando el ambiente tiene capacidad registrada |
| S2·PL-12 | Como **encargado de almacén**, quiero registrar la **entrega y la devolución** del material de cada práctica (L-3), con su vale en Excel. | ✅ | Circuito inicial de Danielo, más las correcciones de QA; en el Sprint 3 el material reutilizable pasó a calcularse por horario (S3·RS-05) |
| S2·PL-13 | Como **encargado de almacén**, quiero registrar **daños y pérdidas** (L-5) sin pasar lo pendiente. | ✅ | Captura final21_l5_tope |
| S2·PL-14 | Como **encargado de cierre**, quiero un módulo de **Cierre de práctica** que solo me muestre lo del día. | ✅ | Captura RC01 |
| S2·PL-15 | Como **administradora**, quiero resolver conflictos desde su detalle: actualizar stock, enlazar conflictos relacionados y reducir cantidades. | ✅ | Panel con reprogramar, reducir cantidad, materiales y stock; conflictos enlazados en Grilla y Disponibilidad; 15 pruebas y revisión en EXP el 18/11 |
| S2·PL-16 | Como **equipo**, queremos un recorrido automático de Planificación con la base actual. | ✅ | Recorrido de 46/46 sobre bandeja, grilla, reposiciones y carga Excel, agregado en noviembre |

### E3 · Mantenimiento de equipos

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·MT-01 | Como **administradora**, quiero una gestión **preventiva y una correctiva activas a la vez**. | ✅ | Migración aplicada en la base real |
| S2·MT-02 | Como **supervisor**, quiero reportar fallas (L-7) **solo desde el asistente**. | ✅ | 56/56 comprobaciones |
| S2·MT-03 | Como **administradora**, quiero **rehacer una fase** sin dejar datos inconsistentes. | ✅ | 59 + 59 comprobaciones |
| S2·MT-04 | Como **administradora**, quiero que los **6 informes Excel** coincidan con la base y con la plantilla institucional. | ✅ | 62/62 comprobaciones |
| S2·MT-05 | Como **administradora**, quiero que cada rol vea solo lo suyo y que la cuenta se bloquee tras 5 intentos. | ✅ | 241 comprobaciones de permisos |
| S2·MT-06 | Como **administradora**, quiero un **historial continuo de estados** de cada equipo, con buscador y «Registrar y finalizar», para seguirlo sin huecos. | ✅ | Commit `a4ae8b1` |
| S2·MT-07 | Como **administradora**, quiero generar el informe L-6 de **un solo equipo**, para no descargar todo el laboratorio. | ✅ | Commit `3fb6c2f` |

### E4 · Calidad y pruebas

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·QA-01 | Tests unitarios de las reglas de negocio | ✅ | **402/402** al cierre; hoy **571/571** |
| S2·QA-02 | Pruebas automáticas que usan la app como un usuario, más reglas de integridad | ✅ | 44 scripts y **3.950 comprobaciones** (incluye 21 invariantes SQL) |
| S2·QA-03 | QA de otra persona del equipo y corrección de sus observaciones | ✅ | 50/50 y 31/31 casos; regresión 833/833 |

### E5 · Operación y nube

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·OP-01 | Publicar en MonsterASP (ASP.NET + SQL Server) | ✅ | Perfil de publicación y DNS documentados |
| S2·OP-02 | Rama `qa` en GitHub y paquete de respaldo para la nube | ✅ | Respaldo verificado con huella sha256 |
| S2·OP-05 | Commit de las correcciones del 19 y 21/10 y unión a `master` | ✅ | `9aec5ca` está en `qa` y en `master` |

### E6 · Experiencia de uso

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S2·UX-01 | Auditoría de usabilidad y glosario de términos | ✅ | `docs/ux/` |
| S2·UX-02 | Nombres claros en pantalla (sin códigos «L-x»), ayuda «¿Qué hago aquí?» y página de acceso denegado | ✅ | Fases 1 y 2 subidas el 26/10 (`9aec5ca`); desde el 16/11, 5 etiquetas recuperan su código L-x por decisión del equipo |

---

## 5. Sprint 3 (27/10/2026 → 18/11/2026)

### E1 · Portal del docente

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·DO-01 | Como **docente**, quiero una cuenta vinculada a mi persona, para entrar a mi propio portal. | ✅ | Recorrido del flujo docente, 12/12 |
| S3·DO-02 | Como **responsable**, quiero que el docente vea solo lo suyo, para proteger la información. | ✅ | Recorridos de casos y de flujo |
| S3·DO-03 | Como **docente**, quiero solicitar una práctica en 4 pasos, para no olvidar ningún dato. | ✅ | Buscador en 246 ms; kit de 10 artículos precargado |
| S3·DO-04 | Como **docente**, quiero solicitar desde una práctica ya programada, para no escribir de nuevo. | ✅ | Recorrido de casos |
| S3·DO-05 | Como **docente**, quiero ver la disponibilidad por semana académica, para elegir un horario libre. | ✅ | Recorrido de planificación, 46/46 |
| S3·DO-06 | Como **docente**, quiero corregir una solicitud observada o retirarla, para mantenerla al día. | ✅ | Recorrido de casos |
| S3·DO-07 | Como **docente**, quiero indicar los estudiantes de mi solicitud, para que el material corresponda. | ✅ | Prueba con 13 estudiantes |
| S3·DO-08 | Como **docente**, quiero un inicio claro con mi próxima práctica, para saber qué sigue. | ✅ | Recorrido del inicio docente, 32/32 |
| S3·DO-09 | Como **docente**, quiero recibir un aviso si cambian mi práctica, para no llegar a un horario equivocado. | ✅ | El recorrido comprueba el de reprogramación; los otros dos están en el commit `b3d2c17`, sin prueba automática propia |
| S3·DO-10 | Como **docente**, quiero una pestaña de novedades, para ver lo que el revisor decidió sobre mis prácticas. | ✅ | Recorrido del inicio docente, 32/32 |

### E2 · Revisión de solicitudes y material por horario

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·RS-01 | Como **revisor**, quiero una bandeja por estados, para atender primero lo pendiente. | ✅ | Recorrido de planificación |
| S3·RS-02 | Como **revisor**, quiero aprobar, observar o rechazar, para responder al docente. | ✅ | Recorrido de casos |
| S3·RS-03 | Como **revisor**, quiero que no se apruebe más material del disponible, para no comprometer lo que no hay. | ✅ | Recorrido de casos; corrige la sobreventa del commit `d1eb049` |
| S3·RS-04 | Como **revisor**, quiero que dos decisiones simultáneas no se pisen, para confiar en el estado. | ✅ | 3 carreras simuladas |
| S3·RS-05 | Como **encargado**, quiero calcular el material por horario, para saber qué está libre en ese momento. | ✅ | Commits `896afb9`, `d1eb049` y `53c25cc` |
| S3·RS-06 | Como **revisor**, quiero repartir cada artículo entre laboratorios de origen, para saber de dónde sale. | ✅ | Recorridos de casos y de flujo |
| S3·RS-07 | Como **encargado**, quiero entregar desde varios orígenes, para reflejar lo que ocurre de verdad. | ✅ | Prueba con 5 unidades de un origen y 2 de otro |
| S3·RS-08 | Como **encargado**, quiero distinguir los consumibles, para descontarlos al entregarlos. | ✅ | Existencia de 10 → 8 en el recorrido |
| S3·RS-09 | Como **encargado**, quiero que una incidencia descuente del origen correcto, para no descuadrar otro laboratorio. | ✅ | Recorrido adicional: un origen de 5 → 0 y otro de 4 → 3 |
| S3·RS-10 | Como **encargado**, quiero cerrar sin novedades, para no registrar devoluciones una por una. | ✅ | Recorridos de flujo y adicional |
| S3·RS-11 | Como **revisor**, quiero comparar prácticas previstas y recibidas, para saber quién falta por solicitar. | ✅ | Recorrido de casos |

### E3 · Planificación del semestre

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·PS-01 | Como **administradora**, quiero cargar el semestre desde Excel con revisión fila por fila, para no registrar práctica por práctica. | ✅ | Recorrido de planificación, con un archivo de 5 filas |
| S3·PS-02 | Como **administradora**, quiero que cada cambio tenga motivo, para explicarlo después. | ✅ | Recorrido de planificación |
| S3·PS-03 | Como **administradora**, quiero suspender un día y reponer sus prácticas, para ordenar lo que quedó pendiente. | ✅ | Recorrido de planificación |
| S3·PS-04 | Como **administradora**, quiero impedir mover una práctica con material entregado, para no perder el control de la salida. | ✅ | Recorrido de planificación |
| S3·PS-05 | Como **administradora**, quiero ver qué cambia antes de confirmar, para evitar errores. | ✅ | Recorrido de planificación |

### E4 · Inicio, menú y avisos

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·IN-01 | Como **administradora**, quiero un Inicio con lo importante, para decidir qué hacer al entrar. | ✅ | Recorrido del Inicio, 47/47 |
| S3·IN-02 | Como **administradora**, quiero gráficos con espacio propio, para leerlos sin desplazarme. | ✅ | Recorrido del Inicio |
| S3·IN-03 | Como **usuario**, quiero que la campana cuente solo lo que debo atender, para confiar en ella. | ✅ | En EXP, de 137 a 2 avisos |
| S3·IN-04 | Como **revisor**, quiero que un aviso desaparezca al resolver la solicitud, para no revisarla dos veces. | ✅ | Pruebas `NotificationRequestResolutionTests` |
| S3·IN-05 | Como **usuario**, quiero un menú plegable, para ver solo la sección que uso. | ✅ | Recorrido del menú, 21/21 |
| S3·IN-06 | Como **usuario**, quiero registros con nombres claros, para encontrarlos rápido. | ✅ | Recorrido de navegación, 46/46 |
| S3·IN-07 | Como **usuario**, quiero la misma paginación en todos los listados, para no perder filtros ni posición. | ✅ | 420 comprobaciones |
| S3·IN-08 | Como **usuario de teléfono**, quiero usar estas pantallas sin cortes, para trabajar desde el laboratorio. | ✅ | Recorridos del Inicio, del docente y de navegación |
| S3·IN-09 | Como **administradora**, quiero ver el catálogo de artículos en el panel de activos, para no perderlo de vista. | ✅ | Commit `3af6fe8`; revisado en EXP el 18/11 |

### E5 · Mantenimiento y reportes

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·MT-01 | Como **administradora**, quiero reconstruir los equipos de cada gestión, para que el panel no salga en cero. | ✅ | En EXP, de 1 a 2.493 planes |
| S3·MT-02 | Como **administradora**, quiero ver cuándo falla un reporte, para no confundirlo con «0 equipos». | ✅ | Commit `545d4a5` |
| S3·MT-03 | Como **administradora**, quiero descargar exactamente el documento elegido, para no recibir otro. | ✅ | Commit `545d4a5` y `ReportServiceExcelTests` |
| S3·MT-04 | Como **administradora**, quiero que Adquisición no pierda ítems, para que el total sea correcto. | ✅ | Commit `545d4a5` y `ReportServiceExcelTests` |
| S3·MT-05 | Como **técnico**, quiero que la plantilla L7 descargue rápido, para no esperar. | ✅ | 1.082 cajas menos: de 1,06 s a 0,06 s |

### E6 · Base de datos real: inventario, procesos e historial

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·BD-01 | Como **administradora**, quiero confirmar que la base sigue fiel a `BaseGastronomia_v1.xlsx`, para confiar en el historial. | ✅ | 44.743 celdas, 0 diferencias; las filas de más se explican en *Estado de la base de datos* |
| S3·BD-02 | Como **equipo**, queremos saber qué trae `BaseGastronomia_v2.xlsx`, para decidir qué cargar. | ✅ | 3 hojas nuevas, vacías; 27 hojas de datos idénticas |
| S3·BD-03 | Como **equipo**, queremos el modelo de datos del circuito docente, para guardar solicitudes y orígenes. | ✅ | 4 migraciones nuevas en EXP |

### E7 · Calidad y pruebas

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·QA-01 | Como **equipo**, queremos pruebas unitarias de las reglas, para detectar errores pronto. | ✅ | **571/571**; eran 402 al cerrar el Sprint 2 |
| S3·QA-02 | Como **equipo**, queremos recorrer el sistema como un usuario, para probar lo que él verá. | ✅ | 12 ejecuciones y **1.001 comprobaciones** sobre los flujos modificados |
| S3·QA-03 | Como **equipo**, queremos integrar las pruebas de la Práctica 3, para cubrir integración y aceptación. | ✅ | 63/63 |

### E8 · Operación y nube

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·OP-01 | Como **equipo**, queremos la base real en la nube y cuentas de prueba, para probar desde cualquier lugar. | ✅ | Publicación del 04/11 |
| S3·OP-02 | Como **equipo**, queremos publicar el circuito docente de prueba, para probarlo en línea. | ✅ | Publicación del 12/11, commit `24a49d9` |
| S3·OP-03 | Como **equipo**, queremos publicar con un script, para no manejar la contraseña a mano. | ✅ | `Tools/Deploy/Publicar-MonsterAsp.ps1`, commit `86717ff` |
| S3·OP-04 | Como **equipo**, queremos cuentas de prueba por rol, para probar sin compartir la cuenta principal. | ✅ | `Tools/QaTestUsers`, commit `3321909` |

---

## 6. Siguiente etapa

| Frente | Qué incluye | De qué depende |
|---|---|---|
| Producto | Cierre de la reposición de incidentes, orden estable en Mantenimientos y planificación eligiendo Grupo → Materia | Próxima iteración |
| Despliegue | Regresión completa, 4 migraciones en la base real, publicación de lo hecho del 15 al 18/11, respaldos automáticos y unión a `qa` | Revisión del equipo y respaldo previo |
| Datos del cliente | Fechas de adquisición, capacidades, existencias de artículos de recetas, hojas nuevas de `BaseGastronomia_v2.xlsx` y Excel real del semestre | Entrega de los datos |

---

## 7. Fuentes

- **Sprint 1:** `Informe_Ejecutivo_Sprint1_Resumen.docx` y antecedente del backlog del Sprint 2.
- **Sprint 2:** `docs/sprint/BACKLOG_SPRINT2.md`, historial de commits y revisión del 18/11.
- **Sprint 3:** `docs/sprint/sprint-3/BACKLOG_SPRINT3.md` y evidencia en `outputs/presentacion-sprint3/datos/`.
