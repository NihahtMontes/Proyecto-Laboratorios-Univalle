using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class FixUsersPersons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "People",
                newName: "Category");

            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "People",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "People");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "People",
                newName: "Type");
        }
    }
}
