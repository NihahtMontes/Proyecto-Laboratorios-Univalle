# 📂 Guía de Estilos y UX - Arquitectura Premium v2.0
**Proyecto**: Laboratorios Univalle  
**Framework**: ASP.NET Core 9.0 (Razor Pages)  
**Template**: NiceAdmin (Bootstrap 4 + Custom Extensions)  
**Última actualización**: 26 de enero de 2026

---

## 🎯 FILOSOFÍA DE DISEÑO

Esta aplicación sigue una **Arquitectura Premium** centrada en:

1. **Consistencia Visual**: Todos los módulos siguen los mismos patrones de diseño
2. **Smart UX**: Búsqueda y filtrado intuitivos en todas las vistas Index
3. **Navegación Contextual**: Uso de pestañas para entidades relacionadas
4. **Feedback Inmediato**: Validación en tiempo real y alertas modernas
5. **Información Inteligente**: Lifecycle tracking, historial de estados, auditoría completa

---

## 📐 ESTRUCTURA TÉCNICA

### Rutas de Assets
```
wwwroot/lib/nice-admin/        → Recursos del template (imágenes, plugins)
wwwroot/dist/                  → CSS/JS compilado
wwwroot/css/custom.css        → Personalizaciones (si se requieren)
```

### Layouts y Componentes Base
```
Pages/Shared/_Layout.cshtml    → Layout principal
Pages/Shared/_Sidebar.cshtml   → Navegación lateral
Pages/Shared/_TopBar.cshtml    → Header/Perfil
Pages/Login.cshtml             → Layout independiente de autenticación
```

### Scripts Globales (ya cargados en _Layout)
- jQuery 3.6+
- Bootstrap 4.6+
- Font Awesome 5.15+
- Perfect Scrollbar
- SweetAlert2 (para Delete confirmations)
- jqBootstrapValidation (para validación en tiempo real)

---

## 🏗️ PATRONES DE DISEÑO IMPLEMENTADOS

### 1. SMART INDEX PATTERN
**Ubicación**: Todas las vistas Index.cshtml

**Propósito**: Proporcionar zona de búsqueda y filtrado unificada con estilo premium.

**Estructura HTML**:
```html
<div class="bg-light p-3 mb-4 rounded border-left border-info shadow-sm">
     <form method="get">
         <div class="row align-items-end">
             <div class="col-md-5">
                 <label class="small font-weight-bold text-muted uppercase">Búsqueda Técnica</label>
                 <div class="input-group drop-shadow">
                     <div class="input-group-prepend">
                         <span class="input-group-text bg-white border-right-0"><i class="fas fa-search text-muted"></i></span>
                     </div>
                     <input type="text" asp-for="SearchTerm" class="form-control border-left-0" placeholder="Nombre, Marca o N° Inventario..." />
                 </div>
             </div>
             <div class="col-md-3">
                 <label class="small font-weight-bold text-muted uppercase">Filtrar por [Criterio]</label>
                 <select asp-for="FilterProperty" class="form-control custom-select" 
                         asp-items="@(new SelectList(Model.ItemsList, "Id", "Name"))">
                     <option value="">-- Todos --</option>
                 </select>
             </div>
             <div class="col-md-4 text-right">
                 <button type="submit" class="btn btn-dark btn-rounded px-4 shadow-sm">
                     <i class="fas fa-filter mr-1"></i> Filtrar
                 </button>
                 <a asp-page="./Index" class="btn btn-outline-secondary btn-rounded ml-1">
                     <i class="fas fa-sync-alt"></i>
                 </a>
             </div>
         </div>
     </form>
 </div>
```

**Backend (.cshtml.cs)**:
```csharp
[BindProperty(SupportsGet = true)]
public string? SearchTerm { get; set; }

[BindProperty(SupportsGet = true)]
public TipoFiltro? FilterCriteria { get; set; }

public async Task OnGetAsync()
{
    var query = _context.Entidades
        .Where(e => e.Status != GeneralStatus.Eliminado);
    
    if (!string.IsNullOrEmpty(SearchTerm))
    {
        var term = SearchTerm.Trim().ToLower();
        query = query.Where(e => e.Name.ToLower().Contains(term));
    }
    
    if (FilterCriteria.HasValue)
    {
        query = query.Where(e => e.Type == FilterCriteria.Value);
    }
    
    Entidades = await query.ToListAsync();
}
```

