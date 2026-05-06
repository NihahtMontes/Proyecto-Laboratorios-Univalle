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

        public class InputModel
        {
            public int Id { get; set; }

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
                .Where(p => p.EquipmentUnitId == history.EquipmentUnitId)
                .OrderByDescending(p => p.CreatedDate)
                .FirstOrDefaultAsync();

            if (plan != null && (int)plan.CurrentPhase >= 5)
            {
                ViewData["IsWizard"] = true;
                ViewData["ManagementPlanId"] = plan.Id;
                ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                
                if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                else if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
            }

            CargarListas(facultyId, labId, history.EquipmentUnitId ?? 0);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                    .Where(p => p.EquipmentUnitId == Input.EquipmentUnitId)
                    .OrderByDescending(p => p.CreatedDate)
                    .FirstOrDefaultAsync();
                if (plan != null && (int)plan.CurrentPhase >= 5)
                {
                    ViewData["IsWizard"] = true;
                    ViewData["ManagementPlanId"] = plan.Id;
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    
                    if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                    else if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
                }

                return Page();
            }

            var historyDB = await _context.EquipmentStateHistories.FindAsync(Input.Id);
            if (historyDB == null) return NotFound();

            historyDB.Status = Input.Status;
            historyDB.StartDate = Input.StartDate;
            historyDB.EndDate = Input.EndDate;
            historyDB.Reason = Input.Reason?.Trim();

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

                TempData["Success"] = "Registro de Kardex actualizado correctamente.";
                return RedirectToPage("/EquipmentStateHistories/Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
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
    }
}
