using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

/// <summary>
/// Cambio de material (L-3) de prácticas YA aprobadas (entrega A): elegibilidad, pedido con sus líneas
/// Previous/New, retiro, aprobación (requirements, orígenes, faltantes), rechazo y bandeja de pendientes.
/// La planificación sigue «Aprobada» durante todo el flujo.
/// </summary>
public class MaterialChangeRequestTests
{
    private const int Bowl = 500;
    private const int Glass = 501;
    private const int Spoon = 502;
    private const int ReviewerUserId = 900;
    private const int TeacherUserId = 901;

    private static TeacherRequestService Service(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static DateTime Futura(int dias = 14) => DateTime.Today.AddDays(dias);

    /// <summary>Práctica aprobada futura con L-3 vigente (Bowl 12, Cuchara 2), revisor y docente con usuario.</summary>
    private static TestDb Base(int planningId = 1, DateTime? fecha = null, string status = PlanningStatus.Approved)
    {
        var db = new TestDb();
        db.Context.Users.AddRange(
            new User { Id = ReviewerUserId, UserName = "revisor", FirstName = "Ana", LastName = "Revisora", Role = UserRole.Administrador },
            new User { Id = TeacherUserId, UserName = "docente", FirstName = "Luis", LastName = "Docente", Role = UserRole.Docente, PersonId = TestDb.Teacher1 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddArticleWithStock(Bowl, "BOW", "Bowl", 30);
        db.AddArticleWithStock(Glass, "GLA", "Vaso", 30);
        db.AddArticleWithStock(Spoon, "SPO", "Cuchara", 10);
        db.AddPlanning(planningId, fecha ?? Futura(), 2, 2, laboratoryId: TestDb.LabB, status: status);
        db.AddRequirement(planningId, Bowl, 12);
        db.AddRequirement(planningId, Spoon, 2);
        return db;
    }

    private static TeacherProfile Docente() => new(TeacherUserId, TestDb.Teacher1, "Luis Docente");

    private static async Task<int> RequirementIdAsync(TestDb db, int planningId, int articleId) =>
        await db.Context.PlanningRequirements.AsNoTracking()
            .Where(r => r.PlanningId == planningId && r.ArticleId == articleId)
            .Select(r => r.Id).SingleAsync();

    // ───────── Pedido válido ─────────

    [Fact]
    public async Task Pedido_valido_registra_review_8_con_lineas_y_avisa_a_los_revisores()
    {
        using var db = Base();
        // L-3 deseado completo: Bowl 12→10, Vaso agregado (4) y Cuchara ausente → se quita.
        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 10), new RequestLineInput(Glass, 4)], "Cambio de número de estudiantes");

        Assert.True(result.Ok, result.Message);
        // La planificación sigue «Aprobada»: el pedido no cambia el estado.
        Assert.Equal(PlanningStatus.Approved, (await db.Context.Plannings.AsNoTracking().SingleAsync(p => p.Id == 1)).Status);

        var review = await db.Context.PlanningReviews.AsNoTracking().Include(r => r.Lines).SingleAsync();
        Assert.Equal(PlanningReviewAction.MaterialChangeRequested, review.Action);
        Assert.Equal(PlanningStatus.Approved, review.FromStatus);
        Assert.Equal(PlanningStatus.Approved, review.ToStatus);
        Assert.Equal("Cambio de número de estudiantes", review.Comment);
        Assert.Equal(TeacherUserId, review.UserId);

        Assert.Equal(3, review.Lines.Count);
        var bowl = review.Lines.Single(l => l.ArticleId == Bowl);
        Assert.Equal(12m, bowl.PreviousQuantity);
        Assert.Equal(10m, bowl.NewQuantity);
        var glass = review.Lines.Single(l => l.ArticleId == Glass);
        Assert.Null(glass.PreviousQuantity);
        Assert.Equal(4m, glass.NewQuantity);
        var spoon = review.Lines.Single(l => l.ArticleId == Spoon);
        Assert.Equal(2m, spoon.PreviousQuantity);
        Assert.Null(spoon.NewQuantity);

        // Aviso a los revisores con el título y la URL de la bandeja de cambios.
        var aviso = await db.Context.Notifications.AsNoTracking().SingleAsync();
        Assert.Equal("Pedido de cambio de material", aviso.Title);
        Assert.Equal("/LaboratoryOperations/Solicitudes/RevisarCambio?id=1", aviso.ActionUrl);
        Assert.Contains("Luis Docente", aviso.Message);
        Assert.Contains("Práctica 1", aviso.Message);
    }

