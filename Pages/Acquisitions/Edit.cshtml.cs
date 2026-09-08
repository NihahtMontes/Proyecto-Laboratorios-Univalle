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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public new Request Request { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Justificación del Pedido")]
            [StringLength(1000)]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; }

            [Display(Name = "Estado")]
            public RequestStatus Status { get; set; }

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500)]
            public string? Observations { get; set; }

            [Required(ErrorMessage = "El código de inversión es obligatorio")]
            [Display(Name = "Código de Inversión")]
            [StringLength(50)]
            public string? InvestmentCode { get; set; }

            [Display(Name = "Centro de Costos")]
            [StringLength(100)]
            public string? CostCenter { get; set; }

            public List<CostItemInput>? Items { get; set; } = new();
        }

        public class CostItemInput
        {
            public string Concept { get; set; } = string.Empty;
            public decimal Quantity { get; set; } = 1;
            public decimal UnitPrice { get; set; }
            public string? UnitOfMeasure { get; set; } = "Unidad";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Request = (await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id))!;

            if (Request == null || Request.Type != RequestType.Purchasing) return NotFound();

            int facultyId = Request.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
            int labId = Request.EquipmentUnit?.LaboratoryId ?? 0;
            int unitId = Request.EquipmentUnitId ?? 0;

            Input = new InputModel
            {
                Id = Request.Id,
                FacultyId = facultyId,
                LaboratoryId = labId,
                EquipmentUnitId = unitId,
                Description = Request.Description,
                Priority = Request.Priority,
                Status = Request.Status,
                Observations = Request.Observations,
                InvestmentCode = Request.InvestmentCode,
                CostCenter = Request.CostCenter,
                Items = Request.CostDetails.Select(c => new CostItemInput
                {
                    Concept = c.Concept,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice,
                    UnitOfMeasure = c.UnitOfMeasure
                }).ToList()
            };

            var plan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.AcquisitionRequestId == id);
            if (plan != null)
            {
                ViewData["IsWizard"] = true;
                ViewData["ManagementPlanId"] = plan.Id;
                ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
            }

            CargarListas(facultyId, labId, unitId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var request = await _context.Requests
                    .Include(r => r.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .FirstOrDefaultAsync(m => m.Id == Input.Id);
                    
                int facultyId = request?.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
                int labId = request?.EquipmentUnit?.LaboratoryId ?? 0;
                CargarListas(facultyId, labId, request?.EquipmentUnitId ?? 0);

                var plan = await _context.ManagementPlans.FirstOrDefaultAsync(p => p.AcquisitionRequestId == Input.Id);
                if (plan != null)
                {
                    ViewData["IsWizard"] = true;
                    ViewData["ManagementPlanId"] = plan.Id;
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
                    if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
                }
                
                return Page();
            }

            var requestToUpdate = await _context.Requests
                .Include(r => r.CostDetails)
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (requestToUpdate == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            requestToUpdate.Description = Input.Description;
            requestToUpdate.Priority = Input.Priority;
            requestToUpdate.Status = Input.Status;
            requestToUpdate.Observations = Input.Observations;
            requestToUpdate.InvestmentCode = Input.InvestmentCode;
            requestToUpdate.CostCenter = Input.CostCenter;
            requestToUpdate.LastModifiedDate = DateTime.UtcNow;
            requestToUpdate.ModifiedById = currentUser?.Id;

            // Preserve cost history: detach old rows instead of deleting them physically.
            foreach (var detail in requestToUpdate.CostDetails.ToList())
            {
                detail.RequestId = null;
                detail.Request = null;
                detail.LastModifiedDate = DateTime.UtcNow;
            }

            if (Input.Items != null)
            {
                foreach (var item in Input.Items)
                {
                    requestToUpdate.CostDetails.Add(new CostDetail
                    {
                        RequestId = requestToUpdate.Id,
                        Concept = item.Concept,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        UnitOfMeasure = item.UnitOfMeasure,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData.Success("Solicitud actualizada con éxito.");
            return RedirectToPage("./Index");
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
