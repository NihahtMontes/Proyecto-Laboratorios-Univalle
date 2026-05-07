-- ============================================================
-- SCRIPT: Reseteo Dinámico y Carga de Datos Demo
-- ⚠️ USAR EN: DB_Laboratorios_Univalle_NH
-- ============================================================

USE [DB_Laboratorios_Univalle_NH];
GO

SET QUOTED_IDENTIFIER ON;
GO

PRINT '>>> 1. Limpiando Base de Datos (Reverse Order)...';

DELETE FROM Notifications;
DELETE FROM ManagementPlans;
DELETE FROM CostDetails;
DELETE FROM MaintenanceTasks;
DELETE FROM Maintenances;
DELETE FROM Requests;
DELETE FROM VerificationCheckResults;
DELETE FROM VerificationFaults;
DELETE FROM Verifications;
DELETE FROM EquipmentStateHistories;
DELETE FROM Departures;
DELETE FROM EquipmentUnits;
DELETE FROM Equipments;
DELETE FROM Interns;
DELETE FROM Externs;
DELETE FROM People;
DELETE FROM Laboratories;
DELETE FROM Careers;
DELETE FROM Faculties;
DELETE FROM Cities;
DELETE FROM Countries;
DELETE FROM Managements;

PRINT '>>> 2. Insertando Datos Base con IDs Dinámicos...';

DECLARE @CountryId INT, @CityId INT, @FacultyId INT, @CareerId INT, @LabId INT, @PersonId INT, @ManagementId INT;
DECLARE @Equipment1Id INT, @Equipment2Id INT, @Equipment3Id INT;
DECLARE @Unit1Id INT, @Unit2Id INT, @Unit3Id INT;
DECLARE @Verification1Id INT, @Verification2Id INT;
DECLARE @Request1Id INT, @Request2Id INT;
DECLARE @Maintenance1Id INT, @Maintenance2Id INT;

-- Países y Ciudades
INSERT INTO Countries (Name, Status, CreatedDate) VALUES ('Bolivia', 0, GETDATE());
SET @CountryId = SCOPE_IDENTITY();

INSERT INTO Cities (CountryId, Name, Region, Status, CreatedDate) VALUES (@CountryId, 'Cochabamba', 'Cercado', 0, GETDATE());
SET @CityId = SCOPE_IDENTITY();

-- Facultades, Carreras, Laboratorios
INSERT INTO Faculties (Name, Code, Status, CreatedDate) VALUES ('Ciencias Gastronómicas', 'GAS', 0, GETDATE());
SET @FacultyId = SCOPE_IDENTITY();

INSERT INTO Careers (Name, FacultadId, Status, CreatedDate) VALUES ('Gastronomía', @FacultyId, 0, GETDATE());
SET @CareerId = SCOPE_IDENTITY();

INSERT INTO Laboratories (FacultyId, Code, Name, Status, CreatedDate, CityId) VALUES (@FacultyId, 'L-01', 'Cocina Caliente', 0, GETDATE(), @CityId);
SET @LabId = SCOPE_IDENTITY();

-- Personal Técnico
INSERT INTO People (Status, Category, CreatedDate) VALUES (0, 1, GETDATE()); -- Category 1 = Intern
SET @PersonId = SCOPE_IDENTITY();

INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, 'Ing. Juan Técnico (Soporte Interno)', 0);

-- Gestión (2026-I)
INSERT INTO Managements (Year, Semester, Code, Description, StartDate, PlannedEndDate, Status, CreatedDate) 
VALUES (2026, 1, '2026-1', 'Gestión Académica I-2026 (Demo)', '2026-02-01', '2026-07-31', 1, GETDATE());
SET @ManagementId = SCOPE_IDENTITY();

-- Catálogo de Equipos
INSERT INTO Equipments (Category, UtensilType, TypeClassification, CountryId, CityId, Name, Brand, Model, CreatedDate)
VALUES (0, 0, 0, @CountryId, @CityId, 'Microondas Industrial', 'Oster', 'M-2000', GETDATE());
SET @Equipment1Id = SCOPE_IDENTITY();

INSERT INTO Equipments (Category, UtensilType, TypeClassification, CountryId, CityId, Name, Brand, Model, CreatedDate)
VALUES (0, 0, 0, @CountryId, @CityId, 'Horno Convector', 'Rational', 'iCombi Pro', GETDATE());
SET @Equipment2Id = SCOPE_IDENTITY();

INSERT INTO Equipments (Category, UtensilType, TypeClassification, CountryId, CityId, Name, Brand, Model, CreatedDate)
VALUES (0, 0, 0, @CountryId, @CityId, 'Batidora Planetaria', 'KitchenAid', 'Artisan', GETDATE());
SET @Equipment3Id = SCOPE_IDENTITY();

-- Unidades Físicas
INSERT INTO EquipmentUnits (EquipmentId, LaboratoryId, InventoryNumber, CurrentStatus, CreatedDate, ManagementId) 
VALUES (@Equipment1Id, @LabId, '005', 0, GETDATE(), @ManagementId);
SET @Unit1Id = SCOPE_IDENTITY();

INSERT INTO EquipmentUnits (EquipmentId, LaboratoryId, InventoryNumber, CurrentStatus, CreatedDate, ManagementId) 
VALUES (@Equipment2Id, @LabId, '006', 0, GETDATE(), @ManagementId);
SET @Unit2Id = SCOPE_IDENTITY();

