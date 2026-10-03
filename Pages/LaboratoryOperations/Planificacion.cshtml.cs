using System.Globalization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class PlanificacionModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly LaboratoryPlanningDemoService _demoService;

    public PlanificacionModel(ApplicationDbContext context, LaboratoryPlanningDemoService demoService)
    {
        _context = context;
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList FacultyList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList CareerList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    public string StatusText { get; private set; } = "Nueva planificación";

    public async Task<IActionResult> OnGetAsync()
    {
        var plan = _demoService.Find(Id);
        if (!string.IsNullOrWhiteSpace(Id) && plan == null)
        {
            TempData.Error("La planificación solicitada no existe en esta sesión de demostración.");
            return RedirectToPage("./Index");
        }

        if (plan != null)
        {
            Input = ToInput(plan);
            StatusText = GetStatusText(plan.Status);
        }

        await LoadListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? id)
    {
        Id = id;

        if (!TryParseTime(Input.StartTime, out var startTime))
            ModelState.AddModelError("Input.StartTime", "Indica una hora de inicio válida.");

        if (!TryParseTime(Input.EndTime, out var endTime))
            ModelState.AddModelError("Input.EndTime", "Indica una hora de finalización válida.");

        if (ModelState.IsValid && endTime <= startTime)
            ModelState.AddModelError("Input.EndTime", "La hora de finalización debe ser posterior a la hora de inicio.");

        var faculty = await _context.Faculties
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Input.FacultyId && item.Status == GeneralStatus.Activo);
        var career = await _context.Careers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Input.CareerId && item.Status == GeneralStatus.Activo);
        var laboratory = await _context.Laboratories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Input.LaboratoryId && item.Status == GeneralStatus.Activo);

        if (faculty == null)
            ModelState.AddModelError("Input.FacultyId", "Selecciona una facultad activa.");

        if (career == null || career.FacultadId != Input.FacultyId)
            ModelState.AddModelError("Input.CareerId", "La carrera no pertenece a la facultad seleccionada.");

        if (laboratory == null || laboratory.FacultyId != Input.FacultyId)
            ModelState.AddModelError("Input.LaboratoryId", "El laboratorio no pertenece a la facultad seleccionada.");

        var existing = _demoService.Find(id);
        if (!string.IsNullOrWhiteSpace(id) && existing == null)
            ModelState.AddModelError(string.Empty, "La planificación ya no está disponible en esta sesión.");

        if (ModelState.IsValid)
        {
            var duplicate = _demoService.GetPlans().FirstOrDefault(plan =>
                !string.Equals(plan.Id, id, StringComparison.Ordinal)
                && plan.Status != LaboratoryPlanningDemoStatus.Cancelled
                && plan.LaboratoryId == Input.LaboratoryId
                && plan.PracticeDate.Date == Input.PracticeDate.Date
                && plan.StartTime < endTime
                && startTime < plan.EndTime);

            if (duplicate != null)
            {
                ModelState.AddModelError(string.Empty,
                    $"El laboratorio ya está ocupado por {duplicate.Subject} entre {duplicate.StartTime:hh\\:mm} y {duplicate.EndTime:hh\\:mm}.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var plan = existing ?? new LaboratoryPlanningDemo
        {
            Id = Guid.NewGuid().ToString("N"),
            Code = CreateCode(),
            CreatedAt = DateTime.UtcNow
        };

        plan.FacultyId = Input.FacultyId;
        plan.CareerId = Input.CareerId;
        plan.LaboratoryId = Input.LaboratoryId;
        plan.ResponsibleName = Input.ResponsibleName.Trim();
        plan.Subject = Input.Subject.Trim();
        plan.Group = Input.Group.Trim();
        plan.PracticeDate = Input.PracticeDate.Date;
        plan.StartTime = startTime;
        plan.EndTime = endTime;
        plan.MaterialCategory = Input.MaterialCategory;
        plan.PracticeNumber = Input.PracticeNumber;
        plan.Observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();
        if (plan.Status != LaboratoryPlanningDemoStatus.Completed)
            plan.Status = LaboratoryPlanningDemoStatus.Draft;

        _demoService.Save(plan);
        TempData.Success(existing == null
            ? "Planificación creada. Ahora define sus requerimientos."
            : "Planificación actualizada correctamente.");

        return RedirectToPage("./Requerimientos", new { id = plan.Id });
    }

    private async Task LoadListsAsync()
    {
        var faculties = await _context.Faculties
            .AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .ToListAsync();
        var careers = await _context.Careers
            .AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .ToListAsync();
        var laboratories = await _context.Laboratories
            .AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .ToListAsync();

        FacultyList = new SelectList(faculties, "Id", "Name", Input.FacultyId);
        CareerList = new SelectList(
            careers.Where(item => Input.FacultyId <= 0 || item.FacultadId == Input.FacultyId),
            "Id", "Name", Input.CareerId);
        LaboratoryList = new SelectList(
            laboratories.Where(item => Input.FacultyId <= 0 || item.FacultyId == Input.FacultyId),
            "Id", "Name", Input.LaboratoryId);
    }

    private string CreateCode()
    {
        var existingCodes = _demoService.GetPlans()
            .Select(plan => plan.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sequence = 1;
        string code;
        do
        {
            code = $"PLN-{DateTime.Today:yyyyMMdd}-{sequence:000}";
            sequence++;
        } while (existingCodes.Contains(code));

        return code;
    }

    private static InputModel ToInput(LaboratoryPlanningDemo plan) => new()
    {
        FacultyId = plan.FacultyId,
        CareerId = plan.CareerId,
        LaboratoryId = plan.LaboratoryId,
        ResponsibleName = plan.ResponsibleName,
        Subject = plan.Subject,
        Group = plan.Group,
        PracticeDate = plan.PracticeDate,
        StartTime = plan.StartTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        EndTime = plan.EndTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        MaterialCategory = plan.MaterialCategory,
        PracticeNumber = plan.PracticeNumber,
        Observations = plan.Observations
    };

    private static bool TryParseTime(string? value, out TimeSpan result)
    {
        return TimeSpan.TryParseExact(value, [@"hh\:mm", @"h\:mm", @"hh\:mm\:ss"],
                   CultureInfo.InvariantCulture, out result)
            || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);
    }

    private static string GetStatusText(LaboratoryPlanningDemoStatus status) => status switch
    {
        LaboratoryPlanningDemoStatus.PendingAvailability => "Pendiente de disponibilidad",
        LaboratoryPlanningDemoStatus.Approved => "Aprobada",
        LaboratoryPlanningDemoStatus.InProgress => "En operación",
        LaboratoryPlanningDemoStatus.Completed => "Cerrada",
        LaboratoryPlanningDemoStatus.Incident => "Con incidencia L-5",
        _ => "Borrador"
    };

    public sealed class InputModel
    {
        [Required(ErrorMessage = "La facultad es obligatoria")]
        public int FacultyId { get; set; }

        [Required(ErrorMessage = "La carrera es obligatoria")]
        public int CareerId { get; set; }

        [Required(ErrorMessage = "El laboratorio es obligatorio")]
        public int LaboratoryId { get; set; }

        [Required(ErrorMessage = "El responsable es obligatorio"), StringLength(150)]
        [Display(Name = "Docente responsable")]
        public string ResponsibleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La asignatura es obligatoria"), StringLength(200)]
        [Display(Name = "Materia / asignatura")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "El grupo es obligatorio"), StringLength(30)]
        public string Group { get; set; } = "A";

        [Required(ErrorMessage = "La fecha es obligatoria"), DataType(DataType.Date)]
        public DateTime PracticeDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public string StartTime { get; set; } = "08:00";

        [Required(ErrorMessage = "La hora de finalización es obligatoria")]
        public string EndTime { get; set; } = "11:00";

        [Required]
        public string MaterialCategory { get; set; } = "Gastronomía (Utensilios)";

        [Range(1, 999, ErrorMessage = "El número de práctica debe ser mayor a 0")]
        public int PracticeNumber { get; set; } = 1;

        [StringLength(1000)]
        public string? Observations { get; set; }
    }
}
