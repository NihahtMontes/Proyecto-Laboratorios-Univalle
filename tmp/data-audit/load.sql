SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @CreatedById int = 1;
DECLARE @Today datetime2 = SYSUTCDATETIME();

DECLARE @FacultyMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);
DECLARE @CareerMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);
DECLARE @LabMap TABLE ([Key] nvarchar(100) PRIMARY KEY, Id int NOT NULL);
DECLARE @PersonMap TABLE ([Key] nvarchar(300) PRIMARY KEY, Id int NOT NULL);
DECLARE @ManagementMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL);
DECLARE @EquipmentMap TABLE ([Key] nvarchar(700) PRIMARY KEY, Id int NOT NULL);
DECLARE @UnitMap TABLE ([Key] nvarchar(50) PRIMARY KEY, Id int NOT NULL, EquipmentId int NOT NULL, LaboratoryId int NULL);
DECLARE @RequestMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);
DECLARE @MaintenanceMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);
DECLARE @DepartureMap TABLE ([Key] nvarchar(120) PRIMARY KEY, Id int NOT NULL);

-- Faculty: Facultad de Gastronomia y Turismo - Carrera de Gastronomía
DECLARE @FacultyId int;
SELECT @FacultyId = Id FROM Faculties WHERE Name = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
IF @FacultyId IS NULL
BEGIN
    INSERT INTO Faculties (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía', N'GASTRO', N'Facultad que administra laboratorios de cocina H-1 a H-7', 0, @Today, @CreatedById);
    SET @FacultyId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía', @FacultyId);

-- Faculty: Facultad de Ciencias de la Salud - Carrera de Medicina
DECLARE @FacultyId int;
SELECT @FacultyId = Id FROM Faculties WHERE Name = N'Facultad de Ciencias de la Salud - Carrera de Medicina';
IF @FacultyId IS NULL
BEGIN
    INSERT INTO Faculties (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Medicina', N'MEDIC', N'Administra laboratorios clínicos en Edificio América', 0, @Today, @CreatedById);
    SET @FacultyId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Medicina', @FacultyId);

-- Faculty: Facultad de Ciencias de la Salud - Carrera de Nutricion
DECLARE @FacultyId int;
SELECT @FacultyId = Id FROM Faculties WHERE Name = N'Facultad de Ciencias de la Salud - Carrera de Nutricion';
IF @FacultyId IS NULL
BEGIN
    INSERT INTO Faculties (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Nutricion', NULL, NULL, 0, @Today, @CreatedById);
    SET @FacultyId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Nutricion', @FacultyId);

-- Career: Gastronomía
DECLARE @CareerId int;
DECLARE @CareerFacultyId int = NULL;
SELECT @CareerFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @CareerId = Id FROM Careers WHERE Name = N'Gastronomía';
IF @CareerId IS NULL
BEGIN
    INSERT INTO Careers (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (N'Gastronomía', @CareerFacultyId, 0, @Today, @CreatedById);
    SET @CareerId = CONVERT(int, SCOPE_IDENTITY());
END
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Gastronomía') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Gastronomía', @CareerId);

-- Career: Turismo y Hoteleria
DECLARE @CareerId int;
DECLARE @CareerFacultyId int = NULL;
SELECT @CareerFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @CareerId = Id FROM Careers WHERE Name = N'Turismo y Hoteleria';
IF @CareerId IS NULL
BEGIN
    INSERT INTO Careers (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (N'Turismo y Hoteleria', @CareerFacultyId, 0, @Today, @CreatedById);
    SET @CareerId = CONVERT(int, SCOPE_IDENTITY());
END
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Turismo y Hoteleria') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Turismo y Hoteleria', @CareerId);

-- Career: Nutricion y Dietetica
DECLARE @CareerId int;
DECLARE @CareerFacultyId int = NULL;
SELECT @CareerFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Ciencias de la Salud - Carrera de Nutricion';
SELECT @CareerId = Id FROM Careers WHERE Name = N'Nutricion y Dietetica';
IF @CareerId IS NULL
BEGIN
    INSERT INTO Careers (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (N'Nutricion y Dietetica', @CareerFacultyId, 0, @Today, @CreatedById);
    SET @CareerId = CONVERT(int, SCOPE_IDENTITY());
END
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Nutricion y Dietetica') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Nutricion y Dietetica', @CareerId);

-- Career: Ciencias Gastronomicas
DECLARE @CareerId int;
DECLARE @CareerFacultyId int = NULL;
SELECT @CareerFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @CareerId = Id FROM Careers WHERE Name = N'Ciencias Gastronomicas';
IF @CareerId IS NULL
BEGIN
    INSERT INTO Careers (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (N'Ciencias Gastronomicas', @CareerFacultyId, 0, @Today, @CreatedById);
    SET @CareerId = CONVERT(int, SCOPE_IDENTITY());
END
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Ciencias Gastronomicas') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Ciencias Gastronomicas', @CareerId);

-- Laboratory: H-1
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-1';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-1', N'Laboratorio de Cocina H-1', N'Campus Tiquipaya', N'Cocina Industrial', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-1', @LabId);

-- Laboratory: H-2
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-2';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-2', N'Laboratorio de Cocina H-2', N'Campus Tiquipaya', N'Cocina Industrial', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-2', @LabId);

-- Laboratory: H-4
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-4';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-4', N'Laboratorio de Cocina H-4', N'Campus Tiquipaya', N'Cocina Industrial', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-4', @LabId);

-- Laboratory: H-5
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-5';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-5', N'Laboratorio de Cocina H-5', N'Campus Tiquipaya', N'Cocina Industrial', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-5', @LabId);

-- Laboratory: H-6
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-6';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-6', N'Laboratorio de Cocina H-6', N'Campus Tiquipaya', N'Cocina Industrial', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-6', @LabId);

-- Laboratory: H-3
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-3';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-3', N'Laboratorio Panadería y Pastelería H-3', N'Campus Tiquipaya', N'Panadería | Laboratorio Area Caliente', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-3', @LabId);

-- Laboratory: H-7
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-7';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-7', N'Laboratorio de Cocina H-7', N'Campus Tiquipaya', N'Cocina Industrial | Laboratorio Frio', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-7', @LabId);

-- Laboratory: K-1
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'K-1';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'K-1', N'Laboratorio K-1 Cocteleria', N'Campus Tiquipaya', N'Cocteleria | Cocteleria y Barismo', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'K-1', @LabId);

-- Laboratory: CIRCULACION
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'CIRCULACION';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'CIRCULACION', N'Área de Circulación General', N'Campus Tiquipaya', N'Área común | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'CIRCULACION', @LabId);

-- Laboratory: OFICINAS 1ER PISO
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'OFICINAS 1ER PISO';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'OFICINAS 1ER PISO', N'Área de Circulación General', N'Campus Tiquipaya', N'Oficinas | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'OFICINAS 1ER PISO', @LabId);

-- Laboratory: OFICINAS PLANTA BAJA
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'OFICINAS PLANTA BAJA';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'OFICINAS PLANTA BAJA', N'Área de Circulación General', N'Campus Tiquipaya', N'Oficinas | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'OFICINAS PLANTA BAJA', @LabId);

-- Laboratory: H-102
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-102';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-102', N'Área de Circulación General', N'Campus Tiquipaya', N'Área común | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-102', @LabId);

-- Laboratory: H-103
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-103';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-103', N'Área de Circulación General', N'Campus Tiquipaya', N'Área común | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-103', @LabId);

-- Laboratory: H-104
DECLARE @LabId int;
DECLARE @LabFacultyId int;
SELECT @LabFacultyId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
SELECT @LabId = Id FROM Laboratories WHERE Code = N'H-104';
IF @LabId IS NULL
BEGIN
    INSERT INTO Laboratories (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@LabFacultyId, N'H-104', N'Área de Circulación General', N'Campus Tiquipaya', N'Área común | Pasillos y áreas comunes', 0, @Today, @CreatedById);
    SET @LabId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @LabMap ([Key], Id) VALUES (N'H-104', @LabId);

-- Person: BRUNO NOVILLO
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'BRUNO NOVILLO';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 1, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'BRUNO NOVILLO', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'BRUNO NOVILLO', @PersonId);

-- Person: CASATERMO SRL
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = N'CASATERMO SRL';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 5, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@PersonId, 1, N'CASATERMO SRL', N'Cochabamba', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'CASATERMO SRL', @PersonId);

-- Person: CONSULTORA PROYECTOS Y SERVICIOS PCS
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = N'CONSULTORA PROYECTOS Y SERVICIOS PCS';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 5, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@PersonId, 1, N'CONSULTORA PROYECTOS Y SERVICIOS PCS', N'Cochabamba', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'CONSULTORA PROYECTOS Y SERVICIOS PCS', @PersonId);

-- Person: DOCENTE MARTINEZ
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'DOCENTE MARTINEZ';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 2, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'DOCENTE MARTINEZ', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'DOCENTE MARTINEZ', @PersonId);

-- Person: ING. APAZA
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. APAZA';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 1, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'ING. APAZA', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. APAZA', @PersonId);

-- Person: ING. EVER HERBAS
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. EVER HERBAS';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 1, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'ING. EVER HERBAS', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. EVER HERBAS', @PersonId);

-- Person: ING. SARA PEREZ YAÑEZ
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. SARA PEREZ YAÑEZ';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 99, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'ING. SARA PEREZ YAÑEZ', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. SARA PEREZ YAÑEZ', @PersonId);

-- Person: TALLER UNIVALLE
DECLARE @PersonId int;
SELECT @PersonId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'TALLER UNIVALLE';
IF @PersonId IS NULL
BEGIN
    INSERT INTO People (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, 1, NULL, NULL, @Today, @CreatedById);
    SET @PersonId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@PersonId, N'TALLER UNIVALLE', 0);
END
INSERT INTO @PersonMap ([Key], Id) VALUES (N'TALLER UNIVALLE', @PersonId);

-- Management: 2023-1
DECLARE @ManagementId int;
SELECT @ManagementId = Id FROM Managements WHERE Year = 2023 AND Semester = 1 AND Type = 0;
IF @ManagementId IS NULL
BEGIN
    INSERT INTO Managements (Year, Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (2023, 1, N'2023-1', N'Gestion historica 2023-1', 0, N'Carga historica', 0, @Today, @CreatedById);
    SET @ManagementId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2023-1', @ManagementId);

-- Management: 2023-2
DECLARE @ManagementId int;
SELECT @ManagementId = Id FROM Managements WHERE Year = 2023 AND Semester = 2 AND Type = 0;
IF @ManagementId IS NULL
BEGIN
    INSERT INTO Managements (Year, Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (2023, 2, N'2023-2', N'Gestion historica 2023-2', 0, N'Carga historica', 0, @Today, @CreatedById);
    SET @ManagementId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2023-2', @ManagementId);

-- Management: 2025-1
DECLARE @ManagementId int;
SELECT @ManagementId = Id FROM Managements WHERE Year = 2025 AND Semester = 1 AND Type = 0;
IF @ManagementId IS NULL
BEGIN
    INSERT INTO Managements (Year, Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (2025, 1, N'2025-1', N'Gestion historica 2025-1', 0, N'Carga historica', 0, @Today, @CreatedById);
    SET @ManagementId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2025-1', @ManagementId);

-- Equipment: ABATIDOR FASTER
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ABATIDOR FASTER' AND ISNULL(Brand, N'') = ISNULL(N'AFINOX', N'') AND ISNULL(Model, N'') = ISNULL(N'FASTER 5T GF 230V', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ABATIDOR FASTER', N'AFINOX', N'FASTER 5T GF 230V', NULL, N'ABATIDOR FASTER REFRIGERANTE 5 BANDEJAS , MATERIAL: ACERO INOXIDABLE, COLOR: MARFIL/BLANCO, MARCA: AFINOX, MODELO: FASTER 5T GF 230V, SERIE: 3016085604', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V', @EquipmentId);

-- Equipment: ACCES POINT
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ACCES POINT' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP370', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ACCES POINT', N'CISCO', N'AIR-CAP370', NULL, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9 , SERIE: SFTX1927S0UL', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ACCES POINT|CISCO|AIR-CAP370', @EquipmentId);

-- Equipment: ACCES POINT
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ACCES POINT' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP3702E-A-K9', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ACCES POINT', N'CISCO', N'AIR-CAP3702E-A-K9', NULL, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9, SERIE: SFTX1927S0VR', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9', @EquipmentId);

-- Equipment: ALL IN ONE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'HP', N'') AND ISNULL(Model, N'') = ISNULL(N'24-E015LA', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ALL IN ONE', N'HP', N'24-E015LA', NULL, N'ALL IN ONE MEMORIA 8GB, DISCO DURO 1 TB, COPIADOR DE DVD, PANTALLA DE 23 PULG , COLOR: BLANCO , MARCA: HP, MODELO: 24-E015LA, SERIE: 8CC80516KV', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|HP|24-E015LA', @EquipmentId);

-- Equipment: ALL IN ONE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'10BB-A0C900', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ALL IN ONE', N'LENOVO', N'10BB-A0C900', NULL, N'ALL IN ONE 500 GB D.D. - 4.00 GB DE RAM, CON MOUSE OPTICO, GRABADOR DE CD/DVD , CAPACIDAD: CORE I5 - 2.90 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: 10BB-A0C900, SERIE: MJ00UZ5R', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|LENOVO|10BB-A0C900', @EquipmentId);

-- Equipment: ALL IN ONE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE M73Z', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ALL IN ONE', N'LENOVO', N'THINK CENTRE M73Z', NULL, N'ALL IN ONE 500 GB D.D. 4.00 GB DE RAM,GRABADOR DE DVD, MOUSE OPTICO. , CAPACIDAD: CORE I5-2.40 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE M73Z', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|LENOVO|THINK CENTRE M73Z', @EquipmentId);

-- Equipment: AMASADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'AMASADORA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'AMASADORA', N'WILDA', NULL, NULL, N'AMAZADORA ESPIRAL DE DOBLE VELOCIDAD, CON CAJA DE CONTROL, FUENTE DE AMASADO INOX , CAPACIDAD: 80 LITROS, COLOR: BLANCO/METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'AMASADORA|WILDA|', @EquipmentId);

