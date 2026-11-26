# Tablero único de pendientes (2026-11-24)

Un solo lugar con **todo lo que pidió el usuario**, de cada fuente, y en qué estado está. Se revisa al empezar cada sesión y se actualiza al terminar cada tarea.

**Fuentes**
- Capturas «Mejoras Final Sprint».
- Excel del generador de la Inge (`outputs/insumos-2026-11-23/GENERADOR_PEDIDO_1-2027.xlsm`).
- PDF de la conversación con la Inge (`outputs/insumos-2026-11-23/Correcciones_Inge.pdf` / `.txt`).
- Pedidos en el chat.

**Estados:** ✅ hecho · 🔄 en curso · ⏸ espera una decisión · ❌ sin empezar · 🚫 fuera de alcance por decisión del usuario.

## 1. Capturas «Mejoras Final Sprint»

| # | Pedido | Estado | Detalle |
|---|---|---|---|
| 1 | La grilla del H-05 debe verse igual para el docente y para el SuperAdmin («global» para todos) | ✅ | `ILabWeekOccupancyService` (fuente única: cualquier gestión + cupos de práctica libre) para la grilla del admin, Disponibilidad, NuevaSolicitud y reprogramar o reponer. E2E `grilla_unica.mjs`: 11/11, con las mismas celdas ocupadas para el admin y el docente. |
| 2 | Limpieza de 10 min (12:05–12:15, 13:55–14:05) | ✅ | La regla permite prácticas pegadas. La franja muestra la hora real: el receso si existe, o los últimos 10 minutos de la práctica. **24/11:** se comprobó 13:55–14:05 con una práctica que termina en P7 (E2E y captura al usuario). Si dos prácticas comparten el día, la hora baja a dos líneas en vez de cortarse. En 2026 hubo 60 prácticas que terminan en P7. Las de P7 a P10 (215) siguen después de las 13:55, así que su limpieza va al final. |
| 3 | «Tuya» → nombre de la práctica | ✅ | Comprobado en el E2E: ya no aparece «Tuya». |
| 4 | Material por periodos: de qué hora a qué hora | ✅ | Conflictos muestra la franja de cada práctica en el panel («07:35–09:25 · P1–P2») y el periodo con su hora en cada chip. |
| 5 | Al reprogramar o reponer, que se agregue a los detalles del L-9 | ✅ | Reporte nuevo «L-9 · Reprogramaciones y reposiciones» en Reportes (Excel, filtro por gestión y fechas). Probado con datos reales de 2026-2. Cuando la Inge pase el formato oficial, se ajusta. |

## 2. Excel del generador (planificación 2027-1)

| Pedido | Estado | Detalle |
|---|---|---|
| Cargar la planificación en la base | ✅ | En EXP hay 422 prácticas aprobadas (todas las del Excel), con 0 choques. |
| Corregir la tabla de semanas (semana 6 = 29/03; las 3 de la semana 20 pasan a la 19) | ✅ | Recargado en EXP el 23/11, con respaldo previo. |
| Ubicar las 11 filas que no entraron (IGA3G8-A con varias prácticas el mismo día; choques de Pedro Guereca) | ✅ | Planificador nuevo (sesión compartida del grupo y reacomodo del día por docente), hecho por GLM-5.3 y revisado por Claude. Recargado en EXP el 24/11 con respaldo previo `EXP_PRE_PLANIFICADOR_20261124.bak`: **422/422 aprobadas**, 0 choques de laboratorio y 0 de docente. |
| Recetas, PRODUCTO (1.121 insumos con costos) y COSTOMATERIA | 🚫 | El usuario eligió «Solo planificación». |
| Datos sucios del Excel (314 insumos inactivos, código 50258 duplicado, unidades distintas, hojas vacías) | ⏸ | Informados. Se corrigen en el Excel de la Inge. |
| ZABALA figura como dos personas | ⏸ | Hay que elegir una en la vista previa de la importación. |

