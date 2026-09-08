using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddHistoricalSourceKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "Verifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "Requests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalSourceKey",
                table: "Maintenances",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verifications_HistoricalSourceKey",
                table: "Verifications",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_HistoricalSourceKey",
                table: "Requests",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenances_HistoricalSourceKey",
                table: "Maintenances",
                column: "HistoricalSourceKey",
                unique: true,
                filter: "[HistoricalSourceKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Verifications_HistoricalSourceKey",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Requests_HistoricalSourceKey",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Maintenances_HistoricalSourceKey",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "HistoricalSourceKey",
                table: "Maintenances");
        }
    }
}
