using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeksToManagementPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExecutedWeek",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedWeek",
                table: "ManagementPlans",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutedWeek",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "PlannedWeek",
                table: "ManagementPlans");
        }
    }
}
