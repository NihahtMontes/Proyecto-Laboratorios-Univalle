# Web moderna: guía para que el código no quede obsoleto

Desde el 2026-11-16 el proyecto usa **Modern Web Guidance**: un conjunto de guías de buenas prácticas
web de los equipos de Google Chrome, Microsoft Edge, Mozilla Firefox y MDN, pensado para agentes de código
(Claude Code, Codex, OpenCode). Los modelos suelen escribir HTML, CSS y JS a la antigua, porque
aprendieron de mucho código viejo. Esta skill les da la forma moderna antes de escribir.

- Documentación oficial: https://developer.chrome.com/docs/modern-web-guidance
- Código: https://github.com/GoogleChrome/modern-web-guidance-src (licencia Apache-2.0)

## Qué quedó instalado

| Ruta | Para qué |
|---|---|
| `.agents/skills/modern-web-guidance/` | Skill completa (SKILL.md + guías). La leen Codex y OpenCode. |
| `.claude/skills/modern-web-guidance/SKILL.md` | Copia del SKILL.md para Claude Code. No necesita las guías: las pide con `npx`. |
| `skills-lock.json` | Versión exacta instalada (commit del repositorio de origen), para reinstalar igual en otra PC. |

La skill no agrega dependencias a la aplicación ni a `wwwroot`: solo cambia cómo trabajan los agentes.

## Regla de uso (está también en `AGENTS.md`)

Antes de escribir o cambiar **HTML, CSS o JavaScript de interfaz**:

```sh
# 1. Buscar la práctica (2-5 palabras en inglés, una capacidad por búsqueda)
npx -y modern-web-guidance@latest search "confirmation modal dialog"

# 2. Leer la guía elegida (se pueden pasar varios ids separados por comas)
npx -y modern-web-guidance@latest retrieve "<id>"
```

Ejemplos de búsquedas útiles en este proyecto: `"responsive data table"`, `"form validation messages"`,
`"accessible tabs"`, `"lazy load chart"`, `"dark mode colors"`, `"dates and timezones"`, `"sanitize html"`.

No se usa para backend (C#, EF Core, SQL), migraciones, scripts de Python ni pruebas.

## Cómo convive con Bootstrap 4, NiceAdmin y SweetAlert2 v7

Las invariantes del proyecto siguen mandando (ver `.agents/skills/ui-premium/SKILL.md`):

- **Se moderniza lo que se toca.** El código nuevo y el código que la tarea modifica siguen la guía moderna.
  No se reescriben librerías (Bootstrap 4, jQuery, SweetAlert2 v7, FullCalendar v3) ni pantallas ajenas a la tarea.
- **No se cambia de librería visual sin aprobación.** Si la guía recomienda algo nativo (por ejemplo
  `<dialog>`, `popover`, container queries, `:has()`, `text-wrap: balance`), se usa en componentes nuevos
  siempre que conviva con Bootstrap 4 y lo soporten Chrome y Edge actuales, que son los navegadores de la universidad.
- **Las confirmaciones siguen siendo SweetAlert2 v7** (`result.value`), porque es la convención del sistema.
- **Fechas:** la regla de hora de Bolivia (`Helpers/BoliviaTime`) sigue siendo la fuente en el servidor.

## Revisar código heredado

```sh
npx -y modern-web-guidance@latest legacy-scan wwwroot
npx -y modern-web-guidance@latest legacy-scan Pages
```

Lista los patrones antiguos encontrados. Sirve para planificar mejoras por partes, no para cambiarlo todo de golpe.

## Mantenerla al día

- Las guías se consultan siempre con `@latest`, así que el contenido está al día sin hacer nada.
- Para actualizar el SKILL.md instalado: `npx -y modern-web-guidance@latest update` y después volver a copiar
  `.agents/skills/modern-web-guidance/SKILL.md` a `.claude/skills/modern-web-guidance/SKILL.md`.
- En otra PC (Fernando, Danilo): basta con el repositorio. Si falta algo, reinstalar con
  `npx modern-web-guidance@latest install --agent claude-code codex opencode` desde la raíz del proyecto
  (pide Node.js 22 o superior). Si el instalador crea `.claude/skills/modern-web-guidance` como enlace simbólico,
  reemplazarlo por la copia del SKILL.md: Git en Windows no guarda bien los enlaces simbólicos.

## Privacidad

La herramienta envía a Google datos anónimos de uso. Para desactivarlo, definir la variable de entorno
`DISABLE_TELEMETRY=1` en el perfil de la terminal. Detalle:
https://github.com/GoogleChrome/modern-web-guidance#-telemetry--privacy
