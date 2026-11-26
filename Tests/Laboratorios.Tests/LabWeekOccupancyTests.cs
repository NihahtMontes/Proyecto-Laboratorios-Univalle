using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Laboratorios.Tests;

/// <summary>
/// Fuente única de ocupación de las grillas (decisión del usuario 2026-11-23): lo que bloquea una celda debe ser
/// idéntico en la grilla del admin, en Disponibilidad/NuevaSolicitud del docente y en reprogramar/reponer.
/// Incluye planificaciones de CUALQUIER gestión, "todos los laboratorios" y cupos de práctica libre; las
/// solicitudes pendientes no ocupan; la limpieza es una franja de 10 minutos calculada con las horas reales.
/// </summary>
public class LabWeekOccupancyTests
{
    private static async Task<HashSet<(DateTime Date, int Order)>> TeacherOccupiedAsync(TestDb db, DateTime from, DateTime to)
    {
        var week = await db.WeekOccupancy().GetAsync(TestDb.LabA, from, to, TestDb.ManagementId);
        return [.. week.Days.SelectMany(d => week.Periods.Where((p, i) => d.Cells[i].IsBusy).Select(p => (d.Date, p.Order)))];
    }

    private static async Task<HashSet<(DateTime Date, int Order)>> AdminOccupiedAsync(TestDb db)
    {
        var grid = await db.Grid().BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false);
        Assert.NotNull(grid);
        return [.. grid!.Weeks.SelectMany(w => w.Days)
            .SelectMany(d => Enumerable.Range(1, 12).Where(d.IsBusy).Select(order => (d.Date, order)))];
    }

    [Fact]
    public async Task OtherManagementPlanningAndFreeSlot_OccupyTheSameCells_ForAdminGridAndTeacherView()
    {
        using var db = new TestDb();
        var tuesday = TestDb.Week1Monday.AddDays(1);
        db.AddManagement(2, "2027-1", 2027, 1);
        db.AddForeignPlanning(30, 2, tuesday, 1, 2);                       // OTRA gestión: P1–P2
        db.AddFreePracticeSlot(40, "PL-0001", tuesday, 5, 2, 6);           // cupo de práctica libre: P5–P6
        db.AddPlanning(50, tuesday, 8, 2, status: PlanningStatus.Requested);   // pendiente: P8–P9 (no ocupa)

        var admin = await AdminOccupiedAsync(db);
        var teacher = await TeacherOccupiedAsync(db, TestDb.Week1Monday, TestDb.Week1Monday.AddDays(5));

        // (a) El conjunto de celdas ocupadas es el mismo para el admin y para el docente.
        Assert.Equal(teacher, admin);
        Assert.Contains((tuesday, 1), admin);
        Assert.Contains((tuesday, 2), admin);
        Assert.Contains((tuesday, 5), admin);
        Assert.Contains((tuesday, 6), admin);
        // (b) Las solicitudes pendientes no ocupan.
        Assert.DoesNotContain((tuesday, 8), admin);
        Assert.DoesNotContain((tuesday, 9), admin);
        Assert.All(teacher, cell => Assert.Equal(tuesday, cell.Date)); // el resto de la semana está libre

        // La grilla admin dibuja el cupo de práctica libre como bloque que ocupa, y el pendiente como "no ocupa".
        var day = (await db.Grid().BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false))!
            .Weeks.SelectMany(w => w.Days).Single(d => d.Date == tuesday);
        var slot = Assert.Single(day.Blocks, b => b.StatusCss == "slot");
        Assert.True(slot.Occupies);
        Assert.Equal("PL-0001", slot.Code);
        Assert.Equal("cupo 6", slot.GroupName);
        var pending = Assert.Single(day.Blocks, b => b.PlanningId == 50);
        Assert.False(pending.Occupies);
    }

    [Fact]
    public async Task PendingReviewRequests_NeverOccupy()
    {
        using var db = new TestDb();
        var wednesday = TestDb.Week1Monday.AddDays(2);
        db.AddPlanning(11, wednesday, 2, 3, status: PlanningStatus.Requested);
        db.AddPlanning(12, wednesday, 8, 2, status: PlanningStatus.Observed);

        var occupied = await TeacherOccupiedAsync(db, TestDb.Week1Monday, TestDb.Week1Monday.AddDays(5));
        Assert.Empty(occupied);

        var admin = await AdminOccupiedAsync(db);
        Assert.Empty(admin);
    }

    [Fact]
    public async Task AllLaboratoriesPlanning_OccupiesEveryActiveLaboratory()
    {
        using var db = new TestDb();
        var monday = TestDb.Week1Monday;
        var planning = new Planning
        {
            Id = 60,
            ManagementId = TestDb.ManagementId,
            LaboratoryId = null,
            AllLaboratories = true,
            TeacherPersonId = TestDb.Teacher2,
            PracticeName = "Práctica global",
            GroupName = "A",
            Status = PlanningStatus.Approved,
            RequestDate = monday,
            Subject = new Subject { Id = 600, Name = "Química" }
        };
        PlanningSlot.Apply(planning, monday, 1, 2, db.Periods());
        db.Context.Plannings.Add(planning);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var service = db.WeekOccupancy();
        foreach (var labId in new[] { TestDb.LabA, TestDb.LabB })
        {
            var week = await service.GetAsync(labId, monday, monday, TestDb.ManagementId);
            var day = week.Day(monday)!;
            Assert.All(day.Cells.Take(2), cell => Assert.True(cell.IsBusy));
            Assert.All(day.Cells.Skip(2), cell => Assert.False(cell.IsBusy));
        }

        // Un ambiente inactivo no recibe la ocupación (la grilla admin tampoco lo lista).
        var inactive = await service.GetAsync(TestDb.LabInactive, monday, monday, TestDb.ManagementId);
        Assert.DoesNotContain(inactive.Blocks, b => b.PlanningId == 60);
    }

    [Fact]
    public async Task CleaningStrip_UsesRealPeriodTimes()
    {
        using var db = new TestDb();
        var periods = (await db.Context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync())
            .Select(p => new LabPeriod(p.Id, p.Order, p.StartTime, p.EndTime)).ToList();

        // (c) P2–P5: receso posterior a P5 (12:05–12:15).
        Assert.Equal("12:05–12:15", LabWeekOccupancyService.CleaningAfter(periods, 5)!.Text);
        // Bloque que termina en P7: receso 13:55–14:05.
        Assert.Equal("13:55–14:05", LabWeekOccupancyService.CleaningAfter(periods, 7)!.Text);
        // P7–P10: no hay receso tras P10 → últimos 10 minutos de la práctica.
        Assert.Equal("16:35–16:45", LabWeekOccupancyService.CleaningAfter(periods, 10)!.Text);

        // La franja llega a la grilla del admin desde la fuente de ocupación.
        db.AddPlanning(10, TestDb.Week1Monday, 2, 4);
        var grid = await db.Grid().BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false);
        var block = grid!.Weeks.SelectMany(w => w.Days).SelectMany(d => d.Blocks).Single(b => b.PlanningId == 10);
        Assert.Equal("12:05–12:15", block.CleaningText);
    }

    [Fact]
    public async Task ContiguousPractices_AreAllowed_AndCleaningNeverBlocksAPeriod()
    {
        using var db = new TestDb();
        var monday = TestDb.Week1Monday;
        db.AddPlanning(10, monday, 1, 3, teacherPersonId: TestDb.Teacher1);   // P1–P3
        db.AddPlanning(11, monday, 4, 3, teacherPersonId: TestDb.Teacher2);   // P4–P6 (contigua)

        var week = await db.WeekOccupancy().GetAsync(TestDb.LabA, monday, monday, TestDb.ManagementId);
        var day = week.Day(monday)!;
        // P1–P6 ocupados solo por las prácticas; ningún periodo bloqueado por limpieza.
        Assert.All(Enumerable.Range(1, 6), order => Assert.Equal(LabCellState.Busy, day.Cells[order - 1].State));
        Assert.All(Enumerable.Range(7, 6), order => Assert.Equal(LabCellState.Free, day.Cells[order - 1].State));

        // La limpieza es una franja al cierre de cada bloque: receso tras P3 y últimos 10 min de P6 (sin receso).
        Assert.Equal("10:15–10:25", LabWeekOccupancyService.CleaningAfter(week.Periods, 3)!.Text);
        Assert.Equal("12:55–13:05", LabWeekOccupancyService.CleaningAfter(week.Periods, 6)!.Text);

        // La grilla del admin coincide: contiguas ocupadas, la siguiente libre (con "+") y sin separación.
        var grid = await db.Grid().BuildAsync(TestDb.ManagementId, TestDb.LabA, 1, null, false);
        var gridDay = grid!.Weeks[0].Days.Single(d => d.Date == monday);
        Assert.All(Enumerable.Range(1, 6), order => Assert.True(gridDay.IsBusy(order)));
        Assert.All(Enumerable.Range(7, 6), order => Assert.False(gridDay.IsBusy(order)));
        Assert.All(Enumerable.Range(1, 12), order => Assert.False(gridDay.IsBuffer(order)));
        var blocks = gridDay.Blocks.OrderBy(b => b.StartOrder).ToList();
        Assert.Equal(["10:15–10:25", "12:55–13:05"], blocks.Select(b => b.CleaningText));
        // Prácticas contiguas: ambos bloques ocupan.
        Assert.All(blocks, b => Assert.True(b.Occupies));
    }
}
