# 🎨 Arquitectura Premium - Sistema de Gestión Laboratorios Univalle
**Versión**: 2.0 - Enero 2026  
**Estado**: Implementado y Operacional

---

## 📋 RESUMEN EJECUTIVO

Durante esta sesión de trabajo, se ha completado una **transformación integral** del sistema de gestión, migrando desde vistas básicas scaffolded hacia una **arquitectura de diseño premium** basada en:

1. **Smart Index Pattern**: Zona de búsqueda y filtrado unificada en todos los módulos
2. **Navegación por Pestañas**: Para entidades relacionadas (Dual-Tab Architecture)
3. **Diseño Premium**: Cards, shadows, badges, iconos, validación en tiempo real
4. **Alertas Modernas**: SweetAlert2 para confirmaciones críticas
5. **Lifecycle Intelligence**: Visualización de estados, históricos y progreso

---

## 🏗️ MÓDULOS TRANSFORMADOS

### 1️⃣ MÓDULO DE USUARIOS ✅ (100% Completado)

#### **Index.cshtml** - Sistema Dual de Pestañas
**Arquitectura**: Navegación por pestañas (Usuarios Activos + Usuarios Eliminados)

**Características Implementadas**:
- ✅ Smart Index Zone con búsqueda por nombre, email, cargo
- ✅ Filtro por rol (Administradores, Asistentes, Jefes Lab, Técnicos)
- ✅ Tabla con badges de estado visual (Activo/Inactivo)
- ✅ Pestaña secundaria para "Usuarios Eliminados" (soft-delete)
- ✅ Botones de acción con tooltips (Editar, Ver, Eliminar)

**Código Clave**:
```html
<ul class="nav nav-tabs customtab px-4 pt-3" role="tablist">
    <li class="nav-item">
        <a class="nav-link active" data-toggle="tab" href="#activos">
            <i class="fas fa-users mr-2"></i> Usuarios Activos
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link" data-toggle="tab" href="#eliminados">
            <i class="fas fa-user-times mr-2"></i> Eliminados
        </a>
    </li>
</ul>
```

#### **Create.cshtml** - Formulario Premium de Dos Columnas
**Diseño**: Layout de dos columnas (Datos Personales | Configuración de Acceso)

**Características**:
- ✅ Validación en tiempo real con jqBootstrapValidation
- ✅ Patrones regex para RUT chileno y emails institucionales
- ✅ Selector de roles con iconos descriptivos
- ✅ Campo de contraseña con generador automático opcional
- ✅ Diseño responsive centrado (col-lg-10)

#### **Edit.cshtml** - Vista de Actualización con Panel Lateral
**Diseño**: Formulario principal + Panel lateral de auditoría

**Características**:
- ✅ Formulario de dos columnas igual que Create
- ✅ Panel lateral con información de registro (CreatedBy, ModifiedBy)
- ✅ Checkbox de "Cambiar Contraseña" opcional
- ✅ Validación de campos únicos (RUT, Email)

#### **Details.cshtml** - Expediente de Usuario
**Diseño**: Tarjeta centrada con información estructurada

**Características**:
- ✅ Badge de rol prominente en header
- ✅ Información personal en tabla visual
- ✅ Panel de auditoría con historial
- ✅ Botones de acción alineados a la derecha

#### **Delete.cshtml** - Confirmación con SweetAlert2
**Diseño**: Alerta de advertencia + Resumen del usuario

**Características**:
- ✅ Modal de confirmación SweetAlert2 antes de eliminar
- ✅ Mensaje de "Baja Lógica" (soft-delete explicado)
- ✅ Resumen visual de datos críticos del usuario

---

### 2️⃣ MÓDULO DE EQUIPOS ✅ (100% Completado)

#### **Index.cshtml** - Dual Tab (Inventario + Categorías)
**Arquitectura**: Navegación por pestañas (Equipos + Tipos de Equipos)

