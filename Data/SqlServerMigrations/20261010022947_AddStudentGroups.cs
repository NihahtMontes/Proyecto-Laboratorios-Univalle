using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class AddStudentGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentGroupId",
                table: "CourseGroups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    Letter = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: true),
                    ScalingFactor = table.Column<int>(type: "int", nullable: true),
                    LaboratoryId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGroups", x => x.Id);
                    table.CheckConstraint("CK_StudentGroups_Semester", "[Semester] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_StudentGroups_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_StudentGroups_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentGroups_Laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentGroups_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroups_StudentGroupId",
                table: "CourseGroups",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_CareerId",
                table: "StudentGroups",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_Code",
                table: "StudentGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_LaboratoryId",
                table: "StudentGroups",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_ManagementId_CareerId_Semester_Name",
                table: "StudentGroups",
                columns: new[] { "ManagementId", "CareerId", "Semester", "Name" },
                unique: true,
                filter: "[Status] <> 2");

            // Conversión de datos: cada conjunto de grupos de materia con la misma gestión, carrera, semestre y
            // nombre pasa a ser un grupo de estudiantes. Nombre = letra + semestre ("A" de 1.er semestre = "A1");
            // los nombres que no son solo letras ("A1", "G-2") se conservan (el nombre es único por semestre). Los grupos cuya materia no tiene
            // carrera o semestre quedan sin grupo (StudentGroupId NULL). Las planificaciones y horarios de práctica
            // que guardan el nombre del grupo se actualizan al nombre nuevo. Un solo lote: la conversión es atómica.
            migrationBuilder.Sql(@"
DECLARE @map TABLE (
    Id int PRIMARY KEY, ManagementId int, SubjectId int, CareerId int, Semester int,
    OldUpper nvarchar(20), Letter nvarchar(18), NewName nvarchar(20));

INSERT INTO @map (Id, ManagementId, SubjectId, CareerId, Semester, OldUpper, Letter, NewName)
SELECT g.Id, g.ManagementId, g.SubjectId, s.CareerId, s.Semester,
       UPPER(LTRIM(RTRIM(g.Name))),
       LEFT(UPPER(LTRIM(RTRIM(g.Name))), 18),
       CASE
           WHEN LEN(LTRIM(RTRIM(g.Name))) BETWEEN 1 AND 17
                AND UPPER(LTRIM(RTRIM(g.Name))) COLLATE Latin1_General_BIN NOT LIKE '%[^A-Z]%'
               THEN UPPER(LTRIM(RTRIM(g.Name))) + CAST(s.Semester AS nvarchar(3))
           ELSE LEFT(UPPER(LTRIM(RTRIM(g.Name))), 20)
       END
FROM CourseGroups g
JOIN Subjects s ON s.Id = g.SubjectId
WHERE g.Status <> 2 AND s.CareerId IS NOT NULL AND s.Semester BETWEEN 1 AND 12;

-- Dos grupos de la misma materia que terminarían con el mismo nombre conservan su nombre original.
UPDATE m SET NewName = m.OldUpper
FROM @map m
WHERE EXISTS (SELECT 1 FROM @map o
              WHERE o.Id <> m.Id AND o.ManagementId = m.ManagementId AND o.SubjectId = m.SubjectId
                AND o.NewName = m.NewName);

UPDATE p SET p.GroupName = m.NewName
FROM Plannings p
JOIN @map m ON m.ManagementId = p.ManagementId AND m.SubjectId = p.SubjectId
WHERE p.GroupName IS NOT NULL AND UPPER(LTRIM(RTRIM(p.GroupName))) = m.OldUpper AND m.NewName <> m.OldUpper;

UPDATE ps SET ps.GroupName = m.NewName
FROM PracticeSchedules ps
JOIN @map m ON m.ManagementId = ps.ManagementId AND m.SubjectId = ps.SubjectId
WHERE ps.GroupName IS NOT NULL AND UPPER(LTRIM(RTRIM(ps.GroupName))) = m.OldUpper AND m.NewName <> m.OldUpper;

UPDATE g SET g.Name = m.NewName
FROM CourseGroups g
JOIN @map m ON m.Id = g.Id
WHERE g.Name <> m.NewName;

INSERT INTO StudentGroups (Code, ManagementId, CareerId, Semester, Letter, Name, StudentCount, ScalingFactor,
                           LaboratoryId, Status, CreatedDate)
SELECT N'GRU-' + RIGHT(N'00000' + CAST(ROW_NUMBER() OVER (ORDER BY x.ManagementId, x.CareerId, x.Semester, x.NewName) AS nvarchar(10)), 5),
       x.ManagementId, x.CareerId, x.Semester, x.Letter, x.NewName, x.StudentCount, x.ScalingFactor,
       x.LaboratoryId, x.Status, SYSUTCDATETIME()
FROM (
    SELECT m.ManagementId, m.CareerId, m.Semester, m.NewName,
           MIN(CASE WHEN m.NewName = m.OldUpper THEN m.NewName ELSE m.Letter END) AS Letter,
           MAX(g.StudentCount) AS StudentCount,
           MAX(g.ScalingFactor) AS ScalingFactor,
           MIN(g.LaboratoryId) AS LaboratoryId,
           MIN(g.Status) AS Status
    FROM @map m
    JOIN CourseGroups g ON g.Id = m.Id
    GROUP BY m.ManagementId, m.CareerId, m.Semester, m.NewName
) x;

UPDATE g SET g.StudentGroupId = sg.Id
FROM CourseGroups g
JOIN @map m ON m.Id = g.Id
JOIN StudentGroups sg ON sg.ManagementId = m.ManagementId AND sg.CareerId = m.CareerId
                      AND sg.Semester = m.Semester AND sg.Name = m.NewName;
");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseGroups_StudentGroups_StudentGroupId",
                table: "CourseGroups",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir no restaura los nombres anteriores de los grupos ("A1" sigue siendo "A1").
            migrationBuilder.DropForeignKey(
                name: "FK_CourseGroups_StudentGroups_StudentGroupId",
                table: "CourseGroups");

            migrationBuilder.DropTable(
                name: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_CourseGroups_StudentGroupId",
                table: "CourseGroups");

            migrationBuilder.DropColumn(
                name: "StudentGroupId",
                table: "CourseGroups");
        }
    }
}
