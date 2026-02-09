using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class SyncNullableFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId",
                table: "MaintenancePlans");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "MaintenancePlans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "EquipmentStateHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "Equipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId",
                table: "EquipmentStateHistories",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId",
                table: "MaintenancePlans",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId",
                table: "MaintenancePlans");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "MaintenancePlans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "EquipmentStateHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Laboratories_LaboratoryId",
                table: "Equipments",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId",
                table: "EquipmentStateHistories",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId",
                table: "MaintenancePlans",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
