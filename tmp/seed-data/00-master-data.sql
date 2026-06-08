-- ============================================================
-- 00 MASTER DATA
-- ============================================================
-- Faculty: Facultad de Gastronomia y Turismo - Carrera de Gastronomía
SET @EntityId = NULL;
MERGE Faculties AS target USING (SELECT N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía' AS Name, N'GASTRO' AS Code, N'Facultad que administra laboratorios de cocina H-1 a H-7' AS Description) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET Code = COALESCE(target.Code, source.Code), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (source.Name, source.Code, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Faculties WHERE Name = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
IF NOT EXISTS (SELECT 1 FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía') INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía', @EntityId);

-- Faculty: Facultad de Ciencias de la Salud - Carrera de Medicina
SET @EntityId = NULL;
MERGE Faculties AS target USING (SELECT N'Facultad de Ciencias de la Salud - Carrera de Medicina' AS Name, N'MEDIC' AS Code, N'Administra laboratorios clínicos en Edificio América' AS Description) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET Code = COALESCE(target.Code, source.Code), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (source.Name, source.Code, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Faculties WHERE Name = N'Facultad de Ciencias de la Salud - Carrera de Medicina';
IF NOT EXISTS (SELECT 1 FROM @FacultyMap WHERE [Key] = N'Facultad de Ciencias de la Salud - Carrera de Medicina') INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Medicina', @EntityId);

-- Faculty: Facultad de Ciencias de la Salud - Carrera de Nutricion
SET @EntityId = NULL;
MERGE Faculties AS target USING (SELECT N'Facultad de Ciencias de la Salud - Carrera de Nutricion' AS Name, NULL AS Code, NULL AS Description) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET Code = COALESCE(target.Code, source.Code), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (Name, Code, Description, Status, CreatedDate, CreatedById) VALUES (source.Name, source.Code, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Faculties WHERE Name = N'Facultad de Ciencias de la Salud - Carrera de Nutricion';
IF NOT EXISTS (SELECT 1 FROM @FacultyMap WHERE [Key] = N'Facultad de Ciencias de la Salud - Carrera de Nutricion') INSERT INTO @FacultyMap ([Key], Id) VALUES (N'Facultad de Ciencias de la Salud - Carrera de Nutricion', @EntityId);

-- Career: Gastronomía
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Careers AS target USING (SELECT N'Gastronomía' AS Name) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET FacultadId = COALESCE(target.FacultadId, @RelatedId) WHEN NOT MATCHED THEN INSERT (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (source.Name, @RelatedId, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Careers WHERE Name = N'Gastronomía';
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Gastronomía') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Gastronomía', @EntityId);

-- Career: Turismo y Hoteleria
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Careers AS target USING (SELECT N'Turismo y Hoteleria' AS Name) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET FacultadId = COALESCE(target.FacultadId, @RelatedId) WHEN NOT MATCHED THEN INSERT (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (source.Name, @RelatedId, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Careers WHERE Name = N'Turismo y Hoteleria';
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Turismo y Hoteleria') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Turismo y Hoteleria', @EntityId);

-- Career: Nutricion y Dietetica
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Ciencias de la Salud - Carrera de Nutricion';
MERGE Careers AS target USING (SELECT N'Nutricion y Dietetica' AS Name) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET FacultadId = COALESCE(target.FacultadId, @RelatedId) WHEN NOT MATCHED THEN INSERT (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (source.Name, @RelatedId, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Careers WHERE Name = N'Nutricion y Dietetica';
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Nutricion y Dietetica') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Nutricion y Dietetica', @EntityId);

-- Career: Ciencias Gastronomicas
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Careers AS target USING (SELECT N'Ciencias Gastronomicas' AS Name) AS source ON target.Name = source.Name WHEN MATCHED THEN UPDATE SET FacultadId = COALESCE(target.FacultadId, @RelatedId) WHEN NOT MATCHED THEN INSERT (Name, FacultadId, Status, CreatedDate, CreatedById) VALUES (source.Name, @RelatedId, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Careers WHERE Name = N'Ciencias Gastronomicas';
IF NOT EXISTS (SELECT 1 FROM @CareerMap WHERE [Key] = N'Ciencias Gastronomicas') INSERT INTO @CareerMap ([Key], Id) VALUES (N'Ciencias Gastronomicas', @EntityId);

-- Laboratory: H-1
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-1' AS Code, N'Laboratorio de Cocina H-1' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-1';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-1') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-1', @EntityId);

-- Laboratory: H-2
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-2' AS Code, N'Laboratorio de Cocina H-2' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-2';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-2') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-2', @EntityId);

-- Laboratory: H-4
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-4' AS Code, N'Laboratorio de Cocina H-4' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-4';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-4') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-4', @EntityId);

-- Laboratory: H-5
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-5' AS Code, N'Laboratorio de Cocina H-5' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-5';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-5') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-5', @EntityId);

-- Laboratory: H-6
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-6' AS Code, N'Laboratorio de Cocina H-6' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-6';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-6') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-6', @EntityId);

-- Laboratory: H-3
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-3' AS Code, N'Laboratorio Panadería y Pastelería H-3' AS Name, N'Campus Tiquipaya' AS Floor, N'Panadería | Laboratorio Area Caliente' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-3';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-3') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-3', @EntityId);

-- Laboratory: H-7
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-7' AS Code, N'Laboratorio de Cocina H-7' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocina Industrial | Laboratorio Frio' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-7';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-7') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-7', @EntityId);

