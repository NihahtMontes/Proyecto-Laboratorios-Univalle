# Estado de la base de datos real

**Proyecto:** Laboratorios Univalle — Gastronomía
**Base:** `DB_Laboratorios_Univalle_REAL` (SQL Server, local) · **Fuente:** `BaseGastronomia_v1.xlsx`
**Consultas y verificación:** 18/11/2026, en modo solo lectura. Salidas completas en `outputs/presentacion-sprint3/datos/`.

---

## 1. ¿La base está «fina»? Respuesta corta

| Pregunta | Respuesta |
|---|---|
| ¿Lo importado sigue **fiel** al Excel? | **Sí.** 27 hojas, 23.542 filas y 44.743 celdas comparadas: **0 diferencias** y 0 violaciones de integridad. |
| ¿Es idéntica en cantidad de filas? | **No del todo.** El verificador informa 4 tablas con filas de más. Son registros creados desde la aplicación el 26/10, después de la importación. |
| ¿Está **completa**? | **No todavía.** Siguen faltando fechas de adquisición, capacidades, existencias y prácticas de catálogo. |
| ¿Está al día con el código? | **No.** Tiene 32 migraciones y el código tiene 36. El circuito docente todavía no puede usarse sobre esta base. |
| ¿`BaseGastronomia_v2.xlsx` trae datos nuevos? | **No.** Agrega 3 hojas con encabezados, pero sin filas. |

**En una frase:** los datos importados siguen intactos, pero la base necesita recibir el modelo nuevo y los datos que el cliente aún no entregó.

---

## 2. Qué contiene

| Área | Datos |
|---|---|
| **Inventario** | 121 equipos · 644 unidades físicas · 1.374 artículos · 702 existencias · 24 laboratorios |
| **Mantenimiento** | 28 gestiones · 1.599 verificaciones · 330 solicitudes · 414 unidades en solicitudes · 1.210 mantenimientos · 387 planes L-48 |
| **Planificación** | 1.038 planificaciones (559 de 2026-1 y 479 de 2026-2) · 11.460 líneas de material · 2 salidas |
| **Catálogo académico** | 41 materias · 196 prácticas · 2.111 líneas de receta · 869 horarios · 147 grupos por materia · 41 semanas · 237 días académicos |
| **Personas** | 57 personas (47 internas y 10 externas) · 3 usuarios |
| **Gestiones activas** | `2026-2` (preventiva) y `CORR-2026-2` (correctiva) |
| **Trazabilidad** | 23.542 filas de origen y 7.459 observaciones de calidad |

---

## 3. Verificación contra `BaseGastronomia_v1.xlsx` (18/11/2026)

El script `Tools/ExcelImport/verify_v12_full.py` compara en cuatro niveles: copia cruda de cada fila, claves, valores tipados e integridad.

| Hoja del Excel | Filas | Celdas comparadas | Diferencias |
|---|---|---|---|
| Unidades físicas | 644 | 1.288 | 0 |
| Verificaciones L-6 | 1.599 | 1.599 | 0 |
| Solicitudes L-7 | 329 | 987 | 0 |
| Mantenimientos L-8 | 1.209 | 3.627 | 0 |
| Plan L-48 | 387 | 774 | 0 |
| Catálogo de equipos | 121 | 605 | 0 |
| Prácticas | 196 | 588 | 0 |
| Requerimientos de práctica | 2.111 | 6.333 | 0 |
| Solicitudes L-3 | 1.037 | 4.148 | 0 |
| Detalle de solicitudes L-3 | 11.451 | 22.902 | 0 |
| Otras 17 hojas | 4.458 | 1.892 | 0 |
| **Total** | **23.542** | **44.743** | **0** |

### Por qué el verificador dice «4 problemas»

El resultado literal del script es **«4 problemas»**, y no conviene ocultarlo. No son valores alterados: son **filas que existen en la base y no en el Excel**. El 26/10 el resultado era 0, y esas filas se crearon esa misma noche, con la cuenta de administración, desde la aplicación.

| Tabla | Excel | Base | Filas de más |
|---|---|---|---|
| Unidades en solicitudes | 413 | 414 | 1 |
| Solicitudes de mantenimiento | 121 | 122 | 1 |
| Participantes de mantenimiento | 1.209 | 1.210 | 1 |
| Existencias | 700 | 702 | 2 |

Ese mismo día también se registraron 1 solicitud y 1 mantenimiento en `CORR-2026-2`, 1 planificación de `2026-2` con 9 líneas de material, 2 salidas y 11 observaciones «existencia no registrada». **Falta confirmar si son operación real o prueba.** Si fueron de prueba, deben retirarse con respaldo previo.

---

## 4. Qué cambia con `BaseGastronomia_v2.xlsx`

