# Arquitectura operativa Gentle AI

> **Documento historico.** Conserva el diagnostico y la propuesta observados el 18 de agosto de 2026. Para la configuracion y operacion vigente de OpenCode en este repositorio, usar `opencode.json`, `.opencode/agents/` y `docs/AGENT_WORKFLOW.md`. Ante discrepancias, prevalecen el codigo, los contratos y la evidencia actual.

Esta guía define cómo usar Gentle AI, OpenCode, SDD, Engram y CodeGraph alrededor de Proyecto Laboratorios Univalle. Gentle AI no forma parte del runtime ASP.NET: es la capa de ingeniería que organiza contexto, agentes, especificaciones, implementación, revisión y memoria.

> Estado observado: 18 de agosto de 2026. La sección [Estado actual y brechas](#estado-actual-y-brechas) describe problemas reales de esta instalación; no representa el diseño objetivo.

## Ruta rápida

1. Verificar herramientas y MCP con los comandos de esta guía.
2. Resolver la eliminación local de `.agent/` antes de modificar código.
3. Rotar y retirar del repositorio cualquier credencial real.
4. Elegir una sola fuente de verdad para perfiles, Engram y artefactos SDD.
5. Inicializar CodeGraph para este proyecto con autorización explícita.
6. Ejecutar cambios pequeños directamente y cambios sustanciales mediante SDD.
7. Verificar build, comportamiento, revisiones y memoria antes de cerrar una sesión.

## Qué problema resuelve

Un asistente sin arquitectura recibe un prompt, busca archivos y modifica código. Ese flujo es rápido, pero no conserva decisiones, no separa responsabilidades y puede ignorar reglas críticas del dominio.

Esta arquitectura agrega contratos explícitos:

- `AGENTS.md` define cómo debe trabajar el agente.
- `context.md` y `.agent/context/` entregan solo el contexto necesario.
- Skills y agentes especializados indican cómo ejecutar cada tipo de tarea.
- SDD acuerda problema, comportamiento y diseño antes de implementar cambios sustanciales.
- CodeGraph permite comprender símbolos, llamadas e impacto antes de editar.
- Context7 entrega documentación vigente de librerías externas.
- Engram conserva decisiones, descubrimientos y continuidad entre sesiones.
- Las revisiones 4R y Judgment Day buscan riesgos antes de integrar.

El objetivo no es usar más agentes. El objetivo es que cada cambio tenga contexto suficiente, un propietario claro, evidencia verificable y una memoria útil.

## Arquitectura por capas

```text
Persona desarrolladora
        |
        v
OpenCode: conversación, permisos y ejecución de herramientas
        |
        v
Gentle Orchestrator: clasifica, enruta y coordina
        |
        +----------------------+-----------------------+
        |                      |                       |
        v                      v                       v
Contrato del proyecto      Conocimiento            Persistencia
AGENTS.md                  CodeGraph               Engram
context.md                 Context7                OpenSpec
.agent/context             código fuente           Git
.agent/skills
        |
        v
SDD cuando el cambio lo requiere
explore -> propose -> spec -> design -> tasks -> apply -> verify -> archive
        |
        v
Agentes de implementación y revisión
UI | Backend/Data | Risk | Readability | Reliability | Resilience
        |
        v
Build, pruebas, revisión humana y commit autorizado
```

La aplicación mantiene su propia arquitectura:

```text
Razor Pages y Controllers
        |
        v
Services
        |
        v
ApplicationDbContext / EF Core
        |
        v
SQL Server
```

Ambos planos se relacionan, pero no deben mezclarse. Gentle AI gobierna el proceso de desarrollo; ASP.NET Core ejecuta el producto.

## Componentes

| Componente | Responsabilidad | No debe convertirse en |
|---|---|---|
| Gentle AI | Instalar y sincronizar agentes, skills, prompts, políticas y componentes | Runtime de la aplicación |
| OpenCode | Ejecutar agentes, herramientas, comandos, plugins y MCP | Fuente canónica de reglas del dominio |
| Orquestador | Clasificar alcance, cargar contexto, delegar sin solapamientos e integrar | Implementador universal de todas las capas |
| Skills | Contratos ejecutables para una tarea concreta | Documentación genérica o duplicada |
| SDD | Convertir una necesidad sustancial en propuesta, specs, diseño, tareas y evidencia | Ceremonia obligatoria para cambios triviales |
| OpenSpec | Persistir artefactos SDD versionables | Caché local |
| Engram | Mantener memoria entre sesiones y artefactos semánticos | Sustituto de Git o de la documentación canónica |
| CodeGraph | Consultar símbolos, llamadas, dependencias e impacto | Fuente de negocio o índice versionado obligatorio |
| Context7 | Consultar documentación vigente de librerías | Herramienta para analizar lógica propia del negocio |
| Revisión 4R | Revisar riesgo, legibilidad, confiabilidad y resiliencia | Reemplazo de pruebas y revisión humana |
| Judgment Day | Ejecutar revisión adversarial dual para cambios de alto riesgo | Paso diario para cualquier ajuste menor |
| GGA/perfiles | Seleccionar proveedores y modelos por rol | Lugar para guardar credenciales |
| Gentleman.Dots/Herdr | Mejorar entorno y coordinación de terminales/agentes | Dependencia del proyecto ASP.NET |

## Fuentes de verdad

La arquitectura solo es reproducible si cada dato tiene un dueño.

| Tipo | Fuente recomendada | Tratamiento |
|---|---|---|
| Código y reglas de dominio | Repositorio Git | Versionado y revisado |
| Política global de trabajo | `AGENTS.md` | Versionada |
| Ruteo de contexto | `context.md` y `.agent/context/` | Versionado |
| Skills específicas del proyecto | `.agent/skills/` | Versionado |
| Configuración declarativa de la plataforma | Manifiesto bajo `~/ai-stack/` | Versionado, sin secretos |
| Configuración live de OpenCode | `~/.config/opencode/` | Generada o sincronizada; no es el diseño canónico |
| Estado de instalación Gentle AI | `~/.gentle-ai/state.json` | Runtime local |
| Índice de skills | `.atl/` | Derivado y regenerable |
| Índice de código | `.codegraph/` | Derivado y regenerable |
| Artefactos SDD compartidos | `openspec/` | Versionados cuando el modo es OpenSpec o híbrido |
| Memoria semántica | Engram | Persistente, con exportación explícita si debe compartirse |
| Credenciales | User Secrets, variables de entorno o gestor de secretos | Nunca versionadas |

### Regla de precedencia del proyecto

Para una tarea en este repositorio, el orden esperado es:

1. `AGENTS.md`.
2. `context.md`.
3. `.agent/context/00-router.md`.
4. `.agent/context/01-global-rules.md`.
5. Contexto del área y del módulo afectado.
6. `AGENTS.md` local de `Pages/`, `Models/` o `Services/`.
7. Skill específica de `.agent/skills/`.
8. Código y pruebas actuales.

La memoria ayuda a recuperar antecedentes, pero una memoria antigua no prevalece sobre el código y los contratos vigentes.

## Ruteo de este proyecto

| Trabajo | Contexto y contrato | Skill esperada |
|---|---|---|
| Razor, JavaScript y SweetAlert2 | `Pages/AGENTS.md`, contexto UI | `ui_premium` |
| PageModels y handlers | `Pages/AGENTS.md`, contexto backend | `backend_methods` |
| EF Core, modelos y migraciones | `Models/AGENTS.md`, contexto database | `database` |
| Excel y PDF | `Services/AGENTS.md`, contexto reporting | `reporting` |
| Controllers | Contexto backend | `api_controllers` |
| Servicios de aplicación | Contexto backend | `services_core` |
| Assets frontend | Contexto UI | `frontend_assets` |
| Inventario | Contextos UI, backend y datos | `asset_inventory` |
| Build y despliegue | `AGENTS_SETUP.md`, contexto infrastructure | Según archivos afectados |

Los módulos Wizard, Dashboard, Borradores, Correctivo, L-6, L-3, Kardex/L-48 y L-12 tienen contexto propio bajo `.agent/context/modules/` cuando ese directorio está disponible.

## Reglas de dominio que los agentes deben preservar

- No usar hard-delete para entidades de negocio.
- No modificar migraciones existentes; crear migraciones nuevas.
- Cargar con `.AsTracking()` o `FindAsync()` toda entidad que se vaya a modificar porque EF Core usa NoTracking global.
- Preservar `ManagementId` en rutas, handlers y redirects del wizard.
- Tratar Completados como fase visual `Step = 7`, no como enum nuevo.
- Mantener compatibilidad con SweetAlert2 v7 y comprobar `result.value`.
- Usar los helpers `TempData.Success/Error/Warning/Info`.
- Confirmar Edit y Delete antes de enviar; mostrar éxito de Create después de persistir.
- No usar `Person.FullName` dentro de LINQ porque es `[NotMapped]`.
- No agregar dependencias, cambiar stack, hacer commit o modificar datos productivos sin aprobación explícita.

## Flujo operativo

### 1. Preparar contexto

1. Detectar la raíz Git y revisar el working tree.
2. Consultar memoria reciente cuando la tarea pueda tener antecedentes.
3. Cargar el contrato raíz, el área, el módulo y la skill correspondiente.
4. Consultar CodeGraph antes de búsquedas amplias de arquitectura o impacto.
5. Usar Context7 si la respuesta depende de una API o librería externa actual.

### 2. Clasificar el cambio

Usar ejecución directa cuando el cambio sea pequeño, claro, reversible y no altere contratos relevantes.

Usar SDD cuando exista al menos una de estas condiciones:

- Nueva capacidad o cambio de comportamiento observable.
- Decisión arquitectónica.
- Cambio de datos, seguridad, permisos o flujo del wizard.
- Varias capas o más de cinco archivos afectados.
- Requisito ambiguo que necesite criterios de aceptación.
- Riesgo suficiente para requerir revisión o trazabilidad formal.

### 3. Diseñar antes de editar

Para cambios sustanciales:

```text
explore  = comprender estado, restricciones y riesgos
propose  = declarar problema, objetivo, alcance y capacidades
spec     = definir comportamiento observable y escenarios
design   = decidir implementación, alternativas y tradeoffs
tasks    = dividir trabajo en unidades verificables
apply    = implementar de acuerdo con specs y diseño
verify   = demostrar cumplimiento con comandos y evidencia
archive  = consolidar especificaciones y cerrar el cambio
```

Los artefactos responden preguntas distintas:

| Artefacto | Pregunta |
|---|---|
| `proposal.md` | ¿Por qué se hará y cuál es el alcance? |
| `spec.md` | ¿Qué comportamiento debe observarse? |
| `design.md` | ¿Cómo se implementará y por qué? |
| `tasks.md` | ¿Qué unidades concretas deben completarse? |
| Informe de verificación | ¿Qué evidencia demuestra el resultado? |

### 4. Delegar por propiedad

- El orquestador conserva alcance, reglas de datos, integración y verificación final.
- El agente UI es propietario de `.cshtml`, JavaScript, SweetAlert2, botones, textos y consistencia NiceAdmin/Bootstrap 4.
- El agente Backend/Data es propietario de `.cshtml.cs`, InputModels, EF tracking, soft-delete, redirects, TempData y validación servidor.
- Dos agentes no deben editar simultáneamente el mismo archivo.
- La delegación se justifica por especialización o paralelismo real; no por cantidad de agentes disponibles.

### 5. Verificar

La verificación mínima debe cubrir:

1. Diff limitado al alcance acordado.
2. Build o prueba ejecutada, no asumida.
3. Escenarios de las specs trazados a evidencia.
4. Reglas de datos, seguridad y navegación revisadas.
5. Revisión 4R o Judgment Day cuando el riesgo lo justifique.
6. Estado del working tree separado entre cambios propios y cambios ajenos.

### 6. Cerrar

1. Registrar en Engram decisiones, bugs y descubrimientos no obvios.
2. Guardar resumen de sesión con archivos relevantes y próximos pasos.
3. Archivar el cambio SDD cuando todas las tareas estén completas.
4. Solicitar aprobación antes de crear commits o pull requests.

## Modos de persistencia SDD

| Modo | Uso recomendado | Consideración |
|---|---|---|
| `engram` | Exploración personal o continuidad semántica | No produce artefactos revisables en Git |
| `openspec` | Trabajo de equipo que requiere revisión e historial | Depende de archivos versionados |
| `hybrid` | Cambios relevantes que necesitan revisión y memoria | Ambas escrituras deben completarse |
| `none` | Investigación efímera | El contexto se pierde al terminar |

Para este proyecto se recomienda `hybrid` en cambios sustanciales y ejecución directa en ajustes menores. El token canónico debe ser `hybrid`; una interfaz puede mostrar “Both”, pero debe normalizarlo antes de ejecutar fases.

## Requisitos

### Producto

- Git.
- .NET SDK 9.
- SQL Server Developer Edition o acceso controlado al servidor autorizado.
- Herramienta `dotnet-ef` compatible con EF Core 9.
- Configuración segura de `ConnectionStrings:DefaultConnection`.

### Plataforma de agentes

- OpenCode.
- Gentle AI.
- Engram y su MCP.
- CodeGraph y su MCP.
- Acceso al MCP remoto Context7.
- Go 1.25.10 solo si se compila Gentle AI desde su fuente actual.
- GGA, Gentleman.Dots y Herdr son complementarios, no requisitos del producto.

### Inventario observado

| Elemento | Estado al 18 de agosto de 2026 |
|---|---|
| OpenCode | `1.17.16` |
| Gentle AI | `1.43.3` |
| Engram | `1.18.0`; existe una actualización a `1.20.0` |
| CodeGraph | `1.2.0` |
| MCP | Context7, CodeGraph y Engram conectados |
| .NET SDK | No disponible en la shell WSL actual |

### Verificación inicial

```bash
git status --short
dotnet --info
opencode --version
gentle-ai --version
engram --version
codegraph --version
opencode mcp list
gentle-ai doctor
engram doctor --json
```

Los comandos deben confirmar binarios disponibles y MCP conectados. Un resultado “healthy” no sustituye la validación de perfiles, plugins, permisos y fuentes de verdad descrita en esta guía.

## Preparación de un clon

### 1. Configurar la aplicación

```bash
dotnet restore
dotnet build "Proyecto Laboratorios Univalle.csproj"
```

Configurar la conexión mediante User Secrets o variables de entorno. Ejemplo de variable de entorno:

```bash
export ConnectionStrings__DefaultConnection="<connection-string>"
```

No colocar la cadena real en documentación, prompts, memoria, logs ni archivos versionados.

### 2. Validar el contrato del proyecto

Confirmar que existan:

```text
AGENTS.md
context.md
.agent/context/00-router.md
.agent/context/01-global-rules.md
.agent/skills/
Pages/AGENTS.md
Models/AGENTS.md
Services/AGENTS.md
```

Si `.agent/` aparece eliminado en `git status`, primero determinar si fue una decisión intencional. Solo si la eliminación fue accidental se debe restaurar desde Git:

```bash
git restore --source=HEAD -- .agent
```

### 3. Inicializar índices locales

El registro `.atl/` es derivado y puede regenerarse mediante el comando de skill registry de Gentle AI.

CodeGraph se inicializa una vez por proyecto, con autorización para crear su índice:

```bash
codegraph init "$(git rev-parse --show-toplevel)"
```

Definir en `.gitignore` si `.codegraph/` será local. El enfoque recomendado es no versionar índices regenerables.

### 4. Elegir persistencia SDD

- Versionar `openspec/` si los artefactos forman parte del contrato del equipo.
- Mantener `.pi/` como estado local salvo que una regla de Gentle AI indique lo contrario.
- No tratar `.atl/` como documentación canónica.
- Exportar memoria Engram solo mediante un flujo explícito y revisado.

### 5. Ejecutar el primer ciclo

```text
/sdd-init
/sdd-new <nombre-del-cambio>
/sdd-status <nombre-del-cambio>
/sdd-continue <nombre-del-cambio>
/sdd-apply <nombre-del-cambio>
/sdd-verify <nombre-del-cambio>
/sdd-archive <nombre-del-cambio>
```

El onboarding SDD puede utilizarse para aprender el ciclo con un cambio real, pequeño y seguro.

## Seguridad

### Límites obligatorios

- No leer ni persistir secretos si no son imprescindibles para la tarea.
- No incluir valores sensibles en prompts enviados a modelos.
- No guardar secretos en Engram, OpenSpec, `.atl`, logs o documentación.
- Mantener `share: disabled` salvo una decisión explícita.
- Aplicar permisos globales que nieguen por defecto `.env`, llaves privadas, SSH y almacenes de credenciales.
- Revisar permisos de lectura, Bash y directorios externos por agente.
- Rotar una credencial expuesta antes de asumir que eliminarla del archivo resolvió el incidente.

### Política de memoria

La captura debe ser explícita y deduplicada. Se recomienda usar Engram MCP como integración principal y retirar plugins antiguos de captura automática después de verificar que no exista una dependencia real.

No deben coexistir sin contrato:

- Captura automática de todos los prompts.
- Captura pasiva de resultados de subagentes.
- Guardado explícito mediante MCP.

## Estado actual y brechas

| Prioridad | Hallazgo | Acción necesaria |
|---|---|---|
| Crítica | `appsettings.json` contiene una credencial SQL en texto plano | Rotar la credencial, retirarla del repositorio y revisar historial Git |
| Alta | Los 50 archivos versionados de `.agent/` aparecen eliminados en este working tree | Determinar si la eliminación fue intencional antes de restaurar o confirmar |
| Alta | No existe `.codegraph/` | Inicializarlo con autorización y definir política de ignore |
| Alta | `openspec/config.yaml` detectó Node.js/TypeScript y Python en lugar de ASP.NET Core 9 | Corregir contexto y comandos de verificación antes de usar SDD |
| Alta | El plugin antiguo `~/.config/opencode/plugins/engram.ts` coexiste con Engram MCP | Elegir una sola integración y migrar sin duplicar capturas |
| Alta | `~/ai-stack` no tiene manifiesto declarativo ni scripts reproducibles | Crear manifiesto, lock de versiones y comandos idempotentes |
| Alta | No está aplicado el overlay global de permisos sensibles | Regenerar o incorporar la política de seguridad validada |
| Alta | La shell WSL actual no encuentra `dotnet` | Instalar .NET SDK 9 o ejecutar restore, build y tests desde un entorno que lo tenga |
| Media | Un perfil OpenCode externo vacío activa una estrategia de perfiles | Elegir un solo propietario de perfiles y validar su schema |
| Media | Plugins TUI están registrados pero no materializados localmente | Verificar instalación, resolución y compatibilidad |
| Media | `dotnet-ef` 10 está declarado para un proyecto EF Core 9 | Alinear la versión mayor antes de ejecutar migraciones |
| Media | No hay proyecto de pruebas ni CI | Añadir build automatizado y una estrategia incremental de pruebas |
| Media | `AGENTS_SETUP.md` describe auto-migración como incondicional | Documentar los flags `Database:AutoMigrate` y `Database:RunSeed` reales |

Las brechas anteriores deben resolverse en cambios separados y revisables. No conviene mezclar seguridad, restauración de `.agent`, OpenSpec y configuración global en un único cambio.

## Arquitectura objetivo reusable

La plataforma global debería tener una fuente declarativa similar a:

```text
~/ai-stack/
├── manifest/
│   ├── stack.yaml
│   ├── profiles/
│   └── policies/
├── locks/
│   └── versions.lock
├── templates/
├── scripts/
│   ├── bootstrap
│   ├── sync
│   ├── verify
│   ├── backup
│   └── restore
├── docs/
└── repos/upstream/
```

El manifiesto debe declarar versiones, componentes, agentes, perfil, MCP, plugins, política de memoria y permisos, pero nunca secretos. `~/.config/opencode/` y `~/.gentle-ai/` deben ser salidas o estado runtime, no la única forma de reconstruir el sistema.

Un verificador reusable debe comprobar:

- Configuración OpenCode válida contra su schema.
- MCP instalados, conectados y con versiones compatibles.
- Plugins registrados y materializados.
- Ausencia de plugins obsoletos.
- Política global de permisos sensibles.
- Agentes y perfiles sin entradas huérfanas.
- Correspondencia entre skills y prompts generados.
- Estrategia única de perfiles y de captura Engram.
- Estado de CodeGraph por proyecto.
- Backup reciente y restaurable.
- Ausencia de rutas personales y secretos en artefactos versionados.
- Idempotencia: una segunda sincronización no debe producir cambios.

## Plan de adopción

### Fase 0: Contención

- Rotar la credencial SQL expuesta.
- Guardar la conexión fuera de Git.
- Revisar qué cambios del working tree pertenecen al usuario.

### Fase 1: Contrato del proyecto

- Decidir el destino de `.agent/`.
- Restaurar o reemplazar sus referencias sin dejar enlaces rotos.
- Mantener `AGENTS.md`, `context.md` y skills alineados.

### Fase 2: SDD e índices

- Corregir `openspec/config.yaml` para .NET 9.
- Definir `hybrid` como modo recomendado.
- Inicializar CodeGraph.
- Regenerar skill registry después de resolver `.agent/`.

### Fase 3: Plataforma global

- Crear manifiesto y lock de versiones en `~/ai-stack`.
- Elegir un solo propietario de perfiles.
- Consolidar Engram en una integración.
- Aplicar permisos globales de seguridad.
- Verificar plugins y actualizar componentes de forma controlada.

### Fase 4: Calidad continua

- Añadir CI con restore y build.
- Introducir pruebas por los flujos de mayor riesgo.
- Validar configuración y referencias rotas automáticamente.
- Probar bootstrap, sincronización, backup y restore en un HOME temporal.

## Checklist de operación

- [ ] La solicitud tiene alcance y resultado esperados claros.
- [ ] El working tree fue revisado sin revertir cambios ajenos.
- [ ] Se cargó contexto global, de área y de módulo.
- [ ] Se consultó CodeGraph para impacto estructural.
- [ ] Se consultó Context7 solo para documentación externa vigente.
- [ ] Se eligió ejecución directa o SDD de forma proporcional.
- [ ] Cada subagente tiene archivos y responsabilidad no solapados.
- [ ] No se expusieron secretos en prompts, logs ni memoria.
- [ ] Build y pruebas se ejecutaron o la limitación quedó documentada.
- [ ] Specs, tareas y evidencia coinciden.
- [ ] Se revisaron riesgo, legibilidad, confiabilidad y resiliencia según alcance.
- [ ] Engram recibió solo decisiones y aprendizajes útiles.
- [ ] No se creó commit sin aprobación explícita.

## Criterio de preparación

La arquitectura está lista para trabajo cotidiano cuando:

- El contrato `.agent/` existe o fue reemplazado completamente.
- No hay credenciales versionadas.
- OpenSpec reconoce el stack .NET correcto.
- CodeGraph está disponible para el proyecto.
- OpenCode tiene perfiles, permisos y plugins coherentes.
- Engram usa una política única de captura.
- `dotnet restore` y `dotnet build` funcionan en el entorno.
- Un cambio pequeño puede completarse directamente.
- Un cambio sustancial puede recorrer SDD de principio a fin con evidencia.
