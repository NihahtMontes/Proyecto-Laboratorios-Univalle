using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class WidenPlanningReviewActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlanningReviews_Action",
                table: "PlanningReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlanningReviews_Action",
                table: "PlanningReviews",
                sql: "[Action] BETWEEN 1 AND 11");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlanningReviews_Action",
                table: "PlanningReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlanningReviews_Action",
                table: "PlanningReviews",
                sql: "[Action] BETWEEN 1 AND 7");
        }
    }
}
