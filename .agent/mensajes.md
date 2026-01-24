    # Guía de Implementación: Mensajes UI y Notificaciones

    Esta guía define el estándar técnico para el manejo de feedback al usuario (Éxito, Error, Advertencia) utilizando SweetAlert2 y un sistema de Helpers centralizado.

    ## 🎯 Estándar de Implementación

    ### 1. Definición en el Backend (`PageModel`)
    Nunca escribas mensajes de texto directamente. Utiliza `NotificationHelper` y los métodos de extensión de `TempData`.

    ```csharp
    // ✅ CORRECTO: Centralizado y Limpio
    if (exists) {
        ModelState.AddModelError("Input.Name", NotificationHelper.Countries.CountryNameDuplicate);
        return Page();
    }

    // ... guardar cambios ...
    TempData.Success(NotificationHelper.Countries.Created(country.Name));
    return RedirectToPage("./Index");
    ```

    ### 2. Renderizado Seguro en la Vista (`.cshtml`)
    Para evitar que caracteres especiales (acentos, comillas) rompan el JavaScript, usa `JsTempData`.

    ```razor
    @section Scripts {
        <script src="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.all.min.js"></script>
        <script>
            @if (TempData["SuccessMessage"] != null) {
                <text>
                swal({
                    title: "¡Logrado!",
                    text: @Html.JsTempData("SuccessMessage"), // Encoding seguro
                    type: "success",
                    confirmButtonColor: "#2962ff",
                    timer: 4000
                });
                </text>
            }
        </script>
    }
    ```

    ---

    ## 📁 Componentes del Sistema de Mensajes

    ### 🔧 `NotificationHelper.cs`
    Centraliza todos los strings del sistema. Si necesitas cambiar un mensaje, hazlo aquí, no en la vista.
    *   **Success**: `Created(name)`, `Updated(name)`, `Deleted(name)`.
    *   **Validation**: `CountryNameDuplicate`, `EmailDuplicate`, etc.

    ### 🔧 `RazorJsHelper.cs` (Seguridad XSS)
    Soluciona el problema de los caracteres especiales decodificando el HTML y serializando a JSON.
    *   **Antes**: `text: "El país 'Perú'"` 💥 (Error JS)
    *   **Ahora**: `text: "El pa\u00EDs 'Per\u00FA'"` ✅ (Correcto)

    ### 🔧 `TempDataExtensions.cs`
    Proporciona métodos semánticos para evitar errores de escritura en las llaves del diccionario:
    *   `TempData.Success("msg")`
    *   `TempData.Error("msg")`
    *   `TempData.Warning("msg")`

    ---

    ## 🔍 Análisis Técnico: SweetAlert v7 vs v11

    Es vital recordar que este proyecto utiliza **SweetAlert2 v7.19.3** (incluida en NiceAdmin).

    | Característica | v7 (Nuestra versión) | v11+ (Moderna) |
    |---|---|---|
    | **Función** | `swal({ ... })` | `Swal.fire({ ... })` |
    | **Ícono** | `type: "success"` | `icon: "success"` |
    | **Progreso** | ❌ No disponible | `timerProgressBar: true` |

    ---

    ## 🗑️ Flujo de Eliminación Estándar

    1.  **Index**: El botón de la tabla es un enlace simple `<a>` hacia la página Delete. No uses popups directos en el listado para permitir una confirmación con más detalle.
    2.  **Delete.cshtml**: Utiliza el diseño **"Clean Card"** (Tarjeta blanca centrada, icono grande `ti-alert`, botones redondeados).
    3.  **Resultado**: Tras el post exitoso, el servidor redirige al Index con un `TempData.Success(...)`.

    ---

    ## ✅ Checklist de Mensajes
    - [ ] ¿El mensaje sale del `NotificationHelper`?
    - [ ] ¿Se usó `TempData.Success/Error/Warning` en el C#?
    - [ ] ¿Se usó `@Html.JsTempData()` en el JavaScript de la vista?
    - [ ] ¿La alerta tiene el `type` correcto (v7)?
    - [ ] ¿Los nombres de entidades en el mensaje fueron procesados con `.Clean()`?