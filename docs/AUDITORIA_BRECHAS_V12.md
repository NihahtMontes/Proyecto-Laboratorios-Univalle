# Auditoría de brechas: BaseGastronomia_v1.xlsx (V12) → base REAL

**Fecha:** 2026-10-13 (resuelto el 2026-10-14: ver `GUIA_IMPORTACION_HISTORICA.md`, «Versión REAL») · **Fase 0 (solo lectura)** · Fuente: `docs/MAPA_EXCEL_V4_A_BD.md`, `docs/INSTRUCCIONES_AGENTE_V12_GASTRONOMIA.md`, `_DICCIONARIO` y `_VERIFICACION` del Excel, `Models/ImportSourceRow.cs`.

Objetivo: que **cada celda del Excel** quede en la BD, en una columna propia o, si no hay columna, íntegra en `ImportSourceRows.OriginalDataJson` (ya existe; no requiere migración).

## A. Filas que hoy se excluyen (hay que cargarlas)
| # | Brecha | Causa | Solución propuesta |
|---|---|---|---|
| A1 | Gestión `1900-1` + `MAN-L8-00962/00963` | `CK_Managements_Year` (2000–2100) | Migración: ampliar el check a 1900–2100 y cargar con `Type = PorConfirmar` + issue `INVALID_MANAGEMENT` |
| A2 | 9 solicitudes L-3 (SOL3-0016…0023, 0312) + 154 líneas | Sin fecha (9) y sin laboratorio (4); `CK_Plannings_Laboratory` | Migración: `Plannings.ScheduledDate` nullable y laboratorio opcional si `Status` = histórico; sin inventar datos; issue `REQUIRED_VALUE_MISSING` |
| A3 | 11 líneas L-3 con `Cantidadsolicitada` vacía | Se omitían | Cargar con cantidad NULL + `VALUE_MISSING` (verificar que `PlanningRequirement.Quantity` admita NULL) |
| A4 | Existencias vacías, hoja `Incidentes` (0 filas) | Sin datos | Crear tabla de incidentes de artículos con las 17 columnas de la hoja (queda vacía hoy) |

## B. Columnas sin destino (hoy se descartan o se pierde el literal)
| # | Hoja.Columna | Hoy | Solución |
|---|---|---|---|
| B1 | `SolicitudesL7.Tiempoestimado` (329) | se descarta | Guardar en `Request.EstimatedRepairTime` si existe (el MAPA dice que se pone NULL) o en JSON crudo |
| B2 | `MantenimientosL8/PlanL48.Estadocalidad` (1.209 + 387) | solo issue para "Requiere revisión" | Columna `QualityStatus` en `Maintenance` y `MaintenancePlan` |
| B3 | `SolicitudesL3.Fechasolicitud` (vacía), `Sedecodigo`, `Carreracodigo`, `Horainicio` sin `Horafin` (6) | ignoradas | JSON crudo (+ issues existentes `CAREER_MISMATCH`) |
| B4 | `MantSolicitudes.Observacion`, `MantParticipantes.Participantecodigo` | no se guardan | JSON crudo |
| B5 | `Catalogo.Categoria/Clasificacion/Estado/Tipoutensilio` = "Por confirmar" o texto suelto | pasan a NULL/enum | Literal original en JSON crudo; enum `PorConfirmar` donde exista |
| B6 | `Actores.Direccion` de personas (solo `Extern.Address` la guarda) | se pierde para internos | JSON crudo o columna en `People` |
| B7 | `Unidadesmedida` `UNI-04` Rollo (sin uso) | no se carga | Cargar (la hoja la trae) |
| B8 | `Planificacion.Horainicio/Horafin` (vacías), `Esdemostrativa`, `Cantestudiantes`, `Factormateria` | parcial | Verificar columna por columna contra `PracticeSchedule`/`CourseGroup` en la Fase 2 |

## C. Formato
| # | Tema | Decisión necesaria |
|---|---|---|
| C1 | Textos en "tipo oración" en el Excel (`Casatermo srl`, `Laboratorio de cocina h-1`) frente a MAYÚSCULAS ya usadas | Cargar tal cual (fiel al Excel) vs. normalizar nombres. Original siempre en JSON crudo |

## D. Conteos a igualar (línea base del Excel)
Sedes 3 · Facultades 2 · Carreras 4 · Laboratorios 24 · Gestiones 27 · Actores 57 · ActorRoles 46 · Catálogo 121 · Unidades 644 · L-6 1.599 · L-7 329 · SolicitudUnidades 413 · L-8 1.209 · MantSolicitudes 121 · MantParticipantes 1.209 · L-48 387 · Materias 41 · Prácticas 196 · Planificación 422 · Categorías 10 · UnidadesMedida 5 · Artículos 1.374 · Requerimientos 2.111 · Existencias 700 · SolicitudesL3 1.037 · Detalles L-3 11.451 · Incidentes 0.

Las 4 solicitudes sin laboratorio y las 9 sin fecha se cargan; los demás conteos deben coincidir 1 a 1 (sin filas demo ni sobrantes).

## E. Pendiente para la Fase 1
- Confirmar con el modelo real cada "verificar" (A3, B1, B8) leyendo `Models/` antes de escribir la migración.
- Migración nueva única (las existentes no se tocan): A1, A2, B2 e incidentes (A4).

## F. Estado final (2026-10-14)
Todas las brechas de A y B quedaron cerradas en `DB_Laboratorios_Univalle_REAL` (verificador: 0 diferencias). Cambios respecto a lo
propuesto arriba: las 9 solicitudes sin fecha entran como `Plannings` con la fecha de la importación (no en `UndatedL3*`, que quedaron
vacías); no hubo tabla nueva de incidentes (la hoja `Incidentes` está vacía); los textos se cargan tal cual.
