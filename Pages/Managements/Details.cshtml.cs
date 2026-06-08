using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    [ValidateAntiForgeryToken]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IManagementActivationService _managementActivation;
        private readonly IManagementContextService _managementContext;

        public DetailsModel(
            ApplicationDbContext context,
            IManagementActivationService managementActivation,
            IManagementContextService managementContext)
        {
            _context = context;
            _managementActivation = managementActivation;
            _managementContext = managementContext;
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
        // Sombreado de 8 semanas (calculado por Semester)
        public string ShadingStart { get; set; } = "";
        public string ShadingEnd   { get; set; } = "";
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
        public string ActiveTab { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (Type == "Corrective" && id == null)
            {
                id = await _context.Managements
                    .AsNoTracking()
                    .Where(m => m.Type == ManagementType.Corrective && m.Status == ManagementStatus.Active)
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Semester)
                    .ThenByDescending(m => m.CreatedDate)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();

                if (id == null)
                {
                    TempData.Warning("No hay una gestión correctiva activa. Cree una gestión correctiva semestral antes de reportar fallas.");
                    return RedirectToPage("./Index", new { Type = ManagementType.Corrective.ToString() });
                }
            }

            if (id == null || _context.Managements == null)
                return NotFound();

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
                TempData.Warning("La gestión solicitada fue eliminada o no existe.");
                return RedirectToPage("./Index");
            }
            Management = m;
            if (string.IsNullOrWhiteSpace(ActiveTab))
            {
                ActiveTab = Management.Type == ManagementType.Corrective ? "dashboard" : "l48";
            }
            ViewData["ManagementId"] = Management.Id;
            ViewData["ManagementType"] = Management.Type.ToString();

            // B-5: Bloque completo de cálculo de métricas Sprint 3B.
            // La operación preventiva solo cuenta planes cuya unidad física sigue vigente.
            var allPlans = Management.ManagementPlans
                .Where(p => p.EquipmentUnit != null)
                .ToList();
            var now = DateTime.Now;

            // Métricas Globales
            TotalActivos    = allPlans.Count;
            TotalPlans      = TotalActivos; // alias para compatibilidad con la vista
            CountBuenos     = allPlans.Count(p => p.CurrentState == WizardEquipmentState.VerifiedGood);
            CompletedPlans  = allPlans.Count(IsCompletedPlan);
            GlobalProgress  = TotalActivos > 0 ? Math.Round((double)(CountBuenos + CompletedPlans) / TotalActivos * 100, 1) : 0;
            TotalVencidos   = allPlans.Count(p => IsActivePipelinePlan(p) && p.PlannedDate.HasValue && p.PlannedDate.Value < now);
            CountPendientes = allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentState >= WizardEquipmentState.AwaitingRequest);

            // Contadores de fase (segunda fila dashboard)
            CountL6         = Management.Type == ManagementType.Corrective ? 0 : allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Verification && (p.CurrentState == WizardEquipmentState.PendingVerification || (p.IsDraft && p.DraftPhase == WizardPhase.Verification)));
            CountL7         = allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.TechnicalRequest);
            CountL8         = allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Maintenance);
            CountSalida     = allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Exit);
            CountDesembolso = allPlans.Count(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Disbursement);

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
                .GroupBy(p => LaboratoryDisplayHelper.Format(p.EquipmentUnit!.Laboratory))
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            OverduePlans = allPlans
                .Where(p => IsActivePipelinePlan(p) && p.PlannedDate.HasValue && p.PlannedDate.Value < now)
                .OrderBy(p => p.PlannedDate)
                .ToList();

            // Listas para filtros del Cronograma L-48
            var labs = await _context.Laboratories.OrderBy(l => l.Code).ThenBy(l => l.Name).ToListAsync();
            LabFList  = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", LabFilterId);
            
            // Fix: OrderBy in-memory since FullName is [NotMapped]
            var techsList = await _context.People
                .Where(p => p.Category == PersonCategory.Tecnico)
                .ToListAsync();
            var techs = techsList.OrderBy(t => t.FullName).ToList();
            TechFList = new SelectList(techs, "Id", "FullName", TechFilterId);

            // Eventos para FullCalendar — SOLO planes que tienen un mantenimiento registrado
            // (fase L8 Maintenance o posterior: L3 Salida, Kardex, Desembolso)
            CalendarEvents = allPlans
                .Where(p => p.CurrentPhase >= WizardPhase.Maintenance)
                .Select(p => {
                    // Fecha que se muestra: prioridad ScheduledDate > EndDate > PlannedDate
                    // Si Maintenance fue cancelado (soft delete), Maintenance será null por QueryFilter
                    // Usamos PlannedDate como fallback para no perder el plan del calendario
                    var displayDate = p.Maintenance?.ScheduledDate
                                   ?? p.Maintenance?.EndDate
                                   ?? p.PlannedDate
                                   ?? p.Maintenance?.CreatedDate
                                   ?? DateTime.Now;
                    // Título: "INV - NombreEquipo"
                    var inv  = p.EquipmentUnit?.InventoryNumber ?? "—";
                    var name = p.EquipmentUnit?.Equipment?.Name ?? "Equipo";
                    return new ManagementPlanCalendarDto
                    {
                        Title           = $"{inv} - {name}",
                        Start           = displayDate.ToString("yyyy-MM-dd"),
                        End             = null, // evento puntual
                        ClassName       = p.Maintenance?.Status == MaintenanceStatus.Completed
                            ? "ev-completed"
                            : Management.Type == ManagementType.Corrective ? "ev-corrective" : "ev-preventive",
                        InventoryNumber = inv,
                        LabName         = p.EquipmentUnit?.Laboratory?.Name ?? "—",
                        TechnicianName  = p.Maintenance?.Technician?.FullName ?? "Sin asignar",
                        MaintenanceId   = p.MaintenanceId,
                        PlanId          = p.Id,
                        CurrentPhaseInt = (int)p.CurrentPhase,
                        StatusLabel     = p.Maintenance?.Status switch
                        {
                            MaintenanceStatus.Completed  => "Completado",
                            MaintenanceStatus.InProgress => "En Progreso",
                            null                         => "Sin mantenimiento activo",
                            _                            => "Pendiente"
                        }
                    };
                })
                .ToList();

            // Cálculo del sombreado de 8 semanas según Semester:
            // Gestión I (sem=1): Junio + Julio del mismo año
            // Gestión II (sem=2): Diciembre del mismo año + Enero del año siguiente
            if (Management.Semester == 1)
            {
                ShadingStart = new DateTime(Management.Year, 6, 1).ToString("yyyy-MM-dd");
                ShadingEnd   = new DateTime(Management.Year, 8, 1).ToString("yyyy-MM-dd"); // 1 Ago exclusive
            }
            else
            {
                ShadingStart = new DateTime(Management.Year, 12, 1).ToString("yyyy-MM-dd");
                ShadingEnd   = new DateTime(Management.Year + 1, 2, 1).ToString("yyyy-MM-dd"); // 1 Feb exclusive
            }

            // B-7: Filtros ampliados del cronograma L-48 y Sanos
            var query = _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.TechnicalRequest)
                .Include(p => p.Verification)
                .Where(p => p.ManagementId == id && p.EquipmentUnit != null);

            if (ActiveTab == "sanos")
            {
                query = query.Where(p => p.CurrentState == WizardEquipmentState.VerifiedGood);
            }
            else if (ActiveTab == "l48")
            {
                query = query.Where(p => p.CurrentState >= WizardEquipmentState.AwaitingRequest);
            }

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
                switch (StatusFilter)
                {
                    case "Planeado":
                        query = query.Where(p => p.PlannedWeek != null);
                        break;
                    case "InProgress":
                        query = query.Where(p => p.Maintenance != null && p.Maintenance.Status == MaintenanceStatus.InProgress);
                        break;
                    case "Completed":
                        query = query.Where(p => p.ExecutedWeek != null);
                        break;
                    case "Externo":
                        query = query.Where(p => p.Maintenance != null && p.Maintenance.ServiceType == ServiceType.External);
                        break;
                    case "Delay":
                        var utcNow = DateTime.UtcNow;
                        query = query.Where(p =>
                            p.PlannedDate.HasValue &&
                            p.PlannedDate.Value < utcNow &&
                            p.CurrentState != WizardEquipmentState.Completed &&
                            p.CurrentState != WizardEquipmentState.VerifiedGood &&
                            p.PlanStatus != ManagementPlanStatus.Completed);
                        break;
                }
            }
            ManagementPlans = await query
                .OrderBy(p => p.EquipmentUnit!.Laboratory!.Name)
                .ThenBy(p => p.EquipmentUnit!.Equipment!.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateActiveManagementAsync(int id)
        {
            var currentManagement = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (currentManagement == null)
            {
                TempData.Warning("No se encontro la gestion solicitada para actualizar el dashboard.");
                return RedirectToPage("./Index");
            }

            var preferredId = currentManagement.Status == ManagementStatus.Active
                ? currentManagement.Id
                : (int?)null;

            var activationResult = await _managementActivation.ReconcileActiveAsync(currentManagement.Type, preferredId);

            if (activationResult.ActiveManagement == null)
            {
                TempData.Warning("No hay una gestion activa para este tipo. Cree o active una gestion antes de continuar.");
                return RedirectToPage("./Index", new { Type = currentManagement.Type.ToString() });
            }

            await _context.SaveChangesAsync();
            _managementContext.InvalidateCache();

            var closedMessage = activationResult.ClosedCount > 0
                ? $" Se cerro {activationResult.ClosedCount} gestion activa duplicada."
                : string.Empty;

            TempData.Success($"Gestion actualizada correctamente. Ahora estas en {activationResult.ActiveManagement.Code}.{closedMessage}");
            return RedirectToPage("./Details", new { id = activationResult.ActiveManagement.Id, ActiveTab = "dashboard" });
        }

        public async Task<IActionResult> OnPostSyncEquipmentsAsync(int id)
        {
            var management = await _context.Managements
                .Include(m => m.ManagementPlans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (management == null || management.Status != ManagementStatus.Active)
            {
                TempData.Error("No se puede sincronizar una gestión que no está activa.");
                return RedirectToPage(new { id, ActiveTab });
            }

            if (management.Type == ManagementType.Corrective)
            {
                TempData.Warning("Las gestiones correctivas no sincronizan equipos. Cada activo entra al flujo cuando se reporta una falla L-7.");
                return RedirectToPage(new { id, ActiveTab });
            }

            // Obtener todos los IDs de equipos ya en esta gestión
            if (management.Type == ManagementType.Preventive)
            {
                TempData.Info("Seleccione las categorias, subclasificaciones y unidades que entraran a esta ronda preventiva.");
                return RedirectToPage("./PlanAssets", new { id });
            }

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
                return RedirectToPage(new { id, ActiveTab });
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

            return RedirectToPage(new { id, ActiveTab });
        }

        public async Task<IActionResult> OnPostToggleWeekAsync(int planId, int weekNumber, string type)
        {
            if (weekNumber < 1 || weekNumber > 8)
                return BadRequest();

            var plan = await _context.ManagementPlans
                .AsTracking()
                .Include(p => p.Management)
                .Include(p => p.Maintenance)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null || plan.Management == null)
                return NotFound();

            if (type == "planned")
            {
                if (plan.PlannedWeek == weekNumber)
                    plan.PlannedWeek = null;
                else
                {
                    plan.PlannedWeek = weekNumber;
                    plan.PlannedDate = CalculateDateFromWeek(plan.Management.Year, plan.Management.Semester, weekNumber);
                }
            }
            else if (type == "executed")
            {
                if (plan.ExecutedWeek == weekNumber)
                {
                    plan.ExecutedWeek = null;
                }
                else
                {
                    if (plan.PlannedWeek == null)
                        return new JsonResult(new { success = false, error = "Debe planificar primero (Prev.) antes de marcar como ejecutado." });
                    if (weekNumber < plan.PlannedWeek.Value)
                        return new JsonResult(new { success = false, error = "La semana ejecutada no puede ser menor que la semana planeada." });

                    plan.ExecutedWeek = weekNumber;
                    if (plan.Maintenance != null)
                        plan.Maintenance.ScheduledDate = CalculateDateFromWeek(plan.Management.Year, plan.Management.Semester, weekNumber);
                }
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        private DateTime CalculateDateFromWeek(int year, int semester, int weekNumber)
        {
            // Semester 1: June, July. Semester 2: December, January.
            int month = semester == 1 ? (weekNumber <= 4 ? 6 : 7) : (weekNumber <= 4 ? 12 : 1);
            int yearToUse = (semester == 2 && month == 1) ? year + 1 : year;
            
            // week 1-4 for the month.
            int weekInMonth = weekNumber <= 4 ? weekNumber : weekNumber - 4;

            // Find the Nth Monday of the month
            DateTime firstDayOfMonth = new DateTime(yearToUse, month, 1);
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
            DateTime firstMonday = firstDayOfMonth.AddDays(daysUntilMonday);
            
            return firstMonday.AddDays((weekInMonth - 1) * 7);
        }

        private static bool IsCompletedPlan(ManagementPlan plan)
        {
            return plan.CurrentState == WizardEquipmentState.Completed ||
                   plan.PlanStatus == ManagementPlanStatus.Completed;
        }

        private static bool IsActivePipelinePlan(ManagementPlan plan)
        {
            return plan.CurrentState != WizardEquipmentState.VerifiedGood &&
                   !IsCompletedPlan(plan);
        }

    }

    // B-4: DTO para el calendario de FullCalendar
    public class ManagementPlanCalendarDto
    {
        public string Title           { get; set; } = string.Empty;
        public string Start           { get; set; } = string.Empty; // formato: "yyyy-MM-dd"
        public string? End            { get; set; }
        public string ClassName       { get; set; } = "ev-planned";
        // Campos extra para el panel de día
        public string InventoryNumber { get; set; } = "—";
        public string LabName         { get; set; } = "—";
        public string TechnicianName  { get; set; } = "Sin asignar";
        public int?   MaintenanceId   { get; set; }
        public int    PlanId          { get; set; }
        public int    CurrentPhaseInt { get; set; }
        public string StatusLabel     { get; set; } = "Pendiente";
    }
}
