using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.StudentGroups;

[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IStudentGroupService _groups;

    public EditModel(ApplicationDbContext context, IAcademicManagementService managements, IStudentGroupService groups)
    {
        _context = context;
        _managements = managements;
        _groups = groups;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public StudentGroupFormInput Input { get; set; } = new();

    public StudentGroupView? Group { get; private set; }
    public AcademicManagementSummary? Management { get; private set; }
    public StudentGroupFormOptions Options { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Group = await _groups.GetAsync(Id);
        if (Group == null)
        {
            TempData.Error("El grupo no existe.");
            return RedirectToPage("./Index");
        }

        Input = new StudentGroupFormInput
        {
            ManagementId = Group.ManagementId,
            CareerId = Group.CareerId,
            Semester = Group.Semester,
            Letter = Group.Letter,
            StudentCount = Group.StudentCount,
            ScalingFactor = Group.ScalingFactor,
            LaboratoryId = Group.LaboratoryId,
            Status = Group.Status,
            Subjects = Group.Subjects
                .Select(s => new StudentGroupFormSubject
                {
                    SubjectId = s.SubjectId,
                    Selected = true,
                    TeacherPersonId = s.TeacherPersonId,
                    // Laboratorio propio solo si difiere del habitual del grupo.
                    LaboratoryId = s.LaboratoryId == Group.LaboratoryId ? null : s.LaboratoryId
                })
                .ToList()
        };
        return await PageAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _groups.UpdateAsync(Id, Input.ToServiceInput());
        if (result.Error != null)
        {
            Group = await _groups.GetAsync(Id);
            if (Group == null)
            {
                TempData.Error(result.Error);
                return RedirectToPage("./Index");
            }
            ModelState.AddModelError(string.Empty, result.Error);
            Input.ManagementId = Group.ManagementId;
            Input.CareerId = Group.CareerId;
            Input.Semester = Group.Semester;
            return await PageAsync();
        }

        var group = (await _groups.GetAsync(Id))!;
        TempData.Success($"Grupo {group.Name} actualizado.");
        return RedirectToPage("./Index", new { managementId = group.ManagementId, careerId = group.CareerId });
    }

    private async Task<IActionResult> PageAsync()
    {
        Management = await _managements.ResolveAsync(Group!.ManagementId);
        Options = await StudentGroupFormOptions.LoadAsync(_context, Group.ManagementId);
        return Page();
    }
}
