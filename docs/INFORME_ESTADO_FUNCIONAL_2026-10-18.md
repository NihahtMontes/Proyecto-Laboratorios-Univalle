# Informe de estado funcional — Laboratorios Univalle

Fecha: 2026-10-18. Rama `integration/planificacion-2026-10`, commits `256256a` y `d21137c`. Base de producción local: `DB_Laboratorios_Univalle_REAL`.

Este informe dice, módulo por módulo:
- **qué debería hacer** el sistema;
- **cómo se probó**;
- **qué resultado dio**;
- **qué no se probó o está mal**, señalado como tal.

«Probado» quiere decir comprobado con evidencia que se puede repetir. No garantiza que no haya ningún defecto: puede aparecer alguno con datos o formas de uso que las pruebas no cubrieron.

Evidencia completa (carpeta local, no está en git): `outputs/qa-usuario/qa-2026-10-18/`, con `INFORME_QA_2026-10-18.md`, `hallazgos.md`, `PROGRESO.md`, las carpetas de resultados y las capturas en `outputs/qa-usuario/img/qa18*`.

---

## 1. Cómo se probó (los 4 tipos de prueba)

| Tipo | Qué es | Volumen |
|---|---|---|
| **Pruebas HTTP automáticas** (`Tools/E2E/http_cases_*.py`) | Un programa usa la app como un usuario: abre pantallas, llena formularios, envía datos válidos e inválidos y compara el resultado con la base de datos. | 44 scripts, **3.950 comprobaciones** |
| **Invariantes SQL** (`Tools/E2E/sql_invariantes.py`) | Reglas que los datos deben cumplir siempre. Ejemplos: una unidad no puede tener dos estados abiertos; el costo de un L-8 es igual a la suma de sus líneas. | 21 reglas |
| **Tests unitarios** (`Tests/Laboratorios.Tests`) | Prueban piezas del código por separado. | **363 tests** |
| **Navegador real** (Chromium visible) | Se abrió la app como una persona y se revisaron pantallas, SweetAlert y errores de JavaScript. | 27 pantallas (12 + 15) con capturas |

Todo se ejecutó sobre una **copia desechable** de la base REAL, que ya se eliminó; la base REAL no se usó para probar. Al final se comprobó que la base REAL sigue idéntica a `BaseGastronomia_v1.xlsx` (0 diferencias).

**Resultado global:** 0 fallas nuevas. Las 4 fallas que quedan en `checks_qa_clasificacion` son de la copia de prueba: las causa la gestión 2027-2 creada para probar, que no está en el Excel. No son defectos.

**Leyenda:**
- ✅ **Funciona**: probado a fondo, sin defectos abiertos.
- 🟡 **Funciona con reservas**: probado, pero queda algo menor o una parte sin probar.
- ⚪ **No probado** en esta ronda.
- 🔴 **Defecto o decisión pendiente** que hay que atender.

---

## 2. Resumen por módulo

| # | Módulo | Estado | En una línea |
|---|---|---|---|
| 1 | Inicio de sesión, usuarios y roles | ✅ | Los 4 roles entran, cada uno ve solo lo suyo, el bloqueo funciona sin error y el estado de la cuenta se puede editar. |
| 2 | Gestiones (preventiva y correctiva) | 🟡 | Funciona, pero en la base REAL falta crear las gestiones de trabajo (§4.1). |
| 3 | Asistente preventivo L-6 → L-12 | ✅ | Flujo completo, borradores, guardas de fase y «Rehacer». |
| 4 | Asistente correctivo L-7 → L-12 | ✅ | Convive con el preventivo; un proceso abierto por unidad. |
| 5 | L-6 Verificaciones | ✅ | Validaciones; la baja no deja planes inconsistentes. |
| 6 | L-7 Solicitudes de servicio | ✅ | Solo se listan; «Nueva» lleva al asistente. |
| 7 | L-8 Mantenimientos y costos | ✅ | Costos validados; el mismo total en todas partes. |
| 8 | L-3 Salidas | ✅ | No se reabre una salida devuelta. |
| 9 | Kardex | ✅ | No se pone «En Préstamo» sin salida. |
| 10 | L-12 Adquisiciones | ✅ | Paginado; líneas anuladas, no borradas. |
| 11 | Calendario y cronograma L-48 | ✅ | Eventos, colores, sombreado y Excel iguales a la base. |
| 12 | Informes Excel (L-6, L-7, L-8, L-3, L-12, L-48) | ✅ | Abren, coinciden con la base y respetan la impresión de la plantilla. |
| 13 | Equipos, unidades y panel de activos | ✅ | Filtros iguales a la base; no se crean unidades en estado inválido. |
| 14 | Existencias y artículos | 🟡 | Funciona; la **importación desde Excel** solo se probó en permisos. |
| 15 | Catálogos académicos (materias, prácticas, grupos) | ✅ | Validaciones y bajas protegidas. |
| 16 | Maestros (país, ciudad, facultad, carrera, laboratorio, personas) | ✅ | Edición y bajas con dependencias. |
| 17 | Listados, filtros, búsqueda y paginación | ✅ | 420 comprobaciones; sin errores con valores extremos ni XSS. |
| 18 | Integridad de datos y base REAL | ✅ | 21 invariantes en 0; la base REAL igual al Excel. |
| 19 | **Planificación académica** (Panel, Grilla, Planificaciones, Calendario, Reposiciones, Demanda, Conflictos) | ⚪ | **No se probó el funcionamiento** con la base actual, solo el acceso por rol (§3). |
| 20 | **Cierre de práctica** (rol restringido) | 🟡 | Probado que el rol entra solo a Cierre y Notificaciones; el flujo de cierre no se probó. |
| 21 | Notificaciones | 🟡 | Se generan y no se duplican; la pantalla solo se probó en acceso. |

