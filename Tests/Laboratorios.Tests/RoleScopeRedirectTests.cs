using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Helpers;
using Xunit;

namespace Laboratorios.Tests;

/// <summary>
/// Alcance por rol (UsePracticeClosureScope → UseTeacherScope → UseStudentScope, en el orden de Program.cs).
/// Regresión del 25/11/2026: una cuenta de «Cierre de práctica» con cambio de contraseña obligatorio rebotaba
/// entre /Cierre y /Account/CambiarClave hasta que el navegador mostraba ERR_TOO_MANY_REDIRECTS.
/// </summary>
public class RoleScopeRedirectTests
{
    private const int MaxHops = 5;

    private static readonly RequestDelegate Pipeline = BuildPipeline();

    private static RequestDelegate BuildPipeline()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.UsePracticeClosureScope();
        app.UseTeacherScope();
        app.UseStudentScope();
        // Si la petición pasa los tres middlewares, la página responde 200.
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        return app.Build();
    }

    private static ClaimsPrincipal UserWith(string role, bool mustChangePassword)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "prueba"), new(ClaimTypes.Role, role) };
        if (mustChangePassword)
            claims.Add(new Claim(TeacherAccess.MustChangePasswordClaim, "true"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
    }

    /// <summary>Sigue las redirecciones como un navegador y devuelve la ruta final y cuántos saltos hizo.</summary>
    private static async Task<(string FinalPath, int Hops, int Status)> FollowAsync(ClaimsPrincipal user, string startPath)
    {
        var path = startPath;
        for (var hops = 0; hops <= MaxHops; hops++)
        {
            var context = new DefaultHttpContext { User = user };
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = path;
            await Pipeline(context);

            if (context.Response.StatusCode != StatusCodes.Status302Found)
                return (path, hops, context.Response.StatusCode);

            path = context.Response.Headers.Location.ToString();
        }
        return (path, MaxHops + 1, StatusCodes.Status302Found);
    }

    public static TheoryData<string, string> RolesAndStartPages()
    {
        var data = new TheoryData<string, string>();
        var roles = new[]
        {
            AuthorizationHelper.RolePracticeClosure, AuthorizationHelper.RoleTeacher, AuthorizationHelper.RoleStudent,
            AuthorizationHelper.RoleSupervisor, AuthorizationHelper.RoleAdministrator, AuthorizationHelper.RoleSuperAdmin
        };
        var pages = new[] { "/", "/Cierre", "/Docente", "/Estudiante", "/Inicio", TeacherAccess.ChangePasswordPath };
        foreach (var role in roles)
            foreach (var page in pages)
                data.Add(role, page);
        return data;
    }

    [Theory]
    [MemberData(nameof(RolesAndStartPages))]
    public async Task Con_cambio_obligatorio_todo_rol_llega_a_cambiar_la_clave_sin_bucle(string role, string startPath)
    {
        var (finalPath, hops, status) = await FollowAsync(UserWith(role, mustChangePassword: true), startPath);

        Assert.True(hops <= 2, $"{role} desde {startPath}: {hops} redirecciones (bucle), terminó en {finalPath}.");
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(TeacherAccess.ChangePasswordPath, finalPath);
    }

    [Theory]
    [MemberData(nameof(RolesAndStartPages))]
    public async Task Sin_cambio_obligatorio_ningun_rol_queda_en_bucle(string role, string startPath)
    {
        var (finalPath, hops, status) = await FollowAsync(UserWith(role, mustChangePassword: false), startPath);

        Assert.True(hops <= 2, $"{role} desde {startPath}: {hops} redirecciones (bucle), terminó en {finalPath}.");
        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Fact]
    public void El_rol_de_cierre_puede_abrir_el_cambio_de_contrasena()
    {
        Assert.True(PracticeClosureAccess.IsAllowedPath(TeacherAccess.ChangePasswordPath));
        Assert.True(TeacherAccess.IsAllowedPath(TeacherAccess.ChangePasswordPath));
        Assert.True(StudentAccess.IsAllowedPath(TeacherAccess.ChangePasswordPath));
    }
}
