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

            Request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id); // Equality check

            if (Request == null || Request.Type != RequestType.Purchasing) return NotFound();

            Input = new InputModel
            {
                Id = Request.Id,
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var requestToUpdate = await _context.Requests
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == Input.Id); // Equality check Corrected!

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

            // Simple update for cost items
            _context.CostDetails.RemoveRange(requestToUpdate.CostDetails);
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
            TempData["Success"] = "Solicitud actualizada con éxito.";
            return RedirectToPage("./Index");
        }
    }
}
