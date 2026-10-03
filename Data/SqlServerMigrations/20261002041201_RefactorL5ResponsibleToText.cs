using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class RefactorL5ResponsibleToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L5Incidents_Users_ResponsibleUserId",
                table: "L5Incidents");

            migrationBuilder.DropIndex(
                name: "IX_L5Incidents_ResponsibleUserId",
                table: "L5Incidents");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "L5Incidents");

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleName",
                table: "L5Incidents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleType",
                table: "L5Incidents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsibleName",
                table: "L5Incidents");

            migrationBuilder.DropColumn(
                name: "ResponsibleType",
                table: "L5Incidents");

            migrationBuilder.AddColumn<int>(
                name: "ResponsibleUserId",
                table: "L5Incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_L5Incidents_ResponsibleUserId",
                table: "L5Incidents",
                column: "ResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_Users_ResponsibleUserId",
                table: "L5Incidents",
                column: "ResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
