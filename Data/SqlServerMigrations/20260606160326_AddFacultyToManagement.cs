using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddFacultyToManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "Managements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managements_FacultyId",
                table: "Managements",
                column: "FacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Managements_Faculties_FacultyId",
                table: "Managements",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Managements_Faculties_FacultyId",
                table: "Managements");

            migrationBuilder.DropIndex(
                name: "IX_Managements_FacultyId",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "Managements");
        }
    }
}
