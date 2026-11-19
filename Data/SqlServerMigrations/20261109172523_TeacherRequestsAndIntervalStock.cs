using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class TeacherRequestsAndIntervalStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Plannings_Status",
                table: "Plannings");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanningReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanningId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningReviews", x => x.Id);
                    table.CheckConstraint("CK_PlanningReviews_Action", "[Action] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_PlanningReviews_Plannings_PlanningId",
                        column: x => x.PlanningId,
                        principalTable: "Plannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningReviewLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanningReviewId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    PreviousQuantity = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    NewQuantity = table.Column<decimal>(type: "decimal(12,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningReviewLines", x => x.Id);
                    table.CheckConstraint("CK_PlanningReviewLines_Quantities", "([PreviousQuantity] IS NULL OR [PreviousQuantity] >= 0) AND ([NewQuantity] IS NULL OR [NewQuantity] >= 0) AND ([PreviousQuantity] IS NOT NULL OR [NewQuantity] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_PlanningReviewLines_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningReviewLines_PlanningReviews_PlanningReviewId",
                        column: x => x.PlanningReviewId,
                        principalTable: "PlanningReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonId",
                table: "Users",
                column: "PersonId",
                unique: true,
                filter: "[PersonId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3, 4, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_TeacherPerson",
                table: "Users",
                sql: "[Role] <> 4 OR [PersonId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Status",
                table: "Plannings",
                sql: "[Status] IN (N'Planificada', N'Aprobada', N'Finalizada', N'Cancelada', N'Reprogramada', N'PendienteReposicion', N'Repuesta', N'Solicitada', N'Observada', N'Rechazada')");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningReviewLines_ArticleId",
                table: "PlanningReviewLines",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningReviewLines_PlanningReviewId_ArticleId",
                table: "PlanningReviewLines",
                columns: new[] { "PlanningReviewId", "ArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningReviews_PlanningId_CreatedDate",
                table: "PlanningReviews",
                columns: new[] { "PlanningId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningReviews_UserId",
                table: "PlanningReviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_People_PersonId",
                table: "Users",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Rol Identity del docente (idempotente).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'TEACHER')
    INSERT INTO [Roles] ([Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'Teacher', N'TEACHER', CONVERT(nvarchar(36), NEWID()));");

            // Stock por horario: hasta ahora la salida L-3 descontaba la existencia y la devolución la reponía.
            // Desde esta versión ninguna de las dos la toca, así que lo entregado y aún no devuelto (ni cubierto por
            // un incidente L-5) en salidas abiertas se suma de vuelta a la existencia del laboratorio de origen.
            migrationBuilder.Sql(@"
;WITH pending AS (
    SELECT d.OriginLaboratoryId AS LaboratoryId, i.ArticleId, d.PlanningId,
           SUM(ISNULL(i.Quantity, 0) - ISNULL(i.ReturnedQuantity, 0)) AS Qty
    FROM [DepartureItems] i
    JOIN [Departures] d ON d.Id = i.DepartureId
    WHERE i.ArticleId IS NOT NULL AND i.EquipmentUnitId IS NULL
      AND d.OriginLaboratoryId IS NOT NULL AND d.PlanningId IS NOT NULL
      AND d.Status IN (0, 2)
    GROUP BY d.OriginLaboratoryId, i.ArticleId, d.PlanningId
), net AS (
    SELECT p.LaboratoryId, p.ArticleId,
           SUM(CASE WHEN p.Qty - ISNULL(c.Covered, 0) > 0 THEN p.Qty - ISNULL(c.Covered, 0) ELSE 0 END) AS Qty
    FROM pending p
    OUTER APPLY (SELECT SUM(l.AffectedQuantity) AS Covered FROM [L5Incidents] l
                 WHERE l.PlanningId = p.PlanningId AND l.ArticleId = p.ArticleId AND l.IsDeleted = 0) c
    GROUP BY p.LaboratoryId, p.ArticleId
)
UPDATE s SET s.AvailableQuantity = s.AvailableQuantity + n.Qty, s.UpdatedOn = CAST(GETDATE() AS date)
FROM [ArticleStocks] s JOIN net n ON n.LaboratoryId = s.LaboratoryId AND n.ArticleId = s.ArticleId
WHERE n.Qty > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE r FROM [Roles] r WHERE r.[NormalizedName] = N'TEACHER'
  AND NOT EXISTS (SELECT 1 FROM [UserRoles] ur WHERE ur.RoleId = r.Id);");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_People_PersonId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "PlanningReviewLines");

            migrationBuilder.DropTable(
                name: "PlanningReviews");

            migrationBuilder.DropIndex(
                name: "IX_Users_PersonId",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_TeacherPerson",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Plannings_Status",
                table: "Plannings");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Users");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN (1, 2, 3, 99)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Status",
                table: "Plannings",
                sql: "[Status] IN (N'Planificada', N'Aprobada', N'Finalizada', N'Cancelada', N'Reprogramada', N'PendienteReposicion', N'Repuesta')");
        }
    }
}
