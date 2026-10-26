namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Reglas puras del paso 1 de planificación y de la asignación de horario (O10): aforo del ambiente,
/// huella de los choques de docente y constancia en observaciones. Sin acceso a datos para poder
/// probarlas directamente.
/// </summary>
public static class PlanningAssignmentRules
{
    /// <summary>
    /// Mensaje de aforo excedido (mismo texto que la reprogramación, SlotValidationService).
    /// Null cuando no se puede evaluar (falta estudiantes o capacidad) o no hay exceso.
    /// </summary>
    public static string? CapacityError(int? students, int? capacity, string labCode)
        => students is int n && capacity is int cap && n > cap
            ? $"El grupo tiene {n} estudiantes y el laboratorio {labCode} admite {cap}."
            : null;

    /// <summary>
    /// Huella de un conjunto de choques: Ids de planificación distintos no nulos, ordenados y unidos
    /// por coma (vacío sin choques). Acompaña a la aceptación para insistir exactamente sobre esos
    /// choques: si aparece uno nuevo, la huella cambia y hay que volver a aceptar.
    /// </summary>
    public static string Fingerprint(IEnumerable<int?> planningIds)
        => string.Join(",", planningIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id));

    /// <summary>
    /// Concatena la nota de choque aceptado a las observaciones existentes. Si el resultado supera
    /// <paramref name="max"/> se recorta el TEXTO PREVIO (nunca la nota); si la nota sola no cabe,
    /// se recorta la nota a <paramref name="max"/>.
    /// </summary>
    public static string AppendTeacherConflictNote(string? observations, string note, int max = 1000)
    {
        if (note.Length >= max) return note[..max];
        var previous = string.IsNullOrWhiteSpace(observations) ? string.Empty : observations.Trim();
        if (previous.Length == 0) return note;

        const string separator = "\n";
        var previousBudget = max - separator.Length - note.Length;
        if (previous.Length > previousBudget) previous = previous[..previousBudget];
        return previous + separator + note;
    }
}
