using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryIdToRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "LaboratoryId",
                table: "Requests",
                type: "int",
                nullable: true);

            // Step 2: Populate LaboratoryId from Equipment table for existing records
            migrationBuilder.Sql(@"
                UPDATE r
                SET r.LaboratoryId = e.LaboratoryId
                FROM Requests r
                INNER JOIN Equipments e ON r.EquipmentId = e.Id
                WHERE r.LaboratoryId IS NULL
            ");

            // Step 3: Make column NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "LaboratoryId",
                table: "Requests",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            // Step 4: Create index
            migrationBuilder.CreateIndex(
                name: "IX_Requests_LaboratoryId",
                table: "Requests",
                column: "LaboratoryId");

            // Step 5: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_LaboratoryId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "LaboratoryId",
                table: "Requests");
        }
    }
}
