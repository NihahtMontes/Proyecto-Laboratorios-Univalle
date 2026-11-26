namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Nombres visibles canónicos de la aplicación (glosario de UX, docs/ux/GLOSARIO.md).
/// En pantalla se usa el nombre claro; los códigos de formato institucional (L-3, L-7…) vuelven
/// SOLO en el menú, los títulos de los listados y las tarjetas del panel en formato corto
/// «L6 · Verificaciones» (decisión del usuario 2026-11-16). Los códigos completos (L-3, L-7…)
/// siguen en los Excel de Services/Reports y en la página de Ayuda.
/// Un concepto = un nombre en menú, título, botones, pasos y mensajes.
/// </summary>
public static class UiTerms
{
    // Mantenimiento de equipos
    public const string ManagementSingular = "Gestión de mantenimiento";
    public const string ManagementPlural = "Gestiones de mantenimiento";
    public const string Preventive = "Mantenimiento preventivo";
    public const string Corrective = "Mantenimiento correctivo";
    public const string Wizard = "Asistente";
    public const string Dashboard = "Panel";
    public const string Verification = "Verificación de equipos";            // antes L-6
    public const string ServiceRequest = "Solicitud de servicio técnico";     // antes L-7
    public const string MaintenanceRecord = "Registro de mantenimiento";      // antes L-8
    public const string EquipmentDeparture = "Salida de equipos";             // antes L-3 (Departures)
    // Nombres cortos con código (decisión del usuario 2026-11-16): menú, títulos de listados y tarjetas del panel.
    public const string VerificationShort = "L6 · Verificaciones";
    public const string ServiceRequestShort = "L7 · Servicio técnico";
    public const string MaintenanceRecordShort = "L8 · Kardex";
    public const string DepartureShort = "L3 · Salidas";
    public const string Acquisition = "Adquisición";
    public const string MaintenanceClosure = "Cierre del mantenimiento";      // antes Kardex (paso 5)
    public const string EquipmentHistory = "Historial del equipo";            // antes Kardex de la unidad
    public const string PurchaseRequest = "Solicitud de compra";              // antes L-12 / Desembolso
    public const string MaintenanceSchedule = "Cronograma de mantenimiento";  // antes L-48
    public const string MaintenanceTasks = "Tareas de mantenimiento";         // antes Tareas L-48

    // Planificación académica
    public const string MaterialDelivery = "Entrega y devolución de material"; // antes L-3 (práctica)
    public const string MaterialIncident = "Incidente de material";            // antes L-5
    public const string SuspendDay = "Suspender día";                          // antes «Reponer día»
    public const string SuspendedDays = "Días suspendidos y reposiciones";     // antes «Reposiciones de día»
    public const string AcademicTerm = "Semestre académico";                   // antes «Gestión académica»
    public const string Laboratory = "Laboratorio";                            // antes «Ambiente»
    public const string ClassPeriod = "Periodo de clase";
    public const string ReschedulingReport = "Informe de reprogramaciones y reposiciones"; // L-9 (nuevo)
    public const string ReschedulingReportShort = "L9 · Reprogramaciones y reposiciones";  // L-9 en tarjetas y menú
    public const string MaterialChangesReport = "Cambios de material del docente";         // pedido del docente vs kit
    public const string MaterialChangesReportShort = "Cambios de material del docente";    // en tarjetas y menú

    // Generales
    public const string ErrorTitle = "No se pudo completar";                  // antes «¡Ups!»
    public const string BackToList = "Volver al listado";
    public const string BackToWizard = "Volver al asistente";
    public const string BackToPanel = "Volver al panel";
}
