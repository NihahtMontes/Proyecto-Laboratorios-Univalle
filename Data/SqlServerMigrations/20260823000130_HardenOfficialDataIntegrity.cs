using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class HardenOfficialDataIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications",
                sql: "[PhysicalCondition] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_Status",
                table: "Verifications",
                sql: "[Status] IN (0, 1, 2, 3, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Status",
                table: "Users",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests",
                sql: "[Priority] BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Type",
                table: "Requests",
                sql: "[Type] IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_Category",
                table: "People",
                sql: "[Category] IN (1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_People_Status",
                table: "People",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Satisfaction",
                table: "Maintenances",
                sql: "[SatisfactionLevel] IS NULL OR [SatisfactionLevel] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances",
                sql: "[ServiceType] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Status",
                table: "Maintenances",
                sql: "[Status] IN (0, 1, 2, 3, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances",
                sql: "[MaintenanceType] IN (1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans",
                sql: "[ServiceType] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_Times",
                table: "MaintenancePlans",
                sql: "([EstimatedTime] IS NULL OR [EstimatedTime] >= 0) AND ([ActualTime] IS NULL OR [ActualTime] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Laboratories_Status",
                table: "Laboratories",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Interns_Status",
                table: "Interns",
                sql: "[InternStatus] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Faculties_Status",
                table: "Faculties",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Externs_Status",
                table: "Externs",
                sql: "[ExternStatus] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_AcquisitionValue",
                table: "EquipmentUnits",
                sql: "[AcquisitionValue] IS NULL OR [AcquisitionValue] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits",
                sql: "[CurrentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits",
                sql: "[PhysicalCondition] IS NULL OR [PhysicalCondition] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentStateHistories_Dates",
                table: "EquipmentStateHistories",
                sql: "[EndDate] IS NULL OR [EndDate] >= [StartDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Category",
                table: "Equipments",
                sql: "[Category] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Status",
                table: "Equipments",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments",
                sql: "[TypeClassification] BETWEEN 0 AND 15");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_UsefulLife",
                table: "Equipments",
                sql: "[UsefulLifeYears] IS NULL OR [UsefulLifeYears] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments",
                sql: "[UtensilType] BETWEEN 0 AND 11");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departures_Status",
                table: "Departures",
                sql: "[Status] IN (0, 1, 2, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departures_Type",
                table: "Departures",
                sql: "[Type] IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Countries_Status",
                table: "Countries",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CostDetails_Category",
                table: "CostDetails",
                sql: "[Category] IN (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cities_Status",
                table: "Cities",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Careers_Status",
                table: "Careers",
                sql: "[Status] BETWEEN 0 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_Status",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Status",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Type",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_Category",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_People_Status",
                table: "People");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Satisfaction",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Status",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Type",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_Times",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Laboratories_Status",
                table: "Laboratories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Interns_Status",
                table: "Interns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Faculties_Status",
                table: "Faculties");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Externs_Status",
                table: "Externs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_AcquisitionValue",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentStateHistories_Dates",
                table: "EquipmentStateHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Category",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Status",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_TypeClassification",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_UsefulLife",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_UtensilType",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departures_Status",
                table: "Departures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departures_Type",
                table: "Departures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Countries_Status",
                table: "Countries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CostDetails_Category",
                table: "CostDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cities_Status",
                table: "Cities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Careers_Status",
                table: "Careers");
        }
    }
}
