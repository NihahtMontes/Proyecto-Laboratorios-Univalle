using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class FilteredUniqueIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityCard",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Laboratories_Code",
                table: "Laboratories");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityCard",
                table: "Users",
                column: "IdentityCard",
                unique: true,
                filter: "[Status] != 2");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_Code",
                table: "Laboratories",
                column: "Code",
                unique: true,
                filter: "[Status] != 2");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments",
                column: "InventoryNumber",
                unique: true,
                filter: "[CurrentStatus] != 99");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IdentityCard",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Laboratories_Code",
                table: "Laboratories");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdentityCard",
                table: "Users",
                column: "IdentityCard",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_Code",
                table: "Laboratories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_InventoryNumber",
                table: "Equipments",
                column: "InventoryNumber",
                unique: true);
        }
    }
}
