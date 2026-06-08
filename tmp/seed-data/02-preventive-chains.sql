-- ============================================================
-- PREVENTIVO CHAINS
-- ============================================================
-- Chain: 2023-2 / 49236
-- Verification: INF_VER_20232_49236
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'49236';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2023-2';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2023-07-09', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 49236.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 49236.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2023-07-09', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20232_49236') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20232_49236', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: INF_REQ_20232_49236
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'49236';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2023-2';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'Solicitud tecnica preventiva inferida para completar la cadena historica del inventario 49236.' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'Registro inferido desde cadena historica del Excel.', NULL, 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2023-07-16', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'Solicitud tecnica preventiva inferida para completar la cadena historica del inventario 49236.' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'INF_REQ_20232_49236') INSERT INTO @RequestMap ([Key], Id) VALUES (N'INF_REQ_20232_49236', @RequestId);

-- Maintenance: INF_MNT_20232_49236
SET @MaintenanceId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL; SET @RequestId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'49236';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2023-2';
SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = N'ING. APAZA';
SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = N'INF_REQ_20232_49236';
MERGE Maintenances AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS MaintenanceType, N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 49236.' AS Description) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND target.MaintenanceType = source.MaintenanceType AND ISNULL(target.Description, N'') = ISNULL(source.Description, N'') WHEN MATCHED THEN UPDATE SET RequestId = COALESCE(target.RequestId, @RequestId), TechnicianId = COALESCE(target.TechnicianId, @RelatedId), Status = CASE WHEN target.Status < 2 THEN 2 ELSE target.Status END, ActualCost = COALESCE(target.ActualCost, NULL) WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, CreatedDate, CreatedById) VALUES (@UnitId, 1, @ManagementId, 0, @RelatedId, @RequestId, CONVERT(datetime2, '2023-07-13', 23), CONVERT(datetime2, '2023-07-14', 23), CONVERT(datetime2, '2023-07-15', 23), N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 49236.', 2, 100, 1, 1, 1, 1, NULL, 4, N'Equipo revisado y habilitado para continuidad del flujo historico.', CONVERT(datetime2, '2024-01-16', 23), @Today, @CreatedById);
SELECT TOP 1 @MaintenanceId = Id FROM Maintenances WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND MaintenanceType = 1 AND ISNULL(Description, N'') = ISNULL(N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 49236.', N'');
IF NOT EXISTS (SELECT 1 FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20232_49236') INSERT INTO @MaintenanceMap ([Key], Id) VALUES (N'INF_MNT_20232_49236', @MaintenanceId);

SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20232_49236';
MERGE MaintenanceTasks AS target USING (SELECT @MaintenanceId AS MaintenanceId, N'Revision tecnica general del equipo' AS Description) AS source ON target.MaintenanceId = source.MaintenanceId AND target.Description = source.Description WHEN MATCHED THEN UPDATE SET IsCompleted = CASE WHEN target.IsCompleted = 1 THEN 1 ELSE 1 END, IsDeleted = 0 WHEN NOT MATCHED THEN INSERT (MaintenanceId, Description, IsCompleted, IsDeleted) VALUES (source.MaintenanceId, source.Description, 1, 0);

-- Kardex history: INF_KDX_20232_49236
SET @HistoryId = NULL; SET @UnitId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'49236';
MERGE EquipmentStateHistories AS target USING (SELECT @UnitId AS EquipmentUnitId, 0 AS Status, CONVERT(datetime2, '2023-07-15', 23) AS StartDate, N'Kardex historico inferido desde mantenimiento INF_MNT_20232_49236.' AS Reason) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.Status = source.Status AND CONVERT(date, target.StartDate) = CONVERT(date, source.StartDate) AND ISNULL(target.Reason, N'') = ISNULL(source.Reason, N'') WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, Status, StartDate, Reason, CreatedDate, CreatedById) VALUES (source.EquipmentUnitId, source.Status, source.StartDate, source.Reason, @Today, @CreatedById);
SELECT TOP 1 @HistoryId = Id FROM EquipmentStateHistories WHERE EquipmentUnitId = @UnitId AND Status = 0 AND CONVERT(date, StartDate) = CONVERT(date, CONVERT(datetime2, '2023-07-15', 23)) AND ISNULL(Reason, N'') = ISNULL(N'Kardex historico inferido desde mantenimiento INF_MNT_20232_49236.', N'');
IF NOT EXISTS (SELECT 1 FROM @HistoryMap WHERE [Key] = N'INF_KDX_20232_49236') INSERT INTO @HistoryMap ([Key], Id) VALUES (N'INF_KDX_20232_49236', @HistoryId);
UPDATE EquipmentUnits SET CurrentStatus = 0 WHERE Id = @UnitId AND CurrentStatus <> 99;


-- Chain: 2025-1 / 17355
-- Verification: INF_VER_20251_17355
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'17355';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 17355.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 17355.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_17355') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_17355', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R22_17355
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'17355';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'MANTENIMIENTO PREVENTIVO, LIMPIEZA PROFUNDA.' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'MANTENIMIENTO PREVENTIVO, LIMPIEZA PROFUNDA.' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R22_17355') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R22_17355', @RequestId);


