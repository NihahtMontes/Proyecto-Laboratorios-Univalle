using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.PlanningImport;

namespace Laboratorios.Tests;

/// <summary>
/// Carga de la planificación del semestre desde Excel: lectura del archivo, revisión fila por fila
/// (errores, choques, duplicados, prácticas ya cargadas) y carga todo-o-nada.
/// </summary>
public class PlanningImportServiceTests
{
    private const int SubjectId = 500;
    private const int OtherSubjectId = 501;
    private const int PracticeP1 = 600;
    private const int PracticeP2 = 601;
    private const int PracticeOther = 602;
    private const int GroupA = 700;
    private const int GroupB = 701;
    private const int ArticleId = 800;

    private static readonly DateTime Tuesday = TestDb.Week1Monday.AddDays(1);     // 06/10/2026
    private static readonly DateTime Wednesday = TestDb.Week1Monday.AddDays(2);   // 07/10/2026
    private static readonly Dictionary<int, string> NoDecisions = [];

    static PlanningImportServiceTests() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    private static PlanningImportService Service(TestDb db) =>
        new(db.Context, db.Occupancy(), db.GroupSchedule(), NullLogger<PlanningImportService>.Instance);

    /// <summary>Materia con dos prácticas (P1 con receta escalable), dos grupos y dos docentes activos.</summary>
    private static TestDb Seed()
    {
        var db = new TestDb();
        var c = db.Context;
        c.Subjects.AddRange(
            new Subject { Id = SubjectId, Code = "GAS-101", Name = "Panadería" },
            new Subject { Id = OtherSubjectId, Code = "GAS-202", Name = "Repostería" });
        c.Practices.AddRange(
            new Practice { Id = PracticeP1, SubjectId = SubjectId, Code = "PAN-P1", Name = "Masas básicas", Number = "P1" },
            new Practice { Id = PracticeP2, SubjectId = SubjectId, Code = "PAN-P2", Name = "Masas fermentadas", Number = "P2" },
            new Practice { Id = PracticeOther, SubjectId = OtherSubjectId, Code = "REP-P1", Name = "Cremas", Number = "P1" });
        c.Interns.AddRange(
            new Intern { Id = TestDb.Teacher1, Name = "Ana Pérez Mamani", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo },
            new Intern { Id = TestDb.Teacher2, Name = "Luis Rojas", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo });
        c.CourseGroups.AddRange(
            new CourseGroup { Id = GroupA, Code = "G-A", ManagementId = TestDb.ManagementId, SubjectId = SubjectId, Name = "A1", TeacherPersonId = TestDb.Teacher1, StudentCount = 12, ScalingFactor = 4 },
            new CourseGroup { Id = GroupB, Code = "G-B", ManagementId = TestDb.ManagementId, SubjectId = SubjectId, Name = "B1", TeacherPersonId = TestDb.Teacher2, StudentCount = 10, ScalingFactor = 4 });
        c.Articles.Add(new Article { Id = ArticleId, Code = "UT-PALA", Name = "Pala de panadería" });
        c.PracticeRequirements.Add(new PracticeRequirement { Id = 900, PracticeId = PracticeP1, ArticleId = ArticleId, Quantity = 1, IsScalable = true });
        c.SaveChanges();
        c.ChangeTracker.Clear();
        return db;
    }

    private static ImportRawRow Row(int number, string practice = "PAN-P1", string group = "A1", DateTime? date = null, string start = "3",
        string count = "2", string lab = "LAB-A", string teacher = "Ana Pérez Mamani", string subject = "GAS-101 · Panadería", string students = "") =>
        new()
        {
            RowNumber = number, Subject = subject, Group = group, Practice = practice,
            Date = (date ?? Tuesday).ToString("yyyy-MM-dd"), StartPeriod = start, PeriodCount = count,
            Laboratory = lab, Teacher = teacher, Students = students
        };

    private static ImportFile File(params ImportRawRow[] rows) =>
        new() { FileName = "plan.xlsx", Sha256 = "ABC", Rows = [.. rows] };

