# Backlog general del proyecto

**Proyecto:** Laboratorios Univalle — Gastronomía
**Alcance:** backlog de producto inicial y Sprints 1, 2 y 3
**Fecha de corte:** 18/11/2026
**Equipo:** Fernando Muñoz Aranda · Danielo Romero Daza · Nihaht Poly Montes Peñarrieta

---

## 1. Resumen

| Bloque | Elementos | ✅ Hechos | 🟡 Parciales | ⚪ Pendientes | ❌ Descartados |
|---|---|---|---|---|---|
| Backlog de producto inicial (propuesta) | 20 | 7 | 11 | 2 | 0 |
| Sprint 1 · frentes de trabajo | 7 | 4 | 1 | 0 | 2 |
| Sprint 2 · historias | 42 | 33 | 5 | 4 | 0 |
| Sprint 3 · historias | 58 | 44 | 4 | 10 | 0 |

**Sprints 2 y 3 sin repetir:** de 100 historias registradas, 6 son continuaciones de otra (sección 6). Quedan **94 historias: 76 hechas (81 %)** y 18 abiertas (19 %), de las cuales 9 son parciales y 9 pendientes.

**Sprint 2 al cierre (26/10):** 30 hechas, 5 parciales y 7 pendientes. La tabla muestra su estado al 18/11.

**Leyenda:**
- ✅ **Hecho:** funciona y está probado. En el Sprint 3, la prueba se hizo en la copia EXP; ese trabajo aún no está en la base real.
- 🟡 **Parcial:** funciona, pero falta una parte, un dato o aplicarlo fuera de la copia.
- ⚪ **Pendiente:** no se hizo.
- ❌ **Descartado:** se decidió no continuar.

Los códigos se repiten entre sprints, por eso aquí llevan prefijo: **S2·BD-01** es la historia BD-01 del Sprint 2.

---

## 2. Backlog de producto inicial

Fue la propuesta para el módulo de utensilios: **20 historias y 130 puntos**, repartidas en 4 sprints. No fue el plan que se ejecutó tal cual: los sprints reales priorizaron datos, migración y luego el circuito docente. La columna «Estado hoy» es una lectura del 18/11 contra el código y las pruebas; **no es una validación del cliente**.

