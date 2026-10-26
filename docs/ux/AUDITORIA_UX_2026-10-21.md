# Auditoría de UX/UI — Laboratorios Univalle (2026-10-21)

**Objetivo:** que un usuario genérico (técnico, ingeniera de mantenimiento, coordinación o auxiliar) use la aplicación con pocas indicaciones, apoyado por talleres.

**Método:** revisión heurística del código y de las pantallas (menú, flujos, formularios y componentes), cruzada con lo que reportaron los probadores (`outputs/qa-usuario/`, `outputs/qa-obs-2026-10-19/`). Hasta hoy **no hubo pruebas con usuarios finales reales**.

**Decisiones del usuario:**
- se trabaja por fases, empezando por la 1 y la 2;
- en pantalla se muestran **solo nombres claros, sin códigos L-x**;
- ayuda contextual «?» y glosario;
- prioridad para Administrador y SuperAdmin.

Glosario: `docs/ux/GLOSARIO.md`.

Leyenda de impacto: 🔴 alto · 🟡 medio · 🟢 bajo.

---

## 1. Orientación y navegación

- 🔴 **Menú confuso** (`Pages/Shared/_Sidebar.cshtml`):
  - «Procesos» aparece dos veces, con contenidos distintos.
  - Las cabeceras no ayudan: «Inicio» no enlaza a nada y «Elementos» agrupa gestiones.
  - La entrada «Ver» lleva al inventario.
  - Para el Supervisor, «Man. Preventivos» abre la planificación académica.
  - Hay tres niveles para una sola pantalla: «Personas ▸ Usuarios ▸ Usuarios».
  - Los iconos se repiten.
- 🔴 **Pantallas importantes fuera del menú:**
  - Reportes: solo desde el panel de la gestión, y el Supervisor no tiene camino.
  - Historial del equipo.
  - Facultades y carreras, escondidas como pestañas de Laboratorios.
  - Países, dentro de «Regiones».
  - Personas, dentro de Usuarios.
  - Cronograma (`/Schedules`), sin enlace.
- 🔴 **Inicio con parpadeo:** `/Index` dibuja la página y luego redirige por JavaScript. El botón «atrás» del navegador vuelve a `/Index`, que redirige de nuevo, y el usuario queda atrapado.
- 🟡 **Títulos y ubicación:**
  - Unas 75 páginas sin título: la cabecera queda vacía y la pestaña dice « - Laboratorios Univalle».
  - Títulos duplicados en Panel, Cierre, Planificaciones, Calendario, Demanda, Reposiciones; el de Crear gestión aparece tres veces.
  - Sin migas de pan.
  - Más de 30 textos distintos para «Volver».
- 🟡 **Búsqueda global** en la barra superior: no hace nada.
- 🟡 **Notificaciones:**
  - El contenido cambia según la página en la que esté el usuario.
  - «Marcar leídas» en realidad las pospone hasta mañana.
  - El rol Cierre de práctica no recibe las devoluciones vencidas.

## 2. Permisos

- 🔴 **Enlaces visibles que terminan en `/Error`**, con el texto «registro histórico incompleto». Nunca dicen «no tiene permiso».
  - «Mi Perfil» para Supervisor y Cierre.
  - Los botones de administración del asistente para el Supervisor.
  - El icono de eliminar de las solicitudes.
  - Los estados vacíos que mandan a «Gestiones».
  - El botón «Volver» de esa pantalla puede formar un bucle.
- 🟡 Los mosaicos del inventario se ven como enlaces, pero para el Supervisor están desactivados sin ninguna señal.

## 3. Lenguaje

- 🔴 **Códigos L-x sin explicación** en menús, títulos y botones.
  - **L-3 tiene tres significados:** salida de equipos, entrega de material de práctica y «planificación».
  - La L-7 tiene cinco nombres distintos.
  - Kardex aparece como paso, como «Concluir mantenimiento» y como historial.
  - L-48 se usa para la gestión, el plan, el cronograma y la lista de tareas.
- 🔴 **Palabras con dos significados:**
  - «Reposición» (día suspendido / material dañado), y el botón «Reponer día» en realidad suspende el día.
  - «Ambiente» y «Laboratorio» para lo mismo.
  - «Periodo» (franja de clase / semestre).
  - «Gestión» (mantenimiento / académica).
