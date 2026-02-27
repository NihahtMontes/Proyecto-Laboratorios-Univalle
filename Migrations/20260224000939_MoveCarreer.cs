using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class MoveCarreer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Career",
                table: "Equipments");

            migrationBuilder.AddColumn<string>(
                name: "Career",
                table: "EquipmentUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Career",
                table: "EquipmentUnits");

            migrationBuilder.AddColumn<string>(
                name: "Career",
                table: "Equipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
