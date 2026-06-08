-- ============================================================
-- 01 EQUIPMENT
-- ============================================================
-- Equipment: ABATIDOR FASTER
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ABATIDOR FASTER' AS Name, N'AFINOX' AS Brand, N'FASTER 5T GF 230V' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ABATIDOR FASTER REFRIGERANTE 5 BANDEJAS , MATERIAL: ACERO INOXIDABLE, COLOR: MARFIL/BLANCO, MARCA: AFINOX, MODELO: FASTER 5T GF 230V, SERIE: 3016085604') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ABATIDOR FASTER REFRIGERANTE 5 BANDEJAS , MATERIAL: ACERO INOXIDABLE, COLOR: MARFIL/BLANCO, MARCA: AFINOX, MODELO: FASTER 5T GF 230V, SERIE: 3016085604', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ABATIDOR FASTER' AND ISNULL(Brand, N'') = ISNULL(N'AFINOX', N'') AND ISNULL(Model, N'') = ISNULL(N'FASTER 5T GF 230V', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V', @EntityId);

-- Equipment: ACCES POINT
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ACCES POINT' AS Name, N'CISCO' AS Brand, N'AIR-CAP370' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9 , SERIE: SFTX1927S0UL') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9 , SERIE: SFTX1927S0UL', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ACCES POINT' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP370', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP370') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ACCES POINT|CISCO|AIR-CAP370', @EntityId);

-- Equipment: ACCES POINT
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ACCES POINT' AS Name, N'CISCO' AS Brand, N'AIR-CAP3702E-A-K9' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9, SERIE: SFTX1927S0VR') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ACCES POINT (ANTENA WIFI) MODEM INALAMBRICO, 802.11 AC. CON CUATRO ANTENAS, AP LICENCIA PARA CISCO. , CAPACIDAD: 2.4GHZ 2DB I/5 DIPOLE , COLOR: BLANCO, MARCA: CISCO, MODELO: AIR-CAP3702E-A-K9, SERIE: SFTX1927S0VR', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ACCES POINT' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'AIR-CAP3702E-A-K9', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9', @EntityId);

-- Equipment: ALL IN ONE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ALL IN ONE' AS Name, N'HP' AS Brand, N'24-E015LA' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ALL IN ONE MEMORIA 8GB, DISCO DURO 1 TB, COPIADOR DE DVD, PANTALLA DE 23 PULG , COLOR: BLANCO , MARCA: HP, MODELO: 24-E015LA, SERIE: 8CC80516KV') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ALL IN ONE MEMORIA 8GB, DISCO DURO 1 TB, COPIADOR DE DVD, PANTALLA DE 23 PULG , COLOR: BLANCO , MARCA: HP, MODELO: 24-E015LA, SERIE: 8CC80516KV', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'HP', N'') AND ISNULL(Model, N'') = ISNULL(N'24-E015LA', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|HP|24-E015LA') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|HP|24-E015LA', @EntityId);

-- Equipment: ALL IN ONE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ALL IN ONE' AS Name, N'LENOVO' AS Brand, N'10BB-A0C900' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ALL IN ONE 500 GB D.D. - 4.00 GB DE RAM, CON MOUSE OPTICO, GRABADOR DE CD/DVD , CAPACIDAD: CORE I5 - 2.90 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: 10BB-A0C900, SERIE: MJ00UZ5R') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ALL IN ONE 500 GB D.D. - 4.00 GB DE RAM, CON MOUSE OPTICO, GRABADOR DE CD/DVD , CAPACIDAD: CORE I5 - 2.90 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: 10BB-A0C900, SERIE: MJ00UZ5R', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'10BB-A0C900', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|10BB-A0C900') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|LENOVO|10BB-A0C900', @EntityId);

-- Equipment: ALL IN ONE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ALL IN ONE' AS Name, N'LENOVO' AS Brand, N'THINK CENTRE M73Z' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ALL IN ONE 500 GB D.D. 4.00 GB DE RAM,GRABADOR DE DVD, MOUSE OPTICO. , CAPACIDAD: CORE I5-2.40 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE M73Z') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ALL IN ONE 500 GB D.D. 4.00 GB DE RAM,GRABADOR DE DVD, MOUSE OPTICO. , CAPACIDAD: CORE I5-2.40 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE M73Z', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ALL IN ONE' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE M73Z', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|THINK CENTRE M73Z') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ALL IN ONE|LENOVO|THINK CENTRE M73Z', @EntityId);

-- Equipment: AMASADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'AMASADORA' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'AMAZADORA ESPIRAL DE DOBLE VELOCIDAD, CON CAJA DE CONTROL, FUENTE DE AMASADO INOX , CAPACIDAD: 80 LITROS, COLOR: BLANCO/METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'AMAZADORA ESPIRAL DE DOBLE VELOCIDAD, CON CAJA DE CONTROL, FUENTE DE AMASADO INOX , CAPACIDAD: 80 LITROS, COLOR: BLANCO/METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'AMASADORA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'AMASADORA|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'AMASADORA|WILDA|', @EntityId);

-- Equipment: APARATO TELEFONICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'APARATO TELEFONICO' AS Name, N'CISCO' AS Brand, N'CP-3905' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'APARATO TELEFONICO CON CABLES DE CONECCION , MATERIAL: PLASTICO, COLOR: NEGRO, MARCA: CISCO, MODELO: CP-3905, SERIE: FCH2045GJVT') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'APARATO TELEFONICO CON CABLES DE CONECCION , MATERIAL: PLASTICO, COLOR: NEGRO, MARCA: CISCO, MODELO: CP-3905, SERIE: FCH2045GJVT', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'APARATO TELEFONICO' AND ISNULL(Brand, N'') = ISNULL(N'CISCO', N'') AND ISNULL(Model, N'') = ISNULL(N'CP-3905', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'APARATO TELEFONICO|CISCO|CP-3905') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'APARATO TELEFONICO|CISCO|CP-3905', @EntityId);

-- Equipment: BACHA DE LAVADO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BACHA DE LAVADO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BACHA DE LAVADO CON SOPORTE ADPATADO , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BACHA DE LAVADO CON SOPORTE ADPATADO , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BACHA DE LAVADO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BACHA DE LAVADO||', @EntityId);

-- Equipment: BALANZA ELECTRONICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BALANZA ELECTRONICA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713783') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713783', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA||', @EntityId);

-- Equipment: BALANZA ELECTRONICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BALANZA ELECTRONICA' AS Name, N'ELECTRONIC SCALE' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713831') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BALANZA ELECTRONICA DE 30 KILOS , MATERIAL: METAL, COLOR: PLOMO/NEGRO, MARCA: ELECTRONIC SCALE , SERIE: 14092713831', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(N'ELECTRONIC SCALE', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|ELECTRONIC SCALE|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA|ELECTRONIC SCALE|', @EntityId);

-- Equipment: BALANZA ELECTRONICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BALANZA ELECTRONICA' AS Name, N'OHAUS' AS Brand, N'RANGER R31P30' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BALANZA ELECTRONICA DE 30 KG X 1 GR , MATERIAL: METAL, COLOR: PLOMO/AZUL ACERO , MARCA: OHAUS, MODELO: RANGER R31P30, SERIE: 8336510968') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BALANZA ELECTRONICA DE 30 KG X 1 GR , MATERIAL: METAL, COLOR: PLOMO/AZUL ACERO , MARCA: OHAUS, MODELO: RANGER R31P30, SERIE: 8336510968', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BALANZA ELECTRONICA' AND ISNULL(Brand, N'') = ISNULL(N'OHAUS', N'') AND ISNULL(Model, N'') = ISNULL(N'RANGER R31P30', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30', @EntityId);

-- Equipment: BANCA DE MADERA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BANCA DE MADERA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BANCA , MATERIAL: MADERA, MEDIDAS: 42X180X30 cm., COLOR: BLANCO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BANCA , MATERIAL: MADERA, MEDIDAS: 42X180X30 cm., COLOR: BLANCO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BANCA DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BANCA DE MADERA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BANCA DE MADERA||', @EntityId);

-- Equipment: BATIDORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BATIDORA' AS Name, N'KITCHENAID' AS Brand, N'5KSM7591' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467699') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467699', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'') AND ISNULL(Model, N'') = ISNULL(N'5KSM7591', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|KITCHENAID|5KSM7591', @EntityId);

-- Equipment: BATIDORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BATIDORA' AS Name, N'KITCHENAID' AS Brand, N'5KSM7591,' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467732') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BATIDORA ACCESORIOS: BOL DE ACERO INOX DE 6.9L, ESCUDO VERTEDOR, BATIDOR VARILLA, GANCHO AMASADOR, BATIDOR PLANO, , MATERIAL: INOX , COLOR: PLOMO , MARCA: KITCHENAID, MODELO: 5KSM7591, SERIE: W74467732', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'KITCHENAID', N'') AND ISNULL(Model, N'') = ISNULL(N'5KSM7591,', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591,') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|KITCHENAID|5KSM7591,', @EntityId);

-- Equipment: BATIDORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BATIDORA' AS Name, N'OSTER' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BATIDORA BOL DE ACERO INOX, CON 2 ASPAS , MATERIAL: IMOX, MARCA: OSTER') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BATIDORA BOL DE ACERO INOX, CON 2 ASPAS , MATERIAL: IMOX, MARCA: OSTER', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BATIDORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BATIDORA|OSTER|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BATIDORA|OSTER|', @EntityId);

-- Equipment: BEBEDERO DE AGUA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'BEBEDERO DE AGUA' AS Name, N'IBBL' AS Brand, N'BAG 40' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'BEBEDERO DE AGUA CON FILTRO , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 98X31X31 cm., COLOR: AZUL ELECTRICO, MARCA: IBBL, MODELO: BAG 40, SERIE: 451P287128') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'BEBEDERO DE AGUA CON FILTRO , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 98X31X31 cm., COLOR: AZUL ELECTRICO, MARCA: IBBL, MODELO: BAG 40, SERIE: 451P287128', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'BEBEDERO DE AGUA' AND ISNULL(Brand, N'') = ISNULL(N'IBBL', N'') AND ISNULL(Model, N'') = ISNULL(N'BAG 40', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'BEBEDERO DE AGUA|IBBL|BAG 40') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'BEBEDERO DE AGUA|IBBL|BAG 40', @EntityId);

-- Equipment: CAFETERA IND
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAFETERA IND' AS Name, N'ASTORIA' AS Brand, N'INDUS. ITALIANA' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAFETERA INDUSTRIAL SEMIAUTOMATICO,MANDO MANUAL CON DOTONES ELECTRONICOS, CARROCERIA CON PANELES DEACERO INOX Y ACERO, MOTOBOMBA INCORPORADA, DOS LANZAS DE VAPOR , MATERIAL: INOX/ACERO, CAPACIDAD: 10,5, MEDIDAS: 70X54X52 cm., COLOR: NEGRO, MARCA: ASTORIA, MODELO: INDUS. ITALIANA, SERIE: 913677') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAFETERA INDUSTRIAL SEMIAUTOMATICO,MANDO MANUAL CON DOTONES ELECTRONICOS, CARROCERIA CON PANELES DEACERO INOX Y ACERO, MOTOBOMBA INCORPORADA, DOS LANZAS DE VAPOR , MATERIAL: INOX/ACERO, CAPACIDAD: 10,5, MEDIDAS: 70X54X52 cm., COLOR: NEGRO, MARCA: ASTORIA, MODELO: INDUS. ITALIANA, SERIE: 913677', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAFETERA IND' AND ISNULL(Brand, N'') = ISNULL(N'ASTORIA', N'') AND ISNULL(Model, N'') = ISNULL(N'INDUS. ITALIANA', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAFETERA IND|ASTORIA|INDUS. ITALIANA') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAFETERA IND|ASTORIA|INDUS. ITALIANA', @EntityId);

-- Equipment: CAMARA CONSERVADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMARA CONSERVADORA' AS Name, N'ASBER' AS Brand, N'ARR-43' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMARA CONSERVADORA CON DOS PUERTAS , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 220V 50/60HZ, COLOR: INOX, MARCA: ASBER, MODELO: ARR-43, SERIE: 11090021M') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMARA CONSERVADORA CON DOS PUERTAS , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 220V 50/60HZ, COLOR: INOX, MARCA: ASBER, MODELO: ARR-43, SERIE: 11090021M', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMARA CONSERVADORA' AND ISNULL(Brand, N'') = ISNULL(N'ASBER', N'') AND ISNULL(Model, N'') = ISNULL(N'ARR-43', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMARA CONSERVADORA|ASBER|ARR-43') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA CONSERVADORA|ASBER|ARR-43', @EntityId);