**PESTAÑA 1 - INVENTARIO DE EQUIPOS**:
- ✅ Smart Index Zone con búsqueda por nombre, marca, N° inventario
- ✅ Filtro rápido por Tipo de Equipo (dropdown)
- ✅ Tabla con estado técnico visual:
  - 🟢 Operativo (badge-success)
  - 🟡 En Mantenimiento (badge-warning)
  - 🔴 Fuera de Servicio (badge-danger)
- ✅ Columna de ubicación (Laboratorio, Ciudad, País)
- ✅ Identificación técnica (N° Inventario, N° Serie)

**PESTAÑA 2 - CATEGORÍAS / TIPOS**:
- ✅ Tabla integrada de Tipos de Equipos
- ✅ Información de frecuencia de mantenimiento
- ✅ Badge de "Calibración Requerida"
- ✅ Botones de creación y gestión rápida

**Backend** (`Index.cshtml.cs`):
```csharp
[BindProperty(SupportsGet = true)]
public string? SearchTerm { get; set; }

[BindProperty(SupportsGet = true)]
public int? TypeFilter { get; set; }

public IList<Models.EquipmentType> EquipmentTypes { get; set; }
```

#### **Create.cshtml** - Registro Técnico de Activos
**Diseño**: Dos columnas (Identificación/Ubicación | Especificaciones/Finanzas)

**Columna 1 - Identificación Base**:
- ✅ Nombre del Equipo
- ✅ N° Inventario (campo crítico resaltado)
- ✅ Categoría (selector)
- ✅ Laboratorio Asignado
- ✅ País y Ciudad de Origen

**Columna 2 - Especificaciones Técnicas**:
- ✅ Marca y Modelo
- ✅ Número de Serie
- ✅ Fecha de Adquisición
- ✅ Valor USD
- ✅ Vida Útil Estimada (años)

**Campo Global**:
- ✅ Observaciones Técnicas (textarea completo)

#### **Edit.cshtml** - Actualización con Lifecycle Intelligence
**Diseño**: Formulario de dos columnas + Panel lateral de lifecycle

**Características Especiales**:
- ✅ **Barra de Progreso de Vida Útil**:
  - Verde: >50% vida restante
  - Amarillo: 20-50% vida restante
  - Rojo: <20% vida restante
- ✅ **Alerta de Reemplazo**: Aparece si el equipo ha superado el 80% de su vida útil
- ✅ Panel de estado operativo destacado (border-warning)
- ✅ Auditoría con timestamps completos

**Código de Lifecycle**:
```csharp
var percent = 0;
if (Model.Equipment.UsefulLifeYears.HasValue && Model.Equipment.AgeYears.HasValue) {
    percent = (int)(100 - ((double)Model.Equipment.AgeYears / Model.Equipment.UsefulLifeYears * 100));
    percent = Math.Max(0, Math.Min(100, percent));
}
var progressColor = percent < 20 ? "bg-danger" : (percent < 50 ? "bg-warning" : "bg-success");
```

#### **Details.cshtml** - Expediente Técnico del Activo
**Diseño**: Tarjeta principal + Panel lateral financiero

**Características**:
- ✅ Badge de estado prominente en header
- ✅ Tabla de especificaciones técnicas
- ✅ **Historial de Disponibilidad**: Tabla con todos los cambios de estado
  - Fecha Desde / Hasta
  - Duración en días
  - Badge de estado actual (verde)
- ✅ Panel lateral con datos financieros y barra de progreso
- ✅ Panel de auditoría con CreatedBy/ModifiedBy

#### **Delete.cshtml** ⚠️
**Estado**: Usuario prefirió mantener la versión original (sin SweetAlert2)

---

### 3️⃣ MÓDULO DE TIPOS DE EQUIPOS ✅ (100% Completado)

