using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Notifications;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

/// <summary>
/// Limpieza de avisos de solicitudes docentes: (a) al resolver la solicitud en TeacherRequestService se marcan
/// leídos los avisos de todos los revisores de ESE id; (b) el feed auto-resuelve los avisos persistidos de
/// solicitudes borradas o ya resueltas; (c) preventivos de unidades "por confirmar"/de baja van a "revisar-datos"
/// sin contar en CriticalCount; (d) las URL de "resolver" llevan donde se actúa.
/// </summary>
public class NotificationRequestResolutionTests
{
    private static readonly DateTime Today = new(2026, 10, 9);
    private const int ReviewerA = 900;
    private const int ReviewerB = 902;
    private const int TeacherUserId = 901;

    private static TeacherRequestService Requests(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static NotificationAlertService Alerts(TestDb db) =>
        new(db.Context,
            new ArticleAvailabilityService(db.Context, new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance)),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<NotificationAlertService>.Instance);

    private static NotificationQuery Query(TestDb db, NotificationAudience? audience = null) =>
        new(UserId: 1, Today: Today, Scope: null, ManagementId: null, ManagementType: null,
            Audience: audience ?? NotificationAudience.Everything, Take: 30);

    private static string RequestUrl(int planningId) => $"/LaboratoryOperations/Solicitudes/Revisar?id={planningId}";