| ID | Historia de usuario | Plan | Estado hoy | Dónde quedó |
|---|---|---|---|---|
| HU-UTEN-001 | Como responsable de laboratorio, quiero crear el plan semestral con gestión, asignatura, grupo y responsable, para organizar las prácticas sobre una base común. | P0 · 5 pts · S1 | 🟡 | Se planifica por práctica (S2·PL-04) y por Excel (S3·PS-01); no existe un borrador semestral único. |
| HU-UTEN-002 | Como responsable de laboratorio, quiero programar fecha, hora y laboratorio de cada práctica, para anticipar la preparación de materiales. | P0 · 5 pts · S1 | ✅ | S2·PL-04, PL-06 y PL-08. |
| HU-UTEN-003 | Como docente, quiero definir utensilios y cantidades por práctica, para asegurar que cada grupo reciba lo necesario. | P0 · 8 pts · S1 | ✅ | S2·PL-01 y S3·DO-03: el docente pide su material. |
| HU-UTEN-004 | Como responsable de laboratorio, quiero consultar disponibilidad por fecha y hora, para evitar asignar la misma unidad a prácticas superpuestas. | P0 · 8 pts · S1 | 🟡 | La regla existe (S3·RS-05), pero 463 de 493 artículos de recetas no tienen existencia en la base real. |
| HU-UTEN-005 | Como responsable de laboratorio, quiero recibir alertas de faltantes y solapamientos, para resolverlos antes del día de la práctica. | P0 · 5 pts · S1 | ✅ | S2·PL-07 y S3·RS-03. |
| HU-UTEN-006 | Como responsable de laboratorio, quiero aprobar una reserva de utensilios, para incorporarla a la cola semanal de preparación. | P0 · 8 pts · S2 | ✅ | S3·RS-02, RS-03 y RS-04. |
| HU-UTEN-007 | Como responsable de laboratorio, quiero solicitar utensilios disponibles de otro laboratorio, para cubrir una brecha sin comprar de inmediato. | P0 · 8 pts · S2 | 🟡 | Hay origen por línea y salida desde varios laboratorios (S3·RS-06 y RS-07); falta la aceptación del laboratorio prestador. |
| HU-UTEN-008 | Como técnico, quiero ver la cola semanal de prácticas aprobadas, para preparar los lotes en orden de fecha y urgencia. | P0 · 5 pts · S2 | ✅ | S2·PL-05 y PL-14, y S3·IN-01. |
| HU-UTEN-009 | Como técnico, quiero confirmar los utensilios preparados, para documentar diferencias antes de la entrega. | P0 · 5 pts · S2 | ⚪ | No hay un paso de «preparado» por ítem ni registro de sustituciones. |
| HU-UTEN-010 | Como técnico, quiero generar y confirmar la entrega L-3, para vincular la salida con práctica, docente, grupo y unidades. | P0 · 8 pts · S2 | ✅ | S2·PL-12 y S3·RS-07. |
| HU-UTEN-011 | Como técnico, quiero registrar cantidades y condición devueltas, para cerrar el préstamo o abrir una novedad. | P0 · 8 pts · S2 | ✅ | S2·PL-12 y S3·RS-10. |
| HU-UTEN-012 | Como técnico, quiero registrar daño, pérdida o faltante con evidencia, para iniciar el tratamiento L-5 sin borrar el historial. | P0 · 8 pts · S3 | 🟡 | Se registra el incidente (S2·PL-13, S3·RS-09); no admite adjuntar evidencia y falta cerrar la reposición (S3·RS-12). |
| HU-UTEN-013 | Como responsable de inventario, quiero bloquear catálogos pendientes o unidades no operativas, para evitar reservas sobre datos no confiables. | P0 · 5 pts · S1 | 🟡 | El selector puede ocultar lo «por confirmar» y excluye unidades retiradas, pero no es un bloqueo obligatorio. |
| HU-UTEN-014 | Como auditor, quiero consultar el historial completo de una práctica, para reconstruir reserva, entrega, devolución e incidentes. | P1 · 8 pts · S3 | 🟡 | La solicitud guarda su historial de decisiones; no hay una línea de tiempo completa de la práctica. |
| HU-UTEN-015 | Como técnico, quiero recibir avisos de preparación y devoluciones próximas, para atender el trabajo a tiempo. | P1 · 5 pts · S3 | 🟡 | Hay avisos de devoluciones vencidas (S3·IN-03); faltan los de preparación y próximas devoluciones. |
| HU-UTEN-016 | Como coordinador, quiero un tablero semanal de prácticas, brechas y devoluciones, para priorizar decisiones. | P1 · 5 pts · S3 | 🟡 | Panel e Inicio muestran prácticas, conflictos y devoluciones (S2·PL-05, S3·IN-01); no se verificó la separación completa pedida. |
| HU-UTEN-017 | Como responsable de inventario, quiero comparar demanda pico y disponibilidad, para identificar brechas recurrentes. | P1 · 8 pts · S4 | 🟡 | Existe la pantalla Demanda (S2·PL-09); depende de existencias que faltan. |
| HU-UTEN-018 | Como responsable de inventario, quiero preparar una propuesta de compra basada en brechas, para sustentar la adquisición institucional. | P2 · 8 pts · S4 | ⚪ | Solo aparece en el prototipo; no hay propuesta de compra en el sistema. |
| HU-UTEN-019 | Como técnico, quiero ver imagen y especificación del utensilio, para reducir errores de preparación. | P2 · 5 pts · S4 | 🟡 | El equipo admite imagen; el artículo no. |
| HU-UTEN-020 | Como coordinador, quiero exportar agenda, L-3 e indicadores, para compartir evidencia con responsables académicos. | P2 · 5 pts · S4 | 🟡 | Hay grilla imprimible, vale L-3 en Excel y ficha PDF; no se exportan todos los indicadores. |

---

## 3. Sprint 1 (hasta el 28/09/2026)

El Sprint 1 no se documentó con historias de usuario, sino con frentes de trabajo. Se listan así para no reescribirlo después. Su informe remite a un tablero externo para los porcentajes finales; ese tablero no forma parte de estas fuentes, por lo que **aquí no se da un porcentaje del Sprint 1**.