**Nota Arquitectural**: Este módulo fue **eliminado del Sidebar** y ahora vive como una pestaña dentro de `Equipment/Index`. Esto simplifica la navegación y agrupa lógicamente las entidades relacionadas.

#### **Create.cshtml** - Definición de Categoría
**Diseño**: Tarjeta centrada (col-lg-7)

**Características**:
- ✅ Campo de Nombre prominente (form-control-lg border-warning)
- ✅ Descripción/Alcance (textarea)
- ✅ Sección de calibración con checkbox moderno
- ✅ Frecuencia de Mantenimiento (input numérico en meses)

#### **Edit.cshtml** - Actualización de Estándar Técnico
**Diseño**: Idéntico a Create, pero con datos prellenados

#### **Details.cshtml** - Expediente de Categoría
**Diseño**: Vista centrada con información estructurada

**Características**:
- ✅ Nombre de la Familia (h3 destacado)
- ✅ Badge de Calibración (coloreado según requerimiento)
- ✅ Frecuencia de Mantenimiento Sugerida
- ✅ Panel de auditoría

#### **Delete.cshtml** ⚠️
**Estado**: Usuario prefirió mantener la versión original

---

## 🎯 PATRONES DE DISEÑO IMPLEMENTADOS

### 1. **Smart Index Pattern**
**Ubicación**: Presente en todos los `Index.cshtml`

**Estructura HTML**:
```html
<div class="bg-light p-3 mb-4 rounded border-left border-info shadow-sm">
    <form method="get">
        <div class="row align-items-end">
            <div class="col-md-5">
                <label class="small font-weight-bold text-muted uppercase">Búsqueda Técnica</label>
                <div class="input-group drop-shadow">
                    <div class="input-group-prepend">
                        <span class="input-group-text bg-white border-right-0">
                            <i class="fas fa-search text-muted"></i>
                        </span>
                    </div>
                    <input type="text" asp-for="SearchTerm" class="form-control border-left-0" placeholder="Nombre, Marca o N° Inventario..." />
                </div>
            </div>
            <div class="col-md-3">
                <label class="small font-weight-bold text-muted uppercase">Filtrar por [Criterio]</label>
                <select asp-for="FilterCriteria" class="form-control custom-select">
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

**Backend Pattern**:
```csharp
[BindProperty(SupportsGet = true)]
public string? SearchTerm { get; set; }

[BindProperty(SupportsGet = true)]
public TipoFiltro? FilterCriteria { get; set; }

