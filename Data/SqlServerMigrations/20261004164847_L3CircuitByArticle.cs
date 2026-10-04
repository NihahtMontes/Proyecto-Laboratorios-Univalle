using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class L3CircuitByArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "L5Incidents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "L5Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TeacherPersonId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SubjectId",
                table: "L3Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "L3Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "L3Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "AllLaboratories",
                table: "L3Requests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CampusId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CareerId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "L3Requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseGroupId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "L3Requests",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "L3Requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PracticeNumber",
                table: "L3Requests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PracticeScheduleId",
                table: "L3Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "L3Requests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "L3Requests",
                type: "time",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "L3RequestDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "L3RequestDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "L3RequestDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "L3RequestDetails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedQuantity",
                table: "L3RequestDetails",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Departures",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedReturnNote",
                table: "Departures",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "L3RequestId",
                table: "Departures",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ReturnedQuantity",
                table: "DepartureItems",
                type: "decimal(12,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // CK_DepartureItems_Quantity depende de la columna: SQL Server no permite cambiar su tipo sin quitarlo antes.
            migrationBuilder.DropCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "DepartureItems",
                type: "decimal(12,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems",
                sql: "[Quantity] IS NULL OR [Quantity] > 0");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DepartureItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementUnitId",
                table: "DepartureItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DepartureReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartureId = table.Column<int>(type: "int", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "date", nullable: true),
                    ResponsiblePersonId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartureReturns", x => x.Id);
                    table.CheckConstraint("CK_DepartureReturns_Status", "[Status] IN (0, 1, 2, 9)");
                    table.ForeignKey(
                        name: "FK_DepartureReturns_Departures_DepartureId",
                        column: x => x.DepartureId,
                        principalTable: "Departures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartureReturns_People_ResponsiblePersonId",
                        column: x => x.ResponsiblePersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepartureReturnItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartureReturnId = table.Column<int>(type: "int", nullable: false),
                    DepartureItemId = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartureReturnItems", x => x.Id);
                    table.CheckConstraint("CK_DepartureReturnItems_Quantity", "[ReturnedQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_DepartureReturnItems_DepartureItems_DepartureItemId",
                        column: x => x.DepartureItemId,
                        principalTable: "DepartureItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartureReturnItems_DepartureReturns_DepartureReturnId",
                        column: x => x.DepartureReturnId,
                        principalTable: "DepartureReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_L5Incidents_ArticleId",
                table: "L5Incidents",
                column: "ArticleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L5Incidents_Target",
                table: "L5Incidents",
                sql: "[EquipmentUnitId] IS NOT NULL OR [ArticleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_CampusId",
                table: "L3Requests",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_CareerId",
                table: "L3Requests",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_Code",
                table: "L3Requests",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_CourseGroupId",
                table: "L3Requests",
                column: "CourseGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_ManagementId",
                table: "L3Requests",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_L3Requests_PracticeScheduleId",
                table: "L3Requests",
                column: "PracticeScheduleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L3Requests_Laboratory",
                table: "L3Requests",
                sql: "[AllLaboratories] = 1 OR [LaboratoryId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L3Requests_Status",
                table: "L3Requests",
                sql: "[Status] IN (N'Planificado', N'Aprobado', N'Finalizado', N'Cancelado')");

            migrationBuilder.CreateIndex(
                name: "IX_L3RequestDetails_ArticleId",
                table: "L3RequestDetails",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_L3RequestDetails_Code",
                table: "L3RequestDetails",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L3RequestDetails_RequestedQuantity",
                table: "L3RequestDetails",
                sql: "[RequestedQuantity] IS NULL OR [RequestedQuantity] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_Code",
                table: "Departures",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_L3RequestId",
                table: "Departures",
                column: "L3RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_Code",
                table: "DepartureItems",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_MeasurementUnitId",
                table: "DepartureItems",
                column: "MeasurementUnitId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DepartureItems_ReturnedQuantity",
                table: "DepartureItems",
                sql: "[ReturnedQuantity] IS NULL OR ([ReturnedQuantity] >= 0 AND ([Quantity] IS NULL OR [ReturnedQuantity] <= [Quantity]))");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturnItems_Code",
                table: "DepartureReturnItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturnItems_DepartureItemId",
                table: "DepartureReturnItems",
                column: "DepartureItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturnItems_DepartureReturnId",
                table: "DepartureReturnItems",
                column: "DepartureReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturns_Code",
                table: "DepartureReturns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturns_DepartureId",
                table: "DepartureReturns",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureReturns_ResponsiblePersonId",
                table: "DepartureReturns",
                column: "ResponsiblePersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartureItems_MeasurementUnits_MeasurementUnitId",
                table: "DepartureItems",
                column: "MeasurementUnitId",
                principalTable: "MeasurementUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_L3Requests_L3RequestId",
                table: "Departures",
                column: "L3RequestId",
                principalTable: "L3Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3RequestDetails_Articles_ArticleId",
                table: "L3RequestDetails",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Campuses_CampusId",
                table: "L3Requests",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Careers_CareerId",
                table: "L3Requests",
                column: "CareerId",
                principalTable: "Careers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_CourseGroups_CourseGroupId",
                table: "L3Requests",
                column: "CourseGroupId",
                principalTable: "CourseGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Managements_ManagementId",
                table: "L3Requests",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_PracticeSchedules_PracticeScheduleId",
                table: "L3Requests",
                column: "PracticeScheduleId",
                principalTable: "PracticeSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_Articles_ArticleId",
                table: "L5Incidents",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartureItems_MeasurementUnits_MeasurementUnitId",
                table: "DepartureItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Departures_L3Requests_L3RequestId",
                table: "Departures");

            migrationBuilder.DropForeignKey(
                name: "FK_L3RequestDetails_Articles_ArticleId",
                table: "L3RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Campuses_CampusId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Careers_CareerId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_CourseGroups_CourseGroupId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_Managements_ManagementId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L3Requests_PracticeSchedules_PracticeScheduleId",
                table: "L3Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_Articles_ArticleId",
                table: "L5Incidents");

            migrationBuilder.DropTable(
                name: "DepartureReturnItems");

            migrationBuilder.DropTable(
                name: "DepartureReturns");

            migrationBuilder.DropIndex(
                name: "IX_L5Incidents_ArticleId",
                table: "L5Incidents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_L5Incidents_Target",
                table: "L5Incidents");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_CampusId",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_CareerId",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_Code",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_CourseGroupId",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_ManagementId",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3Requests_PracticeScheduleId",
                table: "L3Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_L3Requests_Laboratory",
                table: "L3Requests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_L3Requests_Status",
                table: "L3Requests");

            migrationBuilder.DropIndex(
                name: "IX_L3RequestDetails_ArticleId",
                table: "L3RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_L3RequestDetails_Code",
                table: "L3RequestDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_L3RequestDetails_RequestedQuantity",
                table: "L3RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_Departures_Code",
                table: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_Departures_L3RequestId",
                table: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_DepartureItems_Code",
                table: "DepartureItems");

            migrationBuilder.DropIndex(
                name: "IX_DepartureItems_MeasurementUnitId",
                table: "DepartureItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DepartureItems_ReturnedQuantity",
                table: "DepartureItems");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "L5Incidents");

            migrationBuilder.DropColumn(
                name: "AllLaboratories",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "CareerId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "CourseGroupId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "PracticeNumber",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "PracticeScheduleId",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "L3Requests");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "L3RequestDetails");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "L3RequestDetails");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "L3RequestDetails");

            migrationBuilder.DropColumn(
                name: "RequestedQuantity",
                table: "L3RequestDetails");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "EstimatedReturnNote",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "L3RequestId",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DepartureItems");

            migrationBuilder.DropColumn(
                name: "MeasurementUnitId",
                table: "DepartureItems");

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "L5Incidents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TeacherPersonId",
                table: "L3Requests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SubjectId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "L3Requests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EquipmentUnitId",
                table: "L3RequestDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ReturnedQuantity",
                table: "DepartureItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldNullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "DepartureItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DepartureItems_Quantity",
                table: "DepartureItems",
                sql: "[Quantity] IS NULL OR [Quantity] > 0");
        }
    }
}
