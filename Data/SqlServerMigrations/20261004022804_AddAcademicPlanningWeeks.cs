using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddAcademicPlanningWeeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicPlanningId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcademicWeekId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicPlannings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    FacultyId = table.Column<int>(type: "int", nullable: false),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPlannings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicPlannings_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicPlannings_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicPlannings_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicPlanningId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsLective = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicWeeks_AcademicPlannings_AcademicPlanningId",
                        column: x => x.AcademicPlanningId,
                        principalTable: "AcademicPlannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_AcademicPlanningId",
                table: "L3Requests",
                column: "AcademicPlanningId");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_AcademicWeekId",
                table: "L3Requests",
                column: "AcademicWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_CareerId",
                table: "AcademicPlannings",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_FacultyId",
                table: "AcademicPlannings",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_ManagementId_CareerId",
                table: "AcademicPlannings",
                columns: new[] { "ManagementId", "CareerId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicWeeks_AcademicPlanningId_Number",
                table: "AcademicWeeks",
                columns: new[] { "AcademicPlanningId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_AcademicPlannings_AcademicPlanningId",
                table: "L3Requests",
                column: "AcademicPlanningId",
                principalTable: "AcademicPlannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_AcademicWeeks_AcademicWeekId",
                table: "L3Requests",
                column: "AcademicWeekId",
                principalTable: "AcademicWeeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_AcademicPlannings_AcademicPlanningId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_AcademicWeeks_AcademicWeekId",
                table: "L3Requests");

            migrationBuilder.DropTable(
                name: "AcademicWeeks");

            migrationBuilder.DropTable(
                name: "AcademicPlannings");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_AcademicPlanningId",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_AcademicWeekId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "AcademicPlanningId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "AcademicWeekId",
                table: "L3Requests");
        }
    }
}
