using System.Security.Claims;

namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Alcance del rol Estudiante: solo su portal (/Estudiante), su perfil, el cambio de contraseña y las notificaciones.
/// Un GET fuera de alcance vuelve a su portal; otro método recibe 403.
/// </summary>
public static class StudentAccess
{
    public const string HomePage = "/Estudiante/Index";
    public const string InstitutionalDomain = "est.univalle.edu";

    private static readonly string[] AllowedPrefixes =
    [
        "/Estudiante", "/Account", "/Login", "/Logout", "/Error", "/Api/Notifications", "/Notifications",
        "/AccesoDenegado", "/Ayuda", "/Registro"
    ];

    /// <summary>True si el usuario es solo estudiante (sin rol de gestión, cierre ni docente).</summary>
    public static bool IsStudentOnly(ClaimsPrincipal user) =>
        user.IsInRole(AuthorizationHelper.RoleStudent)
        && !user.IsInRole(AuthorizationHelper.RoleTeacher)
        && !user.IsInRole(AuthorizationHelper.RolePracticeClosure)
        && !user.IsInRole(AuthorizationHelper.RoleSupervisor)
        && !user.IsInRole(AuthorizationHelper.RoleAdministrator)
        && !user.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    /// <summary>Correo institucional de estudiante: termina en @est.univalle.edu (sin distinguir mayúsculas).</summary>
    public static bool IsInstitutionalEmail(string? email)
    {
        var text = (email ?? "").Trim();
        var at = text.LastIndexOf('@');
        return at > 0 && at == text.IndexOf('@')
            && string.Equals(text[(at + 1)..], InstitutionalDomain, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAllowedPath(PathString path) =>
        path != "/" && AllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    public static IApplicationBuilder UseStudentScope(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true && IsStudentOnly(user) && !IsAllowedPath(context.Request.Path))
            {
                if (HttpMethods.IsGet(context.Request.Method))
                    context.Response.Redirect("/Estudiante");
                else
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next();
        });
}
