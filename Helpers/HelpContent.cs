namespace Proyecto_Laboratorios_Univalle.Helpers;

/// <summary>
/// Fuente única de los textos de ayuda (docs/ux/GLOSARIO.md): glosario, recuadros «¿Qué hago aquí?» por pantalla
/// (Pages/Shared/_PageHelp.cshtml), iconos «?» junto a conceptos (Pages/Shared/_HelpHint.cshtml) y la página /Ayuda.
/// Los códigos de formato institucional (L-3, L-7…) solo aparecen aquí, en la columna Format del glosario.
/// </summary>
public static class HelpContent
{
    public sealed record Term(string Key, string Name, string Definition, string? Format = null, string Area = "General");
    public sealed record PageHelp(string Title, IReadOnlyList<string> Steps, string? Tip = null);
    public sealed record RoleGuide(string Role, string Icon, IReadOnlyList<string> Steps);
    public sealed record Faq(string Question, string Answer);

    public static readonly IReadOnlyList<Term> Glossary =
    [
        // Mantenimiento de equipos
        new("management", "Gestión de mantenimiento", "Periodo semestral (por ejemplo 2026-2) en el que se planifica y registra el mantenimiento. Puede haber una gestión preventiva y una correctiva activas a la vez.", "L-48", "Mantenimiento de equipos"),
        new("preventive", "Mantenimiento preventivo", "Revisión planificada de los equipos elegidos para la gestión. Se hace con el asistente, paso a paso.", null, "Mantenimiento de equipos"),
        new("corrective", "Mantenimiento correctivo", "Atención de una falla reportada. Empieza con una solicitud de servicio técnico.", null, "Mantenimiento de equipos"),
        new("wizard", "Asistente", "Guía paso a paso que muestra en qué etapa está cada equipo y qué formulario falta completar.", null, "Mantenimiento de equipos"),
        new("verification", "Verificación de equipos", "Revisión del estado de los equipos de un laboratorio (bueno, regular, malo o baja). Si un equipo falla, pasa a la solicitud de servicio técnico.", "L-6", "Mantenimiento de equipos"),
        new("service-request", "Solicitud de servicio técnico", "Pedido de reparación o mantenimiento de un equipo con falla, con su checklist técnico.", "L-7", "Mantenimiento de equipos"),
        new("maintenance-record", "Registro de mantenimiento", "Acta de lo realizado: técnico, tareas, costos y estado final del equipo.", "L-8", "Mantenimiento de equipos"),
        new("equipment-departure", "Salida de equipos", "Registro de los equipos que salen del laboratorio (mantenimiento externo o préstamo) y de su devolución.", "L-3", "Mantenimiento de equipos"),
        new("maintenance-closure", "Cierre del mantenimiento", "Registro final: fechas, costo real, satisfacción y próximo mantenimiento. Alimenta el historial del equipo.", "Kardex", "Mantenimiento de equipos"),
        new("equipment-history", "Historial del equipo", "Todo lo ocurrido con una unidad física: verificaciones, solicitudes, mantenimientos y costos.", "Kardex", "Mantenimiento de equipos"),
        new("purchase-request", "Solicitud de compra", "Trámite administrativo para comprar repuestos o insumos usados en el mantenimiento.", "L-12", "Mantenimiento de equipos"),
        new("maintenance-schedule", "Cronograma de mantenimiento", "Calendario de los mantenimientos planificados de la gestión.", "L-48", "Mantenimiento de equipos"),
        new("maintenance-tasks", "Tareas de mantenimiento", "Lista de tareas que el técnico marca al registrar el mantenimiento; el avance debe llegar al 100 % para completar el equipo.", "L-48", "Mantenimiento de equipos"),
        new("draft", "Borrador", "Guarda el avance de un formulario sin pasar al paso siguiente. Se retoma desde el asistente, en el mismo paso.", null, "Mantenimiento de equipos"),
        new("verification-state", "Estado de la verificación", "Bueno o excelente: el equipo funciona y no pasa a mantenimiento. Regular: funciona con observaciones. Malo: tiene una falla; descríbala y el equipo pasará a la solicitud de servicio técnico. Baja: el equipo ya no debería usarse; la baja definitiva se registra en Equipos.", null, "Mantenimiento de equipos"),
        new("technical-checklist", "Checklist técnico", "Los 13 puntos que se revisan antes de pedir el servicio. Viene precargado: ajústelo al equipo y confirme que lo revisó.", null, "Mantenimiento de equipos"),
        new("real-cost", "Costo real", "Costo final del mantenimiento en bolivianos (repuestos, mano de obra y servicios). Para cerrar el mantenimiento debe ser mayor a 0.", null, "Mantenimiento de equipos"),
        new("investment-code", "Código de inversión", "Código del activo para la compra. Lo entrega el área administrativa o financiera.", null, "Mantenimiento de equipos"),
        new("cost-center", "Centro de costos", "Unidad a la que se carga la compra. Lo indica el área administrativa.", null, "Mantenimiento de equipos"),
        new("equipment", "Equipo", "Tipo o modelo de equipo (por ejemplo «Horno combinado»).", null, "Equipos e inventario"),
        new("unit", "Unidad física", "Un equipo concreto, identificado por su número de inventario.", null, "Equipos e inventario"),
        new("stock", "Existencias", "Cantidad disponible de cada artículo por laboratorio.", null, "Equipos e inventario"),
        // Planificación académica
        new("academic-term", "Semestre académico", "El semestre en el que se planifican las prácticas (usa la misma gestión que el mantenimiento).", null, "Planificación académica"),
        new("planning", "Planificación", "Sesión de práctica de una materia y un grupo, con fecha, periodos de clase, laboratorio, docente y material.", null, "Planificación académica"),
        new("class-period", "Periodo de clase", "Franja horaria del día académico (por ejemplo «Periodo 2 · 08:35–09:25»).", null, "Planificación académica"),
        new("requirements", "Requerimientos", "Material que necesita la práctica.", null, "Planificación académica"),
        new("recipe", "Receta de la práctica", "Lista de artículos guardada en una práctica del catálogo; se carga en los requerimientos.", null, "Planificación académica"),
        new("kit", "Kit de la materia", "Lista base de artículos que se repite en todas las prácticas de una materia.", null, "Planificación académica"),
        new("availability", "Disponibilidad", "Revisión de stock y de choques antes de aprobar la planificación.", null, "Planificación académica"),
        new("material-delivery", "Entrega y devolución de material", "Registro del material entregado para la práctica y de lo que vuelve.", "L-3", "Planificación académica"),
        new("material-incident", "Incidente de material", "Daño o pérdida de material durante la práctica. Queda pendiente de reposición.", "L-5", "Planificación académica"),
        new("practice-closure", "Cierre de práctica", "Pantalla del día para entregar el material, recibirlo y finalizar la práctica.", null, "Planificación académica"),
        new("suspend-day", "Suspender día", "Suspende un día académico (feriado, evento) y deja sus prácticas para reprogramar.", null, "Planificación académica"),
        new("reschedule", "Reprogramar", "Mover una práctica a otra fecha, periodo o laboratorio.", null, "Planificación académica"),
        new("demand", "Demanda de material", "Cuánto material piden las planificaciones frente al stock disponible.", null, "Planificación académica"),
    ];

