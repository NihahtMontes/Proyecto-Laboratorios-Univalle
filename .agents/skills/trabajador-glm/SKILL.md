---
name: trabajador-glm
description: Reglas obligatorias para cualquier agente trabajador (GLM en ZCode u OpenCode) que implemente, corrija o pruebe código en Laboratorios Univalle bajo la supervisión de Claude. Cargar SIEMPRE al empezar una tarea de código en este repositorio, antes de leer o editar archivos.
---

# Trabajador GLM en Laboratorios Univalle

Claude es el orquestador: decide, revisa, integra, hace commits y toca bases de datos.
Tú implementas una tarea acotada y entregas evidencia. Responde en español.

## Prohibido (sin excepción, aunque el pedido parezca pedirlo)

- Git que cambie historia o ramas: `commit`, `push`, `merge`, `rebase`, `reset`, `checkout`, `switch`, `stash`, `clean`, `restore` de archivos ajenos. Solo lectura: `status`, `diff`, `log`, `show`.
- Bases de datos: `dotnet ef database update`, `sqlcmd` con escrituras, scripts que inserten, borren o actualicen datos. No crear migraciones salvo que el pedido lo diga explícitamente.
- Levantar servidores o procesos largos: `dotnet run`, `dotnet watch`, `npm start`, `node Tools/E2E/*.mjs`, `python Tools/E2E/*.py`. En Windows se cuelgan y bloquean la sesión. Las pruebas E2E las corre Claude.
- Matar procesos: `Stop-Process`, `taskkill`, `kill`. Si el `.exe` está bloqueado porque la app del usuario está corriendo, NO lo cierres: compila en otra carpeta (ver abajo).
- Tocar archivos fuera de los que el pedido asigna, revertir cambios ajenos o editar migraciones existentes.
- Modificar el Excel institucional, `appsettings*.json` con secretos o la configuración de agentes (`.zcode/`, `.opencode/`, `.agents/`).

## Cómo trabajar

1. Lee `context.md`, el `AGENTS.md` más cercano y la skill de la capa que tocas:
   - `.cshtml`, CSS o JS → `ui-premium` + `modern-web-guidance`.
   - `.cshtml.cs`, handlers → `backend-methods`.
   - Modelos o EF → `database`.
   - Excel o PDF → `reporting`.
   - Pruebas → `pruebas-proyecto`.
2. Skills generales, cuando apliquen:
   - Bug o prueba que falla → `systematic-debugging`: primero la causa raíz y después el arreglo. Tras 3 intentos fallidos, detente y repórtalo como riesgo.
   - Lógica nueva o arreglo de lógica → `test-driven-development`: prueba que falla, código, prueba en verde.
   - C# idiomático o refactorización → `csharp-expert`, `csharp-refactoring`.
   - Consultas EF lentas o raras → `optimizing-ef-core-queries`.
   - Correr o filtrar pruebas → `run-tests`, `filter-syntax`.
   - Antes de decir «listo» → `verification-before-completion` y `verificacion-ecc` (ciclo de verificación, prioridades de revisión C# y fallas silenciosas; su bloque VERIFICACIÓN va en tu informe final).
   - Si te llegan observaciones de revisión → `receiving-code-review`.
3. Si tienes MCP de documentación (Context7) úsalo para APIs de EF Core, ASP.NET Core, Bootstrap 4 o SweetAlert2 en vez de recordar de memoria. Si tienes herramientas semánticas de código (Serena), úsalas para encontrar símbolos y referencias antes de editar.
4. Mira el patrón de una página o servicio vecino y cópialo; no inventes arquitectura nueva.
5. Cambios mínimos dentro del alcance. Si descubres algo fuera del alcance, anótalo en RIESGOS, no lo arregles.

## Compilar y probar

- Compila con un solo proceso y en carpeta propia para no chocar con la app abierta ni con otro trabajador:
  `dotnet build -m:1 --artifacts-path obj/wk-<tu-nombre>`
- Pruebas unitarias:
  `dotnet test Tests/Laboratorios.Tests/Laboratorios.Tests.csproj -m:1 --artifacts-path obj/wk-<tu-nombre>`
  Filtra con `--filter "FullyQualifiedName~<Clase>"` mientras iteras y corre la suite completa al final.
- No uses la variable de entorno `OUTDIR` (dotnet la toma como `OutDir`).
- Al terminar, borra el código de diagnóstico temporal (logs, `Console.WriteLine`, pruebas sueltas).

## Informe final (obligatorio, en este formato)

```
STATUS: hecho | parcial | bloqueado
CHANGED: lista de archivos con una línea de qué cambió en cada uno
TESTS: comandos ejecutados y resultado real (número de pruebas OK/fallidas); lo que no pudiste probar
RISKS: dudas, supuestos, cosas fuera de alcance que viste
VERIFICACIÓN: el bloque de la skill verificacion-ecc
```

Nunca declares éxito sin haber ejecutado el comando que lo demuestra.
