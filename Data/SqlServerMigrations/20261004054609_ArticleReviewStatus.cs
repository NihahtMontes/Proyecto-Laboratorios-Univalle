using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class ArticleReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReviewStatus",
                table: "Articles",
                column: "ReviewStatus");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Articles_ReviewStatus",
                table: "Articles",
                sql: "[ReviewStatus] IN (0, 9)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_ReviewStatus",
                table: "Articles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Articles_ReviewStatus",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "Articles");
        }
    }
}
