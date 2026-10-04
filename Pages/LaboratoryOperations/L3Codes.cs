namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Códigos visibles del módulo a partir de datos persistidos.</summary>
public static class L3Codes
{
    /// <summary>Código de una práctica (L3Requests.Id).</summary>
    public static string Format(int practiceId) => $"PRA-{practiceId:00000}";

    /// <summary>Código de una planificación: gestión + carrera.</summary>
    public static string Planning(string? managementCode, string? careerCode, int planningId) =>
        string.IsNullOrWhiteSpace(careerCode)
            ? $"{managementCode} · PLAN-{planningId}"
            : $"{managementCode} · {careerCode}";
}
