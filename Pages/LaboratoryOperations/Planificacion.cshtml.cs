using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class PlanificacionModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PlanificacionModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public L3Request Plan { get; set; } = new L3Request();

    public SelectList LaboratoriesList { get; set; } = default!;
    public SelectList SubjectsList { get; set; } = default!;
    public SelectList GroupsList { get; set; } = default!;
    public SelectList TeachersList { get; set; } = default!;

    // Se muestra al crear exitosamente
    public L3Request? CreatedPlan { get; set; }
    public bool ShowConfirmation { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDropdowns();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Limpiar navegaciones que el model binder podría rellenar vacías
        ModelState.Remove("Plan.Subject");
        ModelState.Remove("Plan.Group");
        ModelState.Remove("Plan.Teacher");
        ModelState.Remove("Plan.Laboratory");

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        Plan.Status = "Planificado";
        Plan.CreatedDate = DateTime.UtcNow;

        _context.L3Requests.Add(Plan);
        await _context.SaveChangesAsync();

        // Recargar con navegaciones para mostrar confirmación
        CreatedPlan = await _context.L3Requests
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Group)
            .Include(r => r.Teacher)
            .FirstOrDefaultAsync(r => r.Id == Plan.Id);

        ShowConfirmation = true;
        await LoadDropdowns();
        return Page();
    }

    /// <summary>
    /// Endpoint AJAX: Devuelve las planificaciones existentes en JSON
    /// para el modal "Buscar Código de Planificación".
    /// </summary>
    public async Task<JsonResult> OnGetSearchPlansAsync()
    {
        var plans = await _context.L3Requests
            .Include(p => p.Laboratory)
            .Include(p => p.Subject)
            .Include(p => p.Teacher)
            .OrderByDescending(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id,
                p.PracticeName,
                Lab = p.Laboratory != null ? p.Laboratory.Name : "—",
                Subject = p.Subject != null ? p.Subject.Name : "—",
                Teacher = p.Teacher != null ? (p.Teacher.FirstName + " " + p.Teacher.LastName) : "—",
                Date = p.ScheduledDate.ToString("dd/MM/yyyy HH:mm"),
                p.Status
            })
            .ToListAsync();

        return new JsonResult(plans);
    }

    private async Task LoadDropdowns()
    {
        var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
        LaboratoriesList = new SelectList(labs, "Id", "Name");

        var subjects = await _context.Subjects.OrderBy(s => s.Name).ToListAsync();
        SubjectsList = new SelectList(subjects, "Id", "Name");

        var groups = await _context.Groups.OrderBy(g => g.Name).ToListAsync();
        GroupsList = new SelectList(groups, "Id", "Name");

        var teachers = await _context.Users
            .Where(u => u.Role != Models.Enums.UserRole.Administrador && 
                        u.Role != Models.Enums.UserRole.SuperAdmin)
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        TeachersList = new SelectList(teachers, "Id", "FullName");
    }
}
