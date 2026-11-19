using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class PlanningAssignmentsAndConsumables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginLaboratoryId",
                table: "DepartureItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanningAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanningRequirementId = table.Column<int>(type: "int", nullable: false),
                    LaboratoryId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningAssignments", x => x.Id);
                    table.CheckConstraint("CK_PlanningAssignments_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_PlanningAssignments_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningAssignments_PlanningRequirements_PlanningRequirementId",
                        column: x => x.PlanningRequirementId,
                        principalTable: "PlanningRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartureItems_OriginLaboratoryId",
                table: "DepartureItems",
                column: "OriginLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningAssignments_LaboratoryId",
                table: "PlanningAssignments",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningAssignments_PlanningRequirementId_LaboratoryId",
                table: "PlanningAssignments",
                columns: new[] { "PlanningRequirementId", "LaboratoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartureItems_Laboratories_OriginLaboratoryId",
                table: "DepartureItems",
                column: "OriginLaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartureItems_Laboratories_OriginLaboratoryId",
                table: "DepartureItems");

            migrationBuilder.DropTable(
                name: "PlanningAssignments");

            migrationBuilder.DropIndex(
                name: "IX_DepartureItems_OriginLaboratoryId",
                table: "DepartureItems");

            migrationBuilder.DropColumn(
                name: "OriginLaboratoryId",
                table: "DepartureItems");
        }
    }
}