public async Task OnGetAsync()
{
    var query = _context.Entidades.Where(e => e.Status != Status.Eliminado);
    
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

### 2. **Dual-Tab Architecture**
**Implementado en**: Users/Index, Equipment/Index

**Ventajas**:
- ✅ Reduce ruido en el Sidebar
- ✅ Agrupa entidades lógicamente relacionadas
- ✅ Mantiene al usuario en contexto
- ✅ Permite gestión rápida sin cambiar de módulo

**Estructura de Pestañas**:
```html
<ul class="nav nav-tabs customtab px-4 pt-3" role="tablist">
    <li class="nav-item">
        <a class="nav-link active" data-toggle="tab" href="#tab1" role="tab">
            <i class="fas fa-icon1 mr-2"></i> Entidad Principal
        </a>
    </li>
    <li class="nav-item">
        <a class="nav-link" data-toggle="tab" href="#tab2" role="tab">
            <i class="fas fa-icon2 mr-2"></i> Entidad Secundaria
        </a>
    </li>
</ul>

<div class="tab-content">
    <div class="tab-pane active" id="tab1" role="tabpanel">
        <!-- Contenido Tab 1 -->
    </div>
    <div class="tab-pane" id="tab2" role="tabpanel">
        <!-- Contenido Tab 2 -->
    </div>
</div>
```

**CSS Personalizado** (requerido):
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
```

---

### 3. **Lifecycle Intelligence Pattern**
**Implementado en**: Equipment/Edit, Equipment/Details

**Concepto**: Visualizar el ciclo de vida del activo mediante:
- Cálculo de antigüedad (años desde adquisición)
- Barra de progreso de vida útil restante
- Alertas de reemplazo cuando se supera el 80% de uso

**Ejemplo Visual**:
```html
<div class="progress mt-1" style="height: 6px;">
    @{
        var percent = (int)(100 - ((double)AgeYears / UsefulLifeYears * 100));
        var color = percent < 20 ? "bg-danger" : (percent < 50 ? "bg-warning" : "bg-success");
    }
    <div class="progress-bar @color" style="width: @percent%;"></div>
</div>
<span class="small">@percent% de vida operativa restante</span>

@if (RequiresReplacement)
{
    <div class="alert alert-warning mt-3">
        <i class="fas fa-exclamation-triangle mr-2"></i> 
        <strong>Atención:</strong> Este equipo ha superado el 80% de su vida útil.
    </div>
}
```

---

### 4. **Historial de Estados Pattern**
**Implementado en**: Equipment/Details

**Concepto**: Cada cambio de estado del equipo se registra en la tabla `EquipmentStateHistory`. La vista Details muestra una tabla temporal con:
- Estado
- Fecha Desde / Hasta
- Duración (calculada en días)
- Badge verde para el estado actual

**Tabla HTML**:
```html
<table class="table table-hover v-middle small border">
    <thead class="bg-light">
        <tr>
            <th>Estado</th>
            <th>Desde</th>
            <th>Hasta</th>
            <th>Duración</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var history in StateHistory.OrderByDescending(h => h.StartDate))
        {
            <tr>
                <td>
                    <span class="badge badge-pill @(history.EndDate == null ? "badge-success" : "badge-light") px-2">
                        @history.Status
                    </span>
                </td>
                <td>@history.StartDate.ToString("dd/MM/yyyy HH:mm")</td>
                <td>@(history.EndDate?.ToString("dd/MM/yyyy HH:mm") ?? "En curso")</td>
                <td>@(history.DurationDays ?? "-") días</td>
            </tr>
        }
    </tbody>
</table>
```

---

## 🎨 COMPONENTES PREMIUM REUTILIZABLES

### 1. **Badge de Estado Dinámico**
```html
@{
    var badgeClass = Status switch {
        GeneralStatus.Activo => "badge-success",
        GeneralStatus.Inactivo => "badge-secondary",
        GeneralStatus.Eliminado => "badge-danger",
        _ => "badge-light"
    };
}
<span class="badge @badgeClass px-3 py-1">@Status</span>
```

### 2. **Botones de Acción con Tooltips**
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
    <a asp-page="./Delete" asp-route-id="@Id" 
       class="btn btn-sm btn-outline-danger btn-rounded" 
       data-toggle="tooltip" title="Eliminar">
        <i class="fas fa-trash"></i>
    </a>
</div>

@section Scripts {
    <script>
        $('[data-toggle="tooltip"]').tooltip();
    </script>
}
```

### 3. **Card Header Moderna**
```html
<div class="card shadow-sm border-0">
    <div class="card-body p-4">
        <div class="d-flex no-block align-items-center mb-4">
            <div>
                <h4 class="card-title text-dark">Título Principal</h4>
                <h6 class="card-subtitle text-muted">Subtítulo descriptivo</h6>
            </div>
            <div class="ml-auto">
                <i class="fas fa-icon fa-2x text-info opacity-5"></i>
            </div>
        </div>
        <!-- Contenido -->
    </div>
</div>
```

---

## 📊 ESTADÍSTICAS DE TRANSFORMACIÓN