---

## 3. Detalle por módulo: qué se espera, cómo se probó y resultado

### 1. Inicio de sesión, usuarios y roles ✅
- **Se espera:**
  - SuperAdmin y Administrador ven todo.
  - El Supervisor ve el asistente, reporta fallas (L-7) y ve los informes, pero no las pantallas de administración.
  - «Cierre de práctica» solo entra a Cierre y Notificaciones.
  - Tras 5 intentos fallidos la cuenta se bloquea 15 minutos, con aviso en español.
  - Un `ReturnUrl` que apunta fuera del sitio no redirige afuera.
  - El administrador puede activar o inactivar cuentas.
- **Cómo se probó:**
  - `http_cases_usuarios_auth.py` (30): los 4 roles, cierre de sesión, bloqueo, cambio de rol y enlaces del menú por rol.
  - `http_cases_maestros_users.py` (166): crear y editar, duplicados, contraseña de 12 caracteres o más.
  - `http_cases_roles.py` (241): 58 rutas × 4 roles.
  - Navegador: menú del Supervisor.
- **Resultado:** 437/437 OK.
- **No probado:** la matriz completa de las 152 pantallas × 5 perfiles (US-06). Se cubrieron 58 rutas representativas más todos los enlaces del menú.

### 2. Gestiones 🟡
- **Se espera:**
  - Puede haber activas a la vez **una preventiva y una correctiva**.
  - La correctiva es semestral (`CORR-2027-2`).
  - Activar una gestión pide confirmación y cierra, como Completada y sin vuelta atrás, la activa anterior del mismo tipo.
  - La correctiva no aparece en las pantallas académicas.
- **Cómo se probó:** `http_cases_gestiones.py` (24), `http_cases_correctivo.py` (CR-01) y tests `ManagementCoexistenceTests`.
- **Resultado:** OK. La migración que permite la convivencia **ya está aplicada en la base REAL**.
- 🔴 **Pendiente antes de usar en serio:** ver §4.1.

### 3. Asistente preventivo (L-6 → L-7 → L-8 → L-3 → Kardex → L-12 → Completados) ✅
- **Se espera:**
  - Cada unidad planificada avanza por las fases en orden.
  - Un L-6 sano saca la unidad del flujo; un L-6 con falla crea la L-7.
  - Los borradores no duplican registros.
  - No se puede saltar fases, ni por URL ni con formularios viejos reenviados.
  - «Rehacer fase» vuelve atrás y restaura el estado de la unidad sin dejar historiales abiertos de más.
  - Completados (paso 7) lista exactamente los planes cerrados.
- **Cómo se probó:**
  - `http_cases_procesos_flujo.py` (59) y `http_cases_procesos_rollback.py` (59).
  - Los scripts por módulo (verificaciones, mantenimientos, salidas, kardex, adquisiciones).
  - Invariantes IV-01…05 y 10.
  - Navegador: pasos 0–7.
- **Resultado:** 0 fallas. Antes de esta ronda había 89 fallas en estos scripts.

### 4. Asistente correctivo ✅
- **Se espera:**
  - Se entra por «Man. Correctivos» o por «Nueva solicitud» en L-7, y se abre en el paso 2 (L-7).
  - Si la unidad ya tiene un proceso correctivo abierto, aparece un mensaje claro y no se crea nada.
  - Al cerrarse, la unidad puede abrir otro.
  - El Supervisor puede reportar la falla.
  - Una gestión cerrada o inactiva no acepta L-7 nuevas.
  - Un doble envío no duplica la solicitud.
