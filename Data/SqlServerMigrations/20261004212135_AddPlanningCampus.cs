using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddPlanningCampus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcademicPlannings_ManagementId_CareerId",
                table: "AcademicPlannings");

            migrationBuilder.AddColumn<int>(
                name: "CampusId",
                table: "AcademicPlannings",
                type: "int",
                nullable: true);

            // Planificaciones existentes: la sede donde está la mayoría de laboratorios activos de su facultad.
            migrationBuilder.Sql(@"
UPDATE p
SET p.[CampusId] = (
    SELECT TOP 1 l.[CampusId]
    FROM [Laboratories] l
    WHERE l.[FacultyId] = p.[FacultyId] AND l.[CampusId] IS NOT NULL AND l.[Status] = 0
    GROUP BY l.[CampusId]
    ORDER BY COUNT(*) DESC, l.[CampusId])
FROM [AcademicPlannings] p
WHERE p.[CampusId] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_CampusId",
                table: "AcademicPlannings",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_ManagementId_CampusId_CareerId",
                table: "AcademicPlannings",
                columns: new[] { "ManagementId", "CampusId", "CareerId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicPlannings_Campuses_CampusId",
                table: "AcademicPlannings",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicPlannings_Campuses_CampusId",
                table: "AcademicPlannings");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPlannings_CampusId",
                table: "AcademicPlannings");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPlannings_ManagementId_CampusId_CareerId",
                table: "AcademicPlannings");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "AcademicPlannings");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPlannings_ManagementId_CareerId",
                table: "AcademicPlannings",
                columns: new[] { "ManagementId", "CareerId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
