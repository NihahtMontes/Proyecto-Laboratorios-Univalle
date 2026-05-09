using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Kardex
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ReturnStep { get; set; } = 5;

        public class InputModel
        {
            public int Id { get; set; }

            public int? MaintenanceId { get; set; }

            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El estado es obligatorio")]
            [Display(Name = "Estado")]
            public EquipmentStatus Status { get; set; }

            [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
            [Display(Name = "Fecha de Novedad")]
            [DataType(DataType.DateTime)]
            public DateTime StartDate { get; set; }

            [Display(Name = "Fecha de Fin")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [StringLength(500)]
            [Display(Name = "Motivo del Cambio")]
            public string? Reason { get; set; }

            public int CompletionPercentage { get; set; }

            public List<MaintenanceTask> Tasks { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var history = await _context.EquipmentStateHistories
                .Include(h => h.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null) return NotFound();

            int facultyId = history.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
            int labId = history.EquipmentUnit?.LaboratoryId ?? 0;

            Input = new InputModel
            {
                Id = history.Id,
                FacultyId = facultyId,
                LaboratoryId = labId,
                EquipmentUnitId = history.EquipmentUnitId ?? 0,
                Status = history.Status,
                StartDate = history.StartDate,
                EndDate = history.EndDate,
                Reason = history.Reason
            };

            // Detect Wizard Plan (Since Kardex is not explicitly in ManagementPlan, we can search if there's a plan with this EquipmentUnitId and Phase >= 5)
            // Or we check the latest plan for this equipment.
            var plan = await _context.ManagementPlans
                .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                .Where(p => p.KardexHistoryId == history.Id || p.EquipmentUnitId == history.EquipmentUnitId)
                .OrderByDescending(p => p.KardexHistoryId == history.Id)
                .ThenByDescending(p => p.CreatedDate)
                .FirstOrDefaultAsync();

            if (plan != null && (int)plan.CurrentPhase >= 5)
            {
                ApplyWizardContext(plan, labId);
            }

            Input.MaintenanceId = plan?.MaintenanceId;
            Input.Tasks = plan?.Maintenance?.Tasks?.Where(t => !t.IsDeleted).OrderBy(t => t.Id).ToList() ?? new List<MaintenanceTask>();
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            CargarListas(facultyId, labId, history.EquipmentUnitId ?? 0);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.Tasks = NormalizeTasks(Input.Tasks);
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            if (!ModelState.IsValid)
            {
                var history = await _context.EquipmentStateHistories
                    .Include(h => h.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .FirstOrDefaultAsync(h => h.Id == Input.Id);

                int facultyId = history?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = history?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);

                // Re-detect wizard
                var plan = await _context.ManagementPlans
                    .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                    .Where(p => p.KardexHistoryId == Input.Id || p.EquipmentUnitId == Input.EquipmentUnitId)
                    .OrderByDescending(p => p.KardexHistoryId == Input.Id)
                    .ThenByDescending(p => p.CreatedDate)
                    .FirstOrDefaultAsync();
                if (plan != null && (int)plan.CurrentPhase >= 5)
                {
                    ApplyWizardContext(plan, labId);
                }

                return Page();
            }

            var historyDB = await _context.EquipmentStateHistories.FindAsync(Input.Id);
            if (historyDB == null) return NotFound();

            var wizardPlan = await _context.ManagementPlans
                .AsTracking()
                .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                .Where(p => p.KardexHistoryId == Input.Id || p.MaintenanceId == Input.MaintenanceId)
                .OrderByDescending(p => p.KardexHistoryId == Input.Id)
                .ThenByDescending(p => p.CreatedDate)
                .FirstOrDefaultAsync();

            historyDB.Status = Input.Status;
            historyDB.StartDate = Input.StartDate;
            historyDB.EndDate = Input.EndDate;
            historyDB.Reason = Input.Reason?.Trim();

            if (wizardPlan?.Maintenance != null)
            {
                SyncTasks(wizardPlan.Maintenance, Input.Tasks);
                wizardPlan.Maintenance.CompletionPercentage = Input.CompletionPercentage;
                UpdateLegacySteps(wizardPlan.Maintenance, Input.Tasks);
                wizardPlan.Maintenance.LastModifiedDate = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                
                // Also update EquipmentUnit if this is the latest history (EndDate is null)
                if (!historyDB.EndDate.HasValue)
                {
                    var unit = await _context.EquipmentUnits.FindAsync(historyDB.EquipmentUnitId);
                    if (unit != null)
                    {
                        unit.CurrentStatus = historyDB.Status;
                        _context.EquipmentUnits.Update(unit);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData.Success("Registro de Kardex actualizado correctamente.");
                if (IsWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = ReturnStep, SelectedLabId, ManagementId });
                }

                return RedirectToPage("/EquipmentStateHistories/Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al actualizar: {ex.Message}");
                var history = await _context.EquipmentStateHistories
                    .Include(h => h.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .FirstOrDefaultAsync(h => h.Id == Input.Id);

                int facultyId = history?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = history?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, Input.EquipmentUnitId);
                return Page();
            }
        }

        private static List<MaintenanceTask> NormalizeTasks(List<MaintenanceTask>? tasks)
        {
            return (tasks ?? new List<MaintenanceTask>())
                .Where(t => !t.IsDeleted && (t.Id > 0 || !string.IsNullOrWhiteSpace(t.Description)))
                .Select(t =>
                {
                    t.Description = t.Description?.Trim() ?? string.Empty;
                    return t;
                })
                .ToList();
        }

        private static int CalculateCompletionPercentage(List<MaintenanceTask> tasks)
        {
            if (tasks.Count == 0) return 0;
            return (int)Math.Round(tasks.Count(t => t.IsCompleted) * 100m / tasks.Count);
        }

        private static void SyncTasks(Maintenance maintenance, List<MaintenanceTask> inputTasks)
        {
            var inputIds = inputTasks.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
            foreach (var existingTask in maintenance.Tasks.Where(t => t.Id > 0 && !t.IsDeleted && !inputIds.Contains(t.Id)).ToList())
            {
                existingTask.IsDeleted = true;
                existingTask.IsCompleted = false;
            }

            foreach (var taskForm in inputTasks)
            {
                var existingTask = maintenance.Tasks.FirstOrDefault(t => t.Id == taskForm.Id && t.Id != 0);
                if (existingTask != null)
                {
                    existingTask.Description = taskForm.Description;
                    existingTask.IsCompleted = taskForm.IsCompleted;
                    existingTask.IsDeleted = false;
                }
                else if (!string.IsNullOrWhiteSpace(taskForm.Description))
                {
                    maintenance.Tasks.Add(new MaintenanceTask
                    {
                        Description = taskForm.Description,
                        IsCompleted = taskForm.IsCompleted,
                        IsDeleted = false
                    });
                }
            }
        }

        private static void UpdateLegacySteps(Maintenance maintenance, List<MaintenanceTask> tasks)
        {
            maintenance.Step1_Cleaning = tasks.ElementAtOrDefault(0)?.IsCompleted ?? false;
            maintenance.Step2_Calibration = tasks.ElementAtOrDefault(1)?.IsCompleted ?? false;
            maintenance.Step3_Testing = tasks.ElementAtOrDefault(2)?.IsCompleted ?? false;
            maintenance.Step4_FinalReview = tasks.ElementAtOrDefault(3)?.IsCompleted ?? false;
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
                    DisplayName = $"{u.Equipment!.Name} (Inv: {u.InventoryNumber})"
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(equipos, "Id", "DisplayName", equipmentUnitId);
        }

        private void ApplyWizardContext(ManagementPlan plan, int? labId)
        {
            IsWizard = true;
            ManagementId ??= plan.ManagementId;
            SelectedLabId ??= labId;
            ReturnStep = ReturnStep <= 0 ? 5 : ReturnStep;

            ViewData["IsWizard"] = true;
            ViewData["ManagementPlanId"] = plan.Id;
            ViewData["CurrentPhaseInt"] = 5;
            ViewData["ManagementId"] = ManagementId;
            ViewData["SelectedLabId"] = SelectedLabId;

            if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
            else if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
        }
    }
}
