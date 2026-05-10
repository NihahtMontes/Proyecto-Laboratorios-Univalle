# Problemas Detectados: Reportes Excel + Imagen en Activo + Seed Data

> Análisis realizado post-migración PostgreSQL, commit `0988e78`.

---

## 1. Crash al Descargar Reportes Excel

### Síntoma
Al descargar cualquier reporte Excel (L-7, L-8, L-48, Adquisición), la aplicación se cuelga o crashea.

### Causa Raíz Principal: L7.xlsx es un template anormalmente pesado

| Template | Tamaño | Riesgo |
|----------|--------|--------|
| **L7.xlsx** | **848 KB** | **ALTO** — Probable causa de crash |
| Adquisicion.xlsx | 44 KB | Moderado |
| L8.xlsx | 110 KB | Bajo |
| L48.xlsx | 21 KB | Bajo |
| L-3, L-6 | Sin template (generan desde cero) | Seguro |

Un template de Excel de una hoja con celdas y mergeos debería pesar 20-50 KB. Los 848 KB de L7.xlsx sugieren que contiene:
- Imágenes/logos embebidos
- Dibujos VML (legacy binary drawing objects)
- Formato condicional complejo
- Objetos de dibujo heredados

EPPlus 7.0.0 (primera release de la major version, con bugs conocidos) no maneja bien estos objetos al parsear y manipular el template, provocando crash nativo.

### Causa Secundaria: EPPlus 7.0.0

El proyecto usa `EPPlus 7.0.0` (verificado en `project.assets.json`). Es la primera release de la versión major, con regresiones conocidas en:
- Parseo de templates con objetos embebidos complejos
- Manipulación de celdas merged después de limpiar valores
- `SaveAs()` en packages cargados desde `FileInfo`

### Causa Terciaria: Catch silenciosos ocultan errores

En `ReportService.cs`, múltiples secciones usan:
```csharp
try { worksheet.Cells[r, c].Value = null; } catch { }
try { range.Merge = true; } catch { }
```

Esto oculta `InvalidOperationException` y `COMException` del template corrupto. Los errores desaparecen silenciosamente y explotan al hacer `SaveAs()`.

### Causa Adicional: Includes faltantes en DownloadL7

`Controllers/ReportsController.cs` endpoint `DownloadL7` no carga `.Include(r => r.Equipment)` ni `.Include(r => r.EquipmentUnit).ThenInclude(u => u.Laboratory)`. El servicio usa null-conditional operators como fallback, pero podría producir datos incompletos.

### Fix Propuesto

1. **Actualizar EPPlus** de 7.0.0 a 7.5.x (último patch estable)
2. **Limpiar L7.xlsx**: Abrir en Excel, quitar imágenes/dibujos/logos, dejar solo formato de celdas y mergeos, re-guardar
3. **Eliminar `L6V2.xlsx`** del directorio templates (ya no se usa, documentado como causa de crash)
4. **Reemplazar catch silenciosos** por logging o manejo explícito
5. **Agregar `.Include()` faltantes** en `DownloadL7`
6. **Considerar L-7 desde cero** (como L-6 y L-3 ya lo hacen) para eliminar dependencia de template

---

## 2. Imagen en Activo (Equipment Upload) — 3 Bugs

### Bug Crítico: Espacios en nombres de archivo rompen `<img src>`

Los archivos subidos conservan espacios del nombre original:
```
b18d7445-e611-40d1-bb2f-f89afe46e1fa_Captura de pantalla 2026-02-26 002349.png
```

La vista usa:
```html
<img src="~/uploads/equipment/@Model.Equipment.ImageUrl" />
```

El navegador no puede resolver URLs con espacios sin codificar → **imagen rota/invisible**.

**Fix:** Sanear el nombre del archivo (reemplazar espacios con `-`) Y codificar la URL:
```html
<img src="@Url.Content($"~/uploads/equipment/{Uri.EscapeDataString(Model.Equipment.ImageUrl ?? "")}")">
```

### Bug Alto: Sin try-catch en file I/O

En `Create.cshtml.cs` (líneas 124-140) y `Edit.cshtml.cs` (líneas 144-157), la escritura del archivo está **fuera** de cualquier try-catch. Si `_environment.WebRootPath` es null, el disco está lleno, o hay problemas de permisos, la request crashea sin manejo.

**Fix:** Envolver file I/O en try-catch con rollback (borrar archivo si DB falla).

