# Backlog del Sprint 3

**Proyecto:** Laboratorios Univalle — Gastronomía
**Periodo:** 27/10/2026 → 18/11/2026
**Equipo:** Fernando Muñoz Aranda · Danielo Romero Daza · Nihaht Poly Montes Peñarrieta
**Rama:** `sprint-3` (en GitHub, pendiente de revisión) · **Base de pruebas:** copia `DB_Laboratorios_Univalle_EXP`

---

## 1. Antecedente: de dónde venimos

El Sprint 2 dejó la base real cargada desde `BaseGastronomia_v1.xlsx`, sin diferencias, y el módulo de Planificación funcionando. La migración a React + NestJS + PostgreSQL ya se había descartado porque, medida con el módulo de Usuarios, exigía varios sprints sin entregar nada nuevo. La evidencia está en los documentos del Sprint 2 y en la rama `migration/react`.

Este sprint abre el sistema a un usuario nuevo, **el docente**, y conecta su solicitud con la revisión, el material y la planificación del semestre.

---

## 2. Resumen de lo entregado

| Épica | Historias entregadas |
|---|---|
| E1 · Portal del docente | 10 |
| E2 · Revisión de solicitudes y material por horario | 11 |
| E3 · Planificación del semestre | 5 |
| E4 · Inicio, menú y avisos | 9 |
| E5 · Mantenimiento y reportes | 5 |
| E6 · Base de datos real: inventario, procesos e historial | 3 |
| E7 · Calidad y pruebas | 3 |
| E8 · Operación y nube | 4 |
| **Total** | **50** |

**Hecho** significa que la función existe y se probó en EXP, una copia de la base real. Este trabajo todavía no está en la base real ni en la versión publicada el 12/11; ese paso se resume en *Siguiente etapa*.

Las capturas son del 18/11/2026 y se tomaron sobre **EXP, una copia desechable de la base real**. Los nombres de docentes que aparecen son los de esa copia.

---

## 3. Épicas e historias de usuario

### E1 · Portal del docente

Hasta ahora, el docente dependía de que otra persona registrara su práctica. Ahora entra con su propia cuenta, ve su semana, consulta qué está libre y solicita la práctica con su material.

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| DO-01 | Como **docente**, quiero una cuenta vinculada a mi persona, para entrar a mi propio portal. | Alta con persona vinculada y cambio de clave en el primer ingreso. | ✅ | Recorrido del flujo docente, 12/12 |
| DO-02 | Como **responsable**, quiero que el docente vea solo lo suyo, para proteger la información. | No abre pantallas de gestión, la bandeja del revisor ni solicitudes ajenas. | ✅ | Recorridos de casos y de flujo |
| DO-03 | Como **docente**, quiero solicitar una práctica en 4 pasos, para no olvidar ningún dato. | Práctica → horario y laboratorio → material → revisar y enviar. | ✅ | Buscador en 246 ms; kit de 10 artículos precargado |
| DO-04 | Como **docente**, quiero solicitar desde una práctica ya programada, para no escribir de nuevo. | Se abre el asistente con grupo y práctica cargados. | ✅ | Recorrido de casos |
| DO-05 | Como **docente**, quiero ver la disponibilidad por semana académica, para elegir un horario libre. | Laboratorios × días × periodos; una celda libre abre la solicitud. | ✅ | Recorrido de planificación, 46/46 |
| DO-06 | Como **docente**, quiero corregir una solicitud observada o retirarla, para mantenerla al día. | Reenvío con contador; retiro con registro. | ✅ | Recorrido de casos |
| DO-07 | Como **docente**, quiero indicar los estudiantes de mi solicitud, para que el material corresponda. | El número queda en la solicitud y lo ve el revisor. | ✅ | Prueba con 13 estudiantes |
| DO-08 | Como **docente**, quiero un inicio claro con mi próxima práctica, para saber qué sigue. | Resumen y pestañas Mi semana, Por solicitar y Novedades. | ✅ | Recorrido del inicio docente, 32/32 |
| DO-09 | Como **docente**, quiero recibir un aviso si cambian mi práctica, para no llegar a un horario equivocado. | Aviso al reprogramar, reponer o suspender. | ✅ | El recorrido comprueba el de reprogramación; los otros dos están en el commit `b3d2c17`, sin prueba automática propia |
| DO-10 | Como **docente**, quiero una pestaña de novedades, para ver lo que el revisor decidió sobre mis prácticas. | Pestaña con enlace directo, sin desborde en teléfono. | ✅ | Recorrido del inicio docente, 32/32 |