| Módulo | Vistas Actualizadas | Patrones Aplicados | Estado |
|--------|---------------------|-------------------|--------|
| **Usuarios** | 5/5 (100%) | Smart Index, Dual-Tab, SweetAlert2 | ✅ Completo |
| **Equipos** | 4/5 (80%) | Smart Index, Dual-Tab, Lifecycle | ✅ Completo |
| **Tipos Equipos** | 3/4 (75%) | Form Premium, Audit Panel | ✅ Completo |
| **Laboratorios** | 0/5 (0%) | - | 🔄 Pendiente |
| **Facultades** | 0/5 (0%) | - | 🔄 Pendiente |

**Total de Archivos Modificados**: 14 archivos .cshtml + 2 archivos .cshtml.cs

---

## 🚀 PRÓXIMOS PASOS RECOMENDADOS

### Fase 1 - Laboratorios y Facultades (Siguiente)
1. Implementar Dual-Tab en `Laboratories/Index` (Laboratorios + Facultades)
2. Aplicar Smart Index Pattern con búsqueda por código, nombre, facultad
3. Diseñar Create/Edit con información de ubicación física (Edificio, Piso)
4. Details con mapa de ubicación e inventario de equipos asignados

### Fase 2 - Dashboard Inteligente
1. Crear `Pages/Index.cshtml` con widgets de resumen:
   - Total Equipos (por estado)
   - Total Laboratorios Activos
   - Equipos que requieren mantenimiento próximo
   - Alertas de reemplazo
2. Gráficos con Chart.js (distribución de equipos por facultad)

### Fase 3 - Módulo de Mantenimiento
1. Aplicar todos los patrones Premium
2. Integrar calendario de mantenimientos pendientes
3. Alertas automáticas cuando un equipo se acerca a su fecha de mantenimiento

---

## 🛠️ GUÍA DE USO PARA NUEVOS MÓDULOS

Cuando trabajes en un **nuevo módulo**, sigue esta checklist:

### ✅ Index.cshtml
- [ ] Implementar Smart Index Zone (búsqueda + filtros)
- [ ] Si tiene entidad relacionada pequeña, usar Dual-Tab
- [ ] Tabla con `table table-hover v-middle`
- [ ] Badges de estado con switch de colores
- [ ] Botones de acción con tooltips

### ✅ Create.cshtml
- [ ] Card centrada (col-lg-10 o menor)
- [ ] Formulario de dos columnas si tiene muchos campos
- [ ] Validación en tiempo real (jqBootstrapValidation)
- [ ] Botones alineados a la derecha con iconos

### ✅ Edit.cshtml
- [ ] Mismo layout que Create
- [ ] Panel lateral con auditoría (CreatedBy, ModifiedBy)
- [ ] Si aplica, agregar lifecycle intelligence

### ✅ Details.cshtml
- [ ] Card centrada con información estructurada
- [ ] Panel lateral con datos complementarios
- [ ] Si hay historial, mostrarlo en tabla

### ✅ Delete.cshtml
- [ ] Alerta visual de advertencia
- [ ] Resumen de datos críticos
- [ ] Confirmación con SweetAlert2 (opcional según preferencia del usuario)

---

## 📝 NOTAS TÉCNICAS IMPORTANTES

1. **Enum EquipmentStatus**: Se actualizó explícitamente `OutOfService = 2` para evitar conflictos de mapeo en DB.

2. **Soft Delete**: Todos los módulos usan `Status != GeneralStatus.Eliminado` en las queries. Los registros nunca se borran físicamente.

3. **Navegación Contextual**: Al eliminar "Tipos de Equipos" del Sidebar, los links de Cancel/Volver apuntan a `/Equipment/Index` con el fragmento de pestaña.

4. **Validación Doble Capa**: Frontend (jqBootstrapValidation) + Backend (DataAnnotations + ModelState).

5. **Assets de SweetAlert2**: Ya están incluidos en `wwwroot/lib/nice-admin/assets/libs/sweetalert2/`.

---

**Documento generado**: 26 de enero de 2026  
**Autor**: AI Assistant en colaboración con Wilmher  
**Versión del Sistema**: ASP.NET Core 9.0 + NiceAdmin Template
