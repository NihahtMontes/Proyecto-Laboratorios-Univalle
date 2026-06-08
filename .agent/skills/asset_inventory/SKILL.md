---
name: asset_inventory
description: Modulo de activos, catalogo Equipment, unidades fisicas, AssetView, categorias Equipo/Utensilio/Otro y navegacion en cascada.
trigger: Cambios en `Pages/Equipment`, `Pages/EquipmentUnits`, `Pages/AssetView`, `Models/Equipment*`, enums de activos o inventario visual.
scope: Pages/AssetView/Equipment/EquipmentUnits/Models
context: .agent/context/areas/ui.md
---
# Skill Asset Inventory

## 1. Contexto Del Modulo

El modulo de activos administra definiciones de catalogo (`Equipment`) y unidades fisicas (`EquipmentUnit`). La consulta visual para usuarios esta en `AssetView` y no reemplaza el CRUD administrativo.

Flujo: catalogo define tipo/recurso -> unidad fisica hereda catalogo y ubicacion -> `AssetView` consulta unidades vigentes por categoria -> usuario filtra/ver/edita/da de baja.

## 2. Arquitectura Y Archivos Clave

- Catalogo: `Pages/Equipment/Create/Edit/Details/Index/Delete`.
- Unidades fisicas: `Pages/EquipmentUnits/Create/Edit/Details/Kardex`.
- Consulta visual: `Pages/AssetView/Index`, `EquipmentClassifications`, `Units`, `UtensilClassifications`, `UtensilUnits`, `OtherUnits`.
- Modelos: `Models/Equipment.cs`, `Models/EquipmentUnit.cs`, `Models/EquipmentStateHistory.cs`, `Models/EquipmentNote.cs`.
- Enums: `Models/Enums/EquipmentCategory.cs`, `Models/Enums/EquipmentTypeClassification.cs`, `Models/Enums/EquipmentStatus.cs`.

## 3. Integracion Con NiceAdmin

- `AssetView/Index` usa tabs `Metricas` e `Inventario`.
- Inventario muestra cards de color para `Equipos`, `Utensilios` y `Otros`.
- Equipos navega a clasificaciones tecnicas y luego a `Units`.
- Utensilios navega a subclasificaciones y luego a `UtensilUnits`.
- Otros navega directo a `OtherUnits`.
- Botones Volver deben mantener `activeTab=Inventario`.
- Tablas finales usan filtros por nombre/serie, paginacion y acciones Ver/Editar/Dar de baja.

## 4. Patrones Y Convenciones

- `EquipmentCategory.Equipment`: usa `TypeClassification` controlado y `UtensilType.NoAplica`.
- `EquipmentCategory.Utensil`: usa `UtensilType` controlado y `TypeClassification.Otro`.
- `EquipmentCategory.Other`: usa `TypeClassification.Otro` y `UtensilType.NoAplica`.
- Valores legacy de utensilios se preservan para editar registros existentes, pero no se ofrecen en altas nuevas.
- Unidades eliminadas usan `EquipmentStatus.Deleted`; no hard-delete.
- Conteos/listados salen desde `EquipmentUnits` con `Include(u => u.Equipment)`.

## 5. Contexto Para Agente

- No mezclar CRUD administrativo con consulta `AssetView`.
- No romper query filters: `Equipment.Status != Eliminado` y `EquipmentUnit.CurrentStatus != Deleted`.
- Si se agregan categorias o subclasificaciones, actualizar enum, CRUD, cards, conteos, filtros y docs.
- Los textos deben hablar de activo/recurso cuando aplique, no solo "equipo".
