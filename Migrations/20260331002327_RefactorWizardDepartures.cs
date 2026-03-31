using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class RefactorWizardDepartures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.AddColumn<int>(
                name: "AcquisitionRequestId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPhase",
                table: "ManagementPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentState",
                table: "ManagementPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartureId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: false),
                    BorrowerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartureObservations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReturnObservations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departures_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departures_People_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departures_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Departures_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_AcquisitionRequestId",
                table: "ManagementPlans",
                column: "AcquisitionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_DepartureId",
                table: "ManagementPlans",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_RequestId",
                table: "ManagementPlans",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_VerificationId",
                table: "ManagementPlans",
                column: "VerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_BorrowerId",
                table: "Departures",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_CreatedById",
                table: "Departures",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_EquipmentUnitId",
                table: "Departures",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_ModifiedById",
                table: "Departures",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Departures_DepartureId",
                table: "ManagementPlans",
                column: "DepartureId",
                principalTable: "Departures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Requests_AcquisitionRequestId",
                table: "ManagementPlans",
                column: "AcquisitionRequestId",
                principalTable: "Requests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Requests_RequestId",
                table: "ManagementPlans",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Verifications_VerificationId",
                table: "ManagementPlans",
                column: "VerificationId",
                principalTable: "Verifications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Departures_DepartureId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Requests_AcquisitionRequestId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Requests_RequestId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Verifications_VerificationId",
                table: "ManagementPlans");

            migrationBuilder.DropTable(
                name: "Departures");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_AcquisitionRequestId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_DepartureId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_RequestId",
                table: "ManagementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_VerificationId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "AcquisitionRequestId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "CurrentState",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "DepartureId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "VerificationId",
                table: "ManagementPlans");

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BorrowerId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartureObservations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoanDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnObservations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_People_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Loans_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Loans_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BorrowerId",
                table: "Loans",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CreatedById",
                table: "Loans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_EquipmentUnitId",
                table: "Loans",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ModifiedById",
                table: "Loans",
                column: "ModifiedById");
        }
    }
}
