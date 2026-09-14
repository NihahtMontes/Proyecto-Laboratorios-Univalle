# Flujo multiagente con OpenCode

**Estado operativo**: 9 de septiembre de 2026

**Alcance**: coordinación de trabajo sobre el repositorio existente; no reemplaza el pipeline .NET ni sus contratos.

## Propósito

OpenCode funciona como una capa de coordinación sobre Laboratorios Univalle. El agente principal define alcance, asigna un propietario temporal, integra resultados y verifica evidencia. Los subagentes aportan especialización sin crear una arquitectura, herramientas ni fuentes de verdad paralelas.

La configuración compartida reside en `opencode.json` y los perfiles en `.opencode/agents/`. Las reglas generales continúan en `AGENTS.md`; las reglas locales y skills se cargan solo cuando el módulo afectado las requiere.

## Fuentes de verdad

Usar este orden para resolver discrepancias:

1. Código C# y contratos serializados que determinan el comportamiento real.
2. Self-checks y herramientas QA que verifican comportamiento ejecutable.
3. Manifiestos, snapshots, fingerprints y hashes de la ejecución concreta que se analiza.
4. Documentación, como explicación y guía operativa.

Una cifra documentada no describe automáticamente el estado actual. Debe vincularse con un artefacto, hash, fecha o identificador de ejecución vigente.

## Agentes

| Agente | Modo | Responsabilidad | Escritura ordinaria |
|---|---|---|---|
| `orchestrator` | Primary | Alcance, asignación, integración y verificación | Requiere permiso |
| `data-analyst` | Subagent | Relaciones, integridad y calidad de datos | Ninguna |
| `excel-auditor` | Subagent | Libro Excel, EPPlus y correspondencia contractual | Ninguna |
| `reconciliation-auditor` | Subagent | P0/P1/P2, conflictos y trazabilidad | Ninguna |
| `documentation` | Subagent | Alineación documental basada en evidencia | Markdown asignado bajo `docs/` |
| `dotnet-data-engineer` | Subagent | Implementación en el pipeline histórico | Directorios asignados de Import/Apply |

`subagent_depth: 1` permite que el orquestador invoque especialistas sin habilitar otro nivel de delegación. `permission.task` limita la delegación automática a estos cinco perfiles. El usuario aún puede invocarlos directamente mediante `@`.

## Protocolo de propiedad temporal

OpenCode no proporciona locking transaccional por archivo. La exclusión de escritores es responsabilidad explícita del orquestador.

Antes de cualquier escritura, registrar en la conversación:

```text
Trabajo: OC-AAAA-MM-DD-NN
Agente propietario: <agente>
Objetivo: <resultado acotado>
Archivos o superficies: <rutas exactas>
Actividad: lectura | escritura local | escritura externa
Dependencias: <trabajos o decisiones previas>
Evidencia esperada: <diff, build, self-check, hashes o manifiestos>
Estado: reservado
```

Reglas:

- Los análisis de solo lectura pueden ejecutarse en paralelo.
- Un archivo solo puede tener un escritor activo.
- Los cambios con superficies coincidentes se ejecutan en serie.
- El propietario no amplía sus rutas sin autorización del orquestador.
- La propiedad termina únicamente con un handoff aceptado o una cancelación explícita.
- El orquestador integra los resultados y conserva la decisión final.

## Protección del Excel original

La fuente institucional es:

`D:/proyectoSis/Excels/Plantilla_Original.xlsx`

Nunca debe moverse, renombrarse, editarse, sobrescribirse, copiarse al repositorio ni versionarse. El acceso externo de OpenCode se limita a `D:/proyectoSis/Excels/**` y la edición está denegada.

La denegación de `edit` no constituye por sí sola una barrera frente a comandos de shell. La protección operativa combina:

- agentes auditores sin shell o con allowlist estrictamente de lectura;
- shell del ingeniero y del orquestador en modo `ask`;
- prohibición expresa de escribir mediante shell;
- SHA-256 antes y después de cualquier tarea que utilice el original.

Hash de referencia verificado el 9 de septiembre de 2026:

`FAEAF00640BD4AF05D78C22B3B5FB7E105260BF8AF8A4B5E8FC2FAD4A78484A4`

Si el hash final difiere, detener el trabajo, preservar la evidencia y comunicar el incidente. No intentar reparar o reemplazar el archivo automáticamente.

## Flujo de trabajo

1. El orquestador lee `AGENTS.md`, `context.md` y el contexto mínimo del módulo.
2. Define alcance, riesgos, propietario y evidencia esperada.
3. Delega análisis independientes en paralelo cuando sean de solo lectura.
4. Sintetiza hallazgos y autoriza como máximo un escritor por archivo.
5. El escritor implementa únicamente en la superficie asignada.
6. Se ejecutan comprobaciones proporcionales al riesgo y autorizadas.
7. El escritor entrega el handoff; el orquestador revisa diff y evidencia.
8. El orquestador informa archivos modificados, validaciones ejecutadas y riesgos pendientes.

Las operaciones de datos, migraciones, nuevas dependencias, commits, despliegues y cambios externos necesitan autorización explícita. No se inicia la aplicación web ni un servicio persistente salvo pedido expreso.

## Validación

Para configuración de OpenCode:

```powershell
opencode --version
opencode agent list
git diff --check
git status --short
```

La versión debe pertenecer a la rama estable 1.x. Si OpenCode no está instalado, no se instala automáticamente: se realizan las comprobaciones estáticas y se reporta pendiente el smoke test.

Para cambios C# en herramientas históricas, compilar de forma explícita el proyecto afectado porque `Tools/` no forma parte de la solución principal:

```powershell
dotnet build Tools/HistoricalDataImport/HistoricalDataImport.csproj -m:1
dotnet build Tools/HistoricalDataApply/HistoricalDataApply.csproj -m:1
```

El repositorio no tiene un runner convencional xUnit, NUnit o MSTest. Usar los self-checks y herramientas QA existentes según el alcance, sin presentar `dotnet test` como cobertura disponible. Importaciones, `apply`, SQL o conexiones de base de datos solo se ejecutan con autorización explícita.

## Handoff

Todo escritor entrega:

- trabajo e identidad del propietario;
- archivos modificados;
- resumen de comportamiento;
- comandos ejecutados y resultado observado;
- hashes, conteos o manifiestos comparados cuando correspondan;
- validaciones no ejecutadas y motivo;
- riesgos o decisiones pendientes.

## Referencias

- `AGENTS.md`: reglas generales y enrutamiento.
- `context.md`: índice operativo del proyecto.
- `docs/COORDINACION_MULTI_CHAT.md`: coordinación persistente entre tareas.
- `docs/GUIA_IMPORTACION_HISTORICA.md`: guía viva del pipeline histórico.
- `Tools/HistoricalDataImport/`: lectura, contrato canónico y auditoría.
- `Tools/HistoricalDataApply/`: planificación, reconciliación y aplicación controlada.