### Bug Alto: Missing hidden field para ExistingImageUrl

En `Edit.cshtml` no existe `<input type="hidden" asp-for="Input.ExistingImageUrl" />`. Al fallar validación y re-renderizar, `ExistingImageUrl` es null → preview de imagen desaparece aunque el dato está preservado en BD.

**Fix:** Agregar hidden field y repoblar en OnPost cuando ModelState es inválido.

### Bug Moderado: Sin validación server-side de tipo/tamaño

Solo existe `accept="image/*"` client-side. Un usuario puede bypassearlo y subir archivos `.exe`, `.pdf`, o imágenes de 50MB.

**Fix:** Validar `ImageUpload.ContentType.StartsWith("image/")` y `ImageUpload.Length < maxBytes` server-side.

### Bug Menor: Imágenes viejas no se eliminan al reemplazar

En Edit, al subir nueva imagen, la vieja permanece en disco como archivo huérfano.

### Bug Menor: Inconsistencia en sanitización

- Create usa `Path.GetFileName()` (sanitiza)
- Edit usa `Input.ImageUpload.FileName` directamente (NO sanitiza — riesgo de path traversal)

---

## 3. Seed Data — Es Coherente, Sin Bug

### Análisis

Se revisó `Data/DbInitializer.cs` completo. Los datos seed **son coherentes**:

| Escenario | Equipo | Descripción | Coherencia |
|-----------|--------|------------|------------|
| L-7 (eu5) | Microondas Industrial | "Magnetrón no calienta adecuadamente." | El magnetron es el componente que calienta en un microondas |
| L-8 Planeado (eu6) | Batidora Industrial | "Cambio de aspas y engrase general." | Las aspas son parte central de una batidora |
| L-8 Ejecutado (eu7) | Espectrofotómetro UV-Vis | "Calibración de lente óptico UV." | Usa lentes ópticos UV para medir absorbancia |

Los números `2`, `1`, `1` que se ven en la interfaz probablemente son IDs o counts de la vista, no datos incoherentes del seed.

### Posible problema

Si la BD no se llenó completamente al hacer drop/recreate, las entidades dependientes (Verifications, ManagementPlans, Requests, Maintenances) pueden haberse insertado parcialmente. Verificar que existan:
- 7 EquipmentUnits
- 7 Verifications (1 por unidad)
- 7 ManagementPlans (1 por unidad)
- 2 Requests (eu5, eu6, eu7 con sus descripciones)
- 2 Maintenances (eu6, eu7)

---

## Archivos Clave Involucrados

| Archivo | Rol |
|---------|-----|
| `Services/ReportService.cs` | Generación de todos los reportes Excel |
| `Controllers/ReportsController.cs` | Endpoints de descarga |
| `wwwroot/templates/L7.xlsx` | Template sospechoso (848 KB) |
| `wwwroot/templates/L6V2.xlsx` | Template obsoleto, ya no referenciado |
| `Pages/Equipment/Create.cshtml.cs` | Upload de imagen (sin try-catch) |
| `Pages/Equipment/Edit.cshtml.cs` | Upload de imagen (sin sanitización) |
| `Pages/Equipment/Details.cshtml` | Display de imagen (URL sin codificar) |
| `Data/DbInitializer.cs` | Seed data (coherente) |
| `Program.cs` | EPPlus license + static files config |

---

## Prioridad de Fixes

| # | Fix | Prioridad | Impacto |
|---|-----|-----------|---------|
| 1 | Limpiar/reemplazar L7.xlsx | **Crítica** | Todas las descargas L-7 crashean |
| 2 | Actualizar EPPlus 7.0.0 → 7.5.x | **Alta** | Estabilidad general de reportes |
| 3 | Sanear nombres de archivo (espacios) | **Alta** | Imágenes no se muestran |
| 4 | Codificar URL en `<img src>` | **Alta** | Imágenes no se muestran |
| 5 | Try-catch en file I/O | **Alta** | Crashes al subir imágenes |
| 6 | Hidden field ExistingImageUrl | **Media** | UX: preview desaparece |
| 7 | Reemplazar catch silenciosos en ReportService | **Media** | Debuggear crashes futuros |
| 8 | Includes faltantes en DownloadL7 | **Media** | Datos incompletos en reporte |
| 9 | Validación server-side imagen | **Media** | Seguridad |
| 10 | Eliminar L6V2.xlsx | **Baja** | Limpieza |
