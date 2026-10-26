using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Services.Notifications;

/// <summary>
/// Fuente única de avisos: combina los persistidos del usuario con los calculados en tiempo real.
/// Todos los cálculos usan consultas AsNoTracking con proyecciones ligeras; los conteos son totales
/// aunque la lista de ítems se trunque (máx. 20 ítems traídos por categoría).
/// </summary>
public sealed partial class NotificationAlertService : INotificationAlertService
{
    private const int MaxItemsPerCategory = 20;
    private const string ConflictCachePrefix = "notif-conflicts-";

    [GeneratedRegex(@"^/Maintenances/Details/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex MaintenanceUrlRegex();

    private readonly ApplicationDbContext _context;
    private readonly IArticleAvailabilityService _availability;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NotificationAlertService> _logger;

    public NotificationAlertService(
        ApplicationDbContext context,
        IArticleAvailabilityService availability,
        IMemoryCache cache,
        ILogger<NotificationAlertService> logger)
    {
        _context = context;
        _availability = availability;
        _cache = cache;
        _logger = logger;
    }

    // ==================== Filas proyectadas ====================

    private sealed class MaintenanceRow
    {
        public int Id { get; set; }
        public int ManagementId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public MaintenanceType MaintenanceType { get; set; }
        public string? InventoryNumber { get; set; }
        public string? EquipmentName { get; set; }
    }

    private sealed class LoanRow
    {
        public int Id { get; set; }
        public int ManagementId { get; set; }
        public DateTime EstimatedReturnDate { get; set; }
        public string? InventoryNumber { get; set; }
    }

    private sealed class AcquisitionRow
    {
        public int RequestId { get; set; }
        public int? ManagementId { get; set; }
        public DateTime Date { get; set; }
    }

    private sealed class PersistedRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public string? IconClass { get; set; }
        public string? Scope { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    /// <summary>
    /// Resultado de una categoría calculada: <see cref="Total"/> es el conteo sin truncar y
    /// <see cref="Items"/> trae como máximo 20 ítems (o todos, en categorías evaluadas en memoria).
    /// </summary>
    private sealed class CategoryResult
    {
        public CategoryResult(int total, List<NotificationItem> items, bool uniformUrgency)
        {
            Total = total;
            Items = items;
            UniformUrgency = uniformUrgency;
        }

        public int Total { get; set; }
        public List<NotificationItem> Items { get; }

        /// <summary>True si toda la categoría comparte urgencia: el total cuenta como crítico o no según esa urgencia.</summary>
        public bool UniformUrgency { get; }
    }

    // ==================== Feed ====================

    public async Task<NotificationFeed> GetFeedAsync(NotificationQuery query, CancellationToken cancellationToken = default)
    {
        var today = query.Today.Date;
        var scope = NormalizeScope(query.Scope);
        var filterTerm = query.Filter?.Trim();

        // Dominios que esta combinación de página + rol puede ver.
        var includeMaintenance = query.Audience.CanSeeMaintenance && scope
            is NotificationScopes.Dashboard or NotificationScopes.All or NotificationScopes.Maintenance;
        var includePlanning = query.Audience.CanSeePlanning && scope
            is NotificationScopes.Dashboard or NotificationScopes.All or NotificationScopes.Planning;
        var includeAcquisitions = query.Audience.CanSeeAcquisitions && scope
            is NotificationScopes.Dashboard or NotificationScopes.All or NotificationScopes.Acquisition or NotificationScopes.Request;
        var includeDepartures = query.Audience.CanSeeDepartures && scope
            is NotificationScopes.Dashboard or NotificationScopes.All or NotificationScopes.Departure;

        // ---------- Persistidos (la auto-resolución marca leídos y los saca del resultado) ----------
        var (persistedTotal, persistedItems, persistedGroupCounts, persistedMaintenancePaths) =
            await LoadPersistedAsync(query, scope, filterTerm, cancellationToken);

        // ---------- Calculados ----------
        var categories = new List<CategoryResult>();
        if (includeMaintenance)
        {
            categories.AddRange(await BuildMaintenanceCategoriesAsync(query, today, filterTerm, cancellationToken));
        }

        if (includeDepartures)
        {
            categories.AddRange(await BuildDepartureCategoriesAsync(query, today, filterTerm, cancellationToken));
        }

        if (includeAcquisitions)
        {
            categories.AddRange(await BuildAcquisitionCategoriesAsync(query, today, filterTerm, cancellationToken));
        }

        if (includePlanning)
        {
            categories.AddRange(await BuildPlanningCategoriesAsync(today, filterTerm, cancellationToken));
        }

        // Filtro de texto exacto sobre los calculados (Title/Message, sin distinguir mayúsculas).
        if (!string.IsNullOrWhiteSpace(filterTerm))
        {
            foreach (var category in categories)
            {
                var removed = category.Items.RemoveAll(i => !MatchesFilter(i, filterTerm));
                category.Total = Math.Max(category.Total - removed, category.Items.Count);
            }
        }

        // Un "próximo" calculado no debe duplicar el aviso persistido (asignación) del mismo mantenimiento
        // (comparación por la ruta sin querystring). Vencidos y de hoy se muestran siempre: son más urgentes.
        var computed = new List<NotificationItem>();
        foreach (var category in categories)
        {
            foreach (var item in category.Items)
            {
                if (item.Urgency == NotificationUrgency.Info
                    && item.Domain == NotificationDomains.Maintenance
                    && item.Url != null
                    && item.Url.StartsWith("/Maintenances/Details/", StringComparison.OrdinalIgnoreCase)
                    && persistedMaintenancePaths.Contains(item.Url.Split('?')[0]))
                {
                    category.Total--;
                    continue;
                }

                computed.Add(item);
            }
        }

        // ---------- Totales (siempre sin truncar) ----------
        var computedTotal = categories.Sum(c => Math.Max(c.Total, 0));
        var computedCritical = categories.Sum(c => c.UniformUrgency
            ? (IsCritical(c.Items.Count > 0 ? c.Items[0] : null) ? Math.Max(c.Total, 0) : 0)
            : c.Items.Count(IsCritical));

        var groupCounts = NewGroupCounts();
        foreach (var (group, count) in persistedGroupCounts)
        {
            groupCounts[group] += count;
        }
        foreach (var category in categories)
        {
            var group = category.Items.Count > 0 ? category.Items[0].Group : NotificationGroups.Other;
            groupCounts[group] += Math.Max(category.Total, 0);
        }

        var items = persistedItems
            .Concat(computed)
            .OrderBy(i => NotificationUrgency.Rank(i.Urgency))
            .ThenBy(i => i.IsPersisted ? 1 : 0)
            // Calculados: el más próximo/antiguo primero. Persistidos: el más reciente primero.
            .ThenBy(i => i.IsPersisted ? -i.SortDate.Ticks : i.SortDate.Ticks)
            .Take(Math.Max(query.Take, 0))
            .ToList();

        return new NotificationFeed(
            Count: persistedTotal + computedTotal,
            CriticalCount: computedCritical,
            GroupCounts: groupCounts,
            Items: items);
    }

    // ==================== Persistidos ====================

    private async Task<(int Total, List<NotificationItem> Items, Dictionary<string, int> GroupCounts, HashSet<string> MaintenancePaths)>
        LoadPersistedAsync(NotificationQuery query, string scope, string? filterTerm, CancellationToken cancellationToken)
    {
        var persistedQuery = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == query.UserId && !n.IsRead);

        persistedQuery = ApplyPersistedScope(persistedQuery, scope);

        if (query.ManagementId.HasValue)
        {
            var token = $"managementid={query.ManagementId.Value}";
            persistedQuery = persistedQuery.Where(n =>
                n.ManagementId == query.ManagementId.Value ||
                (n.ManagementId == null &&
                    (n.ActionUrl == null ||
                     !n.ActionUrl.ToLower().Contains("managementid=") ||
                     n.ActionUrl.ToLower().Contains(token))));
        }

        if (query.ManagementType.HasValue)
        {
            var typeToken = $"managementtype={query.ManagementType.Value.ToString().ToLower()}";
            persistedQuery = persistedQuery.Where(n =>
                n.ManagementType == query.ManagementType.Value ||
                (n.ManagementType == null &&
                    (n.ActionUrl == null ||
                     !n.ActionUrl.ToLower().Contains("managementtype=") ||
                     n.ActionUrl.ToLower().Contains(typeToken))));
        }

        if (!string.IsNullOrWhiteSpace(filterTerm))
        {
            var term = filterTerm.Trim().ToLower();
            persistedQuery = persistedQuery.Where(n =>
                n.Title.ToLower().Contains(term) ||
                n.Message.ToLower().Contains(term) ||
                (n.ActionUrl != null && n.ActionUrl.ToLower().Contains(term)) ||
                (n.IconClass != null && n.IconClass.ToLower().Contains(term)));
        }

        // ---------- Auto-resolución: mantenimientos inexistentes o cerrados retiran su aviso ----------
        var unreadRaw = await persistedQuery
            .Select(n => new { n.Id, n.ActionUrl })
            .ToListAsync(cancellationToken);

        var maintenanceIdByNotification = new Dictionary<int, int>();
        foreach (var row in unreadRaw)
        {
            if (row.ActionUrl == null) continue;
            var match = MaintenanceUrlRegex().Match(row.ActionUrl);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var maintenanceId))
            {
                maintenanceIdByNotification[row.Id] = maintenanceId;
            }
        }

        var excludedIds = new HashSet<int>();
        if (maintenanceIdByNotification.Count > 0)
        {
            var maintenanceIds = maintenanceIdByNotification.Values.Distinct().ToList();
            var statuses = await _context.Maintenances
                .AsNoTracking()
                .Where(m => maintenanceIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Status })
                .ToListAsync(cancellationToken);
            var statusById = statuses.ToDictionary(x => x.Id, x => x.Status);

            var deadMaintenanceIds = maintenanceIds
                .Where(id => !statusById.TryGetValue(id, out var status)
                    || status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled)
                .ToHashSet();
            if (deadMaintenanceIds.Count > 0)
            {
                var staleIds = maintenanceIdByNotification
                    .Where(kv => deadMaintenanceIds.Contains(kv.Value))
                    .Select(kv => kv.Key)
                    .ToList();
                var staleNotifications = await _context.Notifications
                    .AsTracking()
                    .Where(n => staleIds.Contains(n.Id))
                    .ToListAsync(cancellationToken);
                foreach (var notification in staleNotifications)
                {
                    notification.IsRead = true;
                }

                if (staleNotifications.Count > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                excludedIds = staleIds.ToHashSet();
            }
        }

        // Rutas de mantenimiento con aviso persistido vigente (para no duplicar los calculados).
        var maintenancePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in unreadRaw)
        {
            if (excludedIds.Contains(row.Id) || row.ActionUrl == null) continue;
            var path = row.ActionUrl.Split('?')[0];
            if (path.StartsWith("/Maintenances/Details/", StringComparison.OrdinalIgnoreCase))
            {
                maintenancePaths.Add(path);
            }
        }