-- Equipment: APARATO TELEFONICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'APARATO TELEFONICO' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'CP-3905', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'APARATO TELEFONICO', N'CISCO', N'CP-3905', NULL, N'APARATO TELEFONICO CON CABLES DE CONECCION , MATERIAL: PLASTICO, COLOR: NEGRO, MARCA: CISCO, MODELO: CP-3905, SERIE: FCH2045GJVT', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'APARATO TELEFONICO|CISCO|CP-3905', @EquipmentId);

-- Equipment: BACHA DE LAVADO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BACHA DE LAVADO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BACHA DE LAVADO', NULL, NULL, NULL, N'BACHA DE LAVADO CON SOPORTE ADPATADO , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BACHA DE LAVADO||', @EquipmentId);

-- Equipment: BALANZA ELECTRONICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BALANZA ELECTRONICA', NULL, NULL, NULL, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713783', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA||', @EquipmentId);

-- Equipment: BALANZA ELECTRONICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(N'ELECTRONIC SCALE', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BALANZA ELECTRONICA', N'ELECTRONIC SCALE', NULL, NULL, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713831', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA|ELECTRONIC SCALE|', @EquipmentId);

-- Equipment: BALANZA ELECTRONICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(N'OHAUS', N'') AND ISNULL(Model, N'') = ISNULL(N'RANGER R31P30', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BALANZA ELECTRONICA', N'OHAUS', N'RANGER R31P30', NULL, N'BALANZA ELECTRONICA DE 30 KG X 1 GR , MATERIAL: METAL, COLOR: PLOMO/AZUL ACERO , MARCA: OHAUS, MODELO: RANGER R31P30, SERIE: 8336510968', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30', @EquipmentId);

-- Equipment: BANCA DE MADERA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BANCA DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BANCA DE MADERA', NULL, NULL, NULL, N'BANCA , MATERIAL: MADERA, MEDIDAS: 42X180X30 cm., COLOR: BLANCO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BANCA DE MADERA||', @EquipmentId);

-- Equipment: BATIDORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'') AND ISNULL(Model, N'') = ISNULL(N'5KSM7591', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BATIDORA', N'KITCHENAID', N'5KSM7591', NULL, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467699', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|KITCHENAID|5KSM7591', @EquipmentId);

-- Equipment: BATIDORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'') AND ISNULL(Model, N'') = ISNULL(N'5KSM7591,', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BATIDORA', N'KITCHENAID', N'5KSM7591,', NULL, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467732', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|KITCHENAID|5KSM7591,', @EquipmentId);

-- Equipment: BATIDORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BATIDORA', N'OSTER', NULL, NULL, N'BATIDORA BOL DE ACERO INOX, CON 2 ASPAS , MATERIAL: IMOX, MARCA: OSTER', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|OSTER|', @EquipmentId);

-- Equipment: BEBEDERO DE AGUA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'BEBEDERO DE AGUA' AND ISNULL(Brand, N'') = ISNULL(N'IBBL', N'') AND ISNULL(Model, N'') = ISNULL(N'BAG 40', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'BEBEDERO DE AGUA', N'IBBL', N'BAG 40', NULL, N'BEBEDERO DE AGUA CON FILTRO , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 98X31X31 cm., COLOR: AZUL ELECTRICO, MARCA: IBBL, MODELO: BAG 40, SERIE: 451P287128', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BEBEDERO DE AGUA|IBBL|BAG 40', @EquipmentId);

-- Equipment: CAFETERA IND
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAFETERA IND' AND ISNULL(Brand, N'') = ISNULL(N'ASTORIA', N'') AND ISNULL(Model, N'') = ISNULL(N'INDUS. ITALIANA', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAFETERA IND', N'ASTORIA', N'INDUS. ITALIANA', NULL, N'CAFETERA INDUSTRIAL SEMIAUTOMATICO,MANDO MANUAL CON DOTONES ELECTRONICOS, CARROCERIA CON PANELES DEACERO INOX Y ACERO, MOTOBOMBA INCORPORADA, DOS LANZAS DE VAPOR , MATERIAL: INOX/ACERO, CAPACIDAD: 10,5, MEDIDAS: 70X54X52 cm., COLOR: NEGRO, MARCA: ASTORIA, MODELO: INDUS. ITALIANA, SERIE: 913677', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAFETERA IND|ASTORIA|INDUS. ITALIANA', @EquipmentId);

-- Equipment: CAMARA CONSERVADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMARA CONSERVADORA' AND ISNULL(Brand, N'') = ISNULL(N'ASBER', N'') AND ISNULL(Model, N'') = ISNULL(N'ARR-43', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMARA CONSERVADORA', N'ASBER', N'ARR-43', NULL, N'CAMARA CONSERVADORA CON DOS PUERTAS , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 220V 50/60HZ, COLOR: INOX, MARCA: ASBER, MODELO: ARR-43, SERIE: 11090021M', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA CONSERVADORA|ASBER|ARR-43', @EquipmentId);

-- Equipment: CAMARA DE VIDEO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMARA DE VIDEO', NULL, NULL, NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, IR DOMO,1/3 SONY ICX633+NEXTCHIP-480 TVL IR LED: E5X24PCS,LENTE 3.6 MM FUENTE DC12V 500 MA BALUMS , COLOR: BLANCO, MARCA: DESL', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO||', @EquipmentId);

-- Equipment: CAMARA DE VIDEO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'') AND ISNULL(Model, N'') = ISNULL(N'DS-2CEE55A2N-IRN', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMARA DE VIDEO', N'HIKVISION', N'DS-2CEE55A2N-IRN', NULL, N'CAMARA DE VIDEO DOMO DOMO ANTIVANDALICA VERDADERO (ICR) C/IR 1.3" HIGH RESOL IP66 ALTA RESOLUCION: 700TVL/01 LUX. BALUMS TRANSMISOR DE SEÑAL, FUENTE DE PODER. , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS-2CEE55A2N-IRN, SERIE: 457428659', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN', @EquipmentId);

-- Equipment: CAMARA DE VIDEO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'') AND ISNULL(Model, N'') = ISNULL(N'DS2CC5192N-IR1', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMARA DE VIDEO', N'HIKVISION', N'DS2CC5192N-IR1', NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS2CC5192N-IR1', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1', @EquipmentId);

-- Equipment: CAMARA DE VIDEO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'SONY BALUMS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMARA DE VIDEO', N'SONY BALUMS', NULL, NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, ANTIBANDALICAIR , CAPACIDAD: 480 TVL 1/3 " , COLOR: BLANCO , MARCA: SONY BALUMS', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|SONY BALUMS|', @EquipmentId);

-- Equipment: CAMPANA DE EXTRACCION
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMPANA DE EXTRACCION' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMPANA DE EXTRACCION', NULL, NULL, NULL, N'CAMPANA CENTRAL DE EXTRACCION FILTROS DESMONTABLES FOCOS Y TOMA DE CORRIENTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 120X120 cm., COLOR: METALICO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMPANA DE EXTRACCION||', @EquipmentId);

-- Equipment: CAMPANA DE EXTRACCION
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CAMPANA DE EXTRACCION' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CAMPANA DE EXTRACCION', N'WILDA', NULL, NULL, N'CAMPANA DE EXTRACCIÓN , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 320X120 cm., COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMPANA DE EXTRACCION|WILDA|', @EquipmentId);

-- Equipment: CARRITO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CARRITO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CARRITO', NULL, NULL, NULL, N'CARRITO PORTA GARRAFAS, CON DOS RUEDAS , MATERIAL: METALICO , COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CARRITO||', @EquipmentId);

-- Equipment: CASILLERO METALICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CASILLERO METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CASILLERO METALICO', NULL, NULL, NULL, N'CASILLERO , MATERIAL: METAL, MEDIDAS: 180X80X43 cm., BANDEJAS/DIVISIONES: 8 PUERTAS, COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CASILLERO METALICO||', @EquipmentId);

-- Equipment: COCINA INDUSTRIAL
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'COCINA INDUSTRIAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'COCINA INDUSTRIAL', N'WILDA', NULL, NULL, N'COCINA INDUSTRIAL GASTRONOMICA A GAS, DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'COCINA INDUSTRIAL|WILDA|', @EquipmentId);

-- Equipment: COCINA INDUSTRIAL
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'COCINA INDUSTRIAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'AISI304L 2B', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'COCINA INDUSTRIAL', N'WILDA', N'AISI304L 2B', NULL, N'COCINA CENTRAL GASTRONOMICA INDUSTRIAL DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X92X92 cm., COLOR: METALICO, MARCA: WILDA, MODELO: AISI304L 2B', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'COCINA INDUSTRIAL|WILDA|AISI304L 2B', @EquipmentId);

-- Equipment: CPU DE ESCRITORIO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CPU DE ESCRITORIO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CPU DE ESCRITORIO', N'LENOVO', N'THINK CENTRE', NULL, N'CPU 500 GB DD, 4.00 GB DE RAM, GRABADOR DE DVD, MOUSE OPTICO , CAPACIDAD: CORE I5 - 3.10 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE, SERIE: MJNHDWZ', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE', @EquipmentId);

-- Equipment: CREDENZA DE MELAMINA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'CREDENZA DE MELAMINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'CREDENZA DE MELAMINA', NULL, NULL, NULL, N'CREDENZA CON 6 MODULOS (DE 3 UNID. C/U) DE ARCHIVOS, 4 PUERTAS CON CHAPAS Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X40,5X93 cm., BANDEJAS/DIVISIONES: DON 2 DIVISIONES REGULABLES, COLOR: WENGUE/NEGRO/PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CREDENZA DE MELAMINA||', @EquipmentId);

-- Equipment: ENVASADORA AL VACIO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ENVASADORA AL VACIO' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ENVASADORA AL VACIO', N'VENTUS', NULL, NULL, N'ENVASADORA AL VACIO DE 310MM VSV310, CON BOMBA DE VACIO DE 4 M3/H , MARCA: VENTUS, SERIE: 202031007', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ENVASADORA AL VACIO|VENTUS|', @EquipmentId);

