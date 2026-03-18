using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class Migracion4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionPercentage",
                table: "Maintenances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Step1_Cleaning",
                table: "Maintenances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Step2_Calibration",
                table: "Maintenances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Step3_Testing",
                table: "Maintenances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Step4_FinalReview",
                table: "Maintenances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionPercentage",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "Step1_Cleaning",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "Step2_Calibration",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "Step3_Testing",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "Step4_FinalReview",
                table: "Maintenances");
        }
    }
}
