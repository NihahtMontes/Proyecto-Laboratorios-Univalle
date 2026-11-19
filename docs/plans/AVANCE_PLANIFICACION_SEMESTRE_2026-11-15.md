# Avance: planificación del semestre (qué se hizo y ojito con qué)

Fecha: 2026-11-15 · Rama `exp/docente-v13` · Plan: `PLAN_PLANIFICACION_SEMESTRE_2026-11-15.md`.

## Estado

- Compila sin errores y pasan **531 de 531** pruebas automáticas.
- **Probado en navegador** (Chromium) sobre la copia EXP, en escritorio y en teléfono:
  - recorrido nuevo `Tools/E2E/planificacion_semestre.mjs`: **46 de 46**;
  - recorridos anteriores del docente: casos 23 de 23, extra 22 de 22 y flujo 12 de 12.
- Capturas en `outputs/e2e/planificacion_semestre`.
- Migración nueva `20261116004453_DayRecoveryReasonCategory`, aplicada **solo en EXP**. REAL y la nube no la tienen.
- En EXP se creó la cuenta de prueba `super.qa` (SuperAdmin, misma contraseña que `admin.qa`).
- El recorrido nuevo deja la copia como estaba: restaura lo que mueve y da de baja lo que crea.

## Qué se hizo

| Fase | Hecho |
|---|---|
| 0 | Commit de la QA del 15/11. |
| 1 Motivos | Aviso de 5 palabras con «Guardar igual» (`wwwroot/js/motivo.js`) en reprogramar, reponer y suspender. Categoría obligatoria al suspender un día con reposición. Filtro por categoría en Reposiciones. Bloqueo: no se reprograma con material entregado. Banda azul (reprogramar) y naranja (reponer) en la Grilla, con resumen antes → después. |
| 2 Bandeja | Pestañas Por revisar, Observadas, Aprobadas, Rechazadas, Retiradas y Todas, con contadores. Filtros por docente, materia, laboratorio, fechas y buscador. |
| 3 Docente | «Consultar disponibilidad» tiene vista por semana académica: laboratorios × días × periodos, con Libre, Ocupado y Tuya. Una celda libre abre la solicitud precargada. |
| 5 Excel | Página «Cargar planificación (Excel)» (`/LaboratoryOperations/Importar`, solo SuperAdmin): plantilla con listas desplegables, revisión fila por fila, duplicados, «Aquí ya está» con enlace a la grilla, sobrescribir u omitir por fila o en bloque, Excel de resultado y carga todo o nada. |
| 6 Avisos | El docente recibe aviso al reprogramar o reponer su práctica y al suspenderse su día. |

## Ojito

1. **Aviso emergente del panel.** Al abrir el Panel de planificación, el aviso de conflictos del día aparece encima del botón «Suspender día» y tapa el clic unos segundos. Ya estaba así; conviene moverlo de esquina.
2. **Fase 4 (Grilla): se hizo menos de lo planeado, porque ya existía.** La Grilla ya tenía arrastre, verde/rojo por horario del grupo, «Tomar bloque» (sirve en teléfono) y el botón «Reprogramar» en la lista de planificaciones. Se agregaron las bandas azul/naranja y el resumen antes → después. No se agregó «Reprogramar» desde el calendario ni se probó el arrastre en teléfono.
3. **Reprogramar con material entregado** se bloquea solo en la reprogramación manual. La reposición de un día suspendido sigue permitida aunque haya entrega.
4. **Una práctica Finalizada sin salida registrada** (las históricas importadas) todavía se puede reprogramar, con la confirmación de siempre.
5. **Excel, sobrescribir:** mueve fecha, horario, laboratorio y docente, pero **conserva el material que ya tenía** y su estado. Solo carga la receta si no tenía material.
6. **Excel, lo cargado queda Aprobada** sin asignar origen por línea; el origen se elige al entregar.
7. **Excel, docente que no existe:** se marca error y sugiere nombres parecidos. No se crea solo.
8. **Excel, choque de docente:** bloquea la fila. No hay opción de aceptarlo como en la planificación manual.
9. **Excel, no revisa stock de material** fila por fila. Después de cargar hay que mirar «Conflictos de material».
10. **Excel, sin bloqueo de concurrencia:** si alguien planifica a mano mientras se carga, la revisión final dentro de la transacción lo detecta casi siempre, pero no hay candado.
11. **La revisión del Excel vive una hora** en memoria del servidor. Si se reinicia la app hay que subir el archivo otra vez.
12. **Suspender un día sin prácticas** (calendario académico) no pide categoría, solo el motivo.
13. **Antes de publicar en la nube:** aplicar las dos migraciones del 15/11 en `db70630` (falta `.bak` y contraseña rotada).

## Falta

- Revisión con Fernando y Danilo, y luego unir a `qa`.
- Probar la carga con el Excel real de la Inge (aquí se probó con 5 filas de ejemplo).
- Publicar en la nube (punto 13).
- En el Excel, el aviso «ni la fila ni el grupo tienen número de estudiantes» sale en casi todas las filas, porque los grupos de la gestión no lo tienen cargado. Conviene cargarlo en Grupos antes de importar.
