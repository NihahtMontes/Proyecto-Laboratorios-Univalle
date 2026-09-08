using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class NormalizeOfficialDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationCheckResults_VerificationId",
                table: "VerificationCheckResults");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ManagementId",
                table: "ManagementPlans");

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "Verifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Verifications",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationResolutionStatus",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Requests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "People",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "People",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Managements",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ManagementPlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "Maintenances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Maintenances",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "HistoricalVerificationQuarantines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationResolutionStatus",
                table: "EquipmentUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EquipmentUnits",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Equipments",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "CostDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    SourceSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.CheckConstraint("CK_ImportBatches_Status", "[Status] IN (0, 1, 2, 99)");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceParticipants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnassignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceParticipants", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceParticipants_Dates", "[UnassignedAt] IS NULL OR [UnassignedAt] >= [AssignedAt]");
                    table.CheckConstraint("CK_MaintenanceParticipants_PrimaryRole", "[IsPrimary] = 0 OR [Role] = 1");
                    table.CheckConstraint("CK_MaintenanceParticipants_Role", "[Role] IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_MaintenanceParticipants_Maintenances_MaintenanceId",
                        column: x => x.MaintenanceId,
                        principalTable: "Maintenances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceParticipants_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NormalizedAlias = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonAliases_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoleAssignments", x => x.Id);
                    table.CheckConstraint("CK_PersonRoleAssignments_Dates", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.CheckConstraint("CK_PersonRoleAssignments_Role", "[Role] IN (1, 2, 3, 4, 99)");
                    table.ForeignKey(
                        name: "FK_PersonRoleAssignments_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataQualityIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    IssueCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataQualityIssues", x => x.Id);
                    table.CheckConstraint("CK_DataQualityIssues_Severity", "[Severity] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_DataQualityIssues_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_DataQualityIssues_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    });

            migrationBuilder.Sql(
                """
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
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Verifications_ImportBatchId",
                table: "Verifications",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCheckResults_VerificationId_CheckItemId",
                table: "VerificationCheckResults",
                columns: new[] { "VerificationId", "CheckItemId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VerificationCheckResults_Result",
                table: "VerificationCheckResults",
                sql: "[Result] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImportBatchId",
                table: "Requests",
                column: "ImportBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_LocationResolution",
                table: "Requests",
                sql: "([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1)");

            migrationBuilder.CreateIndex(
                name: "IX_People_ImportBatchId",
                table: "People",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Managements_Code",
                table: "Managements",
                column: "Code",
                unique: true,
                filter: "[Status] <> 99");

            migrationBuilder.CreateIndex(
                name: "IX_Managements_Status",
                table: "Managements",
                column: "Status",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Semester",
                table: "Managements",
                sql: "[Semester] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Status",
                table: "Managements",
                sql: "[Status] IN (0, 1, 2, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements",
                sql: "[Type] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements",
                sql: "[Year] BETWEEN 2000 AND 2100");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ImportBatchId",
                table: "ManagementPlans",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans",
                columns: new[] { "ManagementId", "EquipmentUnitId" },
                unique: true,
                filter: "[EquipmentUnitId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagementPlans_Phase",
                table: "ManagementPlans",
                sql: "[CurrentPhase] BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagementPlans_State",
                table: "ManagementPlans",
                sql: "[CurrentState] BETWEEN 1 AND 9");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagementPlans_Status",
                table: "ManagementPlans",
                sql: "[PlanStatus] BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagementPlans_Weeks",
                table: "ManagementPlans",
                sql: "([PlannedWeek] IS NULL OR [PlannedWeek] BETWEEN 1 AND 8) AND ([ExecutedWeek] IS NULL OR [ExecutedWeek] BETWEEN 1 AND 8)");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenances_ImportBatchId",
                table: "Maintenances",
                column: "ImportBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_CompletionPercentage",
                table: "Maintenances",
                sql: "[CompletionPercentage] BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_ExecutionDates",
                table: "Maintenances",
                sql: "[StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate] >= [StartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_NonNegativeCosts",
                table: "Maintenances",
                sql: "([EstimatedCost] IS NULL OR [EstimatedCost] >= 0) AND ([ActualCost] IS NULL OR [ActualCost] >= 0)");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalVerificationQuarantines_ImportBatchId",
                table: "HistoricalVerificationQuarantines",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_ImportBatchId",
                table: "EquipmentUnits",
                column: "ImportBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_LocationResolution",
                table: "EquipmentUnits",
                sql: "([LaboratoryId] IS NULL AND [LocationResolutionStatus] = 0) OR ([LaboratoryId] IS NOT NULL AND [LocationResolutionStatus] = 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ImportBatchId",
                table: "Equipments",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CostDetails_ImportBatchId",
                table: "CostDetails",
                column: "ImportBatchId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CostDetails_ExactlyOneParent",
                table: "CostDetails",
                sql: "CASE WHEN [RequestId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [MaintenanceId] IS NULL THEN 0 ELSE 1 END = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CostDetails_PositiveValues",
                table: "CostDetails",
                sql: "[Quantity] > 0 AND [UnitPrice] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_ImportBatchId_IssueCode_EntityName",
                table: "DataQualityIssues",
                columns: new[] { "ImportBatchId", "IssueCode", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_Code",
                table: "ImportBatches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceParticipants_MaintenanceId",
                table: "MaintenanceParticipants",
                column: "MaintenanceId",
                unique: true,
                filter: "[IsPrimary] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceParticipants_MaintenanceId_PersonId_Role",
                table: "MaintenanceParticipants",
                columns: new[] { "MaintenanceId", "PersonId", "Role" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceParticipants_PersonId",
                table: "MaintenanceParticipants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAliases_PersonId",
                table: "PersonAliases",
                column: "PersonId",
                unique: true,
                filter: "[IsPreferred] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAliases_PersonId_NormalizedAlias",
                table: "PersonAliases",
                columns: new[] { "PersonId", "NormalizedAlias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignments_PersonId_Role",
                table: "PersonRoleAssignments",
                columns: new[] { "PersonId", "Role" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_ImportBatches_ImportBatchId",
                table: "CostDetails",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_ImportBatches_ImportBatchId",
                table: "Equipments",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_ImportBatches_ImportBatchId",
                table: "EquipmentUnits",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricalVerificationQuarantines_ImportBatches_ImportBatchId",
                table: "HistoricalVerificationQuarantines",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_ImportBatches_ImportBatchId",
                table: "Maintenances",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_ImportBatches_ImportBatchId",
                table: "ManagementPlans",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_People_ImportBatches_ImportBatchId",
                table: "People",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_ImportBatches_ImportBatchId",
                table: "Requests",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_ImportBatches_ImportBatchId",
                table: "Verifications",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "THROW 51000, 'La normalización oficial elimina únicamente datos derivados sin respaldo. Restaure el backup previo para revertirla.', 1;");

            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_ImportBatches_ImportBatchId",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_ImportBatches_ImportBatchId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_ImportBatches_ImportBatchId",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricalVerificationQuarantines_ImportBatches_ImportBatchId",
                table: "HistoricalVerificationQuarantines");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_ImportBatches_ImportBatchId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_ImportBatches_ImportBatchId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_People_ImportBatches_ImportBatchId",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_ImportBatches_ImportBatchId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_ImportBatches_ImportBatchId",
                table: "Verifications");

            migrationBuilder.DropTable(
                name: "DataQualityIssues");

            migrationBuilder.DropTable(
                name: "MaintenanceParticipants");

            migrationBuilder.DropTable(
                name: "PersonAliases");

            migrationBuilder.DropTable(
                name: "PersonRoleAssignments");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_Verifications_ImportBatchId",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_VerificationCheckResults_VerificationId_CheckItemId",
                table: "VerificationCheckResults");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VerificationCheckResults_Result",
                table: "VerificationCheckResults");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImportBatchId",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_LocationResolution",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_People_ImportBatchId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_Managements_Code",
                table: "Managements");

            migrationBuilder.DropIndex(
                name: "IX_Managements_Status",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Semester",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Status",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ImportBatchId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagementPlans_Phase",
                table: "ManagementPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagementPlans_State",
                table: "ManagementPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagementPlans_Status",
                table: "ManagementPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagementPlans_Weeks",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_Maintenances_ImportBatchId",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_CompletionPercentage",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_ExecutionDates",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_NonNegativeCosts",
                table: "Maintenances");

            migrationBuilder.DropIndex(
                name: "IX_HistoricalVerificationQuarantines_ImportBatchId",
                table: "HistoricalVerificationQuarantines");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_ImportBatchId",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_LocationResolution",
                table: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_ImportBatchId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_CostDetails_ImportBatchId",
                table: "CostDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CostDetails_ExactlyOneParent",
                table: "CostDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CostDetails_PositiveValues",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "LocationResolutionStatus",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "People");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "HistoricalVerificationQuarantines");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "LocationResolutionStatus",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "CostDetails");

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCheckResults_VerificationId",
                table: "VerificationCheckResults",
                column: "VerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ManagementId",
                table: "ManagementPlans",
                column: "ManagementId");
        }
    }
}