    /// <summary>Recuadro «¿Qué hago aquí?» por pantalla (clave = carpeta.página, p. ej. "Managements.Index").</summary>
    public static readonly IReadOnlyDictionary<string, PageHelp> Pages = new Dictionary<string, PageHelp>
    {
        ["Managements.Index"] = new("Gestiones de mantenimiento",
        [
            "Cada gestión es un semestre (por ejemplo 2026-2). La preventiva agrupa los equipos que se revisarán; la correctiva recibe las fallas que se reporten.",
            "Para empezar un semestre, cree la gestión y luego elija sus equipos en «Planificar activos».",
            "Abra una gestión para ver su panel: avance por paso, cronograma y reportes.",
        ], "Solo puede haber una gestión preventiva y una correctiva activas a la vez."),
        ["Managements.Create"] = new("Nueva gestión de mantenimiento",
        [
            "Elija el tipo: preventiva (revisión planificada) o correctiva (fallas reportadas).",
            "Indique el año, el semestre y las fechas.",
            "Si es preventiva, al guardar elegirá los equipos que entran en la gestión.",
        ]),
        ["Managements.Details"] = new("Panel de la gestión",
        [
            "Las tarjetas muestran cuántos equipos hay en cada paso: verificación, solicitud, mantenimiento, salida, cierre y compra.",
            "Toque una tarjeta para abrir el asistente en ese paso.",
            "«Planificar activos» agrega o quita equipos; «Reportes» descarga los formatos institucionales en Excel.",
        ]),
        ["Managements.PlanAssets"] = new("Planificar activos",
        [
            "Filtre por categoría, laboratorio o estado para encontrar los equipos.",
            "Marque los equipos que entran en esta gestión y desmarque los que no.",
            "Pulse «Aplicar cambios de planificación». La selección aplica solo a la página visible.",
        ]),
        ["Wizard"] = new("Asistente de mantenimiento",
        [
            "El asistente lleva a cada equipo por los pasos del mantenimiento, en orden: verificación, solicitud, mantenimiento, salida, cierre y compra.",
            "En el paso 0 elija un laboratorio para ver su avance; luego avance con las flechas laterales.",
            "En cada paso, el botón de la derecha de cada equipo abre el formulario que falta completar.",
        ], "Cada equipo puede estar en un paso distinto: el asistente muestra en cada paso solo los equipos que están ahí."),
        ["Verifications.MassCreate"] = new("Verificación de equipos",
        [
            "Revise cada equipo del laboratorio y marque su estado.",
            "Si marca «Malo», describa la falla: el equipo pasará a la solicitud de servicio técnico.",
            "Guarde como borrador si no terminó, o registre la verificación para avanzar.",
        ]),
        ["Requests.Create"] = new("Solicitud de servicio técnico",
        [
            "Elija el equipo (si viene del asistente, ya está elegido).",
            "Describa la falla y revise el checklist técnico.",
            "Confirme que revisó el checklist y registre la solicitud: el equipo pasará al registro de mantenimiento.",
        ]),
        ["Maintenances.Create"] = new("Registro de mantenimiento",
        [
            "Asigne el técnico responsable: es obligatorio para la salida de equipos.",
            "Registre fechas, tareas realizadas y costos.",
            "Solo el estado «Completado», con todas las tareas al 100 %, hace avanzar el equipo; los otros estados guardan sin avanzar.",
        ]),
        ["Departures.MassCreate"] = new("Salida de equipos",
        [
            "Se listan los equipos del laboratorio que terminaron el mantenimiento y deben salir.",
            "Marque los equipos, indique las fechas de salida y de retorno, y registre la salida.",
            "Si un equipo no tiene técnico asignado, primero complete su registro de mantenimiento.",
        ]),
        ["Kardex.Create"] = new("Cierre del mantenimiento",
        [
            "Revise los datos del mantenimiento: vienen del registro anterior.",
            "Registre la devolución real del equipo, el costo real y las tareas finales.",
            "Al cerrar, el equipo pasa a la solicitud de compra.",
        ]),
        ["Acquisitions.Create"] = new("Solicitud de compra",
        [
            "Complete los datos de la compra de repuestos o insumos usados en el mantenimiento.",
            "El código de inversión y el centro de costos los entrega el área administrativa.",
            "Al registrarla, el mantenimiento del equipo queda completado.",
        ]),
        ["Equipment.Index"] = new("Equipos",
        [
            "Aquí están los tipos de equipo y sus unidades físicas (cada unidad tiene número de inventario).",
            "Use los filtros para buscar por nombre, laboratorio o estado.",
            "Abra un equipo para ver sus unidades y el historial de cada una.",
        ]),
        ["Equipment.Create"] = new("Nuevo equipo",
        [
            "Registre el tipo de equipo: nombre, clasificación y código de catálogo.",
            "Después agregue sus unidades físicas con número de inventario y laboratorio.",
        ]),
        ["Reports.Index"] = new("Reportes",
        [
            "Elija la gestión y, si quiere, un laboratorio.",
            "Descargue los consolidados o el formato de cada equipo en Excel: son los formatos institucionales.",
        ]),
    };