-- Equipment: ESCRITORIO DE MADERA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESCRITORIO DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESCRITORIO DE MADERA', NULL, NULL, NULL, N'ESCRITORIO CON 2 CHAPAS (CON LLAVES) , MATERIAL: MADERA, MEDIDAS: 78X120X63 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO DE MADERA||', @EquipmentId);

-- Equipment: ESCRITORIO DE MELAMINA MODULAR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESCRITORIO DE MELAMINA MODULAR' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESCRITORIO DE MELAMINA MODULAR', NULL, NULL, NULL, N'ESCRITORIO DE MELAMINA FLOW OPERATIVO, PORTA TECLADO , CON PASA CABLE, PATAS DE ALUMIO ANODIZADO , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X180X74 cm., COLOR: WENGUE/NEGRO/PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO DE MELAMINA MODULAR||', @EquipmentId);

-- Equipment: ESCRITORIO METALICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESCRITORIO METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESCRITORIO METALICO', NULL, NULL, NULL, N'ESCRITORIO METALICO CON CUERINA Y VIDRIO TRIPLE , MATERIAL: METAL Y VIDRIO, MEDIDAS: 79X120X70 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: BEIGE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO METALICO||', @EquipmentId);

-- Equipment: ESTANTE BAR DE MADERA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESTANTE BAR DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESTANTE BAR DE MADERA', NULL, NULL, NULL, N'ESTANTE BAR (CON 4 FOCOS) , MATERIAL: MADERA, MEDIDAS: 227X431X60 cm., BANDEJAS/DIVISIONES: 12 PUERTAS, COLOR: CAFE CLARO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE BAR DE MADERA||', @EquipmentId);

-- Equipment: ESTANTE METÁLICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESTANTE METÁLICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESTANTE METÁLICO', NULL, NULL, NULL, N'ESTANTE TIPO MECANO , MATERIAL: METAL, MEDIDAS: 200X90X30 cm., BANDEJAS/DIVISIONES: 5 BANDEJAS, COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE METALICO||', @EquipmentId);

-- Equipment: ESTANTE PORTA BANDEJAS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ESTANTE PORTA BANDEJAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ESTANTE PORTA BANDEJAS', NULL, NULL, NULL, N'ESTANETE PORTA BAMBEJAS CON RODAPIES, PARA BANDEJAS DE PANADERIA , MATERIAL: METALICO , COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE PORTA BANDEJAS||', @EquipmentId);

-- Equipment: EXTINTOR TIPO K
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR TIPO K' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR TIPO K', NULL, NULL, NULL, N'EXTINTOR TIPO K DE 10 LTS, AGENTE ACETATO DE POTASIO (ACERO INOXIDABLE)EXTINFIRE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR TIPO K||', @EquipmentId);

-- Equipment: EXTINTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'ABC', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR', N'ABC', NULL, NULL, N'EXTINTOR DE 8 KG , MARCA: ABC', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|ABC|', @EquipmentId);

-- Equipment: EXTINTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'CYLINDERS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR', N'CYLINDERS', NULL, NULL, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|CYLINDERS|', @EquipmentId);

-- Equipment: EXTINTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR', N'FANACIM', NULL, NULL, N'EXTINTOR ABC , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: FANACIM', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|FANACIM|', @EquipmentId);

-- Equipment: EXTINTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'MMB CILINDERS', N'') AND ISNULL(Model, N'') = ISNULL(N'FNC 10', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR', N'MMB CILINDERS', N'FNC 10', NULL, N'EXTINTOR ABC, CON MANGUERA , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: MMB CILINDERS, MODELO: FNC 10', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|MMB CILINDERS|FNC 10', @EquipmentId);

-- Equipment: EXTINTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'MMB CYLINDERS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTINTOR', N'MMB CYLINDERS', NULL, NULL, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|MMB CYLINDERS|', @EquipmentId);

-- Equipment: EXTRACTOR DE AIRE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE AIRE' AND ISNULL(Brand, N'') = ISNULL(N'LOREN SID', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTRACTOR DE AIRE', N'LOREN SID', NULL, NULL, N'EXTRACTOR DE AIRE CIRCULAR DE 4 ASPAS , MATERIAL: METALICO, CAPACIDAD: 1200 M3/H, MEDIDAS: 30 cm., COLOR: PLOMO, MARCA: LOREN SID', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE AIRE|LOREN SID|', @EquipmentId);

-- Equipment: EXTRACTOR DE HUMOS Y GRASAS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTRACTOR DE HUMOS Y GRASAS', NULL, NULL, NULL, N'TODO EL CAMPO DE DESCRIPCION DETALLADA DEL ACTIVO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE HUMOS Y GRASAS||', @EquipmentId);

-- Equipment: EXTRACTOR DE HUMOS Y GRASAS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'EXTRACTOR DE HUMOS Y GRASAS', N'WILDA', NULL, NULL, N'EXTRACTOR DE HUMOS Y GRASAS CON SISTEMA AUTOLIMPIANTE POR CENTRIFUGACION DE ASPAS, CON FILTROS DESMONTABLES, COLECTOR Y DEPURADOR DE GRASAS, CON MOTOR DE 3 HP , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|', @EquipmentId);

-- Equipment: FERMENTADOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'FERMENTADOR' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'FERMENTADOR', N'WILDA', N'A155 304L2B', NULL, N'FERMENTADOR ELECTRICO PARA 32 BANDEJAS , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 2 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FERMENTADOR|WILDA|A155 304L2B', @EquipmentId);

-- Equipment: FLITRO ABLANDADOR DE AGUA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'FLITRO ABLANDADOR DE AGUA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'FLITRO ABLANDADOR DE AGUA', NULL, NULL, NULL, N'FILTRO ABLANDADOR DE AGUA PARA CAFETERA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FLITRO ABLANDADOR DE AGUA||', @EquipmentId);

-- Equipment: FREIDORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'FREIDORA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'FREIDORA', N'WILDA', NULL, NULL, N'FREIDORA CONEXIÓN A GAS, ELECTRICA, DE 2 CANASTILLOS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FREIDORA|WILDA|', @EquipmentId);

-- Equipment: GARRAFA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'GARRAFA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'GARRAFA', NULL, NULL, NULL, N'GARRAFA DE GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GARRAFA||', @EquipmentId);

-- Equipment: GARRAFA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'GARRAFA' AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'GARRAFA', N'FANACIM', NULL, NULL, N'GARRAFA PARA GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO, MARCA: FANACIM', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GARRAFA|FANACIM|', @EquipmentId);

-- Equipment: GAVETERO DE MELAMINA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'GAVETERO DE MELAMINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'GAVETERO DE MELAMINA', NULL, NULL, NULL, N'GAVETERO MOVIL, CON CHAPA Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 75X46X42 cm., BANDEJAS/DIVISIONES: CON 3 CAJONES, COLOR: WENGUE/NEGRO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GAVETERO DE MELAMINA||', @EquipmentId);

-- Equipment: HORNO CONVECTOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'HORNO CONVECTOR' AND ISNULL(Brand, N'') = ISNULL(N'ARIANNA', N'') AND ISNULL(Model, N'') = ISNULL(N'XEFT-04HS-ELDV', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'HORNO CONVECTOR', N'ARIANNA', N'XEFT-04HS-ELDV', NULL, N'HORNO CONVECTOR ELECTRICO CONTROL LED , MATERIAL: INOX, MEDIDAS: 460X330 cm., MARCA: ARIANNA, MODELO: XEFT-04HS-ELDV, SERIE: 2021L0105834', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV', @EquipmentId);

-- Equipment: HORNO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'HORNO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'HORNO', N'WILDA', NULL, NULL, N'HORNO GASTRONOMICO DE 6 BANDEJAS, CON FERMENTADOR, (CON MOTOR MARCA WEG TRIFASICO) , COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'HORNO|WILDA|', @EquipmentId);

-- Equipment: LAVAPLATOS DE ACERO INOXIDABLE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'LAVAPLATOS DE ACERO INOXIDABLE', NULL, NULL, NULL, N'LAVAPLATOS SANITARIO DE UNA BACHA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 80X50X130 cm., COLOR: PLATEADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LAVAPLATOS DE ACERO INOXIDABLE||', @EquipmentId);

-- Equipment: LAVAPLATOS DE ACERO INOXIDABLE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'LAVAPLATOS DE ACERO INOXIDABLE', N'WILDA', NULL, NULL, N'LAVAPLATOS DE 2 BACHAS (40X40X30) , MATERIAL: ACERO INOXDABLE, MEDIDAS: 85X180X60 cm., COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|', @EquipmentId);

-- Equipment: LICUADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'LICUADORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'LICUADORA', N'OSTER', NULL, NULL, N'LICUADORA ELECTRICA VASO DE VIDRIO, TECNOLOGIA REVERSIBLE 600W DE POTENCIA , MARCA: OSTER', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LICUADORA|OSTER|', @EquipmentId);

-- Equipment: LICUADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'LICUADORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(N'XPERT', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'LICUADORA', N'OSTER', N'XPERT', NULL, N'LICUADRA JARRA DE VIDRIO, DE 6 ASPAS , CAPACIDAD: 2.5 KG, 2 LITROS , MARCA: OSTER, MODELO: XPERT', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LICUADORA|OSTER|XPERT', @EquipmentId);

-- Equipment: MESA DE MADERA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESA DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESA DE MADERA', NULL, NULL, NULL, N'MESA RECTANGULAR , MATERIAL: MADERA, MEDIDAS: 80X240X80 cm., COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA DE MADERA||', @EquipmentId);

-- Equipment: MESA METALICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESA METALICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESA METALICA', NULL, NULL, NULL, N'MESA , MATERIAL: METAL Y FORMICA, MEDIDAS: 30X121X76 cm., COLOR: PLOMO JASPEADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA METALICA||', @EquipmentId);

-- Equipment: MESA METALICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESA METALICA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESA METALICA', N'WILDA', NULL, NULL, N'MESA RECTANGULAR DE APOYO, CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 1 REPISA, COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA METALICA|WILDA|', @EquipmentId);

-- Equipment: MESON DE METAL
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON DE METAL' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON DE METAL', NULL, NULL, NULL, N'MESON GASTRONOMICO CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X100X80 cm., BANDEJAS/DIVISIONES: 1 REPISA INFERIOR, COLOR: METALICO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON DE METAL||', @EquipmentId);

-- Equipment: MESON DE METAL
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON DE METAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON DE METAL', N'WILDA', NULL, NULL, N'MESON GASTRONOMICO DE 1 REPISA INFERIOR , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X60 cm., COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON DE METAL|WILDA|', @EquipmentId);

-- Equipment: MESON REFRIGERADOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(N'VMR2PS-280E', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON REFRIGERADOR', N'VENTUS', N'VMR2PS-280E', NULL, N'MESON REFRIGERANTE CON 2 PUERTAS,4 REJILLAS BLANCAS, 4 DIVISIONES, MOVIBLE CON 4 RUEDAS, CHAPA CON 2 LLAVES, TEMPERATURA DE +5° A -5° , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 280 LTS 250 WATT 220V 50HZ, MEDIDAS: 85X70X136 cm., COLOR: PLATEADO, MARCA: VENTUS, MODELO: VMR2PS-280E, SERIE: EPL3520CL6200319070300350004', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E', @EquipmentId);

-- Equipment: MESON ROBUSTO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON ROBUSTO', NULL, NULL, NULL, N'MESON GASTRONOMICO PARA INSTRUCTOR CON 4 ANAFES, CAJONERIA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X100X85 cm., COLOR: PLATEADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO||', @EquipmentId);

-- Equipment: MESON ROBUSTO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON ROBUSTO', N'WILDA', NULL, NULL, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO|WILDA|', @EquipmentId);

-- Equipment: MESON ROBUSTO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MESON ROBUSTO', N'WILDA', N'A155 304L2B', NULL, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO|WILDA|A155 304L2B', @EquipmentId);

