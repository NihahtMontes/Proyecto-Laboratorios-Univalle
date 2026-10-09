using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Proyecto_Laboratorios_Univalle.Data.SqlServerMigrations
{
    /// <summary>
    /// Reestructura la planificación académica sin pérdida de datos:
    /// - El contenedor AcademicPlannings desaparece del modelo: se archiva en legacy.AcademicPlannings.
    /// - L3Requests pasa a Plannings (RenameTable: se conservan Id y códigos PRA) y L3RequestDetails a PlanningRequirements.
    /// - Las semanas cuelgan de la gestión (lunes a sábado) y la habilitación pasa a ser por día (AcademicDays).
    /// - Las horas libres se convierten a periodos del catálogo ClassPeriods (periodo de inicio + cantidad).
    /// - Estados en femenino y nuevos estados de reprogramación/reposición; tablas Reschedulings y DayRecoveries.
    /// Los valores reemplazados se guardan en el esquema legacy para que Down los restituya.
    /// </summary>
    public partial class RestructurePlanningModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------
            // 0. Esquema de archivo y respaldo de los valores que se reemplazan
            // ---------------------------------------------------------------
            migrationBuilder.EnsureSchema(name: "legacy");

            migrationBuilder.Sql(@"
CREATE TABLE legacy.PlanningLinks(
    PlanningId int NOT NULL PRIMARY KEY,
    AcademicPlanningId int NULL,
    CareerId int NULL,
    OldStatus nvarchar(50) NOT NULL,
    OldScheduledDate datetime2 NOT NULL,
    OldScheduledEndDate datetime2 NULL,
    OldStartTime time NULL,
    OldEndTime time NULL,
    OldAcademicWeek int NOT NULL);
INSERT legacy.PlanningLinks
SELECT Id, AcademicPlanningId, CareerId, Status, ScheduledDate, ScheduledEndDate, StartTime, EndTime, AcademicWeek
FROM dbo.L3Requests;

CREATE TABLE legacy.AcademicWeekLinks(
    AcademicWeekId int NOT NULL PRIMARY KEY,
    AcademicPlanningId int NOT NULL,
    Number int NOT NULL,
    EndDate date NOT NULL,
    IsLective bit NOT NULL,
    Note nvarchar(100) NULL);
INSERT legacy.AcademicWeekLinks
SELECT Id, AcademicPlanningId, Number, EndDate, IsLective, Note
FROM dbo.AcademicWeeks;

CREATE TABLE legacy.ManagementDateFills(
    ManagementId int NOT NULL PRIMARY KEY,
    OldStartDate datetime2 NULL,
    OldPlannedEndDate datetime2 NULL);
INSERT legacy.ManagementDateFills
SELECT m.Id, m.StartDate, m.PlannedEndDate
FROM dbo.Managements m
WHERE (m.StartDate IS NULL OR m.PlannedEndDate IS NULL)
  AND EXISTS (SELECT 1 FROM dbo.AcademicPlannings ap WHERE ap.ManagementId = m.Id AND ap.IsDeleted = 0);");

            // ---------------------------------------------------------------
            // 1. Catálogo de periodos (12 periodos de 50 min, lunes a sábado)
            // ---------------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "ClassPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassPeriods", x => x.Id);
                    table.CheckConstraint("CK_ClassPeriods_Times", "[EndTime] > [StartTime]");
                });

            migrationBuilder.InsertData(
                table: "ClassPeriods",
                columns: new[] { "Id", "EndTime", "Order", "StartTime" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 8, 25, 0, 0), 1, new TimeSpan(0, 7, 35, 0, 0) },
                    { 2, new TimeSpan(0, 9, 25, 0, 0), 2, new TimeSpan(0, 8, 35, 0, 0) },
                    { 3, new TimeSpan(0, 10, 15, 0, 0), 3, new TimeSpan(0, 9, 25, 0, 0) },
                    { 4, new TimeSpan(0, 11, 15, 0, 0), 4, new TimeSpan(0, 10, 25, 0, 0) },
                    { 5, new TimeSpan(0, 12, 5, 0, 0), 5, new TimeSpan(0, 11, 15, 0, 0) },
                    { 6, new TimeSpan(0, 13, 5, 0, 0), 6, new TimeSpan(0, 12, 15, 0, 0) },
                    { 7, new TimeSpan(0, 13, 55, 0, 0), 7, new TimeSpan(0, 13, 5, 0, 0) },
                    { 8, new TimeSpan(0, 14, 55, 0, 0), 8, new TimeSpan(0, 14, 5, 0, 0) },
                    { 9, new TimeSpan(0, 15, 45, 0, 0), 9, new TimeSpan(0, 14, 55, 0, 0) },
                    { 10, new TimeSpan(0, 16, 45, 0, 0), 10, new TimeSpan(0, 15, 55, 0, 0) },
                    { 11, new TimeSpan(0, 17, 35, 0, 0), 11, new TimeSpan(0, 16, 45, 0, 0) },
                    { 12, new TimeSpan(0, 18, 35, 0, 0), 12, new TimeSpan(0, 17, 45, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassPeriods_Order",
                table: "ClassPeriods",
                column: "Order",
                unique: true);

            // ---------------------------------------------------------------
            // 2. Fechas de la gestión: se toman del contenedor cuando faltan
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
UPDATE m SET
    StartDate = COALESCE(m.StartDate, p.StartDate),
    PlannedEndDate = COALESCE(m.PlannedEndDate, p.EndDate)
FROM dbo.Managements m
JOIN legacy.ManagementDateFills f ON f.ManagementId = m.Id
CROSS APPLY (SELECT MIN(ap.StartDate) AS StartDate, MAX(ap.EndDate) AS EndDate
             FROM dbo.AcademicPlannings ap WHERE ap.ManagementId = m.Id AND ap.IsDeleted = 0) p;");

            // ---------------------------------------------------------------
            // 3. Semanas académicas: de la planificación a la gestión, lunes a sábado
            // ---------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicWeeks_AcademicPlannings_AcademicPlanningId",
                table: "AcademicWeeks");

            migrationBuilder.DropIndex(
                name: "IX_AcademicWeeks_AcademicPlanningId_Number",
                table: "AcademicWeeks");

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "AcademicWeeks",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE w SET ManagementId = ap.ManagementId
FROM dbo.AcademicWeeks w
JOIN dbo.AcademicPlannings ap ON ap.Id = w.AcademicPlanningId;

