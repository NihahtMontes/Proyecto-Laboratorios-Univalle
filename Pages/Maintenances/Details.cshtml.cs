using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Maintenance Maintenance { get; set; } = default!;
        public int? ManagementPlanId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FocusPlanId { get; set; }

        public string FormatManagement(Management? management)
        {
            if (management == null) return "Gestión sin detalle";
            if (!string.IsNullOrWhiteSpace(management.Code)) return management.Code;
            return management.Type == Models.Enums.ManagementType.Corrective
                ? $"CORR-{management.Year}-{management.Semester}"
                : $"{management.Year}-{management.Semester}";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return RedirectToError("Sin dato", "No se recibio el identificador del mantenimiento L-8.");
            }

            try
            {
                var maintenance = await _context.Maintenances
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(m => m.Management)
                    .Include(m => m.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment)
                    .Include(m => m.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .Include(m => m.Technician)
                    .Include(m => m.Request)
                    .Include(m => m.CreatedBy)
                    .Include(m => m.ModifiedBy)
                    .Include(m => m.CostDetails)
                    .Include(m => m.Tasks)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (maintenance == null)
                {
                    return RedirectToError(id.Value.ToString(), "No se encontro el mantenimiento L-8 solicitado.");
                }

                Maintenance = maintenance;

                ManagementPlanId = await _context.ManagementPlans
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(p => p.MaintenanceId == Maintenance.Id)
                    .Select(p => (int?)p.Id)
                    .FirstOrDefaultAsync();

                ViewData["ManagementId"] = Maintenance.ManagementId;
                ViewData["ManagementType"] = Maintenance.Management?.Type.ToString();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir detalle L-8 para MaintenanceId {MaintenanceId}", id);
                return RedirectToError(id.Value.ToString(), "Se produjo una excepcion al abrir el mantenimiento L-8.");
            }
        }

        private IActionResult RedirectToError(string entityId, string message)
        {
            var listUrl = Url.Page("/Maintenances/Index") ?? "/Maintenances";
            var returnUrl = ResolveSafeReturnUrl(listUrl);

            return RedirectToPage("/Error", new
            {
                module = "Mantenimiento L-8",
                entityId,
                message,
                returnUrl,
                listUrl
            });
        }

        private string ResolveSafeReturnUrl(string fallbackUrl)
        {
            var referer = Request.Headers.Referer.ToString();
            if (Url.IsLocalUrl(referer)) return referer;

            if (Uri.TryCreate(referer, UriKind.Absolute, out var uri) &&
                string.Equals(uri.Host, HttpContext.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return uri.PathAndQuery;
            }

            return fallbackUrl;
        }
    }
}
