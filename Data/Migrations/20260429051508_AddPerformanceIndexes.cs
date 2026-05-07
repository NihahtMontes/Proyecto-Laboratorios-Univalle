using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_CurrentPhase_ManagementId",
                table: "ManagementPlans",
                columns: new[] { "CurrentPhase", "ManagementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_PlanStatus_ManagementId",
                table: "ManagementPlans",
                columns: new[] { "PlanStatus", "ManagementId" });

            migrationBuilder.CreateIndex(
                name: "IX_Maintenances_Status",
                table: "Maintenances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits",
                column: "CurrentStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_CurrentPhase_ManagementId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_PlanStatus_ManagementId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_Maintenances_Status",
                table: "Maintenances");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits");
        }
    }
}
