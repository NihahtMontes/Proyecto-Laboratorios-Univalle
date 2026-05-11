using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
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
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly IManagementContextService _managementContext;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger, IManagementContextService managementContext)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _managementContext = managementContext;
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
        
        public class OverduePlanDto 
        {
            public int Id { get; set; }
            public DateTime? PlannedDate { get; set; }
            public ManagementPlanStatus PlanStatus { get; set; }
            public string EquipmentName { get; set; } = string.Empty;
            public string LaboratoryName { get; set; } = string.Empty;
        }

        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();
        public IList<OverduePlanDto> OverduePlans { get; set; } = new List<OverduePlanDto>();

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

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

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
        public List<ManagementPlan> CompletedPlans { get; set; } = new();

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

                // Buscar Gestión Activa (cached), o usar ManagementId directo
                if (ManagementId.HasValue)
                {
                    ActiveManagement = await _context.Managements
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == ManagementId.Value);
                }
                else
                {
                    ActiveManagement = await _managementContext.GetCurrentManagementAsync();
                }

                if (ActiveManagement != null)
                {
                    var isCorrective = ActiveManagement.Type == ManagementType.Corrective;
                    ViewData["IsCorrective"] = isCorrective;
                    ViewData["ManagementId"] = ActiveManagement.Id;

                    if (ShowWizard && isCorrective && Request.Query["Step"].Count == 0)
                    {
                        Step = 2; // Iniciar en L-7 para correctivos por defecto
                    }
                    // 1. STATS QUERY: Base ultra ligera sin Includes para conteos masivos
                    var statsQuery = _context.ManagementPlans
                        .AsNoTracking()
                        .Where(p => p.ManagementId == ActiveManagement.Id);

                    // Single database roundtrip: materialize all plans once
                    var allStats = await statsQuery.Select(p => new
                    {
                        p.PlanStatus,
                        p.CurrentPhase,
                        p.CurrentState,
                        p.PlannedDate,
                        p.VerificationId,
                        p.RequestId,
                        p.MaintenanceId,
                        p.DepartureId,
                        p.AcquisitionRequestId,
                        p.IsDraft,
                        p.DraftPhase
                    }).ToListAsync();

                    TotalActivos = allStats.Count;
                    EquiposTerminados = allStats.Count(p => p.CurrentState == WizardEquipmentState.Completed);
                    
                    var now = DateTime.Now;
                    TotalVencidos = allStats.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate < now);
                    GlobalProgress = TotalActivos > 0 ? Math.Round((double)EquiposTerminados / TotalActivos * 100, 1) : 0;

                    CountPendientes = allStats.Count(p =>
                        p.PlanStatus != ManagementPlanStatus.Completed &&
                        p.CurrentState >= WizardEquipmentState.AwaitingRequest);
                    
                    CountBuenos = allStats.Count(p =>
                        p.CurrentState == WizardEquipmentState.VerifiedGood);

                    CountL6 = isCorrective ? 0 : allStats.Count(p => p.VerificationId.HasValue);
                    CountL7 = allStats.Count(p => p.RequestId.HasValue);
                    CountL8 = allStats.Count(p => p.MaintenanceId.HasValue);
                    CountSalida = allStats.Count(p => p.DepartureId.HasValue);
                    CountDesembolso = allStats.Count(p => p.AcquisitionRequestId.HasValue);

                    // 2. DATA QUERY: Base pesada con Includes solo para el Cronograma y el Wizard
                    var dataQuery = _context.ManagementPlans
                        .AsNoTracking()
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                        .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                        .Include(p => p.Verification).ThenInclude(v => v!.CheckResults)
                        .Include(p => p.TechnicalRequest)
                        .Include(p => p.AcquisitionRequest)
                        .Include(p => p.Departure)
                        .Where(p => p.ManagementId == ActiveManagement.Id);

                    // B-3: Agrupación semántica de tipos de equipo
                    var equipmentStats = await _context.ManagementPlans
                        .AsNoTracking()
                        .Where(p => p.ManagementId == ActiveManagement.Id && p.EquipmentUnit != null && p.EquipmentUnit.Equipment != null)
                        .Select(p => new {
                            Category = p.EquipmentUnit!.Equipment!.Category,
                            TypeClass = p.EquipmentUnit!.Equipment!.TypeClassification,
                            LabName = p.EquipmentUnit!.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A"
                        })
                        .ToListAsync();

                    TopEquipmentTypes = equipmentStats
                        .GroupBy(p =>
                            p.Category == EquipmentCategory.Utensil
                                ? "Utensilio"
                                : p.TypeClass switch
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

                    TopGroups = equipmentStats
                        .GroupBy(p => p.TypeClass.ToString())
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    TopLaboratories = equipmentStats
                        .Where(p => p.LabName != "N/A")
                        .GroupBy(p => p.LabName)
                        .OrderByDescending(g => g.Count())
                        .ToDictionary(g => g.Key, g => g.Count());

                    var futureLimit = now.AddDays(7);
                    // Proyección ligera (.Select) explícita para OverduePlans
                    OverduePlans = await dataQuery
                        .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate < futureLimit)
                        .OrderBy(p => p.PlannedDate)
                        .Select(p => new OverduePlanDto {
                            Id = p.Id,
                            PlannedDate = p.PlannedDate,
                            PlanStatus = p.PlanStatus,
                            EquipmentName = p.EquipmentUnit!.Equipment!.Name,
                            LaboratoryName = p.EquipmentUnit.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A"
                        })
                        .ToListAsync();

                    // APLICAR FILTROS AL CRONOGRAMA
                    var cronogramaQuery = dataQuery.AsQueryable();

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

                    // LÓGICA DEL WIZARD FUNCIONAL
                    if (ShowWizard)
                    {
                        LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);
                        var wizardQuery = dataQuery.AsQueryable();
                        if (SelectedLabId.HasValue)
                        {
                            wizardQuery = wizardQuery.Where(p => p.EquipmentUnit!.LaboratoryId == SelectedLabId.Value);
                        }
                        
                        // Límite de seguridad Take(100) para evitar colapso de RAM si no hay filtros
                        ActivePlans = await wizardQuery.Take(100).ToListAsync();

                        // B-1: Poblado de listas por paso (evitando expresión => para no recalcular)
                        Step1Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Verification && (p.VerificationId == null || (p.IsDraft && p.DraftPhase == WizardPhase.Verification))).ToList();
                        Step2Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.TechnicalRequest).ToList();
                        Step3Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Maintenance).ToList();
                        Step4Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Exit).ToList();
                        Step5Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Kardex).ToList();
                        Step6Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Disbursement).ToList();
                        CompletedPlans = ActivePlans
                            .Where(p => p.CurrentState == WizardEquipmentState.Completed || p.PlanStatus == ManagementPlanStatus.Completed)
                            .OrderByDescending(p => p.LastModifiedDate ?? p.CreatedDate)
                            .Take(8)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard OnGetAsync falló");
                TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar el panel de control.";
            }

            return Page();
        }

        public IActionResult OnPostNextStep()
        {
            var nextStep = Step >= 7 ? 7 : Step + 1;
            return RedirectToPage(new { ShowWizard = true, Step = nextStep, SelectedLabId = SelectedLabId, ManagementId = ManagementId ?? ActiveManagement?.Id });
        }

        public IActionResult OnPostPreviousStep()
        {
            var isCorrective = ActiveManagement?.Type == ManagementType.Corrective;
            var minStep = isCorrective ? 2 : 1;
            int prevStep = Step > minStep ? Step - 1 : 0; 
            return RedirectToPage(new { ShowWizard = true, Step = prevStep, SelectedLabId = SelectedLabId, ManagementId = ManagementId ?? ActiveManagement?.Id });
        }

        public async Task<IActionResult> OnPostRefresh()
        {
            var activeManagement = await _context.Managements
                .Include(m => m.ManagementPlans)
                .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);

            if (activeManagement == null)
            {
                TempData.Error("No hay gestión activa para sincronizar.");
                return RedirectToPage();
            }

            var existingEquipmentIds = activeManagement.ManagementPlans
                .Where(p => p.EquipmentUnitId.HasValue)
                .Select(p => p.EquipmentUnitId!.Value)
                .ToList();

            var newEquipments = await _context.EquipmentUnits
                .Where(eu => eu.CurrentStatus != EquipmentStatus.Deleted && !existingEquipmentIds.Contains(eu.Id))
                .ToListAsync();

            if (newEquipments.Any())
            {
                foreach (var eu in newEquipments)
                {
                    _context.ManagementPlans.Add(new ManagementPlan
                    {
                        ManagementId = activeManagement.Id,
                        EquipmentUnitId = eu.Id,
                        CurrentPhase = WizardPhase.Verification,
                        CurrentState = WizardEquipmentState.PendingVerification,
                        PlanStatus = ManagementPlanStatus.Pending,
                        CreatedDate = DateTime.UtcNow,
                        PlannedDate = DateTime.Today.AddDays(7)
                    });
                }
                await _context.SaveChangesAsync();
                TempData.Success($"Se han sincronizado {newEquipments.Count} nuevos equipos a la gestión {activeManagement.Year}-{activeManagement.Semester}.");
            }
            else
            {
                TempData.Success("El dashboard está actualizado. Todos los equipos activos ya están en la ronda.");
            }

            return RedirectToPage();
        }

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
                    return RedirectToPage("/Requests/Create", new { equipmentUnitId = plan.EquipmentUnitId, isWizard = true, managementPlanId = plan.Id, ManagementId = plan.ManagementId });
                }
                else if (plan != null)
                {
                    TempData.Error($"Este equipo no está en fase de Verificación (fase actual: {plan.CurrentPhase}). No se puede registrar falla rápida.");
                    return RedirectToPage(new { ShowWizard = true, Step = (int)plan.CurrentPhase, SelectedLabId = SelectedLabId, ManagementId = plan.ManagementId });
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

            TempData.Error("No se encontró el plan de gestión para este equipo.");
            return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId = ManagementId });
        }

        public async Task<IActionResult> OnPostConfirmKardexAsync(int planId)
        {
            try
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.EquipmentUnit)
                    .Include(p => p.Maintenance)
                    .AsTracking()
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
                    plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
                    plan.PlanStatus = ManagementPlanStatus.InProgress;

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
