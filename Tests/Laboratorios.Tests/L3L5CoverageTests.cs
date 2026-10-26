using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>
/// Cobertura cruzada L-3/L-5: recorte del motivo del historial de estados (BUG-03), devoluciones limitadas
/// al pendiente SIN cubrir por incidentes L-5, cierre de la salida con lo cubierto y tope de L-5 con devuelto.
/// </summary>
public class L3L5CoverageTests
{
    private const int ArticleId = 50;
    private const int EquipmentId = 60;
    private const int UnitId = 61;
    private const int ResponsibleId = 500;

    private static L3CircuitService Circuit(TestDb db) =>
        new(db.Context, NullLogger<L3CircuitService>.Instance);

    /// <summary>Responsable de la devolución: RegisterReturnAsync valida que exista en People.</summary>
    private static void SeedResponsible(TestDb db) => db.AddIntern(ResponsibleId, "Auxiliar de laboratorio");

    /// <summary>
    /// Planificación aprobada con un requerimiento de artículo de inventario y stock en Economato
    /// (AddArticleWithStock siembra el stock en LAB-A, insuficiente para probar la reposición en el origen).
    /// </summary>
    private static PlanningRequirement SeedArticlePlan(TestDb db, int planningId, decimal required, decimal stock)
    {
        db.AddEconomato();
        db.AddPlanning(planningId, TestDb.Week1Monday, 1, 2, status: PlanningStatus.Approved);
        db.AddArticleWithStock(ArticleId, "REB", "Reactivo X", stock: null);
        db.Context.ArticleStocks.Add(new ArticleStock
        {
            ArticleId = ArticleId,
            LaboratoryId = TestDb.EconomatoId,
            AvailableQuantity = stock
        });
        var detail = new PlanningRequirement { PlanningId = planningId, ArticleId = ArticleId, RequestedQuantity = required };
        db.Context.PlanningRequirements.Add(detail);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        return detail;
    }

    [Fact]
    public async Task SaveChanges_IncidenteL5ConDescripcionDe2000_RecortaElMotivoDelHistorial()
    {
        using var db = new TestDb();
        db.AddEquipmentWithUnit(EquipmentId, UnitId, "46019", ArticleId, "EQ-MIC", "Microscopio");

        // El historial se genera en SaveChangesAsync: la siembra síncrona del helper no lo dispara.
        db.Context.L5Incidents.Add(new L5Incident
        {
            EquipmentUnitId = UnitId,
            IncidentType = "Rotura",
            DamageDescription = new string('x', 2000),
            AffectedQuantity = 1
        });
        await db.Context.SaveChangesAsync();

        var history = await db.Context.EquipmentStateHistories.AsNoTracking()
            .SingleAsync(h => h.EquipmentUnitId == UnitId);
        Assert.NotNull(history.Reason);
        // Reason es nvarchar(500): el texto largo (2000) se recorta a 497 + "...".
        Assert.True(history.Reason.Length <= 500, $"Reason mide {history.Reason.Length}");
        Assert.StartsWith("INCIDENTE L5 REGISTRADO", history.Reason);
        Assert.EndsWith("...", history.Reason);
    }

    [Fact]
    public async Task RegisterReturn_ConIncidenteL5_LimitaLaDevolucionAlPendienteSinCubrir()
    {
        using var db = new TestDb();
        SeedResponsible(db);
        var detail = SeedArticlePlan(db, 1, required: 10m, stock: 20m);

        var created = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 10m)], null);
        Assert.True(created.Ok, created.Message);

        // Faltante de 3 unidades declarado en L-5 para el mismo artículo de la planificación.
        db.Context.L5Incidents.Add(new L5Incident
        {
            PlanningId = 1,
            ArticleId = ArticleId,
            IncidentType = "Pérdida",
            DamageDescription = "No volvió del laboratorio",
            AffectedQuantity = 3
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var departureId = await db.Context.Departures.AsNoTracking()
            .Where(d => d.PlanningId == 1).Select(d => d.Id).SingleAsync();
        var itemId = await db.Context.DepartureItems.AsNoTracking()
            .Where(i => i.DepartureId == departureId).Select(i => i.Id).SingleAsync();

        // 8 > 10 entregado − 3 cubiertos: se rechaza sin reponer stock de lo declarado en L-5.
        var fail = await Circuit(db).RegisterReturnAsync(departureId, ResponsibleId, DateTime.Today,
            [new ReturnLine(itemId, 8m)], null);
        Assert.False(fail.Ok);
        Assert.Contains("cubierto por incidentes", fail.Message);

        // 7 = 10 − 3: cierra la salida (devuelto 7 + cubierto 3 >= entregado 10) y repone exactamente 7.
        var ok = await Circuit(db).RegisterReturnAsync(departureId, ResponsibleId, DateTime.Today,
            [new ReturnLine(itemId, 7m)], null);
        Assert.True(ok.Ok, ok.Message);

        var departure = await db.Context.Departures.AsNoTracking().SingleAsync(d => d.Id == departureId);
        Assert.Equal(LoanStatus.Returned, departure.Status);
        Assert.NotNull(departure.ActualReturnDate);

        var stock = await db.Context.ArticleStocks.AsNoTracking()
            .SingleAsync(s => s.LaboratoryId == TestDb.EconomatoId && s.ArticleId == ArticleId);
        Assert.Equal(17m, stock.AvailableQuantity);   // 20 − 10 entregado + 7 devuelto
    }

    [Fact]
    public async Task RegisterReturn_ParcialSinL5_LaSalidaSigueActiva()
    {
        using var db = new TestDb();
        SeedResponsible(db);
        var detail = SeedArticlePlan(db, 1, required: 10m, stock: 20m);

        var created = await Circuit(db).CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detail.Id, 10m)], null);
        Assert.True(created.Ok, created.Message);

        var departureId = await db.Context.Departures.AsNoTracking()
            .Where(d => d.PlanningId == 1).Select(d => d.Id).SingleAsync();
        var itemId = await db.Context.DepartureItems.AsNoTracking()
            .Where(i => i.DepartureId == departureId).Select(i => i.Id).SingleAsync();

        var result = await Circuit(db).RegisterReturnAsync(departureId, ResponsibleId, DateTime.Today,
            [new ReturnLine(itemId, 6m)], null);
        Assert.True(result.Ok, result.Message);

        var departure = await db.Context.Departures.AsNoTracking().SingleAsync(d => d.Id == departureId);
        Assert.Equal(LoanStatus.Active, departure.Status);

        var stock = await db.Context.ArticleStocks.AsNoTracking()
            .SingleAsync(s => s.LaboratoryId == TestDb.EconomatoId && s.ArticleId == ArticleId);
        Assert.Equal(16m, stock.AvailableQuantity);   // 20 − 10 entregado + 6 devuelto
    }

    [Theory]
    [InlineData(10d, 6d, 0, 4)]
    [InlineData(10d, 6d, 4, 0)]
    [InlineData(2.5d, 1d, 0, 2)]
    [InlineData(10d, 0d, 3, 7)]
    [InlineData(5d, 8d, 0, 0)]
    public void MaxAdditional_DescuentaDevueltoYRegistrado(double delivered, double returned, int alreadyRegistered, int expected)
    {
        Assert.Equal(expected, L5Rules.MaxAdditional((decimal)delivered, (decimal)returned, alreadyRegistered));
    }
}
