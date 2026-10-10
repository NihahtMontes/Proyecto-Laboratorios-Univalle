using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.StudentGroups;

[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IStudentGroupService _groups;

    public CreateModel(ApplicationDbContext context, IAcademicManagementService managements, IStudentGroupService groups)
    {
        _context = context;
        _managements = managements;
        _groups = groups;
    }

    [BindProperty]
    public StudentGroupFormInput Input { get; set; } = new();

    /// <summary>Gestión pedida por query; vacío = la gestión actual.</summary>
    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Carrera preseleccionada (filtro del listado).</summary>
    [BindProperty(SupportsGet = true)]
    public int? CareerId { get; set; }

    /// <summary>Materia de origen (crear grupo desde una materia): preselecciona su carrera y semestre.</summary>
    [BindProperty(SupportsGet = true)]
    public int? SubjectId { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];
    public StudentGroupFormOptions Options { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Management = await _managements.ResolveAsync(ManagementId);
        if (Management == null)
        {
            TempData.Warning("Active o elija una gestión semestral para registrar grupos.");
            return RedirectToPage("./Index");
        }
        if (!Management.IsEditable)
        {
            TempData.Error($"La gestión {Management.Code} está completada: sus grupos son de solo lectura.");
            return RedirectToPage("./Index", new { managementId = Management.Id });
        }

        Input.ManagementId = Management.Id;
        Input.StudentCount = 20;
        if (SubjectId > 0)
        {
            var subject = await _context.Subjects.AsNoTracking()
                .Where(s => s.Id == SubjectId)
                .Select(s => new { s.CareerId, s.Semester })
                .FirstOrDefaultAsync();
            Input.CareerId = subject?.CareerId ?? 0;
            Input.Semester = subject?.Semester ?? 0;
        }
        else if (CareerId > 0)
        {
            Input.CareerId = CareerId.Value;
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _groups.CreateAsync(Input.ToServiceInput());
        if (result.Error != null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            Management = await _managements.ResolveAsync(Input.ManagementId);
            await LoadAsync();
            return Page();
        }

        var group = (await _groups.GetAsync(result.GroupId!.Value))!;
        TempData.Success($"Grupo {group.Name} creado con {group.Subjects.Count} materia(s). Ahora acomode su horario semanal.");
        return RedirectToPage("/Catalogs/CourseGroups/Horario",
            new { managementId = group.ManagementId, careerId = group.CareerId, cohort = group.CohortKey });
    }

    private async Task LoadAsync()
    {
        Managements = (await _managements.ListAsync()).Where(m => m.IsEditable).ToList();
        Options = await StudentGroupFormOptions.LoadAsync(_context, Input.ManagementId);
    }
}
