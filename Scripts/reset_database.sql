-- ============================================================
-- SCRIPT: Resetear TODOS los datos de DB_Laboratorios_Univalle
-- Ejecutar en SSMS o Azure Data Studio contra la BD local.
-- ADVERTENCIA: borra toda la data; conserva tablas y migraciones EF.
-- ============================================================

USE [DB_Laboratorios_Univalle];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

-- Proteccion contra ejecucion accidental. Exige confirmacion manual y nombre exacto.
DECLARE @ConfirmDestructive BIT = 0;
IF DB_NAME() <> N'DB_Laboratorios_Univalle'
    THROW 51000, 'Base incorrecta: este script solo admite DB_Laboratorios_Univalle.', 1;
IF @ConfirmDestructive <> 1
    THROW 51001, 'Ejecucion detenida. Establezca @ConfirmDestructive = 1 despues de verificar un respaldo.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @DisableSql NVARCHAR(MAX) = N'';
    SELECT @DisableSql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
        + N' NOCHECK CONSTRAINT ALL;' + CHAR(13)
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.type = 'U';
    EXEC sys.sp_executesql @DisableSql;

    DECLARE @DeleteSql NVARCHAR(MAX) = N'';
    SELECT @DeleteSql += N'DELETE FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.type = 'U'
      AND t.name <> N'__EFMigrationsHistory';
    EXEC sys.sp_executesql @DeleteSql;

    DECLARE @EnableSql NVARCHAR(MAX) = N'';
    SELECT @EnableSql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
        + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13)
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.type = 'U';
    EXEC sys.sp_executesql @EnableSql;

    DECLARE @ReseedSql NVARCHAR(MAX) = N'';
    SELECT @ReseedSql += N'DBCC CHECKIDENT (''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
        + N''', RESEED, 0) WITH NO_INFOMSGS;' + CHAR(13)
    FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    INNER JOIN sys.identity_columns ic ON t.object_id = ic.object_id
    WHERE t.type = 'U';
    EXEC sys.sp_executesql @ReseedSql;

    COMMIT TRANSACTION;
    PRINT 'Base de datos reseteada correctamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