---

### 2. DUAL-TAB ARCHITECTURE
**Ubicación**: Users/Index, Equipment/Index

**Propósito**: Agrupar entidades relacionadas en una sola vista

**Ventajas**:
- Reduce saturación del Sidebar
- Mantiene contexto del usuario
- Permite gestión rápida sin cambiar de módulo

**Estructura HTML**:
```html
<ul class="nav nav-tabs customtab px-4 pt-3" role="tablist">
    <li class="nav-item">
        <a class="nav-link active" data-toggle="tab" href="#principal" role="tab">
            <i class="fas fa-icon1 mr-2"></i> Entidad Principal
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link" data-toggle="tab" href="#secundaria" role="tab">
            <i class="fas fa-icon2 mr-2"></i> Entidad Relacionada
        </a>
    </li>
</ul>

<div class="tab-content">
    <div class="tab-pane active" id="principal" role="tabpanel">
        <!-- Smart Index Zone + Tabla -->
    </div>
    <div class="tab-pane" id="secundaria" role="tabpanel">
        <!-- Smart Index Zone + Tabla -->
    </div>
</div>
```

**CSS Requerido** (agregar a custom.css si no existe):
```css
.customtab .nav-link {
    border: none;
    color: #747d8c;
    padding: 15px 20px;
    transition: all 0.3s ease;
}
.customtab .nav-link.active {
    color: #2962ff;
    background: transparent;
    border-bottom: 3px solid #2962ff;
}
.customtab .nav-link:hover {
    color: #2962ff;
}
```

---

### 3. LIFECYCLE INTELLIGENCE
**Ubicación**: Equipment/Edit, Equipment/Details

**Propósito**: Visualizar ciclo de vida de activos con alertas de reemplazo

**Implementación**:
```html
<div class="card bg-info text-white">
    <div class="card-body">
        <h5>Vida Útil Restante</h5>
        <div class="progress mt-1" style="height: 6px;">
            @{
                var percent = 0;
                if (UsefulLifeYears.HasValue && AgeYears.HasValue) {
                    percent = (int)(100 - ((double)AgeYears / UsefulLifeYears * 100));
                    percent = Math.Max(0, Math.Min(100, percent));
                }
                var color = percent < 20 ? "bg-danger" : (percent < 50 ? "bg-warning" : "bg-success");
            }
            <div class="progress-bar @color" style="width: @percent%;"></div>
        </div>
        <span class="small">@percent% de vida operativa</span>
    </div>
</div>

@if (RequiresReplacement)
{
    <div class="alert alert-warning border-0 mt-3">
        <i class="fas fa-exclamation-triangle mr-2"></i> 
        <strong>Atención:</strong> Este equipo ha superado el 80% de su vida útil.
    </div>
}
```

---

## 🎨 COMPONENTES ESTÁNDAR

### 1. CARDS (Tarjetas)
**Uso**: Contenedor principal de todo contenido

```html
<div class="card shadow-sm border-0">
    <div class="card-body p-4">
        <!-- Header Moderna -->
        <div class="d-flex no-block align-items-center mb-4">
            <div>
                <h4 class="card-title text-dark">Título Principal</h4>
                <h6 class="card-subtitle text-muted">Descripción breve</h6>
            </div>
            <div class="ml-auto">
                <i class="fas fa-icon fa-2x text-info opacity-5"></i>
            </div>
        </div>
        
        <!-- Contenido -->
    </div>
</div>
```

**Variantes de Header**:
- **bg-primary text-white**: Información general
- **bg-info text-white**: Datos financieros/importantes
- **bg-warning text-dark**: Advertencias/alertas
- **bg-danger text-white**: Eliminaciones/crítico
- **bg-dark text-white**: Auditoría/metadata

---

### 2. TABLAS
**Clases Base**: `table table-hover v-middle`

