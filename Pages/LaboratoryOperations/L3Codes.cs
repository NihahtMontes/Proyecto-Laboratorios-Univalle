namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Códigos visibles del módulo a partir de datos persistidos.</summary>
public static class L3Codes
{
    /// <summary>Código de una planificación (Plannings.Id).</summary>
    public static string Format(int planningId) => $"PRA-{planningId:00000}";

    /// <summary>
    /// Código visible de una planificación: el persistido (p. ej. "SOL3-0565" del Excel institucional);
    /// si no tiene, el derivado del Id (PRA-00001).
    /// </summary>
    public static string Format(int planningId, string? code) =>
        string.IsNullOrWhiteSpace(code) ? Format(planningId) : code.Trim();
}