| ID | Frente de trabajo | Responsable según el informe | Estado hoy | Qué pasó después |
|---|---|---|---|---|
| S1-01 | Datos históricos: selección, duplicados, correcciones y referencias | Fernando Muñoz Aranda | ✅ | La normalización se reutilizó para cargar la base real del Sprint 2. |
| S1-02 | Base de datos en PostgreSQL: restricciones, relaciones e índices | Fernando / Nihaht | ❌ | PostgreSQL se descartó con la migración; los datos se cargaron en SQL Server. |
| S1-03 | Vida útil en el modelo y la interfaz | Fernando / Danielo | 🟡 | 121/121 equipos con años de vida útil; faltan las fechas para calcular lo restante (S2·BD-07). |
| S1-04 | Prototipo de planificación y consulta de equipos | Danielo Romero Daza | ✅ | Evolucionó al módulo de Planificación del Sprint 2. |
| S1-05 | Migración a React + NestJS + PostgreSQL | Nihaht Montes | ❌ | Se descartó el 29/09 tras medir su costo; lo hecho queda en la rama `migration/react`. |
| S1-06 | Calidad de arquitectura: SOLID, patrones y evidencia | Sin asignación en el informe | ✅ | Los principios se demostraron después con clases del sistema vigente. |
| S1-07 | Tablero, evidencias y demostración | Sin asignación en el informe | ✅ | Informe ejecutivo del Sprint 1 presentado. |

---

## 4. Sprint 2 (29/09/2026 → 26/10/2026)

### E1 · Base de datos real: inventario, procesos e historial

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·BD-01 | Como **administradora**, quiero que el sistema tenga exactamente los datos de `BaseGastronomia_v1.xlsx`, para confiar en él desde el primer día. | ✅ | ✅ | 27 hojas, 23.542 filas y 44.743 celdas comparadas: **0 diferencias** (verificado el 26/10) **Hoy:** Repetido el 18/11: los datos importados siguen sin diferencias. |
| S2·BD-02 | Como **responsable de los datos**, quiero saber de qué fila del Excel salió cada registro y qué problemas tenía, para rastrear y corregir. | ✅ | ✅ | 23.542 filas de origen guardadas; 7.448 observaciones de calidad registradas **Hoy:** Hoy hay 7.459 observaciones. |
| S2·BD-03 | Como **administradora**, quiero el inventario completo, para gestionar equipos e insumos. | ✅ | ✅ | 121 equipos, 644 unidades, 1.374 artículos, 700 existencias, 24 laboratorios **Hoy:** Hoy hay 702 existencias. |
| S2·BD-04 | Como **administradora**, quiero ver el **historial de mantenimiento** de gestiones pasadas. | ✅ | ✅ | 27 gestiones históricas: 1.599 verificaciones (L-6), 329 solicitudes (L-7), 1.209 mantenimientos (L-8) y 387 planes (L-48) |
| S2·BD-05 | Como **administradora**, quiero ver las **solicitudes de material de 2026** como planificaciones. | ✅ | ✅ | 1.037 planificaciones (559 + 478) y 11.451 líneas |
| S2·BD-06 | Como **administradora**, quiero el **catálogo académico** cargado. | ✅ | ✅ | 41 materias, 196 prácticas, 2.111 líneas de receta, 869 horarios y 147 grupos |
| S2·BD-07 | Como **administradora**, quiero ver la **vida útil** de cada equipo y cuánto le queda, para planificar su reposición. | 🟡 | 🟡 | Años cargados en **121/121** equipos. **Ninguna** de las 644 unidades tiene fecha de adquisición, así que el sistema no puede calcular la vida restante y muestra «falta fecha de adquisición». **Hoy:** Sigue igual: 0/644 fechas. |
| S2·BD-08 | Como **encargado de almacén**, quiero cargar existencias desde un Excel. | 🟡 | 🟡 | La pantalla funciona; falta probarla con archivos reales **Hoy:** No se registró una prueba con archivos reales. |
| S2·BD-09 | Como **equipo**, queremos completar con el cliente los datos que faltan, para que todas las reglas funcionen. | ⚪ | ⚪ | Detalle en el informe *Estado de la base de datos* **Hoy:** Continúa como S3·BD-06. |
| S2·BD-10 | Como **administradora**, quiero las gestiones del semestre listas para operar. | ✅ | ✅ | `2026-2` (preventiva) y `CORR-2026-2` (correctiva), activadas el 19/10 con respaldo previo |

