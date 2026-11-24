# Plan maestro: gestión desde el Excel de la Inge y módulo de estudiantes

**Fecha:** 2026-11-23
**Estado:** decisiones cerradas con el usuario. Pendiente de implementar.

**Orden de trabajo:**
1. **Fase 1:** cargar la gestión 2027-1 desde el generador de la Inge.
2. **Fase 2:** módulo de estudiantes para la práctica libre.
3. **Fase 3:** retomar la grilla única, que está pausada (ver `PLAN_MEJORAS_FINAL_SPRINT_2026-11-23.md`).

**Fuera de alcance por ahora:** la preparación del examen de grado (decoración, menú de tres tiempos), las recetas y los costos del Excel, y el formulario L-9.

**Insumos** (en `outputs/insumos-2026-11-23/`, ignorado por git):
- `GENERADOR_PEDIDO_1-2027.xlsm`: el Excel de la Inge;
- `ANALISIS_GENERADOR_2027.md`: análisis del Excel hecho por GLM;
- `Correcciones_Inge.txt`: transcripción de la conversación con la Inge;
- `docentes_excel.txt`: los 15 docentes que usa PLANIFICA.

---

## Datos verificados (2026-11-23, solo lectura)

| Dato | Valor |
|---|---|
| Materias del Excel en la base (REAL y EXP) | Las 19 existen. |
| Grupos y prácticas de esas materias | 88 grupos y 196 prácticas, de las cuales **180 tienen receta**. Las prácticas usan el mismo número (`1`…`10`, `E1p`, `E2p`, `E3p`) y nombre que el Excel. |
| Docentes de PLANIFICA | Los 15 existen en `PersonAliases`. **ZABALA tiene dos personas**, así que hay que elegirla en la vista previa. |
| Laboratorios | En la base se llaman `H-01`…`H-07` y `K-01`; en el Excel, `H-1`…`H-7` y `K-1`. Se normaliza. La capacidad está vacía (NULL) en todos. |
| Gestión 2027-1 | Ya existe en REAL (Id 27, creada por la importación histórica). En EXP se crea o reutiliza por código. |
| Duración real de una práctica | La mayoría dura 4 periodos (863 de 997). Empieza en P2 o P7 (215 cada uno), en P1 (149) o en P8 (121). |
| Días con varias prácticas en el mismo laboratorio | 2 prácticas: 281 días (mañana y tarde). 3: 28 días. 4 o más: 5 días. |
| Periodos (ClassPeriods) | P1 07:35, P2 08:35, P3 09:25, P4 10:25, P5 11:15, P6 12:15, P7 13:05, P8 14:05, P9 14:55, P10 15:55, P11 16:45 y P12 17:45. Cada periodo dura 50 minutos. Los recesos de 10 minutos van tras P1, P3, P5, P7, P9 y P11. |
| Correo | El sistema no envía correos; solo tiene avisos dentro de la app. |

---

## Fase 1. Cargar la gestión 2027-1 desde el generador

### Decisiones
| Tema | Decisión |
|---|---|
| Base | Primero **una copia de EXP**. Se verifica y después se carga en **EXP**, con respaldo previo y orden explícita del usuario. REAL no se toca. |
| Alcance | Solo la planificación: CONFIG, MATERIAS y PLANIFICA. Las hojas de recetas, PRODUCTO y COSTOMATERIA no se importan todavía. |
| Periodos | 1 grupo en el laboratorio ese día: **P2–P5** (08:35–12:05). 2 grupos: **P2–P5 y P7–P10** (13:05–16:45). 3 grupos: **P1–P4, P5–P8 y P9–P12**, seguidos y con 10 minutos de limpieza. Con 3 grupos o más se muestra el aviso «día cargado», que no bloquea. Con más de 3 grupos, la Inge ubica manualmente los que no entran. |
| Orden en el día | El **orden de las filas de PLANIFICA**. |
| Choque de docente | Si al repartir el docente queda ocupado en otro laboratorio a esa hora, el grupo **pasa al siguiente bloque libre** del laboratorio, y se avisa en la vista previa. |
| Choque de grupo | Se aplica la misma regla que al docente. |
| Limpieza | 10 minutos y sin periodo bloqueado. `PlanningSlot.OverlapsWithGap` pasa a `gap = 0` (es requisito previo). |
| Estado | **Aprobada**, como hace el importador hoy: el docente la ve en su semana. |
| Calendario | **De lunes a sábado en las 20 semanas** de CONFIG, todos habilitados. Los feriados los marca la Inge después en el calendario académico. La semana 6 de CONFIG repite el rango de la semana 3, así que manda la FECHA EJECUCION de cada fila, y se avisa. |
| Material | **La receta del sistema, escalada** (receta × estudiantes ÷ factor). Las 16 prácticas sin receta quedan sin material, y se avisa. |
| Reimportar | **Actualizar sin duplicar.** Cada práctica se reconoce por materia + grupo + número de práctica dentro de la gestión. Si cambió la fecha o el laboratorio, se actualiza y lo nuevo se agrega. Nada se borra. Las prácticas con solicitud, reprogramación, reposición o cierre no se tocan, y se avisa. |

