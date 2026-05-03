-- =============================================
-- MIGRACIÓN MANUAL: AddEquipmentSoftDelete
-- Agrega columna Status a la tabla Equipments
-- para soportar Soft Delete (Status = 2 = Eliminado)
-- =============================================

-- 1. Agregar columna Status con default Activo (0)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Equipments' AND COLUMN_NAME = 'Status'
)
BEGIN
    ALTER TABLE Equipments ADD Status INT NOT NULL DEFAULT 0;
    PRINT 'Columna Status agregada a Equipments con default 0 (Activo).';
END
ELSE
BEGIN
    PRINT 'Columna Status ya existe en Equipments — omitiendo.';
END
GO

-- 2. Verificación
SELECT 'Equipments.Status' AS Columna, 
       COUNT(*) AS TotalRegistros, 
       SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS Activos,
       SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS Eliminados
FROM Equipments;
GO
