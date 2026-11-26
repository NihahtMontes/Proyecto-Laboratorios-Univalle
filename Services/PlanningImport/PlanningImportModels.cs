namespace Proyecto_Laboratorios_Univalle.Services.PlanningImport;

/// <summary>Gravedad de un hallazgo de la revisión de una fila del Excel de planificación.</summary>
public enum ImportIssueLevel
{
    /// <summary>El dato no sirve (no existe, falta o está fuera de la gestión): la fila no se puede cargar.</summary>
    Error,
    /// <summary>La fila choca con otra práctica (laboratorio, docente, estudiantes) o cae en un día suspendido.</summary>
    Clash,
    /// <summary>Aviso: no impide la carga.</summary>
    Warning
}

public sealed record ImportIssue(ImportIssueLevel Level, string Column, string Message);

/// <summary>Fila de la hoja Programacion tal como vino (todo texto; la fecha ya normalizada a yyyy-MM-dd si se pudo leer).</summary>
public sealed class ImportRawRow
{
    public int RowNumber { get; set; }
    public string Management { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Group { get; set; } = "";
    public string Practice { get; set; } = "";
    public string Week { get; set; } = "";
    public string Date { get; set; } = "";
    public string StartPeriod { get; set; } = "";
    public string PeriodCount { get; set; } = "";
    public string Laboratory { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Students { get; set; } = "";
    public string Observations { get; set; } = "";
    /// <summary>Avisos generados al leer (p. ej. cómo se repartieron los periodos del generador): se muestran como avisos de la fila.</summary>
    public List<string> Notes { get; set; } = [];
    /// <summary>Decisión inicial si la persona no eligió otra (p. ej. omitir las prácticas del generador con fecha dudosa).</summary>
    public string DefaultDecision { get; set; } = "";
}

/// <summary>Fila de la hoja Material_extra: material fuera de la receta para una fila de Programacion.</summary>
public sealed class ImportRawExtra
{
    public int RowNumber { get; set; }
    public string TargetRow { get; set; } = "";
    public string Article { get; set; } = "";
    public string Quantity { get; set; } = "";
}

/// <summary>Contenido leído del archivo (se guarda en caché entre los pasos del asistente).</summary>
public sealed class ImportFile
{
    public string FileName { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public List<ImportRawRow> Rows { get; set; } = [];
    public List<ImportRawExtra> Extras { get; set; } = [];
    /// <summary>Datos del generador de la Inge si el archivo es ese formato (null para la plantilla del sistema).</summary>
    public GeneratorWorkbook? Generator { get; set; }
}

public enum ImportRowState
{
    /// <summary>La práctica todavía no está planificada para ese grupo en la gestión.</summary>
    New,
    /// <summary>Ya existe una planificación de esa práctica y grupo (hay que decidir: sobrescribir u omitir).</summary>
    Existing,
    /// <summary>Ya existe y con los mismos datos: no hay nada que hacer.</summary>
    Identical
}

public enum ImportRowAction
{
    Create,
    Overwrite,
    /// <summary>Omitida por decisión de la persona.</summary>
    Skip,
    /// <summary>Ya estaba cargada igual: se deja como está.</summary>
    Unchanged,
    /// <summary>Ya existe y falta decidir si se sobrescribe o se omite.</summary>
    NeedsDecision,
    /// <summary>Tiene errores o choques: hay que corregir el Excel u omitirla.</summary>
    Blocked
}

public static class ImportDecisions
{
    public const string Skip = "skip";
    public const string Overwrite = "overwrite";
}

public sealed record ImportExtraLine(int ArticleId, string ArticleLabel, decimal Quantity);

/// <summary>Fila revisada: datos resueltos contra el sistema, hallazgos, situación y acción resultante.</summary>
public sealed class ImportRow
{
    public required ImportRawRow Raw { get; init; }
    public int RowNumber => Raw.RowNumber;

    public int? SubjectId { get; set; }
    public string? SubjectLabel { get; set; }
    public string? GroupName { get; set; }
    public int? CourseGroupId { get; set; }
    public int? StudentGroupId { get; set; }
    public int? ScalingFactor { get; set; }
    public int? PracticeId { get; set; }
    public string? PracticeLabel { get; set; }
    public string? PracticeNumber { get; set; }
    public DateTime? Date { get; set; }
    public int? AcademicWeekId { get; set; }
    public int? WeekNumber { get; set; }
    public int? StartOrder { get; set; }
    public int? StartPeriodId { get; set; }
    public int? PeriodCount { get; set; }
    public int? LaboratoryId { get; set; }
    public string? LaboratoryLabel { get; set; }
    public int? CampusId { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? Students { get; set; }

    public List<ImportIssue> Issues { get; } = [];
    public List<ImportExtraLine> ExtraLines { get; } = [];

    public ImportRowState State { get; set; } = ImportRowState.New;
    public int? ScheduleId { get; set; }
    public int? ExistingPlanningId { get; set; }
    public string? ExistingCode { get; set; }
    /// <summary>Dónde está hoy la planificación existente ("19/11/2026 · Periodos 3–4 · L1 · Docente").</summary>
    public string? ExistingSummary { get; set; }
    public DateTime? ExistingDate { get; set; }
    public int? ExistingLaboratoryId { get; set; }
    public bool CanOverwrite { get; set; }
    public string? OverwriteBlockReason { get; set; }

    public string Decision { get; set; } = "";
    public ImportRowAction Action { get; set; } = ImportRowAction.Create;

    public bool HasErrors => Issues.Any(i => i.Level == ImportIssueLevel.Error);
    public bool HasClashes => Issues.Any(i => i.Level == ImportIssueLevel.Clash);
    public bool HasWarnings => Issues.Any(i => i.Level == ImportIssueLevel.Warning);
    /// <summary>Tiene horario completo y resuelto: participa en los choques contra otras filas.</summary>
    public bool HasSlot => Date.HasValue && StartOrder.HasValue && PeriodCount.HasValue && LaboratoryId.HasValue;

    public string ResultText => Action switch
    {
        ImportRowAction.Create => "Se cargará (nueva)",
        ImportRowAction.Overwrite => "Se sobrescribirá " + ExistingCode,
        ImportRowAction.Skip => "Omitida",
        ImportRowAction.Unchanged => "Ya estaba cargada igual (" + ExistingCode + ")",
        ImportRowAction.NeedsDecision => "Ya está cargada (" + ExistingCode + "): falta decidir",
        _ => HasErrors ? "Con error" : "Con choque"
    };
}

public sealed class ImportAnalysis
{
    public int ManagementId { get; init; }
    public string ManagementCode { get; init; } = "";
    public string FileName { get; init; } = "";
    public List<ImportRow> Rows { get; init; } = [];
    /// <summary>Problemas de la hoja Material_extra que no se pudieron atribuir a una fila.</summary>
    public List<string> ExtraSheetIssues { get; init; } = [];
    /// <summary>Avisos del archivo completo que no bloquean (p. ej. semanas repetidas en el generador).</summary>
    public List<string> FileNotices { get; init; } = [];

    public int Count(ImportRowAction action) => Rows.Count(r => r.Action == action);
    public int CreateCount => Count(ImportRowAction.Create);
    public int OverwriteCount => Count(ImportRowAction.Overwrite);
    public int SkipCount => Count(ImportRowAction.Skip);
    public int UnchangedCount => Count(ImportRowAction.Unchanged);
    public int NeedsDecisionCount => Count(ImportRowAction.NeedsDecision);
    public int BlockedCount => Count(ImportRowAction.Blocked);
    public int WarningCount => Rows.Count(r => r.HasWarnings && r.Action is ImportRowAction.Create or ImportRowAction.Overwrite);

    /// <summary>Se puede confirmar: nada bloqueado ni sin decidir, y hay algo que cargar.</summary>
    public bool CanApply => BlockedCount == 0 && NeedsDecisionCount == 0 && CreateCount + OverwriteCount > 0;
}

public sealed record ImportApplyResult(bool Ok, string Message, int Created, int Overwritten, string? BatchCode,
    ImportAnalysis? Analysis, DateTime? FirstDate = null, int? FirstLaboratoryId = null);

/// <summary>Error de formato del archivo, con mensaje pensado para la persona que lo subió.</summary>
public sealed class ImportFileException : Exception
{
    public ImportFileException(string message) : base(message) { }
}
