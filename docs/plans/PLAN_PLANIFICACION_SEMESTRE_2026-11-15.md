# Plan: planificación del semestre (Excel, reprogramación, reposición, bandeja y docente)

Fecha: 2026-11-15 · Rama base: `exp/docente-v13` · Pruebas solo sobre `DB_Laboratorios_Univalle_EXP`.
Ejecución: Claude orquesta, revisa, prueba en navegador y verifica; Fable implementa por fase, cada trabajador con archivos distintos.

## Decisiones del usuario (2026-11-15)

- Por ahora todo lo opera el **SuperAdmin**. Los roles se definen después, durante las pruebas.
- Motivo de una **reprogramación**: texto libre y **obligatorio**. Cualquier texto vale, incluso «0». Si tiene menos de 5 palabras se muestra el aviso «Se recomienda escribir al menos 5 palabras para que la descripción sea precisa» con la opción de guardar igual.
- Motivo de una **reposición** (día suspendido): **categoría de una lista** (sirve para filtrar) **más el texto libre** obligatorio, con el mismo aviso de 5 palabras.
- **No se reprograma una práctica con material entregado**, porque ya está en ejecución. Hoy `ReschedulingService` no lo bloquea, así que hay que agregar el bloqueo.
- La reposición debe caer **dentro de la gestión** (calendario académico de la gestión: desde-hasta). Nunca fuera.
- El Excel lo carga **la Inge**, responsable de todo, **una vez por semestre**, al inicio. **Todas las prácticas traen fecha.** El docente se identifica **por nombre**.
- El importador debe **prever todo**: duplicados, conflictos y datos que no existen. Lo hace con alertas paso a paso: señala la fila, muestra «aquí ya está» con enlace al registro existente y ofrece **sobrescribir u omitir** fila por fila.
- El Excel **no trae** reprogramaciones ni reposiciones. Esas las gestiona el sistema después.
- Avisos **solo dentro del sistema**.
- El docente puede ver la ocupación **por semana académica**: laboratorios × periodos, ocupado o libre, y sus propias clases resaltadas.

## Estado actual (verificado en el código)

| Tema | Existe | Falta |
|---|---|---|
| Reprogramar (una clase) | Arrastrar en `Grilla`, solo SuperAdmin; motivo obligatorio de hasta 500 caracteres | Distinción visual clara, aviso de 5 palabras, bloqueo con material entregado, destinos verde/rojo, resumen antes/después |
| Reponer (día entero) | `DayRecoveryService`, bandeja `Reposiciones`, modo reposición en `Grilla`; motivo de hasta 200 caracteres | Categoría de motivo, aviso de 5 palabras, tarjetas de pendientes, pantalla que no se confunda con reprogramar |
| Bandeja de docentes | `Solicitudes/Index` con Por revisar, Observadas y Resueltas | Aprobadas y Rechazadas separadas, Retiradas, Todas, filtros, buscador, historial, globito en el menú |
| Docente: disponibilidad | `Docente/Disponibilidad` por **día** y `Docente/Calendario` (sus prácticas) | Vista **por semana académica** con laboratorios × periodos × días y sus clases resaltadas |
| Excel de planificación | No existe | Plantilla, importador con vista previa, resolución de duplicados y conflictos |
| Avisos | `Notifications` (dentro del sistema) | Avisos de aprobación, observación, rechazo, reprogramación y suspensión al docente |

## Fases

### Fase 0: cerrar la QA del 15/11
Commit en `exp/docente-v13` (StudentCount, incidencias por origen, teléfono, E2E extra), sin coautor.

### Fase 1: motivos y distinción entre reprogramar y reponer
- **Componente compartido de motivo** (JS más validación del servidor):
  - vacío: error y no se guarda;
  - menos de 5 palabras: SweetAlert v7 con «Se recomienda escribir al menos 5 palabras para que la descripción sea precisa», «Guardar igual» o «Corregir».
- **Reposición:** nueva columna `DayRecovery.ReasonCategory`, que exige una migración nueva y nullable para los registros antiguos. Valores: Feriado, Paro o movilización, Corte de servicios, Evento institucional, Clima o emergencia, Otro. El filtro por categoría va en `Reposiciones`.
- **Bloqueo:** en `ReschedulingService`, no se reprograma si hay una salida (L-3) no anulada. El mensaje dice «La práctica ya tiene material entregado (en ejecución); no se puede reprogramar».
- **Distinción visual** en `Grilla`:
  - banda azul «Estás REPROGRAMANDO una clase»;
  - banda naranja «Estás REPONIENDO el día dd/MM (motivo) · quedan N de M».
- **Historial:** la planificación muestra «Reprogramada» o «Repuesta (día dd/MM)», con quién, cuándo y motivo.
- **Pruebas:** unitarias (motivo vacío, 1 palabra o 5 palabras; material entregado; fecha fuera de la gestión) y E2E de los dos flujos.

### Fase 2: bandeja de solicitudes de docentes
- **Pestañas:** Por revisar · Observadas · Aprobadas · Rechazadas · Retiradas · Todas, cada una con su contador.
- **Filtros:** docente (nombre), materia, laboratorio, rango de fechas y buscador por código o práctica. Los filtros se conservan en la URL.
- **Fila:** docente, práctica y grupo, fecha y periodos, laboratorio, n.º de líneas de material y urgencia.
- **Detalle:** línea de tiempo con enviada, observada (con nota), reenviada, aprobada o rechazada (con motivo).
- **Menú lateral:** globito con las solicitudes por revisar.
- **Pruebas:** E2E por pestaña y filtro.