    private static void AssertIssue(ImportRow row, ImportIssueLevel level, string column, string fragment) =>
        Assert.Contains(row.Issues, i => i.Level == level && i.Column == column && i.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    // ───────────────────────────── revisión

    [Fact]
    public async Task ValidRow_IsReadyToCreate()
    {
        using var db = Seed();

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2)), NoDecisions);

        var row = Assert.Single(analysis.Rows);
        // La siembra usa fechas fijas de octubre de 2026: el único hallazgo admisible es el aviso de fecha pasada.
        Assert.DoesNotContain(row.Issues, i => i.Level != ImportIssueLevel.Warning);
        Assert.Equal(ImportRowAction.Create, row.Action);
        Assert.Equal(GroupA, row.CourseGroupId);
        Assert.Equal(1, row.WeekNumber);
        Assert.True(analysis.CanApply);
    }

    [Fact]
    public async Task UnknownData_IsReportedPerColumn_AndBlocksTheRow()
    {
        using var db = Seed();
        var raw = Row(2, practice: "PAN-P9", lab: "LAB-Z", teacher: "Juan Inexistente", start: "13");
        raw.Date = "31/02/2026";

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(raw), NoDecisions);

        var row = Assert.Single(analysis.Rows);
        AssertIssue(row, ImportIssueLevel.Error, "Práctica", "no existe");
        AssertIssue(row, ImportIssueLevel.Error, "Laboratorio", "no existe");
        AssertIssue(row, ImportIssueLevel.Error, "Docente", "No hay un docente activo");
        AssertIssue(row, ImportIssueLevel.Error, "Periodo inicial", "no existe");
        AssertIssue(row, ImportIssueLevel.Error, "Fecha", "no es una fecha válida");
        Assert.Equal(ImportRowAction.Blocked, row.Action);
        Assert.False(analysis.CanApply);
    }

    [Fact]
    public async Task DateOutsideManagementCalendar_Sunday_AndSuspendedDay_AreRejected()
    {
        using var db = Seed();
        var outside = Row(2, date: new DateTime(2027, 3, 1));
        var sunday = Row(3, practice: "PAN-P2", date: TestDb.Week1Monday.AddDays(6));
        var suspended = Row(4, group: "B1", teacher: "Luis Rojas", date: TestDb.SuspendedDay);

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(outside, sunday, suspended), NoDecisions);

        AssertIssue(analysis.Rows[0], ImportIssueLevel.Error, "Fecha", "fuera del calendario");
        AssertIssue(analysis.Rows[1], ImportIssueLevel.Error, "Fecha", "domingo");
        AssertIssue(analysis.Rows[2], ImportIssueLevel.Clash, "Fecha", "suspendido");
    }

    [Fact]
    public async Task PracticeOfAnotherSubject_IsAnError()
    {
        using var db = Seed();

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2, practice: "REP-P1")), NoDecisions);

        AssertIssue(analysis.Rows[0], ImportIssueLevel.Error, "Práctica", "es de Repostería");
    }

    [Fact]
    public async Task TeacherIsMatchedByName_IgnoringAccentsCaseAndTitle()
    {
        using var db = Seed();

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2, teacher: "Lic. ANA PEREZ MAMANI")), NoDecisions);

        var row = analysis.Rows[0];
        Assert.Equal(TestDb.Teacher1, row.TeacherId);
        Assert.DoesNotContain(row.Issues, i => i.Column == "Docente");
    }

    [Fact]
    public async Task IncompleteTeacherName_IsAcceptedWithWarning_AndUnknownSuggestsCandidates()
    {
        using var db = Seed();

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId,
            File(Row(2, teacher: "Ana Pérez"), Row(3, practice: "PAN-P2", teacher: "Ana")), NoDecisions);

        Assert.Equal(TestDb.Teacher1, analysis.Rows[0].TeacherId);
        AssertIssue(analysis.Rows[0], ImportIssueLevel.Warning, "Docente", "se interpretó como Ana Pérez Mamani");
        Assert.Null(analysis.Rows[1].TeacherId);
        AssertIssue(analysis.Rows[1], ImportIssueLevel.Error, "Docente", "¿Quiso decir Ana Pérez Mamani?");
    }

    [Fact]
    public async Task SamePracticeAndGroupTwiceInFile_MarksBothRows_UntilOneIsSkipped()
    {
        using var db = Seed();
        var file = File(Row(2), Row(3, date: Wednesday));

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, file, NoDecisions);
        Assert.All(analysis.Rows, r => AssertIssue(r, ImportIssueLevel.Clash, "Práctica", "Fila repetida"));
        Assert.False(analysis.CanApply);

        var resolved = await Service(db).AnalyzeAsync(TestDb.ManagementId, file, new Dictionary<int, string> { [3] = ImportDecisions.Skip });
        Assert.Equal(ImportRowAction.Create, resolved.Rows[0].Action);
        Assert.Equal(ImportRowAction.Skip, resolved.Rows[1].Action);
        Assert.True(resolved.CanApply);
    }

    [Fact]
    public async Task RowsInFile_ClashByLaboratoryOverlap_AndByTeacher()
    {
        using var db = Seed();
        // Mismo laboratorio en periodos superpuestos (3–4 y 4–5).
        var a = Row(2, start: "3", count: "2");
        var b = Row(3, group: "B1", teacher: "Luis Rojas", start: "4", count: "2");
        // Mismo docente a la vez en otro laboratorio.
        var c = Row(4, practice: "PAN-P2", lab: "LAB-B", start: "3", count: "2");

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(a, b, c), NoDecisions);

        AssertIssue(analysis.Rows[0], ImportIssueLevel.Clash, "Laboratorio", "fila 3");
        AssertIssue(analysis.Rows[1], ImportIssueLevel.Clash, "Laboratorio", "fila 2");
        AssertIssue(analysis.Rows[0], ImportIssueLevel.Clash, "Docente", "fila 4");
        AssertIssue(analysis.Rows[2], ImportIssueLevel.Clash, "Docente", "fila 2");
    }

    [Fact]
    public async Task OccupiedLaboratoryInSystem_IsAClash()
    {
        using var db = Seed();
        db.AddPlanning(40, Tuesday, 3, 2, status: PlanningStatus.Approved, teacherPersonId: TestDb.Teacher2);

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2)), NoDecisions);

        AssertIssue(analysis.Rows[0], ImportIssueLevel.Clash, "Laboratorio", "ya está ocupado");
    }

    [Fact]
    public async Task StudentsOverLaboratoryCapacity_IsAnError()
    {
        using var db = Seed();

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2, students: "25")), NoDecisions);

        AssertIssue(analysis.Rows[0], ImportIssueLevel.Error, "N.º de estudiantes", "admite 20");
    }

    // ───────────────────────────── carga

    [Fact]
    public async Task Apply_CreatesApprovedPlanningWithScaledRecipe()
    {
        using var db = Seed();
        var file = File(Row(2), Row(3, group: "B1", teacher: "Luis Rojas", lab: "LAB-B"));

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, file, NoDecisions);

        Assert.True(result.Ok, result.Message);
        Assert.Equal(2, result.Created);
        var plannings = db.Context.Plannings.AsNoTracking().Include(p => p.Requirements).Include(p => p.PracticeSchedule).OrderBy(p => p.GroupName).ToList();
        Assert.Equal(2, plannings.Count);
        var first = plannings[0];
        Assert.Equal(PlanningStatus.Approved, first.Status);
        Assert.Equal(Tuesday, first.ScheduledDate.Date);
        Assert.Equal(3, first.StartPeriodId);
        Assert.Equal(2, first.PeriodCount);
        Assert.Equal(GroupA, first.CourseGroupId);
        Assert.StartsWith("PRA-", first.Code);
        Assert.StartsWith("PRG-", first.PracticeSchedule!.Code);
        Assert.Equal(PracticeP1, first.PracticeSchedule.PracticeId);
        // Receta escalable: 1 × 12 estudiantes ÷ factor 4 = 3.
        var line = Assert.Single(first.Requirements);
        Assert.Equal(3m, line.RequestedQuantity);
        var batch = Assert.Single(db.Context.ImportBatches.AsNoTracking());
        Assert.Equal(PlanningImportService.SourceType, batch.SourceType);
        Assert.Equal(batch.Id, first.PracticeSchedule.ImportBatchId);
    }

    [Fact]
    public async Task Apply_LoadsNothing_WhileAnyRowIsUnresolved()
    {
        using var db = Seed();
        var file = File(Row(2), Row(3, practice: "PAN-P9"));

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, file, NoDecisions);

        Assert.False(result.Ok);
        Assert.Empty(db.Context.Plannings.AsNoTracking());
        Assert.Empty(db.Context.ImportBatches.AsNoTracking());
    }

    [Fact]
    public async Task AlreadyLoaded_NeedsDecision_ThenOverwriteMovesTheSamePlanning()
    {
        using var db = Seed();
        var service = Service(db);
        Assert.True((await service.ApplyAsync(TestDb.ManagementId, File(Row(2)), NoDecisions)).Ok);
        var originalId = db.Context.Plannings.AsNoTracking().Single().Id;

        // El mismo Excel otra vez: ya está cargada igual, no hay nada que hacer.
        var same = await Service(db).AnalyzeAsync(TestDb.ManagementId, File(Row(2)), NoDecisions);
        Assert.Equal(ImportRowAction.Unchanged, same.Rows[0].Action);

        // Con otra fecha: hay que decidir.
        var moved = File(Row(2, date: Wednesday, lab: "LAB-B"));
        var pending = await Service(db).AnalyzeAsync(TestDb.ManagementId, moved, NoDecisions);
        Assert.Equal(ImportRowAction.NeedsDecision, pending.Rows[0].Action);
        Assert.True(pending.Rows[0].CanOverwrite);
        Assert.False(pending.CanApply);

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, moved, new Dictionary<int, string> { [2] = ImportDecisions.Overwrite });
        Assert.True(result.Ok, result.Message);
        Assert.Equal(1, result.Overwritten);
        var planning = db.Context.Plannings.AsNoTracking().Include(p => p.Requirements).Single();
        Assert.Equal(originalId, planning.Id);
        Assert.Equal(Wednesday, planning.ScheduledDate.Date);
        Assert.Equal(TestDb.LabB, planning.LaboratoryId);
        Assert.Single(planning.Requirements);   // el material que ya tenía se conserva, no se duplica
    }

    [Fact]
    public async Task AlreadyLoadedWithDeliveredMaterial_CanOnlyBeSkipped()
    {
        using var db = Seed();
        Assert.True((await Service(db).ApplyAsync(TestDb.ManagementId, File(Row(2)), NoDecisions)).Ok);
        var planningId = db.Context.Plannings.AsNoTracking().Single().Id;
        db.AddDeparture(planningId, LoanStatus.Active);
        var moved = File(Row(2, date: Wednesday));

        var analysis = await Service(db).AnalyzeAsync(TestDb.ManagementId, moved, new Dictionary<int, string> { [2] = ImportDecisions.Overwrite });

        var row = analysis.Rows[0];
        Assert.False(row.CanOverwrite);
        Assert.Contains("material entregado", row.OverwriteBlockReason);
        Assert.Equal(ImportRowAction.NeedsDecision, row.Action);

        var skipped = await Service(db).AnalyzeAsync(TestDb.ManagementId, moved, new Dictionary<int, string> { [2] = ImportDecisions.Skip });
        Assert.Equal(ImportRowAction.Skip, skipped.Rows[0].Action);
    }

    // ───────────────────────────── archivo

    [Fact]
    public async Task Template_RoundTrips_ThroughReadFile()
    {
        using var db = Seed();
        var service = Service(db);
        var template = await service.BuildTemplateAsync(TestDb.ManagementId);

        using var package = new ExcelPackage(new MemoryStream(template));
        Assert.Equal(PlanningImportService.SheetRows, package.Workbook.Worksheets[0].Name);
        var sheet = package.Workbook.Worksheets[PlanningImportService.SheetRows];
        Assert.True(sheet.DataValidations.Count >= 5);
        var lists = package.Workbook.Worksheets[PlanningImportService.SheetLists];
        Assert.Equal("GAS-101 · Panadería", lists.Cells[2, 1].Text);
        // Se llena una fila como lo haría la persona: valores de las listas y una fecha de Excel.
        sheet.Cells[2, 2].Value = lists.Cells[2, 1].Text;
        sheet.Cells[2, 3].Value = "A1";
        sheet.Cells[2, 4].Value = lists.Cells[2, 2].Text;
        sheet.Cells[2, 6].Value = Tuesday;
        sheet.Cells[2, 7].Value = 3;
        sheet.Cells[2, 8].Value = 2;
        sheet.Cells[2, 9].Value = "LAB-A · Laboratorio A";
        sheet.Cells[2, 10].Value = "Ana Pérez Mamani";

        var file = service.ReadFile(new MemoryStream(package.GetAsByteArray()), "plan.xlsx");
        var analysis = await service.AnalyzeAsync(TestDb.ManagementId, file, NoDecisions);

        var row = Assert.Single(analysis.Rows);
        Assert.Equal(2, row.RowNumber);
        Assert.DoesNotContain(row.Issues, i => i.Level != ImportIssueLevel.Warning);
        Assert.Equal(PracticeP1, row.PracticeId);
        Assert.Equal(ImportRowAction.Create, row.Action);
    }

    [Fact]
    public void ReadFile_WithoutTheSheetOrColumns_ExplainsWhatIsMissing()
    {
        using var db = Seed();
        using var noSheet = new ExcelPackage();
        noSheet.Workbook.Worksheets.Add("Otra");
        var first = Assert.Throws<ImportFileException>(() => Service(db).ReadFile(new MemoryStream(noSheet.GetAsByteArray()), "x.xlsx"));
        Assert.Contains("Programacion", first.Message);

        using var noColumns = new ExcelPackage();
        var sheet = noColumns.Workbook.Worksheets.Add(PlanningImportService.SheetRows);
        sheet.Cells[1, 1].Value = "Materia";
        sheet.Cells[2, 1].Value = "GAS-101";
        var second = Assert.Throws<ImportFileException>(() => Service(db).ReadFile(new MemoryStream(noColumns.GetAsByteArray()), "x.xlsx"));
        Assert.Contains("Fecha", second.Message);
    }
}
