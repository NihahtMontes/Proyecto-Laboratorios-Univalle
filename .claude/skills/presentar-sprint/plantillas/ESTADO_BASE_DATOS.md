# Estado de la base de datos real

**Proyecto:** {{proyecto}}
**Base:** `{{base}}` ({{motor}}) · **Fuente:** `{{archivo fuente del cliente}}`
**Consultas y verificación:** {{dd/mm/aaaa}}, en modo solo lectura. Salidas completas en `outputs/presentacion-sprint{{N}}/datos/`.

---

## 1. ¿La base está «fina»? Respuesta corta

| Pregunta | Respuesta |
|---|---|
| ¿Es **fiel** a la fuente? | {{Sí/No + cifras de verify_v12_full}} |
| ¿Es **trazable**? | {{filas de origen guardadas + observaciones registradas}} |
| ¿Está **completa**? | {{Respuesta honesta + remitir a §4}} |

**En una frase:** {{resumen}}

---

## 2. Qué contiene

| Área | Datos |
|---|---|
| **{{área}}** | {{volúmenes}} |

---

## 3. Verificación contra la fuente ({{dd/mm/aaaa}})

| Hoja | Filas | Celdas comparadas | Diferencias |
|---|---|---|---|
| {{hoja}} | {{n}} | {{n}} | {{n}} |
| **Total** | **{{n}}** | **{{n}}** | **{{n}}** |

---

## 4. Lo que falta y qué impide

| Dato | Cargado | Qué impide hoy |
|---|---|---|
| {{dato}} | **{{n}} / {{total}}** | {{función que no actúa}} |

---

## 5. Observaciones de calidad registradas

| Severidad | Cantidad |
|---|---|
| Información | {{n}} |
| Advertencia | {{n}} |
| Error | {{n}} |

| Tipo | Cantidad | Qué significa |
|---|---|---|
| {{tipo en español}} | {{n}} | {{explicación con ejemplo}} |

---

## 6. Qué hay que pedir al cliente

1. **{{dato}}** ({{cantidad}}), para {{qué se activa}}.

---

## 7. Cómo se protege la base

- {{pruebas solo en copias, respaldos verificados antes de cambios, bajas lógicas, pendientes de operación}}
