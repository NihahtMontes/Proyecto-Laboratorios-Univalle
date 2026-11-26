using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.PracticaLibre;

/// <summary>La Inge abre cupos de práctica libre donde el laboratorio está libre y los cancela si hace falta.</summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class CuposModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public CuposModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Elija el laboratorio.")] public int? LaboratoryId { get; set; }
        [Required(ErrorMessage = "Elija la fecha.")] [DataType(DataType.Date)] public DateTime? Date { get; set; }
        [Required(ErrorMessage = "Elija el periodo inicial.")] public int? StartPeriodId { get; set; }
        [Range(1, 12, ErrorMessage = "Entre 1 y 12 periodos.")] public int PeriodCount { get; set; } = 4;
        [Range(1, 30, ErrorMessage = "Entre 1 y 30 estudiantes.")] public int Capacity { get; set; } = 6;
        [StringLength(500)] public string? Notes { get; set; }
    }

    [BindProperty] public InputModel Input { get; set; } = new();
    public List<(int Id, string Label)> Labs { get; private set; } = [];
    public List<ClassPeriod> Periods { get; private set; } = [];
    public IReadOnlyList<SlotView> Slots { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task LoadAsync()
    {
        Labs = (await _db.Laboratories.AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo && (l.Type == null || !TeacherRequestService.NonRequestableTypes.Contains(l.Type)))
                .OrderBy(l => l.Code).Select(l => new { l.Id, l.Code, l.Name }).ToListAsync())
            .Select(l => (l.Id, $"{l.Code} · {l.Name}")).ToList();
        Periods = await _db.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        Slots = await _students.ListSlotsAsync(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(120), includeCancelled: true);
    }

    public async Task OnGetAsync()
    {
        Input.Date ??= DateTime.Today.AddDays(1);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }
        var result = await _students.CreateSlotAsync(Input.LaboratoryId!.Value, Input.Date!.Value, Input.StartPeriodId!.Value,
            Input.PeriodCount, Input.Capacity, Input.Notes, UserId);
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await LoadAsync();
            return Page();
        }
        TempData.Success(result.Message);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        var result = await _students.CancelSlotAsync(id, reason ?? "");
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return RedirectToPage();
    }
}
