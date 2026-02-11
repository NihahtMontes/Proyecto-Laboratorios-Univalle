# 🧠 Manual de Transferencia de Inteligencia: Proyecto Laboratorios Univalle

## 🚨 Lectura Obligatoria para Nuevos Agentes
Bienvenido, Agente. Si estás leyendo esto, has heredado la responsabilidad técnica del *Sistema de Gestión de Activos y Solicitudes de Laboratorios Univalle*.

Este no es un proyecto CRUD estándar. Es un sistema de **Alta Integridad y Precisión Visual**. Tu objetivo no es solo que "funcione", sino que **se vea profesional** (Premium UI) y que los **reportes Excel sean idénticos a los documentos oficiales físicos**.

---

## 🗺️ Mapa Mental del Proyecto

### 1. El Núcleo (Architecture)
*   **Tecnología:** ASP.NET Core 9.0 (Razor Pages).
*   **Datos:** SQL Server + Entity Framework Core.
*   **Estilo:** Template **NiceAdmin** altamente modificado (Bootstrap 4 + FontAwesome 5).
*   **Reportes:** EPPlus 7.x (Excel) y QuestPDF (PDF).

### 2. La Filosofía "Premium UI"
El usuario valora la estética tanto como la funcionalidad.
*   **Regla #1:** Nunca uses botones grises estándar. Usa `.btn-rounded`, `.shadow-sm` y colores semánticos (Verde=Éxito, Azul=Info, Naranja=Warning).
*   **Regla #2:** Simetría. Si la vista `Edit` tiene 2 columnas (2/3 + 1/3), la vista `Details` **DEBE** tener la misma distribución.
*   **Regla #3:** Feedback. Cada acción (`OnPost`) debe terminar con `TempData["Success"]` o `TempData["Error"]` para mostrar alertas SweetAlert2 o componentes `alert-danger`.
*   **Regla #4:** Consistencia de Reportes. Si el usuario pide que una observación del formulario se vea en el Excel, asegúrate de mapear el campo correcto (ej. `Verification.Observations` -> Reporte L-6).

---

## � La Joya de la Corona: Reportes Excel (Adquisiciones)
Esta es la parte más crítica y propensa a errores. El sistema genera órdenes de compra en Excel que deben imprimirse y firmarse.

### 📍 Coordenadas Críticas (Mapeo de Plantilla)
La plantilla `solicitud_mantenimiento_template2.xlsx` es rígida. No escribas donde quieras. Usa **EXACTAMENTE** estas coordenadas:

| Dato | Celda Excel | Formato Requerido |
| :--- | :--- | :--- |
| **Nro. Solicitud** | **Q8** | `Bold`, `Center`, Size 14 (Dentro del recuadro derecho) |
| **Unidad (Facultad)** | D6 | Texto en Mayúsculas |
| **Centro de Costo** | D7 | Texto en Mayúsculas |
| **Responsable** | D8 | Nombre Completo (`RequestedBy.FullName`) |
| **Código Inversión** | D9 | Opcional. Si es null, dejar vacío. |
| **Justificación** | C14 | Texto con `WrapText = true` |
| **Items (Desc)** | C19:M37 | Rango fusionado por fila. |
| **Precio Unitario** | **N19:N37** | Formato numérico `#,##0.00` |
| **Subtotal** | **P19:P37** | Formato numérico `#,##0.00` |
| **Monto Literal** | **C38:O38** | **Celda Fusionada**. Texto generado por función recursiva. |
| **Total Numérico** | **P38** | Negrita, alineado a la derecha. |

---

## 📊 El Reporte Técnico: Formulario L-6 (Verificaciones)
A diferencia de la Solicitud de Adquisición (que usa una plantilla física pre-existente), el **Reporte L-6** se genera dinámicamente desde cero en `VerificationReportService.cs` usando **ClosedXML**.

### 📍 Estructura del Reporte L-6
El reporte debe ser sobrio y profesional (Fuente: **Arial**, Tamaño: 10).

