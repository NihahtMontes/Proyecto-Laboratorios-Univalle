---
name: ui_premium
description: UI Razor/NiceAdmin WrapPixel Bootstrap 4, layouts, tabs, cards, SweetAlert2 v7, formularios y tablas.
trigger: Modificacion de `.cshtml`, JavaScript embebido, layout, sidebar, cards, tabs, botones, modales, wizard visual o feedback al usuario.
scope: Pages/UI/wwwroot
context: .agent/context/areas/ui.md
---
# Skill UI Premium

## 1. Contexto Del Modulo

La UI del proyecto vive principalmente en `Pages/` con Razor Pages y componentes compartidos en `Pages/Shared/`. El frontend usa NiceAdmin/WrapPixel sobre Bootstrap 4, jQuery, Select2 y SweetAlert2 v7.19.3.

Flujo general: el usuario navega desde `_Sidebar.cshtml` o dashboards, el PageModel prepara datos, Razor pinta formularios/tablas/cards y JavaScript local aplica confirmaciones, cascadas de selects, filtros o validaciones.

## 2. Arquitectura Y Archivos Clave

- Layout base: `Pages/Shared/_Layout.cshtml`.
- Sidebar: `Pages/Shared/_Sidebar.cshtml`.
- Validaciones: `Pages/_ValidationScriptsPartial.cshtml`.
- Dashboard/wizard: `Pages/Index.cshtml`, `Pages/Shared/_WizardStep.cshtml`, `Pages/Shared/_WizardSteps.cshtml`.
- Inventario visual actual: `Pages/AssetView/Index.cshtml`, `EquipmentClassifications.cshtml`, `Units.cshtml`, `UtensilClassifications.cshtml`, `UtensilUnits.cshtml`, `OtherUnits.cshtml`.
- CRUD activos: `Pages/Equipment/*.cshtml`, `Pages/EquipmentUnits/*.cshtml`.
- Assets: `wwwroot/dist/**`, `wwwroot/assets/**`, `wwwroot/js/site.js`, `wwwroot/css/site.css`.

## 3. Integracion Con NiceAdmin

- Usar WrapPixel/NiceAdmin Bootstrap 4 real del proyecto: `wwwroot/dist/css/style.min.css`, `wwwroot/dist/js/app.min.js`, `wwwroot/dist/js/sidebarmenu.js`, `wwwroot/dist/js/waves.js`.
- Iconos disponibles: Material Design Icons (`mdi`), Font Awesome (`fas`) y Themify (`ti`). No asumir BootstrapMade, bootstrap-icons, boxicons o remixicon.
- Cards de color: `card text-white bg-primary/bg-success/bg-warning/bg-info/bg-danger/bg-secondary`.
- Tablas: `table table-hover v-middle`, encabezados `bg-light`, acciones con `btn-outline-info`, `btn-outline-warning`, `btn-outline-danger`.
- Botones: principal `btn-info btn-rounded shadow-sm`, editar `btn-warning`, peligro `btn-danger`, volver/cancelar `btn-outline-secondary`.
- Tabs: `nav nav-tabs customtab`; si una pagina debe abrir un tab especifico, usar query string como `activeTab=Inventario` y activar clases Razor desde el PageModel.
- SweetAlert2 es v7. Usar:

```javascript
Swal.fire({...}).then(function (result) {
    if (result && (result.isConfirmed === true || result.value === true)) {
        HTMLFormElement.prototype.submit.call(form);
    }
});
```

## 4. Patrones Y Convenciones

- UI visible en espanol.
- Create normal: sin confirmacion previa; exito posterior via `TempData.Success()`.
- Edit: confirmar antes de guardar.
- Delete/soft-delete: confirmar antes de enviar; nunca usar `window.confirm`.
- Hidden inputs dentro de `<td>` cuando estan en tablas.
- No inyectar strings C# no confiables en JavaScript sin serializacion segura.
- En activos, la taxonomia actual local es:
  - `Equipment`: clasificaciones tecnicas controladas.
  - `Utensil`: subclasificaciones controladas.
  - `Other`: listado directo sin subclasificacion.

## 5. Contexto Para Agente

- Antes de tocar UI, leer `context.md`, `.agent/context/areas/ui.md`, `Pages/AGENTS.md` y esta skill.
- No cambiar stack visual ni meter nuevas librerias sin aprobacion.
- No usar patrones BootstrapMade; este proyecto usa WrapPixel/NiceAdmin Bootstrap 4.
- En `AssetView`, los botones Volver deben conservar contexto de Inventario; no depender solo de `history.back()`.
- Si se edita una accion critica, preservar fallback si `Swal` no esta disponible.
