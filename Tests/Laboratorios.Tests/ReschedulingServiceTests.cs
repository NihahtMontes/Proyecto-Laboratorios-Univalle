using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Codes = Proyecto_Laboratorios_Univalle.Services.SlotValidationService.Codes;

namespace Laboratorios.Tests;

/// <summary>
/// Reprogramación individual con EF InMemory. El bloqueo es <see cref="NoOpPlanningLock"/> (InMemory no coordina
/// bloqueos) y la transacción se ignora (TestDb ignora TransactionIgnoredWarning): los casos de fallo se comprueban
/// antes del primer guardado, así que "sin cambios" se verifica igual que en SQL Server.
/// </summary>
public class ReschedulingServiceTests
{
    private const int OriginId = 10;
    private const int OtherId = 20;
    private const int ArticleId = 500;

    private static readonly DateTime OriginDate = TestDb.Week1Monday.AddDays(1);   // martes 06/10/2026
    private static readonly DateTime TargetDate = TestDb.Week1Monday.AddDays(2);   // miércoles 07/10/2026

    private static ReschedulingService Service(TestDb db) =>
        new(db.Context, db.Validation(), new NoOpPlanningLock(), NullLogger<ReschedulingService>.Instance);

    private static RescheduleCommand Command(DateTime date, int startPeriodId, int laboratoryId = TestDb.LabA, int teacher = TestDb.Teacher1,
        string reason = "Cambio de horario del docente", string[]? accepted = null, int? dayRecoveryId = null, int planningId = OriginId) =>
        new(planningId, date, startPeriodId, laboratoryId, teacher, reason, accepted ?? [], dayRecoveryId);

    private static Planning Load(TestDb db, int id) =>
        db.Context.Plannings.AsNoTracking().Include(p => p.Requirements).Single(p => p.Id == id);

    [Fact]
    public async Task Success_CreatesDestinationAndRegistersRescheduling()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(ArticleId, "ART-500", "Matraz 250 ml", stock: 50);
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.Context.PlanningRequirements.Add(new PlanningRequirement
        {
            PlanningId = OriginId, ArticleId = ArticleId, RequestedQuantity = 6, Observations = "Lavados", Code = "REQ-1"
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, TestDb.LabB, reason: "  Cambio de horario del docente  "));

        Assert.True(result.Ok, result.Message);
        Assert.Equal("Se reprogramó con éxito", result.Message);
        var destination = Load(db, result.DestinationPlanningId!.Value);
        var periods = db.Periods().ToDictionary(p => p.Order);
        Assert.Equal(3, destination.StartPeriodId);
        Assert.Equal(2, destination.PeriodCount);
        Assert.Equal(TargetDate + periods[3].StartTime, destination.ScheduledDate);
        Assert.Equal(TargetDate + periods[4].EndTime, destination.ScheduledEndDate);
        Assert.Equal(periods[3].StartTime, destination.StartTime);
        Assert.Equal(periods[4].EndTime, destination.EndTime);
        Assert.Equal(TestDb.LabB, destination.LaboratoryId);
        Assert.Equal(TestDb.Teacher1, destination.TeacherPersonId);
        Assert.Equal($"PRA-{destination.Id:00000}", destination.Code);
        Assert.Equal(PlanningStatus.Planned, destination.Status);
        Assert.Equal(TestDb.Week1Id, destination.AcademicWeekId);
        Assert.Equal(1, destination.AcademicWeek);
        Assert.Equal("Práctica 10", destination.PracticeName);
        Assert.Equal(OriginId * 10, destination.PracticeScheduleId);
        Assert.False(destination.AllLaboratories);

        var requirement = Assert.Single(destination.Requirements);
        Assert.Equal(ArticleId, requirement.ArticleId);
        Assert.Equal(6m, requirement.RequestedQuantity);
        Assert.Equal("Lavados", requirement.Observations);
        Assert.Null(requirement.Code);
        Assert.Single(Load(db, OriginId).Requirements);   // el origen conserva su material

        var origin = Load(db, OriginId);
        Assert.Equal(PlanningStatus.Rescheduled, origin.Status);
        Assert.Equal(1, origin.StartPeriodId);             // el origen no se mueve: queda atenuado en su lugar