-- Equipment: MICROONDA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'LG', N'') AND ISNULL(Model, N'') = ISNULL(N'MH8236GIR', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MICROONDA', N'LG', N'MH8236GIR', NULL, N'MICROONDAS PANEL TOUCH , CAPACIDAD: DE 42 LITROS , COLOR: NEGRO JASPEADO, MARCA: LG, MODELO: MH8236GIR, SERIE: 302TATGEX185', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|LG|MH8236GIR', @EquipmentId);

-- Equipment: MICROONDA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'PANASONIC', N'') AND ISNULL(Model, N'') = ISNULL(N'NN-ST34HM', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MICROONDA', N'PANASONIC', N'NN-ST34HM', NULL, N'HORNO MICROONDAS , CAPACIDAD: 23 LITROS, MARCA: PANASAONIC, MODELO: NN-ST34HM, SERIE: 5A39210153', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|PANASONIC|NN-ST34HM', @EquipmentId);

-- Equipment: MICROONDA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'') AND ISNULL(Model, N'') = ISNULL(N'MG402MADXBB', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MICROONDA', N'SAMSUNG', N'MG402MADXBB', NULL, N'MICROONDAS , CAPACIDAD: 40 LITROS, COLOR: NEGRO, MARCA: SAMSUNG, MODELO: MG402MADXBB, SERIE: 0AMM7WFT600193', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|SAMSUNG|MG402MADXBB', @EquipmentId);

-- Equipment: MOLINO DE COFFIE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MOLINO DE COFFIE' AND ISNULL(Brand, N'') = ISNULL(N'FIORENZATO', N'') AND ISNULL(Model, N'') = ISNULL(N'F64 E', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MOLINO DE COFFIE', N'FIORENZATO', N'F64 E', NULL, N'MOLINO DE CAFE VASO DE PLASTICO , MATERIAL: INOX/ACERO, COLOR: PLOMO, MARCA: FIORENZATO, MODELO: F64 E, SERIE: 66666622', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MOLINO DE COFFIE|FIORENZATO|F64 E', @EquipmentId);

-- Equipment: MONITOR LCD
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MONITOR LCD' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'2580AB1', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MONITOR LCD', N'LENOVO', N'2580AB1', NULL, N'MONITOR LCD DE 18.5 PULGADAS , COLOR: NEGRO, MARCA: LENOVO, MODELO: 2580AB1, SERIE: V1RVV53', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MONITOR LCD|LENOVO|2580AB1', @EquipmentId);

-- Equipment: MUEBLE DE MADERA PARA COMPUTADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MUEBLE DE MADERA PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MUEBLE DE MADERA PARA COMPUTADORA', NULL, NULL, NULL, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO Y PORTA CPU , MATERIAL: MADERA/METAL, MEDIDAS: 80X75X50 cm., COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE DE MADERA PARA COMPUTADORA||', @EquipmentId);

-- Equipment: MUEBLE DE MELAMINA PARA COMPUTADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MUEBLE DE MELAMINA PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MUEBLE DE MELAMINA PARA COMPUTADORA', NULL, NULL, NULL, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO CORREDIZO, (1 CHAPA Y 2 LLAVES) , MATERIAL: MELAMINA, MEDIDAS: 189X73.5X51 cm., BANDEJAS/DIVISIONES: 3 PUERTAS, COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE DE MELAMINA PARA COMPUTADORA||', @EquipmentId);

-- Equipment: MUEBLE METALICO PARA COMPUTADORA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'MUEBLE METALICO PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'MUEBLE METALICO PARA COMPUTADORA', NULL, NULL, NULL, N'MUEBLE PARA COMPUTADORA , MATERIAL: METALICA/MELAMINA, MEDIDAS: 83X110X73 cm., COLOR: PLOMO/BLANCO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE METALICO PARA COMPUTADORA||', @EquipmentId);

-- Equipment: PINZA KELLY
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PINZA KELLY' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PINZA KELLY', NULL, NULL, NULL, N'PINZA KELLY CURVA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 14 cm., COLOR: NIQUELADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PINZA KELLY||', @EquipmentId);

-- Equipment: PIZARRA CON MARCO METALICO ACRILICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PIZARRA CON MARCO METALICO ACRILICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PIZARRA CON MARCO METALICO ACRILICO', NULL, NULL, NULL, N'PIZARRA CON PORTA MARCADORES , MATERIAL: ALUMINIO/ACRILICO, MEDIDAS: 120X300 cm., COLOR: PLOMO/BLANCO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PIZARRA CON MARCO METALICO ACRILICO||', @EquipmentId);

-- Equipment: PORTA BANDEJAS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PORTA BANDEJAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PORTA BANDEJAS', NULL, NULL, NULL, N'PORTA BANDEJAS CON RODAPIES, DIVISIONES PARA BANDEJAS , MATERIAL: METALICO, COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA BANDEJAS||', @EquipmentId);

-- Equipment: PORTA TABLAS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PORTA TABLAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PORTA TABLAS', NULL, NULL, NULL, N'PORTA TABLAS , MATERIAL: INOX, COLOR: PLATEADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA TABLAS||', @EquipmentId);

-- Equipment: PORTA UTENSILIOS
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PORTA UTENSILIOS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PORTA UTENSILIOS', NULL, NULL, NULL, N'PORTA UTENSILIOS , MATERIAL: INOX, MEDIDAS: 11X11X18 cm.', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA UTENSILIOS||', @EquipmentId);

-- Equipment: PROYECTOR LED/LASER
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PROYECTOR LED/LASER' AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'') AND ISNULL(Model, N'') = ISNULL(N'(YW-40) XJ-F20XN', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PROYECTOR LED/LASER', N'CASIO', N'(YW-40) XJ-F20XN', NULL, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093815', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN', @EquipmentId);

-- Equipment: PROYECTOR LED/LASER
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'PROYECTOR LED/LASER' AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'') AND ISNULL(Model, N'') = ISNULL(N'XJ-F20XN', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'PROYECTOR LED/LASER', N'CASIO', N'XJ-F20XN', NULL, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093585', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PROYECTOR LED/LASER|CASIO|XJ-F20XN', @EquipmentId);

-- Equipment: REFRIGERADOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS SORP', N'') AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'REFRIGERADOR', N'VENTUS SORP', N'VREF-1000BEN', NULL, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS CORP, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350020', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN', @EquipmentId);

-- Equipment: REFRIGERADOR
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'REFRIGERADOR', N'VENTUS', N'VREF-1000BEN', NULL, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350005', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REFRIGERADOR|VENTUS|VREF-1000BEN', @EquipmentId);

-- Equipment: REPISA DE ACERO INOXIDABLE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'REPISA DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'REPISA DE ACERO INOXIDABLE', NULL, NULL, NULL, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REPISA DE ACERO INOXIDABLE||', @EquipmentId);

-- Equipment: REPISA DE ACERO INOXIDABLE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'REPISA DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'REPISA DE ACERO INOXIDABLE', N'WILDA', NULL, NULL, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REPISA DE ACERO INOXIDABLE|WILDA|', @EquipmentId);

-- Equipment: ROUTER
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'ROUTER' AND ISNULL(Brand, N'') = ISNULL(N'VIEW SONIC', N'') AND ISNULL(Model, N'') = ISNULL(N'WPG-370', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'ROUTER', N'VIEW SONIC', N'WPG-370', NULL, N'ROUTER CON DOS ANTENAS, ADAPTADOR DE ENERGÍA , COLOR: NEGRO, MARCA: VIEW SONIC, MODELO: WPG-370, SERIE: TK2142200824', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ROUTER|VIEW SONIC|WPG-370', @EquipmentId);

-- Equipment: SILLA DE MADERA FIJA TAPIZ TELA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SILLA DE MADERA FIJA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SILLA DE MADERA FIJA TAPIZ TELA', NULL, NULL, NULL, N'SILLA DE MADERA TAPIZ TELA , MATERIAL: MADERA TAPIZ TELA, COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA DE MADERA FIJA TAPIZ TELA||', @EquipmentId);

-- Equipment: SILLA DE MADERA FIJA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SILLA DE MADERA FIJA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SILLA DE MADERA FIJA', NULL, NULL, NULL, N'SILLA FIJA , MATERIAL: MADERA, COLOR: CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA DE MADERA FIJA||', @EquipmentId);

-- Equipment: SILLA METALICA FIJA TAPIZ CUERINA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SILLA METALICA FIJA TAPIZ CUERINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SILLA METALICA FIJA TAPIZ CUERINA', NULL, NULL, NULL, N'SILLA FIJA DE ESPERA, ESTRUCTURA METALICA TAPIZ CUERINA , MATERIAL: METALICO/CUERINA, BANDEJAS/DIVISIONES: , COLOR: NEGRO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA FIJA TAPIZ CUERINA||', @EquipmentId);

-- Equipment: SILLA METALICA FIJA TAPIZ TELA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SILLA METALICA FIJA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SILLA METALICA FIJA TAPIZ TELA', NULL, NULL, NULL, N'SILLA FIJA DE ESPERA , MATERIAL: METAL/TAPIZ TELA, COLOR: NEGRO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA FIJA TAPIZ TELA||', @EquipmentId);

-- Equipment: SILLA METALICA GIRATORIA TAPIZ TELA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SILLA METALICA GIRATORIA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SILLA METALICA GIRATORIA TAPIZ TELA', NULL, NULL, NULL, N'SILLA GIRATORIA CON 5 RODAPIES, APOYA BRAZOS , MATERIAL: METAL/PLASTICO/TAPIZ TELA, COLOR: NEGRO/TELA AZUL', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA GIRATORIA TAPIZ TELA||', @EquipmentId);

-- Equipment: SOUS VIDE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'SOUS VIDE' AND ISNULL(Brand, N'') = ISNULL(N'METVISA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'SOUS VIDE', N'METVISA', NULL, NULL, N'SOUS VIDE , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: METVISA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SOUS VIDE|METVISA|', @EquipmentId);

-- Equipment: TABURETE METALICO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TABURETE METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TABURETE METALICO', NULL, NULL, NULL, N'TABURETE , MATERIAL: METALICO Y MADERA, COLOR: BEIGE/CAFE', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TABURETE METALICO||', @EquipmentId);

-- Equipment: TECLADO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'DELUX', N'') AND ISNULL(Model, N'') = ISNULL(N'K8060', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TECLADO', N'DELUX', N'K8060', NULL, N'TECLADO , COLOR: NEGRO, MARCA: DELUX, MODELO: K8060, SERIE: K80600903013004', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|DELUX|K8060', @EquipmentId);

-- Equipment: TECLADO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'HP', N'') AND ISNULL(Model, N'') = ISNULL(N'PR1101U', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TECLADO', N'HP', N'PR1101U', NULL, N'TECLADO , COLOR: BLANCO , MARCA: HP, MODELO: PR1101U, SERIE: BFZYF0ALAAC0H9', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|HP|PR1101U', @EquipmentId);

-- Equipment: TECLADO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'KU-0225', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TECLADO', N'LENOVO', N'KU-0225', NULL, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: KU-0225, SERIE: 2928973', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|LENOVO|KU-0225', @EquipmentId);

-- Equipment: TECLADO
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'SK - 8825', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TECLADO', N'LENOVO', N'SK - 8825', NULL, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: SK - 8825, SERIE: 03647434', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|LENOVO|SK - 8825', @EquipmentId);

-- Equipment: TELEVISOR LCD
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TELEVISOR LCD' AND ISNULL(Brand, N'') = ISNULL(N'SONY', N'') AND ISNULL(Model, N'') = ISNULL(N'KDL-40BX455', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TELEVISOR LCD', N'SONY', N'KDL-40BX455', NULL, N'TELEVISOR LCD DE 40 PULGADAS, CON CONTROL REMOTO Y PEDESTAL SOPORTE , COLOR: NEGRO, MARCA: SONY, MODELO: KDL-40BX455, SERIE: 5019941', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LCD|SONY|KDL-40BX455', @EquipmentId);

