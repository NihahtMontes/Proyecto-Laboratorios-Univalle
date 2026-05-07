-- ============================================================
-- SCRIPT: Resetear TODOS los datos de DB_Laboratorios_Univalle
-- Ejecutar en SSMS o Azure Data Studio contra la BD local.
-- ⚠️ ESTO BORRA TODA LA DATA. Los esquemas (tablas) se mantienen.
-- ============================================================

USE [DB_Laboratorios_Univalle];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Paso 1: Desactivar TODAS las restricciones FK
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' NOCHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.type = 'U';
EXEC sp_executesql @sql;
PRINT 'FK desactivadas.';
GO

-- Paso 2: Borrar datos de todas las tablas (con QUOTED_IDENTIFIER ON)
SET QUOTED_IDENTIFIER ON;
DECLARE @sql2 NVARCHAR(MAX) = N'';
SELECT @sql2 += 'DELETE FROM ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ';' + CHAR(13)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.type = 'U' AND t.name <> '__EFMigrationsHistory';
EXEC sp_executesql @sql2;
PRINT 'Datos borrados.';
GO

-- Paso 3: Reactivar TODAS las restricciones FK
DECLARE @sql3 NVARCHAR(MAX) = N'';
SELECT @sql3 += 'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.type = 'U';
EXEC sp_executesql @sql3;
PRINT 'FK reactivadas.';
GO

-- Paso 4: Resetear IDENTITY seeds a 0
DECLARE @sql4 NVARCHAR(MAX) = N'';
SELECT @sql4 += 'DBCC CHECKIDENT(''' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ''', RESEED, 0);' + CHAR(13)
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.identity_columns ic ON t.object_id = ic.object_id
WHERE t.type = 'U';
EXEC sp_executesql @sql4;
PRINT 'IDs reiniciados.';
GO

PRINT '=== Base de datos reseteada exitosamente ===';
GO
