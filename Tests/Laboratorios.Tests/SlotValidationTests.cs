using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Codes = Proyecto_Laboratorios_Univalle.Services.SlotValidationService.Codes;

namespace Laboratorios.Tests;

public class SlotValidationTests
{
    private const int OriginId = 10;
    private const int OtherId = 20;

    private static readonly DateTime OriginDate = TestDb.Week1Monday.AddDays(1);   // martes 06/10/2026
    private static readonly DateTime TargetDate = TestDb.Week1Monday.AddDays(2);   // miércoles 07/10/2026 (misma semana)

    private static Task<SlotValidationResult> Validate(TestDb db, DateTime date, int startPeriodId, int laboratoryId = TestDb.LabA, int teacher = TestDb.Teacher1)
        => db.Validation().ValidateAsync(new SlotRequest(OriginId, date, startPeriodId, laboratoryId, teacher));

    private static SlotIssue Single(SlotValidationResult result, string code) => Assert.Single(result.Issues, i => i.Code == code);

    [Fact]
    public async Task ValidTarget_HasNoIssues()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, TargetDate, 3);

        Assert.Empty(result.Issues);
        Assert.Equal("valid", result.Level);
    }

    [Fact]
    public async Task OccupiedLaboratory_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddPlanning(OtherId, TargetDate, 4, 2, teacherPersonId: TestDb.Teacher2, subject: "Física");

        var result = await Validate(db, TargetDate, 3);   // P3–P4 vs P4–P5

        var issue = Single(result, Codes.LabOccupied);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Equal("PRA-00020 · Física (A) · Periodos 4–5", Assert.Single(issue.Details));
        Assert.Equal("invalid", result.Level);
    }

    [Fact]
    public async Task ContiguousLaboratory_BlocksRequiringFreePeriod()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddPlanning(OtherId, TargetDate, 1, 4, teacherPersonId: TestDb.Teacher2);   // ocupa P1–P4

        var result = await Validate(db, TargetDate, 5);   // destino P5–P6: contiguo a P1–P4

        var issue = Single(result, Codes.LabOccupied);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Contains("Debe quedar un periodo libre entre prácticas del laboratorio LAB-A", issue.Message);
    }

    [Fact]
    public async Task FreePeriodBetweenPractices_DoesNotBlock()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddPlanning(OtherId, TargetDate, 1, 4, teacherPersonId: TestDb.Teacher2);   // ocupa P1–P4

        var result = await Validate(db, TargetDate, 6);   // destino P6–P7: queda P5 libre

        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.LabOccupied);
    }

    [Fact]
    public async Task OriginDoesNotBlockItself()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, OriginDate, 2);   // se solapa con su propio horario actual

        Assert.DoesNotContain(result.Issues, i => i.Code is Codes.LabOccupied or Codes.TeacherBusy);
        Assert.False(result.HasBlocking);
    }

    [Fact]
    public async Task LegacyPlanningWithoutPeriods_IsConvertedAndBlocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddLegacyPlanning(OtherId, TargetDate, new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0)); // P2–P3

        var result = await Validate(db, TargetDate, 3);

        var issue = Single(result, Codes.LabOccupied);
        Assert.EndsWith("Periodos 2–3", Assert.Single(issue.Details));
    }

    [Fact]
    public async Task SuspendedDay_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, TestDb.SuspendedDay, 3);

        var issue = Single(result, Codes.DayNotEnabled);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Contains(TestDb.SuspendedReason, issue.Message);
    }

    [Fact]
    public async Task Sunday_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, TestDb.Week1Monday.AddDays(6), 3);

        Assert.Contains("domingo", Single(result, Codes.DayNotEnabled).Message);
    }

    [Fact]
    public async Task DateOutsideManagement_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, new DateTime(2026, 11, 3), 3);

        var issue = Single(result, Codes.DayNotEnabled);
        Assert.Contains("fuera de la gestión", issue.Message);
    }

    [Fact]
    public async Task BlockOutOfRange_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 3);

        var result = await Validate(db, TargetDate, 11);

        var issue = Single(result, Codes.OutOfRange);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Contains("El bloque de 3 periodos no cabe", issue.Message);
    }

    [Fact]
    public async Task InactiveLaboratory_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, TargetDate, 3, TestDb.LabInactive);

        Assert.Equal(SlotSeverity.Block, Single(result, Codes.LabInvalid).Severity);
    }

    [Fact]
    public async Task GroupLargerThanCapacity_Blocks()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, students: 25);

        var result = await Validate(db, TargetDate, 3);   // LAB-A admite 20

        var issue = Single(result, Codes.Capacity);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Contains("25", issue.Message);
    }

    [Fact]
    public async Task MissingCapacity_IsInfoOnly()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, students: 25);

        var result = await Validate(db, TargetDate, 3, TestDb.LabB);

        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.Capacity);
        var info = Single(result, Codes.CapacityNotEvaluated);
        Assert.Equal(SlotSeverity.Info, info.Severity);
        Assert.Contains("capacidad del laboratorio LAB-B", info.Message);
        Assert.Equal("valid", result.Level);
    }

    [Fact]
    public async Task BusyTeacher_Warns()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddPlanning(OtherId, TargetDate, 4, 1, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);

        var result = await Validate(db, TargetDate, 3);

        Assert.Equal(SlotSeverity.Warning, Single(result, Codes.TeacherBusy).Severity);
        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.LabOccupied);
        Assert.Equal("warning", result.Level);
    }

    [Fact]
    public async Task ContiguousTeacherInOtherLaboratory_DoesNotWarn()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        // Misma fecha y docente, otro ambiente: P5–P6 es contiguo al destino P3–P4 pero no se solapa.
        db.AddPlanning(OtherId, TargetDate, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);

        var result = await Validate(db, TargetDate, 3);

        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.TeacherBusy);
        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.LabOccupied);
    }

    [Fact]
    public async Task WeekChange_Warns()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Validate(db, TestDb.Week2Monday.AddDays(1), 3);

        Assert.Equal(SlotSeverity.Warning, Single(result, Codes.WeekChange).Severity);
        Assert.False(result.HasBlocking);
    }

    [Fact]
    public async Task CompletedOrigin_Warns()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: PlanningStatus.Finalized);

        var result = await Validate(db, TargetDate, 3);

        Assert.Equal(SlotSeverity.Warning, Single(result, Codes.Completed).Severity);
        Assert.False(result.HasBlocking);
    }

    [Fact]
    public async Task OpenDeparture_Warns()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddDeparture(OriginId, LoanStatus.Active);

        var result = await Validate(db, TargetDate, 3);

        Assert.Equal(SlotSeverity.Warning, Single(result, Codes.OpenDeparture).Severity);
    }

    [Fact]
    public async Task InsufficientMaterial_WarnsWithShortage()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(500, "ART-500", "Matraz 250 ml", stock: 10);
        db.AddArticleWithStock(501, "ART-501", "Reactivo sin stock", stock: null);
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddRequirement(OriginId, 500, 6);
        db.AddRequirement(OriginId, 501, 1);
        // Otra práctica del día destino, en otro ambiente y con otro docente, que se cruza en P4.
        db.AddPlanning(OtherId, TargetDate, 4, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);
        db.AddRequirement(OtherId, 500, 6);
        // Una práctica que ya no ocupa (reprogramada) no demanda material.
        db.AddPlanning(30, TargetDate, 3, 1, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Rescheduled);
        db.AddRequirement(30, 500, 50);

        var result = await Validate(db, TargetDate, 3);   // P3–P4

        var shortage = Assert.Single(result.Shortages);
        Assert.Equal(500, shortage.ArticleId);
        Assert.Equal(6m, shortage.Requested);
        Assert.Equal(10m, shortage.Available);
        Assert.Equal(2m, shortage.Shortage);
        var issue = Single(result, Codes.Material);
        Assert.Equal(SlotSeverity.Warning, issue.Severity);
        Assert.Contains("faltan 2", Assert.Single(issue.Details));
        Assert.Contains("ART-501", Assert.Single(Single(result, Codes.MaterialNotEvaluated).Details));
        Assert.Equal("warning", result.Level);
    }

    [Fact]
    public async Task EnoughMaterialWhenPracticesDoNotOverlap()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(500, "ART-500", "Matraz 250 ml", stock: 10);
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddRequirement(OriginId, 500, 6);
        db.AddPlanning(OtherId, TargetDate, 5, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2);
        db.AddRequirement(OtherId, 500, 6);

        var result = await Validate(db, TargetDate, 3);   // P3–P4 termina antes de P5

        Assert.Empty(result.Shortages);
        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.Material);
    }

    [Theory]
    [InlineData(PlanningStatus.Rescheduled)]
    [InlineData(PlanningStatus.Recovered)]
    [InlineData(PlanningStatus.Cancelled)]
    public async Task RelocatedOrigin_CannotMove(string status)
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: status);

        var result = await Validate(db, TargetDate, 3);

        var issue = Assert.Single(result.Issues);
        Assert.Equal(Codes.OriginNotMovable, issue.Code);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
    }

    [Fact]
    public async Task PendingRecoveryOrigin_CanMove()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: PlanningStatus.PendingRecovery);

        var result = await Validate(db, TargetDate, 3);

        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.OriginNotMovable);
        Assert.Equal("valid", result.Level);
    }

    [Fact]
    public async Task MissingOrigin_Blocks()
    {
        using var db = new TestDb();

        var result = await Validate(db, TargetDate, 3);

        Assert.Equal(Codes.OriginNotFound, Assert.Single(result.Issues).Code);
    }

    // ==================== Horario de clases del grupo (destino) ====================

    private const int SubjectId = 500;       // Química General (carrera 10, semestre 3)
    private const int OtherSubjectId = 501;  // Física General (misma carrera y semestre)
    private const int GroupId = 600;         // grupo "A" de Química (el de la planificación origen)
    private const int SiblingGroupId = 601;  // grupo "A" de Física (hermano)

    /// <summary>Materia con carrera y semestre + grupo A de cada una (mismos estudiantes).</summary>
    private static void SeedSubjectGroups(TestDb db)
    {
        db.AddIntern(TestDb.Teacher1, "Ana Pérez");
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
    }

    [Fact]
    public async Task SiblingClassInDestinationPeriods_BlocksGroupInClass()
    {
        using var db = new TestDb();
        SeedSubjectGroups(db);
        db.AddGroupSlots(SiblingGroupId, (3, 4));   // el grupo tiene clase de Física el miércoles P4
        db.AddPlanning(OriginId, OriginDate, 1, 2, courseGroupId: GroupId);

        var result = await Validate(db, TargetDate, 3);   // destino P3–P4: rango [3,4]

        var issue = Single(result, Codes.GroupInClass);
        Assert.Equal(SlotSeverity.Block, issue.Severity);
        Assert.Contains("El grupo A tiene clase de otra materia en ese horario", issue.Message);
        Assert.Equal("Física General (A) · Periodo 4", Assert.Single(issue.Details));
        Assert.Equal("invalid", result.Level);
    }

    [Fact]
    public async Task GroupWithoutSchedule_IsInfoOnly()
    {
        using var db = new TestDb();
        SeedSubjectGroups(db);   // grupo sin slots de horario
        db.AddPlanning(OriginId, OriginDate, 1, 2, courseGroupId: GroupId);

        var result = await Validate(db, TargetDate, 3);

        var issue = Single(result, Codes.GroupNoSchedule);
        Assert.Equal(SlotSeverity.Info, issue.Severity);
        Assert.Contains("no tiene horario de clases registrado", issue.Message);
        Assert.False(result.HasBlocking);
        Assert.Equal("valid", result.Level);
    }

    [Fact]
    public async Task OwnClassInDestinationPeriods_IsInfoOnly()
    {
        using var db = new TestDb();
        SeedSubjectGroups(db);
        db.AddGroupSlots(GroupId, (3, 3));   // clase propia del grupo (misma materia) el miércoles P3
        db.AddPlanning(OriginId, OriginDate, 1, 2, courseGroupId: GroupId);

        var result = await Validate(db, TargetDate, 3);   // destino P3–P4: incluye P3 propio

        var issue = Single(result, Codes.GroupOwnClass);
        Assert.Equal(SlotSeverity.Info, issue.Severity);
        Assert.Contains("horario de clases del grupo en esta materia", issue.Message);
        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.GroupInClass);
        Assert.DoesNotContain(result.Issues, i => i.Code == Codes.GroupNoSchedule);
        Assert.False(result.HasBlocking);
    }
}