-- Equipment: CAMARA DE VIDEO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMARA DE VIDEO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, IR DOMO,1/3 SONY ICX633+NEXTCHIP-480 TVL IR LED: E5X24PCS,LENTE 3.6 MM FUENTE DC12V 500 MA BALUMS , COLOR: BLANCO, MARCA: DESL') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, IR DOMO,1/3 SONY ICX633+NEXTCHIP-480 TVL IR LED: E5X24PCS,LENTE 3.6 MM FUENTE DC12V 500 MA BALUMS , COLOR: BLANCO, MARCA: DESL', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO||', @EntityId);

-- Equipment: CAMARA DE VIDEO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMARA DE VIDEO' AS Name, N'HIKVISION' AS Brand, N'DS-2CEE55A2N-IRN' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMARA DE VIDEO DOMO DOMO ANTIVANDALICA VERDADERO (ICR) C/IR 1.3" HIGH RESOL IP66 ALTA RESOLUCION: 700TVL/01 LUX. BALUMS TRANSMISOR DE SEÑAL, FUENTE DE PODER. , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS-2CEE55A2N-IRN, SERIE: 457428659') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMARA DE VIDEO DOMO DOMO ANTIVANDALICA VERDADERO (ICR) C/IR 1.3" HIGH RESOL IP66 ALTA RESOLUCION: 700TVL/01 LUX. BALUMS TRANSMISOR DE SEÑAL, FUENTE DE PODER. , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS-2CEE55A2N-IRN, SERIE: 457428659', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'') AND ISNULL(Model, N'') = ISNULL(N'DS-2CEE55A2N-IRN', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN', @EntityId);

-- Equipment: CAMARA DE VIDEO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMARA DE VIDEO' AS Name, N'HIKVISION' AS Brand, N'DS2CC5192N-IR1' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS2CC5192N-IR1') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN , COLOR: BLANCO, MARCA: HIKVISION, MODELO: DS2CC5192N-IR1', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'HIKVISION', N'') AND ISNULL(Model, N'') = ISNULL(N'DS2CC5192N-IR1', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1', @EntityId);

-- Equipment: CAMARA DE VIDEO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMARA DE VIDEO' AS Name, N'SONY BALUMS' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, ANTIBANDALICAIR , CAPACIDAD: 480 TVL 1/3 " , COLOR: BLANCO , MARCA: SONY BALUMS') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMARA DE VIDEO DE ALTA RESOLUCIÓN, ANTIBANDALICAIR , CAPACIDAD: 480 TVL 1/3 " , COLOR: BLANCO , MARCA: SONY BALUMS', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMARA DE VIDEO' AND ISNULL(Brand, N'') = ISNULL(N'SONY BALUMS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|SONY BALUMS|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMARA DE VIDEO|SONY BALUMS|', @EntityId);

-- Equipment: CAMPANA DE EXTRACCION
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMPANA DE EXTRACCION' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMPANA CENTRAL DE EXTRACCION FILTROS DESMONTABLES FOCOS Y TOMA DE CORRIENTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 120X120 cm., COLOR: METALICO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMPANA CENTRAL DE EXTRACCION FILTROS DESMONTABLES FOCOS Y TOMA DE CORRIENTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 120X120 cm., COLOR: METALICO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMPANA DE EXTRACCION' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMPANA DE EXTRACCION||', @EntityId);

-- Equipment: CAMPANA DE EXTRACCION
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CAMPANA DE EXTRACCION' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CAMPANA DE EXTRACCIÓN , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 320X120 cm., COLOR: PLATEADO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CAMPANA DE EXTRACCIÓN , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 320X120 cm., COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CAMPANA DE EXTRACCION' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CAMPANA DE EXTRACCION|WILDA|', @EntityId);

-- Equipment: CARRITO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CARRITO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CARRITO PORTA GARRAFAS, CON DOS RUEDAS , MATERIAL: METALICO , COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CARRITO PORTA GARRAFAS, CON DOS RUEDAS , MATERIAL: METALICO , COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CARRITO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CARRITO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CARRITO||', @EntityId);

-- Equipment: CASILLERO METALICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CASILLERO METALICO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CASILLERO , MATERIAL: METAL, MEDIDAS: 180X80X43 cm., BANDEJAS/DIVISIONES: 8 PUERTAS, COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CASILLERO , MATERIAL: METAL, MEDIDAS: 180X80X43 cm., BANDEJAS/DIVISIONES: 8 PUERTAS, COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CASILLERO METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CASILLERO METALICO||', @EntityId);

-- Equipment: COCINA INDUSTRIAL
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'COCINA INDUSTRIAL' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'COCINA INDUSTRIAL GASTRONOMICA A GAS, DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'COCINA INDUSTRIAL GASTRONOMICA A GAS, DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'COCINA INDUSTRIAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'COCINA INDUSTRIAL|WILDA|', @EntityId);

-- Equipment: COCINA INDUSTRIAL
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'COCINA INDUSTRIAL' AS Name, N'WILDA' AS Brand, N'AISI304L 2B' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'COCINA CENTRAL GASTRONOMICA INDUSTRIAL DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X92X92 cm., COLOR: METALICO, MARCA: WILDA, MODELO: AISI304L 2B') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'COCINA CENTRAL GASTRONOMICA INDUSTRIAL DE 4 HORNALLAS , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X92X92 cm., COLOR: METALICO, MARCA: WILDA, MODELO: AISI304L 2B', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'COCINA INDUSTRIAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'AISI304L 2B', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'COCINA INDUSTRIAL|WILDA|AISI304L 2B', @EntityId);

-- Equipment: CPU DE ESCRITORIO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CPU DE ESCRITORIO' AS Name, N'LENOVO' AS Brand, N'THINK CENTRE' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CPU 500 GB DD, 4.00 GB DE RAM, GRABADOR DE DVD, MOUSE OPTICO , CAPACIDAD: CORE I5 - 3.10 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE, SERIE: MJNHDWZ') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CPU 500 GB DD, 4.00 GB DE RAM, GRABADOR DE DVD, MOUSE OPTICO , CAPACIDAD: CORE I5 - 3.10 GHZ, COLOR: NEGRO, MARCA: LENOVO, MODELO: THINK CENTRE, SERIE: MJNHDWZ', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CPU DE ESCRITORIO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'THINK CENTRE', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE', @EntityId);

-- Equipment: CREDENZA DE MELAMINA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'CREDENZA DE MELAMINA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'CREDENZA CON 6 MODULOS (DE 3 UNID. C/U) DE ARCHIVOS, 4 PUERTAS CON CHAPAS Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X40,5X93 cm., BANDEJAS/DIVISIONES: DON 2 DIVISIONES REGULABLES, COLOR: WENGUE/NEGRO/PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'CREDENZA CON 6 MODULOS (DE 3 UNID. C/U) DE ARCHIVOS, 4 PUERTAS CON CHAPAS Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X40,5X93 cm., BANDEJAS/DIVISIONES: DON 2 DIVISIONES REGULABLES, COLOR: WENGUE/NEGRO/PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'CREDENZA DE MELAMINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'CREDENZA DE MELAMINA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'CREDENZA DE MELAMINA||', @EntityId);

-- Equipment: ENVASADORA AL VACIO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ENVASADORA AL VACIO' AS Name, N'VENTUS' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ENVASADORA AL VACIO DE 310MM VSV310, CON BOMBA DE VACIO DE 4 M3/H , MARCA: VENTUS, SERIE: 202031007') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ENVASADORA AL VACIO DE 310MM VSV310, CON BOMBA DE VACIO DE 4 M3/H , MARCA: VENTUS, SERIE: 202031007', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ENVASADORA AL VACIO' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ENVASADORA AL VACIO|VENTUS|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ENVASADORA AL VACIO|VENTUS|', @EntityId);

-- Equipment: ESCRITORIO DE MADERA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESCRITORIO DE MADERA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESCRITORIO CON 2 CHAPAS (CON LLAVES) , MATERIAL: MADERA, MEDIDAS: 78X120X63 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESCRITORIO CON 2 CHAPAS (CON LLAVES) , MATERIAL: MADERA, MEDIDAS: 78X120X63 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESCRITORIO DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO DE MADERA||', @EntityId);

-- Equipment: ESCRITORIO DE MELAMINA MODULAR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESCRITORIO DE MELAMINA MODULAR' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESCRITORIO DE MELAMINA FLOW OPERATIVO, PORTA TECLADO , CON PASA CABLE, PATAS DE ALUMIO ANODIZADO , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X180X74 cm., COLOR: WENGUE/NEGRO/PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESCRITORIO DE MELAMINA FLOW OPERATIVO, PORTA TECLADO , CON PASA CABLE, PATAS DE ALUMIO ANODIZADO , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 160X180X74 cm., COLOR: WENGUE/NEGRO/PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESCRITORIO DE MELAMINA MODULAR' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MELAMINA MODULAR||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO DE MELAMINA MODULAR||', @EntityId);

-- Equipment: ESCRITORIO METALICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESCRITORIO METALICO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESCRITORIO METALICO CON CUERINA Y VIDRIO TRIPLE , MATERIAL: METAL Y VIDRIO, MEDIDAS: 79X120X70 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: BEIGE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESCRITORIO METALICO CON CUERINA Y VIDRIO TRIPLE , MATERIAL: METAL Y VIDRIO, MEDIDAS: 79X120X70 cm., BANDEJAS/DIVISIONES: 4 CAJONES, COLOR: BEIGE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESCRITORIO METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO METALICO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESCRITORIO METALICO||', @EntityId);

-- Equipment: ESTANTE BAR DE MADERA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESTANTE BAR DE MADERA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESTANTE BAR (CON 4 FOCOS) , MATERIAL: MADERA, MEDIDAS: 227X431X60 cm., BANDEJAS/DIVISIONES: 12 PUERTAS, COLOR: CAFE CLARO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESTANTE BAR (CON 4 FOCOS) , MATERIAL: MADERA, MEDIDAS: 227X431X60 cm., BANDEJAS/DIVISIONES: 12 PUERTAS, COLOR: CAFE CLARO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESTANTE BAR DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESTANTE BAR DE MADERA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE BAR DE MADERA||', @EntityId);

-- Equipment: ESTANTE METÁLICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESTANTE METÁLICO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESTANTE TIPO MECANO , MATERIAL: METAL, MEDIDAS: 200X90X30 cm., BANDEJAS/DIVISIONES: 5 BANDEJAS, COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESTANTE TIPO MECANO , MATERIAL: METAL, MEDIDAS: 200X90X30 cm., BANDEJAS/DIVISIONES: 5 BANDEJAS, COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESTANTE METÁLICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESTANTE METALICO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE METALICO||', @EntityId);

-- Equipment: ESTANTE PORTA BANDEJAS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ESTANTE PORTA BANDEJAS' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ESTANETE PORTA BAMBEJAS CON RODAPIES, PARA BANDEJAS DE PANADERIA , MATERIAL: METALICO , COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ESTANETE PORTA BAMBEJAS CON RODAPIES, PARA BANDEJAS DE PANADERIA , MATERIAL: METALICO , COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ESTANTE PORTA BANDEJAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ESTANTE PORTA BANDEJAS||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ESTANTE PORTA BANDEJAS||', @EntityId);

-- Equipment: EXTINTOR TIPO K
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR TIPO K' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR TIPO K DE 10 LTS, AGENTE ACETATO DE POTASIO (ACERO INOXIDABLE)EXTINFIRE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR TIPO K DE 10 LTS, AGENTE ACETATO DE POTASIO (ACERO INOXIDABLE)EXTINFIRE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR TIPO K' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR TIPO K||', @EntityId);

-- Equipment: EXTINTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR' AS Name, N'ABC' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR DE 8 KG , MARCA: ABC') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR DE 8 KG , MARCA: ABC', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'ABC', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|ABC|', @EntityId);

