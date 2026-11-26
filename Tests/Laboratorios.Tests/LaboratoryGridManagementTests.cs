using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Ajustada por la grilla global (decisión 2026-11-23): una planificación de OTRA gestión que ocupa el ambiente
/// en las mismas fechas SÍ se dibuja y ocupa las celdas (misma verdad para todos los roles), pero el conteo del
/// selector de laboratorios sigue contando solo las de la gestión elegida.
/// </summary>
public class LaboratoryGridManagementTests
{
    [Fact]
    public async Task SameCalendarWeek_ShowsAnotherManagementsOccupancy_ButCountsOnlyItsOwn()
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

        var grid = await db.Grid().BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false);

        Assert.NotNull(grid);
        // Se dibujan ambas: la propia y la de la otra gestión (ocupa las mismas celdas que ve el docente).
        Assert.Equal([10, 20], grid!.Weeks.SelectMany(w => w.Days).SelectMany(d => d.Blocks).Select(b => b.PlanningId));
        var day = grid.Weeks.SelectMany(w => w.Days).Single(d => d.Date == date);
        Assert.All(new[] { 1, 2, 3, 4 }, order => Assert.True(day.IsBusy(order)));
        // El selector de laboratorios sigue contando solo las de la gestión elegida.
        Assert.Equal(1, grid.Laboratories.Single(l => l.Id == TestDb.LabA).PlanningCount);
    }
}
