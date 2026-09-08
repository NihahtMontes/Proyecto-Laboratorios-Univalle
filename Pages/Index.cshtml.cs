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
        private readonly IDashboardReadService _dashboardReadService;
        private readonly IManagementActivationService _managementActivation;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<IndexModel> logger, IManagementContextService managementContext, IDashboardReadService dashboardReadService, IManagementActivationService managementActivation)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _managementContext = managementContext;
            _dashboardReadService = dashboardReadService;
            _managementActivation = managementActivation;
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
        public IList<DashboardOverduePlanDto> OverduePlans { get; set; } = new List<DashboardOverduePlanDto>();

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

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SerialNumberFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? InventoryNumberFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? FocusPlanId { get; set; }

        public ManagementPlan? FocusedPlan { get; set; }

        public const int PageSize = 5;

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
        public List<Step1LabSummary> Step1Labs { get; set; } = new();
        public List<ManagementPlan> Step2Plans { get; set; } = new();
        public List<ManagementPlan> Step3Plans { get; set; } = new();
        public List<ManagementPlan> Step4Plans { get; set; } = new();
        public List<ManagementPlan> Step5Plans { get; set; } = new();
        public List<ManagementPlan> Step6Plans { get; set; } = new();
        public List<ManagementPlan> CompletedPlans { get; set; } = new();

        public int ActiveTotalItems { get; set; }
        public int Step1TotalItems { get; set; }
        public int Step2TotalItems { get; set; }
        public int Step3TotalItems { get; set; }
        public int Step4TotalItems { get; set; }
        public int Step5TotalItems { get; set; }
        public int Step6TotalItems { get; set; }
        public int CompletedTotalItems { get; set; }

        // Propiedades calculadas para paginación de completados
        public string CompletedBaseUrl => $"?ShowWizard=true&Step=7&ManagementId={ManagementId}" +
            (SelectedLabId.HasValue ? $"&SelectedLabId={SelectedLabId}" : "") +
            (!string.IsNullOrEmpty(SearchTerm) ? $"&SearchTerm={Uri.EscapeDataString(SearchTerm)}" : "") +
            (!string.IsNullOrEmpty(SerialNumberFilter) ? $"&SerialNumberFilter={Uri.EscapeDataString(SerialNumberFilter)}" : "") +
            (!string.IsNullOrEmpty(InventoryNumberFilter) ? $"&InventoryNumberFilter={Uri.EscapeDataString(InventoryNumberFilter)}" : "");

        public int CompletedTotalPages => CompletedTotalItems > 0 ? (int)Math.Ceiling((double)CompletedTotalItems / 5.0) : 0;
        public int Step1LabTotalPages => Step1Labs.Count > 0 ? (int)Math.Ceiling((double)Step1Labs.Count / 3.0) : 0;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Cargar Listas para Filtros
                var labsQuery = _context.Laboratories.Where(l => l.Status == GeneralStatus.Activo);
                if (ActiveManagement?.FacultyId.HasValue == true)
                {
                    labsQuery = labsQuery.Where(l => l.FacultyId == ActiveManagement.FacultyId.Value);
                }
                var labs = await labsQuery.OrderBy(l => l.Code).ThenBy(l => l.Name).ToListAsync();
                LabFList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName");

                var techsList = await _context.People
                    .Where(p => p.Category == PersonCategory.Tecnico && p.Status == GeneralStatus.Activo)
                    .ToListAsync();
                
                var techs = techsList.OrderBy(t => t.FullName).ToList();
                TechFList = new SelectList(techs, "Id", "FullName");

                CategoryFList = new SelectList(Enum.GetValues(typeof(EquipmentCategory))
                    .Cast<EquipmentCategory>()
                    .Select(e => new { Id = (int)e, Name = e.ToString() }), "Id", "Name");

                // Buscar Gestión Activa (cached), o usar ManagementId directo
                if (ManagementId.HasValue && ShowWizard)
                {
                    ActiveManagement = await _context.Managements
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == ManagementId.Value && m.Status == ManagementStatus.Active);
                }

                ActiveManagement ??= await _managementContext.GetCurrentManagementAsync();

                if (!ShowWizard && ActiveManagement != null && ManagementId.HasValue && ManagementId.Value != ActiveManagement.Id)
                {
                    return RedirectToPage(new { ManagementId = ActiveManagement.Id });
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
                    var dashboardData = await _dashboardReadService.LoadAsync(
                        ActiveManagement.Id,
                        ActiveManagement.Type,
                        new DashboardReadFilter
                        {
                            LabFilterId = LabFilterId,
                            CategoryFilterId = CategoryFilterId,
                            TechFilterId = TechFilterId,
                            StatusFilter = StatusFilter,
                            SearchTerm = SearchTerm,
                            SerialNumber = SerialNumberFilter,
                            InventoryNumber = InventoryNumberFilter
                        },
                        ShowWizard,
                        SelectedLabId,
                        Step,
                        CurrentPage);

                    TotalActivos = dashboardData.TotalActivos;
                    EquiposTerminados = dashboardData.EquiposTerminados;
                    TotalVencidos = dashboardData.TotalVencidos;
                    GlobalProgress = dashboardData.GlobalProgress;
                    CountPendientes = dashboardData.CountPendientes;
                    CountBuenos = dashboardData.CountBuenos;
                    CountL6 = dashboardData.CountL6;
                    CountL7 = dashboardData.CountL7;
                    CountL8 = dashboardData.CountL8;
                    CountSalida = dashboardData.CountSalida;
                    CountDesembolso = dashboardData.CountDesembolso;
                    TopEquipmentTypes = dashboardData.TopEquipmentTypes;
                    TopGroups = dashboardData.TopGroups;
                    TopLaboratories = dashboardData.TopLaboratories;
                    OverduePlans = dashboardData.OverduePlans;
                    ManagementPlans = dashboardData.ManagementPlans;

                    if (ShowWizard)
                    {
                        LaboratoriesList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", SelectedLabId);
                        // Nota: Step1Labs ya se carga filtrado por gestión en LoadStep1LabsAsync
                        ActivePlans = dashboardData.ActivePlans;
                        Step1Plans = dashboardData.Step1Plans;
                        Step1Labs = await LoadStep1LabsAsync(ActiveManagement.Id);
                        Step2Plans = dashboardData.Step2Plans;
                        Step3Plans = dashboardData.Step3Plans;
                        Step4Plans = dashboardData.Step4Plans;
                        Step5Plans = dashboardData.Step5Plans;
                        Step6Plans = dashboardData.Step6Plans;
                        CompletedPlans = dashboardData.CompletedPlans;

                        ActiveTotalItems = dashboardData.ActiveTotalItems;
                        Step1TotalItems = dashboardData.Step1TotalItems;
                        Step2TotalItems = dashboardData.Step2TotalItems;
                        Step3TotalItems = dashboardData.Step3TotalItems;
                        Step4TotalItems = dashboardData.Step4TotalItems;
                        Step5TotalItems = dashboardData.Step5TotalItems;
                        Step6TotalItems = dashboardData.Step6TotalItems;
                        CompletedTotalItems = dashboardData.CompletedTotalItems;

                        // Modo equipo único: sobreescribir listas para mostrar solo el plan enfocado
                        if (FocusPlanId.HasValue)
                        {
                            FocusedPlan = await _context.ManagementPlans
                                .AsNoTracking()
                                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                                .Include(p => p.Verification).ThenInclude(v => v!.Faults)
                                .Include(p => p.TechnicalRequest).ThenInclude(r => r!.RequestedBy)
                                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                                .Include(p => p.Departure)
                                .Include(p => p.KardexHistory)
                                .Include(p => p.AcquisitionRequest)
                                .FirstOrDefaultAsync(p => p.Id == FocusPlanId.Value
                                    && p.ManagementId == ActiveManagement.Id);

                            if (FocusedPlan != null)
                            {
                                Step1Plans = (FocusedPlan.VerificationId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.Verification)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                Step2Plans = (FocusedPlan.RequestId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.TechnicalRequest)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                Step3Plans = (FocusedPlan.MaintenanceId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.Maintenance)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                Step4Plans = (FocusedPlan.DepartureId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.Exit)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                Step5Plans = (FocusedPlan.KardexHistoryId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.Kardex)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                Step6Plans = (FocusedPlan.AcquisitionRequestId.HasValue || FocusedPlan.CurrentPhase == WizardPhase.Disbursement)
                                    ? new List<ManagementPlan> { FocusedPlan } : new();
                                ActivePlans = new List<ManagementPlan>();
                                CompletedPlans = IsCompletedPlan(FocusedPlan) || FocusedPlan.VerificationId.HasValue || FocusedPlan.RequestId.HasValue || FocusedPlan.MaintenanceId.HasValue || FocusedPlan.DepartureId.HasValue || FocusedPlan.KardexHistoryId.HasValue || FocusedPlan.AcquisitionRequestId.HasValue
                                    ? new List<ManagementPlan> { FocusedPlan }
                                    : new();

                                SelectedLabId = FocusedPlan.EquipmentUnit?.LaboratoryId;

                                Step1TotalItems = Step1Plans.Count;
                                Step2TotalItems = Step2Plans.Count;
                                Step3TotalItems = Step3Plans.Count;
                                Step4TotalItems = Step4Plans.Count;
                                Step5TotalItems = Step5Plans.Count;
                                Step6TotalItems = Step6Plans.Count;
                                ActiveTotalItems = 0;
                                CompletedTotalItems = CompletedPlans.Count;
                            }
                        }
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
            return RedirectToPage(new {
                ShowWizard = true,
                Step = nextStep,
                SelectedLabId = SelectedLabId,
                ManagementId = ManagementId ?? ActiveManagement?.Id,
                FocusPlanId = FocusPlanId,
                SearchTerm = SearchTerm,
                SerialNumberFilter = SerialNumberFilter,
                InventoryNumberFilter = InventoryNumberFilter,
                CurrentPage = 1
            });
        }

        public async Task<IActionResult> OnPostPreviousStep()
        {
            var management = ManagementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                : await _managementContext.GetCurrentManagementAsync();

            var isCorrective = management?.Type == ManagementType.Corrective;
            var minStep = isCorrective ? 2 : 1;
            int prevStep = Step > minStep ? Step - 1 : 0;
            return RedirectToPage(new {
                ShowWizard = true,
                Step = prevStep,
                SelectedLabId = SelectedLabId,
                ManagementId = ManagementId ?? management?.Id,
                FocusPlanId = FocusPlanId,
                SearchTerm = SearchTerm,
                SerialNumberFilter = SerialNumberFilter,
                InventoryNumberFilter = InventoryNumberFilter,
                CurrentPage = 1
            });
        }

        public async Task<IActionResult> OnPostRefresh()
        {
            Management? activeManagement = null;
            if (ManagementId.HasValue)
            {
                activeManagement = await _context.Managements
                    .Include(m => m.ManagementPlans)
                    .FirstOrDefaultAsync(m => m.Id == ManagementId.Value && m.Status == ManagementStatus.Active);
            }

            activeManagement ??= await _context.Managements
                .Include(m => m.ManagementPlans)
                .Where(m => m.Status == ManagementStatus.Active && m.Type == ManagementType.Preventive)
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Semester)
                .ThenByDescending(m => m.CreatedDate)
                .FirstOrDefaultAsync();

            if (activeManagement == null)
            {
                TempData.Error("No hay gestión activa para sincronizar.");
                return RedirectToPage();
            }

            if (activeManagement.Type == ManagementType.Corrective)
            {
                TempData.Warning("El flujo correctivo no sincroniza equipos. Reporte cada falla desde L-7 para crear su propio proceso.");
                return RedirectToPage(new { ShowWizard = true, Step = 2, SelectedLabId, ManagementId = activeManagement.Id });
            }

            if (activeManagement.Type == ManagementType.Preventive)
            {
                TempData.Info("Seleccione las categorias, subclasificaciones y unidades que entraran a esta ronda preventiva.");
                return RedirectToPage("/Managements/PlanAssets", new { id = activeManagement.Id });
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

            return RedirectToPage(new { ShowWizard, Step, SelectedLabId, ManagementId = activeManagement.Id, FocusPlanId });
        }

        /// <summary>
        /// Carga TODOS los laboratorios con equipos pendientes de L-6, sin filtrar por SelectedLabId.
        /// Step 1 es un selector de laboratorio — debe mostrar todos los disponibles.
        /// </summary>
        private async Task<List<Step1LabSummary>> LoadStep1LabsAsync(int managementId)
        {
            var plans = await _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Where(p => p.ManagementId == managementId
                    && p.EquipmentUnit != null
                    && p.EquipmentUnit.Laboratory != null
                    && p.CurrentPhase == WizardPhase.Verification
                    && (p.CurrentState == WizardEquipmentState.PendingVerification
                        || (p.IsDraft && p.DraftPhase == WizardPhase.Verification)))
                .ToListAsync();

            return plans
                .GroupBy(p => new { Id = p.EquipmentUnit!.LaboratoryId!.Value, Name = p.EquipmentUnit!.Laboratory!.Name })
                .Select(g => new Step1LabSummary
                {
                    LabId = g.Key.Id,
                    LabName = g.Key.Name,
                    PendingCount = g.Count()
                })
                .OrderBy(l => l.LabName)
                .ToList();
        }

        public async Task<IActionResult> OnPostFastFail(int? planId)
        {
            if (planId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(candidate => candidate.Management)
                    .FirstOrDefaultAsync(candidate => candidate.Id == planId.Value &&
                        (!ManagementId.HasValue || candidate.ManagementId == ManagementId.Value));

                if (plan?.IsReadOnly == true)
                {
                    TempData.Error("La gestión está cerrada y no admite cambios.");
                    return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId = plan.ManagementId, FocusPlanId });
                }

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
                    return RedirectToPage(new { ShowWizard = true, Step = (int)plan.CurrentPhase, SelectedLabId = SelectedLabId, ManagementId = plan.ManagementId, FocusPlanId });
                }
            }

            Management? activeManagement = null;
            if (ManagementId.HasValue)
            {
                activeManagement = await _context.Managements
                    .Where(m => m.Id == ManagementId.Value && m.Status == ManagementStatus.Active)
                    .FirstOrDefaultAsync();
            }

            activeManagement ??= await _managementContext.GetCurrentManagementAsync(ManagementType.Preventive);

            if (activeManagement == null)
            {
                TempData.Error("Para usar la Falla Rápida debe existir un periodo de Gestión Institucional activo.");
                return RedirectToPage();
            }

            TempData.Error("No se encontró el plan de gestión para este equipo.");
            return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId = ManagementId, FocusPlanId });
        }

        public async Task<IActionResult> OnPostConfirmKardexAsync(int planId)
        {
            int? redirectManagementId = ManagementId;
            try
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.EquipmentUnit)
                    .Include(p => p.Maintenance)
                    .Include(p => p.Management)
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.Id == planId);

                if (plan == null)
                {
                    TempData.Error("No se encontró el plan solicitado.");
                    return RedirectToPage(new { ShowWizard = true, Step = 5, ManagementId, FocusPlanId });
                }

                redirectManagementId = plan.ManagementId;

                if (plan.IsReadOnly)
                {
                    TempData.Error("La gestión está cerrada y no admite cambios.");
                    return RedirectToPage(new { ShowWizard = true, Step = 5, ManagementId = plan.ManagementId, FocusPlanId });
                }

                if (plan.CurrentPhase != WizardPhase.Kardex || plan.EquipmentUnit == null)
                {
                    TempData.Error("El equipo no está listo para confirmar su Kardex.");
                    return RedirectToPage(new { ShowWizard = true, Step = (int)plan.CurrentPhase, ManagementId = plan.ManagementId, FocusPlanId });
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();
                var historyDate = DateTime.UtcNow;

                var previousOpenHistory = await _context.EquipmentStateHistories
                    .AsTracking()
                    .Where(history => history.EquipmentUnitId == plan.EquipmentUnit.Id && history.EndDate == null)
                    .OrderByDescending(history => history.StartDate)
                    .FirstOrDefaultAsync();

                if (previousOpenHistory != null)
                    previousOpenHistory.EndDate = historyDate;

                var kardexEntry = new EquipmentStateHistory
                {
                    EquipmentUnitId = plan.EquipmentUnit.Id,
                    Status = EquipmentStatus.Operational,
                    StartDate = historyDate,
                    Reason = $"Kardex: Mantenimiento #{plan.MaintenanceId} finalizado en Gestión #{plan.ManagementId}.",
                    CreatedDate = historyDate
                };
                _context.EquipmentStateHistories.Add(kardexEntry);

                plan.EquipmentUnit.CurrentStatus = EquipmentStatus.Operational;

                await _context.SaveChangesAsync();

                plan.KardexHistoryId = kardexEntry.Id;
                plan.CurrentPhase = WizardPhase.Disbursement;
                plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
                plan.PlanStatus = ManagementPlanStatus.InProgress;
                plan.IsDraft = false;
                plan.DraftPhase = null;
                plan.DraftSavedAt = null;
                plan.DraftSummary = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData.Success("Kardex actualizado. El equipo está marcado como Operativo.");
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "No se pudo confirmar el Kardex del plan {PlanId}.", planId);
                TempData.Error("No se pudo actualizar el Kardex. Intente nuevamente.");
            }

            return RedirectToPage(new { ShowWizard = true, Step = 6, SelectedLabId = SelectedLabId, ManagementId = redirectManagementId, FocusPlanId });
        }

        public async Task<IActionResult> OnPostUpdateFocusedVerificationObservationAsync(int planId, int verificationId, string? observations)
        {
            var plan = await _context.ManagementPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId
                    && p.VerificationId == verificationId
                    && (!ManagementId.HasValue || p.ManagementId == ManagementId.Value));

            if (plan == null)
            {
                TempData.Error("No se pudo validar el L-6 del equipo enfocado.");
                return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId, FocusPlanId });
            }

            var verification = await _context.Verifications
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == verificationId
                    && v.EquipmentUnitId == plan.EquipmentUnitId
                    && v.ManagementId == plan.ManagementId);

            if (verification == null)
            {
                TempData.Error("No se encontro la verificacion L-6 del equipo enfocado.");
                return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId = plan.ManagementId, FocusPlanId = plan.Id });
            }

            verification.Observations = observations?.Trim();
            verification.LastModifiedDate = DateTime.UtcNow;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                verification.ModifiedById = user.Id;
            }

            await _context.SaveChangesAsync();
            TempData.Success("Observacion L-6 actualizada para el equipo enfocado.");

            return RedirectToPage(new { ShowWizard = true, Step = 1, ManagementId = plan.ManagementId, FocusPlanId = plan.Id });
        }

        public int CountVerificationFails(Verification? v)
        {
            return v?.FailuresCount ?? 0;
        }

        private static bool IsCompletedPlan(ManagementPlan plan)
        {
            return IsCompletedPlan(plan.PlanStatus, plan.CurrentState);
        }

        private static bool IsCompletedPlan(ManagementPlanStatus planStatus, WizardEquipmentState currentState)
        {
            return currentState == WizardEquipmentState.Completed ||
                   planStatus == ManagementPlanStatus.Completed;
        }

        private static bool IsActivePipelinePlan(ManagementPlan plan)
        {
            return IsActivePipelinePlan(plan.PlanStatus, plan.CurrentState);
        }

        private static bool IsActivePipelinePlan(ManagementPlanStatus planStatus, WizardEquipmentState currentState)
        {
            return currentState != WizardEquipmentState.VerifiedGood &&
                   !IsCompletedPlan(planStatus, currentState);
        }

        public async Task<JsonResult> OnGetKardexDetailAsync(int equipmentId)
        {
            var summary = await EquipmentKardexSummaryBuilder.BuildAsync(_context, equipmentId, HttpContext.RequestAborted);
            return summary == null
                ? new JsonResult(new { error = "No encontrado" }) { StatusCode = StatusCodes.Status404NotFound }
                : new JsonResult(summary);
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

        // Paginación
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 5;

        // Filtros preservados para links de paginación
        public string? SearchTerm { get; set; }
        public string? SerialNumberFilter { get; set; }
        public string? InventoryNumberFilter { get; set; }
        public int? SelectedLabId { get; set; }
        public int? ManagementId { get; set; }
        public int? FocusPlanId { get; set; }
    }
}
