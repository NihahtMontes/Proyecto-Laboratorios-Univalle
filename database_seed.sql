USE [DB_Laboratorios_Univalle_NN];
GO

-- =========================================================================================
-- SCRIPT DE SEMILLA (SEED) PARA DB_Laboratorios_Univalle_NN
-- Ejecuta este script SÓLO después de haber aplicado las migraciones (Update-Database).
-- =========================================================================================

-- Proteccion contra ejecucion accidental. Cambie a 1 solo sobre una base desechable y respaldada.
DECLARE @ConfirmDestructive BIT = 0;
IF DB_NAME() <> N'DB_Laboratorios_Univalle_NN'
    THROW 51000, 'Base incorrecta: este script solo admite DB_Laboratorios_Univalle_NN.', 1;
IF @ConfirmDestructive <> 1
    THROW 51001, 'Ejecucion detenida. Establezca @ConfirmDestructive = 1 despues de verificar un respaldo.', 1;

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

PRINT '>>> LIMPIANDO BASE DE DATOS (RESETEANDO TABLAS)... <<<';
-- 0. Deshabilitar temporalmente las validaciones de claves foráneas
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

-- Limpiar tablas de abajo hacia arriba (por dependencias)
DELETE FROM [ManagementPlans];
DELETE FROM [Departures];
DELETE FROM [Maintenances];
DELETE FROM [Requests];
DELETE FROM [VerificationFaults];
DELETE FROM [VerificationCheckResults];
DELETE FROM [Verifications];
DELETE FROM [EquipmentUnits];
DELETE FROM [Equipments];
DELETE FROM [Externs];
DELETE FROM [Interns];
DELETE FROM [People];
DELETE FROM [Laboratories];
DELETE FROM [Managements];
DELETE FROM [Careers];
DELETE FROM [Faculties];
DELETE FROM [Cities];
DELETE FROM [Countries];

-- Resetear los autoincrementables (Identities) a 0
DBCC CHECKIDENT ('[ManagementPlans]', RESEED, 0);
DBCC CHECKIDENT ('[Departures]', RESEED, 0);
DBCC CHECKIDENT ('[Maintenances]', RESEED, 0);
DBCC CHECKIDENT ('[Requests]', RESEED, 0);
DBCC CHECKIDENT ('[VerificationFaults]', RESEED, 0);
DBCC CHECKIDENT ('[VerificationCheckResults]', RESEED, 0);
DBCC CHECKIDENT ('[Verifications]', RESEED, 0);
DBCC CHECKIDENT ('[EquipmentUnits]', RESEED, 0);
DBCC CHECKIDENT ('[Equipments]', RESEED, 0);
DBCC CHECKIDENT ('[People]', RESEED, 0);
DBCC CHECKIDENT ('[Laboratories]', RESEED, 0);
DBCC CHECKIDENT ('[Managements]', RESEED, 0);
DBCC CHECKIDENT ('[Careers]', RESEED, 0);
DBCC CHECKIDENT ('[Faculties]', RESEED, 0);
DBCC CHECKIDENT ('[Cities]', RESEED, 0);
DBCC CHECKIDENT ('[Countries]', RESEED, 0);

-- Volver a habilitar las validaciones de claves foráneas
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
PRINT '>>> LIMPIEZA COMPLETADA. INICIANDO INSERCIÓN DE DATOS DEMO... <<<';


-- 1. Insertar Países y Ciudades
SET IDENTITY_INSERT [Countries] ON;
INSERT INTO [Countries] ([Id], [Name], [Status], [CreatedDate]) VALUES (1, 'Bolivia', 0, GETDATE());
SET IDENTITY_INSERT [Countries] OFF;

SET IDENTITY_INSERT [Cities] ON;
INSERT INTO [Cities] ([Id], [CountryId], [Name], [Region], [Status], [CreatedDate]) VALUES 
(1, 1, 'Cochabamba', 'Cercado', 0, GETDATE()),
(2, 1, 'La Paz', 'Murillo', 0, GETDATE());
SET IDENTITY_INSERT [Cities] OFF;

