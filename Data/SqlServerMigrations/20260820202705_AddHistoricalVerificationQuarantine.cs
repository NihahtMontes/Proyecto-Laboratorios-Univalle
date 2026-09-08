using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddHistoricalVerificationQuarantine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricalVerificationQuarantines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceSheet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceRow = table.Column<int>(type: "int", nullable: false),
                    InventoryRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalConditionRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FindingRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EquipmentNameRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Column6Raw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Column7Raw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalVerificationQuarantines", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalVerificationQuarantines_SourceKey",
                table: "HistoricalVerificationQuarantines",
                column: "SourceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricalVerificationQuarantines");
        }
    }
}