### E2 · Planificación académica de laboratorios

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·PL-01 | Como **administradora**, quiero registrar prácticas con su **receta de insumos** y el kit de cada materia. | ✅ | ✅ | Catálogo con 196 prácticas |
| S2·PL-02 | Como **administradora**, quiero definir grupos de estudiantes («A1» = grupo A de 1.er semestre) con su horario, para no repetir datos en cada materia. | 🟡 | 🟡 | Creación y horario hechos (fases 1–3). Falta planificar eligiendo Grupo → Materia (fases 4–5). En la base real todavía no hay grupos creados. **Hoy:** Las fases 4 y 5 continúan como S3·PS-07. |
| S2·PL-03 | Como **administradora**, quiero un calendario académico con semanas y días de clase. | ✅ | ✅ | 41 semanas y 237 días |
| S2·PL-04 | Como **administradora**, quiero **planificar una práctica en 3 pasos**: datos → requerimientos → disponibilidad. | ✅ | ✅ | Capturas W03, W05 y D01 |
| S2·PL-05 | Como **administradora**, quiero un **panel** con las prácticas de hoy, los conflictos de la semana y las devoluciones pendientes. | ✅ | ✅ | Captura P02 |
| S2·PL-06 | Como **administradora**, quiero una **grilla semanal por laboratorio** para ver la ocupación. | ✅ | ✅ | Captura P05 |
| S2·PL-07 | Como **administradora**, quiero ver los **conflictos de material** y una recomendación para resolverlos. | ✅ | ✅ | Fase 1 de 4 (captura P07) |
| S2·PL-08 | Como **administradora**, quiero **reprogramar** una práctica arrastrándola y **reponer un día** perdido. | ✅ | ✅ | Capturas E13 y E11 |
| S2·PL-09 | Como **administradora**, quiero ver la **demanda** de insumos por periodo, para comprar a tiempo. | ✅ † | ✅ | Corrección del 21/10 **Hoy:** Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·PL-10 | Como **administradora**, quiero que el sistema **bloquee el choque de docente**, y que si insisto quede constancia. | ✅ † | ✅ | Capturas final21 **Hoy:** Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·PL-11 | Como **administradora**, quiero que no se planifiquen más estudiantes que la **capacidad** del ambiente. | 🟡 † | 🟡 | La regla está hecha, pero **no actúa todavía**: ningún ambiente tiene capacidad cargada (0/24) **Hoy:** Sigue sin capacidades (0/24). Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·PL-12 | Como **encargado de almacén**, quiero registrar la **entrega y la devolución** del material de cada práctica (L-3), con su vale en Excel. | ✅ † | ✅ | Circuito inicial de Danielo, más las correcciones de QA **Hoy:** Subido a GitHub el 26/10 (`9aec5ca`). En el Sprint 3 la regla cambió: el material reutilizable se calcula por horario (S3·RS-05). |
| S2·PL-13 | Como **encargado de almacén**, quiero registrar **daños y pérdidas** (L-5) sin pasar lo pendiente. | ✅ † | ✅ | Captura final21_l5_tope **Hoy:** Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·PL-14 | Como **encargado de cierre**, quiero un módulo de **Cierre de práctica** que solo me muestre lo del día. | ✅ | ✅ | Captura RC01 |
| S2·PL-15 | Como **administradora**, quiero resolver conflictos desde su detalle: actualizar stock, enlazar conflictos relacionados y reducir cantidades. | ⚪ | ⚪ | Planificado para el siguiente sprint **Hoy:** Continúa como S3·PS-06. |
| S2·PL-16 | Como **equipo**, queremos una prueba automática completa de Planificación con la base actual. | ⚪ | 🟡 | Se probó con 81 casos de verificación (50 + 31) y QA manual, pero el script general sigue sin adaptar **Hoy:** Recorrido nuevo de 46 comprobaciones sobre bandeja, grilla, reposiciones y carga Excel; aún no cubre Conflictos ni Demanda. |

