# Instrucciones para implementar C3 Charts (Donut + Bar) en Proyecto-Laboratorios-Univalle

## Recursos incluidos en esta carpeta

Todos los archivos necesarios están ya copiados dentro de esta carpeta `recursos/`.  
La ruta base es:
```
C:\Users\Nihaht\Desktop\Nice Admin\Nice-Admin-master\recursos\
```

### Archivos a copiar al proyecto destino

| # | Archivo en `recursos/` | Destino en el proyecto |
|---|---|---|
| 1 | `assets/extra-libs/c3/d3.min.js` | `wwwroot/assets/extra-libs/c3/d3.min.js` |
| 2 | `assets/extra-libs/c3/c3.min.js` | `wwwroot/assets/extra-libs/c3/c3.min.js` |
| 3 | `assets/extra-libs/c3/c3.min.css` | `wwwroot/assets/extra-libs/c3/c3.min.css` |

> Los archivos `c3-bar.js` y `c3-donut.js` en `recursos/dist/js/pages/c3-chart/bar-pie/` son solo de referencia (datos demo). La página destino usará su propio script inline con datos reales del servidor. Si por alguna razón `wwwroot/dist/js/pages/c3-chart/bar-pie/c3-bar.js` o `c3-donut.js` no existen en el proyecto, cópialos también.

---

## Paso 1 — Copiar librerías C3

Crear la carpeta destino y copiar los 3 archivos:

```
Origen:  C:\Users\Nihaht\Desktop\Nice Admin\Nice-Admin-master\recursos\assets\extra-libs\c3\
Destino: C:\Users\Nihaht\Desktop\Vamoooooos\Proyecto-Laboratorios-Univalle\wwwroot\assets\extra-libs\c3\
```

Archivos a copiar: `d3.min.js`, `c3.min.js`, `c3.min.css`

---

## Paso 2 — Modificar `_Layout.cshtml`

Archivo: `Pages/Shared/_Layout.cshtml`

### 2a. Agregar CSS en `<head>`

Después de la línea donde se carga `sweetalert2.min.css`, agregar:

```html
<link href="~/assets/extra-libs/c3/c3.min.css" rel="stylesheet">
```

### 2b. Agregar JS al final de `<body>`

Después de la línea donde se carga `sweetalert2.all.min.js`, agregar:

```html
<script src="~/assets/extra-libs/c3/d3.min.js"></script>
<script src="~/assets/extra-libs/c3/c3.min.js"></script>
```

**Orden verificado:**
```
jquery.min.js → popper.min.js → bootstrap.min.js → app.min.js → app.init.js
→ waves.js → sidebarmenu.js → custom.min.js → sweetalert2.all.min.js
→ d3.min.js         ← NUEVO
→ c3.min.js         ← NUEVO
→ @RenderSection("Scripts", required: false)
```

---

## Paso 3 — Modificar `Index.cshtml.cs` (PageModel)

Archivo: `Pages/AssetView/Index.cshtml.cs`

Agregar estas propiedades a la clase:

```csharp
// Propiedades para C3 Charts
public string CategoryDataJson { get; set; } = "[]";
public string EquipmentSubClassJson { get; set; } = "[]";
public string UtensilSubClassJson { get; set; } = "[]";
public string OtherSubClassJson { get; set; } = "[]";
```

Agregar estas queries dentro de `OnGetAsync()` (al final, antes del return):

```csharp
// Datos para C3 Donut Chart — Total de activos por categoría
var categoryData = await _context.EquipmentUnits
    .GroupBy(eu => eu.Equipment.Category)
    .Select(g => new
    {
        Category = g.Key.ToString(),
        Count = g.Count()
    })
    .ToListAsync();

CategoryDataJson = JsonConvert.SerializeObject(categoryData.Select(c =>
    new[] { c.Category, (object)c.Count }
));

// Bar Chart 1 — Equipos: subclasificación por TypeClassification
var equipmentData = await _context.EquipmentUnits
    .Where(eu => eu.Equipment.Category == Category.Equipment)
    .GroupBy(eu => eu.Equipment.TypeClassification)
    .Select(g => new
    {
        Label = g.Key.ToString(),
        Count = g.Count()
    })
    .ToListAsync();

EquipmentSubClassJson = JsonConvert.SerializeObject(equipmentData.Select(e =>
    new[] { e.Label, (object)e.Count }
));

// Bar Chart 2 — Utensilios: subclasificación por UtensilType
var utensilData = await _context.EquipmentUnits
    .Where(eu => eu.Equipment.Category == Category.Utensil
        && eu.Equipment.UtensilType != UtensilType.NoAplica)
    .GroupBy(eu => eu.Equipment.UtensilType)
    .Select(g => new
    {
        Label = g.Key.ToString(),
        Count = g.Count()
    })
    .ToListAsync();

UtensilSubClassJson = JsonConvert.SerializeObject(utensilData.Select(u =>
    new[] { u.Label, (object)u.Count }
));

// Bar Chart 3 — Otros: subclasificación por TypeClassification
var otherData = await _context.EquipmentUnits
    .Where(eu => eu.Equipment.Category == Category.Other)
    .GroupBy(eu => eu.Equipment.TypeClassification)
    .Select(g => new
    {
        Label = g.Key.ToString(),
        Count = g.Count()
    })
    .ToListAsync();

OtherSubClassJson = JsonConvert.SerializeObject(otherData.Select(o =>
    new[] { o.Label, (object)o.Count }
));
```

> **Nota:** Los nombres de entidades (`EquipmentUnits`, `Equipment.Category`, `Equipment.TypeClassification`, `Equipment.UtensilType`, `Category.Equipment`, `UtensilType.NoAplica`, etc.) pueden variar. Verifica el modelo real en `Models/` y ajusta si es necesario. Lo importante es la estructura del query.

