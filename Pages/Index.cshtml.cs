using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    public class EquipmentSimulated
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InventoryNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int LabId { get; set; }
        public string StatusStep { get; set; } = string.Empty;
    }

    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Management? ActiveManagement { get; set; }
        
        // --- PROPIEDADES ANALITICAS (BACKEND-DATA SPRINT 3) ---
        public int TotalActivos { get; set; }
        public int EquiposTerminados { get; set; }
        public int TotalVencidos { get; set; }
        public double GlobalProgress { get; set; }
        
        public Dictionary<string, int> TopEquipmentTypes { get; set; } = new();
        public Dictionary<string, int> TopGroups { get; set; } = new();
        public Dictionary<string, int> TopLaboratories { get; set; } = new();
        
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();
        public IList<ManagementPlan> OverduePlans { get; set; } = new List<ManagementPlan>();

        // Propiedades de Filtrado para el Cronograma (L-48 / Dashboard)
        [BindProperty(SupportsGet = true)]
        public int? LabFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TechFilterId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public SelectList LabFList { get; set; } = default!;
        public SelectList CategoryFList { get; set; } = default!;
        public SelectList TechFList { get; set; } = default!;

        // Propiedades del Wizard Embebido
        [BindProperty(SupportsGet = true)]
        public bool ShowWizard { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        // Contadores de fases para el Dashboard Premium
        public int CountL6 { get; set; }
        public int CountL7 { get; set; }
        public int CountL8 { get; set; }
        public int CountSalida { get; set; }
        public int CountDesembolso { get; set; }

        // --- MÉTRICAS FILA 1: Métricas Globales Sprint 3B ---
        public int CountPendientes { get; set; }   // Equipos desde L-7 en adelante, sin completar
        public int CountBuenos { get; set; }       // Equipos verificados en L-6 sin fallas (VerifiedGood)

        public SelectList LaboratoriesList { get; set; } = default!;
        public List<EquipmentUnit> EquipmentUnitsList { get; set; } = new();
        public List<ManagementPlan> ActivePlans { get; set; } = new();
        public List<ManagementPlan> Step1Plans { get; set; } = new();
        public List<ManagementPlan> Step2Plans { get; set; } = new();
        public List<ManagementPlan> Step3Plans { get; set; } = new();
        public List<ManagementPlan> Step4Plans { get; set; } = new();
        public List<ManagementPlan> Step5Plans { get; set; } = new();
        public List<ManagementPlan> Step6Plans { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Cargar Listas para Filtros
                var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
                LabFList = new SelectList(labs, "Id", "Name");

                var techsList = await _context.People
                    .Where(p => p.Category == PersonCategory.Tecnico)
                    .ToListAsync();
                
                var techs = techsList.OrderBy(t => t.FullName).ToList();
                TechFList = new SelectList(techs, "Id", "FullName");

                CategoryFList = new SelectList(Enum.GetValues(typeof(EquipmentCategory))
                    .Cast<EquipmentCategory>()
                    .Select(e => new { Id = (int)e, Name = e.ToString() }), "Id", "Name");

                // Buscar Gestión Activa
                ActiveManagement = await _context.Managements
                    .Where(m => m.Status == ManagementStatus.Active)
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Semester)
                    .FirstOrDefaultAsync();

                if (ActiveManagement != null)
                {
                    // Query Base para el Dashboard y Cronograma con todos los includes necesarios para el Wizard
                    var plansQuery = _context.ManagementPlans
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                        .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                        .Include(p => p.Verification).ThenInclude(v => v!.CheckResults)
                        .Include(p => p.TechnicalRequest)
                        .Include(p => p.Departure)
                        .Where(p => p.ManagementId == ActiveManagement.Id);

                    // Estadísticas Globales
                    var allPlans = await plansQuery.ToListAsync();
                    TotalActivos = allPlans.Count;
                    EquiposTerminados = allPlans.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);
                    TotalVencidos = allPlans.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now);
                    GlobalProgress = TotalActivos > 0 ? Math.Round((double)EquiposTerminados / TotalActivos * 100, 1) : 0;

                    // B-2: Poblar métricas Fila 1 Sprint 3B
                    CountPendientes = allPlans.Count(p =>
                        p.PlanStatus != ManagementPlanStatus.Completed &&
                        p.CurrentState >= WizardEquipmentState.AwaitingRequest);
                    CountBuenos = allPlans.Count(p =>
                        p.CurrentState == WizardEquipmentState.VerifiedGood);

                    // B-3: Agrupación semántica de tipos de equipo
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

                    TopGroups = allPlans
                        .Where(p => p.EquipmentUnit?.Equipment != null)
                        .GroupBy(p => p.EquipmentUnit!.Equipment!.TypeClassification.ToString())
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    TopLaboratories = allPlans
                        .Where(p => p.EquipmentUnit?.Laboratory != null)
                        .GroupBy(p => p.EquipmentUnit!.Laboratory!.Name)
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    OverduePlans = allPlans
                        .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now.AddDays(7))
                        .OrderBy(p => p.PlannedDate)
                        .ToList();

                    // APLICAR FILTROS AL CRONOGRAMA
                    var cronogramaQuery = plansQuery.AsQueryable();

                    if (LabFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit!.LaboratoryId == LabFilterId);
                    if (CategoryFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit!.Equipment!.Category == (EquipmentCategory)CategoryFilterId);
                    if (TechFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance!.TechnicianId == TechFilterId);
                    
                    if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "Todos")
                    {
                        if (StatusFilter == "Externo") 
                            cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance!.ServiceType == ServiceType.External);
                        else if (Enum.TryParse<ManagementPlanStatus>(StatusFilter, out var statusEnum))
                            cronogramaQuery = cronogramaQuery.Where(p => p.PlanStatus == statusEnum);
                    }

                    ManagementPlans = await cronogramaQuery
                        .Where(p => p.PlannedDate.HasValue)
                        .OrderBy(p => p.PlannedDate)
                        .Take(10)
                        .ToListAsync();

                    // Contadores por fase para el Dashboard Premium
                    CountL6 = allPlans.Count(p => p.CurrentPhase == WizardPhase.Verification);
                    CountL7 = allPlans.Count(p => p.CurrentPhase == WizardPhase.TechnicalRequest);
                    CountL8 = allPlans.Count(p => p.CurrentPhase == WizardPhase.Maintenance);
                    CountSalida = allPlans.Count(p => p.CurrentPhase == WizardPhase.Exit);
                    CountDesembolso = allPlans.Count(p => p.CurrentPhase == WizardPhase.Disbursement);

                    // LÓGICA DEL WIZARD FUNCIONAL
                    if (ShowWizard)
                    {
                        LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);
                        var wizardQuery = plansQuery.AsQueryable();
                        if (SelectedLabId.HasValue)
                        {
                            wizardQuery = wizardQuery.Where(p => p.EquipmentUnit.LaboratoryId == SelectedLabId.Value);
                        }
                        ActivePlans = await wizardQuery.ToListAsync();

                        // B-1: Poblado de listas por paso (evitando expresión => para no recalcular)
                        Step1Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Verification).ToList();
                        Step2Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.TechnicalRequest).ToList();
                        Step3Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Maintenance).ToList();
                        Step4Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Exit).ToList();
                        Step5Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Kardex).ToList();
                        Step6Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Disbursement).ToList();
                    }
                }
            }
            catch (Exception)
            {
                // Log error
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostNextStep()
        {
            return RedirectToPage(new { ShowWizard = true, Step = Step + 1, SelectedLabId = SelectedLabId });
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostPreviousStep()
        {
            int prevStep = Step > 1 ? Step - 1 : 0; 
            return RedirectToPage(new { ShowWizard = true, Step = prevStep, SelectedLabId = SelectedLabId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRefresh()
        {
            TempData.Success("Los datos del dashboard se han sincronizado correctamente.");
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostFastFail(int? planId)
        {
            if (planId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(planId.Value);
                if (plan != null && plan.CurrentPhase == WizardPhase.Verification)
                {
                    plan.CurrentPhase = WizardPhase.TechnicalRequest;
                    plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                    await _context.SaveChangesAsync();
                    return RedirectToPage("/Requests/Create", new { equipmentUnitId = plan.EquipmentUnitId, isWizard = true, managementPlanId = plan.Id });
                }
            }

            var activeManagement = await _context.Managements
                .Where(m => m.Status == ManagementStatus.Active)
                .FirstOrDefaultAsync();

            if (activeManagement == null)
            {
                TempData.Error("Para usar la Falla Rápida debe existir un periodo de Gestión Institucional activo.");
                return RedirectToPage();
            }

            return RedirectToPage("/Requests/Create");
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostConfirmKardexAsync(int planId)
        {
            try
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.EquipmentUnit)
                    .Include(p => p.Maintenance)
                    .FirstOrDefaultAsync(p => p.Id == planId);

                if (plan != null)
                {
                    var kardexEntry = new EquipmentStateHistory
                    {
                        EquipmentUnitId = plan.EquipmentUnitId ?? 0,
                        Status = EquipmentStatus.Operational,
                        StartDate = DateTime.UtcNow,
                        Reason = $"Kardex: Mantenimiento #{plan.MaintenanceId} finalizado en Gestión #{plan.ManagementId}."
                    };
                    _context.EquipmentStateHistories.Add(kardexEntry);

                    if (plan.EquipmentUnit != null)
                    {
                        plan.EquipmentUnit.CurrentStatus = EquipmentStatus.Operational;
                        _context.EquipmentUnits.Update(plan.EquipmentUnit);
                    }

                    plan.CurrentPhase = WizardPhase.Disbursement;
                    plan.CurrentState = WizardEquipmentState.Completed;
                    plan.PlanStatus = ManagementPlanStatus.Completed;

                    await _context.SaveChangesAsync();
                    TempData.Success("Kardex actualizado. El equipo está marcado como Operativo.");
                }
            }
            catch (Exception ex) 
            {
                TempData.Error("Error al actualizar Kardex: " + ex.Message);
            }

            return RedirectToPage(new { ShowWizard = true, Step = 5, SelectedLabId = SelectedLabId });
        }

        public int CountVerificationFails(Verification? v)
        {
            return v?.FailuresCount ?? 0;
        }

        // B-2: Handler para detalle del Kardex (Sidebar) con ordenamiento en memoria
        public async Task<JsonResult> OnGetKardexDetailAsync(int equipmentId)
        {
            var unit = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Include(u => u.StateHistory) // Nombre real en el modelo
                .FirstOrDefaultAsync(u => u.Id == equipmentId);

            if (unit == null) return new JsonResult(new { error = "No encontrado" });

            var lastHistory = unit.StateHistory?
                .OrderByDescending(h => h.StartDate)
                .FirstOrDefault();

            return new JsonResult(new {
                name = unit.Equipment?.Name ?? "Sin nombre",
                inventoryNumber = unit.InventoryNumber,
                currentStatus = unit.CurrentStatus.ToString(),
                lastDate = lastHistory?.StartDate.ToString("dd 'de' MMMM, yyyy", new System.Globalization.CultureInfo("es-ES")) ?? "Sin registros",
                reason = lastHistory?.Reason ?? "—"
            });
        }
    }

    public class WizardStepViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EmptyMessage { get; set; } = string.Empty;
        public string EmptyIcon { get; set; } = "fas fa-check-circle";
        public string BorderClass { get; set; } = "border-primary";
        public List<ManagementPlan> Plans { get; set; } = new();
        public string ActionPage { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = string.Empty;
        public string ActionClass { get; set; } = "btn-outline-primary";
        public string StatusLabel { get; set; } = string.Empty;
        public bool ShowFailCount { get; set; } = false;
        public int StepIndex { get; set; }
        public Microsoft.AspNetCore.Mvc.Rendering.SelectList? LaboratoriesList { get; set; }
    }
}