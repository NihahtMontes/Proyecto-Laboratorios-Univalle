using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Franja de contexto que encabeza todos los pasos del wizard de la práctica
/// (partial _PlanningContext): planificación, código de práctica y paso actual.
/// </summary>
public sealed record PlanningContextViewModel(
    PlanningSummary? Planning,
    int? PracticeId,
    string PracticeTitle,
    int Step)
{
    /// <summary>Las escrituras solo se permiten en la planificación activa (o en prácticas legadas sin planificación).</summary>
    public bool IsReadOnly => Planning is { IsActive: false };

    public static readonly string[] StepNames = ["", "Práctica", "Requerimientos", "Disponibilidad", "L-3 Entrega y devolución", "L-5 Incidentes"];
}

public static class PlanningContextLoader
{
    /// <summary>Contexto de una práctica existente (paso 2-5).</summary>
    public static async Task<PlanningContextViewModel> ForPracticeAsync(
        ApplicationDbContext context, IPlanningService planning, int practiceId, int step)
    {
        var planningId = await context.L3Requests.AsNoTracking()
            .Where(r => r.Id == practiceId)
            .Select(r => r.AcademicPlanningId)
            .FirstOrDefaultAsync();
        var summary = planningId.HasValue ? await planning.GetSummaryAsync(planningId.Value) : null;
        return new PlanningContextViewModel(summary, practiceId, $"Práctica {L3Codes.Format(practiceId)}", step);
    }

    /// <summary>
    /// True si la práctica admite escrituras: pertenece a la planificación activa o es legada (sin planificación).
    /// </summary>
    public static async Task<bool> IsWritableAsync(ApplicationDbContext context, int practiceId)
    {
        var row = await context.L3Requests.AsNoTracking()
            .Where(r => r.Id == practiceId)
            .Select(r => new
            {
                r.AcademicPlanningId,
                Active = r.AcademicPlanning != null && r.AcademicPlanning.Status == Models.Enums.AcademicPlanningStatus.Active
            })
            .FirstOrDefaultAsync();
        return row != null && (!row.AcademicPlanningId.HasValue || row.Active);
    }

    public const string ReadOnlyMessage = "La planificación de esta práctica no está activa: solo se puede consultar.";
}