        var total = unreadRaw.Count(r => !excludedIds.Contains(r.Id));

        var persistedTake = Math.Min(Math.Max(query.Take * 2, MaxItemsPerCategory), 100);
        var rows = await persistedQuery
            .Where(n => !excludedIds.Contains(n.Id))
            .OrderByDescending(n => n.CreatedAt)
            .Take(persistedTake)
            .Select(n => new PersistedRow
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                IconClass = n.IconClass,
                Scope = n.Scope,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(MapPersistedItem).ToList();

        // Totales por grupo (sin truncar), con el mismo filtro ya aplicado.
        var groupCounts = NewGroupCounts();
        var byScope = await persistedQuery
            .Where(n => !excludedIds.Contains(n.Id))
            .GroupBy(n => n.Scope)
            .Select(g => new { Scope = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        foreach (var entry in byScope)
        {
            groupCounts[MapPersistedGroup(entry.Scope)] += entry.Count;
        }

        return (total, items, groupCounts, maintenancePaths);
    }

    private static NotificationItem MapPersistedItem(PersistedRow row) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Message = row.Message,
        Url = row.ActionUrl,
        Icon = row.IconClass,
        SortDate = row.CreatedAt,
        Time = row.CreatedAt.ToLocalTime().ToString("dd/MM HH:mm"),
        Group = MapPersistedGroup(row.Scope),
        Urgency = NotificationUrgency.Info,
        Domain = MapPersistedDomain(row.Scope),
        IsRead = row.IsRead
    };

    /// <summary>Filtro de alcance de los persistidos. Para dashboard/all no se filtra por scope.</summary>
    private static IQueryable<Notification> ApplyPersistedScope(IQueryable<Notification> query, string scope)
    {
        if (scope is NotificationScopes.Dashboard or NotificationScopes.All)
        {
            return query;
        }

        if (scope == NotificationScopes.Planning)
        {
            return query.Where(n => n.Scope == NotificationScopes.Planning);
        }

        if (scope == NotificationScopes.Maintenance)
        {
            return query.Where(n =>
                n.Scope == NotificationScopes.Maintenance ||
                (n.Scope == null &&
                    (n.Title.ToLower().Contains("mantenimiento") ||
                     n.Message.ToLower().Contains("mantenimiento") ||
                     (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/maintenances")) ||
                     (n.IconClass != null && (n.IconClass.ToLower().Contains("wrench") || n.IconClass.ToLower().Contains("tools"))))));
        }

        if (scope == NotificationScopes.Acquisition)
        {
            return query.Where(n =>
                n.Scope == NotificationScopes.Acquisition ||
                (n.Scope == null &&
                    (n.Title.ToLower().Contains("adquis") ||
                     n.Message.ToLower().Contains("adquis") ||
                     n.Title.ToLower().Contains("compra") ||
                     n.Message.ToLower().Contains("compra") ||
                     (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/acquisitions")) ||
                     (n.IconClass != null && (n.IconClass.ToLower().Contains("shopping") || n.IconClass.ToLower().Contains("cart"))))));
        }

        if (scope == NotificationScopes.Request)
        {
            return query.Where(n =>
                n.Scope == NotificationScopes.Request ||
                (n.Scope == null &&
                    (n.Title.ToLower().Contains("solicitud") ||
                     n.Message.ToLower().Contains("solicitud") ||
                     (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/requests"))) &&
                    !n.Title.ToLower().Contains("adquis") &&
                    !n.Message.ToLower().Contains("adquis") &&
                    !n.Title.ToLower().Contains("compra") &&
                    !n.Message.ToLower().Contains("compra") &&
                    (n.IconClass == null || (!n.IconClass.ToLower().Contains("shopping") && !n.IconClass.ToLower().Contains("cart")))));
        }

        if (scope == NotificationScopes.Departure)
        {
            return query.Where(n =>
                n.Scope == NotificationScopes.Departure ||
                (n.Scope == null &&
                    (n.Title.ToLower().Contains("préstamo") ||
                     n.Title.ToLower().Contains("prestamo") ||
                     n.Title.ToLower().Contains("salida") ||
                     n.Message.ToLower().Contains("préstamo") ||
                     n.Message.ToLower().Contains("prestamo") ||
                     n.Message.ToLower().Contains("salida") ||
                     (n.ActionUrl != null && n.ActionUrl.ToLower().Contains("/departures")))));
        }

        return query;
    }

    private static string MapPersistedGroup(string? scope) => scope switch
    {
        NotificationScopes.Maintenance => NotificationGroups.Upcoming,
        NotificationScopes.Acquisition => NotificationGroups.Acquisitions,
        NotificationScopes.Departure => NotificationGroups.Departures,
        NotificationScopes.Planning => NotificationGroups.Planning,
        _ => NotificationGroups.Other
    };

    private static string MapPersistedDomain(string? scope) => scope switch
    {
        NotificationScopes.Maintenance => NotificationDomains.Maintenance,
        NotificationScopes.Planning => NotificationDomains.Planning,
        NotificationScopes.Departure => NotificationDomains.Departure,
        NotificationScopes.Acquisition or NotificationScopes.Request => NotificationDomains.Acquisition,
        _ => NotificationDomains.General
    };

    // ==================== A) Mantenimientos ====================

    private async Task<List<CategoryResult>> BuildMaintenanceCategoriesAsync(
        NotificationQuery query, DateTime today, string? filterTerm, CancellationToken cancellationToken)
    {
        var typeQuery = query.ManagementType.HasValue ? $"&ManagementType={query.ManagementType.Value}" : string.Empty;

        var baseQuery = _context.Maintenances
            .AsNoTracking()
            .Where(m => m.ScheduledDate != null
                && m.Status != MaintenanceStatus.Completed
                && m.Status != MaintenanceStatus.Cancelled);

        if (query.ManagementId.HasValue)
        {
            baseQuery = baseQuery.Where(m => m.ManagementId == query.ManagementId.Value);
        }

        var projected = baseQuery.Select(m => new MaintenanceRow
        {
            Id = m.Id,
            ManagementId = m.ManagementId,
            ScheduledDate = m.ScheduledDate!.Value,
            MaintenanceType = m.MaintenanceType,
            InventoryNumber = m.EquipmentUnit != null ? m.EquipmentUnit.InventoryNumber : null,
            EquipmentName = m.EquipmentUnit != null && m.EquipmentUnit.Equipment != null ? m.EquipmentUnit.Equipment.Name : null
        });

        // Coincidencia aproximada en servidor: los términos típicos viven en unidad/equipo
        // (el título es constante por categoría y el filtro exacto se aplica al final).
        if (!string.IsNullOrWhiteSpace(filterTerm))
        {
            var term = filterTerm.Trim().ToLower();
            projected = projected.Where(m =>
                term == "mantenimiento" ||
                (m.InventoryNumber != null && m.InventoryNumber.ToLower().Contains(term)) ||
                (m.EquipmentName != null && m.EquipmentName.ToLower().Contains(term)));
        }

        var results = new List<CategoryResult>();

        // Vencidos: sin límite inferior (el corte antiguo a 30 días era un bug).
        var overdueQuery = projected.Where(m => m.ScheduledDate < today);
        var overdueTotal = await overdueQuery.CountAsync(cancellationToken);
        var overdueRows = await overdueQuery
            .OrderBy(m => m.ScheduledDate)
            .Take(MaxItemsPerCategory)
            .ToListAsync(cancellationToken);
        results.Add(new CategoryResult(overdueTotal, overdueRows.Select(m => BuildMaintenanceItem(
            m, today, typeQuery,
            title: "Mantenimiento vencido",
            group: NotificationGroups.Overdue,
            urgency: NotificationUrgency.Danger,
            icon: "fas fa-exclamation-triangle text-danger")).ToList(), uniformUrgency: true));

        // De hoy.
        var todayQuery = projected.Where(m => m.ScheduledDate >= today && m.ScheduledDate < today.AddDays(1));
        var todayTotal = await todayQuery.CountAsync(cancellationToken);
        var todayRows = await todayQuery
            .OrderBy(m => m.ScheduledDate)
            .Take(MaxItemsPerCategory)
            .ToListAsync(cancellationToken);
        results.Add(new CategoryResult(todayTotal, todayRows.Select(m => BuildMaintenanceItem(
            m, today, typeQuery,
            title: "Mantenimiento para hoy",
            group: NotificationGroups.Today,
            urgency: NotificationUrgency.Warning,
            icon: "fas fa-calendar-day text-warning")).ToList(), uniformUrgency: true));

        // Próximos: hoy+1 .. hoy+7.
        var upcomingQuery = projected.Where(m => m.ScheduledDate > today && m.ScheduledDate <= today.AddDays(7));
        var upcomingTotal = await upcomingQuery.CountAsync(cancellationToken);
        var upcomingRows = await upcomingQuery
            .OrderBy(m => m.ScheduledDate)
            .Take(MaxItemsPerCategory)
            .ToListAsync(cancellationToken);
        results.Add(new CategoryResult(upcomingTotal, upcomingRows.Select(m => BuildMaintenanceItem(
            m, today, typeQuery,
            title: m.MaintenanceType switch
            {
                MaintenanceType.Preventivo => "Mantenimiento preventivo próximo",
                MaintenanceType.Correctivo => "Mantenimiento correctivo próximo",
                _ => "Mantenimiento próximo"
            },
            group: NotificationGroups.Upcoming,
            urgency: NotificationUrgency.Info,
            icon: "fas fa-tools text-info")).ToList(), uniformUrgency: true));

        // Próximo preventivo sugerido (Kardex L-48): dato del equipo, sin filtro de gestión.
        results.Add(await BuildPreventiveCategoryAsync(today, cancellationToken));

        return results;
    }

    private static NotificationItem BuildMaintenanceItem(
        MaintenanceRow m, DateTime today, string typeQuery, string title, string group, string urgency, string icon)
    {
        var date = m.ScheduledDate.Date;
        return new NotificationItem
        {
            Id = -1_000_000 - m.Id,
            Title = title,
            Message = $"Unidad {m.InventoryNumber ?? "S/N"} · {m.EquipmentName}: programado para el {date:dd/MM/yyyy}.",
            Url = $"/Maintenances/Details/{m.Id}?ManagementId={m.ManagementId}{typeQuery}",
            Icon = icon,
            SortDate = date,
            Time = RelativeDayLabel(today, date),
            SyntheticKey = $"mait-{m.Id}",
            Group = group,
            Urgency = urgency,
            Domain = NotificationDomains.Maintenance
        };
    }

    /// <summary>
    /// Kardex L-48: por unidad, el último mantenimiento completado con fecha sugerida. Se avisa si la fecha
    /// sugerida ya llegó (hoy+30) y todavía no existe un mantenimiento posterior de esa unidad (no cancelado,
    /// creado después o programado desde la fecha del completado).
    /// </summary>
    private async Task<CategoryResult> BuildPreventiveCategoryAsync(DateTime today, CancellationToken cancellationToken)
    {
        var horizon = today.AddDays(30);
        var completed = await _context.Maintenances
            .AsNoTracking()
            .Where(m => m.Status == MaintenanceStatus.Completed
                && m.SuggestedNextMaintenanceDate != null
                && m.SuggestedNextMaintenanceDate <= horizon)
            .Select(m => new
            {
                m.Id,
                m.EquipmentUnitId,
                ReferenceDate = m.EndDate ?? m.ScheduledDate ?? m.CreatedDate,
                SuggestedDate = m.SuggestedNextMaintenanceDate!.Value,
                InventoryNumber = m.EquipmentUnit != null ? m.EquipmentUnit.InventoryNumber : null,
                EquipmentName = m.EquipmentUnit != null && m.EquipmentUnit.Equipment != null ? m.EquipmentUnit.Equipment.Name : null
            })
            .ToListAsync(cancellationToken);

        // Último completado con sugerencia por unidad (evaluación completa: el total no se trunca).
        var latestByUnit = completed
            .GroupBy(c => c.EquipmentUnitId)
            .Select(g => g.OrderByDescending(c => c.ReferenceDate).First())
            .ToList();
        if (latestByUnit.Count == 0)
        {
            return new CategoryResult(0, [], uniformUrgency: false);
        }

        // ¿Ya se creó o programó un mantenimiento posterior de la unidad?
        var unitIds = latestByUnit.Select(c => c.EquipmentUnitId).ToList();
        var unitMaintenances = await _context.Maintenances
            .AsNoTracking()
            .Where(m => unitIds.Contains(m.EquipmentUnitId) && m.Status != MaintenanceStatus.Cancelled)
            .Select(m => new { m.Id, m.EquipmentUnitId, m.CreatedDate, m.ScheduledDate })
            .ToListAsync(cancellationToken);
        var byUnit = unitMaintenances
            .GroupBy(m => m.EquipmentUnitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<NotificationItem>();
        foreach (var completedMaintenance in latestByUnit)
        {
            // Con fecha programada manda esa fecha: CreatedDate de los importados históricos es la fecha de importación.
            var alreadyProgrammed = byUnit.TryGetValue(completedMaintenance.EquipmentUnitId, out var rows) && rows.Any(r =>
                r.Id != completedMaintenance.Id &&
                (r.ScheduledDate.HasValue
                    ? r.ScheduledDate.Value.Date >= completedMaintenance.ReferenceDate.Date
                    : r.CreatedDate > completedMaintenance.ReferenceDate));
            if (alreadyProgrammed)
            {
                continue;
            }

            var date = completedMaintenance.SuggestedDate.Date;
            var (title, urgency) = date < today
                ? ("Preventivo vencido según el historial del equipo", NotificationUrgency.Danger)
                : date <= today.AddDays(7)
                    ? ("Preventivo por programar", NotificationUrgency.Warning)
                    : ("Preventivo sugerido", NotificationUrgency.Info);

            items.Add(new NotificationItem
            {
                Id = -2_000_000 - completedMaintenance.EquipmentUnitId,
                Title = title,
                Message = $"Unidad {completedMaintenance.InventoryNumber ?? "S/N"} · {completedMaintenance.EquipmentName}: próximo mantenimiento sugerido para el {date:dd/MM/yyyy}. Aún no está programado.",
                Url = $"/Maintenances/Details/{completedMaintenance.Id}",
                Icon = "fas fa-calendar-check text-primary",
                SortDate = date,
                Time = RelativeDayLabel(today, date),
                SyntheticKey = $"prev-{completedMaintenance.EquipmentUnitId}",
                Group = NotificationGroups.Preventive,
                Urgency = urgency,
                Domain = NotificationDomains.Maintenance
            });
        }

        items.Sort((a, b) => a.SortDate.CompareTo(b.SortDate));
        return new CategoryResult(items.Count, items, uniformUrgency: false);
    }

    // ==================== B) Préstamos ====================

    private async Task<List<CategoryResult>> BuildDepartureCategoriesAsync(
        NotificationQuery query, DateTime today, string? filterTerm, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Departures
            .AsNoTracking()
            .Where(d => d.ActualReturnDate == null
                && d.Status != LoanStatus.Returned
                && d.Status != LoanStatus.Cancelled
                && d.EstimatedReturnDate != null);

        if (query.ManagementId.HasValue)
        {
            baseQuery = baseQuery.Where(d => d.ManagementId == query.ManagementId.Value);
        }

        var projected = baseQuery.Select(d => new LoanRow
        {
            Id = d.Id,
            ManagementId = d.ManagementId,
            EstimatedReturnDate = d.EstimatedReturnDate!.Value,
            InventoryNumber = d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null
        });

        if (!string.IsNullOrWhiteSpace(filterTerm))
        {
            var term = filterTerm.Trim().ToLower();
            projected = projected.Where(d => d.InventoryNumber != null && d.InventoryNumber.ToLower().Contains(term));
        }

        var results = new List<CategoryResult>();

        // Vencidos: sin límite inferior.
        var overdueQuery = projected.Where(d => d.EstimatedReturnDate < today);
        var overdueTotal = await overdueQuery.CountAsync(cancellationToken);
        var overdueRows = await overdueQuery
            .OrderBy(d => d.EstimatedReturnDate)
            .Take(MaxItemsPerCategory)
            .ToListAsync(cancellationToken);
        results.Add(new CategoryResult(overdueTotal, overdueRows.Select(d => BuildLoanItem(
            d, today,
            title: "Préstamo vencido",
            group: NotificationGroups.Overdue,
            urgency: NotificationUrgency.Danger,
            icon: "fas fa-exclamation-triangle text-danger")).ToList(), uniformUrgency: true));

        // Por vencer: hoy .. hoy+7.
        var dueSoonQuery = projected.Where(d => d.EstimatedReturnDate >= today && d.EstimatedReturnDate <= today.AddDays(7));
        var dueSoonTotal = await dueSoonQuery.CountAsync(cancellationToken);
        var dueSoonRows = await dueSoonQuery
            .OrderBy(d => d.EstimatedReturnDate)
            .Take(MaxItemsPerCategory)
            .ToListAsync(cancellationToken);
        results.Add(new CategoryResult(dueSoonTotal, dueSoonRows.Select(d => BuildLoanItem(
            d, today,
            title: "Préstamo por vencer",
            group: NotificationGroups.Departures,
            urgency: NotificationUrgency.Warning,
            icon: "fas fa-exchange-alt text-warning")).ToList(), uniformUrgency: true));

        return results;
    }

    private static NotificationItem BuildLoanItem(
        LoanRow d, DateTime today, string title, string group, string urgency, string icon)
    {
        var date = d.EstimatedReturnDate.Date;
        return new NotificationItem
        {
            Id = -3_000_000 - d.Id,
            Title = title,
            Message = $"Unidad {d.InventoryNumber ?? "S/N"}: devolución estimada el {date:dd/MM/yyyy}.",
            Url = $"/Departures/Details?id={d.Id}&ManagementId={d.ManagementId}",
            Icon = icon,
            SortDate = date,
            Time = RelativeDayLabel(today, date),
            SyntheticKey = $"dept-{d.Id}",
            Group = group,
            Urgency = urgency,
            Domain = NotificationDomains.Departure
        };
    }

    // ==================== C) Adquisiciones ====================

    private async Task<List<CategoryResult>> BuildAcquisitionCategoriesAsync(
        NotificationQuery query, DateTime today, string? filterTerm, CancellationToken cancellationToken)
    {
        var typeQuery = query.ManagementType.HasValue ? $"&ManagementType={query.ManagementType.Value}" : string.Empty;

        var plannedQuery = _context.ManagementPlans
            .AsNoTracking()
            .Where(p => p.AcquisitionRequestId != null
                && p.AcquisitionRequest!.Type == RequestType.Purchasing
                && p.AcquisitionRequest.Status == RequestStatus.Pending
                && p.PlannedDate != null
                && p.PlannedDate.Value.Date >= today
                && p.PlannedDate.Value.Date <= today.AddDays(14));

        if (query.ManagementId.HasValue)
        {
            plannedQuery = plannedQuery.Where(p => p.ManagementId == query.ManagementId.Value);
        }

        var plannedTotal = await plannedQuery.CountAsync(cancellationToken);
        var plannedRows = await plannedQuery
            .OrderBy(p => p.PlannedDate)
            .Take(MaxItemsPerCategory)
            .Select(p => new AcquisitionRow
            {
                RequestId = p.AcquisitionRequestId!.Value,
                ManagementId = p.ManagementId,
                Date = p.PlannedDate!.Value
            })
            .ToListAsync(cancellationToken);

        var plannedItems = plannedRows.Select(r =>
        {
            var date = r.Date.Date;
            return new NotificationItem
            {
                Id = -4_000_000 - r.RequestId,
                Title = "Adquisición pendiente",
                Message = $"Solicitud #{r.RequestId}: revisar antes del {date:dd/MM/yyyy}.",
                Url = $"/Requests/Details/{r.RequestId}?IsWizard=true&ManagementId={r.ManagementId}{typeQuery}&Step=6",
                Icon = "fas fa-shopping-cart text-success",
                SortDate = date,
                Time = RelativeDayLabel(today, date),
                SyntheticKey = $"acq-{r.RequestId}",
                Group = NotificationGroups.Acquisitions,
                Urgency = NotificationUrgency.Success,
                Domain = NotificationDomains.Acquisition
            };
        }).ToList();

        // Solicitudes de compra pendientes con más de 14 días, excluyendo las ya planificadas.
        var plannedRequestIds = plannedRows.Select(r => r.RequestId).Distinct().ToList();
        var staleQuery = _context.Requests
            .AsNoTracking()
            .Where(r => r.Type == RequestType.Purchasing
                && r.Status == RequestStatus.Pending
                && (r.RequestDate ?? r.CreatedDate).Date <= today.AddDays(-14)
                && !plannedRequestIds.Contains(r.Id));

        if (query.ManagementId.HasValue)
        {
            staleQuery = staleQuery.Where(r => r.ManagementId == query.ManagementId.Value);
        }

        var staleTotal = await staleQuery.CountAsync(cancellationToken);
        var staleRows = await staleQuery
            .OrderBy(r => r.RequestDate ?? r.CreatedDate)
            .Take(MaxItemsPerCategory)
            .Select(r => new AcquisitionRow
            {
                RequestId = r.Id,
                ManagementId = r.ManagementId,
                Date = r.RequestDate ?? r.CreatedDate
            })
            .ToListAsync(cancellationToken);

        var staleItems = staleRows.Select(r =>
        {
            var date = r.Date.Date;
            return new NotificationItem
            {
                Id = -5_000_000 - r.RequestId,
                Title = "Adquisición requiere seguimiento",
                Message = $"Solicitud #{r.RequestId}: pendiente desde el {date:dd/MM/yyyy}.",
                Url = $"/Requests/Details/{r.RequestId}?IsWizard=true&ManagementId={r.ManagementId}{typeQuery}&Step=6",
                Icon = "fas fa-shopping-cart text-success",
                SortDate = date,
                Time = RelativeDayLabel(today, date),
                SyntheticKey = $"stale-{r.RequestId}",
                Group = NotificationGroups.Acquisitions,
                Urgency = NotificationUrgency.Success,
                Domain = NotificationDomains.Acquisition
            };
        }).ToList();

        return
        [
            new CategoryResult(plannedTotal, plannedItems, uniformUrgency: true),
            new CategoryResult(staleTotal, staleItems, uniformUrgency: true)
        ];
    }

    // ==================== D) Planificación ====================

    private async Task<List<CategoryResult>> BuildPlanningCategoriesAsync(DateTime today, string? filterTerm, CancellationToken cancellationToken)
    {
        var results = new List<CategoryResult>();

        // Prácticas de hoy (que ocupan ambiente y material).
        var todayCount = await _context.Plannings
            .AsNoTracking()
            .CountAsync(p => !p.IsDeleted
                && p.ScheduledDate >= today && p.ScheduledDate < today.AddDays(1)
                && !PlanningStatus.NonOccupying.Contains(p.Status), cancellationToken);
        AddAggregate(results, todayCount, new NotificationItem
        {
            Id = -6_000_001,
            Title = "Planificaciones de hoy",
            Message = $"{todayCount} práctica(s) programada(s) para hoy.",
            Url = "/LaboratoryOperations/Index",
            Icon = "fas fa-flask text-primary",
            SortDate = today,
            Time = "Hoy",
            SyntheticKey = $"plan-today-{today:yyyyMMdd}",
            Group = NotificationGroups.Planning,
            Urgency = NotificationUrgency.Info,
            Domain = NotificationDomains.Planning
        }, filterTerm);

        // Prácticas esperando reposición.
        var recoveryCount = await _context.Plannings
            .AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.Status == PlanningStatus.PendingRecovery, cancellationToken);
        AddAggregate(results, recoveryCount, new NotificationItem
        {
            Id = -6_000_002,
            Title = "Prácticas por reponer",
            Message = $"{recoveryCount} práctica(s) esperan reposición.",
            Url = "/LaboratoryOperations/Reposiciones",
            Icon = "fas fa-undo-alt text-warning",
            SortDate = today,
            Time = "Hoy",
            SyntheticKey = "plan-recovery",
            Group = NotificationGroups.Planning,
            Urgency = NotificationUrgency.Warning,
            Domain = NotificationDomains.Planning
        }, filterTerm);

        // Conflictos de stock de la semana (lunes..domingo), cálculo costoso cacheado 2 minutos.
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var conflictCount = await GetWeekConflictCountAsync(monday, cancellationToken);
        AddAggregate(results, conflictCount, new NotificationItem
        {
            Id = -6_000_003,
            Title = "Conflictos de material esta semana",
            Message = $"{conflictCount} conflicto(s) de stock esta semana.",
            Url = "/LaboratoryOperations/Conflictos",
            Icon = "fas fa-exclamation-circle text-danger",
            SortDate = monday,
            Time = "Esta semana",
            SyntheticKey = $"plan-conflict-{monday:yyyyMMdd}",
            Group = NotificationGroups.Planning,
            Urgency = NotificationUrgency.Danger,
            Domain = NotificationDomains.Planning
        }, filterTerm);

        // Devoluciones de práctica vencidas (salidas vinculadas a planificación, sin devolver).
        var returnsQuery = _context.Departures
            .AsNoTracking()
            .Where(d => d.PlanningId != null
                && (d.Status == LoanStatus.Active || d.Status == LoanStatus.Overdue)
                && d.EstimatedReturnDate != null
                && d.EstimatedReturnDate.Value.Date < today);
        var returnsCount = await returnsQuery.CountAsync(cancellationToken);
        var earliestReturn = returnsCount > 0
            ? (await returnsQuery.MinAsync(d => d.EstimatedReturnDate!.Value, cancellationToken)).Date
            : today;
        AddAggregate(results, returnsCount, new NotificationItem
        {
            Id = -6_000_004,
            Title = "Devoluciones de práctica vencidas",
            Message = $"{returnsCount} práctica(s) con devolución pendiente.",
            Url = "/LaboratoryOperations/Index",
            Icon = "fas fa-hourglass-end text-danger",
            SortDate = earliestReturn,
            Time = RelativeDayLabel(today, earliestReturn),
            SyntheticKey = "plan-returns",
            Group = NotificationGroups.Planning,
            Urgency = NotificationUrgency.Danger,
            Domain = NotificationDomains.Planning
        }, filterTerm);

        return results;
    }

    /// <summary>Agrega un aviso agregado de planificación solo si su conteo es positivo (y pasa el filtro).</summary>
    private static void AddAggregate(List<CategoryResult> results, int count, NotificationItem item, string? filterTerm)
    {
        if (count <= 0) return;
        if (!string.IsNullOrWhiteSpace(filterTerm) && !MatchesFilter(item, filterTerm)) return;
        results.Add(new CategoryResult(1, [item], uniformUrgency: true));
    }

    /// <summary>Conflictos de material de la semana: cacheado 2 minutos por fecha; ante error, 0.</summary>
    private async Task<int> GetWeekConflictCountAsync(DateTime monday, CancellationToken cancellationToken)
    {
        var cacheKey = $"{ConflictCachePrefix}{monday:yyyyMMdd}";
        if (_cache.TryGetValue(cacheKey, out int cached))
        {
            return cached;
        }

        try
        {
            var week = await _availability.GetRangeAsync(monday, monday.AddDays(6));
            _cache.Set(cacheKey, week.ConflictCount, TimeSpan.FromMinutes(2));
            return week.ConflictCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron calcular los conflictos de material de la semana del {Monday:dd/MM/yyyy}.", monday);
            return 0;
        }
    }

    // ==================== Historial y marcado ====================

    public async Task<IReadOnlyList<NotificationItem>> GetHistoryAsync(int userId, int take = 100, CancellationToken cancellationToken = default)
    {
        var rows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new PersistedRow
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                IconClass = n.IconClass,
                Scope = n.Scope,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            })
            .ToListAsync(cancellationToken);

        return rows.Select(MapPersistedItem).ToList();
    }

    public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        if (notificationId <= 0) return;

        var notification = await _context.Notifications
            .AsTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (notification == null || notification.IsRead) return;

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications
            .AsTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        if (unread.Count == 0) return;

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // ==================== Utilidades ====================

    private static string NormalizeScope(string? scope)
    {
        var normalized = scope?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(normalized) ? NotificationScopes.Dashboard : normalized;
    }

    private static Dictionary<string, int> NewGroupCounts() =>
        NotificationGroups.Order.ToDictionary(g => g, _ => 0);

    private static bool IsCritical(NotificationItem? item) =>
        item != null && item.Urgency is NotificationUrgency.Danger or NotificationUrgency.Warning;

    private static bool MatchesFilter(NotificationItem item, string? filterTerm) =>
        string.IsNullOrWhiteSpace(filterTerm)
        || item.Title.Contains(filterTerm.Trim(), StringComparison.OrdinalIgnoreCase)
        || item.Message.Contains(filterTerm.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string RelativeDayLabel(DateTime today, DateTime targetDate)
    {
        var days = (targetDate.Date - today.Date).Days;
        return days switch
        {
            < 0 => $"Vencido hace {Math.Abs(days)} día(s)",
            0 => "Hoy",
            1 => "Mañana",
            _ => $"En {days} días"
        };
    }
}
