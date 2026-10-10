using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

public class DayRecoveryServiceTests
{
    private static readonly DateTime Day = TestDb.Week1Monday.AddDays(1);

    private static DayRecoveryService Service(TestDb db) =>
        new(db.Context, new NoOpPlanningLock(), NullLogger<DayRecoveryService>.Instance);

    [Fact]
    public async Task Suspend_MarksAllOccupyingPlansAndPreservesTheirPreviousStatus()
    {
        using var db = new TestDb();
        db.AddPlanning(10, Day, 1, 2);
        db.AddPlanning(20, Day, 3, 2, status: PlanningStatus.Approved);
        db.AddPlanning(30, Day, 5, 2, status: PlanningStatus.Rescheduled);
        var id = db.Context.AcademicDays.AsNoTracking().Single(d => d.Date == Day).Id;

        var result = await new DayRecoveryService(db.Context, new NoOpPlanningLock(),
            NullLogger<DayRecoveryService>.Instance).SuspendAsync(id, "  Feriado  ");

        Assert.True(result.Ok, result.Message);
        Assert.Equal(2, result.PendingCount);
        Assert.Equal(PlanningStatus.PendingRecovery, db.Context.Plannings.AsNoTracking().Single(p => p.Id == 10).Status);
        Assert.Equal(PlanningStatus.Planned, db.Context.Plannings.AsNoTracking().Single(p => p.Id == 10).StatusBeforeRecovery);
        Assert.Equal(PlanningStatus.Approved, db.Context.Plannings.AsNoTracking().Single(p => p.Id == 20).StatusBeforeRecovery);
        Assert.Equal(PlanningStatus.Rescheduled, db.Context.Plannings.AsNoTracking().Single(p => p.Id == 30).Status);
        var academicDay = db.Context.AcademicDays.AsNoTracking().Single(d => d.Id == id);
        Assert.Equal(AcademicDayStatus.Suspended, academicDay.Status);
        Assert.Equal(result.RecoveryId, academicDay.DayRecoveryId);
        Assert.Equal("Feriado", db.Context.DayRecoveries.AsNoTracking().Single().Reason);

        var repeated = await new DayRecoveryService(db.Context, new NoOpPlanningLock(),
            NullLogger<DayRecoveryService>.Instance).SuspendAsync(id, "Otro");
        Assert.False(repeated.Ok);
        Assert.Single(db.Context.DayRecoveries.AsNoTracking());
    }

    [Fact]
    public async Task DayWithoutPlannings_DoesNotCreateRecovery()
    {
        using var db = new TestDb();
        var id = db.Context.AcademicDays.AsNoTracking().Single(d => d.Date == Day).Id;
        var result = await new DayRecoveryService(db.Context, new NoOpPlanningLock(),
            NullLogger<DayRecoveryService>.Instance).SuspendAsync(id, "Feriado");
        Assert.False(result.Ok);
        Assert.Empty(db.Context.DayRecoveries.AsNoTracking());
        Assert.Equal(AcademicDayStatus.Enabled, db.Context.AcademicDays.AsNoTracking().Single(d => d.Id == id).Status);
    }

    [Fact]
    public async Task SimpleSuspension_DoesNotCountPlansFromAnotherManagement()
    {
        using var db = new TestDb();
        db.AddPlanning(40, Day, 1, 2);
        db.Context.Managements.Add(new Management { Id = 2, Code = "2027-1", Year = 2027, Semester = 1 });
        var other = db.Context.Plannings.AsTracking().Single(p => p.Id == 40);
        other.ManagementId = 2;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var dayId = db.Context.AcademicDays.AsNoTracking().Single(d => d.Date == Day).Id;
        var result = await new AcademicCalendarService(db.Context, NullLogger<AcademicCalendarService>.Instance,
            new NoOpPlanningLock()).SetDayStatusAsync(dayId, AcademicDayStatus.Suspended, "Feriado");

        Assert.True(result.Ok, result.Message);
        Assert.Equal(AcademicDayStatus.Suspended, db.Context.AcademicDays.AsNoTracking().Single(d => d.Id == dayId).Status);
    }