INSERT INTO EquipmentUnits (EquipmentId, LaboratoryId, InventoryNumber, CurrentStatus, CreatedDate, ManagementId) 
VALUES (@Equipment3Id, @LabId, '007', 0, GETDATE(), @ManagementId);
SET @Unit3Id = SCOPE_IDENTITY();

PRINT '>>> 3. Insertando Datos del Wizard...';

-- ==============================================================
-- ESCENARIO A: EQUIPO '005' (MANTENIMIENTO EN PROGRESO)
-- ==============================================================
INSERT INTO Verifications (EquipmentUnitId, Date, Status, CreatedDate, ManagementId, PhysicalCondition) 
VALUES (@Unit1Id, '2026-04-20', 1, GETDATE(), @ManagementId, 1); 
SET @Verification1Id = SCOPE_IDENTITY();

INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result)
SELECT @Verification1Id, Id, CASE WHEN Id = 10 THEN 2 ELSE 1 END FROM VerificationCheckItems;

INSERT INTO VerificationFaults (VerificationId, Description, IsDeleted, CreatedDate) 
VALUES (@Verification1Id, 'El ventilador interno hace ruido metálico.', 0, GETDATE());

INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, Description, Priority, Status, CreatedDate, Type, ManagementId)
VALUES (@LabId, @Equipment1Id, @Unit1Id, 'Reparación de ventilador', 1, 1, GETDATE(), 1, @ManagementId); 
SET @Request1Id = SCOPE_IDENTITY();

INSERT INTO Maintenances (EquipmentUnitId, MaintenanceType, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, Status, CompletionPercentage, CreatedDate, ManagementId, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview)
VALUES (@Unit1Id, 1, 0, @PersonId, @Request1Id, '2026-04-29', '2026-04-29', 1, 50, GETDATE(), @ManagementId, 1, 1, 0, 0); 
SET @Maintenance1Id = SCOPE_IDENTITY();

INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, VerificationId, RequestId, MaintenanceId, CurrentPhase, CurrentState, PlannedDate, PlanStatus, CreatedDate)
VALUES (@ManagementId, @Unit1Id, @Verification1Id, @Request1Id, @Maintenance1Id, 3, 3, '2026-04-29', 1, GETDATE()); 

-- ==============================================================
-- ESCENARIO B: EQUIPO '006' (PLANIFICADO)
-- ==============================================================
INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, PlannedDate, PlanStatus, CreatedDate)
VALUES (@ManagementId, @Unit2Id, 0, 0, '2026-05-15', 0, GETDATE()); 

-- ==============================================================
-- ESCENARIO C: EQUIPO '007' (COMPLETADO)
-- ==============================================================
INSERT INTO Verifications (EquipmentUnitId, Date, Status, CreatedDate, ManagementId, PhysicalCondition) 
VALUES (@Unit3Id, '2026-04-10', 1, GETDATE(), @ManagementId, 1);
SET @Verification2Id = SCOPE_IDENTITY();

INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, Description, Priority, Status, CreatedDate, Type, ManagementId) 
VALUES (@LabId, @Equipment3Id, @Unit3Id, 'Mantenimiento Preventivo', 0, 1, GETDATE(), 0, @ManagementId);
SET @Request2Id = SCOPE_IDENTITY();

INSERT INTO Maintenances (EquipmentUnitId, MaintenanceType, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, EndDate, Status, CompletionPercentage, CreatedDate, ManagementId, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview)
VALUES (@Unit3Id, 0, 0, @PersonId, @Request2Id, '2026-04-28', '2026-04-28', '2026-04-28', 2, 100, GETDATE(), @ManagementId, 1, 1, 1, 1); 
SET @Maintenance2Id = SCOPE_IDENTITY();

INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, VerificationId, RequestId, MaintenanceId, CurrentPhase, CurrentState, PlannedDate, PlanStatus, CreatedDate)
VALUES (@ManagementId, @Unit3Id, @Verification2Id, @Request2Id, @Maintenance2Id, 3, 4, '2026-04-28', 2, GETDATE()); 

PRINT '>>> 4. Insertando Notificaciones (Admin Id=1)...';

-- NOTA: Si no existe el usuario Admin con ID=1, esto podría fallar. 
-- Usamos un bloque seguro que verifica si el Admin ID=1 existe primero.
IF EXISTS(SELECT 1 FROM Users WHERE Id = 1)
BEGIN
    INSERT INTO Notifications (UserId, Title, Message, IsRead, CreatedAt, IconClass, ActionUrl) VALUES 
    (1, 'L6: Falla Detectada', 'Se reportó una falla en el ventilador del Microondas (005).', 0, GETDATE(), 'bi bi-exclamation-circle text-warning', '/Verifications/Details?id=1'),
    (1, 'L8: Mantenimiento Completado', 'El mantenimiento de la Batidora (007) ha finalizado al 100%.', 0, GETDATE(), 'bi bi-check-circle text-success', '/Maintenances/Details?id=2');
END
ELSE
BEGIN
    PRINT '⚠️ El usuario Administrador no existe aún, saltando la inserción de notificaciones...';
END

PRINT '✅ === PROCESO COMPLETADO SATISFACTORIAMENTE ===';
GO