-- Equipment: EXTINTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR' AS Name, N'CYLINDERS' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'CYLINDERS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|CYLINDERS|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|CYLINDERS|', @EntityId);

-- Equipment: EXTINTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR' AS Name, N'FANACIM' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR ABC , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: FANACIM') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR ABC , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: FANACIM', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|FANACIM|', @EntityId);

-- Equipment: EXTINTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR' AS Name, N'MMB CILINDERS' AS Brand, N'FNC 10' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR ABC, CON MANGUERA , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: MMB CILINDERS, MODELO: FNC 10') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR ABC, CON MANGUERA , CAPACIDAD: 10 KG, COLOR: ROJO, MARCA: MMB CILINDERS, MODELO: FNC 10', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'MMB CILINDERS', N'') AND ISNULL(Model, N'') = ISNULL(N'FNC 10', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CILINDERS|FNC 10') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|MMB CILINDERS|FNC 10', @EntityId);

-- Equipment: EXTINTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTINTOR' AS Name, N'MMB CYLINDERS' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTINTOR ABC DE 12 KG. , COLOR: ROJO, MARCA: MMB CYLINDERS', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTINTOR' AND ISNULL(Brand, N'') = ISNULL(N'MMB CYLINDERS', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTINTOR|MMB CYLINDERS|', @EntityId);

-- Equipment: EXTRACTOR DE AIRE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTRACTOR DE AIRE' AS Name, N'LOREN SID' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTRACTOR DE AIRE CIRCULAR DE 4 ASPAS , MATERIAL: METALICO, CAPACIDAD: 1200 M3/H, MEDIDAS: 30 cm., COLOR: PLOMO, MARCA: LOREN SID') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTRACTOR DE AIRE CIRCULAR DE 4 ASPAS , MATERIAL: METALICO, CAPACIDAD: 1200 M3/H, MEDIDAS: 30 cm., COLOR: PLOMO, MARCA: LOREN SID', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE AIRE' AND ISNULL(Brand, N'') = ISNULL(N'LOREN SID', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE AIRE|LOREN SID|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE AIRE|LOREN SID|', @EntityId);

-- Equipment: EXTRACTOR DE HUMOS Y GRASAS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTRACTOR DE HUMOS Y GRASAS' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TODO EL CAMPO DE DESCRIPCION DETALLADA DEL ACTIVO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TODO EL CAMPO DE DESCRIPCION DETALLADA DEL ACTIVO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE HUMOS Y GRASAS||', @EntityId);

-- Equipment: EXTRACTOR DE HUMOS Y GRASAS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'EXTRACTOR DE HUMOS Y GRASAS' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'EXTRACTOR DE HUMOS Y GRASAS CON SISTEMA AUTOLIMPIANTE POR CENTRIFUGACION DE ASPAS, CON FILTROS DESMONTABLES, COLECTOR Y DEPURADOR DE GRASAS, CON MOTOR DE 3 HP , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'EXTRACTOR DE HUMOS Y GRASAS CON SISTEMA AUTOLIMPIANTE POR CENTRIFUGACION DE ASPAS, CON FILTROS DESMONTABLES, COLECTOR Y DEPURADOR DE GRASAS, CON MOTOR DE 3 HP , MATERIAL: ACERO INOXIDABLE, COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'EXTRACTOR DE HUMOS Y GRASAS' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|', @EntityId);

-- Equipment: FERMENTADOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'FERMENTADOR' AS Name, N'WILDA' AS Brand, N'A155 304L2B' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'FERMENTADOR ELECTRICO PARA 32 BANDEJAS , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 2 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'FERMENTADOR ELECTRICO PARA 32 BANDEJAS , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 2 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'FERMENTADOR' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'FERMENTADOR|WILDA|A155 304L2B') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FERMENTADOR|WILDA|A155 304L2B', @EntityId);

-- Equipment: FLITRO ABLANDADOR DE AGUA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'FLITRO ABLANDADOR DE AGUA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'FILTRO ABLANDADOR DE AGUA PARA CAFETERA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'FILTRO ABLANDADOR DE AGUA PARA CAFETERA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'FLITRO ABLANDADOR DE AGUA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'FLITRO ABLANDADOR DE AGUA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FLITRO ABLANDADOR DE AGUA||', @EntityId);

-- Equipment: FREIDORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'FREIDORA' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'FREIDORA CONEXIÓN A GAS, ELECTRICA, DE 2 CANASTILLOS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'FREIDORA CONEXIÓN A GAS, ELECTRICA, DE 2 CANASTILLOS , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'FREIDORA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'FREIDORA|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'FREIDORA|WILDA|', @EntityId);

-- Equipment: GARRAFA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'GARRAFA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'GARRAFA DE GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'GARRAFA DE GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'GARRAFA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'GARRAFA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GARRAFA||', @EntityId);

-- Equipment: GARRAFA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'GARRAFA' AS Name, N'FANACIM' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'GARRAFA PARA GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO, MARCA: FANACIM') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'GARRAFA PARA GLP , CAPACIDAD: 45 KG, COLOR: AMARILLO, MARCA: FANACIM', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'GARRAFA' AND ISNULL(Brand, N'') = ISNULL(N'FANACIM', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GARRAFA|FANACIM|', @EntityId);

-- Equipment: GAVETERO DE MELAMINA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'GAVETERO DE MELAMINA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'GAVETERO MOVIL, CON CHAPA Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 75X46X42 cm., BANDEJAS/DIVISIONES: CON 3 CAJONES, COLOR: WENGUE/NEGRO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'GAVETERO MOVIL, CON CHAPA Y DOS LLAVES , MATERIAL: MELAMINA DE 25MM, MEDIDAS: 75X46X42 cm., BANDEJAS/DIVISIONES: CON 3 CAJONES, COLOR: WENGUE/NEGRO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'GAVETERO DE MELAMINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'GAVETERO DE MELAMINA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'GAVETERO DE MELAMINA||', @EntityId);

-- Equipment: HORNO CONVECTOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'HORNO CONVECTOR' AS Name, N'ARIANNA' AS Brand, N'XEFT-04HS-ELDV' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'HORNO CONVECTOR ELECTRICO CONTROL LED , MATERIAL: INOX, MEDIDAS: 460X330 cm., MARCA: ARIANNA, MODELO: XEFT-04HS-ELDV, SERIE: 2021L0105834') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'HORNO CONVECTOR ELECTRICO CONTROL LED , MATERIAL: INOX, MEDIDAS: 460X330 cm., MARCA: ARIANNA, MODELO: XEFT-04HS-ELDV, SERIE: 2021L0105834', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'HORNO CONVECTOR' AND ISNULL(Brand, N'') = ISNULL(N'ARIANNA', N'') AND ISNULL(Model, N'') = ISNULL(N'XEFT-04HS-ELDV', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV', @EntityId);

-- Equipment: HORNO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'HORNO' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'HORNO GASTRONOMICO DE 6 BANDEJAS, CON FERMENTADOR, (CON MOTOR MARCA WEG TRIFASICO) , COLOR: METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'HORNO GASTRONOMICO DE 6 BANDEJAS, CON FERMENTADOR, (CON MOTOR MARCA WEG TRIFASICO) , COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'HORNO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'HORNO|WILDA|', @EntityId);

-- Equipment: LAVAPLATOS DE ACERO INOXIDABLE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'LAVAPLATOS DE ACERO INOXIDABLE' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'LAVAPLATOS SANITARIO DE UNA BACHA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 80X50X130 cm., COLOR: PLATEADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'LAVAPLATOS SANITARIO DE UNA BACHA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 80X50X130 cm., COLOR: PLATEADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LAVAPLATOS DE ACERO INOXIDABLE||', @EntityId);

-- Equipment: LAVAPLATOS DE ACERO INOXIDABLE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'LAVAPLATOS DE ACERO INOXIDABLE' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'LAVAPLATOS DE 2 BACHAS (40X40X30) , MATERIAL: ACERO INOXDABLE, MEDIDAS: 85X180X60 cm., COLOR: METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'LAVAPLATOS DE 2 BACHAS (40X40X30) , MATERIAL: ACERO INOXDABLE, MEDIDAS: 85X180X60 cm., COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'LAVAPLATOS DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|', @EntityId);

-- Equipment: LICUADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'LICUADORA' AS Name, N'OSTER' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'LICUADORA ELECTRICA VASO DE VIDRIO, TECNOLOGIA REVERSIBLE 600W DE POTENCIA , MARCA: OSTER') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'LICUADORA ELECTRICA VASO DE VIDRIO, TECNOLOGIA REVERSIBLE 600W DE POTENCIA , MARCA: OSTER', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'LICUADORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LICUADORA|OSTER|', @EntityId);

-- Equipment: LICUADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'LICUADORA' AS Name, N'OSTER' AS Brand, N'XPERT' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'LICUADRA JARRA DE VIDRIO, DE 6 ASPAS , CAPACIDAD: 2.5 KG, 2 LITROS , MARCA: OSTER, MODELO: XPERT') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'LICUADRA JARRA DE VIDRIO, DE 6 ASPAS , CAPACIDAD: 2.5 KG, 2 LITROS , MARCA: OSTER, MODELO: XPERT', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'LICUADORA' AND ISNULL(Brand, N'') = ISNULL(N'OSTER', N'') AND ISNULL(Model, N'') = ISNULL(N'XPERT', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|XPERT') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'LICUADORA|OSTER|XPERT', @EntityId);

-- Equipment: MESA DE MADERA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESA DE MADERA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESA RECTANGULAR , MATERIAL: MADERA, MEDIDAS: 80X240X80 cm., COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESA RECTANGULAR , MATERIAL: MADERA, MEDIDAS: 80X240X80 cm., COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESA DE MADERA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA DE MADERA||', @EntityId);

-- Equipment: MESA METALICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESA METALICA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESA , MATERIAL: METAL Y FORMICA, MEDIDAS: 30X121X76 cm., COLOR: PLOMO JASPEADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESA , MATERIAL: METAL Y FORMICA, MEDIDAS: 30X121X76 cm., COLOR: PLOMO JASPEADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESA METALICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA METALICA||', @EntityId);

-- Equipment: MESA METALICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESA METALICA' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESA RECTANGULAR DE APOYO, CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 1 REPISA, COLOR: METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESA RECTANGULAR DE APOYO, CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, BANDEJAS/DIVISIONES: 1 REPISA, COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESA METALICA' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESA METALICA|WILDA|', @EntityId);

-- Equipment: MESON DE METAL
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON DE METAL' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON GASTRONOMICO CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X100X80 cm., BANDEJAS/DIVISIONES: 1 REPISA INFERIOR, COLOR: METALICO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON GASTRONOMICO CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X100X80 cm., BANDEJAS/DIVISIONES: 1 REPISA INFERIOR, COLOR: METALICO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON DE METAL' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON DE METAL||', @EntityId);

-- Equipment: MESON DE METAL
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON DE METAL' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON GASTRONOMICO DE 1 REPISA INFERIOR , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X60 cm., COLOR: METALICO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON GASTRONOMICO DE 1 REPISA INFERIOR , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X60 cm., COLOR: METALICO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON DE METAL' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON DE METAL|WILDA|', @EntityId);

-- Equipment: MESON REFRIGERADOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON REFRIGERADOR' AS Name, N'VENTUS' AS Brand, N'VMR2PS-280E' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON REFRIGERANTE CON 2 PUERTAS,4 REJILLAS BLANCAS, 4 DIVISIONES, MOVIBLE CON 4 RUEDAS, CHAPA CON 2 LLAVES, TEMPERATURA DE +5° A -5° , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 280 LTS 250 WATT 220V 50HZ, MEDIDAS: 85X70X136 cm., COLOR: PLATEADO, MARCA: VENTUS, MODELO: VMR2PS-280E, SERIE: EPL3520CL6200319070300350004') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON REFRIGERANTE CON 2 PUERTAS,4 REJILLAS BLANCAS, 4 DIVISIONES, MOVIBLE CON 4 RUEDAS, CHAPA CON 2 LLAVES, TEMPERATURA DE +5° A -5° , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 280 LTS 250 WATT 220V 50HZ, MEDIDAS: 85X70X136 cm., COLOR: PLATEADO, MARCA: VENTUS, MODELO: VMR2PS-280E, SERIE: EPL3520CL6200319070300350004', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(N'VMR2PS-280E', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E', @EntityId);

