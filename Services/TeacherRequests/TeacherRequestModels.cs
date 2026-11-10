namespace Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

/// <summary>Configuración de la ventana de solicitud (appsettings: <c>TeacherRequests</c>).</summary>
public sealed class TeacherRequestOptions
{
    public const string Section = "TeacherRequests";

    /// <summary>Anticipación mínima en días hábiles (lunes a viernes) para que el Economato prepare el material.</summary>
    public int MinLeadBusinessDays { get; set; } = 2;

    /// <summary>Hasta cuántos días hacia adelante se puede solicitar (nunca más allá del fin de la gestión).</summary>
    public int MaxLeadDays { get; set; } = 60;
}

/// <summary>Docente autenticado: usuario + persona del catálogo vinculada.</summary>
public sealed record TeacherProfile(int UserId, int PersonId, string Name);

/// <summary>Rango de fechas en el que el docente puede solicitar prácticas.</summary>
public sealed record RequestWindow(
    int? ManagementId,
    string? ManagementCode,
    DateTime MinDate,
    DateTime MaxDate,
    string? ClosedReason)
{
    public bool IsOpen => ClosedReason == null;

    public bool Contains(DateTime date) => IsOpen && date.Date >= MinDate && date.Date <= MaxDate;

    public string RangeText => $"del {MinDate:dd/MM/yyyy} al {MaxDate:dd/MM/yyyy}";
}

public sealed record RequestLineInput(int ArticleId, decimal Quantity);

/// <summary>Origen y cantidad que el revisor asigna a una línea al aprobar (V13: SolicitudL3Asignaciones).</summary>
public sealed record AssignmentInput(int ArticleId, int LaboratoryId, decimal Quantity);

/// <summary>Origen candidato de una línea: laboratorio activo con existencias del artículo y su cantidad libre.</summary>
public sealed record OriginOption(int LaboratoryId, string Code, string Name, decimal Free);

/// <summary>
/// Sugerencia de asignación de una línea: orígenes con su libre y el reparto sugerido (Economato primero;
/// lo que falte cuando no alcanza queda en el Economato).
/// </summary>
public sealed record AssignmentSuggestion(
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    decimal Quantity,
    bool IsConsumable,
    IReadOnlyList<OriginOption> Origins,
    IReadOnlyList<AssignmentInput> Suggested);

/// <summary>Práctica programada del docente (cronograma de la gestión) que todavía no tiene solicitud vigente.</summary>
public sealed record ProgrammedPractice(int ScheduleId, int CourseGroupId, int PracticeId, DateTime Date, int? WeekNumber,
    string Practice, string Subject, string Group, int? LaboratoryId, bool InWindow);

/// <summary>Control semanal: prácticas programadas (previstas) frente a las que ya tienen solicitud (recibidas).</summary>
public sealed record WeekCoverage(int? WeekNumber, DateTime From, DateTime To, int Expected, int Received, IReadOnlyList<string> Missing)
{
    public int MissingCount => Expected - Received;
}

/// <summary>Solicitud pendiente cuyo material se evalúa en lote junto con otras del mismo día.</summary>
public sealed record MaterialDayRequest(int PlanningId, int StartPeriodId, int PeriodCount, IReadOnlyList<RequestLineInput> Lines);

/// <summary>Resultado del material de una solicitud evaluada en lote (vease <see cref="MaterialDayRequest"/>).</summary>
public sealed record DayMaterialCheck(int PlanningId, IReadOnlyList<MaterialLineCheck> Lines);

/// <summary>Datos de una solicitud (alta o corrección de una observada).</summary>
public sealed class RequestInput
{
    public int CourseGroupId { get; set; }
    public int PracticeId { get; set; }
    public DateTime Date { get; set; }
    public int StartPeriodId { get; set; }
    public int PeriodCount { get; set; } = 2;
    public int LaboratoryId { get; set; }
    public int? StudentCount { get; set; }
    public string? Observations { get; set; }
    public List<RequestLineInput> Lines { get; set; } = [];
}

