/*
  Relleno del historial de estados de equipos (2026-10-26).

  Los equipos importados desde BaseGastronomia_v1.xlsx no recibieron una fila
  inicial en EquipmentStateHistories. Este script crea esa fila:
    - Equipos sin historial: estado actual, desde la fecha de alta (CreatedDate), vigente.
    - Equipos con historial que empieza después del alta: fila con el estado de
      importación (columna Estadooperativo del Excel) desde el alta hasta el
      primer cambio registrado. En REAL solo aplica a la unidad 51435
      (Batidora, «Operativo» en el Excel).

  Idempotente: no inserta si el equipo ya tiene una fila que empiece en su alta o antes.
  Inserta en lotes de 100 filas.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Reason nvarchar(500) = N'Estado inicial al importar el inventario (BaseGastronomia_v1.xlsx).';
DECLARE @Rows int = 1;

BEGIN TRANSACTION;

-- 1) Equipos con historial posterior al alta: cerrar el hueco con el estado de importación.
INSERT INTO EquipmentStateHistories (EquipmentUnitId, Status, StartDate, EndDate, Reason, CreatedDate)
SELECT u.Id, 0 /* Operational */, u.CreatedDate, f.FirstStart, @Reason, SYSUTCDATETIME()
FROM EquipmentUnits u
CROSS APPLY (SELECT MIN(h.StartDate) AS FirstStart FROM EquipmentStateHistories h WHERE h.EquipmentUnitId = u.Id) f
WHERE u.InventoryNumber = N'51435'
  AND f.FirstStart IS NOT NULL
  AND f.FirstStart > u.CreatedDate;

-- 2) Equipos sin ninguna fila: estado actual desde el alta, vigente.
WHILE @Rows > 0
BEGIN
    INSERT INTO EquipmentStateHistories (EquipmentUnitId, Status, StartDate, EndDate, Reason, CreatedDate)
    SELECT TOP (100) u.Id, u.CurrentStatus, u.CreatedDate, NULL, @Reason, SYSUTCDATETIME()
    FROM EquipmentUnits u
    WHERE NOT EXISTS (SELECT 1 FROM EquipmentStateHistories h WHERE h.EquipmentUnitId = u.Id)
    ORDER BY u.Id;
    SET @Rows = @@ROWCOUNT;
END;

COMMIT TRANSACTION;

SELECT COUNT(*) AS Filas, COUNT(DISTINCT EquipmentUnitId) AS Equipos FROM EquipmentStateHistories;
