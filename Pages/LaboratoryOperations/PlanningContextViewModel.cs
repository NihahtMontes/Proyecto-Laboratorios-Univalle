using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Franja de contexto que encabeza todos los pasos del wizard de la planificación
/// (partial _PlanningContext): gestión académica, código PRA y paso actual.
/// </summary>
public sealed record PlanningContextViewModel(
    AcademicManagementSummary? Management,
    int? PlanningId,
    string Title,
    int Step)
{
    /// <summary>Las escrituras solo se permiten en gestiones activas o inactivas; las completadas solo se consultan.</summary>
    public bool IsReadOnly => Management is { IsEditable: false };

    /// <summary>Pasos del wizard de planificación. L-3 y L-5 son el cierre de la sesión (proceso aparte, Step 4/5).</summary>
    public static readonly string[] StepNames = ["", "Planificación", "Requerimientos", "Disponibilidad"];

    public const int StepCount = 3;

    /// <summary>True en L-3/L-5: no forman parte del wizard de planificación.</summary>
    public bool IsClosingStep => Step > StepCount;
}

public static class PlanningContextLoader
{
    /// <summary>Contexto de una planificación existente (pasos 1 a 5): carga la gestión a la que pertenece.</summary>
    public static async Task<PlanningContextViewModel> ForPlanningAsync(
        ApplicationDbContext context, IAcademicManagementService managements, int planningId, int step)
    {
        var managementId = await context.Plannings.AsNoTracking()
            .Where(p => p.Id == planningId)
            .Select(p => (int?)p.ManagementId)
            .FirstOrDefaultAsync();
        var summary = managementId.HasValue ? await managements.GetSummaryAsync(managementId.Value) : null;
        return new PlanningContextViewModel(summary, planningId, $"Planificación {L3Codes.Format(planningId)}", step);
    }

    /// <summary>True si la planificación existe y su gestión admite cambios.</summary>
    public static Task<bool> IsWritableAsync(IAcademicManagementService managements, int planningId) =>
        managements.IsPlanningWritableAsync(planningId);

    public const string ReadOnlyMessage = AcademicManagementService.ReadOnlyMessage;
}
