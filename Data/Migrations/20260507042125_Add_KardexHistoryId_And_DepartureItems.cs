using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_KardexHistoryId_And_DepartureItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KardexHistoryId",
                table: "ManagementPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DepartureItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartureId = table.Column<int>(type: "int", nullable: false),
                    EquipmentUnitId = table.Column<int>(type: "int", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartureItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartureItems_Departures_DepartureId",
                        column: x => x.DepartureId,
                        principalTable: "Departures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartureItems_EquipmentUnits_EquipmentUnitId",
                        column: x => x.EquipmentUnitId,
                        principalTable: "EquipmentUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartureItems_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DepartureItems_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementPlans_KardexHistoryId",
                table: "ManagementPlans",
                column: "KardexHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_CreatedById",
                table: "DepartureItems",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_DepartureId",
                table: "DepartureItems",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_EquipmentUnitId",
                table: "DepartureItems",
                column: "EquipmentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_ModifiedById",
                table: "DepartureItems",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_EquipmentStateHistories_KardexHistoryId",
                table: "ManagementPlans",
                column: "KardexHistoryId",
                principalTable: "EquipmentStateHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_EquipmentStateHistories_KardexHistoryId",
                table: "ManagementPlans");

            migrationBuilder.DropTable(
                name: "DepartureItems");

            migrationBuilder.DropIndex(
                name: "IX_ManagementPlans_KardexHistoryId",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "KardexHistoryId",
                table: "ManagementPlans");
        }
    }
}