### Trabajo técnico
1. **Regla de limpieza** (`Services/PlanningSlot.cs`): `gap = 0`, revisando sus 5 usos y actualizando las pruebas `PlanningSlotTests` y `Ej03_ReglaHorarioPracticaTests`.
2. **Lector del generador** (`Services/PlanningImport/`, clase nueva). Detecta el formato por la hoja `PLANIFICA` y deja el lector actual para la plantilla vieja. Lee:
   - **CONFIG:** el título con la gestión («1-2027» → `2027-1`), las 20 semanas, docentes y materias habilitadas;
   - **MATERIAS:** materia, grupo, estudiantes, `FACTOR_MATERIA`, día, laboratorio, docente y si es demostrativa;
   - **PLANIFICA:** fila, materia, grupo, práctica (como **texto exacto**: «E1P» no es 1), descripción, semana, FECHA EJECUCION, laboratorio y docente.
3. **Planificador de periodos**, puro y con pruebas. Agrupa por fecha y laboratorio y aplica la regla de bloques, el orden de las filas, el corrimiento por choque de docente o grupo, y el aviso «día cargado».
4. **Creación y actualización** en la gestión elegida:
   - semanas y días académicos que falten;
   - `CourseGroup` de la gestión, con estudiantes, factor, día, laboratorio y docente;
   - `Practice` si falta, buscando el número sin distinguir mayúsculas («E1P» = «E1p»);
   - `PracticeSchedule` y `Planning` (Aprobada) con la receta escalada.

   Se procesa en lotes de 100, limpiando el `ChangeTracker`, y con `ImportBatch` para la trazabilidad.
5. **Vista previa** (`Pages/LaboratoryOperations/Importar`). Muestra totales de lo que se crea, se actualiza o queda igual, y por separado:
   - días cargados y bloques corridos;
   - avisos: semana 6, hojas AYB4T4, TDC1N7 y NAG1N7, prácticas sin receta;
   - selector para la ambigüedad de ZABALA.

   Antes de escribir HTML, CSS o JS se consulta la skill `modern-web-guidance`.
6. **Pruebas:**
   - unitarias del lector (con un Excel mínimo generado en la prueba), del planificador de periodos y de la práctica como texto;
   - integración: importar dos veces seguidas no duplica nada.
7. **Ensayo:**
   - copiar EXP a `DB_Laboratorios_Univalle_EXP_IMPORT_PRUEBA`;
   - importar el Excel real;
   - verificar con SQL los conteos (422 prácticas, 41 grupos);
   - comparar en el navegador la grilla del H-05 en la semana del 09/03/2027 con el Excel.

### Reparto
| Quién | Qué |
|---|---|
| **GLM-5.3 (fuerte)** | Puntos 1 a 4 y 6: regla, lector, planificador, creación y pruebas. |
| **GLM-5.3-Flash o ZCode** | Punto 5: vista previa e interfaz. |
| **Claude** | Copia de la base, revisión de cada diff, `dotnet build -m:1`, pruebas, ensayo de importación y verificación en el navegador. |

---

## Fase 2. Módulo de estudiantes (práctica libre)