- **Cómo se probó:** `http_cases_correctivo.py` y `http_cases_correctivo2.py` (56; incluye doble envío con 2 hilos). Navegador: Supervisor → «Nueva L-7» → asistente.
- **Resultado:** 56/56 OK.

### 5–10. Módulos de proceso sueltos ✅
| Módulo | Qué se espera (resumen) | Prueba | Resultado |
|---|---|---|---|
| L-6 Verificaciones | Validar unidad, fallas y puntos de control; «Malo» exige observación; la baja reinicia el plan o se bloquea si el plan ya pasó esa fase | `procesos_verifications` (112) | 0 fallas |
| L-7 Solicitudes | Solo listado (filtra técnicas antes de paginar); «Rechazada» exige motivo; no vuelve a «Pendiente»; checklist completo de 13 puntos | `procesos_requests` (119) | 0 fallas |
| L-8 Mantenimientos | Costos con cantidad de 0,01 a 99.999 y precio de 0 a 999.999.999, rechazados con mensaje (nunca corregidos en silencio); quitar una línea la anula; costo 0 permitido en «Completado» | `procesos_maintenances` (113), `http_cases_costos` (21) | 0 fallas |
| L-3 Salidas | No reabrir una salida devuelta; L-3 masivo pone la unidad «En Préstamo» | `procesos_departures` (110) | 0 fallas |
| Kardex | Sin baja 99 ni «En Préstamo» sin salida abierta; «Rehacer Kardex» deja la unidad «En Préstamo» | `procesos_kardex` (88) | 0 fallas |
| L-12 Adquisiciones | Centro de costos obligatorio; líneas validadas y anuladas; baja bloqueada si está vinculada a un plan | `procesos_acquisitions` (95) | 0 fallas |

**Ojo (decisión pendiente, §4.3):** L-6, L-8 y L-3 todavía **se pueden crear fuera del asistente**, desde sus propios módulos, y esos registros quedan fuera del mapa de procesos. Para la L-7 eso ya se cerró.

### 11. Calendario y cronograma L-48 ✅
- **Se espera:**
  - La pestaña L-48 tiene una fila por plan.
  - «Retrasado» sigue la regla de fechas.
  - El calendario tiene un evento por plan con fecha, con colores por tipo.
  - Sombreado: semestre 1 en junio-julio, semestre 2 en diciembre-enero; la correctiva no lleva sombreado ni semanas.
  - El Excel L-48 incluye **todas** las unidades.
- **Cómo se probó:** `http_cases_l48_calendario.py` (20) y navegador (calendario y L-48).
- **Resultado:** 20/20 OK.
- **Sin probar a fondo:** el L-48 histórico `/Maintenances/Plan` (solo se probó que carga y pagina).
- **Sin decidir:** las páginas sin enlace en el menú `/Schedules` y `/Schedules/Practices` (se prueba que no dan error).

### 12. Informes Excel ✅
- **Se espera:**
  - Los 6 informes descargan, abren y coinciden con la base: códigos, fechas, técnico, costos, gestión correcta y requerimiento real.
  - Conservan el área de impresión de la plantilla.
  - Un id inexistente da 404 con mensaje, no un error 500.
  - El L-6 en correctivo da el aviso «solo preventivo».
- **Cómo se probó:** `http_cases_informes.py` (49, abriendo cada Excel con openpyxl) y `http_cases_reportes.py` (13).
- **Resultado:** 62/62 OK.
- **No probado:**
  - la impresión física en papel;
  - el PDF de verificación, que es código sin uso.

### 13. Equipos, unidades y panel de activos ✅
- **Se espera:** filtros iguales a la base; no crear unidades «Eliminada» ni «En Préstamo»; precio con límite y vida útil de 0 a 50 años; el panel cubre las 644 unidades.
- **Prueba:** `procesos_equipment` (126), `procesos_equnits` (99), `http_cases_equipos_stock` (16).
- **Resultado:** 0 fallas.

### 14. Existencias y artículos 🟡
- **Se espera:**
  - Existencias paginada de a 20 y por laboratorio.
  - Al editar se respeta la concurrencia.
  - Los artículos tienen CRUD con baja lógica, y se pueden recrear con un nombre dado de baja.
- **Prueba:** `catalogos_stock` (128), `catalogos_articles` (107), `categories` (70), `units` (70), `checks_academico`. Navegador: Existencias página 3.
- **Resultado:** 0 fallas.
- ⚪ **No probado:** **Importar existencias desde Excel** (`/Catalogs/Stock/Import`) con archivos reales, inválidos o duplicados. Solo se probó quién tiene acceso.

