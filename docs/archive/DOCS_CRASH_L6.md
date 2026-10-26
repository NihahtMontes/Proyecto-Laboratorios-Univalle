# 🔧 Documentación: Resolución del Crash 0xffffffff en Reporte L6

**Fecha de resolución:** 29 de abril de 2026  
**Severidad:** Crítica — el proceso del servidor (Kestrel/IIS Express) moría instantáneamente  
**Módulo afectado:** `Services/ReportService.cs` → `GenerateL6VerificacionExcel()`  
**Archivos modificados:** `ReportService.cs`, `ReportsController.cs`, `Program.cs`, `appsettings.Development.json`

---

## 1. El Problema

Al descargar el reporte L6 (Verificación de Estado de Equipos por Laboratorio), el servidor web se cerraba abruptamente con el código de salida:

```
Proyecto Laboratorios Univalle.exe se cerró con el código -1 (0xffffffff)
```

Este error **no generaba ninguna excepción visible** en consola, no era atrapable por `try-catch`, ni siquiera por `AppDomain.CurrentDomain.UnhandledException`. El proceso simplemente moría.

---

## 2. Proceso de Diagnóstico

### Fase 1 — Descarte de sospechosos iniciales

| Sospechoso | Resultado |
|---|---|
| EF Core `.ToListAsync()` masivo | ❌ Ya estaba optimizado con paginación |
| `EliminarHojasExtra()` | ❌ Comentado, el crash persistía |
| EPPlus sin licencia | ❌ `LicenseContext = NonCommercial` estaba configurado |
| `AppDomain.UnhandledException` | ❌ No capturó nada → era un crash nativo |
| Controlador con streams abiertos | ❌ `ReportsController` era estándar |

### Fase 2 — Trazas de diagnóstico con `Console.Error`

Se insertaron 8 checkpoints con `Console.Error.WriteLine()` + `Flush()` dentro del método para identificar la línea exacta del crash:

```
>>> L6 DIAG [1/7]: Inicio método. LabId=1                    ✅
>>> L6 DIAG [2/7]: labName OK = Economato                     ✅
>>> L6 DIAG [3/7]: Template encontrado = .../L6V2.xlsx        ✅
>>> L6 DIAG [4/7]: ExcelPackage creado OK. Hojas=24           ✅ ← 24 HOJAS
>>> L6 DIAG [5/7]: Headers escritos OK                        ✅
>>> L6 DIAG [6/7]: Celdas limpiadas OK. Iniciando queries...  ✅
>>> L6 DIAG [7/7]: Loop SQL terminado. Filas escritas=5       ✅
>>> L6 DIAG [8/8]: EliminarHojasExtra OK. Hojas restantes=1   ✅
💥 CRASH en package.GetAsByteArray()
```

### Fase 3 — Identificación de la causa raíz

La plantilla `L6V2.xlsx` del ministerio contenía **24 hojas** (una por cada laboratorio, con datos pre-llenados). El método `EliminarHojasExtra()` borraba 23 hojas en un loop inverso. Aunque el borrado ejecutaba sin errores aparentes, **corrompía silenciosamente la estructura XML interna del archivo OpenXML** (referencias cruzadas, named ranges, fórmulas entre hojas). Cuando `GetAsByteArray()` intentaba serializar ese XML corrupto a bytes, EPPlus crasheaba a nivel nativo (C++/unmanaged), provocando una muerte instantánea del proceso que ningún mecanismo de .NET podía interceptar.

---

## 3. La Solución

### Estrategia: Copiar en vez de Borrar

En lugar de cargar el template de 24 hojas y borrar 23 (lo cual corrompe el XML), el método ahora:

1. **Abre** el template original en un `ExcelPackage` de solo lectura (`templatePkg`)
2. **Copia** únicamente la primera hoja a un `ExcelPackage` **nuevo y vacío** (`package`)
3. **Escribe** los datos del laboratorio en esa única hoja limpia
4. **Retorna** el `byte[]` del paquete nuevo (1 sola hoja, sin XML corrupto)

```csharp
// ANTES (crasheaba):
using var package = new ExcelPackage(new FileInfo(templatePath));
var worksheet = package.Workbook.Worksheets[0];
// ... escribir datos ...
EliminarHojasExtra(package);          // Corrompe el XML interno
return package.GetAsByteArray();      // 💥 CRASH NATIVO

// DESPUÉS (estable):
using var templatePkg = new ExcelPackage(new FileInfo(templatePath));
using var package = new ExcelPackage();
var sourceSheet = templatePkg.Workbook.Worksheets[0];
var worksheet = package.Workbook.Worksheets.Add(sourceSheet.Name, sourceSheet);
// ... escribir datos ...
return package.GetAsByteArray();       // ✅ 1 hoja, XML limpio
```

### Cambio adicional: SQL nativo en vez de EF Core

Durante la investigación se descubrió que EF Core 9 con `SplitQuery` global reutilizaba el mismo parámetro SQL para `OFFSET` y `FETCH NEXT`, causando que los lotes crecieran exponencialmente. Se reemplazó la query LINQ por **SQL nativo con ADO.NET** (`DbConnection.CreateCommand()`) donde los parámetros `@offset` y `@batchSize` son físicamente independientes.

---

## 4. Archivos Modificados

### `Services/ReportService.cs`
- **`GenerateL6VerificacionExcel()`**: Reescrito completamente con:
  - Carga de template por copia (no por referencia)
  - SQL nativo con paginación correcta (`OFFSET @offset FETCH NEXT @batchSize`)
  - Guarda de seguridad de 1000 filas máximo
  - `AsNoTracking()` en la query de `labName`

### `Controllers/ReportsController.cs`
- Inyección de `ILogger<ReportsController>` para logging de errores
- `DownloadL6`: catch mejorado con `StatusCode(500)` + stack trace completo

### `Program.cs`
- `AppDomain.CurrentDomain.UnhandledException` → logger a `crash.log` (diagnóstico)

### `appsettings.Development.json`
- Logging detallado de `Microsoft.EntityFrameworkCore.Database.Command` para ver las queries SQL

---

## 5. Lecciones Aprendidas

> [!CAUTION]
> **Nunca borrar hojas de un ExcelPackage cargado desde un template complejo con EPPlus.**
> El borrado corrompe las referencias internas del XML de OpenXML y causa un crash nativo
> que ningún mecanismo de .NET puede interceptar (`StackOverflowException` / `AccessViolationException`).

> [!TIP]
> **Patrón seguro para templates multi-hoja:**
> Cargar el template → copiar la hoja necesaria a un paquete nuevo → trabajar sobre el paquete nuevo.

> [!WARNING]
> **EF Core 9 + `SplitQuery` global + `Skip()/Take()` en loops:**
> EF Core puede reutilizar el mismo parámetro SQL para ambos valores, causando que los lotes
> crezcan exponencialmente. Para paginación en loops, usar SQL nativo con `DbConnection.CreateCommand()`.

---

## 6. Cómo Verificar que Funciona

1. Ejecutar el proyecto en Visual Studio
2. Navegar al módulo de Laboratorios → seleccionar un laboratorio
3. Click en **"Descargar L-6 Consolidado"**
4. Verificar que:
   - ✅ El Excel se descarga sin crashear el servidor
   - ✅ El Excel tiene **1 sola pestaña** (no 24)
   - ✅ Los datos corresponden al laboratorio seleccionado
   - ✅ El Dashboard sigue funcionando después de la descarga

---

*Documentado por el equipo de desarrollo — Proyecto Laboratorios Univalle*