## 3. PDF con la Inge (Correcciones_Inge)

| # | Pedido | Estado | Detalle |
|---|---|---|---|
| 1 | Los estudiantes reservan práctica libre (hasta 6 por laboratorio; la Inge aprueba; faltas y sanción) | ✅ | Fase 2. La migración se aplicó en EXP el 24/11. |
| 2 | Práctica para el examen de grado (mínimo 3 veces, menú de tres tiempos, periodo de decoración) | 🚫 | El usuario lo dejó para después. |
| 3 | Un L-3 por práctica, siempre, separado en insumos y utensilios | ⏸ | Los insumos son la comida del menú y el sistema no los tiene: están en las recetas del Excel, que el usuario dejó fuera («solo planificación»). Se hace cuando se carguen las recetas, después de limpiar esos datos con la Inge. |
| 4 | Reporte de cambios del docente a su material («Pedro Guereca modificó 8 veces su material este mes»; 60 observaciones en el último mes) | ✅ | **Reporte (24/11, `d0dfe01`).** Compara cada solicitud del portal con el kit escalado y descuenta los ajustes de la Inge. Separa los cambios de **cantidad** de los de **artículo**. **Llamada de atención:** 3 o más artículos agregados o quitados en una práctica. **Último momento:** menos de 7 días de anticipación. **«Pedir cambio de material» (24/11, `21b8304`…`84509bb`).** El docente pide cambiar el L-3 de una práctica aprobada (las 422 de 2027-1 incluidas) con un motivo y la Inge aprueba o rechaza desde «Cambios de material». La práctica sigue aprobada, todo queda en el historial y el reporte muestra los pedidos como «Cambio de L-3». Migración mínima `WidenPlanningReviewActions` aplicada en EXP con respaldo `EXP_PRE_CAMBIO_MATERIAL_20261124.bak`. |
| 5 | Calendario por laboratorio con todas las actividades (prácticas, reprogramaciones y reservas de estudiantes) | ✅ | La grilla única muestra las prácticas de cualquier gestión, los cupos de práctica libre y las marcas de reprogramación y reposición. |
| 6 | Misceláneos (papel absorbente y similares) en otro formulario | ⏸ | En el audio (pág. 14 del PDF), la Inge dice que los misceláneos (papeles absorbentes y similares) los entrega con **otro formulario**, aparte del L-3. Lo más parecido en el sistema son 16 artículos de «Material operativo» marcados como consumibles (papel film, papel aluminio, papel manteca, bolsas…), que hoy van dentro del L-3. **Falta que la Inge pase una foto de su formulario.** |

## 4. Preguntas abiertas para la Inge
- ¿La misma letra de grupo en materias distintas son los mismos estudiantes?
- ¿Los grupos que caen el mismo día en el mismo laboratorio comparten el laboratorio al mismo tiempo, o van por turnos? Hoy se cargan por turnos.
- ¿Las prácticas del mismo grupo el mismo día (IGA3G8-A) van en una sola sesión? Hoy se reparte su bloque entre ellas.

## 5. Herramientas
- **ECC** (Everything Claude Code v2.2.3) está instalado, pero la sesión del 23–24/11 no lo cargó, porque se instaló con la sesión ya abierta. Se activa abriendo Claude Code de nuevo en este proyecto (`claude --continue`). Conviene esperar a que terminen los trabajadores GLM, porque al cerrar la sesión se cortan. Uso previsto:
  - `csharp-reviewer` y `database-reviewer` para revisar los diffs;
  - `e2e-runner` para los recorridos.
- **GLM:** un trabajador por tarea, con archivos distintos y su propia carpeta de compilación (`obj/wk-<nombre>`). Claude revisa cada diff y corre las pruebas.
- **Prácticas de ECC adoptadas sin reiniciar** (24/11, el usuario está en remoto): `.agents/skills/verificacion-ecc/SKILL.md` adapta `csharp-reviewer`, `silent-failure-hunter` y `verification-loop` a .NET y a este proyecto. La skill `trabajador-glm` la exige antes de entregar, y Claude la usa para revisar cada diff. El plugin completo se activa la próxima vez que se abra Claude Code en la PC.

