using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;

namespace Proyecto_Laboratorios_Univalle.Pages.Estudiante;

/// <summary>Portal del estudiante: cupos de práctica libre disponibles y sus reservas (cancelar hasta 24 h antes).</summary>
[Authorize(Roles = AuthorizationHelper.RoleStudent)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public IndexModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    public sealed record MyReservation(int Id, int SlotId, string SlotCode, DateTime StartsAt, string Lab, string TimeText,
        string Purpose, SlotReservationStatus Status, string? Note, int Items)
    {
        public bool CanCancel => Status is SlotReservationStatus.Pending or SlotReservationStatus.Approved
            && StartsAt - DateTime.Now >= StudentPracticeService.CancelWindow;
    }

    public StudentProfile? Profile { get; private set; }
    public IReadOnlyList<SlotView> Slots { get; private set; } = [];
    public List<MyReservation> Mine { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGetAsync()
    {
        Profile = await _db.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == UserId);
        var now = DateTime.Now;
        var slots = await _students.ListSlotsAsync(DateTime.Today, DateTime.Today.AddDays(60));
        var all = slots.ToDictionary(s => s.Id);
        Mine = Profile == null ? [] : (await _db.SlotReservations.AsNoTracking()
                .Where(r => r.StudentProfileId == Profile.Id).OrderByDescending(r => r.Slot!.Date).Take(40)
                .Select(r => new { r.Id, r.FreePracticeSlotId, r.Slot!.Code, r.Slot.Date, StartTime = (TimeSpan?)r.Slot.StartPeriod!.StartTime,
                    Lab = r.Slot.Laboratory!.Code, Start = r.Slot.StartPeriod.Order, r.Slot.PeriodCount, r.Purpose, r.Status,
                    Note = r.ReviewNote ?? r.AttendanceNote, Items = r.Items.Count }).ToListAsync())
            .Select(r => new MyReservation(r.Id, r.FreePracticeSlotId, r.Code, r.Date.Date + (r.StartTime ?? TimeSpan.Zero), r.Lab,
                all.TryGetValue(r.FreePracticeSlotId, out var s) ? s.TimeText : Services.PlanningSlot.PeriodsText(r.Start, r.PeriodCount),
                r.Purpose, r.Status, r.Note, r.Items)).ToList();
        var holding = Mine.Where(m => m.Status is SlotReservationStatus.Pending or SlotReservationStatus.Approved).Select(m => m.SlotId).ToHashSet();
        Slots = slots.Where(s => s.StartsAt > now && s.Free > 0 && !holding.Contains(s.Id)).ToList();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var result = await _students.CancelReservationAsync(UserId, id);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage();
    }
}