```html
<div class="table-responsive">
    <table class="table table-hover v-middle">
        <thead class="thead-light">
            <tr>
                <th>Información Principal</th>
                <th>Detalles</th>
                <th class="text-center">Estado</th>
                <th>Auditoría</th>
                <th class="text-center">Acciones</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <!-- Columna con Icono + Texto -->
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="m-r-10">
                                <span class="btn btn-circle btn-light text-info">
                                    <i class="fas fa-icon"></i>
                                </span>
                            </div>
                            <div>
                                <h5 class="m-b-0 font-medium">@item.Name</h5>
                                <span class="text-muted small">Info adicional</span>
                            </div>
                        </div>
                    </td>
                    
                    <!-- Badge de Estado -->
                    <td class="text-center">
                        @{
                            var badgeClass = item.Status switch {
                                Status.Activo => "badge-success",
                                Status.Inactivo => "badge-secondary",
                                _ => "badge-danger"
                            };
                        }
                        <span class="badge @badgeClass px-3 py-1">@item.Status</span>
                    </td>
                    
                    <!-- Botones de Acción -->
                    <td class="text-center">
                        <div class="btn-group">
                            <a asp-page="./Edit" asp-route-id="@item.Id" class="btn btn-sm btn-outline-warning btn-rounded" title="Editar">
                                <i class="fas fa-edit"></i>
                            </a>
                            <a asp-page="./Details" asp-route-id="@item.Id" class="btn btn-sm btn-outline-info btn-rounded mx-1" title="Detalles">
                                <i class="fas fa-search-plus"></i>
                            </a>
                            <a asp-page="./Delete" asp-route-id="@item.Id" class="btn btn-sm btn-outline-danger btn-rounded" title="Eliminar">
                                <i class="fas fa-trash"></i>
                            </a>
                        </div>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@section Scripts {
    <script>
        $(function() {
            $('[data-toggle="tooltip"]').tooltip();
        });
    </script>
}
```

**Mensaje de Vacío**:
```html
@if (!Model.Items.Any())
{
    <tr>
        <td colspan="6" class="text-center py-5 text-muted italic">
            <i class="fas fa-folder-open fa-3x mb-3 d-block opacity-2"></i>
            No se encontraron registros.
        </td>
    </tr>
}
```

---

### 3. FORMULARIOS CON VALIDACIÓN PREMIUM

**Estándar de Doble Capa**:
1. **Frontend (Real-Time)**: jqBootstrapValidation
2. **Backend**: DataAnnotations + ModelState

**Estructura de Form**:
```html
<form method="post" novalidate>
    <div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>
    
    <!-- Formulario de Dos Columnas (si muchos campos) -->
    <div class="row">
        <div class="col-md-6">
            <h5 class="font-weight-bold text-info mb-3">
                <i class="fas fa-icon mr-2"></i> Sección 1
            </h5>
            <!-- Campos -->
        </div>
        <div class="col-md-6">
            <h5 class="font-weight-bold text-info mb-3">
                <i class="fas fa-icon mr-2"></i> Sección 2
            </h5>
            <!-- Campos -->
        </div>
    </div>
    
    <hr />
    
    <!-- Botones -->
    <div class="form-actions text-right mt-4">
        <button type="submit" class="btn btn-info btn-rounded px-4 shadow-sm">
            <i class="fas fa-save mr-1"></i> Guardar
        </button>
        <a asp-page="./Index" class="btn btn-outline-secondary btn-rounded ml-2">
            Cancelar
        </a>
    </div>
</form>
```

**Input Estándar**:
```html
<div class="form-group mb-3">
    <label asp-for="Name" class="control-label font-weight-bold small">
        Nombre <span class="text-danger">*</span>
    </label>
    <input asp-for="Name" class="form-control" 
           placeholder="Ingrese el nombre" 
           required 
           pattern="^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s-]+$"
           data-validation-required-message="Campo obligatorio"
           data-validation-pattern-message="Solo letras" />
    <span asp-validation-for="Name" class="text-danger small"></span>
</div>
```

**Select Estándar**:
```html
<div class="form-group mb-3">
    <label asp-for="CategoryId" class="control-label font-weight-bold small">
        Categoría <span class="text-danger">*</span>
    </label>
    <select asp-for="CategoryId" class="form-control custom-select" 
            asp-items="ViewBag.Categories" required>
        <option value="">-- Seleccione --</option>
    </select>
    <span asp-validation-for="CategoryId" class="text-danger small"></span>
</div>
```

