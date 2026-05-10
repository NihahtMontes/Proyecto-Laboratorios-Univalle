# ✅ Checklist de Implementación Premium - Laboratorios Univalle

**Propósito**: Guía rápida para asegurar consistencia al trabajar en cualquier módulo del sistema.

---

## 🎯 ANTES DE EMPEZAR

### Verificación Inicial
- [ ] ¿El módulo tiene entidad relacionada pequeña? → Considerar **Dual-Tab Architecture**
- [ ] ¿Los usuarios necesitan filtrar frecuentemente? → Implementar **Smart Index Pattern**
- [ ] ¿La entidad tiene ciclo de vida temporal? → Agregar **Lifecycle Intelligence**
- [ ] ¿Hay cambios de estado? → Implementar **Historial de Estados**

---

## 📄 INDEX.CSHTML - CHECKLIST COMPLETO

### 1. Estructura de Header
```html
✅ Card con shadow-sm border-0
✅ d-flex header con título + botón de creación alineado a la derecha
✅ Subtítulo descriptivo (card-subtitle)
✅ Icono decorativo en ml-auto
```

### 2. Smart Index Zone
```html
✅ Contenedor: bg-light p-3 mb-4 rounded border-left border-info shadow-sm
✅ Input de búsqueda con icono prepend (fas fa-search)
✅ Filtros con custom-select
✅ Botones: "Filtrar" (btn-dark btn-rounded) + "Limpiar" (btn-outline-secondary)
```

### 3. Tabla de Datos
```html
✅ Wrapper: <div class="table-responsive">
✅ Clases de tabla: table table-hover v-middle
✅ Header: thead-light
✅ Estado visual con badges dinámicos (switch de colores)
✅ Columna de auditoría (CreatedDate + Initials)
✅ Botones de acción con tooltips
```

### 4. Mensaje Vacío
```html
✅ Si no hay datos: td colspan con ícono fa-folder-open + mensaje centrado
```

### 5. Backend (.cshtml.cs)
```csharp
✅ [BindProperty(SupportsGet = true)] para SearchTerm y FiltrosAdicionales
✅ Query base con .Where(e => e.Status != GeneralStatus.Eliminado)
✅ Aplicar filtros condicionales con if (!string.IsNullOrEmpty(SearchTerm))
✅ Include de entidades relacionadas para evitar lazy loading
```

### 6. Scripts Section
```javascript
✅ Inicializar tooltips: $('[data-toggle="tooltip"]').tooltip()
✅ Manejar TempData con SweetAlert2 si aplica
```

---

## 📝 CREATE.CSHTML - CHECKLIST COMPLETO

### 1. Estructura General
```html
✅ Row justify-content-center
✅ Col-lg-10 o col-lg-8 (según cantidad de campos)
✅ Card shadow-sm border-0
✅ Card-body con p-4
```

### 2. Header del Formulario
```html
✅ d-flex header con título + icono decorativo
✅ Subtítulo descriptivo
```

### 3. Formulario
```html
✅ <form method="post" novalidate>
✅ Validación summary: <div asp-validation-summary="ModelOnly" class="alert alert-danger">
✅ Si tiene muchos campos: row con col-md-6 (dos columnas)
```

### 4. Cada Campo
```html
✅ form-group mb-3 o mb-4
✅ Label con font-weight-bold small
✅ Asterisco rojo (*) para campos requeridos
✅ Input/Select con class="form-control"
✅ Validación span: <span asp-validation-for="..." class="text-danger small">
✅ Placeholder descriptivo
```

### 5. Campos Especiales
```html
✅ Selects: agregar custom-select + opción vacía "-- Seleccione --"
✅ Checkboxes: usar custom-control custom-checkbox
✅ Textareas: definir rows="3" o más
```

### 6. Botones Finales
```html
✅ hr separador
✅ form-actions text-right mt-4
✅ Botón submit: btn btn-info/warning btn-rounded px-4 shadow-sm con icono
✅ Botón cancel: btn btn-outline-secondary btn-rounded
```

### 7. Scripts Section
```javascript
✅ Cargar: jqbootstrapvalidation/validation.js
✅ Inicializar: $("input,select,textarea").not("[type=submit]").jqBootstrapValidation()
✅ Incluir: _ValidationScriptsPartial
```

---

## ✏️ EDIT.CSHTML - CHECKLIST COMPLETO

### 1. Estructura
```html
✅ Si tiene mucha info: row con col-lg-8 (formulario) + col-lg-4 (panel lateral)
✅ Si es simple: mismo layout que Create
```

### 2. Panel Lateral (si aplica)
```html
✅ Card con bg-light o bg-dark según tipo de info
✅ Título: card-title con icono
✅ Auditoría: CreatedBy, CreatedDate, ModifiedBy, ModifiedDate
✅ Si hay lifecycle: barra de progreso con colores dinámicos
✅ Si hay alertas: alert alert-warning con icono
```

### 3. Formulario Principal
```html
✅ Campo hidden: <input type="hidden" asp-for="Entity.Id" />
✅ Resto igual que Create
```

---

## 👁️ DETAILS.CSHTML - CHECKLIST COMPLETO

### 1. Estructura
```html
✅ Row justify-content-center
✅ Col-lg-8 o col-lg-10 según complejidad
✅ Card shadow-sm border-0
```

### 2. Header
```html
✅ d-flex con título + badge de estado prominente (ml-auto)
✅ Subtítulo descriptivo
```

### 3. Datos Principales
```html
✅ Si son pocos: tabla con bg-light rounded
✅ Si son muchos: dividir en secciones con h5 + borders
✅ Formato de filas: <th style="width: 40%;">Label</th><td>Valor</td>
```