-- Equipment: TELEVISOR LED
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'HAIER', N'') AND ISNULL(Model, N'') = ISNULL(N'LE55B8500DUA', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TELEVISOR LED', N'HAIER', N'LE55B8500DUA', NULL, N'TELEVISOR CON CONTROL REMOTO, EMPOTRADO EN LA PARED , COLOR: NEGRO, MARCA: HAIER, MODELO: LE55B8500DUA, SERIE: DH1VM0D4401D9H9C0323', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|HAIER|LE55B8500DUA', @EquipmentId);

-- Equipment: TELEVISOR LED
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'LG', N'') AND ISNULL(Model, N'') = ISNULL(N'50LN5400', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TELEVISOR LED', N'LG', N'50LN5400', NULL, N'TELEVISOR LED DE 50 PULGADAS, CON SOPORTE , COLOR: NEGRO, MARCA: LG, MODELO: 50LN5400, SERIE: 402RMYA62106', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|LG|50LN5400', @EquipmentId);

-- Equipment: TELEVISOR LED
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'') AND ISNULL(Model, N'') = ISNULL(N'UN48J5000AGXZS', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TELEVISOR LED', N'SAMSUNG', N'UN48J5000AGXZS', NULL, N'TELEVISOR LED CON SOPORTE PARA TV , COLOR: LED, MARCA: SAMSUNG, MODELO: UN48J5000AGXZS, SERIE: 04NS3CVH801270', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS', @EquipmentId);

-- Equipment: TERMO TANQUE
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'TERMO TANQUE' AND ISNULL(Brand, N'') = ISNULL(N'A6', N'') AND ISNULL(Model, N'') = ISNULL(N'27284', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'TERMO TANQUE', N'A6', N'27284', NULL, N'TERMO TANQUE DE 1.100 LITROS , MEDIDAS: 143X41 cm., COLOR: PLOMO, MARCA: A6, MODELO: 027284', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TERMO TANQUE|A6|27284', @EquipmentId);

-- Equipment: THERMOMIX
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'THERMOMIX' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(N'TM6', N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'THERMOMIX', NULL, N'TM6', NULL, N'THERMOMIX VASO, MARIPOSA, CESTILLO, VAROMA, ESPATULA , MODELO: TM6, SERIE: 62144842778514045362105', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'THERMOMIX||TM6', @EquipmentId);

-- Equipment: VITRINA DE PARED
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'VITRINA DE PARED' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'VITRINA DE PARED', NULL, NULL, NULL, N'VITRINA PEQUEÑA DE PARED 3 DIVISIONES, 2 PUERTAS DE VIDRIO , MATERIAL: METALICA, MEDIDAS: 80X50X33 cm., COLOR: PLOMO', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'VITRINA DE PARED||', @EquipmentId);

-- Equipment: VITRINA METALICA
DECLARE @EquipmentId int;
SELECT TOP 1 @EquipmentId = Id FROM Equipments WHERE Name = N'VITRINA METALICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF @EquipmentId IS NULL
BEGIN
    INSERT INTO Equipments (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, N'VITRINA METALICA', NULL, NULL, NULL, N'VITRINA METALICA CON 4 PUERTAS CON VIDRIO , UNA CHAPA CON LLAVE , MEDIDAS: 205X155X50 cm., BANDEJAS/DIVISIONES: CON 5 DIVISIONES , COLOR: CREMA', @Today, @CreatedById);
    SET @EquipmentId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'VITRINA METALICA||', @EquipmentId);

