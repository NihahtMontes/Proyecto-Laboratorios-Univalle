using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AlignHistoricalWorkbookV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsiblePersonId",
                table: "Verifications",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RequestedByPersonId",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorCode",
                table: "People",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsiblePersonId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Block",
                table: "Laboratories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "Laboratories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Laboratories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Laboratories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractVersion",
                table: "ImportBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "legacy-v1");

            migrationBuilder.AlterColumn<int>(
                name: "ManagementId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CatalogCode",
                table: "Equipments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "Departures",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BorrowerId",
                table: "Departures",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "Departures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "Departures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "Departures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginLaboratoryId",
                table: "Departures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "DepartureItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateKeys",
                table: "DataQualityIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "DataQualityIssues",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "DataQualityIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCode",
                table: "DataQualityIssues",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceRowNumber",
                table: "DataQualityIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSheet",
                table: "DataQualityIssues",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CostDate",
                table: "CostDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "CostDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderPersonId",
                table: "CostDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Careers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.CheckConstraint("CK_Articles_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_Articles_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Articles_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Articles_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ImportSourceRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    SourceSheet = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    SourceRowKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TargetEntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetEntityKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MigrationStatus = table.Column<int>(type: "int", nullable: false),
                    ReconciliationStatus = table.Column<int>(type: "int", nullable: false),
                    OriginalDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastReconciledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSourceRows", x => x.Id);
                    table.CheckConstraint("CK_ImportSourceRows_MigrationStatus", "[MigrationStatus] IN (0, 1, 2, 3, 99)");
                    table.CheckConstraint("CK_ImportSourceRows_ReconciliationStatus", "[ReconciliationStatus] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_ImportSourceRows_SourceRow", "[SourceRowNumber] > 0");
                    table.ForeignKey(
                        name: "FK_ImportSourceRows_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    IsLegacyPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DeactivatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRequests_Activation", "(([IsActive] = 1 AND [DeactivatedDate] IS NULL) OR ([IsActive] = 0 AND [DeactivatedDate] IS NOT NULL)) AND ([IsLegacyPrimary] = 0 OR [IsActive] = 1)");
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Maintenances_MaintenanceId",
                        column: x => x.MaintenanceId,
                        principalTable: "Maintenances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestEquipmentUnits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: false),
                    IsLegacyPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DeactivatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestEquipmentUnits", x => x.Id);
                    table.CheckConstraint("CK_RequestEquipmentUnits_Activation", "(([IsActive] = 1 AND [DeactivatedDate] IS NULL) OR ([IsActive] = 0 AND [DeactivatedDate] IS NOT NULL)) AND ([IsLegacyPrimary] = 0 OR [IsActive] = 1)");
                    table.ForeignKey(
                        name: "FK_RequestEquipmentUnits_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestEquipmentUnits_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO dbo.RequestEquipmentUnits
                    (RequestId, EquipmentUnitId, IsLegacyPrimary, IsActive, DeactivatedDate,
                     CreatedById, CreatedDate, ModifiedById, LastModifiedDate)
                SELECT r.Id, r.EquipmentUnitId, 1, 1, NULL,
                       r.CreatedById, r.CreatedDate, NULL, NULL
                FROM dbo.Requests AS r
                WHERE r.EquipmentUnitId IS NOT NULL;

                INSERT INTO dbo.MaintenanceRequests
                    (MaintenanceId, RequestId, IsLegacyPrimary, IsActive, DeactivatedDate,
                     CreatedById, CreatedDate, ModifiedById, LastModifiedDate)
                SELECT m.Id, m.RequestId, 1, 1, NULL,
                       m.CreatedById, m.CreatedDate, NULL, NULL
                FROM dbo.Maintenances AS m
                WHERE m.RequestId IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Verifications_ResponsiblePersonId",
                table: "Verifications",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestedByPersonId",
                table: "Requests",
                column: "RequestedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_People_ActorCode",
                table: "People",
                column: "ActorCode",
                unique: true,
                filter: "[ActorCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ResponsiblePersonId",
                table: "ManagementPlans",
                column: "ResponsiblePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CatalogCode",
                table: "Equipments",
                column: "CatalogCode",
                unique: true,
                filter: "[CatalogCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_HistoricalSourceKey",
                table: "Departures",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_ImportBatchId",
                table: "Departures",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_OriginLaboratoryId",
                table: "Departures",
                column: "OriginLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_ArticleId",
                table: "DepartureItems",
                column: "ArticleId");

            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.DepartureItems
                    WHERE EquipmentUnitId IS NULL
                )
                BEGIN
                    THROW 51021, 'AlignHistoricalWorkbookV2 requiere conciliar los DepartureItems legacy sin EquipmentUnitId y asignarles un ArticleId antes de activar CK_DepartureItems_ExactlyOneReference.', 1;
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DepartureItems_ExactlyOneReference",
                table: "DepartureItems",
                sql: "CASE WHEN [EquipmentUnitId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ArticleId] IS NULL THEN 0 ELSE 1 END = 1");

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_ImportBatchId_SourceSheet_SourceRowNumber",
                table: "DataQualityIssues",
                columns: new[] { "ImportBatchId", "SourceSheet", "SourceRowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DataQualityIssues_ResolvedByUserId",
                table: "DataQualityIssues",
                column: "ResolvedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataQualityIssues_SourceRow",
                table: "DataQualityIssues",
                sql: "[SourceRowNumber] IS NULL OR [SourceRowNumber] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_CostDetails_HistoricalSourceKey",
                table: "CostDetails",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CostDetails_ProviderPersonId",
                table: "CostDetails",
                column: "ProviderPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_Code",
                table: "Careers",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Code",
                table: "Articles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CreatedById",
                table: "Articles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ImportBatchId",
                table: "Articles",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ModifiedById",
                table: "Articles",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ImportSourceRows_ImportBatchId_SourceRowKey_TargetEntityName",
                table: "ImportSourceRows",
                columns: new[] { "ImportBatchId", "SourceRowKey", "TargetEntityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_MaintenanceId_RequestId",
                table: "MaintenanceRequests",
                columns: new[] { "MaintenanceId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_RequestId",
                table: "MaintenanceRequests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestEquipmentUnits_EquipmentUnitId",
                table: "RequestEquipmentUnits",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestEquipmentUnits_RequestId_EquipmentUnitId",
                table: "RequestEquipmentUnits",
                columns: new[] { "RequestId", "EquipmentUnitId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_People_ProviderPersonId",
                table: "CostDetails",
                column: "ProviderPersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DataQualityIssues_Users_ResolvedByUserId",
                table: "DataQualityIssues",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartureItems_Articles_ArticleId",
                table: "DepartureItems",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_ImportBatches_ImportBatchId",
                table: "Departures",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_Laboratories_OriginLaboratoryId",
                table: "Departures",
                column: "OriginLaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_People_ResponsiblePersonId",
                table: "ManagementPlans",
                column: "ResponsiblePersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_People_RequestedByPersonId",
                table: "Requests",
                column: "RequestedByPersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_People_ResponsiblePersonId",
                table: "Verifications",
                column: "ResponsiblePersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_People_ProviderPersonId",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_DataQualityIssues_Users_ResolvedByUserId",
                table: "DataQualityIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartureItems_Articles_ArticleId",
                table: "DepartureItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Departures_ImportBatches_ImportBatchId",
                table: "Departures");

            migrationBuilder.DropForeignKey(
                name: "FK_Departures_Laboratories_OriginLaboratoryId",
                table: "Departures");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_People_ResponsiblePersonId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_People_RequestedByPersonId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_People_ResponsiblePersonId",
                table: "Verifications");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "ImportSourceRows");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "RequestEquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_Verifications_ResponsiblePersonId",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RequestedByPersonId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_People_ActorCode",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ResponsiblePersonId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_CatalogCode",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Departures_HistoricalSourceKey",
                table: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_Departures_ImportBatchId",
                table: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_Departures_OriginLaboratoryId",
                table: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_DepartureItems_ArticleId",
                table: "DepartureItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DepartureItems_ExactlyOneReference",
                table: "DepartureItems");

            migrationBuilder.DropIndex(
                name: "IX_DataQualityIssues_ImportBatchId_SourceSheet_SourceRowNumber",
                table: "DataQualityIssues");

            migrationBuilder.DropIndex(
                name: "IX_DataQualityIssues_ResolvedByUserId",
                table: "DataQualityIssues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataQualityIssues_SourceRow",
                table: "DataQualityIssues");

            migrationBuilder.DropIndex(
                name: "IX_CostDetails_HistoricalSourceKey",
                table: "CostDetails");

            migrationBuilder.DropIndex(
                name: "IX_CostDetails_ProviderPersonId",
                table: "CostDetails");

            migrationBuilder.DropIndex(
                name: "IX_Careers_Code",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "ResponsiblePersonId",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "RequestedByPersonId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ActorCode",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ResponsiblePersonId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "Block",
                table: "Laboratories");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "Laboratories");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "Laboratories");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Laboratories");

            migrationBuilder.DropColumn(
                name: "ContractVersion",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "CatalogCode",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "OriginLaboratoryId",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "DepartureItems");

            migrationBuilder.DropColumn(
                name: "CandidateKeys",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "SourceCode",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "SourceRowNumber",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "SourceSheet",
                table: "DataQualityIssues");

            migrationBuilder.DropColumn(
                name: "CostDate",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "ProviderPersonId",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Careers");

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM dbo.Requests WHERE EquipmentId IS NULL)
                    THROW 51022, 'No se puede revertir AlignHistoricalWorkbookV2: existen Requests sin EquipmentId legacy.', 1;

                IF EXISTS (SELECT 1 FROM dbo.EquipmentUnits WHERE ManagementId IS NULL)
                    THROW 51023, 'No se puede revertir AlignHistoricalWorkbookV2: existen EquipmentUnits sin ManagementId de registro.', 1;

                IF EXISTS (SELECT 1 FROM dbo.Departures WHERE EquipmentUnitId IS NULL OR BorrowerId IS NULL)
                    THROW 51024, 'No se puede revertir AlignHistoricalWorkbookV2: existen Departures normalizadas sin cabecera legacy completa.', 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentId",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ManagementId",
                table: "EquipmentUnits",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "Departures",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BorrowerId",
                table: "Departures",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