**Checkbox Moderno**:
```html
<div class="custom-control custom-checkbox">
    <input type="checkbox" asp-for="IsActive" class="custom-control-input" id="checkActive">
    <label class="custom-control-label font-weight-bold" for="checkActive">
        Activo
    </label>
</div>
```

**Scripts Requeridos**:
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

---

### 4. BOTONES

**Semántica de Colores**:
```html
<!-- Acciones Principales -->
<button class="btn btn-info btn-rounded">Guardar</button>
<button class="btn btn-warning btn-rounded">Editar</button>

<!-- Acciones Secundarias -->
<button class="btn btn-outline-secondary btn-rounded">Cancelar</button>
<button class="btn btn-outline-info btn-rounded">Ver Detalles</button>

<!-- Acciones Críticas -->
<button class="btn btn-danger btn-rounded">Eliminar</button>

<!-- Filtros -->
<button class="btn btn-dark btn-rounded">Buscar</button>
```

**Con Iconos** (siempre preferir):
```html
<button class="btn btn-info btn-rounded px-4">
    <i class="fas fa-save mr-1"></i> Guardar Cambios
</button>
```

**Botones Pequeños en Tablas**:
```html
<a class="btn btn-sm btn-outline-warning btn-rounded" data-toggle="tooltip" title="Editar">
    <i class="fas fa-edit"></i>
</a>
```

---

### 5. BADGES (Estados)

**Paleta Estándar**:
```html
<!-- Estados Positivos -->
<span class="badge badge-success px-3 py-1">Activo / Operativo</span>

<!-- Estados Intermedios -->
<span class="badge badge-warning px-3 py-1">En Mantenimiento / Pendiente</span>

<!-- Estados Negativos -->
<span class="badge badge-danger px-3 py-1">Fuera de Servicio / Eliminado</span>

<!-- Estados Neutrales -->
<span class="badge badge-secondary px-3 py-1">Inactivo</span>
<span class="badge badge-info px-3 py-1">Información</span>
```

**Badge Dinámico**:
```csharp
@{
    var badgeClass = item.Status switch {
        GeneralStatus.Activo => "badge-success",
        GeneralStatus.Inactivo => "badge-secondary",
        GeneralStatus.Eliminado => "badge-danger",
        _ => "badge-light"
    };
}
<span class="badge @badgeClass px-3 py-1">@item.Status</span>
```

---

### 6. ALERTAS

**SweetAlert2 (Confirmaciones)**:
```html
@section Scripts {
    <link href="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.min.css" rel="stylesheet">
    <script src="~/lib/nice-admin/assets/libs/sweetalert2/dist/sweetalert2.all.min.js"></script>
    <script>
        $('#deleteForm').submit(function(e) {
            e.preventDefault();
            Swal.fire({
                title: '¿Está seguro?',
                text: "Esta acción no se puede deshacer",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                cancelButtonColor: '#3085d6',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar'
            }).then((result) => {
                if (result.isConfirmed) {
                    this.submit();
                }
            });
        });
    </script>
}
```

**TempData Alerts**:
```html
@section Scripts {
    <script>
        @if (TempData["SuccessMessage"] != null)
        {
            <text>
            Swal.fire({
                title: '¡Éxito!',
                text: '@TempData["SuccessMessage"]',
                icon: 'success',
                confirmButtonColor: '#2962ff'
            });
            </text>
        }
    </script>
}
```

---

## 🎯 GUIDELINES ESPECÍFICOS POR VISTA

### INDEX.CSHTML
✅ Card principal con shadow-sm  
✅ Smart Index Zone  
✅ Tabla responsive con badges  
✅ Botones de acción con tooltips  
✅ Mensaje de vacío si no hay datos  

### CREATE.CSHTML
✅ Card centrada (col-lg-10)  
✅ Dos columnas si >6 campos  
✅ Validación en tiempo real  
✅ Botones alineados a la derecha  

### EDIT.CSHTML (Guía de Estructura Concisa)
🎯 **Layout Base**:
```html
<div class="row">
    <div class="col-lg-8">  <!-- Formulario principal -->
    <div class="col-lg-4">  <!-- Panel lateral informativo -->
</div>
```

