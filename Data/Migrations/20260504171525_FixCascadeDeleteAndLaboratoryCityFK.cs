using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteAndLaboratoryCityFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Managements_ManagementId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratories_CityId",
                table: "Laboratories",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratories_Cities_CityId",
                table: "Laboratories",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Managements_ManagementId",
                table: "ManagementPlans",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Laboratories_Cities_CityId",
                table: "Laboratories");

            migrationBuilder.DropForeignKey(
                name: "FK_ManagementPlans_Managements_ManagementId",
                table: "ManagementPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Laboratories_CityId",
                table: "Laboratories");

            migrationBuilder.AddForeignKey(
                name: "FK_ManagementPlans_Managements_ManagementId",
                table: "ManagementPlans",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Laboratories_LaboratoryId",
                table: "Requests",
                column: "LaboratoryId",
                principalTable: "Laboratories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
