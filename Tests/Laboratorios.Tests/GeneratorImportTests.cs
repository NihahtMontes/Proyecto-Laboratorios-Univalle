using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OfficeOpenXml;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.PlanningImport;

namespace Laboratorios.Tests;

/// <summary>
/// Generador de la Inge (.xlsm con CONFIG, MATERIAS y PLANIFICA, sin horas): lectura, reparto de periodos
/// (decisión del usuario 2026-11-23) y carga con el mismo flujo de la plantilla, sin duplicar al reimportar.
/// </summary>
public class GeneratorImportTests
{
    private static readonly DateTime Day = new(2027, 3, 9);
    private static readonly Dictionary<int, string> NoDecisions = [];

    static GeneratorImportTests() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    private static GeneratorPeriodPlanner.Input In(int row, string lab, string teacher = "", string group = "", DateTime? date = null, string session = "") =>
        new(row, date ?? Day, lab, teacher, group, session);

    // ───────────────────────────── reparto de periodos

    [Fact]
    public void OneGroup_GoesToTheMorning_P2ToP5()
    {
        var plan = GeneratorPeriodPlanner.Plan([In(2, "H-05")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 4), plan[2].Slot);
        Assert.Empty(plan[2].Notes);
    }

    [Fact]
    public void TwoGroups_MorningAndAfternoon_InExcelOrder()
    {
        var plan = GeneratorPeriodPlanner.Plan([In(7, "H-05"), In(3, "H-05")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 4), plan[3].Slot);   // la fila 3 va primero
        Assert.Equal(new GeneratorPeriodPlanner.Slot(7, 4), plan[7].Slot);
    }

    [Fact]
    public void ThreeGroups_GoBackToBack_AndAreMarkedAsBusyDay()
    {
        var plan = GeneratorPeriodPlanner.Plan([In(2, "H-05"), In(3, "H-05"), In(4, "H-05")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(1, 4), plan[2].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(5, 4), plan[3].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(9, 4), plan[4].Slot);
        Assert.All(plan.Values, r => Assert.Contains(r.Notes, n => n.Contains("Día cargado")));
    }

    [Fact]
    public void FourthGroup_IsLeftForManualPlacement()
    {
        var plan = GeneratorPeriodPlanner.Plan([In(2, "H-05"), In(3, "H-05"), In(4, "H-05"), In(5, "H-05")]);
        Assert.Null(plan[5].Slot);
        Assert.Contains(plan[5].Notes, n => n.Contains("a mano"));
    }

    [Fact]
    public void TeacherBusyInAnotherLab_MovesToTheNextFreeBlock()
    {
        // Mismo docente en H-05 y H-01 por la mañana: la segunda fila pasa a la tarde.
        var plan = GeneratorPeriodPlanner.Plan([In(2, "H-05", teacher: "francisco galindo"), In(3, "H-01", teacher: "francisco galindo")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 4), plan[2].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(7, 4), plan[3].Slot);
        Assert.Contains(plan[3].Notes, n => n.Contains("Se movió a P7–P10") && n.Contains("el docente"));
    }

    [Fact]
    public void StudentGroupBusy_AlsoMoves_AndOtherDaysDoNotInterfere()
    {
        var plan = GeneratorPeriodPlanner.Plan([
            In(2, "H-05", group: "lgs|5|a"), In(3, "H-01", group: "lgs|5|a"),
            In(4, "H-01", group: "lgs|5|a", date: Day.AddDays(1))]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(7, 4), plan[3].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 4), plan[4].Slot);
    }

    // ───────────────────────────── sesiones (materia y grupo comparten bloque)

    [Fact]
    public void TwoRowsOfTheSameSession_SplitTheMorningBlock()
    {
        var plan = GeneratorPeriodPlanner.Plan([In(257, "H-05", session: "iga3g8|a"), In(258, "H-05", session: "iga3g8|a")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 2), plan[257].Slot);   // P2–P3
        Assert.Equal(new GeneratorPeriodPlanner.Slot(4, 2), plan[258].Slot);   // P4–P5
        Assert.All(plan.Values, r => Assert.Contains(r.Notes, n => n.Contains("comparten su bloque")));
    }

