using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class MasterDataAndPendingValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "People",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CampusId",
                table: "Laboratories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Campuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.Id);
                    table.CheckConstraint("CK_Campuses_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_Campuses_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications",
                sql: "[ObservedEquipmentStatus] IS NULL OR [ObservedEquipmentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 98, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications",
                sql: "[PhysicalCondition] BETWEEN 1 AND 5 OR [PhysicalCondition] = 9");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true,
                filter: "[Code] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests",
                sql: "[Priority] BETWEEN 0 AND 3 OR [Priority] = 9");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 98, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments",
                sql: "[Role] IN (1, 2, 3, 4, 5, 6, 7, 8, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances",
                sql: "[ServiceType] IN (0, 1, 9)");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_CampusId",
                table: "Laboratories",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_Code",
                table: "Faculties",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL AND [Code] <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits",
                sql: "[CurrentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 98, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits",
                sql: "[PhysicalCondition] IS NULL OR [PhysicalCondition] BETWEEN 1 AND 5 OR [PhysicalCondition] = 9");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 6, 10, 98, 99)");

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_CityId",
                table: "Campuses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_Code",
                table: "Campuses",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratories_Campuses_CampusId",
                table: "Laboratories",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Laboratories_Campuses_CampusId",
                table: "Laboratories");

            migrationBuilder.DropTable(
                name: "Campuses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Code",
                table: "Subjects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances");

            migrationBuilder.DropIndex(
                name: "IX_Laboratories_CampusId",
                table: "Laboratories");

            migrationBuilder.DropIndex(
                name: "IX_Faculties_Code",
                table: "Faculties");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "Laboratories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications",
                sql: "[ObservedEquipmentStatus] IS NULL OR [ObservedEquipmentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_PhysicalCondition",
                table: "Verifications",
                sql: "[PhysicalCondition] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Priority",
                table: "Requests",
                sql: "[Priority] BETWEEN 0 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Requests_Status",
                table: "Requests",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments",
                sql: "[Role] IN (1, 2, 3, 4, 5, 6, 7, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_ServiceType",
                table: "Maintenances",
                sql: "[ServiceType] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_CurrentStatus",
                table: "EquipmentUnits",
                sql: "[CurrentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentUnits_PhysicalCondition",
                table: "EquipmentUnits",
                sql: "[PhysicalCondition] IS NULL OR [PhysicalCondition] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentStateHistories_Status",
                table: "EquipmentStateHistories",
                sql: "[Status] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");
        }
    }
}