-- Laboratory: K-1
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'K-1' AS Code, N'Laboratorio K-1 Cocteleria' AS Name, N'Campus Tiquipaya' AS Floor, N'Cocteleria | Cocteleria y Barismo' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'K-1';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'K-1') INSERT INTO @LabMap ([Key], Id) VALUES (N'K-1', @EntityId);

-- Laboratory: CIRCULACION
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'CIRCULACION' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Área común | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'CIRCULACION';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'CIRCULACION') INSERT INTO @LabMap ([Key], Id) VALUES (N'CIRCULACION', @EntityId);

-- Laboratory: OFICINAS 1ER PISO
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'OFICINAS 1ER PISO' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Oficinas | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'OFICINAS 1ER PISO';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'OFICINAS 1ER PISO') INSERT INTO @LabMap ([Key], Id) VALUES (N'OFICINAS 1ER PISO', @EntityId);

-- Laboratory: OFICINAS PLANTA BAJA
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'OFICINAS PLANTA BAJA' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Oficinas | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'OFICINAS PLANTA BAJA';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'OFICINAS PLANTA BAJA') INSERT INTO @LabMap ([Key], Id) VALUES (N'OFICINAS PLANTA BAJA', @EntityId);

-- Laboratory: H-102
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-102' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Área común | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-102';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-102') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-102', @EntityId);

-- Laboratory: H-103
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-103' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Área común | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-103';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-103') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-103', @EntityId);

-- Laboratory: H-104
SET @EntityId = NULL; SET @RelatedId = NULL;
SELECT @RelatedId = Id FROM @FacultyMap WHERE [Key] = N'Facultad de Gastronomia y Turismo - Carrera de Gastronomía';
MERGE Laboratories AS target USING (SELECT N'H-104' AS Code, N'Área de Circulación General' AS Name, N'Campus Tiquipaya' AS Floor, N'Área común | Pasillos y áreas comunes' AS Description) AS source ON target.Code = source.Code WHEN MATCHED THEN UPDATE SET Name = source.Name, FacultyId = COALESCE(target.FacultyId, @RelatedId), Floor = COALESCE(target.Floor, source.Floor), Description = COALESCE(target.Description, source.Description) WHEN NOT MATCHED THEN INSERT (FacultyId, Code, Name, Floor, Description, Status, CreatedDate, CreatedById) VALUES (@RelatedId, source.Code, source.Name, source.Floor, source.Description, 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Laboratories WHERE Code = N'H-104';
IF NOT EXISTS (SELECT 1 FROM @LabMap WHERE [Key] = N'H-104') INSERT INTO @LabMap ([Key], Id) VALUES (N'H-104', @EntityId);

-- Person: BRUNO NOVILLO
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'BRUNO NOVILLO';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 1 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'BRUNO NOVILLO', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'BRUNO NOVILLO') INSERT INTO @PersonMap ([Key], Id) VALUES (N'BRUNO NOVILLO', @EntityId);

-- Person: CASATERMO SRL
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = N'CASATERMO SRL';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 5 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@EntityId, 1, N'CASATERMO SRL', N'Cochabamba', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'CASATERMO SRL') INSERT INTO @PersonMap ([Key], Id) VALUES (N'CASATERMO SRL', @EntityId);

-- Person: CONSULTORA PROYECTOS Y SERVICIOS PCS
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Externs e ON e.Id = p.Id WHERE e.Name = N'CONSULTORA PROYECTOS Y SERVICIOS PCS';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 5 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Externs (Id, IsEntity, Name, Address, ExternStatus) VALUES (@EntityId, 1, N'CONSULTORA PROYECTOS Y SERVICIOS PCS', N'Cochabamba', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'CONSULTORA PROYECTOS Y SERVICIOS PCS') INSERT INTO @PersonMap ([Key], Id) VALUES (N'CONSULTORA PROYECTOS Y SERVICIOS PCS', @EntityId);

