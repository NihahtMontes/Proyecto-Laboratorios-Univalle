using System.Security.Claims;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.Notifications;

/// <summary>Ámbito de la campana según la página: decide qué dominios se calculan.</summary>
public static class NotificationScopes
{
    public const string Dashboard = "dashboard";     // todo lo visible para el usuario
    public const string Maintenance = "maintenance";
    public const string Planning = "planning";
    public const string Acquisition = "acquisition";
    public const string Departure = "departure";
    public const string Request = "request";
    public const string All = "all";                 // centro de notificaciones
}

/// <summary>Grupos visuales de la campana y del centro de notificaciones.</summary>
public static class NotificationGroups
{
    public const string Overdue = "vencidos";
    public const string Today = "hoy";
    public const string Upcoming = "proximos";
    public const string Preventive = "preventivo";     // próximo mantenimiento sugerido por Kardex/L-48
    public const string Planning = "planificacion";
    public const string Departures = "prestamos";
    public const string Acquisitions = "adquisiciones";
    public const string Other = "otros";

    public static readonly string[] Order = [Overdue, Today, Upcoming, Preventive, Planning, Departures, Acquisitions, Other];
}

/// <summary>Dominio funcional de origen de un aviso.</summary>
public static class NotificationDomains
{
    public const string Maintenance = "maintenance";
    public const string Planning = "planning";
    public const string Departure = "departure";
    public const string Acquisition = "acquisition";
    public const string General = "general";
}

/// <summary>Urgencia: danger (vencido), warning (hoy / requiere acción), info (próximo), success (seguimiento), secondary.</summary>
public static class NotificationUrgency
{
    public const string Danger = "danger";
    public const string Warning = "warning";
    public const string Info = "info";
    public const string Success = "success";
    public const string Secondary = "secondary";

    public static int Rank(string urgency) => urgency switch
    {
        Danger => 0,
        Warning => 1,
        Info => 2,
        Success => 3,
        _ => 4
    };
}

/// <summary>Qué dominios puede ver el usuario: coincide con la autorización de las páginas destino.</summary>
public sealed record NotificationAudience(bool CanSeeMaintenance, bool CanSeeDepartures, bool CanSeeAcquisitions, bool CanSeePlanning)
{
    public static NotificationAudience Everything { get; } = new(true, true, true, true);

    public static NotificationAudience FromPrincipal(ClaimsPrincipal user)
    {
        bool InAny(string roles) => roles.Split(',').Any(user.IsInRole);
        return new NotificationAudience(
            CanSeeMaintenance: InAny(AuthorizationHelper.AdminRoles),
            CanSeeDepartures: InAny(AuthorizationHelper.AdminRoles),
            CanSeeAcquisitions: InAny(AuthorizationHelper.ManagementRoles),
            CanSeePlanning: InAny(AuthorizationHelper.ManagementRoles));
    }
}

/// <param name="UserId">Destinatario de los avisos persistidos.</param>
/// <param name="Today">Fecha local de referencia (la página pasa DateTime.Today; las pruebas fijan la fecha).</param>
/// <param name="Scope">Uno de <see cref="NotificationScopes"/>; null o vacío equivale a Dashboard.</param>
/// <param name="ManagementId">Gestión del wizard: filtra mantenimientos, préstamos, adquisiciones y persistidos. No filtra planificación (el material es compartido).</param>
/// <param name="Take">Máximo de ítems devueltos en <see cref="NotificationFeed.Items"/>; los conteos siempre son totales.</param>
public sealed record NotificationQuery(
    int UserId,
    DateTime Today,
    string? Scope,
    int? ManagementId,
    ManagementType? ManagementType,
    NotificationAudience Audience,
    string? Filter = null,
    int Take = 30);

public sealed class NotificationItem
{
    /// <summary>Positivo = persistido (tabla Notifications). Negativo = calculado en el momento (no se guarda).</summary>
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    /// <summary>Texto relativo ya formateado: "Hoy", "Mañana", "En 3 días", "Vencido hace 2 día(s)", "dd/MM HH:mm".</summary>
    public string Time { get; set; } = string.Empty;
    public DateTime SortDate { get; set; }
    /// <summary>Clave estable de un aviso calculado (p. ej. "mait-15"); el cliente la usa para posponerlo. Null en persistidos.</summary>
    public string? SyntheticKey { get; set; }
    public string Group { get; set; } = NotificationGroups.Other;
    public string Urgency { get; set; } = NotificationUrgency.Info;
    public string Domain { get; set; } = NotificationDomains.General;
    public bool IsPersisted => Id > 0;
    public bool IsRead { get; set; }
}

/// <param name="Count">Total de avisos pendientes (persistidos no leídos + calculados), sin truncar.</param>
/// <param name="CriticalCount">Total de avisos con urgencia danger o warning (vencidos y de hoy), sin truncar.</param>
/// <param name="GroupCounts">Total por grupo (<see cref="NotificationGroups"/>), sin truncar.</param>
/// <param name="Items">Avisos ordenados por urgencia y luego por fecha, truncados a Query.Take.</param>
public sealed record NotificationFeed(
    int Count,
    int CriticalCount,
    IReadOnlyDictionary<string, int> GroupCounts,
    IReadOnlyList<NotificationItem> Items);

/// <summary>
/// Fuente única de avisos: combina los persistidos del usuario con los calculados en tiempo real
/// (mantenimientos vencidos/de hoy/próximos, próximo preventivo sugerido, préstamos, adquisiciones y planificación).
/// Los calculados aparecen "en su momento" porque se derivan de las fechas en cada consulta; no requieren tareas en segundo plano.
/// </summary>
public interface INotificationAlertService
{
    Task<NotificationFeed> GetFeedAsync(NotificationQuery query, CancellationToken cancellationToken = default);

    /// <summary>Historial de avisos persistidos del usuario (leídos y no leídos), más recientes primero.</summary>
    Task<IReadOnlyList<NotificationItem>> GetHistoryAsync(int userId, int take = 100, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>Avisos persistidos de mantenimiento dirigidos al técnico responsable.</summary>
public interface IMaintenanceNotificationWriter
{
    /// <summary>
    /// Crea o actualiza el aviso "Mantenimiento asignado" para el usuario cuyo Email coincide con el del técnico.
    /// Si el mantenimiento está completado/cancelado, marca como leídos sus avisos. Nunca lanza excepciones (registra un warning).
    /// </summary>
    Task NotifyAssignmentAsync(int maintenanceId, CancellationToken cancellationToken = default);
}