**Formulario Principal (col-lg-8)**:
- **Card Container**: `<div class="card shadow-sm border-0"><div class="card-body p-4">`
- **Header**: `<h4 class="card-title text-dark">Editar [Entidad]</h4><h6 class="card-subtitle text-muted">Descripción breve</h6>`
- **Secciones con Iconos**: `<h5 class="font-weight-bold text-info mb-3"><i class="fas fa-[icono] mr-2"></i> Título Sección</h5>`
- **Campo Destacado (Estado/Principal)**: 
  ```html
  <div class="form-group mb-4 bg-light p-3 rounded border-left border-warning">
      <label class="text-warning small uppercase font-weight-bold">...</label>
      <select class="form-control border-warning font-weight-bold custom-select">...</select>
      <small class="text-muted italic mt-1">Texto de ayuda</small>
  </div>
  ```
- **Botones de Acción**:
  ```html
  <button type="submit" class="btn btn-warning btn-rounded px-4 shadow-sm text-white font-weight-bold">
      <i class="fas fa-save mr-1"></i> Actualizar
  </button>
  ```

**Panel Lateral (col-lg-4)**:
- **Card Oscuro (Info Principal)**:
  ```html
  <div class="card shadow-sm border-0 bg-dark text-white p-3 mb-4">
      <h5 class="text-white"><i class="fas fa-info-circle mb-3 mr-2"></i> Datos del Registro</h5>
      <label class="text-muted uppercase small d-block mb-0">Campo</label>
      <span class="font-weight-bold text-info mb-3 d-block">Valor</span>
  </div>
  ```
- **Card Auditoría**: 
  ```html
  <div class="card shadow-sm border-0 p-3">
      <h5 class="text-dark small uppercase font-weight-bold"><i class="fas fa-history mr-2"></i> Auditoría</h5>
      <span class="text-muted small">Registrado el:</span>
      <span class="font-weight-bold d-block">Fecha</span>
  </div>
  ```

---

### DETAILS.CSHTML
✅ Badge de estado en header  
✅ Tabla de datos o secciones  
✅ Historial (si aplica)  
✅ Panel lateral complementario  

### DELETE.CSHTML
✅ Alerta visual clara  
✅ Resumen de entidad  
✅ Confirmación con SweetAlert2  

---

## 📱 RESPONSIVE DESIGN

### Breakpoints Bootstrap 4
```
xs: <576px   (Mobile)
sm: ≥576px   (Tablet vertical)
md: ≥768px   (Tablet horizontal)
lg: ≥992px   (Desktop)
xl: ≥1200px  (Desktop grande)
```

### Clases Útiles
```html
<!-- Ocultar en móvil, mostrar en desktop -->
<span class="d-none d-md-inline">Texto largo</span>

<!-- Centrar en móvil, alinear derecha en desktop -->
<div class="text-center text-md-right">
    <button>Acción</button>
</div>

<!-- Columnas que stackean en móvil -->
<div class="row">
    <div class="col-12 col-md-6">Columna 1</div>
    <div class="col-12 col-md-6">Columna 2</div>
</div>
```

---

## 🚫 REGLAS DE OPERACIÓN

1. **NO modificar** `dist/css/style.min.css` directamente
2. **Personalizaciones** agregar en `wwwroot/css/custom.css`
3. **Mantener** paleta de colores NiceAdmin (no hexadecimales hardcoded)
4. **Revisar siempre** consola de navegador para errores JS
5. **Usar clases** de utilidad de Bootstrap antes de crear CSS custom
6. **Iconos**: Font Awesome 5.15 o Material Design Icons (mdi)
7. **Soft Delete**: Siempre usar `Status != GeneralStatus.Eliminado` en queries
8. **Auditoria**: Incluir CreatedBy, CreatedDate, ModifiedBy, ModifiedDate en todas las entidades

---

## 📚 RECURSOS

- **Bootstrap 4 Docs**: https://getbootstrap.com/docs/4.6/
- **Font Awesome Icons**: https://fontawesome.com/v5/search
- **SweetAlert2 Docs**: https://sweetalert2.github.io/
- **NiceAdmin Theme**: Documentación interna en `wwwroot/lib/nice-admin/docs/`

---

**Mantenido por**: Equipo de Desarrollo Laboratorios Univalle  
**Versión**: 2.0 (Arquitectura Premium)  
**Fecha**: 26 de enero de 2026