-- EquipmentUnit: 225
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BANCA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'225';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'225', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'225', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 239
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BANCA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'239';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'239', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'239', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 401
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'401';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'401', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'401', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 406
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'406';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'406', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'406', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 416
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'416';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'416', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'416', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 423
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'423';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'423', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'423', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 696
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'696';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'696', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'696', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 719
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'719';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'719', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'719', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 1635
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'1635';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'1635', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'1635', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2582
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2582';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2582', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2582', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2584
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2584';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2584', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2584', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2586
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2586';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2586', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2586', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2588
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2588';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2588', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2588', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2589
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2589';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2589', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2589', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2590
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2590';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2590', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2590', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2592
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2592';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2592', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2592', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2593
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2593';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2593', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2593', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2595
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2595';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2595', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2595', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2791
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2791';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2791', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2791', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 2802
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2802';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'2802', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2802', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3275
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3275';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3275', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3275', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3287
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3287';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3287', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3287', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3289
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3289';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3289', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3289', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3476
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3476';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3476', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3476', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3478
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3478';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3478', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3478', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3479
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3479';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3479', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3479', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3480
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3480';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3480', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3480', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3481
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3481';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3481', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3481', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3497
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3497';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3497', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3497', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3687
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3687';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3687', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3687', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 3695
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3695';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'3695', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3695', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 9007
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'9007';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'9007', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'9007', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 9869
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA GIRATORIA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'9869';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'9869', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'9869', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 10714
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE METALICO PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'10714';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'10714', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'10714', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 11848
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|DELUX|K8060';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'11848';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'11848', N'K80600903013004', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'11848', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 12789
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PINZA KELLY||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'12789';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'12789', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'12789', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 12792
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PINZA KELLY||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'12792';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'12792', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'12792', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 13281
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13281';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'13281', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13281', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 13616
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13616';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'13616', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13616', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 13617
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13617';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'13617', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13617', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 14298
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14298';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'14298', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14298', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 14302
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14302';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'14302', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14302', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 14464
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14464';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'14464', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14464', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 14799
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14799';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'14799', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14799', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 15501
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE METALICO PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'15501';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'15501', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'15501', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 17355
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17355';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'17355', N'XX', @UnitCareerId, CONVERT(datetime2, '2015-01-10', 23), NULL, 350, 0, 2, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17355', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 17585
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17585';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'17585', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17585', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 17613
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17613';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'17613', NULL, @UnitCareerId, NULL, NULL, 7500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17613', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 18024
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18024';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'18024', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18024', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 18049
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18049';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'18049', NULL, @UnitCareerId, NULL, NULL, 7500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18049', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 18081
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18081';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'18081', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18081', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 18366
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18366';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'18366', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18366', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 20208
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE BAR DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'20208';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'20208', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'20208', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 20540
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA BANDEJAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'20540';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'20540', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'20540', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21933
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21933';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21933', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21933', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21934
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21934';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21934', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21934', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21935
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21935';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21935', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21935', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21937
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21937';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21937', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21937', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21938
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21938';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21938', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21938', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21940
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21940';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21940', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21940', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21952
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21952';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21952', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21952', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21955
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21955';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21955', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21955', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21956
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21956';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21956', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21956', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21957
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21957';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21957', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21957', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21958
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21958';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21958', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21958', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 21960
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21960';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'21960', NULL, @UnitCareerId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21960', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 22060
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'22060';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'22060', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'22060', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 23096
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE PORTA BANDEJAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'23096';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'23096', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'23096', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 23273
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'23273';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'23273', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'23273', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 25509
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25509';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'25509', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25509', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 25510
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25510';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'25510', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25510', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 25511
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25511';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'25511', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25511', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 25513
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25513';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'25513', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25513', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 25514
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25514';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'25514', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25514', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 26431
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'26431';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'26431', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'26431', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 26681
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CARRITO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'26681';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'26681', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'26681', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 30784
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'30784';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'30784', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'30784', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 31123
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'31123';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'31123', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'31123', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 31169
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'31169';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'31169', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'31169', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33306
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33306';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33306', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33306', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33308
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33308';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33308', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33308', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33650
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33650';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33650', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33650', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33651
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33651';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33651', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33651', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33706
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE AIRE|LOREN SID|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33706';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33706', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33706', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33956
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33956';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33956', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33956', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33957
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33957';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33957', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33957', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33958
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33958';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33958', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33958', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33959
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33959';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33959', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33959', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33960
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33960';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33960', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33960', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 33962
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33962';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'33962', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33962', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34046
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34046';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34046', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34046', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34047
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34047';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34047', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34047', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34073
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34073';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34073', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34073', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34075
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34075';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34075', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34075', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34076
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34076';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34076', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34076', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34077
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34077';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34077', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34077', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34161
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34161';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34161', N'5019590', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34161', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34162
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34162';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34162', N'5019941', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34162', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34164
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34164';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34164', N'5020193', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34164', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34165
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34165';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34165', N'5019944', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34165', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34166
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34166';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34166', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34166', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34167
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34167';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34167', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34167', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34168
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34168';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34168', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34168', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34169
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34169';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34169', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34169', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34170
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34170';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34170', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34170', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34171
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34171';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34171', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34171', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34172
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34172';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34172', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34172', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34173
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34173';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34173', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34173', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34174
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34174';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34174', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34174', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34175
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34175';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34175', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34175', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34178
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CILINDERS|FNC 10';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34178';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34178', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34178', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34179
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34179';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34179', N'XX', @UnitCareerId, CONVERT(datetime2, '2019-03-15', 23), NULL, 8500, 0, 2, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34179', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34180
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34180';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34180', N'XX', @UnitCareerId, CONVERT(datetime2, '2019-03-15', 23), NULL, 8500, 0, 2, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34180', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34181
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34181';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34181', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34181', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34183
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34183';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34183', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34183', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34184
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34184';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34184', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34184', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34185
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34185';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34185', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34185', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34186
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34186';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34186', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34186', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34187
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34187';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34187', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34187', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34188
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34188';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34188', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34188', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34189
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34189';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34189', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34189', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34190
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34190';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34190', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34190', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34191
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34191';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34191', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34191', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34192
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34192';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34192', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34192', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34193
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34193';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34193', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34193', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34245
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34245';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34245', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34245', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34246
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34246';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34246', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34246', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34247
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34247';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34247', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34247', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34248
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34248';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34248', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34248', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34249
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34249';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34249', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34249', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34250
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34250';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34250', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34250', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34253
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34253';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34253', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34253', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34270
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34270';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34270', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34270', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34271
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34271';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34271', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34271', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34272
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34272';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34272', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34272', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34273
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34273';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34273', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34273', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34274
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34274';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34274', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34274', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34292
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34292';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34292', N'MJNHDXM', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34292', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34296
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34296';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34296', N'MJNHDZW', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34296', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34297
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34297';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34297', N'MJNHDWB', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34297', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34309
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34309';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34309', N'MJNHDWZ', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34309', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34363
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34363';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34363', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34363', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34365
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34365';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34365', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34365', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34371
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34371';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34371', N'V1RWP09', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34371', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34373
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FERMENTADOR|WILDA|A155 304L2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34373';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34373', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34373', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34379
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34379';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34379', N'V1RWM86', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34379', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34381
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34381';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34381', N'V1RVW26', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34381', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34386
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34386';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34386', N'V1RVV53', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34386', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34409
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34409';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34409', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34409', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34420
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'AMASADORA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34420';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34420', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34420', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34483
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34483';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34483', N'2928939', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34483', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34491
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34491';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34491', N'2928871', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34491', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34492
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34492';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34492', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34492', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34495
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34495';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34495', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34495', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34500
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34500';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34500', N'2928973', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34500', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34502
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34502';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34502', N'2928897', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34502', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34515
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34515';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34515', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34515', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34518
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34518';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34518', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34518', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34520
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34520';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34520', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34520', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34523
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34523';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34523', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34523', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34537
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34537';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34537', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34537', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34564
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34564';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34564', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34564', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34566
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34566';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34566', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34566', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34567
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34567';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34567', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34567', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34568
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34568';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34568', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34568', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34599
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34599';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34599', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34599', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34600
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34600';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34600', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34600', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34601
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34601';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34601', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34601', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34734
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34734';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34734', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34734', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34735
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34735';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34735', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34735', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34736
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34736';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34736', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34736', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34737
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34737';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34737', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34737', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34738
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34738';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34738', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34738', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34739
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34739';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34739', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34739', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34740
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34740';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34740', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34740', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34741
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34741';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34741', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34741', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34742
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34742';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34742', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34742', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34743
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34743';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34743', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34743', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34744
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34744';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34744', N'XX', @UnitCareerId, CONVERT(datetime2, '2019-03-15', 23), NULL, 12000, 0, 2, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34744', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34745
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34745';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34745', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34745', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34746
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34746';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34746', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34746', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34747
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34747';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34747', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34747', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34748
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34748';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34748', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34748', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34749
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34749';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34749', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34749', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34814
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|CYLINDERS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34814';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34814', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34814', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34815
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34815';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34815', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34815', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34816
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34816';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34816', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34816', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 34817
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34817';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'34817', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34817', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35075
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35075';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35075', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35075', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35196
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35196';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35196', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35196', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35197
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35197';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35197', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35197', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35394
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35394';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35394', NULL, @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35394', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35395
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35395';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35395', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35395', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35396
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35396';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35396', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35396', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35411
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35411';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35411', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35411', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35412
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35412';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35412', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35412', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35413
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35413';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35413', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35413', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35414
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35414';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35414', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35414', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35415
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35415';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35415', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35415', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35416
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35416';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35416', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35416', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35417
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35417';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35417', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35417', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35418
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35418';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35418', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35418', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35419
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35419';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35419', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35419', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35420
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35420';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35420', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35420', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35421
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35421';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35421', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35421', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35422
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35422';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35422', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35422', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35423
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FREIDORA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35423';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35423', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35423', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35424
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35424';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35424', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35424', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35425
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35425';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35425', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35425', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35426
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35426';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35426', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35426', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35427
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35427';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35427', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35427', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35428
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35428';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35428', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35428', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35429
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35429';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35429', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35429', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35430
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35430';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35430', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35430', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35431
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35431';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35431', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35431', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35432
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35432';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35432', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35432', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35433
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35433';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35433', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35433', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35434
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35434';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35434', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35434', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35435
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35435';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35435', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35435', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35436
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35436';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35436', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35436', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35437
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35437';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35437', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35437', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35438
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35438';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35438', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35438', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35528
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TERMO TANQUE|A6|27284';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'CIRCULACION';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35528';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35528', N'XX', @UnitCareerId, CONVERT(datetime2, '2018-08-01', 23), NULL, 1200, 0, 3, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35528', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35542
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35542';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35542', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35542', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35543
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35543';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35543', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35543', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35544
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35544';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35544', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35544', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35545
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35545';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35545', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35545', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35546
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35546';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35546', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35546', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 35733
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35733';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'35733', NULL, @UnitCareerId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35733', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36052
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36052';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36052', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36052', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36053
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36053';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36053', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36053', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36054
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36054';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36054', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36054', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36055
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36055';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36055', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36055', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36056
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36056';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36056', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36056', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36264
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|10BB-A0C900';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36264';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36264', N'MJ00UZ5R', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36264', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36600
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36600';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36600', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36600', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36601
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36601';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36601', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36601', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36602
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36602';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36602', NULL, @UnitCareerId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36602', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36604
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36604';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36604', N'402RMUY62070', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36604', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36605
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36605';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36605', N'402RMYA62106', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36605', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36606
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36606';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36606', N'403RMXX3F434', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36606', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36607
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36607';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36607', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36607', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36608
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36608';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36608', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36608', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36609
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36609';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36609', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36609', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36689
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ROUTER|VIEW SONIC|WPG-370';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36689';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36689', N'TK2142200824', @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36689', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36736
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36736';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36736', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36736', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36737
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36737';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36737', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36737', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36738
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36738';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36738', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36738', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36739
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36739';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36739', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36739', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36740
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36740';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36740', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36740', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36741
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36741';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36741', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36741', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36742
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36742';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36742', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36742', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36743
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36743';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36743', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36743', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36744
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36744';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36744', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36744', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36745
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36745';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36745', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36745', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36746
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36746';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36746', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36746', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36747
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36747';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36747', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36747', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36748
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36748';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36748', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36748', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36749
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36749';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36749', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36749', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36750
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36750';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36750', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36750', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36751
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36751';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36751', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36751', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36752
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36752';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36752', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36752', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36753
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36753';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36753', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36753', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36754
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36754';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36754', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36754', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36755
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36755';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36755', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36755', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36756
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36756';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36756', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36756', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36757
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36757';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36757', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36757', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36758
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36758';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36758', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36758', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36759
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36759';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36759', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36759', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36760
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36760';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36760', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36760', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36761
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36761';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36761', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36761', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36762
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36762';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36762', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36762', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36763
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36763';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36763', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36763', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36764
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36764';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36764', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36764', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36765
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36765';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36765', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36765', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36766
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36766';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36766', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36766', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36767
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36767';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36767', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36767', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36768
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36768';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36768', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36768', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36769
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36769';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36769', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36769', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36770
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36770';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36770', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36770', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36771
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36771';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36771', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36771', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36772
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36772';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36772', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36772', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36773
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36773';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36773', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36773', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36774
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36774';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36774', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36774', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36775
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36775';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36775', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36775', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36776
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36776';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36776', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36776', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36777
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36777';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36777', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36777', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36778
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36778';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36778', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36778', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36779
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36779';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36779', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36779', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36780
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36780';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36780', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36780', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 36781
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36781';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'36781', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36781', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37019
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37019';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37019', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37019', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37021
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37021';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37021', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37021', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37041
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37041';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37041', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37041', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37059
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37059';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37059', N'457428659', @UnitCareerId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37059', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37060
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37060';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37060', N'457428791', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37060', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37061
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37061';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37061', N'457428663', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37061', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37070
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|THINK CENTRE M73Z';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37070';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37070', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37070', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37077
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|SK - 8825';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37077';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37077', N'3647434', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37077', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37203
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ROUTER|VIEW SONIC|WPG-370';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37203';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37203', N'TK2133900878', @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37203', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37204
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37204';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37204', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37204', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37205
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37205';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37205', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37205', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37206
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37206';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37206', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37206', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37207
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37207';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37207', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37207', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37208
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37208';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37208', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37208', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37209
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37209';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37209', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37209', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37210
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37210';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37210', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37210', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37211
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37211';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37211', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37211', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37212
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37212';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37212', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37212', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37213
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37213';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37213', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37213', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37214
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37214';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37214', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37214', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37215
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37215';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37215', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37215', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37216
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37216';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37216', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37216', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37217
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37217';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37217', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37217', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37218
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37218';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37218', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37218', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37219
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37219';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37219', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37219', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37220
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37220';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37220', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37220', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37221
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37221';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37221', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37221', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37222
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37222';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37222', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37222', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37223
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37223';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37223', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37223', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37224
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37224';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37224', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37224', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37225
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37225';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37225', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37225', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37226
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37226';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37226', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37226', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37227
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37227';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37227', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37227', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37228
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37228';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37228', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37228', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37229
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37229';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37229', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37229', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37230
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37230';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37230', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37230', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37231
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37231';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37231', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37231', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37232
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37232';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37232', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37232', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37233
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37233';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37233', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37233', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37234
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37234';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37234', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37234', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37235
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37235';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37235', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37235', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37236
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37236';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37236', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37236', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37237
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37237';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37237', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37237', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37238
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37238';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37238', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37238', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37239
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37239';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37239', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37239', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37240
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37240';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37240', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37240', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37241
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37241';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37241', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37241', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37242
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37242';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37242', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37242', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37243
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37243';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37243', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37243', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37244
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37244';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37244', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37244', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37245
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37245';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37245', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37245', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37246
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37246';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37246', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37246', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37247
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37247';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37247', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37247', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37248
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37248';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37248', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37248', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37249
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37249';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37249', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37249', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37250
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37250';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37250', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37250', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37251
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37251';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37251', NULL, @UnitCareerId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37251', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37252
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37252';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37252', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37252', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37253
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37253';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37253', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37253', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37254
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37254';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37254', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37254', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37255
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37255';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37255', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37255', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37256
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37256';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37256', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37256', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37257
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37257';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37257', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37257', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37258
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37258';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37258', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37258', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37259
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37259';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37259', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37259', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37260
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37260';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37260', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37260', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37261
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37261';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37261', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37261', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37262
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37262';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37262', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37262', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37263
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37263';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37263', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37263', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37265
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37265';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37265', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37265', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37266
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37266';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37266', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37266', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37267
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37267';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37267', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37267', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37268
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37268';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37268', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37268', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37269
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37269';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37269', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37269', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37270
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37270';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37270', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37270', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37271
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37271';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37271', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37271', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37272
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37272';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37272', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37272', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37273
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37273';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37273', NULL, @UnitCareerId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37273', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37274
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37274';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37274', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37274', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37275
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37275';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37275', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37275', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37276
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37276';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37276', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37276', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37277
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37277';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37277', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37277', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37278
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37278';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37278', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37278', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37279
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37279';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37279', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37279', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37280
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37280';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37280', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37280', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37281
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37281';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37281', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37281', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37282
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37282';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37282', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37282', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37283
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37283';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37283', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37283', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37284
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37284';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37284', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37284', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37285
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37285';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37285', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37285', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37286
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37286';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37286', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37286', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37287
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37287';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37287', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37287', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37288
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37288';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37288', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37288', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37289
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37289';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37289', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37289', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37290
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37290';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37290', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37290', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37291
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37291';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37291', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37291', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37292
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37292';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37292', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37292', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37293
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37293';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37293', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37293', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37294
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37294';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37294', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37294', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37295
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37295';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37295', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37295', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37296
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37296';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37296', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37296', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37297
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37297';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37297', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37297', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37299
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37299';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37299', NULL, @UnitCareerId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37299', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37300
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA CONSERVADORA|ASBER|ARR-43';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37300';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37300', N'11090021M', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37300', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37314
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37314';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37314', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37314', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37315
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37315';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37315', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37315', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37316
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37316';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37316', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37316', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37317
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37317';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37317', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37317', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37318
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37318';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37318', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37318', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37319
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37319';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37319', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37319', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37320
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37320';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37320', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37320', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37321
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37321';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37321', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37321', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37322
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37322';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37322', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37322', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37323
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37323';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37323', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37323', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37324
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37324';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37324', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37324', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37325
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37325';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37325', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37325', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37326
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37326';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37326', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37326', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37327
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37327';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37327', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37327', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37361
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BEBEDERO DE AGUA|IBBL|BAG 40';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37361';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37361', N'451P287128', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37361', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37395
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37395';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37395', NULL, @UnitCareerId, NULL, NULL, 2000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37395', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37900
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37900';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37900', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37900', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37901
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37901';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37901', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37901', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37902
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37902';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37902', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37902', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37903
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37903';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37903', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37903', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37904
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37904';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37904', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37904', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37906
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37906';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37906', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37906', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37907
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37907';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37907', NULL, @UnitCareerId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37907', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37908
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37908';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37908', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37908', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37909
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37909';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37909', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37909', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37910
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37910';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37910', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37910', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37911
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37911';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37911', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37911', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37917
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37917';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37917', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37917', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37918
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37918';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37918', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37918', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37919
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37919';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37919', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37919', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37920
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37920';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37920', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37920', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 37922
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37922';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'37922', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37922', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38259
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38259';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38259', N'SFTX1927S0VR', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38259', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38260
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38260';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38260', N'SFTX1927S0VH', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38260', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38270
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP370';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38270';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38270', N'SFTX1927S0UL', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38270', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38639
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38639';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38639', N'1,409271378E+10', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38639', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38641
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|ELECTRONIC SCALE|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38641';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38641', N'1,409271383E+10', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38641', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38643
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|ELECTRONIC SCALE|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38643';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38643', N'1,40927138E+10', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38643', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38798
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38798';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38798', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38798', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38799
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38799';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38799', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38799', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38800
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38800';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38800', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38800', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38801
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38801';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38801', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38801', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38802
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38802';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38802', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38802', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38803
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38803';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38803', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38803', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 38804
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38804';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'38804', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38804', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39021
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA METALICA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39021';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39021', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39021', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39161
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39161';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39161', N'8336330455', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39161', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39163
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39163';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39163', NULL, @UnitCareerId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39163', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39169
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39169';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39169', N'8336510895', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39169', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39170
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39170';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39170', N'8336510968', @UnitCareerId, CONVERT(datetime2, '2021-06-01', 23), NULL, 950, 0, 2, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39170', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39171
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39171';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39171', N'8336330455', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39171', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39269
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39269';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39269', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39269', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39273
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39273';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39273', N'04NS3CVH801270', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39273', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39432
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39432';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39432', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39432', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39433
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39433';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39433', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39433', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39434
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39434';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39434', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39434', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39435
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39435';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39435', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39435', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39436
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39436';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39436', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39436', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39437
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39437';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39437', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39437', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39438
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39438';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39438', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39438', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39479
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|SONY BALUMS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39479';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39479', NULL, @UnitCareerId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39479', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39591
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39591';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39591', N'3016085604', @UnitCareerId, NULL, NULL, 33000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39591', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39632
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MADERA PARA COMPUTADORA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39632';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39632', NULL, @UnitCareerId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39632', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 39691
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39691';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'39691', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39691', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43486
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MELAMINA MODULAR||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43486';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43486', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43486', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43487
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GAVETERO DE MELAMINA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43487';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43487', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43487', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43488
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43488';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43488', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43488', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43489
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43489';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43489', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43489', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43490
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43490';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43490', NULL, @UnitCareerId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43490', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 43491
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CREDENZA DE MELAMINA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43491';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'43491', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43491', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44865
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44865';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44865', NULL, @UnitCareerId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44865', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44866
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44866';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44866', N'A995DDBY7B-093815', @UnitCareerId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44866', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44867
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44867';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44867', N'A995DDBY7B-093585', @UnitCareerId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44867', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44868
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44868';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44868', N'A995DDBY7C-098103', @UnitCareerId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44868', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44869
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44869';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44869', N'A995DDBY7B-093624', @UnitCareerId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44869', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44904
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591,';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44904';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44904', N'W74467732', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44904', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44905
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44905';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44905', N'W74467699', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44905', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44906
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44906';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44906', N'W74467691', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44906', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44907
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44907';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44907', N'W74467680', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44907', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 44908
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44908';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'44908', N'W74467600', @UnitCareerId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44908', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45024
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45024';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45024', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45024', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45025
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45025';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45025', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45025', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45026
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45026';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45026', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45026', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45027
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45027';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45027', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45027', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45028
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45028';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45028', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45028', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45029
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45029';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45029', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45029', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45030
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45030';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45030', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45030', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45031
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45031';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45031', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45031', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45032
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45032';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45032', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45032', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45033
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45033';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45033', NULL, @UnitCareerId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45033', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45744
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|HAIER|LE55B8500DUA';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45744';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45744', N'DH1VM0D4401D9H9C0323', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45744', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45745
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|HAIER|LE55B8500DUA';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45745';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45745', N'DH1VM0D4401D9H9C0353', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45745', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 45840
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'APARATO TELEFONICO|CISCO|CP-3905';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45840';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'45840', N'FCH2045GJVT', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45840', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46358
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|HP|24-E015LA';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46358';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46358', N'8CC80516KV', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46358', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46359
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|HP|PR1101U';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46359';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46359', N'BFZYF0ALAAC0H9', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46359', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46373
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46373';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46373', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46373', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46374
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46374';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46374', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46374', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46375
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46375';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46375', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46375', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46376
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46376';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46376', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46376', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46377
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46377';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46377', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46377', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46378
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46378';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46378', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46378', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46379
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46379';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46379', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46379', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46380
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46380';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46380', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46380', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46381
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46381';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46381', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46381', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46382
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46382';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46382', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46382', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46383
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46383';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46383', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46383', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46384
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46384';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46384', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46384', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46385
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46385';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46385', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46385', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46386
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46386';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46386', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46386', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46387
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46387';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46387', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46387', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46388
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46388';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46388', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46388', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46389
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46389';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46389', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46389', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46390
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46390';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46390', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46390', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46391
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46391';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46391', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46391', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46392
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46392';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46392', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46392', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46393
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46393';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46393', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46393', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46394
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46394';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46394', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46394', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46395
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46395';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46395', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46395', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46396
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46396';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46396', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46396', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46397
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46397';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46397', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46397', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46398
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46398';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46398', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46398', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46399
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46399';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46399', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46399', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46400
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46400';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46400', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46400', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46401
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46401';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46401', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46401', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46402
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46402';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46402', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46402', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46403
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46403';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46403', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46403', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46404
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46404';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46404', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46404', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46405
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46405';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46405', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46405', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 46406
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46406';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'46406', NULL, @UnitCareerId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46406', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 47789
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS|VREF-1000BEN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47789';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'47789', N'YBL9340CL6200319031300350005', @UnitCareerId, NULL, NULL, 1800, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47789', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 47790
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47790';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'47790', N'YBL9340CL6200319031300350020', @UnitCareerId, NULL, NULL, 18000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47790', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 47873
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47873';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'47873', N'EPL3520CL6200319070300350004', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47873', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 47874
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47874';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'47874', N'EPL3520CL6200319070300350002', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47874', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 48024
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48024';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'48024', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48024', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 48885
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAFETERA IND|ASTORIA|INDUS. ITALIANA';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48885';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'48885', N'913677', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48885', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 48886
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FLITRO ABLANDADOR DE AGUA||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48886';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'48886', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48886', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49055
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MOLINO DE COFFIE|FIORENZATO|F64 E';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49055';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49055', N'66666622', @UnitCareerId, NULL, NULL, 18000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49055', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49134
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49134';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49134', N'2021L0105834', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49134', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49136
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|PANASONIC|NN-ST34HM';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49136';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49136', N'5A39210153', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49136', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49190
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|XPERT';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49190';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49190', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49190', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49192
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|XPERT';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49192';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49192', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49192', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49220
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ENVASADORA AL VACIO|VENTUS|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49220';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49220', N'202031007', @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49220', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49221
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|SAMSUNG|MG402MADXBB';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49221';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49221', N'0AMM7WFT600193', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49221', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49222
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49222';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49222', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49222', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49223
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49223';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49223', NULL, @UnitCareerId, NULL, NULL, 12000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49223', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49224
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49224';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49224', NULL, @UnitCareerId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49224', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49236
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'THERMOMIX||TM6';
SELECT @UnitLabId = Id FROM @LabMap WHERE [Key] = N'H-3';
SELECT @UnitCareerId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49236';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49236', N'62144842778514', @UnitCareerId, CONVERT(datetime2, '2023-01-15', 23), NULL, 3500, 0, 4, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49236', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49511
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49511';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49511', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49511', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49512
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49512';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49512', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49512', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49513
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49513';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49513', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49513', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49514
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49514';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49514', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49514', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49515
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49515';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49515', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49515', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49516
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49516';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49516', NULL, @UnitCareerId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49516', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49820
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49820';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49820', N'302TATGEX185', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49820', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49821
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49821';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49821', N'302TALBEX280', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49821', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49822
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49822';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49822', N'302TAACEX312', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49822', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49823
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49823';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49823', N'302TAVYEX295', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49823', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49824
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49824';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49824', N'302TAVVEX226', @UnitCareerId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49824', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49873
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49873';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49873', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49873', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49874
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49874';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49874', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49874', @UnitId, @UnitEquipmentId, @UnitLabId);

