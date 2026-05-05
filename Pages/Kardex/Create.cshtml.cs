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
using System.Security.Claims;

namespace Proyecto_Laboratorios_Univalle.Pages.Kardex
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "El equipo es obligatorio")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "El estado del equipo es obligatorio")]
            [Display(Name = "Estado Final")]
            public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;

            [Required(ErrorMessage = "La fecha es obligatoria")]
            [Display(Name = "Fecha de Registro")]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "El motivo o justificación es obligatorio")]
            [Display(Name = "Observaciones (Motivo)")]
            public string Reason { get; set; } = "Retorno de Mantenimiento/Salida e ingreso a Kardex.";
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

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
                }
            }

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                if (plan != null)
                {
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    ViewData["ManagementId"] = plan.ManagementId;
                    if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                    if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
                }
            }

            await LoadLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            try
            {
                var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
                if (unit == null) return NotFound();

                // Cerrar historial previo si existe
                var lastHistory = await _context.EquipmentStateHistories
                    .Where(h => h.EquipmentUnitId == unit.Id && h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .AsTracking()
                    .FirstOrDefaultAsync();

                if (lastHistory != null)
                {
                    lastHistory.EndDate = Input.StartDate;
                    _context.EquipmentStateHistories.Update(lastHistory);
                }

                // Crear nuevo historial
                var newHistory = new EquipmentStateHistory
                {
                    EquipmentUnitId = unit.Id,
                    Status = Input.Status,
                    StartDate = Input.StartDate,
                    Reason = Input.Reason.Trim(),
                    CreatedDate = DateTime.UtcNow
                };
                
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out var uid))
                {
                    newHistory.CreatedById = uid;
                }

                _context.EquipmentStateHistories.Add(newHistory);

                // Actualizar equipo
                unit.CurrentStatus = Input.Status;
                _context.EquipmentUnits.Update(unit);

                // Avanzar Wizard si corresponde
                if (ManagementPlanId.HasValue)
                {
                    var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                    if (plan != null)
                    {
                        plan.CurrentPhase = WizardPhase.Disbursement;
                        plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
                        _context.ManagementPlans.Update(plan);

                        await _context.SaveChangesAsync();

                        if (isWizard)
                        {
                            return RedirectToPage("/Index", new { ShowWizard = true, Step = 6, SelectedLabId = Input.LaboratoryId, ManagementId = ManagementPlanId });
                        }
                    }
                }
                else
                {
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Kardex actualizado correctamente.";
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 5 });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar Kardex: {ex.Message}";
                await LoadLists();
                return Page();
            }
        }

        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .Select(u => new { id = u.Id, name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                .ToListAsync();
            return new JsonResult(units);
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name).ToListAsync(), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (Input.LaboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}
