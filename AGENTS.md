# 🤖 AGENTS.md (Root Orchestrator)
**Proyecto**: Laboratorios Univalle
**Rol**: Agente Orquestador Principal

## 1. Contexto Cultural y Operativo
Bienvenido al Sistema de Gestión de Laboratorios Univalle. Somo un equipo enfocado en la **Alta Precisión Técnica**. Nuestro stack tecnológico (ASP.NET Core 9, Razor Pages, EF Core 9) exige rigor estructurado.
- **Calidad Premium**: No entregamos trabajos a medias ("MVPs feos"). Nuestra UI (basada en NiceAdmin, Bootstrap 4) debe ser siempre asimétrica, validada y con *Soft Badges*.
- **Integridad de Datos**: La universidad no borra historia. El concepto de `.Remove()` de SQL está estrictamente prohibido a favor del *"Soft Delete"*.

## 2. Instrucciones para el Agente Orquestador
Tu misión primaria es **enrutar y delegar** el contexto. Eres la puerta de entrada.
- **NO DEBES** alucinar respuestas completas basado solo en este archivo.
- Todo tu conocimiento técnico está distribuido en múltiples `AGENTS.md` especializados por "Feature" y en un sistema de `Skills`.
- **ANTES DE EMPEZAR CUALQUIER TAREA**: Leer `CONTEXT.md` completo. Contiene la arquitectura actual, reglas obligatorias, convenciones, y estado de módulos.
- **NO HACER COMMIT** sin aprobación explícita del usuario.
- Si la tarea involucra más de 5 archivos, proponer un plan detallado antes de ejecutar.
- **Documentar** los cambios en `context.md` al finalizar la sesión.

### Flujo de Trabajo Obligatorio
Cuando el usuario haga una solicitud, debes:
1. Analizar de qué capa arquitectónica se trata (¿Frontend? ¿Datos? ¿Reportes? ¿Correctivo?).
2. **Leer `context.md` completo** para entender el estado actual.
3. Leer el sub-manifiesto correspondiente (`AGENTS.md` locales).
4. **Auto-invocar** las Skills necesarias en `.agent/skills/`.

## 3. Directorio de Sub-Manifiestos y Skills (Trigger y Scope)

### 🖥️ Módulo de UI y Vistas (Frontend)
- **Localización**: `Pages/AGENTS.md`
- **Skill a invocar**: `.agent/skills/ui_premium/SKILL.md`
- **Cuándo Invocarlas**: Modificación de vistas `.cshtml`, SweetAlert2 (usar `@Html.Raw()` para acentos), Bootstrap modals, formularios y validación de cliente (`jqBootstrapValidation`).

### 🗄️ Módulo de Datos (Dominio y DB)
- **Localización**: `Models/AGENTS.md`
- **Skill a invocar**: `.agent/skills/database/SKILL.md`
- **Cuándo Invocarlas**: Alteración de entidades, Inyección de EF Core, Migraciones (SIEMPRE nuevas, nunca modificar existentes), Relaciones y Auditoría. Atención: Semester en Management acepta 0 (Correctivo), 1, 2.

### ⚙️ Lógica de Negocio y Controladores
- **Localización**: *Regresa al maestro o lee los PageModels*.
- **Skill a invocar**: `.agent/skills/backend_methods/SKILL.md`
- **Cuándo Invocarlas**: Manejo de `InputModels` dentro de Razor Pages, bloques `try-catch`, y métodos `OnPost`.
- **Correctivo**: Leer sección 6 de `context.md` — routing con `ManagementId`, `ResolveManagementAsync()`, dropdowns condicionales.

### 📊 Sistema Transversal de Reportes
- **Localización**: `Services/AGENTS.md`
- **Skill a invocar**: `.agent/skills/reporting/SKILL.md`
- **Cuándo Invocarlas**: Exportación a PDF (QuestPDF), generación de plantillas de Excel, impresión institucional. L-6 Excel genera desde cero (sin template).

### 🔧 Configuración e Infraestructura (Onboarding)
- **Localización**: `AGENTS_SETUP.md`
- **Cuándo Invocarlas**: Al clonar el proyecto, errores de conexión (`Connection String`), restauración de paquetes, migraciones iniciales y configuración de SQL Server.
- **BD**: `DB_Laboratorios_Univalle` (o `_N` según appsettings). SeedData en `DbInitializer.cs`.

### 🔄 Módulo Correctivo (NUEVO — leer context.md sección 6)
- **Flujo**: Saltar L-6, empezar en L-7, mismo wizard L-8→L-12.
- **Routing**: ManagementId OBLIGATORIO en todos los redirects.
- **Create pages**: dropdowns solo bloqueados en preventivo (`isWizard && !isCorrective`).
- **Delete**: permitido en correctivo activo.

### 📋 L-6 Verificaciones (REFACTORIZADO — leer context.md sección 7)
- **MassCreate**: verificación masiva por laboratorio. Sin checklist.
- **Index**: sesiones agrupadas (SessionGroup). Vista vacía hasta seleccionar lab.
- **Details**: modo sesión (labId+date) y modo individual (id, fallback).

---
*Nota para el Orquestador: Si el usuario solicita un refactor masivo de UI de más de 3 archivos, DEBES levantar un Subagente para procesar la petición y limpiar la memoria contextual.*