### 15. Catálogos académicos ✅
- **Se espera:**
  - Materias, prácticas con receta (cantidad con 2 decimales), grupos de estudiantes y grupos por materia.
  - No se da de baja una materia con prácticas activas.
  - Los grupos se crean solo en gestiones activas o inactivas.
  - Los nombres de grupo se comparan sin espacios.
- **Prueba:** `subjects` (150), `practices` (146), `studentgroups` (110), `coursegroups` (151).
- **Resultado:** 0 fallas.

### 16. Maestros ✅
- **Se espera:** editar guarda; no se da de baja un país con ciudades activas ni una facultad con laboratorios activos; los mensajes salen en español.
- **Prueba:** `career` (94), `cities` (96), `countries` (83), `faculties` (91), `laboratories` (97), `persons` (103), `paises` (7). Navegador: baja de país bloqueada y mínimo de 2 caracteres.
- **Resultado:** 0 fallas.
- **Detalle menor:** en los maestros, la página «Dar de baja» funciona como confirmación y no abre además un SweetAlert (S4, sin corregir).

### 17. Listados, filtros y paginación ✅
- **Se espera:**
  - 20 filas por página y un «Mostrando X–Y de N» correcto.
  - Recorrer todas las páginas da el total de la base.
  - Una página mayor que la última muestra la última.
  - Valores extremos (`-1`, `abc`, `107374183`) no dan error.
  - Búsqueda con `%`, `'` y `<script>` sin error ni XSS.
  - Cada listado carga en menos de 3 segundos.
- **Prueba:** `http_cases_paginacion.py` (420, en 26 listados).
- **Resultado:** 420/420.
- **Límite:** 5 pantallas no se leen fila por fila (Plan, Career, Managements, EnvironmentUnits, Schedules/Practices); en ellas solo se comprobó que cargan sin error.

### 18. Integridad y base REAL ✅
- 21 invariantes en 0 después de todos los flujos de prueba.
- `verify_v12_full.py` sobre la base REAL: **0 diferencias** con `BaseGastronomia_v1.xlsx` y 0 violaciones de restricciones, antes y después de la migración.
- Respaldo antes de migrar: `C:\Users\Public\BackupsUnivalle\REAL_PRE_MIGRACION_20261018_CorrectiveCoexistence.bak`.

### 19. Planificación académica ⚪ (NO PROBADA en esta ronda)
- **Qué incluye:** `LaboratoryOperations` (15 pantallas: Panel, Grilla, Planificaciones, Calendario académico, Reposiciones, Demanda, Conflictos y Práctica), Requerimientos y Disponibilidad.
- **Por qué no se probó:** su script `http_cases_planificacion.py` usa datos de prueba de la base anterior (`Tools/E2E/sql/qa_conflict_fixtures_min.sql`, gestión 4). No corre contra la base REAL actual sin adaptarse.
- **Qué sí se sabe:** todas responden sin error y con el permiso correcto por rol (`http_cases_roles`), y `checks_academico` comprueba que muestran los datos importados.
- **Riesgo:** medio. Es el módulo con más trabajo reciente de otros chats (conflictos, grilla, disponibilidad).

### 20–21. Cierre de práctica y Notificaciones 🟡
- **Cierre de práctica:** se comprobó el acceso restringido del rol. El flujo L-3/L-5 de cierre depende de la planificación (§19) y no se probó.
- **Notificaciones:** el correctivo las genera y la invariante IV-09 confirma que no hay duplicados. La pantalla solo se probó en acceso.

---

## 4. Lo que está mal, falta decidir o falta hacer (priorizado)

### 4.1 ✅ Resuelto el 2026-10-19: preparar la base REAL para operar
> Decisión del usuario: la gestión importada `2026-2` (Id 4) pasó a **Preventiva** y se creó **`CORR-2026-2`** (Id 28), ambas activas. Hubo respaldo previo (`REAL_PRE_GESTIONES_QA_20261019.bak`) y verify_v12_full dio 0 diferencias después del cambio. Lo que sigue queda como historia.

- La base REAL tiene 27 gestiones históricas «Por confirmar». La #4 (`2026-2`) está activa, pero **no hay ninguna gestión preventiva ni correctiva propia**.
- Sin una correctiva activa, «Nueva L-7» responde «No hay una gestión correctiva activa» (comportamiento esperado).
- Para la preventiva del semestre actual hay un choque: los códigos `AAAA-S` (por ejemplo `2026-2`) **ya los ocupan las gestiones importadas**, así que crear la preventiva 2026-2 da «Ya existe una gestión con el código 2026-2».
- **Hay que decidir:** convertir la gestión importada 2026-2 en Preventiva, o usar otro código o semestre. Después hay que crear `CORR-2026-2`.

