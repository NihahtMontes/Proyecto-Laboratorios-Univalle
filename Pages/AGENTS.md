# 🖥️ AGENTS.md (Área de UI y Vistas Razor)
**Ámbito Funcional**: Interfaz de Usuario, Vistas, PageModels

## 1. Contexto Cultural de esta Capa
Las vistas en `Pages/` son el puente de interacción. El proyecto emplea *NiceAdmin*, por ende, cualquier formulario soso será rechazado de inmediato. **Estética Asimétrica y Alertas Atractivas** (SweetAlert2) son tu meta obligada.

## 2. Habilidades Requeridas (Skills)
Cuando detectes que la tarea implica:
- Edición de un archivo `.cshtml`.
- Edición de un `PageModel` interno (clase `InputModel`).
- Lógica de JavaScript incrustado de validaciones...

👉 **DEBES Auto-Invocar la Skill**: `ui_premium` y `backend_methods`.
- Ruta: `.agent/skills/ui_premium/SKILL.md`
- Herramientas: *Sólo lectura y escritura sobre el UI*.

## 3. Prácticas que debes Vigilar
- **Sobreculturización Razor**: NUNCA utilices etiquetas HTML puras sin usar Tag Helpers donde correspondan (ej: `asp-for`, `asp-route-id`, `asp-validation-for`).
- **Seguridad Javascript**: Si pasas un mensaje de éxito del Server a la Vista, tu string JS SIEMPRE debe envolverse en `@Html.JsTempData()` para anular inyecciones XSS o acentos rotos.
- **Layouts Requeridos**: Index = "*Smart Index*" + Opcional *Dual-Tab*. Create/Edit = *Card 8/4 Asimétrico*.
