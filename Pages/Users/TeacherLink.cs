using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Users;

/// <summary>
/// Vínculo usuario ↔ persona docente (rol Docente). Lo comparten Crear y Editar usuario:
/// lista de docentes del catálogo sin cuenta y validación del vínculo.
/// </summary>
public static class TeacherLink
{
    public sealed record TeacherOption(int PersonId, string Name, int Groups);

    /// <summary>Docentes activos (categoría Docente o rol operativo Docente) sin otra cuenta vinculada.</summary>
    public static async Task<List<TeacherOption>> OptionsAsync(ApplicationDbContext context, int? currentUserId)
    {
        var linked = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.PersonId != null && (currentUserId == null || u.Id != currentUserId))
            .Select(u => u.PersonId!.Value)
            .ToListAsync();
        var teacherIds = await context.PersonRoleAssignments.AsNoTracking()
            .Where(a => a.IsActive && a.Role == PersonOperationalRole.Teacher)
            .Select(a => a.PersonId)
            .ToListAsync();
        var rows = await context.Interns.AsNoTracking()
            .Where(i => i.Status == GeneralStatus.Activo && (i.Category == PersonCategory.Docente || teacherIds.Contains(i.Id))
                && !linked.Contains(i.Id))
            .Select(i => new
            {
                i.Id, i.Title, i.Name,
                Groups = context.CourseGroups.Count(g => g.TeacherPersonId == i.Id && g.Status == GeneralStatus.Activo)
            })
            .OrderBy(i => i.Name)
            .ToListAsync();
        return rows.Select(r => new TeacherOption(r.Id, string.IsNullOrWhiteSpace(r.Title) ? r.Name : $"{r.Title} {r.Name}", r.Groups)).ToList();
    }

    public static List<SelectListItem> ToSelectList(IEnumerable<TeacherOption> options, int? selected) =>
        options.Select(o => new SelectListItem(
            o.Groups > 0 ? $"{o.Name} · {o.Groups} grupo(s)" : $"{o.Name} · sin grupos asignados",
            o.PersonId.ToString(),
            o.PersonId == selected)).ToList();

    /// <summary>Mensaje de error del vínculo, o null si es válido (solo se exige para el rol Docente).</summary>
    public static async Task<string?> ValidateAsync(ApplicationDbContext context, UserRole role, int? personId, int? currentUserId)
    {
        if (role != UserRole.Docente) return null;
        if (personId is not int id) return "Selecciona la persona docente del catálogo que usará esta cuenta.";
        var options = await OptionsAsync(context, currentUserId);
        return options.Any(o => o.PersonId == id)
            ? null
            : "La persona elegida no es un docente activo o ya tiene otra cuenta vinculada.";
    }
}