### 4.2 🔴 Prioridad 1: operación
- Los 3 trabajos de SQL Server Agent (respaldo diario, respaldo de log y verificación) apuntan a una base que ya no existe. Hay que reapuntarlos a `DB_Laboratorios_Univalle_REAL` o desactivarlos. **Hoy no hay respaldos automáticos de la base REAL.**
- El usuario `nihaht` tiene cédula y teléfono provisionales (1000001 / 70000001): hay que editarlos en /Users.
- Push: ✅ 2026-10-19, rama `qa` para la ronda de QA. Falta el PR a `master` cuando la QA lo apruebe.

### 4.3 🟡 Prioridad 2: decisiones de diseño
- **L-6, L-8 y L-3 sueltos:** hoy se pueden crear fuera del asistente. Propuesta: aplicarles la misma regla que a la L-7 (solo dentro del proceso).
- **Páginas sin enlace en el menú:** `/Schedules` («Cronograma de Intervenciones L-48») y `/Schedules/Practices`. Hay que decidir si se enlazan o se retiran.
- **Activar una gestión** cierra la anterior del mismo tipo como Completada, sin vuelta atrás. Ya pide confirmación; hay que decidir si debería poder deshacerse.

### 4.4 ⚪ Prioridad 2: pruebas que faltan
1. **Planificación académica completa** (§19): adaptar `http_cases_planificacion.py` a la base actual y probar Panel, Grilla, Conflictos, Reposiciones y Cierre de práctica.
2. **Importar existencias desde Excel** con archivos válidos, inválidos y duplicados.
3. **Concurrencia en costos** (CO-10): dos personas editan el mismo L-8 a la vez.
4. **Matriz completa de permisos** (152 pantallas × 5 perfiles).
5. **L-48 histórico** `/Maintenances/Plan`: filtros contra la base.
6. **Prueba con usuarios reales** (la Inge o los técnicos) con datos del semestre: lo que la máquina no ve, como textos confusos u orden de pasos.
7. **Impresión física** de los Excel y vista en celular o tableta.
8. **Varios usuarios a la vez** (carga ligera) antes de subir a la nube.

### 4.5 🟢 Prioridad 3: deuda técnica y menores
- Las páginas «Dar de baja» de los maestros no usan SweetAlert (la propia página confirma).
- Las advertencias de parámetros de SweetAlert2 v7 en el inicio (inofensivas).
- El aviso de EF Core «global query filter on required end» en 6 relaciones (Management–AcademicWeek, Article–ArticleStock, etc.): si un principal se da de baja, sus dependientes quedan ocultos. No dio fallas, pero conviene revisarlo.
- El PDF `VerificationReportService` es código sin uso: retirarlo o usarlo.
- `Prototype/UtensilPlanning`, `Migration` y `Admin`: pantallas técnicas que hay que revisar si deben quedar.

### 4.6 Hoja de ruta mayor (memoria del proyecto)
- **Reforma de grupos:** faltan F4 (selector Grupo → Materia en Planificación) y F5 (retirar el formulario por materia).
- **Rediseño de conflictos:** las fases 2–4 (botón «Actualizar stock», enlace visual entre conflictos y resolver desde el detalle).
- **Subida a la nube** (MonsterASP): aplicar las migraciones, restaurar el `.bak` de la base REAL, publicar y configurar los secretos. Requiere un pedido específico.

---

## 4.8 QA de planificación y circuito L-3/L-5 (informe de un probador, 2026-10-19)

Se revisaron las observaciones del «Informe de QA Funcional — Auditoría del Circuito y Solicitudes L-3» contra la rama `qa`. Detalle y evidencia (fuera de git): `outputs/qa-obs-2026-10-19/INFORME_OBSERVACIONES_DANILOVER.md`.

- **Ya estaban resueltas:**
  - buscador de `/Departures`;
  - edición de salidas con unidad obligatoria;
  - «GESTIÓN» fija del Excel L-3;
  - L-3 de finalizadas sin salida;
  - kit con artículos retirados.
- **Corregidas** (sin commit; aprobadas por el usuario):
  - código visible `SOL3-…`;
  - falta de stock que solo advierte (D2);
  - unidad física en la salida de práctica;
  - L-5 acumulado;
  - «Asignar horario» para aprobadas sin periodo;
  - Demanda con alcance y stock total;
  - salidas de práctica en `/Departures` (sin editar fuera de L-3);
  - Excel L-3 completo (laboratorio, cabecera académica y filas sin pisar la firma);
  - botón «Descargar vale L-3».
