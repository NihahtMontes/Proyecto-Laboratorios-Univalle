using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Management Management { get; set; } = default!;

        // Dashboard Metrics
        public int TotalPlans { get; set; }
        public int CompletedPlans { get; set; }
        public double GlobalProgress { get; set; }

        public Dictionary<string, int> TopEquipmentTypes { get; set; } = new();
        public Dictionary<string, int> TopGroups { get; set; } = new();
        public Dictionary<string, int> TopLaboratories { get; set; } = new();
        
        public IList<ManagementPlan> OverduePlans { get; set; } = new List<ManagementPlan>();

        // Plan Table L-48
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

        // Sprint 3B: Métricas adicionales para el detalle de gestión
        public int TotalActivos       { get; set; }
        public int TotalVencidos      { get; set; }
        public int CountPendientes    { get; set; }
        public int CountBuenos        { get; set; }
        public int CountL6            { get; set; }
        public int CountL7            { get; set; }
        public int CountL8            { get; set; }
        public int CountSalida        { get; set; }
        public int CountDesembolso    { get; set; }
        // L-48: Planes para el calendario (formato para JSON)
        public List<ManagementPlanCalendarDto> CalendarEvents { get; set; } = new();
        // Filtros del cronograma L-48
        [BindProperty(SupportsGet = true)]
        public int?    LabFilterId       { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter    { get; set; }
        [BindProperty(SupportsGet = true)]
        public int?    TechFilterId      { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter      { get; set; }
        public SelectList LabFList  { get; set; } = default!;
        public SelectList TechFList { get; set; } = default!;
        // Filtros legacy (compatibilidad con vista anterior)
        [BindProperty(SupportsGet = true)]
        public string? SearchResponsible { get; set; }
        [BindProperty(SupportsGet = true)]
        public ManagementPlanStatus? FilterStatus { get; set; }
        // Tab activo (para el frontend)
        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "l48";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Managements == null)
            {
                return NotFound();
            }

            var m = await _context.Managements
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment)
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.Maintenance)
                        .ThenInclude(m => m!.Technician)   // B-6: Include del técnico
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.Verification)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (m == null)
            {
                return NotFound();
            }
            Management = m;

            // B-5: Bloque completo de cálculo de métricas Sprint 3B
            var allPlans = Management.ManagementPlans.ToList();
            var now = DateTime.Now;

            // Métricas Globales
            TotalActivos    = allPlans.Count;
            TotalPlans      = TotalActivos; // alias para compatibilidad con la vista
            CompletedPlans  = allPlans.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);
            GlobalProgress  = TotalActivos > 0 ? Math.Round((double)CompletedPlans / TotalActivos * 100, 1) : 0;
            TotalVencidos   = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < now);
            CountPendientes = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentState >= WizardEquipmentState.AwaitingRequest);
            CountBuenos     = allPlans.Count(p => p.CurrentState == WizardEquipmentState.VerifiedGood);

            // Contadores de fase (segunda fila dashboard)
            CountL6         = allPlans.Count(p => p.CurrentPhase == WizardPhase.Verification && p.CurrentState == WizardEquipmentState.PendingVerification);
            CountL7         = allPlans.Count(p => p.CurrentPhase == WizardPhase.TechnicalRequest);
            CountL8         = allPlans.Count(p => p.CurrentPhase == WizardPhase.Maintenance);
            CountSalida     = allPlans.Count(p => p.CurrentPhase == WizardPhase.Exit);
            CountDesembolso = allPlans.Count(p => p.CurrentPhase == WizardPhase.Disbursement);

            // Gráfico por tipo - misma lógica semántica que Index (B-3)
            TopEquipmentTypes = allPlans
                .Where(p => p.EquipmentUnit?.Equipment != null)
                .GroupBy(p =>
                    p.EquipmentUnit!.Equipment!.Category == EquipmentCategory.Utensil
                        ? "Utensilio"
                        : p.EquipmentUnit!.Equipment!.TypeClassification switch
                        {
                            EquipmentTypeClassification.Electronico => "Electrónico / Eléctrico",
                            EquipmentTypeClassification.Manual      => "Manual / Mecánico",
                            EquipmentTypeClassification.Mobiliario  => "Mobiliario",
                            EquipmentTypeClassification.Medicion    => "Instrumental de Medición",
                            EquipmentTypeClassification.Vidrio      => "Material de Vidrio",
                            EquipmentTypeClassification.Reactivo    => "Reactivo / Químico",
                            EquipmentTypeClassification.Informatico => "Informático",
                            _                                       => "Otro"
                        })
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            TopLaboratories = allPlans
                .Where(p => p.EquipmentUnit?.Laboratory != null)
                .GroupBy(p => p.EquipmentUnit!.Laboratory!.Name)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            OverduePlans = allPlans
                .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < now.AddDays(7))
                .OrderBy(p => p.PlannedDate)
                .ToList();

            // Listas para filtros del Cronograma L-48
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LabFList  = new SelectList(labs, "Id", "Name", LabFilterId);
            
            // Fix: OrderBy in-memory since FullName is [NotMapped]
            var techsList = await _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .ToListAsync();
            var techs = techsList.OrderBy(t => t.FullName).ToList();
            TechFList = new SelectList(techs, "Id", "FullName", TechFilterId);

            // Eventos para FullCalendar
            CalendarEvents = allPlans
                .Where(p => p.PlannedDate.HasValue)
                .Select(p => new ManagementPlanCalendarDto
                {
                    Title = p.EquipmentUnit?.Equipment?.Name ?? "Equipo",
                    Start = p.PlannedDate!.Value.ToString("yyyy-MM-dd"),
                    ClassName = p.PlanStatus switch
                    {
                        ManagementPlanStatus.Completed  => "bg-success",
                        ManagementPlanStatus.InProgress => "bg-warning",
                        _                               => "bg-primary"
                    }
                })
                .ToList();

            // B-7: Filtros ampliados del cronograma L-48
            var query = _context.ManagementPlans
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.Verification)
                .Where(p => p.ManagementId == id);

            if (LabFilterId.HasValue)
                query = query.Where(p => p.EquipmentUnit!.LaboratoryId == LabFilterId);
            if (!string.IsNullOrEmpty(CategoryFilter))
            {
                if (CategoryFilter == "Utensilio")
                    query = query.Where(p => p.EquipmentUnit!.Equipment!.Category == EquipmentCategory.Utensil);
                else if (Enum.TryParse<EquipmentTypeClassification>(CategoryFilter, out var cat))
                    query = query.Where(p => p.EquipmentUnit!.Equipment!.TypeClassification == cat);
            }
            if (TechFilterId.HasValue)
                query = query.Where(p => p.Maintenance!.TechnicianId == TechFilterId);
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Todos")
            {
                if (StatusFilter == "Externo")
                    query = query.Where(p => p.Maintenance!.ServiceType == ServiceType.External);
                else if (Enum.TryParse<ManagementPlanStatus>(StatusFilter, out var statusEnum))
                    query = query.Where(p => p.PlanStatus == statusEnum);
            }
            ManagementPlans = await query
                .OrderBy(p => p.EquipmentUnit!.Laboratory!.Name)
                .ThenBy(p => p.EquipmentUnit!.Equipment!.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSyncEquipmentsAsync(int id)
        {
            var management = await _context.Managements
                .Include(m => m.ManagementPlans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (management == null || management.Status != ManagementStatus.Active)
            {
                TempData.Error("No se puede sincronizar una gestión que no está activa.");
                return RedirectToPage(new { id });
            }

            // Obtener todos los IDs de equipos ya en esta gestión
            var existingEquipmentIds = management.ManagementPlans
                .Select(p => p.EquipmentUnitId)
                .Where(id => id.HasValue)
                .Cast<int>()
                .ToList();

            // Obtener equipos activos no incluidos aún
            var newEquipments = await _context.EquipmentUnits
                .Where(eu => eu.CurrentStatus != EquipmentStatus.Deleted && !existingEquipmentIds.Contains(eu.Id))
                .ToListAsync();

            if (!newEquipments.Any())
            {
                TempData.Info("Todos los equipos activos ya están incluidos en esta ronda.");
                return RedirectToPage(new { id });
            }

            foreach (var eu in newEquipments)
            {
                var plan = new ManagementPlan
                {
                    ManagementId = id,
                    EquipmentUnitId = eu.Id,
                    CurrentPhase = WizardPhase.Verification,
                    CurrentState = WizardEquipmentState.PendingVerification,
                    PlanStatus = ManagementPlanStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    PlannedDate = DateTime.Today.AddDays(7) // Valor por defecto
                };
                _context.ManagementPlans.Add(plan);
            }

            await _context.SaveChangesAsync();
            TempData.Success($"Se han sincronizado {newEquipments.Count} equipos nuevos a esta ronda.");

            return RedirectToPage(new { id });
        }
    }

    // B-4: DTO para el calendario de FullCalendar
    public class ManagementPlanCalendarDto
    {
        public string Title     { get; set; } = string.Empty;
        public string Start     { get; set; } = string.Empty; // formato: "yyyy-MM-dd"
        public string? End      { get; set; }
        public string ClassName { get; set; } = "bg-primary"; // colores FullCalendar
    }
}
