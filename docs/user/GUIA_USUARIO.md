# 🚀 Guía de Usuario: Ecosistema Avanzado de Agentes Univalle

Bienvenido al sistema de orquestación de Agentes de IA del Proyecto Laboratorios Univalle. Esta documentación te guiará sobre cómo interactuar y sacar el máximo provecho de nuestra arquitectura basada en Skills y manifiestos descentralizados.

## 🎯 ¿Qué es esta arquitectura?

A diferencia de proyectos pequeños donde la IA lee un solo archivo de instrucciones, Univalle utiliza una **Jerarquía de Orquestación**. Esto previene que la IA "alucine" al intentar procesar miles de líneas de contexto innecesario.

### Los Manifiestos (AGENTS.md)
En lugar de darle a la IA todas las reglas de la base de datos cuando solo vas a modificar un botón, el contexto se divide:
1. **El Orquestador (`/AGENTS.md`)**: Ubicado en la raíz. Solo contiene la cultura del proyecto y funciona como un "director de orquesta", indicándole a la IA qué sub-manifiesto leer según tu petición.
2. **Sub-Manifiestos (`/Pages/AGENTS.md`, `/Models/AGENTS.md`, etc.)**: Son específicos por característica (feature).

### Las Habilidades (Skills)
Ubicadas en `.agent/skills/`. Son "poderes" específicos que la IA puede autoinvocar **solo cuando es necesario**.
Por ejemplo, la IA no cargará en su memoria las reglas complejas de cómo generar un Excel en ClosedXML a menos que le pidas explícitamente trabajar con reportes.

---

## 🛠️ Cómo interactuar con la IA

Para obtener los mejores resultados, siempre menciona el contexto de la capa en la que quieres trabajar:

### 1. Peticiones de Interfaz de Usuario (UI)
> *"Por favor, crea la vista Index para la entidad `Person`. Lee el manifiesto en `Pages/` y aplica la skill de `ui_premium`."*
La IA automáticamente:
- Aplicará el Layout Asimétrico 8/4 o Dual-Tab.
- Implementará validaciones con jqBootstrapValidation.

### 2. Peticiones de Base de Datos
> *"Agrega un nuevo campo a la tabla Equipos. Revisa `Models/AGENTS.md`."*
La IA automáticamente:
- Aplicará el patrón "Soft Delete" en Entity Framework Core.
- Generará migraciones sin romper las reglas de concurrencia.

### 3. Peticiones de Reportes
> *"Quiero que el campo de observaciones salga en el PDF de mantenimiento. Invoca la skill de `reporting`."*
La IA automáticamente:
- Entenderá las coordenadas rígidas del Excel institucional.
- Sabrá limpiar los datos dummy antes de renderizar (ej. Rango `A19` a `P38`).

## 📁 Estructura del Conocimiento

```text
/
├── AGENTS.md                  <-- Manifiesto Maestro (Contexto Cultural y Orquestación)
├── Pages/
│   └── AGENTS.md              <-- Reglas de UI, Razor y Formatos
├── Models/
│   └── AGENTS.md              <-- Reglas de EF Core, Base de datos y Validaciones
├── Services/
│   └── AGENTS.md              <-- Reglas de Negocio, APIs y PDF/Excel
└── .agent/
    └── skills/                <-- CARPETA DE HABILIDADES AISLADAS
        ├── ui_premium/
        │   └── SKILL.md       <-- (Metadata YAML: Scope, Herramientas, Ejemplos)
        ├── database/
        │   └── SKILL.md
        ├── reporting/
        │   └── SKILL.md
        └── backend_methods/
            └── SKILL.md
```

## 🔄 Sobre los Subagentes
Para tareas gigantescas (Ej. "Cambia el color de todos los botones en 50 vistas"), el agente maestro creará de forma transparente **Subagentes**. Cada subagente operará de forma independiente editando sus archivos, reduciendo el ruido en el hilo principal del chat. No te asustes si ves a la IA derivando trabajo.
