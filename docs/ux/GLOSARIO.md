# Glosario de la aplicación (fuente única de nombres visibles)

Última actualización: 2026-10-21. Constantes en código: `Helpers/UiTerms.cs`. Textos de ayuda: `Helpers/HelpContent.cs`.

**Regla:** en pantalla se usa **solo el nombre claro**. Los códigos de formato institucional (L-3, L-5, L-6, L-7, L-8, L-12, L-48) aparecen **solo** en los Excel (`Services/Reports`, `wwwroot/templates`) y en la columna «Formato institucional» de la página de Ayuda. Un concepto se nombra igual en el menú, el título, los botones, los pasos y los mensajes.

**No se renombran** los códigos de registro: `SOL3-…`, `PRA-…`, `SAL-…`, `DEV-…`, `CORR-AAAA-S`. Tampoco las rutas, los handlers, las clases ni los nombres de archivo.

## Mantenimiento de equipos

| Nombre visible | Antes en pantalla | Formato institucional | Qué es (texto para la ayuda) |
|---|---|---|---|
| Gestión de mantenimiento | Gestión L-48, Gestión | L-48 | Periodo semestral (p. ej. 2026-2) en el que se planifica y registra el mantenimiento. Puede haber una gestión preventiva y una correctiva activas a la vez. |
| Mantenimiento preventivo | Man. Preventivos, Ronda (Progresivo) | — | Revisión planificada de los equipos elegidos para la gestión. Se hace con el asistente, paso a paso. |
| Mantenimiento correctivo | Man. Correctivos | — | Atención de una falla reportada. Empieza con una solicitud de servicio técnico. |
| Asistente | Wizard, Ronda | — | Guía paso a paso que muestra en qué etapa está cada equipo y qué falta hacer. |
| Panel | Dashboard | — | Resumen de la gestión: avance, pendientes y accesos rápidos. |
| Verificación de equipos | Verificaciones (L6), L6 VERIF. | L-6 | Revisión del estado de los equipos de un laboratorio (bueno, regular, malo o baja). Si un equipo falla, pasa a solicitud de servicio técnico. |
| Solicitud de servicio técnico | Solicitud de Servicio (L-7), Requerimiento técnico, Hacer Solicitud L7 | L-7 | Pedido de reparación o mantenimiento de un equipo con falla, con su checklist técnico. |
| Registro de mantenimiento | Mantenimiento (L-8), Acta L8 | L-8 | Acta de lo realizado: técnico, tareas, costos y estado final del equipo. |
| Salida de equipos | Salidas (L-3) | L-3 | Registro de los equipos que salen del laboratorio (mantenimiento externo o préstamo) y de su devolución. |
| Cierre del mantenimiento | Kardex (paso 5), Concluir Mantenimiento | Kardex | Registro final del mantenimiento: fechas, costo real, satisfacción y próximo mantenimiento. Alimenta el historial del equipo. |
| Historial del equipo | Kardex de la unidad | Kardex | Todo lo ocurrido con una unidad: verificaciones, solicitudes, mantenimientos y costos. |
| Solicitud de compra | L-12, Desembolso, DESEMB., Gestionar Adquisición | L-12 | Trámite administrativo para comprar repuestos o insumos usados en el mantenimiento. |
| Cronograma de mantenimiento | Plan L-48, Cronograma L-48 | L-48 | Calendario de los mantenimientos planificados de la gestión. |
| Tareas de mantenimiento | Tareas L-48, Lista de Verificación Técnica (L-48) | L-48 | Lista de tareas que el técnico marca al registrar el mantenimiento. |
| Equipo | — | — | Tipo o modelo de equipo (p. ej. «Horno combinado»). |
| Unidad física | — | — | Un equipo concreto, identificado por su número de inventario. |

## Planificación académica

| Nombre visible | Antes en pantalla | Formato institucional | Qué es |
|---|---|---|---|
| Semestre académico | Gestión académica | — | El semestre en el que se planifican las prácticas (usa la misma gestión que el mantenimiento). |
| Planificación | Planificación L-3, PRA-… | — | Sesión de práctica de una materia y un grupo, con fecha, periodos de clase, laboratorio, docente y material. |
| Periodo de clase | Periodo | — | Franja horaria del día académico (por ejemplo «Periodo 2 · 08:35–09:25»). |
| Laboratorio | Ambiente | — | Espacio físico donde se dicta la práctica. |
| Requerimientos | — | — | Material que necesita la práctica. |
| Receta de la práctica | — | — | Lista de artículos guardada en una práctica del catálogo; se carga en los requerimientos. |
| Kit de la materia | — | — | Lista base de artículos que se repite en todas las prácticas de una materia. |
| Disponibilidad | — | — | Revisión de stock y de choques antes de aprobar la planificación. |
| Entrega y devolución de material | L-3, Finalizar L-3 | L-3 | Registro del material entregado para la práctica y de lo que vuelve. |
| Incidente de material | L-5, Incidencia L-5 | L-5 | Daño o pérdida de material durante la práctica. Queda pendiente de reposición. |
| Cierre de práctica | — | — | Pantalla del día para entregar el material, recibirlo y finalizar la práctica. |
| Suspender día | Reponer día | — | Suspende un día académico (feriado, evento) y deja sus prácticas para reprogramar. |
| Días suspendidos y reposiciones | Reposiciones de día | — | Lista de días suspendidos y de cómo se reprogramaron sus prácticas. |
| Reprogramar | Reubicar, Reprogramar | — | Mover una práctica a otra fecha, periodo o laboratorio. |
| Reposición de material | Reposición | — | Reponer el material dañado o perdido registrado en un incidente. |
| Demanda de material | Demanda, planificaciones L-3 | — | Cuánto material piden las planificaciones frente al stock disponible. |
| Existencias | Stock | — | Cantidad disponible de cada artículo por laboratorio. |

## Códigos de registro (se muestran tal cual)

| Código | Qué identifica |
|---|---|
| `SOL3-0565` | Planificación importada del Excel institucional |
| `PRA-00728` | Planificación creada en la aplicación |
| `SAL-00001` | Salida (de equipos o de material) |
| `DEV-00001` | Devolución |
| `2026-2` / `CORR-2026-2` | Gestión preventiva / correctiva del semestre |

## Textos generales

| Uso | Texto |
|---|---|
| Título de error | No se pudo completar |
| Volver | Volver al listado · Volver al asistente · Volver al panel |
| Acceso denegado | No tiene permiso para abrir esta pantalla. |