-- Chain: 2025-1 / 34179
-- Verification: INF_VER_20251_34179
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 34179.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 34179.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_34179') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_34179', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R10_34179
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34179';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R10_34179') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R10_34179', @RequestId);

-- Maintenance: INF_MNT_20251_34179
SET @MaintenanceId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL; SET @RequestId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = N'ING. SARA PEREZ YAÑEZ';
SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = N'REQ_R10_34179';
MERGE Maintenances AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS MaintenanceType, N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 34179.' AS Description) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND target.MaintenanceType = source.MaintenanceType AND ISNULL(target.Description, N'') = ISNULL(source.Description, N'') WHEN MATCHED THEN UPDATE SET RequestId = COALESCE(target.RequestId, @RequestId), TechnicianId = COALESCE(target.TechnicianId, @RelatedId), Status = CASE WHEN target.Status < 2 THEN 2 ELSE target.Status END, ActualCost = COALESCE(target.ActualCost, NULL) WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, CreatedDate, CreatedById) VALUES (@UnitId, 1, @ManagementId, 1, @RelatedId, @RequestId, CONVERT(datetime2, '2024-11-08', 23), CONVERT(datetime2, '2024-11-09', 23), CONVERT(datetime2, '2024-11-10', 23), N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 34179.', 2, 100, 1, 1, 1, 1, NULL, 4, N'Equipo revisado y habilitado para continuidad del flujo historico.', CONVERT(datetime2, '2025-05-11', 23), @Today, @CreatedById);
SELECT TOP 1 @MaintenanceId = Id FROM Maintenances WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND MaintenanceType = 1 AND ISNULL(Description, N'') = ISNULL(N'Mantenimiento historico inferido para completar Kardex/L-48 del inventario 34179.', N'');
IF NOT EXISTS (SELECT 1 FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20251_34179') INSERT INTO @MaintenanceMap ([Key], Id) VALUES (N'INF_MNT_20251_34179', @MaintenanceId);

SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20251_34179';
MERGE MaintenanceTasks AS target USING (SELECT @MaintenanceId AS MaintenanceId, N'Revision tecnica general del equipo' AS Description) AS source ON target.MaintenanceId = source.MaintenanceId AND target.Description = source.Description WHEN MATCHED THEN UPDATE SET IsCompleted = CASE WHEN target.IsCompleted = 1 THEN 1 ELSE 1 END, IsDeleted = 0 WHEN NOT MATCHED THEN INSERT (MaintenanceId, Description, IsCompleted, IsDeleted) VALUES (source.MaintenanceId, source.Description, 1, 0);

-- Departure: DEP_R6_34179
SET @DepartureId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = N'ING. SARA PEREZ YAÑEZ';
MERGE Departures AS target USING (SELECT @ManagementId AS ManagementId, @UnitId AS EquipmentUnitId, 3 AS [Type], CONVERT(datetime2, '2025-10-24', 23) AS DepartureDate) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND CONVERT(date, target.DepartureDate) = CONVERT(date, source.DepartureDate) WHEN MATCHED THEN UPDATE SET BorrowerId = COALESCE(target.BorrowerId, @RelatedId), EstimatedReturnDate = CONVERT(datetime2, '2025-10-24', 23) WHEN NOT MATCHED THEN INSERT (ManagementId, EquipmentUnitId, BorrowerId, Type, DepartureDate, EstimatedReturnDate, DepartureObservations, Status, CreatedDate, CreatedById) VALUES (@ManagementId, @UnitId, @RelatedId, 3, CONVERT(datetime2, '2025-10-24', 23), CONVERT(datetime2, '2025-10-24', 23), N'Sale para mantenimiento correctivo. Ver kardex ID 1.', 0, @Today, @CreatedById);
SELECT TOP 1 @DepartureId = Id FROM Departures WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 3 AND CONVERT(date, DepartureDate) = CONVERT(date, CONVERT(datetime2, '2025-10-24', 23));
IF NOT EXISTS (SELECT 1 FROM @DepartureMap WHERE [Key] = N'DEP_R6_34179') INSERT INTO @DepartureMap ([Key], Id) VALUES (N'DEP_R6_34179', @DepartureId);

SET @DepartureId = NULL; SET @UnitId = NULL;
SELECT @DepartureId = Id FROM @DepartureMap WHERE [Key] = N'DEP_R6_34179';
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
MERGE DepartureItems AS target USING (SELECT @DepartureId AS DepartureId, @UnitId AS EquipmentUnitId, N'COCINA INDUSTRIAL' AS ProductName) AS source ON target.DepartureId = source.DepartureId AND ISNULL(target.EquipmentUnitId, 0) = ISNULL(source.EquipmentUnitId, 0) AND target.ProductName = source.ProductName WHEN MATCHED THEN UPDATE SET IsRemoved = 0 WHEN NOT MATCHED THEN INSERT (DepartureId, EquipmentUnitId, ProductName, Quantity, UnitOfMeasure, Observations, IsRemoved, CreatedDate, CreatedById) VALUES (@DepartureId, @UnitId, source.ProductName, 1, N'UNIDAD', N'Sale para mantenimiento correctivo. Ver kardex ID 1.', 0, @Today, @CreatedById);

-- Kardex history: INF_KDX_20251_34179
SET @HistoryId = NULL; SET @UnitId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34179';
MERGE EquipmentStateHistories AS target USING (SELECT @UnitId AS EquipmentUnitId, 0 AS Status, CONVERT(datetime2, '2024-11-10', 23) AS StartDate, N'Kardex historico inferido desde mantenimiento INF_MNT_20251_34179.' AS Reason) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.Status = source.Status AND CONVERT(date, target.StartDate) = CONVERT(date, source.StartDate) AND ISNULL(target.Reason, N'') = ISNULL(source.Reason, N'') WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, Status, StartDate, Reason, CreatedDate, CreatedById) VALUES (source.EquipmentUnitId, source.Status, source.StartDate, source.Reason, @Today, @CreatedById);
SELECT TOP 1 @HistoryId = Id FROM EquipmentStateHistories WHERE EquipmentUnitId = @UnitId AND Status = 0 AND CONVERT(date, StartDate) = CONVERT(date, CONVERT(datetime2, '2024-11-10', 23)) AND ISNULL(Reason, N'') = ISNULL(N'Kardex historico inferido desde mantenimiento INF_MNT_20251_34179.', N'');
IF NOT EXISTS (SELECT 1 FROM @HistoryMap WHERE [Key] = N'INF_KDX_20251_34179') INSERT INTO @HistoryMap ([Key], Id) VALUES (N'INF_KDX_20251_34179', @HistoryId);
UPDATE EquipmentUnits SET CurrentStatus = 0 WHERE Id = @UnitId AND CurrentStatus <> 99;


