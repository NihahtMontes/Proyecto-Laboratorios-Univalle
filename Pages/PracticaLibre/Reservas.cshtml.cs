using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.PracticaLibre;

/// <summary>Bandeja de reservas: la Inge aprueba o rechaza cada pedido de lugar, viendo el material y si alcanza.</summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class ReservasModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;
    private readonly ITeacherRequestService _requests;

    public ReservasModel(ApplicationDbContext db, StudentPracticeService students, ITeacherRequestService requests)
    {
        _db = db;
        _students = students;
        _requests = requests;
    }

    public sealed record Row(int Id, int ProfileId, string Student, string Email, string? Phone, string Career, int Semester,
        int Incidents, string SlotCode, DateTime Date, string Lab, string Periods, int StartPeriodId, int PeriodCount,
        string Purpose, SlotReservationStatus Status, DateTime CreatedDate, IReadOnlyList<MaterialLineCheck> Material);

    public List<Row> Pending { get; private set; } = [];
    public List<Row> Upcoming { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var data = await _db.SlotReservations.AsNoTracking()
            .Where(r => r.Status == SlotReservationStatus.Pending || (r.Status == SlotReservationStatus.Approved && r.Slot!.Date >= today))
            .OrderBy(r => r.Slot!.Date).ThenBy(r => r.Slot!.StartPeriod!.Order).ThenBy(r => r.CreatedDate)
            .Select(r => new
            {
                r.Id, r.StudentProfileId, r.Student!.User!.FirstName, r.Student.User.LastName, r.Student.User.Email, r.Student.User.PhoneNumber,
                r.Student.Career, r.Student.Semester, Incidents = r.Student.Incidents.Count,
                SlotCode = r.Slot!.Code, r.Slot.Date, Lab = r.Slot.Laboratory!.Code, StartOrder = r.Slot.StartPeriod!.Order,
                r.Slot.StartPeriodId, r.Slot.PeriodCount, r.Purpose, r.Status, r.CreatedDate,
                Items = r.Items.Select(i => new { i.ArticleId, i.Quantity }).ToList()
            }).ToListAsync();
        foreach (var r in data)
        {
            IReadOnlyList<MaterialLineCheck> material = r.Items.Count == 0 ? []
                : await _requests.CheckMaterialAsync(r.Date, r.StartPeriodId, r.PeriodCount,
                    r.Items.Select(i => new RequestLineInput(i.ArticleId, i.Quantity)).ToList(), null);
            var row = new Row(r.Id, r.StudentProfileId, $"{r.FirstName} {r.LastName}", r.Email ?? "", r.PhoneNumber, r.Career, r.Semester,
                r.Incidents, r.SlotCode, r.Date, r.Lab, Services.PlanningSlot.PeriodsText(r.StartOrder, r.PeriodCount),
                r.StartPeriodId, r.PeriodCount, r.Purpose, r.Status, r.CreatedDate, material);
            (r.Status == SlotReservationStatus.Pending ? Pending : Upcoming).Add(row);
        }
    }

    public async Task<IActionResult> OnPostReviewAsync(int id, bool approve, string? note)
    {
        var result = await _students.ReviewReservationAsync(id, approve, note, UserId);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage();
    }
}
