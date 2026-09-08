BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    DROP INDEX [IX_VerificationCheckResults_VerificationId] ON [VerificationCheckResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    DROP INDEX [IX_ManagementPlans_ManagementId] ON [ManagementPlans];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Verifications] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Verifications] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Requests]') AND [c].[name] = N'LaboratoryId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Requests] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Requests] ALTER COLUMN [LaboratoryId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Requests] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Requests] ADD [LocationResolutionStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Requests] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [People] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [People] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Managements] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [ManagementPlans] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [ManagementPlans] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Maintenances] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Maintenances] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [HistoricalVerificationQuarantines] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [LocationResolutionStatus] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Equipments] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Equipments] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [CostDetails] ADD [ImportBatchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE TABLE [ImportBatches] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [SourceType] nvarchar(30) NOT NULL,
        [SourceName] nvarchar(260) NOT NULL,
        [SourceSha256] nvarchar(64) NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(2000) NULL,
        CONSTRAINT [PK_ImportBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ImportBatches_Status] CHECK ([Status] IN (0, 1, 2, 99))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE TABLE [MaintenanceParticipants] (
        [Id] bigint NOT NULL IDENTITY,
        [MaintenanceId] int NOT NULL,
        [PersonId] int NOT NULL,
        [Role] int NOT NULL,
        [IsPrimary] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [UnassignedAt] datetime2 NULL,
        CONSTRAINT [PK_MaintenanceParticipants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaintenanceParticipants_Dates] CHECK ([UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]),
        CONSTRAINT [CK_MaintenanceParticipants_PrimaryRole] CHECK ([IsPrimary] = 0 OR [Role] = 1),
        CONSTRAINT [CK_MaintenanceParticipants_Role] CHECK ([Role] IN (1, 2, 3, 4)),
        CONSTRAINT [FK_MaintenanceParticipants_Maintenances_MaintenanceId] FOREIGN KEY ([MaintenanceId]) REFERENCES [Maintenances] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MaintenanceParticipants_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE TABLE [PersonAliases] (
        [Id] bigint NOT NULL IDENTITY,
        [PersonId] int NOT NULL,
        [Alias] nvarchar(300) NOT NULL,
        [NormalizedAlias] nvarchar(300) NOT NULL,
        [IsPreferred] bit NOT NULL,
        [Source] nvarchar(100) NULL,
        CONSTRAINT [PK_PersonAliases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PersonAliases_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE TABLE [PersonRoleAssignments] (
        [Id] bigint NOT NULL IDENTITY,
        [PersonId] int NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NULL,
        CONSTRAINT [PK_PersonRoleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PersonRoleAssignments_Dates] CHECK ([ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]),
        CONSTRAINT [CK_PersonRoleAssignments_Role] CHECK ([Role] IN (1, 2, 3, 4, 99)),
        CONSTRAINT [FK_PersonRoleAssignments_People_PersonId] FOREIGN KEY ([PersonId]) REFERENCES [People] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE TABLE [DataQualityIssues] (
        [Id] bigint NOT NULL IDENTITY,
        [ImportBatchId] int NOT NULL,
        [IssueCode] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityKey] nvarchar(200) NULL,
        [FieldName] nvarchar(100) NULL,
        [OriginalValue] nvarchar(max) NULL,
        [NormalizedValue] nvarchar(max) NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Severity] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ResolvedDate] datetime2 NULL,
        CONSTRAINT [PK_DataQualityIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DataQualityIssues_Severity] CHECK ([Severity] BETWEEN 0 AND 3),
        CONSTRAINT [CK_DataQualityIssues_Status] CHECK ([Status] BETWEEN 0 AND 3),
        CONSTRAINT [FK_DataQualityIssues_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchCode nvarchar(100) = N'XLSX-20260820-PLANTILLA-ORIGINAL';
    DECLARE @BatchId int;
    DECLARE @PendingLaboratoryId int;
    DECLARE @RemovedCheckResults int;
    DECLARE @RemovedTasks int;

    IF NOT EXISTS (SELECT 1 FROM dbo.ImportBatches WHERE Code = @BatchCode)
    BEGIN
        INSERT INTO dbo.ImportBatches
            (Code, SourceType, SourceName, SourceSha256, ImportedAt, Status, Notes)
        VALUES
            (@BatchCode, N'Excel', N'Plantilla_Original.xlsx',
             N'FAEAF00640BD4AF05D78C22B3B5FB7E105260BF8AF8A4B5E8FC2FAD4A78484A4',
             COALESCE((SELECT MIN(CreatedDate) FROM dbo.Verifications), SYSUTCDATETIME()),
             2,
             N'Lote histórico oficializado desde la plantilla original. Las excepciones y correcciones quedan registradas en DataQualityIssues.');
    END;

    SELECT @BatchId = Id FROM dbo.ImportBatches WHERE Code = @BatchCode;
    SELECT @PendingLaboratoryId = Id FROM dbo.Laboratories WHERE Code = N'PENDIENTE';

    UPDATE dbo.Equipments SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;
    UPDATE dbo.EquipmentUnits SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;
    UPDATE dbo.People SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;
    UPDATE dbo.Verifications SET ImportBatchId = @BatchId WHERE HistoricalSourceKey IS NOT NULL AND ImportBatchId IS NULL;
    UPDATE dbo.Requests SET ImportBatchId = @BatchId WHERE HistoricalSourceKey IS NOT NULL AND ImportBatchId IS NULL;
    UPDATE dbo.Maintenances SET ImportBatchId = @BatchId WHERE HistoricalSourceKey IS NOT NULL AND ImportBatchId IS NULL;
    UPDATE dbo.ManagementPlans SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;
    UPDATE dbo.CostDetails SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;
    UPDATE dbo.HistoricalVerificationQuarantines SET ImportBatchId = @BatchId WHERE ImportBatchId IS NULL;

    INSERT INTO dbo.DataQualityIssues
        (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
         NormalizedValue, Description, Severity, Status, CreatedDate, ResolvedDate)
    SELECT @BatchId, N'INVALID_SCHEDULED_DATE', N'Maintenance', CONVERT(nvarchar(200), m.Id),
           N'ScheduledDate', CONVERT(nvarchar(33), m.ScheduledDate, 126), NULL,
           N'La fecha programada era posterior a la fecha real de finalización. Se anuló sin reinterpretar la fuente.',
           1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM dbo.Maintenances m
    WHERE m.ScheduledDate IS NOT NULL AND m.EndDate IS NOT NULL AND m.ScheduledDate > m.EndDate;

    UPDATE dbo.Maintenances
    SET ScheduledDate = NULL
    WHERE ScheduledDate IS NOT NULL AND EndDate IS NOT NULL AND ScheduledDate > EndDate;

    INSERT INTO dbo.DataQualityIssues
        (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
         NormalizedValue, Description, Severity, Status, CreatedDate)
    SELECT @BatchId, N'UNRESOLVED_LOCATION', N'EquipmentUnit', CONVERT(nvarchar(200), u.Id),
           N'LaboratoryId', N'PENDIENTE', NULL,
           N'La plantilla no identifica un laboratorio físico. La unidad permanece sin ubicación hasta validación humana.',
           1, 0, SYSUTCDATETIME()
    FROM dbo.EquipmentUnits u
    WHERE u.LaboratoryId = @PendingLaboratoryId OR u.LaboratoryId IS NULL;

    INSERT INTO dbo.DataQualityIssues
        (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
         NormalizedValue, Description, Severity, Status, CreatedDate)
    SELECT @BatchId, N'UNRESOLVED_LOCATION', N'Request', CONVERT(nvarchar(200), r.Id),
           N'LaboratoryId', N'PENDIENTE', NULL,
           N'La solicitud histórica conserva la ausencia de ubicación; no se inventó un laboratorio.',
           1, 0, SYSUTCDATETIME()
    FROM dbo.Requests r
    WHERE r.LaboratoryId = @PendingLaboratoryId OR r.LaboratoryId IS NULL;

    UPDATE dbo.EquipmentUnits
    SET LaboratoryId = NULL, LocationResolutionStatus = 0
    WHERE LaboratoryId = @PendingLaboratoryId OR LaboratoryId IS NULL;
    UPDATE dbo.EquipmentUnits
    SET LocationResolutionStatus = 1
    WHERE LaboratoryId IS NOT NULL;

    UPDATE dbo.Requests
    SET LaboratoryId = NULL, LocationResolutionStatus = 0
    WHERE LaboratoryId = @PendingLaboratoryId OR LaboratoryId IS NULL;
    UPDATE dbo.Requests
    SET LocationResolutionStatus = 1
    WHERE LaboratoryId IS NOT NULL;

    IF @PendingLaboratoryId IS NOT NULL
    BEGIN
        UPDATE dbo.Laboratories
        SET Status = 2,
            Description = CONCAT(COALESCE(NULLIF(Description, N''), N''),
                CASE WHEN NULLIF(Description, N'') IS NULL THEN N'' ELSE N' ' END,
                N'Registro técnico retirado: una ubicación desconocida se representa con FK nula y estado pendiente.'),
            LastModifiedDate = SYSUTCDATETIME()
        WHERE Id = @PendingLaboratoryId;
    END;

    SELECT @RemovedCheckResults = COUNT(*)
    FROM dbo.VerificationCheckResults result
    INNER JOIN dbo.Verifications verification ON verification.Id = result.VerificationId
    WHERE verification.HistoricalSourceKey IS NOT NULL;

    DELETE result
    FROM dbo.VerificationCheckResults result
    INNER JOIN dbo.Verifications verification ON verification.Id = result.VerificationId
    WHERE verification.HistoricalSourceKey IS NOT NULL;

    IF @RemovedCheckResults > 0
    BEGIN
        INSERT INTO dbo.DataQualityIssues
            (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
             NormalizedValue, Description, Severity, Status, CreatedDate, ResolvedDate)
        VALUES
            (@BatchId, N'INVENTED_CHECKLIST_RESULTS_REMOVED', N'VerificationCheckResult', N'LOTE', N'Result',
             CONVERT(nvarchar(30), @RemovedCheckResults), N'0',
             N'Se retiraron resultados de checklist generados automáticamente porque el Excel no contenía ese detalle.',
             1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    SELECT @RemovedTasks = COUNT(*)
    FROM dbo.MaintenanceTasks task
    INNER JOIN dbo.Maintenances maintenance ON maintenance.Id = task.MaintenanceId
    WHERE maintenance.HistoricalSourceKey IS NOT NULL;

    DELETE task
    FROM dbo.MaintenanceTasks task
    INNER JOIN dbo.Maintenances maintenance ON maintenance.Id = task.MaintenanceId
    WHERE maintenance.HistoricalSourceKey IS NOT NULL;

    IF @RemovedTasks > 0
    BEGIN
        INSERT INTO dbo.DataQualityIssues
            (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
             NormalizedValue, Description, Severity, Status, CreatedDate, ResolvedDate)
        VALUES
            (@BatchId, N'INVENTED_MAINTENANCE_TASKS_REMOVED', N'MaintenanceTask', N'LOTE', N'Description',
             CONVERT(nvarchar(30), @RemovedTasks), N'0',
             N'Se retiraron tareas derivadas o copiadas por el importador; el mantenimiento raíz y su descripción se preservan.',
             1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    INSERT INTO dbo.MaintenanceParticipants
        (MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt, UnassignedAt)
    SELECT m.Id, m.TechnicianId, 1, 1, 1, COALESCE(m.CreatedDate, SYSUTCDATETIME()), NULL
    FROM dbo.Maintenances m
    WHERE m.TechnicianId IS NOT NULL
      AND NOT EXISTS
          (SELECT 1 FROM dbo.MaintenanceParticipants participant
           WHERE participant.MaintenanceId = m.Id AND participant.IsPrimary = 1 AND participant.IsActive = 1);

    INSERT INTO dbo.PersonAliases (PersonId, Alias, NormalizedAlias, IsPreferred, Source)
    SELECT source.PersonId, source.Alias,
           UPPER(LTRIM(RTRIM(REPLACE(REPLACE(source.Alias, CHAR(13), N' '), CHAR(10), N' ')))),
           1, N'Plantilla_Original.xlsx'
    FROM
    (
        SELECT p.Id AS PersonId, COALESCE(NULLIF(i.Name, N''), NULLIF(e.Name, N'')) AS Alias
        FROM dbo.People p
        LEFT JOIN dbo.Interns i ON i.Id = p.Id
        LEFT JOIN dbo.Externs e ON e.Id = p.Id
    ) source
    WHERE source.Alias IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.PersonAliases alias WHERE alias.PersonId = source.PersonId);

    INSERT INTO dbo.PersonRoleAssignments (PersonId, Role, IsActive, ValidFrom, ValidTo)
    SELECT p.Id,
           CASE p.Category WHEN 1 THEN 1 WHEN 5 THEN 3 ELSE 99 END,
           1, COALESCE(p.CreatedDate, SYSUTCDATETIME()), NULL
    FROM dbo.People p
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PersonRoleAssignments role WHERE role.PersonId = p.Id AND role.IsActive = 1);

    INSERT INTO dbo.DataQualityIssues
        (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
         NormalizedValue, Description, Severity, Status, CreatedDate)
    SELECT @BatchId, N'POSSIBLE_PERSON_DUPLICATE', N'Person',
           CONCAT(MIN(alias.PersonId), N'|', MAX(alias.PersonId)), N'Alias',
           STRING_AGG(alias.Alias, N' | '), normalized.MatchKey,
           N'Los nombres se parecen tras quitar espacios y puntuación. Requieren validación humana antes de fusionarse.',
           1, 0, SYSUTCDATETIME()
    FROM dbo.PersonAliases alias
    CROSS APPLY
    (
        SELECT REPLACE(REPLACE(REPLACE(alias.NormalizedAlias, N' ', N''), N'.', N''), N'-', N'') AS MatchKey
    ) normalized
    GROUP BY normalized.MatchKey
    HAVING COUNT(DISTINCT alias.PersonId) > 1;

    INSERT INTO dbo.DataQualityIssues
        (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
         NormalizedValue, Description, Severity, Status, CreatedDate)
    SELECT @BatchId, N'MISSING_HISTORICAL_APPROVAL_EVIDENCE', N'Request', CONVERT(nvarchar(200), r.Id),
           N'ApprovedById|ApprovalDate', N'Estado aprobado sin evidencia de aprobador/fecha', NULL,
           N'Excepción histórica conservada. No se inventaron identidad ni fecha de aprobación.',
           1, 2, SYSUTCDATETIME()
    FROM dbo.Requests r
    WHERE r.Status = 3 AND (r.ApprovedById IS NULL OR r.ApprovalDate IS NULL);

    IF EXISTS (SELECT 1 FROM dbo.HistoricalVerificationQuarantines)
    BEGIN
        INSERT INTO dbo.DataQualityIssues
            (ImportBatchId, IssueCode, EntityName, EntityKey, FieldName, OriginalValue,
             NormalizedValue, Description, Severity, Status, CreatedDate)
        VALUES
            (@BatchId, N'AMBIGUOUS_VERIFICATIONS_QUARANTINED', N'HistoricalVerificationQuarantine', N'LOTE',
             N'EquipmentUnitId',
             CONVERT(nvarchar(30), (SELECT COUNT(*) FROM dbo.HistoricalVerificationQuarantines)), NULL,
             N'Filas L-6 ambiguas preservadas fuera del flujo operativo hasta que exista evidencia para enlazarlas.',
             1, 2, SYSUTCDATETIME());
    END;

    UPDATE dbo.Managements
    SET Description = CONCAT(COALESCE(NULLIF(Description, N''), N''),
        CASE WHEN NULLIF(Description, N'') IS NULL THEN N'' ELSE N' ' END,
        N'Gestión técnica usada para registrar el lote histórico; no demuestra fecha de adquisición.'),
        LastModifiedDate = SYSUTCDATETIME()
    WHERE Code = N'2026-2'
      AND (Description IS NULL OR Description NOT LIKE N'%lote histórico%');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_Verifications_ImportBatchId] ON [Verifications] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VerificationCheckResults_VerificationId_CheckItemId] ON [VerificationCheckResults] ([VerificationId], [CheckItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [VerificationCheckResults] ADD CONSTRAINT [CK_VerificationCheckResults_Result] CHECK ([Result] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_Requests_ImportBatchId] ON [Requests] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Requests] ADD CONSTRAINT [CK_Requests_LocationResolution] CHECK (([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_People_ImportBatchId] ON [People] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Managements_Code] ON [Managements] ([Code]) WHERE [Status] <> 99');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Managements_Status] ON [Managements] ([Status]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Managements] ADD CONSTRAINT [CK_Managements_Semester] CHECK ([Semester] IN (0, 1, 2))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Managements] ADD CONSTRAINT [CK_Managements_Status] CHECK ([Status] IN (0, 1, 2, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Managements] ADD CONSTRAINT [CK_Managements_Type] CHECK ([Type] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Managements] ADD CONSTRAINT [CK_Managements_Year] CHECK ([Year] BETWEEN 2000 AND 2100)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_ManagementPlans_ImportBatchId] ON [ManagementPlans] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ManagementPlans_ManagementId_EquipmentUnitId] ON [ManagementPlans] ([ManagementId], [EquipmentUnitId]) WHERE [EquipmentUnitId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [ManagementPlans] ADD CONSTRAINT [CK_ManagementPlans_Phase] CHECK ([CurrentPhase] BETWEEN 1 AND 6)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [ManagementPlans] ADD CONSTRAINT [CK_ManagementPlans_State] CHECK ([CurrentState] BETWEEN 1 AND 9)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [ManagementPlans] ADD CONSTRAINT [CK_ManagementPlans_Status] CHECK ([PlanStatus] BETWEEN 0 AND 3)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [ManagementPlans] ADD CONSTRAINT [CK_ManagementPlans_Weeks] CHECK (([PlannedWeek] IS NULL OR [PlannedWeek] BETWEEN 1 AND 8) AND ([ExecutedWeek] IS NULL OR [ExecutedWeek] BETWEEN 1 AND 8))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_Maintenances_ImportBatchId] ON [Maintenances] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_CompletionPercentage] CHECK ([CompletionPercentage] BETWEEN 0 AND 100)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_ExecutionDates] CHECK ([StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate] >= [StartDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_NonNegativeCosts] CHECK (([EstimatedCost] IS NULL OR [EstimatedCost] >= 0) AND ([ActualCost] IS NULL OR [ActualCost] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_HistoricalVerificationQuarantines_ImportBatchId] ON [HistoricalVerificationQuarantines] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_EquipmentUnits_ImportBatchId] ON [EquipmentUnits] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [CK_EquipmentUnits_LocationResolution] CHECK (([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_Equipments_ImportBatchId] ON [Equipments] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_CostDetails_ImportBatchId] ON [CostDetails] ([ImportBatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [CostDetails] ADD CONSTRAINT [CK_CostDetails_ExactlyOneParent] CHECK (CASE WHEN [RequestId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [MaintenanceId] IS NULL THEN 0 ELSE 1 END = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'ALTER TABLE [CostDetails] ADD CONSTRAINT [CK_CostDetails_PositiveValues] CHECK ([Quantity] > 0 AND [UnitPrice] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_DataQualityIssues_ImportBatchId_IssueCode_EntityName] ON [DataQualityIssues] ([ImportBatchId], [IssueCode], [EntityName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ImportBatches_Code] ON [ImportBatches] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MaintenanceParticipants_MaintenanceId] ON [MaintenanceParticipants] ([MaintenanceId]) WHERE [IsPrimary] = 1 AND [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MaintenanceParticipants_MaintenanceId_PersonId_Role] ON [MaintenanceParticipants] ([MaintenanceId], [PersonId], [Role]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE INDEX [IX_MaintenanceParticipants_PersonId] ON [MaintenanceParticipants] ([PersonId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PersonAliases_PersonId] ON [PersonAliases] ([PersonId]) WHERE [IsPreferred] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PersonAliases_PersonId_NormalizedAlias] ON [PersonAliases] ([PersonId], [NormalizedAlias]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PersonRoleAssignments_PersonId_Role] ON [PersonRoleAssignments] ([PersonId], [Role]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [CostDetails] ADD CONSTRAINT [FK_CostDetails_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [FK_EquipmentUnits_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [HistoricalVerificationQuarantines] ADD CONSTRAINT [FK_HistoricalVerificationQuarantines_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Maintenances] ADD CONSTRAINT [FK_Maintenances_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [ManagementPlans] ADD CONSTRAINT [FK_ManagementPlans_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [People] ADD CONSTRAINT [FK_People_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Requests] ADD CONSTRAINT [FK_Requests_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    ALTER TABLE [Verifications] ADD CONSTRAINT [FK_Verifications_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822235807_NormalizeOfficialDataModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822235807_NormalizeOfficialDataModel', N'9.0.19');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Verifications] ADD CONSTRAINT [CK_Verifications_PhysicalCondition] CHECK ([PhysicalCondition] BETWEEN 1 AND 5)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Verifications] ADD CONSTRAINT [CK_Verifications_Status] CHECK ([Status] IN (0, 1, 2, 3, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (1, 2, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Requests] ADD CONSTRAINT [CK_Requests_Priority] CHECK ([Priority] BETWEEN 0 AND 3)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Requests] ADD CONSTRAINT [CK_Requests_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Requests] ADD CONSTRAINT [CK_Requests_Type] CHECK ([Type] IN (1, 2, 3))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [People] ADD CONSTRAINT [CK_People_Category] CHECK ([Category] IN (1, 2, 3, 4, 5, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [People] ADD CONSTRAINT [CK_People_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_Satisfaction] CHECK ([SatisfactionLevel] IS NULL OR [SatisfactionLevel] BETWEEN 1 AND 5)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_ServiceType] CHECK ([ServiceType] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_Status] CHECK ([Status] IN (0, 1, 2, 3, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Maintenances] ADD CONSTRAINT [CK_Maintenances_Type] CHECK ([MaintenanceType] IN (1, 2, 3, 4, 5, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [MaintenancePlans] ADD CONSTRAINT [CK_MaintenancePlans_ServiceType] CHECK ([ServiceType] IN (0, 1))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [MaintenancePlans] ADD CONSTRAINT [CK_MaintenancePlans_Times] CHECK (([EstimatedTime] IS NULL OR [EstimatedTime] >= 0) AND ([ActualTime] IS NULL OR [ActualTime] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Laboratories] ADD CONSTRAINT [CK_Laboratories_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Interns] ADD CONSTRAINT [CK_Interns_Status] CHECK ([InternStatus] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Faculties] ADD CONSTRAINT [CK_Faculties_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Externs] ADD CONSTRAINT [CK_Externs_Status] CHECK ([ExternStatus] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [CK_EquipmentUnits_AcquisitionValue] CHECK ([AcquisitionValue] IS NULL OR [AcquisitionValue] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [CK_EquipmentUnits_CurrentStatus] CHECK ([CurrentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentUnits] ADD CONSTRAINT [CK_EquipmentUnits_PhysicalCondition] CHECK ([PhysicalCondition] IS NULL OR [PhysicalCondition] BETWEEN 1 AND 5)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [CK_EquipmentStateHistories_Dates] CHECK ([EndDate] IS NULL OR [EndDate] >= [StartDate])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [EquipmentStateHistories] ADD CONSTRAINT [CK_EquipmentStateHistories_Status] CHECK ([Status] IN (0, 1, 2, 3, 4, 5, 6, 10, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_Category] CHECK ([Category] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_TypeClassification] CHECK ([TypeClassification] BETWEEN 0 AND 15)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_UsefulLife] CHECK ([UsefulLifeYears] IS NULL OR [UsefulLifeYears] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_UtensilType] CHECK ([UtensilType] BETWEEN 0 AND 11)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Departures] ADD CONSTRAINT [CK_Departures_Status] CHECK ([Status] IN (0, 1, 2, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Departures] ADD CONSTRAINT [CK_Departures_Type] CHECK ([Type] IN (1, 2, 3, 4, 5))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Countries] ADD CONSTRAINT [CK_Countries_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [CostDetails] ADD CONSTRAINT [CK_CostDetails_Category] CHECK ([Category] IN (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 99))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Cities] ADD CONSTRAINT [CK_Cities_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    EXEC(N'ALTER TABLE [Careers] ADD CONSTRAINT [CK_Careers_Status] CHECK ([Status] BETWEEN 0 AND 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260823000130_HardenOfficialDataIntegrity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260823000130_HardenOfficialDataIntegrity', N'9.0.19');
END;

COMMIT;
GO
