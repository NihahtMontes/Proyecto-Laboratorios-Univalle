---
name: ui_premium
description: Control estético, UI Asimétrica 8/4, Dual-Tab, Smart Index y Javascript/Validaciones Frontend OBLIGATORIO en las Vistas (HTML).
trigger: Modificación / Creación de archivos en `/Pages/` (Ej: `Index.cshtml`, `Edit.cshtml`, JavaScript Validation).
author: Antigravity Orquestador Alpha
version: 1.0.0
license: MIT
scope: UI
tools_allowed: [view_file, write_to_file, multi_replace_file_content, replace_file_content]
---
# 🎨 SKILL: Control Visual Premium (UI/UX)
**Objetivo Primario:** Elevar la plantilla NiceAdmin de un simple andamio HTML a una UI/UX de alta ingeniería mediante asimetría, colores suaves, tabulación inteligente y validación inmediata.

## 1. La Filosofía Visual "Premium"
No aceptamos formularios monótonos o páginas desnudas. Toda vista, sin excepción, necesita peso visual y contexto.

### 1.1 Header Hero Decorativo
Las páginas principales (`Index`) deben tener un encabezado vistoso, con un icono de gran tamaño en baja opacidad para agregar textura, y textos contrastantes.
*Herramienta:* Usa las clases `.opacity-05`, `.card-subtitle.text-muted` y `.bg-light`.

### 1.2 Jerarquía de Botones de Acción (Action Buttons)
- **Acciones primarias e inserción (Crear, Guardar):** Usan obligatoriamente `.btn-info.btn-rounded.shadow-sm`.
- **Acciones reactivas / edición:** `.btn-warning`.
- **Destrucción o Riesgo:** `.btn-danger`.
- **Retroceso y Neutro:** `.btn-outline-secondary`.
- **Filtrado Múltiple:** `.btn-dark`.

## 2. Arquitecturas Requeridas según el Propósito

### 2.1 Index.cshtml (Smart Index & Dual-Tab)
Esta vista NUNCA debe ser una tabla solitaria.
- **Smart Index Zone:** Zona superior delimitada por `.bg-light.p-3.shadow-sm.border-left.border-info`. Requiere un `<form method="get">` para búsqueda robusta y/o filtros `select`. Acompañada de sus botones Filtrar y Limpiar alineados a la derecha.
- **Dual-Tab Architecture:** Si te solicitan crear o modificar un módulo con "sub-ítems" (Ej. Equipos vs Tipos de Equipos, Usuarios vs Eliminados), agrúpalos en pestañas `.nav-tabs.customtab` y usa la lógica JS nativa de Bootstrap 4 para conmutarlos, reduciendo navegación extra al usuario.

### 2.2 Create / Edit (Layout Asimétrico 8/4)
La regla de oro: Rompe la columna única.
- **Zona de Trabajo (`col-lg-8`):** Coloca aquí los atributos pesados: campos obligatorios largos, textareas o datos financieros. Separa por secciones `<h5 class="text-info">`.
- **Zona de Mando Lateral (`col-lg-4`):** Coloca paneles fijos con `.bg-dark` o `bg-info`: Progreso, `CreatedBy` (Auditoría), `LastModifiedBy`, estados actuales con `badge`, y los botones finales `Guardar/Cancelar` siempre perceptibles a la derecha de la vista.

### 2.3 Details.cshtml (Lifecycle Intelligence)
Muestra la "Ficha Técnica Suprema".
- Dibuja el historial (Tablas con estados y fechas de inicio/fin). Transforma variables como *"Años de Uso"* (si existen) en Barras de Progreso usando `width: @porcentaje%` y cambia su clase de `bg-success` a `bg-warning` / `bg-danger` a medida que envejezca el activo.

### 2.4 Delete.cshtml ("Clean Card")
- Visión Centrada Obligatoria: `.row.justify-content-center` con `.min-height: 60vh`.
- Ícono de advertencia de gran formato (tamaño texto gigante).
- Componente "Resumen Destacado" en gris con lo que se eliminará.

## 3. Feedback y Javascript de Validación

### 3.1 SweetAlert2 C# Integration via TempData
Si el `PageModel` arrojó una constante como `TempData.Success(NotificationHelper.Entities.Saved)` al volver de un POST exitoso:
El script general del Layout usa SweetAlert2 para mostrarlo.
**⚠️ CRÍTICO XSS:** Al armar la alerta manual, envuelve cualquier string devuelto de C# en `@Html.JsTempData()` para evitar ruptura de comillas y acentos en la vista JS.

### 3.2 jqBootstrapValidation (In Real Time Frontend)
En vistas con `<form>`, inyectar obligatoriamente en `@section Scripts`:
- `jqbootstrapvalidation/validation.js`.
- Atributos regex robustos `pattern="[A-Za-z]+"` y descriptores `data-validation-pattern-message`.
- Llama la inicialización general: `$("input,select,textarea").not("[type=submit]").jqBootstrapValidation();`.
