# 📘 GUÍA MAESTRA DE IMPLEMENTACIÓN: MÓDULO EQUIPOS
## Estándar de Referencia para el Proyecto Laboratorios Univalle

> **Versión:** 1.0  
> **Última Actualización:** 30 de Enero, 2026  
> **Propósito:** Esta guía documenta el estándar de oro implementado en el módulo de Equipos. Todo módulo nuevo o refactorización debe seguir estos patrones exactos para mantener la consistencia técnica, estética y funcional del sistema.

---

## 📑 TABLA DE CONTENIDOS

1. [Arquitectura de UI (Layouts Premium)](#1-arquitectura-de-ui-layouts-premium)
2. [Frontend: Validaciones y UX](#2-frontend-validaciones-y-experiencia-de-usuario)
3. [Backend: Lógica y Robustez](#3-backend-lógica-y-robustez)
4. [Estrategia de Datos](#4-estrategia-de-datos-y-persistencia)
5. [Sistema de Notificaciones](#5-sistema-de-notificaciones-sweetalert2)
6. [Anti-Patrones (Errores Comunes)](#6-️-anti-patrones-errores-a-evitar)
7. [Templates de Referencia](#7-templates-de-referencia-copy-paste)
8. [Checklist de Calidad](#8-checklist-de-calidad-pre-commit)
9. [Glosario CSS Premium](#9-glosario-css-premium)
10. [Convenciones de Nomenclatura](#10-convenciones-de-nomenclatura)

---

## 1. ARQUITECTURA DE UI (LAYOUTS PREMIUM)

### 1.1 Layout 8-4 (Create/Edit)

**Estructura:** Dos columnas con distribución 8-4 para balancear densidad de información.

#### Columna Principal (col-lg-8)
```html
<div class="col-lg-8">
    <div class="card shadow-sm border-0">
        <div class="card-body p-4">
            <!-- Header con ícono decorativo -->
            <div class="d-flex no-block align-items-center mb-4">
                <div>
                    <h4 class="card-title text-dark">Título de la Página</h4>
                    <h6 class="card-subtitle text-muted">Descripción breve de la funcionalidad</h6>
                </div>
                <div class="ml-auto">
                    <i class="fas fa-edit fa-2x text-warning opacity-5"></i>
                </div>
            </div>

            <!-- Formulario dividido en secciones -->
            <form method="post" novalidate>
                <!-- Sección 1: Identificación -->
                <h5 class="font-weight-bold text-info mb-3">
                    <i class="fas fa-barcode mr-2"></i> Identificación Base
                </h5>
                <!-- Campos aquí -->

                <!-- Sección 2: Ubicación -->
                <h5 class="font-weight-bold text-info mt-4 mb-3">
                    <i class="fas fa-map-marker-alt mr-2"></i> Ubicación y Origen
                </h5>
                <!-- Campos aquí -->
            </form>
        </div>
    </div>
</div>
```

#### Columna Lateral (col-lg-4)
```html
<div class="col-lg-4">
    <!-- Panel de Información Contextual -->
    <div class="card shadow-sm border-0 bg-info text-white mb-4">
        <div class="card-body">
            <h5 class="card-title text-white">
                <i class="fas fa-chart-line mr-2"></i> Ciclo de Vida Útil
            </h5>
            <hr class="border-white opacity-2" />
            <!-- Datos calculados o informativos -->
        </div>
    </div>

    <!-- Panel de Auditoría -->
    <div class="card shadow-sm border-0">
        <div class="card-body">
            <h5 class="card-title text-dark font-weight-bold mb-4">
                <i class="fas fa-history mr-2 text-warning"></i> Control de Auditoría
            </h5>
            <hr />
            <div class="small mt-3">
                <div class="mb-3">
                    <label class="text-muted d-block small uppercase mb-0">Fecha de Alta</label>
                    <span class="text-dark font-weight-bold">@Model.CreatedDate.ToString("dd/MM/yyyy HH:mm")</span>
                </div>
            </div>
        </div>
    </div>
</div>
```

**Clases CSS Obligatorias:**
- Contenedor: `card shadow-sm border-0`
- Cuerpo: `card-body p-4`
- Títulos de sección: `font-weight-bold text-info mb-3`
- Subtítulos: `card-subtitle text-muted`

---

### 1.2 Layout Centrado (Delete/Confirmación)

**Propósito:** Diseño enfocado en la confirmación crítica de acciones destructivas.

```html
<div class="row justify-content-center" style="min-height: 60vh; align-items: center;">
    <div class="col-md-6 col-lg-5">
        <div class="card text-center shadow-sm">
            <div class="card-body p-5">
                <!-- Iconografía de advertencia con doble capa -->
                <div class="mb-4 text-danger">
                    <i class="ti-trash display-3 opacity-2" 
                       style="position: absolute; left: 50%; transform: translateX(-50%); top: 40px;"></i>
                    <i class="fas fa-exclamation-triangle display-4" 
                       style="position: relative; z-index: 1;"></i>
                </div>

                <!-- Título y descripción -->
                <h3 class="card-title text-dark font-weight-bold">¿Retirar este activo?</h3>
                <p class="text-muted mb-4">
                    Estás a punto de dar de baja el equipo <strong class="text-dark">@Model.Equipment.Name</strong>.
                    <br>Esta acción deshabilitará el activo para uso operativo en el sistema.
                </p>

                <!-- Caja de datos clave -->
                <div class="bg-light p-3 rounded mb-4 text-left d-inline-block w-100 border-left border-danger border-3">
                    <div class="d-flex justify-content-between mb-2">
                        <span class="text-muted small uppercase">N° Inventario:</span>
                        <strong class="text-dark">@Model.Equipment.InventoryNumber</strong>
                    </div>
                    <div class="d-flex justify-content-between">
                        <span class="text-muted small uppercase">Estado Actual:</span>
                        <span class="badge badge-pill badge-warning px-3 font-weight-bold">
                            @Model.Equipment.CurrentStatus
                        </span>
                    </div>
                </div>

                <!-- Botones de acción -->
                <form method="post">
                    <input type="hidden" asp-for="Equipment.Id" />
                    <a asp-page="./Details" asp-route-id="@Model.Equipment.Id" 
                       class="btn btn-outline-secondary btn-rounded px-4 mr-2">
                        Revisar Ficha
                    </a>
                    <button type="submit" class="btn btn-danger btn-rounded px-4 shadow-sm font-weight-bold">
                        <i class="fas fa-trash-alt mr-1"></i> Confirmar Baja
                    </button>
                </form>
            </div>
        </div>

        <!-- Nota informativa -->
        <div class="text-center mt-3 text-muted small italic">
            <i class="fas fa-info-circle mr-1"></i> 
            El sistema conservará el historial técnico del activo para fines de auditoría institucional.
        </div>
    </div>
</div>

@section Styles {
    <style>
        .border-3 { border-left-width: 3px !important; }
    </style>
}
```

**Elementos Clave:**
- Icono de fondo traslúcido: `opacity-2` con `position: absolute; top: 40px`
- Borde lateral de advertencia: `border-left border-danger border-3`
- Centrado vertical: `min-height: 60vh; align-items: center`

---

## 2. FRONTEND: VALIDACIONES Y EXPERIENCIA DE USUARIO

### 2.1 Estándar de Etiquetas y Campos

#### Labels
```html
<label asp-for="Input.Name" class="control-label font-weight-bold small uppercase text-muted">
    Nombre del Equipo <span class="text-danger">*</span>
</label>
```

#### Inputs de Texto
```html
<input asp-for="Input.Name" 
       class="form-control" 
       placeholder="Ej: Espectrofotómetro UV-VIS"
       required 
       data-validation-required-message="El nombre es obligatorio"
       pattern="^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.-]+$"
       data-validation-pattern-message="Solo letras, números, puntos y guiones" />
<span asp-validation-for="Input.Name" class="text-danger small"></span>
```

#### Selects (Dropdowns)
```html
<select asp-for="Input.LaboratoryId" 
        class="form-control custom-select" 
        asp-items="ViewBag.LaboratoryId" 
        required>
    <option value="">-- Seleccione Laboratorio --</option>
</select>
```

**Formato de SelectList en Backend:**
```csharp
var laboratories = _context.Laboratories
    .Where(l => l.Status == GeneralStatus.Activo)
    .OrderBy(l => l.Name)
    .Select(l => new { 
        Id = l.Id, 
        DisplayName = $"[{l.Code}] - {l.Name}" 
    })
    .ToList();

ViewData["LaboratoryId"] = new SelectList(laboratories, "Id", "DisplayName");
```

---

### 2.2 Validación JavaScript en Tiempo Real

#### A. Función de Bloqueo de Caracteres

```javascript
@section Scripts {
    <script src="~/lib/nice-admin/assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script>
        $(function () {
            "use strict";
            $("input,select,textarea").not("[type=submit]").jqBootstrapValidation();
            
            // Función para mostrar mensajes de error temporales
            function showTemporaryError($el, message) {
                var $group = $el.closest('.form-group');
                var $help = $group.find('.help-block');
                if ($help.length === 0) {
                    $group.append('<div class="help-block text-danger small font-weight-bold mt-1"></div>');
                    $help = $group.find('.help-block');
                }
                $help.text(message).fadeIn();
                $group.addClass('error');
                
                setTimeout(function() {
                    $help.fadeOut(function() { $(this).text(''); });
                    $group.removeClass('error');
                }, 2000);
            }

            // Patrón: solo letras, números, espacios, guiones, puntos y paréntesis
            var textPattern = /^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-\.\(\)]*$/;
            
            function setupBlocking($selector, pattern, errorMessage) {
                $($selector).on('input', function() {
                    var $field = $(this);
                    var val = $field.val();
                    if (!pattern.test(val)) {
                        $field.val(val.replace(/[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-\.\(\)]/g, ''));
                        showTemporaryError($field, errorMessage);
                    }
                });
            }

            // Aplicar validación agresiva
            setupBlocking('#Input_Name', textPattern, "Carácter no permitido (especial)");
            setupBlocking('#Input_Brand', textPattern, "Marca: Solo letras, números y guiones");
            setupBlocking('#Input_Model', textPattern, "Modelo: Solo letras, números y guiones");
            setupBlocking('#Input_SerialNumber', textPattern, "S/N: Solo caracteres alfanuméricos");
            
            // Validación para Valor USD (solo números y punto decimal)
            $('#Input_AcquisitionValue').on('input', function() {
                var $field = $(this);
                var val = $field.val();
                if (!/^[0-9\.]*$/.test(val)) {
                    $field.val(val.replace(/[^0-9\.]/g, ''));
                    showTemporaryError($field, "Solo se permiten números y punto decimal");
                }
            });
            
            // Validación para Vida Útil (solo números)
            $('#Input_UsefulLifeYears').on('input', function() {
                var $field = $(this);
                var val = $field.val();
                if (!/^[0-9]*$/.test(val)) {
                    $field.val(val.replace(/[^0-9]/g, ''));
                    showTemporaryError($field, "Solo se permiten números enteros");
                }
            });
        });
    </script>
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

#### B. Patrones de Validación por Tipo de Campo

| Tipo de Campo | Patrón Regex | Uso |
|---------------|--------------|-----|
| Alfanumérico con acentos | `/^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-\.\(\)]*$/` | Nombres, Marcas, Modelos |
| Solo letras | `/^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s\(\)-]*$/` | Categorías, Tipos |
| Decimal | `/^[0-9\.]*$/` | Valores monetarios |
| Entero | `/^[0-9]*$/` | Años, Frecuencias |
| Email | `/^[^\s@]+@[^\s@]+\.[^\s@]+$/` | Correos electrónicos |

---

## 3. BACKEND: LÓGICA Y ROBUSTEZ

### 3.1 Normalización de Datos

**REGLA OBLIGATORIA:** Todo string que provenga del usuario debe ser limpiado con `.Clean()` antes de guardarse.

```csharp
// En el método OnPostAsync
Input.Name = Input.Name.Clean();
Input.Brand = Input.Brand?.Clean();
Input.Model = Input.Model?.Clean();
```

**Implementación de StringExtensions.Clean():**
```csharp
public static string Clean(this string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;
    
    // Elimina espacios múltiples y trim
    return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
}
```

---

### 3.2 Validación de Unicidad

#### A. En CREATE (Nuevo Registro)
```csharp
public async Task<IActionResult> OnPostAsync()
{
    // Validación de unicidad del número de inventario
    bool invExists = await _context.Equipments
        .IgnoreQueryFilters() // Incluye registros eliminados lógicamente
        .AnyAsync(e => e.InventoryNumber == Input.InventoryNumber 
                    && e.CurrentStatus != EquipmentStatus.Deleted);

    if (invExists)
    {
        ModelState.AddModelError("Input.InventoryNumber", 
            "El número de inventario ya está en uso por otro equipo activo.");
    }

    if (!ModelState.IsValid)
    {
        // Recargar ViewData para dropdowns
        await LoadDropdownsAsync();
        return Page();
    }

    // Normalización
    Input.Name = Input.Name.Clean();
    
    // Continuar con la creación...
}
```

#### B. En EDIT (Actualización)
```csharp
public async Task<IActionResult> OnPostAsync()
{
    // Validar que el número de inventario no esté en uso por OTRO equipo
    bool invExists = await _context.Equipments
        .IgnoreQueryFilters()
        .AnyAsync(e => e.InventoryNumber == Input.InventoryNumber 
                    && e.Id != Input.Id  // ⭐ EXCLUIR el registro actual
                    && e.CurrentStatus != EquipmentStatus.Deleted);

    if (invExists)
    {
        ModelState.AddModelError("Input.InventoryNumber", 
            "El número de inventario ya está en uso por otro equipo activo.");
    }

    if (!ModelState.IsValid)
    {
        await LoadDropdownsAsync();
        return Page();
    }

    // Continuar con la actualización...
}
```

---

### 3.3 Carga de Relaciones (Eager Loading)

**PROBLEMA COMÚN:** Si ves `Proyecto_Laboratorios_Univalle.Models.Equipment.Laboratory.Name` en la pantalla, significa que el objeto `Laboratory` es `null` porque no se cargó con `.Include()`.

**SOLUCIÓN:** Siempre usar Eager Loading en vistas de Details, Edit y Delete.

```csharp
public async Task<IActionResult> OnGetAsync(int? id)
{
    if (id == null) return NotFound();

    Equipment = await _context.Equipments
        .Include(e => e.Laboratory)
            .ThenInclude(l => l.Faculty)  // Carga anidada si se necesita
        .Include(e => e.EquipmentType)
        .Include(e => e.City)
        .Include(e => e.Country)
        .Include(e => e.CreatedBy)
        .Include(e => e.ModifiedBy)
        .Include(e => e.StateHistory)
            .ThenInclude(h => h.CreatedBy)
        .FirstOrDefaultAsync(m => m.Id == id);

    if (Equipment == null) return NotFound();

    return Page();
}
```

**Regla de Oro:** Si vas a mostrar una propiedad de navegación en la vista (ej. `@Model.Equipment.Laboratory.Name`), DEBES incluirla en el query.

---

## 4. ESTRATEGIA DE DATOS Y PERSISTENCIA

### 4.1 Soft Delete (Borrado Lógico)

**Filosofía:** En sistemas institucionales, los datos NUNCA se destruyen físicamente. Se marcan como eliminados para preservar la integridad histórica.

#### Implementación en Equipment
```csharp
public async Task<IActionResult> OnPostAsync(int? id)
{
    if (id == null) return NotFound();

    var equipment = await _context.Equipments.FindAsync(id);
    if (equipment != null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        
        // ✅ SOFT DELETE: Cambiar estado en lugar de Remove()
        equipment.CurrentStatus = EquipmentStatus.Deleted;
        // equipment.LastModifiedDate y ModifiedById se manejan automáticamente por Interceptor

        // Registrar en historial de estados
        var newHistory = new EquipmentStateHistory
        {
            EquipmentId = equipment.Id,
            Status = equipment.CurrentStatus,
            StartDate = DateTime.Now,
            CreatedDate = DateTime.Now,
            Reason = "Equipment deleted",
            CreatedById = currentUser?.Id
        };

        // Cerrar el último registro de historial activo
        var lastHistory = await _context.EquipmentStateHistories
            .Where(h => h.EquipmentId == equipment.Id && h.EndDate == null)
            .OrderByDescending(h => h.StartDate)
            .FirstOrDefaultAsync();

        if (lastHistory != null)
            lastHistory.EndDate = DateTime.Now;

        _context.EquipmentStateHistories.Add(newHistory);
        await _context.SaveChangesAsync();
    }

    TempData["SuccessMessage"] = "El equipo ha sido dado de baja correctamente.";
    return RedirectToPage("./Index");
}
```

**Ventajas:**
- ✅ Preserva la integridad referencial (no rompe Foreign Keys)
- ✅ Permite auditorías y reportes históricos
- ✅ Posibilidad de "reactivar" registros si fue un error
- ✅ Cumple con normativas contables y de control de activos

---

### 4.2 Validación de Dependencias (Restrict Delete)

Para entidades maestras (como EquipmentTypes), se debe validar que no existan dependencias antes de permitir el borrado.

```csharp
public async Task<IActionResult> OnPostAsync(int? id)
{
    if (id == null) return NotFound();

    // ✅ VALIDAR DEPENDENCIAS
    bool tieneEquipos = await _context.Equipments
        .AnyAsync(e => e.EquipmentTypeId == id && e.CurrentStatus != EquipmentStatus.Deleted);

    if (tieneEquipos)
    {
        TempData["ErrorMessage"] = "No se puede eliminar la categoría porque tiene equipos asociados. " +
                                   "Por favor, reasigne o elimine los equipos primero.";
        return RedirectToPage("./Index");
    }

    var equipmentType = await _context.EquipmentTypes.FindAsync(id);
    if (equipmentType != null)
    {
        _context.EquipmentTypes.Remove(equipmentType);
        await _context.SaveChangesAsync();
    }

    TempData["SuccessMessage"] = "La categoría ha sido eliminada del sistema.";
    return RedirectToPage("/Equipment/Index");
}
```

---

## 5. SISTEMA DE NOTIFICACIONES (SWEETALERT2)

### 5.1 Configuración Global en _Layout.cshtml

El sistema de alertas está centralizado en el layout principal:

```html
<script src="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.all.min.js"></script>

<script>
    $(function () {
        // Alerta de Éxito
        @if (TempData["SuccessMessage"] != null)
        {
            <text>
            Swal.fire({
                icon: 'success',
                title: '¡Éxito!',
                text: "@Html.Raw(System.Web.HttpUtility.JavaScriptStringEncode(TempData["SuccessMessage"]?.ToString()))",
                confirmButtonText: 'OK',
                confirmButtonColor: '#4CAF50'
            });
            </text>
        }
        
        // Alerta de Error
        @if (TempData["ErrorMessage"] != null)
        {
            <text>
            Swal.fire({
                icon: 'error',
                title: '¡Error!',
                text: "@Html.Raw(System.Web.HttpUtility.JavaScriptStringEncode(TempData["ErrorMessage"]?.ToString()))",
                confirmButtonText: 'Entendido',
                confirmButtonColor: '#f44336'
            });
            </text>
        }

        // Alerta de Advertencia
        @if (TempData["WarningMessage"] != null)
        {
            <text>
            Swal.fire({
                icon: 'warning',
                title: 'Advertencia',
                text: "@Html.Raw(System.Web.HttpUtility.JavaScriptStringEncode(TempData["WarningMessage"]?.ToString()))",
                confirmButtonText: 'OK',
                confirmButtonColor: '#ff9800'
            });
            </text>
        }
    });
</script>
```

### 5.2 Mensajes Estándar por Operación

| Operación | Mensaje | Tipo |
|-----------|---------|------|
| Create | `"El equipo '[Nombre]' ha sido registrado exitosamente."` | Success |
| Edit | `"Los datos del equipo han sido actualizados correctamente."` | Success |
| Delete | `"El equipo ha sido dado de baja correctamente."` | Success |
| Error de Validación | `"No se puede eliminar porque tiene registros asociados."` | Error |
| Advertencia | `"El número de inventario ya está en uso."` | Warning |

### 5.3 Uso en Backend

```csharp
// Al final de OnPostAsync, antes del RedirectToPage
TempData["SuccessMessage"] = "Equipo registrado exitosamente.";
return RedirectToPage("./Index");
```

---

## 6. ⚠️ ANTI-PATRONES (ERRORES A EVITAR)

### 6.1 ❌ NO usar Remove() en entidades con historial

```csharp
// ❌ MAL - Destruye datos y rompe integridad referencial
_context.Equipments.Remove(equipment);

// ✅ BIEN - Soft Delete preserva historial
equipment.CurrentStatus = EquipmentStatus.Deleted;
```

### 6.2 ❌ NO olvidar Include() en vistas de detalle

```csharp
// ❌ MAL - Genera "Proyecto_Laboratorios_Univalle.Models.Laboratory" en pantalla
var equipment = await _context.Equipments.FindAsync(id);

// ✅ BIEN - Carga las relaciones necesarias
var equipment = await _context.Equipments
    .Include(e => e.Laboratory)
    .Include(e => e.EquipmentType)
    .FirstOrDefaultAsync(m => m.Id == id);
```

### 6.3 ❌ NO usar dropdowns sin contexto

```csharp
// ❌ MAL - Solo muestra el nombre
new SelectList(laboratories, "Id", "Name")

// ✅ BIEN - Muestra [Código] - Nombre
new SelectList(laboratories.Select(l => new { 
    l.Id, 
    DisplayName = $"[{l.Code}] - {l.Name}" 
}), "Id", "DisplayName")
```

### 6.4 ❌ NO dejar inputs sin validación JavaScript

```html
<!-- ❌ MAL - Solo validación HTML5 -->
<input asp-for="Input.Name" class="form-control" required />

<!-- ✅ BIEN - Validación HTML5 + JavaScript + Pattern -->
<input asp-for="Input.Name" 
       class="form-control" 
       required 
       data-validation-required-message="El nombre es obligatorio"
       pattern="^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.-]+$"
       data-validation-pattern-message="Solo letras, números, puntos y guiones" />
```

### 6.5 ❌ NO omitir TempData en operaciones exitosas

```csharp
// ❌ MAL - El usuario no sabe si funcionó
await _context.SaveChangesAsync();
return RedirectToPage("./Index");

// ✅ BIEN - Feedback visual inmediato
await _context.SaveChangesAsync();
TempData["SuccessMessage"] = "Operación realizada con éxito.";
return RedirectToPage("./Index");
```

### 6.6 ❌ NO usar labels genéricos

```html
<!-- ❌ MAL - Estilo inconsistente -->
<label for="name">Nombre:</label>

<!-- ✅ BIEN - Estilo Premium estandarizado -->
<label asp-for="Input.Name" class="control-label font-weight-bold small uppercase text-muted">
    Nombre del Equipo <span class="text-danger">*</span>
</label>
```

---

## 7. TEMPLATES DE REFERENCIA (COPY-PASTE)

### 7.1 Template: Sección de Formulario Estándar

```html
<h5 class="font-weight-bold text-info mb-3">
    <i class="fas fa-barcode mr-2"></i> Título de la Sección
</h5>

<div class="form-group mb-3">
    <label asp-for="Input.PropertyName" class="control-label font-weight-bold small uppercase text-muted">
        Etiqueta del Campo <span class="text-danger">*</span>
    </label>
    <input asp-for="Input.PropertyName" 
           class="form-control" 
           placeholder="Ej: Valor de ejemplo"
           required 
           data-validation-required-message="Este campo es obligatorio"
           pattern="^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.-]+$"
           data-validation-pattern-message="Formato no válido" />
    <span asp-validation-for="Input.PropertyName" class="text-danger small"></span>
</div>
```

### 7.2 Template: Dropdown con Contexto

```html
<div class="form-group mb-3">
    <label asp-for="Input.LaboratoryId" class="control-label font-weight-bold small uppercase text-muted">
        Laboratorio Destino <span class="text-danger">*</span>
    </label>
    <select asp-for="Input.LaboratoryId" 
            class="form-control custom-select" 
            asp-items="ViewBag.LaboratoryId" 
            required>
        <option value="">-- Seleccione Laboratorio --</option>
    </select>
    <span asp-validation-for="Input.LaboratoryId" class="text-danger small"></span>
</div>
```

**Backend correspondiente:**
```csharp
var laboratories = _context.Laboratories
    .Where(l => l.Status == GeneralStatus.Activo)
    .OrderBy(l => l.Name)
    .Select(l => new { 
        Id = l.Id, 
        DisplayName = $"[{l.Code}] - {l.Name}" 
    })
    .ToList();

ViewData["LaboratoryId"] = new SelectList(laboratories, "Id", "DisplayName");
```

### 7.3 Template: Panel de Auditoría (Sidebar)

```html
<div class="card shadow-sm border-0">
    <div class="card-body">
        <h5 class="card-title text-dark font-weight-bold mb-4">
            <i class="fas fa-history mr-2 text-warning"></i> Control de Auditoría
        </h5>
        <hr />
        <div class="small mt-3">
            <div class="mb-3">
                <label class="text-muted d-block small uppercase mb-0">Fecha de Alta</label>
                <span class="text-dark font-weight-bold">
                    @Model.Equipment.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                </span>
            </div>
            <div>
                <label class="text-muted d-block small uppercase mb-0">Última Modificación</label>
                <span class="text-dark font-weight-bold">
                    @(Model.Equipment.LastModifiedDate?.ToString("dd/MM/yyyy HH:mm") ?? "Sin cambios previos")
                </span>
            </div>
        </div>
    </div>
</div>
```

---

## 8. CHECKLIST DE CALIDAD (PRE-COMMIT)

### 8.1 Frontend (.cshtml)

- [ ] **Labels:** Usan `control-label font-weight-bold small uppercase text-muted`
- [ ] **Inputs de texto:** Tienen `pattern` HTML5 y validación JS con `setupBlocking`
- [ ] **Botones principales:** Usan `btn-rounded px-4 shadow-sm font-weight-bold`
- [ ] **Iconos:** FontAwesome representativos en cada sección (`fas fa-barcode`, `fas fa-map-marker-alt`)
- [ ] **Dropdowns:** Muestran formato `[Código] - Nombre`
- [ ] **Validación:** Todos los campos obligatorios tienen `required` y `data-validation-required-message`
- [ ] **Scripts:** Sección `@section Scripts` incluye `jqBootstrapValidation` y `_ValidationScriptsPartial`
- [ ] **Responsive:** Layout funciona correctamente en móvil (col-lg-8/4 colapsa a col-12)

### 8.2 Backend (.cshtml.cs)

- [ ] **Normalización:** Todos los strings usan `.Clean()` antes de guardar
- [ ] **Validación de unicidad:** Implementada para campos identificadores (códigos, inventarios)
- [ ] **Eager Loading:** `.Include()` completos en OnGetAsync para Details/Edit/Delete
- [ ] **TempData:** Asignado `TempData["SuccessMessage"]` antes de cada `RedirectToPage`
- [ ] **Try-Catch:** Manejo de excepciones en operaciones críticas
- [ ] **ModelState:** Verificación de `!ModelState.IsValid` antes de procesar
- [ ] **Dropdowns:** Recarga de ViewData en caso de error de validación
- [ ] **Soft Delete:** Cambio de estado en lugar de `Remove()` para entidades con historial

### 8.3 Base de Datos

- [ ] **Soft Delete:** Implementado con campo de estado (no borrado físico)
- [ ] **Auditoría:** Campos `CreatedBy`, `CreatedDate`, `ModifiedBy`, `LastModifiedDate` presentes
- [ ] **Historial:** Tabla de historial de cambios si aplica (ej. `EquipmentStateHistory`)
- [ ] **Índices:** Índices únicos en campos identificadores
- [ ] **Foreign Keys:** Configuradas correctamente con `DeleteBehavior` apropiado
- [ ] **Migraciones:** Generadas y aplicadas correctamente

### 8.4 UX/UI

- [ ] **Feedback visual:** SweetAlert2 muestra confirmación de todas las operaciones
- [ ] **Mensajes de error:** Descriptivos y en español
- [ ] **Placeholders:** Ejemplos útiles en todos los inputs
- [ ] **Tooltips:** Información adicional en campos complejos
- [ ] **Estados de carga:** Indicadores visuales durante operaciones asíncronas
- [ ] **Accesibilidad:** Labels asociados correctamente con inputs (`asp-for`)

---

## 9. GLOSARIO CSS PREMIUM

| Clase CSS | Uso | Ejemplo |
|-----------|-----|---------|
| `card shadow-sm border-0` | Contenedor principal de secciones | Formularios, paneles |
| `card-body p-4` | Padding interno de tarjetas | Dentro de `.card` |
| `d-flex no-block align-items-center` | Header de tarjetas con ícono | Título + ícono decorativo |
| `ml-auto` | Empujar elemento a la derecha | Ícono decorativo en header |
| `font-weight-bold text-info mb-3` | Títulos de sección | `<h5>` dentro de formularios |
| `control-label font-weight-bold small uppercase text-muted` | Labels de formulario | Todos los `<label>` |
| `btn-rounded px-4 shadow-sm` | Botones principales | Submit, Guardar, Confirmar |
| `btn-outline-secondary btn-rounded` | Botones secundarios | Cancelar, Volver |
| `badge badge-pill badge-success px-3` | Badges de estado | Activo, Operacional |
| `text-danger small` | Mensajes de error | Validaciones |
| `opacity-2` | Opacidad para iconos de fondo | Iconos decorativos |
| `border-left border-danger border-3` | Borde lateral de advertencia | Cajas de confirmación |
| `customtab` | Pestañas de navegación | Tabs con borde inferior |
| `v-middle` | Alineación vertical en tablas | `<td>` en tablas |
| `drop-shadow` | Sombra suave | Inputs de búsqueda |

---

## 10. CONVENCIONES DE NOMENCLATURA

### 10.1 C# (Backend)

#### Propiedades de Modelo
```csharp
public string InventoryNumber { get; set; }  // PascalCase
public DateTime AcquisitionDate { get; set; }
public EquipmentStatus CurrentStatus { get; set; }
```

#### Variables Locales
```csharp
var equipment = await _context.Equipments.FindAsync(id);  // camelCase
string inventoryNumber = Input.InventoryNumber;
bool invExists = false;
```

#### Métodos
```csharp
public async Task<IActionResult> OnGetAsync(int? id)  // PascalCase
private async Task LoadDropdownsAsync()
private bool EquipmentExists(int id)
```

### 10.2 HTML/Razor

#### IDs de Inputs
```html
<!-- Usar PascalCase con prefijo del modelo -->
<input id="Input_Name" asp-for="Input.Name" />
<input id="EquipmentType_Name" asp-for="EquipmentType.Name" />
```

#### Clases CSS Personalizadas
```html
<!-- Usar kebab-case -->
<div class="custom-search-box"></div>
<button class="toggle-password-btn"></button>
```

### 10.3 JavaScript

#### Variables
```javascript
var textPattern = /^[a-zA-Z]/;  // camelCase
var $field = $(this);
var errorMessage = "Error";
```

#### Funciones
```javascript
function setupBlocking($selector, pattern, errorMessage) { }  // camelCase
function showTemporaryError($el, message) { }
```

### 10.4 Rutas y Páginas

#### Rutas Razor
```
/Equipment/Index
/Equipment/Create
/Equipment/Edit
/EquipmentTypes/Delete
```

#### Parámetros de Query
```
?searchTerm=valor      // camelCase
?typeFilter=1
?id=5
```

### 10.5 Mensajes de Usuario

#### Formato Estándar
```csharp
// ✅ BIEN - Español formal, primera letra mayúscula
TempData["SuccessMessage"] = "El equipo ha sido registrado exitosamente.";

// ❌ MAL - Informal, sin puntuación
TempData["SuccessMessage"] = "equipo creado";
```

#### Uso de Comillas
```csharp
// Para nombres específicos, usar comillas simples
TempData["SuccessMessage"] = $"El equipo '{equipment.Name}' ha sido actualizado.";
```

---

## 📌 NOTAS FINALES

### Mantenimiento de esta Guía
- Esta documentación debe actualizarse cada vez que se implemente una mejora estructural
- Cualquier cambio en los estándares debe ser comunicado al equipo
- Se recomienda revisar esta guía antes de iniciar cualquier módulo nuevo

### Recursos Adicionales
- **NiceAdmin Template:** Documentación oficial para componentes UI
- **jQuery Bootstrap Validation:** Plugin de validación en tiempo real
- **SweetAlert2:** Librería de alertas modales
- **Entity Framework Core:** Documentación de Microsoft para Eager Loading

### Contacto y Soporte
Para dudas o sugerencias sobre estos estándares, consultar con el equipo de desarrollo principal del Proyecto Laboratorios Univalle.

---

**Versión:** 1.0  
**Última Actualización:** 30 de Enero, 2026  
**Autor:** Equipo de Desarrollo - Proyecto Laboratorios Univalle
