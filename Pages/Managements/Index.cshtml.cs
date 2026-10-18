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
        private readonly ICourseGroupPurgeService _courseGroupPurge;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            IManagementContextService managementContext,
            IManagementActivationService managementActivation,
            ICourseGroupPurgeService courseGroupPurge,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _managementContext = managementContext;
            _managementActivation = managementActivation;
            _courseGroupPurge = courseGroupPurge;
            _logger = logger;
        }

        public PaginatedList<Management> ManagementList { get; set; } = new(new List<Management>(), 0, 1, 20);

        /// <summary>
        /// Contadores de datos históricos importados (Excel del cliente) para las
        /// gestiones de la página actual que no tienen registros del wizard (ManagementPlans).
        /// Clave: Management.Id.
        /// </summary>
        public Dictionary<int, HistoricalCounts> HistoricalCountsByManagement { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Type { get; set; }

        /// <summary>True cuando la pestaña activa es "Históricas (Por confirmar)": solo lectura.</summary>
        public bool IsHistorical => string.Equals(Type, ManagementType.PorConfirmar.ToString(), StringComparison.OrdinalIgnoreCase);

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

                // Contadores históricos importados (L-48/L-8/L-6/L-7) en 4 consultas
                // agrupadas (sin consultas por fila), solo para las gestiones visibles
                // de esta página que no tienen registros del wizard. Se respetan los
                // query filters globales (cancelados/anulados quedan excluidos).
                HistoricalCountsByManagement = new Dictionary<int, HistoricalCounts>();
                var historicalPageIds = ManagementList
                    .Where(m => m.ManagementPlans.Count == 0 && m.Type == ManagementType.PorConfirmar)
                    .Select(m => m.Id)
                    .ToList();

                if (historicalPageIds.Count > 0)
                {
                    var l48Counts = await _context.MaintenancePlans
                        .Where(p => p.ManagementId != null && historicalPageIds.Contains(p.ManagementId.Value))
                        .GroupBy(p => p.ManagementId!.Value)
                        .Select(g => new { ManagementId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ManagementId, x => x.Count);

                    var l8Counts = await _context.Maintenances
                        .Where(m => historicalPageIds.Contains(m.ManagementId))
                        .GroupBy(m => m.ManagementId)
                        .Select(g => new { ManagementId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ManagementId, x => x.Count);

                    var l6Counts = await _context.Verifications
                        .Where(v => historicalPageIds.Contains(v.ManagementId))
                        .GroupBy(v => v.ManagementId)
                        .Select(g => new { ManagementId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ManagementId, x => x.Count);

                    var l7Counts = await _context.Requests
                        .Where(r => historicalPageIds.Contains(r.ManagementId))
                        .GroupBy(r => r.ManagementId)
                        .Select(g => new { ManagementId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ManagementId, x => x.Count);

                    foreach (var managementId in historicalPageIds)
                    {
                        HistoricalCountsByManagement[managementId] = new HistoricalCounts
                        {
                            L48Plans = l48Counts.GetValueOrDefault(managementId),
                            L8Maintenances = l8Counts.GetValueOrDefault(managementId),
                            L6Verifications = l6Counts.GetValueOrDefault(managementId),
                            L7Requests = l7Counts.GetValueOrDefault(managementId)
                        };
                    }
                }
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

                // Gestiones históricas (Type == PorConfirmar) son de solo consulta: defensa del servidor
                // aunque la UI ya no expone el formulario.
                if (management.Type == ManagementType.PorConfirmar)
                {
                    TempData.Error("Las gestiones históricas importadas son de solo consulta.");
                    return RedirectToPage("./Index", new { Type = management.Type.ToString() });
                }

                type = management.Type.ToString();
                management.Status = ManagementStatus.Completed;
                management.ActualClosedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Los grupos de la gestión son datos de trabajo desechables: se purgan (borrado físico
                // aprobado por el usuario) después de persistir el cambio de estado, porque la purga
                // limpia el ChangeTracker.
                var purgedGroups = await _courseGroupPurge.PurgeManagementAsync(management.Id);
                _managementContext.InvalidateCache();

                var purgedMessage = purgedGroups > 0
                    ? $" Se eliminaron {purgedGroups} grupo(s) de la gestión."
                    : string.Empty;
                TempData.Success($"La gestion administrativa ha sido cerrada correctamente.{purgedMessage}");
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

                // Gestiones históricas (Type == PorConfirmar) son de solo consulta: defensa del servidor
                // aunque la UI ya no expone el formulario.
                if (management.Type == ManagementType.PorConfirmar)
                {
                    TempData.Error("Las gestiones históricas importadas son de solo consulta.");
                    return RedirectToPage("./Index", new { Type = management.Type.ToString() });
                }

                type = management.Type.ToString();
                var activationResult = await _managementActivation.ActivateAsync(management);
                await _context.SaveChangesAsync();

                // Las gestiones que la activación cerró (Completed) pierden sus grupos de trabajo
                // (borrado físico aprobado): se purgan después de persistir el cambio de estado.
                var purgedGroups = 0;
                foreach (var closedId in activationResult.ClosedManagementIds)
                {
                    purgedGroups += await _courseGroupPurge.PurgeManagementAsync(closedId);
                }
                _managementContext.InvalidateCache();

                var closedMessage = activationResult.ClosedCount > 0
                    ? $" Se cerro {activationResult.ClosedCount} gestion activa anterior del mismo tipo."
                    : string.Empty;
                var purgedMessage = purgedGroups > 0
                    ? $" Se eliminaron {purgedGroups} grupo(s) de la gestión."
                    : string.Empty;

                TempData.Success($"La gestion {management.Code} ahora es la gestion activa correcta.{closedMessage}{purgedMessage}");
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

                // Gestiones históricas (Type == PorConfirmar) son de solo consulta: defensa del servidor
                // aunque la UI ya no expone el formulario.
                if (management.Type == ManagementType.PorConfirmar)
                {
                    TempData.Error("Las gestiones históricas importadas son de solo consulta.");
                    return RedirectToPage("./Index", new { Type = management.Type.ToString() });
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

                // Gestiones históricas (Type == PorConfirmar) son de solo consulta: defensa del servidor
                // aunque la UI ya no expone el formulario.
                if (management.Type == ManagementType.PorConfirmar)
                {
                    TempData.Error("Las gestiones históricas importadas son de solo consulta.");
                    return RedirectToPage("./Index", new { Type = management.Type.ToString() });
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

    /// <summary>
    /// Contadores de datos históricos importados desde el Excel del cliente
    /// para una gestión (L-48 planes, L-8 mantenimientos, L-6 verificaciones, L-7 solicitudes).
    /// </summary>
    public sealed class HistoricalCounts
    {
        public int L48Plans { get; set; }
        public int L8Maintenances { get; set; }
        public int L6Verifications { get; set; }
        public int L7Requests { get; set; }

        public bool HasAny => L48Plans > 0 || L8Maintenances > 0 || L6Verifications > 0 || L7Requests > 0;
    }
}
