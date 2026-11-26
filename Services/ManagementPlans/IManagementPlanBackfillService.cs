using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.ManagementPlans
{
    /// <summary>
    /// Reconstruye los planes L-48 (<see cref="Models.ManagementPlan"/>) a partir de los registros
    /// ya existentes (verificaciones L-6, solicitudes L-7/L-12, mantenimientos L-8 y salidas L-3),
    /// sin modificar ningún registro existente. Es idempotente: los pares gestión-equipo que ya
    /// tienen plan se omiten.
    /// </summary>
    public interface IManagementPlanBackfillService
    {
        /// <summary>
        /// Vista previa por gestión: planes existentes, equipos candidatos, planes a crear y su
        /// desglose por <see cref="WizardEquipmentState"/>. No escribe nada.
        /// </summary>
        Task<PlanBackfillPreview> PreviewAsync(int? managementId, CancellationToken ct = default);

        /// <summary>
        /// Crea los planes faltantes (filtro opcional por gestión) dentro de una sola transacción,
        /// en lotes de 100, y deja traza en un <see cref="Models.ImportBatch"/> con código
        /// PLANES-yyyyMMddHHmmss. No modifica ningún registro existente.
        /// </summary>
        Task<PlanBackfillResult> ApplyAsync(int? managementId, int? userId, CancellationToken ct = default);
    }

    /// <summary>Vista previa de la reconstrucción: una fila por gestión más totales.</summary>
    public sealed class PlanBackfillPreview
    {
        public List<PlanBackfillManagementSummary> Managements { get; } = [];

        public int ManagementsCount => Managements.Count;
        public int TotalExistingPlans => Managements.Sum(m => m.ExistingPlans);
        public int TotalCandidateUnits => Managements.Sum(m => m.CandidateUnits);
        public int TotalNewPlans => Managements.Sum(m => m.NewPlans);
    }

    /// <summary>Resumen de una gestión dentro de la vista previa.</summary>
    public sealed class PlanBackfillManagementSummary
    {
        public int ManagementId { get; init; }
        public string Code { get; init; } = string.Empty;
        public ManagementType Type { get; init; }

        /// <summary>Planes L-48 ya registrados en la gestión (con unidad de equipo).</summary>
        public int ExistingPlans { get; init; }

        /// <summary>Pares gestión-equipo con registros válidos (verificación, solicitud, mantenimiento o salida).</summary>
        public int CandidateUnits { get; init; }

        /// <summary>Candidatos sin plan: lo que crearía <see cref="IManagementPlanBackfillService.ApplyAsync"/>.</summary>
        public int NewPlans { get; init; }

        /// <summary>Desglose de los planes a crear por estado del equipo en el wizard.</summary>
        public IReadOnlyDictionary<WizardEquipmentState, int> NewPlansByState { get; init; }
            = new Dictionary<WizardEquipmentState, int>();
    }

    /// <summary>Resultado de la reconstrucción.</summary>
    public sealed class PlanBackfillResult
    {
        public bool Ok { get; init; }
        public string Message { get; init; } = string.Empty;
        public int CreatedPlans { get; init; }
        public int AffectedManagements { get; init; }
        public int? ImportBatchId { get; init; }
        public string? ImportBatchCode { get; init; }
    }
}