-- Equipment: MESON ROBUSTO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON ROBUSTO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON GASTRONOMICO PARA INSTRUCTOR CON 4 ANAFES, CAJONERIA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X100X85 cm., COLOR: PLATEADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON GASTRONOMICO PARA INSTRUCTOR CON 4 ANAFES, CAJONERIA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X100X85 cm., COLOR: PLATEADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO||', @EntityId);

-- Equipment: MESON ROBUSTO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON ROBUSTO' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO|WILDA|', @EntityId);

-- Equipment: MESON ROBUSTO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MESON ROBUSTO' AS Name, N'WILDA' AS Brand, N'A155 304L2B' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MESON ROBUSTO CON ANAFE DE 4 HORNALLAS, 2 BANDEJAS CORREDIZAS, 1 BANDEJA EXTRAIBLE, 2 CHAPAS, 2 LLAVES , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 85X200X100 cm., BANDEJAS/DIVISIONES: 4 PUERTAS, COLOR: METALICO, MARCA: WILDA, MODELO: A155 304L2B', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MESON ROBUSTO' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(N'A155 304L2B', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MESON ROBUSTO|WILDA|A155 304L2B', @EntityId);

-- Equipment: MICROONDA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MICROONDA' AS Name, N'LG' AS Brand, N'MH8236GIR' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MICROONDAS PANEL TOUCH , CAPACIDAD: DE 42 LITROS , COLOR: NEGRO JASPEADO, MARCA: LG, MODELO: MH8236GIR, SERIE: 302TATGEX185') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MICROONDAS PANEL TOUCH , CAPACIDAD: DE 42 LITROS , COLOR: NEGRO JASPEADO, MARCA: LG, MODELO: MH8236GIR, SERIE: 302TATGEX185', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'LG', N'') AND ISNULL(Model, N'') = ISNULL(N'MH8236GIR', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|LG|MH8236GIR', @EntityId);

-- Equipment: MICROONDA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MICROONDA' AS Name, N'PANASONIC' AS Brand, N'NN-ST34HM' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'HORNO MICROONDAS , CAPACIDAD: 23 LITROS, MARCA: PANASAONIC, MODELO: NN-ST34HM, SERIE: 5A39210153') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'HORNO MICROONDAS , CAPACIDAD: 23 LITROS, MARCA: PANASAONIC, MODELO: NN-ST34HM, SERIE: 5A39210153', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'PANASONIC', N'') AND ISNULL(Model, N'') = ISNULL(N'NN-ST34HM', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MICROONDA|PANASONIC|NN-ST34HM') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|PANASONIC|NN-ST34HM', @EntityId);

-- Equipment: MICROONDA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MICROONDA' AS Name, N'SAMSUNG' AS Brand, N'MG402MADXBB' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MICROONDAS , CAPACIDAD: 40 LITROS, COLOR: NEGRO, MARCA: SAMSUNG, MODELO: MG402MADXBB, SERIE: 0AMM7WFT600193') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MICROONDAS , CAPACIDAD: 40 LITROS, COLOR: NEGRO, MARCA: SAMSUNG, MODELO: MG402MADXBB, SERIE: 0AMM7WFT600193', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MICROONDA' AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'') AND ISNULL(Model, N'') = ISNULL(N'MG402MADXBB', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MICROONDA|SAMSUNG|MG402MADXBB') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MICROONDA|SAMSUNG|MG402MADXBB', @EntityId);

-- Equipment: MOLINO DE COFFIE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MOLINO DE COFFIE' AS Name, N'FIORENZATO' AS Brand, N'F64 E' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MOLINO DE CAFE VASO DE PLASTICO , MATERIAL: INOX/ACERO, COLOR: PLOMO, MARCA: FIORENZATO, MODELO: F64 E, SERIE: 66666622') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MOLINO DE CAFE VASO DE PLASTICO , MATERIAL: INOX/ACERO, COLOR: PLOMO, MARCA: FIORENZATO, MODELO: F64 E, SERIE: 66666622', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MOLINO DE COFFIE' AND ISNULL(Brand, N'') = ISNULL(N'FIORENZATO', N'') AND ISNULL(Model, N'') = ISNULL(N'F64 E', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MOLINO DE COFFIE|FIORENZATO|F64 E') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MOLINO DE COFFIE|FIORENZATO|F64 E', @EntityId);

-- Equipment: MONITOR LCD
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MONITOR LCD' AS Name, N'LENOVO' AS Brand, N'2580AB1' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MONITOR LCD DE 18.5 PULGADAS , COLOR: NEGRO, MARCA: LENOVO, MODELO: 2580AB1, SERIE: V1RVV53') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MONITOR LCD DE 18.5 PULGADAS , COLOR: NEGRO, MARCA: LENOVO, MODELO: 2580AB1, SERIE: V1RVV53', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MONITOR LCD' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'2580AB1', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MONITOR LCD|LENOVO|2580AB1', @EntityId);

-- Equipment: MUEBLE DE MADERA PARA COMPUTADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MUEBLE DE MADERA PARA COMPUTADORA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO Y PORTA CPU , MATERIAL: MADERA/METAL, MEDIDAS: 80X75X50 cm., COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO Y PORTA CPU , MATERIAL: MADERA/METAL, MEDIDAS: 80X75X50 cm., COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MUEBLE DE MADERA PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MADERA PARA COMPUTADORA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE DE MADERA PARA COMPUTADORA||', @EntityId);

-- Equipment: MUEBLE DE MELAMINA PARA COMPUTADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MUEBLE DE MELAMINA PARA COMPUTADORA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO CORREDIZO, (1 CHAPA Y 2 LLAVES) , MATERIAL: MELAMINA, MEDIDAS: 189X73.5X51 cm., BANDEJAS/DIVISIONES: 3 PUERTAS, COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MUEBLE PARA COMPUTADORA CON PORTA TECLADO CORREDIZO, (1 CHAPA Y 2 LLAVES) , MATERIAL: MELAMINA, MEDIDAS: 189X73.5X51 cm., BANDEJAS/DIVISIONES: 3 PUERTAS, COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MUEBLE DE MELAMINA PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE DE MELAMINA PARA COMPUTADORA||', @EntityId);

-- Equipment: MUEBLE METALICO PARA COMPUTADORA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'MUEBLE METALICO PARA COMPUTADORA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'MUEBLE PARA COMPUTADORA , MATERIAL: METALICA/MELAMINA, MEDIDAS: 83X110X73 cm., COLOR: PLOMO/BLANCO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'MUEBLE PARA COMPUTADORA , MATERIAL: METALICA/MELAMINA, MEDIDAS: 83X110X73 cm., COLOR: PLOMO/BLANCO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'MUEBLE METALICO PARA COMPUTADORA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'MUEBLE METALICO PARA COMPUTADORA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'MUEBLE METALICO PARA COMPUTADORA||', @EntityId);

-- Equipment: PINZA KELLY
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PINZA KELLY' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PINZA KELLY CURVA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 14 cm., COLOR: NIQUELADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PINZA KELLY CURVA , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 14 cm., COLOR: NIQUELADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PINZA KELLY' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PINZA KELLY||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PINZA KELLY||', @EntityId);

-- Equipment: PIZARRA CON MARCO METALICO ACRILICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PIZARRA CON MARCO METALICO ACRILICO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PIZARRA CON PORTA MARCADORES , MATERIAL: ALUMINIO/ACRILICO, MEDIDAS: 120X300 cm., COLOR: PLOMO/BLANCO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PIZARRA CON PORTA MARCADORES , MATERIAL: ALUMINIO/ACRILICO, MEDIDAS: 120X300 cm., COLOR: PLOMO/BLANCO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PIZARRA CON MARCO METALICO ACRILICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PIZARRA CON MARCO METALICO ACRILICO||', @EntityId);

-- Equipment: PORTA BANDEJAS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PORTA BANDEJAS' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PORTA BANDEJAS CON RODAPIES, DIVISIONES PARA BANDEJAS , MATERIAL: METALICO, COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PORTA BANDEJAS CON RODAPIES, DIVISIONES PARA BANDEJAS , MATERIAL: METALICO, COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PORTA BANDEJAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PORTA BANDEJAS||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA BANDEJAS||', @EntityId);

-- Equipment: PORTA TABLAS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PORTA TABLAS' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PORTA TABLAS , MATERIAL: INOX, COLOR: PLATEADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PORTA TABLAS , MATERIAL: INOX, COLOR: PLATEADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PORTA TABLAS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA TABLAS||', @EntityId);

-- Equipment: PORTA UTENSILIOS
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PORTA UTENSILIOS' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PORTA UTENSILIOS , MATERIAL: INOX, MEDIDAS: 11X11X18 cm.') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PORTA UTENSILIOS , MATERIAL: INOX, MEDIDAS: 11X11X18 cm.', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PORTA UTENSILIOS' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PORTA UTENSILIOS||', @EntityId);

-- Equipment: PROYECTOR LED/LASER
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PROYECTOR LED/LASER' AS Name, N'CASIO' AS Brand, N'(YW-40) XJ-F20XN' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093815') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093815', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PROYECTOR LED/LASER' AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'') AND ISNULL(Model, N'') = ISNULL(N'(YW-40) XJ-F20XN', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN', @EntityId);

-- Equipment: PROYECTOR LED/LASER
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'PROYECTOR LED/LASER' AS Name, N'CASIO' AS Brand, N'XJ-F20XN' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093585') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'PROYECTOR LED CON CABLES DE CONECCION, CONTROL REMOTO, CON UN MODULO INALAMBRICO (MARCA: CASIO MOD: YW-40) , COLOR: BLANCO , MARCA: CASIO , MODELO: XJ-F20XN, SERIE: A995DDBY7B-093585', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'PROYECTOR LED/LASER' AND ISNULL(Brand, N'') = ISNULL(N'CASIO', N'') AND ISNULL(Model, N'') = ISNULL(N'XJ-F20XN', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'PROYECTOR LED/LASER|CASIO|XJ-F20XN', @EntityId);

-- Equipment: REFRIGERADOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'REFRIGERADOR' AS Name, N'VENTUS SORP' AS Brand, N'VREF-1000BEN' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS CORP, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350020') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS CORP, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350020', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS SORP', N'') AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN', @EntityId);

-- Equipment: REFRIGERADOR
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'REFRIGERADOR' AS Name, N'VENTUS' AS Brand, N'VREF-1000BEN' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350005') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'REFRIGERADOR DE 2 CUERPOS, 4 MEDIDAS PUERAS DUAL, MOVIBLE CON 4 RODAPIES , MATERIAL: ACERO INOXIDABLE, CAPACIDAD: 900 LTS, COLOR: INOX, MARCA: VENTUS, MODELO: VREF-1000BEN, SERIE: YBL9340CL6200319031300350005', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'REFRIGERADOR' AND ISNULL(Brand, N'') = ISNULL(N'VENTUS', N'') AND ISNULL(Model, N'') = ISNULL(N'VREF-1000BEN', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS|VREF-1000BEN') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REFRIGERADOR|VENTUS|VREF-1000BEN', @EntityId);

-- Equipment: REPISA DE ACERO INOXIDABLE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'REPISA DE ACERO INOXIDABLE' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'REPISA DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REPISA DE ACERO INOXIDABLE||', @EntityId);

-- Equipment: REPISA DE ACERO INOXIDABLE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'REPISA DE ACERO INOXIDABLE' AS Name, N'WILDA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO, MARCA: WILDA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'REPISA CON SISTEMA AUTOSOPORTANTE , MATERIAL: ACERO INOXIDABLE, MEDIDAS: 200X35 cm., COLOR: PLATEADO, MARCA: WILDA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'REPISA DE ACERO INOXIDABLE' AND ISNULL(Brand, N'') = ISNULL(N'WILDA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'REPISA DE ACERO INOXIDABLE|WILDA|', @EntityId);

-- Equipment: ROUTER
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'ROUTER' AS Name, N'VIEW SONIC' AS Brand, N'WPG-370' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'ROUTER CON DOS ANTENAS, ADAPTADOR DE ENERGÍA , COLOR: NEGRO, MARCA: VIEW SONIC, MODELO: WPG-370, SERIE: TK2142200824') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'ROUTER CON DOS ANTENAS, ADAPTADOR DE ENERGÍA , COLOR: NEGRO, MARCA: VIEW SONIC, MODELO: WPG-370, SERIE: TK2142200824', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'ROUTER' AND ISNULL(Brand, N'') = ISNULL(N'VIEW SONIC', N'') AND ISNULL(Model, N'') = ISNULL(N'WPG-370', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'ROUTER|VIEW SONIC|WPG-370') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'ROUTER|VIEW SONIC|WPG-370', @EntityId);

