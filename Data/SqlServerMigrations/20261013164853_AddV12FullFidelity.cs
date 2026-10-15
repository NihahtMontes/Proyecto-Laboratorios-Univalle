using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddV12FullFidelity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements");

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "Maintenances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "MaintenancePlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UndatedL3Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagementId = table.Column<int>(type: "int", nullable: true),
                    CampusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CareerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LaboratoryId = table.Column<int>(type: "int", nullable: true),
                    AllLaboratories = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TeacherPersonId = table.Column<int>(type: "int", nullable: true),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTimeText = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EndTimeText = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PracticeNumberText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UndatedL3Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UndatedL3Requests_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UndatedL3Requests_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UndatedL3Requests_People_TeacherPersonId",
                        column: x => x.TeacherPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UndatedL3Requests_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UndatedL3RequestLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UndatedL3RequestId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: true),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UndatedL3RequestLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UndatedL3RequestLines_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UndatedL3RequestLines_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UndatedL3RequestLines_UndatedL3Requests_UndatedL3RequestId",
                        column: x => x.UndatedL3RequestId,
                        principalTable: "UndatedL3Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements",
                sql: "[Year] BETWEEN 1900 AND 2100");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3RequestLines_ArticleId",
                table: "UndatedL3RequestLines",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3RequestLines_EquipmentUnitId",
                table: "UndatedL3RequestLines",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3RequestLines_UndatedL3RequestId",
                table: "UndatedL3RequestLines",
                column: "UndatedL3RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3Requests_Code",
                table: "UndatedL3Requests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3Requests_LaboratoryId",
                table: "UndatedL3Requests",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3Requests_ManagementId",
                table: "UndatedL3Requests",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3Requests_SubjectId",
                table: "UndatedL3Requests",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UndatedL3Requests_TeacherPersonId",
                table: "UndatedL3Requests",
                column: "TeacherPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UndatedL3RequestLines");

            migrationBuilder.DropTable(
                name: "UndatedL3Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "MaintenancePlans");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_Year",
                table: "Managements",
                sql: "[Year] BETWEEN 2000 AND 2100");
        }
    }
}
