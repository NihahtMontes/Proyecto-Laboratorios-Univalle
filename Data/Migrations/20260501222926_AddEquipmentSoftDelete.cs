using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
            
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Equipments");
        }
    }
}