- 🟡 Palabras en inglés: Wizard, Dashboard. Términos inconsistentes: «Ronda Progresivo», «Man.».
- 🟡 Se ven identificadores internos («Req ID», «Mnt #N», «ManagementPlanId recibido») y nombres de fase en inglés.
- 🟡 Validación del navegador y Select2 en inglés en 14 vistas. Faltan tildes en varias pantallas.

## 4. Flujos (asistente de mantenimiento y planificación)

- 🔴 **Correctivo:** «siguiente» desde el paso 0 muestra «¡Ronda Finalizada!» falso (`Pages/Index.cshtml.cs:322-336`).
- 🔴 **Paso L-6:** la paginación calcula de a 3 y muestra de a 2, así que hay laboratorios pendientes que nunca aparecen.
- 🔴 **El stepper no informa el estado real:** marca como «completado» por posición, no muestra cuántos equipos quedan por paso y no se puede pulsar.
- 🔴 **Los mensajes de bloqueo mandan a «Rehacer fase en el wizard»,** pero ese botón no está ahí. Además, L-6 y L-7 no se pueden rehacer desde la interfaz.
- 🔴 **Registro de mantenimiento → Cierre del mantenimiento:** se cargan los mismos datos dos veces y los textos se contradicen. El cierre exige costo mayor que 0 aunque el registro de mantenimiento admite 0.
- 🔴 **Sin compras no se puede cerrar:** el único camino a «Completado» es crear una solicitud de compra, aunque no haya habido compras.
- 🔴 **Checklist técnico:** los 13 puntos son texto libre, y el de refrigeración aparece igual para una mesa.
- 🔴 **Planificación:** después de aprobar, nada dice qué sigue. El stepper tiene 3 pasos, pero las pestañas dicen «Paso 4» y «Paso 5». Cierre manda a aprobar en «Planificaciones», cuando se aprueba en Disponibilidad.
- 🟡 Crear una gestión promete sincronizar todos los equipos, pero lleva a «Planificar activos», y la instrucción se pierde en un aviso de 4 segundos.
- 🟡 **Borradores:** solo se ven al pasar el mouse, no hay lista de «mis borradores» y el botón no dice «Continuar».
- 🟡 «Registrar mantenimiento» no avanza el equipo si el estado no es «Completado», y no lo avisa.
- 🟡 **Verificación masiva:** el estado sale en inglés («Operational»), «Regular» y «Baja» no tienen efecto visible, y al cerrar el modal de «Malo» el valor vuelve a «Bueno» sin aviso.
- 🟡 La capacidad del laboratorio no se puede editar desde la interfaz (la regla de aforo depende de ella).

## 5. Consistencia y respuesta del sistema

- 🔴 **27 pantallas usan una opción que SweetAlert2 v7 no admite** (`icon:`), así que esos modales salen sin icono.
- 🟡 **Confirmaciones incompletas:** 12 Editar y 11 Dar de baja no confirman con SweetAlert, como piden las reglas del proyecto.
- 🟡 **Doble envío:** unos 82 formularios permiten enviar dos veces.
- 🟡 **AJAX sin manejo de error:** si falla, los selects se quedan en «Cargando…».
- 🟡 **Errores lejos del campo:** se muestran en un modal «¡Ups!» y no junto al campo; los obligatorios no siempre llevan asterisco.
- 🟡 **No hay componentes comunes:**
  - Cabecera, estado vacío, paginación y filtros se copian a mano en 33 a 34 vistas.
  - Hay tres «dialectos» visuales y cuatro colores primarios.
  - Se mezclan librerías de iconos (fas, mdi, ti, bi).
- 🟢 **Escritura bloqueada sin aviso:** algunos campos borran caracteres al escribir y no explican por qué (`setupBlocking`).

## 6. Accesibilidad y rendimiento

- 🟡 **Contraste bajo:** `.text-muted` (#afb5c1) tiene un contraste de ~2,1:1 y se usa 1707 veces; `btn-warning text-white` queda en ~1,7:1.
- 🟡 **Letra pequeña:** 166 tamaños de 11 px o menos.
- 🟡 **Teclado y lectores de pantalla:** unos 156 botones solo con icono sin `aria-label`, y foco quitado en varios formularios.
- 🟢 **Carga innecesaria:** unos 380 KB de JS global sin uso (d3, c3, sparkline). Páginas de más de 1000 líneas (`Index`, `Managements/Details`, `LaboratoryOperations/Index`).

## 7. Ayuda

- 🔴 No hay tour, glosario, página de ayuda ni manual de usuario. `docs/user/GUIA_USUARIO.md` trata sobre agentes de IA, no sobre la aplicación.

---

## Plan por fases

| Fase | Contenido | Estado |
|---|---|---|
| 1. Orientación | Menú por tareas y por rol; inicio sin parpadeo; acceso denegado claro; títulos y sección en todas las páginas; quitar la búsqueda falsa; arreglo global de iconos de SweetAlert; 2 defectos del asistente | Hecha (2026-10-26, sin commit) |
| 2. Lenguaje y ayuda | Renombrado según el glosario (sin códigos L-x); español en la validación y en Select2; ayuda «?», recuadro «¿Qué hago aquí?» y página de Ayuda con glosario | Hecha (2026-10-26, sin commit); detalle en docs/INFORME_ESTADO_FUNCIONAL_2026-10-18.md §4.9 |
| 3. Flujos | Stepper con estado real y pendientes; «Rehacer fase» real; cierre del mantenimiento precargado desde el registro y sin exigir costo; compra opcional; checklist con casillas; «qué sigue» después de aprobar; borradores visibles; capacidad editable | Pendiente (requiere decisiones de negocio) |
| 4. Componentes | Partials de cabecera, estado vacío con acción, paginación con ventana y filtros; confirmaciones Editar/Dar de baja; evitar doble envío; AJAX con error; errores junto al campo; asteriscos | Pendiente |
| 5. Accesibilidad y rendimiento | Contraste y tamaños mínimos; `aria-label`; foco visible; quitar JS global sin uso | Pendiente |
| 6. Capacitación y validación | Manual por rol con capturas (material para talleres); tour guiado; prueba de usabilidad con 5 usuarios reales por rol (tareas, tiempos, errores, cuestionario SUS) | Pendiente |
