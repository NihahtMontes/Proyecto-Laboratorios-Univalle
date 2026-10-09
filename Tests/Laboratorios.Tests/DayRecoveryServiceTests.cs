using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

public class DayRecoveryServiceTests
{
    private static readonly DateTime Day = TestDb.Week1Monday.AddDays(1);

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
}
