using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.StudentGroups;

/// <summary>Campos del formulario Crear/Editar grupo (las materias llegan como filas con casilla).</summary>
public sealed class StudentGroupFormInput
{
    [Display(Name = "Gestión")] public int ManagementId { get; set; }
    [Display(Name = "Carrera")] public int CareerId { get; set; }
    [Display(Name = "Semestre")] public int Semester { get; set; }
    // Nombres en español: los mensajes de validación muestran el nombre del campo (antes «El campo Letter es obligatorio»).
    [Display(Name = "Letra del grupo")] public string Letter { get; set; } = string.Empty;
    [Display(Name = "Cantidad de estudiantes")] public int? StudentCount { get; set; }
    [Display(Name = "Factor de escala")] public int? ScalingFactor { get; set; }
    [Display(Name = "Laboratorio")] public int? LaboratoryId { get; set; }
    public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
    public List<StudentGroupFormSubject> Subjects { get; set; } = [];

    public StudentGroupInput ToServiceInput() => new(
        ManagementId, CareerId, Semester, Letter, StudentCount, ScalingFactor, LaboratoryId, Status,
        Subjects.Where(s => s.Selected)
            .Select(s => new StudentGroupSubjectInput(s.SubjectId, s.TeacherPersonId ?? 0, s.LaboratoryId))
            .ToList());
}

public sealed class StudentGroupFormSubject
{
    public int SubjectId { get; set; }
    public bool Selected { get; set; }
    public int? TeacherPersonId { get; set; }
    public int? LaboratoryId { get; set; }
}

/// <summary>Modelo del partial _StudentGroupForm.</summary>
/// <param name="ConfigJson">Catálogos y estado inicial para wwwroot/js/student-group-form.js (JSON con caracteres HTML escapados).</param>
public sealed record StudentGroupFormView(
    StudentGroupFormInput Input,
    StudentGroupFormOptions Options,
    bool IsEdit,
    string? ManagementLabel,
    IReadOnlyList<AcademicManagementSummary> Managements,
    string? CareerName,
    string CancelUrl,
    string ConfigJson);

/// <summary>Catálogos del formulario serializados para el JS (materias por carrera/semestre, docentes, laboratorios).</summary>
public sealed class StudentGroupFormOptions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record Option(int Id, string Name);
    public sealed record SubjectOption(int Id, string Name, string? Code, int CareerId, int Semester);
    /// <summary>Grupo ya existente en la gestión (para sugerir la siguiente letra y evitar duplicados).</summary>
    public sealed record ExistingGroup(int Id, int CareerId, int Semester, string Letter, string Name);

    public List<Option> Careers { get; init; } = [];
    public List<SubjectOption> Subjects { get; init; } = [];
    public List<Option> Teachers { get; init; } = [];
    public List<Option> Laboratories { get; init; } = [];
    public List<ExistingGroup> ExistingGroups { get; init; } = [];

    public string ToJson(StudentGroupFormInput input, int? editingGroupId) => JsonSerializer.Serialize(new
    {
        subjects = Subjects,
        teachers = Teachers,
        laboratories = Laboratories,
        existingGroups = ExistingGroups.Where(g => g.Id != editingGroupId),
        input = new
        {
            input.CareerId,
            input.Semester,
            input.LaboratoryId,
            subjects = input.Subjects
        },
        isEdit = editingGroupId.HasValue
    }, JsonOptions);

    /// <summary>Carga los catálogos: solo carreras con materias activas que tienen semestre.</summary>
    public static async Task<StudentGroupFormOptions> LoadAsync(ApplicationDbContext context, int managementId)
    {
        var subjects = await context.Subjects.AsNoTracking()
            .Where(s => s.Status == GeneralStatus.Activo && s.CareerId != null && s.Semester != null)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectOption(s.Id, s.Name, s.Code, s.CareerId!.Value, s.Semester!.Value))
            .ToListAsync();
        var careerIds = subjects.Select(s => s.CareerId).Distinct().ToList();

        return new StudentGroupFormOptions
        {
            Subjects = subjects,
            Careers = await context.Careers.AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo && careerIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new Option(c.Id, c.Name))
                .ToListAsync(),
            Teachers = await context.Interns.AsNoTracking()
                .Where(i => i.Category == PersonCategory.Docente && i.Status == GeneralStatus.Activo && i.InternStatus == GeneralStatus.Activo)
                .OrderBy(i => i.Name)
                .Select(i => new Option(i.Id, i.Name))
                .ToListAsync(),
            Laboratories = await context.Laboratories.AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .Select(l => new Option(l.Id, l.Code + " · " + l.Name))
                .ToListAsync(),
            ExistingGroups = await context.StudentGroups.AsNoTracking()
                .Where(g => g.ManagementId == managementId && g.Status != GeneralStatus.Eliminado)
                .Select(g => new ExistingGroup(g.Id, g.CareerId, g.Semester, g.Letter, g.Name))
                .ToListAsync()
        };
    }
}