- **Verificación:** 50/50 casos en una copia, regresión E2E 833/833 y 378 tests.
- **Pendientes de decisión:**
  - O2 «Todos los laboratorios» (se eliminó a propósito en `bd0d465`);
  - D10, salidas y devoluciones de V12;
  - L-5 en decimales (requiere migración);
  - importación desde la UI, alertas persistidas, informes de planificación e historial de auditoría.

### 4.8.1 Informe final de QA: pendientes corregidos (2026-10-21, sin commit)

Fuente: `outputs/qa-obs-2026-10-19/REVISION_INFORME_FINAL_GLM.md` (pendientes reales verificados contra el código local).

| # | Corrección | Archivos principales |
|---|---|---|
| BUG-03 | El motivo del historial de estado de un L-5 con unidad física se recorta a 500 (antes `DbUpdateException` con descripciones largas) | `Data/ApplicationDbContext.cs` |
| BUG-02b | La devolución L-3 no admite lo ya cubierto por L-5 (no repone stock perdido); precarga «sin cubrir»; la salida se cierra cuando lo devuelto más lo cubierto alcanza lo entregado (también en orígenes reprogramados) | `Services/L3CircuitService.cs` (`L3ReturnRules`), `L3.cshtml(.cs)` |
| BUG-02a | El tope de L-5 resta lo devuelto: entregado − devuelto − ya registrado; el selector muestra «devuelto: n» | `L5.cshtml.cs` |
| BUG-06 | `/Departures/Create` y `/Departures/Edit` reindexan los ítems al quitar o agregar filas (antes se perdían ítems en silencio) | `Departures/Create.cshtml`, `Edit.cshtml` |
| Aforo | El alta, la edición y «Asignar horario» bloquean si los estudiantes superan la capacidad del ambiente (mismo texto que la reprogramación). **En REAL ningún laboratorio tiene capacidad cargada: la regla se activa al registrarla.** | `Planificacion.cshtml.cs`, `PlanningAssignmentRules.cs` |
| Docente | Choque de docente en el alta, la edición y «Asignar horario»: se bloquea con una alerta destacada; para insistir hay que esperar una cuenta regresiva de 5 s y la aceptación queda en las observaciones («Choque de docente aceptado por {usuario} el {fecha}: …»). Una huella impide aceptar choques distintos de los mostrados. Al editar sin cambiar horario ni docente no se vuelve a pedir. | `Planificacion.cshtml(.cs)`, `wwwroot/js/confirm-countdown.js` |
| Bajos | Grilla conserva `returnUrl` al cambiar de ambiente o semana; Demanda cuenta planificaciones distintas; la reprogramación actualiza `PracticeSchedule.WeekNumber` | `Grilla.cshtml`, `Demanda.cshtml.cs`, `ReschedulingService.cs` |
| Hallados en la verificación | L-5: si la validación fallaba, la página quedaba sin formulario ni mensaje. Planificación: la práctica elegida se perdía al volver con errores o al editar, y el aviso «No hay prácticas del catálogo» se veía siempre. Salidas: la edición borraba las cantidades (`1,00` no es válido en un campo numérico). | `L5.cshtml.cs`, `Planificacion.cshtml`, `Departures/Create.cshtml`, `Edit.cshtml` |

- **Verificación:**
  - 402 tests (24 nuevos);
  - copia desechable de REAL: `outputs/qa-obs-2026-10-21/final_verify.py` 31/31 y `obs_verify.py` 50/50;
  - regresión E2E 833/833 (departures 110, informes 49, reportes 13, paginación 420, roles 241);
  - capturas `outputs/qa-usuario/img/final21_*.png`;
  - REAL y la nube sin tocar; copia y snapshot eliminados.
- **Datos de REAL a tener en cuenta:**
  - ningún artículo está vinculado a un equipo (`Articles.EquipmentId` vacío), así que el L-5 de una unidad física se registra sobre otro artículo entregado;
  - muchas materias importadas no tienen prácticas de catálogo, y sin ellas una planificación importada no se puede editar.

## 4.9 Mejora de UX/UI, fases 1 y 2 (2026-10-26, sin commit)

Auditoría y plan: `docs/ux/AUDITORIA_UX_2026-10-21.md`. Glosario: `docs/ux/GLOSARIO.md`.

Decisiones del usuario:
- en pantalla, **solo nombres claros, sin códigos L-x** (quedan en los Excel y en la página de Ayuda);
- ayuda contextual «?» y glosario;
- prioridad para Administrador y SuperAdmin.

