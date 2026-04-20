# Gastro — Componente: Calendario (`calendar.html`)

> **Para agentes de IA:** Este archivo documenta la arquitectura, dependencias y puntos de personalización del componente de Calendario integrado en el proyecto Gastro. Léelo completo antes de hacer cualquier edición.

---

## 1. ¿Qué contiene esta carpeta?

Este módulo es una extracción autónoma del componente **App Calendar** de la plantilla **Nice Admin** (FullCalendar v3 + jQuery UI). Puede funcionar de forma independiente o integrarse en un proyecto más grande (ASP.NET MVC, Laravel, etc.).

```
Gastro/
├── calendar.html                           ← Página funcional lista para abrir
├── CALENDAR_README.md                      ← Este archivo
│
├── assets/
│   ├── libs/
│   │   ├── jquery/dist/
│   │   │   └── jquery.min.js               ← jQuery 3.x (base de todo)
│   │   ├── popper.js/dist/umd/
│   │   │   └── popper.min.js
│   │   ├── bootstrap/dist/js/
│   │   │   └── bootstrap.min.js
│   │   ├── perfect-scrollbar/dist/
│   │   │   └── perfect-scrollbar.jquery.min.js
│   │   ├── moment/min/
│   │   │   └── moment.min.js               ← REQUERIDO por FullCalendar
│   │   └── fullcalendar/dist/
│   │       ├── fullcalendar.min.css        ← Estilos del grid del calendario
│   │       └── fullcalendar.min.js         ← Motor del calendario
│   │
│   ├── extra-libs/
│   │   ├── calendar/
│   │   │   └── calendar.css               ← Estilos custom (colores de eventos)
│   │   ├── sparkline/
│   │   │   └── sparkline.js
│   │   └── taskboard/js/
│   │       ├── jquery-ui.min.js           ← REQUERIDO para Drag & Drop
│   │       └── jquery.ui.touch-punch-improved.js  ← Drag en móviles
│   │
│   └── images/
│       └── [logos del sistema]
│
└── dist/
    ├── css/
    │   └── style.min.css                  ← Estilos base de Nice Admin (Bootstrap custom)
    └── js/
        ├── app.min.js                     ← Framework Nice Admin
        ├── app.init.js
        ├── app-style-switcher.js
        ├── waves.js                       ← Efecto ripple en botones
        ├── sidebarmenu.js                 ← Comportamiento del sidebar
        ├── custom.min.js
        └── pages/calendar/
            └── cal-init.js               ← ⚠️ ARCHIVO CLAVE — Lógica del calendario
```

---

## 2. Arquitectura del archivo `calendar.html`

El HTML sigue la estructura obligatoria de Nice Admin. **No cambies el orden de los wrappers.**

```
<body>
  .preloader                     ← Pantalla de carga (automática)
  #main-wrapper                  ← Wrapper raíz del template
    header.topbar                ← Navbar superior
    aside.left-sidebar           ← Sidebar izquierdo
    .page-wrapper                ← TODO EL CONTENIDO VA AQUÍ
      .page-breadcrumb           ← Título de página + migas de pan
      .container-fluid           ← Contenido principal
        .row > .col-md-12        ← Columna única
          .card                  ← La card que contiene el calendario
            .col-lg-3            ← Panel Drag & Drop (izquierdo)
            .col-lg-9            ← Área del calendario (derecho)
        #my-event (modal)        ← Modal: editar/eliminar evento
        #add-new-event (modal)   ← Modal: agregar nueva categoría
  [scripts al final del body]
```

---

## 3. Orden de carga de scripts (CRÍTICO)

> **⚠️ No alteres este orden.** Cada script depende del anterior.

