using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.StudentPractice;

namespace Proyecto_Laboratorios_Univalle.Pages.Estudiante;

/// <summary>Pedir un lugar en un cupo: qué va a practicar y el material del catálogo con cantidades (L-3 simple).</summary>
[Authorize(Roles = AuthorizationHelper.RoleStudent)]
public class ReservarModel : PageModel
{
    public const int MaxLines = 10;

    private readonly ApplicationDbContext _db;
    private readonly StudentPracticeService _students;

    public ReservarModel(ApplicationDbContext db, StudentPracticeService students)
    {
        _db = db;
        _students = students;
    }

    [BindProperty(SupportsGet = true)] public int SlotId { get; set; }
    [BindProperty] public string Purpose { get; set; } = "";
    /// <summary>Texto elegido de la lista («CÓDIGO · Nombre») y cantidad, por fila.</summary>
    [BindProperty] public List<string> Articles { get; set; } = [];
    [BindProperty] public List<decimal?> Quantities { get; set; } = [];

    public SlotView? Slot { get; private set; }
    public List<string> Catalog { get; private set; } = [];

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> LoadAsync()
    {
        Slot = (await _students.ListSlotsAsync(DateTime.Today, DateTime.Today.AddDays(60))).FirstOrDefault(s => s.Id == SlotId);
        Catalog = await _db.Articles.AsNoTracking().Where(a => a.Status == GeneralStatus.Activo)
            .OrderBy(a => a.Name).Select(a => a.Code + " · " + a.Name).ToListAsync();
        return Slot != null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync())
        {
            TempData.Error("El cupo ya no está disponible.");
            return RedirectToPage("./Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var lines = new List<ReservationLine>();
        var codes = Articles.Select(t => (t ?? "").Split('·')[0].Trim()).ToList();
        var wanted = codes.Where(c => c.Length > 0).Distinct().ToList();
        var ids = await _db.Articles.AsNoTracking().Where(a => wanted.Contains(a.Code) && a.Status == GeneralStatus.Activo)
            .ToDictionaryAsync(a => a.Code, a => a.Id);
        for (var i = 0; i < Math.Min(codes.Count, MaxLines); i++)
        {
            if (codes[i].Length == 0) continue;
            if (!ids.TryGetValue(codes[i], out var articleId))
                ModelState.AddModelError(string.Empty, $"«{Articles[i]}» no está en el catálogo: elígelo de la lista.");
            else if (Quantities.ElementAtOrDefault(i) is not decimal q || q <= 0)
                ModelState.AddModelError(string.Empty, $"Indica la cantidad de «{Articles[i]}».");
            else
                lines.Add(new ReservationLine(articleId, q));
        }
        if (ModelState.IsValid)
        {
            var result = await _students.ReserveAsync(UserId, SlotId, Purpose, lines);
            if (result.Ok)
            {
                TempData.Success(result.Message);
                return RedirectToPage("./Index");
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }
        if (!await LoadAsync()) return RedirectToPage("./Index");
        return Page();
    }
}