-- EquipmentUnit: 49875
DECLARE @UnitId int;
DECLARE @UnitEquipmentId int;
DECLARE @UnitLabId int = NULL;
DECLARE @UnitCareerId int = NULL;
DECLARE @UnitManagementId int;
SELECT @UnitEquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT @UnitManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @UnitId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49875';
IF @UnitId IS NULL
BEGIN
    INSERT INTO EquipmentUnits (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@UnitManagementId, @UnitEquipmentId, @UnitLabId, N'49875', NULL, @UnitCareerId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
    SET @UnitId = CONVERT(int, SCOPE_IDENTITY());
END
INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49875', @UnitId, @UnitEquipmentId, @UnitLabId);

-- Request: PUR_CUCHARA001_34179
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34179';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'O-Ring válvula gas cocina industrial', 1, N'Responsable: ING. SARA PEREZ YAÑEZ', NULL, 0, 2, N'CUCHARA-001', N'LAB-GASTRO-001', CONVERT(datetime2, '2025-10-24', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'PUR_CUCHARA001_34179', @RequestId);

-- Request: REQ_R10_34179
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34179';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R10_34179', @RequestId);

-- Request: REQ_R11_34180
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34180';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R11_34180', @RequestId);

-- Request: REQ_R169_39170
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'39170';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'MANTENIMIENTO PREVENTIVO, LIMPIEZA PROFUNDA, RETIRO DE GRASAS ADHERIDAS. 
Por uso constante del equipo en los laboratorios.', 1, N'1. REALIZAR CALIBRACIONES PERIODICAS
2. LIMPIEZA Y CUIDADO DIARIO
3. REVISION DE CELDAS DE CARGA Y CONEXIONES
4. EVITAR SOBRECARGAS Y GOLPES
5. CONTRATAR UN SERVICIO TECNICO ESPECIALIZADO
6. LIMPIEZA CON ALCOHOL ISOPROPILICO DEL VISOR | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R169_39170', @RequestId);

-- Request: REQ_R170_34744
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34744';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO, DESENGRASADO Y LIMPIEZA PROFUNDA DE FILTROS , DUCTOS Y LUMINARIAS. CON RETIRO DE GRASAS ADHERIDAS A PARTES.
MANTENIMIENTO DE LUMINARIAS,CABLES ELÉCTRICOS. AJUSTE/ CAMBIO DE SELECTORES DE PRENDIDO Y APAGADO DE LUMINARIAS. (2 UNID)', 1, N'1. LIMPIEZA DE FILTROS DE GRASA
2. REVISION DE SUPERFICIE EXTERNA
3. AISLACION DE TENDIDO ELECTRICO
4. INSPECCION DEL CONDUCTO DE EXTRACCION
5. CHEQUEO DE FLUJO DE AIRE
6. REVISION DE TENSION DE ALAMBRES SUSPENSORES
7. VERIFICACION DE LUMINARIAS DE ACUERDO A NORMA (SEGURIDAD INTRINSECA)
8. LIMPIEZA DE SUPERFICIES EVITANDO SUSTANCIAS CORROSIVAS
9. PROTECCION ADECUADA DEL ACERO INOXIDABLE
10. CONTRATAR UN SERVICIO TECNICO ESPECIALIZADO | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R170_34744', @RequestId);

-- Request: REQ_R173_34179
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34179';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES', 1, N'1. CIERRE DE CANALIZACION DE GNV/GLP
2. LIMPIEZA Y RETIRO DE GRASAS ADHERIDAS A SUPERFICIES EXTERNAS
3. REMOJO, LIMPIEZA PARA RETIRO DE COMIDAS, GRASAS ADHERIDAS DE QUEMADORES Y LIMPIEZA INDIVIDUAL DE APERTURAS
4. REMOJO, LIMPIEZA PARA RETIRO DE GRASAS ADHERIDAS DE TUBO VENTURI Y ACCESORIOS 
5. REVISION,ENGRASE Y/O CAMBIO DE O-RING EN VALVULAS
6. CALIBRACION DE LLAMA, REGULACION DE FLUJO DE GAS
7. REVISION DE FUGAS DE GAS
8. PROTECCION ADECUADA DEL ACERO INOXIDABLE
9. CONTRATAR UN SERVICIO TECNI', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R173_34179', @RequestId);

-- Request: REQ_R174_34180
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34180';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO', 1, N'1. CIERRE DE CANALIZACION DE GNV/GLP
2. LIMPIEZA Y RETIRO DE GRASAS ADHERIDAS A SUPERFICIES EXTERNAS
3. REMOJO, LIMPIEZA PARA RETIRO DE COMIDAS, GRASAS ADHERIDAS DE QUEMADORES Y LIMPIEZA INDIVIDUAL DE APERTURAS
4. REMOJO, LIMPIEZA PARA RETIRO DE GRASAS ADHERIDAS DE TUBO VENTURI Y ACCESORIOS 
5. REVISION,ENGRASE Y/O CAMBIO DE O-RING EN VALVULAS
6. CALIBRACION DE LLAMA, REGULACION DE FLUJO DE GAS
7. REVISION DE FUGAS DE GAS
8. PROTECCION ADECUADA DEL ACERO INOXIDABLE
9. CONTRATAR UN SERVICIO TECNI', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R174_34180', @RequestId);

