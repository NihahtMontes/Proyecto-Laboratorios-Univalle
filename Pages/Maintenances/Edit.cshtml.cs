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

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "El equipo es obligatorio")]
            [Display(Name = "Equipo Objetivo")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Servicio")]
            public MaintenanceType MaintenanceType { get; set; }

            [Display(Name = "Técnico Responsable")]
            public int? TechnicianId { get; set; }

            [Display(Name = "Fecha Programada")]
            [DataType(DataType.Date)]
            public DateTime? ScheduledDate { get; set; }

            [Display(Name = "Inicio Real")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Finalización")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Required]
            [Display(Name = "Estado del Proceso")]
            public MaintenanceStatus Status { get; set; }

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Descripción del Trabajo / Requerimiento")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Nivel de Satisfacción")]
            public MaintenanceSatisfaction? SatisfactionLevel { get; set; }

            [Display(Name = "Recomendaciones Post-Servicio")]
            public string? Recommendations { get; set; }

            [Display(Name = "Observaciones Internas")]
            public string? Observations { get; set; }

            [Display(Name = "Costo Real Total")]
            public decimal ActualCost { get; set; }

            public List<CostDetail> CostDetails { get; set; } = new();

            // --- PROPIEDADES AGREGADAS PARA LOS CHECKBOXES (L-48) ---
            public int CompletionPercentage { get; set; } = 0;
            public bool Step1_Cleaning { get; set; } = false;
            public bool Step2_Calibration { get; set; } = false;
            public bool Step3_Testing { get; set; } = false;
            public bool Step4_FinalReview { get; set; } = false;
            // --------------------------------------------------------

            // --- TAREAS DINÁMICAS L-48 ---
            public List<MaintenanceTask> Tasks { get; set; } = new();
            // -----------------------------
        }

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FocusPlanId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, bool isWizard = false)
        {
            if (id == null) return NotFound();

            var maintenance = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null) return NotFound();

            int facultyId = maintenance.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
            int labId = maintenance.EquipmentUnit?.LaboratoryId ?? 0;

            Input = new InputModel
            {
                Id = maintenance.Id,
                FacultyId = facultyId,
                LaboratoryId = labId,
                EquipmentUnitId = maintenance.EquipmentUnitId,
                MaintenanceType = maintenance.MaintenanceType,
                TechnicianId = maintenance.TechnicianId,
                ScheduledDate = maintenance.ScheduledDate,
                StartDate = maintenance.StartDate,
                EndDate = maintenance.EndDate,
                Status = maintenance.Status,
                Description = maintenance.Description ?? string.Empty,
                SatisfactionLevel = maintenance.SatisfactionLevel,
                Recommendations = maintenance.Recommendations,
                Observations = maintenance.Observations,
                ActualCost = maintenance.ActualCost ?? 0m,
                CostDetails = maintenance.CostDetails.ToList(),

                // --- RECUPERAR DATOS DE CHECKBOXES DESDE LA BD ---
                CompletionPercentage = maintenance.CompletionPercentage,
                Step1_Cleaning = maintenance.Step1_Cleaning,
                Step2_Calibration = maintenance.Step2_Calibration,
                Step3_Testing = maintenance.Step3_Testing,
                Step4_FinalReview = maintenance.Step4_FinalReview
                // -------------------------------------------------
            };

            // Cargar tareas dinámicas (excluyendo eliminadas)
            Input.Tasks = maintenance.Tasks
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Id)
                .ToList();

            // Si no hay tareas, crear las 4 por defecto
            if (!Input.Tasks.Any())
            {
                Input.Tasks = GetDefaultTasks();
            }

            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            // Solo mostrar wizard si viene explícitamente del wizard
            if (ManagementPlanId.HasValue || isWizard)
            {
                ManagementPlan? plan = null;
                if (ManagementPlanId.HasValue)
                    plan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                else
                    plan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.MaintenanceId == id);

                if (plan != null)
                {
                    ViewData["IsWizard"] = true;
                    ViewData["ManagementPlanId"] = plan.Id;
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    ViewData["ManagementId"] = plan.ManagementId;
                    if (plan.RequestId.HasValue) ViewData["PreviousPhaseId"] = plan.RequestId.Value;
                }
            }

            ViewData["IsWizardFlag"] = isWizard;
            ViewData["ReturnUrl"] = HttpContext.Request.Query["returnUrl"].ToString();

            CargarListas(facultyId, labId, maintenance.EquipmentUnitId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Limpieza y Normalización
            Input.Description = Input.Description.Clean();
            Input.Observations = Input.Observations.Clean();
            Input.Recommendations = Input.Recommendations.Clean();

            // Validaciones Lógicas
            if (Input.StartDate.HasValue && Input.EndDate.HasValue)
            {
                if (Input.EndDate < Input.StartDate)
                {
                    ModelState.AddModelError("Input.EndDate", NotificationHelper.Maintenances.EndDateBeforeStart);
                }
            }

            decimal totalCosts = Input.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
            Input.ActualCost = totalCosts;

            if (Input.Status == MaintenanceStatus.Completed)
            {
                if (totalCosts <= 0)
                    ModelState.AddModelError("Input.Status", "Para estado Completado, debe registrar costos reales.");
                if (Input.SatisfactionLevel == null)
                    ModelState.AddModelError("Input.SatisfactionLevel", "Para estado Completado, la Evaluación de Satisfacción es obligatoria.");
                if (string.IsNullOrWhiteSpace(Input.Recommendations))
                    ModelState.AddModelError("Input.Recommendations", "Para estado Completado, las Recomendaciones Técnicas son obligatorias.");
                if (string.IsNullOrWhiteSpace(Input.Observations))
                    ModelState.AddModelError("Input.Observations", "Para estado Completado, las Observaciones Internas son obligatorias.");
            }

            if (!ModelState.IsValid)
            {
                var maintenance = await _context.Maintenances.Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Laboratory).FirstOrDefaultAsync(m => m.Id == Input.Id);
                int facultyId = maintenance?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = maintenance?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);

                // Restore wizard context only if explicitly set on GET
                if (ManagementPlanId.HasValue)
                {
                    ManagementPlan? errorPlan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                    if (errorPlan != null)
                    {
                        ViewData["IsWizard"] = true;
                        ViewData["ManagementPlanId"] = errorPlan.Id;
                        ViewData["CurrentPhaseInt"] = (int)errorPlan.CurrentPhase;
                        ViewData["ManagementId"] = errorPlan.ManagementId;
                        if (errorPlan.RequestId.HasValue) ViewData["PreviousPhaseId"] = errorPlan.RequestId.Value;
                    }
                }

                ViewData["ReturnUrl"] = HttpContext.Request.Query["returnUrl"].ToString();
                return Page();
            }

            var maintenanceDB = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (maintenanceDB == null) return NotFound();

            maintenanceDB.EquipmentUnitId = Input.EquipmentUnitId;
            maintenanceDB.MaintenanceType = Input.MaintenanceType;
            maintenanceDB.TechnicianId = Input.TechnicianId;
            maintenanceDB.ScheduledDate = Input.ScheduledDate;
            maintenanceDB.StartDate = Input.StartDate;
            maintenanceDB.EndDate = Input.EndDate;
            maintenanceDB.Status = Input.Status;
            maintenanceDB.Description = Input.Description;
            maintenanceDB.SatisfactionLevel = Input.SatisfactionLevel;
            maintenanceDB.Recommendations = Input.Recommendations;
            maintenanceDB.Observations = Input.Observations;
            maintenanceDB.ActualCost = Input.ActualCost;

            // --- GUARDAR LOS DATOS DE CHECKBOXES EN LA BD ---
            // Normalizar y sincronizar tareas dinámicas
            Input.Tasks = (Input.Tasks ?? new())
                .Where(t => !string.IsNullOrWhiteSpace(t.Description))
                .ToList();
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            maintenanceDB.CompletionPercentage = Input.CompletionPercentage;
            UpdateLegacySteps(maintenanceDB, Input.Tasks);
            // ------------------------------------------------

            // Sincronizar tareas dinámicas con la BD
            SyncTasks(maintenanceDB, Input.Tasks);

            if (Input.CostDetails != null)
            {
                Input.CostDetails = Input.CostDetails.Where(d => !string.IsNullOrWhiteSpace(d.Concept)).ToList();
            }
            else
            {
                Input.CostDetails = new List<CostDetail>();
            }

            // Desvincular detalles que ya no están sin borrar historial físico.
            var inputDetailIds = Input.CostDetails.Select(d => d.Id).ToList();
            var detailsToRemove = maintenanceDB.CostDetails.Where(d => !inputDetailIds.Contains(d.Id)).ToList();

            foreach (var detail in detailsToRemove)
            {
                detail.MaintenanceId = null;
                detail.Maintenance = null;
                detail.LastModifiedDate = DateTime.UtcNow;
            }

            // Actualizar o agregar detalles
            foreach (var detailForm in Input.CostDetails)
            {
                detailForm.MaintenanceId = maintenanceDB.Id;
                var existingDetail = maintenanceDB.CostDetails.FirstOrDefault(d => d.Id == detailForm.Id && d.Id != 0);
                if (existingDetail != null)
                {
                    _context.Entry(existingDetail).CurrentValues.SetValues(detailForm);
                }
                else
                {
                    maintenanceDB.CostDetails.Add(detailForm);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Maintenances.Updated(maintenanceDB.EquipmentUnit?.Equipment?.Name));
                
                // Solo redirigir al wizard si ManagementPlanId fue explícito (desde wizard)
                if (ManagementPlanId.HasValue)
                {
                    var plan = await _context.ManagementPlans.AsTracking()
                        .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                    if (plan != null)
                    {
                        if (maintenanceDB.Status == MaintenanceStatus.Completed && plan.CurrentPhase == WizardPhase.Maintenance)
                        {
                            plan.CurrentPhase = WizardPhase.Exit;
                            plan.CurrentState = WizardEquipmentState.AwaitingDeparture;
                            plan.PlanStatus = ManagementPlanStatus.InProgress;
                            await _context.SaveChangesAsync();
                        }

                        return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = maintenanceDB.EquipmentUnit?.LaboratoryId, ManagementId = plan.ManagementId, FocusPlanId });
                    }
                }

                // CRUD: redirigir según returnUrl o Index
                var returnUrlPost = HttpContext.Request.Query["returnUrl"].ToString();
                if (returnUrlPost == "Details")
                    return RedirectToPage("./Details", new { id = maintenanceDB.Id, isWizard = ManagementPlanId.HasValue, managementId = maintenanceDB.ManagementId, focusPlanId = FocusPlanId });

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo actualizar el mantenimiento {MaintenanceId}.", Input.Id);
                TempData.Error(NotificationHelper.Maintenances.SaveError("Intente nuevamente o contacte al administrador."));
                var maintenance = await _context.Maintenances.Include(m => m.EquipmentUnit).ThenInclude(eu => eu!.Laboratory).FirstOrDefaultAsync(m => m.Id == Input.Id);
                int facultyId = maintenance?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = maintenance?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);
                return Page();
            }
        }

        private void CargarListas(int facultyId = 0, int laboratoryId = 0, int equipmentUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name), "Id", "Name", facultyId);
            
            if (facultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories.Where(l => l.FacultyId == facultyId).OrderBy(l => l.Name), "Id", "Name", laboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            var equipos = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .AsEnumerable()
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber}) - {(u.Equipment.Brand ?? "S/M")} [S/N: {u.SerialNumber ?? "N/A"}] - [{u.Equipment.Category}]"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName");

            var tecnicos = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .OrderBy(p => p.Id)
                .AsEnumerable()
                .Select(p => new { Id = p.Id, FullName = p.FullName })
                .ToList();

            ViewData["TechnicianId"] = new SelectList(tecnicos, "Id", "FullName");

            ViewData["MaintenanceType"] = EnumHelper.GetStatusSelectList<MaintenanceType>();

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

        // ─── DRAFT HANDLER ────────────────────────────────────────────────
        public async Task<IActionResult> OnPostDraftAsync()
        {
            Input.Description = Input.Description?.Clean() ?? string.Empty;
            Input.Observations = Input.Observations?.Clean();
            Input.Recommendations = Input.Recommendations?.Clean();
            Input.CostDetails = (Input.CostDetails ?? new()).Where(d => !string.IsNullOrWhiteSpace(d.Concept)).ToList();
            Input.Tasks = (Input.Tasks ?? new()).Where(t => !string.IsNullOrWhiteSpace(t.Description)).ToList();
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            var maintenanceDB = await _context.Maintenances
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .Include(m => m.EquipmentUnit)
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (maintenanceDB == null) return NotFound();

            // Guardar todo sin validaciones estrictas
            maintenanceDB.MaintenanceType = Input.MaintenanceType;
            maintenanceDB.TechnicianId = Input.TechnicianId;
            maintenanceDB.ScheduledDate = Input.ScheduledDate;
            maintenanceDB.StartDate = Input.StartDate;
            maintenanceDB.EndDate = Input.EndDate;
            maintenanceDB.Description = Input.Description;
            maintenanceDB.SatisfactionLevel = Input.SatisfactionLevel;
            maintenanceDB.Recommendations = Input.Recommendations;
            maintenanceDB.Observations = Input.Observations;
            maintenanceDB.ActualCost = Input.CostDetails.Sum(d => d.Quantity * d.UnitPrice);
            maintenanceDB.Status = MaintenanceStatus.InProgress;
            maintenanceDB.CompletionPercentage = Input.CompletionPercentage;
            maintenanceDB.LastModifiedDate = DateTime.UtcNow;

            UpdateLegacySteps(maintenanceDB, Input.Tasks);
            SyncTasks(maintenanceDB, Input.Tasks);

            // Sincronizar CostDetails (misma lógica que OnPostAsync)
            var inputDetailIds = Input.CostDetails.Select(d => d.Id).ToList();
            var detailsToRemove = maintenanceDB.CostDetails.Where(d => !inputDetailIds.Contains(d.Id)).ToList();
            foreach (var detail in detailsToRemove)
            {
                detail.MaintenanceId = null;
                detail.Maintenance = null;
                detail.LastModifiedDate = DateTime.UtcNow;
            }
            foreach (var detailForm in Input.CostDetails)
            {
                detailForm.MaintenanceId = maintenanceDB.Id;
                var existingDetail = maintenanceDB.CostDetails.FirstOrDefault(d => d.Id == detailForm.Id && d.Id != 0);
                if (existingDetail != null)
                {
                    _context.Entry(existingDetail).CurrentValues.SetValues(detailForm);
                }
                else
                {
                    maintenanceDB.CostDetails.Add(detailForm);
                }
            }

            // Marcar plan como borrador
            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                if (plan != null)
                {
                    plan.IsDraft = true;
                    plan.DraftPhase = WizardPhase.Maintenance;
                    plan.DraftSavedAt = DateTime.UtcNow;
                    plan.DraftSummary = $"Borrador L-8 guardado al {Input.CompletionPercentage}% de avance técnico.";
                    plan.LastModifiedDate = DateTime.UtcNow;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Borrador L-8 guardado al {Input.CompletionPercentage}%. Puede continuar más tarde.");

                if (ManagementPlanId.HasValue)
                {
                    var plan = await _context.ManagementPlans.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                    if (plan != null)
                        return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = maintenanceDB.EquipmentUnit?.LaboratoryId, ManagementId = plan.ManagementId, FocusPlanId });
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo guardar el borrador del mantenimiento {MaintenanceId}.", Input.Id);
                TempData.Error("No se pudo guardar el borrador. Intente nuevamente.");
                CargarListas(maintenanceDB.EquipmentUnit?.Laboratory?.FacultyId ?? 0, maintenanceDB.EquipmentUnit?.LaboratoryId ?? 0, Input.EquipmentUnitId);
                return Page();
            }
        }

        // ─── HELPER METHODS ───────────────────────────────────────────────
        private static List<MaintenanceTask> GetDefaultTasks()
        {
            return new List<MaintenanceTask>
            {
                new MaintenanceTask { Description = "Limpieza y Desinfección de Componentes" },
                new MaintenanceTask { Description = "Calibración y Ajuste de Sistema" },
                new MaintenanceTask { Description = "Pruebas de Esfuerzo y Carga Operativa" },
                new MaintenanceTask { Description = "Revisión Final de Seguridad y Cierre" }
            };
        }

        private static int CalculateCompletionPercentage(List<MaintenanceTask> tasks)
        {
            if (tasks == null || !tasks.Any()) return 0;
            var completed = tasks.Count(t => t.IsCompleted);
            return (int)Math.Round((double)completed / tasks.Count * 100);
        }

        private static void UpdateLegacySteps(Maintenance maintenance, List<MaintenanceTask> tasks)
        {
            if (tasks.Count >= 1) maintenance.Step1_Cleaning = tasks[0].IsCompleted;
            if (tasks.Count >= 2) maintenance.Step2_Calibration = tasks[1].IsCompleted;
            if (tasks.Count >= 3) maintenance.Step3_Testing = tasks[2].IsCompleted;
            if (tasks.Count >= 4) maintenance.Step4_FinalReview = tasks[3].IsCompleted;
        }

        private void SyncTasks(Maintenance maintenance, List<MaintenanceTask> inputTasks)
        {
            var existingTasks = maintenance.Tasks.Where(t => !t.IsDeleted).ToList();
            var inputIds = inputTasks.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();

            // Marcar como eliminadas las que ya no están en el input
            foreach (var existing in existingTasks)
            {
                if (!inputIds.Contains(existing.Id))
                {
                    existing.IsDeleted = true;
                }
            }

            // Actualizar o agregar tareas
            for (int i = 0; i < inputTasks.Count; i++)
            {
                var inputTask = inputTasks[i];
                if (inputTask.Id > 0)
                {
                    var existing = maintenance.Tasks.FirstOrDefault(t => t.Id == inputTask.Id);
                    if (existing != null)
                    {
                        existing.Description = inputTask.Description;
                        existing.IsCompleted = inputTask.IsCompleted;
                    }
                }
                else
                {
                    maintenance.Tasks.Add(new MaintenanceTask
                    {
                        Description = inputTask.Description,
                        IsCompleted = inputTask.IsCompleted
                    });
                }
            }
        }
    }
}
