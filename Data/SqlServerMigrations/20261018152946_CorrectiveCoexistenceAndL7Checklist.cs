using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class CorrectiveCoexistenceAndL7Checklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managements_Status",
                table: "Managements");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "Requests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managements_Type_Status",
                table: "Managements",
                columns: new[] { "Type", "Status" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans",
                columns: new[] { "ManagementId", "EquipmentUnitId" },
                unique: true,
                filter: "[EquipmentUnitId] IS NOT NULL AND [PlanStatus] <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Managements_Type_Status",
                table: "Managements");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "Requests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managements_Status",
                table: "Managements",
                column: "Status",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ManagementId_EquipmentUnitId",
                table: "ManagementPlans",
                columns: new[] { "ManagementId", "EquipmentUnitId" },
                unique: true,
                filter: "[EquipmentUnitId] IS NOT NULL");
        }
    }
}
