using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class MoveLocationBackToEquipmentV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Cities_CityId",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Countries_CountryId",
                table: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_CityId",
                table: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_CountryId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "EquipmentUnits");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CityId",
                table: "EquipmentUnits",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CountryId",
                table: "EquipmentUnits",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Cities_CityId",
                table: "EquipmentUnits",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Countries_CountryId",
                table: "EquipmentUnits",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
