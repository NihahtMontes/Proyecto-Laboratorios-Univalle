# Backlog del Sprint 2

**Proyecto:** Laboratorios Univalle — Gastronomía
**Periodo:** 29/09/2026 → 26/10/2026
**Equipo:** Fernando Muñoz Aranda · Danielo Romero Daza · Nihaht Poly Montes Peñarrieta
**Rama:** `qa` (GitHub) · **Base de datos:** `DB_Laboratorios_Univalle_REAL` (SQL Server)

---

## 1. Antecedente: qué pasó con el Sprint 1

En el Sprint 1 se empezó a migrar el sistema a **React + NestJS + PostgreSQL**. Se migró completo el módulo de **Usuarios**. Al medir el resto, la migración **se descartó el 29/09/2026** por tres motivos:

- **Costo y tiempo medidos.** En unas 3 semanas solo Usuarios quedó con paridad completa. Faltaban 14 módulos, el asistente de 7 fases y 6 informes Excel institucionales: varios sprints sin entregar nada nuevo al cliente.
- **Hosting ya pagado.** MonsterASP corre ASP.NET + SQL Server y estaba funcionando. Migrar exigía contratar y configurar otro.
- **Riesgo.** Reescribir las reglas del asistente y los informes podía introducir errores en procesos que ya funcionaban.

**Evidencia:** la rama `migration/react` (commit `f07ebd8`, 163 archivos) conserva lo hecho. **Se reutilizó** la normalización de datos y la forma de trabajo por fases con evidencia. Sobre la marcha, el sprint se dedicó a lo nuevo: la base de datos real y el módulo de Planificación.

---

## 2. Resumen de avance

| Épica | Historias | ✅ Hechas | 🟡 Parciales | ⚪ Pendientes |
|---|---|---|---|---|
| E1 · Base de datos real: inventario, procesos e historial | 10 | 7 | 2 | 1 |
| E2 · Planificación académica de laboratorios | 16 | 12 | 2 | 2 |
| E3 · Mantenimiento de equipos | 5 | 5 | 0 | 0 |
| E4 · Calidad y pruebas | 4 | 3 | 0 | 1 |
| E5 · Operación y nube | 5 | 2 | 0 | 3 |
| E6 · Experiencia de uso | 2 | 1 | 1 | 0 |
| **Total** | **42** | **30 (71 %)** | **5 (12 %)** | **7 (17 %)** |

**Leyenda:**
- ✅ **Hecho:** funciona y está probado.
- 🟡 **Parcial:** funciona, pero falta una parte o un dato.
- ⚪ **Pendiente:** no se hizo en este sprint.
- **†** Verificado, pero **todavía sin commit** (correcciones de QA del 19 y 21/10; ver §4).

Las capturas de este documento se tomaron durante las rondas de QA, sobre **copias desechables de la base real**; la base real no se usa para probar.

---

## 3. Épicas e historias de usuario

### E1 · Base de datos real: inventario, procesos e historial

