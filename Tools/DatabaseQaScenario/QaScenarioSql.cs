namespace Proyecto_Laboratorios_Univalle.Tools.DatabaseQaScenario;

internal static class QaScenarioSql
{
    public static string Build() => """
        SET NOCOUNT ON;
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        DECLARE @Now datetime2=SYSUTCDATETIME();
        DECLARE @BatchId int;
        SELECT @BatchId=Id FROM dbo.ImportBatches WHERE Code=N'QA-DATA-001-FUNCTIONAL-V1';
        IF @BatchId IS NULL
        BEGIN
            INSERT dbo.ImportBatches(Code,SourceType,ContractVersion,SourceName,SourceSha256,ImportedAt,Status,Notes)
            VALUES(N'QA-DATA-001-FUNCTIONAL-V1',N'SyntheticQA',N'qa-data-001-v1',N'Generador determinista 20260904',REPLICATE(N'0',64),@Now,1,N'Históricos sintéticos; prohibida su promoción a negocio.');
            SET @BatchId=CONVERT(int,SCOPE_IDENTITY());
        END;

        ;WITH N AS(SELECT 1 n UNION ALL SELECT n+1 FROM N WHERE n<12)
        INSERT dbo.People(Status,Category,Email,PhoneNumber,CreatedDate,CreatedById,ImportBatchId,ActorCode)
        SELECT 0,1,CONCAT(N'qa.tecnico',RIGHT(N'00'+CONVERT(nvarchar(2),n),2),N'@local.test'),CONCAT(N'7100',RIGHT(N'0000'+CONVERT(nvarchar(4),n),4)),@Now,@AdminId,@BatchId,CONCAT(N'QA-TEC-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))
        FROM N WHERE NOT EXISTS(SELECT 1 FROM dbo.People p WHERE p.ActorCode=CONCAT(N'QA-TEC-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))) OPTION(MAXRECURSION 100);

        ;WITH N AS(SELECT 1 n UNION ALL SELECT n+1 FROM N WHERE n<10)
        INSERT dbo.People(Status,Category,Email,PhoneNumber,CreatedDate,CreatedById,ImportBatchId,ActorCode)
        SELECT 0,2,CONCAT(N'qa.solicitante',RIGHT(N'00'+CONVERT(nvarchar(2),n),2),N'@local.test'),CONCAT(N'7200',RIGHT(N'0000'+CONVERT(nvarchar(4),n),4)),@Now,@AdminId,@BatchId,CONCAT(N'QA-SOL-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))
        FROM N WHERE NOT EXISTS(SELECT 1 FROM dbo.People p WHERE p.ActorCode=CONCAT(N'QA-SOL-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))) OPTION(MAXRECURSION 100);

        ;WITH N AS(SELECT 1 n UNION ALL SELECT n+1 FROM N WHERE n<8)
        INSERT dbo.People(Status,Category,Email,PhoneNumber,CreatedDate,CreatedById,ImportBatchId,ActorCode)
        SELECT 0,5,CONCAT(N'qa.proveedor',RIGHT(N'00'+CONVERT(nvarchar(2),n),2),N'@local.test'),CONCAT(N'7300',RIGHT(N'0000'+CONVERT(nvarchar(4),n),4)),@Now,@AdminId,@BatchId,CONCAT(N'QA-PROV-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))
        FROM N WHERE NOT EXISTS(SELECT 1 FROM dbo.People p WHERE p.ActorCode=CONCAT(N'QA-PROV-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))) OPTION(MAXRECURSION 100);

        INSERT dbo.Interns(Id,Name,InternStatus)
        SELECT p.Id,CONCAT(N'Técnico QA ',RIGHT(p.ActorCode,3)),0 FROM dbo.People p
        WHERE p.ActorCode LIKE N'QA-TEC-%' AND NOT EXISTS(SELECT 1 FROM dbo.Interns i WHERE i.Id=p.Id);
        INSERT dbo.Interns(Id,Name,InternStatus)
        SELECT p.Id,CONCAT(N'Solicitante QA ',RIGHT(p.ActorCode,3)),0 FROM dbo.People p
        WHERE p.ActorCode LIKE N'QA-SOL-%' AND NOT EXISTS(SELECT 1 FROM dbo.Interns i WHERE i.Id=p.Id);
        INSERT dbo.Externs(Id,IsEntity,Name,Address,ExternStatus)
        SELECT p.Id,1,CONCAT(N'Proveedor QA ',RIGHT(p.ActorCode,3)),N'Dirección sintética QA',0 FROM dbo.People p
        WHERE p.ActorCode LIKE N'QA-PROV-%' AND NOT EXISTS(SELECT 1 FROM dbo.Externs e WHERE e.Id=p.Id);

        INSERT dbo.PersonRoleAssignments(PersonId,Role,IsActive,ValidFrom)
        SELECT p.Id,1,1,@Now FROM dbo.People p WHERE p.ActorCode LIKE N'QA-TEC-%'
          AND NOT EXISTS(SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId=p.Id AND r.Role=1 AND r.IsActive=1);
        INSERT dbo.PersonRoleAssignments(PersonId,Role,IsActive,ValidFrom)
        SELECT p.Id,5,1,@Now FROM dbo.People p WHERE p.ActorCode LIKE N'QA-SOL-%'
          AND NOT EXISTS(SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId=p.Id AND r.Role=5 AND r.IsActive=1);
        INSERT dbo.PersonRoleAssignments(PersonId,Role,IsActive,ValidFrom)
        SELECT p.Id,7,1,@Now FROM dbo.People p WHERE p.ActorCode LIKE N'QA-SOL-%'
          AND NOT EXISTS(SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId=p.Id AND r.Role=7 AND r.IsActive=1);
        INSERT dbo.PersonRoleAssignments(PersonId,Role,IsActive,ValidFrom)
        SELECT p.Id,3,1,@Now FROM dbo.People p WHERE p.ActorCode LIKE N'QA-PROV-%'
          AND NOT EXISTS(SELECT 1 FROM dbo.PersonRoleAssignments r WHERE r.PersonId=p.Id AND r.Role=3 AND r.IsActive=1);

        ;WITH N AS(SELECT 1 n UNION ALL SELECT n+1 FROM N WHERE n<30)
        INSERT dbo.Articles(Code,Name,Category,UnitOfMeasure,Status,ImportBatchId,CreatedById,CreatedDate)
        SELECT CONCAT(N'QA-ART-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3)),CONCAT(N'Consumible QA ',RIGHT(N'00'+CONVERT(nvarchar(2),n),2)),
            CASE n%3 WHEN 0 THEN N'Limpieza' WHEN 1 THEN N'Repuesto' ELSE N'Laboratorio' END,
            CASE n%3 WHEN 0 THEN N'LITRO' WHEN 1 THEN N'UNIDAD' ELSE N'PAQUETE' END,0,@BatchId,@AdminId,@Now
        FROM N WHERE NOT EXISTS(SELECT 1 FROM dbo.Articles a WHERE a.Code=CONCAT(N'QA-ART-',RIGHT(N'000'+CONVERT(nvarchar(3),n),3))) OPTION(MAXRECURSION 100);

        DECLARE @FacultyId int=(SELECT TOP(1) Id FROM dbo.Faculties ORDER BY Id);
        ;WITH SourceManagement AS
        (
            SELECT * FROM(VALUES
                (2024,1,N'QA-2024-1',CONVERT(date,'2024-02-01'),CONVERT(date,'2024-06-30'),2,0),
                (2024,2,N'QA-2024-2',CONVERT(date,'2024-08-01'),CONVERT(date,'2024-12-15'),2,0),
                (2025,1,N'QA-2025-1',CONVERT(date,'2025-02-01'),CONVERT(date,'2025-06-30'),2,0),
                (2025,2,N'QA-2025-2',CONVERT(date,'2025-08-01'),CONVERT(date,'2025-12-15'),2,0),
                (2026,1,N'QA-2026-1',CONVERT(date,'2026-02-01'),CONVERT(date,'2026-06-30'),2,0),
                (2026,2,N'QA-2026-2',CONVERT(date,'2026-08-01'),CONVERT(date,'2026-12-15'),0,0),
                (2026,0,N'QA-2026-COR',CONVERT(date,'2026-01-01'),CONVERT(date,'2026-12-31'),1,1)
            ) v([Year],Semester,Code,StartDate,PlannedEndDate,[Status],[Type])
        )
        INSERT dbo.Managements([Year],Semester,Code,Description,StartDate,PlannedEndDate,ActualClosedDate,[Status],Responsible,[Type],CreatedById,CreatedDate,FacultyId)
        SELECT [Year],Semester,Code,N'[QA-SINTETICO] Gestión para pruebas',StartDate,PlannedEndDate,
            CASE WHEN [Status]=2 THEN PlannedEndDate ELSE NULL END,[Status],N'Equipo QA-DATA-001',[Type],@AdminId,@Now,@FacultyId
        FROM SourceManagement s WHERE NOT EXISTS(SELECT 1 FROM dbo.Managements m WHERE m.Code=s.Code);

        CREATE TABLE #UnitOrder(rn int PRIMARY KEY,UnitId int NOT NULL,EquipmentId int NOT NULL,LaboratoryId int NOT NULL,CareerId int NULL,InventoryNumber nvarchar(50) NOT NULL);
        INSERT #UnitOrder(rn,UnitId,EquipmentId,LaboratoryId,CareerId,InventoryNumber)
        SELECT rn,Id,EquipmentId,LaboratoryId,CareerId,InventoryNumber FROM
        (
            SELECT ROW_NUMBER() OVER(ORDER BY InventoryNumber,Id) rn,Id,EquipmentId,LaboratoryId,CareerId,InventoryNumber
            FROM dbo.EquipmentUnits WHERE CurrentStatus<>99 AND LaboratoryId IS NOT NULL
        ) u WHERE rn>@StartRn AND rn<=@EndRn;

        CREATE TABLE #Cycles(CycleNo int PRIMARY KEY,ManagementId int NOT NULL,StartDate date NOT NULL);
        INSERT #Cycles VALUES
            (1,(SELECT Id FROM dbo.Managements WHERE Code=N'QA-2025-2'),CONVERT(date,'2025-08-15')),
            (2,(SELECT Id FROM dbo.Managements WHERE Code=N'QA-2026-1'),CONVERT(date,'2026-02-15')),
            (3,(SELECT Id FROM dbo.Managements WHERE Code=N'QA-2026-2'),CONVERT(date,'2026-08-15'));

        INSERT dbo.Verifications(EquipmentUnitId,ManagementId,[Date],Observations,PhysicalCondition,[Status],CreatedById,CreatedDate,HistoricalSourceKey,ImportBatchId,ResponsiblePersonId,ObservedEquipmentStatus)
        SELECT u.UnitId,c.ManagementId,DATEADD(day,u.rn%75,c.StartDate),
            CASE WHEN u.rn%5 IN(0,1) THEN N'[QA-SINTETICO] Se detectó una condición que requiere seguimiento.' ELSE N'[QA-SINTETICO] Verificación satisfactoria.' END,
            CASE WHEN u.rn%5=0 THEN 2 WHEN u.rn%5=1 THEN 3 WHEN u.rn%3=0 THEN 5 ELSE 4 END,
            CASE WHEN u.rn%5 IN(0,1) THEN 2 WHEN (u.rn+c.CycleNo)%9=0 THEN 3 ELSE 1 END,
            @AdminId,DATEADD(day,u.rn%75,c.StartDate),CONCAT(N'QA-V1-VER-',c.CycleNo,N'-',u.UnitId),@BatchId,resp.Id,
            CASE WHEN u.rn%5=0 THEN 6 WHEN u.rn%5=1 THEN 2 ELSE 0 END
        FROM #UnitOrder u CROSS JOIN #Cycles c
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-SOL-%' ORDER BY ABS(CHECKSUM(u.UnitId,c.CycleNo,p.Id))) resp
        WHERE NOT EXISTS(SELECT 1 FROM dbo.Verifications v WHERE v.HistoricalSourceKey=CONCAT(N'QA-V1-VER-',c.CycleNo,N'-',u.UnitId));

        INSERT dbo.VerificationFaults(VerificationId,Description,IsDeleted,CreatedById,CreatedDate)
        SELECT v.Id,N'Falla sintética reproducible QA',0,@AdminId,v.[Date]
        FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Verifications v ON v.HistoricalSourceKey=CONCAT(N'QA-V1-VER-',c.CycleNo,N'-',u.UnitId)
        WHERE u.rn%5 IN(0,1) AND NOT EXISTS(SELECT 1 FROM dbo.VerificationFaults f WHERE f.VerificationId=v.Id AND f.Description=N'Falla sintética reproducible QA');

        INSERT dbo.Requests(LaboratoryId,EquipmentId,EquipmentUnitId,ManagementId,RequestedById,Description,Priority,Observations,Suggestion,RequestDate,EstimatedRepairTime,[Status],ApprovedById,ApprovalDate,RejectionReason,CreatedById,CreatedDate,[Type],InvestmentCode,CostCenter,HistoricalSourceKey,ImportBatchId,LocationResolutionStatus,RequestedByPersonId)
        SELECT u.LaboratoryId,u.EquipmentId,u.UnitId,c.ManagementId,@AdminId,N'[QA-SINTETICO] Solicitud generada desde una verificación con observaciones.',
            (u.rn+c.CycleNo)%4,N'Caso reproducible QA',N'Revisar, diagnosticar y documentar la solución.',DATEADD(day,(u.rn%75)+1,c.StartDate),N'2 a 8 horas',
            CASE WHEN u.rn%5=0 THEN CASE c.CycleNo WHEN 1 THEN 5 WHEN 2 THEN 3 ELSE 2 END ELSE CASE (u.rn+c.CycleNo)%7 WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 WHEN 4 THEN 4 WHEN 5 THEN 5 ELSE 99 END END,
            CASE WHEN u.rn%5=0 OR (u.rn+c.CycleNo)%7 IN(2,3,5) THEN @AdminId ELSE NULL END,
            CASE WHEN u.rn%5=0 OR (u.rn+c.CycleNo)%7 IN(2,3,5) THEN DATEADD(day,(u.rn%75)+2,c.StartDate) ELSE NULL END,
            CASE WHEN u.rn%5=1 AND (u.rn+c.CycleNo)%7=4 THEN N'Rechazo sintético para validar el flujo.' ELSE NULL END,
            @AdminId,DATEADD(day,(u.rn%75)+1,c.StartDate),CASE WHEN u.rn%5=0 THEN CASE WHEN u.rn%2=0 THEN 3 ELSE 1 END ELSE 2 END,
            CASE WHEN u.rn%5=1 THEN CONCAT(N'QA-INV-',c.CycleNo,N'-',u.rn) ELSE NULL END,N'QA-COSTO',CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId),@BatchId,1,requester.Id
        FROM #UnitOrder u CROSS JOIN #Cycles c
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-SOL-%' ORDER BY ABS(CHECKSUM(u.UnitId,c.CycleNo,p.Id))) requester
        WHERE u.rn%5 IN(0,1) AND NOT EXISTS(SELECT 1 FROM dbo.Requests r WHERE r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId));

        INSERT dbo.RequestEquipmentUnits(RequestId,EquipmentUnitId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate)
        SELECT r.Id,u.UnitId,1,1,@AdminId,r.CreatedDate FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Requests r ON r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId)
        WHERE NOT EXISTS(SELECT 1 FROM dbo.RequestEquipmentUnits x WHERE x.RequestId=r.Id AND x.EquipmentUnitId=u.UnitId);
        INSERT dbo.RequestEquipmentUnits(RequestId,EquipmentUnitId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate)
        SELECT r.Id,u2.UnitId,0,1,@AdminId,r.CreatedDate FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN #UnitOrder u2 ON u2.rn=u.rn+1
        JOIN dbo.Requests r ON r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId)
        WHERE u.rn%10=0 AND NOT EXISTS(SELECT 1 FROM dbo.RequestEquipmentUnits x WHERE x.RequestId=r.Id AND x.EquipmentUnitId=u2.UnitId);

        INSERT dbo.Maintenances(EquipmentUnitId,MaintenanceType,ManagementId,ServiceType,InstitutionalCode,TechnicianId,RequestId,ScheduledDate,StartDate,EndDate,Description,[Status],CompletionPercentage,Step1_Cleaning,Step2_Calibration,Step3_Testing,Step4_FinalReview,EstimatedCost,ActualCost,Recommendations,SuggestedNextMaintenanceDate,SatisfactionLevel,Observations,CreatedById,CreatedDate,HistoricalSourceKey,ImportBatchId)
        SELECT u.UnitId,CASE (u.rn/5)%6 WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 4 WHEN 4 THEN 5 ELSE 99 END,c.ManagementId,
            CASE WHEN u.rn%10=0 THEN 1 ELSE 0 END,CONCAT(N'QA-MANT-',c.CycleNo,N'-',RIGHT(N'0000'+CONVERT(nvarchar(4),u.rn),4)),tech.Id,r.Id,
            DATEADD(day,(u.rn%75)+3,c.StartDate),
            CASE WHEN c.CycleNo<3 OR u.rn%20 IN(10,15) THEN DATEADD(day,(u.rn%75)+3,c.StartDate) ELSE NULL END,
            CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN DATEADD(hour,4,CONVERT(datetime2,DATEADD(day,(u.rn%75)+3,c.StartDate))) ELSE NULL END,
            N'[QA-SINTETICO] Diagnóstico, limpieza, calibración y prueba funcional.',
            CASE WHEN c.CycleNo=1 THEN 2 WHEN c.CycleNo=2 THEN CASE WHEN u.rn%10=0 THEN 99 ELSE 2 END ELSE CASE u.rn%20 WHEN 0 THEN 0 WHEN 5 THEN 3 WHEN 10 THEN 1 ELSE 2 END END,
            CASE WHEN c.CycleNo<3 AND NOT(c.CycleNo=2 AND u.rn%10=0) THEN 100 WHEN c.CycleNo=3 AND u.rn%20=10 THEN 50 WHEN c.CycleNo=3 AND u.rn%20=15 THEN 100 ELSE 0 END,
            CASE WHEN c.CycleNo<3 OR u.rn%20 IN(10,15) THEN 1 ELSE 0 END,
            CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN 1 ELSE 0 END,
            CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN 1 ELSE 0 END,
            CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN 1 ELSE 0 END,
            CONVERT(decimal(18,2),100+(u.rn%20)*10),CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN CONVERT(decimal(18,2),90+(u.rn%20)*12) ELSE NULL END,
            N'Continuar monitoreo preventivo.',DATEADD(month,6,DATEADD(day,(u.rn%75)+3,c.StartDate)),
            CASE WHEN c.CycleNo<3 OR u.rn%20=15 THEN ((u.rn+c.CycleNo)%5)+1 ELSE NULL END,N'Caso controlado QA',@AdminId,DATEADD(day,(u.rn%75)+3,c.StartDate),CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId),@BatchId
        FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Requests r ON r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId)
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE CASE WHEN u.rn%10=0 THEN N'QA-PROV-%' ELSE N'QA-TEC-%' END ORDER BY ABS(CHECKSUM(u.UnitId,c.CycleNo,p.Id))) tech
        WHERE u.rn%5=0 AND NOT EXISTS(SELECT 1 FROM dbo.Maintenances m WHERE m.HistoricalSourceKey=CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId));

        INSERT dbo.MaintenanceRequests(MaintenanceId,RequestId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate)
        SELECT m.Id,r.Id,1,1,@AdminId,m.CreatedDate FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Maintenances m ON m.HistoricalSourceKey=CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId)
        JOIN dbo.Requests r ON r.Id=m.RequestId
        WHERE NOT EXISTS(SELECT 1 FROM dbo.MaintenanceRequests x WHERE x.MaintenanceId=m.Id AND x.RequestId=r.Id);
        INSERT dbo.MaintenanceRequests(MaintenanceId,RequestId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate)
        SELECT m.Id,r2.Id,0,1,@AdminId,m.CreatedDate FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN #UnitOrder u2 ON u2.rn=u.rn+1
        JOIN dbo.Maintenances m ON m.HistoricalSourceKey=CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId)
        JOIN dbo.Requests r2 ON r2.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u2.UnitId)
        WHERE u.rn%10=0 AND NOT EXISTS(SELECT 1 FROM dbo.MaintenanceRequests x WHERE x.MaintenanceId=m.Id AND x.RequestId=r2.Id);

        INSERT dbo.MaintenanceParticipants(MaintenanceId,PersonId,Role,IsPrimary,IsActive,AssignedAt)
        SELECT m.Id,m.TechnicianId,1,1,1,m.CreatedDate FROM dbo.Maintenances m
        WHERE m.HistoricalSourceKey LIKE N'QA-V1-MAN-%' AND m.TechnicianId IS NOT NULL
          AND NOT EXISTS(SELECT 1 FROM dbo.MaintenanceParticipants p WHERE p.MaintenanceId=m.Id AND p.IsPrimary=1 AND p.IsActive=1);
        INSERT dbo.MaintenanceParticipants(MaintenanceId,PersonId,Role,IsPrimary,IsActive,AssignedAt)
        SELECT m.Id,assistant.Id,3,0,1,m.CreatedDate FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Maintenances m ON m.HistoricalSourceKey=CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId)
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-TEC-%' AND p.Id<>m.TechnicianId ORDER BY ABS(CHECKSUM(m.Id,p.Id))) assistant
        WHERE u.rn%15=0 AND NOT EXISTS(SELECT 1 FROM dbo.MaintenanceParticipants p WHERE p.MaintenanceId=m.Id AND p.PersonId=assistant.Id AND p.Role=3 AND p.IsActive=1);

        INSERT dbo.MaintenanceTasks(MaintenanceId,Description,IsCompleted,IsDeleted)
        SELECT m.Id,task.Description,CASE WHEN m.CompletionPercentage=100 OR task.n*25<=m.CompletionPercentage THEN 1 ELSE 0 END,0
        FROM dbo.Maintenances m CROSS APPLY(VALUES(1,N'Diagnóstico inicial QA'),(2,N'Limpieza o ajuste QA'),(3,N'Prueba final QA')) task(n,Description)
        WHERE m.HistoricalSourceKey LIKE N'QA-V1-MAN-%' AND NOT EXISTS(SELECT 1 FROM dbo.MaintenanceTasks t WHERE t.MaintenanceId=m.Id AND t.Description=task.Description);

        INSERT dbo.CostDetails(RequestId,MaintenanceId,Concept,Description,Quantity,UnitOfMeasure,UnitPrice,Category,Provider,InvoiceNumber,CreatedById,CreatedDate,ImportBatchId,CostDate,HistoricalSourceKey,ProviderPersonId)
        SELECT NULL,m.Id,cost.Concept,N'Costo sintético QA',cost.Quantity,cost.UnitOfMeasure,cost.UnitPrice,cost.Category,provider.Name,CONCAT(N'QA-FAC-',m.Id,N'-',cost.n),@AdminId,m.CreatedDate,@BatchId,CONVERT(date,m.CreatedDate),CONCAT(m.HistoricalSourceKey,N'-C',cost.n),provider.Id
        FROM dbo.Maintenances m CROSS APPLY(VALUES(1,N'Repuesto QA',CONVERT(decimal(10,2),1),N'UNIDAD',CONVERT(decimal(18,2),125),1),(2,N'Mano de obra QA',CONVERT(decimal(10,2),2),N'HORA',CONVERT(decimal(18,2),80),2)) cost(n,Concept,Quantity,UnitOfMeasure,UnitPrice,Category)
        CROSS APPLY(SELECT TOP(1) p.Id,e.Name FROM dbo.People p JOIN dbo.Externs e ON e.Id=p.Id WHERE p.ActorCode LIKE N'QA-PROV-%' ORDER BY ABS(CHECKSUM(m.Id,cost.n,p.Id))) provider
        WHERE m.HistoricalSourceKey LIKE N'QA-V1-MAN-%' AND NOT EXISTS(SELECT 1 FROM dbo.CostDetails d WHERE d.HistoricalSourceKey=CONCAT(m.HistoricalSourceKey,N'-C',cost.n));

        INSERT dbo.CostDetails(RequestId,MaintenanceId,Concept,Description,Quantity,UnitOfMeasure,UnitPrice,Category,Provider,CreatedById,CreatedDate,ImportBatchId,CostDate,HistoricalSourceKey,ProviderPersonId)
        SELECT r.Id,NULL,N'Cotización de adquisición QA',N'Costo de solicitud sin mantenimiento',1,N'UNIDAD',250,3,provider.Name,@AdminId,r.CreatedDate,@BatchId,CONVERT(date,r.CreatedDate),CONCAT(r.HistoricalSourceKey,N'-C1'),provider.Id
        FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Requests r ON r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId)
        CROSS APPLY(SELECT TOP(1) p.Id,e.Name FROM dbo.People p JOIN dbo.Externs e ON e.Id=p.Id WHERE p.ActorCode LIKE N'QA-PROV-%' ORDER BY ABS(CHECKSUM(r.Id,p.Id))) provider
        WHERE u.rn%10=1 AND NOT EXISTS(SELECT 1 FROM dbo.CostDetails d WHERE d.HistoricalSourceKey=CONCAT(r.HistoricalSourceKey,N'-C1'));

        WHILE 1=1
        BEGIN
            ;WITH DuplicateState AS
            (
                SELECT Id,ROW_NUMBER() OVER(PARTITION BY EquipmentUnitId,Reason ORDER BY Id) duplicateNumber
                FROM dbo.EquipmentStateHistories
                WHERE Reason LIKE N'[[]QA-V1-STATE-%'
            )
            DELETE TOP(100) FROM DuplicateState WHERE duplicateNumber>1;
            IF @@ROWCOUNT=0 BREAK;
        END;

        INSERT dbo.EquipmentStateHistories(EquipmentUnitId,[Status],StartDate,EndDate,Reason,CreatedById,CreatedDate)
        SELECT u.UnitId,CASE WHEN u.rn%5=0 THEN 1 WHEN u.rn%5=1 THEN 2 ELSE 0 END,DATEADD(day,u.rn%75,c.StartDate),DATEADD(day,(u.rn%75)+30,c.StartDate),
            CONCAT(N'[QA-V1-STATE-',c.CycleNo,N'-',u.UnitId,N'] Estado sintético auditado.'),@AdminId,DATEADD(day,u.rn%75,c.StartDate)
        FROM #UnitOrder u CROSS JOIN #Cycles c
        WHERE NOT EXISTS(SELECT 1 FROM dbo.EquipmentStateHistories h WHERE h.EquipmentUnitId=u.UnitId AND h.Reason=CONCAT(N'[QA-V1-STATE-',c.CycleNo,N'-',u.UnitId,N'] Estado sintético auditado.'));

        DECLARE @ActiveManagementId int=(SELECT Id FROM dbo.Managements WHERE Code=N'QA-2026-2');
        INSERT dbo.Departures(ManagementId,EquipmentUnitId,BorrowerId,[Type],DepartureDate,EstimatedReturnDate,ActualReturnDate,DepartureObservations,ReturnObservations,[Status],CreatedById,CreatedDate,Destination,HistoricalSourceKey,ImportBatchId,OriginLaboratoryId)
        SELECT @ActiveManagementId,u.UnitId,borrower.Id,CASE (u.rn/5)%5 WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 4 ELSE 5 END,
            DATEADD(day,u.rn%45,CONVERT(date,'2026-08-01')),DATEADD(day,(u.rn%45)+7,CONVERT(date,'2026-08-01')),
            CASE WHEN (u.rn/5)%4=1 THEN DATEADD(day,(u.rn%45)+6,CONVERT(date,'2026-08-01')) ELSE NULL END,
            N'[QA-SINTETICO] Salida controlada.',CASE WHEN (u.rn/5)%4=1 THEN N'Devolución conforme QA' ELSE NULL END,
            CASE (u.rn/5)%4 WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 99 END,@AdminId,@Now,N'Destino sintético QA',CONCAT(N'QA-V1-DEP-',u.UnitId),@BatchId,u.LaboratoryId
        FROM #UnitOrder u CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-SOL-%' ORDER BY ABS(CHECKSUM(u.UnitId,p.Id))) borrower
        WHERE u.rn%5=0 AND NOT EXISTS(SELECT 1 FROM dbo.Departures d WHERE d.HistoricalSourceKey=CONCAT(N'QA-V1-DEP-',u.UnitId));

        INSERT dbo.DepartureItems(DepartureId,EquipmentUnitId,ArticleId,ProductName,Quantity,UnitOfMeasure,ReturnedQuantity,Observations,IsRemoved,CreatedById,CreatedDate)
        SELECT d.Id,u.UnitId,NULL,CONCAT(N'Unidad ',u.InventoryNumber),1,N'UNIDAD',CASE WHEN d.Status=1 THEN 1 ELSE 0 END,N'Detalle patrimonial QA',0,@AdminId,d.CreatedDate
        FROM #UnitOrder u JOIN dbo.Departures d ON d.HistoricalSourceKey=CONCAT(N'QA-V1-DEP-',u.UnitId)
        WHERE NOT EXISTS(SELECT 1 FROM dbo.DepartureItems i WHERE i.DepartureId=d.Id AND i.EquipmentUnitId=u.UnitId);
        INSERT dbo.DepartureItems(DepartureId,EquipmentUnitId,ArticleId,ProductName,Quantity,UnitOfMeasure,ReturnedQuantity,Observations,IsRemoved,CreatedById,CreatedDate)
        SELECT d.Id,NULL,a.Id,a.Name,2,a.UnitOfMeasure,CASE WHEN d.Status=1 THEN 2 ELSE 0 END,N'Detalle de consumible QA',0,@AdminId,d.CreatedDate
        FROM #UnitOrder u JOIN dbo.Departures d ON d.HistoricalSourceKey=CONCAT(N'QA-V1-DEP-',u.UnitId)
        CROSS APPLY(SELECT TOP(1) a.Id,a.Name,a.UnitOfMeasure FROM dbo.Articles a ORDER BY ABS(CHECKSUM(u.UnitId,a.Id))) a
        WHERE NOT EXISTS(SELECT 1 FROM dbo.DepartureItems i WHERE i.DepartureId=d.Id AND i.ArticleId=a.Id);

        INSERT dbo.MaintenancePlans(LaboratorySnapshot,BlockSnapshot,EquipmentUnitId,Service,ServiceType,CreatedById,CreatedDate,EstimatedTime,ActualTime,AssignedTechnicianId,ProviderSnapshot,LaboratoryId,HistoricalSourceKey,ImportBatchId,MaintenanceType,ManagementId,PlanCode,PlannedDate,ResponsiblePersonId,[Status])
        SELECT l.Name,l.[Block],u.UnitId,N'Mantenimiento preventivo QA',CASE WHEN u.rn%4=0 THEN 1 ELSE 0 END,@AdminId,@Now,
            4,CASE WHEN u.rn%4=0 THEN NULL ELSE 3.5 END,NULL,CASE WHEN u.rn%4=0 THEN N'Proveedor QA' ELSE N'Equipo interno QA' END,u.LaboratoryId,
            CONCAT(N'QA-V1-MPLAN-',u.UnitId),@BatchId,CASE u.rn%5 WHEN 0 THEN 2 WHEN 1 THEN 3 WHEN 2 THEN 4 WHEN 3 THEN 5 ELSE 1 END,@ActiveManagementId,
            CONCAT(N'QA-PLAN-',RIGHT(N'0000'+CONVERT(nvarchar(4),u.rn),4)),DATEADD(day,u.rn%120,CONVERT(date,'2026-09-01')),responsible.Id,
            CASE u.rn%4 WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 3 END
        FROM #UnitOrder u JOIN dbo.Laboratories l ON l.Id=u.LaboratoryId
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-TEC-%' ORDER BY ABS(CHECKSUM(u.UnitId,p.Id))) responsible
        WHERE u.rn%2=0 AND NOT EXISTS(SELECT 1 FROM dbo.MaintenancePlans p WHERE p.HistoricalSourceKey=CONCAT(N'QA-V1-MPLAN-',u.UnitId));

        INSERT dbo.ManagementPlans(ManagementId,MaintenanceId,EquipmentUnitId,VerificationId,RequestId,DepartureId,KardexHistoryId,CurrentPhase,CurrentState,Responsible,PlannedWeek,ExecutedWeek,DocumentReference,Notes,PlannedDate,PlanStatus,IsDraft,CreatedById,CreatedDate,ImportBatchId,ResponsiblePersonId)
        SELECT c.ManagementId,m.Id,u.UnitId,v.Id,r.Id,CASE WHEN c.CycleNo=3 THEN d.Id ELSE NULL END,h.Id,
            CASE WHEN c.CycleNo=3 AND d.Id IS NOT NULL THEN 4 WHEN m.Id IS NOT NULL THEN 3 WHEN r.Id IS NOT NULL THEN 2 ELSE 1 END,
            CASE WHEN c.CycleNo=3 AND d.Id IS NOT NULL THEN CASE WHEN d.Status=1 THEN 7 ELSE 6 END WHEN m.Id IS NOT NULL THEN CASE WHEN m.Status=2 THEN 9 WHEN m.Status=1 THEN 5 ELSE 4 END WHEN r.Id IS NOT NULL THEN 4 ELSE 2 END,
            N'Equipo QA-DATA-001',((u.rn+c.CycleNo-2)%8)+1,CASE WHEN m.Status=2 THEN ((u.rn+c.CycleNo-2)%8)+1 ELSE NULL END,
            CONCAT(N'QA-V1-DOC-',c.CycleNo,N'-',u.UnitId),N'[QA-SINTETICO] Flujo completo reproducible.',DATEADD(day,u.rn%75,c.StartDate),
            CASE WHEN m.Status=2 OR r.Status=5 OR (r.Id IS NULL AND v.Status IN(1,3)) THEN 2 WHEN m.Status=1 OR r.Status IN(1,2,3) THEN 1 WHEN d.Status=2 THEN 3 ELSE 0 END,
            CASE WHEN (u.rn+c.CycleNo)%17=0 THEN 1 ELSE 0 END,@AdminId,@Now,@BatchId,responsible.Id
        FROM #UnitOrder u CROSS JOIN #Cycles c
        JOIN dbo.Verifications v ON v.HistoricalSourceKey=CONCAT(N'QA-V1-VER-',c.CycleNo,N'-',u.UnitId)
        LEFT JOIN dbo.Requests r ON r.HistoricalSourceKey=CONCAT(N'QA-V1-REQ-',c.CycleNo,N'-',u.UnitId)
        LEFT JOIN dbo.Maintenances m ON m.HistoricalSourceKey=CONCAT(N'QA-V1-MAN-',c.CycleNo,N'-',u.UnitId)
        LEFT JOIN dbo.Departures d ON c.CycleNo=3 AND d.HistoricalSourceKey=CONCAT(N'QA-V1-DEP-',u.UnitId)
        OUTER APPLY(SELECT TOP(1) sh.Id FROM dbo.EquipmentStateHistories sh WHERE sh.EquipmentUnitId=u.UnitId AND sh.Reason=CONCAT(N'[QA-V1-STATE-',c.CycleNo,N'-',u.UnitId,N'] Estado sintético auditado.') ORDER BY sh.Id) h
        CROSS APPLY(SELECT TOP(1) p.Id FROM dbo.People p WHERE p.ActorCode LIKE N'QA-TEC-%' ORDER BY ABS(CHECKSUM(u.UnitId,c.CycleNo,p.Id))) responsible
        WHERE NOT EXISTS(SELECT 1 FROM dbo.ManagementPlans p WHERE p.ManagementId=c.ManagementId AND p.EquipmentUnitId=u.UnitId);

        UPDATE u SET ManagementId=@ActiveManagementId
        FROM dbo.EquipmentUnits u JOIN #UnitOrder q ON q.UnitId=u.Id
        WHERE u.ManagementId IS NULL OR u.ManagementId<>@ActiveManagementId;

        COMMIT TRANSACTION;
        """;
}
