-- ============================================================
-- 04 ACQUISITIONS AND COSTS
-- ============================================================
-- Request: PUR_CUCHARA001_34179
SET @RequestId = NULL; SET @UnitId = NULL; SET @EquipmentId = NULL; SET @LabId = NULL; SET @ManagementId = NULL;
SELECT @UnitId = Id, @EquipmentId = EquipmentId, @LabId = LaboratoryId FROM @UnitMap WHERE [Key] = N'34179';
SELECT @ManagementId = Id FROM @ManagementMap WHERE [Key] = N'2025-1';
MERGE Requests AS target USING (SELECT @LabId AS LaboratoryId, @EquipmentId AS EquipmentId, @UnitId AS EquipmentUnitId, @ManagementId AS ManagementId, 2 AS [Type], N'O-Ring válvula gas cocina industrial' AS Description, N'CUCHARA-001' AS InvestmentCode) AS source ON target.ManagementId = source.ManagementId AND target.EquipmentUnitId = source.EquipmentUnitId AND target.[Type] = source.[Type] AND target.Description = source.Description AND ISNULL(target.InvestmentCode, N'') = ISNULL(source.InvestmentCode, N'') WHEN MATCHED THEN UPDATE SET Status = CASE WHEN target.Status < 0 THEN 0 ELSE target.Status END, Priority = 1 WHEN NOT MATCHED THEN INSERT (LaboratoryId, EquipmentId, EquipmentUnitId, ManagementId, Description, Priority, Observations, EstimatedRepairTime, Status, Type, InvestmentCode, CostCenter, CreatedDate, CreatedById) VALUES (@LabId, @EquipmentId, @UnitId, @ManagementId, source.Description, 1, N'Responsable: ING. SARA PEREZ YAÑEZ', NULL, 0, 2, source.InvestmentCode, N'LAB-GASTRO-001', CONVERT(datetime2, '2025-10-24', 23), @CreatedById);
SELECT TOP 1 @RequestId = Id FROM Requests WHERE ManagementId = @ManagementId AND EquipmentUnitId = @UnitId AND [Type] = 2 AND Description = N'O-Ring válvula gas cocina industrial' AND ISNULL(InvestmentCode, N'') = ISNULL(N'CUCHARA-001', N'');
IF NOT EXISTS (SELECT 1 FROM @RequestMap WHERE [Key] = N'PUR_CUCHARA001_34179') INSERT INTO @RequestMap ([Key], Id) VALUES (N'PUR_CUCHARA001_34179', @RequestId);

-- CostDetail: MNT_R6_35528 / Servicio mantenimiento termotanque
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'Servicio mantenimiento termotanque' AS Concept, 1 AS Quantity, 350 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'servicio', Category = 5 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, NULL, source.Quantity, N'servicio', source.UnitPrice, 5, N'CASATERMO SRL', NULL, @Today, @CreatedById);

-- CostDetail: MNT_R6_35528 / Válvula de retención
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'MNT_R6_35528';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'Válvula de retención' AS Concept, 1 AS Quantity, 50 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'pieza', Category = 1 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, NULL, source.Quantity, N'pieza', source.UnitPrice, 1, N'CASATERMO SRL', NULL, @Today, @CreatedById);

-- CostDetail: PUR_CUCHARA001_34179 / O-Ring válvula gas cocina industrial
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = N'PUR_CUCHARA001_34179';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'O-Ring válvula gas cocina industrial' AS Concept, 2 AS Quantity, 45 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'pieza', Category = 1 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, NULL, source.Quantity, N'pieza', source.UnitPrice, 1, NULL, NULL, @Today, @CreatedById);

-- CostDetail: PUR_CUCHARA001_34179 / Mano de obra regulación y limpieza cocina INOX
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @RequestId = Id FROM @RequestMap WHERE [Key] = N'PUR_CUCHARA001_34179';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'Mano de obra regulación y limpieza cocina INOX' AS Concept, 1 AS Quantity, 350 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'servicio', Category = 2 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, NULL, source.Quantity, N'servicio', source.UnitPrice, 2, NULL, NULL, @Today, @CreatedById);

-- CostDetail: INF_MNT_20232_49236 / Servicio preventivo historico inferido
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20232_49236';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'Servicio preventivo historico inferido' AS Concept, 1 AS Quantity, 180 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'servicio', Category = 5 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, N'Costo tecnico inferido para completar cierre de Kardex.', source.Quantity, N'servicio', source.UnitPrice, 5, NULL, NULL, @Today, @CreatedById);

-- CostDetail: INF_MNT_20251_34179 / O-Ring válvula gas cocina industrial (Kardex)
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20251_34179';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'O-Ring válvula gas cocina industrial (Kardex)' AS Concept, 2 AS Quantity, 45 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'pieza', Category = 1 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, N'Copia inferida para reflejar el costo en Kardex/L-8.', source.Quantity, N'pieza', source.UnitPrice, 1, NULL, NULL, @Today, @CreatedById);

-- CostDetail: INF_MNT_20251_34179 / Mano de obra regulación y limpieza cocina INOX (Kardex)
SET @RequestId = NULL; SET @MaintenanceId = NULL;
SELECT @MaintenanceId = Id FROM @MaintenanceMap WHERE [Key] = N'INF_MNT_20251_34179';
MERGE CostDetails AS target USING (SELECT @RequestId AS RequestId, @MaintenanceId AS MaintenanceId, N'Mano de obra regulación y limpieza cocina INOX (Kardex)' AS Concept, 1 AS Quantity, 350 AS UnitPrice) AS source ON ISNULL(target.RequestId, 0) = ISNULL(source.RequestId, 0) AND ISNULL(target.MaintenanceId, 0) = ISNULL(source.MaintenanceId, 0) AND target.Concept = source.Concept AND target.UnitPrice = source.UnitPrice WHEN MATCHED THEN UPDATE SET Quantity = source.Quantity, UnitOfMeasure = N'servicio', Category = 2 WHEN NOT MATCHED THEN INSERT (RequestId, MaintenanceId, Concept, Description, Quantity, UnitOfMeasure, UnitPrice, Category, Provider, InvoiceNumber, CreatedDate, CreatedById) VALUES (@RequestId, @MaintenanceId, source.Concept, N'Copia inferida para reflejar el costo en Kardex/L-8.', source.Quantity, N'servicio', source.UnitPrice, 2, NULL, NULL, @Today, @CreatedById);

-- 04 ACQUISITIONS AND COSTS: Excel directo=19; Inferido=4

