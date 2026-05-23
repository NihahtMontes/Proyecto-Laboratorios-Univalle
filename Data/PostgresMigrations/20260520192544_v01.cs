using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.PostgresMigrations
{
    /// <inheritdoc />
    public partial class v01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManagementType",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Managements\" " +
                "SET \"Semester\" = 1, " +
                "\"Code\" = 'CORR-' || \"Year\" || '-1', " +
                "\"Description\" = COALESCE(NULLIF(\"Description\", ''), 'Gestion correctiva semestral migrada desde contenedor anual.') " +
                "WHERE \"Type\" = 1 AND \"Semester\" = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ManagementId_ManagementType_Scope_IsRead",
                table: "Notifications",
                columns: new[] { "ManagementId", "ManagementType", "Scope", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Managements_Type_Status_Year_Semester",
                table: "Managements",
                columns: new[] { "Type", "Status", "Year", "Semester" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Managements_ManagementId",
                table: "Notifications",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Managements_ManagementId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ManagementId_ManagementType_Scope_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Managements_Type_Status_Year_Semester",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ManagementType",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Notifications");
        }
    }
}
