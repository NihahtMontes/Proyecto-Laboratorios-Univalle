-- ============================================================
-- CORRECTIVO CHAINS
-- ============================================================
-- Chain: 2025-1 / 35528
-- Request: REQ_R42_35528
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'35528';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 1 AS [Type], N'FACTORES' AS Description, NULL AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'NINGUNA | Solicitado por: Ing. Sara Mariel Perez Y.', NULL, 0, 1, source.InvestmentCode, NULL, CONVERT(datetime2, '2024-11-11', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 1 AND Description = N'FACTORES' AND ISNULL(InvestmentCode, N'') = ISNULL(NULL, N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'REQ_R42_35528') INSERT INTO @RequestMap ([Key], Id) VALUES (N'REQ_R42_35528', @RequestId);

-- Maintenance: MNT_R6_35528
SET @MaintenanceId = NULL; SET @UnitId = NULL; SET @ManagementId = NULL; SET @RelatedId = NULL; SET @RequestId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'35528';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
SELECT @RelatedId = Id FROM @PersonMap WHERE [Key] = N'CASATERMO SRL';
SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = N'REQ_R42_35528';
MERGE Maintenances AS target USING (SELECT @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 2 AS MaintenanceType, N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.' AS Description) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.ManagementId = source.ManagementId AND target.MaintenanceType = source.MaintenanceType AND ISNULL(target.Description, N'') = ISNULL(source.Description, N'') WHEN MATCHED THEN UPDATE SET RequestId = COALESCE(target.RequestId, @RequestId), TechnicianId = COALESCE(target.TechnicianId, @RelatedId), Status = CASE WHEN target.Status < 2 THEN 2 ELSE target.Status END, ActualCost = COALESCE(target.ActualCost, 400) WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, MaintenanceType, ManagementId, ServiceType, TechnicianId, RequestId, ScheduledDate, StartDate, EndDate, Description, Status, CompletionPercentage, Step1_Cleaning, Step2_Calibration, Step3_Testing, Step4_FinalReview, ActualCost, SatisfactionLevel, Recommendations, SuggestedNextMaintenanceDate, CreatedDate, CreatedById) VALUES (@UnitId, 2, @ManagementId, 1, @RelatedId, @RequestId, NULL, CONVERT(datetime2, '2023-01-31', 23), CONVERT(datetime2, '2023-01-31', 23), N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.', 2, 100, 1, 1, 1, 1, 400, 5, N'Revisión anual preventiva obligatoria. Verificar válvula.', CONVERT(datetime2, '2024-01-10', 23), @Today, @CreatedById);
SELECT TOP 1 @MaintenanceId = Id FROM Maintenances WHERE EquipmentUnitId = @UnitId AND ManagementId = @ManagementId AND MaintenanceType = 2 AND ISNULL(Description, N'') = ISNULL(N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.', N'');
IF NOT EXISTS (SELECT 1 FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528') INSERT INTO @MaintenanceMap ([Key], Id) VALUES (N'MNT_R6_35528', @MaintenanceId);

SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
MERGE MaintenanceTasks AS target USING (SELECT @MaintenanceId AS MaintenanceId, N'MANTENIMIENTO CORRECTIVO TERMOTANQUE. VALVULA DE RETENCION. ENTREGA DEL EQUIPO FUNCIONANDO.' AS Description) AS source ON target.MaintenanceId = source.MaintenanceId AND target.Description = source.Description WHEN MATCHED THEN UPDATE SET IsCompleted = CASE WHEN target.IsCompleted = 1 THEN 1 ELSE 1 END, IsDeleted = 0 WHEN NOT MATCHED THEN INSERT (MaintenanceId, Description, IsCompleted, IsDeleted) VALUES (source.MaintenanceId, source.Description, 1, 0);

-- Kardex history: INF_KDX_20251_35528
SET @HistoryId = NULL; SET @UnitId = NULL;
SELECT @UnitId = Id FROM @UnitMap WHERE [Key] = N'35528';
MERGE EquipmentStateHistories AS target USING (SELECT @UnitId AS EquipmentUnitId, 0 AS Status, CONVERT(datetime2, '2023-01-31', 23) AS StartDate, N'Kardex historico inferido desde mantenimiento MNT_R6_35528.' AS Reason) AS source ON target.EquipmentUnitId = source.EquipmentUnitId AND target.Status = source.Status AND CONVERT(date, target.StartDate) = CONVERT(date, source.StartDate) AND ISNULL(target.Reason, N'') = ISNULL(source.Reason, N'') WHEN NOT MATCHED THEN INSERT (EquipmentUnitId, Status, StartDate, Reason, CreatedDate, CreatedById) VALUES (source.EquipmentUnitId, source.Status, source.StartDate, source.Reason, @Today, @CreatedById);
SELECT TOP 1 @HistoryId = Id FROM EquipmentStateHistories WHERE EquipmentUnitId = @UnitId AND Status = 0 AND CONVERT(date, StartDate) = CONVERT(date, CONVERT(datetime2, '2023-01-31', 23)) AND ISNULL(Reason, N'') = ISNULL(N'Kardex historico inferido desde mantenimiento MNT_R6_35528.', N'');
IF NOT EXISTS (SELECT 1 FROM @HistoryMap WHERE [Key] = N'INF_KDX_20251_35528') INSERT INTO @HistoryMap ([Key], Id) VALUES (N'INF_KDX_20251_35528', @HistoryId);
UPDATE EquipmentUnits SET CurrentStatus = 0 WHERE Id = @UnitId AND CurrentStatus <> 99;


-- CORRECTIVO CHAINS: Excel directo=21; Inferido=8

