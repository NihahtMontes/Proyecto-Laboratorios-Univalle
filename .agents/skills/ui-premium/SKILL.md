---
name: ui-premium
description: Implementa o revisa UI Razor del proyecto Laboratorios Univalle con NiceAdmin/WrapPixel, Bootstrap 4, SweetAlert2 v7, formularios, tablas, cards y navegacion. Usar al modificar `.cshtml`, JavaScript de interfaz, layouts o feedback visible; no usar para PageModels sin cambios de UI.
---

# UI Premium de Laboratorios Univalle

Lee `context.md` y el `Pages/AGENTS.md` aplicable antes de editar.

## Invariantes

- Conserva NiceAdmin/WrapPixel y Bootstrap 4. No introduzcas BootstrapMade, Bootstrap 5 ni otra libreria visual sin aprobacion.
- Usa los assets, componentes e iconos ya presentes antes de crear variantes nuevas.
- Mantiene textos visibles en espanol y una jerarquia visual clara; evita formularios planos cuando cards, secciones o contexto lateral mejoren la comprension.
- SweetAlert2 es v7. Una confirmacion valida debe aceptar `result.isConfirmed === true` o `result.value === true`.
- En submits criticos confirmados usa `HTMLFormElement.prototype.submit.call(form)` para evitar colisiones con controles llamados `submit`.
- Create normal muestra exito despues de guardar. Edit y Delete/soft-delete confirman antes de enviar.
- No uses `window.confirm` ni insertes texto C# no confiable en JavaScript sin serializacion segura.
- Preserva `ManagementId`, filtros, query strings y el tab activo cuando forman parte del flujo.

## Rarezas de NiceAdmin que rompen trabajo nuevo

- El punto de corte `xl` de este tema está en **1600px**, no en 1200px. Una columna `col-xl-*` en una laptop de 1366 px se comporta como `lg`. Prueba el diseño con 1366 y con 1920 de ancho.
- Iconos: **Font Awesome 5.0.9** (`fa`, `fas`, `far`, `fab`; faltan muchos iconos de versiones posteriores) y **Material Design Icons 1.8.36** (`mdi mdi-*`). Antes de usar un icono, comprueba que su clase exista:
  - `grep -c "mdi-<nombre>:before" wwwroot/dist/css/icons/material-design-iconic-font/css/materialdesignicons.min.css`
  - `grep -rc "fa-<nombre>:before" wwwroot/dist/css/icons/font-awesome/css`
  Un icono inexistente se ve como un cuadro vacío.
- Paneles con «Resumen + pestañas» (Inicio y portal docente): reutiliza `wwwroot/css/panel-home.css` y `wwwroot/js/panel-tabs.js` (prefijo de clases `ph-`, `PanelTabs.whenShown`) en vez de crear otro sistema de pestañas.
- Razor codifica los acentos de las cadenas C# (`é` → `&#xE9;`): en vistas, escribe los textos con acento directamente en el `.cshtml`.
- Los nombres visibles van sin códigos L-x; usa los nombres claros de `Helpers/UiTerms.cs`.

## Forma de trabajo

1. Inspecciona la vista, su PageModel y una pagina vecina que represente el patron vigente.
   Antes de escribir HTML, CSS o JS, busca la practica moderna con la skill `modern-web-guidance`
   (`npx -y modern-web-guidance@latest search "<capacidad en ingles>"` y luego `retrieve`). Aplica la
   recomendacion dentro de las invariantes de arriba (Bootstrap 4, SweetAlert2 v7); ver `docs/WEB_MODERNA.md`.
2. Separa el cambio visual de cualquier cambio de datos. Si hace falta modificar handlers, carga tambien `backend-methods` y coordina ownership.
3. Reutiliza clases y componentes actuales; limita CSS o JavaScript global nuevo.
4. Comprueba estados normal, vacio, error y confirmacion cuando apliquen.
5. Ejecuta `dotnet build` si el cambio afecta Razor o codigo embebido y revisa el diff final.

Reporta archivos modificados, comportamiento comprobado y cualquier validacion visual que no pudo ejecutarse.
