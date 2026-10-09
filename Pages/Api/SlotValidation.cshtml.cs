using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Api
{
    /// <summary>
    /// Validación JSON de un destino (reprogramación/reposición) para una planificación. Solo lectura: la UI consulta
    /// aquí para mostrar avisos, pero la confirmación vuelve a validar en el servidor con <see cref="ISlotValidationService"/>.
    /// GET /Api/SlotValidation?planningId=..&amp;date=yyyy-MM-dd&amp;startPeriodId=..&amp;laboratoryId=..&amp;teacherPersonId=..
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.RoleSuperAdmin)]
    public class SlotValidationModel : PageModel
    {
        private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd/MM/yyyy"];

        private readonly ISlotValidationService _validation;

        public SlotValidationModel(ISlotValidationService validation) => _validation = validation;

        public async Task<IActionResult> OnGetAsync(int planningId, string? date, int startPeriodId, int laboratoryId, int teacherPersonId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(date)
                || !DateTime.TryParseExact(date.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return BadRequest(new { message = "La fecha no es válida: use el formato aaaa-mm-dd." });
            }

            var result = await _validation.ValidateAsync(new SlotRequest(planningId, parsed.Date, startPeriodId, laboratoryId, teacherPersonId), ct);

            return new JsonResult(new
            {
                level = result.Level,
                hasBlocking = result.HasBlocking,
                hasWarnings = result.HasWarnings,
                issues = result.Issues.Select(i => new
                {
                    code = i.Code,
                    severity = SeverityText(i.Severity),
                    message = i.Message,
                    details = i.Details
                }),
                shortages = result.Shortages.Select(s => new
                {
                    articleId = s.ArticleId,
                    articleCode = s.ArticleCode,
                    articleName = s.ArticleName,
                    requested = s.Requested,
                    available = s.Available,
                    shortage = s.Shortage
                })
            });
        }

        private static string SeverityText(SlotSeverity severity) => severity switch
        {
            SlotSeverity.Block => "block",
            SlotSeverity.Warning => "warning",
            _ => "info"
        };
    }
}
