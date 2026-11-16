# Avance: tareas del 16/11 (Inicio, menú, planes de gestión, Reportes, listados)

Fecha: 2026-11-16 · Rama `exp/docente-v13` · Commits locales, sin push.

## Estado

- Compila sin errores y pasan **560 de 560** pruebas automáticas.
- Probado en navegador sobre la copia EXP, en escritorio y teléfono:
  - recorrido nuevo `Tools/E2E/inicio_navegacion.mjs`: **45 de 45**;
  - `http_cases_ux_navegacion.py` (SuperAdmin y Administrador): **275 de 275**;
  - `http_cases_paginacion.py`: **419 de 420**. La única falla es L3 · Salidas, que tarda la primera vez tras reiniciar la app; en caliente abre en 0,05 s;
  - recorridos anteriores: planificación 46 de 46, docente casos 23 de 23, extra 22 de 22 y flujo 12 de 12.
- Capturas en `outputs/e2e/inicio_navegacion`.
- Ninguna migración nueva. Los planes reconstruidos están **solo en EXP**.

## Qué se hizo

| Commit | Tarea |
|---|---|
| `9d26893` | Modern Web Guidance (skill oficial de Chrome) instalada y obligatoria para HTML, CSS y JS de cliente. Ver `docs/WEB_MODERNA.md`. |
| `72b81bc` | Reconstrucción de la lista de equipos de cada gestión (`/Managements/ReconstruirPlanes`, solo SuperAdmin). En EXP: de 1 a 2.493 planes. |
| `5c4ba7e` | El inventario pasa a ser el **Inicio**, con saludo, atajos, Inventario e Indicadores. Después del login se llega ahí. «Mantenimiento preventivo» abre el panel de la gestión. Se quitó «Inventario general» del menú. Registros quedó así: L6 · Verificaciones, L7 · Servicio técnico, L8 · Kardex, L3 · Salidas y Adquisición. |
| `7db1219` | Paginador común en todos los listados. Usuarios y Personas ordenados, con búsqueda por C.I. Gestiones con estados claros. Títulos y tarjetas del panel con código + nombre. |
| `c0e3e38` | Plantilla L7 sin las 1.082 cajas de texto vacías: la descarga pasa de 1,06 s a 0,06 s. Se ve idéntica en Excel. |
| `545d4a5` | Reportes: errores visibles y descargas con el número exacto. Adquisición ya no pierde ítems. L6 sin filas repetidas y con el nombre completo del laboratorio. L3 sale con una sola hoja. |

## Respuesta a «no hay solicitudes de compra, ¿es un error?»

No es un error. `Requests` tiene 330 filas, todas de servicio técnico (`Type = 1`); no hay ninguna compra (`Type = 2`), ni en EXP ni en REAL. El Excel histórico no trae adquisiciones. Lo que sí estaba mal era la columna «Ubicación», que salía vacía; ya se corrigió.

## Ojito

1. **Los planes reconstruidos están solo en EXP.** En REAL y en la nube el panel seguirá en 0 hasta abrir `/Managements/ReconstruirPlanes` y pulsar «Reconstruir». Antes, hacer el respaldo. Es repetible, sin duplicados, y deja un lote `PLANES-…` en `ImportBatches` por si hay que deshacerlo.
2. **Con los planes creados, el asistente de 2026-2 muestra 555 equipos** en sus pasos. Así se ve el estado real de los datos importados: verificados sin falla, con falla esperando L7, etc.
3. **Regla de estado de los planes**, copiada de la importación histórica:

   | Situación del equipo | Estado del plan |
   |---|---|
   | Tiene compra | Completado |
   | Tiene salida por mantenimiento | Espera Kardex |
   | Mantenimiento terminado | Espera salida |
   | Mantenimiento en curso | En mantenimiento |
   | Solo solicitud de servicio técnico | Espera mantenimiento |
   | Verificación con falla | Espera L7 |
   | Verificación sin falla | Verificado bien |

   Las salidas de material de prácticas no generan planes.
4. **«L8 · Kardex» es el registro de mantenimiento.** El paso 5 del asistente, «Cierre del mantenimiento», es otra cosa, aunque antes también se llamaba Kardex. Queda aclarado en el glosario de ayuda.
5. **Los códigos L-x volvieron solo en 5 etiquetas**: menú, títulos de listados y tarjetas del panel. La prueba de UX permite solo esas etiquetas exactas; cualquier otro código sigue contando como falla.
6. **El Supervisor llega al Inicio**, no al Panel de planificación como antes. Tiene el atajo «Panel de planificación».
7. **Los gráficos de Indicadores se guardan en caché 5 minutos.** Un cambio en el inventario puede tardar hasta 5 minutos en verse ahí. El Resumen y el Inventario sí están siempre al día.
8. **Adquisición con muchos ítems:** el formulario institucional es de una página (17 filas). Si hay más ítems, la última fila dice «… y N ítems más (ver sistema)» con su subtotal, y el total general incluye todo. No se insertan filas para no mover firmas ni totales.
9. **Las descargas L7, L3 y Adquisición sin número dan error 400.** Antes adivinaban «la más reciente». Todas las pantallas del sistema ya mandan el número; si alguien tenía un enlace viejo sin número, ahora verá el error.
10. **El arnés HTTP de pruebas acepta la copia EXP**, además de `QA_E2E`. Sigue rechazando REAL y la oficial.
11. **`docente_casos.mjs` usa fechas fijas.** Ahora acepta `E2E_DAY` y `E2E_MONDAY`, porque la regla de 2 días hábiles de anticipación vuelve vieja la fecha cada semana.
12. **Modern Web Guidance envía a Google datos anónimos de uso.** Para desactivarlo, `DISABLE_TELEMETRY=1`.
13. **Delegación a GLM:** la herramienta a veces da por caído a un trabajador que sigue corriendo dentro de `opencode serve`. Antes de reasignar su trabajo, revisar `http://127.0.0.1:4096/session/status`.

## Falta

- Revisión con Fernando y Danilo, y luego unir a `qa`.
- Reconstruir los planes en REAL y en la nube (punto 1), con respaldo.
- Publicar en la nube. Siguen pendientes las dos migraciones del 15/11 y el cambio de contraseña de `db70630`.
- Plan de modernización del front: lo prepara otro chat en `docs/plans/MODERNIZACION_WEB_2026-11-16.md`.