| Fase | Qué se hizo |
|---|---|
| 1. Orientación y navegación | **Menú por tareas**: Inicio · Mantenimiento de equipos (Gestiones, Mantenimiento preventivo, Mantenimiento correctivo, Reportes, Registros) · Equipos e inventario · Planificación académica · Configuración · Ayuda. Sin «Procesos» duplicado; menú distinto por rol; Reportes visible también para el Supervisor; iconos que existen en la fuente del tema. |
| | **Inicio** redirigido en el servidor: sin parpadeo y el botón «atrás» ya no queda en bucle. |
| | **Permisos**: nueva pantalla «Acceso denegado» (antes /Error con «registro histórico incompleto»); «Mi perfil» para todos los roles; botones y enlaces solo para administradores ocultos al Supervisor. |
| | **Títulos y ubicación**: las 139 páginas tienen título y «Sección ›»; sin títulos duplicados; textos de «Volver» unificados; búsqueda global falsa retirada; icono «?» en la barra superior. |
| | **SweetAlert**: `swal-compat.js` hace que las llamadas con `icon:` muestren el icono (v7 solo entiende `type:`). |
| | **Asistente**: el correctivo ya no muestra «¡Ronda Finalizada!» falso; el paso de verificación ya no esconde laboratorios por la paginación. |
| 2. Lenguaje y ayuda | **Renombrado** según el glosario en el mantenimiento (asistente, gestiones, verificación, solicitud, mantenimiento, salida, cierre, compra, equipos y reportes) y en planificación (entrega de material, incidente de material, semestre académico, suspender día, reprogramar, laboratorio en lugar de ambiente), más los mensajes de servicios y guardas. **Unos 400 textos** en total, sin cambios de lógica. |
| | Los mensajes ya no muestran identificadores internos (`ManagementPlanId`, «Req ID»). Las guardas indican dónde está «Rehacer paso anterior». |
| | **Español**: mensajes de validación (`validation-es.js`) y Select2 (`select2-es.js`, 22 páginas). |
| | **Ayuda**: `/Ayuda` con guía por rol, glosario con buscador (incluye el formato institucional de cada término) y preguntas frecuentes; recuadro «¿Qué hago aquí?» plegable en 14 pantallas prioritarias; icono «?» con explicación junto a conceptos difíciles (estado de verificación, checklist técnico, costo real, código de inversión, centro de costos, tareas). Textos en `Helpers/HelpContent.cs`. |

- **Verificación** (copia desechable de REAL, eliminada al terminar):
  - build sin errores y **402/402 tests** (se actualizaron los textos esperados);
  - recorrido nuevo `Tools/E2E/http_cases_ux_navegacion.py`: **329/329**. Cuatro roles, 30 entradas de menú, 15 pasos del asistente; sin /Error, todas las pestañas con nombre, sin códigos ni jerga visibles y sin menús duplicados;
  - **regresión E2E completa: ninguna falla nueva frente a la línea base** (se actualizaron los textos esperados de 20 scripts). Las fallas previas se deben a datos nuevos de REAL desde el 18/10 (`checks_qa_clasificacion` 7, `gestiones` 2, `usuarios_auth` se cae); `checks_academico` pasó de 3 fallas a 0;
  - `final_verify.py` 31/31 y `sql_invariantes` 21/21;
  - capturas `outputs/qa-usuario/img/ux26_*.png`.
- **Pendiente** (fases 3 a 6 de la auditoría):
  - Stepper con el estado real.
  - Cierre del mantenimiento que repite datos del registro.
  - Solicitud de compra obligatoria aunque no haya compras.
  - Checklist con casillas.
  - «Qué sigue» al aprobar una planificación.
  - Borradores visibles.
  - Capacidad del laboratorio editable.
  - Componentes comunes, confirmaciones, doble envío.
  - Contraste y accesibilidad.
  - Manual por rol y prueba con usuarios reales.
- **Fuera de alcance**: «Inventario general» todavía se titula «Ver Inventario» (`Pages/AssetView`, otro trabajo), y en Verificación masiva el «Estado actual» sigue en inglés («Operational»), punto M1 de la fase 3.

## 4.7 Cambios PENDIENTES DE REVISIÓN DEL USUARIO (no aceptados todavía)

> **Actualización 2026-10-19:** el usuario pidió hacer commit de todo y subirlo a la rama `qa`, así que los archivos de la tabla (a) ya están en el commit de QA. Lo que sigue queda como historia.