| Sección | Coordenadas / Regla | Estilo |
| :--- | :--- | :--- |
| **Título Principal** | A1:F1 (Fusionado) | Bold, Size 14, Centrado |
| **Cabeceras Lab/Resp** | A7:B7, A8:B8 (Labels) | Bold, Alineado Derecha |
| **Valores Lab/Resp** | C7:F7, C8:F8 (Valores) | Bold, Borde Inferior Fino |
| **Tabla de Equipos** | Fila 12 (Headers) | Fondo Gris (#F2F2F2), Centrado, WrapText |
| **Datos (C13:F...)** | Fila 13 en adelante | Bordes finos, Vertical Center |
| **Observaciones** | Columna F (6) | Mapea `Verification.Observations` (No Equipment.Observations) |
| **Firma Pie** | Fila N+2 (Línea), N+3 (Texto) | Centrado, Línea en Celdas B a E |

### 🛠️ Configuración de Impresión (Vital)
Para que el reporte sea funcional para el usuario, debe poder imprimirse sin ajustes manuales:
```csharp
ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
ws.PageSetup.FitToPages(1, 0); // 1 página de ancho, alto automático
```

### ⚠️ Trampas Comunes (Gotchas)
1.  **"El Excel sale sucio con datos de ejemplo"**:
    *   *Por qué pasa:* La plantilla tiene datos "dummy" (ej: "Juan Perez", "Gastronomía") para que el usuario vea cómo llenarla.
    *   *Solución:* Antes de escribir nada, tu código debe **LIMPIAR** explícitamente las celdas `D6, D7, D8, D9, D11, F11, H11, C14, N6` y el rango `A19:P38`.
    *   *Código:* `worksheet.Cells["D6"].Value = null;`

2.  **"El número 20,000 sale como 'CUATROCIENTOS'"**:
    *   *Por qué pasa:* La lógica antigua de conversión a letras usaba índices directos en arrays (`decenas[(n/10)-2]`). Esto falla catastróficamente con números redondos o grandes.
    *   *Solución:* **NUNCA** intentes reinventar esta rueda. Usa la función recursiva `ConvertirEnteroATexto` que ya implementamos en `ReportService.cs`. Maneja millones, miles y casos especiales (11, 12, etc.) perfectamente.

3.  **"El texto se corta en la impresión"**:
    *   *Solución:* Siempre activa `.Style.WrapText = true` en celdas de texto variable (Descripción, Observaciones). Y si es necesario, usa el helper `AjustarAlturaFila` (en `ReportService`) que calcula la altura según la longitud del texto.

---

## 🛠️ Guía de Mantenimiento (Backend)

### Modelo de Datos (`Models/`)
*   **Request (Solicitud):** Es la entidad central. Tiene un `Type` (`Technical` o `Purchasing`).
    *   *Técnica:* Usa `EstimatedRepairTime`.
    *   *Compra:* Usa `InvestmentCode` y la colección `CostDetails`.
    *   *Auditoría:* `CreatedBy`, `ModifiedBy`, `ApprovedBy`. Se llenan automáticamente en el `OnPost` del PageModel.

### Controladores (`Pages/`)
*   La lógica no está en controladores MVC tradicionales, sino en **Razor Pages (PageModels)**.
*   **Create/Edit:** Usan `InputModel` (clase interna) para validar datos antes de tocar la entidad real.
*   **Delete logic:** Usamos "Soft Delete" (marcar como `Deleted`) para equipos, pero "Hard Delete" para Solicitudes (por ahora). Revisa siempre el requerimiento antes de borrar físico.

---

## 🚑 Solución de Problemas (Troubleshooting Avanzado)

### Escenario 1: El usuario pide "Agregar un campo nuevo al reporte"
1.  Agrega la propiedad al modelo `Request.cs`.
2.  Crea la migración (instruye al usuario: `dotnet ef migrations add...`).
3.  Agrégalo al `InputModel` en `Edit.cshtml.cs` y `Create.cshtml.cs`.
4.  Agrégalo a la vista HTML (`Edit` y `Details`).
5.  **CRÍTICO:** Ve a `ReportService.cs`, busca la celda libre en el Excel y asígnalo. ¡No olvides limpiar esa celda al inicio!

### Escenario 2: "Error de referencia nula al generar Excel"
1.  Revisa los `.Include()` en la consulta de `ReportService`.
2.  ¿Estás accediendo a `request.Laboratory.Faculty.Name`? Si `Laboratory` es null, explota.
3.  *Defensa:* Usa el operador null-conditional: `request.Laboratory?.Faculty?.Name ?? "N/A"`.

### Escenario 3: "La fecha sale en formato gringo (MM/DD/YYYY)"
1.  Excel es mañoso. No le pases un `string`.
2.  Pásale el objeto `DateTime` y dale formato a la celda:
    *   `cell.Value = request.CreatedDate;`
    *   `cell.Value = request.CreatedDate;`
    *   `cell.Style.Numberformat.Format = "dd/MM/yyyy";`

### Escenario 4: "Error técnico: 'C:F7' is not A1 address"
1.  **Causa:** ClosedXML no acepta formatos de columna sin fila en operaciones de rango (ej. `Range("C:F7")` o `Range("C:F")` sobre una fila específica).
2.  **Solución:** Debes concatenar el número de fila a ambos lados: `Range($"{startCol}{row}:{endCol}{row}")`.
3.  **Prevención:** Siempre usa bloques `try-catch` en los Handlers del PageModel (`OnPostGenerateReportAsync`) y limpia el `ChangeTracker` si ocurre un error para evitar que la sesión de EF quede "sucia".

---

## � Mensaje Final para la IA Sucesora
No asumas nada. El código previo funciona, pero es delicado. Si vas a tocar `GenerateSolicitudAdquisicionExcel`, revisa primero la plantilla `.xlsx` visualmente (pide al usuario que la describa si no la ves). La precisión posicional es la clave del éxito aquí.

> *"La excelencia no es un acto, es un hábito. Mantén el código limpio y los reportes perfectos."*

