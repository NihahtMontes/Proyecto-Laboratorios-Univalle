using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.StudentGroups;

[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class IndexModel : PageModel
{
    private readonly IAcademicManagementService _managements;
    private readonly IStudentGroupService _groups;

    public IndexModel(IAcademicManagementService managements, IStudentGroupService groups)
    {
        _managements = managements;
        _groups = groups;
    }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CareerId { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];
    public IReadOnlyList<StudentGroupView> Groups { get; private set; } = [];
    /// <summary>Carreras con grupos en la gestión (filtro).</summary>
    public List<(int Id, string Name)> Careers { get; private set; } = [];
    public bool IsEditable => Management is { IsEditable: true };

    public async Task<IActionResult> OnGetAsync()
    {
        Managements = await _managements.ListAsync();
        Management = await _managements.ResolveAsync(ManagementId);
        if (ManagementId.HasValue && Management == null)
        {
            TempData.Error("La gestión solicitada no existe.");
            return RedirectToPage();
        }
        if (Management == null) return Page();

        var all = await _groups.ListAsync(Management.Id);
        Careers = all.Select(g => (g.CareerId, g.CareerName)).Distinct().OrderBy(c => c.CareerName).ToList();
        if (CareerId.HasValue && Careers.All(c => c.Id != CareerId))
            CareerId = null;
        Groups = CareerId.HasValue ? all.Where(g => g.CareerId == CareerId).ToList() : all;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var group = await _groups.GetAsync(id);
        var error = await _groups.DeleteAsync(id);
        if (error != null)
            TempData.Error(error);
        else
            TempData.Success($"Grupo {group?.Name} eliminado junto con sus materias y horario. Las planificaciones conservan el nombre del grupo.");
        return RedirectToPage(new { managementId = group?.ManagementId ?? ManagementId, careerId = CareerId });
    }
}
