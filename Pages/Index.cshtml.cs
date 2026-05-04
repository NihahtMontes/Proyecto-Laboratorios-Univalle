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

        [BindProperty(SupportsGet = true)]
        public bool ShowWizard { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        public int CountL6 { get; set; }
        public int CountL7 { get; set; }
        public int CountL8 { get; set; }
        public int CountSalida { get; set; }
        public int CountDesembolso { get; set; }

        public int CountPendientes { get; set; }
        public int CountBuenos { get; set; }

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
                // Aseguramos que el usuario esté cargado para evitar problemas con notificaciones en el Layout
                var user = await _userManager.GetUserAsync(User);

                var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
                LabFList = new SelectList(labs, "Id", "Name");

                var techsList = await _context.People.Where(p => p.Category == PersonCategory.Tecnico).ToListAsync();
                TechFList = new SelectList(techsList.OrderBy(t => t.FullName), "Id", "FullName");

                CategoryFList = new SelectList(Enum.GetValues(typeof(EquipmentCategory))
                    .Cast<EquipmentCategory>()
                    .Select(e => new { Id = (int)e, Name = e.ToString() }), "Id", "Name");

                ActiveManagement = await _managementContext.GetCurrentManagementAsync();

                if (ActiveManagement != null)
                {
                    var statsQuery = _context.ManagementPlans.AsNoTracking().Where(p => p.ManagementId == ActiveManagement.Id);
                    var allStats = await statsQuery.Select(p => new { p.PlanStatus, p.CurrentPhase, p.CurrentState, p.PlannedDate }).ToListAsync();

                    TotalActivos = allStats.Count;
                    EquiposTerminados = allStats.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);

                    var now = DateTime.Now;
                    TotalVencidos = allStats.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate < now);
                    GlobalProgress = TotalActivos > 0 ? Math.Round((double)EquiposTerminados / TotalActivos * 100, 1) : 0;

                    CountPendientes = allStats.Count(p => p.PlanStatus != ManagementPlanStatus.Completed && p.CurrentState >= WizardEquipmentState.AwaitingRequest);
                    CountBuenos = allStats.Count(p => p.CurrentState == WizardEquipmentState.VerifiedGood);

                    CountL6 = allStats.Count(p => p.CurrentPhase == WizardPhase.Verification);
                    CountL7 = allStats.Count(p => p.CurrentPhase == WizardPhase.TechnicalRequest);
                    CountL8 = allStats.Count(p => p.CurrentPhase == WizardPhase.Maintenance);
                    CountSalida = allStats.Count(p => p.CurrentPhase == WizardPhase.Exit);
                    CountDesembolso = allStats.Count(p => p.CurrentPhase == WizardPhase.Disbursement);

                    var dataQuery = _context.ManagementPlans.AsNoTracking()
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                        .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                        .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                        .Where(p => p.ManagementId == ActiveManagement.Id);

                    var equipmentStats = await _context.ManagementPlans.AsNoTracking()
                        .Where(p => p.ManagementId == ActiveManagement.Id && p.EquipmentUnit != null && p.EquipmentUnit.Equipment != null)
                        .Select(p => new { Category = p.EquipmentUnit!.Equipment!.Category, TypeClass = p.EquipmentUnit!.Equipment!.TypeClassification, LabName = p.EquipmentUnit!.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A" })
                        .ToListAsync();

                    TopEquipmentTypes = equipmentStats.GroupBy(p => p.Category == EquipmentCategory.Utensil ? "Utensilio" : p.TypeClass switch
                    {
                        EquipmentTypeClassification.Electronico => "Electrónico / Eléctrico",
                        EquipmentTypeClassification.Manual => "Manual / Mecánico",
                        EquipmentTypeClassification.Mobiliario => "Mobiliario",
                        EquipmentTypeClassification.Medicion => "Instrumental de Medición",
                        EquipmentTypeClassification.Vidrio => "Material de Vidrio",
                        EquipmentTypeClassification.Informatico => "Informático",
                        _ => "Otro"
                    }).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());

                    TopLaboratories = equipmentStats.Where(p => p.LabName != "N/A").GroupBy(p => p.LabName).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());

                    OverduePlans = await dataQuery.Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate < now.AddDays(7))
                        .OrderBy(p => p.PlannedDate).Select(p => new OverduePlanDto { Id = p.Id, PlannedDate = p.PlannedDate, PlanStatus = p.PlanStatus, EquipmentName = p.EquipmentUnit!.Equipment!.Name, LaboratoryName = p.EquipmentUnit.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A" }).ToListAsync();

                    var cronogramaQuery = dataQuery.AsQueryable();
                    if (LabFilterId.HasValue) cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit!.LaboratoryId == LabFilterId);

                    ManagementPlans = await cronogramaQuery.Where(p => p.PlannedDate.HasValue).OrderBy(p => p.PlannedDate).Take(10).ToListAsync();

                    if (ShowWizard)
                    {
                        LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);
                        var wizardQuery = dataQuery.AsQueryable();
                        if (SelectedLabId.HasValue) wizardQuery = wizardQuery.Where(p => p.EquipmentUnit!.LaboratoryId == SelectedLabId.Value);

                        ActivePlans = await wizardQuery.Take(100).ToListAsync();
                        Step1Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Verification).ToList();
                        Step2Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.TechnicalRequest).ToList();
                        Step3Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Maintenance).ToList();
                        Step4Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Exit).ToList();
                        Step5Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Kardex).ToList();
                        Step6Plans = ActivePlans.Where(p => p.CurrentPhase == WizardPhase.Disbursement).ToList();
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

        // Resto de métodos Post se mantienen igual para no afectar la funcionalidad del Wizard y Sincronización
        public IActionResult OnPostNextStep() => RedirectToPage(new { ShowWizard = true, Step = Step + 1, SelectedLabId = SelectedLabId });
        public IActionResult OnPostPreviousStep() => RedirectToPage(new { ShowWizard = true, Step = Step > 1 ? Step - 1 : 0, SelectedLabId = SelectedLabId });

        public async Task<IActionResult> OnPostRefresh()
        {
            var activeManagement = await _context.Managements.Include(m => m.ManagementPlans).FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);
            if (activeManagement == null) return RedirectToPage();

            var existingIds = activeManagement.ManagementPlans.Where(p => p.EquipmentUnitId.HasValue).Select(p => p.EquipmentUnitId!.Value).ToList();
            var newEquipments = await _context.EquipmentUnits.Where(eu => eu.CurrentStatus != EquipmentStatus.Deleted && !existingIds.Contains(eu.Id)).ToListAsync();

            if (newEquipments.Any())
            {
                foreach (var eu in newEquipments)
                {
                    _context.ManagementPlans.Add(new ManagementPlan { ManagementId = activeManagement.Id, EquipmentUnitId = eu.Id, CurrentPhase = WizardPhase.Verification, CurrentState = WizardEquipmentState.PendingVerification, PlanStatus = ManagementPlanStatus.Pending, CreatedDate = DateTime.UtcNow, PlannedDate = DateTime.Today.AddDays(7) });
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Sincronización exitosa.";
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
                    return RedirectToPage("/Requests/Create", new { equipmentUnitId = plan.EquipmentUnitId, isWizard = true, managementPlanId = plan.Id });
                }
            }
            return RedirectToPage(new { ShowWizard = true, Step = 1 });
        }

        public async Task<IActionResult> OnPostConfirmKardexAsync(int planId)
        {
            var plan = await _context.ManagementPlans.Include(p => p.EquipmentUnit).AsTracking().FirstOrDefaultAsync(p => p.Id == planId);
            if (plan != null)
            {
                _context.EquipmentStateHistories.Add(new EquipmentStateHistory { EquipmentUnitId = plan.EquipmentUnitId ?? 0, Status = EquipmentStatus.Operational, StartDate = DateTime.UtcNow, Reason = "Kardex actualizado via Wizard." });
                if (plan.EquipmentUnit != null) plan.EquipmentUnit.CurrentStatus = EquipmentStatus.Operational;
                plan.CurrentPhase = WizardPhase.Disbursement;
                plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage(new { ShowWizard = true, Step = 5, SelectedLabId = SelectedLabId });
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