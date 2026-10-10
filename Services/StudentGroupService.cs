using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Materia elegida para el grupo con su docente (y laboratorio propio opcional).</summary>
public sealed record StudentGroupSubjectInput(int SubjectId, int TeacherPersonId, int? LaboratoryId);

/// <summary>Datos del formulario de grupo. Al editar, gestión, carrera y semestre no cambian.</summary>
public sealed record StudentGroupInput(
    int ManagementId,
    int CareerId,
    int Semester,
    string Letter,
    int? StudentCount,
    int? ScalingFactor,
    int? LaboratoryId,
    GeneralStatus Status,
    IReadOnlyList<StudentGroupSubjectInput> Subjects);

/// <summary>Resultado de guardar: Id del grupo o mensaje de error en español.</summary>
public sealed record StudentGroupResult(int? GroupId, string? Error)
{
    public static StudentGroupResult Ok(int id) => new(id, null);
    public static StudentGroupResult Fail(string error) => new(null, error);
}

public sealed record SemesterSubjectOption(int SubjectId, string? Code, string Name);

/// <summary>Materia de un grupo (su grupo de materia) con docente y cantidad de periodos de clase por semana.</summary>
public sealed record StudentGroupSubjectView(int CourseGroupId, int SubjectId, string SubjectName, int TeacherPersonId,
    string TeacherName, int? LaboratoryId, int PeriodsPerWeek);

/// <summary>Grupo de estudiantes para listados y formularios.</summary>
/// <param name="SemesterSubjectCount">Materias activas del semestre de la carrera (para mostrar "3/5 materias").</param>
public sealed record StudentGroupView(
    int Id,
    string Code,
    int ManagementId,
    int CareerId,
    string CareerName,
    int Semester,
    string Letter,
    string Name,
    int? StudentCount,
    int? ScalingFactor,
    int? LaboratoryId,
    string? LaboratoryName,
    GeneralStatus Status,
    IReadOnlyList<StudentGroupSubjectView> Subjects,
    int SemesterSubjectCount)
{
    public int PeriodsPerWeek => Subjects.Sum(s => s.PeriodsPerWeek);
    public int SubjectsWithoutSchedule => Subjects.Count(s => s.PeriodsPerWeek == 0);
    public string Label => $"Grupo {Name}";

    /// <summary>Clave del cohorte en la página de horario por semestre ("1|A1").</summary>
    public string CohortKey => $"{Semester}|{Name}";
}

public interface IStudentGroupService
{
    /// <summary>Grupos de la gestión (opcionalmente de una carrera), por carrera, semestre y nombre.</summary>
    Task<IReadOnlyList<StudentGroupView>> ListAsync(int managementId, int? careerId = null, CancellationToken ct = default);

    Task<StudentGroupView?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Materias activas de la carrera y semestre, por nombre.</summary>
    Task<IReadOnlyList<SemesterSubjectOption>> SemesterSubjectsAsync(int careerId, int semester, CancellationToken ct = default);

    /// <summary>Crea el grupo y un grupo de materia por cada materia elegida (todo o nada).</summary>
    Task<StudentGroupResult> CreateAsync(StudentGroupInput input, CancellationToken ct = default);

    /// <summary>
    /// Actualiza el grupo: renombra y sincroniza estudiantes/factor/estado en sus materias, cambia docentes,
    /// agrega materias nuevas y quita las desmarcadas (borrado físico aprobado para grupos de la gestión).
    /// </summary>
    Task<StudentGroupResult> UpdateAsync(int id, StudentGroupInput input, CancellationToken ct = default);

