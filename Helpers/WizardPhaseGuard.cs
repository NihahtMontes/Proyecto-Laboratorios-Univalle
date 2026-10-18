using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Guarda común de fase del wizard de mantenimiento (L-6 → L-7 → L-8 → L-3 → Kardex → L-12).
    /// Cada formulario que registra o edita una fase del plan debe llamar a <see cref="Validate"/>
    /// antes de cambiar nada: si el plan ya no está en esa fase (o ya está completado) el registro se
    /// rechaza con un mensaje claro en lugar de mover el plan hacia atrás o saltarse fases.
    /// </summary>
    public static class WizardPhaseGuard
    {
        /// <summary>Nombre visible (español) de la fase.</summary>
        public static string PhaseLabel(WizardPhase phase) => phase switch
        {
            WizardPhase.Verification => "L-6 Verificación",
            WizardPhase.TechnicalRequest => "L-7 Solicitud técnica",
            WizardPhase.Maintenance => "L-8 Mantenimiento",
            WizardPhase.Exit => "L-3 Salida",
            WizardPhase.Kardex => "Kardex",
            WizardPhase.Disbursement => "L-12 Desembolso",
            _ => phase.ToString()
        };

        /// <summary>
        /// Devuelve null si el plan puede operar en la fase <paramref name="expected"/>
        /// (o si no hay plan: registro suelto fuera del wizard). Si no, devuelve el mensaje de rechazo.
        /// Un plan ya completado (L-12 cerrado) no admite ninguna fase.
        /// </summary>
        public static string? Validate(ManagementPlan? plan, WizardPhase expected)
        {
            if (plan == null)
                return null;

            if (plan.CurrentState == WizardEquipmentState.Completed || plan.PlanStatus == ManagementPlanStatus.Completed)
            {
                return $"Este activo ya completó su ronda de mantenimiento; no se puede registrar {PhaseLabel(expected)}. " +
                       "Si necesita corregir algo, use «Rehacer fase» en el wizard.";
            }

            if (plan.CurrentPhase == expected)
                return null;

            return $"Este activo no está en la fase {PhaseLabel(expected)} (fase actual: {PhaseLabel(plan.CurrentPhase)}). " +
                   "No se registró nada. Si necesita corregir una fase anterior, use «Rehacer fase» en el wizard.";
        }
    }
}
