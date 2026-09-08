using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddEquipmentClassificationGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments");

            migrationBuilder.AlterColumn<int>(
                name: "UtensilType",
                table: "Equipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TypeClassification",
                table: "Equipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ClassificationReviewStatus",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtherClassificationDetail",
                table: "Equipments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipmentClassificationDecisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ReviewStatus = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: true),
                    TypeClassification = table.Column<int>(type: "int", nullable: true),
                    UtensilType = table.Column<int>(type: "int", nullable: true),
                    GeneralStatus = table.Column<int>(type: "int", nullable: true),
                    OtherDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResponsiblePersonId = table.Column<int>(type: "int", nullable: true),
                    ResponsibleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    ImportSourceRowId = table.Column<long>(type: "bigint", nullable: true),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentClassificationDecisions", x => x.Id);
                    table.CheckConstraint("CK_EquipmentClassificationDecisions_Confirmed", "[ReviewStatus] <> 2 OR ([GeneralStatus] IS NOT NULL AND [DecisionDate] IS NOT NULL AND LEN(LTRIM(RTRIM(COALESCE([EvidenceReference], '')))) > 0 AND LEN(LTRIM(RTRIM(COALESCE([ResponsibleSnapshot], '')))) > 0 AND (([Category] = 0 AND [TypeClassification] IS NOT NULL AND [UtensilType] IS NULL AND ([TypeClassification] <> 7 OR LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)) OR ([Category] = 1 AND [TypeClassification] IS NULL AND [UtensilType] BETWEEN 1 AND 11 AND ([UtensilType] <> 11 OR LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)) OR ([Category] = 2 AND [TypeClassification] IS NULL AND [UtensilType] IS NULL AND LEN(LTRIM(RTRIM(COALESCE([OtherDetail], '')))) > 0)))");
                    table.CheckConstraint("CK_EquipmentClassificationDecisions_Dates", "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
                    table.CheckConstraint("CK_EquipmentClassificationDecisions_Status", "[ReviewStatus] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_EquipmentClassificationDecisions_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentClassificationDecisions_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentClassificationDecisions_ImportSourceRows_ImportSourceRowId",
                        column: x => x.ImportSourceRowId,
                        principalTable: "ImportSourceRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentClassificationDecisions_People_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentClassificationDecisions_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ClassificationReviewStatus_Category",
                table: "Equipments",
                columns: new[] { "ClassificationReviewStatus", "Category" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_ClassificationReviewStatus",
                table: "Equipments",
                sql: "[ClassificationReviewStatus] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_ConfirmedClassificationHierarchy",
                table: "Equipments",
                sql: "[ClassificationReviewStatus] <> 2 OR (([Category] = 0 AND [TypeClassification] IS NOT NULL AND [UtensilType] IS NULL AND ([TypeClassification] <> 7 OR LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0)) OR ([Category] = 1 AND [TypeClassification] IS NULL AND [UtensilType] BETWEEN 1 AND 11 AND ([UtensilType] <> 11 OR LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0)) OR ([Category] = 2 AND [TypeClassification] IS NULL AND [UtensilType] IS NULL AND LEN(LTRIM(RTRIM(COALESCE([OtherClassificationDetail], '')))) > 0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments",
                sql: "[TypeClassification] IS NULL OR [TypeClassification] BETWEEN 0 AND 15");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments",
                sql: "[UtensilType] IS NULL OR [UtensilType] BETWEEN 0 AND 11");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_DecisionKey",
                table: "EquipmentClassificationDecisions",
                column: "DecisionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_EquipmentId",
                table: "EquipmentClassificationDecisions",
                column: "EquipmentId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_ImportBatchId",
                table: "EquipmentClassificationDecisions",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_ImportSourceRowId",
                table: "EquipmentClassificationDecisions",
                column: "ImportSourceRowId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_RecordedByUserId",
                table: "EquipmentClassificationDecisions",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentClassificationDecisions_ResponsiblePersonId",
                table: "EquipmentClassificationDecisions",
                column: "ResponsiblePersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentClassificationDecisions");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_ClassificationReviewStatus_Category",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_ClassificationReviewStatus",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_ConfirmedClassificationHierarchy",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ClassificationReviewStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "OtherClassificationDetail",
                table: "Equipments");

            migrationBuilder.AlterColumn<int>(
                name: "UtensilType",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TypeClassification",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments",
                sql: "[TypeClassification] BETWEEN 0 AND 15");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments",
                sql: "[UtensilType] BETWEEN 0 AND 11");
        }
    }
}