| Aspecto | Resultado |
|---|---|
| Hojas | v1: 30 · v2: 33 |
| Hojas de datos comunes | 27, **idénticas** |
| Hojas de documentación | `_DICCIONARIO`, `_INDICE` y `_VERIFICACION` cambian para describir la estructura nueva |
| Hojas nuevas | `ActorUsuarios` (7 columnas), `SolicitudL3Asignaciones` (4) y `SolicitudL3Estados` (7) |
| Filas en las hojas nuevas | **0** en las tres |

**Qué significa:** la v2 define dónde irán los usuarios, la asignación por origen y el historial de estados de las solicitudes. El sistema ya tiene pantallas y tablas para ese circuito en EXP, pero **no hay datos históricos que cargar**. Por eso la carpeta del sprint incluye ambos archivos: la v1 es la fuente verificada y la v2 es la estructura esperada.

---

## 5. Migraciones pendientes

| Migración | Qué agrega | EXP | Nube | Base real |
|---|---|---|---|---|
| `20261109172523_TeacherRequestsAndIntervalStock` | Rol docente, solicitudes, revisión y stock por horario | ✅ | ✅ | ⚪ |
| `20261110201202_PlanningAssignmentsAndConsumables` | Origen por línea y por ítem de salida | ✅ | ✅ | ⚪ |
| `20261115223433_PlanningStudentCount` | Estudiantes por solicitud | ✅ | ⚪ | ⚪ |
| `20261116004453_DayRecoveryReasonCategory` | Categoría del motivo de suspensión | ✅ | ⚪ | ⚪ |

Además, la lista de equipos por gestión sigue sin reconstruirse en la base real: tiene **1 plan**, y en EXP la reconstrucción creó 2.493.

---

## 6. Lo que falta y qué impide

| Dato | Cargado | Qué impide hoy |
|---|---|---|
| Vida útil por tipo de equipo | **121 / 121** ✅ | — |
| Fecha de adquisición de las unidades | **0 / 644** | Calcular la vida útil restante |
| Fecha de fabricación | **0 / 644** | Es la alternativa a la anterior |
| Valor de adquisición | 536 / 644 | Valor contable completo |
| Número de serie | 69 / 644 | Identificar por serie |
| Capacidad de los laboratorios | **0 / 24** | Activar el control de aforo |
| Existencias de insumos de recetas | **30 / 493** artículos | Confiar en el semáforo de material; 463 no tienen existencia |
| Prácticas de catálogo por materia | 19 / 41 materias | Solicitar o editar con catálogo en las otras 22 |
| Vínculo artículo ↔ equipo | 0 / 1.374 | Relacionar un incidente con el equipo exacto |
| Grupos de estudiantes | 0 | Completar la reforma de grupos «A1» |

---

## 7. Observaciones de calidad

Cada dato dudoso se registró en lugar de corregirse en silencio. Hay **7.459 observaciones, todas abiertas**: las 7.448 de la importación y 11 del 26/10.

| Severidad | Cantidad |
|---|---|
| Información | 5.031 |
| Advertencia | 2.415 |
| Error | 13 |

| Tipo | Cantidad | Qué significa |
|---|---|---|
| Valor «por confirmar» | 4.273 | El Excel dice «Por confirmar» |
| Requiere revisión | 1.164 | El Excel marca el registro para revisar |
| Artículo «por confirmar» | 700 | Artículo activo, pendiente de confirmar |
| Práctica fuera del catálogo | 581 | La solicitud nombra una práctica que no está en el catálogo |
| Línea repetida en la fuente | 164 | El mismo artículo aparece dos veces |
| Horarios superpuestos en la fuente | 139 | Dos solicitudes chocan |
| Carrera no coincide | 122 | La carrera no corresponde a la materia |
| Cantidad o unidad sospechosa | 102 | Conviene revisar cantidad o unidad |
| Otros | 214 | Actores sin vincular, horarios no alineados, valores vacíos y otros |

---

## 8. Qué hay que pedir o decidir

1. **Fechas de adquisición** o de fabricación de las 644 unidades.
2. **Capacidad** de los 24 laboratorios.
3. **Inventario físico** de los 463 artículos de recetas sin existencia.
4. **Prácticas del catálogo** de las 22 materias que no tienen.
5. **Datos para las 3 hojas nuevas** de `BaseGastronomia_v2.xlsx`, si existen.
6. **Confirmación de los valores «por confirmar»**.
7. **Qué hacer con los registros del 26/10**: conservarlos si son reales o retirarlos si fueron de prueba.

---

## 9. Cómo se protege la base

- **Hoy no se modificó la base real.** Todas las consultas fueron de lectura.
- **Las pruebas se hacen en una copia** (`DB_Laboratorios_Univalle_EXP`).
- **Nada se borra físicamente:** hay 39 filtros globales de baja lógica.
- **Hay respaldos manuales:** el más reciente localizado de la base real es del 09/11/2026.
- **Pendiente (transparencia):** el respaldo diario automático falla porque busca `DB_Laboratorios_Univalle`, que ya no existe. Su última ejecución correcta fue el **15/10/2026**, y el respaldo de registros de 15 minutos no tiene ninguna ejecución correcta en el historial.
