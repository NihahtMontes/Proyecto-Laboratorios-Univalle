using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementContext;
        private readonly IManagementActivationService _managementActivation;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            IManagementContextService managementContext,
            IManagementActivationService managementActivation,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _managementContext = managementContext;
            _managementActivation = managementActivation;
            _logger = logger;
        }

        public PaginatedList<Management> ManagementList { get; set; } = new(new List<Management>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pageIndex)
        {
            try
            {
                var query = _context.Managements
                    .Include(m => m.ManagementPlans)
                    .Include(m => m.Faculty)
                    .Where(m => m.Status != ManagementStatus.Deleted);

                if (string.IsNullOrEmpty(Type))
                {
                    Type = ManagementType.Preventive.ToString();
                }

                if (Enum.TryParse<ManagementType>(Type, out var typeEnum))
                {
                    query = query.Where(m => m.Type == typeEnum);
                }

                ManagementList = await PaginatedList<Management>.CreateAsync(
                    query.OrderByDescending(m => m.CreatedDate),
                    pageIndex ?? 1,
                    20);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar gestiones de mantenimiento.");
                ManagementList = new PaginatedList<Management>(new List<Management>(), 0, 1, 20);
                TempData.Error("No se pudieron cargar las gestiones. Revise el detalle tecnico en logs.");
            }

            return Page();
        }

        public async Task<IActionResult> OnGetCurrentCorrectiveAsync()
        {
            var correctiveId = await _context.Managements
                .AsNoTracking()
                .Where(m => m.Type == ManagementType.Corrective && m.Status == ManagementStatus.Active)
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Semester)
                .ThenByDescending(m => m.CreatedDate)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();

            if (correctiveId == null)
            {
                TempData.Warning("No hay una gestion correctiva activa. Cree o active una gestion correctiva antes de reportar fallas.");
                return RedirectToPage("./Index", new { Type = ManagementType.Corrective.ToString() });
            }

            return RedirectToPage("./Details", new { id = correctiveId.Value, ActiveTab = "dashboard" });
        }

        public async Task<IActionResult> OnPostCloseManagementAsync(int id)
        {
            var type = ManagementType.Preventive.ToString();

            try
            {
                var management = await _context.Managements
                    .AsTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (management == null)
                {
                    return RedirectToManagementError(id, "No se encontro la gestion solicitada para cerrar.", type);
                }

                type = management.Type.ToString();
                management.Status = ManagementStatus.Completed;
                management.ActualClosedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();

                TempData.Success("La gestion administrativa ha sido cerrada correctamente.");
                return RedirectToPage("./Index", new { Type = type });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar gestion {ManagementId}", id);
                return RedirectToManagementError(id, "No se pudo cerrar la gestión. Intente nuevamente.", type);
            }
        }

        public async Task<IActionResult> OnPostActivateManagementAsync(int id)
        {
            var type = ManagementType.Preventive.ToString();

            try
            {
                var management = await _context.Managements
                    .AsTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (management == null || management.Status == ManagementStatus.Deleted)
                {
                    return RedirectToManagementError(id, "No se encontro la gestion solicitada para activar.", type);
                }

                type = management.Type.ToString();
                var activationResult = await _managementActivation.ActivateAsync(management);
                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();

                var closedMessage = activationResult.ClosedCount > 0
                    ? $" Se cerro {activationResult.ClosedCount} gestion activa anterior del mismo tipo."
                    : string.Empty;

                TempData.Success($"La gestion {management.Code} ahora es la gestion activa correcta.{closedMessage}");
                return RedirectToPage("./Details", new { id = management.Id, ActiveTab = "dashboard" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar gestion {ManagementId}", id);
                return RedirectToManagementError(id, "No se pudo activar la gestión. Intente nuevamente.", type);
            }
        }

        public async Task<IActionResult> OnPostDeleteLogicalAsync(int id)
        {
            var type = ManagementType.Preventive.ToString();

            try
            {
                var management = await _context.Managements
                    .AsTracking()
                    .Include(m => m.ManagementPlans)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (management == null)
                {
                    return RedirectToManagementError(id, "No se encontro la gestion solicitada para eliminar.", type);
                }

                type = management.Type.ToString();

                if (management.Status == ManagementStatus.Active && management.Type != ManagementType.Corrective)
                {
                    TempData.Error("No se puede eliminar una gestion preventiva activa. Cierrela o active otra gestion preventiva antes de darla de baja.");
                    return RedirectToPage("./Index", new { Type = type });
                }

                management.Status = ManagementStatus.Deleted;
                management.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();

                var planCount = management.ManagementPlans?.Count ?? 0;
                TempData.Success($"La gestion {management.Code} fue dada de baja logicamente. Su historial L-48 y {planCount} registro(s) asociados quedan preservados.");
                return RedirectToPage("./Index", new { Type = type });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar logicamente gestion {ManagementId}", id);
                return RedirectToManagementError(id, "No se pudo dar de baja la gestión. Intente nuevamente.", type);
            }
        }

        public async Task<IActionResult> OnPostSyncPlansAsync(int id)
        {
            var type = ManagementType.Preventive.ToString();

            try
            {
                var management = await _context.Managements
                    .AsTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (management == null)
                {
                    return RedirectToManagementError(id, "No se encontro la gestion solicitada para planificar activos.", type);
                }

                type = management.Type.ToString();

                if (management.Type == ManagementType.Corrective)
                {
                    TempData.Warning("Las gestiones correctivas no sincronizan equipos. Cada activo entra al flujo cuando se reporta una falla L-7.");
                    return RedirectToPage("./Index", new { Type = type });
                }

                if (management.Status != ManagementStatus.Active)
                {
                    TempData.Warning("Solo se pueden planificar activos en una gestion preventiva activa.");
                    return RedirectToPage("./Index", new { Type = type });
                }

                TempData.Info("Seleccione las categorias, subclasificaciones y unidades que entraran a esta ronda preventiva.");
                return RedirectToPage("./PlanAssets", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir planificacion desde sincronizar gestion {ManagementId}", id);
                return RedirectToManagementError(id, "No se pudo abrir la planificación de la gestión. Intente nuevamente.", type);
            }
        }

        private IActionResult RedirectToManagementError(int id, string message, string type)
        {
            return RedirectToPage("/Error", new
            {
                module = "Gestion L-48",
                entityId = id.ToString(),
                message,
                returnUrl = Url.Page("./Index", new { Type = type }),
                listUrl = Url.Page("./Index", new { Type = type })
            });
        }
    }
}