-- Request: REQ_R185_17355
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'17355';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'MANTENIMIENTO PREVENTIVO, LIMPIEZA PROFUNDA CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE CAMBIO DE TAPAS DE LICUADORA 
SE REQUIERE CAMBIO DE CUCHILLAS PICAS HIELOS DE 6 ASPAS
SE REQUIERE CAMBIO DE O-RING', 1, N'1. LIMPIEZA EXTERNA E INTERNA DE PARTES, RETIRO DE GRASAS ADHERIDAS
2. REVISION DE CUCHILLAS : FILO, OXIDO, FIJACION, VIBRACION, EJES
3. MANTENIMIENTO ELECTRICO: PULSADORES, PALANCAS, CABLE, ENCHUFE Y TOMA
4. MANTENIMIENTO MECANICO: LUBRICACION DE PARTES, MOTOR
5. VERIFICACION DE SELLOS EMPAQUES
6. VERIFICACION DE TAPAS
7. VERIFICACION DE VASOS DE LICUADORA | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R185_17355', @RequestId);

-- Request: REQ_R22_17355
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'17355';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'MANTENIMIENTO PREVENTIVO, LIMPIEZA PROFUNDA.', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R22_17355', @RequestId);

-- Request: REQ_R231_49236
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'49236';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'MANTENIMIENTO PREVENTIVO, LIMPIEZA DE COMPONENTES.
ACTUALIZACION DE SOFTWARE', 1, N'LA LIMPIEZA DE PANEL DELALCOHOL ISOPROPILICO
PARA LA ACTUALIZACION DEL SOFTWARE BUSCAR UN LUGAR CON UN BUEN INTERNET Y CONEXIÓN ELECTRICA EVITANDO CUALQUIER CORTE YA QUE LA REINSTALACION ES CON COMPRA DE DRIVERS DEL FABRICANTE | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R231_49236', @RequestId);

-- Request: REQ_R322_35528
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'35528';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'NINGUNO', 1, N'MANTENIMIENTO POR PERSONAL EXTERNO EXPERTO | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2025-05-16', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R322_35528', @RequestId);

-- Request: REQ_R42_35528
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'35528';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'FACTORES', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R42_35528', @RequestId);

-- Request: REQ_R6_39170
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'39170';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE CALIBRACION ANUAL', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R6_39170', @RequestId);

-- Request: REQ_R7_34744
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34744';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'SE REQUIERE MANTENIMIENTO, DESENGRASADO Y LIMPIEZA PROFUNDA DE FILTROS , DUCTOS Y LUMINARIAS. CON RETIRO DE GRASAS ADHERIDAS A PARTES.
ENTUBADO Y/O CANALIZACIÓN DE CABLES ELÉCTRICOS. AJUSTE DE SELECTORES DE PRENDIDO Y APAGADO DE LUMINARIAS.
SE REQUIERE REVISION DE TENSION DE ALAMBRES SUJETADORES DE CAMPANA ISLA.', 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'XXX', 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R7_34744', @RequestId);

-- Request: REQ_R94_49236
DECLARE @RequestId int;
DECLARE @RequestUnitId int;
DECLARE @RequestEquipmentId int;
DECLARE @RequestLabId int;
DECLARE @RequestManagementId int;
SELECT @RequestUnitId = Id, @RequestEquipmentId = EquipmentId, @RequestLabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'49236';
SELECT @RequestManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
INSERT INTO Requests (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@RequestLabId, @RequestEquipmentId, @RequestUnitId, @RequestManagementId, N'MANTENIMIENTO PREVENTIVO, LIMPIEZA DE COMPONENTES.
ACTUALIZACION DE SOFTWARE', 1, N'Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, NULL, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SET @RequestId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R94_49236', @RequestId);

-- Maintenance: MNT_R6_35528
DECLARE @MaintenanceId int;
DECLARE @MaintenanceUnitId int;
DECLARE @MaintenanceManagementId int;
DECLARE @TechnicianId int = NULL;
SELECT @MaintenanceUnitId = Id FROM @UnitMap WHERE [Key] = N'35528';
SELECT @MaintenanceManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @TechnicianId = Id FROM @PersonMap WHERE [Key] = N'CASATERMO SRL';
INSERT INTO Maintenances (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, CreatedDate, CreatedById) VALUES (@MaintenanceUnitId, 2, @MaintenanceManagementId, 1, @TechnicianId, NULL, CONVERT(datetime2, '2023-01-31', 23), CONVERT(datetime2, '2023-01-31', 23), N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.', 2, 100, 0, 0, 0, 0, 400, 5, N'Revisión anual preventiva obligatoria. Verificar válvula.', CONVERT(datetime2, '2024-01-10', 23), @Today, @CreatedById);
SET @MaintenanceId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @MaintenanceMap ([Key], Id) VALUES (N'MNT_R6_35528', @MaintenanceId);

DECLARE @TaskMaintenanceId int;
SELECT @TaskMaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
IF @TaskMaintenanceId IS NOT NULL INSERT INTO MaintenanceTasks (MaintenanceId, Description, IsCompleted, IsDeleted) VALUES (@TaskMaintenanceId, N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.', 1, 0);

DECLARE @CostRequestId int = NULL;
DECLARE @CostMaintenanceId int = NULL;
SELECT @CostMaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
IF @CostRequestId IS NOT NULL OR @CostMaintenanceId IS NOT NULL INSERT INTO CostDetails (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@CostRequestId, @CostMaintenanceId, N'Servicio mantenimiento termotanque', NULL, 1, N'servicio', 350, 5, N'CASATERMO SRL', NULL, @Today, @CreatedById);

DECLARE @CostRequestId int = NULL;
DECLARE @CostMaintenanceId int = NULL;
SELECT @CostMaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
IF @CostRequestId IS NOT NULL OR @CostMaintenanceId IS NOT NULL INSERT INTO CostDetails (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@CostRequestId, @CostMaintenanceId, N'Válvula de retención', NULL, 1, N'pieza', 50, 1, N'CASATERMO SRL', NULL, @Today, @CreatedById);

DECLARE @CostRequestId int = NULL;
DECLARE @CostMaintenanceId int = NULL;
SELECT @CostRequestId = Id FROM @RequestMap WHERE [Key] = N'PUR_CUCHARA001_34179';
IF @CostRequestId IS NOT NULL OR @CostMaintenanceId IS NOT NULL INSERT INTO CostDetails (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@CostRequestId, @CostMaintenanceId, N'O-Ring válvula gas cocina industrial', NULL, 2, N'pieza', 45, 1, NULL, NULL, @Today, @CreatedById);

DECLARE @CostRequestId int = NULL;
DECLARE @CostMaintenanceId int = NULL;
SELECT @CostRequestId = Id FROM @RequestMap WHERE [Key] = N'PUR_CUCHARA001_34179';
IF @CostRequestId IS NOT NULL OR @CostMaintenanceId IS NOT NULL INSERT INTO CostDetails (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@CostRequestId, @CostMaintenanceId, N'Mano de obra regulación y limpieza cocina INOX', NULL, 1, N'servicio', 350, 2, NULL, NULL, @Today, @CreatedById);

-- Departure: DEP_R6_34179
DECLARE @DepartureId int;
DECLARE @DepartureUnitId int;
DECLARE @DepartureManagementId int;
DECLARE @BorrowerId int;
SELECT @DepartureUnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
SELECT @DepartureManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @BorrowerId = Id FROM @PersonMap WHERE [Key] = N'ING. SARA PEREZ YAÑEZ';
INSERT INTO Departures (ManagementId, EquipmentUnitId, BorrowerId, Type, DepartureDate, EstimatedReturnDate, DepartureObservations, Status, CreatedDate, CreatedById) VALUES (@DepartureManagementId, @DepartureUnitId, @BorrowerId, 3, CONVERT(datetime2, '2025-10-24', 23), CONVERT(datetime2, '2025-10-24', 23), N'Sale para mantenimiento correctivo. Ver kardex ID 1.', 0, @Today, @CreatedById);
SET @DepartureId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @DepartureMap ([Key], Id) VALUES (N'DEP_R6_34179', @DepartureId);

-- Departure: DEP_R7_17355
DECLARE @DepartureId int;
DECLARE @DepartureUnitId int;
DECLARE @DepartureManagementId int;
DECLARE @BorrowerId int;
SELECT @DepartureUnitId = Id FROM @UnitMap WHERE [Key] = N'17355';
SELECT @DepartureManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @BorrowerId = Id FROM @PersonMap WHERE [Key] = N'DOCENTE MARTINEZ';
INSERT INTO Departures (ManagementId, EquipmentUnitId, BorrowerId, Type, DepartureDate, EstimatedReturnDate, DepartureObservations, Status, CreatedDate, CreatedById) VALUES (@DepartureManagementId, @DepartureUnitId, @BorrowerId, 2, CONVERT(datetime2, '2025-11-05', 23), CONVERT(datetime2, '2025-11-10', 23), N'Préstamo para clase magistral. Responsabilidad del docente hasta retorno.', 0, @Today, @CreatedById);
SET @DepartureId = CONVERT(int, SCOPE_IDENTITY());
INSERT INTO @DepartureMap ([Key], Id) VALUES (N'DEP_R7_17355', @DepartureId);

-- DepartureItem: DEP_R6_34179 / 34179
DECLARE @DepartureItemDepartureId int;
DECLARE @DepartureItemUnitId int = NULL;
SELECT @DepartureItemDepartureId = Id FROM @DepartureMap WHERE [Key] = N'DEP_R6_34179';
SELECT @DepartureItemUnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
IF @DepartureItemDepartureId IS NOT NULL INSERT INTO DepartureItems (DepartureId, EquipmentUnitId, ProductName, Quantity, UnitOfMeasure, Observations, IsRemoved, CreatedDate, CreatedById) VALUES (@DepartureItemDepartureId, @DepartureItemUnitId, N'COCINA INDUSTRIAL', 1, N'UNIDAD', N'Sale para mantenimiento correctivo. Ver kardex ID 1.', 0, @Today, @CreatedById);

-- DepartureItem: DEP_R7_17355 / 17355
DECLARE @DepartureItemDepartureId int;
DECLARE @DepartureItemUnitId int = NULL;
SELECT @DepartureItemDepartureId = Id FROM @DepartureMap WHERE [Key] = N'DEP_R7_17355';
SELECT @DepartureItemUnitId = Id FROM @UnitMap WHERE [Key] = N'17355';
IF @DepartureItemDepartureId IS NOT NULL INSERT INTO DepartureItems (DepartureId, EquipmentUnitId, ProductName, Quantity, UnitOfMeasure, Observations, IsRemoved, CreatedDate, CreatedById) VALUES (@DepartureItemDepartureId, @DepartureItemUnitId, N'LICUADORA', 1, N'UNIDAD', N'Préstamo para clase magistral. Responsabilidad del docente hasta retorno.', 0, @Today, @CreatedById);

-- ManagementPlan: PLAN_R7_49236
DECLARE @PlanManagementId int;
DECLARE @PlanUnitId int;
SELECT @PlanManagementId = Id FROM @ManagementMap WHERE [Key] = N'2023-2';
SELECT @PlanUnitId = Id FROM @UnitMap WHERE [Key] = N'49236';
INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, Responsible, PlannedDate, PlanStatus, IsDraft, CreatedDate, CreatedById) VALUES (@PlanManagementId, @PlanUnitId, 6, 9, N'ING. APAZA', CONVERT(datetime2, '2023-07-21', 23), 2, 0, @Today, @CreatedById);

-- ManagementPlan: PLAN_R8_17355
DECLARE @PlanManagementId int;
DECLARE @PlanUnitId int;
SELECT @PlanManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @PlanUnitId = Id FROM @UnitMap WHERE [Key] = N'17355';
INSERT INTO ManagementPlans (ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, Responsible, PlannedDate, PlanStatus, IsDraft, CreatedDate, CreatedById) VALUES (@PlanManagementId, @PlanUnitId, 1, 1, N'ING. EVER HERBAS', NULL, 0, 0, @Today, @CreatedById);

COMMIT TRANSACTION;