    /// <summary>Elimina el grupo y sus materias (las planificaciones conservan el nombre del grupo). Null si borró.</summary>
    Task<string?> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed partial class StudentGroupService : IStudentGroupService
{
    [GeneratedRegex("^[A-Z]{1,3}$")]
    private static partial Regex LetterRegex();

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly ICourseGroupPurgeService _purge;

    public StudentGroupService(ApplicationDbContext context, IAcademicManagementService managements, ICourseGroupPurgeService purge)
    {
        _context = context;
        _managements = managements;
        _purge = purge;
    }

    // ==================== Consultas ====================

    public async Task<IReadOnlyList<StudentGroupView>> ListAsync(int managementId, int? careerId = null, CancellationToken ct = default)
    {
        var ids = await _context.StudentGroups.AsNoTracking()
            .Where(g => g.ManagementId == managementId && g.Status != GeneralStatus.Eliminado
                && (careerId == null || g.CareerId == careerId))
            .Select(g => g.Id)
            .ToListAsync(ct);
        return await LoadViewsAsync(ids, ct);
    }

    public async Task<StudentGroupView?> GetAsync(int id, CancellationToken ct = default) =>
        (await LoadViewsAsync([id], ct)).FirstOrDefault();

    public async Task<IReadOnlyList<SemesterSubjectOption>> SemesterSubjectsAsync(int careerId, int semester, CancellationToken ct = default) =>
        await _context.Subjects.AsNoTracking()
            .Where(s => s.Status == GeneralStatus.Activo && s.CareerId == careerId && s.Semester == semester)
            .OrderBy(s => s.Name)
            .Select(s => new SemesterSubjectOption(s.Id, s.Code, s.Name))
            .ToListAsync(ct);

    private async Task<List<StudentGroupView>> LoadViewsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];

        var groups = await _context.StudentGroups.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .Select(g => new
            {
                g.Id, g.Code, g.ManagementId, g.CareerId, CareerName = g.Career!.Name, g.Semester, g.Letter, g.Name,
                g.StudentCount, g.ScalingFactor, g.LaboratoryId,
                LaboratoryName = g.Laboratory != null ? g.Laboratory.Code + " · " + g.Laboratory.Name : null,
                g.Status
            })
            .ToListAsync(ct);

        var subjects = await _context.CourseGroups.AsNoTracking()
            .Where(c => c.StudentGroupId != null && ids.Contains(c.StudentGroupId.Value))
            .Select(c => new
            {
                c.Id, StudentGroupId = c.StudentGroupId!.Value, c.SubjectId, SubjectName = c.Subject!.Name,
                c.TeacherPersonId, c.LaboratoryId,
                Periods = _context.CourseGroupScheduleSlots.Count(s => s.CourseGroupId == c.Id)
            })
            .ToListAsync(ct);