        var rescheduling = Assert.Single(db.Context.Reschedulings.AsNoTracking());
        Assert.Equal(OriginId, rescheduling.OriginPlanningId);
        Assert.Equal(destination.Id, rescheduling.DestinationPlanningId);
        Assert.Equal(OriginDate, rescheduling.OriginDate);
        Assert.Equal(1, rescheduling.OriginStartPeriodId);
        Assert.Equal(2, rescheduling.OriginPeriodCount);
        Assert.Equal(TestDb.LabA, rescheduling.OriginLaboratoryId);
        Assert.Equal(TestDb.Teacher1, rescheduling.OriginTeacherPersonId);
        Assert.Equal(PlanningStatus.Planned, rescheduling.OriginStatus);
        Assert.Equal(TargetDate, rescheduling.DestinationDate);
        Assert.Equal(3, rescheduling.DestinationStartPeriodId);
        Assert.Equal(2, rescheduling.DestinationPeriodCount);
        Assert.Equal(TestDb.LabB, rescheduling.DestinationLaboratoryId);
        Assert.Equal("Cambio de horario del docente", rescheduling.Reason);
        Assert.Null(rescheduling.DayRecoveryId);
        Assert.Null(rescheduling.AcceptedWarnings);

        var schedule = db.Context.PracticeSchedules.AsNoTracking().Single(s => s.Id == OriginId * 10);
        Assert.Equal(PracticeScheduleStatus.Rescheduled, schedule.Status);
        Assert.Equal(TargetDate, schedule.ExecutionDate);
        Assert.Equal(periods[3].StartTime, schedule.StartTime);
        Assert.Equal(periods[4].EndTime, schedule.EndTime);
        Assert.Equal(TestDb.LabB, schedule.LaboratoryId);
    }

    [Theory]
    [InlineData(PlanningStatus.Approved, new string[0])]
    [InlineData(PlanningStatus.Finalized, new[] { Codes.Completed })]
    public async Task Destination_InheritsStatus(string originStatus, string[] accepted)
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: originStatus);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, accepted: accepted));

        Assert.True(result.Ok, result.Message);
        Assert.Equal(PlanningStatus.Approved, Load(db, result.DestinationPlanningId!.Value).Status);
        Assert.Equal(PlanningStatus.Rescheduled, Load(db, OriginId).Status);
        Assert.Equal(originStatus, db.Context.Reschedulings.AsNoTracking().Single().OriginStatus);
    }

    [Fact]
    public async Task CompletedOrigin_RecordsAcceptedWarnings()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: PlanningStatus.Finalized);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, accepted: [Codes.Completed]));

        Assert.True(result.Ok, result.Message);
        var json = db.Context.Reschedulings.AsNoTracking().Single().AcceptedWarnings;
        Assert.NotNull(json);
        Assert.Contains($"\"code\":\"{Codes.Completed}\"", json);
    }

    [Fact]
    public async Task OccupiedDestination_FailsWithoutChanges()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.AddPlanning(OtherId, TargetDate, 4, 2, teacherPersonId: TestDb.Teacher2);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3));

        Assert.False(result.Ok);
        Assert.NotNull(result.Validation);
        Assert.Contains(result.Validation!.Issues, i => i.Code == Codes.LabOccupied);
        AssertNothingChanged(db, plannings: 2);
    }

    [Fact]
    public async Task MovingSamePracticeOnePeriodLater_Succeeds()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 4);

        // La propia planificación está excluida: moverla 1–4 → 2–5 en el mismo ambiente y fecha es válido.
        var result = await Service(db).ExecuteAsync(Command(OriginDate, 2));

        Assert.True(result.Ok, result.Message);
        Assert.NotNull(result.Validation);
        Assert.DoesNotContain(result.Validation!.Issues, i => i.Code == Codes.LabOccupied);
    }

    [Fact]
    public async Task WarningNotAccepted_Fails()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Service(db).ExecuteAsync(Command(TestDb.Week2Monday.AddDays(1), 3));   // otra semana → WEEK_CHANGE

        Assert.False(result.Ok);
        Assert.Equal(ReschedulingService.ConditionsChangedMessage, result.Message);
        Assert.Contains(result.Validation!.Issues, i => i.Code == Codes.WeekChange);
        AssertNothingChanged(db, plannings: 1);
    }

    [Fact]
    public async Task AcceptedWarning_Succeeds()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        var target = TestDb.Week2Monday.AddDays(1);

        var result = await Service(db).ExecuteAsync(Command(target, 3, accepted: [Codes.WeekChange]));

        Assert.True(result.Ok, result.Message);
        var destination = Load(db, result.DestinationPlanningId!.Value);
        Assert.Equal(TestDb.Week2Id, destination.AcademicWeekId);
        Assert.Equal(2, destination.AcademicWeek);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyReason_Fails(string reason)
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, reason: reason));

        Assert.False(result.Ok);
        Assert.Contains("motivo", result.Message);
        AssertNothingChanged(db, plannings: 1);
    }

    [Fact]
    public async Task TooLongReason_Fails()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, reason: new string('x', 501)));

        Assert.False(result.Ok);
        AssertNothingChanged(db, plannings: 1);
    }

    [Fact]
    public async Task Departures_StayInOrigin()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2, status: PlanningStatus.Approved);
        db.AddDeparture(OriginId, LoanStatus.Active);

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, accepted: [Codes.OpenDeparture]));

        Assert.True(result.Ok, result.Message);
        var departure = Assert.Single(db.Context.Departures.AsNoTracking());
        Assert.Equal(OriginId, departure.PlanningId);
        Assert.False(db.Context.Departures.AsNoTracking().Any(d => d.PlanningId == result.DestinationPlanningId));
    }

    [Fact]
    public async Task DayRecovery_MarksRecoveredAndCompletesWhenNoneLeft()
    {
        using var db = new TestDb();
        const int SecondId = 11;
        db.AddPlanning(OriginId, TestDb.SuspendedDay, 1, 2, status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);
        db.AddPlanning(SecondId, TestDb.SuspendedDay, 3, 2, status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);
        SetStatusBeforeRecovery(db, OriginId, PlanningStatus.Approved);
        SetStatusBeforeRecovery(db, SecondId, PlanningStatus.Planned);
        db.Context.DayRecoveries.Add(new DayRecovery { Id = 1, ManagementId = TestDb.ManagementId, Date = TestDb.SuspendedDay, Reason = TestDb.SuspendedReason });
        db.Context.SaveChanges();
        var day = db.Context.AcademicDays.AsTracking().Single(d => d.Date == TestDb.SuspendedDay);
        day.DayRecoveryId = 1;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        var thursday = TestDb.SuspendedDay.AddDays(1);

        var first = await Service(db).ExecuteAsync(Command(thursday, 1, dayRecoveryId: 1));

        Assert.True(first.Ok, first.Message);
        Assert.Equal(PlanningStatus.Recovered, Load(db, OriginId).Status);
        Assert.Equal(PlanningStatus.Approved, Load(db, first.DestinationPlanningId!.Value).Status);
        Assert.Equal(1, db.Context.Reschedulings.AsNoTracking().Single().DayRecoveryId);
        Assert.Equal(DayRecoveryStatus.InProgress, db.Context.DayRecoveries.AsNoTracking().Single().Status);

        // P4–5 y no P3–4: debe quedar un periodo libre entre prácticas del ambiente y la primera
        // reposición ya ocupa P1–2 ese jueves.
        var second = await Service(db).ExecuteAsync(Command(thursday, 4, dayRecoveryId: 1, planningId: SecondId));

        Assert.True(second.Ok, second.Message);
        Assert.Equal(PlanningStatus.Recovered, Load(db, SecondId).Status);
        Assert.Equal(PlanningStatus.Planned, Load(db, second.DestinationPlanningId!.Value).Status);
        Assert.Equal(DayRecoveryStatus.Completed, db.Context.DayRecoveries.AsNoTracking().Single().Status);
    }

    [Fact]
    public async Task CompletedOriginInRecovery_RequiresExplicitWarningAcceptance()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, TestDb.SuspendedDay, 1, 2,
            status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);
        SetStatusBeforeRecovery(db, OriginId, PlanningStatus.Finalized);
        db.Context.DayRecoveries.Add(new DayRecovery
        {
            Id = 1, ManagementId = TestDb.ManagementId, Date = TestDb.SuspendedDay,
            Reason = TestDb.SuspendedReason
        });
        db.Context.SaveChanges();
        var academicDay = db.Context.AcademicDays.AsTracking().Single(d => d.Date == TestDb.SuspendedDay);
        academicDay.DayRecoveryId = 1;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var destinationDate = TestDb.SuspendedDay.AddDays(1);
        var rejected = await Service(db).ExecuteAsync(Command(destinationDate, 1, dayRecoveryId: 1));
        Assert.False(rejected.Ok);
        Assert.Contains(rejected.Validation!.Issues, i => i.Code == Codes.Completed);

        var accepted = await Service(db).ExecuteAsync(Command(destinationDate, 1,
            accepted: [Codes.Completed], dayRecoveryId: 1));
        Assert.True(accepted.Ok, accepted.Message);
        Assert.Equal(PlanningStatus.Approved, Load(db, accepted.DestinationPlanningId!.Value).Status);
    }

    [Fact]
    public async Task PendingRecoveryWithoutDayRecovery_Fails()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, TestDb.SuspendedDay, 1, 2, status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);

        var result = await Service(db).ExecuteAsync(Command(TestDb.SuspendedDay.AddDays(1), 1));

        Assert.False(result.Ok);
        Assert.Contains("reposición", result.Message);
        Assert.Equal(PlanningStatus.PendingRecovery, Load(db, OriginId).Status);
        Assert.Empty(db.Context.Reschedulings.AsNoTracking());
    }

    [Fact]
    public async Task PendingRecoveryWithoutPreviousStatus_CannotSilentlyBecomePlanned()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, TestDb.SuspendedDay, 1, 2,
            status: PlanningStatus.PendingRecovery, weekId: TestDb.Week2Id);
        db.Context.DayRecoveries.Add(new DayRecovery
        {
            Id = 1, ManagementId = TestDb.ManagementId, Date = TestDb.SuspendedDay,
            Reason = TestDb.SuspendedReason
        });
        db.Context.SaveChanges();
        var day = db.Context.AcademicDays.AsTracking().Single(d => d.Date == TestDb.SuspendedDay);
        day.DayRecoveryId = 1;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await Service(db).ExecuteAsync(Command(TestDb.SuspendedDay.AddDays(1), 1, dayRecoveryId: 1));

        Assert.False(result.Ok);
        AssertNothingChanged(db, plannings: 1);
        Assert.Equal(PlanningStatus.PendingRecovery, Load(db, OriginId).Status);
    }

    [Fact]
    public async Task DayRecoveryForNonPendingOrigin_Fails()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        db.Context.DayRecoveries.Add(new DayRecovery { Id = 1, ManagementId = TestDb.ManagementId, Date = OriginDate, Reason = "x" });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await Service(db).ExecuteAsync(Command(TargetDate, 3, dayRecoveryId: 1));

        Assert.False(result.Ok);
        AssertNothingChanged(db, plannings: 1);
    }

    [Fact]
    public async Task AlreadyRescheduledOrigin_CannotMoveAgain()
    {
        using var db = new TestDb();
        db.AddPlanning(OriginId, OriginDate, 1, 2);
        Assert.True((await Service(db).ExecuteAsync(Command(TargetDate, 3))).Ok);

        var again = await Service(db).ExecuteAsync(Command(TargetDate, 6));

        Assert.False(again.Ok);
        Assert.Contains(again.Validation!.Issues, i => i.Code == Codes.OriginNotMovable);
        Assert.Single(db.Context.Reschedulings.AsNoTracking());
    }

    [Theory]
    [InlineData(PlanningStatus.Planned, null, PlanningStatus.Planned)]
    [InlineData(PlanningStatus.Approved, null, PlanningStatus.Approved)]
    [InlineData(PlanningStatus.Finalized, null, PlanningStatus.Approved)]
    [InlineData(PlanningStatus.PendingRecovery, null, PlanningStatus.Planned)]
    [InlineData(PlanningStatus.PendingRecovery, PlanningStatus.Approved, PlanningStatus.Approved)]
    [InlineData(PlanningStatus.PendingRecovery, PlanningStatus.Finalized, PlanningStatus.Approved)]
    public void InheritedStatus_FollowsRules(string origin, string? before, string expected) =>
        Assert.Equal(expected, ReschedulingService.InheritedStatus(origin, before));

    private static void SetStatusBeforeRecovery(TestDb db, int planningId, string status)
    {
        var planning = db.Context.Plannings.AsTracking().Single(p => p.Id == planningId);
        planning.StatusBeforeRecovery = status;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void AssertNothingChanged(TestDb db, int plannings)
    {
        Assert.Equal(plannings, db.Context.Plannings.AsNoTracking().Count());
        Assert.Empty(db.Context.Reschedulings.AsNoTracking());
        Assert.NotEqual(PlanningStatus.Rescheduled, Load(db, OriginId).Status);
        Assert.False(db.Context.ChangeTracker.HasChanges());
    }
}
