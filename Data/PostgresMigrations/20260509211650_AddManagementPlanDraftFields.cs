using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.PostgresMigrations
{
    /// <inheritdoc />
    public partial class AddManagementPlanDraftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DraftPhase",
                table: "ManagementPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DraftSavedAt",
                table: "ManagementPlans",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftSummary",
                table: "ManagementPlans",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "ManagementPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftPhase",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "DraftSavedAt",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "DraftSummary",
                table: "ManagementPlans");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "ManagementPlans");
        }
    }
}
