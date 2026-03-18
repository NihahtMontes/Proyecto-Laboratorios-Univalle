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
            public RequestType Type { get; set; } // Read-only for display logic

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Descripción / Justificación")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; }

            [Display(Name = "Estado de la Solicitud")]
            public RequestStatus Status { get; set; }

            [Display(Name = "Observaciones Adicionales")]
            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres")]
            public string? Observations { get; set; }

            // Technical Fields
            [Display(Name = "Tiempo Estimado de Reparación")]
            [StringLength(100)]
            public string? EstimatedRepairTime { get; set; }

            // Purchasing Fields
            [Display(Name = "Código de Inversión")]
            [StringLength(50)]
            public string? InvestmentCode { get; set; }

            public List<CostItemInput> Items { get; set; } = new();

            // Admin Fields
            [Display(Name = "Motivo de Rechazo")]
            public string? RejectionReason { get; set; }
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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.RequestedBy)
                .Include(r => r.ModifiedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Request == null) return NotFound();

            Input = new InputModel
            {
                Id = Request.Id,
                Type = Request.Type,
                Description = Request.Description,
                Priority = Request.Priority,
                Status = Request.Status,
                Observations = Request.Observations,
                EstimatedRepairTime = Request.EstimatedRepairTime,
                InvestmentCode = Request.InvestmentCode,
                RejectionReason = Request.RejectionReason,
                Items = Request.CostDetails.Select(c => new CostItemInput
                {
                    Concept = c.Concept,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice,
                    UnitOfMeasure = c.UnitOfMeasure,
                    Provider = c.Provider,
                    Category = c.Category
                }).ToList()
            };

            await LoadLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Re-load request for validation context (and type check)
            var requestToUpdate = await _context.Requests
                .Include(r => r.CostDetails)
                .Include(r => r.Equipment) // Needed for TempData message potentially
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (requestToUpdate == null) return NotFound();

            // Manual Validation for Purchasing items
            if (requestToUpdate.Type == RequestType.Purchasing)
            {
                if (string.IsNullOrWhiteSpace(Input.InvestmentCode))
                    ModelState.AddModelError("Input.InvestmentCode", "El código de inversión es obligatorio.");
                
                if (Input.Items == null || !Input.Items.Any())
                    ModelState.AddModelError("Input.Items", "Debe existir al menos un ítem.");
            }

            if (!ModelState.IsValid)
            {
                // Restore display properties
                Request = requestToUpdate; 
                await LoadLists();
                return Page();
            }

            var isUserAdmin = User.IsInRole("Administrator") || User.IsInRole("SuperAdmin");
            var currentUser = await _userManager.GetUserAsync(User);

            // 1. Update Common Fields
            requestToUpdate.Description = Input.Description.Clean();
            requestToUpdate.Priority = Input.Priority;
            requestToUpdate.Observations = Input.Observations?.Clean();
            requestToUpdate.LastModifiedDate = DateTime.UtcNow;
            requestToUpdate.ModifiedById = currentUser?.Id;

            // 2. Update Type-Specific Fields
            if (requestToUpdate.Type == RequestType.Technical)
            {
                requestToUpdate.EstimatedRepairTime = Input.EstimatedRepairTime?.Clean();
            }
            else // Purchasing
            {
                requestToUpdate.InvestmentCode = Input.InvestmentCode?.Clean();
                
                // Update Items: Strategy -> Remove all and re-add (Simple & Clean for this scale)
                _context.CostDetails.RemoveRange(requestToUpdate.CostDetails);
                
                if (Input.Items != null)
                {
                    foreach (var item in Input.Items)
                    {
                        requestToUpdate.CostDetails.Add(new CostDetail
                        {
                            RequestId = requestToUpdate.Id,
                            Concept = item.Concept.Clean(),
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            UnitOfMeasure = item.UnitOfMeasure?.Clean(),
                            Provider = item.Provider?.Clean(),
                            Category = item.Category,
                            CreatedDate = DateTime.UtcNow
                        });
                    }
                }
            }

            // 3. Admin Logic (Status Changes)
            if (isUserAdmin)
            {
                if (requestToUpdate.Status == RequestStatus.Pending && Input.Status != RequestStatus.Pending)
                {
                    requestToUpdate.ApprovalDate = DateTime.UtcNow;
                    requestToUpdate.ApprovedById = currentUser?.Id;
                }
                
                requestToUpdate.Status = Input.Status;
                requestToUpdate.RejectionReason = Input.RejectionReason?.Clean();
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Solicitud #{requestToUpdate.Id} actualizada correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(requestToUpdate.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            var evaluators = await _context.Users
                .Where(u => (u.Role == UserRole.Administrador || u.Role == UserRole.SuperAdmin) && u.Status == GeneralStatus.Activo)
                .OrderBy(u => u.FirstName)
                .Select(u => new { Id = u.Id, FullName = u.FullName })
                .ToListAsync();

            ViewData["ApprovedById"] = new SelectList(evaluators, "Id", "FullName");
        }

        private bool RequestExists(int id)
        {
            return _context.Requests.Any(e => e.Id == id);
        }
    }
}
