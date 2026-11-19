# Estado de la base de datos real

**Proyecto:** Laboratorios Univalle — Gastronomía
**Base:** `DB_Laboratorios_Univalle_REAL` (SQL Server, local) · **Fuente:** `BaseGastronomia_v1.xlsx`
**Consultas y verificación:** 26/10/2026, en modo solo lectura. Salidas completas en `outputs/presentacion-sprint2/datos/`.

---

## 1. ¿La base está «fina»? Respuesta corta

| Pregunta | Respuesta |
|---|---|
| ¿Es **fiel** al Excel del cliente? | **Sí.** 27 hojas, 23.542 filas y 44.743 celdas comparadas: **0 diferencias** y 0 violaciones de integridad. |
| ¿Es **trazable**? | **Sí.** Cada registro importado guarda su fila de origen, y cada problema encontrado quedó registrado. |
| ¿Está **completa**? | **No todavía.** El Excel trae huecos que el sistema no puede inventar: fechas de adquisición, capacidades de los ambientes, existencias de casi todos los insumos de las recetas y muchos valores «por confirmar». Ver §4. |

**En una frase:** la base refleja con exactitud lo que el cliente entregó. Donde el cliente no entregó un dato, el sistema lo marca y no lo inventa.

---

## 2. Qué contiene

| Área | Datos |
|---|---|
| **Inventario** | 121 equipos (tipos) · 644 unidades físicas · 1.374 artículos · 700 existencias · 24 laboratorios · 10 categorías · 5 unidades de medida |
| **Mantenimiento (historial)** | 27 gestiones históricas · 1.599 verificaciones (L-6) · 329 solicitudes (L-7) · 413 unidades en solicitudes · 1.209 mantenimientos (L-8) · 387 planes (L-48) |
| **Planificación (historial 2026)** | 1.037 planificaciones (559 de 2026-1 y 478 de 2026-2) · 11.451 líneas de material |
| **Catálogo académico** | 41 materias · 196 prácticas · 2.111 líneas de receta · 869 horarios · 147 grupos por materia · 41 semanas y 237 días académicos |
| **Personas** | 57 personas (docentes y técnicos) · 46 asignaciones de rol |
| **Gestiones activas** | `2026-2` (preventiva) y `CORR-2026-2` (correctiva), activadas el 19/10 con respaldo previo |

---

## 3. Verificación contra el Excel (26/10/2026)

El script `Tools/ExcelImport/verify_v12_full.py` compara la base contra el Excel en cuatro niveles:
1. **Copia cruda:** cada celda de cada fila está guardada tal cual (23.542 filas, 0 diferencias).
2. **Claves:** cada fila del Excel existe en su tabla destino (0 faltantes).
3. **Valores tipados:** celda por celda, con fechas, números y textos ya convertidos (44.743 celdas, 0 diferencias).
4. **Integridad:** `DBCC CHECKCONSTRAINTS` sobre toda la base (0 violaciones).

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

---

## 4. Lo que falta y qué impide

| Dato | Cargado | Qué impide hoy |
|---|---|---|
| Vida útil por tipo de equipo | **121 / 121** ✅ | — |
| Fecha de adquisición de las unidades | **0 / 644** | No se calcula la **vida útil restante** de ninguna unidad |
| Fecha de fabricación | **0 / 644** | Es la alternativa a la anterior; tampoco está |
| Valor de adquisición | 536 / 644 | Valor contable incompleto en 108 unidades |
| Número de serie | 69 / 644 | Identificación por serie limitada (el número de inventario sí está en las 644) |
| Capacidad de los ambientes | **0 / 24** | La regla de **aforo** está hecha pero no actúa |
| Existencias de insumos de recetas | **28 / 493** artículos | La **disponibilidad de material** marca «sin stock registrado» en 465 artículos |
| Prácticas de catálogo por materia | 19 / 41 materias | En 22 materias no se puede editar la planificación importada |
| Vínculo artículo ↔ equipo | 0 / 1.374 | Un incidente L-5 sobre un equipo se registra sobre el artículo entregado |
| Salidas y devoluciones L-3 reales | 0 | El Excel no las trae: el circuito está listo pero sin historial |
| Tipo de las gestiones históricas | 26 de 27 «por confirmar» | Se muestran como gestiones históricas sin tipo |

![Detalle de una unidad: «12 años de vida estimada», pero vida útil restante «Sin datos» porque falta la fecha de adquisición (captura del 15/10).](../../outputs/e2e/41-_EquipmentUnits_Details_id_1.png)

---

## 5. Observaciones de calidad registradas

Durante la importación, cada dato dudoso se registró como **observación**, en lugar de corregirlo en silencio. Hay **7.448 observaciones, todas abiertas**: falta que el cliente las confirme.

| Severidad | Cantidad |
|---|---|
| Información | 5.031 |
| Advertencia | 2.404 |
| Error | 13 |

| Tipo | Cantidad | Qué significa |
|---|---|---|
| Valor «por confirmar» | 4.273 | El Excel dice «Por confirmar». Los más frecuentes: tipo de servicio de 1.204 mantenimientos, estado operativo de 595 unidades, condición física de 550 unidades, estado y tipo de 387 planes, estado y prioridad de 329 solicitudes y clasificación de 121 equipos |
| Requiere revisión | 1.164 | El Excel marca el registro como «Requiere revisión» |
| Artículo «por confirmar» | 700 | Artículos cargados como activos, pero pendientes de confirmar |
| Práctica fuera del catálogo | 581 | La solicitud nombra una práctica que no existe en el catálogo de la materia |
| Línea repetida en la fuente | 164 | El mismo artículo aparece dos veces en una solicitud (se cargan las dos, sin sumar) |
| Horarios superpuestos en la fuente | 139 | Dos solicitudes del Excel chocan en el mismo laboratorio o con el mismo docente |
| Carrera no coincide | 122 | La carrera del formulario no es la de la materia |
| Cantidad o unidad sospechosa | 102 | Por ejemplo, 100 «piezas» que probablemente son gramos |
| Otros | 203 | Entre ellos, los 13 de severidad Error: 9 solicitudes sin fecha de práctica (se cargaron con la fecha de importación) y 4 sin laboratorio |

---

## 6. Qué hay que pedir al cliente

1. **Fechas de adquisición** (o de fabricación) de las 644 unidades, para calcular la vida útil restante.
2. **Capacidad** de los 24 ambientes, para activar el control de aforo.
3. **Inventario físico de insumos** (los 465 artículos de recetas sin existencia), para que la disponibilidad de material sea confiable.
4. **Prácticas del catálogo** de las 22 materias que no tienen.
5. **Confirmación de los valores «por confirmar»**, empezando por el estado operativo y la condición física de las unidades.
6. **Tipo de las gestiones históricas** (preventiva o correctiva).
7. **Fecha real** de 9 solicitudes y **laboratorio** de otras 4, que no los traen.

---

## 7. Cómo se protege la base

- **Nadie prueba sobre la base real.** Toda prueba que escribe datos se hace en una copia desechable, restaurada desde un respaldo.
- **Antes de cualquier cambio** se hace un respaldo verificado. Por ejemplo, `REAL_PRE_MIGRACION_20261018` y `REAL_PRE_GESTIONES_QA_20261019`. Después del cambio se vuelve a comparar contra el Excel.
- **Nada se borra físicamente:** las bajas son lógicas, con 38 filtros globales.
- **Pendiente (transparencia):** los **respaldos automáticos** programados apuntan a una base que ya no existe. Hoy los respaldos son manuales, hasta reapuntarlos.
