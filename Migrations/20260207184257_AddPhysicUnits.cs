using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_Equipments_EquipmentId",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Equipments_EquipmentId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Equipments_EquipmentId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Equipments_EquipmentId",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "AcquisitionDate",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "AcquisitionValue",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CurrentStatus",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "InventoryNumber",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "PhysicalCondition",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ServiceStartDate",
                table: "Equipments");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "Verifications",
                newName: "EquipmentUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Verifications_EquipmentId",
                table: "Verifications",
                newName: "IX_Verifications_EquipmentUnitId");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "Maintenances",
                newName: "EquipmentUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_Maintenances_EquipmentId",
                table: "Maintenances",
                newName: "IX_Maintenances_EquipmentUnitId");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "MaintenancePlans",
                newName: "EquipmentUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_MaintenancePlans_EquipmentId",
                table: "MaintenancePlans",
                newName: "IX_MaintenancePlans_EquipmentUnitId");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "EquipmentStateHistories",
                newName: "EquipmentUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_EquipmentStateHistories_EquipmentId",
                table: "EquipmentStateHistories",
                newName: "IX_EquipmentStateHistories_EquipmentUnitId");

            migrationBuilder.RenameColumn(
                name: "Observations",
                table: "Equipments",
                newName: "Description");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentUnitId",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CountryId",
                table: "Equipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Equipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "EquipmentUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    InventoryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcquisitionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcquisitionValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PhysicalCondition = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentUnits_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentUnits_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentUnits_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_EquipmentUnitId",
                table: "Requests",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CreatedById",
                table: "EquipmentUnits",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_EquipmentId",
                table: "EquipmentUnits",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_InventoryNumber",
                table: "EquipmentUnits",
                column: "InventoryNumber",
                unique: true,
                filter: "[CurrentStatus] != 99");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_ModifiedById",
                table: "EquipmentUnits",
                column: "ModifiedById");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_EquipmentUnits_EquipmentUnitId",
                table: "Maintenances",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_EquipmentUnits_EquipmentUnitId",
                table: "Requests",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_EquipmentUnits_EquipmentUnitId",
                table: "Verifications",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_EquipmentUnits_EquipmentUnitId",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_EquipmentUnits_EquipmentUnitId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_EquipmentUnits_EquipmentUnitId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_EquipmentUnits_EquipmentUnitId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_EquipmentUnits_EquipmentUnitId",
                table: "Verifications");

            migrationBuilder.DropTable(
                name: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_Requests_EquipmentUnitId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "EquipmentUnitId",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "EquipmentUnitId",
                table: "Verifications",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Verifications_EquipmentUnitId",
                table: "Verifications",
                newName: "IX_Verifications_EquipmentId");

            migrationBuilder.RenameColumn(
                name: "EquipmentUnitId",
                table: "Maintenances",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Maintenances_EquipmentUnitId",
                table: "Maintenances",
                newName: "IX_Maintenances_EquipmentId");

            migrationBuilder.RenameColumn(
                name: "EquipmentUnitId",
                table: "MaintenancePlans",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_MaintenancePlans_EquipmentUnitId",
                table: "MaintenancePlans",
                newName: "IX_MaintenancePlans_EquipmentId");

            migrationBuilder.RenameColumn(
                name: "EquipmentUnitId",
                table: "EquipmentStateHistories",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_EquipmentStateHistories_EquipmentUnitId",
                table: "EquipmentStateHistories",
                newName: "IX_EquipmentStateHistories_EquipmentId");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Equipments",
                newName: "Observations");

            migrationBuilder.AlterColumn<int>(
                name: "CountryId",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcquisitionDate",
                table: "Equipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AcquisitionValue",
                table: "Equipments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatus",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InventoryNumber",
                table: "Equipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PhysicalCondition",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Equipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartDate",
                table: "Equipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments",
                column: "InventoryNumber",
                unique: true,
                filter: "[CurrentStatus] != 99");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_Equipments_EquipmentId",
                table: "EquipmentStateHistories",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Equipments_EquipmentId",
                table: "MaintenancePlans",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Equipments_EquipmentId",
                table: "Maintenances",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Equipments_EquipmentId",
                table: "Verifications",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
