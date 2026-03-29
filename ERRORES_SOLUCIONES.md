# 🐛 Registro de Errores y Soluciones Implementadas

Este documento sirve como base de conocimiento para no repetir los errores de infraestructura que hemos enfrentado en el entorno local (Windows/Visual Studio/SQL Server).

## 1. Archivos con codificación UTF-8 BOM
**El Problema:** Al clonar o generar el proyecto en Windows, los archivos C# (`.cs`) se guardan con una marca `BOM` (0xEF, 0xBB, 0xBF). Esto bloquea las herramientas automatizadas que leen el texto nativo, causando el error `"failed to detect charset"`.
**La Solución:** 
Se creó el script de PowerShell `fix-encoding.ps1` en la raíz del proyecto.
1. Ejecutar de vez en cuando: `powershell -ExecutionPolicy Bypass -File fix-encoding.ps1`
2. Esto remueve la firma BOM de todos los archivos y permite su lectura directa por sistemas Linux o agentes IA.

## 2. Redundancia de Migraciones de Entity Framework (Tablas Fantasma)
**El Problema:** Al intentar ejecutar `Update-Database`, SQL Server arrojaba `Cannot find the object "ManagementPlans"`. Esto ocurrió porque existía una migración antigua (con errores o sin ejecutar) que creó la tabla en el *Snapshot* del contexto, pero no en la base de datos real. Al crear una segunda migración (`AddManagementL48`), EF intentó hacer un `ALTER TABLE` a una tabla inexistente.
**La Solución:**
1. Siempre eliminar de la carpeta `Migrations` los archivos de migraciones que generaron conflicto o nunca se terminaron de aplicar exitosamente.
2. Usar `Remove-Migration` si es necesario para limpiar el historial local.
3. Generar una **única** migración consolidada (`Add-Migration NombreFresco`) cuando el modelo ya esté estable.

## 3. Advertencia de "Global Query Filter" (Filtros de Borrado Lógico)
**El Problema:** Entity Framework advertía: `Entity 'EquipmentUnit' has a global query filter... and is the required end of a relationship`. EF no permite que un registro hijo (como `ManagementPlan`) tenga un Foreign Key **Requerido** (ej: `int EquipmentUnitId`) si el padre (`EquipmentUnit`) puede ser filtrado globalmente (ej: ser ocultado por `IsDeleted = true`).
**La Solución:**
Se volvió opcional el campo Foreign Key en la tabla hija.
En `ManagementPlan.cs`, se cambió `public int EquipmentUnitId` a nuleable `public int? EquipmentUnitId` y se le quitó la etiqueta `[Required]`. Esto contentó las reglas de cardinalidad de Entity Framework.

## 4. Clases Duplicadas (Ambigüedad de Contexto)
**El Problema:** Al intentar parchar el archivo `ApplicationDbContext.cs` mediante comandos de consola cuando el BOM estaba bloqueado, se triplicó el contenido, generando errores de "Ambigüedad" y "El tipo ya define un miembro". 
**La Solución:**
1. Nunca forzar escrituras de consola en archivos C# con codificación mixta si no se ha aplicado el script `fix-encoding.ps1` primero.
2. El usuario debe verificar abriendo el archivo que solo exista una declaración del `class ApplicationDbContext`.

---
*Fin del registro de problemas recurrentes de arquitectura.*
