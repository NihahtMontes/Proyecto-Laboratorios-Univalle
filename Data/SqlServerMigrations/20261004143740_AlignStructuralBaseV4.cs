using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AlignStructuralBaseV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PracticeRequirements_Quantity",
                table: "PracticeRequirements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PracticeRequirements",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PracticeRequirements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "CostDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "CostDetails",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedDate",
                table: "CostDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PracticeSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    PracticeId = table.Column<int>(type: "int", nullable: false),
                    CourseGroupId = table.Column<int>(type: "int", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TeacherPersonId = table.Column<int>(type: "int", nullable: false),
                    LaboratoryId = table.Column<int>(type: "int", nullable: true),
                    WeekNumber = table.Column<int>(type: "int", nullable: true),
                    AcademicWeekId = table.Column<int>(type: "int", nullable: true),
                    ExecutionDate = table.Column<DateTime>(type: "date", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    StudentCount = table.Column<int>(type: "int", nullable: true),
                    IsDemonstrative = table.Column<bool>(type: "bit", nullable: true),
                    ScalingFactor = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSchedules", x => x.Id);
                    table.CheckConstraint("CK_PracticeSchedules_Counts", "([StudentCount] IS NULL OR [StudentCount] >= 0) AND ([ScalingFactor] IS NULL OR [ScalingFactor] >= 0)");
                    table.CheckConstraint("CK_PracticeSchedules_Status", "[Status] IN (0, 1, 2, 3, 9)");
                    table.CheckConstraint("CK_PracticeSchedules_Times", "[StartTime] IS NULL OR [EndTime] IS NULL OR [EndTime] > [StartTime]");
                    table.CheckConstraint("CK_PracticeSchedules_Week", "[WeekNumber] IS NULL OR [WeekNumber] BETWEEN 1 AND 30");
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_AcademicWeeks_AcademicWeekId",
                        column: x => x.AcademicWeekId,
                        principalTable: "AcademicWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_CourseGroups_CourseGroupId",
                        column: x => x.CourseGroupId,
                        principalTable: "CourseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_People_TeacherPersonId",
                        column: x => x.TeacherPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_Practices_PracticeId",
                        column: x => x.PracticeId,
                        principalTable: "Practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeSchedules_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRequirements_Code",
                table: "PracticeRequirements",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PracticeRequirements_Quantity",
                table: "PracticeRequirements",
                sql: "[Quantity] IS NULL OR [Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements",
                sql: "[Type] IN (0, 1, 9)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances",
                sql: "[MaintenanceType] IN (1, 2, 3, 4, 5, 9, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans",
                sql: "[MaintenanceType] IS NULL OR [MaintenanceType] IN (1, 2, 3, 4, 5, 9, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CostDetails_Voided",
                table: "CostDetails",
                sql: "[IsVoided] = 0 OR [VoidedDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_AcademicWeekId",
                table: "PracticeSchedules",
                column: "AcademicWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_Code",
                table: "PracticeSchedules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_CourseGroupId",
                table: "PracticeSchedules",
                column: "CourseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_LaboratoryId",
                table: "PracticeSchedules",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_ManagementId_PracticeId_GroupName",
                table: "PracticeSchedules",
                columns: new[] { "ManagementId", "PracticeId", "GroupName" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_PracticeId",
                table: "PracticeSchedules",
                column: "PracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_SubjectId",
                table: "PracticeSchedules",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSchedules_TeacherPersonId",
                table: "PracticeSchedules",
                column: "TeacherPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeSchedules");

            migrationBuilder.DropIndex(
                name: "IX_PracticeRequirements_Code",
                table: "PracticeRequirements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PracticeRequirements_Quantity",
                table: "PracticeRequirements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CostDetails_Voided",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "PracticeRequirements");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "VoidedDate",
                table: "CostDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PracticeRequirements",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PracticeRequirements_Quantity",
                table: "PracticeRequirements",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Type",
                table: "Managements",
                sql: "[Type] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances",
                sql: "[MaintenanceType] IN (1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans",
                sql: "[MaintenanceType] IS NULL OR [MaintenanceType] IN (1, 2, 3, 4, 5, 99)");
        }
    }
}
