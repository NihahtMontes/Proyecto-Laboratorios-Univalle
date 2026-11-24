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
| 2 | Limpieza de 10 min (12:05–12:15, 13:55–14:05) | ✅ | La regla permite prácticas pegadas. La franja muestra la hora real: el receso si existe, o los últimos 10 minutos de la práctica. |
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
| 3 | Un L-3 por práctica, siempre, separado en insumos y utensilios | ❌ por revisar | La solicitud del docente tiene un solo L-3 con todos los artículos. No se separa en insumos y utensilios. |
| 4 | Reporte de cambios del docente a su material («Pedro Guereca modificó 8 veces su material este mes»; 60 observaciones en el último mes) | ❌ | **No estaba en el plan; se agrega ahora.** El sistema no guarda el historial de esos cambios, así que hay que diseñarlo. Va después del trabajador C (usa la misma página de Reportes). |
| 5 | Calendario por laboratorio con todas las actividades (prácticas, reprogramaciones y reservas de estudiantes) | ✅ | La grilla única muestra las prácticas de cualquier gestión, los cupos de práctica libre y las marcas de reprogramación y reposición. |
| 6 | Misceláneos (papel absorbente y similares) en otro formulario | ❌ ⏸ | Falta saber qué formulario es y qué datos lleva. |

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
- Actualizar `docente_casos.mjs` para que no dependa del 27/11.
- Puntos ❌ de la sección 3.
