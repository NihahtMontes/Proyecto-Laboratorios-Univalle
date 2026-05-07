using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementService;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager, IManagementContextService managementService)
        {
            _context = context;
            _userManager = userManager;
            _managementService = managementService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

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

            [Required(ErrorMessage = "La justificación es obligatoria")]
            [Display(Name = "Justificación del Requerimiento")]
            [StringLength(1000, ErrorMessage = "La justificación no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Especificaciones / Observaciones (Opcional)")]
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
                    var mgmt = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
                    ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
                    if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                    if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
                }
            }

            await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
            return Page();
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

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            if (!ModelState.IsValid)
            {
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            if (unit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad seleccionada no es válida.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var targetManagementId = (await _managementService.GetCurrentManagementAsync())?.Id;
            if (ManagementPlanId.HasValue)
            {
                targetManagementId = await _context.ManagementPlans
                    .Where(p => p.Id == ManagementPlanId.Value)
                    .Select(p => (int?)p.ManagementId)
                    .FirstOrDefaultAsync();
            }

            if (!targetManagementId.HasValue)
            {
                TempData.Error("No hay una gestiÃ³n activa para registrar la solicitud de adquisiciÃ³n.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var request = new Request
            {
                Type = RequestType.Purchasing,
                LaboratoryId = Input.LaboratoryId,
                EquipmentId = unit.EquipmentId,
                EquipmentUnitId = unit.Id,
                ManagementId = targetManagementId.Value,
                Description = Input.Description.Clean()!,
                Observations = Input.Observations?.Clean(),
                InvestmentCode = Input.InvestmentCode.Clean(),
                CostCenter = Input.CostCenter.Clean(),
                Priority = RequestPriority.Medium,
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

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

            if (currentUser != null)
            {
                var notification = new Notification
                {
                    UserId = currentUser.Id,
                    Title = "Adquisición Solicitada",
                    Message = $"Se registró correctamente tu solicitud de compra para la unidad {unit.InventoryNumber}.",
                    ActionUrl = $"/Requests/Details?id={request.Id}",
                    IconClass = "fas fa-shopping-cart text-success",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                if (plan != null)
                {
                    plan.AcquisitionRequestId = request.Id;
                    plan.CurrentPhase = WizardPhase.Disbursement; 
                    plan.CurrentState = WizardEquipmentState.Completed;
                    await _context.SaveChangesAsync();

                    if (isWizard)
                    {
                        return RedirectToPage("/Index", new { ShowWizard = true, Step = 7, SelectedLabId = Input.LaboratoryId, ManagementId = plan.ManagementId });
                    }
                }
            }

            TempData.Success($"Solicitud de Adquisición para '{unit.InventoryNumber}' registrada exitosamente.");
            return RedirectToPage("./Index");
        }

        private async Task LoadLists(int facultyId = 0, int laboratoryId = 0, int equipmentUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name).ToListAsync(), "Id", "Name", facultyId);

            if (facultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == facultyId).ToListAsync(), "Id", "Name", laboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (laboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == laboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", equipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}