### Fase 3: el docente ve la semana académica
- `Docente/Disponibilidad` suma la **vista Semana**: se elige la semana académica (Semana 5 · 16–21/11) y se ve una grilla de laboratorios × días × periodos.
  - **Libre**, **Ocupado** (sin datos de otros docentes) y **Mía** (enlace a su solicitud).
  - Día suspendido en gris con su motivo.
- Desde una celda libre: «Solicitar aquí», que abre `NuevaSolicitud` con fecha, periodo y laboratorio ya cargados.
- También en el teléfono: selector de día y lista de periodos.

### Fase 4: la Grilla más clara (la más grande de diseño)
- **Al arrastrar:**
  - celdas **verdes** si están libres y **rojas** si chocan;
  - el tooltip explica el choque: laboratorio, docente, estudiantes, material o día suspendido.
- **Resumen antes de guardar:** «Jue 19 · P3–4 · Lab 1 → Vie 20 · P1–2 · Lab 2», más el motivo (Fase 1).
- **Reposición:** panel lateral con **tarjetas** de las clases pendientes del día suspendido; se arrastran una por una. El contador baja y al final aparece «Día repuesto».
- **Leyenda fija** de colores y estados.
- **Navegación:**
  - botón «Reprogramar» en la planificación, el calendario y la lista;
  - miga de pan que dice dónde estás;
  - «Volver» regresa al lugar de origen.
- **Teléfono:** tocar el bloque y luego tocar el destino (sin arrastrar).
- **Límite de fechas:** solo dentro del calendario de la gestión; fuera de ella las celdas se ven deshabilitadas.

### Fase 5: Excel de planificación del semestre
**Plantilla** (se descarga desde el sistema, con EPPlus):
1. `Instrucciones`: cómo llenarla, un ejemplo y las reglas.
2. `Programacion`: una fila por práctica, con estas columnas:

   | Columna | Notas |
   |---|---|
   | Gestión | |
   | Materia | |
   | Grupo | |
   | Práctica | |
   | Semana académica | se puede dejar vacía: el sistema la calcula y avisa si no coincide con la fecha |
   | Fecha | obligatoria |
   | Periodo inicial | |
   | N.º de periodos | |
   | Laboratorio | |
   | Docente (nombre) | |
   | N.º de estudiantes | |
   | Observaciones | |

   Las listas desplegables salen de la hoja Listas.
3. `Listas`: materias, grupos, prácticas por materia, laboratorios, docentes y periodos, tomadas del sistema en ese momento.
4. (Opcional) `Material_extra`: N.º de fila de Programación, artículo y cantidad, para material fuera de la receta.

El material de cada práctica sale de su receta × estudiantes ÷ factor del grupo, como ya hace el sistema.

**Importador** (página nueva en el menú de planificación, solo SuperAdmin), un asistente paso a paso:
1. **Subir** el archivo. Se guarda el hash y quién lo subió, para trazabilidad (`ImportBatch`/`ImportSourceRow` ya existen).
2. **Revisión automática** fila por fila, con tres niveles:
   - **Error** (bloquea la fila): la materia, el grupo, la práctica, el laboratorio o el docente no existen; la fecha está fuera de la gestión o cae en domingo; los periodos no existen.
   - **Choque** (bloquea hasta resolverlo): laboratorio ocupado, docente con otra clase, estudiantes con otra materia, o día suspendido.
   - **Aviso** (no bloquea): material insuficiente en ese horario, la semana no coincide con la fecha, o el nombre de docente se parece a otro (sugerencia).
3. **Duplicados:**
   - **Dentro del Excel:** la misma gestión + grupo + práctica + fecha + periodo marca las dos filas.
   - **Contra el sistema:** «Esta práctica ya está cargada: PRA-00123» con enlace. Por fila se elige **Sobrescribir** u **Omitir**, o se aplica la misma decisión a todas.
4. **Vista previa final:** N nuevas, N a sobrescribir, N omitidas y N con error. Cada error trae el n.º de fila de Excel, la columna y cómo corregirlo. Se puede **descargar el Excel con una columna «Resultado»** para corregirlo y volver a subirlo.
5. **Confirmar:** todo en una transacción. Si algo falla, no se carga nada. Se crean `PracticeSchedule` + `Planning` (estado **Aprobada**) + requerimientos desde la receta.
6. **Resultado:** resumen con enlace a la grilla de la semana 1.

**Pruebas:** unitarias por cada regla, Excel de ejemplo con todos los casos (bueno, duplicado, choque, nombre mal escrito) y E2E del asistente completo.

### Fase 6: avisos dentro del sistema
El docente recibe un aviso cuando:
- su solicitud se aprueba, se observa o se rechaza;
- su clase se reprograma (con el motivo);
- se suspende un día en que tenía práctica (con la categoría y el motivo);
- su práctica se repone.

La campana del layout muestra el contador, y cada aviso enlaza a la solicitud o práctica.

### Fase 7: QA final
- Build, todas las pruebas y E2E de cada fase en el escritorio y en el teléfono, sobre EXP.
- Informe con capturas para la revisión con Fernando y Danilo, y luego la unión a `qa`.

## Orden y dependencias
0 → 1 → 2 → 3 → 4 → 5 → 6 → 7. Las fases 2 y 3 pueden ir en paralelo porque usan archivos distintos. La 5 no depende de las demás, pero conviene hacerla después de la 1, por las reglas de fechas de la gestión.

## Dudas abiertas
1. **Estado al importar:** se propone **Aprobada** directamente, porque la Inge ya lo tiene todo planificado.
2. **Docente que no existe en el sistema:** ¿error (hay que crearlo antes) o se crea desde el importador?
3. **Sobrescribir una práctica que ya tiene material entregado o está finalizada:** se propone no permitirlo nunca y solo ofrecer omitirla.
4. **Categorías de reposición:** ¿sirve la lista propuesta?