### 4. Historial (si aplica)
```html
✅ Título de sección: h5 con icono fas fa-history
✅ Tabla: table table-hover v-middle small border
✅ Badge verde para estado actual
```

### 5. Panel Lateral (si aplica)
```html
✅ Datos financieros o técnicos complementarios
✅ Auditoría
```

### 6. Botones Finales
```html
✅ hr separador
✅ form-actions text-right
✅ Editar: btn btn-warning btn-rounded
✅ Volver: btn btn-outline-secondary btn-rounded
```

---

## 🗑️ DELETE.CSHTML - CHECKLIST COMPLETO

### 1. Alerta de Advertencia
```html
✅ alert alert-danger border-0 shadow-sm
✅ d-flex con icono fa-exclamation-circle fa-3x
✅ Mensaje claro de consecuencias
```

### 2. Resumen de Entidad
```html
✅ Card con datos críticos (Nombre, ID, etc.)
✅ bg-light p-3 rounded border
```

### 3. Auditoría
```html
✅ Mostrar CreatedBy y CreatedDate
```

### 4. Formulario
```html
✅ <form method="post" id="deleteForm">
✅ Hidden input con ID
✅ Botones: Cancelar (outline-secondary) + Confirmar (btn-danger)
```

### 5. Scripts (Opcional)
```html
✅ Si se usa SweetAlert2: cargar CSS y JS de sweetalert2
✅ Interceptar submit con confirmación modal
```

---

## 🎨 COMPONENTES REUTILIZABLES

### Badge de Estado Dinámico
```razor
@{
    var badgeClass = Status switch {
        GeneralStatus.Activo => "badge-success",
        GeneralStatus.Inactivo => "badge-secondary",
        _ => "badge-light"
    };
}
<span class="badge @badgeClass px-3 py-1">@Status</span>
```

### Botones de Acción
```html
<div class="btn-group">
    <a asp-page="./Edit" asp-route-id="@Id" 
       class="btn btn-sm btn-outline-warning btn-rounded" 
       data-toggle="tooltip" title="Editar">
        <i class="fas fa-edit"></i>
    </a>
    <a asp-page="./Details" asp-route-id="@Id" 
       class="btn btn-sm btn-outline-info btn-rounded mx-1" 
       data-toggle="tooltip" title="Detalles">
        <i class="fas fa-search-plus"></i>
    </a>
</div>
```

### Card Header Moderna
```html
<div class="d-flex no-block align-items-center mb-4">
    <div>
        <h4 class="card-title text-dark">Título</h4>
        <h6 class="card-subtitle text-muted">Subtítulo</h6>
    </div>
    <div class="ml-auto">
        <i class="fas fa-icon fa-2x text-info opacity-5"></i>
    </div>
</div>
```

### Input con Validación Premium
```html
<div class="form-group mb-3">
    <label asp-for="Field" class="control-label font-weight-bold small">
        Campo <span class="text-danger">*</span>
    </label>
    <input asp-for="Field" class="form-control" 
           placeholder="Ejemplo" 
           required 
           data-validation-required-message="Este campo es obligatorio" />
    <span asp-validation-for="Field" class="text-danger small"></span>
</div>
```

---

## 🚦 COLORES Y ESTADOS

### Badges
```
✅ Activo/Operativo: badge-success (verde)
⚠️ En Mantenimiento/Pendiente: badge-warning (amarillo)
❌ Fuera de Servicio/Eliminado: badge-danger (rojo)
⚪ Inactivo/Neutral: badge-secondary (gris)
```

### Botones
```
🔵 Acción Principal (Guardar, Crear): btn-info
🟡 Editar: btn-warning
🔴 Eliminar: btn-danger
⚪ Secundario (Cancelar, Volver): btn-outline-secondary
⚫ Filtrar: btn-dark
```

### Bordes
```
🔵 Info/Destacado: border-info
🟡 Advertencia: border-warning
🔴 Peligro: border-danger
```

---

## 📊 SCRIPTS ESTÁNDAR

### En todas las vistas con formularios
```html
@section Scripts {
    <script src="~/lib/nice-admin/assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script>
        $(function () {
            "use strict";
            $("input,select,textarea").not("[type=submit]").jqBootstrapValidation();
        });
    </script>
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### En Index con tooltips
```html
@section Scripts {
    <script>
        $(function () {
            $('[data-toggle="tooltip"]').tooltip();
        });
    </script>
}
```

### En Delete con SweetAlert2
```html
@section Scripts {
    <link href="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.min.css" rel="stylesheet">
    <script src="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.all.min.js"></script>
    <script>
        $('#deleteForm').submit(function(e) {
            e.preventDefault();
            Swal.fire({
                title: '¿Confirmar eliminación?',
                text: "Esta acción no se puede deshacer.",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Sí, eliminar'
            }).then((result) => {
                if (result.isConfirmed) this.submit();
            });
        });
    </script>
}
```

---

## 🔍 VALIDACIONES COMUNES

### RUT Chileno
```html
pattern="^[0-9]{7,8}-[0-9Kk]{1}$"
data-validation-pattern-message="Formato: 12345678-9"
```

### Email Institucional
```html
pattern="^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
data-validation-pattern-message="Email inválido"
```

### Solo Letras
```html
pattern="^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s-]+$"
data-validation-pattern-message="Solo letras permitidas"
```

### Número Positivo
```html
type="number" min="0" step="0.01"
```

---

**Última actualización**: 26 de enero de 2026  
**Mantenido por**: Equipo de Desarrollo Laboratorios Univalle