### E3 · Mantenimiento de equipos

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·MT-01 | Como **administradora**, quiero una gestión **preventiva y una correctiva activas a la vez**. | ✅ | ✅ | Migración aplicada en la base real |
| S2·MT-02 | Como **supervisor**, quiero reportar fallas (L-7) **solo desde el asistente**. | ✅ | ✅ | 56/56 comprobaciones |
| S2·MT-03 | Como **administradora**, quiero **rehacer una fase** sin dejar datos inconsistentes. | ✅ | ✅ | 59 + 59 comprobaciones |
| S2·MT-04 | Como **administradora**, quiero que los **6 informes Excel** coincidan con la base y con la plantilla institucional. | ✅ | ✅ | 62/62 comprobaciones |
| S2·MT-05 | Como **administradora**, quiero que cada rol vea solo lo suyo y que la cuenta se bloquee tras 5 intentos. | ✅ | ✅ | 241 comprobaciones de permisos |

### E4 · Calidad y pruebas

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·QA-01 | Tests unitarios de las reglas de negocio | ✅ | ✅ | **402/402** en verde (corrida del 26/10) **Hoy:** Hoy: 571/571. |
| S2·QA-02 | Pruebas automáticas que usan la app como un usuario, más reglas de integridad | ✅ | ✅ | 44 scripts y **3.950 comprobaciones** (incluye 21 invariantes SQL) **Hoy:** En el Sprint 3 se repitieron 2 de los 43 scripts HTTP. |
| S2·QA-03 | QA de otra persona del equipo y corrección de sus observaciones | ✅ † | ✅ | 50/50 y 31/31 casos; regresión 833/833 **Hoy:** Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·QA-04 | Corregir un hallazgo abierto: la paginación de Mantenimientos puede repetir filas entre páginas | ⚪ | ⚪ | Intermitente; causa identificada (orden no determinista) **Hoy:** Continúa como S3·QA-05. |

### E5 · Operación y nube

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·OP-01 | Publicar en MonsterASP (ASP.NET + SQL Server) | ✅ | ✅ | Perfil de publicación y DNS documentados |
| S2·OP-02 | Rama `qa` en GitHub y paquete de respaldo para la nube | ✅ | ✅ | Respaldo verificado con huella sha256 |
| S2·OP-03 | Restaurar la base real en la nube y crear las cuentas de los probadores | ⚪ | ✅ | Lo hace el equipo desde el panel de MonsterASP **Hoy:** Hecho el 04/11; ver S3·OP-01. |
| S2·OP-04 | Respaldos automáticos de la base real | ⚪ | ⚪ | Los 3 trabajos programados apuntan a una base que ya no existe **Hoy:** Continúa como S3·OP-04; sigue fallando. |
| S2·OP-05 | Commit de las correcciones del 19 y 21/10 y unión a `master` | ⚪ | ✅ | Ver §4 **Hoy:** `9aec5ca` está en `qa` y en `master`. |

### E6 · Experiencia de uso

| ID | Historia | Al cierre | Hoy | Evidencia y seguimiento |
|---|---|---|---|---|
| S2·UX-01 | Auditoría de usabilidad y glosario de términos | ✅ | ✅ | `docs/ux/` |
| S2·UX-02 | Nombres claros en pantalla (sin códigos «L-x»), ayuda «¿Qué hago aquí?» y página de acceso denegado | 🟡 | ✅ | En curso; no se presenta como terminado **Hoy:** Fases 1 y 2 subidas el 26/10 (`9aec5ca`). Desde el 16/11, 5 etiquetas recuperan su código L-x por decisión del equipo. |

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
| S3·DO-10 | Como **docente**, quiero que todo el portal tenga el mismo diseño, para orientarme mejor. | 🟡 | Solo «Mi semana» tiene el diseño nuevo |

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
| S3·RS-12 | Como **encargado**, quiero cerrar la reposición de un incidente, para saber qué sigue pendiente. | ⚪ | Los 12 incidentes de EXP siguen «Pendiente de Reposición» y no hay pantalla que cambie ese estado |