### Decisiones
| Tema | Decisión |
|---|---|
| Registro | Página pública. Pide **correo @est.univalle.edu** (otro dominio se rechaza), contraseña, **nombre y apellidos, carrera y semestre, y celular** (8 dígitos, validado con `BolivianIdentity`). No pide C.I. |
| Aprobación de la cuenta | Queda **pendiente**. El SuperAdministrador o el Administrador la revisa en la bandeja «Nuevas cuentas» y la aprueba o rechaza con motivo. Mientras tanto, el login muestra que sigue pendiente o que fue rechazada. |
| Rol | Rol nuevo **Estudiante** (`Student`). Ve solo su portal: cupos, sus reservas y sus pedidos. La persona se registra con la categoría Estudiante. |
| Cupos | La Inge (roles de gestión) abre un cupo con laboratorio, fecha, periodos y **cantidad de estudiantes que elige en cada cupo** (sugerido 6, entre 1 y 10). Solo puede abrirlo donde el laboratorio esté libre, usando la misma fuente de ocupación (`IOccupancyService`). El cupo ocupa el laboratorio en la grilla («Práctica libre 3/6»). |
| Reserva | El estudiante pide un lugar y **la Inge aprueba o rechaza cada reserva**. Los lugares se cuentan con las reservas aprobadas. |
| Límite | **2 reservas por semana** por estudiante (pendientes más aprobadas). |
| Cancelar | El estudiante puede cancelar **hasta 24 horas antes**. Después ya no puede, y si no se presenta, cuenta como falta. |
| Material | Lo pide **del catálogo, con cantidades** (utensilios e insumos), como un L-3 simple. El sistema avisa si el stock no alcanza en esos periodos, con la misma lógica de disponibilidad. La Inge lo revisa junto con la reserva. |
| Asistencia | La marcan la **Inge, los administradores y el rol Cierre de práctica**: presente, no se presentó o mal uso, con observación. |
| Faltas y sanción | Cada falta queda en la **ficha del estudiante**, con un historial visible para los administradores. **Suspender por un tiempo o dar de baja es manual** y lo hacen solo el SuperAdministrador o el Administrador. Un estudiante suspendido no puede reservar. |
| Avisos | **Dentro de la app** (campana y mensajes en el login). No se configura correo. |

### Trabajo técnico
1. **Modelo y migración nueva** (las existentes no se tocan):
   - estado de cuenta pendiente, aprobada o rechazada, con motivo y quién la revisó;
   - `StudentProfile` (carrera, semestre, celular);
   - `FreePracticeSlot` (laboratorio, fecha, periodo inicial, cantidad de periodos, cupo, quién lo abrió, estado);
   - `SlotReservation` (estudiante, cupo, estado: pendiente, aprobada, rechazada, cancelada, asistió o falta);
   - `SlotMaterialRequest` con sus líneas;
   - `StudentIncident` (tipo, observación, quién y cuándo);
   - `StudentSuspension` (hasta cuándo, motivo).
2. **Servicios:**
   - registro con dominio validado;
   - aprobación de cuentas;
   - cupos, validados contra la ocupación;
   - reservas, con el límite semanal, la regla de 24 horas y el control del cupo;
   - asistencia y faltas;
   - suspensión;
   - avisos con el servicio de notificaciones que ya existe.
3. **Ocupación:** una fuente nueva, `FreePracticeSlotOccupancySource`, registrada en `IOccupancyService`. Así los cupos aparecen en todas las grillas y nadie más puede reservar encima.
4. **Páginas:**
   - registro público y login con el estado de la cuenta;
   - bandeja «Nuevas cuentas»;
   - gestión de cupos (Inge);
   - bandeja de reservas por aprobar, con su material;
   - toma de asistencia;
   - ficha del estudiante con su historial y suspensión;
   - portal del estudiante: cupos disponibles, mis reservas y mi pedido de material.

   Se usa la UI NiceAdmin/Bootstrap 4 con SweetAlert2 v7 y se consulta antes la skill de web moderna.
5. **Pruebas:**
   - unitarias de las reglas: dominio, límite semanal, 24 horas, cupo lleno, suspendido;
   - E2E: registro → aprobación → reserva → aprobación → asistencia → falta → suspensión.

