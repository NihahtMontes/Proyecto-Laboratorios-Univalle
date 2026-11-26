# Informe de pruebas — Sprint 2

**Proyecto:** Laboratorios Univalle — Gastronomía
**Periodo:** 29/09/2026 → 26/10/2026
**Fuentes:** `Tests/Laboratorios.Tests`, `Tools/E2E`, `outputs/qa-usuario`, `outputs/qa-obs-2026-10-19`, `outputs/qa-obs-2026-10-21` y `outputs/presentacion-sprint2/datos`.

---

## 1. Resultados en una tabla

| Prueba | Qué comprueba | Resultado | Fecha |
|---|---|---|---|
| Tests unitarios (C#, xUnit) | Reglas de negocio aisladas: fases del asistente, choques, topes de L-5, códigos, reprogramación… | **402 / 402 en verde** | 26/10 |
| Pruebas automáticas HTTP (Python) | Un programa usa la app como una persona: abre pantallas, envía formularios válidos e inválidos y compara con la base | **3.929 comprobaciones** en 43 scripts | 18/10 |
| Invariantes SQL | Reglas que los datos deben cumplir siempre | **21 / 21** | 18/10 y 21/10 |
| QA de un integrante del equipo (Planificación y circuito L-3/L-5) | Observaciones de un probador, corregidas y verificadas | **50 / 50** | 19/10 |
| Informe final de QA | Pendientes corregidos y verificados | **31 / 31**, regresión **833 / 833** | 21/10 |
| Navegador real (Chromium) | Pantallas, alertas y errores de JavaScript | **49 / 49** pantallas sin errores; 27 más con capturas | 15/10 y 18/10 |
| Base real contra el Excel | Que la base sea idéntica a `BaseGastronomia_v1.xlsx` | **44.743 celdas, 0 diferencias** | 26/10 |

Todas las pruebas que escriben datos se hicieron sobre **copias desechables** de la base real; la base real nunca se usó para probar. El 26/10 se volvieron a correr los tests unitarios y la verificación contra el Excel. Los tests corrieron sobre el árbol de trabajo actual, que incluye la mejora de interfaz en curso.

---

## 2. Los cuatro tipos de prueba

1. **Tests unitarios.** Prueban una pieza del código por separado y con una base en memoria. Ejemplos:
   - el tope de L-5 nunca supera lo entregado menos lo devuelto;
   - una fase del asistente no se puede saltar.
2. **Pruebas HTTP automáticas** (`Tools/E2E/http_cases_*.py`). Inician sesión, recorren pantallas y envían formularios como un usuario real, incluidos datos inválidos o maliciosos (`<script>`, `%`, valores extremos). Después comparan lo que muestra la pantalla con lo que quedó en la base. Cada script imprime `[OK]` o `[FALLA]` por comprobación.
3. **Invariantes SQL** (`Tools/E2E/sql_invariantes.py`). Por ejemplo:
   - una unidad no puede tener dos estados abiertos;
   - el costo de un mantenimiento es igual a la suma de sus líneas.
4. **Navegador real.** Chromium visible, con capturas, para lo que la máquina no ve: alertas, textos, orden de los pasos y errores de JavaScript.

---

## 3. Las pruebas encontraron defectos reales

### Ronda del 18/10 (mantenimiento, catálogos, usuarios)
La primera pasada encontró **18 hallazgos nuevos** y **74 fallas que ya existían** en los scripts de procesos. Todos se corrigieron y se volvieron a verificar el mismo día.

| Hallazgo grave | Qué pasaba | Estado |
|---|---|---|
| H-001 | «Rehacer fase» dejaba dos historiales de estado abiertos en la unidad | Corregido |
| H-004 | El listado de solicitudes L-7 paginaba también las compras y quedaba con páginas incompletas | Corregido |
| H-006 | El Excel L-48 ocultaba 1 de cada 2 unidades | Corregido |
| H-009 | Un costo inválido (cantidad 0 o precio negativo) se guardaba cambiado, en silencio | Corregido: ahora se rechaza con un mensaje |
| H-012 | El Excel L-3 imprimía una gestión equivocada | Corregido |
| H-013 | El Excel de adquisición dejaba el texto de ejemplo de la plantilla | Corregido |
| H-019 | Una cuenta bloqueada veía un error 500 | Corregido: aviso en español |

| Área | Fallas al empezar | Fallas al cerrar |
|---|---|---|
| Procesos del asistente | 71 | **0** |
| Usuarios | 13 | **0** |
| Maestros y catálogos | 27 | **0** |

### Ronda del 19/10 (QA de un integrante del equipo sobre Planificación y el circuito L-3/L-5)
Un integrante del equipo entregó un informe con **17 observaciones**, más pedidos de funcionalidad. Se revisaron una por una contra el código:
- **5 ya estaban resueltas.**
- **Se corrigieron:**
  - el código visible `SOL3-…`;
  - la falta de stock, que ahora solo advierte;
  - la unidad física en la salida;
  - el L-5 acumulado;
  - «Asignar horario»;
  - Demanda;
  - las salidas de práctica en el listado;
  - el Excel L-3 completo y el botón «Descargar vale L-3».
- **Quedaron como decisión de producto:**
  - la opción «Todos los laboratorios»;
  - el cierre sin salidas;
  - el L-5 con decimales;
  - los informes de planificación.
- **Verificación:** 50/50 casos (`obs_verify.py`).

### Ronda del 21/10 (informe final de QA)
Se corrigieron los pendientes reales del informe y 3 defectos más que aparecieron al verificar:
- devolución contra L-5;
- tope del L-5;
- ítems que se perdían al quitar filas;
- aforo;
- choque de docente;
- Grilla, Demanda y número de semana;
- formulario de L-5 sin mensaje;
- práctica perdida al volver con errores;
- cantidades borradas al editar salidas.

**Verificación:** 31/31 casos (`final_verify.py`), regresión **833/833** y 402 tests.

### Hallazgo abierto (transparencia)
- **Paginación de Mantenimientos.** Una corrida del 21/10 (14:29) encontró filas repetidas entre las páginas del listado. La evidencia está copiada en `outputs/presentacion-sprint2/datos/evidencia_paginacion_mantenimientos_20261021.txt`. Las corridas del 18/10, del 21/10 (12:00) y del 26/10 dieron 420/420: el fallo es **intermitente**.
- **Causa probable:** el listado se ordena solo por el nombre del equipo (`Pages/Maintenances/Index.cshtml.cs:88`), y con nombres repetidos el orden no es estable.
- **Corrección propuesta:** ordenar también por identificador. **Pendiente.**

---

## 4. Resultado por script Python (regresión final, 18/10)

| Área | Script | Comprobaciones | Fallas |
|---|---|---|---|
| Datos (lectura) | `checks_academico` | 55 | 0 |
| | `checks_inventario` | 43 | 0 |
| | `checks_muestra` | 170 | 0 |
| | `checks_nuevas` | 9 | 0 |
| | `checks_procesos` | 24 | 0 |
| | `checks_qa_clasificacion` | 89 | 4 * |
| Asistente de mantenimiento | `http_cases_procesos_flujo` | 59 | 0 |
| | `http_cases_procesos_rollback` | 59 | 0 |
| | `http_cases_procesos_verifications` | 112 | 0 |
| | `http_cases_procesos_requests` | 119 | 0 |
| | `http_cases_procesos_maintenances` | 113 | 0 |
| | `http_cases_procesos_departures` | 110 | 0 |
| | `http_cases_procesos_kardex` | 88 | 0 |
| | `http_cases_procesos_acquisitions` | 95 | 0 |
| | `http_cases_procesos_equipment` | 126 | 0 |
| | `http_cases_procesos_equnits` | 99 | 0 |
| Correctivo y gestiones | `http_cases_correctivo` | 28 | 0 |
| | `http_cases_correctivo2` | 28 | 0 |
| | `http_cases_gestiones` | 24 | 0 |
| | `http_cases_l48_calendario` | 20 | 0 |
| | `http_cases_costos` | 21 | 0 |
| Informes Excel | `http_cases_informes` | 49 | 0 |
| | `http_cases_reportes` | 13 | 0 |
| Catálogos | `http_cases_catalogos_articles` | 107 | 0 |
| | `http_cases_catalogos_categories` | 70 | 0 |
| | `http_cases_catalogos_coursegroups` | 151 | 0 |
| | `http_cases_catalogos_practices` | 146 | 0 |
| | `http_cases_catalogos_stock` | 128 | 0 |
| | `http_cases_catalogos_studentgroups` | 110 | 0 |
| | `http_cases_catalogos_subjects` | 150 | 0 |
| | `http_cases_catalogos_units` | 70 | 0 |
| Maestros | `http_cases_maestros_career` | 94 | 0 |
| | `http_cases_maestros_cities` | 96 | 0 |
| | `http_cases_maestros_countries` | 83 | 0 |
| | `http_cases_maestros_faculties` | 91 | 0 |
| | `http_cases_maestros_laboratories` | 97 | 0 |
| | `http_cases_maestros_paises` | 7 | 0 |
| | `http_cases_maestros_persons` | 103 | 0 |
| Usuarios y permisos | `http_cases_maestros_users` | 166 | 0 |
| | `http_cases_usuarios_auth` | 30 | 0 |
| | `http_cases_roles` | 241 | 0 |
| | `http_cases_equipos_stock` | 16 | 0 |
| Listados | `http_cases_paginacion` | 420 | 0 ** |
| Integridad | `sql_invariantes` | 21 | 0 |
| **Total** | **44 scripts** | **3.950** | **4** |

\* Las 4 fallas vienen de la copia de prueba, no del sistema. Las causa la gestión 2027-2, creada en la copia para probar, que no existe en el Excel con el que se comparan los scripts de lectura.
\*\* En la regresión del 18/10. Ver el hallazgo abierto en §3.

**Planificación** tiene además sus propias verificaciones: `obs_verify.py` (50) y `final_verify.py` (31).

---

## 5. Capturas de las pruebas

### Planificación académica

![Panel de planificación: prácticas de hoy, conflictos de la semana y devoluciones pendientes (16/10).](../../outputs/qa-usuario/img/P02_panel_gestion_2026_2.png)

![Paso 2 · Requerimientos: la receta se carga sola y avisa los artículos sin existencia, con acceso a registrarla (16/10).](../../outputs/qa-usuario/img/W05_requerimientos_receta_cargada.png)

![Paso 3 · Disponibilidad: comparación de lo solicitado contra la existencia total (16/10).](../../outputs/qa-usuario/img/D01_disponibilidad.png)

![Grilla semanal por laboratorio, con el formato institucional «Registro de horarios» (16/10).](../../outputs/qa-usuario/img/P05_grilla_gestion4.png)

![Conflictos de material de la semana, clasificados por tipo (16/10).](../../outputs/qa-usuario/img/P06_conflictos_semana_actual.png)

![Reprogramación: advierte que el material no alcanza en el nuevo horario y exige un motivo (16/10).](../../outputs/qa-usuario/img/E13_reprogramar_preview.png)

![Choque de docente: para guardar igual hay que esperar 5 segundos y la acción queda registrada (21/10).](../../outputs/qa-usuario/img/final21_docente_cuenta_regresiva.png)

![Devolución L-3: entregado, devuelto, pendiente y cubierto por L-5, con el vale descargable (19/10).](../../outputs/qa-usuario/img/obs19v_L3_unidad_vale.png)

![Incidente L-5: el tope impide registrar más de lo pendiente (entregado 3 − devuelto 2 − ya registrado 1) (21/10).](../../outputs/qa-usuario/img/final21_l5_tope.png)

![Cierre de práctica: el rol restringido solo ve este módulo y las prácticas del día (16/10).](../../outputs/qa-usuario/img/RC01_cierre_pc.png)

### Mantenimiento de equipos

![Solicitud técnica L-7 desde el asistente correctivo, con el checklist obligatorio (18/10).](../../outputs/qa-usuario/img/qa18_05_l7_correctivo_form.png)

![Cronograma L-48 de la gestión preventiva (18/10).](../../outputs/qa-usuario/img/qa18_11_l48_prev.png)

![Asistente preventivo: una unidad completó las 7 fases (18/10).](../../outputs/qa-usuario/img/qa18_02_wizard_prev_paso7.png)

---

## 6. Lo que no se probó

| Tema | Por qué | Riesgo |
|---|---|---|
| Script automático general de Planificación (`http_cases_planificacion.py`) | Usa datos de la base anterior; falta adaptarlo. Planificación se probó con 81 casos específicos y QA manual. | Medio |
| Importar existencias desde Excel con archivos reales | Solo se probó quién tiene acceso | Medio |
| Matriz completa de permisos (152 pantallas × 5 perfiles) | Se cubrieron 58 rutas representativas y todos los enlaces del menú | Bajo |
| Impresión física de los Excel y uso en celular | No se hizo en esta ronda | Bajo |
| Varios usuarios a la vez (carga) | Antes de abrir la nube a todos | Medio |
| Prueba con usuarios finales (la Inge, técnicos) | Pendiente de agendar | Medio |

---

## 7. Cómo repetir las pruebas

```
dotnet test Tests/Laboratorios.Tests -m:1
cd Tools/ExcelImport && python verify_v12_full.py
bash Tools/E2E/run_scripts.sh <carpeta_salida> http_cases_correctivo.py http_cases_procesos_flujo.py ...
```

Los scripts que escriben datos se niegan a correr si no apuntan a una copia de prueba.
