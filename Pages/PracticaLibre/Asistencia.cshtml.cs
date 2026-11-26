using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;

namespace Proyecto_Laboratorios_Univalle.Pages.PracticaLibre;

/// <summary>Toma de asistencia de la práctica libre del día: asistió, no se presentó o mal uso (las dos últimas son faltas).</summary>
[Authorize(Roles = AuthorizationHelper.AttendanceRoles)]
public class AsistenciaModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public AsistenciaModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    public sealed record Row(int Id, string Student, string SlotCode, string Lab, string Periods, string Purpose,
        SlotReservationStatus Status, string? Note);

    [BindProperty(SupportsGet = true)] public DateTime? Date { get; set; }
    public List<Row> Rows { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGetAsync()
    {
        var day = (Date ?? DateTime.Today).Date;
        Date = day;
        Rows = (await _db.SlotReservations.AsNoTracking()
                .Where(r => r.Slot!.Date == day && (r.Status == SlotReservationStatus.Approved || r.Status == SlotReservationStatus.Attended
                    || r.Status == SlotReservationStatus.NoShow || r.Status == SlotReservationStatus.Misuse))
                .OrderBy(r => r.Slot!.StartPeriod!.Order).ThenBy(r => r.Slot!.Laboratory!.Code).ThenBy(r => r.Student!.User!.LastName)
                .Select(r => new
                {
                    r.Id, Name = r.Student!.User!.FirstName + " " + r.Student.User.LastName, r.Slot!.Code, Lab = r.Slot.Laboratory!.Code,
                    Start = r.Slot.StartPeriod!.Order, r.Slot.PeriodCount, r.Purpose, r.Status, r.AttendanceNote
                }).ToListAsync())
            .Select(r => new Row(r.Id, r.Name, r.Code, r.Lab, Services.PlanningSlot.PeriodsText(r.Start, r.PeriodCount), r.Purpose, r.Status, r.AttendanceNote))
            .ToList();
    }

    public async Task<IActionResult> OnPostMarkAsync(int id, SlotReservationStatus result, string? note, DateTime? date)
    {
        var outcome = await _students.MarkAttendanceAsync(id, result, note, UserId);
        if (outcome.Ok) TempData.Success(outcome.Message); else TempData.Error(outcome.Message);
        return RedirectToPage(new { date = date?.ToString("yyyy-MM-dd") });
    }
}