### Reparto
| Quién | Qué |
|---|---|
| **GLM-5.3 (fuerte)** | Modelo, migración (generada, no aplicada), servicios y pruebas. |
| **GLM-5.3-Flash o ZCode** | Páginas. |
| **Claude** | Revisión, migración aplicada solo en la copia de EXP, pruebas y recorrido en el navegador con un estudiante y con el SuperAdministrador. |

---

## Riesgos y cuidados
- **La regla `gap = 0`** cambia los choques en todo el sistema. Prácticas que antes salían como conflicto por estar contiguas dejan de serlo, que es justo lo que se pidió.
- **ZABALA** tiene dos personas: no se elige una automáticamente.
- **Capacidad de los laboratorios:** está vacía, así que el aforo no se valida en la importación y queda solo como aviso.
- **Cuota de GLM:** GLM Worker y ZCode por terminal comparten el límite de 5 horas. Se trabaja por ventanas y, si se acaba la cuota, se espera.
- **Recursos:** un solo build o prueba a la vez. Los trabajadores no levantan servidores ni escriben en la base. No se hacen commits sin pedido.

---

## Avance de la Fase 1 (2026-11-23, hecho por Claude, sin commit)

### Qué se implementó
- **Regla de limpieza de 10 minutos** (`PlanningSlot.CleaningGapPeriods = 0`). Se ajustaron los mensajes de la validación de horario, del importador, de las solicitudes del docente y de la planificación. Las grillas del docente y del administrador dejaron de marcar un periodo entero como «Limpieza». En `grilla-reprogramar.js` y `planificacion-periodos.js` se quitó el bloqueo por contigüidad.
- **`Services/PlanningImport/GeneratorWorkbook.cs`:**
  - el lector de CONFIG, MATERIAS y PLANIFICA, que deja la práctica como texto («E1P», «11.1») y convierte H-5 en H-05;
  - el planificador de periodos (P2–P5; P2–P5 y P7–P10; P1–P4, P5–P8 y P9–P12), que corre la práctica cuando el docente ya está ocupado.
- **`GeneratorSetupService.cs`:** prepara la gestión (estado Inactiva y fechas), el calendario de lunes a sábado y los grupos.
- **`PlanningImportService`:**
  - reconoce el generador y lo convierte a las mismas filas, reutilizando la revisión y la carga;
  - pasa los avisos de cada fila;
  - en una reimportación, actualiza lo que se puede y omite lo que ya tiene movimientos;
  - omite por defecto las filas de semanas repetidas;
  - rechaza el archivo si es de otra gestión.
- **Página Importar:** acepta `.xlsm`, agrega la tarjeta «Preparar la gestión» antes de la revisión y muestra los avisos del archivo en amarillo.
- **Pruebas:** 618 en verde (14 nuevas en `GeneratorImportTests`).

### Ensayo en `DB_Laboratorios_Univalle_EXP_IMPORT_PRUEBA` (copia de EXP)
- **Respaldo previo de EXP:** `C:\Users\Public\BackupsUnivalle\EXP_PRE_IMPORT_PRUEBA_20261123.bak`.
- **Preparación:** la gestión 2027-1 pasó de Completada a Inactiva, con fin el 03/07/2027. El calendario quedó con 19 semanas y 114 días, y los 41 grupos con su día habitual.
- **Revisión:**
  - 380 prácticas para cargar;
  - 33 omitidas por la semana 6 (es error del Excel);
  - 9 problemas reales del Excel: 5 que no entran en su día y 4 choques de Pedro Guereca. En el ensayo se omitieron para ubicarlas a mano.
- **Carga:** 380 prácticas Aprobadas en 11 s, de las cuales 339 llevan material de la receta.
  - No hay superposiciones de laboratorio ni de docente.
  - El H-05 del 09/03 queda con 3 prácticas seguidas.
- **Reimportar** el mismo Excel no cambia nada.
- **Limpieza al terminar:** la app se cerró y la contraseña temporal se borró. La copia queda para revisarla; el usuario `qa_super` existe solo en la copia.

