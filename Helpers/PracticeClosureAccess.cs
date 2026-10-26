using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Alcance del rol "Encargado de cierre de prácticas": solo el módulo de cierre (prácticas del día,
/// L-3 entrega/devolución y L-5 incidentes). La gestión (Supervisor/Administrador) no tiene estas restricciones.
/// </summary>
public static class PracticeClosureAccess
{
    public const string HomePage = "/Cierre/Index";

    /// <summary>Rutas que el rol de cierre puede abrir (prefijos, sin distinguir mayúsculas).</summary>
    private static readonly string[] AllowedPrefixes =
    [
        "/Cierre", "/LaboratoryOperations/L3", "/LaboratoryOperations/L5",
        "/Login", "/Logout", "/Error", "/Api/Notifications", "/Notifications",
        "/AccesoDenegado", "/Ayuda", "/Account/Profile"
    ];

    /// <summary>True si el usuario solo tiene el rol de cierre (sin rol de gestión).</summary>
    public static bool IsClosureOnly(ClaimsPrincipal user) =>
        user.IsInRole(AuthorizationHelper.RolePracticeClosure)
        && !user.IsInRole(AuthorizationHelper.RoleSupervisor)
        && !user.IsInRole(AuthorizationHelper.RoleAdministrator)
        && !user.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    public static bool IsAllowedPath(PathString path) =>
        path == "/" ? false : AllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Middleware: un usuario de cierre que intenta abrir otra parte del sistema vuelve a su pantalla
    /// (GET) o recibe 403 (otros métodos).
    /// </summary>
    public static IApplicationBuilder UsePracticeClosureScope(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true
                && IsClosureOnly(context.User)
                && !IsAllowedPath(context.Request.Path))
            {
                if (HttpMethods.IsGet(context.Request.Method))
                    context.Response.Redirect("/Cierre");
                else
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next();
        });

    /// <summary>
    /// Para el rol de cierre: con una salida abierta (Active/Overdue) puede abrir la planificación en cualquier
    /// estado (el L-3 queda en la planificación de origen aunque se haya reprogramado o esté en reposición);
    /// si no, debe estar aprobada o finalizada y ser de hoy. Devuelve el motivo del rechazo o null si puede abrirla.
    /// </summary>
    public static async Task<string?> DenyReasonAsync(ApplicationDbContext context, ClaimsPrincipal user, int practiceId)
    {
        if (!IsClosureOnly(user)) return null;

        var today = DateTime.Today;
        var openStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
        var planning = await context.Plannings.AsNoTracking()
            .Where(p => p.Id == practiceId)
            .Select(p => new
            {
                p.ScheduledDate,
                p.Status,
                HasOpenDeparture = context.Departures.Any(d => d.PlanningId == p.Id && openStatuses.Contains(d.Status))
            })
            .FirstOrDefaultAsync();

        if (planning == null) return "La planificación no existe.";
        if (planning.HasOpenDeparture) return null;
        if (planning.Status != PlanningStatus.Approved && planning.Status != PlanningStatus.Finalized)
            return "La planificación todavía no está aprobada.";
        if (planning.ScheduledDate.Date == today) return null;
        return "Solo puedes cerrar las planificaciones del día o las que tienen devoluciones pendientes.";
    }
}