-- Equipment: SILLA DE MADERA FIJA TAPIZ TELA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SILLA DE MADERA FIJA TAPIZ TELA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SILLA DE MADERA TAPIZ TELA , MATERIAL: MADERA TAPIZ TELA, COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SILLA DE MADERA TAPIZ TELA , MATERIAL: MADERA TAPIZ TELA, COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SILLA DE MADERA FIJA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA DE MADERA FIJA TAPIZ TELA||', @EntityId);

-- Equipment: SILLA DE MADERA FIJA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SILLA DE MADERA FIJA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SILLA FIJA , MATERIAL: MADERA, COLOR: CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SILLA FIJA , MATERIAL: MADERA, COLOR: CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SILLA DE MADERA FIJA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA DE MADERA FIJA||', @EntityId);

-- Equipment: SILLA METALICA FIJA TAPIZ CUERINA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SILLA METALICA FIJA TAPIZ CUERINA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SILLA FIJA DE ESPERA, ESTRUCTURA METALICA TAPIZ CUERINA , MATERIAL: METALICO/CUERINA, BANDEJAS/DIVISIONES: , COLOR: NEGRO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SILLA FIJA DE ESPERA, ESTRUCTURA METALICA TAPIZ CUERINA , MATERIAL: METALICO/CUERINA, BANDEJAS/DIVISIONES: , COLOR: NEGRO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SILLA METALICA FIJA TAPIZ CUERINA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA FIJA TAPIZ CUERINA||', @EntityId);

-- Equipment: SILLA METALICA FIJA TAPIZ TELA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SILLA METALICA FIJA TAPIZ TELA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SILLA FIJA DE ESPERA , MATERIAL: METAL/TAPIZ TELA, COLOR: NEGRO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SILLA FIJA DE ESPERA , MATERIAL: METAL/TAPIZ TELA, COLOR: NEGRO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SILLA METALICA FIJA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ TELA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA FIJA TAPIZ TELA||', @EntityId);

-- Equipment: SILLA METALICA GIRATORIA TAPIZ TELA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SILLA METALICA GIRATORIA TAPIZ TELA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SILLA GIRATORIA CON 5 RODAPIES, APOYA BRAZOS , MATERIAL: METAL/PLASTICO/TAPIZ TELA, COLOR: NEGRO/TELA AZUL') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SILLA GIRATORIA CON 5 RODAPIES, APOYA BRAZOS , MATERIAL: METAL/PLASTICO/TAPIZ TELA, COLOR: NEGRO/TELA AZUL', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SILLA METALICA GIRATORIA TAPIZ TELA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA GIRATORIA TAPIZ TELA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SILLA METALICA GIRATORIA TAPIZ TELA||', @EntityId);

-- Equipment: SOUS VIDE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'SOUS VIDE' AS Name, N'METVISA' AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'SOUS VIDE , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: METVISA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'SOUS VIDE , MATERIAL: ACERO INOXIDABLE, COLOR: PLATEADO, MARCA: METVISA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'SOUS VIDE' AND ISNULL(Brand, N'') = ISNULL(N'METVISA', N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'SOUS VIDE|METVISA|', @EntityId);

-- Equipment: TABURETE METALICO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TABURETE METALICO' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TABURETE , MATERIAL: METALICO Y MADERA, COLOR: BEIGE/CAFE') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TABURETE , MATERIAL: METALICO Y MADERA, COLOR: BEIGE/CAFE', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TABURETE METALICO' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TABURETE METALICO||', @EntityId);

-- Equipment: TECLADO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TECLADO' AS Name, N'DELUX' AS Brand, N'K8060' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TECLADO , COLOR: NEGRO, MARCA: DELUX, MODELO: K8060, SERIE: K80600903013004') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TECLADO , COLOR: NEGRO, MARCA: DELUX, MODELO: K8060, SERIE: K80600903013004', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'DELUX', N'') AND ISNULL(Model, N'') = ISNULL(N'K8060', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TECLADO|DELUX|K8060') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|DELUX|K8060', @EntityId);

-- Equipment: TECLADO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TECLADO' AS Name, N'HP' AS Brand, N'PR1101U' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TECLADO , COLOR: BLANCO , MARCA: HP, MODELO: PR1101U, SERIE: BFZYF0ALAAC0H9') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TECLADO , COLOR: BLANCO , MARCA: HP, MODELO: PR1101U, SERIE: BFZYF0ALAAC0H9', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'HP', N'') AND ISNULL(Model, N'') = ISNULL(N'PR1101U', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TECLADO|HP|PR1101U') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|HP|PR1101U', @EntityId);

-- Equipment: TECLADO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TECLADO' AS Name, N'LENOVO' AS Brand, N'KU-0225' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: KU-0225, SERIE: 2928973') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: KU-0225, SERIE: 2928973', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'KU-0225', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|LENOVO|KU-0225', @EntityId);

-- Equipment: TECLADO
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TECLADO' AS Name, N'LENOVO' AS Brand, N'SK - 8825' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: SK - 8825, SERIE: 03647434') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TECLADO , COLOR: NEGRO, MARCA: LENOVO, MODELO: SK - 8825, SERIE: 03647434', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TECLADO' AND ISNULL(Brand, N'') = ISNULL(N'LENOVO', N'') AND ISNULL(Model, N'') = ISNULL(N'SK - 8825', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|SK - 8825') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TECLADO|LENOVO|SK - 8825', @EntityId);

-- Equipment: TELEVISOR LCD
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TELEVISOR LCD' AS Name, N'SONY' AS Brand, N'KDL-40BX455' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TELEVISOR LCD DE 40 PULGADAS, CON CONTROL REMOTO Y PEDESTAL SOPORTE , COLOR: NEGRO, MARCA: SONY, MODELO: KDL-40BX455, SERIE: 5019941') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TELEVISOR LCD DE 40 PULGADAS, CON CONTROL REMOTO Y PEDESTAL SOPORTE , COLOR: NEGRO, MARCA: SONY, MODELO: KDL-40BX455, SERIE: 5019941', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TELEVISOR LCD' AND ISNULL(Brand, N'') = ISNULL(N'SONY', N'') AND ISNULL(Model, N'') = ISNULL(N'KDL-40BX455', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LCD|SONY|KDL-40BX455', @EntityId);

-- Equipment: TELEVISOR LED
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TELEVISOR LED' AS Name, N'HAIER' AS Brand, N'LE55B8500DUA' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TELEVISOR CON CONTROL REMOTO, EMPOTRADO EN LA PARED , COLOR: NEGRO, MARCA: HAIER, MODELO: LE55B8500DUA, SERIE: DH1VM0D4401D9H9C0323') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TELEVISOR CON CONTROL REMOTO, EMPOTRADO EN LA PARED , COLOR: NEGRO, MARCA: HAIER, MODELO: LE55B8500DUA, SERIE: DH1VM0D4401D9H9C0323', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'HAIER', N'') AND ISNULL(Model, N'') = ISNULL(N'LE55B8500DUA', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|HAIER|LE55B8500DUA') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|HAIER|LE55B8500DUA', @EntityId);

-- Equipment: TELEVISOR LED
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TELEVISOR LED' AS Name, N'LG' AS Brand, N'50LN5400' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TELEVISOR LED DE 50 PULGADAS, CON SOPORTE , COLOR: NEGRO, MARCA: LG, MODELO: 50LN5400, SERIE: 402RMYA62106') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TELEVISOR LED DE 50 PULGADAS, CON SOPORTE , COLOR: NEGRO, MARCA: LG, MODELO: 50LN5400, SERIE: 402RMYA62106', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'LG', N'') AND ISNULL(Model, N'') = ISNULL(N'50LN5400', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|LG|50LN5400', @EntityId);

-- Equipment: TELEVISOR LED
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TELEVISOR LED' AS Name, N'SAMSUNG' AS Brand, N'UN48J5000AGXZS' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TELEVISOR LED CON SOPORTE PARA TV , COLOR: LED, MARCA: SAMSUNG, MODELO: UN48J5000AGXZS, SERIE: 04NS3CVH801270') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TELEVISOR LED CON SOPORTE PARA TV , COLOR: LED, MARCA: SAMSUNG, MODELO: UN48J5000AGXZS, SERIE: 04NS3CVH801270', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TELEVISOR LED' AND ISNULL(Brand, N'') = ISNULL(N'SAMSUNG', N'') AND ISNULL(Model, N'') = ISNULL(N'UN48J5000AGXZS', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS', @EntityId);

-- Equipment: TERMO TANQUE
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'TERMO TANQUE' AS Name, N'A6' AS Brand, N'27284' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'TERMO TANQUE DE 1.100 LITROS , MEDIDAS: 143X41 cm., COLOR: PLOMO, MARCA: A6, MODELO: 027284') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'TERMO TANQUE DE 1.100 LITROS , MEDIDAS: 143X41 cm., COLOR: PLOMO, MARCA: A6, MODELO: 027284', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'TERMO TANQUE' AND ISNULL(Brand, N'') = ISNULL(N'A6', N'') AND ISNULL(Model, N'') = ISNULL(N'27284', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'TERMO TANQUE|A6|27284') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'TERMO TANQUE|A6|27284', @EntityId);

-- Equipment: THERMOMIX
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'THERMOMIX' AS Name, NULL AS Brand, N'TM6' AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'THERMOMIX VASO, MARIPOSA, CESTILLO, VAROMA, ESPATULA , MODELO: TM6, SERIE: 62144842778514045362105') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'THERMOMIX VASO, MARIPOSA, CESTILLO, VAROMA, ESPATULA , MODELO: TM6, SERIE: 62144842778514045362105', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'THERMOMIX' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(N'TM6', N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'THERMOMIX||TM6') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'THERMOMIX||TM6', @EntityId);

-- Equipment: VITRINA DE PARED
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'VITRINA DE PARED' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'VITRINA PEQUEÑA DE PARED 3 DIVISIONES, 2 PUERTAS DE VIDRIO , MATERIAL: METALICA, MEDIDAS: 80X50X33 cm., COLOR: PLOMO') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'VITRINA PEQUEÑA DE PARED 3 DIVISIONES, 2 PUERTAS DE VIDRIO , MATERIAL: METALICA, MEDIDAS: 80X50X33 cm., COLOR: PLOMO', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'VITRINA DE PARED' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'VITRINA DE PARED||', @EntityId);

-- Equipment: VITRINA METALICA
SET @EntityId = NULL;
MERGE Equipments AS target USING (SELECT N'VITRINA METALICA' AS Name, NULL AS Brand, NULL AS Model) AS source ON target.Name = source.Name AND ISNULL(target.Brand, N'') = ISNULL(source.Brand, N'') AND ISNULL(target.Model, N'') = ISNULL(source.Model, N'') WHEN MATCHED THEN UPDATE SET Description = COALESCE(target.Description, N'VITRINA METALICA CON 4 PUERTAS CON VIDRIO , UNA CHAPA CON LLAVE , MEDIDAS: 205X155X50 cm., BANDEJAS/DIVISIONES: CON 5 DIVISIONES , COLOR: CREMA') WHEN NOT MATCHED THEN INSERT (Category, UtensilType, TypeClassification, Status, Name, Brand, Model, UsefulLifeYears, Description, CreatedDate, CreatedById) VALUES (0, 0, 7, 0, source.Name, source.Brand, source.Model, NULL, N'VITRINA METALICA CON 4 PUERTAS CON VIDRIO , UNA CHAPA CON LLAVE , MEDIDAS: 205X155X50 cm., BANDEJAS/DIVISIONES: CON 5 DIVISIONES , COLOR: CREMA', @Today, @CreatedById);
SELECT TOP 1 @EntityId = Id FROM Equipments WHERE Name = N'VITRINA METALICA' AND ISNULL(Brand, N'') = ISNULL(NULL, N'') AND ISNULL(Model, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @EquipmentMap WHERE [Key] = N'VITRINA METALICA||') INSERT INTO @EquipmentMap ([Key], Id) VALUES (N'VITRINA METALICA||', @EntityId);

