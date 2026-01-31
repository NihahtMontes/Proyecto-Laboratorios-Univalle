# GUÍA DE ESTANDARIZACIÓN DE MÓDULOS (PROJECT BLUEPRINT)

Esta guía documenta los patrones de diseño, estructura de código y decisiones de UX implementadas en el módulo de **Verificaciones**. Debe utilizarse como referencia ("Gold Standard") para desarrollar o refactorizar cualquier otro módulo del sistema.

---

## 1. PRINCIPIOS DE DISEÑO VISUAL (UI/UX)

### 1.1 "Header Hero" (Index / Dashboard)
Cada módulo principal debe comenzar con un encabezado visualmente impactante que defina el contexto.

*   **Fondo:** Gradiente oscuro o color corporativo profundo (`bg-gradient-dark`, `#1e3c72` -> `#2a5298`).
*   **Contenido:**
    *   Icono grande decorativo (fondo abstracto, baja opacidad).
    *   Título claro e icono semántico.
    *   Subtítulo descriptivo.
    *   Botón de Acción Principal ("Crear Nuevo") flotando a la derecha.

```html
<!-- Ejemplo Estructura Header -->
<div class="card bg-gradient-dark ...">
    <!-- Decoración fondo -->
    <i class="fas fa-icon ... opacity-05"></i>
    <div class="row">
       <h1>Título</h1>
       <a href="Create" class="btn btn-light btn-pill ...">Nueva Acción</a>
    </div>
</div>
```

### 1.2 Layout Asimétrico (Forms & Details)
Para vistas de **Edición (Edit)**, **Creación (Create)** y **Detalles (Details)**, abandonar la columna única centrada. Usar un layout **8/4**:

*   **Columna Izquierda (66% - col-lg-8):** Contenido denso, formularios largos, checklists, tablas de ítems.
*   **Columna Derecha (33% - col-lg-4):** Panel de Control (Sticky se recomienda).
    *   Resumen del objeto (Nombre, ID, Inventario).
    *   Inputs de Fecha/Responsable.
    *   Campos de decisión (Estado, Notas Finales).
    *   **Botones de Acción:** Guardar/Cancelar siempre visibles aquí.

### 1.3 Micro-Componentes "Soft"
Evitar colores sólidos agresivos. Usar versiones "Soft" (Pastel) para estados y badges.

*   **Verde:** `bg-soft-success` (Fondo verde pálido, texto verde oscuro).
*   **Rojo:** `bg-soft-danger`.
*   **Amarillo:** `bg-soft-warning`.
*   **CSS Helper Sugerido:**
    ```css
    .bg-soft-success { background-color: #e8f5e9; color: #1b5e20; }
    .bg-soft-danger { background-color: #ffebee; color: #b71c1c; }
    ```

### 1.4 Delete "Clean Card"
La vista de eliminación no debe ser un formulario genérico.
*   **Centrado:** Perfecto horizontal y vertical (`min-height: 80vh`).
*   **Alerta:** Icono grande animado o destacado.
*   **Resumen:** Card interna mostrando QUÉ se va a borrar (Nombre, ID, Datos Clave).
*   **Acciones:** Botones claros: "Cancelar" (Gris) y "Confirmar" (Rojo).

---

## 2. PATRONES DE CÓDIGO (RAZOR/C#)

### 2.1 Renderizado de Listas Repetitivas (Helpers)
En formularios con muchos campos idénticos (ej. Checklists), **NO** usar `@functions` inline complejas ni bucles `foreach` sobre datos no estructurados si se requiere control preciso.

*   **Recomendación:** Expandir el HTML explícitamente si son pocos ítems (< 20) para total control de `asp-for` y validación.
*   **Alternativa:** Crear un `PartialView` específico si la lógica es muy compleja.
*   **Anti-Patrón:** Usar helpers que devuelvan `Task` asíncronas dentro de Razor sin `await` (produce texto `System.Threading.Tasks...`).

### 2.2 Validación de Enum en Frontend
Mapear Enums de C# a clases CSS visuales instantáneas usando JavaScript/jQuery.

```javascript
// Patrón de coloreado dinámico de Selects
$(".verifier").on('change', function() {
    var val = $(this).val();
    $(this).removeClass("text-success text-danger");
    if(val == "1") $(this).addClass("text-success"); // Good
    if(val == "2") $(this).addClass("text-danger");  // Bad
});
```

### 2.3 Manejo de Badges Asíncronos
Usar `switch expressions` en Razor para lógica de visualización de estados limpia.

```csharp
var (clase, icono) = Model.Status switch {
    Status.Active => ("badge-soft-success", "fa-check"),
    Status.Inactive => ("badge-soft-danger", "fa-times"),
    _ => ("badge-light", "fa-question")
};
```

---

## 3. ESTRUCTURA DE ARCHIVOS

### 3.1 Agrupación Lógica
Al crear formularios largos, usar **Cards** para agrupar campos relacionados lógicamente (ej. "Sistema Eléctrico", "Higiene").

*   Cada Card debe tener un Header visualmente distintivo (Icono + Texto).
*   Esto reduce la carga cognitiva del usuario.

### 3.2 Navegación
*   **Breadcrumbs implícitos:** Botón "Volver al Listado" siempre presente.
*   **Tooltips:** Usar tooltips en botones de acción de tablas (`data-toggle="tooltip"`).

---

## 4. LISTA DE VERIFICACIÓN (CHECKLIST) PARA NUEVOS MÓDULOS

1.  [ ] **Index:** ¿Tiene Header Premium con gradiente? ¿La tabla usa badges soft?
2.  [ ] **Create/Edit:** ¿Usa el layout 8/4? ¿Están los botones a la derecha?
3.  [ ] **Details:** ¿Es visualmente rico (iconos, colores) o solo texto plano?
4.  [ ] **Delete:** ¿Es una tarjeta centrada con advertencia clara?
5.  [ ] **UX General:** ¿Hay feedback inmediato (colores, validación) al interactuar?

---
**Generado por:** Agente Antigravity - Enero 2026
**Contexto:** Proyecto Laboratorios Univalle
