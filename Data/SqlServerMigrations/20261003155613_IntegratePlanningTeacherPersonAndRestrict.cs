using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class IntegratePlanningTeacherPersonAndRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId",
                table: "L3RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Groups_GroupId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Laboratories_LaboratoryId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Subjects_SubjectId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Users_TeacherId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_EquipmentUnits_EquipmentUnitId",
                table: "L5Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_L3Requests_L3RequestId",
                table: "L5Incidents");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "L3Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "L3Requests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndDate",
                table: "L3Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherPersonId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_TeacherPersonId",
                table: "L3Requests",
                column: "TeacherPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId",
                table: "L3RequestDetails",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Groups_GroupId",
                table: "L3Requests",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Laboratories_LaboratoryId",
                table: "L3Requests",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_People_TeacherPersonId",
                table: "L3Requests",
                column: "TeacherPersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Subjects_SubjectId",
                table: "L3Requests",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Users_TeacherId",
                table: "L3Requests",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_EquipmentUnits_EquipmentUnitId",
                table: "L5Incidents",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_L3Requests_L3RequestId",
                table: "L5Incidents",
                column: "L3RequestId",
                principalTable: "L3Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId",
                table: "L3RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Groups_GroupId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Laboratories_LaboratoryId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_People_TeacherPersonId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Subjects_SubjectId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Users_TeacherId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_EquipmentUnits_EquipmentUnitId",
                table: "L5Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_L3Requests_L3RequestId",
                table: "L5Incidents");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_TeacherPersonId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "ScheduledEndDate",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "TeacherPersonId",
                table: "L3Requests");

            migrationBuilder.AlterColumn<int>(
                name: "TeacherId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId",
                table: "L3RequestDetails",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Groups_GroupId",
                table: "L3Requests",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Laboratories_LaboratoryId",
                table: "L3Requests",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Subjects_SubjectId",
                table: "L3Requests",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Users_TeacherId",
                table: "L3Requests",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_EquipmentUnits_EquipmentUnitId",
                table: "L5Incidents",
                column: "EquipmentUnitId",
                principalTable: "EquipmentUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_L3Requests_L3RequestId",
                table: "L5Incidents",
                column: "L3RequestId",
                principalTable: "L3Requests",
                principalColumn: "Id");
        }
    }
}