public enum CheckSeverity
{
    /// <summary>Impide enviar o aprobar.</summary>
    Block,
    /// <summary>Se puede continuar, pero conviene saberlo.</summary>
    Warning,
    /// <summary>Informativo.</summary>
    Info
}

public sealed record CheckIssue(string Code, CheckSeverity Severity, string Title, string Message);

public sealed record SlotCheckResult(IReadOnlyList<CheckIssue> Issues)
{
    public bool HasBlocks => Issues.Any(i => i.Severity == CheckSeverity.Block);
    public IEnumerable<CheckIssue> Blocks => Issues.Where(i => i.Severity == CheckSeverity.Block);
    public IEnumerable<CheckIssue> Warnings => Issues.Where(i => i.Severity == CheckSeverity.Warning);
}

public enum MaterialState
{
    /// <summary>Alcanza con holgura.</summary>
    Ok,
    /// <summary>Alcanza justo (no sobra nada en ese horario).</summary>
    Tight,
    /// <summary>En ese horario no alcanza (lo usan otras prácticas o el stock total es menor).</summary>
    Short,
    /// <summary>El artículo no tiene stock registrado: lo verificará el revisor.</summary>
    NoStock
}

/// <summary>Otra práctica aprobada que usa el artículo en un horario que se cruza con el pedido.</summary>
public sealed record MaterialUse(int PlanningId, string Code, string Label, TimeSpan? Start, TimeSpan? End, decimal Quantity)
{
    public string TimeText => Start.HasValue ? $"{Start.Value:hh\\:mm}–{(End ?? Start.Value):hh\\:mm}" : "todo el día";
}

public sealed record MaterialLineCheck(
    int ArticleId,
    string Code,
    string Name,
    string? Category,
    decimal Requested,
    decimal? Stock,
    decimal UsedByOthers,
    decimal? Free,
    MaterialState State,
    IReadOnlyList<MaterialUse> Uses,
    string Explanation)
{
    public string StateText => State switch
    {
        MaterialState.Ok => "Disponible",
        MaterialState.Tight => "Justo",
        MaterialState.Short => "No alcanza",
        _ => "Sin stock registrado"
    };

    public string StateCss => State switch
    {
        MaterialState.Ok => "success",
        MaterialState.Tight => "warning",
        MaterialState.Short => "danger",
        _ => "secondary"
    };
}

/// <summary>Horario libre sugerido por el buscador.</summary>
public sealed record SlotSuggestion(
    DateTime Date,
    int StartPeriodId,
    int StartOrder,
    int PeriodCount,
    int LaboratoryId,
    string LaboratoryCode,
    string LaboratoryName,
    TimeSpan Start,
    TimeSpan End,
    int MaterialIssues,
    IReadOnlyList<string> Reasons);

public sealed record TeacherOpResult(bool Ok, string Message, int? PlanningId = null, IReadOnlyList<string>? Warnings = null)
{
    public static TeacherOpResult Fail(string message) => new(false, message);
}

/// <summary>Línea de una solicitud con lo pedido por el docente y lo aprobado (si el revisor la cambió).</summary>
public sealed record RequestLineView(int RequirementId, int ArticleId, string Code, string Name, string? Category, string? Unit,
    decimal Quantity, decimal? RequestedByTeacher)
{
    public bool ChangedByReviewer => RequestedByTeacher.HasValue && RequestedByTeacher.Value != Quantity;
}

/// <summary>Evento del historial de una solicitud (línea de tiempo).</summary>
public sealed record ReviewEventView(int Id, Models.PlanningReviewAction Action, string ActionText, string FromStatus, string ToStatus,
    string? Comment, string UserName, DateTime Date, IReadOnlyList<ReviewLineView> Lines);

public sealed record ReviewLineView(string ArticleName, decimal? PreviousQuantity, decimal? NewQuantity);
