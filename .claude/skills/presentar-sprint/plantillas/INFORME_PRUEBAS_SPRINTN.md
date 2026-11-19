# Informe de pruebas — Sprint {{N}}

**Proyecto:** {{proyecto}}
**Periodo:** {{dd/mm/aaaa}} → {{dd/mm/aaaa}}
**Fuentes:** `Tests/Laboratorios.Tests`, `Tools/E2E`, {{carpetas de outputs/ usadas}} y `outputs/presentacion-sprint{{N}}/datos`.

---

## 1. Resultados en una tabla

| Prueba | Qué comprueba | Resultado | Fecha |
|---|---|---|---|
| Tests unitarios (C#, xUnit) | Reglas de negocio aisladas | **{{n}} / {{n}} en verde** | {{dd/mm}} |
| Pruebas automáticas HTTP (Python) | La app usada como una persona, comparada con la base | **{{n}} comprobaciones** en {{n}} scripts | {{dd/mm}} |
| Invariantes SQL | Reglas que los datos cumplen siempre | **{{n}} / {{n}}** | {{dd/mm}} |
| QA de un integrante del equipo | Observaciones corregidas y verificadas | **{{n}} / {{n}}** | {{dd/mm}} |
| Navegador real (Chromium) | Pantallas, alertas y errores de JavaScript | **{{n}} / {{n}}** | {{dd/mm}} |
| Base real contra el Excel | Fidelidad de los datos | **{{n}} celdas, {{n}} diferencias** | {{dd/mm}} |

{{Aclarar qué se volvió a correr hoy y qué se cita de corridas anteriores, con su fecha.}}

---

## 2. Los cuatro tipos de prueba

1. **Tests unitarios.** {{qué prueban, 2 ejemplos}}
2. **Pruebas HTTP automáticas** (`Tools/E2E/http_cases_*.py`). {{cómo funcionan}}
3. **Invariantes SQL** (`Tools/E2E/sql_invariantes.py`). {{2 ejemplos}}
4. **Navegador real.** {{qué cubre}}

---

## 3. Las pruebas encontraron defectos reales

### Ronda del {{dd/mm}} ({{áreas}})

| Hallazgo grave | Qué pasaba | Estado |
|---|---|---|
| {{H-0xx}} | {{síntoma visible para el usuario}} | Corregido |

| Área | Fallas al empezar | Fallas al cerrar |
|---|---|---|
| {{área}} | {{n}} | **{{n}}** |

### Hallazgo abierto (transparencia)
- **{{tema}}.** {{qué pasa, evidencia copiada en datos/, causa probable y corrección propuesta}}

---

## 4. Resultado por script Python

| Área | Script | Comprobaciones | Fallas |
|---|---|---|---|
| {{área}} | `{{script}}` | {{n}} | {{n}} |
| **Total** | **{{n}} scripts** | **{{n}}** | **{{n}}** |

{{Explicar cada falla restante: si es del sistema o un artefacto de la copia de prueba.}}

---

## 5. Capturas de las pruebas

### {{Módulo}}

![{{Qué demuestra}} ({{dd/mm}}).]({{ruta}})

---

## 6. Lo que no se probó

| Tema | Por qué | Riesgo |
|---|---|---|
| {{tema}} | {{motivo}} | {{Alto/Medio/Bajo}} |

---

## 7. Cómo repetir las pruebas

```
dotnet test Tests/Laboratorios.Tests -m:1
cd Tools/ExcelImport && python verify_v12_full.py
bash Tools/E2E/run_scripts.sh <carpeta_salida> <scripts...>
```