```html
<!-- 1. jQuery → base de todo -->
<script src="assets/libs/jquery/dist/jquery.min.js"></script>

<!-- 2. jQuery UI → ANTES de Bootstrap, necesario para draggable() -->
<script src="assets/extra-libs/taskboard/js/jquery.ui.touch-punch-improved.js"></script>
<script src="assets/extra-libs/taskboard/js/jquery-ui.min.js"></script>

<!-- 3. Bootstrap -->
<script src="assets/libs/popper.js/dist/umd/popper.min.js"></script>
<script src="assets/libs/bootstrap/dist/js/bootstrap.min.js"></script>

<!-- 4. Framework Nice Admin (no tocar) -->
<script src="dist/js/app.min.js"></script>
<script src="dist/js/app.init.js"></script>
<script src="dist/js/app-style-switcher.js"></script>
<script src="assets/libs/perfect-scrollbar/dist/perfect-scrollbar.jquery.min.js"></script>
<script src="assets/extra-libs/sparkline/sparkline.js"></script>
<script src="dist/js/waves.js"></script>
<script src="dist/js/sidebarmenu.js"></script>
<script src="dist/js/custom.min.js"></script>

<!-- 5. Momento → SIEMPRE antes de FullCalendar -->
<script src="assets/libs/moment/min/moment.min.js"></script>
<script src="assets/libs/fullcalendar/dist/fullcalendar.min.js"></script>

<!-- 6. Inicializador del calendario → SIEMPRE al final -->
<script src="dist/js/pages/calendar/cal-init.js"></script>
```

---

## 4. IDs críticos del HTML (no renombrar)

Estos IDs son buscados directamente por `cal-init.js`. Si los cambias, el calendario se rompe.

| ID / Selector | Ubicación en HTML | Qué hace `cal-init.js` con él |
| :--- | :--- | :--- |
| `#calendar` | `col-lg-9` | Renderiza el grid del calendario aquí |
| `#calendar-events` | Panel lateral | Convierte sus hijos en eventos arrastrables |
| `#calendar-events div.calendar-events` | Dentro de `#calendar-events` | Cada div = un tipo de evento con color |
| `#my-event` | Modal | Se abre al clicar un evento existente |
| `#add-new-event` | Modal | Se abre con el botón "+ Add New Event" |
| `#add-new-event form` | Dentro del modal | Lee los inputs para crear nueva categoría |
| `#drop-remove` | Checkbox | Si está marcado, elimina el evento del panel tras soltarlo |
| `.save-category` | Botón en modal | Agrega nueva categoría a `#calendar-events` |

---

## 5. Iconos: solución a íconos que aparecen como □

El `style.min.css` necesita archivos de fuente `.woff` que **no están incluidos localmente**. La solución activa es cargar 3 CDNs en el `<head>`:

```html
<!-- SIN ESTO los íconos (fa-circle, ti-plus, mdi-*) aparecen como cuadros □ -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/themify-icons@0.1.2/css/themify-icons.css">
<link rel="stylesheet" href="https://cdn.materialdesignicons.com/2.0.46/css/materialdesignicons.min.css">
```

**Para uso offline:** Copia las carpetas de fuentes desde `Nice-Admin/assets/libs/font-awesome/`, `/themify-icons/` y cambia los CDN por rutas locales. No están en Gastro porque son ~5MB adicionales.

---

## 6. Cómo personalizar eventos por defecto

Edita `dist/js/pages/calendar/cal-init.js` — busca el array `defaultEvents` (línea ~129).

### Estructura de un evento

```javascript
{
    title: 'Nombre del Evento',         // Texto que aparece en el calendario
    start: new Date(),                  // Fecha de inicio (Date object)
    end: new Date(),                    // (Opcional) Fecha de fin
    className: 'bg-info'               // Color de fondo del evento
}
```

### Calcular fechas relativas al día de hoy

```javascript
var hoy = new Date($.now());
var manana  = new Date($.now() + 86400000);   // +1 día  (86400000ms)
var semana  = new Date($.now() + 604800000);  // +7 días
var pasado  = new Date($.now() - 86400000);   // -1 día (eventos del pasado)
```

### Colores disponibles