-- Chain: 2025-1 / 34180
-- Verification: INF_VER_20251_34180
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34180';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 34180.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 34180.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_34180') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_34180', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R11_34180
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34180';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'SE REQUIERE MANTENIMIENTO PREVENTIVO A COCINAS.
SE TIENE PRESENCIA DE LLAMA NARANJA POR COMBUSTION INCOMPLETA. REQUIERE REGULACION DE FLUJO DE GAS.
SE REQUIERE LIMPIEZA PROFUNDA, CON RETIRO DE GRASAS ADHERIDAS A PARTES.
SE REQUIERE REAJUSTE DE PERILLAS
SE REQUIERE REVISION Y/O CAMBIO DE O-RING EN VALVULAS , CAMBIO DE VALVULAS
SE REQUIERE LIMPIEZA VENTURI COCINAS
SE REQUIERE EL CAMBIO DE LLANTAS GIRATORIAS INDUSTRIALES , SIN FRENO Y CON FRENO' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R11_34180') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R11_34180', @RequestId);


-- Chain: 2025-1 / 34744
-- Verification: INF_VER_20251_34744
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'34744';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 34744.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 34744.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_34744') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_34744', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R7_34744
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34744';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'SE REQUIERE MANTENIMIENTO, DESENGRASADO Y LIMPIEZA PROFUNDA DE FILTROS , DUCTOS Y LUMINARIAS. CON RETIRO DE GRASAS ADHERIDAS A PARTES.
ENTUBADO Y/O CANALIZACIÓN DE CABLES ELÉCTRICOS. AJUSTE DE SELECTORES DE PRENDIDO Y APAGADO DE LUMINARIAS.
SE REQUIERE REVISION DE TENSION DE ALAMBRES SUJETADORES DE CAMPANA ISLA.' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'XXX', 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'SE REQUIERE MANTENIMIENTO, DESENGRASADO Y LIMPIEZA PROFUNDA DE FILTROS , DUCTOS Y LUMINARIAS. CON RETIRO DE GRASAS ADHERIDAS A PARTES.
ENTUBADO Y/O CANALIZACIÓN DE CABLES ELÉCTRICOS. AJUSTE DE SELECTORES DE PRENDIDO Y APAGADO DE LUMINARIAS.
SE REQUIERE REVISION DE TENSION DE ALAMBRES SUJETADORES DE CAMPANA ISLA.' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R7_34744') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R7_34744', @RequestId);