El usuario **todavía no revisó ni aceptó** los cambios de esta sesión. El commit de lo que quede sin commit lo hará otro agente **después de esa revisión**. Estado exacto al cerrar el 2026-10-18:

**a) Sin commit, a pedido explícito del usuario** («no quiero que hagas el commit»):

| Archivo | Qué es | Por qué se creó o cambió |
|---|---|---|
| `docs/INFORME_ESTADO_FUNCIONAL_2026-10-18.md` | Este informe | El usuario pidió un informe de «qué debería funcionar, cómo se probó y qué está mal». |
| `docs/GUIA_NUEVA_CONVERSACION.md` | Guía y prompt para empezar una conversación de cero | El usuario pidió darle a la próxima conversación el contexto y las herramientas para hacer mejoras o correcciones. |
| `Tools/E2E/run_scripts.sh` | Ejecutor de los scripts E2E (copia del que estaba en `outputs/`, con la ruta raíz relativa) | `outputs/` está ignorado por git; sin esta copia, la próxima conversación no tendría el ejecutor. |
| `Tools/E2E/stop_app.ps1` | Detiene la app del puerto 5000 | Lo usa la guía; antes solo existía en un archivo temporal. |
| `docs/INIT_CONTINUACION_BASE_REAL.md` (modificado) | Nota al inicio que apunta a la guía y al informe | Para que quien abra el INIT viejo llegue a los documentos nuevos. |

**Qué revisar:**
- si el informe refleja bien el estado;
- si la guía es correcta y suficiente;
- si quieren los dos scripts en el repositorio.

Si algo no se acepta, basta con borrar o cambiar el archivo: ninguno afecta a la app.

**b) Ya hechos en esta sesión a pedido del usuario** (también conviene revisarlos):
- Commits **locales**, sin push: `256256a` (QA funcional y correcciones A1–A4: 132 archivos de código, migración, tests y scripts) y `d21137c` (nota en el INIT).
  - Para no aceptarlos, se pueden revertir con `git revert` o descartar antes de cualquier push.
- Migración `20261018152946_CorrectiveCoexistenceAndL7Checklist` **aplicada en la base REAL** (solo índices y largo de una columna, sin cambios de datos).
  - Esto **no se deshace con git**. Para volver atrás hay que restaurar el respaldo previo `C:\Users\Public\BackupsUnivalle\REAL_PRE_MIGRACION_20261018_CorrectiveCoexistence.bak`, o aplicar el `Down` de la migración con `dotnet ef database update 20261014220842_RelaxPlanningChecksForExcelFidelity`.
  - Si se vuelve atrás, el código de `256256a` deja de coincidir con la base: código y base deben ir juntos.

---

## 5. Cómo comprobarlo tú mismo en el navegador (unos 30 minutos)

Hazlo en una **copia** si no quieres dejar datos de prueba en la base REAL (ver `docs/GUIA_NUEVA_CONVERSACION.md`, «Crear la copia de QA»). Si lo haces en la base REAL, lo que crees queda como dato real.

1. Entra como administrador → **Gestiones** → crea `CORR-2026-2` (Correctiva, semestre 2). Debe aparecer activa **junto** a la preventiva, si la hay.
2. **Procesos → Solicitudes de Servicio (L-7) → Nueva solicitud.** Debe llevarte al asistente en el paso L-7, con el aviso «Iniciar mantenimiento correctivo».
3. Registra una falla de una unidad. Vuelve a intentarlo con **la misma unidad**: debe avisar «ya tiene un proceso correctivo abierto».
4. Avanza esa unidad por L-8 (pon una línea de costo con cantidad 0: debe **rechazarla con un mensaje**), L-3, Kardex y L-12 → debe aparecer en **Completados**.
5. Desde Completados, usa **«Rehacer»** en Kardex: la unidad debe volver a «En Préstamo».
6. **Informes:** descarga el L-8 y el L-48 de ese laboratorio. El costo y la gestión deben coincidir con la pantalla, y en el L-48 deben aparecer todas las unidades.
7. **Gestión → Calendario:** el evento aparece en su fecha. En la correctiva no hay sombreado de semanas.
8. Entra como **Supervisor**: el menú no muestra Usuarios, Catálogos ni Gestiones, y «Man. Correctivos» lleva al asistente.
9. Escribe mal la contraseña 5 veces con un usuario de prueba: debe salir «cuenta bloqueada», no una página de error.
10. **Países →** intenta dar de baja Bolivia: debe bloquearse porque tiene ciudades activas.
11. **Existencias →** cambia de página y de laboratorio: los totales cambian y no hay páginas vacías.

Si algo de esta lista no se comporta así, es un defecto: anótalo y pásalo a la siguiente conversación.
