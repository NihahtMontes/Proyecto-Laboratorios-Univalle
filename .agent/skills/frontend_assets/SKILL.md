---
name: frontend_assets
description: wwwroot, NiceAdmin/WrapPixel Bootstrap 4, CSS/JS globales, librerias frontend, templates y uploads.
trigger: Cambios en `wwwroot/`, assets NiceAdmin, CSS, JS, librerias, templates Excel, imagenes o uploads.
scope: wwwroot/FrontendAssets
context: .agent/context/areas/ui.md
---
# Skill Frontend Assets

## 1. Contexto Del Modulo

`wwwroot` contiene los assets servidos al navegador: estilos WrapPixel/NiceAdmin, JavaScript global, librerias, plantillas institucionales, imagenes subidas y recursos estaticos.

Flujo: `_Layout.cshtml` referencia CSS/JS -> navegador carga assets -> vistas usan clases/componentes -> scripts inicializan interacciones.

## 2. Arquitectura Y Archivos Clave

- WrapPixel/NiceAdmin: `wwwroot/dist/css/*`, `wwwroot/dist/js/*`.
- Assets alternos/base: `wwwroot/assets/css/*`, `wwwroot/assets/js/*`, `wwwroot/assets/scss/*`.
- CSS local activo: `wwwroot/css/site.css`. `wwwroot/css/tailwind-dashboard.css` es legacy no referenciado y excluido de publish.
- JS local: `wwwroot/js/site.js`.
- Librerias: `wwwroot/lib/jquery*`, validation unobtrusive.
- Templates reportes: `wwwroot/templates/**`.
- Uploads activos: `wwwroot/uploads/equipment/**`.

## 3. Integracion Con NiceAdmin

- El proyecto usa WrapPixel/NiceAdmin Bootstrap 4, no BootstrapMade.
- Iconos disponibles por assets locales: `mdi`, `fas`, `ti`.
- Componentes principales: sidebar, topbar, cards, badges, tabs, tablas, botones redondeados y wizard.
- No cambiar `dist`/`assets` minificados manualmente salvo que sea estrictamente necesario.
- Preferir CSS puntual de vista o `site.css` para ajustes locales.

## 4. Patrones Y Convenciones

- No agregar CDN si ya existe asset local.
- No eliminar archivos en `uploads`; son datos de usuario/desarrollo.
- Templates Excel son contrato de reportes; tratarlos como assets funcionales, no decorativos.
- Si se agregan imagenes o templates, usar nombres claros y rutas relativas a `wwwroot`.

## 5. Contexto Para Agente

- Antes de tocar assets, revisar `_Layout.cshtml` para saber que version se carga.
- Verificar que clases nuevas existan en Bootstrap 4/WrapPixel.
- No asumir Bootstrap 5.
- Cuidar cache/minificados; si se cambia JS global, probar que no rompe sidebar, waves, SweetAlert o selectores.