-- Dos contenedores de la misma gestión generan semanas repetidas: se conserva una por lunes.
DECLARE @map TABLE(Id int PRIMARY KEY, KeepId int NOT NULL);
INSERT @map
SELECT Id, KeepId
FROM (SELECT Id, MIN(Id) OVER (PARTITION BY ManagementId, StartDate) AS KeepId FROM dbo.AcademicWeeks) x
WHERE Id <> KeepId;

UPDATE k SET IsLective = 0, Note = COALESCE(k.Note, d.Note)
FROM dbo.AcademicWeeks k
JOIN @map m ON m.KeepId = k.Id
JOIN dbo.AcademicWeeks d ON d.Id = m.Id
WHERE d.IsLective = 0;

UPDATE r SET AcademicWeekId = m.KeepId FROM dbo.L3Requests r JOIN @map m ON m.Id = r.AcademicWeekId;
UPDATE s SET AcademicWeekId = m.KeepId FROM dbo.PracticeSchedules s JOIN @map m ON m.Id = s.AcademicWeekId;
DELETE w FROM dbo.AcademicWeeks w JOIN @map m ON m.Id = w.Id;

-- Numeración por gestión y fin de semana en sábado.
WITH n AS (SELECT Id, ROW_NUMBER() OVER (PARTITION BY ManagementId ORDER BY StartDate) AS NewNumber FROM dbo.AcademicWeeks)
UPDATE w SET Number = n.NewNumber, EndDate = DATEADD(day, 5, w.StartDate)
FROM dbo.AcademicWeeks w JOIN n ON n.Id = w.Id;

