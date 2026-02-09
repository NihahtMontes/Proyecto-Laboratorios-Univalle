using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.Reporting;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IVerificationReportService _reportingService;

        public DetailsModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            IVerificationReportService reportingService)
        {
            _context = context;
            _reportingService = reportingService;
        }

        public Verification Verification { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var verification = await _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .Include(v => v.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null)
            {
                return NotFound();
            }
            else
            {
                Verification = verification;
            }
            return Page();
        }

        public async Task<IActionResult> OnGetDownloadPdfAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var verification = await _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .Include(v => v.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null)
            {
                return NotFound();
            }

            var pdfBytes = _reportingService.GenerateVerificationPdf(verification);
            return File(pdfBytes, "application/pdf", $"Verificacion_{verification.Id}.pdf");
        }
    }
}
