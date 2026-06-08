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
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public new Request Request { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FocusPlanId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return RedirectToError("Sin dato", "No se recibio el identificador de la solicitud L-7.");
            }

            try
            {
                var request = await _context.Requests
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Include(r => r.ApprovedBy)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.Equipment)
                    .Include(r => r.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment)
                    .Include(r => r.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .Include(r => r.Laboratory)
                    .Include(r => r.Management)
                    .Include(r => r.ModifiedBy)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.CostDetails)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (request == null)
                {
                    return RedirectToError(id.Value.ToString(), "No se encontro la solicitud L-7 solicitada.");
                }

                Request = request;

                var currentMgmt = ManagementId.HasValue
                    ? await _context.Managements.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                    : request.Management;

                var isCorrective = currentMgmt?.Type == ManagementType.Corrective;
                ViewData["IsCorrective"] = isCorrective;
                ViewData["IsWizard"] = IsWizard;
                ViewData["ManagementId"] = currentMgmt?.Id ?? request.ManagementId;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir detalle L-7 para RequestId {RequestId}", id);
                return RedirectToError(id.Value.ToString(), "Se produjo una excepcion al abrir la solicitud L-7.");
            }
        }

        private IActionResult RedirectToError(string entityId, string message)
        {
            var listUrl = Url.Page("/Requests/Index") ?? "/Requests";
            var returnUrl = ResolveSafeReturnUrl(listUrl);

            return RedirectToPage("/Error", new
            {
                module = "Solicitud L-7",
                entityId,
                message,
                returnUrl,
                listUrl
            });
        }

        private string ResolveSafeReturnUrl(string fallbackUrl)
        {
            var referer = HttpContext.Request.Headers.Referer.ToString();
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
