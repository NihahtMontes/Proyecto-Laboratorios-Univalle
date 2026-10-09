using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    /// <summary>
    /// Fechas de clases de la gestión (numeran las semanas académicas), estado previo a la reposición de día
    /// y FK de requerimientos en Restrict (sin borrados en cascada).
    /// </summary>
    public partial class AcademicCalendarClassDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusBeforeRecovery",
                table: "Plannings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassesEndDate",
                table: "Managements",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassesStartDate",
                table: "Managements",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Managements_ClassDates",
                table: "Managements",
                sql: "[ClassesStartDate] IS NULL OR [ClassesEndDate] IS NULL OR [ClassesEndDate] >= [ClassesStartDate]");

            // H1: la FK heredada de L3RequestDetails quedó en CASCADE; el modelo exige Restrict (sin borrados en cascada).
            migrationBuilder.DropForeignKey(
                name: "FK_PlanningRequirements_Plannings_PlanningId",
                table: "PlanningRequirements");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanningRequirements_Plannings_PlanningId",
                table: "PlanningRequirements",
                column: "PlanningId",
                principalTable: "Plannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanningRequirements_Plannings_PlanningId",
                table: "PlanningRequirements");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanningRequirements_Plannings_PlanningId",
                table: "PlanningRequirements",
                column: "PlanningId",
                principalTable: "Plannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Managements_ClassDates",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "StatusBeforeRecovery",
                table: "Plannings");

            migrationBuilder.DropColumn(
                name: "ClassesEndDate",
                table: "Managements");

            migrationBuilder.DropColumn(
                name: "ClassesStartDate",
                table: "Managements");
        }
    }
}