### Pendiente
- Cargar en **EXP** (no en la copia), con orden del usuario y un respaldo nuevo.
- Preguntas para la Inge:
  - ¿Cuál es la fecha real de la semana 6?
  - ¿Dónde van las 9 prácticas que no entran en su día?
  - ¿La misma letra de grupo en materias distintas son los mismos estudiantes? Hoy no se valida.
- Revisar la tarjeta de preparación en el navegador.

### Carga en EXP (2026-11-23 16:13, con orden del usuario)
- Respaldo previo: `C:\Users\Public\BackupsUnivalle\EXP_PRE_IMPORT_2027-1_20261123.bak`.
- Se hizo con los mismos servicios de la página Importar, en nombre del usuario 1 (nihaht):
  - preparación: 2027-1 Inactiva, 19 semanas, 41 grupos;
  - 9 problemas omitidos;
  - **380 prácticas Aprobadas** (339 con material);
  - 0 superposiciones de laboratorio;
  - la gestión actual sigue siendo 2026-2, y sus 490 planificaciones están intactas.
- El ejecutor temporal se borró. El detalle de las 9 omitidas está en `outputs/insumos-2026-11-23/carga_exp_2027-1.txt`.

## Avance de la Fase 2 (2026-11-23, Claude, sin commit)
- **Modelo** (`Models/StudentPractice.cs`):
  - entidades `StudentProfile`, `FreePracticeSlot`, `SlotReservation`, `SlotReservationItem` y `StudentIncident`;
  - rol `UserRole.Estudiante = 5` (`Student`).
  - Migración nueva `20261123202823_StudentFreePractice`: crea las 5 tablas y amplía `CK_Users_Role` a (1, 2, 3, 4, 5, 99). **Está aplicada solo en la copia `DB_Laboratorios_Univalle_EXP_IMPORT_PRUEBA`**, no en EXP.
- **Reglas** (`Services/StudentPractice/StudentPracticeService.cs`):
  - correo @est.univalle.edu;
  - la cuenta queda pendiente hasta que la aprueba un administrador;
  - el cupo solo se abre donde el laboratorio está libre;
  - la Inge aprueba cada reserva y se respeta el cupo;
  - máximo 2 reservas por semana;
  - se cancela hasta 24 h antes;
  - asistencia: asistió, no se presentó o mal uso (las dos últimas quedan como falta, sin duplicarse);
  - suspensión y baja manuales;
  - avisos dentro de la app.
- **Ocupación:** `FreePracticeSlotOccupancySource` hace que un cupo abierto ocupe el laboratorio en todas las validaciones (planificación, solicitudes e importación).
- **Páginas:**
  - `/Registro` (pública);
  - mensajes en el login para cuentas pendientes, rechazadas o dadas de baja;
  - `/Estudiante` (portal) y `/Estudiante/Reservar` (material del catálogo con cantidades);
  - `/PracticaLibre/Cupos`, `/Reservas` (con la disponibilidad del material) y `/Asistencia` (también para el rol Cierre);
  - `/PracticaLibre/Cuentas` y `/Ficha/{id}`, solo para administradores;
  - menú del estudiante y sección «Práctica libre»;
  - el alcance del estudiante (`StudentAccess`) lo devuelve a su portal si intenta entrar a otra pantalla.
- **Pruebas:** 630 en verde (12 nuevas en `StudentPracticeServiceTests`).
- **Recorrido HTTP en la copia:** registro, rechazo de correo no institucional, aviso de cuenta pendiente, aprobación, cupos, rechazo de cupo superpuesto, reserva con material, bloqueo de pantallas de gestión, aprobación de la reserva, no se presentó con su falta en la ficha, suspensión y bloqueo de nuevas reservas.

### Pendiente de la Fase 2
- Aplicar la migración en **EXP**, con orden del usuario y respaldo previo.
- La grilla del administrador (`LaboratoryGridService`) todavía no dibuja los cupos; eso entra en la Fase 3 (grilla única).
- El material que pide el estudiante no se cuenta como demanda en la disponibilidad de otras prácticas.
- Al registrarse no se pide C.I.: se guarda un identificador interno «E…» porque la columna es obligatoria y única.
