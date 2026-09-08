:on error exit
:setvar DatabaseName "DB_Laboratorios_Univalle_MIGRACION"

USE [$(DatabaseName)];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
DBCC CHECKDB ([$(DatabaseName)]) WITH NO_INFOMSGS, DATA_PURITY;

IF EXISTS (
    SELECT InventoryNumber
    FROM dbo.EquipmentUnits
    GROUP BY InventoryNumber
    HAVING COUNT_BIG(*) > 1
)
    THROW 52000, 'Hay numeros de inventario duplicados.', 1;

IF EXISTS (
    SELECT Code
    FROM dbo.Laboratories
    GROUP BY Code
    HAVING COUNT_BIG(*) > 1
)
    THROW 52001, 'Hay codigos de laboratorio duplicados.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.Requests AS request
    LEFT JOIN dbo.EquipmentUnits AS unit ON unit.Id = request.EquipmentUnitId
    LEFT JOIN dbo.Laboratories AS laboratory ON laboratory.Id = request.LaboratoryId
    WHERE request.EquipmentUnitId IS NOT NULL
      AND (unit.Id IS NULL OR laboratory.Id IS NULL)
)
    THROW 52002, 'Hay solicitudes con relaciones huerfanas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.EquipmentUnits) <> 556
    THROW 52003, 'La carga no contiene las 556 unidades esperadas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.Requests) <> 520
    THROW 52004, 'La carga no contiene las 520 solicitudes esperadas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.Maintenances) <> 157
    THROW 52005, 'La carga no contiene los 157 mantenimientos esperados.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.Verifications) <> 1020
    THROW 52006, 'La carga no contiene las 1020 verificaciones esperadas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.ManagementPlans) <> 517
    THROW 52007, 'La carga no contiene los 517 planes de gestion esperados.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.MaintenanceTasks) <> 157
    THROW 52008, 'La carga no contiene las 157 tareas de mantenimiento esperadas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.CostDetails) <> 124
    THROW 52009, 'La carga no contiene los 124 detalles de costo esperados.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.EquipmentStateHistories) <> 121
    THROW 52010, 'La carga no contiene los 121 historiales Kardex esperados.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.Departures) <> 1
    THROW 52011, 'La carga no contiene la salida historica esperada.', 1;

IF EXISTS (SELECT 1 FROM dbo.Verifications WHERE HistoricalSourceKey IS NULL)
    THROW 52012, 'Hay verificaciones historicas sin clave de procedencia.', 1;

IF EXISTS (SELECT 1 FROM dbo.Requests WHERE HistoricalSourceKey IS NULL)
    THROW 52013, 'Hay solicitudes historicas sin clave de procedencia.', 1;

IF EXISTS (SELECT 1 FROM dbo.Maintenances WHERE HistoricalSourceKey IS NULL)
    THROW 52014, 'Hay mantenimientos historicos sin clave de procedencia.', 1;

IF (SELECT COUNT_BIG(DISTINCT HistoricalSourceKey) FROM dbo.Verifications) <> 1020
    THROW 52015, 'Las claves historicas de verificaciones no son unicas.', 1;

IF (SELECT COUNT_BIG(DISTINCT HistoricalSourceKey) FROM dbo.Requests) <> 520
    THROW 52016, 'Las claves historicas de solicitudes no son unicas.', 1;

IF (SELECT COUNT_BIG(DISTINCT HistoricalSourceKey) FROM dbo.Maintenances) <> 157
    THROW 52017, 'Las claves historicas de mantenimientos no son unicas.', 1;

IF (SELECT COUNT_BIG(*) FROM dbo.HistoricalVerificationQuarantines) <> 562
    THROW 52018, 'La cuarentena no contiene las 562 verificaciones L-6 sin inventario esperadas.', 1;

IF (SELECT COUNT_BIG(DISTINCT SourceKey) FROM dbo.HistoricalVerificationQuarantines) <> 562
    THROW 52019, 'Las claves de origen de la cuarentena L-6 no son unicas.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.HistoricalVerificationQuarantines
    WHERE SourceKey IS NULL OR SourceSheet IS NULL OR SourceRow <= 0 OR EquipmentNameRaw IS NULL
)
    THROW 52020, 'Hay filas de cuarentena L-6 sin trazabilidad minima.', 1;

SELECT
    (SELECT COUNT_BIG(*) FROM dbo.Faculties) AS Faculties,
    (SELECT COUNT_BIG(*) FROM dbo.Careers) AS Careers,
    (SELECT COUNT_BIG(*) FROM dbo.Laboratories) AS Laboratories,
    (SELECT COUNT_BIG(*) FROM dbo.Equipments) AS Equipments,
    (SELECT COUNT_BIG(*) FROM dbo.EquipmentUnits) AS EquipmentUnits,
    (SELECT COUNT_BIG(*) FROM dbo.Verifications) AS Verifications,
    (SELECT COUNT_BIG(*) FROM dbo.Requests) AS Requests,
    (SELECT COUNT_BIG(*) FROM dbo.Maintenances) AS Maintenances,
    (SELECT COUNT_BIG(*) FROM dbo.ManagementPlans) AS ManagementPlans,
    (SELECT COUNT_BIG(*) FROM dbo.HistoricalVerificationQuarantines) AS QuarantinedVerifications;

SELECT COUNT_BIG(*) AS UnitsPendingPhysicalLocation
FROM dbo.EquipmentUnits AS unit
INNER JOIN dbo.Laboratories AS laboratory ON laboratory.Id = unit.LaboratoryId
WHERE laboratory.Code = N'PENDIENTE';

SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
GO
