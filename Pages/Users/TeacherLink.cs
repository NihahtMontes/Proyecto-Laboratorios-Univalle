using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Users;

/// <summary>
/// Vínculo usuario ↔ persona docente (rol Docente). Lo comparten Crear y Editar usuario:
/// lista de docentes del catálogo sin cuenta, validación del vínculo y alta de un docente nuevo (solo al crear).
/// </summary>
public static class TeacherLink
{
    /// <summary>Largo máximo del nombre de una persona interna (Intern.Name).</summary>
    public const int MaxNameLength = 100;

    /// <param name="CurrentGroups">Grupos activos en la gestión académica actual.</param>
    /// <param name="OtherGroups">Grupos activos en otras gestiones (por ejemplo, la próxima, todavía inactiva).</param>
    /// <param name="CurrentCode">Código de la gestión actual (2026-2), o null si no hay una activa.</param>
    public sealed record TeacherOption(int PersonId, string Name, int CurrentGroups, int OtherGroups, string? CurrentCode)
    {
        /// <summary>Texto de la opción: el docente y sus grupos de la gestión actual, para no confundir con otras gestiones.</summary>
        public string Label
        {
            get
            {
                if (CurrentCode is null)
                {
                    var total = CurrentGroups + OtherGroups;
                    return total > 0 ? $"{Name} · {total} grupo(s)" : $"{Name} · sin grupos asignados";
                }
                if (CurrentGroups > 0)
                {
                    return OtherGroups > 0
                        ? $"{Name} · {CurrentGroups} grupo(s) en {CurrentCode} y {OtherGroups} en otras gestiones"
                        : $"{Name} · {CurrentGroups} grupo(s) en {CurrentCode}";
                }
                return OtherGroups > 0
                    ? $"{Name} · sin grupos en {CurrentCode} ({OtherGroups} en otras gestiones)"
                    : $"{Name} · sin grupos asignados";
            }
        }
    }

    /// <summary>Docentes activos (categoría Docente o rol operativo Docente) sin otra cuenta vinculada.</summary>
    public static async Task<List<TeacherOption>> OptionsAsync(ApplicationDbContext context, int? currentUserId)
    {
        // La misma gestión «actual» que usan el Panel, la Grilla y el portal del docente.
        var current = await new AcademicManagementService(context).GetCurrentAsync();
        var currentId = current?.Id ?? -1;

        var linked = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.PersonId != null && (currentUserId == null || u.Id != currentUserId))
            .Select(u => u.PersonId!.Value)
            .ToListAsync();
        var teacherIds = await TeacherRoleIdsAsync(context);
        var rows = await context.Interns.AsNoTracking()
            .Where(i => i.Status == GeneralStatus.Activo && (i.Category == PersonCategory.Docente || teacherIds.Contains(i.Id))
                && !linked.Contains(i.Id))
            .Select(i => new
            {
                i.Id, i.Title, i.Name,
                Current = context.CourseGroups.Count(g => g.TeacherPersonId == i.Id && g.Status == GeneralStatus.Activo
                    && g.ManagementId == currentId),
                Other = context.CourseGroups.Count(g => g.TeacherPersonId == i.Id && g.Status == GeneralStatus.Activo
                    && g.ManagementId != currentId)
            })
            .OrderBy(i => i.Name)
            .ToListAsync();
        return rows.Select(r => new TeacherOption(
            r.Id, string.IsNullOrWhiteSpace(r.Title) ? r.Name : $"{r.Title} {r.Name}", r.Current, r.Other, current?.Code)).ToList();
    }

    public static List<SelectListItem> ToSelectList(IEnumerable<TeacherOption> options, int? selected) =>
        options.Select(o => new SelectListItem(o.Label, o.PersonId.ToString(), o.PersonId == selected)).ToList();

    /// <summary>
    /// Mensaje de error del vínculo, o null si es válido (solo se exige para el rol Docente).
    /// Con <paramref name="allowNew"/> (crear usuario) se acepta no elegir a nadie: se registrará un docente nuevo.
    /// </summary>
    public static async Task<string?> ValidateAsync(ApplicationDbContext context, UserRole role, int? personId, int? currentUserId,
        bool allowNew = false)
    {
        if (role != UserRole.Docente) return null;
        if (personId is not int id)
            return allowNew ? null : "Selecciona la persona docente del catálogo que usará esta cuenta.";
        var options = await OptionsAsync(context, currentUserId);
        return options.Any(o => o.PersonId == id)
            ? null
            : "La persona elegida no es un docente activo o ya tiene otra cuenta vinculada.";
    }

    /// <summary>Nombre completo de la ficha del docente nuevo: nombres y apellidos de la cuenta, sin espacios de más.</summary>
    public static string FullName(string? firstName, string? lastName, string? secondLastName) =>
        string.Join(" ", new[] { firstName, lastName, secondLastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Clean()));

    /// <summary>
    /// Error si el docente nuevo no se puede registrar: nombre demasiado largo o ya hay en el catálogo un docente activo
    /// con el mismo nombre (se pide elegirlo en la lista para no duplicarlo). Null si se puede registrar.
    /// </summary>
    public static async Task<string?> NewTeacherErrorAsync(ApplicationDbContext context, string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "Escribe los nombres y el primer apellido del docente.";
        if (fullName.Length > MaxNameLength)
            return $"El nombre completo del docente supera {MaxNameLength} caracteres.";

        var teacherIds = await TeacherRoleIdsAsync(context);
        var match = await context.Interns.AsNoTracking()
            .Where(i => i.Status == GeneralStatus.Activo && i.Name == fullName
                && (i.Category == PersonCategory.Docente || teacherIds.Contains(i.Id)))
            .Select(i => new { i.Id, HasAccount = context.Users.IgnoreQueryFilters().Any(u => u.PersonId == i.Id) })
            .FirstOrDefaultAsync();
        if (match == null) return null;
        return match.HasAccount
            ? $"Ya existe el docente «{fullName}» en el catálogo y tiene otra cuenta vinculada."
            : $"El docente «{fullName}» ya está en el catálogo: elígelo en la lista en lugar de registrarlo de nuevo.";
    }

    /// <summary>Ficha nueva de docente (categoría Docente, activa) con los datos de contacto de la cuenta.</summary>
    public static Intern NewTeacher(string fullName, string? email, string? phoneNumber, int? createdById) => new()
    {
        Name = fullName,
        Category = PersonCategory.Docente,
        Status = GeneralStatus.Activo,
        InternStatus = GeneralStatus.Activo,
        Email = email is { Length: > 0 and <= 100 } ? email : null,
        PhoneNumber = phoneNumber is { Length: > 0 and <= 20 } ? phoneNumber : null,
        CreatedById = createdById,
        CreatedDate = DateTime.UtcNow
    };

    private static Task<List<int>> TeacherRoleIdsAsync(ApplicationDbContext context) =>
        context.PersonRoleAssignments.AsNoTracking()
            .Where(a => a.IsActive && a.Role == PersonOperationalRole.Teacher)
            .Select(a => a.PersonId)
            .ToListAsync();
}