---

## Paso 4 — Modificar `Index.cshtml` (Vista)

Archivo: `Pages/AssetView/Index.cshtml`

### 4a. Reemplazar el placeholder

Buscar este bloque:
```html
<div class="bg-light p-5 text-center rounded ...">
    <i class="fas fa-chart-line fa-3x text-info mb-3"></i>
    <h4 class="font-weight-bold text-dark">Metricas en preparacion</h4>
    <p class="text-muted mb-0">Esta seccion queda lista para conectar indicadores...</p>
</div>
```

Reemplazarlo por:

```html
<!-- Donut Chart — Total de activos por categoría -->
<div class="row">
    <div class="col-12">
        <div class="card">
            <div class="card-body">
                <h4 class="card-title">Total de Activos por Categoría</h4>
                <div id="donut-chart"></div>
            </div>
        </div>
    </div>
</div>

<!-- Bar Charts — Subclasificaciones por categoría -->
<div class="row">
    <div class="col-lg-4">
        <div class="card">
            <div class="card-body">
                <h4 class="card-title">Equipos</h4>
                <div id="bar-chart-equipment"></div>
            </div>
        </div>
    </div>
    <div class="col-lg-4">
        <div class="card">
            <div class="card-body">
                <h4 class="card-title">Utensilios</h4>
                <div id="bar-chart-utensil"></div>
            </div>
        </div>
    </div>
    <div class="col-lg-4">
        <div class="card">
            <div class="card-body">
                <h4 class="card-title">Otros</h4>
                <div id="bar-chart-other"></div>
            </div>
        </div>
    </div>
</div>
```

### 4b. Agregar `@section Scripts` al final del archivo

```html
@section Scripts {
<script>
$(function() {
    // Datos desde el servidor
    var categoryData = @Html.Raw(Model.CategoryDataJson);
    var equipData = @Html.Raw(Model.EquipmentSubClassJson);
    var utensilData = @Html.Raw(Model.UtensilSubClassJson);
    var otherData = @Html.Raw(Model.OtherSubClassJson);

    // ========== Donut Chart ==========
    if (categoryData.length > 0) {
        c3.generate({
            bindto: "#donut-chart",
            color: { pattern: ["#2962FF", "#4fc3f7", "#f62d51"] },
            data: {
                columns: categoryData,
                type: "donut"
            },
            donut: {
                title: "Total Activos"
            }
        });
    } else {
        $("#donut-chart").html('<p class="text-center text-muted">Sin datos disponibles</p>');
    }

    // ========== Bar Chart — Equipos ==========
    if (equipData.length > 0) {
        c3.generate({
            bindto: "#bar-chart-equipment",
            size: { height: equipData.length * 30 + 60 },
            color: { pattern: ["#2962FF"] },
            data: {
                columns: equipData,
                type: "bar"
            },
            axis: {
                rotated: true,
                x: { type: "category" }
            },
            grid: { y: { show: true } },
            legend: { show: false }
        });
    } else {
        $("#bar-chart-equipment").html('<p class="text-center text-muted">Sin datos</p>');
    }

    // ========== Bar Chart — Utensilios ==========
    if (utensilData.length > 0) {
        c3.generate({
            bindto: "#bar-chart-utensil",
            size: { height: utensilData.length * 30 + 60 },
            color: { pattern: ["#4fc3f7"] },
            data: {
                columns: utensilData,
                type: "bar"
            },
            axis: {
                rotated: true,
                x: { type: "category" }
            },
            grid: { y: { show: true } },
            legend: { show: false }
        });
    } else {
        $("#bar-chart-utensil").html('<p class="text-center text-muted">Sin datos</p>');
    }

    // ========== Bar Chart — Otros ==========
    if (otherData.length > 0) {
        c3.generate({
            bindto: "#bar-chart-other",
            size: { height: otherData.length * 30 + 60 },
            color: { pattern: ["#f62d51"] },
            data: {
                columns: otherData,
                type: "bar"
            },
            axis: {
                rotated: true,
                x: { type: "category" }
            },
            grid: { y: { show: true } },
            legend: { show: false }
        });
    } else {
        $("#bar-chart-other").html('<p class="text-center text-muted">Sin datos</p>');
    }
});
</script>
}
```

---

## Paso 5 — Verificación

Después de implementar, verifica:

1. **Archivos copiados:** `wwwroot/assets/extra-libs/c3/` contiene `d3.min.js`, `c3.min.js`, `c3.min.css`
2. **CSS en Layout:** `c3.min.css` se enlaza en `<head>` de `_Layout.cshtml`
3. **JS en Layout:** `d3.min.js` y `c3.min.js` se cargan en `<body>` después de jQuery
4. **PageModel:** Las 4 propiedades JSON existen y se llenan en `OnGetAsync()`
5. **Vista:** El placeholder fue reemplazado por los 4 charts + `@section Scripts`
6. **Consola del navegador:** Sin errores de `c3 is not defined` ni `d3 is not defined`

---

## Esquema de colores

| Chart | Color | Significado |
|---|---|---|
| Donut: Equipos | `#2962FF` (azul) | Coincide con cards `bg-primary` |
| Donut: Utensilios | `#4fc3f7` (celeste) | Coincide con `bg-success` |
| Donut: Otros | `#f62d51` (rojo) | Coincide con `bg-warning` |
| Bar Equipos | `#2962FF` | Consistente con donut |
| Bar Utensilios | `#4fc3f7` | Consistente con donut |
| Bar Otros | `#f62d51` | Consistente con donut |
