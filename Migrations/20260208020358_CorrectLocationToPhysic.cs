using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class CorrectLocationToPhysic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_Users_CreatedById",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_Users_ModifiedById",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Users_ModifiedById",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_Users_CreatedById",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_Users_ModifiedById",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Users_ModifiedById",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Laboratories_Users_ModifiedById",
                table: "Laboratories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Users_AssignedTechnicianId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Users_CreatedById",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Users_ModifiedById",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ApprovedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_CreatedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ModifiedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ModifiedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Users_CreatedById",
                table: "Verifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Users_ModifiedById",
                table: "Verifications");

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

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "EquipmentStateHistories",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CityId",
                table: "EquipmentUnits",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CountryId",
                table: "EquipmentUnits",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_Users_CreatedById",
                table: "CostDetails",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_Users_ModifiedById",
                table: "CostDetails",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

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
                name: "FK_Equipments_Users_ModifiedById",
                table: "Equipments",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_Users_CreatedById",
                table: "EquipmentStateHistories",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_Users_ModifiedById",
                table: "EquipmentStateHistories",
                column: "ModifiedById",
                principalTable: "Users",
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

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Users_ModifiedById",
                table: "EquipmentUnits",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratories_Users_ModifiedById",
                table: "Laboratories",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Users_AssignedTechnicianId",
                table: "MaintenancePlans",
                column: "AssignedTechnicianId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances",
                column: "TechnicianId",
                principalTable: "People",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Users_CreatedById",
                table: "Maintenances",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Users_ModifiedById",
                table: "Maintenances",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ApprovedById",
                table: "Requests",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_CreatedById",
                table: "Requests",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ModifiedById",
                table: "Requests",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestedById",
                table: "Requests",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ModifiedById",
                table: "Users",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Users_CreatedById",
                table: "Verifications",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Users_ModifiedById",
                table: "Verifications",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_Users_CreatedById",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_Users_ModifiedById",
                table: "CostDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Cities_CityId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Countries_CountryId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Users_ModifiedById",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_Users_CreatedById",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentStateHistories_Users_ModifiedById",
                table: "EquipmentStateHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Cities_CityId",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Countries_CountryId",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Users_ModifiedById",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Laboratories_Users_ModifiedById",
                table: "Laboratories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Users_AssignedTechnicianId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Users_CreatedById",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Users_ModifiedById",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ApprovedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_CreatedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ModifiedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestedById",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ModifiedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Users_CreatedById",
                table: "Verifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Users_ModifiedById",
                table: "Verifications");

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

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "EquipmentStateHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_Users_CreatedById",
                table: "CostDetails",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_Users_ModifiedById",
                table: "CostDetails",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Users_ModifiedById",
                table: "Equipments",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_Users_CreatedById",
                table: "EquipmentStateHistories",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentStateHistories_Users_ModifiedById",
                table: "EquipmentStateHistories",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Users_ModifiedById",
                table: "EquipmentUnits",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratories_Users_ModifiedById",
                table: "Laboratories",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Users_AssignedTechnicianId",
                table: "MaintenancePlans",
                column: "AssignedTechnicianId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_People_TechnicianId",
                table: "Maintenances",
                column: "TechnicianId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Users_CreatedById",
                table: "Maintenances",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Users_ModifiedById",
                table: "Maintenances",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ApprovedById",
                table: "Requests",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_CreatedById",
                table: "Requests",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ModifiedById",
                table: "Requests",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestedById",
                table: "Requests",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ModifiedById",
                table: "Users",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Users_CreatedById",
                table: "Verifications",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Users_ModifiedById",
                table: "Verifications",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