    [Fact]
    public void GeneratorDay_MixesSingleRowsAndASharedSession()
    {
        var plan = GeneratorPeriodPlanner.Plan([
            In(80, "H-05", teacher: "francisco galindo"),
            In(158, "H-05", teacher: "valeria merida"),
            In(257, "H-05", "pedro guereca", session: "iga3g8|a"),
            In(258, "H-05", "pedro guereca", session: "iga3g8|a"),
            In(264, "H-05", "pedro guereca", session: "iga3g8|a"),
            In(313, "H-07", "pedro guereca", session: "tsc300|a")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(1, 4), plan[80].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(5, 4), plan[158].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(9, 2), plan[257].Slot);   // P9–P10
        Assert.Equal(new GeneratorPeriodPlanner.Slot(11, 1), plan[258].Slot);  // P11
        Assert.Equal(new GeneratorPeriodPlanner.Slot(12, 1), plan[264].Slot);  // P12
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 4), plan[313].Slot);
        Assert.All(plan.Values, r => Assert.NotNull(r.Slot));
    }

    [Fact]
    public void PedroCase_MovesTheSecondSessionOfH07()
    {
        var plan = GeneratorPeriodPlanner.Plan([
            In(2, "H-07", "pedro guereca", session: "bas100|a"),
            In(82, "H-05", "francisco galindo", session: "cbc1g5|a"),
            In(160, "H-05", "valeria merida", session: "ctr1g5|a"),
            In(262, "H-05", "pedro guereca", session: "iga3g8|a"),
            In(315, "H-07", "pedro guereca", session: "tsc300|a")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(1, 4), plan[2].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(5, 4), plan[315].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(1, 4), plan[82].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(5, 4), plan[160].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(9, 4), plan[262].Slot);
        Assert.All(plan.Values, r => Assert.DoesNotContain(r.Notes, n => n.Contains("No se encontró")));
        Assert.Contains(plan[315].Notes, n => n.StartsWith("Se movió a P5–P8"));
    }

    [Fact]
    public void ImpossibleDay_ReportsNoFreeBlock()
    {
        // El mismo docente en 4 laboratorios con 3 grupos cada uno: solo hay 3 bloques seguidos, así que dos chocan sí o sí.
        var plan = GeneratorPeriodPlanner.Plan([
            In(1, "H-01", "pedro guereca"), In(2, "H-01", "ana perez"), In(3, "H-01", "luis rojas"),
            In(4, "H-02", "pedro guereca"), In(5, "H-02", "mario cruz"), In(6, "H-02", "eva loza"),
            In(7, "H-03", "pedro guereca"), In(8, "H-03", "julia vera"), In(9, "H-03", "omar tito"),
            In(10, "H-04", "pedro guereca"), In(11, "H-04", "nora sis"), In(12, "H-04", "ivan reyes")]);
        Assert.Contains(plan[4].Notes, n => n.Contains("No se encontró un bloque libre"));
    }

    [Fact]
    public void SixRowsOfOneSession_FourPeriodsAndTheRestManual()
    {
        var plan = GeneratorPeriodPlanner.Plan([
            In(10, "H-05", session: "iga3g8|a"), In(11, "H-05", session: "iga3g8|a"),
            In(12, "H-05", session: "iga3g8|a"), In(13, "H-05", session: "iga3g8|a"),
            In(14, "H-05", session: "iga3g8|a"), In(15, "H-05", session: "iga3g8|a")]);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(2, 1), plan[10].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(3, 1), plan[11].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(4, 1), plan[12].Slot);
        Assert.Equal(new GeneratorPeriodPlanner.Slot(5, 1), plan[13].Slot);
        Assert.Null(plan[14].Slot);
        Assert.Null(plan[15].Slot);
        Assert.All(new[] { 14, 15 }, r => Assert.Contains(plan[r].Notes, n => n.Contains("a mano")));
    }

    // ───────────────────────────── lectura

    [Theory]
    [InlineData("H-5", "H-05")]
    [InlineData("k-1", "K-01")]
    [InlineData("H-102", "H-102")]
    [InlineData("LAB-A", "LAB-A")]
    public void LabCodes_AreNormalized(string excel, string system) =>
        Assert.Equal(system, GeneratorWorkbookReader.NormalizeLabCode(excel));

    /// <summary>Generador mínimo: título de gestión, dos semanas (la 2 repite la 1), dos grupos y prácticas sin horas.</summary>
    private static MemoryStream Workbook(string title, string weekText, params object?[][] planning)
    {
        using var package = new ExcelPackage();
        var config = package.Workbook.Worksheets.Add("CONFIG");
        config.Cells[1, 1].Value = "CONFIGURACION";
        config.Cells[2, 1].Value = "TITULO_GLOBAL";
        config.Cells[2, 2].Value = title;
        config.Cells[10, 1].Value = "SEMANA_ACADEMICA";
        config.Cells[11, 1].Value = 1; config.Cells[11, 2].Value = weekText;
        config.Cells[12, 1].Value = 2; config.Cells[12, 2].Value = weekText;

        var subjects = package.Workbook.Worksheets.Add("MATERIAS");
        string[] sh = ["HABILITAR", "SEMESTRE", "CODIGO_MATERIA", "MATERIA", "CARRERA", "NOMBRE_DOCENTE", "GRUPO", "CANT_ESTUDIANTES", "FACTOR_MATERIA", "DIA", "HORA", "LABORATORIO", "DEMOSTRATIVA"];
        for (var c = 0; c < sh.Length; c++) subjects.Cells[1, c + 1].Value = sh[c];
        object?[][] groups =
        [
            ["SI", 5, "GAS-101", "PANADERIA", "LGS", "LIC ANA PEREZ", "A1", 12, 4, "MARTES", null, "LAB-A", 0],
            ["SI", 5, "GAS-101", "PANADERIA", "LGS", "ING LUIS ROJAS", "B1", 10, 4, "MARTES", null, "LAB-A", 1],
            ["NO", 5, "GAS-202", "REPOSTERIA", "LGS", "ING LUIS ROJAS", "A1", 10, 4, "MARTES", null, "LAB-A", 0],
        ];
        for (var r = 0; r < groups.Length; r++)
            for (var c = 0; c < groups[r].Length; c++) subjects.Cells[r + 2, c + 1].Value = groups[r][c];

        var plan = package.Workbook.Worksheets.Add("PLANIFICA");
        string[] ph = ["CODIGO_MATERIA", "MATERIA", "GRUPO", "SEMESTRE", "PRACTICA", "DESCRIPCION DE PRACTICA", "SEMANA_ACADEMICA", "Nombre Docente", "FECHA EJECUCION", "LABORATORIO", "CANT_ESTUDIANTES", "CARRERA"];
        for (var c = 0; c < ph.Length; c++) plan.Cells[1, c + 1].Value = ph[c];
        for (var r = 0; r < planning.Length; r++)
            for (var c = 0; c < planning[r].Length; c++) plan.Cells[r + 2, c + 1].Value = planning[r][c];
        package.Workbook.Worksheets.Add("GAS-101");   // hoja de recetas vacía

        return new MemoryStream(package.GetAsByteArray());
    }

    private static object?[] PlanRow(string group, object practice, DateTime date, string teacher, string lab = "LAB-A") =>
        ["GAS-101", "PANADERIA", group, 5, practice, "Masas", 1, teacher, date, lab, null, "LGS"];

    private static PlanningImportService Service(TestDb db) =>
        new(db.Context, db.Occupancy(), db.GroupSchedule(), NullLogger<PlanningImportService>.Instance);

    [Fact]
    public void ReadFile_RecognizesTheGenerator_AndPlansPeriods()
    {
        var monday = new DateTime(2027, 3, 8);
        var stream = Workbook("PEDIDO GENERAL GESTIÓN 1-2027", "SEMANA DEL 08/03/2027 AL 13/03/2027",
            PlanRow("A1", "E1P", monday.AddDays(1), "LIC ANA PEREZ", lab: "H-5"),
            PlanRow("B1", 11.1, monday.AddDays(1), "ING LUIS ROJAS", lab: "H-5"));
        var service = new PlanningImportService(null!, null!, null!, NullLogger<PlanningImportService>.Instance);

        var file = service.ReadFile(stream, "GENERADOR.xlsm");

        Assert.NotNull(file.Generator);
        Assert.Equal("2027-1", file.Generator!.ManagementCode);
        Assert.Equal(new DateTime(2027, 3, 8), file.Generator.ClassesStart);
        Assert.Equal(new DateTime(2027, 3, 13), file.Generator.ClassesEnd);
        Assert.Equal(2, file.Generator.Groups.Count);   // la materia con HABILITAR = NO no cuenta
        Assert.Contains(file.Generator.Notices, n => n.Contains("mismo rango"));

        Assert.Collection(file.Rows,
            r => { Assert.Equal("E1P", r.Practice); Assert.Equal("H-05", r.Laboratory); Assert.Equal("2", r.StartPeriod); Assert.Equal("4", r.PeriodCount); },
            r => { Assert.Equal("11.1", r.Practice); Assert.Equal("7", r.StartPeriod); Assert.Equal("4", r.PeriodCount); });
    }

    /// <summary>TestDb con la materia GAS-101 (prácticas P1 y P2 con receta), grupos A1 y B1 y dos docentes.</summary>
    private static TestDb Seed()
    {
        var db = new TestDb();
        var c = db.Context;
        c.Subjects.Add(new Subject { Id = 500, Code = "GAS-101", Name = "Panadería" });
        c.Practices.AddRange(
            new Practice { Id = 600, SubjectId = 500, Code = "PAN-P1", Name = "Masas básicas", Number = "P1" },
            new Practice { Id = 601, SubjectId = 500, Code = "PAN-P2", Name = "Masas fermentadas", Number = "P2" });
        c.Interns.AddRange(
            new Intern { Id = TestDb.Teacher1, Name = "Ana Pérez Mamani", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo },
            new Intern { Id = TestDb.Teacher2, Name = "Luis Rojas", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo });
        c.CourseGroups.AddRange(
            new CourseGroup { Id = 700, Code = "G-A", ManagementId = TestDb.ManagementId, SubjectId = 500, Name = "A1", TeacherPersonId = TestDb.Teacher1, StudentCount = 12, ScalingFactor = 4 },
            new CourseGroup { Id = 701, Code = "G-B", ManagementId = TestDb.ManagementId, SubjectId = 500, Name = "B1", TeacherPersonId = TestDb.Teacher2, StudentCount = 10, ScalingFactor = 4 });
        c.Articles.Add(new Article { Id = 800, Code = "UT-PALA", Name = "Pala de panadería" });
        c.PracticeRequirements.Add(new PracticeRequirement { Id = 900, PracticeId = 600, ArticleId = 800, Quantity = 1, IsScalable = true });
        c.SaveChanges();
        c.ChangeTracker.Clear();
        return db;
    }

    [Fact]
    public async Task SameDaySameLab_IsNotAClash_AndReimportingDoesNotDuplicate()
    {
        using var db = Seed();
        var tuesday = TestDb.Week1Monday.AddDays(1);
        var service = Service(db);
        ImportFile Read() => service.ReadFile(Workbook("PEDIDO GENERAL GESTIÓN 2-2026", "SEMANA DEL 05/10/2026 AL 10/10/2026",
            PlanRow("A1", "P1", tuesday, "LIC ANA PEREZ"),
            PlanRow("B1", "p2", tuesday, "ING LUIS ROJAS")), "GENERADOR.xlsm");

        var analysis = await service.AnalyzeAsync(TestDb.ManagementId, Read(), NoDecisions);

        Assert.All(analysis.Rows, r => Assert.Equal(ImportRowAction.Create, r.Action));
        Assert.Equal((2, 4), (analysis.Rows[0].StartOrder!.Value, analysis.Rows[0].PeriodCount!.Value));
        Assert.Equal((7, 4), (analysis.Rows[1].StartOrder!.Value, analysis.Rows[1].PeriodCount!.Value));
        // El nombre sin segundo apellido se resuelve sin avisar en cada fila.
        Assert.DoesNotContain(analysis.Rows[0].Issues, i => i.Column == "Docente");
        Assert.Contains(analysis.FileNotices, n => n.Contains("mismo rango"));

        var first = await service.ApplyAsync(TestDb.ManagementId, Read(), NoDecisions);
        Assert.True(first.Ok, first.Message);
        Assert.Equal(2, first.Created);

        var again = await Service(db).AnalyzeAsync(TestDb.ManagementId, Read(), NoDecisions);
        Assert.All(again.Rows, r => Assert.Equal(ImportRowAction.Unchanged, r.Action));
        Assert.Equal(2, await db.Context.Plannings.CountAsync(p => !p.IsDeleted));
    }

    [Fact]
    public void RowsOfARepeatedWeek_AreSkippedByDefault_AndDoNotTakePeriods()
    {
        var tuesday = new DateTime(2027, 3, 9);
        // Semanas 1 y 2 con el mismo rango (como la 6 y la 3 del Excel real): la fila de la semana 2 es dudosa.
        var stream = Workbook("PEDIDO GENERAL GESTIÓN 1-2027", "SEMANA DEL 08/03/2027 AL 13/03/2027",
            PlanRow("A1", "1", tuesday, "LIC ANA PEREZ"),
            ["GAS-101", "PANADERIA", "B1", 5, "2", "Masas", 2, "ING LUIS ROJAS", tuesday, "LAB-A", null, "LGS"]);
        var file = new PlanningImportService(null!, null!, null!, NullLogger<PlanningImportService>.Instance).ReadFile(stream, "g.xlsm");

        Assert.Equal("", file.Rows[0].DefaultDecision);
        Assert.Equal("2", file.Rows[0].StartPeriod);   // sola en el laboratorio: P2–P5
        Assert.Equal(ImportDecisions.Skip, file.Rows[1].DefaultDecision);
        Assert.Equal("", file.Rows[1].StartPeriod);
        Assert.Contains(file.Rows[1].Notes, n => n.StartsWith("Se omite: es de la semana 2"));
    }

    [Fact]
    public async Task GeneratorOfAnotherManagement_IsRejected()
    {
        using var db = Seed();
        var file = Service(db).ReadFile(Workbook("PEDIDO GENERAL GESTIÓN 1-2027", "SEMANA DEL 05/10/2026 AL 10/10/2026",
            PlanRow("A1", "P1", TestDb.Week1Monday.AddDays(1), "LIC ANA PEREZ")), "GENERADOR.xlsm");

        var ex = await Assert.ThrowsAsync<ImportFileException>(() => Service(db).AnalyzeAsync(TestDb.ManagementId, file, NoDecisions));
        Assert.Contains("2027-1", ex.Message);
    }
}
