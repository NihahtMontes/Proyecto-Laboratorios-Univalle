using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class AddCostDetailsAndRequestType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvestmentCode",
                table: "Requests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "MaintenanceId",
                table: "CostDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "CostDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostDetails_RequestId",
                table: "CostDetails",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_CostDetails_Requests_RequestId",
                table: "CostDetails",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostDetails_Requests_RequestId",
                table: "CostDetails");

            migrationBuilder.DropIndex(
                name: "IX_CostDetails_RequestId",
                table: "CostDetails");

            migrationBuilder.DropColumn(
                name: "InvestmentCode",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "CostDetails");

            migrationBuilder.AlterColumn<int>(
                name: "MaintenanceId",
                table: "CostDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
