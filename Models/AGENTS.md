# 🗄️ AGENTS.md (Área de Entidades y Base de Datos)
**Ámbito Funcional**: Entity Framework, Modelado, Migraciones y Estado de Datos.

## 1. Contexto Cultural de esta Capa
Las clases en `Models/` definen el universo universitario de Laboratorios Univalle. Esta capa es la más sagrada: **Los datos nunca se eliminan, trascienden estados.**

## 2. Habilidades Requeridas (Skills)
Cuando el usuario te pida:
- Añadir o modificar una columna a una Base de Datos (`Models/...`).
- Añadir o quitar relaciones (Foreing keys, One-to-Many).
- Correr migraciones o editar el `ApplicationDbContext`.

👉 **DEBES Auto-Invocar la Skill**: `database`.
- Ruta: `.agent/skills/database/SKILL.md`
- Herramientas permitidas: *Lectura profunda de Models y Generación/Ejecución de Migraciones CLI (`dotnet ef`).*

## 3. Prácticas que debes Vigilar
- **Soft Delete Obligatorio**: Tus tablas rastrearán la eliminación con `Status` o `CurrentStatus`. Exige herencia o definición para ignorarlos de consultas directas (`Query Filters`).
- **Auditoría Permanente**: Nadie guarda un registro sin sellarlo con su huella (`CreatedBy`, `CreatedDate`).
- **Unicidad Fuerte**: Configura índices verdaderos en `ApplicationDbContext` (No valides duplicados únicamente en memoria), por ejemplo, para `InventoryNumber` y asegúrate de filtrar estados "eliminados" de la unicidad para evitar cuelgues estáticos si reinsertan un objeto inactivo.