-- EquipmentUnit: 225
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BANCA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'225' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'225';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'225') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'225', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 239
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BANCA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'239' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'239';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'239') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'239', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 401
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'401' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'401';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'401') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'401', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 406
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'406' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'406';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'406') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'406', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 416
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'416' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'416';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'416') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'416', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 423
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'423' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'423';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'423') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'423', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 696
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'696' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'696';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'696') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'696', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 719
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'719' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'719';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'719') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'719', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 1635
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'1635' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'1635';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'1635') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'1635', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2582
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2582' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2582';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2582') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2582', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2584
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2584' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2584';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2584') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2584', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2586
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2586' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2586';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2586') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2586', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2588
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2588' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2588';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2588') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2588', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2589
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2589' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2589';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2589') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2589', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2590
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2590' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2590';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2590') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2590', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2592
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2592' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2592';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2592') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2592', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2593
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2593' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2593';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2593') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2593', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2595
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2595' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2595';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2595') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2595', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2791
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2791' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2791';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2791') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2791', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 2802
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'2802' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'2802';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'2802') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'2802', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3275
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3275' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3275';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3275') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3275', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3287
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3287' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3287';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3287') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3287', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3289
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3289' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3289';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3289') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3289', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3476
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3476' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3476';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3476') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3476', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3478
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3478' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3478';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3478') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3478', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3479
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3479' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3479';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3479') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3479', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3480
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3480' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3480';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3480') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3480', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3481
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3481' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3481';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3481') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3481', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3497
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3497' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3497';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3497') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3497', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3687
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3687' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3687';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3687') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3687', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 3695
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'3695' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'3695';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'3695') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'3695', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 9007
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'9007' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'9007';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'9007') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'9007', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 9869
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA GIRATORIA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'9869' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'9869';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'9869') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'9869', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 10714
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE METALICO PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'10714' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'10714';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'10714') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'10714', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 11848
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|DELUX|K8060';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'11848' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'K80600903013004', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'11848';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'11848') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'11848', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 12789
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PINZA KELLY||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'12789' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'12789';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'12789') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'12789', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 12792
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PINZA KELLY||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'12792' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'12792';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'12792') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'12792', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 13281
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'13281' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13281';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'13281') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13281', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 13616
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'13616' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13616';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'13616') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13616', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 13617
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'13617' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'13617';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'13617') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'13617', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 14298
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA TABLAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'14298' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14298';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'14298') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14298', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 14302
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'14302' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14302';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'14302') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14302', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 14464
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'14464' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14464';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'14464') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14464', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 14799
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'14799' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'14799';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'14799') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'14799', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 15501
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE METALICO PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'15501' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'15501';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'15501') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'15501', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 17355
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'17355' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'XX', @RelatedId, CONVERT(datetime2, '2015-01-10', 23), NULL, 350, 0, 2, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17355';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'17355') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17355', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 17585
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'17585' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17585';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'17585') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17585', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 17613
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'17613' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 7500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'17613';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'17613') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'17613', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 18024
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'18024' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18024';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'18024') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18024', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 18049
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'18049' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 7500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18049';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'18049') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18049', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 18081
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'18081' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18081';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'18081') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18081', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 18366
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'18366' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'18366';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'18366') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'18366', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 20208
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE BAR DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'20208' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'20208';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'20208') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'20208', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 20540
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA BANDEJAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'20540' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'20540';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'20540') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'20540', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21933
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21933' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21933';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21933') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21933', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21934
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21934' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21934';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21934') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21934', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21935
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21935' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21935';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21935') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21935', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21937
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21937' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21937';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21937') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21937', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21938
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21938' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21938';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21938') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21938', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21940
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21940' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21940';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21940') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21940', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21952
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21952' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21952';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21952') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21952', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21955
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21955' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21955';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21955') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21955', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21956
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21956' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21956';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21956') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21956', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21957
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21957' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21957';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21957') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21957', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21958
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21958' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21958';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21958') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21958', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 21960
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TABURETE METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'21960' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'21960';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'21960') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'21960', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 22060
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'22060' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'22060';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'22060') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'22060', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 23096
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESTANTE PORTA BANDEJAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'23096' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'23096';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'23096') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'23096', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 23273
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'23273' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'23273';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'23273') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'23273', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 25509
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'25509' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25509';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'25509') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25509', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 25510
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'25510' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25510';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'25510') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25510', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 25511
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'25511' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25511';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'25511') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25511', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 25513
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'25513' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25513';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'25513') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25513', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 25514
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|ABC|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'25514' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'25514';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'25514') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'25514', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 26431
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'26431' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'26431';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'26431') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'26431', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 26681
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CARRITO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'26681' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'26681';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'26681') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'26681', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 30784
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'30784' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'30784';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'30784') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'30784', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 31123
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'31123' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'31123';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'31123') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'31123', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 31169
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'31169' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'31169';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'31169') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'31169', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33306
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA DE PARED||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33306' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33306';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33306') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33306', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33308
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33308' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33308';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33308') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33308', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33650
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33650' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33650';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33650') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33650', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33651
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33651' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33651';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33651') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33651', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33706
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE AIRE|LOREN SID|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33706' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33706';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33706') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33706', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33956
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33956' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33956';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33956') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33956', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33957
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33957' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33957';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33957') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33957', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33958
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33958' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33958';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33958') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33958', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33959
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33959' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33959';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33959') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33959', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33960
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33960' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33960';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33960') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33960', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 33962
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'33962' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'33962';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'33962') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'33962', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34046
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34046' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34046';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34046') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34046', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34047
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34047' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34047';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34047') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34047', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34073
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34073' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34073';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34073') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34073', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34075
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34075' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34075';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34075') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34075', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34076
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34076' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34076';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34076') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34076', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34077
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34077' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34077';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34077') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34077', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34161
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34161' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'5019590', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34161';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34161') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34161', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34162
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34162' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'5019941', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34162';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34162') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34162', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34164
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34164' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'5020193', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34164';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34164') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34164', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34165
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LCD|SONY|KDL-40BX455';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34165' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'5019944', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34165';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34165') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34165', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34166
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34166' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34166';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34166') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34166', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34167
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34167' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34167';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34167') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34167', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34168
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34168' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34168';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34168') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34168', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34169
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34169' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34169';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34169') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34169', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34170
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34170' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34170';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34170') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34170', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34171
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34171' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34171';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34171') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34171', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34172
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34172' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34172';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34172') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34172', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34173
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34173' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34173';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34173') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34173', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34174
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34174' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34174';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34174') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34174', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34175
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34175' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34175';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34175') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34175', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34178
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CILINDERS|FNC 10';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34178' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34178';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34178') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34178', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34179
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34179' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'XX', @RelatedId, CONVERT(datetime2, '2019-03-15', 23), NULL, 8500, 0, 2, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34179';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34179') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34179', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34180
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34180' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'XX', @RelatedId, CONVERT(datetime2, '2019-03-15', 23), NULL, 8500, 0, 2, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34180';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34180') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34180', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34181
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34181' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34181';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34181') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34181', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34183
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34183' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34183';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34183') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34183', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34184
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34184' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34184';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34184') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34184', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34185
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34185' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34185';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34185') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34185', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34186
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34186' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34186';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34186') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34186', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34187
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|AISI304L 2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34187' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34187';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34187') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34187', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34188
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34188' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34188';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34188') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34188', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34189
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34189' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34189';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34189') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34189', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34190
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34190' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34190';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34190') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34190', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34191
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34191' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34191';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34191') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34191', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34192
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34192' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34192';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34192') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34192', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34193
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34193' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34193';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34193') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34193', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34245
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34245' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34245';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34245') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34245', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34246
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34246' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34246';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34246') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34246', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34247
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34247' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34247';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34247') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34247', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34248
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34248' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34248';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34248') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34248', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34249
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34249' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34249';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34249') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34249', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34250
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34250' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34250';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34250') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34250', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34253
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34253' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34253';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34253') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34253', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34270
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34270' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34270';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34270') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34270', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34271
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34271' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34271';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34271') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34271', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34272
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34272' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34272';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34272') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34272', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34273
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34273' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34273';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34273') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34273', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34274
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34274' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34274';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34274') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34274', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34292
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34292' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'MJNHDXM', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34292';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34292') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34292', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34296
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34296' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'MJNHDZW', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34296';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34296') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34296', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34297
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34297' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'MJNHDWB', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34297';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34297') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34297', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34309
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CPU DE ESCRITORIO|LENOVO|THINK CENTRE';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34309' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'MJNHDWZ', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34309';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34309') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34309', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34363
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34363' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34363';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34363') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34363', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34365
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34365' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34365';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34365') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34365', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34371
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34371' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'V1RWP09', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34371';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34371') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34371', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34373
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FERMENTADOR|WILDA|A155 304L2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34373' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34373';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34373') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34373', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34379
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34379' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'V1RWM86', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34379';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34379') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34379', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34381
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34381' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'V1RVW26', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34381';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34381') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34381', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34386
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MONITOR LCD|LENOVO|2580AB1';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34386' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'V1RVV53', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34386';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34386') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34386', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34409
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO|WILDA|A155 304L2B';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34409' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34409';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34409') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34409', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34420
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'AMASADORA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34420' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34420';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34420') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34420', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34483
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34483' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'2928939', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34483';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34483') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34483', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34491
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34491' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'2928871', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34491';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34491') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34491', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34492
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34492' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34492';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34492') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34492', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34495
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34495' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34495';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34495') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34495', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34500
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34500' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'2928973', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34500';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34500') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34500', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34502
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|KU-0225';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34502' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'2928897', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34502';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34502') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34502', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34515
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34515' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34515';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34515') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34515', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34518
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34518' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34518';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34518') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34518', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34520
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34520' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34520';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34520') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34520', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34523
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34523' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34523';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34523') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34523', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34537
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34537' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34537';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34537') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34537', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34564
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34564' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34564';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34564') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34564', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34566
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34566' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34566';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34566') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34566', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34567
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34567' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34567';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34567') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34567', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34568
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34568' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34568';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34568') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34568', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34599
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34599' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34599';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34599') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34599', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34600
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34600' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34600';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34600') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34600', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34601
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34601' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34601';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34601') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34601', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34734
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34734' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34734';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34734') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34734', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34735
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34735' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34735';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34735') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34735', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34736
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34736' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34736';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34736') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34736', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34737
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34737' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34737';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34737') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34737', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34738
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34738' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34738';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34738') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34738', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34739
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34739' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34739';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34739') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34739', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34740
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34740' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34740';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34740') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34740', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34741
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34741' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34741';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34741') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34741', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34742
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34742' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34742';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34742') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34742', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34743
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34743' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34743';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34743') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34743', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34744
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34744' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'XX', @RelatedId, CONVERT(datetime2, '2019-03-15', 23), NULL, 12000, 0, 2, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34744';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34744') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34744', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34745
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34745' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34745';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34745') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34745', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34746
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34746' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34746';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34746') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34746', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34747
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34747' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34747';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34747') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34747', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34748
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34748' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34748';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34748') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34748', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34749
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34749' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34749';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34749') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34749', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34814
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|CYLINDERS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34814' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34814';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34814') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34814', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34815
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34815' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34815';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34815') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34815', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34816
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34816' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34816';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34816') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34816', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 34817
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|MMB CYLINDERS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'34817' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'34817';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'34817') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'34817', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35075
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35075' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35075';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35075') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35075', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35196
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35196' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35196';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35196') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35196', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35197
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35197' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35197';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35197') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35197', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35394
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35394' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35394';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35394') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35394', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35395
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35395' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35395';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35395') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35395', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35396
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35396' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35396';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35396') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35396', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35411
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35411' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35411';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35411') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35411', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35412
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35412' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35412';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35412') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35412', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35413
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35413' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35413';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35413') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35413', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35414
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35414' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35414';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35414') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35414', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35415
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35415' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35415';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35415') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35415', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35416
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON DE METAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35416' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35416';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35416') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35416', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35417
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35417' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35417';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35417') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35417', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35418
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35418' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35418';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35418') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35418', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35419
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35419' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35419';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35419') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35419', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35420
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35420' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35420';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35420') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35420', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35421
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35421' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35421';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35421') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35421', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35422
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35422' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35422';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35422') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35422', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35423
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FREIDORA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35423' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35423';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35423') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35423', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35424
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35424' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35424';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35424') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35424', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35425
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35425' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35425';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35425') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35425', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35426
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35426' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35426';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35426') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35426', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35427
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35427' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35427';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35427') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35427', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35428
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35428' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35428';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35428') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35428', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35429
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35429' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35429';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35429') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35429', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35430
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35430' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35430';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35430') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35430', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35431
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35431' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35431';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35431') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35431', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35432
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35432' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35432';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35432') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35432', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35433
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35433' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35433';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35433') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35433', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35434
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35434' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35434';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35434') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35434', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35435
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35435' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35435';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35435') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35435', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35436
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35436' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35436';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35436') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35436', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35437
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35437' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35437';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35437') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35437', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35438
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35438' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35438';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35438') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35438', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35528
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TERMO TANQUE|A6|27284';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'CIRCULACION';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35528' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'XX', @RelatedId, CONVERT(datetime2, '2018-08-01', 23), NULL, 1200, 0, 3, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35528';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35528') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35528', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35542
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35542' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35542';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35542') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35542', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35543
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35543' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35543';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35543') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35543', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35544
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35544' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35544';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35544') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35544', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35545
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35545' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35545';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35545') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35545', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35546
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35546' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35546';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35546') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35546', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 35733
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS2CC5192N-IR1';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'35733' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'35733';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'35733') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'35733', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36052
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36052' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36052';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36052') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36052', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36053
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36053' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36053';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36053') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36053', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36054
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36054' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36054';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36054') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36054', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36055
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36055' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36055';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36055') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36055', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36056
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36056' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36056';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36056') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36056', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36264
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|10BB-A0C900';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36264' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'MJ00UZ5R', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36264';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36264') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36264', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36600
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36600' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36600';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36600') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36600', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36601
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36601' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36601';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36601') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36601', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36602
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR|FANACIM|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36602' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36602';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36602') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36602', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36604
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36604' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'402RMUY62070', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36604';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36604') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36604', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36605
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36605' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'402RMYA62106', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36605';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36605') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36605', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36606
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|LG|50LN5400';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36606' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'403RMXX3F434', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36606';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36606') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36606', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36607
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36607' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36607';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36607') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36607', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36608
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36608' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36608';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36608') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36608', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36609
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36609' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36609';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36609') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36609', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36689
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ROUTER|VIEW SONIC|WPG-370';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36689' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'TK2142200824', @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36689';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36689') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36689', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36736
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36736' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36736';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36736') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36736', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36737
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36737' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36737';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36737') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36737', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36738
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36738' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36738';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36738') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36738', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36739
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36739' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36739';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36739') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36739', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36740
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36740' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36740';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36740') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36740', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36741
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36741' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36741';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36741') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36741', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36742
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36742' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36742';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36742') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36742', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36743
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36743' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36743';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36743') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36743', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36744
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36744' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36744';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36744') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36744', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36745
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36745' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36745';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36745') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36745', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36746
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36746' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36746';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36746') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36746', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36747
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36747' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36747';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36747') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36747', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36748
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36748' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36748';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36748') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36748', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36749
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36749' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36749';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36749') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36749', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36750
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36750' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36750';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36750') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36750', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36751
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36751' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36751';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36751') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36751', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36752
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36752' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36752';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36752') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36752', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36753
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36753' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36753';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36753') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36753', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36754
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36754' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36754';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36754') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36754', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36755
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36755' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36755';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36755') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36755', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36756
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36756' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36756';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36756') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36756', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36757
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36757' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36757';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36757') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36757', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36758
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36758' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36758';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36758') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36758', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36759
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36759' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36759';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36759') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36759', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36760
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36760' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36760';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36760') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36760', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36761
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36761' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36761';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36761') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36761', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36762
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36762' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36762';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36762') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36762', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36763
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36763' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36763';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36763') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36763', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36764
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36764' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36764';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36764') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36764', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36765
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36765' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36765';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36765') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36765', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36766
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36766' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36766';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36766') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36766', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36767
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36767' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36767';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36767') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36767', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36768
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36768' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36768';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36768') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36768', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36769
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36769' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36769';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36769') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36769', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36770
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36770' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36770';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36770') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36770', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36771
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36771' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36771';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36771') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36771', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36772
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36772' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36772';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36772') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36772', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36773
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36773' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36773';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36773') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36773', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36774
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36774' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36774';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36774') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36774', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36775
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36775' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36775';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36775') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36775', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36776
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36776' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36776';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36776') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36776', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36777
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36777' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36777';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36777') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36777', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36778
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36778' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36778';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36778') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36778', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36779
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36779' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36779';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36779') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36779', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36780
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36780' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36780';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36780') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36780', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 36781
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'36781' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'36781';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'36781') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'36781', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37019
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37019' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37019';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37019') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37019', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37021
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37021' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37021';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37021') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37021', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37041
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37041' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37041';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37041') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37041', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37059
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37059' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'457428659', @RelatedId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37059';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37059') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37059', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37060
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37060' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'457428791', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37060';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37060') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37060', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37061
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|HIKVISION|DS-2CEE55A2N-IRN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37061' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'457428663', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37061';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37061') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37061', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37070
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|LENOVO|THINK CENTRE M73Z';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37070' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37070';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37070') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37070', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37077
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|LENOVO|SK - 8825';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37077' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'3647434', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37077';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37077') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37077', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37203
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ROUTER|VIEW SONIC|WPG-370';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37203' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'TK2133900878', @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37203';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37203') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37203', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37204
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37204' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37204';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37204') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37204', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37205
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37205' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37205';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37205') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37205', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37206
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37206' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37206';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37206') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37206', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37207
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37207' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37207';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37207') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37207', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37208
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37208' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37208';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37208') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37208', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37209
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37209' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37209';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37209') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37209', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37210
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37210' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37210';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37210') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37210', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37211
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37211' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37211';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37211') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37211', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37212
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37212' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37212';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37212') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37212', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37213
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37213' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37213';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37213') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37213', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37214
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37214' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37214';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37214') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37214', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37215
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37215' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37215';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37215') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37215', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37216
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37216' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37216';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37216') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37216', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37217
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37217' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37217';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37217') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37217', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37218
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37218' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37218';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37218') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37218', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37219
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37219' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37219';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37219') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37219', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37220
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37220' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37220';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37220') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37220', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37221
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'COCINA INDUSTRIAL|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37221' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37221';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37221') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37221', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37222
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37222' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37222';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37222') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37222', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37223
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37223' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37223';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37223') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37223', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37224
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37224' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37224';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37224') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37224', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37225
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37225' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37225';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37225') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37225', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37226
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37226' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37226';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37226') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37226', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37227
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37227' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37227';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37227') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37227', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37228
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37228' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37228';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37228') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37228', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37229
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37229' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37229';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37229') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37229', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37230
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37230' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37230';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37230') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37230', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37231
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37231' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37231';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37231') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37231', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37232
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37232' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37232';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37232') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37232', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37233
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37233' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37233';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37233') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37233', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37234
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37234' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37234';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37234') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37234', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37235
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37235' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37235';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37235') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37235', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37236
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37236' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37236';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37236') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37236', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37237
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37237' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37237';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37237') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37237', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37238
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37238' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37238';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37238') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37238', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37239
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37239' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37239';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37239') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37239', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37240
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37240' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37240';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37240') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37240', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37241
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37241' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37241';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37241') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37241', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37242
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37242' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37242';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37242') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37242', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37243
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37243' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37243';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37243') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37243', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37244
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37244' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37244';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37244') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37244', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37245
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37245' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37245';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37245') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37245', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37246
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37246' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37246';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37246') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37246', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37247
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37247' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37247';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37247') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37247', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37248
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37248' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37248';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37248') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37248', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37249
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37249' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37249';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37249') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37249', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37250
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37250' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37250';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37250') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37250', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37251
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37251' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 25000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37251';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37251') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37251', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37252
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37252' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37252';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37252') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37252', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37253
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37253' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37253';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37253') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37253', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37254
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37254' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37254';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37254') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37254', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37255
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37255' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37255';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37255') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37255', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37256
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37256' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37256';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37256') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37256', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37257
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37257' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37257';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37257') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37257', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37258
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37258' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37258';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37258') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37258', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37259
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37259' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37259';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37259') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37259', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37260
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37260' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37260';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37260') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37260', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37261
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37261' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37261';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37261') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37261', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37262
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37262' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37262';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37262') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37262', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37263
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37263' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37263';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37263') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37263', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37265
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMPANA DE EXTRACCION|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37265' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37265';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37265') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37265', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37266
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37266' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37266';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37266') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37266', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37267
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37267' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37267';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37267') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37267', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37268
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37268' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37268';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37268') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37268', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37269
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37269' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37269';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37269') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37269', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37270
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37270' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37270';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37270') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37270', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37271
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37271' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37271';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37271') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37271', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37272
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37272' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37272';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37272') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37272', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37273
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTRACTOR DE HUMOS Y GRASAS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37273' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 32000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37273';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37273') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37273', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37274
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37274' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37274';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37274') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37274', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37275
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37275' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37275';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37275') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37275', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37276
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37276' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37276';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37276') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37276', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37277
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37277' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37277';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37277') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37277', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37278
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37278' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37278';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37278') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37278', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37279
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37279' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37279';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37279') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37279', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37280
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37280' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37280';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37280') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37280', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37281
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37281' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37281';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37281') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37281', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37282
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37282' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37282';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37282') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37282', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37283
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37283' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37283';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37283') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37283', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37284
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37284' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37284';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37284') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37284', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37285
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37285' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37285';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37285') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37285', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37286
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37286' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37286';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37286') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37286', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37287
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37287' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37287';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37287') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37287', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37288
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37288' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37288';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37288') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37288', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37289
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37289' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37289';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37289') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37289', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37290
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37290' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37290';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37290') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37290', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37291
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA METALICA|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37291' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37291';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37291') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37291', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37292
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37292' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37292';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37292') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37292', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37293
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BACHA DE LAVADO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37293' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37293';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37293') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37293', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37294
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LAVAPLATOS DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37294' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37294';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37294') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37294', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37295
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37295' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37295';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37295') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37295', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37296
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37296' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37296';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37296') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37296', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37297
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37297' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37297';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37297') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37297', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37299
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON ROBUSTO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37299' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 23000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37299';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37299') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37299', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37300
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA CONSERVADORA|ASBER|ARR-43';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37300' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'11090021M', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37300';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37300') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37300', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37314
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37314' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37314';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37314') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37314', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37315
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37315' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37315';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37315') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37315', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37316
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37316' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37316';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37316') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37316', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37317
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37317' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37317';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37317') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37317', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37318
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37318' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37318';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37318') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37318', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37319
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37319' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37319';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37319') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37319', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37320
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37320' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37320';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37320') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37320', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37321
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37321' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37321';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37321') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37321', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37322
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37322' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37322';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37322') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37322', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37323
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37323' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37323';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37323') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37323', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37324
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE|WILDA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37324' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37324';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37324') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37324', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37325
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37325' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37325';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37325') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37325', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37326
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37326' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37326';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37326') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37326', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37327
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REPISA DE ACERO INOXIDABLE||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37327' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37327';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37327') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37327', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37361
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BEBEDERO DE AGUA|IBBL|BAG 40';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37361' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'451P287128', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37361';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37361') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37361', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37395
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MELAMINA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37395' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37395';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37395') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37395', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37900
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37900' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37900';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37900') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37900', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37901
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37901' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37901';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37901') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37901', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37902
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37902' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37902';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37902') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37902', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37903
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37903' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37903';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37903') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37903', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37904
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37904' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37904';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37904') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37904', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37906
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37906' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37906';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37906') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37906', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37907
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37907' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37907';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37907') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37907', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37908
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37908' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37908';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37908') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37908', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37909
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37909' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37909';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37909') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37909', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37910
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37910' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37910';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37910') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37910', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37911
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37911' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37911';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37911') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37911', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37917
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37917' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37917';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37917') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37917', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37918
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37918' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37918';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37918') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37918', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37919
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37919' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37919';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37919') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37919', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37920
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37920' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37920';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37920') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37920', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 37922
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'37922' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'37922';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'37922') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'37922', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38259
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38259' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'SFTX1927S0VR', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38259';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38259') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38259', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38260
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP3702E-A-K9';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38260' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'SFTX1927S0VH', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38260';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38260') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38260', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38270
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ACCES POINT|CISCO|AIR-CAP370';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38270' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'SFTX1927S0UL', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38270';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38270') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38270', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38639
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38639' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'1,409271378E+10', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38639';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38639') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38639', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38641
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|ELECTRONIC SCALE|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38641' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'1,409271383E+10', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38641';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38641') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38641', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38643
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|ELECTRONIC SCALE|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38643' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'1,40927138E+10', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38643';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38643') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38643', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38798
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38798' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38798';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38798') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38798', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38799
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38799' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38799';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38799') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38799', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38800
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38800' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38800';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38800') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38800', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38801
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38801' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38801';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38801') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38801', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38802
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38802' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38802';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38802') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38802', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38803
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38803' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38803';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38803') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38803', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 38804
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'38804' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'38804';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'38804') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'38804', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39021
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'VITRINA METALICA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39021' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39021';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39021') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39021', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39161
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39161' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'8336330455', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39161';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39161') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39161', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39163
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39163' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39163';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39163') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39163', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39169
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39169' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'8336510895', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39169';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39169') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39169', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39170
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-1';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39170' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'8336510968', @RelatedId, CONVERT(datetime2, '2021-06-01', 23), NULL, 950, 0, 2, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39170';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39170') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39170', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39171
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BALANZA ELECTRONICA|OHAUS|RANGER R31P30';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39171' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'8336330455', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39171';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39171') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39171', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39269
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GARRAFA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39269' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39269';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39269') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39269', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39273
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|SAMSUNG|UN48J5000AGXZS';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39273' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'04NS3CVH801270', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39273';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39273') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39273', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39432
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39432' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39432';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39432') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39432', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39433
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39433' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39433';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39433') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39433', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39434
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39434' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39434';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39434') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39434', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39435
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39435' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39435';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39435') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39435', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39436
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39436' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39436';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39436') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39436', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39437
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39437' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39437';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39437') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39437', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39438
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESA DE MADERA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39438' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39438';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39438') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39438', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39479
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAMARA DE VIDEO|SONY BALUMS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39479' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39479';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39479') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39479', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39591
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ABATIDOR FASTER|AFINOX|FASTER 5T GF 230V';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39591' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'3016085604', @RelatedId, NULL, NULL, 33000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39591';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39591') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39591', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39632
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MUEBLE DE MADERA PARA COMPUTADORA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39632' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 3800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39632';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39632') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39632', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 39691
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA DE MADERA FIJA TAPIZ TELA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'39691' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'39691';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'39691') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'39691', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43486
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ESCRITORIO DE MELAMINA MODULAR||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43486' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43486';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43486') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43486', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43487
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'GAVETERO DE MELAMINA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43487' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43487';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43487') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43487', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43488
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43488' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43488';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43488') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43488', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43489
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43489' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43489';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43489') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43489', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43490
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SILLA METALICA FIJA TAPIZ CUERINA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43490' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43490';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43490') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43490', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 43491
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CREDENZA DE MELAMINA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'43491' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'43491';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'43491') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'43491', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44865
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44865' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44865';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44865') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44865', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44866
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|(YW-40) XJ-F20XN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44866' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'A995DDBY7B-093815', @RelatedId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44866';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44866') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44866', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44867
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44867' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'A995DDBY7B-093585', @RelatedId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44867';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44867') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44867', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44868
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44868' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'A995DDBY7C-098103', @RelatedId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44868';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44868') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44868', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44869
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PROYECTOR LED/LASER|CASIO|XJ-F20XN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44869' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'A995DDBY7B-093624', @RelatedId, NULL, NULL, 13000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44869';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44869') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44869', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44904
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591,';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44904' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'W74467732', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44904';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44904') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44904', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44905
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44905' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'W74467699', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44905';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44905') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44905', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44906
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44906' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'W74467691', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44906';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44906') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44906', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44907
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44907' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'W74467680', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44907';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44907') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44907', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 44908
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'BATIDORA|KITCHENAID|5KSM7591';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'44908' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'W74467600', @RelatedId, NULL, NULL, 8500, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'44908';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'44908') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'44908', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45024
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45024' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45024';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45024') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45024', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45025
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45025' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45025';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45025') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45025', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45026
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45026' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45026';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45026') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45026', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45027
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45027' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45027';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45027') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45027', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45028
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45028' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45028';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45028') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45028', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45029
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45029' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45029';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45029') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45029', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45030
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45030' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45030';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45030') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45030', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45031
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45031' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45031';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45031') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45031', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45032
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45032' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45032';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45032') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45032', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45033
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CASILLERO METALICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45033' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 2300, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45033';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45033') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45033', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45744
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|HAIER|LE55B8500DUA';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45744' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'DH1VM0D4401D9H9C0323', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45744';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45744') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45744', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45745
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TELEVISOR LED|HAIER|LE55B8500DUA';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45745' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'DH1VM0D4401D9H9C0353', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45745';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45745') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45745', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 45840
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'APARATO TELEFONICO|CISCO|CP-3905';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'45840' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'FCH2045GJVT', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'45840';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'45840') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'45840', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46358
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ALL IN ONE|HP|24-E015LA';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46358' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'8CC80516KV', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46358';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46358') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46358', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46359
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'TECLADO|HP|PR1101U';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46359' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'BFZYF0ALAAC0H9', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46359';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46359') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46359', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46373
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46373' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46373';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46373') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46373', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46374
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46374' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46374';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46374') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46374', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46375
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46375' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46375';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46375') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46375', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46376
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46376' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46376';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46376') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46376', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46377
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46377' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46377';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46377') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46377', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46378
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46378' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46378';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46378') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46378', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46379
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46379' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46379';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46379') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46379', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46380
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46380' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46380';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46380') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46380', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46381
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46381' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46381';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46381') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46381', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46382
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46382' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46382';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46382') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46382', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46383
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46383' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46383';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46383') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46383', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46384
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46384' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46384';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46384') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46384', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46385
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46385' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46385';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46385') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46385', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46386
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46386' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46386';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46386') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46386', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46387
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46387' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46387';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46387') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46387', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46388
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46388' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46388';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46388') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46388', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46389
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46389' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46389';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46389') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46389', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46390
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46390' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46390';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46390') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46390', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46391
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46391' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46391';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46391') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46391', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46392
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46392' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46392';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46392') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46392', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46393
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46393' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46393';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46393') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46393', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46394
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46394' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46394';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46394') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46394', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46395
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46395' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46395';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46395') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46395', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46396
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46396' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46396';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46396') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46396', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46397
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46397' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46397';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46397') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46397', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46398
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46398' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46398';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46398') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46398', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46399
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46399' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46399';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46399') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46399', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46400
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46400' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46400';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46400') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46400', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46401
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46401' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46401';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46401') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46401', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46402
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46402' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46402';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46402') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46402', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46403
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46403' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46403';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46403') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46403', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46404
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46404' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46404';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46404') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46404', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46405
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46405' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46405';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46405') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46405', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 46406
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PORTA UTENSILIOS||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'46406' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'46406';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'46406') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'46406', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 47789
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS|VREF-1000BEN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'47789' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'YBL9340CL6200319031300350005', @RelatedId, NULL, NULL, 1800, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47789';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'47789') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47789', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 47790
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'REFRIGERADOR|VENTUS SORP|VREF-1000BEN';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'47790' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'YBL9340CL6200319031300350020', @RelatedId, NULL, NULL, 18000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47790';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'47790') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47790', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 47873
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'47873' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'EPL3520CL6200319070300350004', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47873';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'47873') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47873', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 47874
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MESON REFRIGERADOR|VENTUS|VMR2PS-280E';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'47874' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'EPL3520CL6200319070300350002', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'47874';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'47874') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'47874', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 48024
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'PIZARRA CON MARCO METALICO ACRILICO||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'48024' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48024';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'48024') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48024', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 48885
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'CAFETERA IND|ASTORIA|INDUS. ITALIANA';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'48885' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'913677', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48885';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'48885') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48885', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 48886
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'FLITRO ABLANDADOR DE AGUA||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'48886' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'48886';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'48886') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'48886', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49055
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MOLINO DE COFFIE|FIORENZATO|F64 E';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49055' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'66666622', @RelatedId, NULL, NULL, 18000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49055';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49055') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49055', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49134
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'HORNO CONVECTOR|ARIANNA|XEFT-04HS-ELDV';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49134' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'2021L0105834', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49134';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49134') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49134', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49136
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|PANASONIC|NN-ST34HM';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49136' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'5A39210153', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49136';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49136') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49136', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49190
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|XPERT';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49190' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49190';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49190') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49190', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49192
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|XPERT';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49192' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49192';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49192') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49192', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49220
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'ENVASADORA AL VACIO|VENTUS|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49220' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'202031007', @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49220';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49220') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49220', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49221
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|SAMSUNG|MG402MADXBB';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49221' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'0AMM7WFT600193', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49221';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49221') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49221', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49222
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49222' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49222';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49222') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49222', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49223
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49223' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 12000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49223';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49223') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49223', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49224
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'LICUADORA|OSTER|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49224' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 1200, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49224';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49224') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49224', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49236
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'THERMOMIX||TM6';
SELECT @LabId = Id FROM @LabMap WHERE [Key] = N'H-3';
SELECT @RelatedId = Id FROM @CareerMap WHERE [Key] = N'Gastronomía';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49236' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'62144842778514', @RelatedId, CONVERT(datetime2, '2023-01-15', 23), NULL, 3500, 0, 4, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49236';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49236') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49236', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49511
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49511' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49511';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49511') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49511', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49512
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49512' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49512';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49512') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49512', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49513
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49513' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49513';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49513') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49513', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49514
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49514' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49514';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49514') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49514', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49515
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49515' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49515';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49515') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49515', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49516
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'EXTINTOR TIPO K||';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49516' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, 4000, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49516';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49516') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49516', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49820
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49820' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'302TATGEX185', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49820';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49820') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49820', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49821
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49821' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'302TALBEX280', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49821';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49821') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49821', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49822
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49822' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'302TAACEX312', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49822';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49822') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49822', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49823
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49823' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'302TAVYEX295', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49823';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49823') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49823', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49824
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'MICROONDA|LG|MH8236GIR';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49824' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, N'302TAVVEX226', @RelatedId, NULL, NULL, 1600, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49824';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49824') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49824', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49873
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49873' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49873';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49873') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49873', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49874
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49874' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49874';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49874') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49874', @EntityId, @EquipmentId, @LabId);

