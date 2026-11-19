using System.Security.Claims;

namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Alcance del rol Docente: solo su portal (/Docente), su perfil, el cambio de contraseña y las notificaciones.
/// Además obliga a cambiar la contraseña generada por el sistema antes de usar la aplicación (cualquier rol).
/// </summary>
public static class TeacherAccess
{
    public const string HomePage = "/Docente/Index";
    public const string ChangePasswordPath = "/Account/CambiarClave";
    public const string MustChangePasswordClaim = "must_change_password";
    public const string PersonIdClaim = "person_id";

    private static readonly string[] AllowedPrefixes =
    [
        "/Docente", "/Account", "/Login", "/Logout", "/Error", "/Api/Notifications", "/Notifications",
        "/AccesoDenegado", "/Ayuda"
    ];

    private static readonly string[] PasswordChangeAllowed = [ChangePasswordPath, "/Logout", "/Login", "/Error"];

    /// <summary>True si el usuario es docente (sin rol de gestión).</summary>
    public static bool IsTeacherOnly(ClaimsPrincipal user) =>
        user.IsInRole(AuthorizationHelper.RoleTeacher)
        && !user.IsInRole(AuthorizationHelper.RoleSupervisor)
        && !user.IsInRole(AuthorizationHelper.RoleAdministrator)
        && !user.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    public static int? PersonId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(PersonIdClaim)?.Value, out var id) ? id : null;

    public static bool MustChangePassword(ClaimsPrincipal user) =>
        string.Equals(user.FindFirst(MustChangePasswordClaim)?.Value, "true", StringComparison.OrdinalIgnoreCase);

    public static bool IsAllowedPath(PathString path) =>
        path != "/" && AllowedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Middleware (después de UseAuthentication): primero el cambio de contraseña obligatorio; luego el alcance
    /// del docente. Un GET fuera de alcance vuelve a su portal; otro método recibe 403.
    /// </summary>
    public static IApplicationBuilder UseTeacherScope(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path;
                if (MustChangePassword(user)
                    && !PasswordChangeAllowed.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
                {
                    if (HttpMethods.IsGet(context.Request.Method))
                        context.Response.Redirect(ChangePasswordPath);
                    else
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                if (IsTeacherOnly(user) && !IsAllowedPath(path))
                {
                    if (HttpMethods.IsGet(context.Request.Method))
                        context.Response.Redirect("/Docente");
                    else
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }
            await next();
        });
}