## 6. Pruebas del 24/11
Se corrieron sobre una copia de EXP, nunca sobre EXP.

**Unitarias**
- 646/646.
- Build sin errores.

**E2E**
- `grilla_unica.mjs` (nuevo): 11/11.
- `planificacion_semestre.mjs`: 46/46.
- `docente_extra.mjs`: 22/22.
- `docente_flow.mjs`: 12/12.
- `docente_inicio.mjs`: 32/32.
- `http_cases_planificacion.py`: 35/35.
- `docente_casos.mjs`: 21/22. Falla «Por solicitar» porque el script cancela una práctica fija del 27/11 que no existe en EXP. La misma función quedó probada en `docente_extra`.
- Recorrido de estudiantes: todas las acciones verificadas en la base.

**Errores corregidos**
- **S20 (de octubre):** dos envíos simultáneos de «Registrar salida» creaban dos salidas. Ahora `L3CircuitService` toma el bloqueo de la planificación y vuelve a revisar dentro de la transacción.
- Insignia morada en «Mi semana» del docente.

**E2E actualizados**
- Regla de limpieza: una práctica pegada se acepta.
- Choque de docente bloqueado (decisión del 21/10).
- `/AccesoDenegado`.
- Supervisor configurable por variable de entorno.
- Fila «nueva» de la importación: IGA3G8-C ya tiene todo planificado desde el 18/11.

**Pendiente**
- Refactorizar `GeneratorPeriodPlanner.Plan` en métodos más chicos (aviso MEDIO de la revisión).
- ~~Actualizar `docente_casos.mjs` para que no dependa del 27/11~~ (hecho el 24/11).

## 7. Ronda del 24/11 (mañana)

**Commits locales en sprint-3, sin push (el repositorio es público):**
- `9959dd7` a `c1ddaee`: lo probado el 23 y 24/11, en 7 commits por tema.
- `d0dfe01`: reporte de cambios de material.
- `8c3daa1`: hora de limpieza en bloques angostos.
- `f0e2e35`: E2E.

**Pruebas (sobre una copia de EXP recién restaurada)**
- 656/656 unitarias.
- `docente_casos` 23/23 (ya no depende del 27/11).
- `grilla_unica` 13/13 (con la limpieza de 13:55 y la hora visible).
- `http_cases_planificacion` 35/35.
- Descarga del reporte desde la app: 2 docentes, 10 solicitudes, 127 cambios; 2 filas verificadas a mano contra la base.

**Bases de datos (al 24/11)**

| Base | Qué es | Usuarios | Prácticas | Migraciones |
|---|---|---|---|---|
| REAL | La original, desde el Excel | 3 | 1.038 | 32 |
| NUBE_20261103 | Foto de la nube del 03/11 (REAL + 3 usuarios) | 6 | 1.038 | 32 |
| EXP | Copia del Sprint 3 | 58 | 1.643 | 38 (desde el mediodía del 24/11) |

- EXP tiene, además: 5 migraciones (portal docente, consumibles, número de estudiantes, motivo de reposición y estudiantes), 2027-1 con 422 prácticas y 183 prácticas de prueba de 2026-2 (12–20/11).
- Para pasar algo a REAL o a la nube no se copia EXP: se aplican las migraciones y se vuelve a importar el Excel 2027-1.
- Las copias desechables `EXP_IMPORT_PRUEBA` y `QA_E2E` se borraron al terminar.

## 8. «Pedir cambio de material» (24/11, mediodía)

**Commits locales en sprint-3, sin push:**
- `21b8304`: acciones 8-11 y migración mínima.
- `b1bb350`: servicio y pruebas.
- `7dc4306`: el reporte suma los cambios de L-3.
- `918dcac`: pantallas del docente y de la Inge.
- `84509bb`: E2E.

