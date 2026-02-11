using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class CompleteMoveLaboratoryToUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_LaboratoryId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "Equipments");

            migrationBuilder.AddColumn<int>(
                name: "LaboratoryId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_LaboratoryId",
                table: "EquipmentUnits",
                column: "LaboratoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Laboratories_LaboratoryId",
                table: "EquipmentUnits",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Laboratories_LaboratoryId",
                table: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_LaboratoryId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "EquipmentUnits");

            migrationBuilder.AddColumn<int>(
                name: "LaboratoryId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LaboratoryId",
                table: "Equipments",
                column: "LaboratoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
