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

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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
            [Required(ErrorMessage = "El tipo de solicitud es obligatorio")]
            [Display(Name = "Tipo de Solicitud")]
            public RequestType Type { get; set; } = RequestType.Technical;

            // CAMPOS COMUNES
            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "El equipamiento es obligatorio")]
            [Display(Name = "Equipamiento")]
            public int EquipmentId { get; set; }

            [Required(ErrorMessage = "La descripción o justificación es obligatoria")]
            [Display(Name = "Descripción / Justificación")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres")]
            public string? Observations { get; set; }

            // CAMPOS TÉCNICOS
            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;

            [Display(Name = "Tiempo Estimado de Reparación")]
            [StringLength(100)]
            public string? EstimatedRepairTime { get; set; }

            // CAMPOS ADMINISTRATIVOS
            [Display(Name = "Código de Inversión")]
            [StringLength(50)]
            public string? InvestmentCode { get; set; }

            public List<CostItemInput> Items { get; set; } = new();
        }

        public class CostItemInput
        {
            [Required(ErrorMessage = "La descripción del ítem es obligatoria")]
            public string Concept { get; set; } = string.Empty;

            [Required]
            [Range(0.01, 99999)]
            public decimal Quantity { get; set; } = 1;

            [Required]
            [Range(0, 999999999)]
            public decimal UnitPrice { get; set; }

            public string? UnitOfMeasure { get; set; } = "Unidad";
            public string? Provider { get; set; }
            public CostCategory Category { get; set; } = CostCategory.SparePart;
        }

        // AJAX Handler: Get equipments or units by laboratory
        public async Task<JsonResult> OnGetEquipmentsByLabAsync(int laboratoryId, RequestType type)
        {
            if (type == RequestType.Technical)
            {
                // Find units physically located in this laboratory
                var rawUnits = await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == laboratoryId && 
                                u.CurrentStatus != EquipmentStatus.Deleted)
                    .Select(u => new { 
                        Id = u.Id, 
                        EquipmentName = u.Equipment != null ? u.Equipment.Name : "Desconocido",
                        InventoryNumber = u.InventoryNumber
                    })
                    .ToListAsync();

                var units = rawUnits
                    .Select(u => new { 
                        id = u.Id, 
                        name = $"{u.EquipmentName} (Inv: {u.InventoryNumber})" 
                    })
                    .OrderBy(x => x.name)
                    .ToList();

                return new JsonResult(units);
            }
            else
            {
                var equipments = await _context.Equipments
                    .Where(e => e.Units.Any(u => u.LaboratoryId == laboratoryId))
                    .Select(e => new { 
                        id = e.Id, 
                        name = e.Name 
                    })
                    .OrderBy(x => x.name)
                    .ToListAsync();
                
                return new JsonResult(equipments);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validación Condicional según Tipo
            if (Input.Type == RequestType.Purchasing)
            {
                if (string.IsNullOrWhiteSpace(Input.InvestmentCode))
                    ModelState.AddModelError("Input.InvestmentCode", "El código de inversión es obligatorio para solicitudes administrativas.");

                if (Input.Items == null || !Input.Items.Any())
                    ModelState.AddModelError("Input.Items", "Debe agregar al menos un ítem al presupuesto.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            var request = new Request
            {
                Type = Input.Type,
                InvestmentCode = Input.Type == RequestType.Purchasing ? Input.InvestmentCode?.Clean() : null,
                LaboratoryId = Input.LaboratoryId,
                Description = Input.Description.Clean(),
                Priority = Input.Priority,
                Observations = Input.Observations?.Clean(),
                EstimatedRepairTime = Input.Type == RequestType.Technical ? Input.EstimatedRepairTime?.Clean() : null,
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.Now
            };

            if (Input.Type == RequestType.Technical)
            {
                // For technical requests, EquipmentId is the UnitId from the selector
                var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentId);
                if (unit != null)
                {
                    request.EquipmentUnitId = unit.Id;
                    request.EquipmentId = unit.EquipmentId;
                }
            }
            else
            {
                request.EquipmentId = Input.EquipmentId;
            }

            // Mapeo manual de items
            if (Input.Type == RequestType.Purchasing && Input.Items != null)
            {
                foreach (var item in Input.Items)
                {
                    request.CostDetails.Add(new CostDetail
                    {
                        Concept = item.Concept.Clean(),
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        UnitOfMeasure = item.UnitOfMeasure.Clean(),
                        Provider = item.Provider?.Clean(),
                        Category = item.Category,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                request.CreatedById = currentUser.Id;
                request.RequestedById = currentUser.Id; // Always the current user
            }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Get equipment name for success message
            var equipment = await _context.Equipments.FindAsync(request.EquipmentId);
            TempData.Success($"Solicitud para '{(request.EquipmentUnitId.HasValue ? ("Unidad " + request.EquipmentUnitId) : (equipment?.Name ?? "el equipo seleccionado"))}' registrada exitosamente.");
            
            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            // Load laboratories
            var laboratories = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            
            ViewData["LaboratoryId"] = new SelectList(laboratories, "Id", "Name");

            // Equipments will be loaded via AJAX, but provide empty list initially
            ViewData["EquipmentId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}