    /// <summary>Siembra los avisos "Nueva solicitud de práctica" que crea NotifyReviewersAsync (una copia por revisor).</summary>
    private static void AddReviewerAlerts(TestDb db, params int[] planningIds)
    {
        foreach (var planningId in planningIds)
        {
            foreach (var userId in new[] { ReviewerA, ReviewerB })
            {
                db.Context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Nueva solicitud de práctica",
                    Message = $"Docente · Práctica {planningId} · {TestDb.Week1Monday:dd/MM}",
                    ActionUrl = RequestUrl(planningId),
                    IconClass = "fas fa-inbox text-primary",
                    Scope = "planning",
                    ManagementId = TestDb.ManagementId
                });
            }
        }
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Estado de lectura por URL: hay una copia del aviso por revisor, así que se agrupan.</summary>
    private static Dictionary<string, (bool AllRead, bool NoneRead)> ReadStateByUrl(TestDb db) =>
        db.Context.Notifications.AsNoTracking().ToList()
            .GroupBy(n => n.ActionUrl!)
            .ToDictionary(g => g.Key, g => (g.All(n => n.IsRead), g.All(n => !n.IsRead)));

    private static void AddUnit(TestDb db, int unitId, string inventory, string equipmentName, EquipmentStatus status)
    {
        db.Context.Equipments.Add(new Equipment { Id = unitId, Name = equipmentName });
        db.Context.EquipmentUnits.Add(new EquipmentUnit { Id = unitId, EquipmentId = unitId, InventoryNumber = inventory, CurrentStatus = status });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static void AddMaintenance(TestDb db, int id, int unitId, DateTime? scheduled, MaintenanceStatus status,
        DateTime? suggestedNext = null, DateTime? endDate = null, DateTime? created = null)
    {
        db.Context.Maintenances.Add(new Maintenance
        {
            Id = id,
            EquipmentUnitId = unitId,
            ManagementId = TestDb.ManagementId,
            ScheduledDate = scheduled,
            Status = status,
            MaintenanceType = MaintenanceType.Preventivo,
            SuggestedNextMaintenanceDate = suggestedNext,
            EndDate = endDate,
            CreatedDate = created ?? DateTime.UtcNow,
            Description = "Mantenimiento de prueba"
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    // ───────── (a) Limpieza en el origen: aprobar / rechazar / retirar ─────────

    [Fact]
    public async Task ResolverSolicitud_MarcaLeidosLosAvisosDeTodosLosRevisores_DeEseIdSolo()
    {
        using var db = new TestDb();
        db.Context.Users.Add(new User { Id = ReviewerA, UserName = "revisor", FirstName = "Ana", LastName = "Revisora", Role = UserRole.Administrador });
        db.Context.Users.Add(new User { Id = ReviewerB, UserName = "super", FirstName = "Sam", LastName = "Super", Role = UserRole.SuperAdmin });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70, teacherPersonId: TestDb.Teacher1);
        // 1 se aprueba; 12 comparte prefijo con 1 (id=1 NO debe tocar id=12); 2 se retira.
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabA, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddPlanning(12, TestDb.Week1Monday, 5, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddPlanning(2, TestDb.Week2Monday, 2, 2, laboratoryId: TestDb.LabA, status: PlanningStatus.Requested, courseGroupId: 80);
        AddReviewerAlerts(db, 1, 12, 2);

        // Aprueba la 1: avisos de AMBOS revisores leídos; 12 y 2 intactos.
        Assert.True((await Requests(db).ApproveAsync(ReviewerA, 1, "Aprobada", null)).Ok);
        var byUrl = ReadStateByUrl(db);
        Assert.True(byUrl[RequestUrl(1)].AllRead);
        Assert.True(byUrl[RequestUrl(12)].NoneRead);
        Assert.True(byUrl[RequestUrl(2)].NoneRead);
        // Una copia por revisor: ambas marcadas.
        Assert.Equal(2, db.Context.Notifications.AsNoTracking().Count(n => n.ActionUrl == RequestUrl(1) && n.IsRead));

        // Rechaza la 12: sus avisos quedan leídos; la 2 sigue pendiente.
        Assert.True((await Requests(db).RejectAsync(ReviewerA, 12, "Sin material disponible")).Ok);
        byUrl = ReadStateByUrl(db);
        Assert.True(byUrl[RequestUrl(12)].AllRead);
        Assert.True(byUrl[RequestUrl(2)].NoneRead);

        // El docente retira la 2: también se resuelven sus avisos.
        var teacher = new TeacherProfile(TeacherUserId, TestDb.Teacher1, "Luis Docente");
        Assert.True((await Requests(db).WithdrawAsync(teacher, 2, "Ya no la necesito")).Ok);
        byUrl = ReadStateByUrl(db);
        Assert.True(byUrl[RequestUrl(2)].AllRead);
    }

    [Fact]
    public async Task ObservarSolicitud_TambienResuelveSusAvisos()
    {
        using var db = new TestDb();
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        AddReviewerAlerts(db, 1);

        Assert.True((await Requests(db).ObserveAsync(ReviewerA, 1, "Cambia al jueves, el lab está ocupado.")).Ok);

        Assert.True(ReadStateByUrl(db)[RequestUrl(1)].AllRead);
    }

    // ───────── (b) Autolimpieza al leer el feed ─────────

    [Fact]
    public async Task Feed_OcultaYMarcaLeidosAvisosDeSolicitudesBorradasOResueltas()
    {
        using var db = new TestDb();
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);  // sigue en revisión: se conserva
        db.AddPlanning(2, TestDb.Week1Monday, 3, 2, status: PlanningStatus.Approved);   // ya resuelta
        db.AddPlanning(3, TestDb.Week1Monday, 4, 2, status: PlanningStatus.Requested);  // se borra lógicamente
        var plan3 = db.Context.Plannings.AsTracking().Single(p => p.Id == 3);
        plan3.IsDeleted = true;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        db.Context.Notifications.AddRange(
            new Notification { UserId = 1, Title = "Nueva solicitud de práctica", Message = "Solicitud 1", ActionUrl = RequestUrl(1), Scope = "planning", ManagementId = TestDb.ManagementId },
            new Notification { UserId = 1, Title = "Nueva solicitud de práctica", Message = "Solicitud 2", ActionUrl = RequestUrl(2), Scope = "planning", ManagementId = TestDb.ManagementId },
            new Notification { UserId = 1, Title = "Nueva solicitud de práctica", Message = "Solicitud 3", ActionUrl = RequestUrl(3), Scope = "planning", ManagementId = TestDb.ManagementId },
            // La planificación 4 no existe (borrado físico histórico).
            new Notification { UserId = 1, Title = "Nueva solicitud de práctica", Message = "Solicitud 4", ActionUrl = RequestUrl(4), Scope = "planning", ManagementId = TestDb.ManagementId });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var feed = await Alerts(db).GetFeedAsync(Query(db));

        // Solo queda visible el aviso de la solicitud que sigue en "Solicitada".
        var persisted = feed.Items.Where(i => i.Id > 0).ToList();
        Assert.Single(persisted);
        Assert.Equal(RequestUrl(1), persisted[0].Url);
        Assert.Equal(1, feed.Count);

        // Los demás quedaron marcados leídos en la tabla: al primer acceso se limpia el backlog.
        var byUrl = ReadStateByUrl(db);
        Assert.True(byUrl[RequestUrl(1)].NoneRead);
        Assert.True(byUrl[RequestUrl(2)].AllRead);
        Assert.True(byUrl[RequestUrl(3)].AllRead);
        Assert.True(byUrl[RequestUrl(4)].AllRead);
    }

    // ───────── (c) Preventivo de unidad por confirmar / de baja → "revisar-datos" ─────────

    [Fact]
    public async Task Preventivo_UnidadPorConfirmarODeBaja_CaeEnRevisarDatos_SinContarCriticos()
    {
        using var db = new TestDb();
        AddUnit(db, 1, "INV-98", "Estufa histórica", EquipmentStatus.PorConfirmar);
        AddUnit(db, 2, "INV-03", "Mufla vieja", EquipmentStatus.Decommissioned);
        AddUnit(db, 3, "INV-99", "Balanza eliminada", EquipmentStatus.Deleted);
        foreach (var (unitId, maintenanceId) in new[] { (1, 60), (2, 61), (3, 62) })
        {
            AddMaintenance(db, maintenanceId, unitId, new DateTime(2024, 5, 10), MaintenanceStatus.Completed,
                suggestedNext: Today.AddDays(-500), endDate: new DateTime(2024, 5, 10), created: new DateTime(2024, 5, 10));
        }

        var feed = await Alerts(db).GetFeedAsync(Query(db));

        // La unidad por confirmar y la dada de baja piden confirmar datos; la eliminada no genera avisos.
        Assert.Equal(2, feed.GroupCounts[NotificationGroups.DataReview]);
        Assert.Equal(0, feed.GroupCounts[NotificationGroups.Preventive]);
        // Urgencia secondary: ninguna cuenta como crítica aunque la fecha sugerida esté vencida.
        Assert.Equal(0, feed.CriticalCount);

        var confirmar = Assert.Single(feed.Items, i => i.SyntheticKey == "prev-1");
        Assert.Equal("Confirmar datos del equipo INV-98", confirmar.Title);
        Assert.Equal(NotificationGroups.DataReview, confirmar.Group);
        Assert.Equal(NotificationUrgency.Secondary, confirmar.Urgency);
        Assert.Equal("/EquipmentUnits/Edit?id=1", confirmar.Url);

        Assert.Contains(feed.Items, i => i.SyntheticKey == "prev-2" && i.Group == NotificationGroups.DataReview);
        Assert.DoesNotContain(feed.Items, i => i.SyntheticKey == "prev-3");
    }

    // ───────── (d) URL de "resolver" ─────────

    [Fact]
    public async Task Preventivo_UnidadNormal_ResuelveEnCrearMantenimientoDeLaUnidad()
    {
        using var db = new TestDb();
        AddUnit(db, 1, "INV-10", "Balanza analítica", EquipmentStatus.Operational);
        AddMaintenance(db, 70, 1, Today.AddDays(-40), MaintenanceStatus.Completed,
            suggestedNext: Today.AddDays(-5), endDate: Today.AddDays(-40), created: Today.AddDays(-45));

        var feed = await Alerts(db).GetFeedAsync(Query(db));

        var preventive = Assert.Single(feed.Items, i => i.SyntheticKey == "prev-1");
        Assert.Equal(NotificationGroups.Preventive, preventive.Group);
        Assert.Equal(NotificationUrgency.Danger, preventive.Urgency); // vencido sigue siendo alarma
        Assert.Equal("/Maintenances/Create?equipmentUnitId=1", preventive.Url);
        Assert.Equal(1, feed.CriticalCount);
    }

    [Fact]
    public async Task MantenimientoVencidoYPrestamoVencido_ResuelvenEnSuPaginaEdit()
    {
        using var db = new TestDb();
        AddUnit(db, 1, "INV-11", "Mufla", EquipmentStatus.Operational);
        AddMaintenance(db, 80, 1, Today.AddDays(-5), MaintenanceStatus.Pending);
        db.Context.Departures.Add(new Departure
        {
            Id = 7,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = 1,
            Status = LoanStatus.Active,
            EstimatedReturnDate = Today.AddDays(-2)
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var feed = await Alerts(db).GetFeedAsync(Query(db));

        var maintenance = Assert.Single(feed.Items, i => i.SyntheticKey == "mait-80");
        Assert.Equal($"/Maintenances/Edit?id=80&ManagementId={TestDb.ManagementId}", maintenance.Url);

        var loan = Assert.Single(feed.Items, i => i.SyntheticKey == "dept-7");
        Assert.Equal($"/Departures/Edit?id=7&ManagementId={TestDb.ManagementId}", loan.Url);
    }
}
