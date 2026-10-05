using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddPracticeName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Practices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Las prácticas existentes toman su nombre de la descripción, el número o el código.
            migrationBuilder.Sql(@"
UPDATE [Practices]
SET [Name] = LEFT(COALESCE(NULLIF(LTRIM(RTRIM(CAST([Description] AS nvarchar(2000)))), N''),
                           NULLIF(LTRIM(RTRIM([Number])), N''),
                           [Code]), 200)
WHERE [Name] = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Practices");
        }
    }
}
