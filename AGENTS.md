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

### Flujo de Trabajo Obligatorio
Cuando el usuario haga una solicitud, debes:
1. Analizar de qué capa arquitectónica se trata (¿Frontend? ¿Datos? ¿Reportes?).
2. Leer el sub-manifiesto correspondiente (`AGENTS.md` locales).
3. **Auto-invocar** las Skills necesarias en `.agent/skills/`.

## 3. Directorio de Sub-Manifiestos y Skills (Trigger y Scope)

### 🖥️ Módulo de UI y Vistas (Frontend)
- **Localización**: `Pages/AGENTS.md`
- **Skill a invocar**: `.agent/skills/ui_premium/SKILL.md`
- **Cuándo Invocarlas**: Modificación de vistas `.cshtml`, cambios en colores (SweetAlert2), formularios y validación de cliente (`jqBootstrapValidation`).

### 🗄️ Módulo de Datos (Dominio y DB)
- **Localización**: `Models/AGENTS.md`
- **Skill a invocar**: `.agent/skills/database/SKILL.md`
- **Cuándo Invocarlas**: Alteración de entidades, Inyección de EF Core, Migraciones, Relaciones y Auditoría.

### ⚙️ Lógica de Negocio y Controladores
- **Localización**: *Regresa al maestro o lee los PageModels*.
- **Skill a invocar**: `.agent/skills/backend_methods/SKILL.md`
- **Cuándo Invocarlas**: Manejo de `InputModels` dentro de Razor Pages, bloques `try-catch`, y métodos `OnPost`.

### 📊 Sistema Transversal de Reportes
- **Localización**: `Services/AGENTS.md`
- **Skill a invocar**: `.agent/skills/reporting/SKILL.md`
- **Cuándo Invocarlas**: Exportación a PDF (QuestPDF), generación de plantillas de Excel, impresión institucional de oficios (ClosedXML).

## 4. Arquitectura: Wizard de Mantenimiento Inteligente (Flujo L6->L7->L8->L3)
*Esta sección define las directrices estrictas para cuando se programe el nuevo flujo integrado de mantenimientos.*

### 🚀 Directrices de Implementación Backend (PageModels & EF Core)
1. **Dashboard Matriz Principal (L48 + L22)**:
   - El **L48** es el componente orquestador central. Debe mostrarse como una matriz transversal (tipo Gantt) de mantenimientos planeados vs. ejecutados.
   - El **L22** integrará métricas de % de avance (Overall, Subgrupos, Tipo de Equipo).
2. **Máquina de Estados (State Machine) No Bloqueante**:
   - El progreso por las fases del Wizard (`Verificación L6` -> `Solicitud L7` -> `Kardex L8` -> `Salida L3`) no debe bloquear un lote entero de equipos si uno falla.
   - **Regla Estricta de Datos**: No borrar registros. Usar **"Soft Flags"** en las entidades (ej. `EstadoVerificacion = "Fallido"`, `RequiereMantenimiento = true`). Los `Select` o `Where` de la siguiente etapa del Wizard solo filtrarán los equipos que cumplen las banderas necesarias.
3. **Flujo Preventivo vs. Correctivo**:
   - **Flujo Lineal (Preventivo)**: Obligatorio pase por L6, L7, L8, apoyado en guardados intermedios.
   - **Flujo de Excepción (Correctivo / Falla Rápida)**: Debe existir un "bypass" (atajo) que instancie directamente un requerimiento de **L8 (Kardex de Intervención)** para emergencias, brincando L6 y L7, de manera aislada al Wizard principal.

---
*Nota para el Orquestador: Si el usuario solicita un refactor masivo de UI de más de 3 archivos, DEBES levantar un Subagente para procesar la petición y limpiar la memoria contextual.*
