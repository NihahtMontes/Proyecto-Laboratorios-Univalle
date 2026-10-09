namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Códigos visibles del módulo a partir de datos persistidos.</summary>
public static class L3Codes
{
    /// <summary>Código de una planificación (Plannings.Id).</summary>
    public static string Format(int planningId) => $"PRA-{planningId:00000}";
}