![Inicio del docente: próxima práctica, indicadores y accesos a cada sección (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_doc_inicio.png)

![Paso de material del asistente: el kit se carga solo y cada artículo indica si alcanza en ese horario (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_asistente.png)

![Disponibilidad por semana académica: el docente ve lo libre y lo ocupado sin acceder a datos de otros docentes (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_semana_doc.png)

### E2 · Revisión de solicitudes y material por horario

Una solicitud solo sirve si alguien puede decidir con datos. Esta épica responde a tres preguntas: **¿alcanza el material en ese horario?, ¿de qué laboratorio sale? y ¿qué pasó después de entregarlo?**

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| RS-01 | Como **revisor**, quiero una bandeja por estados, para atender primero lo pendiente. | Seis pestañas con contadores, filtros y buscador. | ✅ | Recorrido de planificación |
| RS-02 | Como **revisor**, quiero aprobar, observar o rechazar, para responder al docente. | Cada decisión queda en el historial. | ✅ | Recorrido de casos |
| RS-03 | Como **revisor**, quiero que no se apruebe más material del disponible, para no comprometer lo que no hay. | La segunda aprobación se detiene; solo pasa con «Aprobar igual con faltantes», y queda registrado. | ✅ | Recorrido de casos; corrige la sobreventa del commit `d1eb049` |
| RS-04 | Como **revisor**, quiero que dos decisiones simultáneas no se pisen, para confiar en el estado. | Un solo resultado coherente. | ✅ | 3 carreras simuladas |
| RS-05 | Como **encargado**, quiero calcular el material por horario, para saber qué está libre en ese momento. | Se considera el mayor uso simultáneo y lo no devuelto. | ✅ | Commits `896afb9`, `d1eb049` y `53c25cc` |
| RS-06 | Como **revisor**, quiero repartir cada artículo entre laboratorios de origen, para saber de dónde sale. | El reparto debe sumar lo aprobado. | ✅ | Recorridos de casos y de flujo |
| RS-07 | Como **encargado**, quiero entregar desde varios orígenes, para reflejar lo que ocurre de verdad. | Un ítem de salida por origen. | ✅ | Prueba con 5 unidades de un origen y 2 de otro |
| RS-08 | Como **encargado**, quiero distinguir los consumibles, para descontarlos al entregarlos. | Bajan la existencia y no se devuelven. | ✅ | Existencia de 10 → 8 en el recorrido |
| RS-09 | Como **encargado**, quiero que una incidencia descuente del origen correcto, para no descuadrar otro laboratorio. | Se descuenta de cada origen lo que entregó. | ✅ | Recorrido adicional: un origen de 5 → 0 y otro de 4 → 3 |
| RS-10 | Como **encargado**, quiero cerrar sin novedades, para no registrar devoluciones una por una. | Devuelve lo pendiente y finaliza. | ✅ | Recorridos de flujo y adicional |
| RS-11 | Como **revisor**, quiero comparar prácticas previstas y recibidas, para saber quién falta por solicitar. | Resumen por semana. | ✅ | Recorrido de casos |

![Bandeja de solicitudes: pestañas por estado, filtros y motivo de cada decisión (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_bandeja_sol.png)

![Reparto por origen: el sistema avisa cuando un laboratorio recibe más de lo que tiene libre en ese horario (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_revision.png)

![Entrega y devolución: el detalle muestra origen, entregado, devuelto y lo que falta cubrir con un incidente (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_salida.png)

### E3 · Planificación del semestre

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| PS-01 | Como **administradora**, quiero cargar el semestre desde Excel con revisión fila por fila, para no registrar práctica por práctica. | Plantilla, revisión fila por fila y carga de todo o nada. | ✅ | Recorrido de planificación, con un archivo de 5 filas |
| PS-02 | Como **administradora**, quiero que cada cambio tenga motivo, para explicarlo después. | Motivo obligatorio y aviso si tiene menos de 5 palabras. | ✅ | Recorrido de planificación |
| PS-03 | Como **administradora**, quiero suspender un día y reponer sus prácticas, para ordenar lo que quedó pendiente. | Categoría obligatoria y filtro en Reposiciones. | ✅ | Recorrido de planificación |
| PS-04 | Como **administradora**, quiero impedir mover una práctica con material entregado, para no perder el control de la salida. | La reprogramación manual se bloquea. | ✅ | Recorrido de planificación |
| PS-05 | Como **administradora**, quiero ver qué cambia antes de confirmar, para evitar errores. | Banda azul al reprogramar, naranja al reponer y resumen antes → después. | ✅ | Recorrido de planificación |

![Carga desde Excel: cada fila indica si tiene errores, si ya existe o si está lista, y qué decisión falta (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_importar.png)

![Grilla durante una reposición: la banda naranja mantiene a la vista qué día se está reponiendo (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_grilla.png)

### E4 · Inicio, menú y avisos

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| IN-01 | Como **administradora**, quiero un Inicio con lo importante, para decidir qué hacer al entrar. | Indicadores y tarjetas que abren Prácticas, Bandeja, Gráficos e Inventario. | ✅ | Recorrido del Inicio, 47/47 |
| IN-02 | Como **administradora**, quiero gráficos con espacio propio, para leerlos sin desplazarme. | Se dibujan al abrir su pestaña. | ✅ | Recorrido del Inicio |
| IN-03 | Como **usuario**, quiero que la campana cuente solo lo que debo atender, para confiar en ella. | «Revisar datos» queda aparte y no suma. | ✅ | En EXP, de 137 a 2 avisos |
| IN-04 | Como **revisor**, quiero que un aviso desaparezca al resolver la solicitud, para no revisarla dos veces. | Solicitud resuelta = aviso leído. | ✅ | Pruebas `NotificationRequestResolutionTests` |
| IN-05 | Como **usuario**, quiero un menú plegable, para ver solo la sección que uso. | La sección actual se abre sola. | ✅ | Recorrido del menú, 21/21 |
| IN-06 | Como **usuario**, quiero registros con nombres claros, para encontrarlos rápido. | L6 · Verificaciones, L7 · Servicio técnico, L8 · Kardex, L3 · Salidas y Adquisición. | ✅ | Recorrido de navegación, 46/46 |
| IN-07 | Como **usuario**, quiero la misma paginación en todos los listados, para no perder filtros ni posición. | «Mostrando a–b de N», con filtros y pestaña conservados. | ✅ | 420 comprobaciones |
| IN-08 | Como **usuario de teléfono**, quiero usar estas pantallas sin cortes, para trabajar desde el laboratorio. | Sin desborde a 390 px; el Inicio y «Mi semana», también a 320 px. | ✅ | Recorridos del Inicio, del docente y de navegación |
| IN-09 | Como **administradora**, quiero ver el catálogo de artículos en el panel de activos, para no perderlo de vista. | Tarjeta de artículos con enlace al catálogo. | ✅ | Commit `3af6fe8`; revisado en EXP el 18/11 |

![Inicio del administrador: indicadores blancos y cuatro tarjetas con datos reales (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_inicio.png)

![Bandeja del Inicio: lo que requiere acción aparece primero y «Revisar datos» queda plegado (captura del 18/11).](../../../outputs/presentacion-sprint3/img/s_bandeja.png)

### E5 · Mantenimiento y reportes

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| MT-01 | Como **administradora**, quiero reconstruir los equipos de cada gestión, para que el panel no salga en cero. | Vista previa, sin duplicados y con lote de trazabilidad. | ✅ | En EXP, de 1 a 2.493 planes |
| MT-02 | Como **administradora**, quiero ver cuándo falla un reporte, para no confundirlo con «0 equipos». | Error visible y registrado. | ✅ | Commit `545d4a5` |
| MT-03 | Como **administradora**, quiero descargar exactamente el documento elegido, para no recibir otro. | Sin número, la descarga responde con error. | ✅ | Commit `545d4a5` y `ReportServiceExcelTests` |
| MT-04 | Como **administradora**, quiero que Adquisición no pierda ítems, para que el total sea correcto. | Si no caben, una nota muestra su cantidad y subtotal. | ✅ | Commit `545d4a5` y `ReportServiceExcelTests` |
| MT-05 | Como **técnico**, quiero que la plantilla L7 descargue rápido, para no esperar. | Se eliminan las cajas de texto vacías. | ✅ | 1.082 cajas menos: de 1,06 s a 0,06 s |

### E6 · Base de datos real: inventario, procesos e historial

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| BD-01 | Como **administradora**, quiero confirmar que la base sigue fiel a `BaseGastronomia_v1.xlsx`, para confiar en el historial. | Comparación automática sin diferencias en los datos importados. | ✅ | 44.743 celdas, 0 diferencias; las filas de más se explican en *Estado de la base de datos* |
| BD-02 | Como **equipo**, queremos saber qué trae `BaseGastronomia_v2.xlsx`, para decidir qué cargar. | Comparación hoja por hoja con la v1. | ✅ | 3 hojas nuevas, vacías; 27 hojas de datos idénticas |
| BD-03 | Como **equipo**, queremos el modelo de datos del circuito docente, para guardar solicitudes y orígenes. | Migraciones aplicadas y probadas en la copia. | ✅ | 4 migraciones nuevas en EXP |

### E7 · Calidad y pruebas

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| QA-01 | Como **equipo**, queremos pruebas unitarias de las reglas, para detectar errores pronto. | Todas en verde. | ✅ | **571/571**; eran 402 al cerrar el Sprint 2 |
| QA-02 | Como **equipo**, queremos recorrer el sistema como un usuario, para probar lo que él verá. | Recorridos sin fallas sobre EXP. | ✅ | 12 ejecuciones y **1.001 comprobaciones** sobre los flujos modificados |
| QA-03 | Como **equipo**, queremos integrar las pruebas de la Práctica 3, para cubrir integración y aceptación. | Suite en verde. | ✅ | 63/63 |

### E8 · Operación y nube

| ID | Historia de usuario | Criterio de aceptación | Estado | Evidencia |
|---|---|---|---|---|
| OP-01 | Como **equipo**, queremos la base real en la nube y cuentas de prueba, para probar desde cualquier lugar. | Copia restaurada y sitio funcionando. | ✅ | Publicación del 04/11 |
| OP-02 | Como **equipo**, queremos publicar el circuito docente de prueba, para probarlo en línea. | Sitio actualizado y rutas protegidas. | ✅ | Publicación del 12/11, commit `24a49d9` |
| OP-03 | Como **equipo**, queremos publicar con un script, para no manejar la contraseña a mano. | Usa un perfil fuera del repositorio y conserva los archivos del servidor. | ✅ | `Tools/Deploy/Publicar-MonsterAsp.ps1`, commit `86717ff` |
| OP-04 | Como **equipo**, queremos cuentas de prueba por rol, para probar sin compartir la cuenta principal. | Se niega a operar sobre la base real y guarda las claves fuera del repositorio. | ✅ | `Tools/QaTestUsers`, commit `3321909` |

---

## 4. Siguiente etapa

| Frente | Qué incluye | De qué depende |
|---|---|---|
| Producto | Cierre de la reposición de incidentes, orden estable en Mantenimientos y planificación eligiendo Grupo → Materia | Próxima iteración |
| Despliegue | Regresión completa, 4 migraciones en la base real, publicación de lo hecho del 15 al 18/11, respaldos automáticos y unión a `qa` | Revisión del equipo y respaldo previo |
| Datos del cliente | Fechas de adquisición, capacidades, existencias de artículos de recetas, hojas nuevas de `BaseGastronomia_v2.xlsx` y Excel real del semestre | Entrega de los datos |

---

## 5. Documentos de respaldo

- **Informe de pruebas:** resultados, defectos encontrados y lo que no se probó.
- **Arquitectura, SOLID y patrones:** cómo se aplicaron en las clases nuevas.
- **Estado de la base de datos:** fidelidad, diferencias con el Excel, migraciones y datos faltantes.
- **Evidencia técnica:** `outputs/presentacion-sprint3/datos/`.