-- Person: DOCENTE MARTINEZ
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'DOCENTE MARTINEZ';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 2 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'DOCENTE MARTINEZ', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'DOCENTE MARTINEZ') INSERT INTO @PersonMap ([Key], Id) VALUES (N'DOCENTE MARTINEZ', @EntityId);

-- Person: ING. APAZA
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. APAZA';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 1 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'ING. APAZA', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'ING. APAZA') INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. APAZA', @EntityId);

-- Person: ING. EVER HERBAS
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. EVER HERBAS';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 1 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'ING. EVER HERBAS', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'ING. EVER HERBAS') INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. EVER HERBAS', @EntityId);

-- Person: ING. SARA PEREZ YAÑEZ
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'ING. SARA PEREZ YAÑEZ';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 99 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'ING. SARA PEREZ YAÑEZ', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'ING. SARA PEREZ YAÑEZ') INSERT INTO @PersonMap ([Key], Id) VALUES (N'ING. SARA PEREZ YAÑEZ', @EntityId);

-- Person: TALLER UNIVALLE
SET @EntityId = NULL;
SELECT @EntityId = p.Id FROM People p INNER JOIN Interns i ON i.Id = p.Id WHERE i.Name = N'TALLER UNIVALLE';
IF @EntityId IS NULL
BEGIN
    MERGE People AS target USING (SELECT 1 AS Category) AS source ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Status, Category, Email, PhoneNumber, CreatedDate, CreatedById) VALUES (0, source.Category, NULL, NULL, @Today, @CreatedById);
    SET @EntityId = CONVERT(int, SCOPE_IDENTITY());
    INSERT INTO Interns (Id, Name, InternStatus) VALUES (@EntityId, N'TALLER UNIVALLE', 0);
END
IF NOT EXISTS (SELECT 1 FROM @PersonMap WHERE [Key] = N'TALLER UNIVALLE') INSERT INTO @PersonMap ([Key], Id) VALUES (N'TALLER UNIVALLE', @EntityId);

-- Management: 2023-1
SET @EntityId = NULL;
MERGE Managements AS target USING (SELECT 2023 AS [Year], 1 AS Semester, N'2023-1' AS Code) AS source ON target.[Year] = source.[Year] AND target.Semester = source.Semester AND target.Type = 0 WHEN MATCHED THEN UPDATE SET Code = source.Code WHEN NOT MATCHED THEN INSERT ([Year], Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (source.[Year], source.Semester, source.Code, N'Gestion historica 2023-1', 0, N'Carga historica seed', 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Managements WHERE [Year] = 2023 AND Semester = 1 AND Type = 0;
IF NOT EXISTS (SELECT 1 FROM @ManagementMap WHERE [Key] = N'2023-1') INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2023-1', @EntityId);

-- Management: 2023-2
SET @EntityId = NULL;
MERGE Managements AS target USING (SELECT 2023 AS [Year], 2 AS Semester, N'2023-2' AS Code) AS source ON target.[Year] = source.[Year] AND target.Semester = source.Semester AND target.Type = 0 WHEN MATCHED THEN UPDATE SET Code = source.Code WHEN NOT MATCHED THEN INSERT ([Year], Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (source.[Year], source.Semester, source.Code, N'Gestion historica 2023-2', 0, N'Carga historica seed', 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Managements WHERE [Year] = 2023 AND Semester = 2 AND Type = 0;
IF NOT EXISTS (SELECT 1 FROM @ManagementMap WHERE [Key] = N'2023-2') INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2023-2', @EntityId);

-- Management: 2025-1
SET @EntityId = NULL;
MERGE Managements AS target USING (SELECT 2025 AS [Year], 1 AS Semester, N'2025-1' AS Code) AS source ON target.[Year] = source.[Year] AND target.Semester = source.Semester AND target.Type = 0 WHEN MATCHED THEN UPDATE SET Code = source.Code WHEN NOT MATCHED THEN INSERT ([Year], Semester, Code, Description, Status, Responsible, Type, CreatedDate, CreatedById) VALUES (source.[Year], source.Semester, source.Code, N'Gestion historica 2025-1', 0, N'Carga historica seed', 0, @Today, @CreatedById);
SELECT @EntityId = Id FROM Managements WHERE [Year] = 2025 AND Semester = 1 AND Type = 0;
IF NOT EXISTS (SELECT 1 FROM @ManagementMap WHERE [Key] = N'2025-1') INSERT INTO @ManagementMap ([Key], Id) VALUES (N'2025-1', @EntityId);

-- 00 MASTER DATA: Excel directo=32; Inferido=0

