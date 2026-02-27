---
name: database
description: Manipulación estricta de Entity Framework Core, Reglas de "Soft Delete", Eager Loading, Auditorías automáticas y Control de Duplicados DB.
trigger: Creación de Migraciones, Entidades nuevas en `Models/`, o consultas LINQ en PageModels de escritura y borrado.
author: Antigravity Orquestador Alpha
version: 1.0.0
license: MIT
scope: Data
tools_allowed: [view_file, write_to_file, run_command]
---
# 🗄️ SKILL: Base de Datos, Entity Framework y Persistencia
**Objetivo Primario:** La integridad y rastreabilidad forense de los datos en este sistema universitario es el activo máximo. No se tolera pérdida física de información.

## 1. El Veto Absoluto: "Soft Delete" (Borrado Lógico)
En registros core (`User`, `Equipment`, `Verification`), **ESTÁ ESTRICTAMENTE PROHIBIDO** ejecutar `_context.Entidad.Remove(...)` (Delete físico).

### 1.1 Flujo de Eliminación de Activos
1. **Paso 1:** Asigna el enum correspondiente: `Entidad.Status = Status.Deleted` o `Status.Inactivo`.
2. **Paso 2:** Si el objeto posee historial temporal (Ej. `EquipmentStateHistory`), debes insertar un registro histórico nuevo anexando el `Status.Deleted` y cerrar la fecha del registro anterior si existiese.
3. **Paso 3:** ¡Nunca olvides hacer `await _context.SaveChangesAsync()`!

### 1.2 Query Filters (Exclusión por Defecto)
Asegura que `ApplicationDbContext` globalice `.HasQueryFilter(e => e.Status != EquipmentStatus.Deleted)` en la definición `.OnModelCreating`. Si no es posible, cada query LINQ `.Where(...)` debe filtrar explícitamente los dados de baja.

## 2. Bloqueo Limpiador C#: `.Clean()` y `.IsValidName()`
La base de datos repudia espacios dobles o carácteres ciegos.
- Todo string insertado por el usuario: `Input.Name = Input.Name.Clean();`
- Si no posee una validación de formato custom, invoca `.IsValidName()` en el Frontend / InputModel para purgar "%\$&".

## 3. Unicidad, Concurrencia y Null Exceptions

### 3.1 Anti-Duplicación Dual
Si diseñas tablas con Códigos Biométricos, DNI o Nros. de Inventario:
1. `DbSet.HasIndex(x => x.InventoryNumber).IsUnique()` (A nivel tabla SQL).
2. Valida la duplicidad primero en `OnPostAsync()` antes de golpear el Contexto SQL.
> **⚠️ WARNING (Edit Mode):** En el ciclo "Editar", al buscar unicidad asegúrate de IGNORAR el registro actual: `.Where(e => e.Code == In.Code && e.Id != In.Id)`. Y siempre notifica del error con el sistema `NotificationHelper`.

### 3.2 Eager Loading (`.Include()`) de Hierro
Evitar Excepciones Null en cascada al renderizar Razor (`Model.Objeto.Relacional.String`).
- **SIEMPRE**: Todo PageModel `OnGetAsync` o Detail/Edit debe cargar dependencias complejas anidando `.Include(x => x.Padre).ThenInclude(p => p.Abuelo)`. Descartarlo trae colapsos fatales en tiempo de ejecución.

## 4. Auditoría Automática Inyectada
Una entidad núcleo debe implementar o contener estas 4 cabeceras: `CreatedBy`, `CreatedDate`, `ModifiedBy`, `LastModifiedDate`.
- El bloque centralizador en `ApplicationDbContext.SaveChangesAsync()` ya inyecta estas variables dinámicamente según el claim ID del usuario autenticado; **PERO**, debes asegurarte que tus modelos `Models/` expongan estas propiedades relacionales hacia `User`. No las toques manualmente en tu PageModel.
