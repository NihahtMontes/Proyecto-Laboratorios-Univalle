using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Notifications;

namespace Laboratorios.Tests;

/// <summary>
/// Pruebas de la fuente única de avisos (INotificationAlertService) con EF InMemory.
/// Fecha fija de referencia: viernes 09/10/2026 (semana 1 del calendario sembrado por TestDb).
/// </summary>
public class NotificationAlertServiceTests
{
    private static readonly DateTime Today = new(2026, 10, 9);

    private static NotificationAlertService Service(TestDb db) =>
        new(db.Context,
            new ArticleAvailabilityService(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance)),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<NotificationAlertService>.Instance);

    private static NotificationQuery Query(TestDb db, NotificationAudience? audience = null, int take = 30) =>
        new(UserId: 1, Today: Today, Scope: null, ManagementId: null, ManagementType: null,
            Audience: audience ?? NotificationAudience.Everything, Take: take);

    private static void AddEquipmentUnit(TestDb db, int unitId, string inventory, string equipmentName)
    {
        db.Context.Equipments.Add(new Equipment { Id = unitId, Name = equipmentName });
        db.Context.EquipmentUnits.Add(new EquipmentUnit { Id = unitId, EquipmentId = unitId, InventoryNumber = inventory });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void AddMaintenance(TestDb db, int id, int unitId, DateTime? scheduled,
        MaintenanceStatus status = MaintenanceStatus.Pending,
        MaintenanceType type = MaintenanceType.Preventivo,
        DateTime? suggestedNext = null,
        DateTime? endDate = null,
        DateTime? created = null)
    {
        db.Context.Maintenances.Add(new Maintenance
        {
            Id = id,
            EquipmentUnitId = unitId,
            ManagementId = TestDb.ManagementId,
            ScheduledDate = scheduled,
            Status = status,
            MaintenanceType = type,
            SuggestedNextMaintenanceDate = suggestedNext,
            EndDate = endDate,
            CreatedDate = created ?? DateTime.UtcNow,
            Description = "Mantenimiento de prueba"
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    // (1) Regresión: un mantenimiento vencido de hace 45 días aparece (el código viejo cortaba a 30 días).
    [Fact]
    public async Task OverdueMaintenanceOlderThan30Days_IsStillReported()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-001", "Balanza analítica");
        AddMaintenance(db, 10, 1, Today.AddDays(-45));

        var feed = await Service(db).GetFeedAsync(Query(db));

        var item = Assert.Single(feed.Items, i => i.SyntheticKey == "mait-10");
        Assert.Equal("Mantenimiento vencido", item.Title);
        Assert.Equal(NotificationGroups.Overdue, item.Group);
        Assert.Equal(NotificationUrgency.Danger, item.Urgency);
        Assert.Equal(1, feed.Count);
        Assert.Contains("45", item.Time);
    }

    // (2) Mantenimiento programado para hoy: grupo "hoy" y urgencia warning.
    [Fact]
    public async Task MaintenanceScheduledToday_IsInTodayGroupWithWarning()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-002", "Centrífuga");
        AddMaintenance(db, 11, 1, Today);

        var feed = await Service(db).GetFeedAsync(Query(db));

        var item = Assert.Single(feed.Items, i => i.SyntheticKey == "mait-11");
        Assert.Equal("Mantenimiento para hoy", item.Title);
        Assert.Equal(NotificationGroups.Today, item.Group);
        Assert.Equal(NotificationUrgency.Warning, item.Urgency);
        Assert.Equal("Hoy", item.Time);
    }

    // (3) Completados y cancelados no generan avisos.
    [Fact]
    public async Task CompletedOrCancelledMaintenances_AreNotReported()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-003", "Estufa");
        AddMaintenance(db, 12, 1, Today, status: MaintenanceStatus.Completed);
        AddMaintenance(db, 13, 1, Today.AddDays(-2), status: MaintenanceStatus.Cancelled);

        var feed = await Service(db).GetFeedAsync(Query(db));

        Assert.DoesNotContain(feed.Items, i => i.SyntheticKey == "mait-12" || i.SyntheticKey == "mait-13");
        Assert.Equal(0, feed.Count);
    }

    // (4) Preventivo sugerido por Kardex: aparece sin mantenimiento posterior y desaparece al programarlo.
    [Fact]
    public async Task SuggestedPreventive_AppearsWithoutFollowUp_AndDisappearsOnceScheduled()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-004", "Espectrofotómetro");
        // Completado hace 40 días; el Kardex sugiere el próximo para hoy+3.
        AddMaintenance(db, 20, 1, Today.AddDays(-40),
            status: MaintenanceStatus.Completed,
            suggestedNext: Today.AddDays(3),
            endDate: Today.AddDays(-40),
            created: Today.AddDays(-45));

        var feed = await Service(db).GetFeedAsync(Query(db));

        var preventive = Assert.Single(feed.Items, i => i.SyntheticKey == "prev-1");
        Assert.Equal("Preventivo por programar", preventive.Title);
        Assert.Equal(NotificationGroups.Preventive, preventive.Group);
        Assert.Equal(NotificationUrgency.Warning, preventive.Urgency);
        Assert.True(feed.Count >= 1);

        // Se programa el siguiente mantenimiento: la sugerencia deja de tener sentido.
        AddMaintenance(db, 21, 1, Today.AddDays(3),
            status: MaintenanceStatus.Scheduled,
            created: Today.AddDays(-1));

        feed = await Service(db).GetFeedAsync(Query(db));
        Assert.DoesNotContain(feed.Items, i => i.SyntheticKey == "prev-1");
    }

    // (5) Una audiencia sin acceso a mantenimientos no ve ninguno de sus avisos.
    [Fact]
    public async Task AudienceWithoutMaintenanceAccess_DoesNotSeeMaintenanceAlerts()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-005", "Mufla");
        AddMaintenance(db, 30, 1, Today.AddDays(-45));

        var audience = new NotificationAudience(CanSeeMaintenance: false, CanSeeDepartures: true, CanSeeAcquisitions: true, CanSeePlanning: true);
        var feed = await Service(db).GetFeedAsync(Query(db, audience));

        Assert.DoesNotContain(feed.Items, i => i.Domain == NotificationDomains.Maintenance);
        Assert.Equal(0, feed.Count);
    }

    // (6) Count y GroupCounts son totales aunque Take trunque la lista de ítems.
    [Fact]
    public async Task CountsAreTotals_EvenWhenTakeIsOne()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-006", "Fotómetro");
        AddMaintenance(db, 40, 1, Today.AddDays(-10));  // vencido
        AddMaintenance(db, 41, 1, Today);               // hoy
        AddMaintenance(db, 42, 1, Today.AddDays(5));    // próximo

        var feed = await Service(db).GetFeedAsync(Query(db, take: 1));

        Assert.Single(feed.Items);
        Assert.Equal(3, feed.Count);
        Assert.Equal(2, feed.CriticalCount);
        Assert.Equal(1, feed.GroupCounts[NotificationGroups.Overdue]);
        Assert.Equal(1, feed.GroupCounts[NotificationGroups.Today]);
        Assert.Equal(1, feed.GroupCounts[NotificationGroups.Upcoming]);
    }

    // (7) Un aviso persistido de un mantenimiento ya completado se marca leído y desaparece del feed.
    [Fact]
    public async Task PersistedAlertOfCompletedMaintenance_IsAutoResolved()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-007", "Incubadora");
        AddMaintenance(db, 50, 1, Today.AddDays(-5), status: MaintenanceStatus.Completed);
        db.Context.Notifications.Add(new Notification
        {
            UserId = 1,
            Title = "Mantenimiento asignado",
            Message = "Unidad INV-007 · Incubadora: programado para el 04/10/2026.",
            ActionUrl = $"/Maintenances/Details/50?ManagementId={TestDb.ManagementId}&ManagementType=Preventive",
            IconClass = "fas fa-user-cog text-info",
            Scope = "maintenance",
            ManagementId = TestDb.ManagementId
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var feed = await Service(db).GetFeedAsync(Query(db));

        Assert.DoesNotContain(feed.Items, i => i.Id > 0);
        Assert.Equal(0, feed.Count);
        var persisted = db.Context.Notifications.AsNoTracking().Single(n => n.UserId == 1);
        Assert.True(persisted.IsRead);
    }

    // (7b) El aviso persistido de asignación no oculta el calculado "vencido" del mismo mantenimiento,
    // pero sí evita duplicar el "próximo".
    [Fact]
    public async Task PersistedAssignment_DoesNotHideOverdue_ButDedupesUpcoming()
    {
        using var db = new TestDb();
        AddEquipmentUnit(db, 1, "INV-008", "Autoclave");
        AddMaintenance(db, 70, 1, Today.AddDays(-3));
        AddMaintenance(db, 71, 1, Today.AddDays(4));
        foreach (var id in new[] { 70, 71 })
        {
            db.Context.Notifications.Add(new Notification
            {
                UserId = 1,
                Title = "Mantenimiento asignado",
                Message = $"Mantenimiento {id}",
                ActionUrl = $"/Maintenances/Details/{id}?ManagementId={TestDb.ManagementId}",
                Scope = "maintenance",
                ManagementId = TestDb.ManagementId
            });
        }
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var feed = await Service(db).GetFeedAsync(Query(db));

        Assert.Contains(feed.Items, i => i.SyntheticKey == "mait-70" && i.Urgency == NotificationUrgency.Danger);
        Assert.DoesNotContain(feed.Items, i => i.SyntheticKey == "mait-71");
        Assert.Equal(3, feed.Count); // 2 persistidos + 1 vencido calculado
        Assert.Equal(1, feed.CriticalCount);
    }

    // (8) Las prácticas de hoy generan un aviso agregado de planificación.
    [Fact]
    public async Task TodayPlannings_GenerateAggregatedAlert()
    {
        using var db = new TestDb();
        db.AddPlanning(60, Today, 1, 2);

        var feed = await Service(db).GetFeedAsync(Query(db));

        var item = Assert.Single(feed.Items, i => i.SyntheticKey == $"plan-today-{Today:yyyyMMdd}");
        Assert.Equal("Planificaciones de hoy", item.Title);
        Assert.Contains("1 práctica", item.Message);
        Assert.Equal(NotificationGroups.Planning, item.Group);
        Assert.Equal(NotificationUrgency.Info, item.Urgency);
        Assert.Equal(NotificationDomains.Planning, item.Domain);
        Assert.Equal(1, feed.Count);
        Assert.Equal(1, feed.GroupCounts[NotificationGroups.Planning]);
    }
}