-- EquipmentUnit: 49875
SET @EntityId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @RelatedId = NULL; SET @ManagementId = NULL;
SELECT @EquipmentId = Id FROM @EquipmentMap WHERE [Key] = N'SOUS VIDE|METVISA|';
SELECT TOP 1 @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE EquipmentUnits AS target USING (SELECT N'49875' AS InventoryNumber) AS source ON target.InventoryNumber = source.InventoryNumber WHEN MATCHED THEN UPDATE SET EquipmentId = COALESCE(target.EquipmentId, @EquipmentId), LaboratoryId = COALESCE(target.LaboratoryId, @LabId), CareerId = COALESCE(target.CareerId, @RelatedId) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentId, LaboratoryId, InventoryNumber, SerialNumber, CareerId, AcquisitionDate, ManufacturingDate, AcquisitionValue, CurrentStatus, PhysicalCondition, Notes, CreatedDate, CreatedById) VALUES (@ManagementId, @EquipmentId, @LabId, source.InventoryNumber, NULL, @RelatedId, NULL, NULL, NULL, 0, NULL, NULL, @Today, @CreatedById);
SELECT @EntityId = Id FROM EquipmentUnits WHERE InventoryNumber = N'49875';
IF NOT EXISTS (SELECT 1 FROM @UnitMap WHERE [Key] = N'49875') INSERT INTO @UnitMap ([Key], Id, EquipmentId, LaboratoryId) VALUES (N'49875', @EntityId, @EquipmentId, @LabId);

-- 01 EQUIPMENT: Excel directo=663; Inferido=0