-- 2. Insertar Facultades y Carreras
SET IDENTITY_INSERT [Faculties] ON;
INSERT INTO [Faculties] ([Id], [Name], [Code], [Description], [Status], [CreatedDate]) VALUES 
(1, 'Facultad de Tecnología', 'FT-01', 'Tecnología e Ingenierías', 0, GETDATE()),
(2, 'Facultad de Ciencias de la Salud', 'FCS-01', 'Medicina, Enfermería y afines', 0, GETDATE()),
(3, 'Facultad de Gastronomía', 'FG-01', 'Gastronomía y Artes Culinarias', 0, GETDATE());
SET IDENTITY_INSERT [Faculties] OFF;

SET IDENTITY_INSERT [Careers] ON;
INSERT INTO [Careers] ([Id], [Name], [FacultadId], [Status], [CreatedDate]) VALUES 
(1, 'Ingeniería de Sistemas', 1, 0, GETDATE()),
(2, 'Ingeniería Biomédica', 1, 0, GETDATE()),
(3, 'Medicina', 2, 0, GETDATE()),
(4, 'Gastronomía Comercial', 3, 0, GETDATE());
SET IDENTITY_INSERT [Careers] OFF;

-- 3. Insertar Gestión Actual (Solo una, la actual 2026-1)
SET IDENTITY_INSERT [Managements] ON;
INSERT INTO [Managements] ([Id], [Year], [Semester], [Code], [Description], [StartDate], [PlannedEndDate], [Status], [Responsible], [CreatedDate]) VALUES 
(1, 2026, 1, '2026-1', 'Gestión Académica I-2026 (Demo)', '2026-02-01', '2026-07-31', 0, 'Admin', GETDATE());
SET IDENTITY_INSERT [Managements] OFF;

-- 4. Insertar Laboratorios
SET IDENTITY_INSERT [Laboratories] ON;
INSERT INTO [Laboratories] ([Id], [FacultyId], [Code], [Name], [Description], [Status], [CityId], [CreatedDate]) VALUES 
(1, 1, 'LAB-SYS-1', 'Laboratorio de Redes Avanzadas', 'Centro de simulación Cisco', 0, 1, GETDATE()),
(2, 3, 'LAB-GAS-1', 'Cocina Caliente', 'Laboratorio principal de Gastronomía', 0, 1, GETDATE()),
(3, 2, 'LAB-MED-1', 'Laboratorio de Anatomía', 'Morgue y simuladores biológicos', 0, 1, GETDATE());
SET IDENTITY_INSERT [Laboratories] OFF;

-- 5. Insertar Técnicos (Personas y Externos/Internos)
-- Category: Intern(0), Extern(1)
-- Status: Active(0)
SET IDENTITY_INSERT [People] ON;
INSERT INTO [People] ([Id], [Status], [Category], [Email], [PhoneNumber], [CreatedDate]) VALUES 
(1, 0, 0, 'tecnico.redes@univalle.edu', '71000001', GETDATE()),
(2, 0, 0, 'tecnico.gastronomia@univalle.edu', '71000002', GETDATE()),
(3, 0, 1, 'soporte@proveedor-medico.com', '71000003', GETDATE());
SET IDENTITY_INSERT [People] OFF;

INSERT INTO [Interns] ([Id], [Name], [InternStatus]) VALUES 
(1, 'Ing. Carlos Mendez (Sistemas)', 0),
(2, 'Chef Roberto Alanis (Gastronomía)', 0);

INSERT INTO [Externs] ([Id], [IsEntity], [Name], [Address], [ExternStatus]) VALUES 
(3, 1, 'Medical Equipments S.A.', 'Av. Blanco Galindo Km 4', 0);

