using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, 
            UserManager<User> userManager,
            IManagementContextService managementContext)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            LoadLists();

            Input = new InputModel
            {
                ScheduledDate = DateTime.UtcNow,
                MaintenanceType = isWizard ? MaintenanceType.Preventivo : MaintenanceType.Otros,
                CostDetails = new List<CostDetail>(),
                Tasks = new List<MaintenanceTask>
                {
                    new MaintenanceTask { Description = "Limpieza y Desinfección de Componentes" },
                    new MaintenanceTask { Description = "Calibración y Ajuste de Sistema" },
                    new MaintenanceTask { Description = "Pruebas de Esfuerzo y Carga Operativa" },
                    new MaintenanceTask { Description = "Revisión Final de Seguridad y Cierre" }
                }
            };

            if (managementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(managementPlanId.Value);
                if (plan != null)
                {
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    ViewData["ManagementId"] = plan.ManagementId;
                    var mgmt = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
                    ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
                    if (plan.RequestId.HasValue)
                    {
                        Input.RequestId = plan.RequestId;
                    }
                    if (!equipmentUnitId.HasValue && plan.EquipmentUnitId.HasValue)
                    {
                        equipmentUnitId = plan.EquipmentUnitId;
                    }
                }
            }

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;

                    ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
                    ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
                }
            }

            ViewData["IsWizard"] = isWizard;
            ManagementPlanId = managementPlanId;



            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Mantenimiento")]
            public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Otros;

            [Display(Name = "Técnico Responsable")]
            public int? TechnicianId { get; set; }

            [Display(Name = "Solicitud Relacionada")]
            public int? RequestId { get; set; }

            [Display(Name = "Descripción del Trabajo")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Fecha Programada")]
            [DataType(DataType.Date)]
            public DateTime ScheduledDate { get; set; } = DateTime.UtcNow;

            [Display(Name = "Fecha Inicio Real")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Fin Real")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Display(Name = "Costo Real Total")]
            public decimal ActualCost { get; set; }

            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }

            [Display(Name = "Recomendaciones")]
            public string? Recommendations { get; set; }

            [Display(Name = "Estado Inicial")]
            public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

            public List<CostDetail> CostDetails { get; set; } = new();

            public int CompletionPercentage { get; set; } = 0;
            public List<MaintenanceTask> Tasks { get; set; } = new();
            
            // Legacy steps (to be removed once fully migrated if needed)
            public bool Step1_Cleaning { get; set; } = false;
            public bool Step2_Calibration { get; set; } = false;
            public bool Step3_Testing { get; set; } = false;
            public bool Step4_FinalReview { get; set; } = false;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha Sugerida de Próximo Mantenimiento")]
            public DateTime? SuggestedNextMaintenanceDate { get; set; }

            [Display(Name = "Nivel de Satisfacción")]
            public int? SatisfactionLevel { get; set; }
        }

        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new {
                    id = u.Id,
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.eqName} (Inv: {x.inv})"
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            // ─────────────────────────────────────────────────────────────
            // WIZARD PRE-VALIDATION: reconstruir valores ANTES de ModelState
            // Los selects disabled no postean → FacultyId/LaboratoryId/
            // EquipmentUnitId/MaintenanceType llegan como 0 → [Required] falla.
            // ─────────────────────────────────────────────────────────────
            ManagementPlan? wizardPlan = null;
            Management? currentMgmt = null;

            if (ManagementPlanId.HasValue)
            {
                wizardPlan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (wizardPlan != null)
                {
                    currentMgmt = wizardPlan.Management;

                    if (wizardPlan.EquipmentUnitId.HasValue)
                    {
                        Input.EquipmentUnitId = wizardPlan.EquipmentUnitId.Value;
                        Input.LaboratoryId = wizardPlan.EquipmentUnit?.LaboratoryId ?? Input.LaboratoryId;
                        Input.FacultyId = wizardPlan.EquipmentUnit?.Laboratory?.FacultyId ?? Input.FacultyId;
                    }

                    if (isWizard)
                    {
                        Input.MaintenanceType = MaintenanceType.Preventivo;
                    }

                    if (wizardPlan.RequestId.HasValue)
                    {
                        Input.RequestId = wizardPlan.RequestId;
                    }

                    // Limpiar keys que el servidor acaba de reconstruir (disabled inputs NO postean)
                    ModelState.Remove("Input.FacultyId");
                    ModelState.Remove("Input.LaboratoryId");
                    ModelState.Remove("Input.EquipmentUnitId");
                    ModelState.Remove("Input.MaintenanceType");
                }
            }

            if (!isWizard && !ManagementPlanId.HasValue)
            {
                currentMgmt = await _managementContext.GetCurrentManagementAsync();
            }

            // Restaurar ViewData wizard para re-render en caso de error
            if (wizardPlan != null)
            {
                ViewData["IsWizard"] = true;
                ViewData["CurrentPhaseInt"] = (int)wizardPlan.CurrentPhase;
                ViewData["ManagementId"] = wizardPlan.ManagementId;
                ViewData["ManagementPlanId"] = wizardPlan.Id;
                var mgmt = await _context.Managements.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == wizardPlan.ManagementId);
                ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
            }