    // ───────── Rechazos del pedido ─────────

    [Fact]
    public async Task Pedido_falla_cuando_la_practica_no_es_del_docente()
    {
        using var db = Base();
        var otro = new TeacherProfile(TeacherUserId, TestDb.Teacher2, "Otro Docente");

        var result = await Service(db).RequestMaterialChangeAsync(otro, 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio");

        Assert.False(result.Ok);
        Assert.Empty(await db.Context.PlanningReviews.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Pedido_falla_cuando_no_esta_aprobada()
    {
        using var db = Base(status: PlanningStatus.Requested);

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio");

        Assert.False(result.Ok);
        Assert.Contains("solo para prácticas aprobadas", result.Message);
        Assert.Empty(await db.Context.PlanningReviews.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Pedido_falla_cuando_ya_hay_salida_de_material()
    {
        using var db = Base();
        db.AddDeparture(1, LoanStatus.Active);

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio");

        Assert.False(result.Ok);
        Assert.Contains("material entregado", result.Message);
    }

    [Fact]
    public async Task Pedido_falla_cuando_ya_hay_uno_pendiente()
    {
        using var db = Base();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var segundo = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 8)], "Otro motivo distinto");

        Assert.False(segundo.Ok);
        Assert.Contains("Ya tienes un pedido", segundo.Message);
        Assert.Single(await db.Context.PlanningReviews.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Pedido_falla_cuando_la_practica_ya_paso()
    {
        using var db = Base(fecha: TestDb.Week1Monday);

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio");

        Assert.False(result.Ok);
        Assert.Contains("ya pasó", result.Message);
    }

    [Fact]
    public async Task Pedido_falla_cuando_el_L3_es_igual_al_vigente()
    {
        using var db = Base();

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 12), new RequestLineInput(Spoon, 2)], "Motivo del cambio");

        Assert.False(result.Ok);
        Assert.Contains("igual al vigente", result.Message);
    }

    [Fact]
    public async Task Pedido_falla_cuando_el_L3_queda_vacio()
    {
        using var db = Base();

        var vacio = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [], "Motivo del cambio");
        Assert.False(vacio.Ok);
        Assert.Contains("al menos un artículo", vacio.Message);

        var enCero = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 0)], "Motivo del cambio");
        Assert.False(enCero.Ok);
        Assert.Contains("al menos un artículo", enCero.Message);
    }

    [Fact]
    public async Task Pedido_falla_con_motivo_corto()
    {
        using var db = Base();

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "corto");

        Assert.False(result.Ok);
        Assert.Contains("motivo", result.Message);
    }

    [Fact]
    public async Task Pedido_falla_cuando_cambia_la_cantidad_de_una_unidad_fisica()
    {
        using var db = Base();
        // La práctica también pide una balanza (unidad física concreta).
        db.AddEquipmentWithUnit(equipmentId: 70, unitId: 700, "INV-700", articleId: 800, code: "BAL", name: "Balanza");
        db.AddUnitRequirement(1, 800, 700);

        var result = await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 10), new RequestLineInput(800, 2)], "Cambiar la balanza por dos");

        Assert.False(result.Ok);
        Assert.Contains("unidades físicas", result.Message);
        Assert.Empty(await db.Context.PlanningReviews.AsNoTracking().ToListAsync());
    }

    // ───────── Retiro ─────────

    [Fact]
    public async Task Retirar_deja_review_11_y_ya_no_queda_pendiente()
    {
        using var db = Base();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var retiro = await Service(db).WithdrawMaterialChangeAsync(Docente(), 1);

        Assert.True(retiro.Ok, retiro.Message);
        var reviews = await db.Context.PlanningReviews.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(PlanningReviewAction.MaterialChangeRequested, reviews[0].Action);
        Assert.Equal(PlanningReviewAction.MaterialChangeWithdrawn, reviews[1].Action);
        Assert.Equal(PlanningStatus.Approved, reviews[1].FromStatus);
        // Ya no hay pendiente: el docente puede pedir otro cambio.
        Assert.True((await Service(db).CanRequestMaterialChangeAsync(Docente(), 1)).Allowed);
        Assert.Null(await Service(db).GetPendingMaterialChangeAsync(1));
    }

    // ───────── Aprobación ─────────

    [Fact]
    public async Task Aprobar_actualiza_agrega_quita_requirements_y_recalcula_origenes()
    {
        using var db = Base();
        db.AddEconomato();
        // La línea que se va a quitar tiene un origen asignado: debe desaparecer con ella.
        var spoonReq = await RequirementIdAsync(db, 1, Spoon);
        db.Context.PlanningAssignments.Add(new PlanningAssignment { PlanningRequirementId = spoonReq, LaboratoryId = TestDb.EconomatoId, Quantity = 2 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 10), new RequestLineInput(Glass, 4)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var result = await Service(db).ApproveMaterialChangeAsync(ReviewerUserId, 1, "Cambio aprobado");

        Assert.True(result.Ok, result.Message);
        // Requirements finales: Bowl actualizado a 10, Vaso agregado en 4, Cuchara quitada.
        var requirements = await db.Context.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == 1).ToListAsync();
        Assert.Equal(10m, requirements.Single(r => r.ArticleId == Bowl).RequestedQuantity);
        Assert.Equal(10, requirements.Single(r => r.ArticleId == Bowl).ExpectedQuantity);
        Assert.Equal(4m, requirements.Single(r => r.ArticleId == Glass).RequestedQuantity);
        Assert.DoesNotContain(requirements, r => r.ArticleId == Spoon);

        // Orígenes: se borró la asignación de lo quitado y las líneas restantes quedaron cubiertas.
        var assignments = await db.Context.PlanningAssignments.AsNoTracking().ToListAsync();
        Assert.DoesNotContain(assignments, a => a.PlanningRequirementId == spoonReq);
        var bowlReq = await RequirementIdAsync(db, 1, Bowl);
        var glassReq = await RequirementIdAsync(db, 1, Glass);
        Assert.Equal(10m, assignments.Where(a => a.PlanningRequirementId == bowlReq).Sum(a => a.Quantity));
        Assert.Equal(4m, assignments.Where(a => a.PlanningRequirementId == glassReq).Sum(a => a.Quantity));

        // Review 9 sin líneas, aviso al docente y la planificación sigue aprobada.
        var reviews = await db.Context.PlanningReviews.AsNoTracking().Include(r => r.Lines).OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(PlanningReviewAction.MaterialChangeApproved, reviews[1].Action);
        Assert.Empty(reviews[1].Lines);
        Assert.Equal("Cambio aprobado", reviews[1].Comment);
        var aviso = await db.Context.Notifications.AsNoTracking().SingleAsync(n => n.UserId == TeacherUserId);
        Assert.Equal("Cambio de material aprobado", aviso.Title);
        Assert.Equal(PlanningStatus.Approved, (await db.Context.Plannings.AsNoTracking().SingleAsync(p => p.Id == 1)).Status);
        Assert.Null(await Service(db).GetPendingMaterialChangeAsync(1));
    }

    [Fact]
    public async Task Aprobar_con_faltantes_sin_marca_falla_y_con_marca_aprueba_con_nota()
    {
        using var db = Base();
        db.AddEconomato();
        // Vaso: solo hay 30 en toda la institución y el cambio pide 40.
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 12), new RequestLineInput(Glass, 40)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var sinMarcar = await Service(db).ApproveMaterialChangeAsync(ReviewerUserId, 1, null);
        Assert.False(sinMarcar.Ok);
        Assert.Contains("no alcanza el material", sinMarcar.Message);

        // Sin cambios: el L-3 vigente se mantiene y el pedido sigue pendiente.
        db.Context.ChangeTracker.Clear();
        Assert.Equal(12m, (await db.Context.PlanningRequirements.AsNoTracking().SingleAsync(r => r.PlanningId == 1 && r.ArticleId == Bowl)).RequestedQuantity);
        Assert.NotNull(await Service(db).GetPendingMaterialChangeAsync(1));

        var conMarcar = await Service(db).ApproveMaterialChangeAsync(ReviewerUserId, 1, "Se consigue prestado", allowShortage: true);
        Assert.True(conMarcar.Ok, conMarcar.Message);
        Assert.Contains(conMarcar.Warnings!, w => w.Contains("faltantes"));
        var review9 = await db.Context.PlanningReviews.AsNoTracking().SingleAsync(r => r.Action == PlanningReviewAction.MaterialChangeApproved);
        Assert.StartsWith("Se consigue prestado", review9.Comment);
        Assert.Contains("[Aprobado con faltantes:", review9.Comment);
        Assert.Equal(40m, (await db.Context.PlanningRequirements.AsNoTracking().SingleAsync(r => r.PlanningId == 1 && r.ArticleId == Glass)).RequestedQuantity);
    }

    // ───────── Rechazo ─────────

    [Fact]
    public async Task Rechazar_deja_el_L3_intacto_y_avisa_al_docente()
    {
        using var db = Base();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 10), new RequestLineInput(Glass, 4)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var result = await Service(db).RejectMaterialChangeAsync(ReviewerUserId, 1, "No hay stock del vaso nuevo");

        Assert.True(result.Ok, result.Message);
        var requirements = await db.Context.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == 1).ToListAsync();
        Assert.Equal(12m, requirements.Single(r => r.ArticleId == Bowl).RequestedQuantity);
        Assert.DoesNotContain(requirements, r => r.ArticleId == Glass);
        Assert.Contains(requirements, r => r.ArticleId == Spoon);
        var reviews = await db.Context.PlanningReviews.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(PlanningReviewAction.MaterialChangeRejected, reviews[1].Action);
        Assert.Equal("No hay stock del vaso nuevo", reviews[1].Comment);
        var aviso = await db.Context.Notifications.AsNoTracking().SingleAsync(n => n.UserId == TeacherUserId);
        Assert.Equal("Cambio de material rechazado", aviso.Title);
        Assert.Null(await Service(db).GetPendingMaterialChangeAsync(1));
    }

    [Fact]
    public async Task Rechazar_despues_de_aprobar_falla_porque_ya_no_hay_pendiente()
    {
        using var db = Base();
        db.AddEconomato();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();
        Assert.True((await Service(db).ApproveMaterialChangeAsync(ReviewerUserId, 1, null)).Ok);
        db.Context.ChangeTracker.Clear();

        var rechazo = await Service(db).RejectMaterialChangeAsync(ReviewerUserId, 1, "Ya no corresponde");

        Assert.False(rechazo.Ok);
        Assert.Contains("No hay un pedido de cambio de material pendiente", rechazo.Message);
        Assert.Equal(2, await db.Context.PlanningReviews.AsNoTracking().CountAsync());
    }

    // ───────── Elegibilidad ─────────

    [Fact]
    public async Task Elegibilidad_devuelve_el_motivo_cuando_no_se_puede_pedir()
    {
        using var db = Base();
        Assert.True((await Service(db).CanRequestMaterialChangeAsync(Docente(), 1)).Allowed);

        db.AddDeparture(1, LoanStatus.Active);
        var conSalida = await Service(db).CanRequestMaterialChangeAsync(Docente(), 1);
        Assert.False(conSalida.Allowed);
        Assert.Equal("La práctica ya tiene material entregado: no se puede cambiar su L-3.", conSalida.Reason);

        Assert.False((await Service(db).CanRequestMaterialChangeAsync(Docente(), 99)).Allowed);
    }

    // ───────── Historial ─────────

    [Fact]
    public async Task El_historial_muestra_el_pedido_con_sus_lineas()
    {
        using var db = Base();
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1,
            [new RequestLineInput(Bowl, 10), new RequestLineInput(Glass, 4)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var history = await Service(db).GetHistoryAsync(1);

        var evento = Assert.Single(history);
        Assert.Equal(PlanningReviewAction.MaterialChangeRequested, evento.Action);
        Assert.Equal("Pidió cambio de material", evento.ActionText);
        Assert.Equal(3, evento.Lines.Count);
        Assert.Equal(12m, evento.Lines.Single(l => l.ArticleName == "Bowl").PreviousQuantity);
        Assert.Equal(10m, evento.Lines.Single(l => l.ArticleName == "Bowl").NewQuantity);
        Assert.Null(evento.Lines.Single(l => l.ArticleName == "Cuchara").NewQuantity);
    }

    // ───────── Bandeja y detalle del pendiente ─────────

    [Fact]
    public async Task Bandeja_y_detalle_del_pendiente_muestran_el_pedido()
    {
        using var db = Base();
        db.AddIntern(TestDb.Teacher1, "Luis Docente");
        Assert.True((await Service(db).RequestMaterialChangeAsync(Docente(), 1, [new RequestLineInput(Bowl, 10)], "Motivo del cambio")).Ok);
        db.Context.ChangeTracker.Clear();

        var bandeja = await Service(db).GetPendingMaterialChangesAsync();
        var fila = Assert.Single(bandeja);
        Assert.Equal(1, fila.PlanningId);
        Assert.Equal("Luis Docente", fila.TeacherName);
        Assert.Equal("Práctica 1", fila.Practice);
        Assert.Equal("LAB-B", fila.LabCode);
        Assert.Equal("Motivo del cambio", fila.Reason);
        Assert.Equal(1, fila.QuantityChanges);   // Bowl 12→10
        Assert.Equal(1, fila.ItemChanges);       // Cuchara quitada
        Assert.InRange(fila.DaysAhead, 13, 14);  // la práctica está a 14 días
        Assert.False(fila.LastMinute);
        Assert.False(fila.Warning);
        Assert.Equal(1, await Service(db).CountPendingMaterialChangesAsync());

        var detalle = await Service(db).GetPendingMaterialChangeAsync(1);
        Assert.NotNull(detalle);
        Assert.Equal("Luis Docente", detalle!.RequestedBy);
        Assert.Equal("Motivo del cambio", detalle.Reason);
        Assert.Equal(2, detalle.Lines.Count);
        Assert.Equal("Disminuyó", detalle.Lines.Single(l => l.ArticleId == Bowl).Change);
        Assert.False(detalle.Lines.Single(l => l.ArticleId == Bowl).IsItemChange);
        Assert.Equal("Quitó", detalle.Lines.Single(l => l.ArticleId == Spoon).Change);
        Assert.True(detalle.Lines.Single(l => l.ArticleId == Spoon).IsItemChange);
        Assert.Equal("BOW", detalle.Lines.Single(l => l.ArticleId == Bowl).Code);

        // El stock del L-3 final del pedido (Bowl 10) se revisa excluyendo esta planificación.
        var check = await Service(db).CheckPendingMaterialChangeAsync(1);
        var bowl = Assert.Single(check);
        Assert.Equal(10m, bowl.Requested);
        Assert.Equal(MaterialState.Ok, bowl.State);
    }
}
