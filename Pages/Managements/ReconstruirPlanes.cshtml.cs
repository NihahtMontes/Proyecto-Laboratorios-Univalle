using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.ManagementPlans;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    /// <summary>
    /// Reconstrucción de planes L-48 (solo SuperAdmin): arma la lista de equipos de cada gestión a
    /// partir de las verificaciones, solicitudes, mantenimientos y salidas ya registradas. Muestra una
    /// vista previa por gestión y crea los planes faltantes en un lote trazable; no modifica ningún
    /// registro existente y se puede repetir sin duplicar.
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.RoleSuperAdmin)]
    public class ReconstruirPlanesModel : PageModel
    {
        private readonly IManagementPlanBackfillService _backfill;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReconstruirPlanesModel> _logger;

        public ReconstruirPlanesModel(IManagementPlanBackfillService backfill, ApplicationDbContext context,
            ILogger<ReconstruirPlanesModel> logger)
        {
            _backfill = backfill;
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        public PlanBackfillPreview Preview { get; private set; } = new();
        public List<ManagementOption> Managements { get; private set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadManagementsAsync();
            Preview = await _backfill.PreviewAsync(ManagementId);
            return Page();
        }

        public async Task<IActionResult> OnPostApplyAsync()
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

            PlanBackfillResult result;
            try
            {
                result = await _backfill.ApplyAsync(ManagementId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reconstruir planes de la gestión {ManagementId}", ManagementId);
                TempData.Error("No se pudo reconstruir los planes. Revise el registro e intente nuevamente.");
                return RedirectToPage(new { managementId = ManagementId });
            }

            if (!result.Ok)
            {
                TempData.Error(result.Message);
            }
            else if (result.CreatedPlans > 0)
            {
                TempData.Success(
                    $"Se crearon {result.CreatedPlans} planes en {result.AffectedManagements} gestiones (lote {result.ImportBatchCode}).");
            }
            else
            {
                TempData.Info(result.Message);
            }

            return RedirectToPage(new { managementId = ManagementId });
        }

        private async Task LoadManagementsAsync()
        {
            Managements = await _context.Managements
                .AsNoTracking()
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).ThenBy(m => m.Code)
                .Select(m => new ManagementOption(m.Id, m.Code, m.Type))
                .ToListAsync();
        }

        public sealed record ManagementOption(int Id, string Code, ManagementType Type);

        /// <summary>Etiqueta corta en español del estado del equipo dentro del wizard.</summary>
        public string StateLabel(WizardEquipmentState state) => state switch
        {
            WizardEquipmentState.PendingVerification => "Pendientes de L-6",
            WizardEquipmentState.VerifiedGood => "Sanos (L-6 sin fallas)",
            WizardEquipmentState.AwaitingRequest => "Requieren L-7",
            WizardEquipmentState.AwaitingMaintenance => "Esperan L-8",
            WizardEquipmentState.InMaintenance => "En L-8",
            WizardEquipmentState.AwaitingDeparture => "Requieren L-3",
            WizardEquipmentState.AwaitingKardex => "Requieren Kardex",
            WizardEquipmentState.AwaitingDisbursement => "Requieren L-12",
            WizardEquipmentState.Completed => "Completados",
            _ => state.ToString()
        };

        /// <summary>Color de badge de Bootstrap 4 para el estado.</summary>
        public string StateBadge(WizardEquipmentState state) => state switch
        {
            WizardEquipmentState.VerifiedGood => "success",
            WizardEquipmentState.Completed => "success",
            WizardEquipmentState.AwaitingRequest => "warning",
            WizardEquipmentState.PendingVerification => "secondary",
            WizardEquipmentState.InMaintenance => "info",
            WizardEquipmentState.AwaitingMaintenance => "info",
            WizardEquipmentState.AwaitingDeparture => "primary",
            WizardEquipmentState.AwaitingKardex => "warning",
            WizardEquipmentState.AwaitingDisbursement => "dark",
            _ => "secondary"
        };

        /// <summary>Nombre visible del tipo de gestión.</summary>
        public string TypeLabel(ManagementType type) => type switch
        {
            ManagementType.Preventive => "Preventiva",
            ManagementType.Corrective => "Correctiva",
            _ => "Por confirmar"
        };
    }
}