**Quién lo hizo**
- El backend lo hizo GLM-5.3 y las pantallas GLM-5.3-Flash; Claude revisó, corrigió e hizo la migración y el E2E.
- Correcciones de Claude:
  - un «agregado» cuyo artículo ya está en el L-3 ajusta la cantidad en vez de duplicar la línea;
  - el estado real queda en el historial al retirar o rechazar;
  - el resumen en vivo del docente leía la cantidad vigente de la celda y no de la fila, y contaba todo como agregado;
  - un artículo agregado desde el buscador arrancaba en 0;
  - se usaba un icono que no existe en Font Awesome 5.0.9.

**Pruebas**
- 678/678 unitarias, con las de aprobación de antes sin cambios.
- E2E `cambio_material` 20/20:
  - pedido con aumento, quitado y agregado;
  - número del menú y bandeja;
  - aprobación, también con faltantes;
  - rechazo y retiro;
  - carrera aprobar/rechazar con una sola decisión;
  - reporte.
- Regresión: `docente_casos` 23/23, `grilla_unica` 13/13 y `http_cases_planificacion` 35/35.

**Bases**
- EXP quedó con 38 migraciones; la regla de acciones del historial ahora acepta 1–11.
- REAL sigue con 32 migraciones.
- La copia QA_E2E se borró al terminar.

**Siguiente / observaciones**
- Al pasar todo a REAL o a la nube, aplicar las 6 migraciones que EXP tiene y REAL no: las 5 de antes más `WidenPlanningReviewActions`.
- ✅ (26/11) Ya existía: el icono `fa-check-double` no está en el Font Awesome del tema y se ve vacío en 12 pantallas (Notificaciones, Kardex, Mantenimientos…). Se cambió por `mdi-check-all`.
- ✅ (26/11) Ya existía: el aviso flotante «Tienes N avisos que requieren atención» tapa 7 s los botones de arriba a la derecha y se queda fijo con el mouse encima. Ahora sale abajo a la derecha.
- Ya existía: `docente_casos.mjs` tiene escritas las claves de las cuentas de prueba. Pasarlas a variables de entorno, como `cambio_material.mjs`.
- Opcional: en el portal docente, escribir la hora de la limpieza en la celda. Hoy es una línea naranja y la hora solo aparece al pasar el mouse.

## 9. Pruebas del equipo en la nube (25–26/11, noche)

Detalle completo: `docs/deployment/PUBLICACION_CORRECCIONES_2026-11-26.md`. Rama `fix/nube-redirecciones-docentes`.

| Pedido o falla | Estado |
|---|---|
| Cuentas de «Cierre de práctica» con cambio de clave obligatorio quedaban en bucle (ERR_TOO_MANY_REDIRECTS) | ✅ publicado |
| Campana con el menú contraído se salía de la pantalla | ✅ publicado |
| Login cortado abajo en laptops y teléfonos | ✅ publicado |
| Crear un docente nuevo desde Usuarios (no estaba en el catálogo) | ✅ publicado |
| Saber dónde se asignan los grupos de un docente y ver sus grupos por gestión | ✅ publicado |
| Enlaces que el Supervisor y el rol de cierre veían pero no podían abrir | ✅ publicado |
| Zona horaria del servidor: confirmar la de MonsterASP y, si no es Bolivia, un reloj único con `America/La_Paz` | ❌ sin empezar |
| Dos fichas para un mismo docente de 2027-1 (una con nombre completo y otra solo con el apellido) | ⏸ unificar a mano o decidir cómo |
| «Asignar el material» que buscaba Danilo: aclarar qué pantalla esperaba | ⏸ espera la respuesta de Danilo |
| Pantalla que parta del docente para asignarle varios grupos a la vez | ⏸ decidir si se hace |
