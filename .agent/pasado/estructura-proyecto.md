# 📂 Guía de Estructura de Proyecto: Laboratorios Univalle

Este documento explica la organización de carpetas, archivos y las decisiones arquitectónicas del proyecto para asegurar que cualquier desarrollador entienda el flujo y el estándar de calidad.

## 🏗️ Estructura Raíz

| Carpeta / Archivo | Descripción |
|-------------------|-------------|
| **`Pages/`** | El corazón de la aplicación (UI). Contiene las Vistas Razor (`.cshtml`) y su lógica (`.cshtml.cs`). |
| **`Models/`** | Definiciones de datos (Entidades de BD como `User`, `City`, `Request`) y Enums. |
| **`Helpers/`** | **NUEVO**. Clases estáticas y métodos de extensión que centralizan la lógica transversal (Mensajes, Seguridad, Normalización). |
| **`Services/`** | Lógica de negocio pura (ej: Generador de PDFs, Servicios de Email) separada de la UI. |
| **`wwwroot/`** | Archivos estáticos públicos (CSS, JS, Imágenes). |
| **`_TemplateReference/`** | **NO TOCAR**. Archivo histórico de la plantilla original para consultas rápidas de diseño. |
| **`.agent/`** | Documentación técnica y manuales de estándares para el equipo. |
| `Program.cs` | Configuración de Inyección de Dependencias, Middleware y Base de Datos. |

---

## 🚀 Arquitectura y Mejores Prácticas Implementadas

El proyecto sigue un enfoque de **Arquitectura Limpia** y **Separación de Responsabilidades (SoC)** mediante el uso de Helpers especializados:

### 1️⃣ Centralización de Mensajes (`NotificationHelper.cs`)
*   **Problema**: Mensajes escritos a mano (hardcoded) en cada archivo, causando inconsistencias y errores de dedo.
*   **Solución**: Todos los mensajes UI (éxito, error, duplicados) están en una clase estática centralizada.
*   **Beneficio**: Cambiar un mensaje en un solo lugar actualiza todo el sistema automáticamente.

### 2️⃣ Normalización y Validación de Datos (`StringExtensions.cs`)
*   **Lógica**:
    *   `.Normalize()`: Para comparaciones seguras (SQL).
    *   `.Clean()`: Para guardado estético.
    *   `.IsValidName()`: **NUEVO**. Regex que solo permite letras, acentos, espacios y guiones.
*   **Beneficio**: Blindaje total contra caracteres especiales (`&%$#`) y duplicados.

### 3️⃣ Validación de Doble Capa (IRT + Server)
*   **Frontend**: Uso de `jqBootstrapValidation` con atributos `pattern` y `data-validation-*-message` para feedback instantáneo.
*   **Backend**: Refuerzo con `IsValidName()` y `ModelState.AddModelError` para integridad final.

### 3️⃣ Seguridad XSS y Encoding (`RazorJsHelper.cs`)
*   **Lógica**: Uso de `@Html.JsTempData("Key")` para renderizar mensajes en JavaScript.
*   **Beneficio**: Escapa correctamente acentos, comillas y caracteres especiales, evitando que el JavaScript se rompa o sea vulnerable a ataques XSS.

### 4️⃣ Gestión de Estado Temporal (`TempDataExtensions.cs`)
*   **Lógica**: Métodos de extensión como `TempData.Success("msg")`, `TempData.Error()` y `TempData.Warning()`.
*   **Beneficio**: Elimina el uso de cadenas de texto fijas ("SuccessMessage") en el código C#, reduciendo errores de tipografía.

---

## 🌐 Carpeta `wwwroot` (Archivos Públicos)

*   **`lib/nice-admin/`**: Contiene la plantilla original. Los assets se deben referenciar desde aquí usando la ruta canónica `~/lib/nice-admin/assets/...`.
*   **`css/site.css`** & **`js/site.js`**: Únicos archivos donde se deben realizar personalizaciones globales propias.

---

## ⚠️ Reglas de Oro para Desarrolladores
1.  **NUNCA** utilices mensajes de texto directo en el PageModel; usa `NotificationHelper`.
2.  **SIEMPRE** normaliza las entradas del usuario antes de validar duplicados en la base de datos.
3.  **USA** `DateTime.UtcNow` para registros de auditoría para evitar conflictos de zona horaria.
4.  **EVITA** inyectar `TempData` directo en JS; usa el helper `JsTempData`.
