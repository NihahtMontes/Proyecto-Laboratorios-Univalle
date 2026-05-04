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
        public int TotalActivos { get; set; }
        public int TotalVencidos { get; set; }
        public int CountPendientes { get; set; }
        public int CountBuenos { get; set; }
        public int CountL6 { get; set; }
        public int CountL7 { get; set; }
        public int CountL8 { get; set; }
        public int CountSalida { get; set; }
        public int CountDesembolso { get; set; }

        public List<ManagementPlanCalendarDto> CalendarEvents { get; set; } = new();
        public string ShadingStart { get; set; } = "";
        public string ShadingEnd { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public int? LabFilterId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? CategoryFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? TechFilterId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }
        public SelectList LabFList { get; set; } = default!;
        public SelectList TechFList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchResponsible { get; set; }
        [BindProperty(SupportsGet = true)]
        public ManagementPlanStatus? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "l48";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Managements == null) return NotFound();

            var m = await _context.Managements
                .Include(mg => mg.ManagementPlans).ThenInclude(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(mg => mg.ManagementPlans).ThenInclude(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(mg => mg.ManagementPlans).ThenInclude(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(mg => mg.ManagementPlans).ThenInclude(p => p.Verification)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (m == null) return NotFound();
            Management = m;

            var allPlans = Management.ManagementPlans.ToList();
            var now = DateTime.Now;

            // Métricas Globales
            TotalActivos = allPlans.Count;
            TotalPlans = TotalActivos;
            CompletedPlans = allPlans.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);
            GlobalProgress = TotalActivos > 0 ? Math.Round((double)CompletedPlans / TotalActivos * 100, 1) : 0;
            TotalVencidos = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < now);
            CountPendientes = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentState >= WizardEquipmentState.AwaitingRequest);
            CountBuenos = allPlans.Count(p => p.CurrentState == WizardEquipmentState.VerifiedGood);

            CountL6 = allPlans.Count(p => p.CurrentPhase == WizardPhase.Verification && p.CurrentState == WizardEquipmentState.PendingVerification);
            CountL7 = allPlans.Count(p => p.CurrentPhase == WizardPhase.TechnicalRequest);
            CountL8 = allPlans.Count(p => p.CurrentPhase == WizardPhase.Maintenance);
            CountSalida = allPlans.Count(p => p.CurrentPhase == WizardPhase.Exit);
            CountDesembolso = allPlans.Count(p => p.CurrentPhase == WizardPhase.Disbursement);

            TopEquipmentTypes = allPlans.Where(p => p.EquipmentUnit?.Equipment != null)
                .GroupBy(p => p.EquipmentUnit!.Equipment!.Category == EquipmentCategory.Utensil ? "Utensilio" : p.EquipmentUnit!.Equipment!.TypeClassification switch
                {
                    EquipmentTypeClassification.Electronico => "Electrónico / Eléctrico",
                    EquipmentTypeClassification.Manual => "Manual / Mecánico",
                    EquipmentTypeClassification.Mobiliario => "Mobiliario",
                    EquipmentTypeClassification.Medicion => "Instrumental de Medición",
                    EquipmentTypeClassification.Vidrio => "Material de Vidrio",
                    EquipmentTypeClassification.Informatico => "Informático",
                    _ => "Otro"
                }).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());

            TopLaboratories = allPlans.Where(p => p.EquipmentUnit?.Laboratory != null)
                .GroupBy(p => p.EquipmentUnit!.Laboratory!.Name).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());

            OverduePlans = allPlans.Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < now.AddDays(7))
                .OrderBy(p => p.PlannedDate).ToList();

            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LabFList = new SelectList(labs, "Id", "Name", LabFilterId);
            var techsList = await _context.People.Where(p => p.Category == PersonCategory.Tecnico).ToListAsync();
            TechFList = new SelectList(techsList.OrderBy(t => t.FullName), "Id", "FullName", TechFilterId);

            CalendarEvents = allPlans.Where(p => p.CurrentPhase >= WizardPhase.Maintenance).Select(p => {
                var displayDate = p.Maintenance?.ScheduledDate ?? p.Maintenance?.EndDate ?? p.PlannedDate ?? DateTime.Now;
                return new ManagementPlanCalendarDto
                {
                    Title = $"{p.EquipmentUnit?.InventoryNumber} - {p.EquipmentUnit?.Equipment?.Name}",
                    Start = displayDate.ToString("yyyy-MM-dd"),
                    ClassName = p.Maintenance?.Status switch
                    {
                        MaintenanceStatus.Completed => "ev-completed",
                        MaintenanceStatus.InProgress => "ev-progress",
                        _ => "ev-planned"
                    },
                    InventoryNumber = p.EquipmentUnit?.InventoryNumber ?? "—",
                    LabName = p.EquipmentUnit?.Laboratory?.Name ?? "—",
                    PlanId = p.Id
                };
            }).ToList();

            if (Management.Semester == 1)
            {
                ShadingStart = new DateTime(Management.Year, 6, 1).ToString("yyyy-MM-dd");
                ShadingEnd = new DateTime(Management.Year, 8, 1).ToString("yyyy-MM-dd");
            }
            else
            {
                ShadingStart = new DateTime(Management.Year, 12, 1).ToString("yyyy-MM-dd");
                ShadingEnd = new DateTime(Management.Year + 1, 2, 1).ToString("yyyy-MM-dd");
            }

            var query = _context.ManagementPlans.AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.Verification)
                .Where(p => p.ManagementId == id);

            if (LabFilterId.HasValue) query = query.Where(p => p.EquipmentUnit!.LaboratoryId == LabFilterId);
            if (TechFilterId.HasValue) query = query.Where(p => p.Maintenance!.TechnicianId == TechFilterId);

            ManagementPlans = await query.OrderBy(p => p.EquipmentUnit!.Laboratory!.Name).ToListAsync();

            return Page();
        }

        // ============================================================
        // FUNCIÓN DE SINCRONIZACIÓN CORREGIDA Y FUNCIONAL
        // ============================================================
        public async Task<IActionResult> OnPostSyncEquipmentsAsync(int id)
        {
            var management = await _context.Managements
                .Include(m => m.ManagementPlans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (management == null || management.Status != ManagementStatus.Active)
            {
                TempData["Error"] = "No se puede sincronizar una gestión que no está activa.";
                return RedirectToPage(new { id });
            }

            // 1. Obtener IDs de equipos que ya están en esta gestión para no duplicarlos
            var existingEquipmentIds = management.ManagementPlans
                .Where(p => p.EquipmentUnitId.HasValue)
                .Select(p => p.EquipmentUnitId.Value)
                .ToList();

            // 2. Obtener equipos físicos que están Operativos o en Mantenimiento, pero NO eliminados
            // y que NO existan en la lista de planes de esta gestión
            var newEquipments = await _context.EquipmentUnits
                .Where(eu => eu.CurrentStatus != EquipmentStatus.Deleted && !existingEquipmentIds.Contains(eu.Id))
                .ToListAsync();

            if (!newEquipments.Any())
            {
                TempData["Warning"] = "Todos los equipos activos ya se encuentran en el cronograma de esta gestión.";
                return RedirectToPage(new { id, ActiveTab = "l48" });
            }

            // 3. Crear los planes de gestión para los equipos faltantes
            foreach (var eu in newEquipments)
            {
                var plan = new ManagementPlan
                {
                    ManagementId = id,
                    EquipmentUnitId = eu.Id,
                    CurrentPhase = WizardPhase.Verification, // Inicia en L-6
                    CurrentState = WizardEquipmentState.PendingVerification,
                    PlanStatus = ManagementPlanStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    PlannedDate = DateTime.Today.AddDays(7), // Fecha estimada inicial
                    Notes = "Sincronizado automáticamente desde inventario físico."
                };
                _context.ManagementPlans.Add(plan);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"¡Sincronización exitosa! Se han añadido {newEquipments.Count} equipos nuevos al cronograma L-48.";

            return RedirectToPage(new { id, ActiveTab = "l48" });
        }
    }

    public class ManagementPlanCalendarDto
    {
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string? End { get; set; }
        public string ClassName { get; set; } = "ev-planned";
        public string InventoryNumber { get; set; } = "—";
        public string LabName { get; set; } = "—";
        public string TechnicianName { get; set; } = "Sin asignar";
        public int? MaintenanceId { get; set; }
        public int PlanId { get; set; }
        public int CurrentPhaseInt { get; set; }
        public string StatusLabel { get; set; } = "Pendiente";
    }
}