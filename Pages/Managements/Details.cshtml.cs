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
    [ValidateAntiForgeryToken]
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
        public string ActiveTab { get; set; } = "l48";

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (Type == "Corrective" && id == null)
                id = await EnsureCorrectiveContainerExists();

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
            CountL6         = Management.Type == ManagementType.Corrective ? 0 : allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentPhase == WizardPhase.Verification && (p.VerificationId == null || (p.IsDraft && p.DraftPhase == WizardPhase.Verification)));
            CountL7         = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentPhase == WizardPhase.TechnicalRequest);
            CountL8         = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentPhase == WizardPhase.Maintenance);
            CountSalida     = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentPhase == WizardPhase.Exit);
            CountDesembolso = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentPhase == WizardPhase.Disbursement && p.CurrentState != WizardEquipmentState.Completed);

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
                .Where(p => p.ManagementId == id);

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
                }
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

        private async Task<int> EnsureCorrectiveContainerExists()
        {
            var currentYear = DateTime.Now.Year;
            var corrective = await _context.Managements
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Type == ManagementType.Corrective && m.Year == currentYear);

            if (corrective != null)
                return corrective.Id;

            corrective = new Management
            {
                Year = currentYear,
                Semester = 0,
                Code = $"CORR-{currentYear}",
                Description = "Contenedor automático de fallas correctivas.",
                Status = ManagementStatus.Active,
                Type = ManagementType.Corrective,
                CreatedDate = DateTime.UtcNow
            };

            _context.Managements.Add(corrective);
            await _context.SaveChangesAsync();

            return corrective.Id;
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