No es solo cargar un Excel. Es que el sistema arranque con **todo lo que ya ocurrió** en los laboratorios: el inventario, los mantenimientos de gestiones pasadas, las solicitudes de material de 2026 y el catálogo académico, con cada dato rastreable hasta su origen.

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| BD-01 | Como **administradora**, quiero que el sistema tenga exactamente los datos de `BaseGastronomia_v1.xlsx`, para confiar en él desde el primer día. | Comparación automática con 0 diferencias y 0 violaciones de integridad. | ✅ | 27 hojas, 23.542 filas y 44.743 celdas comparadas: **0 diferencias** (verificado el 26/10) |
| BD-02 | Como **responsable de los datos**, quiero saber de qué fila del Excel salió cada registro y qué problemas tenía, para rastrear y corregir. | Cada registro apunta a su fila; los problemas se registran, no se corrigen en silencio. | ✅ | 23.542 filas de origen guardadas; 7.448 observaciones de calidad registradas |
| BD-03 | Como **administradora**, quiero el inventario completo, para gestionar equipos e insumos. | Equipos, unidades, artículos, existencias y laboratorios cargados. | ✅ | 121 equipos, 644 unidades, 1.374 artículos, 700 existencias, 24 laboratorios |
| BD-04 | Como **administradora**, quiero ver el **historial de mantenimiento** de gestiones pasadas. | Verificaciones, solicitudes, mantenimientos y planes visibles por gestión. | ✅ | 27 gestiones históricas: 1.599 verificaciones (L-6), 329 solicitudes (L-7), 1.209 mantenimientos (L-8) y 387 planes (L-48) |
| BD-05 | Como **administradora**, quiero ver las **solicitudes de material de 2026** como planificaciones. | 2026-1 y 2026-2 consultables, con sus líneas de material. | ✅ | 1.037 planificaciones (559 + 478) y 11.451 líneas |
| BD-06 | Como **administradora**, quiero el **catálogo académico** cargado. | Materias, prácticas, recetas, horarios y grupos por materia. | ✅ | 41 materias, 196 prácticas, 2.111 líneas de receta, 869 horarios y 147 grupos |
| BD-07 | Como **administradora**, quiero ver la **vida útil** de cada equipo y cuánto le queda, para planificar su reposición. | Años de vida útil por equipo; vida restante por unidad. | 🟡 | Años cargados en **121/121** equipos. **Ninguna** de las 644 unidades tiene fecha de adquisición, así que el sistema no puede calcular la vida restante y muestra «falta fecha de adquisición». |
| BD-08 | Como **encargado de almacén**, quiero cargar existencias desde un Excel. | Importa por laboratorio y rechaza filas inválidas. | 🟡 | La pantalla funciona; falta probarla con archivos reales |
| BD-09 | Como **equipo**, queremos completar con el cliente los datos que faltan, para que todas las reglas funcionen. | Fechas de adquisición, capacidades, existencias y prácticas cargadas. | ⚪ | Detalle en el informe *Estado de la base de datos* |
| BD-10 | Como **administradora**, quiero las gestiones del semestre listas para operar. | Una preventiva y una correctiva activas. | ✅ | `2026-2` (preventiva) y `CORR-2026-2` (correctiva), activadas el 19/10 con respaldo previo |

![Detalle de una unidad: los años de vida útil están cargados (12), pero la vida restante dice «Sin datos» porque falta la fecha de adquisición (captura del 15/10).](../../outputs/e2e/41-_EquipmentUnits_Details_id_1.png)

### E2 · Planificación académica de laboratorios

