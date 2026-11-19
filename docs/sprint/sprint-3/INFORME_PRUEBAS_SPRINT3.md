# Informe de pruebas — Sprint 3

**Proyecto:** Laboratorios Univalle — Gastronomía
**Periodo:** 27/10/2026 → 18/11/2026
**Fuentes:** `Tests/Laboratorios.Tests`, `Tools/E2E`, `Tools/ExcelImport` y `outputs/presentacion-sprint3/datos`.

---

## 1. Resultados en una tabla

| Prueba | Qué comprueba | Resultado | Fecha |
|---|---|---|---|
| Pruebas unitarias e integración (C#, xUnit) | Reglas de negocio, servicios, reportes y avisos | **571 / 571** | 18/11 |
| Incluye: pruebas de la Práctica 3 | Unidad, integración y aceptación | **63 / 63** | 18/11 |
| Navegador real (Chromium) | 8 recorridos: docente, revisor, planificación, Inicio, menú y teléfono | **249 / 249** | 18/11 |
| Pruebas HTTP automáticas | Navegación por rol y paginación de listados | **752 / 752** | 18/11 |
| Base real contra el Excel | `BaseGastronomia_v1.xlsx` frente a la base, celda por celda | **44.743 celdas, 0 diferencias** | 18/11 |
| Integridad de la base real | `DBCC CHECKCONSTRAINTS` | **0 violaciones** | 18/11 |

Los recorridos y las pruebas HTTP suman **1.001 comprobaciones sin fallas**. Todas las pruebas que escriben datos se hicieron en **EXP, una copia desechable**. Sobre la base real solo se hicieron consultas de lectura.

**Lo que este resultado no dice:** no se repitió toda la regresión del Sprint 2. El detalle está en la sección 6.

---

## 2. Los tipos de prueba

1. **Pruebas unitarias y de integración.** Prueban reglas con una base en memoria. Por ejemplo, que una aprobación no supere el material libre o que un aviso se limpie al resolver la solicitud.
2. **Recorridos en navegador** (`Tools/E2E/*.mjs`). Chromium usa la aplicación como una persona: inicia sesión, pulsa botones, llena formularios, revisa la base y guarda capturas. También detecta errores de JavaScript y desbordes en teléfono.
3. **Pruebas HTTP** (`Tools/E2E/http_cases_*.py`). Recorren muchas pantallas por rol, comprueban permisos, títulos y paginación.
4. **Verificación de datos** (`Tools/ExcelImport/verify_v12_full.py`). Compara la base real con el Excel en cuatro niveles.

---

## 3. Resultado por recorrido (18/11)

| Área | Script | Comprobaciones | Fallas |
|---|---|---|---|
| Docente | `docente_flow` (flujo completo) | 12 | 0 |
| | `docente_casos` (observar, rechazar, carreras, faltantes) | 23 | 0 |
| | `docente_extra` (estudiantes, orígenes, incidencias, teléfono) | 22 | 0 |
| | `docente_inicio` (Mi semana con pestañas) | 32 | 0 |
| Planificación | `planificacion_semestre` (bandeja, motivos, reposición, Excel) | 46 | 0 |
| Inicio y navegación | `inicio_panel` | 47 | 0 |
| | `inicio_navegacion` (planes, menú, reportes, títulos, teléfono) | 46 | 0 |
| | `menu_plegable` | 21 | 0 |
| Roles | `http_cases_ux_navegacion`, SuperAdmin y Administrador | 275 | 0 |
| | `http_cases_ux_navegacion`, Supervisor | 47 | 0 |
| | `http_cases_ux_navegacion`, Cierre | 10 | 0 |
| Listados | `http_cases_paginacion` | 420 | 0 |
| **Total** | **12 ejecuciones** | **1.001** | **0** |

**Transparencia sobre la primera pasada.** En la ronda de hoy, `inicio_navegacion` marcó 2 fallas. La barra de pestañas se desplaza a propósito en el teléfono, y el script la interpretó como un elemento cortado. Se ajustó esa comprobación y el recorrido se repitió con 46/46. Antes, `docente_casos` y `docente_extra` se adaptaron a la nueva pestaña «Por solicitar». Los resultados de la tabla son los de la última ejecución de cada script.

---

## 4. Las pruebas encontraron defectos reales

| Defecto | Qué pasaba | Estado |
|---|---|---|
| Sobreventa al aprobar | Dos solicitudes pendientes podían aprobarse sobre el mismo material escaso | Corregido: se revalida dentro del bloqueo |
| Decisiones simultáneas | Observar, rechazar o retirar podían cruzarse con una aprobación | Corregido: toman el bloqueo y releen el estado |
| Programaciones huérfanas | Una solicitud rechazada o retirada dejaba su programación creada | Corregido: se cancela si la creó el docente y nadie más la usa |
| Material no devuelto | El semáforo no descontaba lo pendiente de prácticas anteriores | Corregido |
| Economato casi siempre en «libre 0» | Se sumaba la demanda de todas las prácticas cruzadas y se bloqueaban aprobaciones válidas | Corregido: se usa el mayor uso simultáneo |
| Incidencia en el origen equivocado | No se distinguía de qué laboratorio salió cada ítem | Corregido: descuenta por origen |
| Menú con la base caída | Los contadores podían romper la página | Corregido: devuelven 0 |
| Reporte vacío engañoso | Un error se mostraba como «0 equipos» | Corregido: se avisa y queda registrado |
| Adquisición con muchos ítems | Los ítems que no cabían se perdían | Corregido: nota con cantidad y subtotal |
| Campana saturada | En EXP mostraba 137 avisos, incluidas solicitudes ya resueltas y datos por revisar | Corregido: muestra 2, los que requieren acción |

![Prueba de faltantes: el revisor ve el aviso y debe decidir antes de aprobar (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_revision.png)

![Gráficos del Inicio: la prueba exige que cada gráfico se dibuje con tamaño real al abrir la pestaña (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_graficos.png)

---

## 5. Hallazgos abiertos

| Hallazgo | Impacto | Siguiente paso |
|---|---|---|
| Mantenimientos se ordena solo por nombre de equipo | Con nombres repetidos, una fila podría aparecer en dos páginas. La prueba de hoy no lo detectó, pero el código sigue igual | Agregar una segunda columna de orden |
| Incidentes L-5 sin cierre de reposición | En EXP, 12 incidentes quedan «Pendiente de Reposición» | Crear la pantalla |
| Carga por Excel sin bloqueo de concurrencia | Si alguien planifica durante la carga, la revisión final lo detecta casi siempre, pero no hay candado | Agregar bloqueo |
| Carga por Excel sin revisión de material por fila | Hay que revisar «Conflictos de material» después de cargar | Decidir si se valida antes |
| Respaldos automáticos | El respaldo diario falla porque busca una base que ya no existe | Reapuntarlo |

---

## 6. Lo que no se probó

- **41 de los 43 scripts HTTP no se repitieron.** Su última regresión completa es del Sprint 2. Hoy se corrieron navegación por rol y paginación.
- **Las 21 reglas SQL no se corrieron.** `sql_invariantes` solo acepta la copia `DB_Laboratorios_Univalle_QA_E2E`, que hoy no existe, y no se forzó sobre EXP. La integridad de la base real sí se comprobó con `DBCC CHECKCONSTRAINTS`.
- **El Excel real del semestre.** La carga se probó con un archivo de 5 filas.
- **La nube.** No se probó hoy. Tiene la versión del 12/11.
- **La base real con el código nuevo.** No tiene las 4 migraciones de este sprint, por lo que el circuito docente no puede ejecutarse allí todavía.
- **Otros navegadores y carga.** Los recorridos usan Chromium y no se hicieron pruebas de estrés.
