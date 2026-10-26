using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class RelaxPlanningChecksForExcelFidelity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Plannings_Laboratory",
                table: "Plannings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlanningRequirements_RequestedQuantity",
                table: "PlanningRequirements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlanningRequirements_RequestedQuantity",
                table: "PlanningRequirements",
                sql: "[RequestedQuantity] IS NULL OR [RequestedQuantity] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlanningRequirements_RequestedQuantity",
                table: "PlanningRequirements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Laboratory",
                table: "Plannings",
                sql: "[AllLaboratories] = 1 OR [LaboratoryId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlanningRequirements_RequestedQuantity",
                table: "PlanningRequirements",
                sql: "[RequestedQuantity] IS NULL OR [RequestedQuantity] > 0");
        }
    }
}