-- Chain: 2025-1 / 39170
-- Verification: INF_VER_20251_39170
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'39170';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 39170.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 39170.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_39170') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_39170', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R6_39170
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'39170';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'SE REQUIERE CALIBRACION ANUAL' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', N'xxx', 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'SE REQUIERE CALIBRACION ANUAL' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R6_39170') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R6_39170', @RequestId);


-- Chain: 2025-1 / 49236
-- Verification: INF_VER_20251_49236
SET @VerificationId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'49236';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Verifications AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, CONVERT(datetime2, '2024-11-04', 23) AS [Date]) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND CONVERT(date, target.[Date]) = CONVERT(date, source.[Date]) WHEN MATCHED THEN UPDATE SET Observations = COALESCE(target.Observations, N'Verificacion preventiva inferida para completar la cadena historica del inventario 49236.'), Status = CASE WHEN target.Status = 0 THEN 2 ELSE target.Status END WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, ManagementId, [Date], Observations, PhysicalCondition, Status, CreatedDate, CreatedById) VALUES (@UnitId, @ManagementId, source.[Date], N'Verificacion preventiva inferida para completar la cadena historica del inventario 49236.', 3, 2, @Today, @CreatedById);
SELECT TOP 1 @VerificationId = Id FROM Verifications WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND CONVERT(date, [Date]) = CONVERT(date, CONVERT(datetime2, '2024-11-04', 23));
IF NOT EXISTS (SELECT 1 FROM @VerificationMap WHERE [Key] = N'INF_VER_20251_49236') INSERT INTO @VerificationMap ([Key], Id) VALUES (N'INF_VER_20251_49236', @VerificationId);
MERGE VerificationFaults AS target USING (SELECT @VerificationId AS VerificationId, N'Observacion tecnica inferida desde solicitud/mantenimiento historico.' AS Description) AS source ON target.VerificationId = source.VerificationId AND target.Description = source.Description WHEN NOT MATCHED THEN INSERT (VerificationId, Description, IsDeleted, CreatedDate, CreatedById) VALUES (source.VerificationId, source.Description, 0, @Today, @CreatedById);
INSERT INTO VerificationCheckResults (VerificationId, CheckItemId, Result) SELECT @VerificationId, i.Id, 1 FROM VerificationCheckItems i WHERE i.IsActive = 1 AND NOT EXISTS (SELECT 1 FROM VerificationCheckResults r WHERE r.VerificationId = @VerificationId AND r.CheckItemId = i.Id);

-- Request: REQ_R94_49236
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'49236';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'MANTENIMIENTO PREVENTIVO, LIMPIEZA DE COMPONENTES.
ACTUALIZACION DE SOFTWARE' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'MANTENIMIENTO PREVENTIVO, LIMPIEZA DE COMPONENTES.
ACTUALIZACION DE SOFTWARE' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R94_49236') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R94_49236', @RequestId);


-- PREVENTIVO CHAINS: Excel directo=21; Inferido=22

