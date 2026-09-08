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

## Forma de trabajo

1. Inspecciona la vista, su PageModel y una pagina vecina que represente el patron vigente.
2. Separa el cambio visual de cualquier cambio de datos. Si hace falta modificar handlers, carga tambien `backend-methods` y coordina ownership.
3. Reutiliza clases y componentes actuales; limita CSS o JavaScript global nuevo.
4. Comprueba estados normal, vacio, error y confirmacion cuando apliquen.
5. Ejecuta `dotnet build` si el cambio afecta Razor o codigo embebido y revisa el diff final.

Reporta archivos modificados, comportamiento comprobado y cualquier validacion visual que no pudo ejecutarse.
