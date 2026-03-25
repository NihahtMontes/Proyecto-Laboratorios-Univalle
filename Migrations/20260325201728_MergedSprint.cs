using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class MergedSprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Managements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Managements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Managements_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Managements_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ManagementPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    MaintenanceId = table.Column<int>(type: "int", nullable: true),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: true),
                    Responsible = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlanStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagementPlans_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagementPlans_Maintenances_MaintenanceId",
                        column: x => x.MaintenanceId,
                        principalTable: "Maintenances",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagementPlans_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManagementPlans_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManagementPlans_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_CreatedById",
                table: "ManagementPlans",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_EquipmentUnitId",
                table: "ManagementPlans",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_MaintenanceId",
                table: "ManagementPlans",
                column: "MaintenanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ManagementId",
                table: "ManagementPlans",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_ModifiedById",
                table: "ManagementPlans",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Managements_CreatedById",
                table: "Managements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Managements_ModifiedById",
                table: "Managements",
                column: "ModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagementPlans");

            migrationBuilder.DropTable(
                name: "Managements");
        }
    }
}
