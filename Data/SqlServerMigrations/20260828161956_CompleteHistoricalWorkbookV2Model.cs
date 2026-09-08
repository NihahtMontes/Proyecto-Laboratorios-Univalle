using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class CompleteHistoricalWorkbookV2Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Laboratories_LaboratoryId",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans");

            migrationBuilder.AddColumn<int>(
                name: "ObservedEquipmentStatus",
                table: "Verifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ServiceType",
                table: "MaintenancePlans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "LaboratorySnapshot",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "BlockSnapshot",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                table: "MaintenancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceType",
                table: "MaintenancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "MaintenancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanCode",
                table: "MaintenancePlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDate",
                table: "MaintenancePlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsiblePersonId",
                table: "MaintenancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MaintenancePlans",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EstimatedReturnDate",
                table: "Departures",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "DepartureItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "DepartureItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications",
                sql: "[ObservedEquipmentStatus] IS NULL OR [ObservedEquipmentStatus] IN (0, 1, 2, 3, 4, 5, 6, 10, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments",
                sql: "[Role] IN (1, 2, 3, 4, 5, 6, 7, 99)");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_HistoricalSourceKey",
                table: "MaintenancePlans",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_ImportBatchId",
                table: "MaintenancePlans",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_ManagementId",
                table: "MaintenancePlans",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_PlanCode",
                table: "MaintenancePlans",
                column: "PlanCode",
                unique: true,
                filter: "[PlanCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePlans_ResponsiblePersonId",
                table: "MaintenancePlans",
                column: "ResponsiblePersonId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans",
                sql: "[MaintenanceType] IS NULL OR [MaintenanceType] IN (1, 2, 3, 4, 5, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans",
                sql: "[ServiceType] IS NULL OR [ServiceType] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_Status",
                table: "MaintenancePlans",
                sql: "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems",
                sql: "[Quantity] IS NULL OR [Quantity] > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_ImportBatches_ImportBatchId",
                table: "MaintenancePlans",
                column: "ImportBatchId",
                principalTable: "ImportBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Laboratories_LaboratoryId",
                table: "MaintenancePlans",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Managements_ManagementId",
                table: "MaintenancePlans",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_People_ResponsiblePersonId",
                table: "MaintenancePlans",
                column: "ResponsiblePersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_ImportBatches_ImportBatchId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Laboratories_LaboratoryId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_Managements_ManagementId",
                table: "MaintenancePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenancePlans_People_ResponsiblePersonId",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Verifications_ObservedEquipmentStatus",
                table: "Verifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_HistoricalSourceKey",
                table: "MaintenancePlans");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_ImportBatchId",
                table: "MaintenancePlans");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_ManagementId",
                table: "MaintenancePlans");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_PlanCode",
                table: "MaintenancePlans");

            migrationBuilder.DropIndex(
                name: "IX_MaintenancePlans_ResponsiblePersonId",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_MaintenanceType",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenancePlans_Status",
                table: "MaintenancePlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems");

            migrationBuilder.DropColumn(
                name: "ObservedEquipmentStatus",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "MaintenanceType",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "PlanCode",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "PlannedDate",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "ResponsiblePersonId",
                table: "MaintenancePlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MaintenancePlans");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceType",
                table: "MaintenancePlans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LaboratorySnapshot",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlockSnapshot",
                table: "MaintenancePlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EstimatedReturnDate",
                table: "Departures",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "DepartureItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "DepartureItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PersonRoleAssignments_Role",
                table: "PersonRoleAssignments",
                sql: "[Role] IN (1, 2, 3, 4, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenancePlans_ServiceType",
                table: "MaintenancePlans",
                sql: "[ServiceType] IN (0, 1)");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenancePlans_Laboratories_LaboratoryId",
                table: "MaintenancePlans",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id");
        }
    }
}
