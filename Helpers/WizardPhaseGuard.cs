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
            WizardPhase.Verification => "Verificación de equipos",
            WizardPhase.TechnicalRequest => "Solicitud de servicio técnico",
            WizardPhase.Maintenance => "Registro de mantenimiento",
            WizardPhase.Exit => "Salida de equipos",
            WizardPhase.Kardex => "Cierre del mantenimiento",
            WizardPhase.Disbursement => "Solicitud de compra",
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
                       "Si necesita corregir un paso anterior, abra el formulario del paso actual del equipo desde el asistente y use el botón «Rehacer paso anterior».";
            }

            if (plan.CurrentPhase == expected)
                return null;

            return $"Este activo no está en el paso {PhaseLabel(expected)} (paso actual: {PhaseLabel(plan.CurrentPhase)}). " +
                       "No se registró nada. Si necesita corregir un paso anterior, abra el formulario del paso actual del equipo desde el asistente y use el botón «Rehacer paso anterior».";
        }
    }
}