    [Fact]
    public async Task SuspendByDate_WithCalendarDate_SuspendsDay()
    {
        using var db = new TestDb();
        db.AddPlanning(50, Day, 1, 2);

        var result = await Service(db).SuspendByDateAsync(TestDb.ManagementId, Day, "Feriado");

        Assert.True(result.Ok, result.Message);
        var academicDay = db.Context.AcademicDays.AsNoTracking().Single(d => d.Date == Day);
        Assert.Equal(AcademicDayStatus.Suspended, academicDay.Status);
        Assert.Equal(result.RecoveryId, academicDay.DayRecoveryId);
        Assert.Equal(PlanningStatus.PendingRecovery, db.Context.Plannings.AsNoTracking().Single(p => p.Id == 50).Status);
    }

    [Fact]
    public async Task SuspendByDate_OutsideCalendar_FailsWithExplicitMessage()
    {
        using var db = new TestDb();
        db.AddPlanning(51, Day, 1, 2);
        var outside = TestDb.Week2Monday.AddDays(7);   // domingo de la semana 3: sin calendario

        var result = await Service(db).SuspendByDateAsync(TestDb.ManagementId, outside, "Feriado");

        Assert.False(result.Ok);
        Assert.Contains($"La fecha {outside:dd/MM/yyyy} no pertenece al calendario académico", result.Message);
        Assert.Empty(db.Context.DayRecoveries.AsNoTracking());
        Assert.Equal(AcademicDayStatus.Enabled, db.Context.AcademicDays.AsNoTracking().Single(d => d.Date == Day).Status);
    }

    [Fact]
    public async Task SuspendByDate_AlreadySuspended_FailsWithExplicitMessage()
    {
        using var db = new TestDb();
        db.AddPlanning(52, Day, 1, 2);

        var result = await Service(db).SuspendByDateAsync(TestDb.ManagementId, TestDb.SuspendedDay, "Otro motivo");

        Assert.False(result.Ok);
        Assert.Contains("ya está suspendido o tiene una reposición", result.Message);
        Assert.Empty(db.Context.DayRecoveries.AsNoTracking());
    }

    [Fact]
    public async Task Preview_CountsPlanningsByLaboratory()
    {
        using var db = new TestDb();
        db.AddPlanning(53, Day, 1, 2);
        db.AddPlanning(54, Day, 3, 2, laboratoryId: TestDb.LabB);
        db.AddPlanning(55, Day, 5, 2);

        var preview = await Service(db).PreviewAsync(TestDb.ManagementId, Day);

        Assert.True(preview.Ok, preview.Message);
        Assert.Equal(Day.Date, preview.Date);
        Assert.Equal(3, preview.Total);
        Assert.Equal(2, preview.Labs.Count);
        var labA = preview.Labs.Single(l => l.LaboratoryId == TestDb.LabA);
        var labB = preview.Labs.Single(l => l.LaboratoryId == TestDb.LabB);
        Assert.Equal(2, labA.Count);
        Assert.Equal("LAB-A · Laboratorio A", labA.Label);
        Assert.Equal(1, labB.Count);
        Assert.Empty(preview.Blockers);
    }

    [Fact]
    public async Task Preview_WithIncompatibleStatus_ListsBlockersAndFails()
    {
        using var db = new TestDb();
        db.AddPlanning(56, Day, 1, 2);
        db.AddPlanning(57, Day, 3, 2, status: "EnRevisión");   // ocupa ambiente pero no es compatible

        var preview = await Service(db).PreviewAsync(TestDb.ManagementId, Day);

        Assert.False(preview.Ok);
        Assert.Single(preview.Blockers);
        Assert.Equal(57, preview.Blockers[0].PlanningId);
        Assert.Equal("PRA-00057", preview.Blockers[0].Code);
        Assert.Equal("EnRevisión", preview.Blockers[0].StatusLabel);
        Assert.Contains("1 planificación(es) en estado no compatible", preview.Message);
        Assert.Contains("PRA-00057", preview.Message);
    }
}
