using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

public class LaboratoryGridManagementTests
{
    [Fact]
    public async Task SameCalendarWeek_DoesNotShowAnotherManagementsPlannings()
    {
        using var db = new TestDb();
        var date = TestDb.Week1Monday.AddDays(1);
        db.AddPlanning(10, date, 1, 2);
        db.Context.Managements.Add(new Management { Id = 2, Code = "2027-1", Year = 2027, Semester = 1 });
        db.Context.AcademicWeeks.Add(new AcademicWeek
        {
            Id = 3, ManagementId = 2, Number = 1,
            StartDate = TestDb.Week1Monday, EndDate = TestDb.Week1Monday.AddDays(5)
        });
        db.Context.SaveChanges();
        db.AddPlanning(20, date, 3, 2);
        var other = db.Context.Plannings.AsTracking().Single(p => p.Id == 20);
        other.ManagementId = 2;
        other.AcademicWeekId = 3;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var grid = await new LaboratoryGridService(db.Context, new AcademicManagementService(db.Context))
            .BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false);

        Assert.NotNull(grid);
        Assert.Equal([10], grid.Weeks.SelectMany(w => w.Days).SelectMany(d => d.Blocks).Select(b => b.PlanningId));
        Assert.Equal(1, grid.Laboratories.Single(l => l.Id == TestDb.LabA).PlanningCount);
    }
}
