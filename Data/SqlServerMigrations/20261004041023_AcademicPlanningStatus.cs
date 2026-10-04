using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AcademicPlanningStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AcademicPlannings",
                type: "int",
                nullable: false,
                // Filas existentes quedan Inactivas (1) para no chocar con el índice de una sola activa.
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "UX_AcademicPlannings_SingleActive",
                table: "AcademicPlannings",
                column: "Status",
                unique: true,
                filter: "[Status] = 0 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AcademicPlannings_SingleActive",
                table: "AcademicPlannings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AcademicPlannings");
        }
    }
}
