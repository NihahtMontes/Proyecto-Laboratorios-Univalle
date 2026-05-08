using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public new Request Request { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(r => r.Laboratory)
                .Include(r => r.ModifiedBy)
                .Include(r => r.RequestedBy)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            Request = request;

            var currentMgmt = ManagementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                : await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);

            var isCorrective = currentMgmt?.Type == ManagementType.Corrective;
            ViewData["IsCorrective"] = isCorrective;
            ViewData["IsWizard"] = IsWizard;
            ViewData["ManagementId"] = currentMgmt?.Id;

            return Page();
        }


    }
}