### E3 · Planificación del semestre

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·PS-01 | Como **administradora**, quiero cargar el semestre desde Excel, para no registrar práctica por práctica. | 🟡 | Probado con 5 filas de ejemplo; falta el Excel real del semestre |
| S3·PS-02 | Como **administradora**, quiero que cada cambio tenga motivo, para explicarlo después. | ✅ | Recorrido de planificación |
| S3·PS-03 | Como **administradora**, quiero suspender un día y reponer sus prácticas, para ordenar lo que quedó pendiente. | ✅ | Recorrido de planificación |
| S3·PS-04 | Como **administradora**, quiero impedir mover una práctica con material entregado, para no perder el control de la salida. | ✅ | Recorrido de planificación |
| S3·PS-05 | Como **administradora**, quiero ver qué cambia antes de confirmar, para evitar errores. | ✅ | Recorrido de planificación |
| S3·PS-06 | Como **administradora**, quiero completar las fases 2 a 4 de Conflictos, para resolverlos en menos pasos. | ⚪ | Viene del Sprint 2; no hubo avance |
| S3·PS-07 | Como **administradora**, quiero terminar los grupos «A1», para no repetir datos por materia. | ⚪ | Viene del Sprint 2; no hubo avance |

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

### E5 · Mantenimiento y reportes

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·MT-01 | Como **administradora**, quiero reconstruir los equipos de cada gestión, para que el panel no salga en cero. | 🟡 | En EXP, de 1 a 2.493 planes; la base real sigue con 1 |
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
| S3·BD-04 | Como **administradora**, quiero cargar usuarios, estados y asignaciones de la v2, para completar el historial. | ⚪ | Las 3 hojas no tienen filas: todavía no hay nada que cargar |
| S3·BD-05 | Como **equipo**, queremos llevar el modelo nuevo a la base real, para usar el sistema con datos reales. | ⚪ | La base real sigue con 32 migraciones; el código tiene 36 |
| S3·BD-06 | Como **equipo**, queremos completar los datos que faltan con el cliente, para que todas las reglas funcionen. | ⚪ | Detalle en *Estado de la base de datos* |

### E7 · Calidad y pruebas

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·QA-01 | Como **equipo**, queremos pruebas unitarias de las reglas, para detectar errores pronto. | ✅ | **571/571**; eran 402 al cerrar el Sprint 2 |
| S3·QA-02 | Como **equipo**, queremos recorrer el sistema como un usuario, para probar lo que él verá. | ✅ | 12 ejecuciones y **1.001 comprobaciones** |
| S3·QA-03 | Como **equipo**, queremos integrar las pruebas de la Práctica 3, para cubrir integración y aceptación. | ✅ | 63/63 |
| S3·QA-04 | Como **equipo**, queremos repetir toda la regresión anterior, para descartar efectos en otros módulos. | 🟡 | Hoy se repitieron 2 scripts HTTP; las reglas SQL no se corrieron |
| S3·QA-05 | Como **usuario**, quiero que Mantenimientos no repita filas entre páginas, para confiar en el listado. | ⚪ | Sigue ordenando solo por nombre del equipo |

### E8 · Operación y nube

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| S3·OP-01 | Como **equipo**, queremos la base real en la nube y cuentas de prueba, para probar desde cualquier lugar. | ✅ | Publicación del 04/11 |
| S3·OP-02 | Como **equipo**, queremos publicar el circuito docente de prueba, para probarlo en línea. | ✅ | Publicación del 12/11, commit `24a49d9` |
| S3·OP-03 | Como **equipo**, queremos publicar lo hecho del 15 al 18/11, para probarlo en línea. | ⚪ | Lo publicado es del 12/11 |
| S3·OP-04 | Como **responsable**, quiero respaldos automáticos, para no depender de copias manuales. | ⚪ | El respaldo diario sigue fallando; último correcto: 15/10 |
| S3·OP-05 | Como **equipo**, queremos revisar y unir la rama, para tener una sola versión. | ⚪ | `sprint-3` ya está en GitHub; falta la revisión del equipo y la unión a `qa` |