        // Person.FullName es NotMapped: el nombre del docente sale de Interns.
        var teacherIds = subjects.Select(s => s.TeacherPersonId).Distinct().ToList();
        var teacherNames = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name })
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        var semesterKeys = groups.Select(g => new { g.CareerId, g.Semester }).Distinct().ToList();
        var careerIds = semesterKeys.Select(k => k.CareerId).Distinct().ToList();
        var subjectCounts = (await _context.Subjects.AsNoTracking()
                .Where(s => s.Status == GeneralStatus.Activo && s.CareerId != null && careerIds.Contains(s.CareerId.Value))
                .GroupBy(s => new { CareerId = s.CareerId!.Value, s.Semester })
                .Select(g => new { g.Key.CareerId, g.Key.Semester, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => (x.CareerId, x.Semester), x => x.Count);

        return groups
            .OrderBy(g => g.CareerName).ThenBy(g => g.Semester).ThenBy(g => g.Name)
            .Select(g => new StudentGroupView(
                g.Id, g.Code, g.ManagementId, g.CareerId, g.CareerName, g.Semester, g.Letter, g.Name,
                g.StudentCount, g.ScalingFactor, g.LaboratoryId, g.LaboratoryName, g.Status,
                subjects.Where(s => s.StudentGroupId == g.Id)
                    .OrderBy(s => s.SubjectName)
                    .Select(s => new StudentGroupSubjectView(s.Id, s.SubjectId, s.SubjectName, s.TeacherPersonId,
                        teacherNames.TryGetValue(s.TeacherPersonId, out var t) ? t : $"Docente #{s.TeacherPersonId}",
                        s.LaboratoryId, s.Periods))
                    .ToList(),
                subjectCounts.GetValueOrDefault((g.CareerId, (int?)g.Semester))))
            .ToList();
    }

    // ==================== Crear / editar ====================

    public async Task<StudentGroupResult> CreateAsync(StudentGroupInput input, CancellationToken ct = default)
    {
        var letter = (input.Letter ?? string.Empty).Trim().ToUpperInvariant();
        if (!LetterRegex().IsMatch(letter))
            return StudentGroupResult.Fail("La letra del grupo debe tener de 1 a 3 letras (A, B, C...).");

        var name = StudentGroup.ComposeName(letter, input.Semester);
        var error = await ValidateHeaderAsync(input, null, name, ct)
                    ?? await ValidateSubjectsAsync(input, ct);
        if (error != null) return StudentGroupResult.Fail(error);

        var conflict = await FindSubjectNameConflictAsync(input.ManagementId, name, input.Subjects.Select(s => s.SubjectId), null, ct);
        if (conflict != null) return StudentGroupResult.Fail(conflict);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var group = new StudentGroup
        {
            ManagementId = input.ManagementId,
            CareerId = input.CareerId,
            Semester = input.Semester,
            Letter = letter,
            Name = name,
            StudentCount = input.StudentCount,
            ScalingFactor = input.ScalingFactor,
            LaboratoryId = input.LaboratoryId,
            Status = input.Status == GeneralStatus.Inactivo ? GeneralStatus.Inactivo : GeneralStatus.Activo
        };
        _context.StudentGroups.Add(group);
        await _context.SaveWithCodeAsync(
            _context.StudentGroups.Select(g => (string?)g.Code), CatalogCodes.StudentGroup, code => group.Code = code);

        foreach (var subject in input.Subjects)
            await AddSubjectAsync(group, subject, ct);

        await transaction.CommitAsync(ct);
        _context.ChangeTracker.Clear();
        return StudentGroupResult.Ok(group.Id);
    }

    public async Task<StudentGroupResult> UpdateAsync(int id, StudentGroupInput input, CancellationToken ct = default)
    {
        var group = await _context.StudentGroups.AsTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group == null) return StudentGroupResult.Fail("El grupo no existe.");

        // Gestión, carrera y semestre definen el grupo: no cambian al editar.
        input = input with { ManagementId = group.ManagementId, CareerId = group.CareerId, Semester = group.Semester };

        var letter = (input.Letter ?? string.Empty).Trim().ToUpperInvariant();
        var keepsLegacyLetter = letter == group.Letter;
        if (!keepsLegacyLetter && !LetterRegex().IsMatch(letter))
            return StudentGroupResult.Fail("La letra del grupo debe tener de 1 a 3 letras (A, B, C...).");

        var newName = keepsLegacyLetter ? group.Name : StudentGroup.ComposeName(letter, group.Semester);
        var error = await ValidateHeaderAsync(input, group.Id, newName, ct)
                    ?? await ValidateSubjectsAsync(input, ct);
        if (error != null) return StudentGroupResult.Fail(error);

        var conflict = await FindSubjectNameConflictAsync(group.ManagementId, newName, input.Subjects.Select(s => s.SubjectId), group.Id, ct);
        if (conflict != null) return StudentGroupResult.Fail(conflict);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var oldName = group.Name;
        group.Letter = letter;
        group.Name = newName;
        group.StudentCount = input.StudentCount;
        group.ScalingFactor = input.ScalingFactor;
        group.LaboratoryId = input.LaboratoryId;
        group.Status = input.Status == GeneralStatus.Inactivo ? GeneralStatus.Inactivo : GeneralStatus.Activo;

        var existing = await _context.CourseGroups.AsTracking()
            .Where(c => c.StudentGroupId == group.Id)
            .ToListAsync(ct);
        var wanted = input.Subjects.ToDictionary(s => s.SubjectId);

        // Materias que siguen: sincronizar nombre, docente, laboratorio, estudiantes, factor y estado.
        foreach (var courseGroup in existing.Where(c => wanted.ContainsKey(c.SubjectId)))
        {
            var subject = wanted[courseGroup.SubjectId];
            courseGroup.Name = newName;
            courseGroup.TeacherPersonId = subject.TeacherPersonId;
            courseGroup.LaboratoryId = subject.LaboratoryId ?? group.LaboratoryId;
            courseGroup.StudentCount = group.StudentCount;
            courseGroup.ScalingFactor = group.ScalingFactor;
            courseGroup.Status = group.Status;
        }

        // Las planificaciones y horarios de práctica que guardan el nombre del grupo siguen al nombre nuevo.
        if (!string.Equals(oldName, newName, StringComparison.Ordinal))
            await RenameReferencesAsync(group.ManagementId, existing.Select(c => c.SubjectId).ToList(),
                existing.Select(c => c.Id).ToList(), oldName, newName, ct);

        await _context.SaveChangesAsync(ct);

        // Materias nuevas.
        var current = existing.Select(c => c.SubjectId).ToHashSet();
        foreach (var subject in input.Subjects.Where(s => !current.Contains(s.SubjectId)))
            await AddSubjectAsync(group, subject, ct);

        // Materias desmarcadas: se quitan del grupo (borrado físico aprobado; libera planificaciones).
        foreach (var removed in existing.Where(c => !wanted.ContainsKey(c.SubjectId)).Select(c => c.Id).ToList())
            await _purge.DeleteGroupAsync(removed, ct);

        await transaction.CommitAsync(ct);
        _context.ChangeTracker.Clear();
        return StudentGroupResult.Ok(group.Id);
    }

    public async Task<string?> DeleteAsync(int id, CancellationToken ct = default)
    {
        var group = await _context.StudentGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
        if (group == null) return "El grupo no existe.";

        var management = await _managements.ResolveAsync(group.ManagementId);
        if (management is { IsEditable: false })
            return $"La gestión {management.Code} está completada: sus grupos no se pueden eliminar.";

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var courseGroupIds = await _context.CourseGroups.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.StudentGroupId == id)
            .Select(c => c.Id)
            .ToListAsync(ct);
        foreach (var courseGroupId in courseGroupIds)
            await _purge.DeleteGroupAsync(courseGroupId, ct);

        // Grupo de trabajo de la gestión: borrado físico aprobado, igual que sus grupos de materia.
        var tracked = await _context.StudentGroups.AsTracking().FirstAsync(g => g.Id == id, ct);
        _context.StudentGroups.Remove(tracked);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        _context.ChangeTracker.Clear();
        return null;
    }

    // ==================== Auxiliares ====================

    private async Task AddSubjectAsync(StudentGroup group, StudentGroupSubjectInput subject, CancellationToken ct)
    {
        var courseGroup = new CourseGroup
        {
            ManagementId = group.ManagementId,
            SubjectId = subject.SubjectId,
            StudentGroupId = group.Id,
            Name = group.Name,
            TeacherPersonId = subject.TeacherPersonId,
            LaboratoryId = subject.LaboratoryId ?? group.LaboratoryId,
            StudentCount = group.StudentCount,
            ScalingFactor = group.ScalingFactor,
            Status = group.Status
        };
        _context.CourseGroups.Add(courseGroup);
        await _context.SaveWithCodeAsync(
            _context.CourseGroups.IgnoreQueryFilters().Select(c => (string?)c.Code), CatalogCodes.CourseGroup,
            code => courseGroup.Code = code);
    }

    private async Task<string?> ValidateHeaderAsync(StudentGroupInput input, int? groupId, string name, CancellationToken ct)
    {
        var management = await _managements.ResolveAsync(input.ManagementId);
        if (management == null) return "La gestión seleccionada no existe o no es semestral.";
        if (!management.IsEditable) return $"La gestión {management.Code} está completada: sus grupos son de solo lectura.";

        var careerOk = await _context.Careers.AsNoTracking()
            .AnyAsync(c => c.Id == input.CareerId && c.Status == GeneralStatus.Activo, ct);
        if (!careerOk) return "La carrera seleccionada no existe o no está activa.";
        if (input.Semester is < 1 or > 12) return "El semestre debe estar entre 1 y 12.";

        if (input.StudentCount is < 0 or > 1000) return "El número de estudiantes debe estar entre 0 y 1000.";
        if (input.ScalingFactor is < 0 or > 1000) return "El factor debe estar entre 0 y 1000.";

        if (input.LaboratoryId.HasValue
            && !await _context.Laboratories.AsNoTracking().AnyAsync(l => l.Id == input.LaboratoryId && l.Status == GeneralStatus.Activo, ct))
            return "El laboratorio habitual no existe o no está activo.";

        var duplicate = await _context.StudentGroups.AsNoTracking()
            .AnyAsync(g => g.ManagementId == input.ManagementId && g.CareerId == input.CareerId
                && g.Semester == input.Semester && g.Name == name && g.Status != GeneralStatus.Eliminado && g.Id != (groupId ?? 0), ct);
        if (duplicate) return $"Ya existe el grupo {name} en este semestre de la carrera.";

        return null;
    }

    private async Task<string?> ValidateSubjectsAsync(StudentGroupInput input, CancellationToken ct)
    {
        if (input.Subjects.Count == 0) return "Marque al menos una materia del semestre para el grupo.";
        if (input.Subjects.Select(s => s.SubjectId).Distinct().Count() != input.Subjects.Count)
            return "Hay materias repetidas en el grupo.";

        var subjectIds = input.Subjects.Select(s => s.SubjectId).ToList();
        var subjects = await _context.Subjects.AsNoTracking()
            .Where(s => subjectIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.Status, s.CareerId, s.Semester })
            .ToListAsync(ct);
        if (subjects.Count != subjectIds.Count) return "Una de las materias elegidas no existe.";
        var foreign = subjects.FirstOrDefault(s => s.Status != GeneralStatus.Activo || s.CareerId != input.CareerId || s.Semester != input.Semester);
        if (foreign != null) return $"La materia {foreign.Name} no es una materia activa del {input.Semester}.º semestre de la carrera.";

        var missingTeacher = input.Subjects.FirstOrDefault(s => s.TeacherPersonId <= 0);
        if (missingTeacher != null)
            return $"Asigne el docente de {subjects.First(s => s.Id == missingTeacher.SubjectId).Name}.";

        var teacherIds = input.Subjects.Select(s => s.TeacherPersonId).Distinct().ToList();
        var validTeachers = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id) && i.Category == PersonCategory.Docente
                && i.Status == GeneralStatus.Activo && i.InternStatus == GeneralStatus.Activo)
            .Select(i => i.Id)
            .ToListAsync(ct);
        var badTeacher = input.Subjects.FirstOrDefault(s => !validTeachers.Contains(s.TeacherPersonId));
        if (badTeacher != null)
            return $"El docente elegido para {subjects.First(s => s.Id == badTeacher.SubjectId).Name} no existe o no está activo.";

        var labIds = input.Subjects.Where(s => s.LaboratoryId.HasValue).Select(s => s.LaboratoryId!.Value).Distinct().ToList();
        if (labIds.Count > 0)
        {
            var validLabs = await _context.Laboratories.AsNoTracking()
                .Where(l => labIds.Contains(l.Id) && l.Status == GeneralStatus.Activo)
                .CountAsync(ct);
            if (validLabs != labIds.Count) return "Uno de los laboratorios elegidos no existe o no está activo.";
        }

        return null;
    }

    /// <summary>Una materia no puede tener dos grupos con el mismo nombre en la gestión (índice único de CourseGroups).</summary>
    private async Task<string?> FindSubjectNameConflictAsync(int managementId, string name, IEnumerable<int> subjectIds,
        int? ownGroupId, CancellationToken ct)
    {
        var ids = subjectIds.ToList();
        var clash = await _context.CourseGroups.AsNoTracking()
            .Where(c => c.ManagementId == managementId && ids.Contains(c.SubjectId) && c.Name == name
                && (ownGroupId == null || c.StudentGroupId != ownGroupId))
            .Select(c => c.Subject!.Name)
            .FirstOrDefaultAsync(ct);
        return clash == null ? null : $"La materia {clash} ya tiene un grupo {name} en esta gestión (fuera de este grupo).";
    }

    private async Task RenameReferencesAsync(int managementId, List<int> subjectIds, List<int> courseGroupIds,
        string oldName, string newName, CancellationToken ct)
    {
        var plannings = await _context.Plannings.IgnoreQueryFilters().AsTracking()
            .Where(p => (p.CourseGroupId != null && courseGroupIds.Contains(p.CourseGroupId.Value))
                || (p.ManagementId == managementId && p.SubjectId != null && subjectIds.Contains(p.SubjectId.Value) && p.GroupName == oldName))
            .ToListAsync(ct);
        foreach (var planning in plannings)
            planning.GroupName = newName;

        var schedules = await _context.PracticeSchedules.IgnoreQueryFilters().AsTracking()
            .Where(s => (s.CourseGroupId != null && courseGroupIds.Contains(s.CourseGroupId.Value))
                || (s.ManagementId == managementId && subjectIds.Contains(s.SubjectId) && s.GroupName == oldName))
            .ToListAsync(ct);
        foreach (var schedule in schedules)
            schedule.GroupName = newName;
    }
}