-- 6. Insertar Equipos Base (Equipments)
-- Category: 0 (Electrónico), 1 (Manual), 3 (Medición), 6 (Utensilio)
-- UtensilType: 0 (None)
-- TypeClassification: 0 (Propio)
SET IDENTITY_INSERT [Equipments] ON;
INSERT INTO [Equipments] ([Id], [Category], [UtensilType], [TypeClassification], [Name], [Brand], [Model], [CreatedDate]) VALUES 
(1, 0, 0, 0, 'Router Cisco 2911', 'Cisco', '2911-K9', GETDATE()),
(2, 0, 0, 0, 'Microondas Industrial', 'Oster', 'M-2000', GETDATE()),
(3, 0, 0, 0, 'Horno Convector', 'Rational', 'iCombi Pro', GETDATE()),
(4, 3, 0, 0, 'Microscopio Electrónico', 'Olympus', 'CX-43', GETDATE()),
(5, 0, 0, 0, 'Servidor Blade', 'Dell', 'PowerEdge M1000e', GETDATE()),
(6, 6, 0, 0, 'Batidora Planetaria', 'KitchenAid', 'Artisan', GETDATE());
SET IDENTITY_INSERT [Equipments] OFF;

-- 7. Insertar Unidades de Equipos (EquipmentUnits)
-- CurrentStatus: 0 (Active), 1 (Maintenance), 2 (Inactive)
SET IDENTITY_INSERT [EquipmentUnits] ON;
INSERT INTO [EquipmentUnits] ([Id], [EquipmentId], [LaboratoryId], [InventoryNumber], [ManagementId], [CurrentStatus], [CreatedDate]) VALUES 
(1, 1, 1, 'INV-SYS-001', 1, 0, GETDATE()), -- Finalizado, verificado Good
(2, 2, 2, 'INV-GAS-005', 1, 1, GETDATE()), -- L-7 Solicitud (Esperando Mantenimiento)
(3, 3, 2, 'INV-GAS-006', 1, 1, GETDATE()), -- L-8 Mantenimiento (En Curso)
(4, 4, 3, 'INV-MED-010', 1, 1, GETDATE()), -- L-3 Salida (Terminó L8, esperando retornar)
(5, 5, 1, 'INV-SYS-002', 1, 1, GETDATE()), -- L-12 Adquisición (Desembolso)
(6, 6, 2, 'INV-GAS-007', 1, 0, GETDATE()); -- Terminado 100% (Completed)
SET IDENTITY_INSERT [EquipmentUnits] OFF;


-- =========================================================================================
-- WIZARD MANAGEMENT PLANS & WORKFLOWS
-- Fases: Verification=1, Request=2, Maintenance=3, Departure(Exit)=4, Kardex=5, Disbursement=6
-- Estados: PendingVerification=1, VerifiedGood=2, AwaitingRequest=3, AwaitingMaintenance=4, InMaintenance=5, AwaitingDeparture=6, AwaitingKardex=7, AwaitingDisbursement=8, Completed=9
-- =========================================================================================

