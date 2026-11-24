using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Contadores del menú lateral: solicitudes por revisar (revisores) y observadas por corregir (docente).
/// Se calculan en el layout de TODAS las páginas (también la de error): si la base de datos no responde se
/// devuelven (0, 0) y se registra el aviso, en lugar de tumbar la página (mismo criterio que UserProfileViewComponent).
/// </summary>
public sealed class SidebarCountsService
{
    /// <summary>
    /// Acciones 8-11 del historial que abren o cierran un pedido de cambio de material (L-3); el pedido sigue
    /// pendiente mientras el más reciente de esas acciones sea «Pidió cambio de material» (8).
    /// </summary>
    private static readonly PlanningReviewAction[] MaterialChangeActions =
    [
        PlanningReviewAction.MaterialChangeRequested, PlanningReviewAction.MaterialChangeApproved,
        PlanningReviewAction.MaterialChangeRejected, PlanningReviewAction.MaterialChangeWithdrawn
    ];

    private readonly ApplicationDbContext _context;
    private readonly ILogger<SidebarCountsService> _logger;

    public SidebarCountsService(ApplicationDbContext context, ILogger<SidebarCountsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Pending, int Observed)> GetAsync(ClaimsPrincipal user)
    {
        var closureOnly = PracticeClosureAccess.IsClosureOnly(user);
        var teacherOnly = TeacherAccess.IsTeacherOnly(user);
        var isReviewer = !closureOnly && !teacherOnly
            && (user.IsInRole(AuthorizationHelper.RoleSupervisor)
                || user.IsInRole(AuthorizationHelper.RoleAdministrator)
                || user.IsInRole(AuthorizationHelper.RoleSuperAdmin));
        try
        {
            // Una sola consulta traducida a SQL (corre en cada página): solicitudes en revisión MÁS pedidos de
            // cambio de material (L-3) pendientes. El pedido cuenta como pendiente cuando el review más reciente
            // de acciones 8-11 (por CreatedDate y luego Id) sigue siendo MaterialChangeRequested: la misma regla
            // que PendingMaterialChangeReviewAsync de TeacherRequestService.
            var pending = isReviewer
                ? await _context.Plannings.AsNoTracking()
                    .CountAsync(p => !p.IsDeleted && (p.Status == PlanningStatus.Requested
                        || p.Reviews.Where(r => MaterialChangeActions.Contains(r.Action))
                            .OrderByDescending(r => r.CreatedDate).ThenByDescending(r => r.Id)
                            .Select(r => (int?)r.Action).FirstOrDefault()
                            == (int)PlanningReviewAction.MaterialChangeRequested))
                : 0;
            var observed = teacherOnly && TeacherAccess.PersonId(user) is int personId
                ? await _context.Plannings.AsNoTracking()
                    .CountAsync(p => !p.IsDeleted && p.TeacherPersonId == personId && p.Status == PlanningStatus.Observed)
                : 0;
            return (pending, observed);
        }
        catch (Exception ex) when (ex is System.Data.Common.DbException || ex.InnerException is System.Data.Common.DbException
            || ex is InvalidOperationException || ex is TimeoutException)
        {
            // Defecto D-02 (Práctica 3): sin base de datos el menú no debe arrastrar también la página de error.
            _logger.LogWarning(ex, "No se pudieron calcular los contadores del menú lateral.");
            return (0, 0);
        }
    }
}
