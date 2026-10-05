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

/// <summary>
/// Planificación académica (gestión + facultad + carrera + rango) y sus semanas académicas lunes-viernes.
/// </summary>
public class PlanificacionModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlanificacionModel> _logger;
    private readonly IPlanningService _planning;

    public PlanificacionModel(ApplicationDbContext context, ILogger<PlanificacionModel> logger, IPlanningService planning)
    {
        _planning = planning;
        _context = context;
        _logger = logger;
    }

    /// <summary>Id de AcademicPlannings cuando se edita.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public List<WeekInput> WeekInputs { get; set; } = [];

    public SelectList ManagementList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList CampusList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList FacultyList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList CareerList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    public string Code { get; private set; } = "Nueva planificación";
    public int PracticeCount { get; private set; }
    /// <summary>Con prácticas registradas no se cambian sede, facultad, carrera ni fecha de inicio.</summary>
    public bool StructureLocked => PracticeCount > 0;
    /// <summary>Estado de la planificación editada (null al crear).</summary>
    public AcademicPlanningStatus? Status { get; private set; }
    /// <summary>Las planificaciones cerradas solo se consultan.</summary>
    public bool IsReadOnly => Status == AcademicPlanningStatus.Completed;
    public bool HasActivePlanning { get; private set; }
    public List<WeekRow> Weeks { get; private set; } = [];

    public sealed record WeekRow(int Id, int Number, DateTime StartDate, DateTime EndDate, bool IsLective, string? Note, int PracticeCount);

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id.HasValue)
        {
            var planning = await _context.AcademicPlannings.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Id.Value);
            if (planning == null)
            {
                TempData.Error("La planificación no existe.");
                return RedirectToPage("./Index");
            }
            Input = InputModel.From(planning);
        }
        else
        {
            // Al crear, se propone activarla si no hay ninguna planificación activa.
            Input.Activate = await _planning.GetActiveAsync() == null;
        }

        await LoadPageAsync();
        return Page();
    }

    /// <summary>AJAX: carreras activas de una facultad.</summary>
    public async Task<JsonResult> OnGetCareersByFacultyAsync(int facultyId)
    {
        var careers = await _context.Careers.AsNoTracking()
            .Where(c => c.FacultadId == facultyId && c.Status == GeneralStatus.Activo)
            .OrderBy(c => c.Name)
            .Select(c => new { id = c.Id, name = c.Name })
            .ToListAsync();
        return new JsonResult(careers);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var management = await _context.Managements.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == Input.ManagementId && m.Status != ManagementStatus.Deleted && m.Semester > 0);
        if (management == null)
            ModelState.AddModelError("Input.ManagementId", "Selecciona una gestión semestral válida.");

        if (!await _context.Campuses.AsNoTracking().AnyAsync(c => c.Id == Input.CampusId && c.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.CampusId", "Selecciona una sede activa.");
        if (!await _context.Faculties.AsNoTracking().AnyAsync(f => f.Id == Input.FacultyId && f.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.FacultyId", "Selecciona una facultad activa.");
        if (!await _context.Careers.AsNoTracking()
                .AnyAsync(c => c.Id == Input.CareerId && c.FacultadId == Input.FacultyId && c.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.CareerId", "La carrera no pertenece a la facultad seleccionada.");

        IReadOnlyList<AcademicWeekGenerator.WeekSlot> slots = [];
        if (Input.StartDate != null && Input.EndDate != null)
        {
            if (Input.EndDate < Input.StartDate)
                ModelState.AddModelError("Input.EndMonth", "El mes de fin no puede ser anterior al de inicio.");
            else
            {
                try { slots = AcademicWeekGenerator.Generate(Input.StartDate.Value, Input.EndDate.Value); }
                catch (ArgumentException ex) { ModelState.AddModelError("Input.EndMonth", ex.Message); }
            }
        }

        if (await _context.AcademicPlannings.AsNoTracking()
                .AnyAsync(p => p.Id != (Id ?? 0) && p.ManagementId == Input.ManagementId && p.CampusId == Input.CampusId && p.CareerId == Input.CareerId))
            ModelState.AddModelError(string.Empty, "Ya existe una planificación para esa gestión, sede y carrera.");

        AcademicPlanning? planning = null;
        Dictionary<int, int> practicesByWeek = [];
        if (Id.HasValue)
        {
            planning = await _context.AcademicPlannings.AsTracking()
                .Include(p => p.Weeks)
                .FirstOrDefaultAsync(p => p.Id == Id.Value);
            if (planning == null)
            {
                TempData.Error("La planificación ya no existe.");
                return RedirectToPage("./Planificaciones");
            }
            if (planning.Status == AcademicPlanningStatus.Completed)
            {
                TempData.Error("La planificación está cerrada; actívala desde Planificaciones para modificarla.");
                return RedirectToPage("./Planificaciones");
            }

            practicesByWeek = await PracticesByWeekAsync(planning.Id);
            var hasPractices = await _context.L3Requests.AsNoTracking()
                .AnyAsync(r => r.AcademicPlanningId == planning.Id && r.Status != L3RequestStatus.Cancelled);
            if (hasPractices)
            {
                if (planning.CampusId != Input.CampusId || planning.FacultyId != Input.FacultyId || planning.CareerId != Input.CareerId)
                    ModelState.AddModelError(string.Empty, "La planificación ya tiene prácticas: no se puede cambiar la sede, la facultad ni la carrera.");
                if (Input.StartDate != null && AcademicWeekGenerator.MondayOf(planning.StartDate) != AcademicWeekGenerator.MondayOf(Input.StartDate.Value))
                    ModelState.AddModelError("Input.StartMonth", "La planificación ya tiene prácticas: no se puede cambiar el mes de inicio.");
            }

            var lastNumber = slots.Count;
            var droppedWithPractices = planning.Weeks
                .Where(w => w.Number > lastNumber && practicesByWeek.GetValueOrDefault(w.Id) > 0)
                .Select(w => w.Number)
                .ToList();
            if (droppedWithPractices.Count > 0)
                ModelState.AddModelError("Input.EndMonth",
                    $"No se puede acortar el rango: las semanas {string.Join(", ", droppedWithPractices)} tienen prácticas.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageAsync();
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var isNew = planning == null;
            planning ??= new AcademicPlanning();
            planning.ManagementId = Input.ManagementId;
            planning.CampusId = Input.CampusId;
            planning.FacultyId = Input.FacultyId;
            planning.CareerId = Input.CareerId;
            planning.Name = Input.Name.Trim();
            planning.StartDate = Input.StartDate!.Value;
            planning.EndDate = Input.EndDate!.Value;
            if (isNew) _context.AcademicPlannings.Add(planning);
            var closedOthers = Input.Activate && planning.Status != AcademicPlanningStatus.Active
                ? await _planning.ActivateAsync(planning)
                : 0;

            var existing = planning.Weeks.ToDictionary(w => w.Number);
            foreach (var slot in slots)
            {
                if (existing.TryGetValue(slot.Number, out var week))
                {
                    week.StartDate = slot.Monday;
                    week.EndDate = slot.Friday;
                }
                else
                {
                    planning.Weeks.Add(new AcademicWeek { Number = slot.Number, StartDate = slot.Monday, EndDate = slot.Friday });
                }
            }

            // Semanas que quedan fuera del rango y no tienen prácticas (validado arriba): filas de configuración sin historial.
            foreach (var week in planning.Weeks.Where(w => w.Number > slots.Count && w.Id > 0).ToList())
                _context.AcademicWeeks.Remove(week);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _planning.InvalidateCache();

            TempData.Success((isNew
                ? $"Planificación creada con {slots.Count} semanas académicas."
                : "Planificación actualizada.")
                + (planning.Status == AcademicPlanningStatus.Active ? " Es la planificación activa." : "")
                + (closedOthers > 0 ? " Se cerró la planificación que estaba activa." : ""));
            return RedirectToPage(new { id = planning.Id });
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
        {
            // Carrera concurrente: otra planificación ocupó la misma gestión, sede y carrera.
            await transaction.RollbackAsync();
            TempData.Error("Ya existe una planificación para esa gestión, sede y carrera.");
            return Id.HasValue ? RedirectToPage(new { id = Id }) : RedirectToPage("./Planificacion");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo guardar la planificación académica {PlanningId}", Id);
            TempData.Error("No se pudo guardar la planificación. Intente nuevamente.");
            return RedirectToPage(new { id = Id });
        }
    }

    /// <summary>Guarda lectiva/no lectiva y nota de cada semana.</summary>
    public async Task<IActionResult> OnPostWeeksAsync()
    {
        if (!Id.HasValue) return RedirectToPage("./Index");

        if (await _context.AcademicPlannings.AsNoTracking()
                .AnyAsync(p => p.Id == Id.Value && p.Status == AcademicPlanningStatus.Completed))
        {
            TempData.Error("La planificación está cerrada; sus semanas solo se consultan.");
            return RedirectToPage(new { id = Id });
        }

        var weeks = await _context.AcademicWeeks.AsTracking()
            .Where(w => w.AcademicPlanningId == Id.Value)
            .ToListAsync();
        if (weeks.Count == 0)
        {
            TempData.Error("La planificación no existe o no tiene semanas.");
            return RedirectToPage("./Index");
        }

        var byId = weeks.ToDictionary(w => w.Id);
        if (WeekInputs.Any(i => !byId.ContainsKey(i.Id)))
        {
            TempData.Error("Las semanas enviadas no pertenecen a esta planificación. Recargue la página.");
            return RedirectToPage(new { id = Id });
        }

        var practicesByWeek = await PracticesByWeekAsync(Id.Value);
        var blocked = WeekInputs
            .Where(i => !i.IsLective && byId[i.Id].IsLective && practicesByWeek.GetValueOrDefault(i.Id) > 0)
            .Select(i => byId[i.Id].Number)
            .ToList();
        if (blocked.Count > 0)
        {
            TempData.Error($"Las semanas {string.Join(", ", blocked)} tienen prácticas; muévalas antes de marcarlas como no lectivas.");
            return RedirectToPage(new { id = Id });
        }

        foreach (var input in WeekInputs)
        {
            var week = byId[input.Id];
            week.IsLective = input.IsLective;
            week.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        }

        await _context.SaveChangesAsync();
        TempData.Success("Semanas académicas actualizadas.");
        return RedirectToPage(new { id = Id });
    }

    private async Task<Dictionary<int, int>> PracticesByWeekAsync(int planningId) =>
        await _context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == planningId && r.AcademicWeekId != null && r.Status != L3RequestStatus.Cancelled)
            .GroupBy(r => r.AcademicWeekId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    private async Task LoadPageAsync()
    {
        var managements = await _context.Managements.AsNoTracking()
            .Where(m => m.Status != ManagementStatus.Deleted && m.Semester > 0)
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester)
            .Select(m => new { m.Id, m.Code })
            .ToListAsync();
        var campuses = await _context.Campuses.AsNoTracking()
            .Where(c => c.Status == GeneralStatus.Activo || c.Id == Input.CampusId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var faculties = await _context.Faculties.AsNoTracking()
            .Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name).ToListAsync();
        var careers = await _context.Careers.AsNoTracking()
            .Where(c => c.Status == GeneralStatus.Activo && c.FacultadId == Input.FacultyId)
            .OrderBy(c => c.Name).ToListAsync();

        ManagementList = new SelectList(managements, "Id", "Code", Input.ManagementId);
        CampusList = new SelectList(campuses, "Id", "Name", Input.CampusId);
        FacultyList = new SelectList(faculties, "Id", "Name", Input.FacultyId);
        CareerList = new SelectList(careers, "Id", "Name", Input.CareerId);
        HasActivePlanning = await _planning.GetActiveAsync() != null;

        if (!Id.HasValue) return;

        Status = await _context.AcademicPlannings.AsNoTracking()
            .Where(p => p.Id == Id.Value)
            .Select(p => (AcademicPlanningStatus?)p.Status)
            .FirstOrDefaultAsync();

        var header = await _context.AcademicPlannings.AsNoTracking()
            .Where(p => p.Id == Id.Value)
            .Select(p => new { ManagementCode = p.Management.Code, CareerCode = p.Career.Code })
            .FirstOrDefaultAsync();
        Code = L3Codes.Planning(header?.ManagementCode, header?.CareerCode, Id.Value);

        var practicesByWeek = await PracticesByWeekAsync(Id.Value);
        PracticeCount = await _context.L3Requests.AsNoTracking()
            .CountAsync(r => r.AcademicPlanningId == Id.Value && r.Status != L3RequestStatus.Cancelled);
        Weeks = (await _context.AcademicWeeks.AsNoTracking()
                .Where(w => w.AcademicPlanningId == Id.Value)
                .OrderBy(w => w.Number)
                .ToListAsync())
            .Select(w => new WeekRow(w.Id, w.Number, w.StartDate, w.EndDate, w.IsLective, w.Note, practicesByWeek.GetValueOrDefault(w.Id)))
            .ToList();
    }

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "La gestión es obligatoria")]
        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La sede es obligatoria")]
        [Display(Name = "Sede")]
        public int CampusId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La facultad es obligatoria")]
        [Display(Name = "Facultad")]
        public int FacultyId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La carrera es obligatoria")]
        [Display(Name = "Carrera")]
        public int CareerId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(150)]
        [Display(Name = "Nombre de la planificación")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Mes de inicio (yyyy-MM) elegido en el selector visual.</summary>
        [Required(ErrorMessage = "Selecciona el mes de inicio")]
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Mes de inicio inválido")]
        [Display(Name = "Mes de inicio")]
        public string? StartMonth { get; set; }

        /// <summary>Mes de fin (yyyy-MM) elegido en el selector visual.</summary>
        [Required(ErrorMessage = "Selecciona el mes de fin")]
        [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Mes de fin inválido")]
        [Display(Name = "Mes de fin")]
        public string? EndMonth { get; set; }

        [Display(Name = "Activar esta planificación al guardar")]
        public bool Activate { get; set; }

        /// <summary>Primer día del mes de inicio.</summary>
        public DateTime? StartDate => ParseMonth(StartMonth);

        /// <summary>Último día del mes de fin.</summary>
        public DateTime? EndDate => ParseMonth(EndMonth)?.AddMonths(1).AddDays(-1);

        private static DateTime? ParseMonth(string? value) =>
            DateTime.TryParseExact(value, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var month) ? month : null;

        public static InputModel From(AcademicPlanning planning) => new()
        {
            ManagementId = planning.ManagementId,
            CampusId = planning.CampusId ?? 0,
            FacultyId = planning.FacultyId,
            CareerId = planning.CareerId,
            Name = planning.Name,
            StartMonth = planning.StartDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            EndMonth = planning.EndDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            Activate = planning.Status == AcademicPlanningStatus.Active
        };
    }

    public sealed class WeekInput
    {
        public int Id { get; set; }
        public bool IsLective { get; set; }

        [StringLength(100)]
        public string? Note { get; set; }
    }
}
