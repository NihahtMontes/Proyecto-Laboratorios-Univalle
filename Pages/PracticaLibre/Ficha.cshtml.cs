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

/// <summary>Ficha del estudiante: datos, reservas, faltas y acciones manuales (registrar falta, suspender, levantar, dar de baja).</summary>
[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class FichaModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public FichaModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    public sealed record ReservationRow(string SlotCode, DateTime Date, string Lab, string Periods, string Purpose, SlotReservationStatus Status, string? Note);
    public sealed record IncidentRow(DateTime Date, StudentIncidentType Type, string? Note, string? By);

    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public StudentProfile? Profile { get; private set; }
    public List<ReservationRow> Reservations { get; private set; } = [];
    public List<IncidentRow> Incidents { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        Profile = await _db.StudentProfiles.AsNoTracking().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == Id);
        if (Profile == null) return NotFound();
        Reservations = (await _db.SlotReservations.AsNoTracking().Where(r => r.StudentProfileId == Id)
                .OrderByDescending(r => r.Slot!.Date)
                .Select(r => new { r.Slot!.Code, r.Slot.Date, Lab = r.Slot.Laboratory!.Code, Start = r.Slot.StartPeriod!.Order, r.Slot.PeriodCount,
                    r.Purpose, r.Status, Note = r.AttendanceNote ?? r.ReviewNote }).ToListAsync())
            .Select(r => new ReservationRow(r.Code, r.Date, r.Lab, Services.PlanningSlot.PeriodsText(r.Start, r.PeriodCount), r.Purpose, r.Status, r.Note))
            .ToList();
        var incidents = await _db.StudentIncidents.AsNoTracking().Where(i => i.StudentProfileId == Id)
            .OrderByDescending(i => i.CreatedDate).Select(i => new { i.CreatedDate, i.Type, i.Note, i.CreatedById }).ToListAsync();
        var authors = await _db.Users.AsNoTracking().Where(u => incidents.Select(i => i.CreatedById).Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName }).ToDictionaryAsync(u => u.Id, u => u.Name);
        Incidents = incidents.Select(i => new IncidentRow(i.CreatedDate, i.Type, i.Note,
            i.CreatedById is int by ? authors.GetValueOrDefault(by) : null)).ToList();
        return Page();
    }

    private IActionResult Done(StudentResult result)
    {
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostIncidentAsync(StudentIncidentType type, string? note) =>
        Done(await _students.AddIncidentAsync(Id, type, note ?? "", UserId));

    public async Task<IActionResult> OnPostSuspendAsync(DateTime? until, string? reason) =>
        Done(until is DateTime d ? await _students.SuspendAsync(Id, d, reason ?? "", UserId) : new StudentResult(false, "Elija hasta qué fecha."));

    public async Task<IActionResult> OnPostLiftAsync() => Done(await _students.LiftSuspensionAsync(Id));

    public async Task<IActionResult> OnPostDeactivateAsync(string? reason) => Done(await _students.DeactivateAsync(Id, reason ?? ""));
}