-- EJEMPLO 1: Equipo 1 -> Aprobó L-6 sin fallas (Completed early)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(1, 1, 1, GETDATE(), 'Equipo en perfectas condiciones, ninguna falla', 0, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;
-- Sin VerificationFaults porque pasó L-6 limpio

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(1, 1, 1, 1, 1, 2, 2, GETDATE());
SET IDENTITY_INSERT [ManagementPlans] OFF;

--------------------------------------------------------------------------------------------
-- EJEMPLO 2: Equipo 2 -> L-6 Falló, tiene L-7 Solicitud (En L-7 / Esperando Mantenimiento L-8)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(2, 2, 1, GETDATE(), 'Microondas no calienta de forma uniforme', 2, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;

INSERT INTO [VerificationFaults] ([VerificationId], [Description], [IsDeleted], [CreatedDate]) VALUES (2, 'Magnetrón presenta fallas intermitentes', 0, GETDATE());

SET IDENTITY_INSERT [Requests] ON;
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [CreatedDate]) VALUES 
(1, 1, 2, 2, 2, 'Cambio de Magnetrón del microondas', 2, 0, 1, GETDATE()); -- Status=1(Approved), Type=0(Technical)
SET IDENTITY_INSERT [Requests] OFF;

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [RequestId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(2, 1, 2, 2, 1, 3, 4, 1, GETDATE()); -- Phase=3(Maintenance), State=4(AwaitingMaintenance), PlanStatus=1(InProgress)
SET IDENTITY_INSERT [ManagementPlans] OFF;


--------------------------------------------------------------------------------------------
-- EJEMPLO 3: Equipo 3 -> L-8 Mantenimiento en curso (Phase=3)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(3, 3, 1, GETDATE(), 'Horno no enciende', 3, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;

INSERT INTO [VerificationFaults] ([VerificationId], [Description], [IsDeleted], [CreatedDate]) VALUES (3, 'Falla en tarjeta electrónica principal', 0, GETDATE());

SET IDENTITY_INSERT [Requests] ON;
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [CreatedDate]) VALUES 
(2, 1, 3, 3, 2, 'Sustitución de tarjeta lógica del horno', 2, 0, 1, GETDATE()); 
SET IDENTITY_INSERT [Requests] OFF;

SET IDENTITY_INSERT [Maintenances] ON;
INSERT INTO [Maintenances] ([Id], [ManagementId], [EquipmentUnitId], [RequestId], [MaintenanceType], [ServiceType], [TechnicianId], [Status], [CompletionPercentage], [Step1_Cleaning], [Step2_Calibration], [Step3_Testing], [Step4_FinalReview], [CreatedDate]) VALUES 
(1, 1, 3, 2, 1, 0, 2, 2, 50, 1, 1, 0, 0, GETDATE()); -- Status=2(InProgress), 50%
SET IDENTITY_INSERT [Maintenances] OFF;

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [RequestId], [MaintenanceId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(3, 1, 3, 3, 2, 1, 3, 5, 1, GETDATE()); -- Phase=3, State=5(InMaintenance)
SET IDENTITY_INSERT [ManagementPlans] OFF;


--------------------------------------------------------------------------------------------
-- EJEMPLO 4: Equipo 4 -> L-3 Salida (Kardex Esperando Retorno - Phase=4 o 5)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(4, 4, 1, GETDATE(), 'Microscopio lente sucio y descalibrado', 2, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;

INSERT INTO [VerificationFaults] ([VerificationId], [Description], [IsDeleted], [CreatedDate]) VALUES (4, 'Descalibración óptica severa', 0, GETDATE());

SET IDENTITY_INSERT [Requests] ON;
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [CreatedDate]) VALUES 
(3, 1, 4, 4, 3, 'Mantenimiento Óptico Externo', 1, 0, 1, GETDATE()); 
SET IDENTITY_INSERT [Requests] OFF;

SET IDENTITY_INSERT [Maintenances] ON;
INSERT INTO [Maintenances] ([Id], [ManagementId], [EquipmentUnitId], [RequestId], [MaintenanceType], [ServiceType], [TechnicianId], [Status], [CompletionPercentage], [Step1_Cleaning], [Step2_Calibration], [Step3_Testing], [Step4_FinalReview], [CreatedDate]) VALUES 
(2, 1, 4, 3, 1, 1, 3, 3, 100, 1, 1, 1, 1, GETDATE()); -- Status=3(Completed), 100%, ServiceType=1(External)
SET IDENTITY_INSERT [Maintenances] OFF;

SET IDENTITY_INSERT [Departures] ON;
INSERT INTO [Departures] ([Id], [ManagementId], [EquipmentUnitId], [BorrowerId], [Type], [DepartureDate], [EstimatedReturnDate], [Status], [CreatedDate]) VALUES 
(1, 1, 4, 3, 0, GETDATE(), DATEADD(day, 5, GETDATE()), 0, GETDATE()); -- Borrower=Externo, Type=0(Out), Status=0(Active)
SET IDENTITY_INSERT [Departures] OFF;

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [RequestId], [MaintenanceId], [DepartureId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(4, 1, 4, 4, 3, 2, 1, 5, 7, 1, GETDATE()); -- Phase=5(Kardex Return), State=7(AwaitingKardex)
SET IDENTITY_INSERT [ManagementPlans] OFF;


--------------------------------------------------------------------------------------------
-- EJEMPLO 5: Equipo 5 -> L-12 Adquisición / Desembolso (Phase=6)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(5, 5, 1, GETDATE(), 'Servidor requiere repuesto de disco SAS urgentemente', 4, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;

INSERT INTO [VerificationFaults] ([VerificationId], [Description], [IsDeleted], [CreatedDate]) VALUES (5, 'Falla crítica de Disco Duro SAS 10K', 0, GETDATE());

SET IDENTITY_INSERT [Requests] ON;
-- Request 1: Técnica (L-7)
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [CreatedDate]) VALUES 
(4, 1, 5, 5, 1, 'Sustitución de disco servidor', 2, 0, 1, GETDATE()); 

-- Request 2: Adquisición / Compras (L-12)
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [InvestmentCode], [CreatedDate]) VALUES 
(5, 1, 5, 5, 1, 'Compra de Disco Duro SAS 1.2TB 10K para servidor Dell Blade', 2, 1, 1, 'INV-2026-001', GETDATE()); -- Type=1(Purchasing), Status=1(Approved)
SET IDENTITY_INSERT [Requests] OFF;

SET IDENTITY_INSERT [Maintenances] ON;
INSERT INTO [Maintenances] ([Id], [ManagementId], [EquipmentUnitId], [RequestId], [MaintenanceType], [ServiceType], [TechnicianId], [Status], [CompletionPercentage], [Step1_Cleaning], [Step2_Calibration], [Step3_Testing], [Step4_FinalReview], [CreatedDate]) VALUES 
(3, 1, 5, 4, 1, 0, 1, 3, 100, 1, 1, 1, 1, GETDATE()); -- Completed Internal Maintenance
SET IDENTITY_INSERT [Maintenances] OFF;

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [RequestId], [MaintenanceId], [AcquisitionRequestId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(5, 1, 5, 5, 4, 3, 5, 6, 8, 1, GETDATE()); -- Phase=6(Disbursement), State=8(AwaitingDisbursement)
SET IDENTITY_INSERT [ManagementPlans] OFF;


--------------------------------------------------------------------------------------------
-- EJEMPLO 6: Equipo 6 -> Flujo Completado al 100% (Completed)
SET IDENTITY_INSERT [Verifications] ON;
INSERT INTO [Verifications] ([Id], [EquipmentUnitId], [ManagementId], [Date], [Observations], [PhysicalCondition], [Status], [CreatedDate]) VALUES 
(6, 6, 1, GETDATE(), 'Mantenimiento Preventivo Normal de rutina', 1, 0, GETDATE());
SET IDENTITY_INSERT [Verifications] OFF;

INSERT INTO [VerificationFaults] ([VerificationId], [Description], [IsDeleted], [CreatedDate]) VALUES (6, 'Engrase y limpieza profunda requerida', 0, GETDATE());

SET IDENTITY_INSERT [Requests] ON;
INSERT INTO [Requests] ([Id], [ManagementId], [EquipmentId], [EquipmentUnitId], [LaboratoryId], [Description], [Priority], [Type], [Status], [CreatedDate]) VALUES 
(6, 1, 6, 6, 2, 'Mantenimiento Preventivo de Batidora', 0, 0, 1, GETDATE()); 
SET IDENTITY_INSERT [Requests] OFF;

SET IDENTITY_INSERT [Maintenances] ON;
INSERT INTO [Maintenances] ([Id], [ManagementId], [EquipmentUnitId], [RequestId], [MaintenanceType], [ServiceType], [TechnicianId], [Status], [CompletionPercentage], [Step1_Cleaning], [Step2_Calibration], [Step3_Testing], [Step4_FinalReview], [CreatedDate]) VALUES 
(4, 1, 6, 6, 0, 0, 2, 3, 100, 1, 1, 1, 1, GETDATE()); 
SET IDENTITY_INSERT [Maintenances] OFF;

SET IDENTITY_INSERT [ManagementPlans] ON;
INSERT INTO [ManagementPlans] ([Id], [ManagementId], [EquipmentUnitId], [VerificationId], [RequestId], [MaintenanceId], [CurrentPhase], [CurrentState], [PlanStatus], [CreatedDate]) VALUES 
(6, 1, 6, 6, 6, 4, 6, 9, 2, GETDATE()); -- Phase=6(Exit), State=9(Completed), PlanStatus=2(Completed)
SET IDENTITY_INSERT [ManagementPlans] OFF;

-- =========================================================================================
-- FIN DEL SCRIPT DE SEMILLA
-- =========================================================================================
    COMMIT TRANSACTION;
PRINT '>>> SEMILLA DE DATOS DEMO APLICADA CON ÉXITO <<<';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