UPDATE r SET AcademicWeek = w.Number
FROM dbo.L3Requests r JOIN dbo.AcademicWeeks w ON w.Id = r.AcademicWeekId;");

            // ---------------------------------------------------------------
            // 4. Reposiciones de día y días académicos (habilitación por día)
            // ---------------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "DayRecoveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManagementId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayRecoveries", x => x.Id);
                    table.CheckConstraint("CK_DayRecoveries_Status", "[Status] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_DayRecoveries_Managements_ManagementId",
                        column: x => x.ManagementId,
                        principalTable: "Managements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicWeekId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DayRecoveryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDays", x => x.Id);
                    table.CheckConstraint("CK_AcademicDays_Status", "[Status] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_AcademicDays_AcademicWeeks_AcademicWeekId",
                        column: x => x.AcademicWeekId,
                        principalTable: "AcademicWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicDays_DayRecoveries_DayRecoveryId",
                        column: x => x.DayRecoveryId,
                        principalTable: "DayRecoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Seis días por semana, solo dentro de las fechas de la gestión; una semana no lectiva
            // se traduce en días suspendidos con su nota como motivo.
            migrationBuilder.Sql(@"
INSERT dbo.AcademicDays(AcademicWeekId, [Date], [Status], Reason, DayRecoveryId)
SELECT w.Id, d.[Date],
       CASE WHEN w.IsLective = 0 THEN 1 ELSE 0 END,
       CASE WHEN w.IsLective = 0 THEN LEFT(COALESCE(NULLIF(LTRIM(RTRIM(w.Note)), N''), N'Semana no lectiva'), 200) END,
       NULL
FROM dbo.AcademicWeeks w
JOIN dbo.Managements m ON m.Id = w.ManagementId
CROSS APPLY (VALUES (0), (1), (2), (3), (4), (5)) o(n)
CROSS APPLY (SELECT DATEADD(day, o.n, w.StartDate) AS [Date]) d
WHERE (m.StartDate IS NULL OR d.[Date] >= CAST(m.StartDate AS date))
  AND (m.PlannedEndDate IS NULL OR d.[Date] <= CAST(m.PlannedEndDate AS date));");

            migrationBuilder.DropColumn(name: "AcademicPlanningId", table: "AcademicWeeks");
            migrationBuilder.DropColumn(name: "IsLective", table: "AcademicWeeks");
            migrationBuilder.DropColumn(name: "Note", table: "AcademicWeeks");

            migrationBuilder.AlterColumn<int>(
                name: "ManagementId",
                table: "AcademicWeeks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicWeeks_ManagementId_Number",
                table: "AcademicWeeks",
                columns: new[] { "ManagementId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicWeeks_ManagementId_StartDate",
                table: "AcademicWeeks",
                columns: new[] { "ManagementId", "StartDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicWeeks_Managements_ManagementId",
                table: "AcademicWeeks",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDays_AcademicWeekId_Date",
                table: "AcademicDays",
                columns: new[] { "AcademicWeekId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDays_Date",
                table: "AcademicDays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDays_DayRecoveryId",
                table: "AcademicDays",
                column: "DayRecoveryId");

            migrationBuilder.CreateIndex(
                name: "IX_DayRecoveries_ManagementId_Date",
                table: "DayRecoveries",
                columns: new[] { "ManagementId", "Date" },
                unique: true);

            // ---------------------------------------------------------------
            // 5. L3Requests -> Plannings y L3RequestDetails -> PlanningRequirements (se conservan los Id)
            // ---------------------------------------------------------------
            migrationBuilder.DropForeignKey(name: "FK_Departures_L3Requests_L3RequestId", table: "Departures");
            migrationBuilder.DropForeignKey(name: "FK_L5Incidents_L3Requests_L3RequestId", table: "L5Incidents");
            migrationBuilder.DropForeignKey(name: "FK_L3Requests_AcademicPlannings_AcademicPlanningId", table: "L3Requests");
            migrationBuilder.DropForeignKey(name: "FK_L3Requests_Careers_CareerId", table: "L3Requests");

            migrationBuilder.DropCheckConstraint(name: "CK_L3Requests_Laboratory", table: "L3Requests");
            migrationBuilder.DropCheckConstraint(name: "CK_L3Requests_Status", table: "L3Requests");

            migrationBuilder.DropIndex(name: "IX_L3Requests_AcademicPlanningId", table: "L3Requests");
            migrationBuilder.DropIndex(name: "IX_L3Requests_CareerId", table: "L3Requests");
            migrationBuilder.DropIndex(name: "IX_L3Requests_LaboratoryId", table: "L3Requests");
            migrationBuilder.DropIndex(name: "IX_L3Requests_TeacherPersonId", table: "L3Requests");

            migrationBuilder.DropColumn(name: "AcademicPlanningId", table: "L3Requests");
            migrationBuilder.DropColumn(name: "CareerId", table: "L3Requests");

            migrationBuilder.RenameTable(name: "L3Requests", newName: "Plannings");
            migrationBuilder.RenameTable(name: "L3RequestDetails", newName: "PlanningRequirements");

            migrationBuilder.RenameColumn(name: "L3RequestId", table: "PlanningRequirements", newName: "PlanningId");
            migrationBuilder.RenameColumn(name: "L3RequestId", table: "Departures", newName: "PlanningId");
            migrationBuilder.RenameColumn(name: "L3RequestId", table: "L5Incidents", newName: "PlanningId");

            migrationBuilder.RenameIndex(name: "IX_Departures_L3RequestId", table: "Departures", newName: "IX_Departures_PlanningId");
            migrationBuilder.RenameIndex(name: "IX_L5Incidents_L3RequestId", table: "L5Incidents", newName: "IX_L5Incidents_PlanningId");

            migrationBuilder.RenameIndex(name: "IX_L3Requests_AcademicWeekId", table: "Plannings", newName: "IX_Plannings_AcademicWeekId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_CampusId", table: "Plannings", newName: "IX_Plannings_CampusId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_Code", table: "Plannings", newName: "IX_Plannings_Code");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_CourseGroupId", table: "Plannings", newName: "IX_Plannings_CourseGroupId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_GroupId", table: "Plannings", newName: "IX_Plannings_GroupId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_ManagementId", table: "Plannings", newName: "IX_Plannings_ManagementId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_PracticeScheduleId", table: "Plannings", newName: "IX_Plannings_PracticeScheduleId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_SubjectId", table: "Plannings", newName: "IX_Plannings_SubjectId");
            migrationBuilder.RenameIndex(name: "IX_L3Requests_TeacherId", table: "Plannings", newName: "IX_Plannings_TeacherId");

            migrationBuilder.RenameIndex(name: "IX_L3RequestDetails_ArticleId", table: "PlanningRequirements", newName: "IX_PlanningRequirements_ArticleId");
            migrationBuilder.RenameIndex(name: "IX_L3RequestDetails_Code", table: "PlanningRequirements", newName: "IX_PlanningRequirements_Code");
            migrationBuilder.RenameIndex(name: "IX_L3RequestDetails_EquipmentUnitId", table: "PlanningRequirements", newName: "IX_PlanningRequirements_EquipmentUnitId");
            migrationBuilder.RenameIndex(name: "IX_L3RequestDetails_L3RequestId", table: "PlanningRequirements", newName: "IX_PlanningRequirements_PlanningId");

            // Claves primarias, foráneas y CHECK conservadas: solo cambian de nombre.
            migrationBuilder.Sql(@"
EXEC sp_rename N'dbo.PK_L3Requests', N'PK_Plannings', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_AcademicWeeks_AcademicWeekId', N'FK_Plannings_AcademicWeeks_AcademicWeekId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Campuses_CampusId', N'FK_Plannings_Campuses_CampusId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_CourseGroups_CourseGroupId', N'FK_Plannings_CourseGroups_CourseGroupId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Groups_GroupId', N'FK_Plannings_Groups_GroupId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Laboratories_LaboratoryId', N'FK_Plannings_Laboratories_LaboratoryId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Managements_ManagementId', N'FK_Plannings_Managements_ManagementId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_People_TeacherPersonId', N'FK_Plannings_People_TeacherPersonId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_PracticeSchedules_PracticeScheduleId', N'FK_Plannings_PracticeSchedules_PracticeScheduleId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Subjects_SubjectId', N'FK_Plannings_Subjects_SubjectId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3Requests_Users_TeacherId', N'FK_Plannings_Users_TeacherId', N'OBJECT';
EXEC sp_rename N'dbo.PK_L3RequestDetails', N'PK_PlanningRequirements', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3RequestDetails_Articles_ArticleId', N'FK_PlanningRequirements_Articles_ArticleId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId', N'FK_PlanningRequirements_EquipmentUnits_EquipmentUnitId', N'OBJECT';
EXEC sp_rename N'dbo.FK_L3RequestDetails_L3Requests_L3RequestId', N'FK_PlanningRequirements_Plannings_PlanningId', N'OBJECT';
EXEC sp_rename N'dbo.CK_L3RequestDetails_RequestedQuantity', N'CK_PlanningRequirements_RequestedQuantity', N'OBJECT';");

            // ---------------------------------------------------------------
            // 6. Horas libres -> periodo de inicio + cantidad de periodos
            //    Inicio = periodo cuya hora de inicio es la más cercana; fin = periodo cuya hora de fin es la más cercana.
            // ---------------------------------------------------------------
            migrationBuilder.AddColumn<int>(name: "StartPeriodId", table: "Plannings", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PeriodCount", table: "Plannings", type: "int", nullable: true);

            migrationBuilder.Sql(@"
WITH src AS (
    SELECT p.Id,
           COALESCE(p.StartTime, CAST(p.ScheduledDate AS time)) AS T1,
           COALESCE(p.EndTime, CAST(p.ScheduledEndDate AS time)) AS T2
    FROM dbo.Plannings p
    WHERE p.StartTime IS NOT NULL OR CAST(p.ScheduledDate AS time) <> '00:00'
), mapped AS (
    SELECT s.Id,
           (SELECT TOP 1 c.[Order] FROM dbo.ClassPeriods c
            ORDER BY ABS(DATEDIFF(minute, c.StartTime, s.T1)), c.[Order]) AS FirstOrder,
           CASE WHEN s.T2 IS NULL OR s.T2 <= s.T1 THEN NULL ELSE
               (SELECT TOP 1 c.[Order] FROM dbo.ClassPeriods c
                ORDER BY ABS(DATEDIFF(minute, c.EndTime, s.T2)), c.[Order]) END AS LastOrder
    FROM src s
)
UPDATE p SET
    StartPeriodId = f.Id,
    PeriodCount = CASE WHEN m.LastOrder IS NULL OR m.LastOrder < m.FirstOrder THEN 1 ELSE m.LastOrder - m.FirstOrder + 1 END
FROM dbo.Plannings p
JOIN mapped m ON m.Id = p.Id
JOIN dbo.ClassPeriods f ON f.[Order] = m.FirstOrder;

-- Campos derivados: fecha + hora de inicio del primer periodo y hora de fin del último.
UPDATE p SET
    StartTime = f.StartTime,
    EndTime = l.EndTime,
    ScheduledDate = DATEADD(minute, DATEDIFF(minute, CAST('00:00' AS time), f.StartTime), CAST(CAST(p.ScheduledDate AS date) AS datetime2)),
    ScheduledEndDate = DATEADD(minute, DATEDIFF(minute, CAST('00:00' AS time), l.EndTime), CAST(CAST(p.ScheduledDate AS date) AS datetime2))
FROM dbo.Plannings p
JOIN dbo.ClassPeriods f ON f.Id = p.StartPeriodId
JOIN dbo.ClassPeriods l ON l.[Order] = f.[Order] + p.PeriodCount - 1;

-- Estados en femenino (la entidad pasa a ser la Planificación).
UPDATE dbo.Plannings SET Status = CASE Status
    WHEN N'Planificado' THEN N'Planificada'
    WHEN N'Aprobado' THEN N'Aprobada'
    WHEN N'Finalizado' THEN N'Finalizada'
    WHEN N'Cancelado' THEN N'Cancelada'
    ELSE Status END;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Laboratory",
                table: "Plannings",
                sql: "[AllLaboratories] = 1 OR [LaboratoryId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Periods",
                table: "Plannings",
                sql: "([StartPeriodId] IS NULL AND [PeriodCount] IS NULL) OR ([StartPeriodId] IS NOT NULL AND [PeriodCount] BETWEEN 1 AND 12)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Plannings_Status",
                table: "Plannings",
                sql: "[Status] IN (N'Planificada', N'Aprobada', N'Finalizada', N'Cancelada', N'Reprogramada', N'PendienteReposicion', N'Repuesta')");

            migrationBuilder.CreateIndex(
                name: "IX_Plannings_LaboratoryId_ScheduledDate",
                table: "Plannings",
                columns: new[] { "LaboratoryId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Plannings_TeacherPersonId_ScheduledDate",
                table: "Plannings",
                columns: new[] { "TeacherPersonId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Plannings_StartPeriodId",
                table: "Plannings",
                column: "StartPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plannings_ClassPeriods_StartPeriodId",
                table: "Plannings",
                column: "StartPeriodId",
                principalTable: "ClassPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_Plannings_PlanningId",
                table: "Departures",
                column: "PlanningId",
                principalTable: "Plannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_Plannings_PlanningId",
                table: "L5Incidents",
                column: "PlanningId",
                principalTable: "Plannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------
            // 7. El contenedor sale del modelo: se archiva (no se borra)
            // ---------------------------------------------------------------
            migrationBuilder.RenameTable(
                name: "AcademicPlannings",
                newName: "AcademicPlannings",
                newSchema: "legacy");

            // ---------------------------------------------------------------
            // 8. Capacidad del ambiente y reprogramaciones
            // ---------------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Laboratories",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Laboratories_Capacity",
                table: "Laboratories",
                sql: "[Capacity] IS NULL OR [Capacity] > 0");

            migrationBuilder.CreateTable(
                name: "Reschedulings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginPlanningId = table.Column<int>(type: "int", nullable: false),
                    DestinationPlanningId = table.Column<int>(type: "int", nullable: false),
                    OriginDate = table.Column<DateTime>(type: "date", nullable: false),
                    OriginStartPeriodId = table.Column<int>(type: "int", nullable: true),
                    OriginPeriodCount = table.Column<int>(type: "int", nullable: true),
                    OriginLaboratoryId = table.Column<int>(type: "int", nullable: true),
                    OriginTeacherPersonId = table.Column<int>(type: "int", nullable: false),
                    OriginStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DestinationDate = table.Column<DateTime>(type: "date", nullable: false),
                    DestinationStartPeriodId = table.Column<int>(type: "int", nullable: false),
                    DestinationPeriodCount = table.Column<int>(type: "int", nullable: false),
                    DestinationLaboratoryId = table.Column<int>(type: "int", nullable: false),
                    DestinationTeacherPersonId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DayRecoveryId = table.Column<int>(type: "int", nullable: true),
                    AcceptedWarnings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    ModifiedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reschedulings", x => x.Id);
                    table.CheckConstraint("CK_Reschedulings_DestinationPeriods", "[DestinationPeriodCount] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_Reschedulings_DistinctPlannings", "[OriginPlanningId] <> [DestinationPlanningId]");
                    table.ForeignKey(
                        name: "FK_Reschedulings_ClassPeriods_DestinationStartPeriodId",
                        column: x => x.DestinationStartPeriodId,
                        principalTable: "ClassPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_ClassPeriods_OriginStartPeriodId",
                        column: x => x.OriginStartPeriodId,
                        principalTable: "ClassPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_DayRecoveries_DayRecoveryId",
                        column: x => x.DayRecoveryId,
                        principalTable: "DayRecoveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_Laboratories_DestinationLaboratoryId",
                        column: x => x.DestinationLaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_Laboratories_OriginLaboratoryId",
                        column: x => x.OriginLaboratoryId,
                        principalTable: "Laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_People_DestinationTeacherPersonId",
                        column: x => x.DestinationTeacherPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_People_OriginTeacherPersonId",
                        column: x => x.OriginTeacherPersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_Plannings_DestinationPlanningId",
                        column: x => x.DestinationPlanningId,
                        principalTable: "Plannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reschedulings_Plannings_OriginPlanningId",
                        column: x => x.OriginPlanningId,
                        principalTable: "Plannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_DayRecoveryId",
                table: "Reschedulings",
                column: "DayRecoveryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_DestinationLaboratoryId",
                table: "Reschedulings",
                column: "DestinationLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_DestinationPlanningId",
                table: "Reschedulings",
                column: "DestinationPlanningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_DestinationStartPeriodId",
                table: "Reschedulings",
                column: "DestinationStartPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_DestinationTeacherPersonId",
                table: "Reschedulings",
                column: "DestinationTeacherPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_OriginLaboratoryId",
                table: "Reschedulings",
                column: "OriginLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_OriginPlanningId",
                table: "Reschedulings",
                column: "OriginPlanningId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_OriginStartPeriodId",
                table: "Reschedulings",
                column: "OriginStartPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Reschedulings_OriginTeacherPersonId",
                table: "Reschedulings",
                column: "OriginTeacherPersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Las reprogramaciones, reposiciones y días no existían antes: se descartan.
            migrationBuilder.DropTable(name: "Reschedulings");

            migrationBuilder.DropCheckConstraint(name: "CK_Laboratories_Capacity", table: "Laboratories");
            migrationBuilder.DropColumn(name: "Capacity", table: "Laboratories");

            // ---------------------------------------------------------------
            // 7. El contenedor vuelve a dbo
            // ---------------------------------------------------------------
            migrationBuilder.RenameTable(
                name: "AcademicPlannings",
                schema: "legacy",
                newName: "AcademicPlannings",
                newSchema: "dbo");

            // ---------------------------------------------------------------
            // 6. Periodos -> horas; estados en masculino
            // ---------------------------------------------------------------
            migrationBuilder.DropForeignKey(name: "FK_Departures_Plannings_PlanningId", table: "Departures");
            migrationBuilder.DropForeignKey(name: "FK_L5Incidents_Plannings_PlanningId", table: "L5Incidents");
            migrationBuilder.DropForeignKey(name: "FK_Plannings_ClassPeriods_StartPeriodId", table: "Plannings");

            migrationBuilder.DropIndex(name: "IX_Plannings_LaboratoryId_ScheduledDate", table: "Plannings");
            migrationBuilder.DropIndex(name: "IX_Plannings_TeacherPersonId_ScheduledDate", table: "Plannings");
            migrationBuilder.DropIndex(name: "IX_Plannings_StartPeriodId", table: "Plannings");

            migrationBuilder.DropCheckConstraint(name: "CK_Plannings_Laboratory", table: "Plannings");
            migrationBuilder.DropCheckConstraint(name: "CK_Plannings_Periods", table: "Plannings");
            migrationBuilder.DropCheckConstraint(name: "CK_Plannings_Status", table: "Plannings");

            // Horas originales de las planificaciones previas a la migración (si su fecha no cambió).
            // Los estados nuevos (reprogramación/reposición) no existían: quedan como Cancelado.
            migrationBuilder.Sql(@"
UPDATE p SET
    ScheduledDate = l.OldScheduledDate,
    ScheduledEndDate = l.OldScheduledEndDate,
    StartTime = l.OldStartTime,
    EndTime = l.OldEndTime
FROM dbo.Plannings p
JOIN legacy.PlanningLinks l ON l.PlanningId = p.Id
WHERE CAST(p.ScheduledDate AS date) = CAST(l.OldScheduledDate AS date);

UPDATE dbo.Plannings SET Status = CASE Status
    WHEN N'Planificada' THEN N'Planificado'
    WHEN N'Aprobada' THEN N'Aprobado'
    WHEN N'Finalizada' THEN N'Finalizado'
    ELSE N'Cancelado' END;");

            migrationBuilder.DropColumn(name: "StartPeriodId", table: "Plannings");
            migrationBuilder.DropColumn(name: "PeriodCount", table: "Plannings");

            // ---------------------------------------------------------------
            // 5. Plannings -> L3Requests y PlanningRequirements -> L3RequestDetails
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
EXEC sp_rename N'dbo.CK_PlanningRequirements_RequestedQuantity', N'CK_L3RequestDetails_RequestedQuantity', N'OBJECT';
EXEC sp_rename N'dbo.FK_PlanningRequirements_Plannings_PlanningId', N'FK_L3RequestDetails_L3Requests_L3RequestId', N'OBJECT';
EXEC sp_rename N'dbo.FK_PlanningRequirements_EquipmentUnits_EquipmentUnitId', N'FK_L3RequestDetails_EquipmentUnits_EquipmentUnitId', N'OBJECT';
EXEC sp_rename N'dbo.FK_PlanningRequirements_Articles_ArticleId', N'FK_L3RequestDetails_Articles_ArticleId', N'OBJECT';
EXEC sp_rename N'dbo.PK_PlanningRequirements', N'PK_L3RequestDetails', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Users_TeacherId', N'FK_L3Requests_Users_TeacherId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Subjects_SubjectId', N'FK_L3Requests_Subjects_SubjectId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_PracticeSchedules_PracticeScheduleId', N'FK_L3Requests_PracticeSchedules_PracticeScheduleId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_People_TeacherPersonId', N'FK_L3Requests_People_TeacherPersonId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Managements_ManagementId', N'FK_L3Requests_Managements_ManagementId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Laboratories_LaboratoryId', N'FK_L3Requests_Laboratories_LaboratoryId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Groups_GroupId', N'FK_L3Requests_Groups_GroupId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_CourseGroups_CourseGroupId', N'FK_L3Requests_CourseGroups_CourseGroupId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_Campuses_CampusId', N'FK_L3Requests_Campuses_CampusId', N'OBJECT';
EXEC sp_rename N'dbo.FK_Plannings_AcademicWeeks_AcademicWeekId', N'FK_L3Requests_AcademicWeeks_AcademicWeekId', N'OBJECT';
EXEC sp_rename N'dbo.PK_Plannings', N'PK_L3Requests', N'OBJECT';");

            migrationBuilder.RenameIndex(name: "IX_PlanningRequirements_PlanningId", table: "PlanningRequirements", newName: "IX_L3RequestDetails_L3RequestId");
            migrationBuilder.RenameIndex(name: "IX_PlanningRequirements_EquipmentUnitId", table: "PlanningRequirements", newName: "IX_L3RequestDetails_EquipmentUnitId");
            migrationBuilder.RenameIndex(name: "IX_PlanningRequirements_Code", table: "PlanningRequirements", newName: "IX_L3RequestDetails_Code");
            migrationBuilder.RenameIndex(name: "IX_PlanningRequirements_ArticleId", table: "PlanningRequirements", newName: "IX_L3RequestDetails_ArticleId");

            migrationBuilder.RenameIndex(name: "IX_Plannings_TeacherId", table: "Plannings", newName: "IX_L3Requests_TeacherId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_SubjectId", table: "Plannings", newName: "IX_L3Requests_SubjectId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_PracticeScheduleId", table: "Plannings", newName: "IX_L3Requests_PracticeScheduleId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_ManagementId", table: "Plannings", newName: "IX_L3Requests_ManagementId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_GroupId", table: "Plannings", newName: "IX_L3Requests_GroupId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_CourseGroupId", table: "Plannings", newName: "IX_L3Requests_CourseGroupId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_Code", table: "Plannings", newName: "IX_L3Requests_Code");
            migrationBuilder.RenameIndex(name: "IX_Plannings_CampusId", table: "Plannings", newName: "IX_L3Requests_CampusId");
            migrationBuilder.RenameIndex(name: "IX_Plannings_AcademicWeekId", table: "Plannings", newName: "IX_L3Requests_AcademicWeekId");

            migrationBuilder.RenameIndex(name: "IX_L5Incidents_PlanningId", table: "L5Incidents", newName: "IX_L5Incidents_L3RequestId");
            migrationBuilder.RenameIndex(name: "IX_Departures_PlanningId", table: "Departures", newName: "IX_Departures_L3RequestId");

            migrationBuilder.RenameColumn(name: "PlanningId", table: "L5Incidents", newName: "L3RequestId");
            migrationBuilder.RenameColumn(name: "PlanningId", table: "Departures", newName: "L3RequestId");
            migrationBuilder.RenameColumn(name: "PlanningId", table: "PlanningRequirements", newName: "L3RequestId");

            migrationBuilder.RenameTable(name: "PlanningRequirements", newName: "L3RequestDetails");
            migrationBuilder.RenameTable(name: "Plannings", newName: "L3Requests");

            migrationBuilder.AddColumn<int>(name: "AcademicPlanningId", table: "L3Requests", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "CareerId", table: "L3Requests", type: "int", nullable: true);

            // Vínculo al contenedor: el original si existía; las creadas después toman el contenedor de su gestión.
            migrationBuilder.Sql(@"
UPDATE r SET AcademicPlanningId = l.AcademicPlanningId, CareerId = l.CareerId
FROM dbo.L3Requests r JOIN legacy.PlanningLinks l ON l.PlanningId = r.Id;

UPDATE r SET
    AcademicPlanningId = ap.Id,
    CareerId = COALESCE(ap.CareerId, s.CareerId)
FROM dbo.L3Requests r
LEFT JOIN dbo.Subjects s ON s.Id = r.SubjectId
OUTER APPLY (SELECT TOP 1 a.Id, a.CareerId FROM dbo.AcademicPlannings a
             WHERE a.ManagementId = r.ManagementId AND a.IsDeleted = 0 ORDER BY a.Id) ap
WHERE NOT EXISTS (SELECT 1 FROM legacy.PlanningLinks l WHERE l.PlanningId = r.Id);");

            migrationBuilder.CreateIndex(name: "IX_L3Requests_AcademicPlanningId", table: "L3Requests", column: "AcademicPlanningId");
            migrationBuilder.CreateIndex(name: "IX_L3Requests_CareerId", table: "L3Requests", column: "CareerId");
            migrationBuilder.CreateIndex(name: "IX_L3Requests_LaboratoryId", table: "L3Requests", column: "LaboratoryId");
            migrationBuilder.CreateIndex(name: "IX_L3Requests_TeacherPersonId", table: "L3Requests", column: "TeacherPersonId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L3Requests_Laboratory",
                table: "L3Requests",
                sql: "[AllLaboratories] = 1 OR [LaboratoryId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L3Requests_Status",
                table: "L3Requests",
                sql: "[Status] IN (N'Planificado', N'Aprobado', N'Finalizado', N'Cancelado')");

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_AcademicPlannings_AcademicPlanningId",
                table: "L3Requests",
                column: "AcademicPlanningId",
                principalTable: "AcademicPlannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L3Requests_Careers_CareerId",
                table: "L3Requests",
                column: "CareerId",
                principalTable: "Careers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_L3Requests_L3RequestId",
                table: "Departures",
                column: "L3RequestId",
                principalTable: "L3Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_L5Incidents_L3Requests_L3RequestId",
                table: "L5Incidents",
                column: "L3RequestId",
                principalTable: "L3Requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------
            // 3-4. Semanas vuelven al contenedor (lunes a viernes, lectiva por semana)
            // ---------------------------------------------------------------
            migrationBuilder.DropForeignKey(name: "FK_AcademicWeeks_Managements_ManagementId", table: "AcademicWeeks");
            migrationBuilder.DropIndex(name: "IX_AcademicWeeks_ManagementId_Number", table: "AcademicWeeks");
            migrationBuilder.DropIndex(name: "IX_AcademicWeeks_ManagementId_StartDate", table: "AcademicWeeks");

            migrationBuilder.AddColumn<int>(name: "AcademicPlanningId", table: "AcademicWeeks", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsLective", table: "AcademicWeeks", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "Note", table: "AcademicWeeks", type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.Sql(@"
-- Semanas previas: valores originales. Semanas nuevas: contenedor de su gestión; lectiva si ningún día está suspendido.
UPDATE w SET
    AcademicPlanningId = l.AcademicPlanningId,
    Number = l.Number,
    EndDate = l.EndDate,
    IsLective = l.IsLective,
    Note = l.Note
FROM dbo.AcademicWeeks w JOIN legacy.AcademicWeekLinks l ON l.AcademicWeekId = w.Id;

UPDATE w SET
    AcademicPlanningId = ap.Id,
    EndDate = DATEADD(day, 4, w.StartDate),
    IsLective = CASE WHEN EXISTS (SELECT 1 FROM dbo.AcademicDays d WHERE d.AcademicWeekId = w.Id AND d.Status = 1) THEN 0 ELSE 1 END,
    Note = (SELECT TOP 1 LEFT(d.Reason, 100) FROM dbo.AcademicDays d WHERE d.AcademicWeekId = w.Id AND d.Status = 1 ORDER BY d.[Date])
FROM dbo.AcademicWeeks w
OUTER APPLY (SELECT TOP 1 a.Id FROM dbo.AcademicPlannings a
             WHERE a.ManagementId = w.ManagementId AND a.IsDeleted = 0 ORDER BY a.Id) ap
WHERE NOT EXISTS (SELECT 1 FROM legacy.AcademicWeekLinks l WHERE l.AcademicWeekId = w.Id);

-- Semanas de gestiones sin contenedor no caben en el modelo anterior: se desvinculan y se retiran.
UPDATE r SET AcademicWeekId = NULL
FROM dbo.L3Requests r JOIN dbo.AcademicWeeks w ON w.Id = r.AcademicWeekId
WHERE w.AcademicPlanningId IS NULL;
UPDATE s SET AcademicWeekId = NULL
FROM dbo.PracticeSchedules s JOIN dbo.AcademicWeeks w ON w.Id = s.AcademicWeekId
WHERE w.AcademicPlanningId IS NULL;
DELETE d FROM dbo.AcademicDays d JOIN dbo.AcademicWeeks w ON w.Id = d.AcademicWeekId WHERE w.AcademicPlanningId IS NULL;
DELETE FROM dbo.AcademicWeeks WHERE AcademicPlanningId IS NULL;

UPDATE r SET AcademicWeek = COALESCE(l.OldAcademicWeek, w.Number)
FROM dbo.L3Requests r
JOIN dbo.AcademicWeeks w ON w.Id = r.AcademicWeekId
LEFT JOIN legacy.PlanningLinks l ON l.PlanningId = r.Id;");

            migrationBuilder.DropTable(name: "AcademicDays");
            migrationBuilder.DropTable(name: "DayRecoveries");

            migrationBuilder.DropColumn(name: "ManagementId", table: "AcademicWeeks");

            migrationBuilder.AlterColumn<int>(
                name: "AcademicPlanningId",
                table: "AcademicWeeks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicWeeks_AcademicPlanningId_Number",
                table: "AcademicWeeks",
                columns: new[] { "AcademicPlanningId", "Number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicWeeks_AcademicPlannings_AcademicPlanningId",
                table: "AcademicWeeks",
                column: "AcademicPlanningId",
                principalTable: "AcademicPlannings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---------------------------------------------------------------
            // 2. Fechas de la gestión completadas por la migración
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
UPDATE m SET StartDate = f.OldStartDate, PlannedEndDate = f.OldPlannedEndDate
FROM dbo.Managements m JOIN legacy.ManagementDateFills f ON f.ManagementId = m.Id;");

            // ---------------------------------------------------------------
            // 1 y 0. Catálogo de periodos y archivo
            // ---------------------------------------------------------------
            migrationBuilder.DropTable(name: "ClassPeriods");

            migrationBuilder.Sql(@"
DROP TABLE legacy.PlanningLinks;
DROP TABLE legacy.AcademicWeekLinks;
DROP TABLE legacy.ManagementDateFills;
DROP SCHEMA legacy;");
        }
    }
}