El núcleo del sprint. Permite organizar **qué práctica se hace, dónde, cuándo, con quién y con qué material**, detectar a tiempo si el material no alcanza y cerrar la práctica con la entrega y la devolución.

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| PL-01 | Como **administradora**, quiero registrar prácticas con su **receta de insumos** y el kit de cada materia. | Código automático; cantidades con 2 decimales. | ✅ | Catálogo con 196 prácticas |
| PL-02 | Como **administradora**, quiero definir grupos de estudiantes («A1» = grupo A de 1.er semestre) con su horario, para no repetir datos en cada materia. | Grupo = gestión + carrera + semestre + letra. | 🟡 | Creación y horario hechos (fases 1–3). Falta planificar eligiendo Grupo → Materia (fases 4–5). En la base real todavía no hay grupos creados. |
| PL-03 | Como **administradora**, quiero un calendario académico con semanas y días de clase. | Las semanas se generan solas. | ✅ | 41 semanas y 237 días |
| PL-04 | Como **administradora**, quiero **planificar una práctica en 3 pasos**: datos → requerimientos → disponibilidad. | La receta se carga sola; avisa los artículos sin existencia. | ✅ | Capturas W03, W05 y D01 |
| PL-05 | Como **administradora**, quiero un **panel** con las prácticas de hoy, los conflictos de la semana y las devoluciones pendientes. | Avisos accionables. | ✅ | Captura P02 |
| PL-06 | Como **administradora**, quiero una **grilla semanal por laboratorio** para ver la ocupación. | Periodos × días; imprimible con el formato institucional. | ✅ | Captura P05 |
| PL-07 | Como **administradora**, quiero ver los **conflictos de material** y una recomendación para resolverlos. | Se distingue conflicto de horario, stock insuficiente, rotación y falta de registro. | ✅ | Fase 1 de 4 (captura P07) |
| PL-08 | Como **administradora**, quiero **reprogramar** una práctica arrastrándola y **reponer un día** perdido. | Valida el destino y advierte faltantes. | ✅ | Capturas E13 y E11 |
| PL-09 | Como **administradora**, quiero ver la **demanda** de insumos por periodo, para comprar a tiempo. | Cuenta planificaciones distintas y usa el stock total. | ✅ † | Corrección del 21/10 |
| PL-10 | Como **administradora**, quiero que el sistema **bloquee el choque de docente**, y que si insisto quede constancia. | Bloqueo; cuenta regresiva de 5 s; constancia en Observaciones. | ✅ † | Capturas final21 |
| PL-11 | Como **administradora**, quiero que no se planifiquen más estudiantes que la **capacidad** del ambiente. | Bloqueo duro. | 🟡 † | La regla está hecha, pero **no actúa todavía**: ningún ambiente tiene capacidad cargada (0/24) |
| PL-12 | Como **encargado de almacén**, quiero registrar la **entrega y la devolución** del material de cada práctica (L-3), con su vale en Excel. | La salida descuenta stock y la devolución lo repone. | ✅ † | Circuito inicial de Danielo, más las correcciones de QA |
| PL-13 | Como **encargado de almacén**, quiero registrar **daños y pérdidas** (L-5) sin pasar lo pendiente. | Tope = entregado − devuelto − ya registrado. | ✅ † | Captura final21_l5_tope |
| PL-14 | Como **encargado de cierre**, quiero un módulo de **Cierre de práctica** que solo me muestre lo del día. | Rol propio: no ve el resto del sistema. | ✅ | Captura RC01 |
| PL-15 | Como **administradora**, quiero resolver conflictos desde su detalle: actualizar stock, enlazar conflictos relacionados y reducir cantidades. | Fases 2–4 del rediseño de conflictos. | ⚪ | Planificado para el siguiente sprint |
| PL-16 | Como **equipo**, queremos una prueba automática completa de Planificación con la base actual. | El script recorre todo el módulo. | ⚪ | Se probó con 81 casos de verificación (50 + 31) y QA manual, pero el script general sigue sin adaptar |

![Panel de planificación de la gestión 2026-2: prácticas del día, conflictos de la semana y accesos (captura del 16/10).](../../outputs/qa-usuario/img/P02_panel_gestion_2026_2.png)

![Conflictos de material: la recomendación indica cuánto falta y qué prácticas mover (captura del 16/10).](../../outputs/qa-usuario/img/P07_conflicto_detalle_mixer.png)

![Choque de docente: la planificación no se guarda; para insistir hay que esperar 5 segundos y queda constancia (captura del 21/10).](../../outputs/qa-usuario/img/final21_docente_alerta.png)

### E3 · Mantenimiento de equipos (mejoras sobre lo existente)

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| MT-01 | Como **administradora**, quiero una gestión **preventiva y una correctiva activas a la vez**. | La correctiva es semestral (`CORR-AAAA-S`). | ✅ | Migración aplicada en la base real |
| MT-02 | Como **supervisor**, quiero reportar fallas (L-7) **solo desde el asistente**. | Checklist de 13 puntos; un solo proceso correctivo abierto por unidad; un doble envío no duplica. | ✅ | 56/56 comprobaciones |
| MT-03 | Como **administradora**, quiero **rehacer una fase** sin dejar datos inconsistentes. | Guardas de fase; el rollback restaura el estado. | ✅ | 59 + 59 comprobaciones |
| MT-04 | Como **administradora**, quiero que los **6 informes Excel** coincidan con la base y con la plantilla institucional. | L-6, L-7, L-8, L-3, L-12 y L-48. | ✅ | 62/62 comprobaciones |
| MT-05 | Como **administradora**, quiero que cada rol vea solo lo suyo y que la cuenta se bloquee tras 5 intentos. | 4 roles, incluido Cierre de práctica. | ✅ | 241 comprobaciones de permisos |

