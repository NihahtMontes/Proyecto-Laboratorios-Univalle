using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddPracticeClosureRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3, 99)");

            // Rol de Identity del encargado de cierre de prácticas (SynchronizeManagedRoleAsync lo exige).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'PRACTICECLOSURE')
    INSERT INTO [Roles] ([Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'PracticeClosure', N'PRACTICECLOSURE', CONVERT(nvarchar(36), NEWID()));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo si ningún usuario conserva el rol (la restricción anterior no lo admitiría).
            migrationBuilder.Sql(@"
DELETE r FROM [Roles] r
WHERE r.[NormalizedName] = N'PRACTICECLOSURE'
  AND NOT EXISTS (SELECT 1 FROM [UserRoles] ur WHERE ur.[RoleId] = r.[Id]);");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 99)");
        }
    }
}
