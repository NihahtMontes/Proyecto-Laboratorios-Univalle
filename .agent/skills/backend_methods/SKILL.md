---
name: backend_methods
description: Convenciones exclusivas de Backend, PageModels (OnGet/OnPost), clases internas (InputModels), inyección de dependencias seguras y capturas de excepciones.
trigger: Interacción con el archivo lógico `.cshtml.cs` (PageModel) o cualquier controlador C# base.
author: Antigravity Orquestador Alpha
version: 1.0.0
license: MIT
scope: Backend/PageModels
tools_allowed: [view_file, write_to_file, multi_replace_file_content]
---
# ⚙️ SKILL: Controladores, PageModels y Arquitectura C#

En lugar de controladores web caóticos, el Laboratorio gestiona el estado bidireccional mediante el paradigma **Razor Pages**.

## 1. El Muro de Contención: InputModel
NUNCA utilices `[BindProperty]` apuntando a la entidad desnuda del negocio (Ej. `[BindProperty] public Equipment Equipo { get; set; }`) al capturar la entrada del usuario (Forms). Esta práctica lleva al desastre de seguridad (Overposting / Mass Assignment).
- **Tu Única Opción:** Define una clase pública interna heredada en el page model: `public class InputModel { ... }`, extrae allí las variables necesarias (`Id`, `Name`, `Description`), perfílalas con atributos de validación exclusivas para ESA VISTA (`[Required]`, `[StringLength]`) y expón solo la clase Input.

## 2. Null-Checkers Paranoicos y Coalescing (`??`)
Nadie sabe cuándo el motor de base de datos devolverá listas vacías o referencias cruzadas amputadas.
- Si navegas profundamente en asociaciones `request.Laboratory?.Faculty?.Name`, envuélvelo en coalescencias defensivas: `?? "Dato no asilado"`.
- Protege tus ciclos `.Select(x => ...)` inyectando primero la limpieza previa: `.Where(x => x.Active)`.

## 3. Handlers C# y Try-Catch Total
Las lógicas propensas a colapsos físicos (`System.IO`, correos SMTP, inserciones EF Core que violan constraints sutiles)... necesitan la encapsulación de `try { ... } catch (Exception ex)`.
- **Ante errores (Catch):**
  1. No devuelvas un HTTP 500 al usuario de tajo. 
  2. Loguea en archivo usando `_logger.LogError(...)`.
  3. Establece `TempData.Error(...)` detallando cordialmente el motivo (Verifica el patrón de `NotificationHelper`).
  4. Retorna la Vista actual con el estado de formulario congelado.

## 4. Inyecciones Singleton/Scoped (Prohibido el `new`)
Instanciar directamente manejadores genéricos de servicios (`var gen = new ReportService()`) rompe el testing y la vida de instancias de ASP.NET Core.
- **Tu Responsabilidad:** Inyéctalo todo por constructor (`Constructor Injection`).
- Matricula los servicios de terceros nuevos en el archivo raíz `Program.cs` mediante `.AddScoped<IMisServicios, MisServicios>()` o `.AddSingleton()`.
