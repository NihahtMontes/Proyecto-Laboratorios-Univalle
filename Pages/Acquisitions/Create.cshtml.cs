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

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadLists();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física (Activo)")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "La justificación es obligatoria")]
            [Display(Name = "Justificación del Pedido")]
            [StringLength(1000, ErrorMessage = "La justificación no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres")]
            public string? Observations { get; set; }

            [Required(ErrorMessage = "El código de inversión es obligatorio")]
            [Display(Name = "Código de Inversión")]
            [StringLength(50)]
            public string InvestmentCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "El centro de costos es obligatorio")]
            [Display(Name = "Centro de Costos")]
            [StringLength(100)]
            public string CostCenter { get; set; } = string.Empty;

            [Display(Name = "Referencia de Mantenimiento (Opcional)")]
            public int? MaintenanceId { get; set; }
        }

        // AJAX Handler: Get laboratories by faculty
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        // AJAX Handler: Get units by laboratory
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

        // AJAX Handler: Get pending maintenances for a specific unit
        public async Task<JsonResult> OnGetMaintenancesByUnitAsync(int unitId)
        {
            var maintenances = await _context.Maintenances
                .Where(m => m.EquipmentUnitId == unitId &&
                           (m.Status == MaintenanceStatus.Pending || m.Status == MaintenanceStatus.InProgress || m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.Completed))
                .Select(m => new {
                    id = m.Id,
                    displayName = $"Mnt #{m.Id} - {m.ScheduledDate:dd/MM/yyyy} - {m.Status}"
                })
                .OrderByDescending(m => m.id)
                .ToListAsync();

            return new JsonResult(maintenances);
        }

        public async Task<JsonResult> OnGetMaintenanceCostsAsync(int maintenanceId)
        {
            var costs = await _context.CostDetails
                .Where(d => d.MaintenanceId == maintenanceId)
                .Select(d => new {
                    id = d.Id,
                    concept = d.Concept,
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice,
                    unitOfMeasure = d.UnitOfMeasure,
                    categoryName = d.Category.ToString()
                })
                .ToListAsync();

            return new JsonResult(costs);
        }

        public async Task<JsonResult> OnGetNextInvestmentCodeAsync(int unitId)
        {
            var unit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == unitId);
            if (unit?.Equipment == null) return new JsonResult(new { code = "" });

            var count = await _context.Requests
                .Where(r => r.EquipmentUnitId == unitId)
                .CountAsync();

            string suggestedCode = $"{unit.Equipment!.Name.Replace(" ", "").ToUpper()}-{(count + 1):D3}";
            return new JsonResult(new { code = suggestedCode });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            if (unit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad seleccionada no es válida.");
                await LoadLists();
                return Page();
            }

            var request = new Request
            {
                Type = RequestType.Purchasing, // FIJAMOS EL TIPO AQUÍ
                LaboratoryId = Input.LaboratoryId,
                EquipmentId = unit.EquipmentId,
                EquipmentUnitId = unit.Id,
                Description = Input.Description.Clean()!,
                Observations = Input.Observations?.Clean(),
                InvestmentCode = Input.InvestmentCode.Clean(),
                CostCenter = Input.CostCenter.Clean(),
                Priority = RequestPriority.Medium, // Por defecto para compras
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            // Vincular ítems importados del mantenimiento si se seleccionó uno
            if (Input.MaintenanceId.HasValue)
            {
                var maintenanceCosts = await _context.CostDetails
                    .Where(d => d.MaintenanceId == Input.MaintenanceId.Value)
                    .ToListAsync();

                foreach (var cost in maintenanceCosts)
                {
                    cost.RequestId = request.Id;
                    request.CostDetails.Add(cost);
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                request.CreatedById = currentUser.Id;
                request.RequestedById = currentUser.Id;
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            TempData.Success($"Solicitud de Adquisición para '{unit.InventoryNumber}' registrada exitosamente.");
            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name)
                .ToListAsync(), "Id", "Name");

            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}