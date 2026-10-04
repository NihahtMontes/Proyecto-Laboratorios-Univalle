using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AcademicAndArticleCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CareerId",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ArticleCategoryId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArticleType",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EquipmentId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementUnitId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArticleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleCategories", x => x.Id);
                    table.CheckConstraint("CK_ArticleCategories_Status", "[Status] BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "ArticleStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaboratoryId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "date", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleStocks", x => x.Id);
                    table.CheckConstraint("CK_ArticleStocks_Quantity", "[AvailableQuantity] >= 0");
                    table.ForeignKey(
                        name: "FK_ArticleStocks_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArticleStocks_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TeacherPersonId = table.Column<int>(type: "int", nullable: false),
                    LaboratoryId = table.Column<int>(type: "int", nullable: true),
                    Weekday = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    StudentCount = table.Column<int>(type: "int", nullable: true),
                    IsDemonstrative = table.Column<bool>(type: "bit", nullable: false),
                    ScalingFactor = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGroups", x => x.Id);
                    table.CheckConstraint("CK_CourseGroups_Status", "[Status] BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_CourseGroups_Times", "[StartTime] IS NULL OR [EndTime] IS NULL OR [EndTime] > [StartTime]");
                    table.CheckConstraint("CK_CourseGroups_Weekday", "[Weekday] IS NULL OR [Weekday] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_CourseGroups_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGroups_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGroups_People_TeacherPersonId",
                        column: x => x.TeacherPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGroups_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementUnits", x => x.Id);
                    table.CheckConstraint("CK_MeasurementUnits_Status", "[Status] BETWEEN 0 AND 2");
                });

            migrationBuilder.CreateTable(
                name: "Practices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CoverageStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CoverageMethod = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practices", x => x.Id);
                    table.CheckConstraint("CK_Practices_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_Practices_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracticeId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsScalable = table.Column<bool>(type: "bit", nullable: false),
                    ValidationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Observations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeRequirements", x => x.Id);
                    table.CheckConstraint("CK_PracticeRequirements_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_PracticeRequirements_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PracticeRequirements_Practices_PracticeId",
                        column: x => x.PracticeId,
                        principalTable: "Practices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CareerId_Name",
                table: "Subjects",
                columns: new[] { "CareerId", "Name" },
                unique: true,
                filter: "[CareerId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Subjects_Status",
                table: "Subjects",
                sql: "[Status] BETWEEN 0 AND 2");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleCategoryId",
                table: "Articles",
                column: "ArticleCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_EquipmentId",
                table: "Articles",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_MeasurementUnitId",
                table: "Articles",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "UX_Articles_Name_Active",
                table: "Articles",
                column: "Name",
                unique: true,
                filter: "[Status] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCategories_Code",
                table: "ArticleCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCategories_Name",
                table: "ArticleCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStocks_ArticleId",
                table: "ArticleStocks",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleStocks_LaboratoryId_ArticleId",
                table: "ArticleStocks",
                columns: new[] { "LaboratoryId", "ArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_Code",
                table: "CourseGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_LaboratoryId",
                table: "CourseGroups",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_ManagementId_SubjectId_Name",
                table: "CourseGroups",
                columns: new[] { "ManagementId", "SubjectId", "Name" },
                unique: true,
                filter: "[Status] <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_SubjectId",
                table: "CourseGroups",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_TeacherPersonId",
                table: "CourseGroups",
                column: "TeacherPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_Code",
                table: "MeasurementUnits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_Name",
                table: "MeasurementUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRequirements_ArticleId",
                table: "PracticeRequirements",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeRequirements_PracticeId_ArticleId",
                table: "PracticeRequirements",
                columns: new[] { "PracticeId", "ArticleId" },
                unique: true,
                filter: "[IsRemoved] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Practices_Code",
                table: "Practices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Practices_SubjectId",
                table: "Practices",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_ArticleCategories_ArticleCategoryId",
                table: "Articles",
                column: "ArticleCategoryId",
                principalTable: "ArticleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Equipments_EquipmentId",
                table: "Articles",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_MeasurementUnits_MeasurementUnitId",
                table: "Articles",
                column: "MeasurementUnitId",
                principalTable: "MeasurementUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Careers_CareerId",
                table: "Subjects",
                column: "CareerId",
                principalTable: "Careers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_ArticleCategories_ArticleCategoryId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Equipments_EquipmentId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_MeasurementUnits_MeasurementUnitId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Careers_CareerId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "ArticleCategories");

            migrationBuilder.DropTable(
                name: "ArticleStocks");

            migrationBuilder.DropTable(
                name: "CourseGroups");

            migrationBuilder.DropTable(
                name: "MeasurementUnits");

            migrationBuilder.DropTable(
                name: "PracticeRequirements");

            migrationBuilder.DropTable(
                name: "Practices");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_CareerId_Name",
                table: "Subjects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Subjects_Status",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ArticleCategoryId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_EquipmentId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_MeasurementUnitId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "UX_Articles_Name_Active",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "CareerId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ArticleCategoryId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ArticleType",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "MeasurementUnitId",
                table: "Articles");
        }
    }
}
