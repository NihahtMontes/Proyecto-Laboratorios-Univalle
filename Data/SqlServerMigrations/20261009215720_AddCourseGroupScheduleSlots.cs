using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddCourseGroupScheduleSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseGroupScheduleSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseGroupId = table.Column<int>(type: "int", nullable: false),
                    Weekday = table.Column<int>(type: "int", nullable: false),
                    ClassPeriodId = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGroupScheduleSlots", x => x.Id);
                    table.CheckConstraint("CK_CourseGroupScheduleSlots_Weekday", "[Weekday] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_CourseGroupScheduleSlots_ClassPeriods_ClassPeriodId",
                        column: x => x.ClassPeriodId,
                        principalTable: "ClassPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGroupScheduleSlots_CourseGroups_CourseGroupId",
                        column: x => x.CourseGroupId,
                        principalTable: "CourseGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupScheduleSlots_ClassPeriodId",
                table: "CourseGroupScheduleSlots",
                column: "ClassPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupScheduleSlots_CourseGroupId_Weekday_ClassPeriodId",
                table: "CourseGroupScheduleSlots",
                columns: new[] { "CourseGroupId", "Weekday", "ClassPeriodId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseGroupScheduleSlots");
        }
    }
}
