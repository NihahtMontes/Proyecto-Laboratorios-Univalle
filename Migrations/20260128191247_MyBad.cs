using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class MyBad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Users_TechnicianId",
                table: "Maintenances");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances",
                column: "TechnicianId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Users_TechnicianId",
                table: "Maintenances",
                column: "TechnicianId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