| Clase `className` | Color visible |
| :--- | :--- |
| `bg-info` | Azul claro |
| `bg-success` | Verde |
| `bg-danger` | Rojo |
| `bg-warning` | Amarillo/Naranja |
| `bg-purple` | Morado |
| `bg-primary` | Azul marino |
| `bg-dark` | Gris oscuro |

---

## 7. Cómo agregar tipos de eventos arrastrables

En `calendar.html`, dentro de `#calendar-events`, agrega un nuevo `div` con el patrón:

```html
<div class="calendar-events m-b-20" data-class="bg-purple">
    <i class="fa fa-circle text-purple m-r-10"></i>Mi Nuevo Tipo
</div>
```

- `data-class` → define el color CSS que tendrá el evento al soltarlo en el calendario.
- La clase `text-*` del icono debe coincidir con el color de `data-class`.

---

## 8. Integración en proyectos con framework (ASP.NET MVC / Laravel / etc.)

### Qué va en el Layout (`_Layout.cshtml` o `layout.blade.php`)
```html
<!-- En <head> -->
<link rel="stylesheet" href="[CDN Font Awesome]">
<link rel="stylesheet" href="[CDN Themify Icons]">
<link rel="stylesheet" href="[CDN Material Design Icons]">
<link href="~/dist/css/style.min.css" rel="stylesheet">

<!-- Al final de <body> (scripts core) -->
<script src="~/assets/libs/jquery/dist/jquery.min.js"></script>
<script src="~/assets/extra-libs/taskboard/js/jquery-ui.min.js"></script>
<script src="~/assets/libs/popper.js/dist/umd/popper.min.js"></script>
<script src="~/assets/libs/bootstrap/dist/js/bootstrap.min.js"></script>
<script src="~/dist/js/app.min.js"></script>
<script src="~/dist/js/custom.min.js"></script>
@RenderSection("Scripts", required: false)
```

### Qué va en la View del Calendario (`Calendar.cshtml`)
```html
@section Styles {
    <link href="~/assets/libs/fullcalendar/dist/fullcalendar.min.css" rel="stylesheet" />
    <link href="~/assets/extra-libs/calendar/calendar.css" rel="stylesheet" />
}

<!-- Pegar aquí el contenido de .container-fluid del calendar.html (sin navbar/sidebar) -->

@section Scripts {
    <script src="~/assets/libs/moment/min/moment.min.js"></script>
    <script src="~/assets/libs/fullcalendar/dist/fullcalendar.min.js"></script>
    <script src="~/dist/js/pages/calendar/cal-init.js"></script>
}
```

---

## 9. Errores comunes y sus soluciones

| Síntoma | Causa | Solución |
| :--- | :--- | :--- |
| Calendario en blanco / no aparece | `cal-init.js` cargó antes de FullCalendar | Verifica el orden de scripts (Sección 3) |
| Íconos ● aparecen como □ | Font Awesome / Themify no cargados | Agregar los 3 CDN del `<head>` (Sección 5) |
| Drag & Drop no funciona | `jquery-ui.min.js` cargó DESPUÉS de Bootstrap | jQuery UI debe ir ANTES de Bootstrap |
| Modal no abre | ID del modal no coincide | Verificar que `data-target="#add-new-event"` coincida con `id="add-new-event"` |
| Eventos se ven sin color | `className` incorrecto en `defaultEvents` | Usar exactamente `bg-info`, `bg-danger`, etc. |
| `moment is not defined` | Moment cargó después de FullCalendar | Moment SIEMPRE antes de `fullcalendar.min.js` |

---

## 10. Origen de los archivos

- **Plantilla fuente:** `Nice-Admin/html/ltr/app-calendar.html`
- **Script fuente:** `Nice-Admin/dist/js/pages/calendar/cal-init.js`
- **CSS fuente:** `Nice-Admin/assets/libs/fullcalendar/` y `assets/extra-libs/calendar/`
- **Librería:** [FullCalendar v3](https://fullcalendar.io/docs/v3) — usa la API v3, NO la v4/v5 (son incompatibles)