---

## 6. Historias que continuaron de un sprint a otro

| Sprint 2 | Sprint 3 | Situación al 18/11 |
|---|---|---|
| S2·OP-03 | S3·OP-01 | Hecho el 04/11 |
| S2·PL-15 | S3·PS-06 | Sin avance |
| S2·PL-02 | S3·PS-07 | Fases 1 a 3 hechas; 4 y 5 sin avance |
| S2·BD-09 | S3·BD-06 | Esperando datos del cliente |
| S2·QA-04 | S3·QA-05 | Sigue abierto |
| S2·OP-04 | S3·OP-04 | Sigue fallando |

---

## 7. Lo que sigue abierto (18 historias)

| ID | Tema | Estado | Situación |
|---|---|---|---|
| S2·BD-07 | Ver la vida útil de cada equipo y cuánto le queda | 🟡 | Sigue igual: 0/644 fechas. |
| S2·BD-08 | Cargar existencias desde un Excel | 🟡 | No se registró una prueba con archivos reales. |
| S2·PL-02 | Definir grupos de estudiantes («A1» = grupo A de 1.er semestre) con su horario | 🟡 | Las fases 4 y 5 continúan como S3·PS-07. |
| S2·PL-11 | Que no se planifiquen más estudiantes que la capacidad del ambiente | 🟡 | Sigue sin capacidades (0/24). Subido a GitHub el 26/10 (`9aec5ca`). |
| S2·PL-16 | Una prueba automática completa de Planificación con la base actual | 🟡 | Recorrido nuevo de 46 comprobaciones sobre bandeja, grilla, reposiciones y carga Excel; aún no cubre Conflictos ni Demanda. |
| S3·DO-10 | Que todo el portal tenga el mismo diseño | 🟡 | Solo «Mi semana» tiene el diseño nuevo |
| S3·PS-01 | Cargar el semestre desde Excel | 🟡 | Probado con 5 filas de ejemplo; falta el Excel real del semestre |
| S3·MT-01 | Reconstruir los equipos de cada gestión | 🟡 | En EXP, de 1 a 2.493 planes; la base real sigue con 1 |
| S3·QA-04 | Repetir toda la regresión anterior | 🟡 | Hoy se repitieron 2 scripts HTTP; las reglas SQL no se corrieron |
| S3·RS-12 | Cerrar la reposición de un incidente | ⚪ | Los 12 incidentes de EXP siguen «Pendiente de Reposición» y no hay pantalla que cambie ese estado |
| S3·PS-06 | Completar las fases 2 a 4 de Conflictos | ⚪ | Viene del Sprint 2; no hubo avance |
| S3·BD-04 | Cargar usuarios, estados y asignaciones de la v2 | ⚪ | Las 3 hojas no tienen filas: todavía no hay nada que cargar |
| S3·BD-05 | Llevar el modelo nuevo a la base real | ⚪ | La base real sigue con 32 migraciones; el código tiene 36 |
| S3·BD-06 | Completar los datos que faltan con el cliente | ⚪ | Detalle en *Estado de la base de datos* |
| S3·QA-05 | Que Mantenimientos no repita filas entre páginas | ⚪ | Sigue ordenando solo por nombre del equipo |
| S3·OP-03 | Publicar lo hecho del 15 al 18/11 | ⚪ | Lo publicado es del 12/11 |
| S3·OP-04 | Respaldos automáticos | ⚪ | El respaldo diario sigue fallando; último correcto: 15/10 |
| S3·OP-05 | Revisar y unir la rama | ⚪ | `sprint-3` ya está en GitHub; falta la revisión del equipo y la unión a `qa` |

---

## 8. Fuentes

- **Backlog de producto:** `Backlog.xlsx`, hoja `01_PRODUCT_BACKLOG`.
- **Sprint 1:** `Informe_Ejecutivo_Sprint1_Resumen.docx` y antecedente del backlog del Sprint 2.
- **Sprint 2:** `docs/sprint/BACKLOG_SPRINT2.md`.
- **Sprint 3:** `docs/sprint/sprint-3/BACKLOG_SPRINT3.md` y evidencia en `outputs/presentacion-sprint3/datos/`.