if (Input.StartDate.HasValue && Input.EndDate.HasValue)
            {
                if (Input.EndDate < Input.StartDate)
                    ModelState.AddModelError("Input.EndDate", "La fecha de finalización no puede ser anterior al inicio.");
            }

            if (isWizard && !Input.TechnicianId.HasValue)
            {
                ModelState.AddModelError("Input.TechnicianId", "El técnico responsable es obligatorio en el flujo de mantenimiento. Asígnelo para que L-3 pueda derivar el responsable de la salida.");
            }

            if (Input.Status == MaintenanceStatus.Completed && Input.CompletionPercentage != 100)
            {
                ModelState.AddModelError("Input.CompletionPercentage", "Para marcar el mantenimiento como Completado, el avance debe estar al 100%.");
            }

            var equipmentUnit = await _context.EquipmentUnits.Include(u => u.Equipment).AsTracking().FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);
            if (equipmentUnit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no existe.");
            }
            else if (equipmentUnit.CurrentStatus == EquipmentStatus.OnLoan)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "No se puede realizar mantenimiento a un equipo que actualmente está en préstamo.");
            }
            else if (equipmentUnit.LaboratoryId == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no tiene laboratorio asignado. Asigne un laboratorio al equipo antes de registrar el mantenimiento.");
            }

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.CostDetails")).ToList())
            {
                ModelState.Remove(key);
            }

            Input.CostDetails = Input.CostDetails?
                .Where(d => !string.IsNullOrWhiteSpace(d.Concept))
                .ToList() ?? new List<CostDetail>();

            foreach (var detail in Input.CostDetails)
            {
                detail.Quantity = detail.Quantity <= 0 ? 1 : detail.Quantity;
                detail.UnitPrice = detail.UnitPrice < 0 ? 0 : detail.UnitPrice;
            }

            decimal totalCosts = Input.CostDetails?.Sum(d => d.Subtotal) ?? 0;
            Input.ActualCost = totalCosts;

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage))
                    .Distinct()
                    .Take(4)
                    .ToList();

                TempData.Error(errors.Count > 0
                    ? "No se pudo registrar el mantenimiento: " + string.Join(" ", errors)
                    : "No se pudo registrar el mantenimiento. Revise los campos obligatorios.");

                LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            try
            {
                currentMgmt ??= await _managementContext.GetCurrentManagementAsync();
                if (currentMgmt == null)
                {
                    TempData.Warning("No hay una gestion activa disponible. Por favor active una gestion institucional para registrar el mantenimiento.");
                    LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    return Page();
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var maintenance = new Maintenance
                {
                    EquipmentUnitId = Input.EquipmentUnitId,
                    ManagementId = currentMgmt.Id,
                    MaintenanceType = Input.MaintenanceType,
                    TechnicianId = Input.TechnicianId,
                    RequestId = Input.RequestId,
                    Description = Input.Description?.Clean(),
                    ScheduledDate = Input.ScheduledDate,
                    StartDate = Input.StartDate,
                    EndDate = Input.EndDate,
                    ActualCost = Input.ActualCost,
                    Observations = Input.Observations?.Clean(),
                    Recommendations = Input.Recommendations?.Clean(),
                    SuggestedNextMaintenanceDate = Input.SuggestedNextMaintenanceDate,
                    SatisfactionLevel = (MaintenanceSatisfaction?)Input.SatisfactionLevel,
                    Status = Input.Status,
                    CostDetails = Input.CostDetails ?? new(),
                    CreatedDate = DateTime.UtcNow,

                    CompletionPercentage = Input.CompletionPercentage,
                    Tasks = Input.Tasks ?? new()
                };

                var currentUser = await _userManager.GetUserAsync(User);
                maintenance.CreatedById = currentUser?.Id;

                _context.Maintenances.Add(maintenance);

                if (equipmentUnit != null && equipmentUnit.CurrentStatus != EquipmentStatus.UnderMaintenance)
                {
                    var lastHistory = await _context.EquipmentStateHistories
                        .Where(h => h.EquipmentUnitId == equipmentUnit.Id && h.EndDate == null)
                        .OrderByDescending(h => h.StartDate)
                        .AsTracking()
                        .FirstOrDefaultAsync();

                    if (lastHistory != null)
                    {
                        lastHistory.EndDate = DateTime.UtcNow;
                        _context.EquipmentStateHistories.Update(lastHistory);
                    }

                    var newHistory = new EquipmentStateHistory
                    {
                        EquipmentUnitId = equipmentUnit.Id,
                        Status = EquipmentStatus.UnderMaintenance,
                        StartDate = DateTime.UtcNow,
                        Reason = "Ingreso a proceso de mantenimiento."
                    };
                    _context.EquipmentStateHistories.Add(newHistory);

                    equipmentUnit.CurrentStatus = EquipmentStatus.UnderMaintenance;
                    _context.EquipmentUnits.Update(equipmentUnit);
                }

                await _context.SaveChangesAsync();

                if (wizardPlan != null)
                {
                    wizardPlan.MaintenanceId = maintenance.Id;
                    wizardPlan.CurrentPhase = WizardPhase.Exit;
                    wizardPlan.CurrentState = WizardEquipmentState.AwaitingDeparture;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                await TryCreateMaintenanceNotificationAsync(maintenance, equipmentUnit);

                TempData.Success($"Mantenimiento para '{equipmentUnit?.Equipment?.Name}' guardado correctamente.");

                if (wizardPlan != null && isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 4, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al guardar el registro: {ex.Message}");
                LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }
        }

        private async Task TryCreateMaintenanceNotificationAsync(Maintenance maintenance, EquipmentUnit? equipmentUnit)
        {
            if (!maintenance.ScheduledDate.HasValue || !maintenance.TechnicianId.HasValue)
                return;

            var daysUntil = (maintenance.ScheduledDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
            if (daysUntil < 0 || daysUntil > 7)
                return;

            try
            {
                var technician = await _context.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == maintenance.TechnicianId.Value);

                if (string.IsNullOrWhiteSpace(technician?.Email))
                    return;

                var recipient = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == technician.Email);

                if (recipient == null)
                    return;

                _context.Notifications.Add(new Notification
                {
                    UserId = recipient.Id,
                    Title = "Mantenimiento Próximo a Vencer",
                    Message = $"El mantenimiento de la unidad {equipmentUnit?.InventoryNumber} debe realizarse el {maintenance.ScheduledDate.Value:dd/MM/yyyy}.",
                    ActionUrl = $"/Maintenances/Details/{maintenance.Id}",
                    IconClass = "fas fa-exclamation-triangle text-warning",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // La notificación no forma parte del guardado crítico de L-8.
            }
        }

        private void LoadLists(int facultyId = 0, int labId = 0, int equipUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name", facultyId);

            if (facultyId > 0)
            {
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories
                    .Where(l => l.FacultyId == facultyId)
                    .ToList(), "Id", "Name", labId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (labId > 0)
            {
                ViewData["EquipmentUnitId"] = new SelectList(_context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == labId)
                    .Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToList(), "Id", "Name", equipUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            // Fix: FullName is NotMapped and causes LINQ translation errors in TPT.
            var people = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .ToList();

            var technicianList = people
                .Select(p => new { 
                    Id = p.Id, 
                    Name = p is Intern i ? i.Name : (p is Extern e ? e.Name : "Técnico #" + p.Id) 
                })
                .OrderBy(x => x.Name)
                .ToList();

            ViewData["TechnicianId"] = new SelectList(technicianList, "Id", "Name");

            ViewData["MaintenanceType"] = EnumHelper.GetStatusSelectList<MaintenanceType>();
            ViewData["ServiceType"] = EnumHelper.GetStatusSelectList<ServiceType>();
            var requests = _context.Requests
                .Include(r => r.Laboratory)
                .OrderByDescending(r => r.CreatedDate)
                .Take(20)
                .ToList()
                .Select(r => new {
                    Id = r.Id,
                    DisplayText = $"#{r.Id} - {r.Laboratory?.Name} ({r.CreatedDate:dd/MM}): " + (r.Description.Length > 40 ? r.Description.Substring(0, 40) + "..." : r.Description)
                });
            ViewData["RequestId"] = new SelectList(requests, "Id", "DisplayText");
        }
    }
}