![Asistente preventivo completo: la unidad recorrió las 7 fases y aparece en «Completados» con sus documentos (captura del 18/10).](../../outputs/qa-usuario/img/qa18_02_wizard_prev_paso7.png)

### E4 · Calidad y pruebas

| ID | Historia | Estado | Evidencia |
|---|---|---|---|
| QA-01 | Tests unitarios de las reglas de negocio | ✅ | **402/402** en verde (corrida del 26/10) |
| QA-02 | Pruebas automáticas que usan la app como un usuario, más reglas de integridad | ✅ | 44 scripts y **3.950 comprobaciones** (incluye 21 invariantes SQL) |
| QA-03 | QA de otra persona del equipo y corrección de sus observaciones | ✅ † | 50/50 y 31/31 casos; regresión 833/833 |
| QA-04 | Corregir un hallazgo abierto: la paginación de Mantenimientos puede repetir filas entre páginas | ⚪ | Intermitente; causa identificada (orden no determinista) |

Detalle completo en el informe *Informe de pruebas*.

### E5 · Operación y nube

| ID | Historia | Estado | Nota |
|---|---|---|---|
| OP-01 | Publicar en MonsterASP (ASP.NET + SQL Server) | ✅ | Perfil de publicación y DNS documentados |
| OP-02 | Rama `qa` en GitHub y paquete de respaldo para la nube | ✅ | Respaldo verificado con huella sha256 |
| OP-03 | Restaurar la base real en la nube y crear las cuentas de los probadores | ⚪ | Lo hace el equipo desde el panel de MonsterASP |
| OP-04 | Respaldos automáticos de la base real | ⚪ | Los 3 trabajos programados apuntan a una base que ya no existe |
| OP-05 | Commit de las correcciones del 19 y 21/10 y unión a `master` | ⚪ | Ver §4 |

### E6 · Experiencia de uso

| ID | Historia | Estado | Nota |
|---|---|---|---|
| UX-01 | Auditoría de usabilidad y glosario de términos | ✅ | `docs/ux/` |
| UX-02 | Nombres claros en pantalla (sin códigos «L-x»), ayuda «¿Qué hago aquí?» y página de acceso denegado | 🟡 | En curso; no se presenta como terminado |

---

## 4. Lo que no se completó y por qué

| Tema | Estado | Por qué | Siguiente paso |
|---|---|---|---|
| Vida útil restante por unidad | 🟡 | El Excel no trae fechas de adquisición (0/644) | Pedir las fechas al cliente o cargarlas por unidad |
| Control de aforo | 🟡 | Ningún ambiente tiene capacidad (0/24) | Registrar la capacidad de los 24 ambientes |
| Disponibilidad de material confiable | 🟡 | 465 de los 493 artículos usados en recetas no tienen existencia registrada | Inventario físico y carga de existencias |
| Grupos «A1» en Planificación | 🟡 | Faltan las fases 4–5 | Siguiente sprint |
| Conflictos: fases 2–4 | ⚪ | Se priorizó corregir lo encontrado por QA | Siguiente sprint |
| Prueba automática de Planificación | ⚪ | El script usa datos de la base anterior | Adaptarlo a la base real |
| Respaldos automáticos | ⚪ | Los trabajos programados apuntan a una base eliminada | Reapuntarlos a la base real |
| Correcciones del 19 y 21/10 en GitHub | ⚪ | Están verificadas, pero mezcladas con la mejora de UX en curso | Separarlas y hacer commit en `qa` |
| Paginación de Mantenimientos | ⚪ | Hallazgo intermitente del 21/10 | Ordenar también por identificador |
| Migración a React + NestJS + PostgreSQL | ❌ | Descartada (§1) | — |

---

## 5. Documentos de respaldo

1. **Informe de pruebas:** tipos de prueba, resultados por script, hallazgos y capturas.
2. **Arquitectura: SOLID y patrones:** diseño por capas y cada principio con su clase real.
3. **Estado de la base de datos:** volúmenes, verificación contra el Excel y la lista de datos que faltan.
