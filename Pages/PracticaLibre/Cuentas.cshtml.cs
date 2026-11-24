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

/// <summary>Cuentas de estudiantes: aprobar o rechazar registros nuevos y ver a todos con sus faltas.</summary>
[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class CuentasModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public CuentasModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    public sealed record Row(int Id, string Name, string Email, string? Phone, string Career, int Semester, StudentAccountStatus Status,
        DateTime CreatedDate, DateTime? SuspendedUntil, int Incidents, int Reservations);

    public List<Row> Pending { get; private set; } = [];
    public List<Row> Others { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task OnGetAsync()
    {
        var rows = await _db.StudentProfiles.AsNoTracking()
            .OrderBy(p => p.AccountStatus).ThenByDescending(p => p.CreatedDate)
            .Select(p => new Row(p.Id, p.User!.FirstName + " " + p.User.LastName + (p.User.SecondLastName != null ? " " + p.User.SecondLastName : ""),
                p.User.Email ?? "", p.User.PhoneNumber, p.Career, p.Semester, p.AccountStatus, p.CreatedDate, p.SuspendedUntil,
                p.Incidents.Count, p.Reservations.Count))
            .ToListAsync();
        Pending = rows.Where(r => r.Status == StudentAccountStatus.Pending).ToList();
        Others = rows.Where(r => r.Status != StudentAccountStatus.Pending).ToList();
    }

    public async Task<IActionResult> OnPostReviewAsync(int id, bool approve, string? note)
    {
        var result = await _students.ReviewAccountAsync(id, approve, note, UserId);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage();
    }
}