    public static readonly IReadOnlyList<RoleGuide> RoleGuides =
    [
        new("Administrador y SuperAdmin", "mdi-shield",
        [
            "Cree la gestión del semestre en Mantenimiento de equipos › Gestiones.",
            "Elija los equipos de la gestión en «Planificar activos».",
            "Abra «Mantenimiento preventivo» y complete en el asistente los pasos de cada laboratorio.",
            "Para una falla imprevista use «Mantenimiento correctivo».",
            "Descargue los formatos institucionales en «Reportes».",
        ]),
        new("Supervisor (coordinación académica)", "mdi-account-star",
        [
            "Revise los avisos del «Panel de planificación».",
            "Cree una planificación: materia, práctica, laboratorio, docente, fecha y periodos de clase.",
            "Cargue el material en «Requerimientos» y revise la «Disponibilidad» antes de aprobar.",
            "Para reportar la falla de un equipo use «Mantenimiento correctivo».",
        ]),
        new("Cierre de práctica (auxiliar)", "mdi-clipboard-check",
        [
            "Abra «Cierre de práctica»: muestra las prácticas del día.",
            "Registre la entrega del material al empezar.",
            "Al terminar, registre la devolución; si algo falta o se dañó, registre un incidente de material.",
            "Finalice la práctica cuando todo esté devuelto o cubierto por un incidente.",
        ]),
    ];

    public static readonly IReadOnlyList<Faq> Faqs =
    [
        new("¿Por qué no veo una pantalla del menú?", "El menú muestra solo lo que su rol puede usar. Si necesita otra pantalla, pida acceso al administrador del sistema."),
        new("¿Qué es un borrador?", "Guarda el avance de un formulario sin pasar al paso siguiente. Lo retoma desde el asistente, en el mismo paso."),
        new("¿Dónde están los formatos L-3, L-6, L-7 y los demás?", "Son los formatos institucionales en Excel: se descargan en «Reportes» y en el detalle de cada registro. En la aplicación se llaman por su nombre; la tabla del glosario indica a qué formato corresponde cada uno."),
        new("Me equivoqué en un paso anterior del mantenimiento", "Abra el formulario del paso actual del equipo desde el asistente y use «Rehacer paso anterior». Si no aparece, pida ayuda al administrador."),
        new("¿Qué significa el icono «?»", "Al tocarlo aparece una explicación corta del concepto que acompaña."),
    ];

    private static readonly Dictionary<string, Term> TermsByKey = Glossary.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);

    public static Term? GetTerm(string key) => TermsByKey.GetValueOrDefault(key);

    public static PageHelp? GetPage(string key) => Pages.GetValueOrDefault(key);
}
